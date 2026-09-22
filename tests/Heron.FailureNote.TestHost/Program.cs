// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Heron.Revit.Addin;

namespace Heron.FailureNote.TestHost
{
    /// <summary>
    /// The fragment write path's failure rule, checked without Revit.
    ///
    /// Until 2026-09-23 a fragment that wrote had NO failure handling: every
    /// warning and every error Revit posted at commit went to Revit's own
    /// handling, and for an error that handling offers resolutions - one of
    /// which can be to delete the elements involved. RevitFragment now reads
    /// what Revit posts and asks HeronFailureNote what to do. That judgement,
    /// and the sentence the modeller reads, are what this checks; whether
    /// Revit then does what it is told is the PC's to prove.
    ///
    /// THE SEVERITY NUMBERS BELOW ARE REVIT'S, read out of every release's
    /// RevitAPI.dll with `tools/api-surface --members FailureSeverity`:
    /// None=0, Warning=1, Error=2, DocumentCorruption=3, on all eight. The
    /// production call passes `(int)FailureSeverity.Warning` itself, so these
    /// are the values the checks have to be about, not a copy the code uses.
    /// </summary>
    internal static class Program
    {
        private const int None = 0;
        private const int Warning = 1;
        private const int Error = 2;
        private const int DocumentCorruption = 3;

        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private static void Check(bool condition, string what)
        {
            _checks++;
            Console.WriteLine((condition ? "  ok    " : "  FAIL  ") + what);
            if (!condition) Failures.Add(what);
        }

        private static List<HeronFailureNote.Posted> Batch(params HeronFailureNote.Posted[] posted)
        {
            return new List<HeronFailureNote.Posted>(posted);
        }

        private static HeronFailureNote.Posted Said(int severity, string text, int elements)
        {
            return new HeronFailureNote.Posted(severity, text, elements);
        }

        private static int Main()
        {
            Console.WriteLine("A fragment write never leaves a failure to Revit's own handling");
            Console.WriteLine();

            // 1. WARNINGS ONLY: dismissed, counted, and nothing refused.
            var note = new HeronFailureNote();
            var back = note.RollsBack(null, Batch(
                Said(Warning, "Duct is slightly off axis and may cause inaccuracies.", 1),
                Said(Warning, "Duct is slightly off axis and may cause inaccuracies.", 2),
                Said(Warning, "Elements have duplicate 'Mark' values.", 2)), Warning);
            Check(!back, "a batch of warnings only is KEPT - no rollback");
            Check(note.DismissedCount == 3, "all three are counted as dismissed (" + note.DismissedCount + ")");
            Check(note.RefusedCount == 0, "and nothing is refused");
            Check(note.Dismissed.Count == 2, "the same words twice are ONE entry, not two (" + note.Dismissed.Count + ")");
            Check(note.Dismissed[0].Times == 2 && note.Dismissed[0].Elements == 3,
                  "and that entry says 2 times, 3 elements - " + note.Dismissed[0].Times + " times, "
                  + note.Dismissed[0].Elements + " elements");

            var sentence = note.DismissedSentence();
            Check(sentence.StartsWith("Revit raised 3 warnings", StringComparison.Ordinal),
                  "the sentence says how many were raised: \"" + sentence + "\"");
            Check(sentence.Contains("\"Duct is slightly off axis and may cause inaccuracies.\" (2 times, 3 elements)"),
                  "it QUOTES what Revit said, with how often and how many elements");
            Check(sentence.Contains("\"Elements have duplicate 'Mark' values.\" (2 elements)"),
                  "and quotes the second kind too");
            Check(sentence.Contains("removes the message, not its cause"),
                  "and says a dismissed warning is still true about the model");
            Check(note.RefusedSentence() == "", "a run nothing was refused in adds no refusal words");

            // 2. AN ERROR AMONG WARNINGS: the whole batch rolls back, and NO
            // warning is dismissed - they are judged together, before any is
            // touched, which is the move path's order as well.
            note = new HeronFailureNote();
            back = note.RollsBack(null, Batch(
                Said(Warning, "Highlighted walls overlap.", 2),
                Said(Error, "Can't keep elements joined.", 2),
                Said(Warning, "Highlighted walls overlap.", 2)), Warning);
            Check(back, "one error among warnings ROLLS BACK");
            Check(note.DismissedCount == 0, "and dismisses NONE of the warnings beside it (" + note.DismissedCount + ")");
            Check(note.RefusedCount == 1 && note.Refused[0].Text == "Can't keep elements joined.",
                  "the error is what is recorded as refused");
            var refusal = note.RefusedSentence();
            Check(refusal == "Revit would not let it through: \"Can't keep elements joined.\" (2 elements).",
                  "the refusal quotes Revit's own words: \"" + refusal + "\"");
            Check(note.DismissedSentence() == "", "and there is nothing dismissed to report");

            // 3. EVERY SEVERITY THAT IS NOT A PLAIN WARNING ROLLS BACK -
            // including the one above Error that the move path once missed
            // (FRAGMENT-ISSUES 5b row 10), and values Revit has not got.
            foreach (var severity in new[] { DocumentCorruption, Error, None, 4, 99, -1 })
            {
                note = new HeronFailureNote();
                Check(note.RollsBack(null, Batch(Said(severity, "something", 0)), Warning),
                      "severity " + severity + " rolls back - only an exact Warning is ever dismissed");
                Check(note.DismissedCount == 0, "  and is never counted as dismissed");
            }

            // 4. NOTHING POSTED IS NOT A FAILURE.
            note = new HeronFailureNote();
            Check(!note.RollsBack(null, Batch(), Warning) && !note.RollsBack(null, null, Warning),
                  "an empty or missing batch keeps the transaction");
            Check(note.DismissedCount == 0 && note.RefusedCount == 0 && note.DismissedSentence() == "",
                  "and a clean run gains no words at all");

            // 5. THE STEP IS KEPT. A warning a setup step raised is not the
            // fragment's, and the reader must be able to tell them apart.
            note = new HeronFailureNote();
            note.RollsBack("create-line", Batch(Said(Warning, "Line is slightly off axis.", 1)), Warning);
            note.RollsBack(null, Batch(Said(Warning, "Line is slightly off axis.", 1)), Warning);
            Check(note.Dismissed.Count == 2, "the same words from a setup step and from the fragment are two entries");
            Check(note.DismissedSentence().Contains("(1 element, in setup step 'create-line')"),
                  "and the setup step is named in the sentence: \"" + note.DismissedSentence() + "\"");

            // 6. NOTHING IS DROPPED PAST THE QUOTE LIMIT. It is counted and
            // said to exist - the reply carries every one.
            note = new HeronFailureNote();
            var many = new List<HeronFailureNote.Posted>();
            for (var i = 1; i <= HeronFailureNote.Quoted + 3; i++)
                many.Add(Said(Warning, "Warning number " + i + ".", 0));
            note.RollsBack(null, many, Warning);
            sentence = note.DismissedSentence();
            Check(note.Dismissed.Count == HeronFailureNote.Quoted + 3,
                  "every distinct warning is kept for the reply (" + note.Dismissed.Count + ")");
            Check(sentence.Contains("\"Warning number " + HeronFailureNote.Quoted + ".\"")
                  && !sentence.Contains("\"Warning number " + (HeronFailureNote.Quoted + 1) + ".\""),
                  "the sentence quotes the first " + HeronFailureNote.Quoted + " and no more");
            Check(sentence.Contains("and 3 more kinds, listed with this answer"),
                  "and says how many it did not quote: \"" + sentence + "\"");

            // ...BUT WHAT STOPPED A JOB IS NEVER CUT SHORT. A refusal has no
            // list beside it to carry the rest, so every one is quoted.
            note = new HeronFailureNote();
            many = new List<HeronFailureNote.Posted>();
            for (var i = 1; i <= HeronFailureNote.Quoted + 3; i++)
                many.Add(Said(Error, "Error number " + i + ".", 0));
            note.RollsBack(null, many, Warning);
            refusal = note.RefusedSentence();
            Check(refusal.Contains("\"Error number " + (HeronFailureNote.Quoted + 3) + ".\"")
                  && !refusal.Contains("more kind"),
                  "every error that stopped the job is quoted, the last included");

            // 7. EMPTY WORDS ARE NOT QUOTED AS NOTHING.
            note = new HeronFailureNote();
            note.RollsBack(null, Batch(Said(Error, "  ", 0)), Warning);
            Check(note.RefusedSentence().Contains("(Revit gave no description)"),
                  "an error with no words says so, rather than quoting an empty string");

            // 8. A WARNING THAT COULD NOT BE DISMISSED is taken back out of the
            // dismissed count and recorded with what was refused - the caller
            // rolls the job back rather than leave it to Revit.
            note = new HeronFailureNote();
            note.RollsBack(null, Batch(Said(Warning, "Stubborn warning.", 1), Said(Warning, "Easy warning.", 0)), Warning);
            note.CouldNotDismiss(null, "Stubborn warning.", 1, "the failure has not been properly initialized");
            Check(note.DismissedCount == 1 && note.Dismissed.Count == 1 && note.Dismissed[0].Text == "Easy warning.",
                  "the stubborn warning is no longer counted as dismissed (" + note.DismissedCount + ")");
            Check(note.RefusedCount == 1 && note.RefusedSentence().Contains(
                      "a warning Heron could not dismiss: Stubborn warning. (the failure has not been properly initialized)"),
                  "and is reported as the reason for the rollback, with Revit's reason: \"" + note.RefusedSentence() + "\"");

            // 9. A LIST REVIT WOULD NOT HAND OVER is a rollback with a reason.
            note = new HeronFailureNote();
            note.Unreadable("setup one", "the accessor is inactive");
            Check(note.RefusedCount == 1 && note.RefusedSentence().Contains("could not be read (the accessor is inactive)"),
                  "an unreadable failure list is recorded, with why: \"" + note.RefusedSentence() + "\"");

            // 10. SINGULAR AND PLURAL, because "1 warnings" is how a reader
            // learns not to trust the rest of the sentence.
            note = new HeronFailureNote();
            note.RollsBack(null, Batch(Said(Warning, "Only one.", 0)), Warning);
            sentence = note.DismissedSentence();
            Check(sentence.StartsWith("Revit raised 1 warning while", StringComparison.Ordinal)
                  && sentence.Contains("dismissed it rather"),
                  "one warning reads as one: \"" + sentence + "\"");

            Console.WriteLine();
            if (Failures.Count > 0)
            {
                Console.WriteLine("FAILED - " + Failures.Count + " of " + _checks);
                return 1;
            }
            Console.WriteLine("PASSED - " + _checks + " checks. Only a plain warning is ever dismissed,");
            Console.WriteLine("everything else rolls back, and what Revit said reaches the reader.");
            return 0;
        }
    }
}
