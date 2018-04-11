using System;
using System.Collections.Generic;
using System.Linq;
using Discord.Addons.MpGame.Collections;
using Xunit;

namespace MpGame.Tests.CollectionTests
{
    public sealed class HandTests
    {
        private static IEnumerable<TestCard> CardFactory(int amount)
            => Enumerable.Range(1, amount).Select(i => new TestCard { Id = i });

        [Fact]
        public void SeedingCtorThrowsOnNullSequence()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new Hand<TestCard>(cards: null));
            Assert.Equal(expected: "cards", actual: ex.ParamName);
        }

        [Fact]
        public void EmptyHandIsNotNull()
        {
            var hand = new Hand<TestCard>();
            Assert.Equal(expected: 0, actual: hand.Count);
            Assert.NotNull(hand.Cards);
            Assert.Empty(hand.Cards);
        }

        [Fact]
        public void AddThrowsOnNullCard()
        {
            var hand = new Hand<TestCard>(CardFactory(5));
            var ex = Assert.Throws<ArgumentNullException>(() => hand.Add(card: null));
            Assert.Equal(expected: "card", actual: ex.ParamName);
        }

        [Fact]
        public void TakeThrowsNegativeIndex()
        {
            var hand = new Hand<TestCard>(CardFactory(5));
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => hand.TakeAt(index: - 1));
            Assert.Equal(expected: ErrorStrings.RetrievalNegative, actual: ex.Message);
            Assert.Equal(expected: "index", actual: ex.ParamName);
        }

        [Fact]
        public void TakeThrowsTooHighIndex()
        {
            var hand = new Hand<TestCard>(CardFactory(5));
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => hand.TakeAt(index: hand.Count + 1));
            Assert.Equal(expected: ErrorStrings.RetrievalTooHighH, actual: ex.Message);
            Assert.Equal(expected: "index", actual: ex.ParamName);
        }

        [Fact]
        public void TakePredicateThrowsOnNullPredicate()
        {
            var hand = new Hand<TestCard>(CardFactory(5));
            var ex = Assert.Throws<ArgumentNullException>(() => hand.TakeFirstOrDefault(predicate: null));
            Assert.Equal(expected: "predicate", actual: ex.ParamName);
        }

        [Fact]
        public void OrderThrowsOnNullFunc()
        {
            var hand = new Hand<TestCard>(CardFactory(5));
            var ex = Assert.Throws<ArgumentNullException>(() => hand.Order(orderFunc: null));
            Assert.Equal(expected: "orderFunc", actual: ex.ParamName);
        }

        [Fact]
        public void OrderThrowsOnNullFuncReturn()
        {
            var hand = new Hand<TestCard>(CardFactory(5));
            var ex = Assert.Throws<InvalidOperationException>(() => hand.Order(orderFunc: cards => null));
            Assert.Equal(expected: ErrorStrings.NullSequence, actual: ex.Message);
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
}
