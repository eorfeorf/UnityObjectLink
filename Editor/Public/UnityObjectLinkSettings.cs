using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UnityObjectLink
{
    [FilePath("ProjectSettings/UnityObjectLinkSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class UnityObjectLinkSettings : ScriptableSingleton<UnityObjectLinkSettings>
    {
        public const string DefaultScheme = "unity-object-link";

        [SerializeField] private string scheme = DefaultScheme;
        [SerializeField] private string projectId = "";
        [SerializeField] private string previousScheme = "";

        public string Scheme
        {
            get
            {
                EnsureInitialized();
                return scheme;
            }
        }

        public string ProjectId
        {
            get
            {
                EnsureInitialized();
                return projectId;
            }
        }

        public string PreviousScheme { get { return previousScheme; } }

        public bool TryUpdate(string newScheme, string newProjectId, out string error)
        {
            string normalizedScheme;
            string normalizedProjectId;
            if (!UnityObjectLinkValidation.TryNormalizeScheme(newScheme, out normalizedScheme, out error) ||
                !UnityObjectLinkValidation.TryNormalizeProjectId(newProjectId, out normalizedProjectId, out error))
            {
                return false;
            }

            bool schemeChanged = !string.Equals(scheme, normalizedScheme, StringComparison.OrdinalIgnoreCase);
            if (schemeChanged &&
                !string.IsNullOrEmpty(previousScheme) &&
                !string.Equals(previousScheme, normalizedScheme, StringComparison.OrdinalIgnoreCase))
            {
                error = "Unregister the previous scheme '" + previousScheme + "' before changing the URI scheme again.";
                return false;
            }

            if (schemeChanged)
            {
                previousScheme = scheme;
            }

            scheme = normalizedScheme;
            projectId = normalizedProjectId;
            Save(true);
            return true;
        }

        internal void ClearPreviousScheme(string value)
        {
            if (string.Equals(previousScheme, value, StringComparison.OrdinalIgnoreCase))
            {
                previousScheme = string.Empty;
                Save(true);
            }
        }

        internal void RestoreSerializedState(string restoredScheme, string restoredProjectId, string restoredPreviousScheme)
        {
            scheme = restoredScheme;
            projectId = restoredProjectId;
            previousScheme = restoredPreviousScheme;
            Save(true);
        }

        internal void EnsureInitialized()
        {
            bool changed = false;
            string normalized;
            string error;
            if (!UnityObjectLinkValidation.TryNormalizeScheme(scheme, out normalized, out error))
            {
                scheme = DefaultScheme;
                changed = true;
            }
            else if (scheme != normalized)
            {
                scheme = normalized;
                changed = true;
            }

            if (!UnityObjectLinkValidation.TryNormalizeProjectId(projectId, out normalized, out error))
            {
                projectId = CreateInitialProjectId();
                changed = true;
            }
            else if (projectId != normalized)
            {
                projectId = normalized;
                changed = true;
            }

            if (changed)
            {
                Save(true);
            }
        }

        private static string CreateInitialProjectId()
        {
            string projectName = new DirectoryInfo(Path.GetDirectoryName(Application.dataPath) ?? "unity-project").Name;
            return CreateProjectId(projectName, Guid.NewGuid().ToString("N").Substring(0, 8));
        }

        internal static string CreateProjectId(string projectName, string uniqueSuffix)
        {
            var builder = new StringBuilder();
            foreach (char character in (projectName ?? string.Empty).ToLowerInvariant())
            {
                if ((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9'))
                {
                    builder.Append(character);
                }
                else if (builder.Length > 0 && builder[builder.Length - 1] != '-')
                {
                    builder.Append('-');
                }

                if (builder.Length == 40)
                {
                    break;
                }
            }

            string prefix = builder.ToString().Trim('-', '.', '_');
            if (string.IsNullOrEmpty(prefix))
            {
                prefix = "unity-project";
            }

            return prefix + "-" + uniqueSuffix;
        }
    }
}
