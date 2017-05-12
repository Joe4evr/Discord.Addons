using System;
using System.Collections.Generic;

namespace Discord.Addons.MpGame
{
    //internal class EntityEqualityComparer<TId> : EqualityComparer<IEntity<TId>>
    //    where TId : IEquatable<TId>
    //{
    //    public override bool Equals(IEntity<TId> x, IEntity<TId> y)
    //    {
    //        return x.Id.Equals(y.Id);
    //    }

    //    public override int GetHashCode(IEntity<TId> obj)
    //    {
    //        return obj.Id.GetHashCode();
    //    }
    //}

    internal static class Comparers
    {
        public static IEqualityComparer<IUser> UserComparer                     { get; } = new EntityEqualityComparer<IUser, ulong>();
        public static IEqualityComparer<IGuild> GuildComparer                   { get; } = new EntityEqualityComparer<IGuild, ulong>();
        public static IEqualityComparer<IChannel> ChannelComparer               { get; } = new EntityEqualityComparer<IChannel, ulong>();
        public static IEqualityComparer<IMessageChannel> MessageChannelComparer { get; } = new EntityEqualityComparer<IMessageChannel, ulong>();
        public static IEqualityComparer<IRole> RoleComparer                     { get; } = new EntityEqualityComparer<IRole, ulong>();

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
