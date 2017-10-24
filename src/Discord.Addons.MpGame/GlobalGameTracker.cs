using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Discord.Addons.MpGame
{
    /// <summary> Keeps track of *all* channels that are playing a game. </summary>
    internal sealed class GameTracker
    {
        private static readonly Lazy<GameTracker> _lazy = new Lazy<GameTracker>(() => new GameTracker(), LazyThreadSafetyMode.PublicationOnly);
        private readonly ConcurrentDictionary<ulong, string> _channelGames = new ConcurrentDictionary<ulong, string>();

        /// <summary> The singleton-instance of this class. </summary>
        /// <remarks>This feels so dirty, but it's hard to think of a way that
        /// doesn't also leak the implementation to end-users.</remarks>
        public static GameTracker Instance => _lazy.Value;

        private GameTracker() { }

        /// <summary> Determines whether the tracker contains the specified key. </summary>
        public bool TryGet(IMessageChannel channel, out string value) => _channelGames.TryGetValue(channel.Id, out value);

        /// <summary> Attempts to add the channel/string pair to the tracker. </summary>
        public bool TryAdd(IMessageChannel channel, string value) => _channelGames.TryAdd(channel.Id, value);

        /// <summary> Attempts to remove the channel/string pair from the tracker. </summary>
        public bool TryRemove(IMessageChannel channel) => _channelGames.TryRemove(channel.Id, out var _);
    }
}
