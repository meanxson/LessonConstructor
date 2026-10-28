using System.Collections.Generic;
using App.Code.Domain.Journal;

namespace App.Code.Presentation.UI.Hud.States
{
    public sealed class JournalState
    {
        public JournalState()
        {
        }

        public JournalState(bool isVisible, string source, IReadOnlyList<LogRecord> records)
        {
            IsVisible = isVisible;
            Source = source ?? string.Empty;
            Records = records ?? new LogRecord[0];
        }

        public bool IsVisible { get; }

        public string Source { get; } = string.Empty;

        public IReadOnlyList<LogRecord> Records { get; } = new LogRecord[0];
    }
}
