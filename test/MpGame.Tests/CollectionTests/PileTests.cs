using System;
using System.Collections.Generic;
using System.Linq;
using Discord.Addons;
using Discord.Addons.MpGame.Collections;
using Xunit;

namespace MpGame.Tests.CollectionTests
{
    public sealed class PileTests
    {
        private static IEnumerable<TestCard> CardFactory(int amount, int start = 1)
            => Enumerable.Range(start, amount).Select(i => new TestCard { Id = i });

        private sealed class DummyBufferStrat : IBufferStrategy<TestCard>
        {
            TestCard[] IBufferStrategy<TestCard>.GetBuffer(int size) => throw new NotImplementedException();
            void IBufferStrategy<TestCard>.ReturnBuffer(TestCard[] buffer) => throw new NotImplementedException();
        }

        public sealed class Ctor
        {
            [Fact]
            public void SeedingCtorThrowsOnNullSequence()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new TestPile(withPerms: PilePerms.None, cards: null));
                Assert.Equal(expected: "cards", actual: ex.ParamName);
            }

            [Fact]
            public void SeedingCtorFiltersOutNulls()
            {
                var seed = CardFactory(20).ToArray();
                seed[5] = null;
                seed[10] = null;
                seed[15] = null;
                var nulls = seed.Count(c => c == null);
                var pile = new TestPile(withPerms: PilePerms.CanBrowse, cards: seed);

                Assert.Equal(expected: seed.Length - nulls, actual: pile.Count);
                Assert.All(pile.Cards, c => Assert.NotNull(c));
            }
        }

        public sealed class Browse
        {
            [Fact]
            public void PileThrowsWhenNotBrowsable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanBrowse, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Cards);
                Assert.Equal(expected: ErrorStrings.NoBrowse, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void BrowsingDoesNotChangePileSize()
            {
                var pile = new TestPile(withPerms: PilePerms.CanBrowse, cards: CardFactory(20));
                var priorSize = pile.Count;
                var cards = pile.Cards;

                Assert.NotNull(cards);
                Assert.NotEmpty(cards);
                Assert.Equal(expected: priorSize, actual: pile.Count);
                Assert.Equal(expected: priorSize, actual: cards.Count);
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
        }

        public sealed class BufferStrategy
        {
            [Fact]
            public void BufferStratThrowsOnNull()
            {
                var pile = new TestPile(withPerms: PilePerms.None, cards: Enumerable.Empty<TestCard>());
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentNullException>(() => pile.SetBufferStrat(null));
                Assert.Equal(expected: "value", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void SettingBufferStratAfterUseThrows()
            {
                var pile = new TestPile(withPerms: PilePerms.CanBrowse, cards: CardFactory(20));
                var priorSize = pile.Count;
                var browsing = pile.Cards;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.SetBufferStrat(new DummyBufferStrat()));
                Assert.StartsWith(expectedStartString: ErrorStrings.NoSwappingStrategy, actualString: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }
        }

        public sealed class Clear
        {
            [Fact]
            public void PileThrowsWhenNotClearable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanClear, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Clear());
                Assert.Equal(expected: ErrorStrings.NoClear, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ClearingEmptiesPile()
            {
                var pile = new TestPile(withPerms: PilePerms.CanClear, cards: CardFactory(20));
                var priorSize = pile.Count;
                var cleared = pile.Clear();

                Assert.Equal(expected: 0, actual: pile.Count);
                Assert.Equal(expected: priorSize, actual: cleared.Count);
            }
        }

        public sealed class Cut
        {
            [Fact]
            public void PileThrowsWhenNotCuttable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanCut, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Cut(cutIndex: 10));
                Assert.Equal(expected: ErrorStrings.NoCut, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void CutThrowsNegativeIndex()
            {
                var pile = new TestPile(withPerms: PilePerms.CanCut, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.Cut(cutIndex: -1));
                Assert.StartsWith(expectedStartString: ErrorStrings.CutIndexNegative, actualString: ex.Message);
                Assert.Equal(expected: "cutIndex", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void CutThrowsTooHighIndex()
            {
                var pile = new TestPile(withPerms: PilePerms.CanCut, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.Cut(cutIndex: pile.Count + 1));
                Assert.StartsWith(expectedStartString: ErrorStrings.CutIndexTooHigh, actualString: ex.Message);
                Assert.Equal(expected: "cutIndex", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void CutDoesNotChangePileSize()
            {
                var pile = new TestPile(withPerms: PilePerms.CanCut, cards: CardFactory(20));
                var priorSize = pile.Count;
                pile.Cut(cutIndex: 10);

                Assert.Equal(expected: priorSize, actual: pile.Count);
            }
        }

        public sealed class Draw
        {
            [Fact]
            public void PileThrowsWhenNotDrawable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanDraw, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Draw());
                Assert.Equal(expected: ErrorStrings.NoDraw, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void DrawDecreasesPileByOne()
            {
                var pile = new TestPile(withPerms: PilePerms.CanDraw, cards: CardFactory(20));
                var priorSize = pile.Count;
                var drawn = pile.Draw();

                Assert.NotNull(drawn);
                Assert.Equal(expected: priorSize - 1, actual: pile.Count);
            }

            [Fact]
            public void LastDrawCallsOnLastDraw()
            {
                var pile = new TestPile(withPerms: PilePerms.CanDraw, cards: CardFactory(1));

                var ev = Assert.Raises<EventArgs>(handler => pile.LastDrawCalled += handler, handler => pile.LastDrawCalled -= handler, () => pile.Draw());
                Assert.Same(expected: pile, actual: ev.Sender);
                Assert.Equal(expected: 0, actual: pile.Count);
            }

            [Fact]
            public void DrawingOnEmptyPileThrows()
            {
                var pile = new TestPile(withPerms: PilePerms.CanDraw, cards: Enumerable.Empty<TestCard>());
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Draw());
                Assert.Equal(expected: ErrorStrings.PileEmpty, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }
        }

        public sealed class InsertAt
        {
            [Fact]
            public void PileThrowsWhenNotInsertable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanInsert, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.InsertAt(card: new TestCard(), index: 15));
                Assert.Equal(expected: ErrorStrings.NoInsert, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void InsertThrowsOnNullCard()
            {
                var pile = new TestPile(withPerms: PilePerms.CanInsert, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentNullException>(() => pile.InsertAt(card: null, index: 10));
                Assert.Equal(expected: "card", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void InsertThrowsNegativeIndex()
            {
                var pile = new TestPile(withPerms: PilePerms.CanInsert, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.InsertAt(card: new TestCard(), index: -1));
                Assert.StartsWith(expectedStartString: ErrorStrings.InsertionNegative, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void InsertThrowsTooHighIndex()
            {
                var pile = new TestPile(withPerms: PilePerms.CanInsert, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.InsertAt(new TestCard(), index: pile.Count + 1));
                Assert.StartsWith(expectedStartString: ErrorStrings.InsertionTooHigh, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void InsertIncreasesPileByOne()
            {
                var pile = new TestPile(withPerms: PilePerms.CanInsert, cards: CardFactory(20));
                var priorSize = pile.Count;
                var newcard = new TestCard { Id = 1 };
                pile.InsertAt(card: newcard, index: 10);

                Assert.Equal(expected: priorSize + 1, actual: pile.Count);
            }
        }

        public sealed class PeekTop
        {
            [Fact]
            public void PileThrowsWhenNotPeekable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanPeek, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.PeekTop(amount: 5));
                Assert.Equal(expected: ErrorStrings.NoPeek, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void PeekThrowsNegativeIndex()
            {
                var pile = new TestPile(withPerms: PilePerms.CanPeek, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.PeekTop(amount: -1));
                Assert.StartsWith(expectedStartString: ErrorStrings.PeekAmountNegative, actualString: ex.Message);
                Assert.Equal(expected: "amount", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void PeekThrowsTooHighIndex()
            {
                var pile = new TestPile(withPerms: PilePerms.CanPeek, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.PeekTop(amount: pile.Count + 1));
                Assert.StartsWith(expectedStartString: ErrorStrings.PeekAmountTooHigh, actualString: ex.Message);
                Assert.Equal(expected: "amount", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void PeekDoesNotChangePileSize()
            {
                var pile = new TestPile(withPerms: PilePerms.CanPeek, cards: CardFactory(20));
                var priorSize = pile.Count;
                var peeked = pile.PeekTop(3);

                Assert.NotNull(peeked);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }
        }

        public sealed class Put
        {
            [Fact]
            public void PileThrowsWhenNotPuttable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanPut, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Put(card: new TestCard()));
                Assert.Equal(expected: ErrorStrings.NoPut, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void PutThrowsOnNullCard()
            {
                var pile = new TestPile(withPerms: PilePerms.CanPut, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentNullException>(() => pile.Put(card: null));
                Assert.Equal(expected: "card", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void PutCallsOnPut()
            {
                var pile = new TestPile(withPerms: PilePerms.CanPut, cards: Enumerable.Empty<TestCard>());
                var priorSize = pile.Count;
                var newcard = new TestCard { Id = 1 };

                var ev = Assert.Raises<PutEventArgs>(handler => pile.PutCalled += handler, handler => pile.PutCalled -= handler, () => pile.Put(card: newcard));
                Assert.Same(expected: pile, actual: ev.Sender);
                Assert.NotNull(ev.Arguments.Card);
                Assert.Same(expected: newcard, actual: ev.Arguments.Card);
                Assert.Equal(expected: priorSize + 1, actual: pile.Count);
            }

            //[Fact]
            //public void PutIncreasesPileByOne()
            //{
            //    var pile = new TestPile(withPerms: PilePerms.CanPut, cards: Enumerable.Empty<TestCard>());
            //    var priorSize = pile.Count;
            //    var newcard = new TestCard { Id = 1 };
            //    pile.Put(card: newcard);
            //    Assert.Equal(expected: priorSize + 1, actual: pile.Count);
            //}
        }

        public sealed class PutBottom
        {
            [Fact]
            public void PileThrowsWhenNotPuttableBottom()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanPutBottom, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.PutBottom(card: new TestCard()));
                Assert.Equal(expected: ErrorStrings.NoPutBtm, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void PutBottomThrowsOnNullCard()
            {
                var pile = new TestPile(withPerms: PilePerms.CanPutBottom, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentNullException>(() => pile.PutBottom(card: null));
                Assert.Equal(expected: "card", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void PutBottomIncreasesPileByOne()
            {
                var pile = new TestPile(withPerms: PilePerms.CanPutBottom, cards: Enumerable.Empty<TestCard>());
                var priorSize = pile.Count;
                var newcard = new TestCard { Id = 1 };
                pile.PutBottom(card: newcard);

                Assert.Equal(expected: priorSize + 1, actual: pile.Count);
            }
        }

        public sealed class Shuffle
        {
            [Fact]
            public void PileThrowsWhenNotShufflable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanShuffle, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Shuffle(shuffleFunc: c => c.Reverse()));
                Assert.Equal(expected: ErrorStrings.NoShuffle, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ShuffleThrowsOnNullFunc()
            {
                var pile = new TestPile(withPerms: PilePerms.CanShuffle, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentNullException>(() => pile.Shuffle(shuffleFunc: null));
                Assert.Equal(expected: "shuffleFunc", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ShuffleThrowsOnNullFuncReturn()
            {
                var pile = new TestPile(withPerms: PilePerms.CanShuffle, cards: CardFactory(20));
                var priorSize = pile.Count;
                bool funcCalled = false;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Shuffle(shuffleFunc: cards =>
                {
                    funcCalled = true;
                    return null;
                }));
                Assert.True(funcCalled);
                Assert.Equal(expected: ErrorStrings.NewSequenceNull, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ShuffleFuncDoesNotGiveNullArgument()
            {
                const int added = 10;

                var pile = new TestPile(withPerms: PilePerms.CanShuffle, cards: Enumerable.Empty<TestCard>());
                var priorSize = pile.Count;
                bool funcCalled = false;
                pile.Shuffle(shuffleFunc: cards =>
                {
                    funcCalled = true;
                    Assert.NotNull(cards);
                    return cards.Concat(CardFactory(added));
                });

                Assert.True(funcCalled);
                Assert.Equal(expected: priorSize + added, actual: pile.Count);
            }
        }

        public sealed class TakeAt
        {
            [Fact]
            public void PileThrowsWhenNotTakable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanTake, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.TakeAt(index: 14));
                Assert.Equal(expected: ErrorStrings.NoTake, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void TakeThrowsNegativeIndex()
            {
                var pile = new TestPile(withPerms: PilePerms.CanTake, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.TakeAt(index: -1));
                Assert.StartsWith(expectedStartString: ErrorStrings.RetrievalNegative, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void TakeThrowsTooHighIndex()
            {
                var pile = new TestPile(withPerms: PilePerms.CanTake, cards: CardFactory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.TakeAt(index: pile.Count));
                Assert.StartsWith(expectedStartString: ErrorStrings.RetrievalTooHighP, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void TakeDecreasesPileByOne()
            {
                var pile = new TestPile(withPerms: PilePerms.CanTake, cards: CardFactory(20));
                var priorSize = pile.Count;
                var taken = pile.TakeAt(10);

                Assert.Equal(expected: priorSize - 1, actual: pile.Count);
            }
        }
    }
}
