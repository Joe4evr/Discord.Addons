using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.MpGame
{
    /// <summary> Service managing games for <see cref="MpGameModuleBase{TService, TGame, TPlayer, TContext}"/>. </summary>
    /// <typeparam name="TGame">The type of game to manage.</typeparam>
    /// <typeparam name="TPlayer">The type of the <see cref="Player"/> object.</typeparam>
    public class MpGameService</*TData,*/ TGame, TPlayer>
        //where TData   : PersistentGameData<TGame, TPlayer>
        where TGame : GameBase<TPlayer>
        where TPlayer : Player
    {
        ///// <summary> A cached <see cref="IEqualityComparer{IUser}"/> instance to use when
        ///// instantiating a <see cref="Dictionary{TKey, TValue}"/> using an <see cref="IUser"/> as the key. </summary>
        //protected static IEqualityComparer<IUser> UserComparer { get; } = Comparers.UserComparer;

        /// <summary> A cached <see cref="IEqualityComparer{IMessageChannel}"/> instance to use when
        /// instantiating a <see cref="Dictionary{TKey, TValue}"/> using <see cref="IMessageChannel"/> as the key. </summary>
        protected static IEqualityComparer<IMessageChannel> MessageChannelComparer { get; } = Comparers.ChannelComparer;

        private readonly object _lock = new object();
        private readonly ConcurrentDictionary<IMessageChannel, PersistentGameData<TGame, TPlayer>> _dataList
            = new ConcurrentDictionary<IMessageChannel, PersistentGameData<TGame, TPlayer>>(MessageChannelComparer);

        protected Func<LogMessage, Task> Logger { get; }

        /// <summary> The instance of a game being played, keyed by channel. </summary>
        public IReadOnlyDictionary<IMessageChannel, TGame> GameList
            => _dataList.ToDictionary(d => d.Key, d => d.Value.Game);

        /// <summary> Indicates whether the users can join a game about to start, keyed by channel. </summary>
        public IReadOnlyDictionary<IMessageChannel, bool> OpenToJoin
            => _dataList.ToDictionary(d => d.Key, d => d.Value.OpenToJoin);

        /// <summary> The list of users scheduled to join game, keyed by channel. </summary>
        public IReadOnlyDictionary<IMessageChannel, ImmutableHashSet<IUser>> PlayerList
            => _dataList.ToDictionary(d => d.Key, d => d.Value.JoinedUsers);

        public MpGameService(Func<LogMessage, Task> logger = null)
        {
            Logger = logger ?? (msg => Task.CompletedTask);
        }

        //internal Task Log(LogSeverity severity, string msg)
        //{
        //    return _logger(new LogMessage(severity, $"MpGameSvc<{typeof(TGame).Name}>", msg));
        //}

        internal PersistentGameData<TGame, TPlayer> GetData(IMessageChannel channel)
            => _dataList.GetValueOrDefault(channel);

        private Task _onGameEnd(IMessageChannel channel)
        {
            if (_dataList.TryRemove(channel, out var data))
            {
                data.Game.GameEnd -= _onGameEnd;
            }
            return Task.CompletedTask;
        }

        /// <summary> Prepare to set up a new game in a specified channel. </summary>
        /// <param name="channel">Public facing channel of this game.</param>
        /// <returns>true if the operation succeeded, otherwise false.</returns>
        public bool OpenNewGame(IMessageChannel channel)
        {
            lock (_lock)
            {
                if (!_dataList.TryGetValue(channel, out var data))
                {
                    data = new PersistentGameData<TGame, TPlayer>();
                    _dataList.TryAdd(channel, data);
                }
                data.NewPlayerList();
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
                    //var builder = _playerList[channel].ToBuilder();
                    //var result = builder.Add(user);
                    //if (result)
                    //    _playerList[channel] = builder.ToImmutable();

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
                    //var builder = _playerList[channel].ToBuilder();
                    //var result = builder.Remove(user);
                    //if (result)
                    //    _playerList[channel] = builder.ToImmutable();

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
            //lock (_lock)
            //{
            //    return (TryUpdateOpenToJoin(channel, newValue: false, comparisonValue: true)
            //        && _playerList.TryRemove(channel, out var _));
            //}
            return _dataList.TryRemove(channel, out var _);
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
                        game.GameEnd += _onGameEnd;
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
            if (!_dataList.TryGetValue(channel, out var data))
            {
                data = new PersistentGameData<TGame, TPlayer>();
                _dataList.TryAdd(channel, data);
            }
            return data.TryUpdateOpenToJoin(comparisonValue, newValue);
        }

        /// <summary> Retrieve the game instance being played, if any. </summary>
        /// <param name="channel"></param>
        /// <returns>The <see cref="TGame"/> instance being played in the specified channel,
        /// or <see cref="null"/> if there is none.</returns>
        public async Task<TGame> GetGameFromChannelAsync(IMessageChannel channel)
        {
            if (GameList.TryGetValue(channel, out var g))
            {
                return g;
            }
            foreach (var game in GameList.Values)
            {
                if ((await game.PlayerChannels().ConfigureAwait(false)).Select(c => c.Id).Contains(channel.Id))
                {
                    return game;
                }
            }
            return null;
        }
    }

    ///// <summary> Service managing games for <see cref="MpGameModuleBase{TService, TGame, TContext}"/>
    ///// using the default <see cref="Player"/> type. </summary>
    ///// <typeparam name="TGame">The type of game to manage.</typeparam>
    //public class MpGameService<TGame> : MpGameService<TGame, Player>
    //    where TGame : GameBase<Player>
    //{
    //}

    //public class MpGameService<TPlayer> : MpGameService<GameBase<TPlayer>, TPlayer>
    //    where TPlayer : Player
    //{
    //}

    //public class MpGameService : MpGameService<GameBase, Player>
    //{
    //}
}
