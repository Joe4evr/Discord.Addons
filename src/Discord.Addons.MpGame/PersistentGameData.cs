using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Core;

namespace Discord.Addons.MpGame
{
    public partial class MpGameService<TGame, TPlayer>
    {
        internal sealed class PersistentGameData
        {
            private readonly MpGameService<TGame, TPlayer> _service;

            internal bool OpenToJoin => _openToJoin > 0;
            private int _openToJoin = 0;

            internal TGame Game => _game;
            private TGame _game;

            internal ImmutableHashSet<IUser> JoinedUsers => _builder.ToImmutable();
            private ImmutableHashSet<IUser>.Builder _builder = ImmutableHashSet.CreateBuilder<IUser>(DiscordComparers.UserComparer);

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
                var dmchannel = await user.GetOrCreateDMChannelAsync().ConfigureAwait(false);
                await _service.Logger(new LogMessage(LogSeverity.Debug, "MpGame", $"Adding DM channel #{dmchannel.Id}")).ConfigureAwait(false);
                return GameTracker.Instance.TryAddGameChannel(dmchannel, _channel)
                    && _builder.Add(user);
            }

            internal async Task<bool> TryRemoveUser(IUser user)
            {
                var dmchannel = await user.GetOrCreateDMChannelAsync().ConfigureAwait(false);
                await _service.Logger(new LogMessage(LogSeverity.Debug, "MpGame", $"Removing DM channel #{dmchannel.Id}")).ConfigureAwait(false);
                return GameTracker.Instance.TryRemoveGameChannel(dmchannel)
                    && _builder.Remove(user);
            }

            internal bool SetGame(TGame game)
            {
                return (Interlocked.CompareExchange(ref _game, value: game, comparand: null) == null);
            }
        }
    }
}
