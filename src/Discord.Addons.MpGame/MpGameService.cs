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
        /// <summary> A cached IEqualityComparer&lt;<see cref="IMessageChannel"/>&gt;instance to use when
        /// instantiating a <see cref="Dictionary{TKey, TValue}"/> using <see cref="IMessageChannel"/> as the key.
        /// This is the same instance as <see cref="DiscordComparers.ChannelComparer"/>.</summary>
        protected static IEqualityComparer<IMessageChannel> MessageChannelComparer { get; } = DiscordComparers.ChannelComparer;

        //private readonly object _lock = new object();
        private readonly ConcurrentDictionary<IMessageChannel, PersistentGameData> _dataList =
            new ConcurrentDictionary<IMessageChannel, PersistentGameData>(MessageChannelComparer);

        protected internal Func<LogMessage, Task> Logger { get; }

        public MpGameService(
            BaseSocketClient client,
            Func<LogMessage, Task> logger = null)
        {
            Logger = logger ?? Extensions.NoOpLogger;
            Logger(new LogMessage(LogSeverity.Debug, "MpGame", $"Registered service for '{_gameName}'"));

            client.ChannelDestroyed += CheckDestroyedChannel;
        }

        private Task CheckDestroyedChannel(SocketChannel channel)
        {
            return (channel is IMessageChannel msgChannel)
                ? OnGameEnd(msgChannel)
                : Task.CompletedTask;
        }

        private async Task<bool> OnGameEnd(IMessageChannel channel)
        {
            var success = _dataList.TryRemove(channel, out var data);
            if (success)
            {
                await Logger(new LogMessage(LogSeverity.Verbose, "MpGame", $"Cleaning up '{_gameName}' data for channel: #{channel.Id}")).ConfigureAwait(false);
                var tracker = GameTracker.Instance;
                var channels = await Task.WhenAll(data.JoinedUsers.Select(u => u.GetOrCreateDMChannelAsync())).ConfigureAwait(false);
                foreach (var ch in channels)
                {
                    await Logger(new LogMessage(LogSeverity.Debug, "MpGame", $"Cleaning up DM channel key: #{ch.Id}")).ConfigureAwait(false);
                    tracker.TryRemoveGameChannel(ch);
                }
                await Logger(new LogMessage(LogSeverity.Debug, "MpGame", $"Cleaning up game string for channel: #{channel.Id}")).ConfigureAwait(false);
                tracker.TryRemoveGameString(channel);
            }
            return success;
        }

        /// <summary> Prepare to set up a new game in a specified channel. </summary>
        /// <param name="context">Context of where this game is intended to be opened.</param>
        /// <returns><see cref="true"/> if the operation succeeded, otherwise <see cref="false"/>.</returns>
        public async Task<bool> OpenNewGame(ICommandContext context)
        {
            if (GameTracker.Instance.TryAddGameString(context.Channel, GameName))
            {
                await Logger(new LogMessage(LogSeverity.Verbose, "MpGame", $"Creating '{_gameName}' data for channel: #{context.Channel.Id}")).ConfigureAwait(false);
                var data = new PersistentGameData(context.Channel, context.User, this);
                return _dataList.TryAdd(context.Channel, data)
                    && data.TryUpdateOpenToJoin(newValue: true, oldValue: false);
            }
            return false;
        }

        /// <summary> Add a user to join an unstarted game. </summary>
        /// <param name="channel">Public facing channel of this game.</param>
        /// <param name="user">The user.</param>
        /// <returns><see cref="true"/> if the operation succeeded, otherwise <see cref="false"/>.</returns>
        public async Task<bool> AddUser(IMessageChannel channel, IUser user)
            => TryGetPersistentData(channel, out var data)
                && await data.TryAddUser(user);

        /// <summary> Remove a user from an unstarted game. </summary>
        /// <param name="channel">Public facing channel of this game.</param>
        /// <param name="user">The user.</param>
        /// <returns><see cref="true"/> if the operation succeeded, otherwise <see cref="false"/>.</returns>
        public async Task<bool> RemoveUser(IMessageChannel channel, IUser user)
            => TryGetPersistentData(channel, out var data)
                && await data.TryRemoveUser(user);

        /// <summary> Cancel a game that has not yet started. </summary>
        /// <param name="channel">Public facing channel of this game.</param>
        /// <returns><see cref="true"/> if the operation succeeded, otherwise <see cref="false"/>.</returns>
        public Task<bool> CancelGame(IMessageChannel channel)
            => OnGameEnd(channel);

        /// <summary> Add a new game to the list of active games. </summary>
        /// <param name="channel">Public facing channel of this game.</param>
        /// <param name="game">Instance of the game.</param>
        /// <returns><see cref="true"/> if the operation succeeded, otherwise <see cref="false"/>.</returns>
        public async Task<bool> TryAddNewGame(IMessageChannel channel, TGame game)
        {
            if (TryGetPersistentData(channel, out var data))
            {
                await Logger(new LogMessage(LogSeverity.Verbose, "MpGame", $"Setting game '{_gameName}' for channel: #{channel.Id}")).ConfigureAwait(false);
                var gameSet = data.SetGame(game);
                if (gameSet)
                {
                    game.GameEnd = OnGameEnd;
                }
                return gameSet;
            }
            return false;
        }

        /// <summary> Updates the flag indicating if a game can be joined or not. </summary>
        /// <param name="channel">The Channel ID.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="comparisonValue">The value that should be compared against.</param>
        /// <returns><see cref="true"/> if the value was updated, otherwise <see cref="false"/>.</returns>
        public bool TryUpdateOpenToJoin(IMessageChannel channel, bool newValue, bool comparisonValue)
            => TryGetPersistentData(channel, out var data)
                && data.TryUpdateOpenToJoin(comparisonValue, newValue);

        /// <summary> Retrieve the game instance being played, if any. </summary>
        /// <param name="channel">A message channel. Can be both the public-facing channel
        /// or the DM channel of one of the players.</param>
        /// <returns>The <see cref="TGame"/> instance being played in the specified channel,
        /// or that the user is playing in, or <see cref="null"/> if there is none.</returns>
        public TGame GetGameFromChannel(IMessageChannel channel)
            => (TryGetPersistentData(channel, out var data))
                ? data.Game : null;

        /// <summary> Retrieve the users set to join an open game, if any. </summary>
        /// <param name="channel">The public-facing message channel.</param>
        /// <returns>The users set to join a new game, or an empty collection
        /// if there is no data.</returns>
        public IReadOnlyCollection<IUser> GetJoinedUsers(IMessageChannel channel)
        {
            return (TryGetPersistentData(channel, out var data))
                ? data.JoinedUsers
                : ImmutableHashSet<IUser>.Empty;
        }

        /// <summary> Retrieve whether a game has been opened and users can join. </summary>
        /// <param name="channel">The public-facing message channel.</param>
        /// <returns><see cref="true"/> if a game has been opened
        /// and users can join, otherwise <see cref="false"/>.</returns>
        public bool IsOpenToJoin(IMessageChannel channel)
        {
            return TryGetPersistentData(channel, out var data) && data.OpenToJoin;
        }

        internal bool TryGetPersistentData(IMessageChannel channel, out PersistentGameData data)
        {
            var chan = (channel is IDMChannel dm && GameTracker.Instance.TryGetGameChannel(dm, out var pubc))
                ? pubc : channel;

            return _dataList.TryGetValue(chan, out data);
        }

        //micro-optimization
        private static readonly string _gameName = typeof(TGame).Name;
        private static readonly string _gameFullName = typeof(TGame).FullName;
        internal string GameName => _gameFullName;
    }

    /// <summary> Service managing games for <see cref="MpGameModuleBase{TService, TGame, TPlayer}"/>
    /// using the default <see cref="Player"/> type. </summary>
    /// <typeparam name="TGame">The type of game to manage.</typeparam>
    public class MpGameService<TGame> : MpGameService<TGame, Player>
        where TGame : GameBase<Player>
    {
        public MpGameService(
            BaseSocketClient client,
            Func<LogMessage, Task> logger = null)
            : base(client, logger) { }
    }
}
