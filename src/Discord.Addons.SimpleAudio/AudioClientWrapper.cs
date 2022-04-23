using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using JiiLib.Media;
using Discord.Addons.Core;
using Discord.Audio;

namespace Discord.Addons.SimpleAudio;

internal sealed class AudioClientWrapper
{
    private static readonly Embed _readyEmbed = new EmbedBuilder { Title = "Connected", Description = "Ready" }.Build();
    private static readonly IEmote _playEmote = new Emoji("\u25B6");
    private static readonly IEmote _pauseEmote = new Emoji("\u23F8");
    private static readonly IEmote _stopEmote = new Emoji("\u23F9");
    private static readonly IEmote _fwdEmote = new Emoji("\u23ED");
    private static readonly IEmote _ejectEmote = new Emoji("\u23CF");
    private static readonly IEmote[] _emotes = new[] { _stopEmote, _playEmote, _pauseEmote, _fwdEmote, _ejectEmote };

    //private readonly object _cancelLock = new();
    private ConcurrentQueue<FileInfo> Playlist { get; } = new();

    internal IAudioClient Client { get; }
    internal IUserMessage Message { get; }
    //internal ulong MessageId => Message.Id;

    private bool _next = false;
    //private int _paused = 0;
    private float _playingVolume = 0.5f;
    //private CancellationTokenSource _cancelToken = new();
    //private TaskCompletionSource<bool> _pauseTask = new();

    private IMediaTag? _songTags;
    private IEmote _statusEmote = _stopEmote;
    private Color _statusColor;
    private Process? _ffmpeg;
    private AsyncPause.WithCancel _pauser = new();

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
            await using (var outputStream = Client.CreatePCMStream(AudioApplication.Music))
            {
                await PausableCopyToAsync(_ffmpeg!.StandardOutput.BaseStream, outputStream, 8192)
                    .ContinueWith(static (_, s) =>
                        {
                            var @this = (AudioClientWrapper)s!;
                            if (@this._pauser.IsCanceled)
                            {
                                if (@this._next)
                                    @this._next = false;
                                else
                                    @this.Playlist.Clear();
                            }

                            @this._pauser = new();
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
        _pauser.Pause();
        _statusEmote = _pauseEmote;
        _statusColor = Color.Blue;
        return RefreshEmbed();
    }

    public Task Resume()
    {
        _pauser.Resume();
        _statusEmote = _playEmote;
        _statusColor = Color.Green;
        return RefreshEmbed();
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
        if (IsPlaying())
        {
            _pauser.Cancel();
        }
        _statusColor = Color.Red;
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
        Contract.Requires(buffersize > 0 && IsDiv4((uint)buffersize), $"{nameof(buffersize)} is 0 or less or not a multiple of 4.");

        var buffer = AllocExtensions.AllocateAlignedBuffer<ushort>(buffersize);
        int count;

#pragma warning disable CA1835 // Arbitrary streams aren't guaranteed to override the Memory overloads for better perf.
        while ((count = await source.ReadAsync(buffer.Array!, buffer.Offset, buffersize).ConfigureAwait(false)) > 0
            && await _pauser.WaitForResumeOrCancelAsync().ConfigureAwait(false))
        {
            ScaleVolumeSpanOfT(buffer.AsSpan()[buffer.Offset..count], _playingVolume);
            await destination.WriteAsync(buffer.Array!, buffer.Offset, count).ConfigureAwait(false);
        }
#pragma warning restore
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
    private static bool IsDiv4(uint number)
    {
        if (Bmi1.IsSupported)
            return Bmi1.TrailingZeroCount(number) >= 2;

        const uint magic = ~0b11u;
        return (number | magic) == magic;
    }

    // https://gist.github.com/Joe4evr/e102d8d8627989a61624237e44210838
    private static void ScaleVolumeSpanOfT(Span<byte> audioSamples, float volume)
    {
        Contract.Requires(volume is >= 0f and <= 1f);
        Contract.Assert(BitConverter.IsLittleEndian);

        // Don't change if the volume factor is too small.
        if (MathF.Abs(volume - 1f) < 0.0001f)
            return;

        // 'volume' is bound between 0 and 1 so this should be good, right?
        ushort volumeFixed = (ushort)MathF.Round(volume * UInt16.MaxValue);
        var asShorts = MemoryMarshal.Cast<byte, ushort>(audioSamples);

        if (Vector.IsHardwareAccelerated)
        {
            // Curious if the copying back-and-forth isn't taking away
            // too much of the advantage of hw-accelerated multiplication
            // compared to changing the values in-place....
            Vector.Multiply(new Vector<ushort>(asShorts), volumeFixed).CopyTo(asShorts);
        }
        else
        {
            // JIT elides bounds-checks when iterating forwards.
            // See: https://github.com/dotnet/runtime/issues/9505
            for (int i = 0; i < asShorts.Length; i++)
                asShorts[i] = (ushort)((asShorts[i] * volumeFixed) >> 16);
        }
    }

}
