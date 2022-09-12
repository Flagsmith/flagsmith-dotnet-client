using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Flagsmith.Extensions
{
    internal static class DictionaryExtensions
    {
        public static void ForEach<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> keyValuePairs, Action<KeyValuePair<TKey, TValue>> action)
        {
            foreach (var kvp in keyValuePairs)
                action(kvp);
        }

        public static void ForEach<TValue>(this IEnumerable<TValue> values, Action<TValue> action)
        {
            foreach (var kvp in values)
                action(kvp);
        }

        public static async Task ForEachAsync<TValue>(this IEnumerable<TValue> values, Func<TValue, Task> action)
        {
            foreach (var kvp in values)
                await action(kvp).ConfigureAwait(false);
        }
    }
}
