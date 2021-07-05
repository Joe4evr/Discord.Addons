using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using JiiLib.Media;
using Discord.Addons.Core;
using Discord.Audio;

#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics.X86;
#else
using System.Numerics;
#endif

namespace Discord.Addons.SimpleAudio
{
    internal sealed class AudioClientWrapper
    {
        private static readonly Embed _readyEmbed = new EmbedBuilder { Title = "Connected", Description = "Ready" }.Build();
        private static readonly IEmote _playEmote = new Emoji("\u25B6");
        private static readonly IEmote _pauseEmote = new Emoji("\u23F8");
        private static readonly IEmote _stopEmote = new Emoji("\u23F9");
        private static readonly IEmote _fwdEmote = new Emoji("\u23ED");
        private static readonly IEmote _ejectEmote = new Emoji("\u23CF");
        private static readonly IEmote[] _emotes = new[] { _stopEmote, _playEmote, _pauseEmote, _fwdEmote, _ejectEmote };

        private readonly object _cancelLock = new();
        private ConcurrentQueue<FileInfo> Playlist { get; } = new();

        internal IAudioClient Client { get; }
        internal IUserMessage Message { get; }
        //internal ulong MessageId => Message.Id;

        private bool _next = false;
        private int _pause = 0;
        private float _playingVolume = 0.5f;
        private CancellationTokenSource _cancelToken = new();
        private TaskCompletionSource<bool> _pauseTask = new();

        private IMediaTag? _songTags;
        private IEmote _statusEmote = _stopEmote;
        private Color _statusColor;
        private Process? _ffmpeg;

        public AudioClientWrapper(
            IAudioClient client, IUserMessage message,
            IAudioConfig globalConfig, IAudioGuildConfig? guildConfig = null)
        {
            Message = message;
            Client = client;
            if (guildConfig?.AllowReactions ?? globalConfig.AllowReactions)
            {
                _ = message.AddReactionsAsync(_emotes);
            }
            message.ModifyAsync(m => m.Embed = _readyEmbed);
        }

        public async Task SendAudioAsync(FileInfo ffmpeg)
        {
            while (Playlist.TryDequeue(out var path))
            {
                MediaTag.Parse(path, out _songTags);
                _statusEmote = _playEmote;
                _statusColor = Color.Green;
                await RefreshEmbed().ConfigureAwait(false);

                using (_ffmpeg = Process.Start(new ProcessStartInfo
                {
                    FileName = ffmpeg.FullName,
                    Arguments = $"-hide_banner -loglevel panic -i \"{path.FullName}\" -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    //RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = false
                }))
                //using (var input = _ffmpeg.StandardInput.BaseStream)
                await using (var stream = Client.CreatePCMStream(AudioApplication.Music))
                {
                    await PausableCopyToAsync(_ffmpeg.StandardOutput.BaseStream, stream, 8192)
                        .OnCancellation(static (_, @this) =>
                            {
                                if (@this._next)
                                    @this._next = false;
                                else
                                    @this.Playlist.Clear();
                            }, this)
                        .ConfigureAwait(false);
                }
            }

            _ffmpeg = null;
            _songTags = null;
            _statusEmote = _stopEmote;
            _statusColor = Color.Red;
            await RefreshEmbed().ConfigureAwait(false);
        }

        public Task AddToPlaylist(FileInfo file)
        {
            Playlist.Enqueue(file);
            return (IsPlaying() || Playlist.Count > 1)
                ? RefreshEmbed()
                : Task.CompletedTask;
        }

        public Task AddToPlaylist(IEnumerable<FileInfo> files)
        {
            foreach (var item in files)
            {
                Playlist.Enqueue(item);
            }
            return RefreshEmbed();
        }

        public Task Pause()
        {
            if (Interlocked.Exchange(ref _pause, value: 1) == 0)
            {
                _statusEmote = _pauseEmote;
                _statusColor = Color.Blue;
                return RefreshEmbed();
            }
            return Task.CompletedTask;
        }

        public Task Resume()
        {
            if (Interlocked.Exchange(ref _pause, value: 0) == 1)
            {
                _pauseTask.SetResult(true);
                _pauseTask = new TaskCompletionSource<bool>();
                _statusEmote = _playEmote;
                _statusColor = Color.Green;
                return RefreshEmbed();
            }
            return Task.CompletedTask;
        }

        public void SkipToNext()
        {
            if (!Playlist.IsEmpty)
            {
                _next = true;
                Stop();
            }
        }

        public void Stop()
        {
            lock (_cancelLock)
            {
                if (IsPlaying())
                {
                    using (_cancelToken)
                    {
                        _cancelToken.Cancel();
                    }
                    _cancelToken = new CancellationTokenSource();
                }
                _statusColor = Color.Red;
            }
        }

        public Task SetVolume(float newVolume)
        {
            _playingVolume = newVolume;
            return RefreshEmbed();
        }

        public bool IsPlaying()
        {
            return !(_ffmpeg == null || _ffmpeg.HasExited);
        }

        private async Task PausableCopyToAsync(Stream source, Stream destination, int buffersize)
        {
            //Contract.Requires(source != null && source.CanRead, $"{nameof(source)} is null or not readable.");
            //Contract.Requires(destination != null && destination.CanWrite, $"{nameof(destination)} is null or not writable.");
            Contract.Requires(buffersize > 0 && IsAligned((uint)buffersize), $"{nameof(buffersize)} is 0 or less or not a multiple of 4.");

            byte[] buffer = new byte[buffersize];
            int count;

            while ((count = await source.ReadAsync(buffer, 0, buffersize, _cancelToken.Token).ConfigureAwait(false)) > 0)
            {
                if (_pause > 0)
                {
                    await _pauseTask.Task;
                }

                ScaleVolumeSpanOfT(buffer.AsSpan()[..count], _playingVolume);
                await destination.WriteAsync(buffer, 0, count, _cancelToken.Token).ConfigureAwait(false);
            }
        }

        private Task RefreshEmbed()
        {
            var emb = new EmbedBuilder
            {
                Title = "Now playing:",
                Description = $"{_statusEmote ?? _stopEmote} {_songTags?.Title ?? "*Stopped*"}",
                Color = _statusColor,
                Fields =
                {
                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Volume:",
                        Value = $"{(int)(_playingVolume * 101)}%"
                    },
                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Next song:",
                        Value = PeekNextSongTitle() ?? "*(None)*"
                    }
                }
            }.Build();
            return Message.ModifyAsync(m => m.Embed = emb);

            string? PeekNextSongTitle()
            {
                if (Playlist.TryPeek(out var next))
                {
                    MediaTag.Parse(next, out var tags);
                    return tags?.Title ?? next.Name;
                }

                return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAligned(uint number)
        {
#if NETCOREAPP3_0_OR_GREATER
            if (Bmi1.IsSupported)
                return Bmi1.TrailingZeroCount(number) >= 2;
#endif

            const uint magic = ~0b11u;
            return (number | magic) == magic;
        }

        //https://hastebin.com/umapabejis.cs
        //private unsafe static byte[] ScaleVolumeUnsafeNoAlloc(byte[] audioSamples, float volume)
        //{
        //    Contract.Requires(audioSamples != null);
        //    Contract.Requires(volume >= 0f && volume <= 1f);
        //    Contract.Assert(BitConverter.IsLittleEndian);

        //    if (Math.Abs(volume - 1f) < 0.0001f) return audioSamples;

        //    // 16-bit precision for the multiplication
        //    int volumeFixed = (int)Math.Round(volume * 65536d);

        //    int count = audioSamples.Length >> 1;

        //    fixed (byte* srcBytes = audioSamples)
        //    {
        //        short* src = (short*)srcBytes;

        //        for (int i = count; i != 0; i--, src++)
        //            *src = (short)(((*src) * volumeFixed) >> 16);
        //    }

        //    return audioSamples;
        //}

        //https://gist.github.com/Joe4evr/e102d8d8627989a61624237e44210838
        private static void ScaleVolumeSpanOfT(Span<byte> audioSamples, float volume)
        {
            Contract.Requires(volume >= 0f && volume <= 1f);
            Contract.Assert(BitConverter.IsLittleEndian);

            // Don't change if the volume factor is too small
            if (Math.Abs(volume - 1f) < 0.0001f)
                return;

            // 16-bit precision for the multiplication
            int volumeFixed = (int)Math.Round(volume * 65536d);

            // Reinterpret the bytes as shorts
            var asShorts = MemoryMarshal.Cast<byte, ushort>(audioSamples);

            // JIT actually elides bounds-checks when iterating forwards
            // See: https://github.com/dotnet/runtime/issues/9505
            for (int i = 0; i < asShorts.Length; i++)
                asShorts[i] = (ushort)((asShorts[i] * volumeFixed) >> 16);
        }
    }
}
