// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Ms.Configuration.Abstracts;
using System;
using System.Collections.Generic;

namespace Ms.Configuration
{
    public class ConfigurationKeyComparer : IComparer<string>
    {
        private static readonly string[] _keyDelimiterArray = new[] { ConfigurationPath.KeyDelimiter };

        public static ConfigurationKeyComparer Instance { get; } = new ConfigurationKeyComparer();

        internal static Comparison<string> Comparison { get; } = Instance.Compare;

        public int Compare(string x, string y)
        {
            string[] xParts = x?.Split(_keyDelimiterArray, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            string[] yParts = y?.Split(_keyDelimiterArray, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

            // Compare each part until we get two parts that are not equal
            for (int i = 0; i < Math.Min(xParts.Length, yParts.Length); i++)
            {
                x = xParts[i];
                y = yParts[i];

                int value1 = 0;
                int value2 = 0;

                bool xIsInt = x != null && int.TryParse(x, out value1);
                bool yIsInt = y != null && int.TryParse(y, out value2);

                int result;

                if (!xIsInt && !yIsInt)
                {
                    // Both are strings
                    result = string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
                }
                else if (xIsInt && yIsInt)
                {
                    // Both are int
                    result = value1 - value2;
                }
                else
                {
                    // Only one of them is int
                    result = xIsInt ? -1 : 1;
                }

                if (result != 0)
                {
                    // One of them is different
                    return result;
                }
            }

            // If we get here, the common parts are equal.
            // If they are of the same length, then they are totally identical
            return xParts.Length - yParts.Length;
        }
    }
}
