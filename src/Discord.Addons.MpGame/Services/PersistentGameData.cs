using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Core;
using Discord.Commands;

namespace Discord.Addons.MpGame
{
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

        public sealed class MpGameData
        {
            internal static MpGameData Default { get; } = new MpGameData();

            /// <summary>
            ///     Determines if a game in the current channel is open to join or not.
            /// </summary>
            public bool OpenToJoin { get; }

            /// <summary>
            ///     The instance of the game being played (if active).
            /// </summary>
            public TGame? Game { get; }

            /// <summary>
            ///     The player object that wraps the user executing this command
            ///     (if a game is active AND the user is a player in that game).
            /// </summary>
            public TPlayer? Player { get; }

            /// <summary>
            ///     Determines if a game in the current channel is in progress or not.
            /// </summary>
            public CurrentlyPlaying GameInProgress { get; }

            /// <summary>
            ///     The list of users ready to play.
            /// </summary>
            /// <remarks>
            ///     <note type="note">
            ///         This is an immutable snapshot, it is not updated until the
            ///         <i>next</i> time an instance of this class is created.
            ///     </note>
            /// </remarks>
            public IReadOnlyCollection<IUser> JoinedUsers { get; }

            private MpGameData()
            {
                OpenToJoin  = false;
                JoinedUsers = ImmutableHashSet<IUser>.Empty;
                Game        = null;
                Player      = null;
                GameInProgress = CurrentlyPlaying.None;
            }

            internal MpGameData(PersistentGameData data, ICommandContext context)
            {
                OpenToJoin  = data.OpenToJoin;
                JoinedUsers = data.JoinedUsers;
                Game        = data.Game;
                Player      = Game?.Players.SingleOrDefault(p => p.User.Id == context.User.Id);

                GameInProgress = GameTracker.Instance.TryGetGameString(context.Channel, out var name) switch
                {
                    true when (name == _gameFullName) => CurrentlyPlaying.ThisGame,
                    true => CurrentlyPlaying.DifferentGame,
                    false => CurrentlyPlaying.None
                };
            }
        }
    }
}
