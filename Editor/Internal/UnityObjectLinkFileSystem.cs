using System;
using System.IO;
using System.Text;

namespace UnityObjectLink
{
    internal interface IUnityObjectLinkFileSystem
    {
        void CreateDirectory(string path);
        string[] GetRequestFiles(string directory);
        long GetFileLength(string path);
        DateTime GetLastWriteTimeUtc(string path);
        string ReadAllTextUtf8(string path);
        void DeleteFile(string path);
    }

    internal sealed class PhysicalUnityObjectLinkFileSystem : IUnityObjectLinkFileSystem
    {
        internal static readonly PhysicalUnityObjectLinkFileSystem Instance = new PhysicalUnityObjectLinkFileSystem();

        private PhysicalUnityObjectLinkFileSystem()
        {
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public string[] GetRequestFiles(string directory)
        {
            return Directory.GetFiles(directory, "*.request", SearchOption.TopDirectoryOnly);
        }

        public long GetFileLength(string path)
        {
            return new FileInfo(path).Length;
        }

        public DateTime GetLastWriteTimeUtc(string path)
        {
            return File.GetLastWriteTimeUtc(path);
        }

        public string ReadAllTextUtf8(string path)
        {
            using (var reader = new StreamReader(path, new UTF8Encoding(false, true), false))
            {
                return reader.ReadToEnd();
            }
        }

        public void DeleteFile(string path)
        {
            File.Delete(path);
        }
    }
}
