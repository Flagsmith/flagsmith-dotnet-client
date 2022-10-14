using System;
using System.Collections.Generic;

namespace Flagsmith.Extensions
{
    internal static class DictionaryExtensions
    {
        public static void ForEach<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> keyValuePairs, Action<KeyValuePair<TKey, TValue>> action)
        {
            foreach (var kvp in keyValuePairs)
            {
                action(kvp);
            }
        }
    }
}
