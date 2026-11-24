using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AppWrapper
{
    /// <summary>
    /// Orders mod version numbers part by part, so 1.10 comes after 1.9 and 2.0.1 after 2.0.
    /// Missing parts count as zero, so 1, 1.0 and 1.00 are the same version.
    /// </summary>
    public static class ModVersionComparer
    {
        public static bool TryParse(string version, out int[] parts)
        {
            parts = null;
            if (string.IsNullOrWhiteSpace(version))
                return false;

            string[] pieces = version.Trim().Replace(',', '.').Split('.');
            var parsed = new List<int>();
            foreach (string piece in pieces)
            {
                if (
                    !int.TryParse(
                        piece,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out int value
                    )
                )
                    return false;
                parsed.Add(value);
            }

            parts = parsed.ToArray();
            return true;
        }

        /// <summary>Text of a version as written in mod.xml, keeping trailing zeros (1.10).</summary>
        public static string ToText(decimal version)
        {
            return version.ToString(CultureInfo.InvariantCulture);
        }

        public static int Compare(int[] a, int[] b)
        {
            int length = Math.Max(a.Length, b.Length);
            for (int i = 0; i < length; i++)
            {
                int x = i < a.Length ? a[i] : 0;
                int y = i < b.Length ? b[i] : 0;
                if (x != y)
                    return x.CompareTo(y);
            }
            return 0;
        }

        /// <summary>
        /// True when the version is within the bounds. A null or unreadable bound is ignored;
        /// an unreadable version is never within a bound that is set.
        /// </summary>
        public static bool IsWithin(string version, string minVersion, string maxVersion)
        {
            bool hasMin = TryParse(minVersion, out int[] min);
            bool hasMax = TryParse(maxVersion, out int[] max);
            if (!hasMin && !hasMax)
                return true;

            if (!TryParse(version, out int[] v))
                return false;

            return (!hasMin || Compare(v, min) >= 0) && (!hasMax || Compare(v, max) <= 0);
        }

        public static bool IsWithin(decimal version, string minVersion, string maxVersion)
        {
            return IsWithin(ToText(version), minVersion, maxVersion);
        }
    }
}
