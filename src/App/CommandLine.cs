using System;

namespace Etsu.App
{
    public static class CommandLine
    {
        public static bool TryParse(string[] args, out string action, out string file)
        {
            action = null;
            file = null;

            if (args == null || args.Length != 2)
            {
                return false;
            }

            string candidate = args[0] == null ? "" : args[0].ToLowerInvariant();
            if (candidate != "read" && candidate != "clean" && candidate != "date")
            {
                return false;
            }

            if (string.IsNullOrEmpty(args[1]))
            {
                return false;
            }

            action = candidate;
            file = args[1];
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
    }
}
