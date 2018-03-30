using System;
using System.Collections.Concurrent;
using System.Threading;
using Discord.Addons.Core;

namespace Discord.Addons.MpGame
{
    /// <summary> Keeps track of *all* channels that are playing a game. </summary>
    internal sealed class GameTracker
    {
        /// <summary> The singleton-instance of this class. </summary>
        /// <remarks>This feels so dirty, but it's hard to think of a way that
        /// doesn't also leak the implementation to end-users.</remarks>
        public static GameTracker Instance => _lazy.Value;
        private static readonly Lazy<GameTracker> _lazy = new Lazy<GameTracker>(() => new GameTracker(), LazyThreadSafetyMode.PublicationOnly);

        private GameTracker() { }

        private readonly ConcurrentDictionary<ulong, string> _channelGames = new ConcurrentDictionary<ulong, string>();

        private readonly ConcurrentDictionary<ulong, IMessageChannel> _dmList = new ConcurrentDictionary<ulong, IMessageChannel>();

        /// <summary> Determines whether the DM Channel tracker contains the specified key. </summary>
        public bool TryGetGameChannel(IDMChannel channel, out IMessageChannel value)
            => _dmList.TryGetValue(channel.Id, out value);

        /// <summary> Attempts to add the channel/channel pair to the DM Channel tracker. </summary>
        public bool TryAddGameChannel(IDMChannel channel, IMessageChannel value)
            => _dmList.TryAdd(channel.Id, value);

        /// <summary> Attempts to remove the channel/channel pair from the DM Channel tracker. </summary>
        public bool TryRemoveGameChannel(IDMChannel channel)
            => _dmList.TryRemove(channel.Id, out var _);

        /// <summary> Determines whether the game string tracker contains the specified key. </summary>
        public bool TryGetGameString(IMessageChannel channel, out string value)
            => _channelGames.TryGetValue(channel.Id, out value);

        /// <summary> Attempts to add the channel/string pair to the game string tracker. </summary>
        public bool TryAddGameString(IMessageChannel channel, string value)
            => _channelGames.TryAdd(channel.Id, value);

        /// <summary> Attempts to remove the channel/string pair from the game string tracker. </summary>
        public bool TryRemoveGameString(IMessageChannel channel)
            => _channelGames.TryRemove(channel.Id, out var _);
    }
}
