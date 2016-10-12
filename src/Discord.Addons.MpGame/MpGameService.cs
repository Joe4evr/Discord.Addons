using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Discord.Addons.MpGame
{
    /// <summary>
    /// Service managing games for the <see cref="MpGameModuleBase{TGame, TPlayer}"/>.
    /// </summary>
    /// <typeparam name="TGame">The type of game to manage.</typeparam>
    /// <typeparam name="TPlayer">The type of the <see cref="Player"/> object.</typeparam>
    public sealed class MpGameService<TGame, TPlayer>
        where TGame : GameBase<TPlayer>
        where TPlayer : Player
    {
        /// <summary>
        /// The instance of a game being played, keyed by channel ID.
        /// </summary>
        internal readonly ConcurrentDictionary<ulong, TGame> GameList = new ConcurrentDictionary<ulong, TGame>();

        /// <summary>
        /// The list of users scheduled to join game, keyed by channel ID.
        /// </summary>
        /// <remarks>
        /// When instantiating the <see cref="HashSet{IGuildUser}"/>,
        /// pass in <see cref="MpGameModuleBase{TGame, TPlayer}.UserComparer"/> for the <see cref="IEqualityComparer{IGuildUser}"/>.
        /// </remarks>
        internal readonly ConcurrentDictionary<ulong, HashSet<IGuildUser>> PlayerList
            = new ConcurrentDictionary<ulong, HashSet<IGuildUser>>();

        /// <summary>
        /// Indicates whether the users can join a game about to start, keyed by channel ID.
        /// </summary>
        internal readonly ConcurrentDictionary<ulong, bool> OpenToJoin = new ConcurrentDictionary<ulong, bool>();

        /// <summary>
        /// Indicates whether a game is currently going on, keyed by channel ID.
        /// </summary>
        internal readonly ConcurrentDictionary<ulong, bool> GameInProgress = new ConcurrentDictionary<ulong, bool>();

        /// <summary>
        /// Add a new game to the list of active games.
        /// </summary>
        /// <param name="channelId">Public facing channel of this game.</param>
        /// <param name="game">Instance of the game.</param>
        public void AddNewGame(ulong channelId, TGame game)
            => GameList[channelId] = game;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="users"></param>
        public void SetPlayerList(ulong channelId, HashSet<IGuildUser> users)
            => PlayerList[channelId] = users;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="value"></param>
        public void SetOpenToJoin(ulong channelId, bool value)
            => OpenToJoin[channelId] = value;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="value"></param>
        public void SetInProgress(ulong channelId, bool value)
            => GameInProgress[channelId] = value;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        public bool TryRemoveGame(ulong channelId, out TGame game)
            => GameList.TryRemove(channelId, out game);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="newValue"></param>
        /// <param name="comparisonValue"></param>
        /// <returns></returns>
        public bool TryUpdateOpenToJoin(ulong channelId, bool newValue, bool comparisonValue)
            => OpenToJoin.TryUpdate(channelId, newValue, comparisonValue);
    }
}
