using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using App.Code.Domain.Journal;

namespace App.Code.Infrastructure.Journal
{
    public sealed class JsonLinesFileLogger : IScenarioLogger
    {
        private readonly StreamWriter _writer;

        public JsonLinesFileLogger(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path must not be empty", nameof(filePath));

            FilePath = filePath;
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            _writer = new StreamWriter(filePath, false, new UTF8Encoding(false)) { AutoFlush = true };
        }

        public string FilePath { get; }

        public void Write(LogRecord record) => _writer.WriteLine(JsonLinesCodec.Encode(record));

        public void Dispose() => _writer.Dispose();
    }

    public sealed class JournalFile
    {
        public JournalFile(string path, IReadOnlyList<LogRecord> records, IReadOnlyList<string> warnings)
        {
            Path = path;
            Records = records;
            Warnings = warnings;
        }

        public string Path { get; }

        public IReadOnlyList<LogRecord> Records { get; }

        public IReadOnlyList<string> Warnings { get; }
    }

    public static class JsonLinesJournalReader
    {
        public const string Extension = ".jsonl";

        public static JournalFile Read(string path)
        {
            var records = new List<LogRecord>();
            var warnings = new List<string>();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                warnings.Add($"Journal file not found: {path}");
                return new JournalFile(path, records, warnings);
            }

            var lineNumber = 0;
            foreach (var line in File.ReadLines(path, Encoding.UTF8))
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (JsonLinesCodec.TryDecode(line, out var record, out var error)) records.Add(record);
                else warnings.Add($"Line {lineNumber} skipped: {error}");
            }

            return new JournalFile(path, records, warnings);
        }

        public static string FindLatest(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return string.Empty;

            return new DirectoryInfo(directory)
                .GetFiles("*" + Extension)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Select(file => file.FullName)
                .FirstOrDefault() ?? string.Empty;
        }
    }
}
