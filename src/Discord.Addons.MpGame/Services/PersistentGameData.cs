using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Discord.Addons.MpGame;

public partial class MpGameService<TGame, TPlayer>
{
    internal sealed class PersistentGameData
    {
        private readonly MpGameService<TGame, TPlayer> _service;

        internal bool OpenToJoin => _openToJoin > 0;
        private int _openToJoin = 0;

        internal TGame? Game => _game;
        private TGame? _game;

        internal ImmutableHashSet<IUser> JoinedUsers => _builder.ToImmutable();
        private readonly ImmutableHashSet<IUser>.Builder _builder = ImmutableHashSet.CreateBuilder<IUser>(DiscordComparers.UserComparer);

        private readonly IMessageChannel _channel;

        internal IUser GameOrganizer { get; }

        public PersistentGameData(IMessageChannel channel, IUser organizer, MpGameService<TGame, TPlayer> service)
        {
            _channel = channel;
            _service = service;
            GameOrganizer = organizer;
        }

        internal bool TryUpdateOpenToJoin(bool oldValue, bool newValue)
        {
            var oldInt = oldValue ? 1 : 0;
            var newInt = newValue ? 1 : 0;
            return (Interlocked.CompareExchange(ref _openToJoin, value: newInt, comparand: oldInt) == oldInt);
        }

        internal async Task<bool> TryAddUser(IUser user)
        {
            if (!user.IsBot)
            {
                var dmchannel = await user.GetOrCreateDMChannelAsync().ConfigureAwait(false);
                await _service.Logger(new LogMessage(LogSeverity.Debug, LogSource, $"Adding DM channel #{dmchannel.Id}")).ConfigureAwait(false);

                return GameTracker.Instance.TryAddGameChannel(dmchannel, _channel)
                    && _builder.Add(user);
            }
            else
            {
                return _builder.Add(user);
            }
        }

        internal async Task<bool> TryRemoveUser(IUser user)
        {
            var dmchannel = await user.GetOrCreateDMChannelAsync().ConfigureAwait(false);
            await _service.Logger(new LogMessage(LogSeverity.Debug, LogSource, $"Removing DM channel #{dmchannel.Id}")).ConfigureAwait(false);
            return GameTracker.Instance.TryRemoveGameChannel(dmchannel)
                && _builder.Remove(user);
        }

        internal bool SetGame(TGame game)
        {
            return (Interlocked.CompareExchange(ref _game, value: game, comparand: null) is null);
        }
    }

    /// <summary>
    ///     Contains metadata about a game given a command context.
    /// </summary>
    public sealed class MpGameData : IMpGameData
    {
        internal static MpGameData Default { get; } = new MpGameData();

        /// <inheritdoc/>
        public bool OpenToJoin { get; private set; }

        /// <summary>
        ///     The instance of the game being played (if active).
        /// </summary>
        public TGame? Game { get; private set; }

        /// <summary>
        ///     The player object that wraps the user executing this command
        ///     (if a game is active AND the user is a player in that game).
        /// </summary>
        public TPlayer? Player { get; private set; }

        /// <inheritdoc/>
        public CurrentlyPlaying GameInProgress { get; private set; }

        /// <inheritdoc/>
        /// <remarks>
        ///     <note type="note">
        ///         This is an immutable snapshot, it is not updated until the
        ///         <i>next</i> time an instance of this class is created.
        ///     </note>
        /// </remarks>
        public IReadOnlyCollection<IUser> JoinedUsers { get; private set; }

        private MpGameData()
        {
            OpenToJoin = false;
            JoinedUsers = ImmutableHashSet<IUser>.Empty;
            Game = null;
            Player = null;
            GameInProgress = CurrentlyPlaying.None;
        }

        internal MpGameData(PersistentGameData data, ICommandContext context)
        {
            OpenToJoin = data.OpenToJoin;
            JoinedUsers = data.JoinedUsers;
            Game = data.Game;
            Player = Game?.Players.SingleOrDefault(p => p.User.Id == context.User.Id);

            GameInProgress = GameTracker.Instance.TryGetGameString(context.Channel, out var name) switch
            {
                true when (name == _gameFullName) => CurrentlyPlaying.ThisGame,
                true => CurrentlyPlaying.DifferentGame,
                false => CurrentlyPlaying.None
            };
        }

        /// <inheritdoc/>
        ulong? IMpGameData.PlayerUserId => Player?.User.Id;
        /// <inheritdoc/>
        ulong? IMpGameData.GameChannelId => Game?.Channel.Id;
        /// <inheritdoc/>
        IReadOnlyCollection<ulong> IMpGameData.JoinedUsers => JoinedUsers.Select(u => u.Id).ToImmutableArray();

        [return: NotNullIfNotNull("other")]
        internal static MpGameData? CopyFrom(IMpGameData? other, TGame? game, BaseSocketClient client)
        {
            if (other is null) return null;

            if (other is not MpGameData data)
            {
                data = new MpGameData
                {
                    OpenToJoin = other.OpenToJoin,
                    Game = game,
                    Player = game?.Players.SingleOrDefault(p => p.User.Id == other.PlayerUserId),
                    GameInProgress = other.GameInProgress,
                    JoinedUsers = other.JoinedUsers.Select(id => client.GetUser(id)).ToImmutableArray()
                };
            }
            return data;
        }
    }
}
