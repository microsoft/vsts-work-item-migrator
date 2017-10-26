using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Common
{
    public static class ConcurrentDictionaryExtensions
    {
        public static bool ContainsKeyIgnoringCase(this ConcurrentDictionary<string, WorkItemField> dictionary, string desiredKeyOfAnyCase)
        {
            return GetKeyIgnoringCase(dictionary, desiredKeyOfAnyCase) != null;
        }

        public static string GetKeyIgnoringCase(this ConcurrentDictionary<string, WorkItemField> dictionary, string desiredKeyOfAnyCase)
        {
            return dictionary.FirstOrDefault(a => a.Key.Equals(desiredKeyOfAnyCase, StringComparison.OrdinalIgnoreCase)).Key;
        }

        public static bool TryGetValueIgnoringCase(this ConcurrentDictionary<string, WorkItemField> dictionary, string desiredKeyOfAnyCase, out WorkItemField value)
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
