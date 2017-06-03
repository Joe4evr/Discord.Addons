using System;
using System.Collections.Generic;

namespace Discord.Addons.MpGame
{
    internal static class Comparers
    {
        public static IEqualityComparer<IUser> UserComparer                     { get; } = new EntityEqualityComparer<IUser, ulong>();
        public static IEqualityComparer<IGuild> GuildComparer                   { get; } = new EntityEqualityComparer<IGuild, ulong>();
        public static IEqualityComparer<IChannel> ChannelComparer               { get; } = new EntityEqualityComparer<IChannel, ulong>();
        public static IEqualityComparer<IRole> RoleComparer                     { get; } = new EntityEqualityComparer<IRole, ulong>();
        public static IEqualityComparer<IMessageChannel> MessageChannelComparer { get; } = ChannelComparer;

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
