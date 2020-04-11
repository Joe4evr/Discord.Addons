using System;
using System.Collections.Generic;
using System.Linq;

namespace MpGame.Tests
{
    public sealed class TestCard : ITestCard
    {
        public int Id { get; private set; }
        public CardColor Color { get; private set; }

        private TestCard() { }
        public TestCard(int id)
            : this(id, (CardColor)(id % _colorCount))
        {
        }
        public TestCard(int id, CardColor color)
        {
            Id = id;
            Color = color;
        }


        private static readonly int _colorCount = Enum.GetValues(typeof(CardColor)).Length;
        internal static IEnumerable<TestCard> Factory(int amount, int start = 1)
            => Enumerable.Range(start, amount).Select(i => new TestCard { Id = i, Color = (CardColor)(i % _colorCount) });
    }

    internal sealed class FaceDownCard : ITestCard
    {
        public static FaceDownCard Instance { get; } = new FaceDownCard();
        private FaceDownCard() { }

        public CardColor Color => CardColor.None;
        public int Id          => -1;
    }

    public enum CardColor
    {
        None   = -1,
        Yellow = 0,
        Green  = 1,
        Red    = 2,
        Blue   = 3,
        White  = 4,
        Black  = 5
    }
}
