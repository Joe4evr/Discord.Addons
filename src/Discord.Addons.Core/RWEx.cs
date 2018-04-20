using System;
using System.Threading;

namespace Discord.Addons.Core
{
    internal static class RWEx
    {
        public static IDisposable UsingReadLock(this ReaderWriterLockSlim readerWriterLock)
            => new ReadLock(readerWriterLock);
        public static IDisposable UsingWriteLock(this ReaderWriterLockSlim readerWriterLock)
            => new WriteLock(readerWriterLock);

        private sealed class ReadLock : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;

            public ReadLock(ReaderWriterLockSlim @lock)
            {
                _lock = @lock;
                _lock.EnterReadLock();
            }

            public void Dispose() => _lock.ExitReadLock();
        }
        private sealed class WriteLock : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;

            public WriteLock(ReaderWriterLockSlim @lock)
            {
                _lock = @lock;
                _lock.EnterWriteLock();
            }

            public void Dispose() => _lock.ExitWriteLock();
        }
    }
}
