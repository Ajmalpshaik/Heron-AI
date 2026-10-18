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

            var perSet = HeronKeepQuestion.OnScreen(4, 4120, true, true, false, true, false);
            Check(perSet.Contains("The added elements are selected and isolated"),
                  "4 added are still shown when 4,120 changed are too many");
            Check(perSet.Contains("4,120 changed elements are too many to highlight (more than 500)"),
                  "and the set left out says how many and why");
            Check(!perSet.Contains("added elements are too many"),
                  "and the set that fits is not called too many");

            var both = HeronKeepQuestion.OnScreen(900, 700, true, false, false, false, false);
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
                HeronKeepQuestion.OnScreen(1, 2, true, true, true, true, false), false, false, true);
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
                HeronKeepQuestion.OnScreen(1, 0, true, true, false, true, false), false, true, true);
            Check(tried.StartsWith("Heron ran it once, for real, on this model, and the view now shows the result."),
                  "a DRAFT's one run says it ran once, for real, and the view shows the result");
            var unseen = HeronKeepQuestion.Content("NEW_TOOL", "Project1", 1, 0, 0,
                HeronKeepQuestion.OnScreen(1, 0, true, true, false, true, false), false, true, false);
            Check(unseen.Contains("COULD NOT REDRAW THE VIEW") && unseen.Contains("Discard unless you can see it"),
                  "and when the view could not be redrawn it says so, rather than invite a Keep on an old picture");

            var publish = HeronKeepQuestion.Content("EXPORT_VIEWS_TO_DWG", "Project1", 0, 1, 0,
                HeronKeepQuestion.OnScreen(0, 1, true, false, true, false, false), true, false, true);
            Check(publish.Contains("is done already, and Discard cannot take it back"),
                  "a publish says an export, a save or a sync is not undone by Discard");
            Check(publish.Contains("The changed elements are selected in the active view."),
                  "selected and not isolated is said as just selected");

            var behind = HeronKeepQuestion.OnScreen(3, 0, false, false, false, false, false);
            Check(behind.Contains("not the one in front"),
                  "a model that is not in front is not drawn on, and the box says so");
            Check(HeronKeepQuestion.OnScreen(0, 0, true, false, false, false, false)
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
            Console.WriteLine("6. the view zoomed to the change - the owner's ask, 2026-10-18");
            var zoomed = HeronKeepQuestion.OnScreen(1, 2, true, true, true, true, true);
            Check(zoomed.Contains("The view is zoomed to them, with some room around."),
                  "a zoomed view says so, so the modeller knows why the picture moved");
            Check(zoomed.Contains("Keep leaves it there; Discard puts the view back where it was."),
                  "and what each answer does to the view: Keep stays, Discard puts it back");
            Check(HeronKeepQuestion.Content("CREATE_LEVEL", "Project1", 1, 2, 0, zoomed, false, false, true)
                      .Contains("zoomed to them"),
                  "and the box's body carries it");
            Check(!HeronKeepQuestion.OnScreen(1, 2, true, true, true, true, false).Contains("zoom"),
                  "a view that was not zoomed never claims it was");
            Check(!HeronKeepQuestion.OnScreen(900, 700, true, false, false, false, true).Contains("zoom"),
                  "nothing selected is never called zoomed to - there is nothing to be zoomed to");
            Check(!HeronKeepQuestion.OnScreen(3, 0, false, false, false, false, true).Contains("zoom"),
                  "nor is a model that is not in front");
            Check(HeronKeepQuestion.OnScreen(1, 0, true, true, false, false, true)
                      .StartsWith("The added elements are selected in the active view. The view is zoomed"),
                  "selected but not isolated can still be zoomed to, and says both");

            const double feet = 304.8;
            Check(Math.Abs(HeronKeepQuestion.ZoomMarginFeet(0, 0, 0) - 1000 / feet) < 1e-9,
                  "a point-sized change still gets 1000 mm of room - a rectangle Revit can centre on");
            Check(Math.Abs(HeronKeepQuestion.ZoomMarginFeet(20000 / feet, 3000 / feet, 500 / feet)
                           - 5000 / feet) < 1e-9,
                  "a 20 m run gets a quarter of its larger side - 5000 mm - round it");
            Check(Math.Abs(HeronKeepQuestion.ZoomMarginFeet(2000 / feet, 1000 / feet, 0) - 1000 / feet) < 1e-9,
                  "a quarter below 1000 mm is lifted to the 1000 mm minimum");
            Check(Math.Abs(HeronKeepQuestion.ZoomMarginFeet(1000 / feet, 12000 / feet, 0) - 3000 / feet) < 1e-9,
                  "the larger side decides, whichever side it is");
            Check(Math.Abs(HeronKeepQuestion.ZoomMarginFeet(double.NaN, -4, double.PositiveInfinity)
                           - 1000 / feet) < 1e-9,
                  "a size that is not a size gives the minimum, never NaN or infinity");

            Grant();

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

        /// <summary>
        /// D-138: one Keep per job - the owner's "OK, OK, OK, OK", 2026-10-18.
        /// Every rule of the grant with the clock in the test's hand: who holds
        /// it, on which model, for how long, what it never covers, and what
        /// ends it. A fresh HeronKeepGrant each time, never Session.
        /// </summary>
        private static void Grant()
        {
            var t0 = new DateTime(2026, 10, 18, 9, 0, 0, DateTimeKind.Utc);
            var local = new DateTime(2026, 10, 18, 12, 5, 0, DateTimeKind.Local);
            const string chat = "a1b2c3d4e5f6";
            const string model = "guid|C:\\Jobs\\Tower.rvt|Tower";
            string at;

            Console.WriteLine();
            Console.WriteLine("7. one Keep per job (D-138): given only by the third link, and held by one chat on one model");
            var none = new HeronKeepGrant();
            Check(none.Decide(chat, model, false, t0, out at) == HeronKeepGrant.NoGrant && at == null,
                  "with nothing granted, every change is asked, as D-126 always did");
            Check(!none.Active(t0) && none.GrantedAt == null, "and nothing is shown as held");

            var g = new HeronKeepGrant();
            Check(!g.Give("", model, t0, local) && !g.Active(t0),
                  "a request with no chat id is given nothing - the next chat could not be told apart");
            Check(!g.Give(chat, "", t0, local) && !g.Active(t0),
                  "nor a model with no key");
            Check(!HeronKeepGrant.CanHold(null, model) && !HeronKeepGrant.CanHold(chat, null)
                  && HeronKeepGrant.CanHold(chat, model),
                  "so the third link is offered only when the chat and the model can be named");
            Check(g.Give(chat, model, t0, local) && g.Active(t0), "given, it is held");
            Check(g.GrantedAt == "12:05", "and the time it was given is the PC's own HH:MM - 12:05");

            Check(g.Decide(chat, model, false, t0.AddMinutes(2), out at) == HeronKeepGrant.Covered
                  && at == "12:05",
                  "the same chat's next change to the same model is kept with no box, under the 12:05 grant");
            Check(g.Decide(chat, model, false, t0.AddMinutes(11), out at) == HeronKeepGrant.Covered,
                  "each change kept under it restarts the ten minutes - 9 after the last one, still covered");
            Check(g.Decide(chat, model, false, t0.AddMinutes(21), out at) == HeronKeepGrant.Covered,
                  "exactly ten minutes after the last change is still inside");

            Console.WriteLine();
            Console.WriteLine("8. still asked every time, grant or not: a publish and a DRAFT's one run");
            Check(g.Decide(chat, model, true, t0.AddMinutes(22), out at) == HeronKeepGrant.AlwaysAsked,
                  "a publish or a DRAFT's one run from the same chat is asked");
            Check(g.Active(t0.AddMinutes(22)) && g.Decide(chat, model, false, t0.AddMinutes(23), out at)
                      == HeronKeepGrant.Covered,
                  "and the grant stands for the change after it");
            var publish = HeronKeepGrant.WhyAskedAgain(HeronKeepGrant.AlwaysAsked, "12:05", true);
            Check(publish.Contains("12:05") && publish.Contains("an export, a save or a sync is always asked"),
                  "the box says why it is up for a publish, naming the grant's time");
            Check(HeronKeepGrant.WhyAskedAgain(HeronKeepGrant.AlwaysAsked, "12:05", false)
                      .Contains("a tool that has never been proved is always asked"),
                  "and for a DRAFT's one run");
            Check(HeronKeepGrant.WhyAskedAgain(HeronKeepGrant.NoGrant, null, false) == null
                  && HeronKeepGrant.WhyAskedAgain(HeronKeepGrant.Covered, "12:05", false) == null,
                  "with no grant in play the box says nothing about one");

            Console.WriteLine();
            Console.WriteLine("9. what ends it - and then the next change is asked");
            var idle = new HeronKeepGrant();
            idle.Give(chat, model, t0, local);
            Check(idle.Active(t0.AddMinutes(10)) && !idle.Active(t0.AddMinutes(10).AddSeconds(1)),
                  "ten minutes with no change and it is no longer shown as held");
            Check(idle.Decide(chat, model, false, t0.AddMinutes(10).AddSeconds(1), out at)
                      == HeronKeepGrant.EndedIdle && at == "12:05",
                  "the next change after that ends it, naming the grant it ended");
            Check(idle.Decide(chat, model, false, t0.AddMinutes(11), out at) == HeronKeepGrant.NoGrant,
                  "and every change after is asked again");
            Check(HeronKeepGrant.WhyAskedAgain(HeronKeepGrant.EndedIdle, "12:05", false)
                      .Contains("10 minutes passed with no change"),
                  "the box says it ended because ten minutes passed");

            var other = new HeronKeepGrant();
            other.Give(chat, model, t0, local);
            Check(other.Decide("ffffffffffff", model, false, t0.AddMinutes(1), out at)
                      == HeronKeepGrant.EndedOtherChat,
                  "another chat is asked - and its asking ends the grant");
            Check(other.Decide(chat, model, false, t0.AddMinutes(2), out at) == HeronKeepGrant.NoGrant,
                  "so the first chat is asked again too");

            var moved = new HeronKeepGrant();
            moved.Give(chat, model, t0, local);
            Check(moved.Decide(chat, "guid|C:\\Jobs\\Podium.rvt|Podium", false, t0.AddMinutes(1), out at)
                      == HeronKeepGrant.EndedOtherModel,
                  "the same chat on another model is asked, and that ends the grant");
            Check(moved.Decide(chat, model, true, t0.AddMinutes(2), out at) == HeronKeepGrant.NoGrant,
                  "and a publish after that is asked as it always was");

            var back = new HeronKeepGrant();
            back.Give(chat, model, t0, local);
            Check(back.Decide(chat, model, false, t0.AddMinutes(-1), out at) == HeronKeepGrant.EndedIdle,
                  "a PC clock put back ends it - a clock nobody can trust is answered by asking");

            var off = new HeronKeepGrant();
            off.Give(chat, model, t0, local);
            Check(off.HeldFor(model) && !off.HeldFor("guid|C:\\Jobs\\Podium.rvt|Podium") && !off.HeldFor(null),
                  "a closing model is matched by its own key, and only its own");
            Check(off.End() == "12:05" && !off.Active(t0) && off.GrantedAt == null,
                  "End - the switch turned off, its model closed - ends it and says which grant it was");
            Check(off.End() == null, "and ending nothing says so, so nothing is logged twice");
            Check(off.Decide(chat, model, false, t0.AddMinutes(1), out at) == HeronKeepGrant.NoGrant,
                  "the change after is asked");
            Check(HeronKeepGrant.Ends(HeronKeepGrant.EndedIdle) && HeronKeepGrant.Ends(HeronKeepGrant.EndedOtherChat)
                  && HeronKeepGrant.Ends(HeronKeepGrant.EndedOtherModel)
                  && !HeronKeepGrant.Ends(HeronKeepGrant.Covered) && !HeronKeepGrant.Ends(HeronKeepGrant.AlwaysAsked)
                  && !HeronKeepGrant.Ends(HeronKeepGrant.NoGrant),
                  "only the three ending answers count as ended");
            foreach (var why in new[] { HeronKeepGrant.EndedIdle, HeronKeepGrant.EndedOtherChat,
                                        HeronKeepGrant.EndedOtherModel, HeronKeepGrant.EndedSwitch,
                                        HeronKeepGrant.EndedClosed })
            {
                Check(HeronKeepGrant.EndedLine(why, "12:05").Contains("12:05")
                      && !HeronKeepGrant.Reason(why).Contains("no reason"),
                      "the log says why '" + why + "' ended the 12:05 grant");
            }

            var again = new HeronKeepGrant();
            again.Give(chat, model, t0, local);
            again.Give("ffffffffffff", model, t0.AddMinutes(1), local.AddMinutes(1));
            Check(again.Decide(chat, model, false, t0.AddMinutes(2), out at) == HeronKeepGrant.EndedOtherChat,
                  "a grant given again replaces the one before - one grant per Revit, never two");

            Console.WriteLine();
            Console.WriteLine("10. the words: the third link, the reply, and the box's Keep still says its undo step");
            Check(HeronKeepGrant.KeepRestLink == "Keep, and keep the rest of this job without asking",
                  "the third link says what it does in the modeller's words");
            var note = HeronKeepGrant.KeepRestNote("CREATE_EXTRUSION");
            Check(note.Contains("\"Heron: CREATE_EXTRUSION\"") && note.Contains("10 minutes")
                  && note.Contains("its own undo step")
                  && note.Contains("An export, a save or a sync, and a tool that has never been proved, are still asked"),
                  "its second line: this undo step, ten minutes, each its own undo step, and what is still asked");
            var kept = HeronKeepGrant.KeptSentence("12:05");
            Check(kept.Contains("WITHOUT ASKING") && kept.Contains("12:05")
                  && kept.Contains(HeronKeepGrant.KeepRestLink),
                  "a change kept under it says so in the reply - without asking, under the grant given at 12:05");
            Check(HeronKeepGrant.ChoseKeepRest().Contains("KEEP THE REST OF THIS JOB WITHOUT ASKING"),
                  "and the reply to the click itself says what was chosen");
            Check(HeronKeepGrant.IdleLimit == TimeSpan.FromMinutes(10), "ten minutes, as asked");
        }

        private static string Short(string text)
        {
            var one = text.Replace("\n", " ");
            return one.Length <= 48 ? one : one.Substring(0, 48) + "...";
        }
    }
}
