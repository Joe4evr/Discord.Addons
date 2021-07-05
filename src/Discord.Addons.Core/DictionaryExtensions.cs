using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Discord.Addons.Core
{
    internal static class DictionaryExtensions
    {
        [return: MaybeNull]
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary, TKey key,
            [AllowNull] TValue defaultValue = default)
            where TKey : notnull
        {
            return dictionary.TryGetValue(key, out var ret) ? ret : defaultValue;
        }

        [return: MaybeNull]
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key,
            [AllowNull] TValue defaultValue = default)
            where TKey : notnull
        {
            return dictionary.TryGetValue(key, out var ret) ? ret : defaultValue;
        }

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue val)
        {
            key = kvp.Key;
            val = kvp.Value;
        }
    }
}
