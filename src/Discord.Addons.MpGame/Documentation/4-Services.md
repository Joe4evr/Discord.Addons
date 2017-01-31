Services
========

The service is a major component to know about, whether you make your own or not.

Since Discord.Net beta2, the lifetime of Modules has changed from being
the same instance at all times to being new for every time a command is ran.
This necessitated some changes, namely that you can't store persistent data *in*
a Module anymore. Instead, you now need a 'service' class that stores persistent data
outside of a module lifetime.

The `MpGameService` class looks like this:
```cs
public class MpGameService<TGame, TPlayer>
    where TGame   : GameBase<TPlayer>
    where TPlayer : Player
{
    public IReadOnlyDictionary<IMessageChannel, TGame> GameList { get; }

    public IReadOnlyDictionary<IMessageChannel, bool> OpenToJoin { get; }

    public IReadOnlyDictionary<IMessageChannel, ImmutableHashSet<IUser>> PlayerList { get; }

    public bool MakeNewPlayerList(IMessageChannel channel);

    public bool AddUser(IMessageChannel channel, IUser user);

    public bool RemoveUser(IMessageChannel channel, IUser user);

    public bool TryAddNewGame(IMessageChannel channel, TGame game);

    public bool TryUpdateOpenToJoin(IMessageChannel channel, bool newValue, bool comparisonValue);
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

    // Alternatively, you can use 'IMessageChannel' as a key
    // like the base class does, as long as you pass in the
    // base-provided 'ChannelComparer'.
    public readonly Dictionary<ulong, DataType> SomeDataDictionary = new Dictionary<ulong, DataType>(ChannelComparer);
}
```

[<- Part 3 - Games](3-Games.md) - Services - [Part 5 - Modules ->](5-Modules.md)
