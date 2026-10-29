using System;

namespace Etsu.App
{
    public static class CommandLine
    {
        public static bool TryParse(string[] args, out string action, out string[] files)
        {
            action = null;
            files = null;

            if (args == null || args.Length < 2)
            {
                return false;
            }

            string candidate = args[0] == null ? "" : args[0].ToLowerInvariant();
            if (candidate != "read" && candidate != "clean" && candidate != "date")
            {
                return false;
            }

            if (candidate == "read" && args.Length != 2)
            {
                return false;
            }

            string[] parsed = new string[args.Length - 1];
            for (int i = 0; i < parsed.Length; i++)
            {
                if (string.IsNullOrEmpty(args[i + 1]))
                {
                    return false;
                }
                parsed[i] = args[i + 1];
            }

            action = candidate;
            files = parsed;
            return true;
        }

        public static string OperationTitle(string action)
        {
            switch (action)
            {
                case "read":
                    return "Read metadata";
                case "clean":
                    return "Clean metadata";
                case "date":
                    return "Set date";
                default:
                    return action ?? "";
            }
        }

        public static string OperationLabel(string action)
        {
            switch (action)
            {
                case "read":
                    return "Read metadata";
                case "clean":
                    return "Clean";
                case "date":
                    return "Set date";
                default:
                    return action ?? "";
            }
        }
    }
}
