using System;
using System.Collections.Generic;
using System.Linq;
using Discord.Addons.MpGame.Collections;
using Xunit;

namespace MpGame.Tests.CollectionTests
{
    public sealed class PileTests
    {
        private static IEnumerable<TestCard> CardFactory(int amount, int start = 1)
            => Enumerable.Range(start, amount).Select(i => new TestCard { Id = i });

        [Fact]
        public void SeedingCtorThrowsOnNullSequence()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new TestPile(withPerms: PilePerms.None, cards: null));
            Assert.Equal(expected: "cards", actual: ex.ParamName);
        }

        [Fact]
        public void PileThrowsWhenNotBrowsable()
        {
            var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanBrowse, cards: CardFactory(20));
            var ex = Assert.Throws<InvalidOperationException>(() => pile.Cards);
            Assert.Equal(expected: ErrorStrings.NoBrowse, actual: ex.Message);
        }

        [Fact]
        public void BrowsingDoesNotClearPile()
        {
            var pile = new TestPile(withPerms: PilePerms.CanBrowse, cards: CardFactory(20));
            var cards = pile.Cards;
            Assert.NotNull(cards);
            Assert.NotEmpty(cards);
            Assert.Equal(expected: cards.Count, actual: pile.Count);
        }

        [Fact]
        public void EmptyPileIsNotNull()
        {
            var pile = new TestPile(withPerms: PilePerms.CanBrowse, cards: Enumerable.Empty<TestCard>());
            var cards = pile.Cards;
            Assert.Equal(expected: 0, actual: pile.Count);
            Assert.NotNull(cards);
            Assert.Empty(cards);
        }

        [Fact]
        public void PileThrowsWhenNotClearable()
        {
            var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanClear, cards: CardFactory(20));
            var ex = Assert.Throws<InvalidOperationException>(() => pile.Clear());
            Assert.Equal(expected: ErrorStrings.NoClear, actual: ex.Message);
        }

        [Fact]
        public void PileThrowsWhenNotCuttable()
        {
            var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanCut, cards: CardFactory(20));
            var ex = Assert.Throws<InvalidOperationException>(() => pile.Cut(cutIndex: 10));
            Assert.Equal(expected: ErrorStrings.NoCut, actual: ex.Message);
        }

        [Fact]
        public void CutThrowsNegativeIndex()
        {
            var pile = new TestPile(withPerms: PilePerms.CanCut, cards: CardFactory(20));
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.Cut(cutIndex: -1));
            Assert.Equal(expected: ErrorStrings.CutIndexNegative, actual: ex.Message);
        }

        [Fact]
        public void CutThrowsTooHighIndex()
        {
            var pile = new TestPile(withPerms: PilePerms.CanCut, cards: CardFactory(20));
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.Cut(cutIndex: pile.Count + 1));
            Assert.Equal(expected: ErrorStrings.CutIndexTooHigh, actual: ex.Message);
        }

        [Fact]
        public void PileThrowsWhenNotDrawable()
        {
            var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanDraw, cards: CardFactory(20));
            var ex = Assert.Throws<InvalidOperationException>(() => pile.Draw());
            Assert.Equal(expected: ErrorStrings.NoDraw, actual: ex.Message);
        }

        [Fact]
        public void LastDrawCallsOnLastDraw()
        {
            var pile = new TestPile(withPerms: PilePerms.CanDraw, cards: CardFactory(1));
            //Assert.Raises<EventArgs>(ea => pile.LastDrawCalled += ea, ea => pile.LastDrawCalled -= ea, () => pile.Draw());
            pile.LastDrawCalled += p =>
            {
                Assert.Same(expected: pile, actual: p);
            };
            pile.Draw();
        }

        [Fact]
        public void PileThrowsWhenNotInsertable()
        {
            var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanInsert, cards: CardFactory(20));
            var ex = Assert.Throws<InvalidOperationException>(() => pile.InsertAt(card: new TestCard(), index: 15));
            Assert.Equal(expected: ErrorStrings.NoInsert, actual: ex.Message);
        }

        [Fact]
        public void InsertThrowsOnNullCard()
        {
            var pile = new TestPile(withPerms: PilePerms.CanInsert, cards: CardFactory(20));
            var ex = Assert.Throws<ArgumentNullException>(() => pile.InsertAt(card: null, index: 10));
            Assert.Equal(expected: "card", actual: ex.ParamName);
        }

        [Fact]
        public void InsertThrowsNegativeIndex()
        {
            var pile = new TestPile(withPerms: PilePerms.CanInsert, cards: CardFactory(20));
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.InsertAt(card: new TestCard(), index: -1));
            Assert.Equal(expected: ErrorStrings.InsertionNegative, actual: ex.Message);
            Assert.Equal(expected: "index", actual: ex.ParamName);
        }

        [Fact]
        public void InsertThrowsTooHighIndex()
        {
            var pile = new TestPile(withPerms: PilePerms.CanInsert, cards: CardFactory(20));
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.InsertAt(new TestCard(), index: pile.Count + 1));
            Assert.Equal(expected: ErrorStrings.InsertionTooHigh, actual: ex.Message);
            Assert.Equal(expected: "index", actual: ex.ParamName);
        }

        [Fact]
        public void PileThrowsWhenNotPeekable()
        {
            var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanPeek, cards: CardFactory(20));
            var ex = Assert.Throws<InvalidOperationException>(() => pile.PeekTop(amount: 5));
            Assert.Equal(expected: ErrorStrings.NoPeek, actual: ex.Message);
        }

        [Fact]
        public void PeekThrowsNegativeIndex()
        {
            var pile = new TestPile(withPerms: PilePerms.CanPeek, cards: CardFactory(20));
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.PeekTop(amount: -1));
            Assert.Equal(expected: ErrorStrings.PeekAmountNegative, actual: ex.Message);
            Assert.Equal(expected: "amount", actual: ex.ParamName);
        }

        [Fact]
        public void PeekThrowsTooHighIndex()
        {
            var pile = new TestPile(withPerms: PilePerms.CanPeek, cards: CardFactory(20));
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.PeekTop(amount: pile.Count + 1));
            Assert.Equal(expected: ErrorStrings.PeekAmountTooHigh, actual: ex.Message);
            Assert.Equal(expected: "amount", actual: ex.ParamName);
        }

        [Fact]
        public void PileThrowsWhenNotPuttable()
        {
            var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanPut, cards: CardFactory(20));
            var ex = Assert.Throws<InvalidOperationException>(() => pile.Put(card: new TestCard()));
            Assert.Equal(expected: ErrorStrings.NoPut, actual: ex.Message);
        }

        [Fact]
        public void PutThrowsOnNullCard()
        {
            var pile = new TestPile(withPerms: PilePerms.CanPut, cards: CardFactory(20));
            var ex = Assert.Throws<ArgumentNullException>(() => pile.Put(card: null));
            Assert.Equal(expected: "card", actual: ex.ParamName);
        }

        [Fact]
        public void PutCallsOnPut()
        {
            var pile = new TestPile(withPerms: PilePerms.CanPut, cards: Enumerable.Empty<TestCard>());
            var newcard = new TestCard { Id = 1 };
            pile.PutCalled += (p, c) =>
            {
                Assert.Same(expected: pile, actual: p);
                Assert.NotNull(c);
                Assert.Same(expected: newcard, actual: c);
            };
            pile.Put(card: newcard);
        }

        [Fact]
        public void PileThrowsWhenNotPuttableBottom()
        {
            var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanPutBottom, cards: CardFactory(20));
            var ex = Assert.Throws<InvalidOperationException>(() => pile.PutBottom(card: new TestCard()));
            Assert.Equal(expected: ErrorStrings.NoPutBtm, actual: ex.Message);
        }

        [Fact]
        public void PutBottomThrowsOnNullCard()
        {
            var pile = new TestPile(withPerms: PilePerms.CanPutBottom, cards: CardFactory(20));
            var ex = Assert.Throws<ArgumentNullException>(() => pile.PutBottom(card: null));
            Assert.Equal(expected: "card", actual: ex.ParamName);
        }

        [Fact]
        public void PileThrowsWhenNotShufflable()
        {
            var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanShuffle, cards: CardFactory(20));
            var ex = Assert.Throws<InvalidOperationException>(() => pile.Shuffle(shuffleFunc: c => c.Reverse()));
            Assert.Equal(expected: ErrorStrings.NoShuffle, actual: ex.Message);
        }

        [Fact]
        public void ShuffleThrowsOnNullFunc()
        {
            var pile = new TestPile(withPerms: PilePerms.CanShuffle, cards: CardFactory(20));
            var ex = Assert.Throws<ArgumentNullException>(() => pile.Shuffle(shuffleFunc: null));
            Assert.Equal(expected: "shuffleFunc", actual: ex.ParamName);
        }

        [Fact]
        public void ShuffleThrowsOnNullFuncReturn()
        {
            var pile = new TestPile(withPerms: PilePerms.CanShuffle, cards: CardFactory(20));
            var ex = Assert.Throws<InvalidOperationException>(() => pile.Shuffle(shuffleFunc: cards => null));
            Assert.Equal(expected: ErrorStrings.NullSequence, actual: ex.Message);
        }

        [Fact]
        public void ShuffleFuncDoesNotGiveNullArgument()
        {
            const int init = 0;
            const int added = 10;

            var pile = new TestPile(withPerms: PilePerms.CanShuffle, cards: CardFactory(init));
            pile.Shuffle(shuffleFunc: cards =>
            {
                Assert.NotNull(cards);
                return cards.Concat(CardFactory(added));
            });

            Assert.Equal(expected: init + added, actual: pile.Count);
        }

        [Fact]
        public void PileThrowsWhenNotTakable()
        {
            var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanTake, cards: CardFactory(20));
            var ex = Assert.Throws<InvalidOperationException>(() => pile.TakeAt(index: 14));
            Assert.Equal(expected: ErrorStrings.NoTake, actual: ex.Message);
        }

        [Fact]
        public void TakeThrowsNegativeIndex()
        {
            var pile = new TestPile(withPerms: PilePerms.CanTake, cards: CardFactory(20));
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.TakeAt(index: -1));
            Assert.Equal(expected: ErrorStrings.RetrievalNegative, actual: ex.Message);
            Assert.Equal(expected: "index", actual: ex.ParamName);
        }

        [Fact]
        public void TakeThrowsTooHighIndex()
        {
            var pile = new TestPile(withPerms: PilePerms.CanTake, cards: CardFactory(20));
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.TakeAt(index: pile.Count + 1));
            Assert.Equal(expected: ErrorStrings.RetrievalTooHigh, actual: ex.Message);
            Assert.Equal(expected: "index", actual: ex.ParamName);
        }
    }
}
