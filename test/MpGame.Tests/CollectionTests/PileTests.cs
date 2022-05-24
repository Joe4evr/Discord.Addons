using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.MpGame.Collections;
using Xunit;

namespace MpGame.Tests.CollectionTests
{
    public class PileTests
    {
        private static IEnumerable<Pile<ITestCard>> Piles(PilePerms withPerms, IEnumerable<ITestCard> items)
        {
            yield return new TestPile(withPerms, items);
            yield return new WrappingTestPile(withPerms, items);
        }

        protected static IEnumerable<object[]> TheoryPiles(PilePerms withPerms, int num)
        {
            var seed = TestCard.Factory(num).ToArray();
            yield return new Pile<ITestCard>[] { new TestPile(withPerms, seed) };
            yield return new Pile<ITestCard>[] { new WrappingTestPile(withPerms, seed) };
        }

        public sealed class Ctor
        {
            [Fact]
            public void SeedingCtorThrowsOnNullSequence()
            {
                //this test method is special
                var pilefuncs = new Func<Pile<ITestCard>>[]
                {
                    () => new TestPile(withPerms: PilePerms.None, items: null!),
                    () => new WrappingTestPile(withPerms: PilePerms.None, items: null!)
                };

                foreach (var pile in pilefuncs)
                {
                    var ex = Assert.Throws<ArgumentNullException>(pile);
                    Assert.Equal(expected: "items", actual: ex.ParamName);
                }
            }

            [Fact]
            public void SeedingCtorFiltersOutNulls()
            {
                var seed = TestCard.Factory(20).ToArray<TestCard?>();
                seed[5] = null;
                seed[10] = null;
                seed[15] = null;
                var nulls = seed.Count(c => c is null);

                foreach (var pile in Piles(withPerms: PilePerms.CanBrowse, items: seed!))
                {
                    Assert.Equal(expected: seed.Length - nulls, actual: pile.Count);
                    Assert.All(pile.Browse(), c => Assert.NotNull(c));
                }

            }

            [Fact]
            public void SeedingCtorFiltersOutDuplicateReferences()
            {
                var initSeed = TestCard.Factory(5).ToArray();
                var seed = initSeed.Concat(initSeed).Concat(initSeed).Concat(initSeed).ToArray();
                int[] expectedIds = new[] { 1, 2, 3, 4, 5 };

                foreach (var pile in Piles(withPerms: PilePerms.CanBrowse, items: seed))
                {
                    Assert.Equal(expected: initSeed.Length, actual: pile.Count);
                    Assert.Equal(expected: expectedIds, actual: pile.AsEnumerable().Select(t => t.Id));
                }
            }

            [Fact]
            public void ShufflingCtorWorks()
            {
                var seed = TestCard.Factory(20).ToArray();
                var pile = new TestPile(withPerms: PilePerms.All, items: seed, initShuffle: true);
                var events = (ITestPileEvents)pile;

                var ev = Assert.Raises<ShuffleEventArgs>(
                    attach: handler => events.ShuffleCalled += handler,
                    detach: handler => events.ShuffleCalled -= handler,
                    () => pile.Shuffle());

                Assert.Equal(expected: seed.Length, actual: pile.Count);

                Assert.NotNull(ev.Arguments.OriginalSequence);
                Assert.NotEmpty(ev.Arguments.OriginalSequence);

                Assert.NotNull(ev.Arguments.NewSequence);
                Assert.NotEmpty(ev.Arguments.NewSequence);
            }
        }

        public sealed class AsEnumerable : PileTests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ PilePerms.CanBrowse), 20)]
            public void ThrowsWhenNotBrowsable(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.AsEnumerable());
                Assert.Equal(expected: PileErrorStrings.NoBrowse, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanBrowse, 20)]
            public void IsSameSequenceAsPile(Pile<ITestCard> pile)
            {
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20
                };

                Assert.Equal(expected: expectedSeq, actual: pile.AsEnumerable().Select(c => c.Id));
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.CanBrowse | PilePerms.CanDraw), 20)]
            public void ThrowsWhenAttemptingToChangeDuringEnumeration(Pile<ITestCard> pile)
            {
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

        public sealed class Browse : PileTests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ PilePerms.CanBrowse), 20)]
            public void ThrowsWhenNotBrowsable(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Browse());
                Assert.Equal(expected: PileErrorStrings.NoBrowse, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanBrowse, 20)]
            public void BrowsingDoesNotChangePileSize(Pile<ITestCard> pile)
            {
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20
                };

                var priorSize = pile.Count;
                var cards = pile.Browse();

                Assert.False(cards.IsDefault, "Returned array was defaulted.");
                Assert.NotEmpty(cards);
                Assert.Equal(expected: priorSize, actual: pile.Count);
                Assert.Equal(expected: priorSize, actual: cards.Length);
                Assert.Equal(expected: expectedSeq, actual: cards.Select(c => c.Id));
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanBrowse, 0)]
            public void EmptyPileIsNotNull(Pile<ITestCard> pile)
            {
                var cards = pile.Browse();

                Assert.Equal(expected: 0, actual: pile.Count);
                Assert.False(cards.IsDefault, "Returned array was defaulted.");
                Assert.Empty(cards);
            }
        }

        public sealed class BrowseAndTake : PileTests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ PilePerms.CanBrowse), 20)]
            public async Task ThrowsWhenNotBrowsable(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;
                bool selectorCalled = false;

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => pile.BrowseAndTakeAsync(selector: cards =>
                    {
                        selectorCalled = true;

                        return Task.FromResult<int[]?>(new[] { 5, 10 });
                    }));
                Assert.False(selectorCalled, "Selector function was called.");
                Assert.Equal(expected: PileErrorStrings.NoBrowseAndTake, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ PilePerms.CanTake), 20)]
            public async Task ThrowsWhenNotTakable(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;
                bool selectorCalled = false;

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => pile.BrowseAndTakeAsync(selector: cards =>
                    {
                        selectorCalled = true;

                        return Task.FromResult<int[]?>(new[] { 5, 10 });
                    }));
                Assert.False(selectorCalled, "Selector function was called.");
                Assert.Equal(expected: PileErrorStrings.NoBrowseAndTake, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.CanBrowse | PilePerms.CanTake), 20)]
            public async Task ThrowsOnNullSelector(Pile<ITestCard> pile)
            {
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20,
                };

                var priorSize = pile.Count;

                var ex = await Assert.ThrowsAsync<ArgumentNullException>(
                    () => pile.BrowseAndTakeAsync(selector: null!));
                Assert.Equal(expected: "selector", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.CanBrowse | PilePerms.CanTake), 20)]
            public async Task ThrowsOnBadIndices(Pile<ITestCard> pile)
            {
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20,
                };

                var priorSize = pile.Count;
                bool selectorCalled = false;

                var ex = await Assert.ThrowsAsync<IndexOutOfRangeException>(() =>
                    pile.BrowseAndTakeAsync(selector: cards =>
                    {
                        selectorCalled = true;
                        Assert.NotNull(cards);
                        Assert.NotEmpty(cards);

                        return Task.FromResult<int[]?>(new[] { 25 });
                    }));
                Assert.True(selectorCalled, "Selector function was not called.");
                Assert.Equal(expected: "Selected indeces '25' must be one of the provided item indices.", actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.CanBrowse | PilePerms.CanTake), 20)]
            public async Task DuplicateIndicesAreIgnored(Pile<ITestCard> pile)
            {
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5,    7, 8, 9,10,
                       12,13,14,15,16,17,18,19,20,
                };

                var priorSize = pile.Count;
                bool selectorCalled = false;
                var picks = await pile.BrowseAndTakeAsync(
                    selector: cards =>
                    {
                        selectorCalled = true;
                        Assert.NotNull(cards);
                        Assert.NotEmpty(cards);

                        return Task.FromResult<int[]?>(new[] { 5, 10, 5, 10 });
                    });

                Assert.True(selectorCalled, "Selector function was not called.");
                Assert.False(picks.IsDefault, "Returned array was defaulted.");
                Assert.NotEmpty(picks);
                Assert.Equal(expected: 2, actual: picks.Length);
                Assert.Equal(expected: new[] { 6, 11 }, actual: picks.Select(c => c.Id));
                Assert.Equal(expected: priorSize - 2, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.CanBrowse | PilePerms.CanTake), 20)]
            public async Task DoesNotThrowWhenNotShufflable(Pile<ITestCard> pile)
            {
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5,    7, 8, 9,10,
                       12,13,14,15,16,17,18,19,20,
                };

                var priorSize = pile.Count;
                bool selectorCalled = false;
                ImmutableArray<ITestCard> picks = default;
                var events = (ITestPileEvents)pile;

                await AssertEx.DoesNotRaiseAsync<ShuffleEventArgs>(
                    attach: handler => events.ShuffleCalled += handler,
                    detach: handler => events.ShuffleCalled -= handler,
                    async () => picks = await pile.BrowseAndTakeAsync(
                        selector: cards =>
                        {
                            selectorCalled = true;
                            Assert.NotEmpty(cards);

                            return Task.FromResult<int[]?>(new[] { 5, 10 });
                        },
                        shuffle: true));

                Assert.True(selectorCalled, "Selector function was not called.");
                Assert.False(picks.IsDefault, "Returned array was defaulted.");
                Assert.NotEmpty(picks);
                Assert.Equal(expected: new[] { 6, 11 }, actual: picks.Select(c => c.Id));
                Assert.Equal(expected: priorSize - 2, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.CanBrowse | PilePerms.CanTake), 20)]
            public async Task SelectorReceivesOnlyItemsThatAreNotFilteredOut(Pile<ITestCard> pile)
            {
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5,    7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20,
                };

                var priorSize = pile.Count;
                bool selectorCalled = false;
                bool filterCalled = false;
                var picks = await pile.BrowseAndTakeAsync(
                    selector: cards =>
                    {
                        selectorCalled = true;
                        Assert.NotEmpty(cards);
                        Assert.All(cards.Values, c => Assert.True(LessThanOrEqualToTenFilter(c),
                            $"Item passed that did not pass predicate. Value: '{c.Id}'"));

                        return Task.FromResult<int[]?>(new[] { 5 });
                    },
                    filter: (c, _) =>
                    {
                        filterCalled = true;

                        return LessThanOrEqualToTenFilter(c);
                    });

                Assert.True(selectorCalled, "Selector function was not called.");
                Assert.True(filterCalled, "Filter function was not called.");
                Assert.False(picks.IsDefault, "Returned array was defaulted.");
                Assert.NotEmpty(picks);
                Assert.Single(picks);
                Assert.Equal(expected: new[] { 6 }, actual: picks.Select(c => c.Id));
                Assert.Equal(expected: priorSize - 1, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));

                static bool LessThanOrEqualToTenFilter(ITestCard c) => c.Id <= 10;
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.CanBrowse | PilePerms.CanTake | PilePerms.CanShuffle), 20)]
            public async Task ShuffleWorksWhenAllowed(Pile<ITestCard> pile)
            {
                var expectedSeq = new[]
                {
                    20,19,18,17,16,15,14,13,12,
                    10, 9, 8, 7,    5, 4, 3, 2, 1
                };

                var priorSize = pile.Count;
                bool selectorCalled = false;
                ImmutableArray<ITestCard> picks = default;
                var events = (ITestPileEvents)pile;

                var ev = await Assert.RaisesAsync<ShuffleEventArgs>(
                    attach: handler => events.ShuffleCalled += handler,
                    detach: handler => events.ShuffleCalled -= handler,
                    async () => picks = await pile.BrowseAndTakeAsync(
                        selector: cards =>
                        {
                            selectorCalled = true;
                            Assert.NotNull(cards);
                            Assert.NotEmpty(cards);

                            return Task.FromResult<int[]?>(new[] { 5, 10 });
                        },
                        shuffle: true));

                Assert.True(selectorCalled, "Selector function was not called.");
                Assert.False(picks.IsDefault, "Returned array was defaulted.");
                Assert.NotEmpty(picks);
                Assert.Equal(expected: expectedSeq, actual: ev.Arguments.NewSequence.Select(c => c.Id));
                Assert.Equal(expected: 2, actual: picks.Length);
                Assert.Equal(expected: new[] { 6, 11 }, actual: picks.Select(c => c.Id));
                Assert.Equal(expected: priorSize - 2, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.CanBrowse | PilePerms.CanTake | PilePerms.CanShuffle), 10)]
            public async Task ShufflesBackAllCards(Pile<ITestCard> pile)
            {
                var expectedSeq = new[]
                {
                    10, 9, 8, 7, 6, 5, 4, 3, 2
                };

                var priorSize = pile.Count;
                bool selectorCalled = false;
                bool filterCalled = false;
                ImmutableArray<ITestCard> picks = default;
                var events = (ITestPileEvents)pile;

                var ev = await Assert.RaisesAsync<ShuffleEventArgs>(
                    attach: handler => events.ShuffleCalled += handler,
                    detach: handler => events.ShuffleCalled -= handler,
                    async () => picks = await pile.BrowseAndTakeAsync(
                        selector: cards =>
                        {
                            selectorCalled = true;
                            Assert.NotNull(cards);
                            Assert.NotEmpty(cards);

                            return Task.FromResult<int[]?>(new[] { cards.First().Key });
                        },
                        filter: (card, _) =>
                        {
                            filterCalled = true;

                            return card.Color == CardColor.Green;
                        },
                        shuffle: true));

                Assert.True(selectorCalled, "Selector function was not called.");
                Assert.True(filterCalled, "Filter function was not called.");
                Assert.False(picks.IsDefault, "Returned array was defaulted.");
                Assert.NotEmpty(picks);
                Assert.Equal(expected: new[] { 1 }, actual: picks.Select(c => c.Id));
                Assert.Equal(expected: priorSize - 1, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.CanBrowse | PilePerms.CanTake), 20)]
            public async Task EmptyOrNullSelectionArrayDoesNotDecreasePileSize(Pile<ITestCard> pile)
            {
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20,
                };

                var priorSize = pile.Count;
                bool selectorCalled = false;
                var picks = await pile.BrowseAndTakeAsync(
                    selector: cards =>
                    {
                        selectorCalled = true;
                        Assert.NotEmpty(cards);

                        return Task.FromResult<int[]?>(Array.Empty<int>());
                    });

                Assert.True(selectorCalled, "Selector function was not called.");
                Assert.False(picks.IsDefault, "Returned array was defaulted.");
                Assert.Empty(picks);
                Assert.Equal(expected: priorSize, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));

                selectorCalled = false;
                picks = await pile.BrowseAndTakeAsync(
                    selector: cards =>
                    {
                        selectorCalled = true;
                        Assert.NotEmpty(cards);

                        return Task.FromResult((int[]?)null);
                    });

                Assert.True(selectorCalled, "Selector function was not called.");
                Assert.False(picks.IsDefault, "Returned array was defaulted.");
                Assert.Empty(picks);
                Assert.Equal(expected: priorSize, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }
        }

        public sealed class Clear : PileTests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ PilePerms.CanClear), 20)]
            public void ThrowsWhenNotClearable(Pile<ITestCard> pile)
            {
                Assert.False(pile.CanClear, "Input pile had CanClear.");
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Clear());
                Assert.Equal(expected: PileErrorStrings.NoClear, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanClear, 20)]
            public void ClearingEmptiesPile(Pile<ITestCard> pile)
            {
                Assert.True(pile.CanClear, "Input pile did not have CanClear.");
                var priorSize = pile.Count;
                var cleared = pile.Clear();

                Assert.Equal(expected: 0, actual: pile.Count);
                Assert.Equal(expected: priorSize, actual: cleared.Length);
            }
        }

        public sealed class Cut : PileTests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ PilePerms.CanCut), 20)]
            public void ThrowsWhenNotCuttable(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Cut(amount: 10));
                Assert.Equal(expected: PileErrorStrings.NoCut, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanCut, 20)]
            public void ThrowsNegativeIndex(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.Cut(amount: -1));
                Assert.StartsWith(expectedStartString: PileErrorStrings.CutAmountNegative, actualString: ex.Message);
                Assert.Equal(expected: "amount", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanCut, 20)]
            public void ThrowsTooHighIndex(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.Cut(amount: pile.Count + 1));
                Assert.StartsWith(expectedStartString: PileErrorStrings.CutAmountTooHigh, actualString: ex.Message);
                Assert.Equal(expected: "amount", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.CanCut | PilePerms.CanBrowse), 20)]
            public void DoesNotChangePileSize(Pile<ITestCard> pile)
            {
                var expectedSeq = new[]
                {
                    11,12,13,14,15,16,17,18,19,20,
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10
                };
                var priorSize = pile.Count;
                pile.Cut(amount: 10);

                Assert.Equal(expected: priorSize, actual: pile.Count);
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }
        }

        public sealed class Draw : PileTests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ PilePerms.CanDraw), 20)]
            public void ThrowsWhenNotDrawable(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Draw());
                Assert.Equal(expected: PileErrorStrings.NoDraw, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanDraw, 20)]
            public void DecreasesPileByOne(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;
                var drawn = pile.Draw();

                Assert.NotNull(drawn);
                Assert.Equal(expected: 1, actual: drawn.Id);
                Assert.Equal(expected: priorSize - 1, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanDraw, 1)]
            public void LastDrawCallsOnLastRemoved(Pile<ITestCard> pile)
            {
                var events = (ITestPileEvents)pile;
                var ev = Assert.Raises<EventArgs>(
                    attach: handler => events.LastRemoveCalled += handler,
                    detach: handler => events.LastRemoveCalled -= handler,
                    () => pile.Draw());

                Assert.Same(expected: pile, actual: ev.Sender);
                Assert.Equal(expected: 0, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanDraw, 0)]
            public void ThrowsOnEmptyPile(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Draw());
                Assert.Equal(expected: PileErrorStrings.PileEmpty, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }
        }

        public sealed class DrawBottom : PileTests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ PilePerms.CanDrawBottom), 20)]
            public void ThrowsWhenNotDrawable(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.DrawBottom());
                Assert.Equal(expected: PileErrorStrings.NoDraw, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanDrawBottom, 20)]
            public void DecreasesPileByOne(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;
                var drawn = pile.DrawBottom();

                Assert.NotNull(drawn);
                Assert.Equal(expected: 20, actual: drawn.Id);
                Assert.Equal(expected: priorSize - 1, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanDrawBottom, 1)]
            public void LastDrawCallsOnLastRemoved(Pile<ITestCard> pile)
            {
                var events = (ITestPileEvents)pile;
                var ev = Assert.Raises<EventArgs>(
                    attach: handler => events.LastRemoveCalled += handler,
                    detach: handler => events.LastRemoveCalled -= handler,
                    () => pile.DrawBottom());

                Assert.Same(expected: pile, actual: ev.Sender);
                Assert.Equal(expected: 0, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanDrawBottom, 0)]
            public void ThrowsOnEmptyPile(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.DrawBottom());
                Assert.Equal(expected: PileErrorStrings.PileEmpty, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }
        }

        public sealed class MultiDraw : PileTests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ PilePerms.CanDraw), 20)]
            public void ThrowsWhenNotDrawable(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.DrawMultiple(3));
                Assert.Equal(expected: PileErrorStrings.NoDraw, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanDraw, 20)]
            public void DecreasesPileByAmount(Pile<ITestCard> pile)
            {
                const int amount = 4;
                var priorSize = pile.Count;
                var drawn = pile.DrawMultiple(amount);

                Assert.NotEmpty(drawn);
                Assert.Equal(expected: amount, actual: drawn.Length);
                Assert.Equal(expected: priorSize - amount, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanDraw, 4)]
            public void LastDrawCallsOnLastRemoved(Pile<ITestCard> pile)
            {
                var events = (ITestPileEvents)pile;
                var ev = Assert.Raises<EventArgs>(
                    attach: handler => events.LastRemoveCalled += handler,
                    detach: handler => events.LastRemoveCalled -= handler,
                    () => pile.DrawMultiple(4));

                Assert.Same(expected: pile, actual: ev.Sender);
                Assert.Equal(expected: 0, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanDraw, 0)]
            public void ThrowsWhenArgOutOfRange(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex1 = Assert.Throws<ArgumentOutOfRangeException>(() => pile.DrawMultiple(amount: 3));
                Assert.StartsWith(expectedStartString: PileErrorStrings.RetrievalTooHighP, actualString: ex1.Message);
                Assert.Equal(expected: "amount", actual: ex1.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);


                var ex2 = Assert.Throws<ArgumentOutOfRangeException>(() => pile.DrawMultiple(amount: - 3));
                Assert.StartsWith(expectedStartString: PileErrorStrings.RetrievalNegative, actualString: ex2.Message);
                Assert.Equal(expected: "amount", actual: ex2.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanDraw, 20)]
            public void AmountOfZeroReturnsAnEmptyArray(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;
                var drawn = pile.DrawMultiple(amount: 0);

                Assert.Empty(drawn);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }
        }

        public sealed class InsertAt : PileTests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ PilePerms.CanInsert), 20)]
            public void ThrowsWhenNotInsertable(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.InsertAt(item: new TestCard(id: 2), index: 15));
                Assert.Equal(expected: PileErrorStrings.NoInsert, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanInsert, 20)]
            public void ThrowsOnNullCard(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentNullException>(() => pile.InsertAt(item: null!, index: 10));
                Assert.Equal(expected: "item", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanInsert, 20)]
            public void ThrowsNegativeIndex(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.InsertAt(item: new TestCard(id: 2), index: -1));
                Assert.StartsWith(expectedStartString: PileErrorStrings.InsertionNegative, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanInsert, 20)]
            public void ThrowsTooHighIndex(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.InsertAt(new TestCard(id: 2), index: pile.Count + 1));
                Assert.StartsWith(expectedStartString: PileErrorStrings.InsertionTooHigh, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanInsert, 20)]
            public void IncreasesPileByOne(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;
                var newcard = new TestCard(id: 1);
                pile.InsertAt(item: newcard, index: 10);

                Assert.Equal(expected: priorSize + 1, actual: pile.Count);
            }
        }

        public sealed class Mill : PileTests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ (PilePerms.CanBrowse | PilePerms.CanDraw)), 20)]
            public void ThrowsWhenSourceNotDrawableOrBrowsable(Pile<ITestCard> source)
            {
                foreach (var target in Piles(withPerms: PilePerms.CanPut, items: Enumerable.Empty<TestCard>()))
                {
                    var sourceSize = source.Count;
                    var targetSize = target.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => source.Mill(target));
                    Assert.Equal(expected: PileErrorStrings.NoDraw, actual: ex.Message);
                    Assert.Equal(expected: sourceSize, actual: source.Count);
                    Assert.Equal(expected: targetSize, actual: target.Count);
                }
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanDraw, 20)]
            public void ThrowsWhenTargetNull(Pile<ITestCard> source)
            {
                var sourceSize = source.Count;

                var ex = Assert.Throws<ArgumentNullException>(() => source.Mill(targetPile: null!));
                Assert.Equal(expected: "targetPile", actual: ex.ParamName);
                Assert.Equal(expected: sourceSize, actual: source.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanDraw, 20)]
            public void ThrowsWhenTargetNotPuttable(Pile<ITestCard> source)
            {
                foreach (var target in Piles(withPerms: PilePerms.All ^ PilePerms.CanPut, items: Enumerable.Empty<TestCard>()))
                {
                    var sourceSize = source.Count;
                    var targetSize = target.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => source.Mill(target));
                    Assert.Equal(expected: PileErrorStrings.NoPutTarget, actual: ex.Message);
                    Assert.Equal(expected: sourceSize, actual: source.Count);
                    Assert.Equal(expected: targetSize, actual: target.Count);
                }
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.CanDraw | PilePerms.CanPut), 20)]
            public void ThrowsWhenTargetIsSourceInstance(Pile<ITestCard> source)
            {
                var sourceSize = source.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => source.Mill(source));
                Assert.Equal(expected: PileErrorStrings.MillTargetSamePile, actual: ex.Message);
                Assert.Equal(expected: sourceSize, actual: source.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanDraw, 0)]
            public void ThrowsOnEmptyPile(Pile<ITestCard> source)
            {
                foreach (var target in Piles(withPerms: PilePerms.CanPut, items: Enumerable.Empty<TestCard>()))
                {
                    var sourceSize = source.Count;
                    var targetSize = target.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => source.Mill(target));
                    Assert.Equal(expected: PileErrorStrings.PileEmpty, actual: ex.Message);
                    Assert.Equal(expected: sourceSize, actual: source.Count);
                    Assert.Equal(expected: targetSize, actual: target.Count);
                }
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.CanPut | PilePerms.CanBrowse), 0)]
            public void DecreasesSourseSizeIncreasesTargetSizeByOne(Pile<ITestCard> target)
            {
                var sourceSeed = TestCard.Factory(1).ToArray();
                foreach (var source in Piles(withPerms: PilePerms.CanDraw | PilePerms.CanPeek, items: sourceSeed))
                {
                    var sourceSize = source.Count;
                    var targetSize = target.Count;
                    var topcard = source.PeekAt(0);
                    int firstCall = 0;
                    bool lastRmCalled = false;
                    bool putCalled = false;

                    (source as ITestPileEvents)!.LastRemoveCalled += (s, e) =>
                    {
                        lastRmCalled = true;
                        Interlocked.CompareExchange(ref firstCall, value: 1, comparand: 0);
                    };
                    (target as ITestPileEvents)!.PutCalled += (s, e) =>
                    {
                        putCalled = true;
                        Interlocked.CompareExchange(ref firstCall, value: 2, comparand: 0);
                    };

                    source.Mill(target);
                    Assert.True(lastRmCalled, "OnLastRemove function was not called.");
                    Assert.True(putCalled, "OnPut function was not called.");
                    Assert.Equal(expected: 1, actual: firstCall);
                    Assert.Equal(expected: sourceSize - 1, actual: source.Count);
                    Assert.Equal(expected: targetSize + 1, actual: target.Count);
                    Assert.Same(expected: topcard, actual: target.PeekAt(0));
                }
            }
        }

        public sealed class PeekAt : PileTests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ (PilePerms.CanBrowse | PilePerms.CanPeek)), 20)]
            public void ThrowsWhenNotBrowsableOrPeekable(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.PeekAt(index: 5));
                Assert.Equal(expected: PileErrorStrings.NoBrowseOrPeek, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanPeek, 20)]
            public void ThrowsNegativeIndex(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.PeekAt(index: -1));
                Assert.StartsWith(expectedStartString: PileErrorStrings.PeekAmountNegative, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanPeek, 20)]
            public void ThrowsTooHighIndex(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.PeekAt(index: pile.Count + 1));
                Assert.StartsWith(expectedStartString: PileErrorStrings.PeekAmountTooHigh, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanPeek, 20)]
            public void DoesNotChangePileSize(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var peeked = pile.PeekAt(index: 5)!;

                Assert.NotNull(peeked);
                Assert.Equal(expected: 6, actual: peeked.Id);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanBrowse, 0)]
            public void ReturnsNullOnEmptyPile(Pile<ITestCard> pile)
            {
                var c = pile.PeekAt(0);

                Assert.Equal(expected: 0, actual: pile.Count);
                Assert.Null(c);
            }
        }

        public sealed class PeekTop : PileTests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ (PilePerms.CanBrowse | PilePerms.CanPeek)), 20)]
            public void ThrowsWhenNotBrowsableOrPeekable(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.PeekTop(amount: 5));
                Assert.Equal(expected: PileErrorStrings.NoBrowseOrPeek, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanPeek, 20)]
            public void ThrowsNegativeIndex(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.PeekTop(amount: -1));
                Assert.StartsWith(expectedStartString: PileErrorStrings.PeekAmountNegative, actualString: ex.Message);
                Assert.Equal(expected: "amount", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanPeek, 20)]
            public void ThrowsTooHighIndex(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.PeekTop(amount: pile.Count + 1));
                Assert.StartsWith(expectedStartString: PileErrorStrings.PeekAmountTooHigh, actualString: ex.Message);
                Assert.Equal(expected: "amount", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanPeek, 20)]
            public void DoesNotChangePileSize(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;
                var expectedSeq = new[] { 1, 2, 3 };

                var peeked = pile.PeekTop(amount: 3);

                Assert.False(peeked.IsDefault, "Returned array was defaulted.");
                Assert.Equal(expected: expectedSeq, actual: peeked.Select(c => c.Id));
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }
        }

        public sealed class Put : PileTests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ PilePerms.CanPut), 20)]
            public void ThrowsWhenNotPuttable(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Put(item: new TestCard(id: 2)));
                Assert.Equal(expected: PileErrorStrings.NoPut, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanPut, 20)]
            public void ThrowsOnNullCard(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentNullException>(() => pile.Put(item: null!));
                Assert.Equal(expected: "item", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanPut, 0)]
            public void CallsOnPut(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;
                var newcard = new TestCard(id: 1);
                var events = (ITestPileEvents)pile;

                var ev = Assert.Raises<PutEventArgs>(
                    attach: handler => events.PutCalled += handler,
                    detach: handler => events.PutCalled -= handler,
                    () => pile.Put(item: newcard));

                Assert.Same(expected: pile, actual: ev.Sender);
                Assert.NotNull(ev.Arguments.Card);
                Assert.Same(expected: newcard, actual: ev.Arguments.Card);
                Assert.Equal(expected: priorSize + 1, actual: pile.Count);
            }
        }

        public sealed class PutBottom : PileTests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ PilePerms.CanPutBottom), 20)]
            public void ThrowsWhenNotPuttableBottom(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.PutBottom(item: new TestCard(id: 2)));
                Assert.Equal(expected: PileErrorStrings.NoPutBottom, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanPutBottom, 20)]
            public void ThrowsOnNullCard(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentNullException>(() => pile.PutBottom(item: null!));
                Assert.Equal(expected: "item", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanPutBottom, 0)]
            public void IncreasesPileByOne(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;
                var newcard = new TestCard(id: 1);
                pile.PutBottom(item: newcard);

                Assert.Equal(expected: priorSize + 1, actual: pile.Count);
            }
        }

        public sealed class Shuffle : PileTests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ PilePerms.CanShuffle), 20)]
            public void ThrowsWhenNotShufflable(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Shuffle());
                Assert.Equal(expected: PileErrorStrings.NoShuffle, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanShuffle, 20)]
            public void ThrowsOnNullFuncReturn(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;
                bool funcCalled = false;
                var events = (ITestPileEvents)pile;
                events.ShuffleFuncOverride = _ =>
                {
                    funcCalled = true;
                    return null!;
                };

                var ex = Assert.Throws<InvalidOperationException>(() => pile.Shuffle());
                Assert.True(funcCalled, "Shuffle function was not called.");
                Assert.Equal(expected: PileErrorStrings.NewSequenceNull, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanShuffle, 200)]
            public void Test(Pile<ITestCard> pile)
            {
                pile.Shuffle();
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanShuffle, 0)]
            public void ShuffleFuncDoesNotGiveDefaultArray(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;
                var events = (ITestPileEvents)pile;

                var ev = Assert.Raises<ShuffleEventArgs>(
                    attach: handler => events.ShuffleCalled += handler,
                    detach: handler => events.ShuffleCalled -= handler,
                    () => pile.Shuffle());

                Assert.NotNull(ev.Arguments.OriginalSequence);
            }
        }

        public sealed class TakeAt : PileTests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), (PilePerms.All ^ PilePerms.CanTake), 20)]
            public void ThrowsWhenNotTakable(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => pile.TakeAt(index: 14));
                Assert.Equal(expected: PileErrorStrings.NoTake, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanTake, 20)]
            public void ThrowsNegativeIndex(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.TakeAt(index: -1));
                Assert.StartsWith(expectedStartString: PileErrorStrings.RetrievalNegative, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanTake, 20)]
            public void ThrowsTooHighIndex(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.TakeAt(index: pile.Count));
                Assert.StartsWith(expectedStartString: PileErrorStrings.RetrievalTooHighP, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanTake, 20)]
            public void DecreasesPileByOne(Pile<ITestCard> pile)
            {
                var priorSize = pile.Count;
                _ = pile.TakeAt(10);

                Assert.Equal(expected: priorSize - 1, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.CanTake, 1)]
            public void LastTakeCallsOnLastRemoved(Pile<ITestCard> pile)
            {
                var events = (ITestPileEvents)pile;
                var ev = Assert.Raises<EventArgs>(
                    attach: handler => events.LastRemoveCalled += handler,
                    detach: handler => events.LastRemoveCalled -= handler,
                    () => pile.TakeAt(0));

                Assert.Same(expected: pile, actual: ev.Sender);
                Assert.Equal(expected: 0, actual: pile.Count);
            }
        }
    }
}
