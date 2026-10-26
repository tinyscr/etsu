using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

class ReadTool
{
    public static int Run(string[] paths = null)
    {
        Etsu.WriteHeader("Read");

        string file;
        if (paths != null)
        {
            if (paths.Length != 1)
            {
                Etsu.WriteLine("read takes exactly one file path.", ConsoleColor.Red);
                return 2;
            }

            file = paths[0];
            if (!File.Exists(file))
            {
                Etsu.WriteLine("File not found: " + file, ConsoleColor.Red);
                return 2;
            }
        }
        else
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Select a file to read metadata from";
            dialog.Filter = "All Files|*.*";

            Console.Write("  Open file picker? (Y/N): ");
            string response = Console.ReadLine();
            if (response == null || !response.Trim().ToLower().StartsWith("y"))
            {
                Etsu.WriteLine("Cancelled operation.", ConsoleColor.DarkGray);
                return 0;
            }

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                Etsu.WriteLine("No file selected.", ConsoleColor.DarkGray);
                return 0;
            }

            file = dialog.FileName;
        }

        Console.WriteLine("  Selected: " + Path.GetFileName(file));
        Etsu.WriteSep();

        string output = Etsu.RunTool(Etsu.ExifPath, "\"" + file + "\"");
        var logLines = new List<string>();
        foreach (string ln in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            Console.WriteLine("  " + ln);
            logLines.Add(ln);
        }

        Console.WriteLine();
        Etsu.WriteSep();
        Etsu.WriteLog("read", "SUCCESS", logLines);
        return Etsu.WaitExit();
    }
}
