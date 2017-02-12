using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Addons.TriviaGames
{
    internal static class Extensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            return dictionary.TryGetValue(key, out var ret) ? ret : defaultValue;
        }
    }
}
