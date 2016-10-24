Players
=======

Though optional, it helps to start thinking about the player type first.

The base `Player` class looks like this:
```cs
public class Player
{
    public Player(IUser user, IMessageChannel channel);
    public IUser User { get; }
    public async Task SendMessageAsync(string text);
}
```

There's no state held in the base class. So if you want to implement, for example a card game,
it would make sense that a player would need a property to contain the cards he or she has in their hand.
In order to do this, create a class that derives from `Player` and add such properties/methods.
```cs
public class CardPlayer : Player
{
    //It would make a lot of sense to keep a property
    //like this internal, or even private if you really want to
    internal IList<Card> Hand { get; } = new List<Card>();
    //You'll also have to provide your own 'Card' type for this

    //You need a constructor to call the base constructor
    public CardPlayer(IUser user, IMessageChannel channel)
        : base(user, channel)
    {
    }

    //And you'll want a method that adds a card to the player's hand
    public void AddCard(Card card) => Hand.Add(card);
}
```

[<- Part 1 - Intro](1-Intro.md) - Players - [Part 3 - Games ->](3-Games.md)
