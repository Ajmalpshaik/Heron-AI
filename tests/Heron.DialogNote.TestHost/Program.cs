// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Heron.Bridge;
using Heron.Revit.Addin;

namespace Heron.DialogNote.TestHost
{
    /// <summary>
    /// How Heron answers a Revit dialog met inside a job, checked without
    /// Revit.
    ///
    /// FRAGMENT-ISSUES section 5b row 300: a family saved in a later release
    /// opened Revit's own dialog inside LOAD_FAMILY, and the job hung behind
    /// it for over 90 seconds. HeronDialogWatch now answers such a dialog from
    /// HeronDialogRule and records it in HeronDialogNote. The answer chosen,
    /// the words the modeller reads and the fields the reply carries are what
    /// this checks; whether Revit takes the answer is the PC's to prove.
    ///
    /// THE ANSWER CODES ARE REVIT'S TaskDialogResult values, read out of every
    /// release's RevitAPIUI.dll with `tools/api-surface --members
    /// TaskDialogResult`: Ok=1, Cancel=2, Retry=4, Yes=6, No=7, Close=8,
    /// CommandLink1=1001, on all eight. They are typed here so the checks are
    /// about Revit's numbers, not about the copy the rule keeps.
    /// </summary>
    internal static class Program
    {
        private const int Ok = 1;
        private const int Cancel = 2;
        private const int Retry = 4;
        private const int Yes = 6;
        private const int No = 7;
        private const int Close = 8;
        private const int CommandLink1 = 1001;

        private const string LaterVersion =
            "The file Catch_Basin new.rfa was saved in a later version of Revit\r\n" +
            "and cannot be retrieved in this version.";

        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private static void Check(bool condition, string what)
        {
            _checks++;
            Console.WriteLine((condition ? "  ok    " : "  FAIL  ") + what);
            if (!condition) Failures.Add(what);
        }

        private static bool Has(string text, string part)
        {
            return text != null && text.IndexOf(part, StringComparison.Ordinal) >= 0;
        }

        private static int Main()
        {
            Console.WriteLine("1. the table: a dialog no row names is declined, and the job fails");
            Check(Ok == HeronDialogRule.Ok && Cancel == HeronDialogRule.Cancel
                  && Retry == HeronDialogRule.Retry && Yes == HeronDialogRule.Yes
                  && No == HeronDialogRule.No && Close == HeronDialogRule.Close,
                  "the rule's codes are Revit's TaskDialogResult numbers");

            var unknown = HeronDialogRule.For("Dialog_Nobody_Has_Recorded");
            Check(ReferenceEquals(unknown, HeronDialogRule.Unknown),
                  "an id no row names gets the default");
            Check(ReferenceEquals(HeronDialogRule.For(""), HeronDialogRule.Unknown)
                  && ReferenceEquals(HeronDialogRule.For(null), HeronDialogRule.Unknown),
                  "so does a dialog with no id - a message box often has none");
            Check(unknown.Codes.Length == 2 && unknown.Codes[0] == Cancel && unknown.Codes[1] == Close,
                  "the default tries Cancel, then Close - nothing else");
            Check(unknown.FailsRun, "and the job fails, so its group is rolled back");

            foreach (var picks in new[] { Ok, Yes, Retry, No, CommandLink1 })
            {
                Check(!HeronDialogRule.Declines(picks),
                      HeronDialogNote.AnswerName(picks) + " picks something, so it is not a decline");
            }
            Check(HeronDialogRule.Declines(Cancel) && HeronDialogRule.Declines(Close),
                  "Cancel and Close are the two declines");
            Check(Array.IndexOf(unknown.Codes, No) < 0,
                  "No is never the default - \"Save changes? No\" throws work away");

            Console.WriteLine("\n2. a row may only answer for the owner with the run that showed it");
            var rows = 0;
            foreach (var row in HeronDialogRule.All)
            {
                rows++;
                var problem = HeronDialogRule.Problem(row.Key, row.Value);
                Check(problem == null, "row '" + row.Key + "' may stand" + (problem == null ? "" : ": " + problem));
            }
            Console.WriteLine("  (" + rows + " row(s) in the table - none until a run records one)");

            Check(HeronDialogRule.Problem("Dialog_X", new HeronDialogRule.Answer(new[] { Cancel }, true, null)) == null,
                  "a row that declines and fails needs no evidence - it is the default with a name");
            Check(HeronDialogRule.Problem("Dialog_X", new HeronDialogRule.Answer(new[] { Ok }, true, null)) != null,
                  "a row answering OK with no evidence is refused");
            Check(HeronDialogRule.Problem("Dialog_X", new HeronDialogRule.Answer(new[] { Cancel, Yes }, true, null)) != null,
                  "so is one that only falls back to Yes");
            Check(HeronDialogRule.Problem("Dialog_X", new HeronDialogRule.Answer(new[] { No }, true, null)) != null,
                  "and one answering No - not a decline everywhere");
            Check(HeronDialogRule.Problem("Dialog_X", new HeronDialogRule.Answer(new[] { Cancel }, false, null)) != null,
                  "a row that lets the job carry on is a decision too, and needs its run");
            Check(HeronDialogRule.Problem("Dialog_X",
                      new HeronDialogRule.Answer(new[] { Ok }, false, "Project2, Revit 2024, 2026-10-20")) == null,
                  "with the run named, it may stand");
            Check(HeronDialogRule.Problem("", new HeronDialogRule.Answer(new[] { Cancel }, true, null)) != null
                  && HeronDialogRule.Problem("Dialog_X", new HeronDialogRule.Answer(new int[0], true, null)) != null
                  && HeronDialogRule.Problem("Dialog_X", null) != null,
                  "a row with no id, no answer to try, or no answer at all is refused");

            Console.WriteLine("\n3. the record");
            var none = new HeronDialogNote();
            Check(none.Count == 0 && none.Answered == 0 && !none.Refused && none.RefusedSentence() == "",
                  "a job that met no dialog has nothing to say");

            var later = new HeronDialogNote();
            later.Add("Dialog_Later_Version", "task dialog", LaterVersion, Cancel, true);
            Check(later.Count == 1 && later.Answered == 1 && later.Refused,
                  "row 300's dialog, declined: met once, answered once, and the job stops");
            var sentence = later.RefusedSentence();
            Check(Has(sentence, "\"The file Catch_Basin new.rfa was saved in a later version of Revit and cannot be retrieved in this version.\""),
                  "it quotes Revit's words, on one line");
            Check(Has(sentence, "id Dialog_Later_Version") && Has(sentence, "answered Cancel"),
                  "with the dialog's id and the answer Heron gave");
            Check(Has(sentence, "Heron would not answer it for you"),
                  "and says Heron declined rather than decided");

            var stuck = new HeronDialogNote();
            stuck.Add("", "message box", "Something", 0, false);
            Check(stuck.Refused && stuck.Answered == 0,
                  "a dialog Revit would not let Heron answer stops the job, whatever its row says - it is still on screen");
            Check(Has(stuck.RefusedSentence(), "still open in Revit") && Has(stuck.RefusedSentence(), "no id"),
                  "and the modeller is told it is still open, and that it had no id");

            var allowed = new HeronDialogNote();
            allowed.Add("Dialog_Measured", "task dialog", "Informational", Close, false);
            Check(!allowed.Refused && allowed.RefusedSentence() == "" && allowed.Answered == 1,
                  "a row with evidence that lets the job go on does not stop it");

            var silent = new HeronDialogNote();
            silent.Add("Dialog_Plain", "dialog", null, Cancel, true);
            Check(Has(silent.RefusedSentence(), "a dialog with no text"),
                  "a dialog that carries no words is named as such, never quoted empty");

            var noisy = new HeronDialogNote();
            noisy.Add("x", "task dialog", "  one\ttwo\n\nthree\u0007  ", Cancel, true);
            Check(noisy.Dialogs[0].Message == "one two three",
                  "white space folds to single spaces and control characters go: '" + noisy.Dialogs[0].Message + "'");
            var huge = new HeronDialogNote();
            huge.Add("x", "task dialog", new string('a', HeronDialogNote.MessageLimit * 3), Cancel, true);
            Check(huge.Dialogs[0].Message.Length == HeronDialogNote.MessageLimit + 1
                  && huge.Dialogs[0].Message.EndsWith("…", StringComparison.Ordinal),
                  "a message longer than the limit is cut there, and says so");

            var two = new HeronDialogNote();
            two.Add("A", "task dialog", "first", Cancel, true);
            two.Add("B", "dialog", "second", Close, true);
            Check(Has(two.RefusedSentence(), "Revit showed 2 dialogs") && Has(two.RefusedSentence(), "\"first\"")
                  && Has(two.RefusedSentence(), "\"second\"") && Has(two.RefusedSentence(), "answered Close"),
                  "two dialogs: both quoted, each with its own answer");

            Console.WriteLine("\n4. the refusal says only what Revit confirmed");
            var read = later.Refusal("'load-family'", HeronDialogNote.Outcome.ReadOnly);
            var undone = later.Refusal("'load-family'", HeronDialogNote.Outcome.RolledBack);
            var unsure = later.Refusal("'load-family'", HeronDialogNote.Outcome.RollbackUnconfirmed);
            var kept = later.Refusal("This job", HeronDialogNote.Outcome.NotUndone);

            Check(Json.ReadString(read, "error") == "dialog_refused"
                  && Has(Json.ReadString(read, "message"), "nothing in the model was changed"),
                  "a read: dialog_refused, and nothing changed");
            Check(Json.ReadString(undone, "error") == "dialog_refused"
                  && Has(Json.ReadString(undone, "message"), "rolled back, so the model is exactly as it was"),
                  "a rolled-back write: dialog_refused, and the model is as it was");
            Check(Json.ReadString(unsure, "error") == "dialog_unconfirmed"
                  && Has(Json.ReadString(unsure, "message"), "MAY STILL HOLD")
                  && !Has(Json.ReadString(unsure, "message"), "exactly as it was"),
                  "a rollback Revit did not confirm: dialog_unconfirmed, and never 'as it was'");
            Check(Json.ReadString(kept, "error") == "dialog_unconfirmed"
                  && Has(Json.ReadString(kept, "message"), "MAY STILL BE IN THE MODEL")
                  && !Has(Json.ReadString(kept, "message"), "rolled back"),
                  "a write nothing undid: dialog_unconfirmed, and no rollback is claimed");
            foreach (var refusal in new[] { read, undone, unsure, kept })
            {
                var said = Json.ReadString(refusal, "message");
                Check(Has(said, "Catch_Basin new.rfa") && Has(said, "do this step in Revit yourself"),
                      "every refusal quotes the dialog and says what to do next");
            }
            Check(Has(Json.ReadString(undone, "message"), "'load-family' stopped."),
                  "and names the job first");

            Console.WriteLine("\n5. the reply carries what the job met, and nothing when it met none");
            var success = Json.Ok(Json.Str("ran", "load-family"), Json.Str("document", "Family1"));
            Check(HeronDialogNote.Attach(success, null, true) == success
                  && HeronDialogNote.Attach(success, new HeronDialogNote(), true) == success,
                  "no dialog met: the reply is byte for byte what it was");
            Check(HeronDialogNote.Attach("not json", later, false) == "not json"
                  && HeronDialogNote.Attach("", later, false) == "",
                  "a reply that is not an object is left alone");

            var fromWrite = HeronDialogNote.Attach(undone, later, true);
            Check(Json.ReadString(fromWrite, "error") == "dialog_refused"
                  && Json.ReadString(fromWrite, "message") == Json.ReadString(undone, "message"),
                  "a refusal the write path already made is kept as it is");
            Check(Json.ReadLong(fromWrite, "dialogsAnswered") == 1,
                  "dialogsAnswered is a number the reader takes");
            var listed = Json.ReadObjectArray(fromWrite, "dialogs");
            Check(listed != null && listed.Count == 1
                  && listed[0]["id"] == "Dialog_Later_Version"
                  && listed[0]["kind"] == "task dialog"
                  && listed[0]["answer"] == "Cancel"
                  && Has(listed[0]["message"], "saved in a later version"),
                  "and each dialog is listed with its id, kind, answer and words");

            var readOk = HeronDialogNote.Attach(success, later, false);
            Check(Json.ReadString(readOk, "error") == "dialog_refused"
                  && Has(Json.ReadString(readOk, "message"), "nothing in the model was changed"),
                  "a READ that came back a success after a declined dialog becomes the refusal");
            var writeOk = HeronDialogNote.Attach(success, later, true);
            Check(Json.ReadString(writeOk, "error") == "dialog_unconfirmed"
                  && !Has(Json.ReadString(writeOk, "message"), "rolled back"),
                  "a WRITE that came back a success is never told it was rolled back");

            var other = HeronDialogNote.Attach(Json.Error("fragment_threw", "it threw"), later, true);
            Check(Json.ReadString(other, "error") == "fragment_threw" && Json.ReadLong(other, "dialogsAnswered") == 1,
                  "another error keeps its own code and still carries the dialogs");

            var carried = HeronDialogNote.Attach(success, allowed, true);
            Check(Json.ReadString(carried, "error") == null && Json.ReadString(carried, "ran") == "load-family"
                  && Json.ReadLong(carried, "dialogsAnswered") == 1,
                  "a dialog that does not stop the job leaves the success standing, and is still reported");

            var unanswered = HeronDialogNote.Attach(Json.Error("x", "y"), stuck, false);
            var stuckList = Json.ReadObjectArray(unanswered, "dialogs");
            Check(Json.ReadLong(unanswered, "dialogsAnswered") == 0
                  && stuckList != null && stuckList.Count == 1 && stuckList[0]["id"] == ""
                  && Has(stuckList[0]["answer"], "would not take"),
                  "a dialog Revit would not let Heron answer is listed, and not counted as answered");

            var quoted = new HeronDialogNote();
            quoted.Add("q", "task dialog", "Type \"A\" \\ B", Cancel, true);
            var quotedReply = HeronDialogNote.Attach(success, quoted, false);
            var quotedList = Json.ReadObjectArray(quotedReply, "dialogs");
            Check(quotedList != null && quotedList[0]["message"] == "Type \"A\" \\ B"
                  && Has(Json.ReadString(quotedReply, "message"), "Type \"A\" \\ B"),
                  "quotes and backslashes in Revit's words survive the trip");

            Console.WriteLine();
            if (Failures.Count > 0)
            {
                Console.WriteLine("FAILED - " + Failures.Count + " of " + _checks);
                return 1;
            }
            Console.WriteLine("PASSED - " + _checks + " checks. A dialog met in a job is declined unless a");
            Console.WriteLine("measured row says otherwise, the job stops, and the reply says what Revit asked.");
            return 0;
        }
    }
}
