// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Heron.Bridge;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// `undo_last` (D-157): Revit's own Undo, sent only while Heron's last kept
    /// change is still the top of the model's undo list.
    ///
    /// REVIT'S UNDO, NEVER A SECOND CHANGE. Heron does not work out how to put
    /// the model back and write that - a second change with risks of its own,
    /// and two entries in the undo list. It posts Revit's Undo command, which
    /// is exactly what pressing Ctrl+Z does, and only when HeronUndoGuard has
    /// seen Heron's entry land on top and nothing land on it since. Otherwise
    /// it refuses and says why, and nothing is sent.
    ///
    /// THE UNDO RUNS AFTER THIS ANSWER, NOT IN IT. PostCommand queues the
    /// command; Revit runs it once this external event returns. So the reply
    /// says it was SENT, never that it was done. The guard hears the undo when
    /// Revit reports it, and a second request before then is refused.
    ///
    /// ONLY THE MODEL ON SCREEN. Revit's Undo acts on the active document, so
    /// the pinned model must be the one in front (Golden Rule 20), checked
    /// before anything is posted. No transaction is opened here: there is
    /// nothing to change, only a command to send.
    /// </summary>
    internal static class RevitUndo
    {
        /// <summary>
        /// Whether Heron's last kept change is still the top of each model's
        /// undo list. Held HERE, in a class that names no compiler, so
        /// HeronApplication can tell it of a sync or a close without loading
        /// RevitFragment - and Roslyn with it - outside its guard.
        /// RevitFragment feeds it from the keep and its permanent listener.
        /// </summary>
        internal static readonly HeronUndoGuard Guard = new HeronUndoGuard();

        /// <summary>
        /// A model's identity as three strings - its CreationGUID where the
        /// release has one (2024 on), its path, its title. The guid alone is
        /// shared by copies of one model, so two copies open at once are told
        /// apart by path; a Save As starts a new key. RevitFragment's rollback
        /// witness keys by this too, so both watchers agree on a model.
        /// </summary>
        internal static string KeyOf(Document doc)
        {
            if (doc == null) return null;

            string guid = null;
            try { guid = RevitOperations.CreationGuid(doc); } catch { }
            var path = "";
            try { path = doc.PathName ?? ""; } catch { }
            var title = "";
            try { title = doc.Title ?? ""; } catch { }

            return (guid ?? "") + "|" + path + "|" + title;
        }

        /// <summary>Synchronized with central: Revit cleared the model's undo list.</summary>
        internal static void Synced(Document doc)
        {
            try { Guard.SyncedIn(KeyOf(doc)); }
            catch (Exception ex) { HeronApplication.Log("Undo guard missed a sync: " + ex.Message); }
        }

        /// <summary>The model is closing: nothing about its undo list is kept.</summary>
        internal static void Forget(Document doc)
        {
            try { Guard.Closed(KeyOf(doc)); }
            catch (Exception ex) { HeronApplication.Log("Undo guard could not forget a model: " + ex.Message); }
        }

        public static string Last(UIApplication app, string expectProject)
        {
            var front = app == null ? null : app.ActiveUIDocument;
            var doc = front == null ? null : front.Document;
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit, so there is nothing to undo. Nothing was undone.");
            }

            if (!string.IsNullOrEmpty(expectProject) && RevitOperations.ProjectKey(doc) != expectProject)
            {
                return Json.Error("wrong_document",
                    "This chat has been working on another model, and the one in front of Revit "
                    + "now is \"" + doc.Title + "\". Revit's Undo acts on the model in front, so "
                    + "NOTHING was undone. Bring the chat's model to the front and ask again.");
            }

            var key = KeyOf(doc);
            var undo = RevitCommandId.LookupPostableCommandId(PostableCommand.Undo);
            bool canPost;
            try { canPost = undo != null && app.CanPostCommand(undo); }
            catch (Exception) { canPost = false; }

            var refusal = Guard.Refusal(key, doc.Title, true, canPost);
            if (refusal != null) return Json.Error("undo_refused", refusal);

            var said = Guard.SentSentence(key);
            var entry = Guard.Of(key);
            try
            {
                app.PostCommand(undo);
            }
            catch (Exception ex)
            {
                return Json.Error("undo_refused",
                    "Revit would not take the Undo command: " + ex.Message + " Nothing was undone.");
            }
            Guard.SentFor(key);

            return Json.Ok(
                Json.Bool("sent", true),
                Json.Str("undoing", entry == null ? null : entry.Job),
                Json.Str("undoEntry", entry == null ? null : entry.Label),
                Json.Str("document", doc.Title),
                Json.Str("documentPath", string.IsNullOrEmpty(doc.PathName) ? null : doc.PathName),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Str("creationGuid", RevitOperations.CreationGuid(doc)),
                Json.Str("said", said));
        }
    }
}
