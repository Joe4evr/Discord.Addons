using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Discord.Addons.MpGame
{
    internal sealed class PersistentGameData<TGame, TPlayer>
        where TGame : GameBase<TPlayer>
        where TPlayer : Player
    {
        private readonly object _lock = new object();

        internal bool OpenToJoin { get; private set; } = false;
        internal TGame Game { get; private set; }
        internal ImmutableHashSet<IUser> JoinedUsers => _builder.ToImmutable();
        private ImmutableHashSet<IUser>.Builder _builder = ImmutableHashSet.CreateBuilder<IUser>(Comparers.UserComparer);

        internal bool TryUpdateOpenToJoin(bool oldValue, bool newValue)
        {
            lock (_lock)
            {
                if (OpenToJoin == oldValue)
                {
                    OpenToJoin = newValue;
                    return true;
                }
                else
                {
                    return false;
                }
            }
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
            lock (_lock)
            {
                if (Game == null)
                {
                    Game = game;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
