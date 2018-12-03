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
    public sealed class PileTests
    {
        private static IEnumerable<Pile<ITestCard>> Piles(PilePerms withPerms, IEnumerable<ITestCard> items)
        {
            yield return new TestPile(withPerms, items);
            yield return new WrappingTestPile(withPerms, items);
        }

        public sealed class Ctor
        {
            [Fact]
            public void SeedingCtorThrowsOnNullSequence()
            {
                //this test method is special
                var pilefuncs = new Func<Pile<ITestCard>>[]
                {
                    () => new TestPile(withPerms: PilePerms.None, items: null),
                    () => new WrappingTestPile(withPerms: PilePerms.None, items: null)
                };

                foreach (var pile in pilefuncs)
                {
                    var ex = Assert.Throws<ArgumentNullException>(() => pile());
                    Assert.Equal(expected: "items", actual: ex.ParamName);
                }
            }

            [Fact]
            public void SeedingCtorFiltersOutNulls()
            {
                var seed = TestCard.Factory(20).ToArray();
                seed[5] = null;
                seed[10] = null;
                seed[15] = null;
                var nulls = seed.Count(c => c == null);

                foreach (var pile in Piles(withPerms: PilePerms.CanBrowse, items: seed))
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
        }

        public sealed class AsEnumerable
        {
            [Fact]
            public void ThrowsWhenNotBrowsable()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.All ^ PilePerms.CanBrowse, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => pile.AsEnumerable());
                    Assert.Equal(expected: ErrorStrings.NoBrowse, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void IsSameSequenceAsPile()
            {
                var seed = TestCard.Factory(20).ToArray();
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20
                };

                foreach (var pile in Piles(withPerms: PilePerms.CanBrowse, items: seed))
                {
                    Assert.Equal(expected: expectedSeq, actual: pile.AsEnumerable().Select(c => c.Id));
                    Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
                }
            }

            [Fact]
            public void ThrowsWhenAttemptingToChangeDuringEnumeration()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanBrowse | PilePerms.CanDraw, items: seed))
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
        }

        public sealed class Browse
        {
            [Fact]
            public void ThrowsWhenNotBrowsable()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.All ^ PilePerms.CanBrowse, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => pile.Browse());
                    Assert.Equal(expected: ErrorStrings.NoBrowse, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void BrowsingDoesNotChangePileSize()
            {
                var seed = TestCard.Factory(20).ToArray();
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20
                };

                foreach (var pile in Piles(withPerms: PilePerms.CanBrowse, items: seed))
                {
                    var priorSize = pile.Count;
                    var cards = pile.Browse();

                    Assert.False(cards.IsDefault);
                    Assert.NotEmpty(cards);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                    Assert.Equal(expected: priorSize, actual: cards.Length);
                    Assert.Equal(expected: expectedSeq, actual: cards.Select(c => c.Id));
                }
            }

            [Fact]
            public void EmptyPileIsNotNull()
            {
                foreach (var pile in Piles(withPerms: PilePerms.CanBrowse, items: Enumerable.Empty<ITestCard>()))
                {
                    var cards = pile.Browse();

                    Assert.Equal(expected: 0, actual: pile.Count);
                    Assert.False(cards.IsDefault);
                    Assert.True(cards.IsEmpty);
                }
            }
        }

        public sealed class BrowseAndTake
        {
            [Fact]
            public async Task ThrowsWhenNotBrowsable()
            {
                var seed = TestCard.Factory(20).ToArray();

                foreach (var pile in Piles(withPerms: PilePerms.All ^ PilePerms.CanBrowse, items: seed))
                {
                    var priorSize = pile.Count;
                    bool selectorCalled = false;

                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                        () => pile.BrowseAndTakeAsync(selector: cards =>
                        {
                            selectorCalled = true;

                            return Task.FromResult(new[] { 5, 10 });
                        }));
                    Assert.False(selectorCalled);
                    Assert.Equal(expected: ErrorStrings.NoBrowseAndTake, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public async Task ThrowsWhenNotTakable()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.All ^ PilePerms.CanTake, items: seed))
                {
                    var priorSize = pile.Count;
                    bool selectorCalled = false;

                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                        () => pile.BrowseAndTakeAsync(selector: cards =>
                        {
                            selectorCalled = true;

                            return Task.FromResult(new[] { 5, 10 });
                        }));
                    Assert.False(selectorCalled);
                    Assert.Equal(expected: ErrorStrings.NoBrowseAndTake, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public async Task ThrowsOnNullSelector()
            {
                var seed = TestCard.Factory(20).ToArray();
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20,
                };

                foreach (var pile in Piles(withPerms: PilePerms.CanBrowse | PilePerms.CanTake, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = await Assert.ThrowsAsync<ArgumentNullException>(
                        () => pile.BrowseAndTakeAsync(selector: null));
                    Assert.Equal(expected: "selector", actual: ex.ParamName);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                    Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
                }
            }

            [Fact]
            public async Task ThrowsOnBadIndices()
            {
                var seed = TestCard.Factory(20).ToArray();
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20,
                };

                foreach (var pile in Piles(withPerms: PilePerms.CanBrowse | PilePerms.CanTake, items: seed))
                {
                    var priorSize = pile.Count;
                    bool selectorCalled = false;

                    var ex = await Assert.ThrowsAsync<IndexOutOfRangeException>(() =>
                        pile.BrowseAndTakeAsync(selector: cards =>
                        {
                            selectorCalled = true;
                            Assert.NotNull(cards);
                            Assert.NotEmpty(cards);

                            return Task.FromResult(new[] { 25 });
                        }));
                    Assert.True(selectorCalled);
                    Assert.Equal(expected: "Selected indeces '25' must be one of the provided item indices.", actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                    Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
                }
            }

            [Fact]
            public async Task DuplicateIndicesAreIgnored()
            {
                var seed = TestCard.Factory(20).ToArray();
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5,    7, 8, 9,10,
                       12,13,14,15,16,17,18,19,20,
                };

                foreach (var pile in Piles(withPerms: PilePerms.CanBrowse | PilePerms.CanTake, items: seed))
                {
                    var priorSize = pile.Count;
                    bool selectorCalled = false;
                    var picks = await pile.BrowseAndTakeAsync(
                        selector: cards =>
                        {
                            selectorCalled = true;
                            Assert.NotNull(cards);
                            Assert.NotEmpty(cards);

                            return Task.FromResult(new[] { 5, 10, 5, 10 });
                        });

                    Assert.True(selectorCalled);
                    Assert.False(picks.IsDefault);
                    Assert.NotEmpty(picks);
                    //Assert.Equal(expected: 2, actual: picks.Length);
                    Assert.Equal(expected: new[] { 6, 11 }, actual: picks.Select(c => c.Id));
                    Assert.Equal(expected: priorSize - 2, actual: pile.Count);
                    Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
                }
            }

            [Fact]
            public async Task DoesNotThrowWhenNotShufflable()
            {
                var seed = TestCard.Factory(20).ToArray();
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5,    7, 8, 9,10,
                       12,13,14,15,16,17,18,19,20,
                };

                foreach (var pile in Piles(withPerms: PilePerms.CanBrowse | PilePerms.CanTake, items: seed))
                {
                    var priorSize = pile.Count;
                    bool selectorCalled = false;
                    bool shuffleFuncCalled = false;
                    var picks = await pile.BrowseAndTakeAsync(
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

                    Assert.True(selectorCalled);
                    Assert.False(shuffleFuncCalled);
                    Assert.False(picks.IsDefault);
                    Assert.NotEmpty(picks);
                    //Assert.Equal(expected: 2, actual: picks.Length);
                    Assert.Equal(expected: new[] { 6, 11 }, actual: picks.Select(c => c.Id));
                    Assert.Equal(expected: priorSize - 2, actual: pile.Count);
                    Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
                }
            }

            [Fact]
            public async Task SelectorReceivesOnlyItemsThatAreNotFilteredOut()
            {
                var seed = TestCard.Factory(20).ToArray();
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5,    7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20,
                };

                foreach (var pile in Piles(withPerms: PilePerms.CanBrowse | PilePerms.CanTake, items: seed))
                {
                    var priorSize = pile.Count;
                    bool selectorCalled = false;
                    bool filterCalled = false;
                    var picks = await pile.BrowseAndTakeAsync(
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

                    Assert.True(selectorCalled);
                    Assert.True(filterCalled);
                    Assert.False(picks.IsDefault);
                    Assert.NotEmpty(picks);
                    //Assert.Single(picks);
                    Assert.Equal(expected: new[] { 6 }, actual: picks.Select(c => c.Id));
                    Assert.Equal(expected: priorSize - 1, actual: pile.Count);
                    Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
                }

                bool LessThanOrEqualToTenFilter(ITestCard c) => c.Id <= 10;
            }

            [Fact]
            public async Task ShuffleWorksWhenAllowed()
            {
                var seed = TestCard.Factory(20).ToArray();
                var expectedSeq = new[]
                {
                    20,19,18,17,16,15,14,13,12,
                    10, 9, 8, 7,    5, 4, 3, 2, 1
                };

                foreach (var pile in Piles(withPerms: PilePerms.CanBrowse | PilePerms.CanTake | PilePerms.CanShuffle, items: seed))
                {
                    var priorSize = pile.Count;
                    bool selectorCalled = false;
                    bool shuffleFuncCalled = false;
                    var picks = await pile.BrowseAndTakeAsync(
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

                    Assert.True(selectorCalled);
                    Assert.True(shuffleFuncCalled);
                    Assert.False(picks.IsDefault);
                    Assert.NotEmpty(picks);
                    //Assert.Equal(expected: 2, actual: picks.Length);
                    Assert.Equal(expected: new[] { 6, 11 }, actual: picks.Select(c => c.Id));
                    Assert.Equal(expected: priorSize - 2, actual: pile.Count);
                    Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
                }
            }

            [Fact]
            public async Task ShufflesBackAllCards()
            {
                var seed = TestCard.Factory(10).ToArray();
                var expectedSeq = new[]
                {
                    10, 9, 8, 7, 6, 5, 4, 3, 2
                };

                foreach (var pile in Piles(withPerms: PilePerms.CanBrowse | PilePerms.CanTake | PilePerms.CanShuffle, items: seed))
                {
                    var priorSize = pile.Count;
                    bool selectorCalled = false;
                    bool shuffleFuncCalled = false;
                    bool filterCalled = false;
                    var picks = await pile.BrowseAndTakeAsync(
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

                    Assert.True(selectorCalled);
                    Assert.True(shuffleFuncCalled);
                    Assert.True(filterCalled);
                    Assert.False(picks.IsDefault);
                    Assert.NotEmpty(picks);
                    Assert.Equal(expected: new[] { 1 }, actual: picks.Select(c => c.Id));
                    Assert.Equal(expected: priorSize - 1, actual: pile.Count);
                    Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
                }
            }

            [Fact]
            public async Task EmptySelectionArrayDoesNotDecreasePileSize()
            {
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20,
                };
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanBrowse | PilePerms.CanTake, items: seed))
                {
                    var priorSize = pile.Count;
                    bool selectorCalled = false;
                    var picks = await pile.BrowseAndTakeAsync(
                        selector: cards =>
                        {
                            selectorCalled = true;
                            Assert.NotEmpty(cards);

                            return Task.FromResult(Array.Empty<int>());
                        });

                    Assert.True(selectorCalled);
                    Assert.False(picks.IsDefault);
                    Assert.True(picks.IsEmpty);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                    Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
                }
            }
        }

        public sealed class Clear
        {
            [Fact]
            public void ThrowsWhenNotClearable()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.All ^ PilePerms.CanClear, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => pile.Clear());
                    Assert.Equal(expected: ErrorStrings.NoClear, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ClearingEmptiesPile()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanClear, items: seed))
                {
                    var priorSize = pile.Count;
                    var cleared = pile.Clear();

                    Assert.Equal(expected: 0, actual: pile.Count);
                    Assert.Equal(expected: priorSize, actual: cleared.Length);
                }
            }
        }

        public sealed class Cut
        {
            [Fact]
            public void ThrowsWhenNotCuttable()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.All ^ PilePerms.CanCut, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => pile.Cut(amount: 10));
                    Assert.Equal(expected: ErrorStrings.NoCut, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ThrowsNegativeIndex()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanCut, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.Cut(amount: -1));
                    Assert.StartsWith(expectedStartString: ErrorStrings.CutAmountNegative, actualString: ex.Message);
                    Assert.Equal(expected: "amount", actual: ex.ParamName);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ThrowsTooHighIndex()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanCut, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.Cut(amount: pile.Count + 1));
                    Assert.StartsWith(expectedStartString: ErrorStrings.CutAmountTooHigh, actualString: ex.Message);
                    Assert.Equal(expected: "amount", actual: ex.ParamName);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void DoesNotChangePileSize()
            {
                var seed = TestCard.Factory(20).ToArray();
                var expectedSeq = new[]
                {
                    11,12,13,14,15,16,17,18,19,20,
                     1, 2, 3, 4, 5, 6, 7, 8, 9,10
                };
                foreach (var pile in Piles(withPerms: PilePerms.CanCut | PilePerms.CanBrowse, items: seed))
                {
                    var priorSize = pile.Count;
                    pile.Cut(amount: 10);

                    Assert.Equal(expected: priorSize, actual: pile.Count);
                    Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
                }
            }
        }

        public sealed class Draw
        {
            [Fact]
            public void ThrowsWhenNotDrawable()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.All ^ PilePerms.CanDraw, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => pile.Draw());
                    Assert.Equal(expected: ErrorStrings.NoDraw, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void DecreasesPileByOne()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanDraw, items: seed))
                {
                    var priorSize = pile.Count;
                    var drawn = pile.Draw();

                    Assert.NotNull(drawn);
                    Assert.Equal(expected: 1, actual: drawn.Id);
                    Assert.Equal(expected: priorSize - 1, actual: pile.Count);
                }
            }

            //[Fact]
            //public void LastDrawCallsOnLastRemoved()
            //{
            //    var seed = TestCard.Factory(1).ToArray();
            //    foreach (var pile in Piles(withPerms: PilePerms.CanDraw, items: seed))
            //    {
            //        var ev = Assert.Raises<EventArgs>(handler => pile.LastRemoveCalled += handler, handler => pile.LastRemoveCalled -= handler, () => pile.Draw());
            //        Assert.Same(expected: pile, actual: ev.Sender);
            //        Assert.Equal(expected: 0, actual: pile.Count);
            //    }
            //}

            [Fact]
            public void DrawingOnEmptyPileThrows()
            {
                foreach (var pile in Piles(withPerms: PilePerms.CanDraw, items: Enumerable.Empty<TestCard>()))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => pile.Draw());
                    Assert.Equal(expected: ErrorStrings.PileEmpty, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }
        }

        public sealed class DrawBottom
        {
            [Fact]
            public void ThrowsWhenNotDrawable()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.All ^ PilePerms.CanDrawBottom, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => pile.DrawBottom());
                    Assert.Equal(expected: ErrorStrings.NoDraw, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void DecreasesPileByOne()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanDrawBottom, items: seed))
                {
                    var priorSize = pile.Count;
                    var drawn = pile.DrawBottom();

                    Assert.NotNull(drawn);
                    Assert.Equal(expected: 20, actual: drawn.Id);
                    Assert.Equal(expected: priorSize - 1, actual: pile.Count);
                }
            }

            //[Fact]
            //public void LastDrawCallsOnLastRemoved()
            //{
            //    var seed = TestCard.Factory(1).ToArray();
            //    var pile = new TestPile(withPerms: PilePerms.CanDrawBottom, items: seed);
            //    {
            //        var ev = Assert.Raises<EventArgs>(
            //            handler => pile.LastRemoveCalled += handler,
            //            handler => pile.LastRemoveCalled -= handler,
            //            () => pile.DrawBottom());

            //        Assert.Same(expected: pile, actual: ev.Sender);
            //        Assert.Equal(expected: 0, actual: pile.Count);
            //    }
            //}

            [Fact]
            public void ThrowsOnEmptyPile()
            {
                foreach (var pile in Piles(withPerms: PilePerms.CanDrawBottom, items: Enumerable.Empty<TestCard>()))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => pile.DrawBottom());
                    Assert.Equal(expected: ErrorStrings.PileEmpty, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }
        }

        public sealed class InsertAt
        {
            [Fact]
            public void ThrowsWhenNotInsertable()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.All ^ PilePerms.CanInsert, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => pile.InsertAt(item: new TestCard(id: 2), index: 15));
                    Assert.Equal(expected: ErrorStrings.NoInsert, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ThrowsOnNullCard()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanInsert, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<ArgumentNullException>(() => pile.InsertAt(item: null, index: 10));
                    Assert.Equal(expected: "item", actual: ex.ParamName);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ThrowsNegativeIndex()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanInsert, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.InsertAt(item: new TestCard(id: 2), index: -1));
                    Assert.StartsWith(expectedStartString: ErrorStrings.InsertionNegative, actualString: ex.Message);
                    Assert.Equal(expected: "index", actual: ex.ParamName);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ThrowsTooHighIndex()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanInsert, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.InsertAt(new TestCard(id: 2), index: pile.Count + 1));
                    Assert.StartsWith(expectedStartString: ErrorStrings.InsertionTooHigh, actualString: ex.Message);
                    Assert.Equal(expected: "index", actual: ex.ParamName);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void IncreasesPileByOne()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanInsert, items: seed))
                {
                    var priorSize = pile.Count;
                    var newcard = new TestCard(id: 1);
                    pile.InsertAt(item: newcard, index: 10);

                    Assert.Equal(expected: priorSize + 1, actual: pile.Count);
                }
            }
        }

        public sealed class Mill
        {
            [Fact]
            public void ThrowsWhenSourceNotDrawableOrBrowsable()
            {
                var sourceSeed = TestCard.Factory(20).ToArray();
                foreach (var source in Piles(withPerms: PilePerms.All ^ (PilePerms.CanBrowse | PilePerms.CanDraw), items: sourceSeed))
                foreach (var target in Piles(withPerms: PilePerms.CanPut, items: Enumerable.Empty<TestCard>()))
                {
                    var sourceSize = source.Count;
                    var targetSize = target.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => source.Mill(target));
                    Assert.Equal(expected: ErrorStrings.NoDraw, actual: ex.Message);
                    Assert.Equal(expected: sourceSize, actual: source.Count);
                    Assert.Equal(expected: targetSize, actual: target.Count);
                }
            }

            [Fact]
            public void ThrowsWhenTargetNull()
            {
                var sourceSeed = TestCard.Factory(20).ToArray();
                foreach (var source in Piles(withPerms: PilePerms.CanDraw, items: sourceSeed))
                {
                    var sourceSize = source.Count;

                    var ex = Assert.Throws<ArgumentNullException>(() => source.Mill(targetPile: null));
                    Assert.Equal(expected: "targetPile", actual: ex.ParamName);
                    Assert.Equal(expected: sourceSize, actual: source.Count);
                }
            }

            [Fact]
            public void ThrowsWhenTargetNotPuttable()
            {
                var sourceSeed = TestCard.Factory(20).ToArray();
                foreach (var source in Piles(withPerms: PilePerms.CanDraw, items: sourceSeed))
                foreach (var target in Piles(withPerms: PilePerms.All ^ PilePerms.CanPut, items: Enumerable.Empty<TestCard>()))
                {
                    var sourceSize = source.Count;
                    var targetSize = target.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => source.Mill(target));
                    Assert.Equal(expected: ErrorStrings.NoPutTarget, actual: ex.Message);
                    Assert.Equal(expected: sourceSize, actual: source.Count);
                    Assert.Equal(expected: targetSize, actual: target.Count);
                }
            }

            [Fact]
            public void ThrowsWhenTargetIsSourceInstance()
            {
                var sourceSeed = TestCard.Factory(20).ToArray();
                foreach (var source in Piles(withPerms: PilePerms.CanDraw | PilePerms.CanPut, items: sourceSeed))
                {
                    var sourceSize = source.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => source.Mill(source));
                    Assert.Equal(expected: ErrorStrings.MillTargetSamePile, actual: ex.Message);
                    Assert.Equal(expected: sourceSize, actual: source.Count);
                }
            }

            [Fact]
            public void ThrowsOnEmptyPile()
            {
                foreach (var source in Piles(withPerms: PilePerms.CanDraw, items: Enumerable.Empty<TestCard>()))
                foreach (var target in Piles(withPerms: PilePerms.CanPut, items: Enumerable.Empty<TestCard>()))
                {
                    var sourceSize = source.Count;
                    var targetSize = target.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => source.Mill(target));
                    Assert.Equal(expected: ErrorStrings.PileEmpty, actual: ex.Message);
                    Assert.Equal(expected: sourceSize, actual: source.Count);
                    Assert.Equal(expected: targetSize, actual: target.Count);
                }
            }

            //[Fact]
            //public void DecreasesSourseSizeIncreasesTargetSizeByOne()
            //{
            //    var sourceSeed = TestCard.Factory(1).ToArray();
            //    foreach (var source in Piles(withPerms: PilePerms.CanDraw | PilePerms.CanPeek, items: sourceSeed))
            //    foreach (var target in Piles(withPerms: PilePerms.CanPut | PilePerms.CanBrowse, items: Enumerable.Empty<TestCard>()))
            //    {
            //        var sourceSize = source.Count;
            //        var targetSize = target.Count;
            //        var topcard = source.PeekAt(0);
            //        int firstCall = 0;
            //        bool putCalled = false;
            //        bool lastRmCalled = false;

            //        source.LastRemoveCalled += (s, e) =>
            //        {
            //            lastRmCalled = true;
            //            Interlocked.CompareExchange(ref firstCall, value: 1, comparand: 0);
            //        };
            //        target.PutCalled += (s, e) =>
            //        {
            //            putCalled = true;
            //            Interlocked.CompareExchange(ref firstCall, value: 2, comparand: 0);
            //        };

            //        source.Mill(target);
            //        Assert.True(lastRmCalled);
            //        Assert.True(putCalled);
            //        Assert.Equal(expected: 1, actual: firstCall);
            //        Assert.Equal(expected: sourceSize - 1, actual: source.Count);
            //        Assert.Equal(expected: targetSize + 1, actual: target.Count);
            //        Assert.Same(expected: topcard, actual: target.PeekAt(0));
            //    }
            //}
        }

        public sealed class PeekAt
        {
            [Fact]
            public void ThrowsWhenNotBrowsableOrPeekable()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.All ^ (PilePerms.CanBrowse | PilePerms.CanPeek), items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => pile.PeekAt(index: 5));
                    Assert.Equal(expected: ErrorStrings.NoBrowseOrPeek, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ThrowsNegativeIndex()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanPeek, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.PeekAt(index: -1));
                    Assert.StartsWith(expectedStartString: ErrorStrings.PeekAmountNegative, actualString: ex.Message);
                    Assert.Equal(expected: "index", actual: ex.ParamName);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ThrowsTooHighIndex()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanPeek, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.PeekAt(index: pile.Count + 1));
                    Assert.StartsWith(expectedStartString: ErrorStrings.PeekAmountTooHigh, actualString: ex.Message);
                    Assert.Equal(expected: "index", actual: ex.ParamName);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void DoesNotChangePileSize()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanPeek, items: seed))
                {
                    var priorSize = pile.Count;

                    var peeked = pile.PeekAt(index: 5);

                    Assert.NotNull(peeked);
                    Assert.Equal(expected: 6, actual: peeked.Id);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ReturnsNullOnEmptyPile()
            {
                foreach (var pile in Piles(withPerms: PilePerms.CanBrowse, items: Enumerable.Empty<TestCard>()))
                {
                    var c = pile.PeekAt(0);

                    Assert.Equal(expected: 0, actual: pile.Count);
                    Assert.Null(c);
                }
            }
        }

        public sealed class PeekTop
        {
            [Fact]
            public void ThrowsWhenNotBrowsableOrPeekable()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.All ^ (PilePerms.CanBrowse | PilePerms.CanPeek), items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => pile.PeekTop(amount: 5));
                    Assert.Equal(expected: ErrorStrings.NoBrowseOrPeek, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ThrowsNegativeIndex()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanPeek, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.PeekTop(amount: -1));
                    Assert.StartsWith(expectedStartString: ErrorStrings.PeekAmountNegative, actualString: ex.Message);
                    Assert.Equal(expected: "amount", actual: ex.ParamName);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ThrowsTooHighIndex()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanPeek, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.PeekTop(amount: pile.Count + 1));
                    Assert.StartsWith(expectedStartString: ErrorStrings.PeekAmountTooHigh, actualString: ex.Message);
                    Assert.Equal(expected: "amount", actual: ex.ParamName);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void DoesNotChangePileSize()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanPeek, items: seed))
                {
                    var priorSize = pile.Count;
                    var expectedSeq = new[] { 1, 2, 3 };

                    var peeked = pile.PeekTop(amount: 3);

                    Assert.False(peeked.IsDefault);
                    Assert.Equal(expected: expectedSeq, actual: peeked.Select(c => c.Id));
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
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
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.All ^ PilePerms.CanPut, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => pile.Put(item: new TestCard(id: 2)));
                    Assert.Equal(expected: ErrorStrings.NoPut, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ThrowsOnNullCard()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanPut, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<ArgumentNullException>(() => pile.Put(item: null));
                    Assert.Equal(expected: "item", actual: ex.ParamName);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            //[Fact]
            //public void CallsOnPut()
            //{
            //    foreach (var pile in Piles(withPerms: PilePerms.CanPut, items: Enumerable.Empty<TestCard>()))
            //    {
            //        var priorSize = pile.Count;
            //        var newcard = new TestCard(id: 1);

            //        var ev = Assert.Raises<PutEventArgs>(handler => pile.PutCalled += handler, handler => pile.PutCalled -= handler, () => pile.Put(item: newcard));
            //        Assert.Same(expected: pile, actual: ev.Sender);
            //        Assert.NotNull(ev.Arguments.Card);
            //        Assert.Same(expected: newcard, actual: ev.Arguments.Card);
            //        Assert.Equal(expected: priorSize + 1, actual: pile.Count);
            //    }
            //}
        }

        public sealed class PutBottom
        {
            [Fact]
            public void ThrowsWhenNotPuttableBottom()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.All ^ PilePerms.CanPutBottom, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => pile.PutBottom(item: new TestCard(id: 2)));
                    Assert.Equal(expected: ErrorStrings.NoPutBottom, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ThrowsOnNullCard()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanPutBottom, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<ArgumentNullException>(() => pile.PutBottom(item: null));
                    Assert.Equal(expected: "item", actual: ex.ParamName);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void IncreasesPileByOne()
            {
                foreach (var pile in Piles(withPerms: PilePerms.CanPutBottom, items: Enumerable.Empty<TestCard>()))
                {
                    var priorSize = pile.Count;
                    var newcard = new TestCard(id: 1);
                    pile.PutBottom(item: newcard);

                    Assert.Equal(expected: priorSize + 1, actual: pile.Count);
                }
            }
        }

        public sealed class Shuffle
        {
            [Fact]
            public void ThrowsWhenNotShufflable()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.All ^ PilePerms.CanShuffle, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => pile.Shuffle(shuffleFunc: c => c.Reverse()));
                    Assert.Equal(expected: ErrorStrings.NoShuffle, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ThrowsOnNullFunc()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanShuffle, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<ArgumentNullException>(() => pile.Shuffle(shuffleFunc: null));
                    Assert.Equal(expected: "shuffleFunc", actual: ex.ParamName);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ThrowsOnNullFuncReturn()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanShuffle, items: seed))
                {
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
            }

            [Fact]
            public void ShuffleFuncDoesNotGiveDefaultArray()
            {
                const int added = 10;

                foreach (var pile in Piles(withPerms: PilePerms.CanShuffle, items: Enumerable.Empty<TestCard>()))
                {
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
        }

        public sealed class TakeAt
        {
            [Fact]
            public void ThrowsWhenNotTakable()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.All ^ PilePerms.CanTake, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<InvalidOperationException>(() => pile.TakeAt(index: 14));
                    Assert.Equal(expected: ErrorStrings.NoTake, actual: ex.Message);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ThrowsNegativeIndex()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanTake, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.TakeAt(index: -1));
                    Assert.StartsWith(expectedStartString: ErrorStrings.RetrievalNegative, actualString: ex.Message);
                    Assert.Equal(expected: "index", actual: ex.ParamName);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void ThrowsTooHighIndex()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanTake, items: seed))
                {
                    var priorSize = pile.Count;

                    var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.TakeAt(index: pile.Count));
                    Assert.StartsWith(expectedStartString: ErrorStrings.RetrievalTooHighP, actualString: ex.Message);
                    Assert.Equal(expected: "index", actual: ex.ParamName);
                    Assert.Equal(expected: priorSize, actual: pile.Count);
                }
            }

            [Fact]
            public void DecreasesPileByOne()
            {
                var seed = TestCard.Factory(20).ToArray();
                foreach (var pile in Piles(withPerms: PilePerms.CanTake, items: seed))
                {
                    var priorSize = pile.Count;
                    var taken = pile.TakeAt(10);

                    Assert.Equal(expected: priorSize - 1, actual: pile.Count);
                }
            }

            //[Fact]
            //public void LastTakeCallsOnLastRemoved()
            //{
            //    var seed = TestCard.Factory(1).ToArray();
            //    foreach (var pile in Piles(withPerms: PilePerms.CanTake, items: seed))
            //    {
            //        var ev = Assert.Raises<EventArgs>(handler => pile.LastRemoveCalled += handler, handler => pile.LastRemoveCalled -= handler, () => pile.TakeAt(0));
            //        Assert.Same(expected: pile, actual: ev.Sender);
            //        Assert.Equal(expected: 0, actual: pile.Count);
            //    }
            //}
        }
    }
}
