using System;
using System.Reflection;

namespace Etsu.App
{
    public static class AppInfo
    {
        public static string Version
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version == null)
                {
                    return "0.0.0";
                }
                int build = version.Build < 0 ? 0 : version.Build;
                return version.Major + "." + version.Minor + "." + build;
            }
        }
    }
}
