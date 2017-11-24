using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
//using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Discord.Audio;

namespace Discord.Addons.SimpleAudio
{
    internal sealed class AudioClientWrapper
    {
        private static readonly IEmote _playEmote = new Emoji("▶️");
        private static readonly IEmote _pauseEmote = new Emoji("⏸️");
        private static readonly IEmote _stopEmote = new Emoji("⏹️");

        private readonly object _cancelLock = new object();
        //private readonly object _pauseLock = new object();
        private readonly ConcurrentQueue<string> _playlist = new ConcurrentQueue<string>();

        private readonly IUserMessage _message;

        private bool _next = false;
        private int _pause = 0;
        private float _playingVolume = 0.5f;
        private CancellationTokenSource _cancelToken = new CancellationTokenSource();
        private CancellationTokenSource _pauseToken = new CancellationTokenSource();

        private string _song;
        private IEmote _statusEmote;
        private Process _ffmpeg;

        public IAudioClient Client { get; }

        public AudioClientWrapper(IAudioClient client, IUserMessage message)
        {
            _message = message;
            Client = client;
        }

        public async Task SendAudioAsync(string ffmpeg)
        {
            while (!_playlist.IsEmpty)
            {
                if (_playlist.TryDequeue(out var path))
                {
                    _song = Path.GetFileNameWithoutExtension(path);
                    _statusEmote = _playEmote;
                    await RefreshEmbed().ConfigureAwait(false);

                    using (_ffmpeg = Process.Start(new ProcessStartInfo
                    {
                        FileName = ffmpeg,
                        Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = false
                    }))
                    using (var output = _ffmpeg.StandardOutput.BaseStream)
                    using (var stream = Client.CreatePCMStream(AudioApplication.Music))
                    {
                        try
                        {
                            await PausableCopyToAsync(output, stream, 81920).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            if (!_next)
                            {
                                //TODO: Replace with .Clear() when NET Core 2.0 is a thing
                                while (_playlist.TryDequeue(out var _)) { }
                                break;
                            }
                            else
                            {
                                _next = false;
                            }
                        }
                        finally
                        {
                            await stream.FlushAsync().ConfigureAwait(false);
                        }
                    }
                }
            }

            _song = "Stopped";
            _statusEmote = _stopEmote;
            await RefreshEmbed().ConfigureAwait(false);
        }

        public Task AddToPlaylist(string file)
        {
            _playlist.Enqueue(file);
            return (IsPlaying() || _playlist.Count > 1)
                ? RefreshEmbed()
                : Task.CompletedTask;
        }

        public Task AddToPlaylist(IEnumerable<string> files)
        {
            foreach (var item in files)
            {
                _playlist.Enqueue(item);
            }
            return RefreshEmbed();
        }

        // setting '_pause' to true multiple times
        // in a row *should* be completely thread-safe
        public Task Pause()
        {
            if (_pause == 0)
            {
                _pause = 1;
                _statusEmote = _pauseEmote;
                return RefreshEmbed();
            }
            return Task.CompletedTask;
        }

        public Task Resume()
        {
            if (Interlocked.Exchange(ref _pause, value: 0) == 1)
            {
                using (_pauseToken)
                {
                    _pauseToken.Cancel();
                }
                _pauseToken = new CancellationTokenSource();
                _statusEmote = new Emoji("▶️");
                return RefreshEmbed();
            }
            return Task.CompletedTask;
        }

        public void SkipToNext()
        {
            if (!_playlist.IsEmpty)
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
            Contract.Requires(source != null && source.CanRead, $"{nameof(source)} is null or not readable.");
            Contract.Requires(destination != null && destination.CanWrite, $"{nameof(destination)} is null or not writable.");
            Contract.Requires(buffersize > 0 && IsEvenBinaryOp(buffersize), $"{nameof(buffersize)} is 0 or less or not even.");

            byte[] buffer = new byte[buffersize];
            int count;

            while ((count = await source.ReadAsync(buffer, 0, buffersize, _cancelToken.Token).ConfigureAwait(false)) > 0)
            {
                if (_pause > 0)
                {
                    try
                    {
                        await Task.Delay(Timeout.Infinite, _pauseToken.Token);
                    }
                    catch (OperationCanceledException) { }
                }

                var volAdjusted = ScaleVolumeUnsafeNoAlloc(buffer, _playingVolume);
                await destination.WriteAsync(volAdjusted, 0, count, _cancelToken.Token).ConfigureAwait(false);
            }
        }

        private Task RefreshEmbed()
        {
            var emb = new EmbedBuilder
            {
                Title = "Now playing:",
                Description = $"{_statusEmote ?? _stopEmote} {_song ?? "*Stopped*"}",
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
                        Value = (_playlist.TryPeek(out var n) ? Path.GetFileNameWithoutExtension(n) : "*(None)*")
                    }
                }
            }.Build();
            return _message.ModifyAsync(m => m.Embed = emb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsEvenBinaryOp(int number)
        {
            const int magic = Int32.MaxValue - 1;
            return (number | magic) == magic;
        }

        //https://hastebin.com/umapabejis.cs
        private unsafe static byte[] ScaleVolumeUnsafeNoAlloc(byte[] audioSamples, float volume)
        {
            Contract.Requires(audioSamples != null);
            Contract.Requires(volume >= 0f && volume <= 1f);
            Contract.Assert(BitConverter.IsLittleEndian);

            if (Math.Abs(volume - 1f) < 0.0001f) return audioSamples;

            // 16-bit precision for the multiplication
            int volumeFixed = (int)Math.Round(volume * 65536d);

            int count = audioSamples.Length >> 1;

            fixed (byte* srcBytes = audioSamples)
            {
                short* src = (short*)srcBytes;

                for (int i = count; i != 0; i--, src++)
                    *src = (short)(((*src) * volumeFixed) >> 16);
            }

            return audioSamples;
        }
    }
}
