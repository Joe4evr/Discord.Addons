using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Addons
{
    internal static class Extensions
    {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue val)
        {
            key = kvp.Key;
            val = kvp.Value;
        }
    }
}
