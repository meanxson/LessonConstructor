using System.Globalization;
using System.Linq;
using System.Text;
using App.Code.Domain.Journal;

namespace App.Code.Presentation.Logging
{
    public static class JournalFormatter
    {
        public static string Line(LogRecord record)
        {
            var builder = new StringBuilder();
            builder.Append('#').Append(record.Sequence.ToString(CultureInfo.InvariantCulture))
                .Append(" t=").Append(record.Time.ToString("0.0", CultureInfo.InvariantCulture))
                .Append(' ').Append(record.Type);
            if (record.HasStep) builder.Append(" step=").Append(record.StepId);
            if (record.Outcome.Length > 0) builder.Append(" outcome=").Append(record.Outcome);
            if (record.Attempt != LogRecord.NoValue) builder.Append(" attempt=").Append(record.Attempt);
            if (record.Group != LogRecord.NoValue) builder.Append(" group=").Append(record.Group + 1);
            if (record.Reason.Length > 0) builder.Append(" reason=").Append(record.Reason);
            foreach (var pair in record.Data.OrderBy(pair => pair.Key)) builder.Append(' ').Append(pair.Key).Append('=').Append(pair.Value);
            return builder.ToString();
        }
    }
}
