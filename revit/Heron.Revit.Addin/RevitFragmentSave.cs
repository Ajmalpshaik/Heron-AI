// Heron-Agent:  HERON-REVIT-CMP-021
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Heron.Bridge;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// The save of the document Heron works in, done by the add-in once
    /// Heron's one Undo entry has closed - OPEN-QUESTIONS Q-69, Shape 2,
    /// accepted by Ajmal PS on 2026-11-16 in his own words, "go with you
    /// suggetions": save after the group closes, from the add-in, only when
    /// the modeller asks; Heron never picks a name itself. Recorded as D-171,
    /// and its preview point read back to him the same day: "okkey".
    ///
    /// WHY THE ADD-IN AND NOT THE FRAGMENT. Revit's Document.Save and SaveAs
    /// "may not be called unless all transactions, sub-transactions, and
    /// transaction groups that were opened by the API code were closed"
    /// (RevitAPI.xml, 2020 to 2027), and every change Heron makes runs inside
    /// ONE TransactionGroup in that document for its one Undo entry - Article
    /// 8. So a fragment can never save the model it runs in, however it is
    /// written (row 5b-365). Twice in two days a whole build was lost because
    /// the model had never been saved (row 5b-513). Once RevitFragment.Run's
    /// group block has closed, nothing of Heron's is open, and Revit allows
    /// it. SAVE_DOCUMENT version 3 asks through the ambient need
    /// `saveWhenKept`; this does it.
    ///
    /// THE OWNER'S THREE POINTS, AS BUILT:
    ///
    ///   1. The add-in does work after the group closes. Only this save, and
    ///      only what the fragment asked for - its own file, or a Save As to
    ///      the full path the modeller typed. The reply's `save` block says
    ///      what was done, and the group itself does not change: one request,
    ///      one group, one Undo entry, and the save is not in it.
    ///   2. A preview saves too (D-171 point 6, superseding D-55 in part). A
    ///      group Revit reports KEPT is followed by the save, and so is a run
    ///      nobody was asked about that was rolled back and whose rollback
    ///      HELD - a preview, a proof's trial run: the change is undone, the
    ///      save is not, and the reply says both in as many words. It is the
    ///      one way the route can be proved while D-117 refuses a DRAFT kept
    ///      for real. A DISCARD, or a closed box, saves nothing: Revit's Keep
    ///      or Discard box is answered inside the group, before the save, so a
    ///      save there follows only a Keep (D-171 point 5). Nor does a run
    ///      whose rollback did not hold, or one Revit did not report kept.
    ///      The owner read this back on 2026-11-16 and answered "okkey".
    ///   3. A Save As renames the model. The reply names it as the change was
    ///      made (Run builds it before calling here) and this block names its
    ///      new title and file, so the chat's pin follows deliberately
    ///      (heron_write.after_save) rather than reading it as another model.
    ///
    /// AND ANOTHER OPEN DOCUMENT, THE SAME WAY - row 5b-1824, 2026-11-16.
    /// SAVE_DOCUMENT's version-2 job - a nested family built in its own
    /// window, saved so its host can load it, and its window closed - was
    /// done by the fragment INSIDE the run, before Revit's Keep or Discard
    /// box, so a Discard still wrote that file and closed it, against D-171
    /// point 5. It asks through `saveOtherWhenKept` now, and SaveOtherNow
    /// saves it - then closes its window, when asked - after the group, by
    /// the same rule as the model Heron works in: a preview that asked saves,
    /// a Discard saves nothing, a rollback that did not hold saves nothing.
    /// One save per run: a second ask stops the run (SaveAsk.Take). And
    /// Revit's box tells a publish whose save waits for Keep that nothing
    /// is saved yet (SaveWaitsForKeep, HeronKeepQuestion.WaitsForKeep).
    ///
    /// A SAVE IS NOT A SYNC (Article 10). Nothing here calls
    /// SynchronizeWithCentral. A workshared LOCAL copy is saved locally and
    /// the reply says nothing went to central; the central model itself,
    /// opened directly, is refused - saving it writes the file the whole job
    /// works from. A Save As of a workshared model is refused: it makes or
    /// moves a central model.
    ///
    /// AND IT CANNOT BE UNDONE. Ctrl+Z takes back the change, never the save:
    /// the file on disk holds it. Every sentence that reports a save says so.
    ///
    /// NOT RUN IN REVIT. Whether Revit lets an ExternalEvent callback save at
    /// all, what dialogs a save raises there, how long a big model takes, and
    /// whether another document's window closes from here after the group
    /// are NEEDS-CHECKING Group EN.
    /// </summary>
    internal static partial class RevitFragment
    {
        /// <summary>
        /// Where a run's ask is kept beside the delegate the fragment calls.
        /// A '#' is never in a need's name - BindNeeds refuses a need that is
        /// not a C# identifier - so it cannot meet one.
        /// </summary>
        private const string SaveAskKey = "saveWhenKept#asked";

        /// <summary>The same, for ANOTHER open document's save - row 5b-1824.</summary>
        private const string OtherAskKey = "saveOtherWhenKept#asked";

        /// <summary>
        /// Why a run that asks for a second save is stopped, in the modeller's
        /// words. One save per run: the box and the reply's `save` block each
        /// speak for one save, and a second would be made with neither saying so.
        /// </summary>
        private const string TwoSaves = "This run asked Heron to save two documents, and "
            + "Heron saves one document per run - ask for each in a run of its own. Nothing was "
            + "saved.";

        /// <summary>
        /// What a fragment asked for during its run. Through `saveWhenKept`: a
        /// save of the document it works in to its own file (no path), or a
        /// Save As to the path the modeller gave, and whether a file already
        /// there may be replaced. Through `saveOtherWhenKept` (row 5b-1824): a
        /// save of ANOTHER open document to the file it was opened from, and
        /// whether its window is then closed. Nothing is saved or closed by
        /// asking - the add-in decides after the group, in SaveAfterTheGroup.
        /// </summary>
        private sealed class SaveAsk
        {
            private readonly IDictionary<string, object> _heron;

            private SaveAsk(IDictionary<string, object> heron, bool other)
            {
                _heron = heron;
                Other = other;
            }

            internal bool Asked { get; private set; }

            /// <summary>True for another open document's save, false for the model Heron works in.</summary>
            internal bool Other { get; private set; }

            internal string SaveAsPath { get; private set; }
            internal bool Replace { get; private set; }

            /// <summary>Another document: the file it was opened from.</summary>
            internal string OtherPath { get; private set; }

            /// <summary>Another document: close its window once it is saved.</summary>
            internal bool CloseWindow { get; private set; }

            /// <summary>
            /// A fresh ask for this binding, kept in `heron` beside the need,
            /// and the delegate the fragment is handed. Fresh each time, so
            /// D-142's second run is judged on its own ask.
            /// </summary>
            internal static Action<string, bool> Bind(IDictionary<string, object> heron)
            {
                var ask = new SaveAsk(heron, false);
                heron[SaveAskKey] = ask;
                return ask.Take;
            }

            /// <summary>Bind's twin for `saveOtherWhenKept`: (its file, close its window).</summary>
            internal static Action<string, bool> BindOther(IDictionary<string, object> heron)
            {
                var ask = new SaveAsk(heron, true);
                heron[OtherAskKey] = ask;
                return ask.Take;
            }

            /// <summary>
            /// The run's ask, through whichever need it came - the one asked,
            /// or else whichever was bound, or null. Never two asked: Take
            /// stops the run first.
            /// </summary>
            internal static SaveAsk Of(IDictionary<string, object> heron)
            {
                var own = Find(heron, SaveAskKey);
                if (own != null && own.Asked) return own;
                var other = Find(heron, OtherAskKey);
                if (other != null && other.Asked) return other;
                return own ?? other;
            }

            private static SaveAsk Find(IDictionary<string, object> heron, string key)
            {
                object found;
                if (heron == null || !heron.TryGetValue(key, out found)) return null;
                return found as SaveAsk;
            }

            private void Take(string path, bool flag)
            {
                var given = (path ?? "").Trim().Trim('"').Trim();

                // ONE SAVE PER RUN: the other need already asked, or another
                // document than the one this need already named. Thrown inside
                // the fragment, so the run stops and is rolled back, and
                // nothing is saved.
                var sibling = Find(_heron, Other ? SaveAskKey : OtherAskKey);
                if ((sibling != null && sibling.Asked) || (Other && Asked && !SamePath(given, OtherPath)))
                    throw new InvalidOperationException(TwoSaves);

                Asked = true;
                if (Other)
                {
                    OtherPath = given;
                    CloseWindow = flag;
                }
                else
                {
                    SaveAsPath = given.Length == 0 ? null : given;
                    Replace = flag;
                }
            }
        }

        /// <summary>
        /// The reply, with a `save` block when the fragment asked for a save
        /// - and the save done, after a kept change or a preview, when every
        /// condition still holds. The reply unchanged when nothing was asked.
        ///
        /// CALLED ONCE BY Run, AT ITS END - after the group's block has closed
        /// and the reply is built; only the modeller's answer or D-138's grant
        /// is added to it after this, as fields. `kept` is Revit's own status for the
        /// group (groupKept), `tried` whether Heron asked Revit to keep it
        /// (`apply` as the run ended), `rollbackHeld` whether Revit reported
        /// the group's rollback held, `sendsOut` whether this job is
        /// run_fragment_publish, `answered` the modeller's answer to the box
        /// or null when nobody was asked.
        ///
        /// THE RULE, D-171, IN ORDER:
        ///   asked for no save                        - nothing, and no block
        ///   the modeller did not keep it (Discard,
        ///   a closed box, any answer but Keep)       - NOT saved (point 5)
        ///   Heron asked to keep, Revit did not say   - NOT saved
        ///   kept nothing although Keep was pressed   - NOT saved
        ///   rolled back, nobody asked, rollback held - SAVED: a preview (point 6)
        ///   rolled back and the rollback did not hold- NOT saved
        ///   kept                                     - SAVED
        /// and in every SAVED case only for a Publish job whose switches still
        /// stand, to the model's own file or the path given - or, for ANOTHER
        /// open document (row 5b-1824), to the file it was opened from, its
        /// window then closed when asked. One rule for both: whichever the run
        /// asked for, it is decided here, before the one line that saves.
        /// </summary>
        private static string SaveAfterTheGroup(string reply, IDictionary<string, object> heron,
                                                UIApplication app, Document target, string name,
                                                HeronRisk risk, bool sendsOut, bool tried,
                                                bool kept, bool rollbackHeld, string answered)
        {
            var ask = SaveAsk.Of(heron);
            if (ask == null || !ask.Asked) return reply;
            if (string.IsNullOrEmpty(reply) || !reply.EndsWith("}", StringComparison.Ordinal))
                return reply;

            var saveAs = ask.SaveAsPath != null;
            var title = ask.Other ? OtherTitle(target, ask.OtherPath) : TitleOf(target);
            Func<string, string> notSaved = said => ask.Other ? NotSavedOther(ask, said)
                                                              : NotSaved(saveAs, said);
            string block;

            if (answered != null && answered != Keep && answered != RestOfJob)
            {
                // D-171 POINT 5: the box is answered inside the group, before
                // the save, so a save follows only a Keep.
                block = notSaved(NotKeptSentence(title, answered));
            }
            else if (tried && !kept)
            {
                // Neither kept nor rolled back, as far as Revit said - the
                // verdict says what that means for the model. Nothing is saved
                // on top of a change nobody can vouch for.
                block = notSaved("NOT SAVED. Revit did not report Heron's change kept, so '"
                    + title + "' was not saved - read the verdict above before trusting the model.");
            }
            else if (!kept && answered != null)
            {
                // Keep pressed, and Revit counted nothing to keep (D-160).
                block = notSaved("NOT SAVED. Nothing of the change was kept, so '" + title
                    + "' was not saved.");
            }
            else if (!kept && !rollbackHeld)
            {
                // A PREVIEW SAVES ONLY A MODEL ITS ROLLBACK PUT BACK. Saving one
                // that may still hold part of the change would write that part
                // to disk, where no Undo reaches.
                block = notSaved("NOT SAVED. This run was rolled back and Revit did not "
                    + "report that the rollback held, so the model may still hold part of the "
                    + "change - Heron saved nothing on top of it. '" + title + "' is as it was on "
                    + "disk. Look at the model before saving it yourself.");
            }
            else if (!sendsOut)
            {
                // A save is a Publish job, and the run guard cancels a save of
                // an open model under any other (HeronRunPolicy). Said rather
                // than tried.
                block = notSaved("NOT SAVED. A save is a Publish job, and this one did not "
                    + "run as one, so '" + title + "' was not saved. " + ChangeState(kept));
            }
            else
            {
                // ARTICLE 13: the switches again, as the save begins - the
                // modeller's Keep can come minutes after the gate let the job
                // in. HeronRunPolicy.Withdrawn's own reason, not WhyNotKept's
                // sentence: that one says the change was not kept.
                var off = HeronRunPolicy.Withdrawn(risk, HeronConfig.Load(), HeronStop.IsStopped);
                if (off != null)
                    block = notSaved("NOT SAVED. " + Capital(off) + ", so '" + title
                        + "' was not saved. " + ChangeState(kept));
                else if (target == null)
                    block = notSaved("NOT SAVED. Heron could not reach the model to save it.");
                else
                    block = ask.Other ? SaveOtherNow(app, target, ask, kept)
                          : saveAs ? SaveAsNow(target, ask, kept) : SaveNow(target, kept);
            }

            return reply.Substring(0, reply.Length - 1) + ",\"save\":" + block + "}";
        }

        /// <summary>
        /// What Revit's Keep or Discard box says waits for Keep -
        /// HeronKeepQuestion.WaitsForKeep's sentence for the save this run
        /// asked for, or null when it asked for none. Read inside the group,
        /// before the box: the save itself is made after the group, and only
        /// after a Keep (D-171 point 5) - so the box never tells a publish its
        /// save "is done already" (row 5b-1824).
        /// </summary>
        private static string SaveWaitsForKeep(IDictionary<string, object> heron, Document target)
        {
            var ask = SaveAsk.Of(heron);
            if (ask == null || !ask.Asked) return null;
            if (!ask.Other)
            {
                var own = PathOf(target);
                return HeronKeepQuestion.WaitsForKeep(
                    ask.SaveAsPath ?? (own.Length > 0 ? own : TitleOf(target)), true, false);
            }

            // ANOTHER DOCUMENT: Keep saves it only if it has unsaved changes -
            // the fragment asks for a window's close alone when it has none.
            var other = OpenAt(target, ask.OtherPath);
            var unsaved = true;
            try { unsaved = other == null || other.IsModified; } catch (Exception) { unsaved = true; }
            return HeronKeepQuestion.WaitsForKeep(ask.OtherPath, unsaved, ask.CloseWindow);
        }

        /// <summary>Why a run the modeller did not keep saved nothing, in their words.</summary>
        private static string NotKeptSentence(string title, string answered)
        {
            if (answered == Discard || answered == Closed)
            {
                return "NOT SAVED. You did not keep the change in Revit ("
                    + (answered == Discard ? "you pressed Discard" : "the box was closed")
                    + "), so nothing was kept and '" + title + "' was not saved - a save follows "
                    + "only a Keep.";
            }
            return "NOT SAVED. The change was not kept in Revit, so '" + title
                + "' was not saved - a save follows only a Keep.";
        }

        /// <summary>What happened to the change itself, said beside a save not done.</summary>
        private static string ChangeState(bool kept)
        {
            return kept
                ? "The change itself was kept - one Ctrl+Z in Revit undoes it."
                : "The change itself was rolled back, as a preview is.";
        }

        /// <summary>
        /// The opening of a done save's sentence on a PREVIEW, or "" after a
        /// kept change: the change is undone and the save is not, and the
        /// reply says both before anything else (D-171).
        /// </summary>
        private static string PreviewOpening(bool kept)
        {
            return kept
                ? ""
                : "THE CHANGE WAS ROLLED BACK - this run was a preview - AND THE SAVE WAS DONE: "
                  + "what was saved is the model as it is now, without Heron's change. ";
        }

        /// <summary>
        /// A save of the model to its own file, done now - after a kept change,
        /// or after a preview (`kept` false), which saves the model as its
        /// rollback left it.
        /// </summary>
        private static string SaveNow(Document target, bool kept)
        {
            var title = TitleOf(target);
            var path = PathOf(target);
            if (path.Length == 0)
            {
                return NotSaved(false, "NOT SAVED. '" + title + "' has never been saved, so it has "
                    + "no file to save to. Give the full path to save it as - Heron never chooses "
                    + "a name.");
            }
            var refused = OpenToSave(target, title);
            if (refused != null) return NotSaved(false, refused);

            var central = CentralItself(target, title, path);
            if (central != null) return NotSaved(false, central);

            if (!target.IsModified)
            {
                return Json.Obj(Json.Bool("asked", true), Json.Str("kind", "save"),
                    Json.Bool("done", false), Json.Bool("nothingToSave", true),
                    Json.Bool("preview", !kept), Json.Str("savedTo", path),
                    Json.Str("said", "NOTHING TO SAVE. '" + title + "' had no unsaved changes, "
                        + "so its file at " + path + " already holds what is open, and nothing "
                        + "was written. " + ChangeState(kept)));
            }

            var workshared = false;
            try { workshared = target.IsWorkshared; } catch (Exception) { workshared = false; }

            try
            {
                target.Save(new SaveOptions());
            }
            catch (Exception ex)
            {
                return NotSaved(false, "NOT SAVED. Revit refused to save '" + title + "': "
                    + ex.Message + " " + ChangeState(kept) + " The file on disk is as it was.");
            }

            // READ BACK, not assumed: a saved model has nothing unsaved.
            var still = true;
            try { still = target.IsModified; } catch (Exception) { still = true; }

            var said = still
                ? PreviewOpening(kept) + "Revit returned from the save of '" + title + "' without "
                  + "an error, and the model still reads as having unsaved changes - look at the "
                  + "file's time on disk before trusting it. A save cannot be undone."
                : PreviewOpening(kept) + "SAVED. '" + title + "' was saved to " + path + " once "
                  + "Heron's Undo entry had closed. A SAVE CANNOT BE UNDONE: "
                  + (kept ? "Ctrl+Z takes back the change, never the save, and " : "")
                  + "the file on disk holds it now.";
            if (workshared)
            {
                said += " IT IS WORKSHARED AND NOTHING WENT TO CENTRAL - this wrote your local "
                      + "copy only, and nobody else on the job sees it until you Synchronize with "
                      + "Central yourself. Heron never syncs.";
            }

            return Json.Obj(Json.Bool("asked", true), Json.Str("kind", "save"),
                Json.Bool("done", !still), Json.Bool("preview", !kept), Json.Str("savedTo", path),
                Json.Bool("localOnly", workshared), Json.Str("said", said));
        }

        /// <summary>
        /// A Save As of a model that has NEVER been saved, to the full path
        /// the modeller gave, done now - after a kept change or a preview. A
        /// model that has a file is saved to it, never under a new name.
        /// </summary>
        private static string SaveAsNow(Document target, SaveAsk ask, bool kept)
        {
            var was = TitleOf(target);
            var had = PathOf(target);
            if (had.Length > 0)
            {
                return NotSaved(true, "NOT SAVED. '" + was + "' already has a file at " + had
                    + ", and Heron saves it there - never under a new name. Ask for a save to its "
                    + "own file instead.");
            }
            var refused = OpenToSave(target, was);
            if (refused != null) return NotSaved(true, refused);

            var workshared = true;
            try { workshared = target.IsWorkshared; } catch (Exception) { workshared = true; }
            if (workshared)
            {
                return NotSaved(true, "NOT SAVED. '" + was + "' is workshared, and a Save As makes "
                    + "or moves a central model - that is the modeller's to do, with File > Save "
                    + "As. Nothing was written.");
            }

            string full;
            var wrong = SaveAsPathRefusal(target, ask.SaveAsPath, ask.Replace, out full);
            if (wrong != null) return NotSaved(true, wrong);

            var options = new SaveAsOptions();
            if (ask.Replace) options.OverwriteExistingFile = true;

            try
            {
                target.SaveAs(full, options);
            }
            catch (Exception ex)
            {
                return NotSaved(true, "NOT SAVED. Revit refused to save '" + was + "' as " + full
                    + ": " + ex.Message + " " + ChangeState(kept) + " Nothing was written.");
            }

            // READ BACK: the model now names the file it was saved to, and
            // the file is on disk.
            var now = PathOf(target);
            var landed = SamePath(now, full) && File.Exists(full);
            var title = TitleOf(target);
            string guid = null;
            try { guid = RevitOperations.CreationGuid(target); } catch (Exception) { guid = null; }

            var said = landed
                ? PreviewOpening(kept) + "SAVED AS. '" + was + "' was saved as " + full
                  + " once Heron's Undo entry had closed, and in Revit it is now '" + title
                  + "'. The name is the path you gave; Heron chose none."
                  + (ask.Replace ? " It REPLACED the file that was there, as you said to." : "")
                  + " A SAVE CANNOT BE UNDONE"
                  + (kept ? ": Ctrl+Z takes back the change, never the save." : ".")
                : PreviewOpening(kept) + "Revit returned from the Save As of '" + was + "' to "
                  + full + " without an error, but the model does not name that file now ("
                  + (now.Length == 0 ? "it has none" : now) + ") - look on disk before trusting it.";

            return Json.Obj(Json.Bool("asked", true), Json.Str("kind", "saveAs"),
                Json.Bool("done", landed), Json.Bool("preview", !kept),
                Json.Str("savedTo", landed ? full : null),
                Json.Bool("replaced", landed && ask.Replace),
                Json.Str("wasDocument", was),
                Json.Str("document", title),
                Json.Str("documentPath", now.Length == 0 ? null : now),
                Json.Str("creationGuid", guid),
                Json.Str("said", said));
        }

        /// <summary>
        /// ANOTHER open document - not the one Heron works in - saved to the
        /// file it was opened from, and its window then closed when the run
        /// asked: SAVE_DOCUMENT's version-2 job, a nested family built in its
        /// own window and saved so its host can load it. Done here, after the
        /// group, since 2026-11-16 (row 5b-1824): until then the fragment saved
        /// and closed it inside the run, before Revit's Keep or Discard box, so
        /// a Discard still wrote it (D-171 point 5). A preview saves it too, as
        /// version 2's proof did (D-171 point 6) - `kept` false.
        ///
        /// VERSION 2'S RULES, READ AGAIN NOW - the modeller's answer can come
        /// minutes after the run: found by its file, not read-only, saved only
        /// when it has unsaved changes, never the window in front closed
        /// (Revit lets no tool close that one), never a window still holding
        /// unsaved changes closed - Close(false) would lose them - and the
        /// close read back. Never a sync.
        /// </summary>
        private static string SaveOtherNow(UIApplication app, Document target, SaveAsk ask,
                                           bool kept)
        {
            var file = ask.OtherPath ?? "";
            var other = OpenAt(target, file);
            if (other == null)
            {
                return NotSavedOther(ask, "NOT SAVED. No document open in this Revit is saved at '"
                    + file + "' now, so nothing was saved. " + ChangeState(kept));
            }
            var title = TitleOf(other);
            if (other.Equals(target))
            {
                return NotSavedOther(ask, "NOT SAVED. '" + title + "' is the model Heron works in, "
                    + "and this save was asked for as another open document's. Nothing was written.");
            }
            var refused = OpenToSave(other, title);
            if (refused != null) return NotSavedOther(ask, refused);

            var workshared = false;
            try { workshared = other.IsWorkshared; } catch (Exception) { workshared = false; }
            var unsaved = true;
            try { unsaved = other.IsModified; } catch (Exception) { unsaved = true; }

            if (unsaved)
            {
                try
                {
                    other.Save(new SaveOptions());
                }
                catch (Exception ex)
                {
                    return NotSavedOther(ask, "NOT SAVED. Revit refused to save '" + title + "': "
                        + ex.Message + " " + ChangeState(kept) + " The file on disk is as it was.");
                }
            }

            // READ BACK, not assumed: a saved document has nothing unsaved.
            var still = true;
            try { still = other.IsModified; } catch (Exception) { still = true; }

            string said;
            if (!unsaved)
            {
                said = "NOTHING TO SAVE. '" + title + "' had no unsaved changes, so its file at "
                     + file + " already holds what is open, and nothing was written. "
                     + ChangeState(kept);
            }
            else if (still)
            {
                said = PreviewOpeningOther(kept, title) + "Revit returned from the save of '" + title
                     + "' without an error, and it still reads as having unsaved changes - look "
                     + "at the file's time on disk before trusting it. A save cannot be undone.";
            }
            else
            {
                said = PreviewOpeningOther(kept, title) + "SAVED. '" + title + "' was saved to "
                     + file + " once Heron's Undo entry had closed. A SAVE CANNOT BE UNDONE: the "
                     + "file on disk holds it now.";
            }
            if (unsaved && workshared)
            {
                said += " IT IS WORKSHARED AND NOTHING WENT TO CENTRAL - this wrote your local "
                      + "copy only, and nobody else on the job sees it until you Synchronize with "
                      + "Central yourself. Heron never syncs.";
            }

            // THE CLOSE THE RUN ASKED FOR, after the save and only after it -
            // version 2's closeWindow, kept in one call so the family method's
            // step 11 f still reads "saved, its window closed".
            var closed = false;
            if (ask.CloseWindow)
            {
                Document inFront = null;
                try
                {
                    var active = app == null ? null : app.ActiveUIDocument;
                    inFront = active == null ? null : active.Document;
                }
                catch (Exception) { inFront = null; }

                if (inFront != null && inFront.Equals(other))
                {
                    said += " '" + title + "' is the window in front, and Revit lets no tool close "
                          + "that one, so its window was left open - close it by hand.";
                }
                else if (still)
                {
                    said += " It still reads as having unsaved changes, so its window was NOT "
                          + "closed - closing would lose them.";
                }
                else
                {
                    try
                    {
                        closed = other.Close(false);
                        if (!closed)
                        {
                            said += " Revit did not close '" + title + "' - another add-in may have "
                                  + "cancelled it. Its file holds what was open; close it by hand.";
                        }
                    }
                    catch (Exception ex)
                    {
                        closed = false;
                        said += " Revit would not close '" + title + "': " + ex.Message
                              + ". Its file holds what was open; close it by hand.";
                    }

                    if (closed)
                    {
                        // READ BACK, not assumed: the document must be gone from
                        // what is open.
                        closed = OpenAt(target, file) == null;
                        said += closed
                            ? " Its window is closed."
                            : " Revit said '" + title + "' closed, but it is still open.";
                    }
                }
            }

            return Json.Obj(Json.Bool("asked", true), Json.Str("kind", "saveOther"),
                Json.Bool("done", unsaved && !still),
                unsaved ? null : Json.Bool("nothingToSave", true),
                Json.Bool("preview", !kept), Json.Str("savedTo", file),
                Json.Str("savedDocument", title),
                Json.Bool("closeAsked", ask.CloseWindow), Json.Bool("closed", closed),
                Json.Bool("localOnly", unsaved && workshared), Json.Str("said", said));
        }

        /// <summary>
        /// PreviewOpening for another document: the change rolled back, the
        /// save done - and that document held none of Heron's change.
        /// </summary>
        private static string PreviewOpeningOther(bool kept, string title)
        {
            return kept
                ? ""
                : "THE CHANGE WAS ROLLED BACK - this run was a preview - AND THE SAVE WAS DONE: '"
                  + title + "' was saved as it is open now, which this run did not change. ";
        }

        /// <summary>The `save` block for another document's save that was not done.</summary>
        private static string NotSavedOther(SaveAsk ask, string said)
        {
            return Json.Obj(Json.Bool("asked", true), Json.Str("kind", "saveOther"),
                Json.Bool("done", false), Json.Str("savedTo", ask.OtherPath),
                Json.Bool("closeAsked", ask.CloseWindow),
                ask.CloseWindow ? Json.Bool("closed", false) : null,
                Json.Str("said", ask.CloseWindow ? said + " Its window was left open." : said));
        }

        /// <summary>The open document saved at `path`, or null - never a linked one.</summary>
        private static Document OpenAt(Document target, string path)
        {
            if (target == null || string.IsNullOrEmpty(path)) return null;
            try
            {
                foreach (Document open in target.Application.Documents)
                {
                    if (open == null || open.IsLinked) continue;
                    if (SamePath(PathOf(open), path)) return open;
                }
            }
            catch (Exception) { }
            return null;
        }

        /// <summary>The name another document is said by: its title, or its file's name.</summary>
        private static string OtherTitle(Document target, string path)
        {
            var open = OpenAt(target, path);
            if (open != null) return TitleOf(open);
            try
            {
                var named = Path.GetFileNameWithoutExtension(path ?? "");
                return string.IsNullOrEmpty(named) ? "the document" : named;
            }
            catch (Exception) { return "the document"; }
        }

        /// <summary>
        /// Why the model cannot be saved at all right now, or null: read-only,
        /// or a transaction still open in it - Revit saves nothing then.
        /// </summary>
        private static string OpenToSave(Document target, string title)
        {
            try
            {
                if (target.IsReadOnly)
                    return "NOT SAVED. '" + title + "' is open read-only and cannot be saved.";
                if (target.IsModifiable)
                    return "NOT SAVED. A transaction is still open in '" + title + "', and Revit "
                        + "saves nothing while one is. Nothing was written.";
            }
            catch (Exception ex)
            {
                return "NOT SAVED. Heron could not read whether '" + title + "' can be saved: "
                    + ex.Message;
            }
            return null;
        }

        /// <summary>
        /// The refusal for a workshared model opened as its CENTRAL file
        /// itself, or null. Saving that writes the file the whole job works
        /// from; it is not Heron's to do. Fails closed: a workshared model
        /// Heron cannot place is refused too.
        /// </summary>
        private static string CentralItself(Document target, string title, string path)
        {
            bool workshared;
            try { workshared = target.IsWorkshared; } catch (Exception) { workshared = true; }
            if (!workshared) return null;
            try
            {
                var central = target.GetWorksharingCentralModelPath();
                var visible = central == null
                    ? "" : ModelPathUtils.ConvertModelPathToUserVisiblePath(central) ?? "";
                if (visible.Length > 0 && SamePath(visible, path))
                {
                    return "NOT SAVED. '" + title + "' is the CENTRAL model itself, opened "
                        + "directly: saving it writes the file everybody on the job works from. "
                        + "Open a local copy, or Synchronize with Central yourself - Heron never "
                        + "syncs. Nothing was written.";
                }
                return null;
            }
            catch (Exception ex)
            {
                return "NOT SAVED. '" + title + "' is workshared and Heron could not tell whether "
                    + "it is the central model itself (" + ex.Message + "), so it was not saved.";
            }
        }

        /// <summary>
        /// Why `given` is not a path to save this model as, or null - and the
        /// path, spelt in full, in `full`. Heron never makes one up: it must be
        /// a full path, to a folder that exists, ending .rvt for a project or
        /// .rfa for a family, with no file there unless the modeller said to
        /// replace it, and not the file of a model open in this Revit.
        /// </summary>
        private static string SaveAsPathRefusal(Document target, string given, bool replace,
                                                out string full)
        {
            full = null;
            var path = given ?? "";
            var rooted = path.StartsWith(@"\\", StringComparison.Ordinal)
                || (path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':'
                    && (path[2] == '\\' || path[2] == '/'));
            if (!rooted)
            {
                return "NOT SAVED. '" + path + "' is not a full path - it needs the drive or the "
                    + "network share as well as the folder and the name. Heron never chooses a "
                    + "folder or a name. Nothing was written.";
            }
            try { full = Path.GetFullPath(path); }
            catch (Exception ex)
            {
                full = null;
                return "NOT SAVED. '" + path + "' cannot be a file's path: " + ex.Message;
            }

            var family = false;
            try { family = target.IsFamilyDocument; } catch (Exception) { family = false; }
            var wanted = family ? ".rfa" : ".rvt";
            if (!string.Equals(Path.GetExtension(full), wanted, StringComparison.OrdinalIgnoreCase))
            {
                return "NOT SAVED. A " + (family ? "family" : "project") + " is saved as a "
                    + wanted + " file, and '" + full + "' is not one. Nothing was written.";
            }

            var folder = Path.GetDirectoryName(full) ?? "";
            if (folder.Length == 0 || !Directory.Exists(folder))
            {
                return "NOT SAVED. The folder " + folder + " does not exist, and Heron makes no "
                    + "folder. Nothing was written.";
            }

            try
            {
                foreach (Document open in target.Application.Documents)
                {
                    if (open == null || open.IsLinked) continue;
                    if (SamePath(PathOf(open), full))
                    {
                        return "NOT SAVED. " + full + " is the file of '" + TitleOf(open)
                            + "', open in this Revit. Nothing was written.";
                    }
                }
            }
            catch (Exception) { }

            if (File.Exists(full) && !replace)
            {
                return "NOT SAVED. A file is already at " + full + ", and Heron replaces a file "
                    + "only when you say to replace it. Nothing was written.";
            }
            return null;
        }

        /// <summary>The `save` block for a save that was not done.</summary>
        private static string NotSaved(bool saveAs, string said)
        {
            return Json.Obj(Json.Bool("asked", true), Json.Str("kind", saveAs ? "saveAs" : "save"),
                Json.Bool("done", false), Json.Str("said", said));
        }

        /// <summary>A reason that reads as the start of a sentence.</summary>
        private static string Capital(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return char.ToUpperInvariant(text[0]) + text.Substring(1);
        }

        private static string TitleOf(Document doc)
        {
            try { return doc == null ? "the model" : (doc.Title ?? "the model"); }
            catch (Exception) { return "the model"; }
        }

        private static string PathOf(Document doc)
        {
            try { return doc == null ? "" : (doc.PathName ?? ""); }
            catch (Exception) { return ""; }
        }

        /// <summary>One spelling of a path, so "D:\a\b.rvt" and "d:/a/b.rvt" are one file.</summary>
        private static bool SamePath(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            try
            {
                return string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'),
                                     Path.GetFullPath(b).TrimEnd('\\', '/'),
                                     StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
