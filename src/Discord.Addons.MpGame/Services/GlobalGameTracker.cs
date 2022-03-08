using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Discord.Addons.MpGame;

/// <summary>
///     Keeps track of *all* channels that are playing a game.
/// </summary>
internal sealed class GameTracker
{
#nullable disable warnings
    private static GameTracker _instance;
    /// <summary>
    ///     The singleton-instance of this class.
    /// </summary>
    /// <remarks>
    ///     This feels so dirty, but it's hard to think of a way that doesn't also leak the implementation to end-users.
    /// </remarks>
    public static GameTracker Instance
    {
        get
        {
            if (_instance is null)
                Interlocked.CompareExchange(ref _instance, new GameTracker(), null);

            return _instance;
        }
    }
#nullable restore

    private GameTracker() { }

    private readonly ConcurrentDictionary<ulong, string> _channelGames = new();

    private readonly ConcurrentDictionary<ulong, IMessageChannel> _dmList = new();

    /// <summary>
    ///     Determines whether the DM Channel tracker contains the specified key.
    /// </summary>
    public bool TryGetGameChannel(IDMChannel channel, [NotNullWhen(true)] out IMessageChannel? value)
        => _dmList.TryGetValue(channel.Id, out value);

    /// <summary>
    ///     Attempts to add the channel/channel pair to the DM Channel tracker.
    /// </summary>
    public bool TryAddGameChannel(IDMChannel channel, IMessageChannel value)
        => _dmList.TryAdd(channel.Id, value);

    /// <summary>
    ///     Attempts to remove the channel/channel pair from the DM Channel tracker.
    /// </summary>
    public bool TryRemoveGameChannel(IDMChannel channel)
        => _dmList.TryRemove(channel.Id, out var _);

    /// <summary>
    ///     Determines whether the game string tracker contains the specified key.
    /// </summary>
    public bool TryGetGameString(IMessageChannel channel, [NotNullWhen(true)]  out string? value)
        => _channelGames.TryGetValue(channel.Id, out value);

    /// <summary>
    ///     Attempts to add the channel/string pair to the game string tracker.
    /// </summary>
    public bool TryAddGameString(IMessageChannel channel, string value)
        => _channelGames.TryAdd(channel.Id, value);

    /// <summary>
    ///     Attempts to remove the channel/string pair from the game string tracker.
    /// </summary>
    public bool TryRemoveGameString(IMessageChannel channel)
        => _channelGames.TryRemove(channel.Id, out var _);
}
