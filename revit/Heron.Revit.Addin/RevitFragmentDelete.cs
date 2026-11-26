// Heron-Agent:  HERON-REVIT-CMP-021
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Heron.Bridge;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// The delete box - D-142. The owner, 2026-10-18: "When I type delete, it
    /// means delete - that is OK. But anything deleting from the model should
    /// come through the pop-up, in any situation, Ask to Keep on or off. It
    /// shows: this many elements are going to be deleted - can we delete or
    /// not? If I agree, it deletes. If I disagree, no."
    ///
    /// RevitFragment.Run's half, in a file of its own so the run path's own
    /// file grows by a few lines. Run calls DeleteGate once per run, straight
    /// after the change is described and BEFORE D-138's grant or D-126's box
    /// is looked at - so neither Ask to Keep nor "keep the rest of this job"
    /// can reach it.
    ///
    /// BY MEASUREMENT, NEVER BY A CARD. What Revit's DocumentChanged reported
    /// deleted (RevitChangedSet.DeletedIds) is the whole test: a fragment that
    /// deletes, and what Revit deletes along with what it was told to - a
    /// wall's doors, a duct's fittings - all count, whatever the fragment's
    /// card says. Only a run that would KEEP is asked: `apply`, or a DRAFT's
    /// one run. A preview rolls back whatever anybody answers, so no proof is
    /// ever asked (tools/batch-prove.py never sends apply).
    ///
    /// THE MODELLER SEES THEM, which a deleted element inside an open group
    /// cannot be. So the run is MEASURED, ROLLED BACK - every element back in
    /// the model, checked one by one - SHOWN, ASKED, and on Delete RUN AGAIN
    /// from the top of the group and kept only if that run deletes exactly the
    /// set shown (HeronDeleteQuestion.Differs). Shown means selected and
    /// zoomed to in the model in front: no transaction, so nothing reaches the
    /// undo list, which an isolate made after the rollback would.
    ///
    /// NO ANSWER IN TIME DELETES NOTHING. Esc, the close button and Don't
    /// delete are no; a Delete pressed after RevitDispatcher.TimeToAnswer ran
    /// out is no too, and the box says by when. At that moment the box closes
    /// by itself (BoxCloser, 5b-1700), so a box nobody is there to answer no
    /// longer keeps Revit busy for every request after it.
    ///
    /// AN ELEMENT WITH NO CATEGORY IS NAMED BY ITS KIND (ListedAs, 5b-1700):
    /// "(no category) SketchPlane", not "(no category)" alone.
    /// </summary>
    internal static partial class RevitFragment
    {
        /// <summary>
        /// What the modeller pressed Delete for - the ids shown and how many -
        /// carried from the first run to the second, inside one call to Run.
        /// Nothing in the request can make one: only AskToDelete does, after
        /// the Delete link was pressed in Revit (Golden Rule 19).
        /// </summary>
        private sealed class ConfirmedDelete
        {
            internal ConfirmedDelete(IEnumerable<string> ids, int total, int selected, string asking)
            {
                Ids = new HashSet<string>(ids, StringComparer.Ordinal);
                Total = total;
                Selected = selected;
                Asking = asking;
            }

            internal HashSet<string> Ids { get; private set; }
            internal int Total { get; private set; }
            internal int Selected { get; private set; }
            internal string Asking { get; private set; }

            /// <summary>The reply's delete fields, with the modeller's Delete.</summary>
            internal string OnReply(string answer)
            {
                return DeleteFields(answer, Total, Selected, HeronDeleteQuestion.Deleted, Asking);
            }
        }

        /// <summary>
        /// D-142, once per run, with the group still open and the change
        /// described. Null to carry on; otherwise the reply to return, the
        /// group already rolled back. `again` true: the modeller pressed
        /// Delete, the first run is rolled back, and Run makes the same run
        /// again from the top of its group.
        ///
        ///   not keeping                carry on - nothing is kept to delete
        ///   second run, same set       carry on, and keep it
        ///   second run, other set      roll back, refuse: delete_set_changed
        ///   nothing counted deleted    carry on - unless Revit's report never
        ///                              arrived and the model holds fewer
        ///                              elements: delete_not_measured
        ///   deleted, in a publish      roll back, refuse: delete_in_publish -
        ///                              showing it first means exporting twice
        ///   deleted                    roll back; ask, unless the rollback or
        ///                              any element's return cannot be confirmed
        ///                              (delete_not_shown)
        ///   Ask to Delete off          carry on, and keep it as it ran - no
        ///                              box, no second run; `unasked` is how
        ///                              many Revit counted deleted (D-197)
        ///   not keeping, deletes       carry on - a preview rolls back - and
        ///                              `ahead` holds what Revit counted
        ///                              deleted, so the reply can say a Delete
        ///                              box comes when it is kept (row 5b-1992)
        /// </summary>
        private static string DeleteGate(UIApplication app, Document target, string name,
                                         string request, bool keeping, bool asksInRevit,
                                         bool sendsOut, RevitChangedSet changed,
                                         HeronChangeReport.Described described,
                                         ReadFootprint before, TransactionGroup group,
                                         HeronRollbackWitness.Watch watch,
                                         ref ConfirmedDelete confirmed, out bool again,
                                         out int unasked, out List<ElementId> ahead)
        {
            again = false;
            unasked = 0;
            ahead = null;

            // ROW 5b-1992: A PREVIEW THAT DELETES SAYS THE BOX IS COMING. Seen
            // 2026-11-22, Revit 2024.3: a curtain system on 14 faces would
            // delete 49 elements; the Delete box came up unannounced when it
            // was kept, closed by itself unanswered, and the ids the dry run
            // had listed were gone. The ids are kept here and read after the
            // preview's rollback (WithDeleteAhead) - by category, and the ones
            // the change itself made and removed said as such.
            if (!keeping && changed.DeletedIds.Count > 0) ahead = changed.DeletedIds.ToList();
            if (!keeping) return null;

            // D-197: THE OWNER'S ASK TO DELETE SWITCH IS OFF. Revit shows no
            // Delete box, and the run is kept as it ran - the chat's yes in
            // words, asked before the call (Article 7), is the only one. What
            // Revit counted deleted is still counted, logged and said first on
            // the reply (WithDeleteUnasked), so nothing goes unsaid.
            if (!asksInRevit)
            {
                unasked = changed.DeletedIds.Count;
                if (unasked > 0)
                    HeronApplication.Log("Delete box: off (Ask to Delete) - '" + RevitDispatcher.Clean(name)
                        + "' deletes " + unasked + " element(s) in '" + target.Title
                        + "' with no box; the chat's word is the yes (D-197).");
                return null;
            }

            var now = changed.DeletedIds.Select(id => id.ToString()).ToList();

            if (confirmed != null)
            {
                var how = HeronDeleteQuestion.Differs(confirmed.Ids, now);
                if (how == null) return null;

                var after = RollBackAndCheck(group, target, watch);
                HeronApplication.Log("Delete box: the second run of '" + RevitDispatcher.Clean(name)
                    + "' would delete " + how.Now + " elements, not the " + how.Shown
                    + " shown - nothing deleted.");
                return confirmed.OnReply(Json.Error("delete_set_changed",
                    HeronDeleteQuestion.SetChanged(name, how, after.Account())));
            }

            if (now.Count == 0)
            {
                // THE ONE CASE REVIT'S COUNT CANNOT ANSWER: its report never
                // reached Heron. The model's own element count then says
                // whether anything went - a delete that also adds as many
                // passes this, and the decision record says so.
                if (before == null || (described != null && described.Witnessed)) return null;
                var footprint = ReadFootprint.Of(target);
                if (footprint.Placed >= before.Placed && footprint.Types >= before.Types) return null;
                var unmeasured = RollBackAndCheck(group, target, watch);
                return Json.Error("delete_not_measured",
                    HeronDeleteQuestion.NotMeasured(name, unmeasured.Account()));
            }

            var going = changed.DeletedIds.ToList();
            var measured = RollBackAndCheck(group, target, watch);

            if (sendsOut)
            {
                return Json.Error("delete_in_publish",
                    HeronDeleteQuestion.InPublish(name, measured.Account()));
            }

            // EVERY ONE OF THEM BACK, or none of them is shown: a box listing
            // elements the rollback did not return would ask about a model that
            // is not there.
            var returned = going.Count(id => InModel(target, id));
            if (!measured.Held || returned < going.Count)
            {
                return Json.Error("delete_not_shown",
                    HeronDeleteQuestion.NotShown(name, going.Count, measured.Account()));
            }

            var refused = AskToDelete(app, target, name, request, going, changed.Added.Count,
                                      changed.Changed.Count, measured, out confirmed);
            if (confirmed == null) return refused;
            again = true;
            return null;
        }

        /// <summary>
        /// Show the modeller what would be deleted and ask Delete or Don't
        /// delete. Null, with `confirmed` set, on Delete in time; otherwise the
        /// refusal to return. The first run is already rolled back.
        ///
        /// IN ORDER:
        ///   1. Read each one's category, and what it goes with - an element
        ///      Revit lists as dependent on another that goes (a door on its
        ///      wall). Not above GoesWithCap, and the box says so.
        ///   2. Select the ones that can be shown and zoom to them - only in
        ///      the model in front, only up to D-60's cap. No transaction.
        ///   3. Redraw, mark the dispatcher's job as waiting for a person, and
        ///      ask inside HeronDialogWatch.HeronAsks(), so the job's dialog
        ///      watch never answers it.
        ///   4. Put the modeller's selection and view back WHATEVER they
        ///      answered: Delete runs the change again, on their own selection.
        /// </summary>
        private static string AskToDelete(UIApplication app, Document target, string name,
                                          string request, IList<ElementId> going, int added,
                                          int altered, HeronRollbackWitness.Outcome measured,
                                          out ConfirmedDelete confirmed)
        {
            confirmed = null;
            var shown = RevitDispatcher.Clean(name);
            var model = "";
            try { model = target.Title; } catch { }

            var goesWithRead = going.Count <= HeronDeleteQuestion.GoesWithCap;
            var with = goesWithRead ? WhatGoesWith(target, going)
                                    : new Dictionary<ElementId, ElementId>();
            var rows = new List<HeronDeleteQuestion.Going>();
            foreach (var id in going)
            {
                ElementId root;
                var withWhat = with.TryGetValue(id, out root) ? ListedAs(target, root) : null;
                rows.Add(new HeronDeleteQuestion.Going(id.ToString(), ListedAs(target, id), withWhat));
            }

            UIDocument front = null;
            try { front = app.ActiveUIDocument; } catch { front = null; }
            var inFront = front != null && front.Document != null && front.Document.Equals(target);

            var showable = Showable(target, going);
            var selected = false;
            var zoomed = false;
            Func<bool> putBack = () => true;
            if (inFront && HeronKeepQuestion.Highlights(showable.Count))
            {
                try
                {
                    var theirs = front.Selection.GetElementIds();
                    front.Selection.SetElementIds(showable);
                    var shownIn = front;
                    putBack = () => PutSelectionBack(shownIn, theirs);
                    selected = true;
                }
                catch (Exception ex)
                {
                    HeronApplication.Log("Could not show what would be deleted: " + ex.Message);
                }

                if (selected)
                {
                    var cameraBack = ZoomTo(front, target, showable);
                    if (cameraBack != null)
                    {
                        zoomed = true;
                        var selectionBack = putBack;
                        putBack = () => { cameraBack(); return selectionBack(); };
                    }
                }
            }

            try { if (inFront) front.RefreshActiveView(); }
            catch (Exception ex) { HeronApplication.Log("Could not redraw the view: " + ex.Message); }

            var onScreen = selected ? showable.Count : 0;
            var window = RevitDispatcher.TimeToAnswer();

            // NO TIME LEFT TO ASK - the caller has already stopped waiting.
            // A box that can only mean no is not shown.
            if (window.HasValue && window.Value <= TimeSpan.Zero)
            {
                var restoredEarly = putBack();
                return DeleteFields(Json.Error("delete_too_late",
                        HeronDeleteQuestion.NoTimeToAsk(name, going.Count, measured.Account(),
                                                        restoredEarly)),
                    going.Count, 0, HeronDeleteQuestion.TooLate,
                    HeronDeleteQuestion.Asking(going.Count, 0, HeronDeleteQuestion.TooLate));
            }

            var answerBy = window.HasValue
                ? DateTime.Now.Add(window.Value).ToString("HH:mm", CultureInfo.InvariantCulture)
                : null;

            RevitDispatcher.AwaitingDeleteAnswer();

            var result = TaskDialogResult.Cancel;
            var closedByItself = false;
            var clock = Stopwatch.StartNew();
            try
            {
                var box = new TaskDialog(HeronDeleteQuestion.Title(going.Count))
                {
                    MainInstruction = HeronDeleteQuestion.Headline(going.Count, model),
                    MainContent = HeronDeleteQuestion.Content(
                        Json.ReadString(request, "chatWords"), shown, model, rows, goesWithRead,
                        added, altered,
                        HeronDeleteQuestion.OnScreen(going.Count, showable.Count, inFront,
                                                     selected, zoomed),
                        answerBy),
                    ExpandedContent = HeronDeleteQuestion.Ids(rows),
                    AllowCancellation = true,
                    CommonButtons = TaskDialogCommonButtons.None,
                };
                box.AddCommandLink(TaskDialogCommandLinkId.CommandLink1,
                    HeronDeleteQuestion.DeleteLink, HeronDeleteQuestion.DeleteNote(shown));
                box.AddCommandLink(TaskDialogCommandLinkId.CommandLink2,
                    HeronDeleteQuestion.DontDeleteLink, HeronDeleteQuestion.DontDeleteNote);

                // DON'T DELETE IS THE DEFAULT, set once both links exist -
                // Revit's setter refuses a button the box does not have yet.
                box.DefaultButton = TaskDialogResult.CommandLink2;

                // IT CLOSES BY ITSELF AT ITS DEADLINE - 5b-1700. A Delete after
                // it is not kept anyway; left open, the box only kept Revit
                // busy, and every request after it was refused until a person
                // came back to close it.
                using (var closer = BoxCloser.Start(HeronDeleteQuestion.CloseAfter(window), going.Count))
                using (HeronDialogWatch.HeronAsks())
                {
                    result = box.Show();
                    closedByItself = closer != null && closer.Closed;
                }
            }
            catch (Exception ex)
            {
                // A box that could not be built or shown is not a yes.
                HeronApplication.Log("The delete box could not be built or shown: " + ex.Message);
                result = TaskDialogResult.Cancel;
            }
            clock.Stop();

            var answer = result == TaskDialogResult.CommandLink1
                ? (HeronDeleteQuestion.Late(clock.Elapsed, window)
                    ? HeronDeleteQuestion.TooLate
                    : HeronDeleteQuestion.Deleted)
                : result == TaskDialogResult.CommandLink2
                    ? HeronDeleteQuestion.DontDelete
                    : closedByItself
                        ? HeronDeleteQuestion.TimedOut
                        : HeronDeleteQuestion.Closed;

            var restored = putBack();
            HeronApplication.Log("Delete box: '" + shown + "' would delete " + going.Count
                + " elements in '" + model + "'; the modeller answered " + answer + ".");

            var asking = HeronDeleteQuestion.Asking(going.Count, onScreen, answer, window);
            if (answer == HeronDeleteQuestion.Deleted)
            {
                confirmed = new ConfirmedDelete(rows.Select(row => row.Id), going.Count, onScreen,
                                                asking);

                // THE SECOND RUN GETS ITS OWN TIME, from now: the box took the
                // modeller's, and the dispatcher still waits a whole operation
                // timeout after the last moment a Delete is taken.
                HeronRunClock.Again();
                return null;
            }

            // EACH CODE WRITTEN OUT, as every refusal in the add-in is, so the
            // failure table's check can find what the add-in produces.
            var declined = HeronDeleteQuestion.Declined(asking, name, measured.Account(), restored);
            var refusal = answer == HeronDeleteQuestion.TooLate || answer == HeronDeleteQuestion.TimedOut
                ? Json.Error("delete_too_late", declined)
                : Json.Error("delete_declined", declined);
            return DeleteFields(refusal, going.Count, onScreen, answer, asking);
        }

        /// <summary>
        /// For each element that goes only because another one goes, the
        /// element it goes with - followed up to the one deleted in its own
        /// right. Revit's own list, Element.GetDependentElements, asked once
        /// per element; one it will not answer is left as deleted in its own
        /// right, and the box still lists it.
        /// </summary>
        private static Dictionary<ElementId, ElementId> WhatGoesWith(Document doc,
                                                                     ICollection<ElementId> going)
        {
            var set = new HashSet<ElementId>(going);
            var parent = new Dictionary<ElementId, ElementId>();
            foreach (var id in going)
            {
                ICollection<ElementId> dependants;
                try
                {
                    var element = doc.GetElement(id);
                    if (element == null) continue;
                    dependants = element.GetDependentElements(null);
                }
                catch (Exception)
                {
                    continue;
                }
                if (dependants == null) continue;

                foreach (var dependant in dependants)
                {
                    if (dependant == null || dependant.Equals(id) || !set.Contains(dependant)) continue;
                    if (!parent.ContainsKey(dependant)) parent[dependant] = id;
                }
            }

            // UP TO THE ONE DELETED IN ITS OWN RIGHT - a wall's door's tag goes
            // with the wall. Bounded, because two elements can each list the
            // other.
            var root = new Dictionary<ElementId, ElementId>();
            foreach (var pair in parent)
            {
                var at = pair.Value;
                ElementId up;
                for (var step = 0; step < 16 && parent.TryGetValue(at, out up)
                                   && !up.Equals(pair.Key); step++)
                {
                    at = up;
                }
                root[pair.Key] = at;
            }
            return root;
        }

        /// <summary>An element's category's name, or null for none or for one that cannot be read.</summary>
        private static string CategoryName(Document doc, ElementId id)
        {
            try
            {
                var element = doc.GetElement(id);
                return element == null || element.Category == null ? null : element.Category.Name;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// What the box lists an element under: its category's name, or for
        /// one with none, "(no category)" and Revit's own name for its kind -
        /// FRAGMENT-ISSUES 5b-1700. Read after the rollback, when every one of
        /// them is back in the model. Never throws.
        /// </summary>
        private static string ListedAs(Document doc, ElementId id)
        {
            var category = CategoryName(doc, id);
            if (category != null) return category;
            string kind = null;
            try
            {
                var element = doc.GetElement(id);
                if (element != null) kind = element.GetType().Name;
            }
            catch (Exception)
            {
                kind = null;
            }
            return HeronDeleteQuestion.WithoutCategory(kind);
        }

        /// <summary>
        /// Closes the delete box when its time to answer runs out - 5b-1700.
        /// A thread-pool timer, because Revit's own thread is inside the box
        /// until it closes; at the deadline it finds the box among the windows
        /// of the thread that showed it - by its class and its exact title,
        /// HeronDeleteQuestion.IsTheBox - and posts it WM_CLOSE, which the box
        /// takes as Esc (AllowCancellation) and so as no. Nothing else is
        /// touched, and a box already answered is simply not found. Dispose it
        /// once the box has its answer.
        ///
        /// NOT YET RUN IN REVIT (NEEDS-CHECKING Group NY, NY2): whether Revit's
        /// task dialog closes on WM_CLOSE is the part a real Revit must show.
        /// If it does not, the box stays open exactly as it did before this.
        /// </summary>
        private sealed class BoxCloser : IDisposable
        {
            private const uint WmClose = 0x0010;
            private readonly System.Threading.Timer _timer;
            private readonly uint _thread;
            private readonly int _total;
            private int _closed;

            private BoxCloser(TimeSpan after, int total)
            {
                _thread = GetCurrentThreadId();
                _total = total;
                _timer = new System.Threading.Timer(delegate { CloseTheBox(); }, null, after,
                                                    System.Threading.Timeout.InfiniteTimeSpan);
            }

            /// <summary>A closer for the box about to be shown, or null when it has no deadline.</summary>
            internal static BoxCloser Start(TimeSpan? after, int total)
            {
                return after.HasValue ? new BoxCloser(after.Value, total) : null;
            }

            /// <summary>Did it close the box?</summary>
            internal bool Closed
            {
                get { return System.Threading.Interlocked.CompareExchange(ref _closed, 0, 0) == 1; }
            }

            public void Dispose()
            {
                _timer.Dispose();
            }

            private void CloseTheBox()
            {
                try
                {
                    EnumThreadWindows(_thread, delegate (IntPtr window, IntPtr unused)
                    {
                        if (!IsWindowVisible(window)) return true;
                        var kind = new System.Text.StringBuilder(64);
                        GetClassName(window, kind, kind.Capacity);
                        if (!string.Equals(kind.ToString(), HeronDeleteQuestion.DialogClass,
                                           StringComparison.Ordinal)) return true;
                        var title = new System.Text.StringBuilder(256);
                        GetWindowText(window, title, title.Capacity);
                        if (!HeronDeleteQuestion.IsTheBox(kind.ToString(), title.ToString(), _total))
                            return true;
                        if (PostMessage(window, WmClose, IntPtr.Zero, IntPtr.Zero))
                            System.Threading.Interlocked.Exchange(ref _closed, 1);
                        return false;
                    }, IntPtr.Zero);
                }
                catch (Exception ex)
                {
                    // A thread-pool thread: nothing may escape it. The box then
                    // stays open, as it did before this closer existed.
                    HeronApplication.Log("Could not close the delete box at its deadline: " + ex.Message);
                }
            }

            private delegate bool EnumWindowsProc(IntPtr window, IntPtr lParam);

            [System.Runtime.InteropServices.DllImport("kernel32.dll")]
            private static extern uint GetCurrentThreadId();

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            private static extern bool EnumThreadWindows(uint threadId, EnumWindowsProc callback, IntPtr lParam);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            private static extern bool IsWindowVisible(IntPtr window);

            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            private static extern int GetClassName(IntPtr window, System.Text.StringBuilder name, int size);

            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            private static extern int GetWindowText(IntPtr window, System.Text.StringBuilder text, int size);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
        }

        /// <summary>Is the element in the model now? Never throws.</summary>
        private static bool InModel(Document doc, ElementId id)
        {
            try { return doc.GetElement(id) != null; }
            catch (Exception) { return false; }
        }

        /// <summary>
        /// D-197: a kept change Revit counted deletions in, kept with no Delete
        /// box because Ask to Delete is off. `deleteAsked` false, the count,
        /// and the sentence the chat prints first - the same place D-142's
        /// `deleteAsking` goes - so the modeller hears what went.
        /// </summary>
        private static string WithDeleteUnasked(string answer, int deleted)
        {
            if (string.IsNullOrEmpty(answer) || !answer.EndsWith("}", StringComparison.Ordinal))
                return answer;

            var said = "Revit did not ask before deleting: Ask to Delete is off in Heron's Settings. "
                     + deleted + " element" + (deleted == 1 ? " was" : "s were")
                     + " deleted on the chat's word; Ctrl+Z brings back the whole change as one step.";

            return answer.Substring(0, answer.Length - 1)
                 + "," + Json.Bool("deleteAsked", false)
                 + "," + Json.Num("deletedUnasked", deleted)
                 + "," + Json.Str("deleteSkipped", AskSkippedBySwitch)
                 + "," + Json.Str("deleteAsking", said) + "}";
        }

        /// <summary>
        /// ROW 5b-1992: a PREVIEW Revit counted deletions in. Read after the
        /// preview's rollback, so the elements that were in the model are back
        /// and named by category (ListedAs); an id that names nothing now was
        /// made by the change itself and removed by it again - never in the
        /// model, and said so rather than listed. `deleteAheadSays` is the
        /// sentence the chat prints first: when this change is kept, Revit's
        /// Delete box comes up for them - or, with Ask to Delete off, it does
        /// not, and the chat's word is the yes (D-197).
        /// </summary>
        private static string WithDeleteAhead(string answer, Document doc, IList<ElementId> ahead,
                                              bool asksInRevit)
        {
            if (string.IsNullOrEmpty(answer) || !answer.EndsWith("}", StringComparison.Ordinal)
                || ahead == null || ahead.Count == 0)
                return answer;

            var byCategory = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var madeAndGone = 0;
            foreach (var id in ahead)
            {
                if (!InModel(doc, id)) { madeAndGone++; continue; }
                var listed = ListedAs(doc, id);
                int had;
                byCategory[listed] = byCategory.TryGetValue(listed, out had) ? had + 1 : 1;
            }
            var parts = byCategory.OrderByDescending(pair => pair.Value)
                                  .Select(pair => pair.Value.ToString("N0", CultureInfo.InvariantCulture)
                                                  + " " + pair.Key)
                                  .ToList();
            var categories = string.Join(", ", parts);
            var says = HeronDeleteQuestion.Ahead(ahead.Count, categories, madeAndGone, asksInRevit);

            return answer.Substring(0, answer.Length - 1)
                 + "," + Json.Num("deleteAhead", ahead.Count)
                 + "," + Json.Str("deleteAheadByCategory", categories)
                 + "," + Json.Num("deleteAheadMadeAndGone", madeAndGone)
                 + "," + Json.Str("deleteAheadSays", says) + "}";
        }

        /// <summary>
        /// The delete box's fields on a reply - whether it was asked, how many
        /// it listed and how many were selected, the answer, and the sentence
        /// the chat prints first (heron_run_records.verdict_line). Beside
        /// D-126's `asked` and `asking`, never in them: a DRAFT's one run is
        /// asked both questions.
        /// </summary>
        private static string DeleteFields(string answer, int total, int selected, string said,
                                           string asking)
        {
            if (string.IsNullOrEmpty(answer) || !answer.EndsWith("}", StringComparison.Ordinal))
                return answer;

            return answer.Substring(0, answer.Length - 1)
                 + "," + Json.Bool("deleteAsked", true)
                 + "," + Json.Num("deleteShown", total)
                 + "," + Json.Num("deleteSelected", selected)
                 + "," + Json.Str("deleteAnswer", said)
                 + "," + Json.Str("deleteAsking", asking) + "}";
        }
    }
}
