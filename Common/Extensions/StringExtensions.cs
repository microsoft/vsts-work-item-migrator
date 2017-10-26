using System.Collections.Generic;
using Microsoft.VisualStudio.Services.Common;

namespace Common
{
    public static class StringExtensions
    {
        public static ISet<string> SplitBySemicolonToHashSet(this string input)
        {
            string[] parts = input.Split(';');
            return parts.ToHashSet();
        }
    }
}
