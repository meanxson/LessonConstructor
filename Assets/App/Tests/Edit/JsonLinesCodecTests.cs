using System.Collections.Generic;
using System.IO;
using App.Code.Domain.Analysis;
using App.Code.Domain.Execution;
using App.Code.Domain.Journal;
using App.Code.Infrastructure.Journal;
using NUnit.Framework;
using static App.Tests.Edit.ScenarioTestKit;

namespace App.Tests.Edit
{
    public sealed class JsonLinesCodecTests
    {
        [Test]
        public void EncodeDecode_RoundTripsAllFields()
        {
            var source = new LogRecord(LogRecordTypes.StepViolation, "step \"1\"", LogOutcomes.Retry, 2, 1, ViolationReasons.WrongOrder,
                new Dictionary<string, string> { ["note"] = "line\nbreak Ж" }, 7, 3.25, "2026-01-01T00:00:00Z");

            Assert.IsTrue(JsonLinesCodec.TryDecode(JsonLinesCodec.Encode(source), out var decoded, out var error), error);

            Assert.AreEqual(source.Type, decoded.Type);
            Assert.AreEqual(source.StepId, decoded.StepId);
            Assert.AreEqual(source.Outcome, decoded.Outcome);
            Assert.AreEqual(source.Attempt, decoded.Attempt);
            Assert.AreEqual(source.Group, decoded.Group);
            Assert.AreEqual(source.Reason, decoded.Reason);
            Assert.AreEqual(source.Sequence, decoded.Sequence);
            Assert.AreEqual(source.Time, decoded.Time, 0.001);
            Assert.AreEqual(source.Timestamp, decoded.Timestamp);
            Assert.AreEqual("line\nbreak Ж", decoded.Get("note"));
        }

        [Test]
        public void Decode_KeepsUnknownFieldsAsData()
        {
            Assert.IsTrue(JsonLinesCodec.TryDecode("{\"type\":\"future\",\"severity\":3,\"tag\":\"x\"}", out var record, out _));

            Assert.AreEqual("future", record.Type);
            Assert.AreEqual("3", record.Get("severity"));
            Assert.AreEqual("x", record.Get("tag"));
            Assert.AreEqual(LogRecord.NoValue, record.Attempt);
        }

        [Test]
        public void SavedJournal_IsReanalyzedFromFile()
        {
            var path = Path.Combine(Path.GetTempPath(), $"journal_{System.Guid.NewGuid():N}.jsonl");
            try
            {
                using (var logger = new FanOutLogger(new IScenarioLogger[] { new ScenarioJournal(), new JsonLinesFileLogger(path) }))
                {
                    var session = new ScenarioSession(new ScenarioPlan("file", new[] { Group("g", After("a"), With("b")) }),
                        new TrainingPolicy(), OutOfTurnBehaviour.RecordViolation, logger);
                    session.Start();
                    session.Violate("a", ViolationReasons.WrongAction);
                    session.Complete("a");
                    session.Complete("b");
                }

                File.AppendAllText(path, "not json\n");
                var journal = JsonLinesJournalReader.Read(path);

                Assert.AreEqual(1, journal.Warnings.Count);
                Assert.AreEqual(15, new AttemptScoreAnalyzer().Analyze(journal.Records).Score);
                Assert.AreEqual(100, new PenaltyAnalyzer().Analyze(journal.Records).Score);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
