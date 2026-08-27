// Heron-Agent:  HERON-KRN-LOG-006, HERON-MCP-LOG-010
// Heron-Step:   4
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Heron.Core
{
    /// <summary>
    /// The audit trail: one append-only line per request, keyed by Workflow ID.
    ///
    /// Three lines of code now, and the foundation of the cost meter, the
    /// "what did Heron change?" report and the Capability Gap report later
    /// (docs/27 Step 4). Starting it before there is anything to report is the
    /// point - a trail that begins the day writing does is a trail with a hole
    /// exactly where it matters.
    ///
    /// IT LIVES UNDER DATA, NOT DERIVED. Logs are disposable; this is not.
    /// Golden Rule 14 makes every autonomous operation auditable, so the trail
    /// must survive a cache wipe, an update and a reinstall. HeronPaths refuses
    /// to let a cleanup reach it (docs/06 section 2, D-17).
    ///
    /// NEVER PRUNED, for the same reason. The verbose add-in log rotates and is
    /// deleted; this does not. If it ever needs bounding, that is a decision to
    /// be taken deliberately and written down, not a retention default someone
    /// copied across from the log.
    ///
    /// One JSON object per line, so it can be read by eye, by grep, or by a
    /// tool - and so a truncated write costs one entry rather than the file.
    /// </summary>
    public static class HeronAudit
    {
        private static readonly object WriteLock = new object();

        /// <summary>
        /// A new Workflow ID. One request, one id, carried through every layer
        /// that touches it so a single line of the trail can be followed all
        /// the way back.
        /// </summary>
        public static string NewWorkflowId()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 12);
        }

        /// <summary>
        /// Records one request. Never throws: an audit failure must not take
        /// down the operation being audited, and a Revit session must not die
        /// because a disk was full.
        /// </summary>
        public static void Record(
            string workflowId,
            string operation,
            bool succeeded,
            IEnumerable<KeyValuePair<string, string>> fields)
        {
            try
            {
                var line = new StringBuilder();
                line.Append('{');
                Append(line, "at", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
                line.Append(", ");
                Append(line, "workflow", workflowId);
                line.Append(", ");
                Append(line, "op", operation);
                line.Append(", \"ok\": ").Append(succeeded ? "true" : "false");

                if (fields != null)
                {
                    foreach (var field in fields)
                    {
                        if (field.Value == null) continue;
                        line.Append(", ");
                        Append(line, field.Key, field.Value);
                    }
                }
                line.Append('}');

                lock (WriteLock)
                {
                    File.AppendAllText(CurrentFile(), line.ToString() + Environment.NewLine,
                                       new UTF8Encoding(false));
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (ArgumentException) { }
        }

        /// <summary>
        /// One file per month. Enough to keep any single file openable without
        /// ever splitting a day's work across two of them.
        /// </summary>
        private static string CurrentFile()
        {
            return Path.Combine(
                HeronPaths.Audit,
                "audit-" + DateTime.UtcNow.ToString("yyyyMM", CultureInfo.InvariantCulture) + ".jsonl");
        }

        private static void Append(StringBuilder sb, string name, string value)
        {
            sb.Append('"').Append(Escape(name)).Append("\": \"").Append(Escape(value)).Append('"');
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var sb = new StringBuilder(value.Length + 8);
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        // A control character would make the line unreadable to
                        // anything parsing it, which is most of the point.
                        if (c < ' ')
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
