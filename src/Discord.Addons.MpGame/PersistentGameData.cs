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
            internal bool OpenToJoin => _openToJoin > 0;
            private int _openToJoin = 0;

            internal TGame Game => _game;
            private TGame _game;

            internal ImmutableHashSet<IUser> JoinedUsers => _builder.ToImmutable();
            private ImmutableHashSet<IUser>.Builder _builder = ImmutableHashSet.CreateBuilder<IUser>(Comparers.UserComparer);

            private readonly IMessageChannel _channel;

            public PersistentGameData(IMessageChannel channel)
            {
                _channel = channel;
            }

            internal bool TryUpdateOpenToJoin(bool oldValue, bool newValue)
            {
                var oldInt = oldValue ? 1 : 0;
                var newInt = newValue ? 1 : 0;
                return (Interlocked.CompareExchange(ref _openToJoin, value: newInt, comparand: oldInt) == oldInt);
            }

            internal void NewPlayerList()
            {
                _builder = ImmutableHashSet.CreateBuilder<IUser>(Comparers.UserComparer);
            }

            internal async Task<bool> TryAddUser(IUser user)
            {
                return GameTracker.Instance.TryAddGameChannel(await user.GetOrCreateDMChannelAsync(), _channel)
                    && _builder.Add(user);
            }

            internal async Task<bool> TryRemoveUser(IUser user)
            {
                return GameTracker.Instance.TryRemoveGameChannel(await user.GetOrCreateDMChannelAsync())
                    && _builder.Remove(user);
            }

            internal bool SetGame(TGame game)
            {
                return (Interlocked.CompareExchange(ref _game, value: game, comparand: null) == null);
            }
        }
    }
}
