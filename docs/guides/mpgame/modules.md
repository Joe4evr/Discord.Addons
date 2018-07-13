---
uid: Addons.MpGame.Modules
title: Modules
---
## Modules

The Module is the final piece, and is what is needed to move a game forward.

The `MpGameModule` class looks like this:
```cs
public abstract class MpGameModuleBase<TService, TGame, TPlayer> : ModuleBase<SocketCommandContext>
    where TService : MpGameService<TGame, TPlayer>
    where TGame    : GameBase<TPlayer>
    where TPlayer  : Player
{
    protected MpGameModuleBase(TService gameService);

    protected TService GameService { get; }

    protected TGame Game { get; }

    protected TPlayer Player { get; }

    protected CurrentlyPlaying GameInProgress { get; }

    protected bool OpenToJoin { get; }

    protected IReadOnlyCollection<IUser> JoinedUsers { get; }

    protected virtual bool RegisterPlayerTypeReader { get; }

    public abstract Task OpenGameCmd();

    public abstract Task CancelGameCmd();

    public abstract Task JoinGameCmd();

    public abstract Task LeaveGameCmd();

    public abstract Task StartGameCmd();

    public abstract Task NextTurnCmd();

    public abstract Task GameStateCmd();

    public abstract Task EndGameCmd();

    public virtual async Task ResendCmd();
}

public enum CurrentlyPlaying
{
    None,
    ThisGame,
    DifferentGame
}
```

Other versions of this class with less generic parameters also exist,
so you don't *need* to supply all the type parameters. Consult
IntelliSense for the details.

`CurrentlyPlaying` is an enum that indicates if the channel already
has a game going on, and if it is the game type that this module handles
or another game type. You can use this to ensure that you won't get
two different games being played simultaniously in the same channel.

`RegisterPlayerTypeReader` is an overridable flag with which you can specify
to add a TypeReader for your `Player` type so that you can take in that type
as a command parameter directly. The default value of this flag is `true`.

There are 8 methods you can implement, corresponding to the
actions needed in most games. When you implement these, you decorate them with `[Command]`
so that the command system recognizes them. There may be methods you don't want or need to
implement, in which case you can omit the `[Command]` attribute so it can't be called.
Likewise, you'll most likely be adding *more* commands in order to control your game.

One command is predefined which will retry sending a DM
to a user after they have been notified to enable DMs.
If you want to make use of this command, you will need to override
it just to call the base method and add the `[Command]` attribute.

With your own service class and a data type for persistent data, you should derive
from this class as follows:
```cs
public class CardGameModule : MpGameModuleBase<CardGameService, CardGame, CardPlayer>
{
    public CardGameModule(CardGameService service)
        : base(service)
    {
    }

    protected override void BeforeExecute(CommandInfo command)
    {
        // If you choose to override this method, you *must* call the base version first.
        base.BeforeExecute(command);
        // If you have any persistent data of your own, load
        // the relevant instance from the dictionary
        // in your service class here and store
        // the result in a private field.
        GameService.SomeDataDictionary.TryGetValue(Context.Channel.Id, out _data);
    }
    private DataType _data;
}
```

While having an explicit service class will make it easier to expand in the future,
you *can* omit the type parameter to use the default if you have no other persistent
data to store for your game:
```cs
public class CardGameModule : MpGameModuleBase<CardGame, CardPlayer>
{
    public CardGameModule(MpGameService<CardGame, CardPlayer> service)
        : base(service)
    {
    }
}
```

Example implementations for the abstract methods can be found
[on the API documentation page](xref:Discord.Addons.MpGame.MpGameModuleBase`3).
An extensive example can be found as my implementation of
[Secret Hitler](https://github.com/Joe4evr/MechHisui/tree/master/src/MechHisui.SecretHitler).

### Inheriting a different ModuleBase
To aid in composability, starting in version 1.2.0, `MpGameModuleBase<>`
is decoupled from the `MpGameService<>`. So if you wish to combine MpGame
with a different ModuleBase (or inherit only `ModuleBase<T>` directly),
you can do that by
1. injecting the instance of the service into your module
2. overriding `BeforeExecute()` where you can call `GetGameData()`
to get the associated data for that context.
