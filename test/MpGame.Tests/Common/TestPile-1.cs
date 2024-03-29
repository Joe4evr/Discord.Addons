﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Discord.Addons.MpGame.Collections;

namespace MpGame.Tests
{
    internal sealed class TestPile : Pile<ITestCard>, ITestPileEvents
    {
        private readonly PilePerms _perms;

        public TestPile(PilePerms withPerms, IEnumerable<ITestCard> items)
            : this(withPerms, items, false)
        {
        }

        public TestPile(PilePerms withPerms, IEnumerable<ITestCard> items, bool initShuffle)
            : base(items, initShuffle: initShuffle)
        {
            _perms = withPerms;
        }

        public override bool CanBrowse     => HasPerm(PilePerms.CanBrowse);
        public override bool CanClear      => HasPerm(PilePerms.CanClear);
        public override bool CanCut        => HasPerm(PilePerms.CanCut);
        public override bool CanDraw       => HasPerm(PilePerms.CanDraw);
        public override bool CanDrawBottom => HasPerm(PilePerms.CanDrawBottom);
        public override bool CanInsert     => HasPerm(PilePerms.CanInsert);
        public override bool CanPeek       => HasPerm(PilePerms.CanPeek);
        public override bool CanPut        => HasPerm(PilePerms.CanPut);
        public override bool CanPutBottom  => HasPerm(PilePerms.CanPutBottom);
        public override bool CanShuffle    => HasPerm(PilePerms.CanShuffle);
        public override bool CanTake       => HasPerm(PilePerms.CanTake);


        public event EventHandler<EventArgs>? LastRemoveCalled;
        protected override void OnLastRemoved()
        {
            base.OnLastRemoved();
            LastRemoveCalled?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<PutEventArgs>? PutCalled;
        protected override void OnPut(ITestCard card)
        {
            base.OnPut(card);
            PutCalled?.Invoke(this, new PutEventArgs(card));
        }

        public Func<IEnumerable<ITestCard>, IEnumerable<ITestCard>>? ShuffleFuncOverride { private get; set; }
        public event EventHandler<ShuffleEventArgs>? ShuffleCalled;
        protected override IEnumerable<ITestCard> ShuffleItems(IEnumerable<ITestCard> items)
        {
            var shuffled = (ShuffleFuncOverride is null)
                ? items.Reverse()
                : ShuffleFuncOverride.Invoke(items);

            ShuffleCalled?.Invoke(this,
                new ShuffleEventArgs(originalSequence: items, newSequence: shuffled));

            return shuffled;
        }



        private bool HasPerm(PilePerms perm) => (_perms & perm) == perm;
    }

    [Flags]
    public enum PilePerms
    {
        None          = 0,
        CanBrowse     = 1 << 0,
        CanClear      = 1 << 1,
        CanCut        = 1 << 2,
        CanDraw       = 1 << 3,
        CanDrawBottom = 1 << 4,
        CanInsert     = 1 << 5,
        CanPeek       = 1 << 6,
        CanPut        = 1 << 7,
        CanPutBottom  = 1 << 8,
        CanShuffle    = 1 << 9,
        CanTake       = 1 << 10,
        All = CanBrowse | CanClear  | CanCut | CanDraw      | CanDrawBottom
            | CanInsert | CanPeek   | CanPut | CanPutBottom | CanShuffle | CanTake
    }
}
