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
    /// What Revit asks the modeller before a change from a chat is kept, and
    /// what each answer means - D-117.
    ///
    /// D-99 kept every revit_change at once, with no preview. D-117 puts
    /// Revit itself in front of the keep: the run has happened inside Heron's
    /// TransactionGroup, the changed elements are selected and isolated, and a
    /// TaskDialog asks Keep or Discard. Keep assimilates the group - one undo
    /// entry - and Discard rolls it back.
    ///
    /// THIS FILE NAMES NO AUTODESK TYPE, on purpose. The words the modeller
    /// reads, the rule that turns a dialog result into Keep or Discard, and
    /// D-60's highlight cap are the half that can be wrong without Revit, so
    /// tests/Heron.KeepQuestion.TestHost links this file by source and checks
    /// them on any machine. RevitFragment shows the dialog, selects and
    /// isolates; whether Revit then does what it is told is the PC's to
    /// prove (NEEDS-CHECKING Group EL).
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
        /// so it is a constant here, and D-117 records that gap.
        /// </summary>
        public const int HighlightCap = 500;

        public enum Answer
        {
            Keep,
            Discard,
        }

        /// <summary>
        /// The dialog's result as Keep or Discard.
        ///
        /// ONLY THE KEEP LINK KEEPS. Esc, the close button, the Discard link
        /// and any result nobody expected are all Discard, because the safe
        /// answer is the one a modeller gets without choosing. Both values are
        /// Revit's TaskDialogResult as numbers - the caller casts, so this file
        /// stays free of Revit.
        /// </summary>
        public static Answer Decide(int shown, int keepLink)
        {
            return shown == keepLink ? Answer.Keep : Answer.Discard;
        }

        /// <summary>
        /// Is there anything to ask about? Not when Revit reported nothing
        /// added, changed or deleted - a run that opened a view (D-115) or a
        /// rename that matched nothing. The group holds no change, so Keep and
        /// Discard would leave the model the same, and a question with no
        /// difference between its answers is one a modeller learns to click
        /// through.
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

        public const string Title = "Heron - keep this change?";
        public const string Headline = "Keep what Heron just did?";
        public const string KeepLink = "Keep";
        public const string DiscardLink = "Discard";
        public const string DiscardNote = "Put the model back as it was. Nothing is kept.";

        /// <summary>The Keep link's second line: what Ctrl+Z will be called.</summary>
        public static string KeepNote(string name)
        {
            return "One entry in Revit's undo list: Heron: " + Named(name) + ".";
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
        public static string Content(string name, string model, int added, int changed,
                                     int deleted, string onScreen, bool sendsOut)
        {
            var text = "'" + Named(name) + "' ran in " + Named(model) + ". Revit counts "
                     + Count(added, "element") + " added, " + Count(changed, "element")
                     + " changed and " + Count(deleted, "element") + " deleted.";

            if (!string.IsNullOrEmpty(onScreen)) text += "\n\n" + onScreen;

            if (sendsOut)
            {
                text += "\n\nWhat it sent out of the model - an exported file, a save, a sync - "
                      + "is done already, and Discard cannot take it back.";
            }

            text += "\n\nNothing is kept until you choose Keep. Discard, Esc or closing this "
                  + "window puts the model back as it was.";
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
        public static string OnScreen(int added, int changed, bool inFront, bool addedShown,
                                      bool changedShown, bool isolated)
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

            if (added > HighlightCap)
                sentence += " " + TooMany(added, "added");
            if (changed > HighlightCap)
                sentence += " " + TooMany(changed, "changed");
            return sentence;
        }

        /// <summary>
        /// The modeller chose Keep. Put in front of the add-in's KEPT verdict,
        /// so the chat can tell a change the modeller looked at from one kept
        /// with nobody asked.
        /// </summary>
        public const string KeptSentence = "The modeller chose Keep in Revit.";

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
        /// The modeller chose Discard - the message the chat reads.
        ///
        /// "NOTHING WAS KEPT" ONLY WHEN REVIT REPORTED THE GROUP ROLLED BACK,
        /// for the reason WithVerdict gives: on 2026-09-09 a rollback did not
        /// hold while Heron said it had. Otherwise it says the model may still
        /// hold the change.
        /// </summary>
        public static string Discarded(string name, bool rolledBack, bool selectionRestored)
        {
            if (!rolledBack)
            {
                return "The modeller chose Discard in Revit, and Revit did NOT report the "
                     + "transaction group rolled back afterwards - so THE MODEL MAY STILL HOLD "
                     + "what '" + Named(name) + "' did. Look at it in Revit before trusting "
                     + "anything here, and do not save until you have.";
            }

            return "The modeller chose Discard in Revit, so NOTHING WAS KEPT: '" + Named(name)
                 + "' ran, Revit reported the transaction group rolled back, and there is no "
                 + "new undo entry. "
                 + (selectionRestored
                       ? "Their own selection was put back. "
                       : "Their own selection could not be put back. ")
                 + "This was their decision, not a failure - send it again only if they ask.";
        }

        /// <summary>
        /// The bridge stopped waiting while the dialog was still up. The run
        /// is neither kept nor discarded yet, and only the modeller can say
        /// which it will be.
        /// </summary>
        public static string StillAsking(int seconds)
        {
            return "Revit is asking the modeller to Keep or Discard the change, and nobody has "
                 + "answered in " + seconds.ToString(CultureInfo.InvariantCulture) + " seconds. "
                 + "Nothing is kept until they choose Keep in Revit. Do not send it again - ask "
                 + "them which they chose.";
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
