using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Etsu.Ops;

namespace Etsu.Ui
{
    internal sealed class Reporter : IOperationReporter
    {
        public const int RuleWidth = 54;

        private static readonly Regex StepLine = new Regex(@"^\[(\d+)/(\d+)\] ", RegexOptions.Compiled);
        private static readonly string Rule = new string('━', RuleWidth);

        private readonly List<string> _lines = new List<string>();
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

        public void Header(string operation, string file)
        {
            string[] lines =
            {
                "┏┓  ┏┳┓  ┏┓  ┳┳",
                "┣    ┃   ┗┓  ┃┃",
                "┗┛   ┻   ┗┛  ┗┛",
                Rule,
                " App: " + _appVersion + " | Exiftool: " + _exifVersion + " | Operation: " + operation,
                "",
                " File(s): " + file,
                Rule,
                ""
            };

            for (int i = 0; i < lines.Length; i++)
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

            int width = RuleWidth;
            if (number > 1)
            {
                width = line.Length + 2;
            }
            Write(new string('━', width));
        }

        private void Write(string line)
        {
            _lines.Add(line);
            if (_sink != null)
            {
                _sink(line);
            }
        }

        private void Blank(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Write("");
            }
        }
    }
}
