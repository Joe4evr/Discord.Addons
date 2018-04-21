using System;
using System.Diagnostics;
using System.Threading;

namespace Discord.Addons.Core
{
    internal static class RWEx
    {
        [DebuggerStepThrough]
        public static IDisposable UsingReadLock(this ReaderWriterLockSlim readerWriterLock)
            => new ReadLock(readerWriterLock);
        [DebuggerStepThrough]
        public static IDisposable UsingWriteLock(this ReaderWriterLockSlim readerWriterLock)
            => new WriteLock(readerWriterLock);

        private sealed class ReadLock : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;

            [DebuggerStepThrough]
            public ReadLock(ReaderWriterLockSlim @lock)
            {
                _lock = @lock;
                _lock.EnterReadLock();
            }

            [DebuggerStepThrough]
            public void Dispose() => _lock.ExitReadLock();
        }
        private sealed class WriteLock : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;

            [DebuggerStepThrough]
            public WriteLock(ReaderWriterLockSlim @lock)
            {
                _lock = @lock;
                _lock.EnterWriteLock();
            }

            [DebuggerStepThrough]
            public void Dispose() => _lock.ExitWriteLock();
        }
    }
}
