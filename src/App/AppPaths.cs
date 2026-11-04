using System;
using System.IO;
using System.Reflection;

namespace Etsu.App
{
    public static class AppPaths
    {
        public static string BaseDir { get; private set; }
        public static string RootDir { get; private set; }

        public static string ConfigPath
        {
            get { return Path.Combine(RequireBaseDir(), "config.ini"); }
        }

        public static string LogDir
        {
            get { return Path.Combine(RequireRootDir(), "logs"); }
        }

        public static string TempDir
        {
            get { return Path.Combine(RequireRootDir(), "tmp"); }
        }

        public static void Init()
        {
            string location = Assembly.GetExecutingAssembly().Location;
            string baseDir = Path.GetDirectoryName(location);
            if (string.IsNullOrEmpty(baseDir))
            {
                throw new InvalidOperationException("Could not determine the application directory.");
            }

            BaseDir = baseDir;
            RootDir = ResolveRootDir(baseDir);
            Directory.CreateDirectory(LogDir);
            Directory.CreateDirectory(TempDir);

            string probe = Path.Combine(TempDir, ".etsu-write-test-" + Guid.NewGuid().ToString("N"));
            using (File.Create(probe))
            {
            }
            File.Delete(probe);
        }

        internal static string ResolveRootDir(string baseDir)
        {
            if (string.Equals(Path.GetFileName(baseDir), "bin", StringComparison.OrdinalIgnoreCase))
            {
                string parent = Path.GetDirectoryName(baseDir);
                if (!string.IsNullOrEmpty(parent))
                {
                    return parent;
                }
            }

            return baseDir;
        }

        private static string RequireBaseDir()
        {
            if (string.IsNullOrEmpty(BaseDir))
            {
                throw new InvalidOperationException("AppPaths.Init must be called first.");
            }
            return BaseDir;
        }

        private static string RequireRootDir()
        {
            if (string.IsNullOrEmpty(RootDir))
            {
                throw new InvalidOperationException("AppPaths.Init must be called first.");
            }
            return RootDir;
        }
    }
}
