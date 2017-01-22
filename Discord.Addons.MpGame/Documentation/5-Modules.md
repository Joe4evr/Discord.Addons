Modules
=======

The Module is the final piece, and is what is needed to move a game forward.

The `MpGameModule` class looks like this:
```cs
public abstract class MpGameModuleBase<TService, TGame, TPlayer> : ModuleBase
    where TService : MpGameService<TGame, TPlayer>
    where TGame : GameBase<TPlayer>
    where TPlayer : Player
{
    protected MpGameModuleBase(TService gameService);

    protected TGame Game { get; }

    protected bool GameInProgress { get; }

    protected TService GameService { get; }

    protected bool OpenToJoin { get; }

    protected ImmutableHashSet<IUser> PlayerList { get; }

    public abstract Task OpenGameCmd();

    public abstract Task CancelGameCmd();

    public abstract Task JoinGameCmd();

    public abstract Task LeaveGameCmd();

    public abstract Task StartGameCmd();

    public abstract Task NextTurnCmd();

    public abstract Task GameStateCmd();

    public abstract Task EndGameCmd();

    [Command("resend")]
    public async Task ResendCmd();
}
```

There are 8 methods you can implement, corresponding to the
actions needed in most games. When you implement these, you decorate them with `[Command]`
so that the command system recognizes them. There may be methods you don't want or need to
implement, in which case you can omit the `[Command]` attribute so it can't be called.
Likewise, you'll most likely be adding *more* commands in order to control your game.

One command is predefined which will retry sending a DM
to a user after they have been notified to enable DMs.

With your own service class for persistent data, you should derive
from this class as follows:
```cs
public class CardGameModule : MpGameModuleBase<CardGameService, CardGame, CardPlayer>
{
    public CardGameModule(CardGameService service)
        : base(service)
    {
        //If you have any persistent data, load
        //the relevant instance from the dictionary
        //in your service class here
        //and store the result in a field
    }
}
```

If you didn't make a service class, you'd have to write this to use the default:
```cs
public class CardGameModule : MpGameModuleBase<MpGameService<CardGame, CardPlayer>, CardGame, CardPlayer>
{
    public CardGameModule(MpGameService<CardGame, CardPlayer> service)
        : base(service)
    {
        //See why it pays off to at least write an empty service class?
    }
}
```

Example implementations for the abstract methods are as follows:
```cs
[Command("opengame")]
public override async Task OpenGameCmd()
{
    //Make sure to check if a game is already going...
    if (GameInProgress)
    {
        await ReplyAsync("Another game already in progress.");
    }
    //...or if it's looking for players but hasn't yet started...
    else if (OpenToJoin)
    {
        await ReplyAsync("There is already a game open to join.");
    }
    //...before deciding what needs to be done.
    else
    {
        if (GameService.TryUpdateOpenToJoin(Context.Channel.Id, newValue: true, comparisonValue: false))
        {
            GameService.MakeNewPlayerList(Context.Channel.Id);
            await ReplyAsync("Opening for a game.");
        }
    }
}

[Command("join")]
public override async Task JoinGameCmd()
{
    if (GameInProgress)
    {
        await ReplyAsync("Cannot join a game already in progress.");
    }
    else if (!OpenToJoin)
    {
        await ReplyAsync("No game open to join.");
    }
    else
    {
        if (GameService.AddUser(Context.Channel.Id, Context.User))
        {
            await ReplyAsync($"**{Context.User.Username}** has joined.");
        }
    }
}

[Command("leave")]
public override async Task LeaveGameCmd()
{
    if (GameInProgress)
    {
        await ReplyAsync("Cannot leave a game already in progress.");
    }
    else if (!OpenToJoin)
    {
        await ReplyAsync("No game open to leave.");
    }
    else
    {
        if (GameService.RemoveUser(Context.Channel.Id, Context.User))
        {
            await ReplyAsync($"**{Context.User.Username}** has left.");
        }
    }
}

[Command("cancel")]
public override async Task CancelGameCmd()
{
    if (GameInProgress)
    {
        await ReplyAsync("Cannot cancel a game already in progress.");
    }
    else if (!OpenToJoin)
    {
        await ReplyAsync("No game open to cancel.");
    }
    else
    {
        if (GameService.TryUpdateOpenToJoin(Context.Channel.Id, newValue: false, comparisonValue: true))
        {
            PlayerList.Clear();
            await ReplyAsync("Game was canceled.");
        }
    }
}

[Command("start")]
public override async Task StartGameCmd()
{
    if (GameInProgress)
    {
        await ReplyAsync("Another game already in progress.");
    }
    else if (!OpenToJoin)
    {
        await ReplyAsync("No game has been opened at this time.");
    }
    //Example value if a game has a minimum player requirement
    else if (PlayerList.Count < 4)
    {
        await ReplyAsync("Not enough players have joined.");
    }
    else
    {
        if (GameService.TryUpdateOpenToJoin(Context.Channel.Id, newValue: false, comparisonValue: true))
        {
            //Tip: Shuffle the players before projecting them like this
            var players = PlayerList.Select(u => new Player(u, Context.Channel));

            var game = new ExampleGame(Context.Channel, players);
            if (GameService.TryAddNewGame(Context.Channel.Id, game))
            {
                await game.SetupGame();
                await game.StartGame();
            }
        }
    }
}

[Command("turn")]
public override Task NextTurnCmd()
    => GameInProgress ? Game.NextTurn() : ReplyAsync("No game in progress.");

[Command("state")]
public override Task GameStateCmd()
   => GameInProgress ? ReplyAsync(Game.GetGameState()) : ReplyAsync("No game in progress.");

[Command("end")] //Limit this command to only be used by moderators to prevent abuse
public override Task EndGameCmd()
    => !GameInProgress ? ReplyAsync("No game in progress to end.") : Game.EndGame("Game ended early by moderator.");
```

[<- Part 4 - Services](4-Services.md) - Modules - [Part 6 - Final step ->](6-FinalStep.md)
