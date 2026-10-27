using System.Collections.Generic;
using System.Linq;
using App.Code.Domain.Analysis;
using App.Code.Domain.Execution;
using App.Code.Domain.Journal;
using NUnit.Framework;
using static App.Tests.Edit.ScenarioTestKit;

namespace App.Tests.Edit
{
    public sealed class AnalyzerTests
    {
        [Test]
        public void Training_FirstSecondThirdAttempts_Score15()
        {
            var journal = new ScenarioJournal();
            var session = Session(journal, new TrainingPolicy(), Group("g", After("a"), With("b"), With("c")));

            session.Complete("a");
            session.Violate("b", ViolationReasons.WrongAction);
            session.Complete("b");
            session.Violate("c", ViolationReasons.WrongAction);
            session.Violate("c", ViolationReasons.WrongAction);
            session.Complete("c");

            var report = new AttemptScoreAnalyzer().Analyze(journal.Records);

            Assert.AreEqual(15, report.Score);
            Assert.AreEqual(30, report.MaxScore);
            CollectionAssert.AreEqual(new[] { 10d, 5d, 0d }, report.Steps.Select(step => step.Points));
            Assert.IsEmpty(report.Warnings);
        }

        [Test]
        public void Exam_TwoFailedTwoSkippedInGroup_Score70()
        {
            var journal = new ScenarioJournal();
            var session = Session(journal, new ExamPolicy(2),
                Group("first", After("a"), With("b"), With("c"), With("d")),
                Group("second", After("e"), After("f")));

            session.Violate("b", ViolationReasons.WrongAction);
            session.Violate("c", ViolationReasons.WrongAction);
            session.Complete("e");
            session.Complete("f");

            var report = new PenaltyAnalyzer().Analyze(journal.Records);

            Assert.AreEqual(70, report.Score);
            Assert.AreEqual(2, report.Count(StepStatus.Failed));
            Assert.AreEqual(2, report.Count(StepStatus.Skipped));
            Assert.AreEqual(2, report.Count(StepStatus.Completed));
            Assert.IsEmpty(report.Warnings);
        }

        [Test]
        public void Exam_ScoreNeverDropsBelowZero()
        {
            var journal = new ScenarioJournal();
            var groups = Enumerable.Range(0, 6)
                .Select(index => Group($"g{index}", After($"a{index}"), With($"b{index}"), With($"c{index}")))
                .ToArray();
            var session = Session(journal, new ExamPolicy(2), groups);
            for (var index = 0; index < 6; index++)
            {
                session.Violate($"a{index}", ViolationReasons.WrongAction);
                session.Violate($"b{index}", ViolationReasons.WrongAction);
            }

            Assert.AreEqual(0, new PenaltyAnalyzer().Analyze(journal.Records).Score);
        }

        [Test]
        public void SameJournal_CanBeReanalyzedWithAnotherAnalyzer()
        {
            var journal = new ScenarioJournal();
            var session = Session(journal, new ExamPolicy(2), Group("g", After("a"), After("b")));
            session.Complete("a");
            session.Violate("b", ViolationReasons.WrongAction);

            var training = new AttemptScoreAnalyzer().Analyze(journal.Records);
            var exam = new PenaltyAnalyzer().Analyze(journal.Records);

            Assert.AreEqual(10, training.Score);
            Assert.AreEqual(90, exam.Score);
        }

        [Test]
        public void MissingOptionalData_ProducesWarningsInsteadOfFailure()
        {
            var records = new List<LogRecord>
            {
                new LogRecord(LogRecordTypes.StepViolation, "a", reason: ViolationReasons.WrongAction),
                new LogRecord(LogRecordTypes.StepCompleted, "a"),
                new LogRecord("custom_future_type", "a", data: new Dictionary<string, string> { ["extra"] = "1" })
            };

            var report = new AttemptScoreAnalyzer().Analyze(records);

            Assert.AreEqual(5, report.Score);
            Assert.That(report.Warnings.Count, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void EmptyJournal_ReturnsBaseScoreWithWarning()
        {
            var report = new PenaltyAnalyzer().Analyze(new List<LogRecord>());

            Assert.AreEqual(100, report.Score);
            Assert.IsNotEmpty(report.Warnings);
        }
    }
}
