using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
    public partial class MpGameService<TGame, TPlayer> : IDisposable
        where TGame   : GameBase<TPlayer>
        where TPlayer : Player
    {
        internal const string LogSource = "MpGame";

        /// <summary>
        ///     A cached <see cref="IEqualityComparer{IMessageChannel}"/> instance to use
        ///     when instantiating a <see cref="Dictionary{IMessageChannel, TValue}"/> keyed by channel.<br/>
        ///     This is the same instance as <see cref="DiscordComparers.ChannelComparer"/>.
        /// </summary>
        /// <example>
        ///     <code language="c#">
        ///         private readonly ConcurrentDictionary&lt;IMessageChannel, MyData&gt; _data
        ///             = new ConcurrentDictionary&lt;IMessageChannel, MyData&gt;(MessageChannelComparer);
        ///     </code>
        /// </example>
        protected static IEqualityComparer<IMessageChannel> MessageChannelComparer { get; } = DiscordComparers.ChannelComparer;

        private readonly ConcurrentDictionary<IMessageChannel, PersistentGameData> _dataList
            = new(MessageChannelComparer);

        /// <summary>
        ///     Logger function.
        /// </summary>
        protected Func<LogMessage, Task> Logger { get; }

        private readonly BaseSocketClient _client;
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
            BaseSocketClient client,
            IMpGameServiceConfig? mpconfig = null,
            Func<LogMessage, Task>? logger = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _mpconfig = mpconfig ??
#if !NETCOREAPP3_1_OR_GREATER
                IMpGameServiceConfig.Default;
#else
                IMpGameServiceConfig.DefaultConfig.Instance;
#endif
            Logger = logger ?? Extensions.NoOpLogger;
            Logger(new LogMessage(LogSeverity.Debug, LogSource, _mpconfig.LogStrings.LogRegistration(_gameName)));

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
        public async Task<bool> OpenNewGameAsync(ICommandContext context)
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
        public async Task<bool> AddUserAsync(IMessageChannel channel, IUser user)
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
        /// <example>
        ///   <code language="c#">
        ///     if (await GameService.RemoveUserAsync(Context.Channel, user).ConfigureAwait(false))
        ///         await ReplyAsync($"**{user.Username}** has been kicked.").ConfigureAwait(false);
        ///   </code>
        /// </example>
        public async Task<bool> RemoveUserAsync(IMessageChannel channel, IUser user)
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
        /// <example>
        ///   <code language="c#">
        ///     if (await GameService.AddPlayerAsync(Game, new MyPlayer(Context.User, Context.Channel)).ConfigureAwait(false))
        ///         await ReplyAsync($"**{Context.User.Username}** has joined.").ConfigureAwait(false);
        ///   </code>
        /// </example>
        public async Task<bool> AddPlayerAsync(TGame game, TPlayer player)
        {
            if (!TryGetGameChannel(await player.User.GetOrCreateDMChannelAsync().ConfigureAwait(false), out var _))
            {
                await PrepPlayerAsync(game, player, addedInOngoig: true).ConfigureAwait(false);
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
        public Task<bool> KickPlayerAsync(TGame game, TPlayer player)
            => RemovePlayerAsync(game, player, _mpconfig.LogStrings.PlayerKicked(player.User));

        /// <summary>
        ///     Cancel a game that has not yet started.
        /// </summary>
        /// <param name="channel">
        ///     Public facing channel of this game.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the operation succeeded, otherwise <see langword="false"/>.
        /// </returns>
        public Task<bool> CancelGameAsync(IMessageChannel channel)
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
        public async Task<bool> TryAddNewGameAsync(IMessageChannel channel, TGame game)
        {
            if (_dataList.TryGetValue(channel, out var data))
            {
                await Logger(new LogMessage(LogSeverity.Verbose, LogSource,
                    _mpconfig.LogStrings.SettingGame(channel, _gameName))).ConfigureAwait(false);

                var gameSet = data.SetGame(game);
                if (gameSet)
                {
                    foreach (var player in game.Players)
                    {
                        await PrepPlayerAsync(game, player, addedInOngoig: false).ConfigureAwait(false);
                    }
                    game.GameEnd = (c => OnGameEnd(c));
                }
                return gameSet;
            }
            return false;
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
        /// <returns>
        ///     <see langword="true"/> if the value was updated, otherwise <see langword="false"/>.
        /// </returns>
        public bool TryUpdateOpenToJoin(IMessageChannel channel, bool newValue)
            => TryGetPersistentData(channel, out var data)
                && data.TryUpdateOpenToJoin(!newValue, newValue);

        /// <summary> 
        ///     Retrieve the game instance being played, if any.
        /// </summary>
        /// <param name="channel">
        ///     A message channel. Can be both the public-facing channel or the DM channel of one of the players.
        /// </param>
        /// <returns>
        ///     The <typeparamref name="TGame"/> instance being played in the specified channel,
        ///     or that the user is playing in, or <see langword="null"/> if there is none.
        /// </returns>
        public TGame? GetGameFromChannel(IMessageChannel channel)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dmChannel"></param>
        /// <param name="publicChannel"></param>
        /// <returns></returns>
        protected static bool TryGetGameChannel(IDMChannel dmChannel, [NotNullWhen(true)] out IMessageChannel? publicChannel)
            => GameTracker.Instance.TryGetGameChannel(dmChannel, out publicChannel);

        private bool TryGetPersistentData(IMessageChannel channel, [NotNullWhen(true)] out PersistentGameData? data)
        {
            var chan = (channel is IDMChannel dm && TryGetGameChannel(dm, out var pubc))
                ? pubc : channel;

            return _dataList.TryGetValue(chan, out data);
        }

        private async Task PrepPlayerAsync(TGame game, TPlayer player, bool addedInOngoig)
        {
            player.AutoKickCallback = (async reason => await RemovePlayerAsync(game, player, reason).ConfigureAwait(false));
            player.DMsDisabledMessage = _mpconfig.LogStrings.DMsDisabledMessage(player.User);
            player.DMsDisabledKickMessage = _mpconfig.LogStrings.DMsDisabledKickMessage(player.User);

            if (addedInOngoig)
            {
                await game.OnPlayerAdded(player).ConfigureAwait(false);
            }
        }

        private async Task<bool> OnGameEnd(IMessageChannel channel)
        {
            if (_dataList.TryRemove(channel, out var data))
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
                return true;
            }
            return false;
        }

        private Task CheckDestroyedChannel(SocketChannel channel)
        {
            return (channel is IMessageChannel msgChannel)
                ? OnGameEnd(msgChannel)
                : Task.CompletedTask;
        }

        private static async Task<bool> RemovePlayerAsync(
            TGame game, TPlayer player, string reason)
        {
            var success = game.Players.RemoveItem(player);
            if (success)
            {
                await game.OnPlayerKicked(player).ConfigureAwait(false);
                GameTracker.Instance.TryRemoveGameChannel(await player.User.GetOrCreateDMChannelAsync().ConfigureAwait(false));
                await game.Channel.SendMessageAsync(reason).ConfigureAwait(false);
            }
            return success;
        }

        //micro-optimization
        private static readonly string _gameName = typeof(TGame).Name;
        private static readonly string _gameFullName = typeof(TGame).FullName!;
        internal string GameName => _gameFullName;

        /// <summary>
        ///     
        /// </summary>
        protected virtual void Dispose() { }
        void IDisposable.Dispose()
        {
            _client.ChannelDestroyed -= CheckDestroyedChannel;
            Dispose();
        }
    }

    /// <summary>
    ///     Service managing games for <see cref="MpGameModuleBase{TService, TGame, TPlayer}"/>
    ///     with the default <see cref="Player"/> type.
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
            IMpGameServiceConfig? mpconfig = null,
            Func<LogMessage, Task>? logger = null)
            : base(client, mpconfig, logger) { }
    }
}
