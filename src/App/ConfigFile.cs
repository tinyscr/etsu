using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Etsu.App
{
    public sealed class ConfigFile
    {
        private readonly Dictionary<string, Dictionary<string, string>> _sections =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public static ConfigFile Load(string path)
        {
            ConfigFile file = new ConfigFile();
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (Exception)
            {
                return file;
            }

            Dictionary<string, string> current = null;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = (lines[i] ?? "").Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == ';')
                {
                    continue;
                }

                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    string name = line.Substring(1, line.Length - 2).Trim();
                    if (!file._sections.TryGetValue(name, out current))
                    {
                        current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        file._sections[name] = current;
                    }
                    continue;
                }

                int equals = line.IndexOf('=');
                if (equals <= 0 || current == null)
                {
                    continue;
                }

                string key = line.Substring(0, equals).Trim();
                string value = line.Substring(equals + 1).Trim();
                current[key] = value;
            }

            return file;
        }

        public string GetString(string section, string key, string fallback)
        {
            Dictionary<string, string> values;
            if (_sections.TryGetValue(section ?? "", out values))
            {
                string value;
                if (values.TryGetValue(key ?? "", out value))
                {
                    return value;
                }
            }
            return fallback;
        }

        public bool GetBool(string section, string key, bool fallback)
        {
            string value = GetString(section, key, null);
            if (value == null)
            {
                return fallback;
            }
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        public int GetInt(string section, string key, int fallback)
        {
            string value = GetString(section, key, null);
            if (value == null)
            {
                return fallback;
            }

            int parsed;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) || parsed < 0)
            {
                return fallback;
            }
            return parsed;
        }
    }
}
