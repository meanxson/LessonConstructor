using System.Linq;
using App.Code.Domain.Execution;
using App.Code.Domain.Journal;
using NUnit.Framework;
using static App.Tests.Edit.ScenarioTestKit;

namespace App.Tests.Edit
{
    public sealed class ScenarioSessionTests
    {
        [Test]
        public void MixedActivation_OpensBatchesInConfiguredOrder()
        {
            var group = Group("all",
                After("s1"), With("s2"), With("s3"), With("s4"),
                After("s5"), After("s6"), After("s7"), After("s8"),
                After("s9"), With("s10"),
                After("s11"), With("s12"));
            var session = Session(new ScenarioJournal(), new TrainingPolicy(), group);

            CollectionAssert.AreEqual(new[] { "s1", "s2", "s3", "s4" }, session.ActiveStepIds);

            Assert.IsTrue(session.Complete("s3"));
            Assert.IsTrue(session.Complete("s1"));
            Assert.IsTrue(session.Complete("s4"));
            CollectionAssert.AreEqual(new[] { "s2" }, session.ActiveStepIds);

            Assert.IsTrue(session.Complete("s2"));
            foreach (var id in new[] { "s5", "s6", "s7", "s8" })
            {
                CollectionAssert.AreEqual(new[] { id }, session.ActiveStepIds);
                Assert.IsTrue(session.Complete(id));
            }

            CollectionAssert.AreEqual(new[] { "s9", "s10" }, session.ActiveStepIds);
            Assert.IsTrue(session.Complete("s10"));
            Assert.IsTrue(session.Complete("s9"));
            CollectionAssert.AreEqual(new[] { "s11", "s12" }, session.ActiveStepIds);
            Assert.IsTrue(session.Complete("s12"));
            Assert.IsTrue(session.Complete("s11"));
            Assert.IsTrue(session.IsFinished);
        }

        [Test]
        public void CompletingPendingStep_IsRejected()
        {
            var session = Session(new ScenarioJournal(), new TrainingPolicy(), Group("g", After("a"), After("b")));

            Assert.IsFalse(session.Complete("b"));
            Assert.AreEqual(StepStatus.Pending, session.GetStatus("b"));
        }

        [Test]
        public void Training_ViolationKeepsStepActiveAndCountsAttempt()
        {
            var journal = new ScenarioJournal();
            var session = Session(journal, new TrainingPolicy(), Group("g", After("a"), With("b")));

            session.Violate("a", ViolationReasons.WrongAction);

            Assert.AreEqual(StepStatus.Active, session.GetStatus("a"));
            Assert.AreEqual(StepStatus.Active, session.GetStatus("b"));
            Assert.AreEqual(2, session.GetAttempt("a"));

            session.Complete("a");
            Assert.AreEqual(2, journal.Records.Single(record => record.Type == LogRecordTypes.StepCompleted).Attempt);
        }

        [Test]
        public void Exam_TwoViolationsSkipRestOfGroupAndStartNextGroup()
        {
            var session = Session(new ScenarioJournal(), new ExamPolicy(2),
                Group("first", After("a"), With("b"), With("c"), With("d")),
                Group("second", After("e")));

            session.Violate("a", ViolationReasons.WrongAction);
            Assert.AreEqual(StepStatus.Failed, session.GetStatus("a"));
            Assert.AreEqual(0, session.CurrentGroupIndex);

            session.Violate("b", ViolationReasons.WrongAction);

            Assert.AreEqual(StepStatus.Failed, session.GetStatus("b"));
            Assert.AreEqual(StepStatus.Skipped, session.GetStatus("c"));
            Assert.AreEqual(StepStatus.Skipped, session.GetStatus("d"));
            Assert.AreEqual(1, session.CurrentGroupIndex);
            CollectionAssert.AreEqual(new[] { "e" }, session.ActiveStepIds);
        }

        [Test]
        public void Exam_FailedStepStillLetsSequenceContinue()
        {
            var session = Session(new ScenarioJournal(), new ExamPolicy(2), Group("g", After("a"), After("b")));

            session.Violate("a", ViolationReasons.WrongAction);

            CollectionAssert.AreEqual(new[] { "b" }, session.ActiveStepIds);
        }

        [Test]
        public void OutOfTurn_RecordsViolationByDefault()
        {
            var journal = new ScenarioJournal();
            var session = Session(journal, new ExamPolicy(2), Group("g", After("a"), After("b"), After("c")));

            Assert.IsTrue(session.ReportOutOfTurn("c"));
            Assert.AreEqual(StepStatus.Pending, session.GetStatus("c"));
            Assert.AreEqual(1, session.CurrentGroupViolations);
            Assert.AreEqual(ViolationReasons.OutOfTurn, journal.Records.Last(record => record.Type == LogRecordTypes.StepViolation).Reason);
        }

        [Test]
        public void OutOfTurn_IgnoredWhenConfigured()
        {
            var journal = new ScenarioJournal();
            var session = Session(journal, new ExamPolicy(2), OutOfTurnBehaviour.Ignore, Group("g", After("a"), After("b")));

            Assert.IsFalse(session.ReportOutOfTurn("b"));
            Assert.AreEqual(0, session.CurrentGroupViolations);
            Assert.IsFalse(journal.Records.Any(record => record.Type == LogRecordTypes.StepViolation));
        }

        [Test]
        public void DuplicateStepInPlan_IsRejected()
        {
            Assert.Throws<System.ArgumentException>(() =>
                new ScenarioPlan("dup", new[] { Group("a", After("x")), Group("b", After("x")) }));
        }
    }
}
