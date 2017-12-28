using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace LogFilter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
                throw new ArgumentException($"Not enough arguments were provided expected atleast two got {args.Length}");
            if(File.Exists(args[0]) == false)
                throw new FileNotFoundException($"Could not find file {args[0]}");
            var filePath = args[0];
            var outputPath = Path.Combine(Path.GetDirectoryName(filePath), $"filtered_{Path.GetFileName(filePath)}");
            var filterByLog = args[1];
            List<ProcessedLine> lines = new List<ProcessedLine>();
            using (var reader = File.OpenText(filePath))
            using (var writer  = File.CreateText(outputPath))
            {
                reader.ReadLine();
                ProcessedLine prevLine = null;
                do
                {
                    var currentLine = reader.ReadLine();

                    if (currentLine == null)
                    {
                        writer.Flush();
                        break;
                    }
                    if(string.IsNullOrWhiteSpace(currentLine))
                        continue;
                    
                    var newLogLine = ProcessLogLine(currentLine, filterByLog);
                    //probably part of the message
                    if (newLogLine.Valid == ProcessResult.NotValid)
                    {
                        if(prevLine != null)
                            prevLine.Message += currentLine;
                        continue;
                    }
                    if (newLogLine.Valid == ProcessResult.Filtered)
                    {
                        continue;
                    }
                    prevLine = newLogLine.Line;
                    //The idea here is to have overlap of 1/4 of the lines so we don't have to sort everything
                    lines.Add(newLogLine.Line);
                    if (lines.Count >= 1_000)
                    {
                        int lineCount = 0;
                        var newList = new List<ProcessedLine>();
                        foreach (var line in lines.OrderBy(l => l.Time))
                        {
                            if (lineCount <= 750)
                            {
                                writer.WriteLine(line);
                                lineCount++;
                            }
                            else
                            {
                                newList.Add(line);
                            }                                
                        }
                        lines = newList;
                        writer.Flush();
                    }
                } while (true);
                if (lines.Count > 0)
                {
                    foreach (var line in lines.OrderBy(l => l.Time))
                    {
                        writer.WriteLine(line);
                    }
                    writer.Flush();
                }
            }
        }

        private static (ProcessResult Valid, ProcessedLine Line) ProcessLogLine(string currentLine, string logFilter)
        {
            try
            {
                int index = -1;
                var token = GetNextToken(currentLine, ref index);
                if (token == null)
                    return (ProcessResult.NotValid, null);
                var time = DateTime.Parse(token);
                token = GetNextToken(currentLine, ref index);
                if (token == null)
                    return (ProcessResult.NotValid, null);
                var thread = int.Parse(token);
                token = GetNextToken(currentLine, ref index);
                LogLevel logLevel = LogLevel.None;
                if (token != null)
                {
                    logLevel = (LogLevel)Enum.Parse(typeof(LogLevel), token);
                }
                token = GetNextToken(currentLine, ref index);
                if (token == null)
                    return (ProcessResult.NotValid, null);
                var source = token;
                token = GetNextToken(currentLine, ref index);
                if (token == null)
                    return (ProcessResult.NotValid, null);
                var logger = token;
                if (logger.Trim().Equals(logFilter, StringComparison.OrdinalIgnoreCase) == false)
                    return (ProcessResult.Filtered, null);
                var message = currentLine.Substring(index+1);
                return (ProcessResult.Valid, new ProcessedLine
                {
                    Level = logLevel,
                    Logger = logger,
                    Message = message,
                    Source = source,
                    Thread = thread,
                    Time = time
                });
            }
            catch
            {
                return (ProcessResult.NotValid, null);
            }
            
        }

        private static string GetNextToken(string s, ref int prevIndex)
        {
            var nextPrev = s.IndexOf(',', prevIndex+1);
            if (nextPrev <= prevIndex || nextPrev == prevIndex+1)
            {
                return null;
            }
            var res = s.Substring(prevIndex + 1, nextPrev- prevIndex-1);
            prevIndex = nextPrev;
            return res;
        }
    }

    public enum ProcessResult
    {
        Valid,
        NotValid,
        Filtered
    }
}
