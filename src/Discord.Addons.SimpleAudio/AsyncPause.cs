using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Addons.SimpleAudio;

[StructLayout(LayoutKind.Auto)]
internal struct AsyncPause
{
    private TaskCompletionSource<bool>? _tcs;

    // Open question: Use TaskCreationOptions other than the default?
    public void Pause() => Interlocked.CompareExchange(ref _tcs, new(), null);

    public void Resume() => Interlocked.Exchange(ref _tcs, null)?.TrySetResult(true);

    public readonly Task WaitForResumeAsync() => _tcs?.Task ?? Task.CompletedTask;

    [StructLayout(LayoutKind.Auto)]
    internal struct WithCancel
    {
        private AsyncPause _pause;

        public bool IsCanceled { get; private set; }

        public void Pause() => _pause.Pause();
        public void Resume()
        {
            if (!IsCanceled)
                _pause.Resume();
        }

        public void Cancel()
        {
            IsCanceled = true;
            (_pause._tcs ??= new()).TrySetResult(false);
        }
        public readonly Task<bool> WaitForResumeOrCancelAsync()
            => IsCanceled
                ? Task.FromResult(false)
                : _pause._tcs?.Task ?? Task.FromResult(true);
    }
}
