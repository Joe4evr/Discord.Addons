using System;
using System.Diagnostics;
using System.Threading;

namespace Discord.Addons.Core
{
    internal static class RWEx
    {
        [DebuggerStepThrough]
        internal static AcquiredReadLock UsingReadLock(this ReaderWriterLockSlim readerWriterLock)
            => new AcquiredReadLock(readerWriterLock);
        [DebuggerStepThrough]
        internal static AcquiredWriteLock UsingWriteLock(this ReaderWriterLockSlim readerWriterLock)
            => new AcquiredWriteLock(readerWriterLock);

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
            void IDisposable.Dispose() => _lock.ExitReadLock();
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
            void IDisposable.Dispose() => _lock.ExitWriteLock();
        }
    }
}
