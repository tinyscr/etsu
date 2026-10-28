using System;
using System.IO;
using System.Reflection;

namespace Etsu.App
{
    public static class AppPaths
    {
        public static string BaseDir { get; private set; }

        public static string ConfigPath
        {
            get { return Path.Combine(RequireBaseDir(), "config.ini"); }
        }

        public static string LogDir
        {
            get { return Path.Combine(RequireBaseDir(), "logs", "etsu"); }
        }

        public static string TempDir
        {
            get { return Path.Combine(RequireBaseDir(), "tmp"); }
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
            Directory.CreateDirectory(LogDir);
            Directory.CreateDirectory(TempDir);

            string probe = Path.Combine(TempDir, ".etsu-write-test-" + Guid.NewGuid().ToString("N"));
            using (File.Create(probe))
            {
            }
            File.Delete(probe);
        }

        private static string RequireBaseDir()
        {
            if (string.IsNullOrEmpty(BaseDir))
            {
                throw new InvalidOperationException("AppPaths.Init must be called first.");
            }
            return BaseDir;
        }
    }
}
