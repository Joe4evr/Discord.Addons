using System.Collections.Concurrent;

namespace Discord.Addons.MpGame
{
    /// <summary> Keeps track of *all* channels that are playing a game. </summary>
    /// <remarks>This feels so dirty, but I can't think of a way that
    /// doesn't also leak the implementation to end-users.</remarks>
    internal static class GlobalGameTracker
    {
        private static ConcurrentDictionary<ulong, string> _channelGames = new ConcurrentDictionary<ulong, string>();

        /// <summary> Determines whether the tracker contains the specified key. </summary>
        public static bool ContainsKey(IMessageChannel channel) => _channelGames.ContainsKey(channel.Id);

        /// <summary> Attempts to add the channel/string pair to the tracker. </summary>
        public static bool TryAdd(IMessageChannel channel, string value) => _channelGames.TryAdd(channel.Id, value);

        /// <summary> Attempts to remove the channel/string pair from the tracker. </summary>
        public static bool TryRemove(IMessageChannel channel) => _channelGames.TryRemove(channel.Id, out var _);
    }
}
