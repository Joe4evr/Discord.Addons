using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Core;

namespace Discord.Addons.MpGame
{
    /// <summary> Service managing games for a <see cref="MpGameModuleBase{TService, TGame, TPlayer}"/>. </summary>
    /// <typeparam name="TGame">The type of game to manage.</typeparam>
    /// <typeparam name="TPlayer">The type of the <see cref="Player"/> object.</typeparam>
    public partial class MpGameService<TGame, TPlayer>
        where TGame   : GameBase<TPlayer>
        where TPlayer : Player
    {
        ///// <summary> A cached <see cref="IEqualityComparer{IUser}"/> instance to use when
        ///// instantiating a <see cref="Dictionary{TKey, TValue}"/> using an <see cref="IUser"/> as the key. </summary>
        //protected static IEqualityComparer<IUser> UserComparer { get; } = Comparers.UserComparer;
        /// <summary> A cached IEqualityComparer&lt;<see cref="IMessageChannel"/>&gt;instance to use when
        /// instantiating a <see cref="Dictionary{TKey, TValue}"/> using <see cref="IMessageChannel"/> as the key. </summary>
        protected static IEqualityComparer<IMessageChannel> MessageChannelComparer { get; } = Comparers.ChannelComparer;

        private readonly object _lock = new object();
        private readonly ConcurrentDictionary<IMessageChannel, PersistentGameData> _dataList
            = new ConcurrentDictionary<IMessageChannel, PersistentGameData>(MessageChannelComparer);

        protected Func<LogMessage, Task> Logger { get; }

        private MpGameService(Func<LogMessage, Task> logger = null)
        {
            Logger = logger ?? Extensions.NoOpLogger;
        }

        public MpGameService(DiscordSocketClient socketClient, Func<LogMessage, Task> logger = null)
            : this(logger)
        {
            socketClient.ChannelDestroyed += CheckDestroyedChannel;
        }

        public MpGameService(DiscordShardedClient shardedClient, Func<LogMessage, Task> logger = null)
            : this(logger)
        {
            shardedClient.ChannelDestroyed += CheckDestroyedChannel;
        }

        internal PersistentGameData GetData(IMessageChannel channel)
            => _dataList.GetValueOrDefault(channel);

        private Task CheckDestroyedChannel(SocketChannel channel)
        {
            return (channel is IMessageChannel msgChannel && !(msgChannel is IDMChannel))
                ? OnGameEnd(msgChannel)
                : Task.CompletedTask;
        }

        private async Task OnGameEnd(IMessageChannel channel)
        {
            if (_dataList.TryRemove(channel, out var data))
            {
                var instance = GameTracker.Instance;
                var channels = await data.Game.PlayerChannels();
                foreach (var ch in channels)
                {
                    instance.TryRemoveGameChannel(ch);
                }
                instance.TryRemoveGameString(channel);
                data.Game.GameEnd -= OnGameEnd;
            }
        }

        /// <summary> Prepare to set up a new game in a specified channel. </summary>
        /// <param name="channel">Public facing channel of this game.</param>
        /// <returns><see langword="true"/> if the operation succeeded, otherwise <see langword="false"/>.</returns>
        public bool OpenNewGame(IMessageChannel channel)
        {
            lock (_lock)
            {
                if (GameTracker.Instance.TryGetGameString(channel, out var _))
                {
                    return false;
                }

                var data = new PersistentGameData(channel);
                GameTracker.Instance.TryAddGameString(channel, GameName);
                data.NewPlayerList();
                _dataList.AddOrUpdate(channel, data, (k, v) => data);
                return data.TryUpdateOpenToJoin(newValue: true, oldValue: false);
            }
        }

        /// <summary> Add a user to join an unstarted game. </summary>
        /// <param name="channel">Public facing channel of this game.</param>
        /// <param name="user">The user.</param>
        /// <returns>true if the operation succeeded, otherwise false.</returns>
        public Task<bool> AddUser(IMessageChannel channel, IUser user)
        {
            if (_dataList.TryGetValue(channel, out var data))
            {
                lock (_lock)
                {
                    return data.TryAddUser(user);
                }
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        /// <summary> Remove a user from an unstarted game. </summary>
        /// <param name="channel">Public facing channel of this game.</param>
        /// <param name="user">The user.</param>
        /// <returns>true if the operation succeeded, otherwise false.</returns>
        public Task<bool> RemoveUser(IMessageChannel channel, IUser user)
        {
            if (_dataList.TryGetValue(channel, out var data))
            {
                lock (_lock)
                {
                    return data.TryRemoveUser(user);
                }
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        /// <summary> Cancel a game that has not yet started. </summary>
        /// <param name="channel">Public facing channel of this game.</param>
        /// <returns>true if the operation succeeded, otherwise false.</returns>
        public async Task<bool> CancelGame(IMessageChannel channel)
        {
            await OnGameEnd(channel);
            return _dataList.TryRemove(channel, out var _);
        }

        /// <summary> Add a new game to the list of active games. </summary>
        /// <param name="channel">Public facing channel of this game.</param>
        /// <param name="game">Instance of the game.</param>
        /// <returns>true if the operation succeeded, otherwise false.</returns>
        public bool TryAddNewGame(IMessageChannel channel, TGame game)
        {
            if (_dataList.TryGetValue(channel, out var data))
            {
                lock (_lock)
                {
                    var success = data.SetGame(game) && TryUpdateOpenToJoin(channel, newValue: false, comparisonValue: true);
                    if (success)
                    {
                        game.GameEnd += OnGameEnd;
                    }
                    return success;
                }
            }
            else
            {
                return false;
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
                return false;
            }
            return data.TryUpdateOpenToJoin(comparisonValue, newValue);
        }

        /// <summary> Retrieve the game instance being played, if any. </summary>
        /// <param name="channel">A message channel. Can be both the public-facing channel
        /// or the DM channel of one of the players.</param>
        /// <returns>The <see cref="TGame"/> instance being played in the specified channel,
        /// or that the user is playing in, or <see cref="null"/> if there is none.</returns>
        public TGame GetGameFromChannel(IMessageChannel channel)
        {
            var chan = (channel is IDMChannel dm && GameTracker.Instance.TryGetGameChannel(dm, out var pubc))
                ? pubc
                : channel;

            return (_dataList.TryGetValue(chan, out var data))
                ? data.Game
                : null;
        }

        /// <summary> Retrieve the users set to join an open game, if any. </summary>
        /// <param name="channel">The public-facing message channel.</param>
        /// <returns>The users set to join a new game, or an empty collection
        /// if there is no data.</returns>
        public IReadOnlyCollection<IUser> GetJoinedUsers(IMessageChannel channel)
        {
            return (_dataList.TryGetValue(channel, out var data))
                ? data.JoinedUsers
                : ImmutableHashSet<IUser>.Empty;
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

    /// <summary> Service managing games for <see cref="MpGameModuleBase{TService, TGame, TPlayer}"/>
    /// using the default <see cref="Player"/> type. </summary>
    /// <typeparam name="TGame">The type of game to manage.</typeparam>
    public class MpGameService<TGame> : MpGameService<TGame, Player>
        where TGame : GameBase<Player>
    {
        public MpGameService(DiscordSocketClient socketClient, Func<LogMessage, Task> logger = null)
            : base(socketClient, logger) { }

        public MpGameService(DiscordShardedClient shardedClient, Func<LogMessage, Task> logger = null)
            : base(shardedClient, logger) { }
    }
}
