using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Discord.Addons.MpGame.Collections;

namespace MpGame.Tests
{
    internal sealed class TestPile2 : Pile<ITestCard, TestPile2.CustomWrapper>
    {
        private readonly PilePerms _perms;

        public TestPile2(PilePerms withPerms, IEnumerable<ITestCard> cards)
            : base(cards)
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

        protected sealed override CustomWrapper Wrap(ITestCard card)
            => new CustomWrapper(card);

        private bool HasPerm(PilePerms perm) => (_perms & perm) == perm;

        public void FlipCardAt(int index)
            => GetWrapperAndUpdate(index, w => w.Flip());

        public ITestCard GetCardAt(int index)
            => GetWrapperAt(index).Unwrap(revealing: false);

        public struct CustomWrapper : ICardWrapper<ITestCard>
        {
            private readonly ITestCard _card;
            private bool _isFaceDown;

            internal CustomWrapper(ITestCard card)
            {
                _card = card;
                _isFaceDown = false;
            }

            internal CustomWrapper Flip()
            {
                _isFaceDown = !_isFaceDown;
                return this;
            }

            public ITestCard Unwrap(bool revealing)
            {
                return (revealing || !_isFaceDown)
                    ? _card
                    : FaceDownCard.Instance;
            }

            public void Reset<TWrapper>(Pile<ITestCard, TWrapper> newPile)
                where TWrapper : ICardWrapper<ITestCard>
            {
            }
        }
    }
}
