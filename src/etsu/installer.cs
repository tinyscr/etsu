using System;
using System.IO;
using Microsoft.Win32;

static class Installer
{
    public static int Run(bool uninstall = false)
    {
        string parentKey = @"Software\Classes\*\shell\etsu";

        if (uninstall)
        {
            using (var classes = Registry.CurrentUser.OpenSubKey(@"Software\Classes\*\shell", writable: true))
            {
                if (classes == null)
                {
                    Etsu.WriteLine("etsu context menu is not installed.", ConsoleColor.DarkGray);
                    return 0;
                }

                using (var existing = classes.OpenSubKey("etsu"))
                {
                    if (existing == null)
                    {
                        Etsu.WriteLine("etsu context menu is not installed.", ConsoleColor.DarkGray);
                        return 0;
                    }
                }

                classes.DeleteSubKeyTree("etsu");
            }

            Etsu.WriteLine("Removed etsu Explorer context menu.", ConsoleColor.Green);
            return 0;
        }

        string exePath = Path.Combine(Etsu.BaseDir, "etsu.exe");
        if (!File.Exists(exePath))
        {
            Etsu.WriteLine("etsu.exe not found next to the binary: " + exePath, ConsoleColor.Red);
            return 1;
        }

        using (var parent = Registry.CurrentUser.CreateSubKey(parentKey))
        {
            parent.SetValue("MUIVerb", "etsu", RegistryValueKind.String);
            parent.SetValue("subcommands", "", RegistryValueKind.String);
            parent.SetValue("MultiSelectModel", "Single", RegistryValueKind.String);

            AddChild(parent, "a_read",  "&Read metadata...", "read", exePath);
            AddChild(parent, "b_clean", "&Clean metadata...", "clean", exePath);
            AddChild(parent, "c_date",  "&Set date...", "date", exePath);
        }

        Etsu.WriteLine("Registered etsu Explorer context menu.", ConsoleColor.Green);
        Etsu.WriteLine("On Windows 11 it appears under Show more options.", ConsoleColor.DarkGray);
        return 0;
    }

    static void AddChild(RegistryKey parent, string key, string label, string command, string exePath)
    {
        using (var child = parent.CreateSubKey("shell\\" + key))
        {
            child.SetValue("MUIVerb", label, RegistryValueKind.String);
            child.SetValue("MultiSelectModel", "Single", RegistryValueKind.String);
            using (var cmd = child.CreateSubKey("command"))
                cmd.SetValue(null, "\"" + exePath + "\" " + command + " \"%L\"", RegistryValueKind.String);
        }
    }
}
