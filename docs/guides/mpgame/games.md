---
uid: Addons.MpGame.Games
title: Games
---
## Games

Implementing the game logic is probably one of the hardest things,
but it's useful to at least acquaint yourself with the shape
of this type after you have made your `Player` class.
```cs
public abstract class GameBase<TPlayer>
    where TPlayer : Player
{
    protected GameBase(
        IMessageChannel channel,
        IEnumerable<TPlayer> players,
        bool setFirstPlayerImmediately = false);

    protected IMessageChannel Channel { get; }

    protected CircularLinkedList<TPlayer> Players { get; }

    public Task<IEnumerable<IDMChannel>> PlayerChannels() { get; }

    public Node<TPlayer> TurnPlayer { get; protected set; }

    protected bool IsTurnPlayerFirstPlayer();
    
    protected bool IsTurnPlayerLastPlayer();

    protected virtual Task OnPlayerAdded(TPlayer player);

    protected virtual Task OnPlayerKicked(TPlayer player);

    public abstract string GetGameState();

    public abstract Embed GetGameStateEmbed();

    public abstract Task SetupGame();

    public abstract Task StartGame();

    public abstract Task NextTurn();
    
    public Task EndGame(string endmsg);

    public virtual async Task OnGameEnd();
}
```

This type is abstract, which means it *must* be inherited before it can be used.
Any `abstract` members must also be overridden because the behavior is implementation-specific.
You'll also have to declare your `Player` type when you inherit the base class.
Continuing on with our card game example:
```cs
public class CardGame : GameBase<CardPlayer>, // Any player in a 'CardGame' is of type 'CardPlayer'.
    ISimpleStateProvider<CardGameState> // Implementing this interface is required for some of the built-in
                                        // preconditions (see section 7), but otherwise optional.
{
    // Example property that's needed for your game.
    private CardDeck Deck { get; }

    // Example items for keeping track of the game state:
    public CardGameState State { get; private set; } = CardGameState.Setup;
    internal int Turn { get; private set; } = 0;
    internal uint DeckSize => (uint)Deck.Count;

    // Again, you need to call the base constructor.
    public CardGame(
        IMessageChannel channel,
        IEnumerable<CardPlayer> players,
        IEnumerable<Card> cards)
        // Use the 'setFirstPlayerImmediately' flag to control
        // where or not to set the 'TurnPlayer' to the first player
        // or to an empty node. Default is 'false' (sets an empty node).
        : base(channel, players, setFirstPlayerImmediately: false)
    {
        Deck = new CardDeck(cards);
    }

    public override Task SetupGame()
    {
        // Use this method to perform pre-game setup, like
        // shuffling the Deck if you have a method for that at hand.

        // If you're not 'await'ing anything, you can avoid
        // warnings by using the following:
        return Task.CompletedTask;
    }

    public override async Task StartGame()
    {
        // This method represents the 'real' start of the game,
        // for example: dealing out cards to all players.
        for (int i = 0; i < 5; i++)
        {
            foreach (var p in Players)
            {
                p.AddCard(Deck.Draw());
            }
        }

        foreach (var p in Players)
        {
            await Task.Delay(1000);
            // Ideally, you would have a '.ToString()' override on the 'Card' type for this.
            await p.SendMessageAsync($"Your hand:\n{String.Join("\n", p.Hand)}");
        }
    }

    public override Task NextTurn()
    {
        // This is where you put the logic that happens
        // at the start of every turn.
        Turn++;
        State = CardGameState.StartOfTurn;

        // Use 'TurnPlayer.Next' and assign it as the new TurnPlayer.
        // The list will automatically cycle through once it reaches the end.
        TurnPlayer = TurnPlayer.Next;

        // On the other hand, if you would like to stop the game
        // automatically once everyone has had a turn (or a set number of turns),
        // you can use this method from the base class to check.
        if (!IsTurnPlayerLastPlayer())
        {
            // Use 'TurnPlayer.Value' to get the
            // actual instance value of the player.
            TurnPlayer.Value.AddCard(Deck.Draw());
        }
    }

    public override string GetGameState()
    {
        // This method should provide a summary of the state that the game is in.

        // Starting in v1.2, you can also choose to implement 'GetGameStateEmbed()' instead
        // if you want to get real fancy.
        // You do not need to implement *both* methods, just make sure that your
        // 'GameState' command doesn't call the one that remains unimplemented.

        var sb = new StringBuilder($"State of the board at turn {Turn}:\n")
            .AppendLine($"Turn state is {State.ToString()}.")
            .AppendLine($"There are {Deck.Count} cards in the deck.")
            .AppendLine($"It is {TurnPlayer.Value.User.Username}'s turn.");

        foreach (var p in Players)
        {
            sb.AppendLine($"Player {p.User.Username} has {p.Hand.Count} cards in hand.");
        }

        return sb.ToString();
    }

    // Many more methods...
}
// Make an enum like this so you can use it in
// a custom precondition for your commands (see section 7).
public enum CardGameState
{
    Setup,
    StartOfTurn,
    //....
}
```

