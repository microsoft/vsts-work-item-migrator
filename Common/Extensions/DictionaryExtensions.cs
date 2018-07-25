using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public static class DictionaryExtensions
    {
        public static bool ContainsKeyIgnoringCase<T>(this IDictionary<string, T> dictionary, string desiredKeyOfAnyCase)
        {
            return GetKeyIgnoringCase(dictionary, desiredKeyOfAnyCase) != null;
        }

        public static string GetKeyIgnoringCase<T>(this IDictionary<string, T> dictionary, string desiredKeyOfAnyCase)
        {
            return dictionary.FirstOrDefault(a => a.Key.Equals(desiredKeyOfAnyCase, StringComparison.OrdinalIgnoreCase)).Key;
        }

        public static bool TryGetValueIgnoringCase<T>(this IDictionary<string, T> dictionary, string desiredKeyOfAnyCase, out T value) 
        {
            var key = GetKeyIgnoringCase(dictionary, desiredKeyOfAnyCase);
            if (key != null)
            {
                return dictionary.TryGetValue(key, out value);
            }
            else
            {
                value = default(T);
                return false;
            }
        }

        public static void AddRange<T>(this IDictionary<string, T> dictionary, IDictionary<string, T> entries)
        {
            foreach (var entry in entries)
            {
                dictionary[entry.Key] = entry.Value;
            }
        }
    }
}
