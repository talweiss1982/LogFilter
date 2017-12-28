using System;
using System.Collections.Generic;
using System.Text;

namespace LogFilter
{
    public class ProcessedLine
    {
        public DateTime Time { get; set; }
        public int Thread { get; set; }
        public LogLevel Level{get; set; }
        public string Message { get; set; }
        public override string ToString()
        {
            string lvl = Level == LogLevel.None ? string.Empty : Level.ToString();
            return $"{Time:o}, {Thread}, {lvl},{Source},{Logger},{Message}";
        }

        public string Source
        {
            get => _source;
            set => _source = StaticString.GetStringInstance(value);
        }

        public string Logger
        {
            get => _logger;
            set => _logger = StaticString.GetStringInstance(value);
        }
        private string _source = string.Empty;
        private string _logger;
    }


    public static class StaticString
    {
        private static Dictionary<string,string> _strings = new Dictionary<string, string>();

        public static string GetStringInstance(string s)
        {
            if (_strings.TryGetValue(s, out var ss))
            {
                return ss;
            }
            _strings.Add(s,s);
            return s;
        }
    }

    public enum LogLevel
    {
        None,
        Information,
        Operation
    }
}
