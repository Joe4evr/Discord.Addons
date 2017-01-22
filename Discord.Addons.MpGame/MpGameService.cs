using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.MpGame
{
    /// <summary> Service managing games of type <see cref="MpGameModuleBase{TService, TGame, TPlayer}"/>. </summary>
    /// <typeparam name="TGame">The type of game to manage.</typeparam>
    /// <typeparam name="TPlayer">The type of the <see cref="Player"/> object.</typeparam>
    public class MpGameService<TGame, TPlayer>
        where TGame : GameBase<TPlayer>
        where TPlayer : Player
    {
        /// <summary> A cached <see cref="IEqualityComparer{IUser}"/> instance to use when
        /// instantiating the <see cref="PlayerList"/>'s <see cref="HashSet{IUser}"/>. </summary>
        private static readonly IEqualityComparer<IUser> UserComparer = new EntityEqualityComparer<ulong>();

        private readonly ConcurrentDictionary<ulong, TGame> _gameList
            = new ConcurrentDictionary<ulong, TGame>();

        private readonly ConcurrentDictionary<ulong, ImmutableHashSet<IUser>> _playerList
            = new ConcurrentDictionary<ulong, ImmutableHashSet<IUser>>();

        private readonly ConcurrentDictionary<ulong, bool> _openToJoin
            = new ConcurrentDictionary<ulong, bool>();

        /// <summary> The instance of a game being played, keyed by channel ID. </summary>
        public IReadOnlyDictionary<ulong, TGame> GameList => _gameList.ToImmutableDictionary();

        /// <summary> The list of users scheduled to join game, keyed by channel ID. </summary>
        public IReadOnlyDictionary<ulong, ImmutableHashSet<IUser>> PlayerList => _playerList.ToImmutableDictionary();

        /// <summary> Indicates whether the users can join a game about to start, keyed by channel ID. </summary>
        public IReadOnlyDictionary<ulong, bool> OpenToJoin => _openToJoin.ToImmutableDictionary();

        private Task _onGameEnd(ulong channelId)
        {
            if (_gameList.TryRemove(channelId, out var game))
            {
                _playerList.TryRemove(channelId, out var _);
                game.GameEnd -= _onGameEnd;
            }
            return Task.CompletedTask;
        }

        /// <summary> Add a new game to the list of active games. </summary>
        /// <param name="channelId">Public facing channel of this game.</param>
        /// <param name="game">Instance of the game.</param>
        /// <returns>true if the operation succeeded, otherwise false.</returns>
        public bool TryAddNewGame(ulong channelId, TGame game)
        {
            var success = _gameList.TryAdd(channelId, game);
            if (success)
                game.GameEnd += _onGameEnd;

            return success;
        }

        /// <summary> Add a user to join an unstarted game. </summary>
        /// <param name="channelId">Public facing channel of this game.</param>
        /// <param name="user">The user.</param>
        /// <returns>true if the operation succeeded, otherwise false.</returns>
        public bool AddUser(ulong channelId, IUser user)
        {
            var builder = _playerList[channelId].ToBuilder();
            var result = builder.Add(user);
            if (result)
                _playerList[channelId] = builder.ToImmutable();

            return result;
        }

        /// <summary> Remove a user from an unstarted game. </summary>
        /// <param name="channelId">Public facing channel of this game.</param>
        /// <param name="user">The user.</param>
        /// <returns>true if the operation succeeded, otherwise false.</returns>
        public bool RemoveUser(ulong channelId, IUser user)
        {
            var builder = _playerList[channelId].ToBuilder();
            var result = builder.Remove(user);
            if (result)
                _playerList[channelId] = builder.ToImmutable();

            return result;
        }

        /// <summary> Cancel a game that has not yet started. </summary>
        /// <param name="channelId">Public facing channel of this game.</param>
        /// <returns>true if the operation succeeded, otherwise false.</returns>
        public bool CancelGame(ulong channelId)
        {
            return (TryUpdateOpenToJoin(channelId, newValue: false, comparisonValue: true)
                && _playerList.TryRemove(channelId, out var _));
        }

        /// <summary> Sets a new Player List for the specified channel. </summary>
        /// <param name="channelId">The Channel ID.</param>
        public bool MakeNewPlayerList(ulong channelId)
            => _playerList.TryAdd(channelId, ImmutableHashSet.Create(UserComparer));

        /// <summary> Updates the flag indicating if a game can be joined or not. </summary>
        /// <param name="channelId">The Channel ID.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="comparisonValue">The value that should be compared against.</param>
        /// <returns>true if the value was updated, otherwise false.</returns>
        public bool TryUpdateOpenToJoin(ulong channelId, bool newValue, bool comparisonValue)
            => _openToJoin.TryUpdate(channelId, newValue, comparisonValue);
    }
}
