using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Discord.Addons.SimplePermissions
{
    internal static class Converter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong LongToUlong(long l)
        {
            return BitConverter.ToUInt64(BitConverter.GetBytes(l), 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long UlongToLong(ulong ul)
        {
            return BitConverter.ToInt64(BitConverter.GetBytes(ul), 0);
        }
    }
}
