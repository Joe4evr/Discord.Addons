using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Discord.Addons.Core
{
    internal static class Comparers
    {
        public static IEqualityComparer<IUser>    UserComparer    => _userComparer    ?? Create<IUser   , ulong>(ref _userComparer);
        public static IEqualityComparer<IGuild>   GuildComparer   => _guildComparer   ?? Create<IGuild  , ulong>(ref _guildComparer);
        public static IEqualityComparer<IChannel> ChannelComparer => _channelComparer ?? Create<IChannel, ulong>(ref _channelComparer);
        public static IEqualityComparer<IRole>    RoleComparer    => _roleComparer    ?? Create<IRole   , ulong>(ref _roleComparer);

        private static IEqualityComparer<IUser>    _userComparer;
        private static IEqualityComparer<IGuild>   _guildComparer;
        private static IEqualityComparer<IChannel> _channelComparer;
        private static IEqualityComparer<IRole>    _roleComparer;

        private static IEqualityComparer<TEntity> Create<TEntity, TId>(ref IEqualityComparer<TEntity> field)
            where TEntity : IEntity<TId>
            where TId : IEquatable<TId>
        {
            return field = new EntityEqualityComparer<TEntity, TId>();
            //var value = new EntityEqualityComparer<TEntity, TId>();
            //return (Interlocked.CompareExchange(ref field, value, null) == null) ? value : field;
        }

        private sealed class EntityEqualityComparer<TEntity, TId> : EqualityComparer<TEntity>
            where TEntity : IEntity<TId>
            where TId : IEquatable<TId>
        {
            public override bool Equals(TEntity x, TEntity y)
            {
                return x.Id.Equals(y.Id);
            }

            public override int GetHashCode(TEntity obj)
            {
                return obj.Id.GetHashCode();
            }
        }
    }
}
