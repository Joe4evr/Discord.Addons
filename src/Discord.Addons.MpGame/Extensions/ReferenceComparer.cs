using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Discord.Addons.MpGame
{
    internal sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public static IEqualityComparer<object> Instance { get; } = new ReferenceComparer();

        private ReferenceComparer() { }

        bool IEqualityComparer<object>.Equals([AllowNull] object x, [AllowNull] object y) 
            => ReferenceEquals(x, y);

        int IEqualityComparer<object>.GetHashCode(object obj)
            => obj?.GetHashCode() ?? 0;
    }
}
