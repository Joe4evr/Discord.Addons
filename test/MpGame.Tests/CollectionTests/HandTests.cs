using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Discord.Addons.MpGame.Collections;
using Xunit;

namespace MpGame.Tests.CollectionTests
{
    public static class HandTests
    {
        private static IEnumerable<TestCard> CardFactory(int amount)
            => Enumerable.Range(1, amount).Select(i => new TestCard { Id = i });

        public sealed class Ctor
        {
            [Fact]
            public void SeedingCtorThrowsOnNullSequence()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new Hand<TestCard>(cards: null));
                Assert.Equal(expected: "cards", actual: ex.ParamName);
            }

            [Fact]
            public void SeedingCtorFiltersOutNulls()
            {
                var seed = CardFactory(5).ToArray();
                seed[1] = null;
                seed[3] = null;
                var nulls = seed.Count(c => c == null);
                var hand = new Hand<TestCard>(seed);

                Assert.Equal(expected: seed.Length - nulls, actual: hand.Count);
                Assert.All(hand.Cards, c => Assert.NotNull(c));
            }
        }

        public sealed class Browse
        {
            [Fact]
            public void BrowsingDoesNotChangeHandSize()
            {
                var hand = new Hand<TestCard>(CardFactory(5));
                var cards = hand.Cards;
                Assert.NotNull(cards);
                Assert.NotEmpty(cards);
                Assert.Equal(expected: cards.Count, actual: hand.Count);
            }

            [Fact]
            public void EmptyHandIsNotNull()
            {
                var hand = new Hand<TestCard>();
                Assert.Equal(expected: 0, actual: hand.Count);
                Assert.NotNull(hand.Cards);
                Assert.Empty(hand.Cards);
            }
        }

        public sealed class Add
        {
            [Fact]
            public void AddThrowsOnNullCard()
            {
                var hand = new Hand<TestCard>(CardFactory(5));
                var ex = Assert.Throws<ArgumentNullException>(() => hand.Add(card: null));
                Assert.Equal(expected: "card", actual: ex.ParamName);
            }

            [Fact]
            public void AddIncreasesPileByOne()
            {
                var hand = new Hand<TestCard>(CardFactory(5));
                var priorSize = hand.Count;
                var newcard = new TestCard { Id = 1 };
                hand.Add(newcard);
                Assert.Equal(expected: priorSize + 1, actual: hand.Count);
            }
        }

        public sealed class Clear
        {
            [Fact]
            public void ClearingEmptiesHand()
            {
                var hand = new Hand<TestCard>(CardFactory(5));
                var priorSize = hand.Count;
                var cleared = hand.Clear();
                Assert.Equal(expected: 0, actual: hand.Count);
                Assert.Equal(expected: priorSize, actual: cleared.Count);
            }
        }

        public sealed class Order
        {
            [Fact]
            public void OrderThrowsOnNullFunc()
            {
                var hand = new Hand<TestCard>(CardFactory(5));
                var priorSize = hand.Count;
                var ex = Assert.Throws<ArgumentNullException>(() => hand.Order(orderFunc: null));
                Assert.Equal(expected: "orderFunc", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: hand.Count);
            }

            [Fact]
            public void OrderThrowsOnNullFuncReturn()
            {
                var hand = new Hand<TestCard>(CardFactory(5));
                var priorSize = hand.Count;
                var ex = Assert.Throws<InvalidOperationException>(() => hand.Order(orderFunc: cards => null));
                Assert.Equal(expected: ErrorStrings.NewSequenceNull, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: hand.Count);
            }

            [Fact]
            public void OrderFuncDoesNotGiveNullArgument()
            {
                const int init = 0;
                const int added = 5;

                var hand = new Hand<TestCard>();
                hand.Order(orderFunc: cards =>
                {
                    Assert.NotNull(cards);
                    return cards.Concat(CardFactory(added));
                });

                Assert.Equal(expected: init + added, actual: hand.Count);
            }
        }

        public sealed class TakeAt
        {
            [Fact]
            public void TakeThrowsNegativeIndex()
            {
                var hand = new Hand<TestCard>(CardFactory(5));
                var priorSize = hand.Count;
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => hand.TakeAt(index: -1));
                Assert.StartsWith(expectedStartString: ErrorStrings.RetrievalNegative, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: hand.Count);
            }

            [Fact]
            public void TakeThrowsTooHighIndex()
            {
                var hand = new Hand<TestCard>(CardFactory(5));
                var priorSize = hand.Count;
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => hand.TakeAt(index: hand.Count + 1));
                Assert.StartsWith(expectedStartString: ErrorStrings.RetrievalTooHighH, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: hand.Count);
            }

            [Fact]
            public void TakeDecreasesPileByOne()
            {
                var hand = new Hand<TestCard>(CardFactory(5));
                var priorSize = hand.Count;
                var taken = hand.TakeAt(index: 3);
                Assert.NotNull(taken);
                Assert.Equal(expected: priorSize - 1, actual: hand.Count);
            }
        }

        public sealed class TakeFirstOrDefault
        {
            [Fact]
            public void TakePredicateThrowsOnNullPredicate()
            {
                var hand = new Hand<TestCard>(CardFactory(5));
                var priorSize = hand.Count;

                var ex = Assert.Throws<ArgumentNullException>(() => hand.TakeFirstOrDefault(predicate: null));
                Assert.Equal(expected: "predicate", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: hand.Count);
            }

            [Fact]
            public void MatchingPredicateDecreasesPileByOne()
            {
                var hand = new Hand<TestCard>(CardFactory(5));
                var priorSize = hand.Count;
                bool funcCalled = false;
                var taken = hand.TakeFirstOrDefault(predicate: c =>
                {
                    funcCalled = true;
                    return c.Id == 3;
                });

                Assert.True(funcCalled);
                Assert.NotNull(taken);
                Assert.Equal(expected: priorSize - 1, actual: hand.Count);
            }

            [Fact]
            public void NonMatchingPredicateReturnsNull()
            {
                var hand = new Hand<TestCard>(CardFactory(5));
                var priorSize = hand.Count;
                bool funcCalled = false;
                var taken = hand.TakeFirstOrDefault(predicate: c =>
                {
                    funcCalled = true;
                    return c.Id == 10;
                });

                Assert.True(funcCalled);
                Assert.Null(taken);
                Assert.Equal(expected: priorSize, actual: hand.Count);
            }
        }

        //[Fact]
        //public void IDvISD()
        //{
        //    var d = CardFactory(10).ToImmutableDictionary(c => c.Id);
        //    var sd = CardFactory(10).ToImmutableSortedDictionary(c => c.Id, c => c);
        //}
    }
}
