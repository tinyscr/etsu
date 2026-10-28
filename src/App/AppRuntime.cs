using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Etsu.App
{
    public static class AppRuntime
    {
        public static ConfigFile Config { get; private set; }
        public static string ExifPath { get; private set; }
        public static string ExifVersion { get; private set; }

        public static void Initialize()
        {
            Config = ConfigFile.Load(AppPaths.ConfigPath);
            ExifPath = null;
            ExifVersion = "";
        }

        public static bool FindExifTool(out string error)
        {
            error = null;
            string configured = GeneralString("exiftoolPATH", "null");

            if (!string.Equals(configured, "null", StringComparison.OrdinalIgnoreCase))
            {
                if (configured.IndexOf('"') >= 0)
                {
                    error = "exiftoolPATH contains an illegal quote character";
                    return false;
                }
                if (!File.Exists(configured))
                {
                    error = "ExifTool was not found at the configured path:\n" + configured;
                    return false;
                }

                ExifPath = configured;
                ExifVersion = RunTool(ExifPath, "-ver").Trim();
                return true;
            }

            string path = FindOnPath("exiftool.exe");
            if (!string.IsNullOrEmpty(path))
            {
                ExifPath = path;
                ExifVersion = RunTool(path, "-ver").Trim();
                return true;
            }

            string local = Path.Combine(AppPaths.BaseDir, "exiftool.exe");
            if (File.Exists(local))
            {
                ExifPath = local;
                ExifVersion = RunTool(local, "-ver").Trim();
                return true;
            }

            error = "ExifTool was not found.\n\nInstall ExifTool or set exiftoolPATH in:\n" + AppPaths.ConfigPath;
            return false;
        }

        public static string GeneralString(string key, string fallback)
        {
            return Config == null ? fallback : Config.GetString("General", key, fallback);
        }

        public static bool LogEnabled(string tool)
        {
            return Config == null
                ? !string.Equals(tool, "read", StringComparison.OrdinalIgnoreCase)
                : Config.GetBool(tool, "log", !string.Equals(tool, "read", StringComparison.OrdinalIgnoreCase));
        }

        public static int LogCount(string tool)
        {
            return Config == null ? 10 : Config.GetInt(tool, "logCount", 10);
        }

        public static string WriteLog(string prefix, IList<string> lines)
        {
            if (!LogEnabled(prefix))
            {
                return "";
            }

            try
            {
                Directory.CreateDirectory(AppPaths.LogDir);
            }
            catch (Exception)
            {
                return "";
            }

            string path = Path.Combine(
                AppPaths.LogDir,
                prefix + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".log");

            StringBuilder text = new StringBuilder();
            if (lines != null)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    text.Append(lines[i] ?? "");
                    text.Append('\n');
                }
            }

            try
            {
                File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
            }
            catch (Exception)
            {
                return "";
            }

            try
            {
                string[] files = Directory.GetFiles(AppPaths.LogDir, prefix + "_*.log");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                int keep = LogCount(prefix);
                for (int i = 0; i < files.Length - keep; i++)
                {
                    try
                    {
                        File.Delete(files[i]);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception)
            {
            }

            return File.Exists(path) ? path : "";
        }

        public static string[] SplitLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new string[0];
            }
            return text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static void CopyFile(string source, string destination, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            using (FileStream input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] buffer = new byte[1024 * 1024];
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    int read = input.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                    {
                        break;
                    }
                    output.Write(buffer, 0, read);
                }
                output.Flush();
            }
        }

        public static byte[] ComputeSha256(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (SHA256 sha = SHA256.Create())
            {
                byte[] buffer = new byte[1024 * 1024];
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                    {
                        break;
                    }
                    sha.TransformBlock(buffer, 0, read, buffer, 0);
                }
                sha.TransformFinalBlock(new byte[0], 0, 0);
                return sha.Hash;
            }
        }

        public static bool HashesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static void SetCreationTime(string path, DateTime value)
        {
            File.SetCreationTime(path, value);
        }

        private static string RunTool(string executable, string argument)
        {
            try
            {
                return ProcessRunner.Run(executable, new[] { argument }, CancellationToken.None).Output;
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static string FindOnPath(string fileName)
        {
            string path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string[] parts = path.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                string directory = parts[i].Trim().Trim('"');
                if (directory.Length == 0)
                {
                    continue;
                }
                try
                {
                    string candidate = Path.Combine(directory, fileName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                catch (Exception)
                {
                }
            }
            return null;
        }
    }
}
