using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MpGame.Tests
{
    internal interface ITestPileEvents
    {
        event EventHandler<EventArgs> LastRemoveCalled;
        event EventHandler<PutEventArgs> PutCalled;
        event EventHandler<ShuffleEventArgs> ShuffleCalled;

        Func<IEnumerable<ITestCard>, IEnumerable<ITestCard>> ShuffleFuncOverride { set; }
    }

    //internal class PileEventArgs : EventArgs
    //{

    //}

    internal sealed class PutEventArgs : EventArgs
    {
        public PutEventArgs(ITestCard card)
        {
            Card = card;
        }

        public ITestCard Card { get; }
    }

    internal sealed class ShuffleEventArgs : EventArgs
    {
        public ShuffleEventArgs(
            IEnumerable<ITestCard> originalSequence,
            IEnumerable<ITestCard> newSequence)
        {
            OriginalSequence = originalSequence;
            NewSequence = newSequence;
        }

        public IEnumerable<ITestCard> OriginalSequence { get; }
        public IEnumerable<ITestCard> NewSequence { get; }
    }
}
