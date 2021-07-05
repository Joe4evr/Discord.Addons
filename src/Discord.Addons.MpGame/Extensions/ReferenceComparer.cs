using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Discord.Addons.MpGame
{
    internal sealed class ReferenceComparer<T> : IEqualityComparer<T>
    {
        public static IEqualityComparer<T> Instance { get; } = new ReferenceComparer<T>();

        private ReferenceComparer() { }

        bool IEqualityComparer<T>.Equals([AllowNull] T x, [AllowNull] T y) 
            => ReferenceEquals(x, y);

        int IEqualityComparer<T>.GetHashCode(T obj)
            => obj?.GetHashCode() ?? 0;
    }
}
