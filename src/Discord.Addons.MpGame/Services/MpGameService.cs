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
    /// <summary>
    ///     Service managing games for a <see cref="MpGameModuleBase{TService, TGame, TPlayer}"/>.
    /// </summary>
    /// <typeparam name="TGame">
    ///     The type of game to manage.
    /// </typeparam>
    /// <typeparam name="TPlayer">
    ///     The type of the <see cref="Player"/> object.
    /// </typeparam>
    public partial class MpGameService<TGame, TPlayer>
        where TGame   : GameBase<TPlayer>
        where TPlayer : Player
    {
        private const string LogSource = "MpGame";

        /// <summary>
        ///     A cached IEqualityComparer&lt;IMessageChannel&gt;instance to use when instantiating a Dictionary&lt;IMessageChannel, TValue&gt;.<br/>
        ///     This is the same instance as <see cref="DiscordComparers.ChannelComparer"/>.
        /// </summary>
        protected static IEqualityComparer<IMessageChannel> MessageChannelComparer { get; } = DiscordComparers.ChannelComparer;

        //private readonly object _lock = new object();
        private readonly ConcurrentDictionary<IMessageChannel, PersistentGameData> _dataList =
            new ConcurrentDictionary<IMessageChannel, PersistentGameData>(MessageChannelComparer);

        protected Func<LogMessage, Task> Logger { get; }

        private readonly IMpGameServiceConfig _mpconfig;

        /// <summary>
        ///     Instantiates the MpGameService for the specified Game and Player type.
        /// </summary>
        /// <param name="client">
        ///     The Discord client.
        /// </param>
        /// <param name="mpconfig">
        ///     An optional config type.
        /// </param>
        /// <param name="logger">
        ///     An optional logging method.
        /// </param>
        public MpGameService(
#if TEST
            IDiscordClient iclient,
#else
            BaseSocketClient client,
#endif
            IMpGameServiceConfig mpconfig = null,
            Func<LogMessage, Task> logger = null)
        {
            _mpconfig = mpconfig ?? DefaultConfig.Instance;
            Logger = logger ?? Extensions.NoOpLogger;
            Logger(new LogMessage(LogSeverity.Debug, LogSource, _mpconfig.LogStrings.LogRegistration(_gameName)));

#if TEST
            if (iclient is BaseSocketClient client)
#endif
            client.ChannelDestroyed += CheckDestroyedChannel;
        }

        /// <summary>
        ///     Prepare to set up a new game in a specified channel.
        /// </summary>
        /// <param name="context">
        ///     Context of where this game is intended to be opened.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the operation succeeded, otherwise <see langword="false"/>.
        /// </returns>
        public async ValueTask<bool> OpenNewGame(ICommandContext context)
        {
            if (GameTracker.Instance.TryAddGameString(context.Channel, GameName))
            {
                await Logger(new LogMessage(LogSeverity.Verbose, LogSource,
                    _mpconfig.LogStrings.CreatingGame(context.Channel, _gameName))).ConfigureAwait(false);

                var data = new PersistentGameData(context.Channel, context.User, this);
                return _dataList.TryAdd(context.Channel, data)
                    && data.TryUpdateOpenToJoin(newValue: true, oldValue: false);
            }
            return false;
        }

        /// <summary>
        ///     Add a user to join an unstarted game.
        /// </summary>
        /// <param name="channel">
        ///     Public facing channel of this game.
        /// </param>
        /// <param name="user">
        ///     The user.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the operation succeeded, otherwise <see langword="false"/>.
        /// </returns>
        public async ValueTask<bool> AddUser(IMessageChannel channel, IUser user)
            => _dataList.TryGetValue(channel, out var data)
                && await data.TryAddUser(user);

        /// <summary>
        ///     Remove a user from an unstarted game.
        /// </summary>
        /// <param name="channel">
        ///     Public facing channel of this game.
        /// </param>
        /// <param name="user">
        ///     The user.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the operation succeeded, otherwise <see langword="false"/>.
        /// </returns>
        public async ValueTask<bool> RemoveUser(IMessageChannel channel, IUser user)
            => _dataList.TryGetValue(channel, out var data)
                && await data.TryRemoveUser(user);

        /// <summary>
        ///     Adds a player to an ongoing game.
        /// </summary>
        /// <param name="game">
        ///     The game instance.
        /// </param>
        /// <param name="player">
        ///     The player to add.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the operation succeeded, otherwise <see langword="false"/>.
        /// </returns>
        public async ValueTask<bool> AddPlayer(TGame game, TPlayer player)
        {
            if (!GameTracker.Instance.TryGetGameChannel(await player.User.GetOrCreateDMChannelAsync().ConfigureAwait(false), out var _))
            {
                PrepPlayer(game, player, addedInOngoig: true);
                game.Players.AddLast(player);
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Kicks a player from an ongoing game.
        /// </summary>
        /// <param name="game">
        ///     The game instance.
        /// </param>
        /// <param name="player">
        ///     The player to kick.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the operation succeeded, otherwise <see langword="false"/>.
        /// </returns>
        public ValueTask<bool> KickPlayer(TGame game, TPlayer player)
            => RemovePlayer(game, player, _mpconfig.LogStrings.PlayerKicked(player.User));

        /// <summary>
        ///     Cancel a game that has not yet started.
        /// </summary>
        /// <param name="channel">
        ///     Public facing channel of this game.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the operation succeeded, otherwise <see langword="false"/>.
        /// </returns>
        public ValueTask<bool> CancelGame(IMessageChannel channel)
            => OnGameEnd(channel);

        /// <summary>
        ///     Add a new game to the list of active games.
        /// </summary>
        /// <param name="channel">
        ///     Public facing channel of this game.
        /// </param>
        /// <param name="game">
        ///     Instance of the game.</param>
        /// <returns>
        ///     <see langword="true"/> if the operation succeeded, otherwise <see langword="false"/>.
        /// </returns>
        public async ValueTask<bool> TryAddNewGame(IMessageChannel channel, TGame game)
        {
            var success = _dataList.TryGetValue(channel, out var data);
            if (success)
            {
                await Logger(new LogMessage(LogSeverity.Verbose, LogSource,
                    _mpconfig.LogStrings.SettingGame(channel, _gameName))).ConfigureAwait(false);

                var gameSet = data.SetGame(game);
                if (gameSet)
                {
                    foreach (var player in game.Players)
                    {
                        PrepPlayer(game, player, addedInOngoig: false);
                    }
                    game.GameEnd = (async c => await OnGameEnd(c).ConfigureAwait(false));
                }
                return gameSet;
            }
            return success;
        }

        /// <summary>
        ///     Updates the flag indicating if a game can be joined or not.
        /// </summary>
        /// <param name="channel">
        ///     A message channel. Can be both the public-facing channel or the DM channel of one of the players.
        /// </param>
        /// <param name="newValue">
        ///     The new value.
        /// </param>
        /// <param name="comparisonValue">
        ///     The value that should be compared against.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the value was updated, otherwise <see langword="false"/>.
        /// </returns>
        public bool TryUpdateOpenToJoin(IMessageChannel channel, bool newValue, bool comparisonValue)
            => TryGetPersistentData(channel, out var data)
                && data.TryUpdateOpenToJoin(comparisonValue, newValue);

        /// <summary> 
        /// Retrieve the game instance being played, if any.
        /// </summary>
        /// <param name="channel">
        /// A message channel. Can be both the public-facing channel or the DM channel of one of the players.
        /// </param>
        /// <returns>
        /// The <typeparamref name="TGame"/> instance being played in the specified channel, or that the user is playing in, or <see langword="null"/> if there is none.
        /// </returns>
        public TGame GetGameFromChannel(IMessageChannel channel)
            => (TryGetPersistentData(channel, out var data))
                ? data.Game : null;

        /// <summary>
        ///     Retrieve the users set to join an open game, if any.
        /// </summary>
        /// <param name="channel">
        ///     A message channel. Can be both the public-facing channel or the DM channel of one of the players.
        /// </param>
        /// <returns>
        ///     The users set to join a new game, or an empty collection if there is no data.
        /// </returns>
        public IReadOnlyCollection<IUser> GetJoinedUsers(IMessageChannel channel)
        {
            return (TryGetPersistentData(channel, out var data))
                ? data.JoinedUsers
                : ImmutableHashSet<IUser>.Empty;
        }

        /// <summary>
        ///     Retrieve whether a game has been opened and users can join.
        /// </summary>
        /// <param name="channel">
        ///     A message channel. Can be both the public-facing channel or the DM channel of one of the players.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if a game has been opened and users can join, otherwise <see langword="false"/>.
        /// </returns>
        public bool IsOpenToJoin(IMessageChannel channel)
        {
            return TryGetPersistentData(channel, out var data) && data.OpenToJoin;
        }

        /// <summary>
        ///     Gets the game metadata associated with this context.
        /// </summary>
        /// <param name="context">
        ///     The CommandContext to fetch metadata for.
        /// </param>
        /// <returns>
        ///     A snapshot of the current game metadata.
        /// </returns>
        public MpGameData GetGameData(ICommandContext context)
        {
            return (TryGetPersistentData(context.Channel, out var internalData))
                ? new MpGameData(internalData, context)
                : MpGameData.Default;
        }

        internal void LogRegisteringPlayerTypeReader()
            => Logger(new LogMessage(LogSeverity.Info, LogSource,
                _mpconfig.LogStrings.RegisteringPlayerTypeReader(typeof(TPlayer).Name))).ConfigureAwait(false);

        private bool TryGetPersistentData(IMessageChannel channel, out PersistentGameData data)
        {
            var chan = (channel is IDMChannel dm && GameTracker.Instance.TryGetGameChannel(dm, out var pubc))
                ? pubc : channel;

            return _dataList.TryGetValue(chan, out data);
        }

        private void PrepPlayer(TGame game, TPlayer player, bool addedInOngoig)
        {
            player.AutoKickCallback = (async reason => await RemovePlayer(game, player, reason).ConfigureAwait(false));
            player.DMsDisabledMessage = _mpconfig.LogStrings.DMsDisabledMessage(player.User);
            player.DMsDisabledKickMessage = _mpconfig.LogStrings.DMsDisabledKickMessage(player.User);

            if (addedInOngoig)
            {
                game.OnPlayerAdded(player);
            }
        }

        private async ValueTask<bool> OnGameEnd(IMessageChannel channel)
        {
            var success = _dataList.TryRemove(channel, out var data);
            if (success)
            {
                await Logger(new LogMessage(LogSeverity.Verbose, LogSource,
                    _mpconfig.LogStrings.CleaningGameData(channel, _gameName))).ConfigureAwait(false);

                var tracker = GameTracker.Instance;
                var channels = await Task.WhenAll(data.JoinedUsers.Select(u => u.GetOrCreateDMChannelAsync())).ConfigureAwait(false);
                foreach (var ch in channels)
                {
                    await Logger(new LogMessage(LogSeverity.Debug, LogSource,
                        _mpconfig.LogStrings.CleaningDMChannel(ch))).ConfigureAwait(false);

                    tracker.TryRemoveGameChannel(ch);
                }
                await Logger(new LogMessage(LogSeverity.Debug, LogSource,
                    _mpconfig.LogStrings.CleaningGameString(channel))).ConfigureAwait(false);

                tracker.TryRemoveGameString(channel);
            }
            return success;
        }

        private Task CheckDestroyedChannel(SocketChannel channel)
        {
            return (channel is IMessageChannel msgChannel)
                ? OnGameEnd(msgChannel).AsTask()
                : Task.CompletedTask;
        }

        private static async ValueTask<bool> RemovePlayer(
            TGame game,
            TPlayer player,
            string reason)
        {
            var success = game.Players.RemoveItem(player);
            if (success)
            {
                game.OnPlayerKicked(player);
                GameTracker.Instance.TryRemoveGameChannel(await player.User.GetOrCreateDMChannelAsync().ConfigureAwait(false));
                await game.Channel.SendMessageAsync(reason).ConfigureAwait(false);
            }
            return success;
        }

        //micro-optimization
        private static readonly string _gameName = typeof(TGame).Name;
        private static readonly string _gameFullName = typeof(TGame).FullName;
        internal string GameName => _gameFullName;
    }

    /// <summary>
    ///     Service managing games for <see cref="MpGameModuleBase{TService, TGame, TPlayer}"/> using the default <see cref="Player"/> type.
    /// </summary>
    /// <typeparam name="TGame">
    ///     The type of game to manage.
    /// </typeparam>
    public class MpGameService<TGame> : MpGameService<TGame, Player>
        where TGame : GameBase<Player>
    {
        /// <summary>
        ///     Instantiates the MpGameService for the specified Game type.
        /// </summary>
        /// <param name="client">
        ///     The Discord client.
        /// </param>
        /// <param name="mpconfig">
        ///     An optional config type.
        /// </param>
        /// <param name="logger">
        ///     An optional logging method.
        /// </param>
        public MpGameService(
            BaseSocketClient client,
            IMpGameServiceConfig mpconfig = null,
            Func<LogMessage, Task> logger = null)
            : base(client, mpconfig, logger) { }
    }
}
