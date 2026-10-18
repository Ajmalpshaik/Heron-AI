// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// The words of Revit's "Delete N elements?" box, and the two rules it
    /// stands on - D-142, the owner's decision of 2026-10-18: "anything
    /// deleting from the model should come through the pop-up, in any
    /// situation, Ask to Keep on or off."
    ///
    /// WHAT THE BOX IS. A change kept from any door - revit_change, the words
    /// alone, the command line's --write --apply, a DRAFT's one run - that
    /// Revit reports deleted ANY element in is asked here first, whatever
    /// write.askBeforeKeeping says and never under D-138's grant. It is a
    /// measurement, never a card's flag: what Revit's own DocumentChanged
    /// counted deleted (RevitChangedSet), the doors that go with a wall and
    /// the fittings that go with a duct included. RevitFragment runs the
    /// change, rolls it back so the elements are in the model again, shows
    /// them - selected and zoomed to - and asks; on Delete it runs the change
    /// again and keeps it only if that second run deletes exactly the set
    /// shown. SameSet is that rule.
    ///
    /// THIS FILE NAMES NO AUTODESK TYPE, like HeronKeepQuestion: the words,
    /// the grouping by category and the same-set rule can be wrong without
    /// Revit, so tests/Heron.KeepQuestion.TestHost links this file by source
    /// and checks them on any machine. Whether Revit then does what it is
    /// told is NEEDS-CHECKING Group HL's.
    /// </summary>
    internal static class HeronDeleteQuestion
    {
        /// <summary>The two links - the owner's own words, 2026-10-18.</summary>
        public const string DeleteLink = "Delete";
        public const string DontDeleteLink = "Don't delete";

        public const string DontDeleteNote =
            "Nothing is deleted. The model stays exactly as it is.";

        /// <summary>
        /// Above this many elements, which of them go WITH another - a door
        /// with its wall - is not worked out, and the box says so. Revit is
        /// asked once per element, and the box must come up in seconds.
        /// The same number as D-60's highlight cap, so one figure is learned.
        /// </summary>
        public const int GoesWithCap = HeronKeepQuestion.HighlightCap;

        /// <summary>How many category lines the box lists before it counts the rest.</summary>
        public const int LinesShown = 12;

        /// <summary>How many element ids the box's "more" half lists before it counts the rest.</summary>
        public const int IdsShown = 40;

        /// <summary>The category an element with none is listed under - the change report's word.</summary>
        public const string NoCategory = "(no category)";

        /// <summary>The modeller's answers, as the reply's `deleteAnswer` carries them.</summary>
        public const string Deleted = "delete";
        public const string DontDelete = "dont_delete";
        public const string Closed = "closed";
        public const string TooLate = "late";

        /// <summary>
        /// One element that goes: its id as text, its category's name (null
        /// for none), and the category of the element it goes WITH, when
        /// Revit removes it only because that one goes - null when it is
        /// deleted in its own right, or when that was not worked out.
        /// </summary>
        internal sealed class Going
        {
            internal Going(string id, string category, string goesWith)
            {
                Id = id;
                Category = category;
                GoesWith = goesWith;
            }

            internal string Id { get; private set; }
            internal string Category { get; private set; }
            internal string GoesWith { get; private set; }
        }

        /// <summary>The window's title - the owner's words: "Delete N elements?"</summary>
        public static string Title(int total)
        {
            return "Delete " + Count(total) + "?";
        }

        /// <summary>The one large line, with the model named.</summary>
        public static string Headline(int total, string model)
        {
            return "Delete " + Count(total) + " from " + Named(model) + "?";
        }

        /// <summary>The Delete link's second line: what happens, and what Ctrl+Z will be called.</summary>
        public static string DeleteNote(string name)
        {
            return "Heron runs '" + Named(name) + "' again and keeps it as one undo step, \"Heron: "
                 + Named(name) + "\" - Ctrl+Z puts them back. If that run would delete anything "
                 + "other than what is listed here, nothing is deleted.";
        }

        /// <summary>
        /// The category lines: each category with its count, and "(goes with
        /// the ...)" where Revit removes them only because another goes.
        /// Those deleted in their own right come first, the largest first;
        /// what goes along with them after, under what they go with. Above
        /// LinesShown, the rest are counted in one line, never dropped.
        /// </summary>
        public static List<string> Lines(IEnumerable<Going> going)
        {
            var groups = new Dictionary<string, int>(StringComparer.Ordinal);
            var order = new List<KeyValuePair<string, string>>();
            foreach (var one in going ?? Enumerable.Empty<Going>())
            {
                if (one == null) continue;
                var category = string.IsNullOrEmpty(one.Category) ? NoCategory : one.Category;
                var with = string.IsNullOrEmpty(one.GoesWith) ? null : one.GoesWith;
                var key = category + "\u0001" + (with ?? "");
                int seen;
                if (!groups.TryGetValue(key, out seen))
                {
                    order.Add(new KeyValuePair<string, string>(category, with));
                    seen = 0;
                }
                groups[key] = seen + 1;
            }

            var sorted = order
                .Select(pair => new
                {
                    Category = pair.Key,
                    With = pair.Value,
                    Count = groups[pair.Key + "\u0001" + (pair.Value ?? "")],
                })
                .OrderBy(g => g.With == null ? 0 : 1)
                .ThenBy(g => g.With ?? "", StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(g => g.Count)
                .ThenBy(g => g.Category, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var lines = new List<string>();
            for (var index = 0; index < sorted.Count && index < LinesShown; index++)
            {
                var g = sorted[index];
                lines.Add("    " + g.Category + "  " + g.Count.ToString("N0", CultureInfo.InvariantCulture)
                          + (g.With == null ? "" : "  (goes with the " + g.With + ")"));
            }

            if (sorted.Count > LinesShown)
            {
                var rest = sorted.Skip(LinesShown).ToList();
                var count = rest.Sum(g => g.Count);
                lines.Add("    and " + Count(count) + " more, in "
                          + rest.Count.ToString(CultureInfo.InvariantCulture) + " other "
                          + (rest.Count == 1 ? "group" : "groups"));
            }
            return lines;
        }

        /// <summary>
        /// The box's body: what the chat asked, in which model, everything
        /// that goes grouped by category, what else the change does, what is
        /// on screen, and what each answer does - with the time after which
        /// no answer deletes anything.
        /// </summary>
        /// <param name="asked">The chat's own words, or null - then the capability is named.</param>
        /// <param name="name">The fragment, as the undo entry will name it.</param>
        /// <param name="model">The document's title.</param>
        /// <param name="going">Every element Revit reported deleted.</param>
        /// <param name="goesWithRead">Was "goes with" worked out? Not above GoesWithCap.</param>
        /// <param name="added">Elements the same change adds - shown nowhere, only counted.</param>
        /// <param name="changed">Elements it changes - counted the same way.</param>
        /// <param name="onScreen">What OnScreen said is drawn.</param>
        /// <param name="answerBy">"HH:MM", or null when no deadline is known.</param>
        public static string Content(string asked, string name, string model,
                                     IList<Going> going, bool goesWithRead, int added,
                                     int changed, string onScreen, string answerBy)
        {
            var total = going == null ? 0 : going.Count;

            var text = string.IsNullOrEmpty((asked ?? "").Trim())
                ? "The chat asked for '" + Named(name) + "'"
                : "The chat asked: \"" + asked.Trim() + "\"";
            text += ", in " + Named(model) + ".\n\n";

            text += "Revit counts " + Count(total) + " that would go, with everything that goes "
                  + "along with them:\n" + string.Join("\n", Lines(going));
            if (!goesWithRead)
            {
                text += "\n(Which of them go only along with another was not worked out: more than "
                      + GoesWithCap.ToString(CultureInfo.InvariantCulture) + " go.)";
            }

            if (added > 0 || changed > 0)
            {
                text += "\n\nThe same change also adds " + Count(added) + " and changes "
                      + Count(changed) + ". Those cannot be shown before it runs.";
            }

            if (!string.IsNullOrEmpty(onScreen)) text += "\n\n" + onScreen;

            text += "\n\nDon't delete, Esc or closing this window deletes nothing.";
            if (!string.IsNullOrEmpty(answerBy))
            {
                text += " Answer by " + answerBy + ": after that nothing is deleted, whatever "
                      + "you press.";
            }
            return text;
        }

        /// <summary>
        /// What the modeller is looking at, said rather than left to be
        /// guessed. The selection and the view are put back whatever they
        /// answer - Delete runs the change again on their own selection.
        /// </summary>
        /// <param name="total">Every element that goes.</param>
        /// <param name="showable">How many of them can be selected - not a type, a view or one with no category.</param>
        /// <param name="inFront">Is the model the one in front?</param>
        /// <param name="selected">Were they selected?</param>
        /// <param name="zoomed">Was the view zoomed to them?</param>
        public static string OnScreen(int total, int showable, bool inFront, bool selected,
                                      bool zoomed)
        {
            if (!inFront)
            {
                return "This model is not the one in front, so nothing is selected on screen.";
            }

            if (showable == 0)
            {
                return "None of them can be shown on screen: they are types, views, filters or "
                     + "Revit's own elements.";
            }

            if (showable > HeronKeepQuestion.HighlightCap)
            {
                return Count(showable) + " of them could be shown, which is too many to select "
                     + "(more than " + HeronKeepQuestion.HighlightCap.ToString(CultureInfo.InvariantCulture)
                     + "), so nothing is selected.";
            }

            if (!selected) return "Nothing could be selected on screen.";

            var sentence = (showable == total ? "They are" : "The " + Count(showable) + " that can be shown are")
                         + " selected in the active view"
                         + (zoomed ? ", and the view is zoomed to them with some room around." : ".");
            if (showable < total)
            {
                sentence += " The other " + (total - showable).ToString("N0", CultureInfo.InvariantCulture)
                          + " are types, views, filters or Revit's own elements, which cannot be "
                          + "shown.";
            }
            return sentence + " Your own selection and view come back whatever you choose.";
        }

        /// <summary>The box's "more" half: the element ids, the first IdsShown of them.</summary>
        public static string Ids(IList<Going> going)
        {
            if (going == null || going.Count == 0) return "";
            var first = going.Take(IdsShown).Select(g => g.Id);
            var text = "Element ids: " + string.Join(", ", first);
            if (going.Count > IdsShown)
            {
                text += " and " + (going.Count - IdsShown).ToString("N0", CultureInfo.InvariantCulture)
                      + " more.";
            }
            return text;
        }

        /// <summary>
        /// THE SAME-SET RULE. Delete was pressed for the set shown; the second
        /// run is kept only if it deletes exactly that set - no element shown
        /// missing from it, and none it deletes that was not shown. Null when
        /// they are the same; otherwise how they differ, for SetChanged.
        ///
        /// IDS AS TEXT, AND ONLY ELEMENTS THAT WERE IN THE MODEL BEFORE THE
        /// RUN: what one run adds and deletes again is no deletion at all
        /// (RevitChangedSet), and an element that existed before has the same
        /// id in both runs.
        /// </summary>
        public static Difference Differs(IEnumerable<string> shown, IEnumerable<string> now)
        {
            var before = new HashSet<string>(shown ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            var after = new HashSet<string>(now ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            if (before.SetEquals(after)) return null;
            return new Difference(before.Count, after.Count,
                                  before.Count(id => !after.Contains(id)),
                                  after.Count(id => !before.Contains(id)));
        }

        /// <summary>How the second run's set differs from the one shown.</summary>
        internal sealed class Difference
        {
            internal Difference(int shown, int now, int missing, int extra)
            {
                Shown = shown;
                Now = now;
                Missing = missing;
                Extra = extra;
            }

            internal int Shown { get; private set; }
            internal int Now { get; private set; }
            internal int Missing { get; private set; }
            internal int Extra { get; private set; }
        }

        /// <summary>
        /// The reply's sentence saying the modeller was asked, how many were
        /// shown and what they chose - `deleteAsking`, printed first by the
        /// chat (heron_run_records.verdict_line), as D-126's `asking` is.
        /// </summary>
        /// <param name="total">How many the box listed.</param>
        /// <param name="selected">How many of them were selected on screen.</param>
        /// <param name="answer">Deleted, DontDelete, Closed or TooLate.</param>
        public static string Asking(int total, int selected, string answer)
        {
            var asked = "The modeller was asked in Revit whether to delete " + Count(total)
                      + " - listed by category, "
                      + (selected > 0 ? selected.ToString("N0", CultureInfo.InvariantCulture)
                                        + " of them selected on screen"
                                      : "none of them selected on screen")
                      + " - and ";

            if (answer == Deleted) return asked + "pressed DELETE.";
            if (answer == DontDelete) return asked + "pressed DON'T DELETE.";
            if (answer == TooLate)
                return asked + "pressed Delete only after Heron had stopped waiting for the answer, "
                     + "which deletes nothing.";
            return asked + "closed the box, which deletes nothing.";
        }

        /// <summary>
        /// The refusal when the modeller did not say Delete in time. "NOTHING
        /// WAS DELETED" only when the run that counted them was confirmed
        /// rolled back - the caller asks only then - and never "send it again":
        /// the modeller has just answered.
        /// </summary>
        /// <param name="asking">Asking's sentence.</param>
        /// <param name="name">The fragment.</param>
        /// <param name="rolledBack">What the rollback check said about the model.</param>
        /// <param name="selectionRestored">Was their selection put back?</param>
        public static string Declined(string asking, string name, string rolledBack,
                                      bool selectionRestored)
        {
            return asking + " So NOTHING WAS DELETED: '" + Named(name) + "' ran once to count what "
                 + "it would delete, and that run was rolled back before the box was shown. "
                 + (string.IsNullOrEmpty(rolledBack) ? "" : rolledBack + " ")
                 + (selectionRestored
                       ? "Their own selection was put back. "
                       : "Their own selection could not be put back. ")
                 + "This was their decision, not a failure - send it again only if they ask.";
        }

        /// <summary>
        /// The refusal when Heron had already stopped waiting before the box
        /// could go up: a box whose every answer means no is not shown.
        /// </summary>
        public static string NoTimeToAsk(string name, int total, string rolledBack,
                                         bool selectionRestored)
        {
            return "'" + Named(name) + "' would delete " + Count(total) + ", and Heron had stopped "
                 + "waiting before it could ask the modeller, so the delete box was not shown and "
                 + "NOTHING WAS DELETED. "
                 + (string.IsNullOrEmpty(rolledBack) ? "" : rolledBack + " ")
                 + (selectionRestored ? "" : "Their own selection could not be put back. ")
                 + "Ask again when Revit is not busy.";
        }

        /// <summary>The refusal when the second run would delete a different set than the one shown.</summary>
        public static string SetChanged(string name, Difference how, string rolledBack)
        {
            return "The modeller pressed Delete for the " + Count(how.Shown) + " shown, and when '"
                 + Named(name) + "' ran again to keep it, it would have deleted " + Count(how.Now)
                 + " - " + how.Missing.ToString(CultureInfo.InvariantCulture) + " of those shown not "
                 + "among them and " + how.Extra.ToString(CultureInfo.InvariantCulture) + " not shown "
                 + "that were - so NOTHING WAS DELETED and that run was rolled back too. "
                 + (string.IsNullOrEmpty(rolledBack) ? "" : rolledBack + " ")
                 + "Something in the model changed between the two runs. Ask again to see a "
                 + "fresh list.";
        }

        /// <summary>
        /// The refusal when Revit's report of what changed never reached Heron
        /// and the model holds fewer elements than before: something was
        /// deleted that the box cannot list.
        /// </summary>
        public static string NotMeasured(string name, string rolledBack)
        {
            return "'" + Named(name) + "' ran, and Revit's own report of what it changed did not "
                 + "reach Heron, while the model now holds fewer elements than before it ran - so "
                 + "it deleted something Heron cannot show the modeller first. NOTHING WAS KEPT. "
                 + (string.IsNullOrEmpty(rolledBack) ? "" : rolledBack + " ")
                 + "Nothing is deleted from the model without the delete box (D-142).";
        }

        /// <summary>
        /// The refusal for an export or a save that also deletes elements:
        /// showing them first would mean running the export twice.
        /// </summary>
        public static string InPublish(string name, string rolledBack)
        {
            return "'" + Named(name) + "' is an export, a save or a sync that also deletes elements "
                 + "from the model. What it deletes cannot be shown first without sending it out "
                 + "twice, so NOTHING WAS KEPT in the model. "
                 + (string.IsNullOrEmpty(rolledBack) ? "" : rolledBack + " ")
                 + "What it sent out of the model during that run - an exported file, a save, a "
                 + "sync - is done already, and may not hold those elements.";
        }

        /// <summary>
        /// The refusal when the run that counted them could not be confirmed
        /// rolled back, or not every element came back: they cannot be shown,
        /// and the model may still be without them.
        /// </summary>
        public static string NotShown(string name, int total, string rolledBack)
        {
            return "'" + Named(name) + "' deleted " + Count(total) + ", and the run that counted "
                 + "them could not be confirmed rolled back with every one of them back in the "
                 + "model - so they could not be shown to the modeller first, and THE MODEL MAY "
                 + "STILL BE WITHOUT THEM. "
                 + (string.IsNullOrEmpty(rolledBack) ? "" : rolledBack + " ")
                 + "Look at the model in Revit before trusting anything here, and do not save "
                 + "until you have.";
        }

        /// <summary>
        /// Did the answer come too late to delete anything? `elapsed` is how
        /// long the box was up; `window` the time Revit had to answer when it
        /// went up, or null when no deadline was known. An answer at the very
        /// moment the window closes is late: the safe side.
        /// </summary>
        public static bool Late(TimeSpan elapsed, TimeSpan? window)
        {
            return window.HasValue && elapsed >= window.Value;
        }

        private static string Count(int count)
        {
            return count.ToString("N0", CultureInfo.InvariantCulture)
                 + (count == 1 ? " element" : " elements");
        }

        private static string Named(string text)
        {
            return string.IsNullOrEmpty(text) ? "(unnamed)" : text;
        }
    }
}
