using System;
using System.Collections.Generic;

namespace Discord.Addons.MpGame
{
    internal class EntityEqualityComparer<TId> : EqualityComparer<IEntity<TId>>
        where TId : IEquatable<TId>
    {
        public override bool Equals(IEntity<TId> x, IEntity<TId> y)
        {
            return x.Id.Equals(y.Id);
        }

        public override int GetHashCode(IEntity<TId> obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
