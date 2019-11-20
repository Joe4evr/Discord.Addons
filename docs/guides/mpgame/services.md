---
uid: Addons.MpGame.Services
title: Services
---
## Services

The service is a major component to know about, whether you make your own or not.

The lifetime of a Module is only as long as a command is running, similar to Controllers
in ASP.NET MVC. This means that you can't store persistent data *in*
a Module; you need a 'service' class that stores persistent data
outside of a module lifetime instead.

The `MpGameService` class looks like this:
```cs
public class MpGameService<TGame, TPlayer>
    where TGame   : GameBase<TPlayer>
    where TPlayer : Player
{
    protected static IEqualityComparer<IMessageChannel> MessageChannelComparer { get; }

    protected Func<LogMessage, Task> Logger { get; }

    public MpGameService(
        BaseSocketClient client,
        IMpGameServiceConfig? mpconfig = null,
        Func<LogMessage, Task>? logger = null);

    public bool OpenNewGame(ICommandContext context);

    public Task<bool> AddUser(IMessageChannel channel, IUser user);

    public Task<bool> RemoveUser(IMessageChannel channel, IUser user);

    public Task<bool> AddPlayer(TGame game, TPlayer player);

    public Task<bool> KickPlayer(TGame game, TPlayer player);

    public bool CancelGame(IMessageChannel channel);

    public bool TryAddNewGame(IMessageChannel channel, TGame game);

    public bool TryUpdateOpenToJoin(
        IMessageChannel channel, bool newValue, bool comparisonValue);

    public TGame GetGameFromChannel(IMessageChannel channel);

    public IReadOnlyCollection<IUser> GetJoinedUsers(IMessageChannel channel);

    public bool IsOpenToJoin(IMessageChannel channel);

    public MpGameData GetGameData(ICommandContext context);
}
```

While making your own is technically optional, it's recommended to at least make
an empty class that derives from it, so that adding persistent data later on
won't involve a lot more trouble than necessary.
```cs
public sealed class CardGameService : MpGameService<CardGame, CardPlayer>
{
    // It's generally advised to store your data in some kind of
    // 'ConcurrentDictionary<ulong, T>' where the key is the channel/guild/user ID
    // and replace 'T' with whatever type you have your data in.
    public ConcurrentDictionary<ulong, DataType> SomeDataDictionary { get; }
        = new ConcurrentDictionary<ulong, DataType>();

    // Alternatively, you can use 'IMessageChannel' as a key
    // like the base class does, as long as you pass in the
    // base-provided 'MessageChannelComparer'.
    public ConcurrentDictionary<IMessageChannel, DataType> SomeDataDictionary { get; }
        = new ConcurrentDictionary<IMessageChannel, DataType>(MessageChannelComparer);
}
```

The constructor for the service has to get either a 'DiscordSocketClient'
or a 'DiscordShardedClient' instance so that the service
can listen for the 'ChannelDestroyed' event.

There is an optional parameter to pass in an object to configure
parts of the base service, such as log strings and other switches
that may be added in the future.

There's also an optional paramater to pass a logging method from
the caller to the base class. If you want to make use of the logger, then
add the same parameter to your constructor in the derived class.
```cs
public sealed class CardGameService : MpGameService<CardGame, CardPlayer>
{
    public CardGameService(
        BaseSocketClient client,
        IMpGameServiceConfig? mpconfig = null,
        Func<LogMessage, Task>? logger = null)
        : base(client, mpconfig, logger)
    {
        // You can now log anything you like by invoking the 'Logger'
        // delegate on the base class you can make use of. I would personally
        // recommend having your own method as seen below as a wrapper.
        Log(LogSeverity.Debug, "Creating CardGame Service");
    }

    intenal Task Log(LogSeverity severity, string msg)
    {
        return base.Logger(new LogMessage(severity, "CardGameService", msg));
    }
}
```
