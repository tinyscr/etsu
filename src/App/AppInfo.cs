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
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(
                    typeof(AssemblyInformationalVersionAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyInformationalVersionAttribute informational =
                        attributes[0] as AssemblyInformationalVersionAttribute;
                    if (informational != null && !string.IsNullOrEmpty(informational.InformationalVersion))
                    {
                        return informational.InformationalVersion;
                    }
                }

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
