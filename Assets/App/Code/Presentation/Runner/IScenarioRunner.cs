using System;
using System.Collections.Generic;
using App.Code.Domain.Analysis;
using App.Code.Domain.Execution;
using App.Code.Domain.Journal;

namespace App.Code.Presentation.Runner
{
    public readonly struct ViolationEvent
    {
        public ViolationEvent(string stepTitle, ViolationNotice notice, string message)
        {
            StepTitle = stepTitle;
            Notice = notice;
            Message = message;
        }

        public string StepTitle { get; }

        public ViolationNotice Notice { get; }

        public string Message { get; }
    }

    public interface IScenarioRunner
    {
        event Action StateChanged;

        event Action<ViolationEvent> ViolationRaised;

        event Action<LogRecord> RecordAdded;

        event Action<AnalysisReport> ReportReady;

        IReadOnlyList<string> ModeNames { get; }

        IReadOnlyList<string> AnalyzerNames { get; }

        string CurrentModeName { get; }

        ScenarioSession Session { get; }

        IReadOnlyList<LogRecord> Records { get; }

        string JournalSource { get; }

        AnalysisReport LastReport { get; }

        string LastAnalyzerName { get; }

        string LastError { get; }

        bool TryGetInstruction(string stepId, out string instruction);

        void StartMode(int index);

        void Restart();

        void Analyze(int analyzerIndex);

        bool LoadLatestJournalFile(out string message);
    }
}
