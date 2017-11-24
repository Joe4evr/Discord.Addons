Services
========

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

    public MpGameService(Func<LogMessage, Task> logger = null);

    public bool OpenNewGame(IMessageChannel channel);

    public bool AddUser(IMessageChannel channel, IUser user);

    public bool RemoveUser(IMessageChannel channel, IUser user);

    public bool CancelGame(IMessageChannel channel);

    public bool TryAddNewGame(IMessageChannel channel, TGame game);

    public bool TryUpdateOpenToJoin(IMessageChannel channel, bool newValue, bool comparisonValue);

    public Task<TGame> GetGameFromChannelAsync(IMessageChannel channel);

    public IReadOnlyCollection<IUser> GetJoinedUsers(IMessageChannel channel);

    public bool IsOpenToJoin(IMessageChannel channel);
}
```

While making your own is technically optional, it's recommended to at least make
an empty class that derives from it, so that adding persistent data later on
won't involve a lot more trouble than necessary.
```cs
public sealed class CardGameService : MpGameService<CardGame, CardPlayer>
{
    // It's generally advised to store your data in some kind of
    // 'Dictionary<ulong, T>' where the key is the channel/guild/user ID
    // and replace 'T' with whatever type you have your data in.
    public Dictionary<ulong, DataType> SomeDataDictionary { get; }
        = new Dictionary<ulong, DataType>();

    // Alternatively, you can use 'IMessageChannel' as a key
    // like the base class does, as long as you pass in the
    // base-provided 'MessageChannelComparer'.
    public Dictionary<IMessageChannel, DataType> SomeDataDictionary { get; }
        = new Dictionary<IMessageChannel, DataType>(MessageChannelComparer);
}
```

The constructor has an optional paramater to pass a logging method from
the caller to the base class. If you want to make use of the logger, then
add the same parameter to your constructor in the derived class.
```cs
public sealed class CardGameService : MpGameService<CardGame, CardPlayer>
{
    public CardGameService(Func<LogMessage, Task> logger = null)
        : base(logger)
    {
        // You can now log anything you like by invoking the 'Logger'
        // delegate on the base class you can make use of. I would personally
        // recommend having your own method as seen below as a wrapper.
        Log(LogSeverity.Info, "Creating CardGame Service");
    }

    intenal Task Log(LogSeverity severity, string msg)
    {
        return base.Logger(new LogMessage(severity, "CardGameService", msg));
    }

}
```

[<- Part 3 - Games](3-Games.md) - Services - [Part 5 - Modules ->](5-Modules.md)
