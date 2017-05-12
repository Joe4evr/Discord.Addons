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

        internal bool OpenToJoin { get; private set; } = true;
        internal TGame Game { get; private set; }
        internal ImmutableHashSet<IUser> JoinedUsers { get; private set; } = ImmutableHashSet.Create(Comparers.UserComparer);

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
            JoinedUsers = ImmutableHashSet.Create(UserComparer);
        }

        internal bool TryAddUser(IUser user)
        {
            lock (_lock)
            {
                var builder = JoinedUsers.ToBuilder();
                bool r = builder.Add(user);
                if (r)
                {
                    JoinedUsers = builder.ToImmutable();
                }
                return r;
            }
        }

        internal bool TryRemoveUser(IUser user)
        {
            lock (_lock)
            {
                var builder = JoinedUsers.ToBuilder();
                bool r = builder.Remove(user);
                if (r)
                {
                    JoinedUsers = builder.ToImmutable();
                }
                return r;
            }
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
