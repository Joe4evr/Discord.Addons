using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Discord.Addons.MpGame.Collections;

namespace MpGame.Tests
{
    internal class WrappingTestPile : WrappingPile<ITestCard, WrappingTestPile.FlipWrapper>, ITestPileEvents
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

        protected override FlipWrapper Wrap(ITestCard card)
            => new FlipWrapper(card, faceDownByDefault: false);

        private bool HasPerm(PilePerms perm) => (_perms & perm) == perm;

        public void FlipCardAt(int index)
            => GetWrapperRefAt(index).Flip();
        public string GetStatusAt(int index)
            => GetWrapperRefAt(index).GetStatus();

        internal FlipWrapper GetWrapper(int index)
            => GetWrapperRefAt(index);

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

        public struct FlipWrapper : IWrapper<ITestCard>
        {
            private readonly ITestCard _card;
            private bool _isFaceDown;

            internal FlipWrapper(ITestCard card, bool faceDownByDefault)
            {
                _card = card;
                _isFaceDown = faceDownByDefault;
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

    internal class FacedownWrappingTestPile : WrappingTestPile
    {
        public FacedownWrappingTestPile(PilePerms withPerms, IEnumerable<ITestCard> items)
            : base(withPerms, items)
        {
        }

        protected override FlipWrapper Wrap(ITestCard card)
            => new FlipWrapper(card, faceDownByDefault: true);
    }

}
