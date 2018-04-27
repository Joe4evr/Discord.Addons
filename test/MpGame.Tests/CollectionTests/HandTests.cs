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
                var seed = TestCard.Factory(5).ToArray();
                seed[1] = null;
                seed[3] = null;
                var nulls = seed.Count(c => c == null);
                var hand = new Hand<TestCard>(seed);

                Assert.Equal(expected: seed.Length - nulls, actual: hand.Count);
                Assert.All(hand.Browse(), c => Assert.NotNull(c));
            }
        }

        public sealed class Add
        {
            [Fact]
            public void ThrowsOnNullCard()
            {
                var hand = new Hand<TestCard>(TestCard.Factory(5));
                var ex = Assert.Throws<ArgumentNullException>(() => hand.Add(card: null));
                Assert.Equal(expected: "card", actual: ex.ParamName);
            }

            [Fact]
            public void IncreasesPileByOne()
            {
                var hand = new Hand<TestCard>(TestCard.Factory(5));
                var priorSize = hand.Count;
                var newcard = new TestCard(id: 1);
                hand.Add(newcard);
                Assert.Equal(expected: priorSize + 1, actual: hand.Count);
            }
        }

        public sealed class Browse
        {
            [Fact]
            public void DoesNotChangeHandSize()
            {
                var hand = new Hand<TestCard>(TestCard.Factory(5));
                var cards = hand.Browse();

                Assert.False(cards.IsDefault);
                Assert.NotEmpty(cards);
                Assert.Equal(expected: cards.Length, actual: hand.Count);
            }

            [Fact]
            public void EmptyHandIsNotNull()
            {
                var hand = new Hand<TestCard>();
                var cards = hand.Browse();

                Assert.Equal(expected: 0, actual: hand.Count);
                Assert.False(cards.IsDefault);
                Assert.True(cards.IsEmpty);
            }
        }

        public sealed class Clear
        {
            [Fact]
            public void EmptiesHand()
            {
                var hand = new Hand<TestCard>(TestCard.Factory(5));
                var priorSize = hand.Count;
                var cleared = hand.Clear();

                Assert.False(cleared.IsDefault);
                Assert.Equal(expected: 0, actual: hand.Count);
                Assert.Equal(expected: priorSize, actual: cleared.Length);
            }
        }

        public sealed class Order
        {
            [Fact]
            public void ThrowsOnNullFunc()
            {
                var hand = new Hand<TestCard>(TestCard.Factory(5));
                var priorSize = hand.Count;

                var ex = Assert.Throws<ArgumentNullException>(() => hand.Order(orderFunc: null));
                Assert.Equal(expected: "orderFunc", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: hand.Count);
            }

            [Fact]
            public void ThrowsOnNullFuncReturn()
            {
                var hand = new Hand<TestCard>(TestCard.Factory(5));
                var priorSize = hand.Count;

                var ex = Assert.Throws<InvalidOperationException>(() => hand.Order(orderFunc: cards => null));
                Assert.Equal(expected: ErrorStrings.NewSequenceNull, actual: ex.Message);
                Assert.Equal(expected: priorSize, actual: hand.Count);
            }

            [Fact]
            public void FuncDoesNotGiveDefaultArray()
            {
                const int init = 0;
                const int added = 5;

                var hand = new Hand<TestCard>();
                bool funcCalled = false;
                hand.Order(orderFunc: cards =>
                {
                    funcCalled = true;
                    Assert.False(cards.IsDefault);
                    return cards.Concat(TestCard.Factory(added));
                });

                Assert.True(funcCalled);
                Assert.Equal(expected: init + added, actual: hand.Count);
            }
        }

        public sealed class TakeAt
        {
            [Fact]
            public void ThrowsNegativeIndex()
            {
                var hand = new Hand<TestCard>(TestCard.Factory(5));
                var priorSize = hand.Count;
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => hand.TakeAt(index: -1));
                Assert.StartsWith(expectedStartString: ErrorStrings.RetrievalNegative, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: hand.Count);
            }

            [Fact]
            public void ThrowsTooHighIndex()
            {
                var hand = new Hand<TestCard>(TestCard.Factory(5));
                var priorSize = hand.Count;
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => hand.TakeAt(index: hand.Count + 1));
                Assert.StartsWith(expectedStartString: ErrorStrings.RetrievalTooHighH, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: hand.Count);
            }

            [Fact]
            public void DecreasesPileByOne()
            {
                var hand = new Hand<TestCard>(TestCard.Factory(5));
                var priorSize = hand.Count;
                var taken = hand.TakeAt(index: 3);
                Assert.NotNull(taken);
                Assert.Equal(expected: priorSize - 1, actual: hand.Count);
            }
        }

        public sealed class TakeFirstOrDefault
        {
            [Fact]
            public void PredicateThrowsOnNullPredicate()
            {
                var hand = new Hand<TestCard>(TestCard.Factory(5));
                var priorSize = hand.Count;

                var ex = Assert.Throws<ArgumentNullException>(() => hand.TakeFirstOrDefault(predicate: null));
                Assert.Equal(expected: "predicate", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: hand.Count);
            }

            [Fact]
            public void MatchingPredicateDecreasesPileByOne()
            {
                var hand = new Hand<TestCard>(TestCard.Factory(5));
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
                var hand = new Hand<TestCard>(TestCard.Factory(5));
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
    }
}
