namespace MpGame.Tests
{
    internal interface ITestCard
    {
        //bool IsFaceDown { get; set; }

        CardColor Color { get; }
        int Id { get; }
    }
}