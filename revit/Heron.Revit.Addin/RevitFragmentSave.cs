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
using Heron.Bridge;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// The save of the document Heron works in, done by the add-in once
    /// Heron's one Undo entry has closed - OPEN-QUESTIONS Q-69, Shape 2,
    /// accepted by Ajmal PS on 2026-11-16 in his own words, "go with you
    /// suggetions": save after the group closes, from the add-in, only when
    /// the modeller asks; Heron never picks a name itself.
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
    ///   2. A preview (D-55) never saves. Only a group Revit reports KEPT is
    ///      followed by a save; a preview, a proof's rolled-back run, a Discard
    ///      or a closed box saves nothing, and the reply says so. A save cannot
    ///      be rolled back, so a run that is rolled back must leave nothing.
    ///   3. A Save As renames the model. The reply names it as the change was
    ///      made (Run builds it before calling here) and this block names its
    ///      new title and file, so the chat's pin follows deliberately
    ///      (heron_write.after_save) rather than reading it as another model.
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
    /// all, what dialogs a save raises there, and how long a big model takes
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

        /// <summary>
        /// What a fragment asked for through `saveWhenKept` during its run: a
        /// save of the document it works in to its own file (no path), or a
        /// Save As to the path the modeller gave, and whether a file already
        /// there may be replaced. Nothing is saved by asking - the add-in
        /// decides after the group, in SaveAfterTheGroup.
        /// </summary>
        private sealed class SaveAsk
        {
            internal bool Asked { get; private set; }
            internal string SaveAsPath { get; private set; }
            internal bool Replace { get; private set; }

            /// <summary>
            /// A fresh ask for this binding, kept in `heron` beside the need,
            /// and the delegate the fragment is handed. Fresh each time, so
            /// D-142's second run is judged on its own ask.
            /// </summary>
            internal static Action<string, bool> Bind(IDictionary<string, object> heron)
            {
                var ask = new SaveAsk();
                heron[SaveAskKey] = ask;
                return ask.Take;
            }

            internal static SaveAsk Of(IDictionary<string, object> heron)
            {
                object found;
                if (heron == null || !heron.TryGetValue(SaveAskKey, out found)) return null;
                return found as SaveAsk;
            }

            private void Take(string saveAsPath, bool replace)
            {
                Asked = true;
                var path = (saveAsPath ?? "").Trim().Trim('"').Trim();
                SaveAsPath = path.Length == 0 ? null : path;
                Replace = replace;
            }
        }

        /// <summary>
        /// The reply, with a `save` block when the fragment asked for a save
        /// - and the save done, when the group was kept and every condition
        /// still holds. The reply unchanged when nothing was asked.
        ///
        /// CALLED ONCE BY Run, AT ITS END - after the group's block has closed
        /// and the reply is built; only the modeller's answer or D-138's grant
        /// is added to it after this, as fields. `kept` is Revit's own status for the
        /// group (groupKept), `tried` whether Heron asked Revit to keep it
        /// (`apply` as the run ended), `sendsOut` whether this job is
        /// run_fragment_publish, `answered` the modeller's answer to the box
        /// or null when nobody was asked.
        /// </summary>
        private static string SaveAfterTheGroup(string reply, IDictionary<string, object> heron,
                                                Document target, string name, HeronRisk risk,
                                                bool sendsOut, bool tried, bool kept,
                                                string answered)
        {
            var ask = SaveAsk.Of(heron);
            if (ask == null || !ask.Asked) return reply;
            if (string.IsNullOrEmpty(reply) || !reply.EndsWith("}", StringComparison.Ordinal))
                return reply;

            var saveAs = ask.SaveAsPath != null;
            var title = TitleOf(target);
            string block;

            if (tried && !kept)
            {
                // Neither kept nor rolled back, as far as Revit said - the
                // verdict says what that means for the model. Nothing is saved
                // on top of a change nobody can vouch for.
                block = NotSaved(saveAs, "NOT SAVED. Revit did not report Heron's change kept, so '"
                    + title + "' was not saved - read the verdict above before trusting the model.");
            }
            else if (!kept)
            {
                block = NotSaved(saveAs, NotKeptSentence(title, answered));
            }
            else if (!sendsOut)
            {
                // A save is a Publish job, and the run guard cancels a save of
                // an open model under any other (HeronRunPolicy). Said rather
                // than tried.
                block = NotSaved(saveAs, "NOT SAVED. A save is a Publish job, and this one did not "
                    + "run as one, so '" + title + "' was not saved. The change itself was kept - "
                    + "one Ctrl+Z in Revit undoes it.");
            }
            else
            {
                // ARTICLE 13: the switches again, as the save begins - the
                // modeller's Keep can come minutes after the gate let the job
                // in. HeronRunPolicy.Withdrawn's own reason, not WhyNotKept's
                // sentence: that one says the change was not kept, and by now
                // it was.
                var off = HeronRunPolicy.Withdrawn(risk, HeronConfig.Load(), HeronStop.IsStopped);
                if (off != null)
                    block = NotSaved(saveAs, "NOT SAVED. " + Capital(off) + ", so '" + title
                        + "' was not saved. The change itself was kept - one Ctrl+Z in Revit "
                        + "undoes it.");
                else if (target == null)
                    block = NotSaved(saveAs, "NOT SAVED. Heron could not reach the model to save it.");
                else
                    block = saveAs ? SaveAsNow(target, ask) : SaveNow(target);
            }

            return reply.Substring(0, reply.Length - 1) + ",\"save\":" + block + "}";
        }

        /// <summary>Why a run that was not kept saved nothing, in the modeller's words.</summary>
        private static string NotKeptSentence(string title, string answered)
        {
            if (answered == Discard || answered == Closed)
            {
                return "NOT SAVED. You did not keep the change in Revit ("
                    + (answered == Discard ? "you pressed Discard" : "the box was closed")
                    + "), so nothing was kept and '" + title + "' was not saved.";
            }
            if (answered != null && answered != Keep && answered != RestOfJob)
            {
                return "NOT SAVED. The change was not kept in Revit, so '" + title
                    + "' was not saved.";
            }
            return "NOT SAVED. This run was rolled back - a preview, a proof's trial run, or a run "
                + "sent without asking to keep it - and a run that is rolled back never saves: a "
                + "save cannot be rolled back (Q-69). '" + title + "' is as it was, on screen and "
                + "on disk. Ask for the change to be kept and the save follows it.";
        }

        /// <summary>A save of the model to its own file, done now.</summary>
        private static string SaveNow(Document target)
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
                    Json.Str("savedTo", path),
                    Json.Str("said", "NOTHING TO SAVE. '" + title + "' had no unsaved changes, "
                        + "so its file at " + path + " already holds what is open, and nothing "
                        + "was written."));
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
                    + ex.Message + " The change itself was kept - one Ctrl+Z in Revit undoes it - "
                    + "and the file on disk is as it was.");
            }

            // READ BACK, not assumed: a saved model has nothing unsaved.
            var still = true;
            try { still = target.IsModified; } catch (Exception) { still = true; }

            var said = still
                ? "Revit returned from the save of '" + title + "' without an error, and the model "
                  + "still reads as having unsaved changes - look at the file's time on disk before "
                  + "trusting it. A save cannot be undone."
                : "SAVED. '" + title + "' was saved to " + path + " once Heron's Undo entry had "
                  + "closed. A SAVE CANNOT BE UNDONE: Ctrl+Z takes back the change, never the save, "
                  + "and the file on disk holds it now.";
            if (workshared)
            {
                said += " IT IS WORKSHARED AND NOTHING WENT TO CENTRAL - this wrote your local "
                      + "copy only, and nobody else on the job sees it until you Synchronize with "
                      + "Central yourself. Heron never syncs.";
            }

            return Json.Obj(Json.Bool("asked", true), Json.Str("kind", "save"),
                Json.Bool("done", !still), Json.Str("savedTo", path),
                Json.Bool("localOnly", workshared), Json.Str("said", said));
        }

        /// <summary>
        /// A Save As of a model that has NEVER been saved, to the full path
        /// the modeller gave, done now. A model that has a file is saved to
        /// it, never under a new name.
        /// </summary>
        private static string SaveAsNow(Document target, SaveAsk ask)
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
                    + ": " + ex.Message + " The change itself was kept - one Ctrl+Z in Revit undoes "
                    + "it - and nothing was written.");
            }

            // READ BACK: the model now names the file it was saved to, and
            // the file is on disk.
            var now = PathOf(target);
            var landed = SamePath(now, full) && File.Exists(full);
            var title = TitleOf(target);
            string guid = null;
            try { guid = RevitOperations.CreationGuid(target); } catch (Exception) { guid = null; }

            var said = landed
                ? "SAVED AS. '" + was + "' was saved as " + full + " once Heron's Undo entry had "
                  + "closed, and in Revit it is now '" + title + "'. The name is the path you "
                  + "gave; Heron chose none."
                  + (ask.Replace ? " It REPLACED the file that was there, as you said to." : "")
                  + " A SAVE CANNOT BE UNDONE: Ctrl+Z takes back the change, never the save."
                : "Revit returned from the Save As of '" + was + "' to " + full + " without an "
                  + "error, but the model does not name that file now (" + (now.Length == 0
                      ? "it has none" : now) + ") - look on disk before trusting it.";

            return Json.Obj(Json.Bool("asked", true), Json.Str("kind", "saveAs"),
                Json.Bool("done", landed), Json.Str("savedTo", landed ? full : null),
                Json.Bool("replaced", landed && ask.Replace),
                Json.Str("wasDocument", was),
                Json.Str("document", title),
                Json.Str("documentPath", now.Length == 0 ? null : now),
                Json.Str("creationGuid", guid),
                Json.Str("said", said));
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
