using System;
using System.Collections.Generic;

namespace Discord.Addons.MpGame
{
    internal static class Comparers
    {
        public static IEqualityComparer<IUser> UserComparer         => _userComparer.Value;
        public static IEqualityComparer<IGuild> GuildComparer       => _guildComparer.Value;
        public static IEqualityComparer<IChannel> ChannelComparer   => _channelComparer.Value;
        public static IEqualityComparer<IRole> RoleComparer         => _roleComparer.Value;
        //public static IEqualityComparer<IMessageChannel> MessageChannelComparer { get; } = ChannelComparer;

        private static readonly Lazy<IEqualityComparer<IUser>> _userComparer = new Lazy<IEqualityComparer<IUser>>(() => new EntityEqualityComparer<IUser, ulong>());
        private static readonly Lazy<IEqualityComparer<IGuild>> _guildComparer = new Lazy<IEqualityComparer<IGuild>>(() => new EntityEqualityComparer<IGuild, ulong>());
        private static readonly Lazy<IEqualityComparer<IChannel>> _channelComparer = new Lazy<IEqualityComparer<IChannel>>(() => new EntityEqualityComparer<IChannel, ulong>());
        private static readonly Lazy<IEqualityComparer<IRole>> _roleComparer = new Lazy<IEqualityComparer<IRole>>(() => new EntityEqualityComparer<IRole, ulong>());

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

        public static IEqualityComparer<Player> PlayerComparer      => _playerComparer.Value;

        private static readonly Lazy<IEqualityComparer<Player>> _playerComparer = new Lazy<IEqualityComparer<Player>>(() => new PlayerEqualityComparer());

        private sealed class PlayerEqualityComparer : EqualityComparer<Player>
        {
            public override bool Equals(Player x, Player y)
            {
                return x?.User.Id == y?.User.Id;
            }

            public override int GetHashCode(Player obj)
            {
                return obj.User.Id.GetHashCode();
            }
        }
    }
}
