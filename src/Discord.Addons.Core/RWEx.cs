using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Addons.Core
{
    internal static class RWEx
    {
        [DebuggerStepThrough]
        internal static AcquiredReadLock UsingReadLock(this ReaderWriterLockSlim readerWriterLock)
            => new(readerWriterLock);
        [DebuggerStepThrough]
        internal static AcquiredWriteLock UsingWriteLock(this ReaderWriterLockSlim readerWriterLock)
            => new(readerWriterLock);
        [DebuggerStepThrough]
        internal static async Task<AcquiredSemaphoreSlim> UsingSemaphore(this SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            return new AcquiredSemaphoreSlim(semaphore);
        }

        internal readonly struct AcquiredReadLock : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;

            [DebuggerStepThrough]
            public AcquiredReadLock(ReaderWriterLockSlim @lock)
            {
                _lock = @lock;
                _lock.EnterReadLock();
            }

            [DebuggerStepThrough]
            public void Dispose() => _lock.ExitReadLock();
        }
        internal readonly struct AcquiredWriteLock : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;

            [DebuggerStepThrough]
            public AcquiredWriteLock(ReaderWriterLockSlim @lock)
            {
                _lock = @lock;
                _lock.EnterWriteLock();
            }

            [DebuggerStepThrough]
            public void Dispose() => _lock.ExitWriteLock();
        }
        internal readonly struct AcquiredSemaphoreSlim : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            [DebuggerStepThrough]
            public AcquiredSemaphoreSlim(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            [DebuggerStepThrough]
            public void Dispose() => _semaphore.Release();
        }
    }
}
