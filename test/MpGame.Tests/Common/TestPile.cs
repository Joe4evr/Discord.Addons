﻿using System;
using System.Collections.Generic;
using Discord.Addons.MpGame.Collections;

namespace MpGame.Tests
{
    internal sealed class TestPile : Pile<TestCard>
    {
        private readonly PilePerms _perms;

        public TestPile(PilePerms withPerms, IEnumerable<TestCard> cards)
            : base(cards)
        {
            _perms = withPerms;
        }

        public override bool CanBrowse    => HasPerm(PilePerms.CanBrowse);
        public override bool CanClear     => HasPerm(PilePerms.CanClear);
        public override bool CanCut       => HasPerm(PilePerms.CanCut);
        public override bool CanDraw      => HasPerm(PilePerms.CanDraw);
        public override bool CanInsert    => HasPerm(PilePerms.CanInsert);
        public override bool CanPeek      => HasPerm(PilePerms.CanPeek);
        public override bool CanPut       => HasPerm(PilePerms.CanPut);
        public override bool CanPutBottom => HasPerm(PilePerms.CanPutBottom);
        public override bool CanShuffle   => HasPerm(PilePerms.CanShuffle);
        public override bool CanTake      => HasPerm(PilePerms.CanTake);

        internal event EventHandler<EventArgs> LastDrawCalled;

        protected override void OnLastDraw()
        {
            base.OnLastDraw();
            LastDrawCalled?.Invoke(this, EventArgs.Empty);
        }

        internal event EventHandler<PutEventArgs> PutCalled;

        protected override void OnPut(TestCard card)
        {
            base.OnPut(card);
            PutCalled?.Invoke(this, new PutEventArgs(card));
        }

        private bool HasPerm(PilePerms perm) => (_perms & perm) == perm;
    }

    internal class PutEventArgs : EventArgs
    {
        public PutEventArgs(TestCard card)
        {
            Card = card;
        }

        public TestCard Card { get; }
    }

    [Flags]
    internal enum PilePerms
    {
        None         = 0,
        CanBrowse    = 1 << 0,
        CanClear     = 1 << 1,
        CanCut       = 1 << 2,
        CanDraw      = 1 << 3,
        CanInsert    = 1 << 4,
        CanPeek      = 1 << 5,
        CanPut       = 1 << 6,
        CanPutBottom = 1 << 7,
        CanShuffle   = 1 << 8,
        CanTake      = 1 << 9,
        All = CanBrowse | CanClear | CanCut       | CanDraw    | CanInsert
            | CanPeek   | CanPut   | CanPutBottom | CanShuffle | CanTake
    }
}
