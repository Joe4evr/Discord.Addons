using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Addons.Core
{
    internal sealed class AsyncReaderWriterLock
    {
        private readonly Queue<ReaderLock> _readerLocks = new();

        private WriterLock? _writerLock;

        /// <summary>
        ///     The object used for mutual exclusion.
        /// </summary>
        private readonly object _mutex = new();

        ///// <summary>
        /////   The semi-unique identifier for this instance. This is 0 if the id has not yet been created.
        ///// </summary>
        //private int _id;

        /// <summary>
        ///     Number of reader locks held; -1 if a writer lock is held; 0 if no locks are held.
        /// </summary>
        private int _locksHeld;

        [DebuggerNonUserCode]
        internal State GetStateForDebugger
        {
            get
            {
                if (_locksHeld == 0)
                    return State.Unlocked;
                if (_locksHeld == -1)
                    return State.WriteLocked;
                return State.ReadLocked;
            }
        }

        internal enum State
        {
            Unlocked,
            ReadLocked,
            WriteLocked,
        }

        [DebuggerNonUserCode]
        internal int GetReaderCountForDebugger { get { return (_locksHeld > 0 ? _locksHeld : 0); } }

        ///// <summary>
        ///// Applies a continuation to the task that will call <see cref="ReleaseWaiters"/> if the task is canceled. This method may not be called while holding the sync lock.
        ///// </summary>
        ///// <param name="task">The task to observe for cancellation.</param>
        //private void ReleaseWaitersWhenCanceled(Task task)
        //{
        //    task.ContinueWith(_ =>
        //    {
        //        lock (_mutex) { ReleaseWaiters(); }
        //    }, CancellationToken.None, TaskContinuationOptions.OnlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        //}

//        /// <summary>
//        /// Grants lock(s) to waiting tasks. This method assumes the sync lock is already held.
//        /// </summary>
//        private void ReleaseWaiters()
//        {
//            if (_locksHeld == -1)
//                return;

//            // Give priority to writers, then readers.
//            if (!_writerQueue.IsEmpty)
//            {
//                if (_locksHeld == 0)
//                {
//                    _locksHeld = -1;
//#pragma warning disable CA2000 // Dispose objects before losing scope
//                    _writerQueue.Dequeue(new WriterKey(this));
//#pragma warning restore CA2000 // Dispose objects before losing scope
//                    return;
//                }
//            }
//            else
//            {
//                while (!_readerQueue.IsEmpty)
//                {
//#pragma warning disable CA2000 // Dispose objects before losing scope
//                    _readerQueue.Dequeue(new ReaderKey(this));
//#pragma warning restore CA2000 // Dispose objects before losing scope
//                    ++_locksHeld;
//                }
//            }
//        }

        public ReaderLock AcquireReadLock()
        {
            lock (_mutex)
            {
                if (_locksHeld >= 0)
                {
                    ++_locksHeld;
                    var rl = new ReaderLock(this);
                    _readerLocks.Enqueue(rl);
                    return rl;
                }
                else
                {
                    throw new LockRecursionException();
                }
            }
        }

        public WriterLock AcquireWriteLock()
        {
            lock (_mutex)
            {
                if (_locksHeld == 0)
                {
                    _locksHeld = -1;
                    var wl = new WriterLock(this);
                    _writerLock = wl;
                    return wl;
                }
                else
                {
                    throw new LockRecursionException();
                }
            }
        }

        private void ReleaseReadLock()
        {
            lock (_mutex)
            {
                if (_locksHeld > 0)
                {
                    _ = _readerLocks.Dequeue();
                    --_locksHeld;
                }
            }
        }
        private void ReleaseWriteLock()
        {
            lock (_mutex)
            {
                if (_locksHeld == -1)
                {
                    _writerLock = null;
                    _locksHeld = 0;
                }
            }
        }

        private void ReleaseAll()
        {
            if (_locksHeld == -1)
            {
                _writerLock?.Dispose();
            }
            else
            {
                lock (_mutex)
                {
                    _readerLocks.Clear();
                    _locksHeld = 0;
                }
            }

        }

        public readonly struct ReaderLock : IDisposable
        {
            private readonly AsyncReaderWriterLock _arwl;

            public ReaderLock(AsyncReaderWriterLock arwl)
            {
                _arwl = arwl;
            }

            public void Dispose()
            {
                _arwl.ReleaseReadLock();
            }
        }

        public readonly struct WriterLock : IDisposable
        {
            private readonly AsyncReaderWriterLock _arwl;

            public WriterLock(AsyncReaderWriterLock arwl)
            {
                _arwl = arwl;
            }

            public void Dispose()
            {
                _arwl.ReleaseWriteLock();
            }
        }
    }
}
