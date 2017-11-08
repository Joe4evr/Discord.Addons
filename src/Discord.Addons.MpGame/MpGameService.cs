using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.Core;

namespace Discord.Addons.MpGame
{
    /// <summary> Service managing games for a <see cref="MpGameModuleBase{TService, TGame, TPlayer}"/>. </summary>
    /// <typeparam name="TGame">The type of game to manage.</typeparam>
    /// <typeparam name="TPlayer">The type of the <see cref="Player"/> object.</typeparam>
    public class MpGameService<TGame, TPlayer>
        where TGame : GameBase<TPlayer>
        where TPlayer : Player
    {
        ///// <summary> A cached <see cref="IEqualityComparer{IUser}"/> instance to use when
        ///// instantiating a <see cref="Dictionary{TKey, TValue}"/> using an <see cref="IUser"/> as the key. </summary>
        //protected static IEqualityComparer<IUser> UserComparer { get; } = Comparers.UserComparer;
        /// <summary> A cached <see cref="IEqualityComparer{T}">IEqualityComparer</see>&lt;<see cref="IMessageChannel"/>&gt;instance to use when
        /// instantiating a <see cref="Dictionary{TKey, TValue}"/> using <see cref="IMessageChannel"/> as the key. </summary>
        protected static IEqualityComparer<IMessageChannel> MessageChannelComparer { get; } = Comparers.ChannelComparer;

        private readonly object _lock = new object();
        private readonly ConcurrentDictionary<IMessageChannel, PersistentGameData<TGame, TPlayer>> _dataList
            = new ConcurrentDictionary<IMessageChannel, PersistentGameData<TGame, TPlayer>>(MessageChannelComparer);

        protected Func<LogMessage, Task> Logger { get; }

        public MpGameService(Func<LogMessage, Task> logger = null)
        {
            Logger = logger ?? (_ => Task.CompletedTask);
        }

        internal PersistentGameData<TGame, TPlayer> GetData(IMessageChannel channel)
            => _dataList.GetValueOrDefault(channel);

        private Task OnGameEnd(IMessageChannel channel)
        {
            if (_dataList.TryRemove(channel, out var data))
            {
                GameTracker.Instance.TryRemove(channel);
                data.Game.GameEnd -= OnGameEnd;
            }
            return Task.CompletedTask;
        }

        /// <summary> Prepare to set up a new game in a specified channel. </summary>
        /// <param name="channel">Public facing channel of this game.</param>
        /// <returns>true if the operation succeeded, otherwise false.</returns>
        public bool OpenNewGame(IMessageChannel channel)
        {
            if (GameTracker.Instance.TryGet(channel, out var _))
            {

            }

            lock (_lock)
            {
                var data = new PersistentGameData<TGame, TPlayer>();
                data.NewPlayerList();
                _dataList.AddOrUpdate(channel, data, (k, v) => data);
                GameTracker.Instance.TryAdd(channel, GameName);
                return data.TryUpdateOpenToJoin(newValue: true, oldValue: false);
            }
        }

        /// <summary> Add a user to join an unstarted game. </summary>
        /// <param name="channel">Public facing channel of this game.</param>
        /// <param name="user">The user.</param>
        /// <returns>true if the operation succeeded, otherwise false.</returns>
        public bool AddUser(IMessageChannel channel, IUser user)
        {
            lock (_lock)
            {
                if (_dataList.TryGetValue(channel, out var data))
                {
                    return data.TryAddUser(user);
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary> Remove a user from an unstarted game. </summary>
        /// <param name="channel">Public facing channel of this game.</param>
        /// <param name="user">The user.</param>
        /// <returns>true if the operation succeeded, otherwise false.</returns>
        public bool RemoveUser(IMessageChannel channel, IUser user)
        {
            lock (_lock)
            {
                if (_dataList.TryGetValue(channel, out var data))
                {
                    return data.TryRemoveUser(user);
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary> Cancel a game that has not yet started. </summary>
        /// <param name="channel">Public facing channel of this game.</param>
        /// <returns>true if the operation succeeded, otherwise false.</returns>
        public bool CancelGame(IMessageChannel channel)
        {
            lock (_lock)
            {
                GameTracker.Instance.TryRemove(channel);
                return _dataList.TryRemove(channel, out var _);
            }
        }

        /// <summary> Add a new game to the list of active games. </summary>
        /// <param name="channel">Public facing channel of this game.</param>
        /// <param name="game">Instance of the game.</param>
        /// <returns>true if the operation succeeded, otherwise false.</returns>
        public bool TryAddNewGame(IMessageChannel channel, TGame game)
        {
            lock (_lock)
            {
                if (_dataList.TryGetValue(channel, out var data))
                {
                    var success = data.SetGame(game) && TryUpdateOpenToJoin(channel, newValue: false, comparisonValue: true);
                    if (success)
                    {
                        game.GameEnd += OnGameEnd;
                    }
                    return success;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary> Updates the flag indicating if a game can be joined or not. </summary>
        /// <param name="channel">The Channel ID.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="comparisonValue">The value that should be compared against.</param>
        /// <returns>true if the value was updated, otherwise false.</returns>
        public bool TryUpdateOpenToJoin(IMessageChannel channel, bool newValue, bool comparisonValue)
        {
            lock (_lock)
            {
                if (!_dataList.TryGetValue(channel, out var data))
                {
                    data = new PersistentGameData<TGame, TPlayer>();
                    _dataList.TryAdd(channel, data);
                }
                return data.TryUpdateOpenToJoin(comparisonValue, newValue);
            }
        }

        /// <summary> Retrieve the game instance being played, if any. </summary>
        /// <param name="channel">A message channel. Can be both the public-facing channel
        /// or the DM channel of one of the players.</param>
        /// <returns>The <see cref="TGame"/> instance being played in the specified channel,
        /// or that the user is playing in, or <see cref="null"/> if there is none.</returns>
        public async Task<TGame> GetGameFromChannelAsync(IMessageChannel channel)
        {
            if (_dataList.TryGetValue(channel, out var d))
            {
                return d.Game;
            }
            foreach (var data in _dataList.Values)
            {
                if ((await data.Game.PlayerChannels().ConfigureAwait(false)).Any(c => c.Id == channel.Id))
                {
                    return data.Game;
                }
            }
            return null;
        }

        /// <summary> Retrieve the users set to join an open game, if any. </summary>
        /// <param name="channel">The public-facing message channel.</param>
        /// <returns>The users set to join a new game, or an empty collection
        /// if there is no data.</returns>
        public IReadOnlyCollection<IUser> GetJoinedUsers(IMessageChannel channel)
        {
            if (_dataList.TryGetValue(channel, out var data))
            {
                return data.JoinedUsers;
            }

            return ImmutableHashSet<IUser>.Empty;
        }

        /// <summary> Retrieve whether a game has been opened and users can join. </summary>
        /// <param name="channel">The public-facing message channel.</param>
        /// <returns><see cref="true"/> if a game has been opened
        /// and users can join, otherwise <see cref="false"/>.</returns>
        public bool IsOpenToJoin(IMessageChannel channel)
        {
            return _dataList.TryGetValue(channel, out var data) && data.OpenToJoin;
        }

        //micro-optimization
        private static readonly string _gameName = typeof(TGame).FullName;
        internal string GameName => _gameName;
    }

    /// <summary> Service managing games for <see cref="MpGameModuleBase{TService, TGame, TContext}"/>
    /// using the default <see cref="Player"/> type. </summary>
    /// <typeparam name="TGame">The type of game to manage.</typeparam>
    public class MpGameService<TGame> : MpGameService<TGame, Player>
        where TGame : GameBase<Player>
    {
    }
}
