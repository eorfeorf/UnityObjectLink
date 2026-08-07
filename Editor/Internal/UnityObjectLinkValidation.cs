using System;
using System.Text.RegularExpressions;

namespace UnityObjectLink
{
    internal static class UnityObjectLinkValidation
    {
        private static readonly Regex SchemePattern = new Regex("^[A-Za-z][A-Za-z0-9+.-]{0,31}$", RegexOptions.CultureInvariant);
        private static readonly Regex ProjectIdPattern = new Regex("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.CultureInvariant);

        internal static bool TryNormalizeScheme(string value, out string normalized, out string error)
        {
            normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (!SchemePattern.IsMatch(normalized))
            {
                error = "Scheme must follow RFC 3986 syntax and be 1 to 32 ASCII characters.";
                return false;
            }

            error = null;
            return true;
        }

        internal static bool TryNormalizeProjectId(string value, out string normalized, out string error)
        {
            normalized = (value ?? string.Empty).Trim();
            if (!ProjectIdPattern.IsMatch(normalized) || normalized == "." || normalized == ".." || normalized.IndexOf("..", StringComparison.Ordinal) >= 0)
            {
                error = "Project ID must be 1 to 64 ASCII letters, digits, dots, underscores, or hyphens, and cannot contain '..'.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
