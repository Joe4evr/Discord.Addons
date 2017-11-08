Games
=====

Implementing the game logic is probably one of the hardest things,
but it's useful to at least acquaint yourself with the shape
of this type after you have made your `Player` class.
```cs
public abstract class GameBase<TPlayer>
    where TPlayer : Player
{
    protected GameBase(IMessageChannel channel, IEnumerable<TPlayer> players);

    protected IMessageChannel Channel { get; }

    protected CircularLinkedList<TPlayer> Players { get; }

    public Task<IEnumerable<IDMChannel>> PlayerChannels() { get; }

    public Node<TPlayer> TurnPlayer { get; protected set; }

    protected bool IsTurnPlayerLastPlayer();

    public abstract string GetGameState();

    public abstract Task SetupGame();

    public abstract Task StartGame();

    public abstract Task NextTurn();

    public virtual async Task EndGame(string endmsg);
}
```

This type is abstract, which means it *must* be inherited before it can be used.
Any `abstract` members must also be overridden because the behavior is implementation-specific.
You'll also have to declare your `Player` type when you inherit the base class.
Continuing on with our card game example:
```cs
public class CardGame : GameBase<CardPlayer> // Any player in a 'CardGame' is of type 'CardPlayer'
{
    // Again, you need to call the base constructor
    public CardGame(IMessageChannel channel, IEnumerable<CardPlayer> players)
        : base(channel, players)
    {
    }

    // Example property that's needed for the game
    private Stack<Card> Deck { get; set; }

    public override Task SetupGame()
    {
        // Use this method to perform pre-game setup, like
        // shuffling the Deck if you have a method for that at hand

        // If you're not 'await'ing anything, you can avoid
        // warnings by using the following:
        return Task.CompletedTask;
    }

    public override async Task StartGame()
    {
        // This method represents the 'real' start of the game,
        // for example: dealing out cards to all players
        for (int i = 0; i < 5; i++)
        {
            foreach (var p in Players)
            {
                p.AddCard(Deck.Pop());
            }
        }

        foreach (var p in Players)
        {
            await Task.Delay(1000);
            // Ideally, you would have a '.ToString()' override on the 'Card' type for this
            await p.SendMessageAsync($"Your hand:\n{String.Join("\n", p.Hand)}");
        }
    }

    public override Task NextTurn()
    {
        // This is where you put the logic that happens
        // at the start of every turn
        _turn++;
        State = GameState.StartOfTurn;

        // Use 'TurnPlayer.Next' and assign it as the new TurnPlayer
        // The list will automatically cycle through once it reaches the end

        // On the other hand, if you would like to stop the game
        // automatically once everyone has had a turn (or a set number of turns),
        // you can use this method from the base class to check
        if (!IsTurnPlayerLastPlayer())
        {
            TurnPlayer = TurnPlayer.Next;

            // Use 'TurnPlayer.Value' to get the
            // actual instance value of the player
            TurnPlayer.Value.AddCard(Deck.Pop());
        }

    }

    public override string GetGameState()
    {
        // This method should provide a summary of the state that the game is in

        var sb = new StringBuilder($"State of the board at turn {_turn}:\n")
            .AppendLine($"Turn state is {State.ToString()}.")
            .AppendLine($"There are {Deck.Count} cards in the deck.")
            .AppendLine($"It is {TurnPlayer.Value.User.Username}'s turn.");

        foreach (var p in Players)
        {
            sb.AppendLine($"Player {p.User.Username} has {p.Hand.Count} cards in hand.");
        }

        return sb.ToString();
    }

    // Many more methods

    // Example items for keeping track of the game state
    private int _turn = 0;
    internal GameState State { get; private set; } = GameState.Setup;
}
// Make an enum like this so you can use it in
// a custom precondition for your commands (see page 7).
internal enum GameState
{
    Setup,
    StartOfTurn,
    //....
}
```

The final method from the base class you need to know is `EndGame()`.
It's not required to override this, but if you do, you ***must*** call
`base.EndGame()` in order to make the game instance eligible for garbage collection.

[<- Part 2 - Players](2-Players.md) - Games - [Part 4 - Services ->](4-Services.md)
