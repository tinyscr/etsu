using System;
using System.Collections.Generic;
using Etsu.App;

namespace Etsu.Ops
{
    internal static class ReadFormatter
    {
        public const int TableWidth = 72;

        public static void Log(IOperationReporter reporter, string output)
        {
            string[] lines = Format(output);
            for (int i = 0; i < lines.Length; i++)
            {
                reporter.Log(lines[i]);
            }
        }

        internal static string[] Format(string output)
        {
            string[] lines = AppRuntime.SplitLines(output);
            int width = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string label;
                string value;
                if (TrySplit(lines[i], out label, out value))
                {
                    label = ReadLabel(label);
                    if (label.Length > width)
                    {
                        width = label.Length;
                    }
                }
            }

            List<string> formatted = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                string label;
                string value;
                if (!TrySplit(lines[i], out label, out value))
                {
                    formatted.Add(lines[i]);
                    continue;
                }

                label = ReadLabel(label);
                string prefix = label.PadLeft(width) + ":  ";
                int valueWidth = TableWidth - prefix.Length;
                if (valueWidth < 1)
                {
                    valueWidth = 1;
                }

                string[] parts = WrapValue(value, valueWidth);
                for (int partIndex = 0; partIndex < parts.Length; partIndex++)
                {
                    if (partIndex == 0)
                    {
                        formatted.Add(prefix + parts[partIndex]);
                    }
                    else
                    {
                        formatted.Add(new string(' ', prefix.Length) + parts[partIndex]);
                    }
                }
            }

            int maxWidth = 0;
            for (int i = 0; i < formatted.Count; i++)
            {
                if (formatted[i].Length > maxWidth)
                {
                    maxWidth = formatted[i].Length;
                }
            }
            if (maxWidth > TableWidth)
            {
                maxWidth = TableWidth;
            }

            string[] result = new string[formatted.Count];
            for (int i = 0; i < formatted.Count; i++)
            {
                string line = formatted[i];
                if (line.Length < maxWidth)
                {
                    line += new string(' ', maxWidth - line.Length);
                }
                result[i] = line;
            }
            return result;
        }

        internal static string ReadLabel(string label)
        {
            if (label == "File Modification Date/Time")
            {
                return "File Modification Time";
            }
            return label;
        }

        internal static string[] WrapValue(string value, int limit)
        {
            if (limit < 1)
            {
                limit = 1;
            }
            if (value.Length <= limit)
            {
                return new[] { value };
            }

            List<string> parts = new List<string>();
            string remaining = value;
            while (remaining.Length > limit)
            {
                int cut = limit;
                while (cut > 0 && remaining[cut] != ' ')
                {
                    cut--;
                }
                if (cut == 0)
                {
                    cut = limit;
                }

                string part = remaining.Substring(0, cut).TrimEnd(' ');
                if (part.Length > 0)
                {
                    parts.Add(part);
                }
                remaining = remaining.Substring(cut).TrimStart(' ');
            }
            if (remaining.Length > 0)
            {
                parts.Add(remaining);
            }
            return parts.ToArray();
        }

        internal static bool TrySplit(string line, out string label, out string value)
        {
            label = null;
            value = null;
            int index = line.IndexOf(" : ", StringComparison.Ordinal);
            if (index < 0)
            {
                return false;
            }
            label = line.Substring(0, index).Trim();
            value = line.Substring(index + 3).Trim();
            return true;
        }
    }
}
