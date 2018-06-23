﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.MpGame.Collections;
using Xunit;

namespace MpGame.Tests.CollectionTests
{
    public sealed class PileTests
    {
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
                var seed = TestCard.Factory(20).ToArray();
                seed[5] = null;
                seed[10] = null;
                seed[15] = null;
                var nulls = seed.Count(c => c == null);
                var pile = new TestPile(withPerms: PilePerms.CanBrowse, cards: seed);

                Assert.Equal(expected: seed.Length - nulls, actual: pile.Count);
                Assert.All(pile.Browse(), c => Assert.NotNull(c));
            }
        }

        public sealed class AsEnumerable
        {
            [Fact]
            public void ThrowsWhenNotBrowsable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanBrowse, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.AsEnumerable());
                Assert.Equal(expected: ErrorStrings.NoBrowse, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void IsSameSequenceAsPile()
            {
                var pile = new TestPile(withPerms: PilePerms.CanBrowse, cards: TestCard.Factory(20));
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20
                };

                Assert.Equal(expected: expectedSeq, actual: pile.AsEnumerable().Select(c => c.Id));
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }

            [Fact]
            public void ThrowsWhenAttemptingToChangeDuringEnumeration()
            {
                var pile = new TestPile(withPerms: PilePerms.CanBrowse | PilePerms.CanDraw, cards: TestCard.Factory(20));
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20
                };

                int i = 0;
                foreach (var item in pile.AsEnumerable())
                {
                    if (i >= 3)
                    {
                        var ex = Assert.Throws<LockRecursionException>(() => pile.Draw());

                        break;
                    }
                    i++;
                }
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }
        }

        public sealed class Browse
        {
            [Fact]
            public void ThrowsWhenNotBrowsable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanBrowse, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Browse());
                Assert.Equal(expected: ErrorStrings.NoBrowse, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void BrowsingDoesNotChangePileSize()
            {
                var pile = new TestPile(withPerms: PilePerms.CanBrowse, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                var cards = pile.Browse();

                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20,
                };

                Assert.False(cards.IsDefault);
                Assert.NotEmpty(cards);
                Assert.Equal(expected: priorSize, actual: pile.Count);
                Assert.Equal(expected: priorSize, actual: cards.Length);
                Assert.Equal(expected: expectedSeq, actual: cards.Select(c => c.Id));
            }

            [Fact]
            public void EmptyPileIsNotNull()
            {
                var pile = new TestPile(withPerms: PilePerms.CanBrowse, cards: Enumerable.Empty<TestCard>());
                var cards = pile.Browse();

                Assert.Equal(expected: 0, actual: pile.Count);
                Assert.False(cards.IsDefault);
                Assert.True(cards.IsEmpty);
            }
        }

        public sealed class BrowseAndTake
        {
            [Fact]
            public async Task ThrowsWhenNotBrowsable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanBrowse, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                bool selectorCalled = false;

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => pile.BrowseAndTake(selector: cards =>
                    {
                        selectorCalled = true;

                        return Task.FromResult(new[] { 5, 10 });
                    }));
                Assert.False(selectorCalled);
                Assert.Equal(expected: ErrorStrings.NoBrowseAndTake, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public async Task ThrowsWhenNotTakable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanTake, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                bool selectorCalled = false;

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => pile.BrowseAndTake(selector: cards =>
                    {
                        selectorCalled = true;

                        return Task.FromResult(new[] { 5, 10 });
                    }));
                Assert.False(selectorCalled);
                Assert.Equal(expected: ErrorStrings.NoBrowseAndTake, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public async Task ThrowsOnNullSelector()
            {
                var pile = new TestPile(withPerms: PilePerms.CanBrowse | PilePerms.CanTake, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20,
                };

                var ex = await Assert.ThrowsAsync<ArgumentNullException>(
                    () => pile.BrowseAndTake(selector: null));
                Assert.Equal(expected: "selector", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }

            [Fact]
            public async Task ThrowsOnBadIndices()
            {
                var pile = new TestPile(withPerms: PilePerms.CanBrowse | PilePerms.CanTake, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20,
                };
                bool selectorCalled = false;

                var ex = await Assert.ThrowsAsync<IndexOutOfRangeException>(() =>
                    pile.BrowseAndTake(selector: cards =>
                    {
                        selectorCalled = true;
                        Assert.NotNull(cards);
                        Assert.NotEmpty(cards);

                        return Task.FromResult(new[] { 25 });
                    }));
                Assert.True(selectorCalled);
                Assert.Equal(expected: "Selected indeces '25' must be one of the provided card indices.", actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }

            [Fact]
            public async Task DuplicateIndicesAreIgnored()
            {
                var pile = new TestPile(withPerms: PilePerms.CanBrowse | PilePerms.CanTake, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                bool selectorCalled = false;
                var picks = await pile.BrowseAndTake(
                    selector: cards =>
                    {
                        selectorCalled = true;
                        Assert.NotNull(cards);
                        Assert.NotEmpty(cards);

                        return Task.FromResult(new[] { 5, 10, 5, 10 });
                    });
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5,    7, 8, 9,10,
                       12,13,14,15,16,17,18,19,20,
                };

                Assert.True(selectorCalled);
                Assert.False(picks.IsDefault);
                Assert.NotEmpty(picks);
                //Assert.Equal(expected: 2, actual: picks.Length);
                Assert.Equal(expected: new[] { 6, 11 }, actual: picks.Select(c => c.Id));
                Assert.Equal(expected: priorSize - 2, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }

            [Fact]
            public async Task DoesNotThrowWhenNotShufflable()
            {
                var pile = new TestPile(withPerms: PilePerms.CanBrowse | PilePerms.CanTake, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                bool selectorCalled = false;
                bool shuffleFuncCalled = false;
                var picks = await pile.BrowseAndTake(
                    selector: cards =>
                    {
                        selectorCalled = true;
                        Assert.NotEmpty(cards);

                        return Task.FromResult(new[] { 5, 10 });
                    },
                    shuffleFunc: cards =>
                    {
                        shuffleFuncCalled = true;

                        return cards.Reverse();
                    });
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5,    7, 8, 9,10,
                       12,13,14,15,16,17,18,19,20,
                };

                Assert.True(selectorCalled);
                Assert.False(shuffleFuncCalled);
                Assert.False(picks.IsDefault);
                Assert.NotEmpty(picks);
                //Assert.Equal(expected: 2, actual: picks.Length);
                Assert.Equal(expected: new[] { 6, 11 }, actual: picks.Select(c => c.Id));
                Assert.Equal(expected: priorSize - 2, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }

            [Fact]
            public async Task SelectorReceivesOnlyItemsThatAreNotFilteredOut()
            {
                var pile = new TestPile(withPerms: PilePerms.CanBrowse | PilePerms.CanTake, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                bool selectorCalled = false;
                bool filterCalled = false;
                var picks = await pile.BrowseAndTake(
                    selector: cards =>
                    {
                        selectorCalled = true;
                        Assert.NotEmpty(cards);
                        Assert.All(cards.Values, c => Assert.True(LessThanOrEqualToTenFilter(c)));

                        return Task.FromResult(new[] { 5 });
                    },
                    filter: c => 
                    {
                        filterCalled = true;
                        
                        return LessThanOrEqualToTenFilter(c);
                    });
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5,    7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20,
                };

                Assert.True(selectorCalled);
                Assert.True(filterCalled);
                Assert.False(picks.IsDefault);
                Assert.NotEmpty(picks);
                //Assert.Single(picks);
                Assert.Equal(expected: new[] { 6 }, actual: picks.Select(c => c.Id));
                Assert.Equal(expected: priorSize - 1, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));

                bool LessThanOrEqualToTenFilter(TestCard c) => c.Id <= 10;
            }

            [Fact]
            public async Task ShuffleWorksWhenAllowed()
            {
                var pile = new TestPile(withPerms: PilePerms.CanBrowse | PilePerms.CanTake | PilePerms.CanShuffle, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                bool selectorCalled = false;
                bool shuffleFuncCalled = false;
                var picks = await pile.BrowseAndTake(
                    selector: cards =>
                    {
                        selectorCalled = true;
                        Assert.NotNull(cards);
                        Assert.NotEmpty(cards);

                        return Task.FromResult(new[] { 5, 10 });
                    },
                    shuffleFunc: cards =>
                    {
                        shuffleFuncCalled = true;

                        return cards.Reverse();
                    });
                var expectedSeq = new[]
                {
                    20,19,18,17,16,15,14,13,12,
                    10, 9, 8, 7,    5, 4, 3, 2, 1
                };

                Assert.True(selectorCalled);
                Assert.True(shuffleFuncCalled);
                Assert.False(picks.IsDefault);
                Assert.NotEmpty(picks);
                //Assert.Equal(expected: 2, actual: picks.Length);
                Assert.Equal(expected: new[] { 6, 11 }, actual: picks.Select(c => c.Id));
                Assert.Equal(expected: priorSize - 2, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }

            [Fact]
            public async Task ShufflesBackAllCards()
            {
                var pile = new TestPile(withPerms: PilePerms.CanBrowse | PilePerms.CanTake | PilePerms.CanShuffle, cards: TestCard.Factory(10));
                var priorSize = pile.Count;
                bool selectorCalled = false;
                bool shuffleFuncCalled = false;
                bool filterCalled = false;
                var picks = await pile.BrowseAndTake(
                    selector: cards =>
                    {
                        selectorCalled = true;
                        Assert.NotNull(cards);
                        Assert.NotEmpty(cards);

                        return Task.FromResult(new[] { cards.First().Key });
                    },
                    shuffleFunc: cards =>
                    {
                        shuffleFuncCalled = true;

                        return cards.Reverse();
                    },
                    filter: card =>
                    {
                        filterCalled = true;

                        return card.Color == CardColor.Green;
                    });
                var expectedSeq = new[]
                {
                    10, 9, 8, 7, 6, 5, 4, 3, 2
                };

                Assert.True(selectorCalled);
                Assert.True(shuffleFuncCalled);
                Assert.True(filterCalled);
                Assert.False(picks.IsDefault);
                Assert.NotEmpty(picks);
                Assert.Equal(expected: new[] { 1 }, actual: picks.Select(c => c.Id));
                Assert.Equal(expected: priorSize - 1, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }

            [Fact]
            public async Task EmptySelectionArrayDoesNotDecreasePileSize()
            {
                var pile = new TestPile(withPerms: PilePerms.CanBrowse | PilePerms.CanTake, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                bool selectorCalled = false;
                var picks = await pile.BrowseAndTake(
                    selector: cards =>
                    {
                        selectorCalled = true;
                        Assert.NotEmpty(cards);

                        return Task.FromResult(Array.Empty<int>());
                    });
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20,
                };

                Assert.True(selectorCalled);
                Assert.False(picks.IsDefault);
                Assert.True(picks.IsEmpty);
                Assert.Equal(expected: priorSize, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }
        }

        public sealed class Clear
        {
            [Fact]
            public void ThrowsWhenNotClearable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanClear, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Clear());
                Assert.Equal(expected: ErrorStrings.NoClear, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ClearingEmptiesPile()
            {
                var pile = new TestPile(withPerms: PilePerms.CanClear, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                var cleared = pile.Clear();

                Assert.Equal(expected: 0, actual: pile.Count);
                Assert.Equal(expected: priorSize, actual: cleared.Length);
            }
        }

        public sealed class Cut
        {
            [Fact]
            public void ThrowsWhenNotCuttable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanCut, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Cut(amount: 10));
                Assert.Equal(expected: ErrorStrings.NoCut, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ThrowsNegativeIndex()
            {
                var pile = new TestPile(withPerms: PilePerms.CanCut, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.Cut(amount: -1));
                Assert.StartsWith(expectedStartString: ErrorStrings.CutAmountNegative, actualString: ex.Message);
                Assert.Equal(expected: "amount", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ThrowsTooHighIndex()
            {
                var pile = new TestPile(withPerms: PilePerms.CanCut, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.Cut(amount: pile.Count + 1));
                Assert.StartsWith(expectedStartString: ErrorStrings.CutAmountTooHigh, actualString: ex.Message);
                Assert.Equal(expected: "amount", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void DoesNotChangePileSize()
            {
                var pile = new TestPile(withPerms: PilePerms.CanCut | PilePerms.CanBrowse, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                pile.Cut(amount: 10);
                var expectedSeq = new[]
                {
                    11,12,13,14,15,16,17,18,19,20,
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10
                };

                Assert.Equal(expected: priorSize, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }
        }

        public sealed class Draw
        {
            [Fact]
            public void ThrowsWhenNotDrawable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanDraw, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Draw());
                Assert.Equal(expected: ErrorStrings.NoDraw, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void DecreasesPileByOne()
            {
                var pile = new TestPile(withPerms: PilePerms.CanDraw, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                var drawn = pile.Draw();

                Assert.NotNull(drawn);
                Assert.Equal(expected: 1, actual: drawn.Id);
                Assert.Equal(expected: priorSize - 1, actual: pile.Count);
            }

            [Fact]
            public void LastDrawCallsOnLastRemoved()
            {
                var pile = new TestPile(withPerms: PilePerms.CanDraw, cards: TestCard.Factory(1));

                var ev = Assert.Raises<EventArgs>(handler => pile.LastRemoveCalled += handler, handler => pile.LastRemoveCalled -= handler, () => pile.Draw());
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

        public sealed class DrawBottom
        {
            [Fact]
            public void ThrowsWhenNotDrawable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanDrawBottom, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.DrawBottom());
                Assert.Equal(expected: ErrorStrings.NoDraw, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void DecreasesPileByOne()
            {
                var pile = new TestPile(withPerms: PilePerms.CanDrawBottom, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                var drawn = pile.DrawBottom();

                Assert.NotNull(drawn);
                Assert.Equal(expected: 20, actual: drawn.Id);
                Assert.Equal(expected: priorSize - 1, actual: pile.Count);
            }

            [Fact]
            public void LastDrawCallsOnLastRemoved()
            {
                var pile = new TestPile(withPerms: PilePerms.CanDrawBottom, cards: TestCard.Factory(1));

                var ev = Assert.Raises<EventArgs>(handler => pile.LastRemoveCalled += handler, handler => pile.LastRemoveCalled -= handler, () => pile.DrawBottom());
                Assert.Same(expected: pile, actual: ev.Sender);
                Assert.Equal(expected: 0, actual: pile.Count);
            }

            [Fact]
            public void ThrowsOnEmptyPile()
            {
                var pile = new TestPile(withPerms: PilePerms.CanDrawBottom, cards: Enumerable.Empty<TestCard>());
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.DrawBottom());
                Assert.Equal(expected: ErrorStrings.PileEmpty, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }
        }

        public sealed class InsertAt
        {
            [Fact]
            public void ThrowsWhenNotInsertable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanInsert, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.InsertAt(card: new TestCard(id: 2), index: 15));
                Assert.Equal(expected: ErrorStrings.NoInsert, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ThrowsOnNullCard()
            {
                var pile = new TestPile(withPerms: PilePerms.CanInsert, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentNullException>(() => pile.InsertAt(card: null, index: 10));
                Assert.Equal(expected: "card", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ThrowsNegativeIndex()
            {
                var pile = new TestPile(withPerms: PilePerms.CanInsert, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.InsertAt(card: new TestCard(id: 2), index: -1));
                Assert.StartsWith(expectedStartString: ErrorStrings.InsertionNegative, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ThrowsTooHighIndex()
            {
                var pile = new TestPile(withPerms: PilePerms.CanInsert, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.InsertAt(new TestCard(id: 2), index: pile.Count + 1));
                Assert.StartsWith(expectedStartString: ErrorStrings.InsertionTooHigh, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void IncreasesPileByOne()
            {
                var pile = new TestPile(withPerms: PilePerms.CanInsert, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                var newcard = new TestCard(id: 1);
                pile.InsertAt(card: newcard, index: 10);

                Assert.Equal(expected: priorSize + 1, actual: pile.Count);
            }
        }

        public sealed class Mill
        {
            [Fact]
            public void ThrowsWhenNotSourceDrawable()
            {
                var source = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanDraw, cards: TestCard.Factory(20));
                var target = new TestPile(withPerms: PilePerms.CanPut, cards: Enumerable.Empty<TestCard>());
                var sourceSize = source.Count;
                var targetSize = target.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => source.Mill(target));
                Assert.Equal(expected: ErrorStrings.NoDraw, actual: ex.Message);
                Assert.Equal(expected: sourceSize, actual: source.Count);
                Assert.Equal(expected: targetSize, actual: target.Count);
            }

            [Fact]
            public void ThrowsWhenTargetNotPuttable()
            {
                var source = new TestPile(withPerms: PilePerms.CanDraw, cards: TestCard.Factory(20));
                var target = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanPut, cards: Enumerable.Empty<TestCard>());
                var sourceSize = source.Count;
                var targetSize = target.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => source.Mill(target));
                Assert.Equal(expected: ErrorStrings.NoPutTarget, actual: ex.Message);
                Assert.Equal(expected: sourceSize, actual: source.Count);
                Assert.Equal(expected: targetSize, actual: target.Count);
            }

            [Fact]
            public void ThrowsWhenTargetIsSourceInstance()
            {
                var source = new TestPile(withPerms: PilePerms.CanDraw | PilePerms.CanPut, cards: TestCard.Factory(20));
                var sourceSize = source.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => source.Mill(source));
                Assert.Equal(expected: ErrorStrings.MillTargetSamePile, actual: ex.Message);
                Assert.Equal(expected: sourceSize, actual: source.Count);
            }

            [Fact]
            public void ThrowsOnEmptyPile()
            {
                var source = new TestPile(withPerms: PilePerms.CanDraw, cards: Enumerable.Empty<TestCard>());
                var target = new TestPile(withPerms: PilePerms.CanPut, cards: Enumerable.Empty<TestCard>());
                var sourceSize = source.Count;
                var targetSize = target.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => source.Mill(target));
                Assert.Equal(expected: ErrorStrings.PileEmpty, actual: ex.Message);
                Assert.Equal(expected: sourceSize, actual: source.Count);
                Assert.Equal(expected: targetSize, actual: target.Count);
            }

            [Fact]
            public void DecreasesSourseSizeIncreasesTargetSizeByOne()
            {
                var source = new TestPile(withPerms: PilePerms.CanDraw | PilePerms.CanPeek,   cards: TestCard.Factory(1));
                var target = new TestPile(withPerms: PilePerms.CanPut  | PilePerms.CanBrowse, cards: Enumerable.Empty<TestCard>());
                var sourceSize = source.Count;
                var targetSize = target.Count;
                var topcard = source.Top;
                int firstCall = 0;
                bool putCalled = false;
                bool lastRmCalled = false;

                target.PutCalled += (s, e) =>
                {
                    putCalled = true;
                    Interlocked.CompareExchange(ref firstCall, value: 1, comparand: 0);
                };
                source.LastRemoveCalled += (s, e) =>
                {
                    lastRmCalled = true;
                    Interlocked.CompareExchange(ref firstCall, value: 2, comparand: 0);
                };

                source.Mill(target);
                Assert.True(lastRmCalled);
                Assert.True(putCalled);
                Assert.Equal(expected: 2, actual: firstCall);
                Assert.Equal(expected: sourceSize - 1, actual: source.Count);
                Assert.Equal(expected: targetSize + 1, actual: target.Count);
                Assert.Same(expected: topcard, actual: target.Top);
            }
        }

        public sealed class PeekTop
        {
            [Fact]
            public void ThrowsWhenNotPeekable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanPeek, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.PeekTop(amount: 5));
                Assert.Equal(expected: ErrorStrings.NoPeek, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ThrowsNegativeIndex()
            {
                var pile = new TestPile(withPerms: PilePerms.CanPeek, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.PeekTop(amount: -1));
                Assert.StartsWith(expectedStartString: ErrorStrings.PeekAmountNegative, actualString: ex.Message);
                Assert.Equal(expected: "amount", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ThrowsTooHighIndex()
            {
                var pile = new TestPile(withPerms: PilePerms.CanPeek, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.PeekTop(amount: pile.Count + 1));
                Assert.StartsWith(expectedStartString: ErrorStrings.PeekAmountTooHigh, actualString: ex.Message);
                Assert.Equal(expected: "amount", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void DoesNotChangePileSize()
            {
                var pile = new TestPile(withPerms: PilePerms.CanPeek, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                var expectedSeq = new[] { 1, 2, 3 };
                var peeked = pile.PeekTop(3);

                Assert.False(peeked.IsDefault);
                Assert.Equal(expected: expectedSeq, actual: peeked.Select(c => c.Id));
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }


            //[Fact]
            //public void MathMinTest()
            //{
            //    var pile = new TestPile(withPerms: PilePerms.CanPeek, cards: TestCard.Factory(3));
            //    var peeked = pile.PeekTop(Math.Min(4, pile.Count));

            //    Assert.False(peeked.IsDefault);
            //}
        }

        public sealed class Put
        {
            [Fact]
            public void ThrowsWhenNotPuttable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanPut, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Put(card: new TestCard(id: 2)));
                Assert.Equal(expected: ErrorStrings.NoPut, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ThrowsOnNullCard()
            {
                var pile = new TestPile(withPerms: PilePerms.CanPut, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentNullException>(() => pile.Put(card: null));
                Assert.Equal(expected: "card", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void CallsOnPut()
            {
                var pile = new TestPile(withPerms: PilePerms.CanPut, cards: Enumerable.Empty<TestCard>());
                var priorSize = pile.Count;
                var newcard = new TestCard(id: 1);

                var ev = Assert.Raises<PutEventArgs>(handler => pile.PutCalled += handler, handler => pile.PutCalled -= handler, () => pile.Put(card: newcard));
                Assert.Same(expected: pile, actual: ev.Sender);
                Assert.NotNull(ev.Arguments.Card);
                Assert.Same(expected: newcard, actual: ev.Arguments.Card);
                Assert.Equal(expected: priorSize + 1, actual: pile.Count);
            }
        }

        public sealed class PutBottom
        {
            [Fact]
            public void ThrowsWhenNotPuttableBottom()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanPutBottom, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.PutBottom(card: new TestCard(id: 2)));
                Assert.Equal(expected: ErrorStrings.NoPutBottom, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ThrowsOnNullCard()
            {
                var pile = new TestPile(withPerms: PilePerms.CanPutBottom, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentNullException>(() => pile.PutBottom(card: null));
                Assert.Equal(expected: "card", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void IncreasesPileByOne()
            {
                var pile = new TestPile(withPerms: PilePerms.CanPutBottom, cards: Enumerable.Empty<TestCard>());
                var priorSize = pile.Count;
                var newcard = new TestCard(id: 1);
                pile.PutBottom(card: newcard);

                Assert.Equal(expected: priorSize + 1, actual: pile.Count);
            }
        }

        public sealed class Shuffle
        {
            [Fact]
            public void ThrowsWhenNotShufflable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanShuffle, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Shuffle(shuffleFunc: c => c.Reverse()));
                Assert.Equal(expected: ErrorStrings.NoShuffle, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ThrowsOnNullFunc()
            {
                var pile = new TestPile(withPerms: PilePerms.CanShuffle, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentNullException>(() => pile.Shuffle(shuffleFunc: null));
                Assert.Equal(expected: "shuffleFunc", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ThrowsOnNullFuncReturn()
            {
                var pile = new TestPile(withPerms: PilePerms.CanShuffle, cards: TestCard.Factory(20));
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
            public void ShuffleFuncDoesNotGiveDefaultArray()
            {
                const int added = 10;

                var pile = new TestPile(withPerms: PilePerms.CanShuffle, cards: Enumerable.Empty<TestCard>());
                var priorSize = pile.Count;
                bool funcCalled = false;
                pile.Shuffle(shuffleFunc: cards =>
                {
                    funcCalled = true;
                    Assert.False(cards.IsDefault);
                    return cards.Concat(TestCard.Factory(added));
                });

                Assert.True(funcCalled);
                Assert.Equal(expected: priorSize + added, actual: pile.Count);
            }
        }

        public sealed class TakeAt
        {
            [Fact]
            public void ThrowsWhenNotTakable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ PilePerms.CanTake, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.TakeAt(index: 14));
                Assert.Equal(expected: ErrorStrings.NoTake, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ThrowsNegativeIndex()
            {
                var pile = new TestPile(withPerms: PilePerms.CanTake, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.TakeAt(index: -1));
                Assert.StartsWith(expectedStartString: ErrorStrings.RetrievalNegative, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ThrowsTooHighIndex()
            {
                var pile = new TestPile(withPerms: PilePerms.CanTake, cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.TakeAt(index: pile.Count));
                Assert.StartsWith(expectedStartString: ErrorStrings.RetrievalTooHighP, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void DecreasesPileByOne()
            {
                var pile = new TestPile(withPerms: PilePerms.CanTake, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                var taken = pile.TakeAt(10);

                Assert.Equal(expected: priorSize - 1, actual: pile.Count);
            }

            [Fact]
            public void LastTakeCallsOnLastRemoved()
            {
                var pile = new TestPile(withPerms: PilePerms.CanTake, cards: TestCard.Factory(1));

                var ev = Assert.Raises<EventArgs>(handler => pile.LastRemoveCalled += handler, handler => pile.LastRemoveCalled -= handler, () => pile.TakeAt(0));
                Assert.Same(expected: pile, actual: ev.Sender);
                Assert.Equal(expected: 0, actual: pile.Count);
            }
        }

        public sealed class Top
        {
            [Fact]
            public void ThrowsWhenNotBrowsableOrPeekable()
            {
                var pile = new TestPile(withPerms: PilePerms.All ^ (PilePerms.CanBrowse | PilePerms.CanPeek), cards: TestCard.Factory(20));
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Top);
                Assert.Equal(expected: ErrorStrings.NoBrowseOrPeek, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Fact]
            public void ReturnsNullOnEmptyPile()
            {
                var pile = new TestPile(withPerms: PilePerms.CanBrowse, cards: Enumerable.Empty<TestCard>());
                var c = pile.Top;

                Assert.Equal(expected: 0, actual: pile.Count);
                Assert.Null(c);
            }

            [Fact]
            public void DoesNotChangePileSize()
            {
                var pile = new TestPile(withPerms: PilePerms.CanBrowse, cards: TestCard.Factory(20));
                var priorSize = pile.Count;
                var c = pile.Top;

                Assert.NotNull(c);
                Assert.Equal(expected: 1, actual: c.Id);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }
        }
    }
}
