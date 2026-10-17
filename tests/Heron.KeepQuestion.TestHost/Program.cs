// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Heron.Revit.Addin;

namespace Heron.KeepQuestion.TestHost
{
    /// <summary>
    /// Revit's Keep or Discard question - D-117 - checked without Revit.
    ///
    /// Three things can be wrong here with no Revit anywhere near, and each
    /// would cost a modeller something real:
    ///
    ///   * THE MAPPING. Only the Keep link may keep. If Esc, the close button
    ///     or a result nobody expected ever read as Keep, a modeller who
    ///     dismissed the question would find the change kept.
    ///   * D-60'S CAP. Above 500 in one set, nothing of that set is drawn and
    ///     the question says why - each set on its own.
    ///   * THE WORDS. What ran, what Revit counted, what is on screen, and
    ///     that nothing is kept until Keep. And the chat's sentence for a
    ///     Discard says NOTHING WAS KEPT only when Revit reported the rollback.
    ///
    /// THE DIALOG RESULTS BELOW ARE REVIT'S, read out of every release's
    /// RevitAPI.dll with `tools/api-surface --members TaskDialogResult`:
    /// None=0, Ok=1, Cancel=2, Retry=4, Yes=6, No=7, Close=8,
    /// CommandLink1..4=1001..1004, the same on all eight releases 2020 to 2027.
    /// The production call casts TaskDialogResult itself, so these are the
    /// values the checks must be about, not a copy the code uses.
    /// </summary>
    internal static class Program
    {
        private const int None = 0;
        private const int Ok = 1;
        private const int Cancel = 2;
        private const int Retry = 4;
        private const int Yes = 6;
        private const int No = 7;
        private const int Close = 8;
        private const int CommandLink1 = 1001;
        private const int CommandLink2 = 1002;
        private const int CommandLink3 = 1003;
        private const int CommandLink4 = 1004;

        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private static void Check(bool condition, string what)
        {
            _checks++;
            Console.WriteLine((condition ? "  ok    " : "  FAIL  ") + what);
            if (!condition) Failures.Add(what);
        }

        private static int Main()
        {
            Console.WriteLine("1. only the Keep link keeps");
            Check(HeronKeepQuestion.Decide(CommandLink1, CommandLink1) == HeronKeepQuestion.Answer.Keep,
                  "the Keep link keeps");
            Check(HeronKeepQuestion.Decide(CommandLink2, CommandLink1) == HeronKeepQuestion.Answer.Discard,
                  "the Discard link discards");
            Check(HeronKeepQuestion.Decide(Cancel, CommandLink1) == HeronKeepQuestion.Answer.Discard,
                  "Esc - Revit answers Cancel - discards");
            Check(HeronKeepQuestion.Decide(Close, CommandLink1) == HeronKeepQuestion.Answer.Discard,
                  "the close button discards");
            foreach (var other in new[] { None, Ok, Retry, Yes, No, CommandLink3, CommandLink4, -1, 999 })
            {
                Check(HeronKeepQuestion.Decide(other, CommandLink1) == HeronKeepQuestion.Answer.Discard,
                      "a result nobody expected (" + other + ") discards");
            }
            Check(HeronKeepQuestion.Decide(CommandLink1, CommandLink2) == HeronKeepQuestion.Answer.Discard
                  && HeronKeepQuestion.Decide(CommandLink2, CommandLink2) == HeronKeepQuestion.Answer.Keep,
                  "the rule follows the link it is told is Keep, not a number of its own");

            Console.WriteLine();
            Console.WriteLine("2. D-60's cap of 500, each set on its own");
            Check(HeronKeepQuestion.HighlightCap == 500, "the cap is D-60's 500");
            Check(!HeronKeepQuestion.Highlights(0), "an empty set draws nothing");
            Check(HeronKeepQuestion.Highlights(1), "one is drawn");
            Check(HeronKeepQuestion.Highlights(500), "500 is drawn - the cap is inclusive");
            Check(!HeronKeepQuestion.Highlights(501), "501 is not");
            Check(!HeronKeepQuestion.Highlights(-3), "a count below zero draws nothing");

            var perSet = HeronKeepQuestion.OnScreen(4, 4120, true, true, false, true);
            Check(perSet.Contains("The added elements are selected and isolated"),
                  "4 added are still shown when 4,120 changed are too many");
            Check(perSet.Contains("4,120 changed elements are too many to highlight (more than 500)"),
                  "and the set left out says how many and why");
            Check(!perSet.Contains("added elements are too many"),
                  "and the set that fits is not called too many");

            var both = HeronKeepQuestion.OnScreen(900, 700, true, false, false, false);
            Check(both.Contains("Nothing is selected on screen.")
                  && both.Contains("900 added") && both.Contains("700 changed"),
                  "two sets over the cap: nothing drawn, and both are named");

            Console.WriteLine();
            Console.WriteLine("3. whether to ask at all");
            Check(!HeronKeepQuestion.Asks(0, 0, 0),
                  "Revit counted nothing - nothing to keep or discard, so no question");
            Check(HeronKeepQuestion.Asks(1, 0, 0) && HeronKeepQuestion.Asks(0, 1, 0)
                  && HeronKeepQuestion.Asks(0, 0, 1),
                  "an add, a change or a delete each asks");
            Check(HeronKeepQuestion.NothingToAskSentence.Contains("not asked"),
                  "and a run kept without the question says it was not asked");

            Console.WriteLine();
            Console.WriteLine("4. the question's words");
            Check(HeronKeepQuestion.Title.Length > 0 && HeronKeepQuestion.Headline.Length > 0,
                  "it has a title and a headline");
            Check(HeronKeepQuestion.KeepLink == "Keep" && HeronKeepQuestion.DiscardLink == "Discard",
                  "the two links say Keep and Discard");
            Check(HeronKeepQuestion.KeepNote("CREATE_LEVEL").Contains("Heron: CREATE_LEVEL"),
                  "Keep names the undo entry it will make, as Revit will list it");
            Check(HeronKeepQuestion.DiscardNote.Contains("Nothing is kept"),
                  "Discard says nothing is kept");

            var said = HeronKeepQuestion.Content("CREATE_LEVEL", "Project1", 1, 2, 0,
                HeronKeepQuestion.OnScreen(1, 2, true, true, true, true), false);
            Check(said.Contains("'CREATE_LEVEL' ran in Project1"), "it names what ran, and where");
            Check(said.Contains("1 element added, 2 elements changed and no elements deleted"),
                  "it gives Revit's counts, singular and plural right");
            Check(said.Contains("The added and changed elements are selected and isolated in the active view."),
                  "it says what is on screen");
            Check(said.Contains("Nothing is kept until you choose Keep")
                  && said.Contains("Discard, Esc or closing this window puts the model back"),
                  "and that nothing is kept until Keep - Esc and the close button included");
            Check(!said.Contains("Discard cannot take it back"),
                  "a change inside the model does not warn about what Discard cannot undo");

            var publish = HeronKeepQuestion.Content("EXPORT_VIEWS_TO_DWG", "Project1", 0, 1, 0,
                HeronKeepQuestion.OnScreen(0, 1, true, false, true, false), true);
            Check(publish.Contains("is done already, and Discard cannot take it back"),
                  "a publish says an export, a save or a sync is not undone by Discard");
            Check(publish.Contains("The changed elements are selected in the active view."),
                  "selected and not isolated is said as just selected");

            var behind = HeronKeepQuestion.OnScreen(3, 0, false, false, false, false);
            Check(behind.Contains("not the one in front"),
                  "a model that is not in front is not drawn on, and the question says so");
            Check(HeronKeepQuestion.OnScreen(0, 0, true, false, false, false)
                      .Contains("Nothing it added or changed can be shown"),
                  "a run that only made views or types says nothing can be shown");
            Check(HeronKeepQuestion.Content(null, null, 0, 0, 1, null, false).Contains("'(unnamed)' ran in (unnamed)"),
                  "no name and no model still reads as a sentence");

            foreach (var text in new[] { said, publish, behind, HeronKeepQuestion.DiscardNote })
            {
                Check(!text.Contains("Exception") && !text.Contains("TransactionGroup")
                      && !text.Contains("TaskDialog") && !text.Contains("null"),
                      "no developer word reaches the modeller: \"" + Short(text) + "\"");
            }

            Console.WriteLine();
            Console.WriteLine("5. what the chat is told");
            var gone = HeronKeepQuestion.Discarded("CREATE_LEVEL", true, true);
            Check(gone.Contains("NOTHING WAS KEPT") && gone.Contains("no new undo entry"),
                  "a Discard Revit rolled back says nothing was kept and no undo entry was made");
            Check(gone.Contains("selection was put back"), "and that their selection is back");
            Check(gone.Contains("not a failure") && gone.Contains("only if they ask"),
                  "and that it is their decision, never to be sent again on its own (Article 25)");
            Check(HeronKeepQuestion.Discarded("CREATE_LEVEL", true, false).Contains("could not be put back"),
                  "a selection that could not be restored is said, not hidden");

            var unsure = HeronKeepQuestion.Discarded("CREATE_LEVEL", false, true);
            Check(unsure.Contains("MAY STILL HOLD") && !unsure.Contains("NOTHING WAS KEPT"),
                  "a Discard Revit did not report rolled back never claims nothing was kept");

            var waiting = HeronKeepQuestion.StillAsking(300);
            Check(waiting.Contains("300 seconds") && waiting.Contains("Nothing is kept until")
                  && waiting.Contains("Do not send it again"),
                  "a question still open says how long, that nothing is kept yet, and not to resend");
            Check(HeronKeepQuestion.KeptSentence.Contains("chose Keep"),
                  "a Keep says the modeller chose it");

            Console.WriteLine();
            if (Failures.Count > 0)
            {
                Console.WriteLine("FAILED - " + Failures.Count + " of " + _checks);
                return 1;
            }
            Console.WriteLine("PASSED - " + _checks + " checks. Only Keep keeps, the cap holds per set,");
            Console.WriteLine("and every sentence says what happened and what comes next.");
            return 0;
        }

        private static string Short(string text)
        {
            var one = text.Replace("\n", " ");
            return one.Length <= 48 ? one : one.Substring(0, 48) + "...";
        }
    }
}
