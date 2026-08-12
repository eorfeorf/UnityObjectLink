using System;
using System.Collections.Generic;

namespace UnityObjectLink
{
    public sealed class UnityObjectLinkUri
    {
        public const int CurrentVersion = 1;
        public const int MaximumUriLength = 8192;

        public int Version { get; private set; }
        public string Scheme { get; private set; }
        public string ProjectId { get; private set; }
        public string GlobalObjectId { get; private set; }

        private UnityObjectLinkUri(string scheme, string projectId, string globalObjectId)
        {
            Version = CurrentVersion;
            Scheme = scheme;
            ProjectId = projectId;
            GlobalObjectId = globalObjectId;
        }

        public static bool TryCreate(string scheme, string projectId, string globalObjectId, out UnityObjectLinkUri link, out string error)
        {
            link = null;
            string normalizedScheme;
            string normalizedProjectId;
            if (!UnityObjectLinkValidation.TryNormalizeScheme(scheme, out normalizedScheme, out error) ||
                !UnityObjectLinkValidation.TryNormalizeProjectId(projectId, out normalizedProjectId, out error))
            {
                return false;
            }

            if (!TryValidateGlobalObjectIdText(globalObjectId, out error))
            {
                return false;
            }

            link = new UnityObjectLinkUri(normalizedScheme, normalizedProjectId, globalObjectId);
            error = null;
            return true;
        }

        public static bool TryParse(string rawUri, string expectedScheme, string expectedProjectId, out UnityObjectLinkUri link, out string error)
        {
            link = null;
            error = null;

            if (string.IsNullOrEmpty(rawUri) || rawUri.Length > MaximumUriLength || ContainsControlCharacter(rawUri))
            {
                error = "The URI is empty, too long, or contains control characters.";
                return false;
            }

            Uri parsed;
            if (!Uri.TryCreate(rawUri, UriKind.Absolute, out parsed) ||
                !string.Equals(parsed.Host, "select", StringComparison.OrdinalIgnoreCase) ||
                (parsed.AbsolutePath != string.Empty && parsed.AbsolutePath != "/") ||
                !string.IsNullOrEmpty(parsed.Fragment) ||
                !string.IsNullOrEmpty(parsed.UserInfo) ||
                !parsed.IsDefaultPort)
            {
                error = "The URI must use the form <scheme>://select?... without a path, fragment, user info, or port.";
                return false;
            }

            string normalizedScheme;
            if (!UnityObjectLinkValidation.TryNormalizeScheme(parsed.Scheme, out normalizedScheme, out error))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(expectedScheme) && !string.Equals(normalizedScheme, expectedScheme, StringComparison.OrdinalIgnoreCase))
            {
                error = "The URI scheme does not match this project.";
                return false;
            }

            Dictionary<string, string> parameters;
            if (!TryParseQuery(parsed.Query, out parameters, out error))
            {
                return false;
            }

            string versionText;
            string projectIdText;
            string globalObjectIdText;
            if (parameters.Count != 3 ||
                !parameters.TryGetValue("v", out versionText) ||
                !parameters.TryGetValue("project", out projectIdText) ||
                !parameters.TryGetValue("object", out globalObjectIdText))
            {
                error = "The URI must contain exactly one v, project, and object parameter.";
                return false;
            }

            if (!string.Equals(versionText, CurrentVersion.ToString(), StringComparison.Ordinal))
            {
                error = "The URI version is not supported.";
                return false;
            }

            string normalizedProjectId;
            if (!UnityObjectLinkValidation.TryNormalizeProjectId(projectIdText, out normalizedProjectId, out error))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(expectedProjectId) && !string.Equals(normalizedProjectId, expectedProjectId, StringComparison.Ordinal))
            {
                error = "The URI is addressed to a different Unity project.";
                return false;
            }

            if (!TryValidateGlobalObjectIdText(globalObjectIdText, out error))
            {
                return false;
            }

            link = new UnityObjectLinkUri(normalizedScheme, normalizedProjectId, globalObjectIdText);
            return true;
        }

        public override string ToString()
        {
            return Scheme + "://select?v=" + CurrentVersion +
                   "&project=" + Uri.EscapeDataString(ProjectId) +
                   "&object=" + Uri.EscapeDataString(GlobalObjectId);
        }

        private static bool TryParseQuery(string query, out Dictionary<string, string> parameters, out string error)
        {
            parameters = new Dictionary<string, string>(StringComparer.Ordinal);
            error = null;
            string text = query.StartsWith("?", StringComparison.Ordinal) ? query.Substring(1) : query;
            if (string.IsNullOrEmpty(text))
            {
                error = "The URI query is missing.";
                return false;
            }

            string[] pairs = text.Split('&');
            foreach (string pair in pairs)
            {
                int separator = pair.IndexOf('=');
                if (separator <= 0 || separator != pair.LastIndexOf('='))
                {
                    error = "Every URI query parameter must contain one name and one value.";
                    return false;
                }

                string encodedName = pair.Substring(0, separator);
                string encodedValue = pair.Substring(separator + 1);
                if (!HasValidPercentEncoding(encodedName) || !HasValidPercentEncoding(encodedValue))
                {
                    error = "The URI contains invalid percent encoding.";
                    return false;
                }

                string name;
                string value;
                try
                {
                    name = Uri.UnescapeDataString(encodedName);
                    value = Uri.UnescapeDataString(encodedValue);
                }
                catch (UriFormatException)
                {
                    error = "The URI contains invalid escaped text.";
                    return false;
                }

                if (parameters.ContainsKey(name))
                {
                    error = "The URI contains a duplicate parameter.";
                    return false;
                }

                parameters.Add(name, value);
            }

            return true;
        }

        private static bool TryValidateGlobalObjectIdText(string text, out string error)
        {
            if (string.IsNullOrEmpty(text) || text.Length > 512 || ContainsControlCharacter(text) ||
                !text.StartsWith("GlobalObjectId_V1-", StringComparison.Ordinal))
            {
                error = "The object parameter is not a valid GlobalObjectId string.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool HasValidPercentEncoding(string text)
        {
            for (int index = 0; index < text.Length; index++)
            {
                if (text[index] != '%')
                {
                    continue;
                }

                if (index + 2 >= text.Length || !IsHex(text[index + 1]) || !IsHex(text[index + 2]))
                {
                    return false;
                }

                index += 2;
            }

            return true;
        }

        private static bool IsHex(char value)
        {
            return (value >= '0' && value <= '9') || (value >= 'a' && value <= 'f') || (value >= 'A' && value <= 'F');
        }

        private static bool ContainsControlCharacter(string value)
        {
            foreach (char character in value)
            {
                if (char.IsControl(character))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
