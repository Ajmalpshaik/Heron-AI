// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Heron.Bridge;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// What Revit said while a fragment was writing, and what Heron did about
    /// it - the judgement and the sentence, with nothing Revit-shaped in it.
    ///
    /// SPLIT OUT OF RevitFragment.cs SO IT CAN BE PROVED WITHOUT REVIT, the
    /// move HeronBindingNote.cs and HeronStackGuard.cs already make: this file
    /// names no Autodesk type, so tests/Heron.FailureNote.TestHost links it BY
    /// SOURCE and there is exactly ONE copy of the rule. RevitFragment's
    /// preprocessor reads what Revit posts, hands the batch here as numbers
    /// and words, and does what this decides.
    ///
    /// THE RULE, AND WHY. When a transaction commits, Revit posts what went
    /// wrong and asks the transaction's preprocessor first. A write with no
    /// preprocessor - which the fragment path had none of until 2026-09-23 -
    /// leaves everything to Revit's own handling, and Revit's handling of an
    /// ERROR is a resolution, and a resolution can be to DELETE the elements
    /// the error names. So:
    ///
    ///   a WARNING   is dismissed and counted, so no dialog can stop a job
    ///               nobody is sitting in front of - and it is REPORTED,
    ///               because dismissing a warning removes the message, not
    ///               what the message is about
    ///   ANYTHING
    ///   ELSE        rolls the whole batch back, and is reported in Revit's
    ///               own words. Revit never resolves it its own way
    ///
    /// ONLY AN EXACT WARNING IS DISMISSED. That is stricter than the move
    /// path's `>= Error` and agrees with it on every severity Revit can post:
    /// FailureSeverity is None=0, Warning=1, Error=2, DocumentCorruption=3 on
    /// all eight releases - read out of each release's RevitAPI.dll with
    /// `tools/api-surface --members FailureSeverity`, not from memory - and
    /// None cannot be posted at all. The two differ only on a value nobody has
    /// met yet: `>= Error` would let one added between Warning and Error
    /// through to Revit's own handling, and this rolls it back. In front of a
    /// possible delete, the rule that fails closed is the only one worth
    /// having.
    ///
    /// A ROLLED-BACK BATCH DISMISSES NOTHING. Its warnings are not counted as
    /// dismissed, because nothing was kept for them to be about - which is
    /// the move path's order too: every message is judged before any warning
    /// is touched.
    ///
    /// EACH ONE NAMES ITS ELEMENTS (FRAGMENT-ISSUES row 5b-1889, issue #1112).
    /// Until 2026-11-21 a warning was counted with how many elements it named
    /// and not WHICH: on Revit 2020 "Line in Sketch is slightly off axis"
    /// (4 elements) came back from three CREATE_FAMILY_FORMS calls, and the
    /// caller could not tell which of its forms to fix. Now every message
    /// carries the ids Revit named, as Revit prints them (ElementId.ToString(),
    /// the way every Heron answer prints an id), and each kind kept here holds
    /// them once each, in the order first named. The sentence names the first
    /// few, the reply lists more and says how many it left out. A message
    /// whose ids could not be read is quoted exactly as before.
    /// </summary>
    internal sealed class HeronFailureNote
    {
        /// <summary>
        /// One failure as Revit posted it: its severity as a number, Revit's
        /// words for it, how many elements it named, and - when they could be
        /// read - which: each id as Revit prints it. Null when they could not.
        /// </summary>
        internal struct Posted
        {
            internal readonly int Severity;
            internal readonly string Text;
            internal readonly int Elements;
            internal readonly IList<string> Ids;

            internal Posted(int severity, string text, int elements)
                : this(severity, text, elements, null)
            {
            }

            internal Posted(int severity, string text, int elements, IList<string> ids)
            {
                Severity = severity;
                Text = text;
                Elements = elements;
                Ids = ids;
            }
        }

        /// <summary>
        /// One distinct message and how often it came. Distinct by the step it
        /// came from AND its words, so a warning a setup step raised is never
        /// folded into one the fragment under test raised.
        ///
        /// `Elements` counts every element each time, repeats included, as it
        /// always did; `Ids` holds each element named ONCE, in the order Revit
        /// first named it (row 5b-1889). So one element named twice reads
        /// "2 times, 2 elements" beside one id - and both are true.
        /// </summary>
        internal sealed class Said
        {
            internal string Step;       // null: the fragment itself
            internal string Text;
            internal int Times;
            internal int Elements;
            internal readonly List<string> Ids = new List<string>();
        }

        /// <summary>
        /// How many DIFFERENT warnings the dismissed sentence quotes. The rest
        /// are counted and said to exist, never dropped, and every one is in
        /// `Dismissed` for the reply to carry in full.
        ///
        /// What stopped a job is never cut short: a refusal has no list beside
        /// it to carry the rest, and it is the one answer a modeller needs
        /// whole.
        /// </summary>
        internal const int Quoted = 5;

        /// <summary>
        /// How many element ids the SENTENCE names for one kind; the rest are
        /// counted and said to exist. The chat prints the sentence whole, so
        /// it stays short - the reply lists up to `IdsListed`.
        /// </summary>
        internal const int IdsQuoted = 5;

        /// <summary>
        /// How many element ids the REPLY lists for one kind. What it leaves
        /// out is counted beside them (`elementIdsMore`), never dropped.
        /// </summary>
        internal const int IdsListed = 50;

        private readonly List<Said> _dismissed = new List<Said>();
        private readonly List<Said> _refused = new List<Said>();

        /// <summary>Every warning dismissed, repeats included.</summary>
        internal int DismissedCount { get; private set; }

        /// <summary>Every failure that rolled a batch back, repeats included.</summary>
        internal int RefusedCount { get; private set; }

        internal IList<Said> Dismissed { get { return _dismissed.AsReadOnly(); } }
        internal IList<Said> Refused { get { return _refused.AsReadOnly(); } }

        /// <summary>
        /// Judge one batch - everything Revit posted at one commit. True when
        /// the transaction must be ROLLED BACK.
        ///
        /// True: every failure that is not a warning is recorded as refused,
        /// and nothing is recorded as dismissed. False: every failure was a
        /// warning, each one is recorded as dismissed, and the caller deletes
        /// exactly those. `warning` is the caller's own
        /// `(int)FailureSeverity.Warning`, passed in rather than typed here so
        /// that this file and Revit's enum cannot come to disagree.
        /// </summary>
        internal bool RollsBack(string step, IList<Posted> batch, int warning)
        {
            if (batch == null || batch.Count == 0) return false;

            var refusing = false;
            foreach (var one in batch)
            {
                if (one.Severity != warning) { refusing = true; break; }
            }

            if (refusing)
            {
                foreach (var one in batch)
                {
                    if (one.Severity == warning) continue;
                    Add(_refused, step, one.Text, one.Elements, one.Ids);
                    RefusedCount++;
                }
                return true;
            }

            foreach (var one in batch)
            {
                Add(_dismissed, step, one.Text, one.Elements, one.Ids);
                DismissedCount++;
            }
            return false;
        }

        /// <summary>
        /// A warning Revit would not let Heron dismiss. The caller rolls the
        /// job back rather than leave it to Revit's own handling, so it moves
        /// OUT of what was dismissed - it never was - and into what stopped
        /// the job, with `why` beside it.
        ///
        /// `text` MUST BE THE WARNING'S OWN WORDS, exactly as RollsBack was
        /// given them: they are what finds its entry among the dismissed. The
        /// reason Revit gave for refusing travels separately, so appending it
        /// cannot leave the warning counted as dismissed.
        /// </summary>
        internal void CouldNotDismiss(string step, string text, int elements, string why)
        {
            CouldNotDismiss(step, text, elements, null, why);
        }

        /// <summary>
        /// The same, with the ids the warning named (row 5b-1889): they go
        /// with it to what stopped the job. An id another time of the same
        /// warning also named stays with the dismissed kind as well - which
        /// time named which is not kept - and the job rolls back either way.
        /// </summary>
        internal void CouldNotDismiss(string step, string text, int elements,
                                      IList<string> ids, string why)
        {
            var words = Words(text);
            for (var i = 0; i < _dismissed.Count; i++)
            {
                var said = _dismissed[i];
                if (said.Step != step || said.Text != words) continue;

                said.Times--;
                said.Elements = System.Math.Max(0, said.Elements - System.Math.Max(0, elements));
                DismissedCount--;
                if (said.Times <= 0) _dismissed.RemoveAt(i);
                break;
            }

            Add(_refused, step,
                "a warning Heron could not dismiss: " + words +
                (string.IsNullOrEmpty(why) ? "" : " (" + why.Trim() + ")"), elements, ids);
            RefusedCount++;
        }

        /// <summary>
        /// Revit's list of failures could not be read at all. Nothing can be
        /// judged, so nothing is kept - and the reason is recorded, because a
        /// rollback with no stated cause reads as Heron breaking.
        /// </summary>
        internal void Unreadable(string step, string why)
        {
            Add(_refused, step,
                "Revit's list of what went wrong could not be read (" + Words(why) + ")", 0, null);
            RefusedCount++;
        }

        /// <summary>
        /// What was dismissed, as one sentence a modeller can read. Empty when
        /// nothing was, so a clean run gains no words.
        /// </summary>
        internal string DismissedSentence()
        {
            if (DismissedCount == 0) return "";

            var many = DismissedCount != 1;
            return "Revit raised " + Count(DismissedCount, "warning", "warnings")
                 + " while this ran, and Heron dismissed " + (many ? "them" : "it")
                 + " rather than let a dialog stop the job: "
                 + Quote(_dismissed, Quoted)
                 + ". Dismissing a warning removes the message, not its cause - check what "
                 + (many ? "they name" : "it names") + " before relying on this.";
        }

        /// <summary>
        /// What stopped the job, in Revit's words, every one of them. Empty
        /// when nothing did.
        /// </summary>
        internal string RefusedSentence()
        {
            if (RefusedCount == 0) return "";
            return "Revit would not let it through: " + Quote(_refused, int.MaxValue) + ".";
        }

        /// <summary>
        /// One kept kind as the reply lists it in `warningsDismissed`: Revit's
        /// words, how often, how many elements, the setup step, and (row
        /// 5b-1889) the ids it named, as JSON strings, up to `IdsListed`, with
        /// `elementIdsMore` beside a list cut short. A kind that named no id
        /// that could be read carries no `elementIds`, so its object reads
        /// exactly as it did. The ONE writer of that object - RevitFragment
        /// and the test host both call it.
        /// </summary>
        internal static string Listed(Said one)
        {
            if (one == null) return "";
            var ids = new List<string>();
            for (var i = 0; i < one.Ids.Count && i < IdsListed; i++) ids.Add(JsonText(one.Ids[i]));
            var more = one.Ids.Count - ids.Count;
            return Json.Obj(Json.Str("text", one.Text),
                            Json.Num("times", one.Times),
                            Json.Num("elements", one.Elements),
                            Json.Str("step", one.Step),
                            ids.Count == 0 ? null : Json.Arr("elementIds", ids),
                            more > 0 ? Json.Num("elementIdsMore", more) : null);
        }

        // ------------------------------------------------------------ helpers

        private static void Add(List<Said> into, string step, string text, int elements,
                                IList<string> ids)
        {
            var words = Words(text);
            var count = elements < 0 ? 0 : elements;
            Said kind = null;
            foreach (var said in into)
            {
                if (said.Step == step && said.Text == words)
                {
                    kind = said;
                    break;
                }
            }

            if (kind == null)
            {
                kind = new Said { Step = step, Text = words };
                into.Add(kind);
            }
            kind.Times++;
            kind.Elements += count;

            // EACH ID ONCE, IN THE ORDER FIRST NAMED (row 5b-1889). A blank id
            // is not one: it would print as a gap in the list.
            if (ids == null) return;
            foreach (var id in ids)
            {
                var trimmed = id == null ? "" : id.Trim();
                if (trimmed.Length == 0 || kind.Ids.Contains(trimmed)) continue;
                kind.Ids.Add(trimmed);
            }
        }

        /// <summary>
        /// An id as a JSON string value. Json.Str writes a name AND a value
        /// and an array holds bare values, so the escaping is borrowed from
        /// Str by writing a throwaway pair and keeping its value half - the
        /// move HeronChangeReport makes for `firstIds`. An id is digits from
        /// ElementId.ToString(), so the escaping is a guard, not a path a real
        /// id takes.
        /// </summary>
        private static string JsonText(string id)
        {
            var pair = Json.Str("x", id ?? "");
            return pair.Substring(pair.IndexOf(':') + 2);
        }

        /// <summary>
        /// "element ids 1001, 1002" - the first `IdsQuoted`, then how many
        /// more (row 5b-1889). Empty when none was read, so the quote is
        /// exactly what it was.
        /// </summary>
        private static string Named(List<string> ids)
        {
            if (ids == null || ids.Count == 0) return "";
            var shown = new List<string>();
            for (var i = 0; i < ids.Count && i < IdsQuoted; i++) shown.Add(ids[i]);
            var left = ids.Count - shown.Count;
            return (ids.Count == 1 ? "element id " : "element ids ")
                 + string.Join(", ", shown.ToArray())
                 + (left > 0 ? " and " + left.ToString(CultureInfo.InvariantCulture) + " more" : "");
        }

        /// <summary>
        /// The words, or a sentence saying there were none. An empty quote
        /// would read as Heron dropping what Revit said.
        /// </summary>
        private static string Words(string text)
        {
            var trimmed = text == null ? "" : text.Trim();
            return trimmed.Length == 0 ? "(Revit gave no description)" : trimmed;
        }

        private static string Quote(List<Said> said, int limit)
        {
            var parts = new List<string>();
            for (var i = 0; i < said.Count && i < limit; i++)
            {
                var entry = said[i];
                var detail = new List<string>(4);
                if (entry.Times > 1) detail.Add(Count(entry.Times, "time", "times"));
                if (entry.Elements > 0) detail.Add(Count(entry.Elements, "element", "elements"));
                var named = Named(entry.Ids);
                if (named.Length > 0) detail.Add(named);
                if (entry.Step != null) detail.Add("in setup step '" + entry.Step + "'");

                var text = new StringBuilder("\"").Append(entry.Text).Append('"');
                if (detail.Count > 0)
                    text.Append(" (").Append(string.Join(", ", detail.ToArray())).Append(')');
                parts.Add(text.ToString());
            }

            var line = string.Join("; ", parts.ToArray());
            var left = said.Count - limit;
            if (left > 0)
                line += "; and " + Count(left, "more kind", "more kinds") + ", listed with this answer";
            return line;
        }

        private static string Count(int number, string one, string many)
        {
            return number.ToString(CultureInfo.InvariantCulture) + " " + (number == 1 ? one : many);
        }
    }
}
