﻿using System;
using System.Collections.Generic;
using System.Linq;
using Discord.Addons.MpGame.Collections;
using Xunit;

namespace MpGame.Tests.CollectionTests
{
    public class Pile2Tests
    {
        protected static IEnumerable<object[]> TheoryPiles(PilePerms withPerms, int num)
        {
            var seed = TestCard.Factory(num).ToArray();
            yield return new object[] { new WrappingTestPile(withPerms, seed) };
        }

        public sealed class GetWrapperAt : Pile2Tests
        {
            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.All, 20)]
            public void ThrowsNegativeIndex(WrappingTestPile pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.FlipCardAt(index: -1));
                Assert.StartsWith(expectedStartString: PileErrorStrings.RetrievalNegative, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.All, 20)]
            public void ThrowsTooHighIndex(WrappingTestPile pile)
            {
                var priorSize = pile.Count;

                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => pile.FlipCardAt(index: pile.Count + 1));
                Assert.StartsWith(expectedStartString: PileErrorStrings.RetrievalTooHighP, actualString: ex.Message);
                Assert.Equal(expected: "index", actual: ex.ParamName);
                Assert.Equal(expected: priorSize, actual: pile.Count);
            }

            [Theory]
            [MemberData(nameof(TheoryPiles), PilePerms.All, 20)]
            public void DoesNotChangePileSize(WrappingTestPile pile)
            {
                var priorSize = pile.Count;

                pile.FlipCardAt(index: 4);
                var expectedSeq = new[]
                {
                     1, 2, 3, 4, -1, 6, 7, 8, 9,10,
                    11,12,13,14,15,16,17,18,19,20
                };

                Assert.Equal(expected: priorSize, actual: pile.Count);
                Assert.Same(expected: FaceDownCard.Instance, actual: pile.GetWrapper(index: 4).Unwrap(false));
                Assert.Equal(expected: expectedSeq, actual: pile.Browse().Select(c => c.Id));
            }

            [Fact]
            public void NodeResetIsReflected()
            {
                var sourcePile1 = new WrappingTestPile(withPerms: PilePerms.All, items: TestCard.Factory(10));
                var destPile1 = new WrappingTestPile(withPerms: PilePerms.All, items: Enumerable.Empty<TestCard>());

                Assert.Equal(expected: "Card is face-up", actual: sourcePile1.GetStatusAt(0));
                sourcePile1.FlipCardAt(0);
                Assert.Equal(expected: "Card is face-down", actual: sourcePile1.GetStatusAt(0));
                sourcePile1.Mill(destPile1);
                Assert.Equal(expected: "Card is face-up", actual: destPile1.GetStatusAt(0));


                var sourcePile2 = new FacedownWrappingTestPile(withPerms: PilePerms.All, items: TestCard.Factory(10));
                var destPile2 = new FacedownWrappingTestPile(withPerms: PilePerms.All, items: Enumerable.Empty<TestCard>());

                Assert.Equal(expected: "Card is face-down", actual: sourcePile2.GetStatusAt(0));
                sourcePile2.FlipCardAt(0);
                Assert.Equal(expected: "Card is face-up", actual: sourcePile2.GetStatusAt(0));
                sourcePile2.Mill(destPile2);
                Assert.Equal(expected: "Card is face-down", actual: destPile2.GetStatusAt(0));
            }
        }
    }
}
