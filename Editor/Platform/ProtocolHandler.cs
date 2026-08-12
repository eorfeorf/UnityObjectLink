using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor.PackageManager;

namespace UnityObjectLink
{
    internal sealed class ProtocolCommandResult
    {
        internal bool Succeeded;
        internal string Output;

        internal static ProtocolCommandResult Failure(string message)
        {
            return new ProtocolCommandResult { Succeeded = false, Output = message };
        }
    }

    internal interface IProtocolHandler
    {
        ProtocolCommandResult Install(string scheme);
        ProtocolCommandResult Uninstall(string scheme);
        ProtocolCommandResult Status(string scheme);
    }

    internal static class ProtocolHandler
    {
        private static readonly IProtocolHandler Instance = Create();

        internal static ProtocolCommandResult Install(string scheme) { return Instance.Install(scheme); }
        internal static ProtocolCommandResult Uninstall(string scheme) { return Instance.Uninstall(scheme); }
        internal static ProtocolCommandResult Status(string scheme) { return Instance.Status(scheme); }

        private static IProtocolHandler Create()
        {
#if UNITY_EDITOR_WIN
            return new ScriptProtocolHandler("powershell.exe", "Editor/Platform/Windows/UnityObjectLinkProtocol.ps1");
#elif UNITY_EDITOR_OSX
            return new ScriptProtocolHandler("/bin/bash", "Editor/Platform/macOS/unity-object-link-protocol.sh");
#else
            return new UnsupportedProtocolHandler();
#endif
        }
    }

    internal sealed class ScriptProtocolHandler : IProtocolHandler
    {
        private readonly string executable;
        private readonly string relativeScriptPath;

        internal ScriptProtocolHandler(string executable, string relativeScriptPath)
        {
            this.executable = executable;
            this.relativeScriptPath = relativeScriptPath;
        }

        public ProtocolCommandResult Install(string scheme) { return Run("install", scheme); }
        public ProtocolCommandResult Uninstall(string scheme) { return Run("uninstall", scheme); }
        public ProtocolCommandResult Status(string scheme) { return Run("status", scheme); }

        private ProtocolCommandResult Run(string command, string scheme)
        {
            string packageRoot;
            try
            {
                PackageInfo info = PackageInfo.FindForAssembly(Assembly.GetExecutingAssembly());
                packageRoot = info == null ? null : info.resolvedPath;
            }
            catch (Exception exception)
            {
                return ProtocolCommandResult.Failure("Could not locate the package: " + exception.Message);
            }

            if (string.IsNullOrEmpty(packageRoot))
            {
                return ProtocolCommandResult.Failure("Could not locate the Unity Object Link package.");
            }

            string scriptPath = Path.Combine(packageRoot, relativeScriptPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(scriptPath))
            {
                return ProtocolCommandResult.Failure("Protocol script not found: " + scriptPath);
            }

            string arguments;
#if UNITY_EDITOR_WIN
            arguments = "-NoProfile -ExecutionPolicy Bypass -File " + Quote(scriptPath) + " -Command " + Quote(command) + " -Scheme " + Quote(scheme);
#else
            arguments = Quote(scriptPath) + " " + Quote(command) + " " + Quote(scheme);
#endif
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return ProtocolCommandResult.Failure("The protocol command did not start.");
                    }

                    if (!process.WaitForExit(15000))
                    {
                        try { process.Kill(); } catch { }
                        return ProtocolCommandResult.Failure("The protocol command timed out.");
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    string combined = (output + Environment.NewLine + error).Trim();
                    return new ProtocolCommandResult { Succeeded = process.ExitCode == 0, Output = combined };
                }
            }
            catch (Exception exception)
            {
                return ProtocolCommandResult.Failure(exception.Message);
            }
        }

        private static string Quote(string value)
        {
#if UNITY_EDITOR_OSX
            return "'" + value.Replace("'", "'\"'\"'") + "'";
#else
            return "\"" + value.Replace("\"", "\\\"") + "\"";
#endif
        }
    }

    internal sealed class UnsupportedProtocolHandler : IProtocolHandler
    {
        public ProtocolCommandResult Install(string scheme) { return Unsupported(); }
        public ProtocolCommandResult Uninstall(string scheme) { return Unsupported(); }
        public ProtocolCommandResult Status(string scheme) { return Unsupported(); }

        private static ProtocolCommandResult Unsupported()
        {
            return ProtocolCommandResult.Failure("Protocol registration is supported only on Windows and macOS.");
        }
    }
}
