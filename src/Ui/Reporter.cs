using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Etsu.App;
using Etsu.Ops;

namespace Etsu.Ui
{
    internal sealed class Reporter : IOperationReporter
    {
        public const int RuleWidth = 54;

        private static readonly Regex StepLine = new Regex(@"^\[(\d+)/(\d+)\] ", RegexOptions.Compiled);
        private static readonly string Rule = new string('━', RuleWidth);

        private readonly List<string> _lines = new List<string>();
        private readonly List<string> _rawLines = new List<string>();
        private readonly string _appVersion;
        private readonly string _exifVersion;
        private readonly Action<string> _sink;

        public Reporter(string appVersion, string exifVersion, Action<string> sink)
        {
            _appVersion = appVersion;
            _exifVersion = exifVersion;
            _sink = sink;
        }

        public IList<string> Lines
        {
            get { return _lines; }
        }

        public IList<string> RawLines
        {
            get { return _rawLines; }
        }

        public void Header(string operation, string file)
        {
            Header(operation, new[] { file });
        }

        public void Header(string operation, string[] files)
        {
            int count = files == null ? 0 : files.Length;
            List<string> lines = new List<string>();
            lines.Add("┏┓  ┏┳┓  ┏┓  ┳┳");
            lines.Add("┣    ┃   ┗┓  ┃┃");
            lines.Add("┗┛   ┻   ┗┛  ┗┛");
            lines.Add(Rule);
            lines.Add(
                " App: " + _appVersion + " | Exiftool: " + _exifVersion +
                " | Operation: " + CommandLine.OperationLabel(operation));
            lines.Add("");
            lines.Add(" File(s): " + CountText(count));
            if (files != null)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    lines.Add(" " + (files[i] ?? ""));
                }
            }
            lines.Add(Rule);
            lines.Add("");

            for (int i = 0; i < lines.Count; i++)
            {
                Write(lines[i]);
            }
        }

        public void Progress(int percent, string label)
        {
        }

        public void Log(string line)
        {
            Match match = StepLine.Match(line ?? "");
            if (!match.Success)
            {
                Write(line);
                return;
            }

            int number;
            int.TryParse(match.Groups[1].Value, out number);
            if (number > 1)
            {
                Blank(2);
            }
            Write(line);
        }

        public void LogRaw(string line)
        {
            _rawLines.Add(line ?? "");
        }

        private void Write(string line)
        {
            string text = line ?? "";
            _lines.Add(text);
            _rawLines.Add(text);
            if (_sink != null)
            {
                _sink(text);
            }
        }

        private void Blank(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Write("");
            }
        }

        private static string CountText(int count)
        {
            return count + (count == 1 ? " file" : " files");
        }
    }
}
