using System;
using System.Collections.Generic;

namespace Discord.Addons.MpGame
{
    internal static class MpGameComparers
    {
        public static IEqualityComparer<Player> PlayerComparer { get; } = new PlayerEqualityComparer();

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
