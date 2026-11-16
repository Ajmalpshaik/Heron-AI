// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Heron.Bridge;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// `undo_own` (D-157, Q-88): Revit's own Undo, sent only while Heron saw
    /// its last kept change land, has seen nothing change the model since,
    /// and THIS CHAT kept it - and `undo_own_check`, the same questions asked
    /// with nothing sent.
    ///
    /// ONLY THE CHAT THAT MADE A CHANGE MAY UNDO IT (Q-88, decided
    /// 2026-11-15). The guard records the chat whose request kept each change
    /// - the request's conversation, one per chat's own server process
    /// (D-141) - and the request that posts is the request that is compared,
    /// in this one call: no check-then-post, so no window between the two.
    /// Ctrl+Z in Revit is the modeller's and is never refused.
    ///
    /// AND SO IS THE ACTIVITY PANE'S UNDO BUTTON (row 5b-1741, the owner's
    /// answer of 2026-11-16): `undo_from_pane`, FromPane, the modeller's own
    /// click in Revit - any chat's change, by D-157's rule, for the change the
    /// button sits beside. It is answered only for a job the pane's in-process
    /// door queued (RevitDispatcher.DispatchFromPane); off the pipe it is
    /// refused (NotFromPane), so no chat can use it round Q-88. Both doors post
    /// through the one Post.
    ///
    /// THE OPERATION WAS `undo_last` IN THE FIRST BUILD, THEN `undo_guarded`
    /// (row 5b-1604, the second review), AND IS `undo_own` SINCE 2026-11-16.
    /// Each time what the request means changed, so the name did: an add-in
    /// built before the change knows only an older name, so it answers that
    /// it has no such operation and nothing is posted by its older rule -
    /// which would send Undo for whichever chat's change is on top. Nothing
    /// answers `undo_last` - the pane's button sent it, unanswered, until row
    /// 5b-1741 gave the pane a name of its own; the first build's Revit still
    /// knows `undo_last`, so it stays unanswered here and never means the
    /// pane's rule. `undo_guarded` and `undo_check` are answered, by a
    /// refusal with nothing posted (OlderChat): only a chat started before
    /// this change still sends them, and it cannot say whose change it asks for.
    ///
    /// REVIT'S UNDO, NEVER A SECOND CHANGE. Heron does not work out how to put
    /// the model back and write that - a second change with risks of its own,
    /// and two entries in the undo list. It posts Revit's Undo command, which
    /// is exactly what pressing Ctrl+Z does, and only when HeronUndoGuard
    /// allows it. Otherwise it refuses and says why, and nothing is sent.
    ///
    /// THE UNDO RUNS AFTER THIS ANSWER, NOT IN IT. PostCommand queues the
    /// command; Revit runs it once this external event returns. So the reply
    /// says it was SENT, never that it was done. The guard hears the undo when
    /// Revit reports it; until then a second request is refused, the
    /// dispatcher stops draining its queue (TakePosted), every other Heron
    /// job - in any model - is held back for a short wait (Waiting), and no
    /// keep in that model is vouched for however long it takes: a change
    /// kept in between would be what the Undo takes (row 5b-1605).
    ///
    /// ONLY THE MODEL THIS CHAT IS PINNED TO, AND ONLY WHILE IT IS ON SCREEN
    /// (row 5b-1604). Revit's Undo acts on the active document. So the
    /// request names the pinned model exactly as a kept change does - its
    /// path, else its title - it is found among the open models exactly as
    /// RevitFragment.Run finds it (RevitOperations.HoldTarget), and it must BE
    /// the active document, checked before anything is posted. The Project
    /// Information id alone decided this once: every project started from one
    /// template carries the same one, and a family has none. No transaction
    /// is opened here: there is nothing to change, only a command to send.
    /// </summary>
    internal static class RevitUndo
    {
        /// <summary>
        /// What Heron has seen since its last kept change in each model. Held
        /// HERE, in a class that names no compiler, so HeronApplication can
        /// tell it of a sync, a reload, a save or a close without loading
        /// RevitFragment - and Roslyn with it - outside its guard.
        /// RevitFragment feeds it from the keep and its permanent listener.
        /// Every transition goes to the add-in's log.
        /// </summary>
        internal static readonly HeronUndoGuard Guard = new HeronUndoGuard(HeronApplication.Log);

        // Whether the request now ending posted Undo - read once by the
        // dispatcher, on the same thread, straight after it.
        private static bool _posted;

        // A model's key as its save began, so a save that moves the key (a
        // first save, Save As) can move what is remembered with it. A string,
        // never the Document.
        private static string _keyBeforeSave;

        // Reload Latest's event arrived at Revit 2021, so it is found by name:
        // one build serves 2020 too, where there is none to hear.
        private static Delegate _reloadHandler;

        /// <summary>Synchronized with central: Revit cleared the model's undo list.</summary>
        internal static void Synced(Document doc)
        {
            try { Guard.SyncedIn(RevitOperations.DocumentKey(doc)); }
            catch (Exception ex) { Missed(null, "a sync", ex); }
        }

        /// <summary>The model is closing: nothing about its undo list is kept.</summary>
        internal static void Forget(Document doc)
        {
            try { Guard.Closed(RevitOperations.DocumentKey(doc)); }
            catch (Exception ex) { Missed(null, "a model closing", ex); }
        }

        /// <summary>
        /// Heron missed something in this model - or, with none given, cannot
        /// tell in which - and must stop vouching for what is on top there.
        /// </summary>
        internal static void Doubt(Document doc, string why)
        {
            string key = null;
            try { key = doc == null ? null : RevitOperations.DocumentKey(doc); }
            catch (Exception) { key = null; }
            try { Guard.Doubt(key, why); }
            catch (Exception ex) { HeronApplication.Log("Undo guard could not record a doubt: " + ex.Message); }
        }

        /// <summary>
        /// The guard could not be told of something. It fails closed: every
        /// model it remembers - or the one named - refuses from here on.
        /// </summary>
        private static void Missed(string key, string what, Exception ex)
        {
            HeronApplication.Log("Undo guard missed " + what + ": " + ex.Message);
            try { Guard.Doubt(key, "Heron could not record " + what + " that Revit reported"); }
            catch (Exception) { }
        }

        /// <summary>
        /// The events only RevitUndo needs: Reload Latest, which clears the
        /// undo list as a sync does, and a save, which can move a model's key.
        /// </summary>
        internal static void Listen(ControlledApplication application)
        {
            application.DocumentSaving += OnSaving;
            application.DocumentSavingAs += OnSavingAs;
            application.DocumentSaved += OnSaved;
            application.DocumentSavedAs += OnSavedAs;

            // BY NAME, NOT BY MEMBER: DocumentReloadedLatest is in Revit 2021
            // and later and not in 2020 (read from the shipped assemblies).
            // A direct reference compiles for 2024 and is missing on 2020 -
            // exactly what tools/check-api-surface.py fails. On 2020 nothing
            // is subscribed and Reload Latest goes unheard (Group NU).
            try
            {
                var reloaded = typeof(ControlledApplication).GetEvent(ReloadEvent);
                if (reloaded == null)
                {
                    HeronApplication.Log("Undo guard: this Revit has no Reload Latest event, "
                        + "so a reload is not heard.");
                    return;
                }
                _reloadHandler = Delegate.CreateDelegate(reloaded.EventHandlerType,
                    typeof(RevitUndo).GetMethod("OnReloaded",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
                reloaded.AddEventHandler(application, _reloadHandler);
            }
            catch (Exception ex)
            {
                _reloadHandler = null;
                HeronApplication.Log("Undo guard could not listen for Reload Latest: " + ex.Message);
            }
        }

        private const string ReloadEvent = "DocumentReloadedLatest";

        internal static void StopListening(ControlledApplication application)
        {
            application.DocumentSaving -= OnSaving;
            application.DocumentSavingAs -= OnSavingAs;
            application.DocumentSaved -= OnSaved;
            application.DocumentSavedAs -= OnSavedAs;
            try
            {
                var reloaded = typeof(ControlledApplication).GetEvent(ReloadEvent);
                if (reloaded != null && _reloadHandler != null)
                    reloaded.RemoveEventHandler(application, _reloadHandler);
            }
            catch (Exception ex)
            {
                HeronApplication.Log("Undo guard could not stop listening for Reload Latest: " + ex.Message);
            }
            _reloadHandler = null;
        }

        /// <summary>
        /// Reload Latest finished in a model. Its arguments are read by name
        /// for the reason the event is: the type does not exist on 2020.
        /// With no model readable from them, every model refuses.
        /// </summary>
        private static void OnReloaded(object sender, EventArgs e)
        {
            try
            {
                var property = e == null ? null : e.GetType().GetProperty("Document");
                var doc = property == null ? null : property.GetValue(e, null) as Document;
                if (doc == null)
                    Guard.Doubt(null, "a model was reloaded from central and Heron could not tell which");
                else
                    Guard.ReloadedIn(RevitOperations.DocumentKey(doc));
            }
            catch (Exception ex) { Missed(null, "a Reload Latest", ex); }
        }

        private static void OnSaving(object sender, DocumentSavingEventArgs e)
        {
            Saving(e == null ? null : e.Document);
        }

        private static void OnSavingAs(object sender, DocumentSavingAsEventArgs e)
        {
            Saving(e == null ? null : e.Document);
        }

        private static void OnSaved(object sender, DocumentSavedEventArgs e)
        {
            Saved(e == null ? null : e.Document);
        }

        private static void OnSavedAs(object sender, DocumentSavedAsEventArgs e)
        {
            Saved(e == null ? null : e.Document);
        }

        private static void Saving(Document doc)
        {
            try { _keyBeforeSave = Guard.Any && doc != null ? RevitOperations.DocumentKey(doc) : null; }
            catch (Exception ex) { _keyBeforeSave = null; Missed(null, "a save starting", ex); }
        }

        /// <summary>
        /// A save finished. An ordinary save leaves the key - and, as far as
        /// Heron knows, the undo list - as it was. A first save or a Save As
        /// gives the model a new path and title, so its key moves.
        /// </summary>
        private static void Saved(Document doc)
        {
            var before = _keyBeforeSave;
            _keyBeforeSave = null;
            if (before == null || doc == null) return;
            try
            {
                var after = RevitOperations.DocumentKey(doc);
                if (after != before) Guard.Renamed(before, after);
            }
            catch (Exception ex) { Missed(before, "a save", ex); }
        }

        /// <summary>
        /// Whether the request that just ended posted Undo - asked once by the
        /// dispatcher, which then stops draining its queue so Revit can run
        /// the Undo before any job queued behind it.
        /// </summary>
        internal static bool TakePosted()
        {
            var posted = _posted;
            _posted = false;
            return posted;
        }

        /// <summary>
        /// The refusal for a job that would act in Revit while an Undo Heron
        /// sent a moment ago has not been seen to run - or null. A read goes
        /// on: it leaves nothing for the Undo to take.
        ///
        /// IN ANY MODEL, NOT ONLY THE ONE THE UNDO WAS SENT IN. Revit's Undo
        /// acts on the model on screen when it runs, and a job from another
        /// chat that brings another model to the front (ACTIVATE_DOCUMENT is
        /// EXECUTE) would send it there. A second Undo is held the same way.
        /// </summary>
        internal static string Waiting(UIApplication app, string request, string op)
        {
            try
            {
                if (!Guard.Any) return null;
                if (!HeronOperationRegistry.IsDeclared(op)
                    || HeronOperationRegistry.RiskOf(op) < HeronRisk.Execute) return null;

                var why = Guard.Pending(DateTime.Now);
                return why == null ? null : Json.Error("undo_pending", why);
            }
            catch (Exception ex)
            {
                HeronApplication.Log("Undo guard could not say whether an Undo is waiting: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// What the guard holds for a model straight after a keep there, for
        /// the kept reply's `undoState` field - or null when it holds nothing.
        /// </summary>
        internal static string StateOf(Document doc)
        {
            try
            {
                var entry = Guard.Of(RevitOperations.DocumentKey(doc));
                return entry == null ? null : entry.State;
            }
            catch (Exception) { return null; }
        }

        /// <summary>
        /// The guard's own reason when it is unsure of a model straight after
        /// a keep there, for the kept reply's `undoWhy` - or null.
        /// </summary>
        internal static string WhyOf(Document doc)
        {
            try
            {
                var entry = Guard.Of(RevitOperations.DocumentKey(doc));
                return entry == null || entry.State != HeronUndoGuard.Unsure ? null : entry.Why;
            }
            catch (Exception) { return null; }
        }

        /// <summary>
        /// Why the model on screen is not shown to be the one this chat is
        /// pinned to - or null when it is. The identity is the one a kept
        /// change is aimed by: `documentPath`, else `document`.
        /// </summary>
        private static string NotThePinned(UIApplication app, string request, Document doc)
        {
            var title = "";
            try { title = doc.Title ?? ""; } catch (Exception) { title = ""; }
            const string NothingSent = " NOTHING was sent to Revit, and nothing was undone.";

            var wantedPath = Json.ReadString(request, "documentPath");
            var wanted = Json.ReadString(request, "document");
            if (string.IsNullOrEmpty(wantedPath) && string.IsNullOrEmpty(wanted))
            {
                return "This request does not say which model this chat works on, so Heron cannot "
                     + "tell that \"" + title + "\", the model on screen, is the one its change "
                     + "was kept in." + NothingSent + " Ctrl+Z in Revit undoes the last change, "
                     + "whoever made it.";
            }

            Document pinned;
            try { pinned = RevitOperations.HoldTarget(app, request); }
            catch (Exception) { pinned = null; }
            if (pinned == null)
            {
                return "The model this chat works on"
                     + (string.IsNullOrEmpty(wanted) ? "" : ", \"" + wanted + "\",")
                     + " is not open in this Revit - or two open models share its name - so "
                     + "Heron cannot tell that \"" + title + "\", the model on screen, is it."
                     + NothingSent + " Open it, bring it to the front and ask again.";
            }

            if (!pinned.Equals(doc))
            {
                var pinnedTitle = "";
                try { pinnedTitle = pinned.Title ?? ""; } catch (Exception) { pinnedTitle = ""; }
                return "This chat works on \"" + pinnedTitle + "\", and the model on screen in "
                     + "Revit now is "
                     + (string.Equals(pinnedTitle, title, StringComparison.OrdinalIgnoreCase)
                            ? "a different model with the same name"
                            : "\"" + title + "\"")
                     + ". Revit's Undo acts on the model on screen." + NothingSent
                     + " Bring the chat's model to the front and ask again.";
            }

            // A DIFFERENT SHARED ID STILL PROVES A DIFFERENT MODEL; the same
            // one proves nothing, which is why it is asked last and never alone.
            var expectProject = Json.ReadString(request, "expectProject");
            if (!string.IsNullOrEmpty(expectProject)
                && RevitOperations.ProjectKey(doc) != expectProject)
            {
                return "This chat has been working on another model, and the one on screen in "
                     + "Revit now is \"" + title + "\"." + NothingSent
                     + " Bring the chat's model to the front and ask again.";
            }
            return null;
        }

        /// <summary>
        /// `undo_own`, or with `checkOnly` its questions and nothing sent.
        /// `gate` is what the switches would answer an `undo_own` now -
        /// asked by the caller, for a check only: the real one never reaches
        /// here while they refuse it. A CHECK CHANGES NOTHING: nothing on its
        /// path writes to the guard, so asking twice answers twice the same.
        /// </summary>
        public static string Last(UIApplication app, string request, bool checkOnly, string gate)
        {
            var front = app == null ? null : app.ActiveUIDocument;
            var doc = front == null ? null : front.Document;

            string code = null;
            string refusal = null;
            if (checkOnly && gate != null)
            {
                code = Json.ReadString(gate, "error") ?? "write_disabled";
                refusal = Json.ReadString(gate, "message") ?? "Changes are off.";
            }
            else if (doc == null)
            {
                code = "no_document";
                refusal = "No model is open in Revit, so there is nothing to undo. Nothing was undone.";
            }
            else
            {
                // THE PINNED MODEL, BEFORE THE GUARD AND BEFORE ANYTHING IS POSTED.
                refusal = NotThePinned(app, request, doc);
                if (refusal != null) code = "wrong_document";
            }

            string key = null;
            RevitCommandId undo = null;
            if (refusal == null)
            {
                key = RevitOperations.DocumentKey(doc);
                undo = RevitCommandId.LookupPostableCommandId(PostableCommand.Undo);
                bool canPost;
                try { canPost = undo != null && app.CanPostCommand(undo); }
                catch (Exception) { canPost = false; }

                refusal = Guard.Refusal(key, doc.Title, true, canPost);

                // Q-88: AND ONLY THE CHAT THAT KEPT IT. Asked once everything
                // above said yes, in this same request - the one that posts -
                // so it can only refuse more. The chat is the request's own
                // conversation, read as RevitFragment.Run reads it for the
                // keep: a request that names none is a chat of one request,
                // which kept nothing.
                if (refusal == null)
                    refusal = Guard.ChatRefusal(key, doc.Title, HeronConversation.ChatOf(
                        Json.ReadString(request, HeronConversation.EnvelopeField)));
                if (refusal != null) code = "undo_refused";
            }

            if (checkOnly) return Checked(doc, key, refusal);
            if (refusal != null) return Json.Error(code, refusal);

            return Post(app, doc, key, undo);
        }

        /// <summary>
        /// `undo_from_pane` - THE ACTIVITY PANE'S UNDO BUTTON (row 5b-1741),
        /// reached only through the pane's own in-process door: RevitOperations
        /// answers it for no job the pipe queued.
        ///
        /// THE MODELLER'S OWN CLICK, LIKE CTRL+Z - the owner's answer of
        /// 2026-11-16: it undoes Heron's last change in the model on screen,
        /// whichever chat made it. So NO CHAT IS ASKED: not Q-88's ChatRefusal,
        /// which stays a chat's rule, and not the model's hold (D-141), which
        /// keeps two chats apart. Everything else is D-157's, asked exactly as a
        /// chat's Undo asks it: Revit's own Undo, posted only while Heron saw its
        /// last kept change land and has seen nothing change the model since.
        ///
        /// AND ONLY THE CHANGE THE BUTTON SITS BESIDE. The request names the
        /// row's model and the run it was kept in: the model on screen must be
        /// that model - Revit's Undo acts on the one on screen - and Heron's last
        /// change there must be that run (HeronUndoGuard.PaneRefusal), or the
        /// Undo would take a change the modeller did not press. Every refusal
        /// posts nothing and says so.
        /// </summary>
        public static string FromPane(UIApplication app, string request)
        {
            var front = app == null ? null : app.ActiveUIDocument;
            var doc = front == null ? null : front.Document;
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit, so there is nothing to undo. Nothing was sent to Revit "
                    + "and nothing was undone.");
            }

            // THE ROW'S MODEL, ON SCREEN - by its title, as the run record names
            // it and as Select and Zoom compare it (RevitShowElements). Two open
            // models sharing a title are told apart below, by the run.
            var document = Json.ReadString(request, "document");
            if (string.IsNullOrEmpty(document)
                || !string.Equals(doc.Title, document, StringComparison.Ordinal))
            {
                return Json.Error("wrong_document",
                    (string.IsNullOrEmpty(document)
                        ? "This change's record does not say which model it was made in"
                        : "This change was made in \"" + document + "\"")
                    + ", and the model on screen in Revit is \"" + doc.Title + "\". Revit's Undo acts "
                    + "on the model on screen. Nothing was sent to Revit and nothing was undone."
                    + (string.IsNullOrEmpty(document) ? ""
                        : " Bring \"" + document + "\" to the front and press Undo again."));
            }

            var key = RevitOperations.DocumentKey(doc);
            var undo = RevitCommandId.LookupPostableCommandId(PostableCommand.Undo);
            bool canPost;
            try { canPost = undo != null && app.CanPostCommand(undo); }
            catch (Exception) { canPost = false; }

            // D-157, AS FOR A CHAT; THEN THE PANE'S OWN QUESTION, which can only
            // refuse more. No chat's.
            var refusal = Guard.Refusal(key, doc.Title, true, canPost);
            if (refusal == null)
                refusal = Guard.PaneRefusal(key, doc.Title, Json.ReadString(request, "run"),
                                            Json.ReadString(request, "fragment"));
            if (refusal != null) return Json.Error("undo_refused", refusal);

            return Post(app, doc, key, undo);
        }

        /// <summary>
        /// `undo_from_pane` asked for by a job the pipe queued - a chat's, an
        /// older client's, or one written by hand. Refused, NOTHING POSTED and
        /// the guard not asked: only the pane's own button sends it, and a chat
        /// that could would undo another chat's change round Q-88.
        /// </summary>
        internal static string NotFromPane()
        {
            return Json.Error("pane_only",
                "Only the Undo button on Heron's Activity pane in Revit sends this - it is the "
                + "modeller's own click there. Nothing was sent to Revit and nothing was undone. A "
                + "chat undoes only a change it made itself, and Ctrl+Z in Revit undoes the last "
                + "change, whoever made it.");
        }

        /// <summary>
        /// THE ONE POST, for every door that may send Undo - a chat's `undo_own`
        /// and the pane's `undo_from_pane` - each having asked every question
        /// it must before it calls this. Revit's Undo command, posted; the
        /// guard told it was sent; the dispatcher told to stop draining.
        /// </summary>
        private static string Post(UIApplication app, Document doc, string key, RevitCommandId undo)
        {
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
            Guard.SentFor(key, DateTime.Now);
            _posted = true;

            return Json.Ok(
                Json.Bool("sent", true),
                Json.Str("did", "undo_sent"),
                Json.Str("undoing", entry == null ? null : entry.Job),
                Json.Str("undoEntry", entry == null ? null : entry.Label),
                Json.Str("document", doc.Title),
                Json.Str("documentPath", string.IsNullOrEmpty(doc.PathName) ? null : doc.PathName),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Str("creationGuid", RevitOperations.CreationGuid(doc)),
                Json.Str("said", said));
        }

        /// <summary>
        /// A check's answer. Always `ok`: the check ran, and "no" with the
        /// exact refusal is its answer. Nothing is posted on this path.
        /// </summary>
        private static string Checked(Document doc, string key, string refusal)
        {
            var entry = refusal == null ? Guard.Of(key) : null;
            return Json.Ok(
                Json.Bool("checked", true),
                Json.Bool("sent", false),
                Json.Str("did", "undo_checked"),
                Json.Bool("wouldSend", refusal == null && entry != null),
                Json.Str("undoing", entry == null ? null : entry.Job),
                Json.Str("undoEntry", entry == null ? null : entry.Label),
                Json.Str("document", doc == null ? null : doc.Title),
                Json.Str("documentPath",
                         doc == null || string.IsNullOrEmpty(doc.PathName) ? null : doc.PathName),
                Json.Str("projectKey", doc == null ? null : RevitOperations.ProjectKey(doc)),
                Json.Str("creationGuid", doc == null ? null : RevitOperations.CreationGuid(doc)),
                Json.Str("said", Guard.CheckSentence(key, refusal)));
        }

        /// <summary>
        /// `undo_guarded` and `undo_check`, the names a chat's Heron sent
        /// before Q-88: refused, and NOTHING POSTED, whatever the guard holds.
        /// Such a chat asks for "Heron's last change in the model", not for
        /// its own, so it cannot be answered by the rule that now holds - and
        /// answering it by the old one would undo another chat's change. A
        /// chat keeps the Python it started with (mcp/README rule 5), so this
        /// is a chat opened before Heron was updated, talking to the updated
        /// add-in. `undo_refused`, which that Heron already reads as a
        /// refusal and says in these words.
        /// </summary>
        internal static string OlderChat(bool checkOnly)
        {
            return Json.Error("undo_refused",
                "This chat runs an older Heron than the add-in in this Revit, and it does not say "
                + "which chat made the change it asks to undo - only the chat that made a change "
                + "may undo it."
                + (checkOnly ? " This was only a check: nothing was sent to Revit."
                             : " Nothing was sent to Revit and nothing was undone.")
                + " Ctrl+Z in Revit still undoes the last change, whoever made it. A chat "
                + "opened after Heron was updated can undo the changes it makes itself.");
        }
    }
}
