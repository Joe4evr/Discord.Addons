using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord.Audio;
using System.IO;

namespace Discord.Addons.SimpleAudio
{
    internal sealed class AudioClientWrapper
    {
        private CancellationTokenSource _cancel = new CancellationTokenSource();
        private bool _next = false;
        private Process _ffmpeg;
        private double _defaultVolume = 0.1;

        public IAudioClient Client { get; }
        public ConcurrentQueue<string> Playlist { get; } = new ConcurrentQueue<string>();

        public AudioClientWrapper(IAudioClient client)
        {
            Client = client;
        }

        public async Task SendAudioAsync(string ffmpeg)
        {
            while (!Playlist.IsEmpty)
            {
                if (Playlist.TryDequeue(out var path))
                {
                    using (_ffmpeg = Process.Start(new ProcessStartInfo
                    {
                        FileName = ffmpeg,
                        Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = false
                    }))
                    {
                        var stream = Client.CreatePCMStream(AudioApplication.Music);
                        try
                        {
                            await _ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream, 81920, _cancel.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            if (!_next)
                            {
                                //TODO: Replace with .Clear() when NET Core 2.0 is a thing
                                while (Playlist.TryDequeue(out var _)) {}
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
                            _cancel.Dispose();
                            _cancel = new CancellationTokenSource();
                        }
                    }
                }
            }
        }

        public void SetVolume(double newVolume)
        {
            _defaultVolume = newVolume;
        }

        public void Cancel()
        {
            _cancel.Cancel();
            _ffmpeg.Kill();
        }

        public void SkipToNext()
        {
            _next = true;
            Cancel();
        }

        public bool IsPlaying()
        {
            if (_ffmpeg == null)
            {
                return false;
            }

            // Don't try this at home - I'm a professional
            var disposedField = typeof(Process)
                .GetTypeInfo()
                .GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic);
            var isDisposed = (bool)disposedField.GetValue(_ffmpeg);

            return !(isDisposed || _ffmpeg.HasExited);
        }

        private bool _pause = false;
        private CancellationTokenSource _pauseToken = new CancellationTokenSource();

        public void Pause() => _pause = true;
        public void Resume()
        {
            if (_pause)
            {
                _pauseToken.Cancel();
                _pauseToken.Dispose();
                _pauseToken = new CancellationTokenSource();
            }
        }

        private async Task PausableCopyToAsync(Stream source, Stream destination, int buffersize)
        {
            byte[] buffer = new byte[buffersize];
            int offset = 0;
            int count = await source.ReadAsync(buffer, offset, buffersize);

            while (count > 0)
            {
                if (_pause)
                {
                    await Task.Delay(-1, _pauseToken.Token);
                }
                await destination.WriteAsync(buffer, offset, count);
                offset = count;
                count = await source.ReadAsync(buffer, offset, buffersize);
            }
        }
    }
}
