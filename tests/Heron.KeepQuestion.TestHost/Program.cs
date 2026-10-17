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
    /// The words of Revit's Keep or Discard box, and D-60's cap - D-126 -
    /// checked without Revit.
    ///
    /// ONE BOX, TWO DOORS: a DRAFT's one run (D-119) and a keep from a chat
    /// (D-126). Two things can be wrong here with no Revit anywhere near, and
    /// each would cost a modeller something real:
    ///
    ///   * D-60'S CAP. Above 500 in one set, nothing of that set is drawn and
    ///     the box says why - each set on its own.
    ///   * THE WORDS. What ran, what Revit counted, what is on screen, that
    ///     nothing is kept until Keep, and - for a DRAFT - that it has never
    ///     been proved and whether the view was redrawn. And the chat's
    ///     sentence for a Discard says NOTHING WAS KEPT only when Revit
    ///     reported the rollback.
    ///
    /// WHICH RESULT KEEPS IS NOT HERE. It is RevitFragment.AskTheModeller's -
    /// only the Keep link - read there as text by tests/test_try_once.py and
    /// tests/test_keep_question.py.
    /// </summary>
    internal static class Program
    {
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
            Console.WriteLine("1. one box, two doors");
            Check(HeronKeepQuestion.Title.Length > 0, "it has a title");
            Check(HeronKeepQuestion.Headline("CREATE_LEVEL", false) == "Keep what Heron just did?",
                  "a keep from a chat asks plainly");
            Check(HeronKeepQuestion.Headline("NEW_TOOL", true).Contains("'NEW_TOOL' has never been proved"),
                  "a DRAFT's one run says it has never been proved - the reason it is asked at all");
            Check(HeronKeepQuestion.KeepLink == "Keep it" && HeronKeepQuestion.DiscardLink == "Discard it",
                  "the two links say Keep it and Discard it");
            Check(HeronKeepQuestion.KeepNote("CREATE_LEVEL").Contains("\"Heron: CREATE_LEVEL\""),
                  "Keep names the undo entry it will make, as Revit will list it");
            Check(HeronKeepQuestion.DiscardNote.Contains("Nothing is kept"),
                  "Discard says nothing is kept");

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
            Console.WriteLine("3. whether a keep from a chat is asked at all");
            Check(!HeronKeepQuestion.Asks(0, 0, 0),
                  "Revit counted nothing - nothing to keep or discard, so no question");
            Check(HeronKeepQuestion.Asks(1, 0, 0) && HeronKeepQuestion.Asks(0, 1, 0)
                  && HeronKeepQuestion.Asks(0, 0, 1),
                  "an add, a change or a delete each asks");
            Check(HeronKeepQuestion.NothingToAskSentence.Contains("not asked"),
                  "and a run kept without the question says it was not asked");

            Console.WriteLine();
            Console.WriteLine("4. the box's words");
            var said = HeronKeepQuestion.Content("CREATE_LEVEL", "Project1", 1, 2, 0,
                HeronKeepQuestion.OnScreen(1, 2, true, true, true, true), false, false, true);
            Check(said.Contains("'CREATE_LEVEL' ran in Project1"), "it names what ran, and where");
            Check(said.Contains("1 element added, 2 elements changed and no elements deleted"),
                  "it gives Revit's counts, singular and plural right");
            Check(said.Contains("The added and changed elements are selected and isolated in the active view."),
                  "it says what is on screen");
            Check(said.Contains("Nothing is kept until you choose Keep")
                  && said.Contains("Discard, Esc or closing this window puts the model back"),
                  "and that nothing is kept until Keep - Esc and the close button included");
            Check(said.Contains("You cannot move around the model while this is open"),
                  "and that the box is modal, so a view that does not show the change means Discard");
            Check(!said.Contains("Discard cannot take it back") && !said.Contains("REDRAW")
                  && !said.Contains("ran it once"),
                  "a keep from a chat warns about neither an export nor a DRAFT's redraw");

            var tried = HeronKeepQuestion.Content("NEW_TOOL", "Project1", 1, 0, 0,
                HeronKeepQuestion.OnScreen(1, 0, true, true, false, true), false, true, true);
            Check(tried.StartsWith("Heron ran it once, for real, on this model, and the view now shows the result."),
                  "a DRAFT's one run says it ran once, for real, and the view shows the result");
            var unseen = HeronKeepQuestion.Content("NEW_TOOL", "Project1", 1, 0, 0,
                HeronKeepQuestion.OnScreen(1, 0, true, true, false, true), false, true, false);
            Check(unseen.Contains("COULD NOT REDRAW THE VIEW") && unseen.Contains("Discard unless you can see it"),
                  "and when the view could not be redrawn it says so, rather than invite a Keep on an old picture");

            var publish = HeronKeepQuestion.Content("EXPORT_VIEWS_TO_DWG", "Project1", 0, 1, 0,
                HeronKeepQuestion.OnScreen(0, 1, true, false, true, false), true, false, true);
            Check(publish.Contains("is done already, and Discard cannot take it back"),
                  "a publish says an export, a save or a sync is not undone by Discard");
            Check(publish.Contains("The changed elements are selected in the active view."),
                  "selected and not isolated is said as just selected");

            var behind = HeronKeepQuestion.OnScreen(3, 0, false, false, false, false);
            Check(behind.Contains("not the one in front"),
                  "a model that is not in front is not drawn on, and the box says so");
            Check(HeronKeepQuestion.OnScreen(0, 0, true, false, false, false)
                      .Contains("Nothing it added or changed can be shown"),
                  "a run that only made views or types says nothing can be shown");
            Check(HeronKeepQuestion.Content(null, null, 0, 0, 1, null, false, false, true)
                      .Contains("'(unnamed)' ran in (unnamed)"),
                  "no name and no model still reads as a sentence");

            foreach (var text in new[] { said, tried, publish, behind, HeronKeepQuestion.DiscardNote })
            {
                Check(!text.Contains("Exception") && !text.Contains("TransactionGroup")
                      && !text.Contains("TaskDialog") && !text.Contains("null"),
                      "no developer word reaches the modeller: \"" + Short(text) + "\"");
            }

            Console.WriteLine();
            Console.WriteLine("5. what the chat is told after a Discard");
            var gone = HeronKeepQuestion.Discarded("CREATE_LEVEL", true, true);
            Check(gone.Contains("NOTHING WAS KEPT") && gone.Contains("no new undo entry"),
                  "a Discard Revit rolled back says nothing was kept and no undo entry was made");
            Check(gone.Contains("selection was put back"), "and that their selection is back");
            Check(gone.Contains("not a failure") && gone.Contains("only if they ask")
                  && !gone.Contains("with apply"),
                  "and that it is their decision, never to be sent again on its own (Article 25)");
            Check(HeronKeepQuestion.Discarded("CREATE_LEVEL", true, false).Contains("could not be put back"),
                  "a selection that could not be restored is said, not hidden");

            var unsure = HeronKeepQuestion.Discarded("CREATE_LEVEL", false, true);
            Check(unsure.Contains("MAY STILL HOLD") && !unsure.Contains("NOTHING WAS KEPT"),
                  "a Discard Revit did not report rolled back never claims nothing was kept");

            Console.WriteLine();
            if (Failures.Count > 0)
            {
                Console.WriteLine("FAILED - " + Failures.Count + " of " + _checks);
                return 1;
            }
            Console.WriteLine("PASSED - " + _checks + " checks. One box for both doors, the cap holds per");
            Console.WriteLine("set, and every sentence says what happened and what comes next.");
            return 0;
        }

        private static string Short(string text)
        {
            var one = text.Replace("\n", " ");
            return one.Length <= 48 ? one : one.Substring(0, 48) + "...";
        }
    }
}
