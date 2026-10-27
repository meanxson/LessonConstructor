using System;
using System.Collections.Generic;
using System.Linq;

namespace App.Code.Domain.Journal
{
    public interface IScenarioLogger : IDisposable
    {
        void Write(LogRecord record);
    }

    public sealed class ScenarioJournal : IScenarioLogger
    {
        private readonly List<LogRecord> _records = new List<LogRecord>();

        public ScenarioJournal()
        {
        }

        public ScenarioJournal(IEnumerable<LogRecord> records)
        {
            _records.AddRange(records ?? throw new ArgumentNullException(nameof(records)));
        }

        public event Action<LogRecord> Added;

        public IReadOnlyList<LogRecord> Records => _records;

        public void Write(LogRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            _records.Add(record);
            Added?.Invoke(record);
        }

        public void Dispose()
        {
        }
    }

    public sealed class FanOutLogger : IScenarioLogger
    {
        private readonly IReadOnlyList<IScenarioLogger> _targets;

        public FanOutLogger(IEnumerable<IScenarioLogger> targets)
        {
            _targets = (targets ?? throw new ArgumentNullException(nameof(targets))).Where(target => target != null).ToArray();
        }

        public event Action<IScenarioLogger, Exception> WriteFailed;

        public void Write(LogRecord record)
        {
            foreach (var target in _targets)
            {
                try
                {
                    target.Write(record);
                }
                catch (Exception exception)
                {
                    WriteFailed?.Invoke(target, exception);
                }
            }
        }

        public void Dispose()
        {
            foreach (var target in _targets)
            {
                try
                {
                    target.Dispose();
                }
                catch (Exception exception)
                {
                    WriteFailed?.Invoke(target, exception);
                }
            }
        }
    }
}
