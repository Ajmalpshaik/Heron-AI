// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Heron.Bridge;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// How Heron answers a Revit dialog that opens while one of its jobs is
    /// running - the table, with nothing Revit-shaped in it.
    ///
    /// WHY THERE IS AN ANSWER AT ALL. A modal dialog opened inside a job stops
    /// the job: Revit waits for a click, the job waits for Revit, and the chat
    /// waits for the job. FRAGMENT-ISSUES section 5b row 300, measured
    /// 2026-10-03 on Revit 2024: LOAD_FAMILY given a family saved in a later
    /// release opened Revit's own "saved in a later version" dialog, and the
    /// chat read unknown_outcome, then revit_busy, then still_running for over
    /// 90 seconds until somebody closed it by hand. HeronDialogWatch answers
    /// it instead, from this table.
    ///
    /// WHY THE ANSWER IS ALWAYS "NO" UNLESS A ROW SAYS OTHERWISE. A dialog is
    /// Revit asking the person a question, and the person is not there - only
    /// a job is. Answering OK or Yes would decide the change on the owner's
    /// behalf (Article 7): "Duplicate Types" during a view transfer is exactly
    /// that question, and an automatic OK would choose whose types win. So a
    /// dialog no row names is answered Cancel, or Close if Revit will not take
    /// Cancel, and the run FAILS - the job's transaction group is rolled back
    /// and the modeller is told what Revit asked, in Revit's words.
    ///
    /// THE TABLE HOLDS NO ROW YET, AND THAT IS THE HONEST STATE. A row needs a
    /// DialogId Revit really reported and a run that showed the answer is
    /// right, and none has been recorded: the ids are not published, and a
    /// guessed id is a row that never matches - or worse, matches something
    /// else. Every dialog met is recorded on the reply with its id, so the
    /// first rows come from real runs (NEEDS-CHECKING group EJ).
    ///
    /// THE NUMBERS ARE REVIT'S. TaskDialogResult is None=0, Ok=1, Cancel=2,
    /// Retry=4, Yes=6, No=7, Close=8, CommandLink1..4=1001..1004 on all eight
    /// releases - read with `tools/api-surface --members TaskDialogResult`,
    /// not from memory - and a message box answers with the same Windows
    /// result codes.
    /// </summary>
    internal static class HeronDialogRule
    {
        internal const int Ok = 1;
        internal const int Cancel = 2;
        internal const int Retry = 4;
        internal const int Yes = 6;
        internal const int No = 7;
        internal const int Close = 8;

        /// <summary>One row: the answers to try, in order, and what follows.</summary>
        internal sealed class Answer
        {
            /// <summary>Tried in order until Revit accepts one.</summary>
            internal readonly int[] Codes;

            /// <summary>Whether meeting this dialog stops the job and rolls it back.</summary>
            internal readonly bool FailsRun;

            /// <summary>
            /// The run that showed this answer is right - model, release,
            /// date. Null only for the default, which needs none because it
            /// decides nothing.
            /// </summary>
            internal readonly string Evidence;

            internal Answer(int[] codes, bool failsRun, string evidence)
            {
                Codes = codes ?? new int[0];
                FailsRun = failsRun;
                Evidence = evidence;
            }
        }

        /// <summary>
        /// A dialog no row names: Cancel, then Close, and the job fails.
        ///
        /// NOT No. "No" is not a refusal everywhere - "Save changes? No"
        /// throws work away. Cancel and Close are the only two answers that
        /// mean "do not do what you were about to do" on every dialog.
        /// </summary>
        internal static readonly Answer Unknown = new Answer(new[] { Cancel, Close }, true, null);

        /// <summary>
        /// Rows keyed by Revit's DialogId, compared exactly. EMPTY UNTIL A RUN
        /// RECORDS ONE - see the class summary. Problem() says what a row
        /// must carry, and the test host holds every row here to it.
        /// </summary>
        private static readonly Dictionary<string, Answer> Rows =
            new Dictionary<string, Answer>(StringComparer.Ordinal);

        /// <summary>The answer for one DialogId. Never null.</summary>
        internal static Answer For(string dialogId)
        {
            Answer row;
            if (!string.IsNullOrEmpty(dialogId) && Rows.TryGetValue(dialogId, out row) && row != null)
                return row;
            return Unknown;
        }

        /// <summary>Every row, for the test host to check.</summary>
        internal static IEnumerable<KeyValuePair<string, Answer>> All
        {
            get { return Rows; }
        }

        /// <summary>
        /// Cancel and Close: the two answers that decline. Everything else -
        /// Ok, Yes, Retry, No, a command link - picks something.
        /// </summary>
        internal static bool Declines(int code)
        {
            return code == Cancel || code == Close;
        }

        /// <summary>
        /// What is wrong with a row, or null when it may stand.
        ///
        /// A row that declines and fails the run is the default with a name,
        /// and is always allowed. A row that picks an answer, or that lets the
        /// job carry on, is a decision made for the owner - so it must name
        /// the run that showed it is right.
        /// </summary>
        internal static string Problem(string dialogId, Answer row)
        {
            if (string.IsNullOrEmpty(dialogId)) return "a row with no DialogId can never match";
            if (row == null) return "row '" + dialogId + "' has no answer";
            if (row.Codes.Length == 0) return "row '" + dialogId + "' tries no answer at all";

            var decides = !row.FailsRun;
            foreach (var code in row.Codes)
                if (!Declines(code)) decides = true;

            if (decides && string.IsNullOrEmpty(row.Evidence))
            {
                return "row '" + dialogId + "' answers for the owner - it picks an answer or lets "
                     + "the job go on - and names no run that showed that is right";
            }
            return null;
        }
    }

    /// <summary>
    /// Every Revit dialog one job met, and what Heron answered - the record
    /// and the sentences, proved without Revit by
    /// tests/Heron.DialogNote.TestHost.
    ///
    /// HeronFailureNote's shape, for the same reason: this file names no
    /// Autodesk type, so the test host links it BY SOURCE and there is one
    /// copy of what the reply says. HeronDialogWatch reads the dialog, asks
    /// HeronDialogRule, and records the result here.
    /// </summary>
    internal sealed class HeronDialogNote
    {
        /// <summary>
        /// What the job that met a refused dialog left behind - each a
        /// different sentence, because each is a different thing to do next.
        /// </summary>
        internal enum Outcome
        {
            /// <summary>A read: no transaction was open, so nothing changed.</summary>
            ReadOnly,

            /// <summary>Revit reported the group rolled back.</summary>
            RolledBack,

            /// <summary>The rollback was asked for and Revit did not report it.</summary>
            RollbackUnconfirmed,

            /// <summary>
            /// The job wrote and nothing rolled it back - a write path that
            /// does not check the watch. None does today; the dispatcher says
            /// this rather than claim an undo nobody asked for.
            /// </summary>
            NotUndone,
        }

        /// <summary>One dialog, as Revit showed it and as Heron answered it.</summary>
        internal sealed class Met
        {
            internal string Id;         // Revit's DialogId; "" when it gave none
            internal string Kind;       // task dialog, message box, dialog
            internal string Message;    // Revit's words, one line; "" when the kind has none
            internal int Answer;        // the code Revit accepted; 0 when it accepted none
            internal bool FailsRun;
        }

        /// <summary>
        /// How much of Revit's message one entry keeps. A dialog can carry a
        /// whole report, and the reply is one line on a pipe.
        /// </summary>
        internal const int MessageLimit = 400;

        private readonly List<Met> _met = new List<Met>();

        internal IList<Met> Dialogs { get { return _met.AsReadOnly(); } }

        /// <summary>Every dialog met while the job was armed.</summary>
        internal int Count { get { return _met.Count; } }

        /// <summary>Every dialog Revit let Heron answer.</summary>
        internal int Answered { get; private set; }

        /// <summary>
        /// Whether the job must stop: a dialog whose row fails the run, or
        /// one Revit would not let Heron answer at all - that one is still on
        /// screen, and a job that carried on would be reporting around it.
        /// </summary>
        internal bool Refused { get; private set; }

        /// <summary>
        /// Record one dialog. `answer` is the code Revit ACCEPTED - 0 when it
        /// took none of them.
        /// </summary>
        internal void Add(string id, string kind, string message, int answer, bool failsRun)
        {
            var met = new Met
            {
                Id = id ?? "",
                Kind = string.IsNullOrEmpty(kind) ? "dialog" : kind,
                Message = Words(message),
                Answer = answer,
                FailsRun = failsRun,
            };
            _met.Add(met);
            if (answer != 0) Answered++;
            if (failsRun || answer == 0) Refused = true;
        }

        /// <summary>The name of an answer code, for a person to read.</summary>
        internal static string AnswerName(int code)
        {
            switch (code)
            {
                case 0: return "none - Revit would not take an answer";
                case HeronDialogRule.Ok: return "OK";
                case HeronDialogRule.Cancel: return "Cancel";
                case HeronDialogRule.Retry: return "Retry";
                case HeronDialogRule.Yes: return "Yes";
                case HeronDialogRule.No: return "No";
                case HeronDialogRule.Close: return "Close";
            }
            return "code " + code.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The dialogs that stopped the job, in Revit's words, every one.
        /// Empty when none did.
        /// </summary>
        internal string RefusedSentence()
        {
            var stopped = new List<Met>();
            foreach (var met in _met)
                if (met.FailsRun || met.Answer == 0) stopped.Add(met);
            if (stopped.Count == 0) return "";

            var parts = new List<string>(stopped.Count);
            foreach (var met in stopped)
            {
                var text = new StringBuilder();
                text.Append(met.Message.Length == 0 ? "a " + met.Kind + " with no text" : "\"" + met.Message + "\"");
                text.Append(" (").Append(met.Kind);
                text.Append(met.Id.Length == 0 ? ", no id" : ", id " + met.Id);
                text.Append(met.Answer == 0
                    ? ", which Revit would not let Heron close - it is still open in Revit"
                    : ", answered " + AnswerName(met.Answer));
                text.Append(')');
                parts.Add(text.ToString());
            }

            var one = stopped.Count == 1;
            return "Revit showed " + (one ? "a dialog" : stopped.Count.ToString(CultureInfo.InvariantCulture) + " dialogs")
                 + " while this ran, and Heron would not answer " + (one ? "it" : "them")
                 + " for you: " + string.Join("; ", parts.ToArray()) + ".";
        }

        /// <summary>
        /// The refusal for a job a dialog stopped, as the error the reply
        /// carries. `job` names it - "'load-family'", or "This job".
        /// </summary>
        internal string Refusal(string job, Outcome outcome)
        {
            var told = (string.IsNullOrEmpty(job) ? "This job" : job) + " stopped. " + RefusedSentence();

            switch (outcome)
            {
                case Outcome.ReadOnly:
                    told += " This job only reads, so nothing in the model was changed.";
                    break;
                case Outcome.RolledBack:
                    told += " The whole job was rolled back, so the model is exactly as it was.";
                    break;
                case Outcome.RollbackUnconfirmed:
                    told += " Heron asked Revit to roll the job back and Revit did not report that "
                          + "it had, so THE MODEL MAY STILL HOLD PART OF THIS CHANGE. Check it before "
                          + "saving.";
                    break;
                default:
                    told += " The job had already finished and nothing undid it, so WHATEVER IT DID "
                          + "MAY STILL BE IN THE MODEL. Check it before saving, and use Undo if it did "
                          + "something you did not want.";
                    break;
            }

            told += " If the dialog needs an answer, do this step in Revit yourself, where you can "
                  + "give it.";

            // Each code spelled out in its own call: tests/test_failure_analysis.py
            // finds the codes the add-in can produce by reading Json.Error calls,
            // and holds mcp/server/heron_failure.py to every one.
            return outcome == Outcome.ReadOnly || outcome == Outcome.RolledBack
                ? Json.Error("dialog_refused", told)
                : Json.Error("dialog_unconfirmed", told);
        }

        /// <summary>
        /// The reply as it leaves Revit, with the dialogs the job met on it.
        ///
        /// NOTHING IS ADDED WHEN NONE WAS MET, so every reply from a job no
        /// dialog interrupted reads exactly as it did. A reply that came back
        /// a SUCCESS from a job a dialog stopped - a path that does not check
        /// the watch - is turned into the refusal here: a read changed
        /// nothing, and a write is told plainly that nothing undid it.
        /// </summary>
        internal static string Attach(string response, HeronDialogNote note, bool writes)
        {
            if (note == null || note.Count == 0) return response;
            if (string.IsNullOrEmpty(response) || !response.EndsWith("}", StringComparison.Ordinal))
                return response;

            if (note.Refused && Json.ReadString(response, "error") == null)
                response = note.Refusal("This job", writes ? Outcome.NotUndone : Outcome.ReadOnly);

            var listed = new List<string>(note.Count);
            foreach (var met in note._met)
            {
                listed.Add(Json.Obj(Json.Str("id", met.Id),
                                    Json.Str("kind", met.Kind),
                                    Json.Str("message", met.Message),
                                    Json.Str("answer", AnswerName(met.Answer)),
                                    Json.Bool("stoppedTheJob", met.FailsRun || met.Answer == 0)));
            }

            return response.Substring(0, response.Length - 1)
                 + ", " + Json.Num("dialogsAnswered", note.Answered)
                 + ", " + Json.Arr("dialogs", listed) + "}";
        }

        /// <summary>
        /// Revit's words as one bounded line: control characters dropped,
        /// every run of white space one space.
        /// </summary>
        private static string Words(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var kept = new StringBuilder(Math.Min(text.Length, MessageLimit + 1));
            var space = false;
            foreach (var c in text)
            {
                if (char.IsWhiteSpace(c)) { space = kept.Length > 0; continue; }
                if (char.IsControl(c)) continue;
                if (space) { kept.Append(' '); space = false; }
                if (kept.Length >= MessageLimit) { kept.Append('…'); break; }
                kept.Append(c);
            }
            return kept.ToString();
        }
    }
}
