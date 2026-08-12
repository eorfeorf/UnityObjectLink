using System;
using System.IO;
using System.Text;

namespace UnityObjectLink
{
    internal sealed class UnityObjectLinkStorage
    {
        internal const string ProductDirectoryName = "UnityObjectLink";

        private readonly string rootDirectory;

        internal UnityObjectLinkStorage(string rootDirectory)
        {
            this.rootDirectory = rootDirectory;
        }

        internal static UnityObjectLinkStorage CreateDefault()
        {
            string root;
#if UNITY_EDITOR_OSX
            root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", ProductDirectoryName);
#else
            root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProductDirectoryName);
#endif
            return new UnityObjectLinkStorage(root);
        }

        internal string RootDirectory { get { return rootDirectory; } }

        internal string GetInstanceDirectory(string scheme, string projectId)
        {
            string normalizedScheme;
            string normalizedProjectId;
            string error;
            if (!UnityObjectLinkValidation.TryNormalizeScheme(scheme, out normalizedScheme, out error) ||
                !UnityObjectLinkValidation.TryNormalizeProjectId(projectId, out normalizedProjectId, out error))
            {
                throw new ArgumentException(error);
            }

            return Path.Combine(rootDirectory, "instances", normalizedScheme, normalizedProjectId);
        }

        internal string GetHeartbeatPath(string scheme, string projectId)
        {
            return Path.Combine(GetInstanceDirectory(scheme, projectId), "heartbeat.json");
        }

        internal string GetInboxDirectory(string scheme, string projectId)
        {
            return Path.Combine(GetInstanceDirectory(scheme, projectId), "inbox");
        }

        internal static void WriteAllTextAtomic(string path, string contents)
        {
            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("The destination has no parent directory.", "path");
            }

            Directory.CreateDirectory(directory);
            string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                var encoding = new UTF8Encoding(false, true);
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                using (var writer = new StreamWriter(stream, encoding))
                {
                    writer.Write(contents);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(temporaryPath, path, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Delete(path);
                        File.Move(temporaryPath, path);
                    }
                    catch (IOException)
                    {
                        File.Delete(path);
                        File.Move(temporaryPath, path);
                    }
                }
                else
                {
                    File.Move(temporaryPath, path);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
    }
}
