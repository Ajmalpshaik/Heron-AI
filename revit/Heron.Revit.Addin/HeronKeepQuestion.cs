// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Globalization;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// The words of Revit's Keep or Discard question, and D-60's highlight
    /// cap - D-126, which makes one question of two.
    ///
    /// ONE QUESTION, TWO DOORS. D-119 put a Keep or Discard box in front of a
    /// DRAFT fragment's one run on a live model ("apply": "ask"). D-126 puts
    /// the same box in front of every change a chat keeps through
    /// revit_change, while the owner's write.askBeforeKeeping is on. Either
    /// way the run has happened inside Heron's TransactionGroup, the changed
    /// elements are selected, isolated and zoomed to, and
    /// RevitFragment.AskTheModeller shows this file's words. Keep assimilates
    /// the group - one undo entry - and Discard rolls it back.
    ///
    /// THIS FILE NAMES NO AUTODESK TYPE, on purpose. The words and the cap
    /// are the half that can be wrong without Revit, so
    /// tests/Heron.KeepQuestion.TestHost links this file by source and checks
    /// them on any machine. Which dialog result keeps is RevitFragment's -
    /// only the Keep link, read there and checked by tests/test_try_once.py.
    /// Whether Revit then does what it is told is the PC's to prove
    /// (NEEDS-CHECKING Groups EM and ET).
    /// </summary>
    internal static class HeronKeepQuestion
    {
        /// <summary>
        /// D-60's cap: above this many elements in one set, the set is not
        /// selected or isolated, and the question says so rather than drawing
        /// a plausible part of it. Applied to each set on its own - added and
        /// changed - so a run that changed thousands can still show the few it
        /// added.
        ///
        /// D-60 calls 500 "the default of a setting". No setting reads it yet,
        /// so it is a constant here, and D-126 records that gap.
        /// </summary>
        public const int HighlightCap = 500;

        /// <summary>
        /// Is there anything to ask about? Not when Revit reported nothing
        /// added, changed or deleted - a run that opened a view (D-115) or a
        /// rename that matched nothing. The group holds no change, so Keep and
        /// Discard would leave the model the same, and a question with no
        /// difference between its answers is one a modeller learns to click
        /// through. A DRAFT's one run (D-119) is asked whatever it counted:
        /// its Keep is its proof's positive case.
        /// </summary>
        public static bool Asks(int added, int changed, int deleted)
        {
            return added > 0 || changed > 0 || deleted > 0;
        }

        /// <summary>Is a set of this size shown on screen? Empty shows nothing.</summary>
        public static bool Highlights(int count)
        {
            return count > 0 && count <= HighlightCap;
        }

        /// <summary>
        /// The room left round the change when the view is zoomed to it, in
        /// Revit's internal feet: a quarter of the larger side of the box
        /// round everything shown, and never less than 1000 mm. The owner's
        /// ask, 2026-10-18 - see the change "with a little space around it".
        ///
        /// THE MINIMUM IS WHAT MAKES ONE ELEMENT ZOOMABLE. A single pipe or a
        /// level is a box with no depth in a plan or an elevation, and a
        /// rectangle with no height is not one Revit can centre on. A size
        /// that is not a size - NaN, infinite, below zero - gives the minimum.
        /// </summary>
        /// <param name="width">The box's size along X, in feet.</param>
        /// <param name="depth">Along Y, in feet.</param>
        /// <param name="height">Along Z, in feet.</param>
        public static double ZoomMarginFeet(double width, double depth, double height)
        {
            var larger = 0.0;
            foreach (var side in new[] { width, depth, height })
            {
                if (!double.IsNaN(side) && !double.IsInfinity(side) && side > larger)
                    larger = side;
            }
            return Math.Max(larger * ZoomMarginShare, ZoomMarginMinimumMm / MmPerFoot);
        }

        /// <summary>A quarter of the larger side - ZoomMarginFeet.</summary>
        public const double ZoomMarginShare = 0.25;

        /// <summary>Never less than this much room, in millimetres - ZoomMarginFeet.</summary>
        public const double ZoomMarginMinimumMm = 1000;

        private const double MmPerFoot = 304.8;

        public const string Title = "Heron - keep this change?";
        public const string KeepLink = "Keep it";
        public const string DiscardLink = "Discard it";
        public const string DiscardNote = "Put the model back exactly as it was. Nothing is kept.";

        /// <summary>
        /// The question in one line. A DRAFT's one run says it has never been
        /// proved, because that is the reason it is being asked at all.
        /// </summary>
        public static string Headline(string name, bool neverProved)
        {
            return neverProved
                ? "'" + Named(name) + "' has never been proved. Keep what it did?"
                : "Keep what Heron just did?";
        }

        /// <summary>The Keep link's second line: what Ctrl+Z will be called.</summary>
        public static string KeepNote(string name)
        {
            return "I have checked it and it is right. It stays as one undo step, \"Heron: "
                 + Named(name) + "\".";
        }

        /// <summary>
        /// The dialog's body: what ran, what Revit counted, what is on screen,
        /// and what each answer does.
        /// </summary>
        /// <param name="name">The fragment, as the undo entry will name it.</param>
        /// <param name="model">The document's title.</param>
        /// <param name="added">Elements Revit reported added - all of them,
        /// views and types included, because a view made is a change too.</param>
        /// <param name="changed">Elements Revit reported changed, the same way.</param>
        /// <param name="deleted">Elements Revit reported deleted. Only counted -
        /// a deleted element cannot be shown.</param>
        /// <param name="onScreen">What OnScreen said is drawn.</param>
        /// <param name="sendsOut">Did the run send something out of the model -
        /// an export, a save, a sync? Discard cannot take that back.</param>
        /// <param name="neverProved">A DRAFT's one run (D-119)?</param>
        /// <param name="redrawn">Was the view redrawn after the change? When it
        /// was not, what is on screen may not show the change, and the box
        /// says so rather than invite a Keep on an old picture.</param>
        public static string Content(string name, string model, int added, int changed,
                                     int deleted, string onScreen, bool sendsOut,
                                     bool neverProved, bool redrawn)
        {
            var text = "";
            if (neverProved)
            {
                text = redrawn
                    ? "Heron ran it once, for real, on this model, and the view now shows the "
                      + "result. Look at it before you choose.\n\n"
                    : "Heron ran it once, for real, on this model, but COULD NOT REDRAW THE VIEW - "
                      + "what you see may not show the change. Discard unless you can see it.\n\n";
            }

            text += "'" + Named(name) + "' ran in " + Named(model) + ". Revit counts "
                  + Count(added, "element") + " added, " + Count(changed, "element")
                  + " changed and " + Count(deleted, "element") + " deleted.";

            if (!string.IsNullOrEmpty(onScreen)) text += "\n\n" + onScreen;

            if (sendsOut)
            {
                text += "\n\nWhat it sent out of the model - an exported file, a save, a sync - "
                      + "is done already, and Discard cannot take it back.";
            }

            text += "\n\nNothing is kept until you choose Keep. Discard, Esc or closing this "
                  + "window puts the model back as it was. You cannot move around the model "
                  + "while this is open: if this view does not show the change, Discard, open "
                  + "a view that does, and ask again.";
            return text;
        }

        /// <summary>
        /// What the modeller is looking at, said rather than left to be
        /// guessed. A set above the cap says it was not drawn, and why - D-60:
        /// a preview that silently stopped highlighting would be a plausible
        /// zero drawn on the screen.
        /// </summary>
        /// <param name="added">Added elements that can be selected - not a type
        /// or a view. The cap is about these.</param>
        /// <param name="changed">Changed elements that can be selected.</param>
        /// <param name="inFront">Is the model the one in front? Nothing is
        /// selected or isolated in a model that is not.</param>
        /// <param name="addedShown">Were the added elements selected?</param>
        /// <param name="changedShown">Were the changed elements selected?</param>
        /// <param name="isolated">Was the selection also isolated in the active view?</param>
        /// <param name="zoomed">Was the active view zoomed to the selection?
        /// Said only when something is selected - with nothing selected there
        /// was nothing to zoom to.</param>
        public static string OnScreen(int added, int changed, bool inFront, bool addedShown,
                                      bool changedShown, bool isolated, bool zoomed)
        {
            if (!inFront)
            {
                return "This model is not the one in front, so nothing is selected or "
                     + "isolated on screen.";
            }

            if (added + changed == 0)
            {
                return "Nothing it added or changed can be shown on screen.";
            }

            var shown = "";
            if (addedShown && changedShown) shown = "The added and changed elements are";
            else if (addedShown) shown = "The added elements are";
            else if (changedShown) shown = "The changed elements are";

            var sentence = shown.Length == 0
                ? "Nothing is selected on screen."
                : shown + (isolated ? " selected and isolated in the active view."
                                    : " selected in the active view.");

            // WHY THE PICTURE MOVED, AND WHAT EACH ANSWER DOES TO IT. Keep
            // leaves the modeller looking at what they kept; every other way
            // out puts the camera back where they left it.
            if (zoomed && shown.Length > 0)
            {
                sentence += " The view is zoomed to them, with some room around. Keep leaves it "
                          + "there; Discard puts the view back where it was.";
            }

            if (added > HighlightCap)
                sentence += " " + TooMany(added, "added");
            if (changed > HighlightCap)
                sentence += " " + TooMany(changed, "changed");
            return sentence;
        }

        /// <summary>
        /// Kept without a question because Revit reported no change - said, so
        /// a question that did not appear is never read as one that was
        /// answered. If the run plainly did change something, this sentence is
        /// the sign that Revit's report did not reach Heron.
        /// </summary>
        public const string NothingToAskSentence =
            "Revit reported nothing added, changed or deleted in the model, so the modeller "
            + "was not asked.";

        /// <summary>
        /// The verdict when the modeller chose Discard - or closed the box,
        /// which discards.
        ///
        /// "NOTHING WAS KEPT" ONLY WHEN THE ROLLBACK HELD - Revit reported the
        /// group rolled back AND nothing the run added is still standing
        /// (RevitFragment.RollBackAndCheck) - for the reason WithVerdict gives:
        /// on 2026-09-09 a rollback did not hold while Heron said it had.
        /// Otherwise it says the model may still hold the change. And never
        /// "send it again with apply", which the plain rollback's verdict
        /// says: the modeller has just answered.
        /// </summary>
        public static string Discarded(string name, bool held, bool selectionRestored)
        {
            if (!held)
            {
                return "The modeller chose Discard in Revit, and the rollback could not be "
                     + "confirmed - Revit did not report the transaction group rolled back, or "
                     + "something the run added is still in the model - so "
                     + "THE MODEL MAY STILL HOLD what '" + Named(name) + "' did. Look at it in "
                     + "Revit before trusting anything here, and do not save until you have.";
            }

            return "The modeller chose Discard in Revit, so NOTHING WAS KEPT: '" + Named(name)
                 + "' ran, Revit reported the transaction group rolled back, and there is no "
                 + "new undo entry. "
                 + (selectionRestored
                       ? "Their own selection was put back. "
                       : "Their own selection could not be put back. ")
                 + "This was their decision, not a failure - send it again only if they ask.";
        }

        private static string TooMany(int count, string which)
        {
            return count.ToString("N0", CultureInfo.InvariantCulture) + " " + which
                 + " elements are too many to highlight (more than "
                 + HighlightCap.ToString(CultureInfo.InvariantCulture)
                 + "), so they are not selected.";
        }

        private static string Count(int count, string noun)
        {
            if (count == 0) return "no " + noun + "s";
            return count.ToString("N0", CultureInfo.InvariantCulture) + " "
                 + noun + (count == 1 ? "" : "s");
        }

        private static string Named(string text)
        {
            return string.IsNullOrEmpty(text) ? "(unnamed)" : text;
        }
    }
}
