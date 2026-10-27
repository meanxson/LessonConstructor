using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using App.Code.Domain.Journal;

namespace App.Code.Infrastructure.Journal
{
    public static class JsonLinesCodec
    {
        private const string SequenceKey = "seq";
        private const string TimeKey = "time";
        private const string TimestampKey = "timestamp";
        private const string TypeKey = "type";
        private const string StepKey = "step";
        private const string OutcomeKey = "outcome";
        private const string AttemptKey = "attempt";
        private const string GroupKey = "group";
        private const string ReasonKey = "reason";
        private const string DataKey = "data";

        public static string Encode(LogRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            var builder = new StringBuilder(160);
            builder.Append('{');
            AppendNumber(builder, SequenceKey, record.Sequence.ToString(CultureInfo.InvariantCulture), true);
            AppendNumber(builder, TimeKey, record.Time.ToString("0.###", CultureInfo.InvariantCulture), false);
            AppendString(builder, TimestampKey, record.Timestamp);
            AppendString(builder, TypeKey, record.Type);
            AppendString(builder, StepKey, record.StepId);
            AppendString(builder, OutcomeKey, record.Outcome);
            AppendNumber(builder, AttemptKey, record.Attempt.ToString(CultureInfo.InvariantCulture), false);
            AppendNumber(builder, GroupKey, record.Group.ToString(CultureInfo.InvariantCulture), false);
            AppendString(builder, ReasonKey, record.Reason);
            builder.Append(",\"").Append(DataKey).Append("\":{");
            var first = true;
            foreach (var pair in record.Data)
            {
                if (!first) builder.Append(',');
                first = false;
                AppendQuoted(builder, pair.Key);
                builder.Append(':');
                AppendQuoted(builder, pair.Value);
            }

            builder.Append("}}");
            return builder.ToString();
        }

        public static bool TryDecode(string line, out LogRecord record, out string error)
        {
            record = null;
            try
            {
                var fields = new FlatJsonReader(line).ReadObject();
                var data = new Dictionary<string, string>();
                if (fields.TryGetValue(DataKey, out var nested) && nested is Dictionary<string, object> nestedFields)
                    foreach (var pair in nestedFields)
                        data[pair.Key] = Convert.ToString(pair.Value, CultureInfo.InvariantCulture);

                foreach (var pair in fields)
                    if (!IsKnownKey(pair.Key) && !(pair.Value is Dictionary<string, object>))
                        data[pair.Key] = Convert.ToString(pair.Value, CultureInfo.InvariantCulture);

                var type = Text(fields, TypeKey);
                if (type.Length == 0)
                {
                    error = "record has no type";
                    return false;
                }

                record = new LogRecord(type, Text(fields, StepKey), Text(fields, OutcomeKey), Integer(fields, AttemptKey),
                    Integer(fields, GroupKey), Text(fields, ReasonKey), data, Integer(fields, SequenceKey, 0),
                    Number(fields, TimeKey), Text(fields, TimestampKey));
                error = string.Empty;
                return true;
            }
            catch (FormatException exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static bool IsKnownKey(string key) =>
            key == SequenceKey || key == TimeKey || key == TimestampKey || key == TypeKey || key == StepKey ||
            key == OutcomeKey || key == AttemptKey || key == GroupKey || key == ReasonKey || key == DataKey;

        private static string Text(Dictionary<string, object> fields, string key) =>
            fields.TryGetValue(key, out var value) && value != null ? Convert.ToString(value, CultureInfo.InvariantCulture) : string.Empty;

        private static double Number(Dictionary<string, object> fields, string key) =>
            fields.TryGetValue(key, out var value) && value is double number ? number : 0;

        private static int Integer(Dictionary<string, object> fields, string key, int fallback = LogRecord.NoValue) =>
            fields.TryGetValue(key, out var value) && value is double number ? (int)number : fallback;

        private static void AppendNumber(StringBuilder builder, string key, string value, bool first)
        {
            if (!first) builder.Append(',');
            AppendQuoted(builder, key);
            builder.Append(':').Append(value);
        }

        private static void AppendString(StringBuilder builder, string key, string value)
        {
            builder.Append(',');
            AppendQuoted(builder, key);
            builder.Append(':');
            AppendQuoted(builder, value);
        }

        private static void AppendQuoted(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (var symbol in value ?? string.Empty)
            {
                switch (symbol)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (symbol < ' ') builder.Append("\\u").Append(((int)symbol).ToString("x4", CultureInfo.InvariantCulture));
                        else builder.Append(symbol);
                        break;
                }
            }

            builder.Append('"');
        }

        private sealed class FlatJsonReader
        {
            private readonly string _text;
            private int _position;

            public FlatJsonReader(string text)
            {
                _text = text ?? string.Empty;
            }

            public Dictionary<string, object> ReadObject()
            {
                SkipWhitespace();
                Expect('{');
                var result = new Dictionary<string, object>();
                SkipWhitespace();
                if (Peek() == '}')
                {
                    _position++;
                    return result;
                }

                while (true)
                {
                    SkipWhitespace();
                    var key = ReadString();
                    SkipWhitespace();
                    Expect(':');
                    result[key] = ReadValue();
                    SkipWhitespace();
                    var next = Next();
                    if (next == '}') return result;
                    if (next != ',') throw new FormatException($"expected ',' or '}}' at {_position}");
                }
            }

            private object ReadValue()
            {
                SkipWhitespace();
                var symbol = Peek();
                if (symbol == '"') return ReadString();
                if (symbol == '{') return ReadObject();
                if (Match("true")) return "true";
                if (Match("false")) return "false";
                if (Match("null")) return null;
                return ReadNumber();
            }

            private double ReadNumber()
            {
                var start = _position;
                while (_position < _text.Length && "+-0123456789.eE".IndexOf(_text[_position]) >= 0) _position++;
                if (!double.TryParse(_text.Substring(start, _position - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    throw new FormatException($"invalid value at {start}");
                return value;
            }

            private string ReadString()
            {
                Expect('"');
                var builder = new StringBuilder();
                while (true)
                {
                    var symbol = Next();
                    if (symbol == '"') return builder.ToString();
                    if (symbol != '\\')
                    {
                        builder.Append(symbol);
                        continue;
                    }

                    var escaped = Next();
                    switch (escaped)
                    {
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'u':
                            if (_position + 4 > _text.Length) throw new FormatException("truncated unicode escape");
                            builder.Append((char)int.Parse(_text.Substring(_position, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            _position += 4;
                            break;
                        default: builder.Append(escaped); break;
                    }
                }
            }

            private bool Match(string literal)
            {
                if (string.CompareOrdinal(_text, _position, literal, 0, literal.Length) != 0) return false;
                _position += literal.Length;
                return true;
            }

            private void Expect(char expected)
            {
                if (Next() != expected) throw new FormatException($"expected '{expected}' at {_position - 1}");
            }

            private char Peek() => _position < _text.Length ? _text[_position] : '\0';

            private char Next()
            {
                if (_position >= _text.Length) throw new FormatException("unexpected end of line");
                return _text[_position++];
            }

            private void SkipWhitespace()
            {
                while (_position < _text.Length && char.IsWhiteSpace(_text[_position])) _position++;
            }
        }
    }
}
