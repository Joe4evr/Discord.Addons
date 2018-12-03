using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Discord.Addons.MpGame.Collections;

namespace MpGame.Tests
{
    internal sealed class WrappingTestPile : WrappingPile<ITestCard, WrappingTestPile.FlipWrapper>
    {
        private readonly PilePerms _perms;

        public WrappingTestPile(PilePerms withPerms, IEnumerable<ITestCard> items)
            : base(items)
        {
            _perms = withPerms;
        }

        public override bool CanBrowse => HasPerm(PilePerms.CanBrowse);
        public override bool CanClear => HasPerm(PilePerms.CanClear);
        public override bool CanCut => HasPerm(PilePerms.CanCut);
        public override bool CanDraw => HasPerm(PilePerms.CanDraw);
        public override bool CanDrawBottom => HasPerm(PilePerms.CanDrawBottom);
        public override bool CanInsert => HasPerm(PilePerms.CanInsert);
        public override bool CanPeek => HasPerm(PilePerms.CanPeek);
        public override bool CanPut => HasPerm(PilePerms.CanPut);
        public override bool CanPutBottom => HasPerm(PilePerms.CanPutBottom);
        public override bool CanShuffle => HasPerm(PilePerms.CanShuffle);
        public override bool CanTake => HasPerm(PilePerms.CanTake);

        protected sealed override FlipWrapper Wrap(ITestCard card)
            => new FlipWrapper(card);

        private bool HasPerm(PilePerms perm) => (_perms & perm) == perm;

        public void FlipCardAt(int index)
            => GetWrapperAt(index).Flip();
        public string GetStatusAt(int index)
            => GetWrapperAt(index).GetStatus();

        public struct FlipWrapper : IWrapper<ITestCard>
        {
            private readonly ITestCard _card;
            private bool _isFaceDown;

            internal FlipWrapper(ITestCard card)
            {
                _card = card;
                _isFaceDown = false;
            }

            internal void Flip()
                => _isFaceDown = !_isFaceDown;

            public string GetStatus()
                => (_isFaceDown)
                    ? "Card is face-down"
                    : "Card is face-up";

            public ITestCard Unwrap(bool revealing)
            {
                return (revealing || !_isFaceDown)
                    ? _card
                    : FaceDownCard.Instance;
            }
        }
    }
}
