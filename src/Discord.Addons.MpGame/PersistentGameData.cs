using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Discord.Addons.Core;

namespace Discord.Addons.MpGame
{
    internal sealed class PersistentGameData<TGame, TPlayer>
        where TGame : GameBase<TPlayer>
        where TPlayer : Player
    {
        internal bool OpenToJoin { get => _openToJoin > 0; }
        private int _openToJoin = 0;

        internal TGame Game { get => _game; }
        private TGame _game;

        internal ImmutableHashSet<IUser> JoinedUsers => _builder.ToImmutable();
        private ImmutableHashSet<IUser>.Builder _builder = ImmutableHashSet.CreateBuilder<IUser>(Comparers.UserComparer);

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

        internal bool TryAddUser(IUser user)
        {
            return _builder.Add(user);
        }

        internal bool TryRemoveUser(IUser user)
        {
            return _builder.Remove(user);
        }

        internal bool SetGame(TGame game)
        {
            return (Interlocked.CompareExchange(ref _game, value: game, comparand: null) == null);
        }
    }
}
