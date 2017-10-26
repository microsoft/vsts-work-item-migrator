using System;
using System.Collections.Generic;
using System.Linq;
using Common.Config;

namespace Common
{
    public static class DictionaryExtensions
    {
        public static bool ContainsKeyIgnoringCase(this IDictionary<string, object> dictionary, string desiredKeyOfAnyCase)
        {
            return GetKeyIgnoringCase(dictionary, desiredKeyOfAnyCase) != null;
        }

        public static string GetKeyIgnoringCase(this IDictionary<string, object> dictionary, string desiredKeyOfAnyCase)
        {
            return dictionary.FirstOrDefault(a => a.Key.Equals(desiredKeyOfAnyCase, StringComparison.OrdinalIgnoreCase)).Key;
        }

        public static bool TryGetValueIgnoringCase(this IDictionary<string, object> dictionary, string desiredKeyOfAnyCase, out object value)
        {
            var key = GetKeyIgnoringCase(dictionary, desiredKeyOfAnyCase);
            if (key != null)
            {
                return dictionary.TryGetValue(key, out value);
            }
            else
            {
                value = null;
                return false;
            }
        }

        public static bool ContainsKeyIgnoringCase(this IDictionary<string, TargetFieldMap> dictionary, string desiredKeyOfAnyCase)
        {
            return GetKeyIgnoringCase(dictionary, desiredKeyOfAnyCase) != null;
        }

        public static string GetKeyIgnoringCase(this IDictionary<string, TargetFieldMap> dictionary, string desiredKeyOfAnyCase)
        {
            return dictionary.FirstOrDefault(a => a.Key.Equals(desiredKeyOfAnyCase, StringComparison.OrdinalIgnoreCase)).Key;
        }

        public static bool TryGetValueIgnoringCase(this IDictionary<string, TargetFieldMap> dictionary, string desiredKeyOfAnyCase, out TargetFieldMap value)
        {
            var key = GetKeyIgnoringCase(dictionary, desiredKeyOfAnyCase);
            if (key != null)
            {
                return dictionary.TryGetValue(key, out value);
            }
            else
            {
                value = null;
                return false;
            }
        }
    }
}
