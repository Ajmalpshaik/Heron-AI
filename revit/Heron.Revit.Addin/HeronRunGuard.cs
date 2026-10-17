// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// The run guard: Revit's own events, listened to while a Heron job runs
    /// and ignored the rest of the time.
    ///
    /// THIN ON PURPOSE. Every decision - what is cancelled, what refuses the
    /// job, what the modeller is told - is HeronRunPolicy's, which names no
    /// Autodesk type and is proved without Revit by
    /// tests/Heron.RunGuard.TestHost. This file only turns Revit's events into
    /// that class's questions, and does what it answers.
    ///
    /// ARMED BY THE DISPATCHER AROUND ONE JOB, in a finally, so it is never
    /// left armed: a save the modeller presses between two requests is theirs,
    /// and nothing here may touch it. Everything is on Revit's own thread -
    /// the events, the job and the dispatcher's Execute - so the state below
    /// needs no lock, the same reasoning RevitFragment's chain rests on.
    ///
    /// THE DOCUMENTS HELD HERE LIVE FOR ONE JOB. The conventions forbid
    /// caching a Document across invocations; these are taken when the job is
    /// armed and dropped when it is disarmed, inside the one invocation.
    /// </summary>
    internal static class HeronRunGuard
    {
        private static HeronRunPolicy _policy;
        private static readonly List<Document> OpenAtStart = new List<Document>();
        private static readonly List<Document> CreatedByJob = new List<Document>();
        private static Document _target;

        /// <summary>
        /// ViewExporting is raised by the DB Application and NOT by the
        /// ControlledApplication OnStartup is handed - read out of every
        /// release's RevitAPI.dll with `tools/api-surface --members`, 2020 to
        /// 2027 alike. The first armed job has that Application, so the
        /// handler is added then, once, and this is kept to take it off again.
        /// </summary>
        private static Application _viewExports;

        private static Action<string> _log = delegate { };

        /// <summary>
        /// How many HeronExports scopes are open - see that method. A count
        /// rather than a flag so that two nested scopes cannot end each
        /// other's.
        /// </summary>
        private static int _heronExports;

        /// <summary>
        /// Revit's thread, from OnStartup: listen, once, for the life of the
        /// session. Every handler returns at once while no job is armed.
        /// </summary>
        internal static void Attach(UIControlledApplication application, Action<string> log)
        {
            _log = log ?? delegate { };

            var events = application.ControlledApplication;
            events.DocumentSaving += OnSaving;
            events.DocumentSavingAs += OnSavingAs;
            events.DocumentSynchronizingWithCentral += OnSynchronizing;
            events.FileExporting += OnFileExporting;
            events.DocumentPrinting += OnPrinting;
            events.DocumentCreated += OnCreated;
            events.DocumentOpened += OnOpened;
            events.DocumentChanged += OnChanged;
        }

        /// <summary>Revit's thread, from OnShutdown.</summary>
        internal static void Detach(UIControlledApplication application)
        {
            var events = application.ControlledApplication;
            events.DocumentSaving -= OnSaving;
            events.DocumentSavingAs -= OnSavingAs;
            events.DocumentSynchronizingWithCentral -= OnSynchronizing;
            events.FileExporting -= OnFileExporting;
            events.DocumentPrinting -= OnPrinting;
            events.DocumentCreated -= OnCreated;
            events.DocumentOpened -= OnOpened;
            events.DocumentChanged -= OnChanged;

            if (_viewExports != null)
            {
                _viewExports.ViewExporting -= OnViewExporting;
                _viewExports = null;
            }

            Forget();
        }

        /// <summary>
        /// Revit's thread, before the dispatcher runs one job.
        ///
        /// THE RISK IS THE OPERATION'S, from the tool registry by name. An
        /// operation the registry does not declare is refused by the gate
        /// before it is reached; it is armed at Read meanwhile, the strictest
        /// the policy knows.
        ///
        /// WHAT IS OPEN NOW IS WHAT THE JOB DID NOT OPEN, and it is the
        /// authority for every later question. DocumentOpened is listened for
        /// as well, and recorded, but a document opened in a way Revit may
        /// not report that way - EditFamily is the one nobody has watched yet
        /// - is still known to be the job's because it was not here before.
        /// That snapshot is the one thing here that may not fail quietly:
        /// without it every open model would look like the job's own, which
        /// is the guard failing OPEN. So it is not caught - the dispatcher
        /// refuses the job instead.
        /// </summary>
        internal static void Arm(UIApplication app, string operation)
        {
            Forget();

            var risk = HeronOperationRegistry.IsDeclared(operation)
                ? HeronOperationRegistry.RiskOf(operation)
                : HeronRisk.Read;

            ListenForViewExports(app);

            foreach (Document open in app.Application.Documents)
            {
                if (open != null) OpenAtStart.Add(open);
            }

            // The model in front, until the job says otherwise. A fragment
            // names its own target through Target(); the move path writes to
            // the active model, and so does every other operation that writes.
            try
            {
                var active = app.ActiveUIDocument;
                _target = active == null ? null : active.Document;
            }
            catch (Exception)
            {
                _target = null;
            }

            _policy = new HeronRunPolicy(operation, risk);
        }

        /// <summary>
        /// The document the job was sent to, once the job has chosen it. Its
        /// own changes are never "elsewhere" - the transaction group and the
        /// gate govern those.
        /// </summary>
        internal static void Target(Document document)
        {
            if (_policy != null && document != null) _target = document;
        }

        /// <summary>
        /// Heron's OWN export, about to happen - `using (HeronRunGuard.HeronExports())`
        /// round the one call, and nothing else inside it.
        ///
        /// capture_view is ANALYZE and exports a picture of the open model
        /// with ExportImage, so without this the guard would cancel it as it
        /// cancels any read that exports. But it is fixed C# Heron wrote, and
        /// the file goes to HeronPaths.Captures - Heron's own Derived folder -
        /// is read back and deleted before it answers: nothing reaches anybody
        /// else, which is what Article 10 is about. A fragment cannot name
        /// this scope: it is internal to the add-in, and a fragment compiles
        /// into an assembly of its own. (Code that reaches past the compiler
        /// with reflection can reach anything in-process; no guard in Revit's
        /// own process is a defence against that.) The same shape as
        /// HeronDialogWatch.HeronAsks.
        /// </summary>
        internal static IDisposable HeronExports()
        {
            _heronExports++;
            return new ExportScope();
        }

        private sealed class ExportScope : IDisposable
        {
            private bool _closed;

            public void Dispose()
            {
                if (_closed) return;
                _closed = true;
                _heronExports--;
            }
        }

        /// <summary>The armed job's record, or null when no job is armed.</summary>
        internal static HeronRunPolicy Policy
        {
            get { return _policy; }
        }

        /// <summary>
        /// Revit's thread, after the job, from the dispatcher's finally:
        /// close what the job left open in the background, stop listening, and
        /// hand back the record for the reply and the audit line.
        ///
        /// HERE AND NOT INSIDE THE JOB, so it runs after every path out of it
        /// - a fragment that threw with a family open from EditFamily included
        /// - and after any transaction group in the model it ran in has been
        /// kept or rolled back.
        /// </summary>
        internal static HeronRunPolicy Disarm(UIApplication app)
        {
            var policy = _policy;
            if (policy == null) return null;

            try
            {
                if (app != null) CloseLeftovers(app, policy);
            }
            catch (Exception failure)
            {
                _log("Run guard could not close what the job left open: " + failure.Message);
                policy.CouldNotClose("(what the job left open)", failure.Message);
            }
            finally
            {
                Forget();
            }

            return policy;
        }

        // ---------------------------------------------------------- the events

        private static void OnSaving(object sender, DocumentSavingEventArgs e)
        {
            Judge(HeronRunPolicy.Act.Save, e);
        }

        private static void OnSavingAs(object sender, DocumentSavingAsEventArgs e)
        {
            Judge(HeronRunPolicy.Act.SaveAs, e);
        }

        private static void OnSynchronizing(object sender, DocumentSynchronizingWithCentralEventArgs e)
        {
            Judge(HeronRunPolicy.Act.Sync, e);
        }

        private static void OnFileExporting(object sender, FileExportingEventArgs e)
        {
            Judge(HeronRunPolicy.Act.Export, e);
        }

        private static void OnViewExporting(object sender, ViewExportingEventArgs e)
        {
            Judge(HeronRunPolicy.Act.ExportView, e);
        }

        private static void OnPrinting(object sender, DocumentPrintingEventArgs e)
        {
            Judge(HeronRunPolicy.Act.Print, e);
        }

        /// <summary>
        /// Something is about to leave a model. Cancel it if the policy says
        /// so - and ONLY after asking Revit whether it can be: Cancel() on an
        /// event that is not cancellable throws.
        ///
        /// FAILS TOWARDS THE WARNING. An event that cannot be cancelled, or a
        /// cancel that throws, is recorded as NOT stopped, so the refusal says
        /// it may have happened. Saying "stopped" without Revit confirming it
        /// is the sentence this repository has had to take back before.
        /// </summary>
        private static void Judge(HeronRunPolicy.Act act, RevitAPIPreDocEventArgs e)
        {
            var policy = _policy;
            if (policy == null || e == null) return;

            Document document = null;
            try { document = e.Document; }
            catch (Exception) { document = null; }

            if (!policy.Cancels(act, OriginOf(document), _heronExports > 0)) return;

            var cancelled = false;
            try
            {
                if (e.Cancellable)
                {
                    e.Cancel();
                    cancelled = e.IsCancelled();
                }
            }
            catch (Exception failure)
            {
                _log("Run guard could not cancel " + act + ": " + failure.Message);
                cancelled = false;
            }

            var title = Title(document);
            policy.Stopped(act, title, cancelled);
            _log("Run guard " + (cancelled ? "cancelled" : "could NOT cancel") + " " + act
                 + " of '" + title + "' during " + policy.Operation + ".");
        }

        private static void OnCreated(object sender, DocumentCreatedEventArgs e)
        {
            var policy = _policy;
            if (policy == null || e == null) return;

            try
            {
                if (e.Status != RevitAPIEventStatus.Succeeded) return;
                var document = e.Document;
                if (document == null) return;

                CreatedByJob.Add(document);
                if (policy.Created(Title(document)))
                {
                    _log("Run guard refused a new document, '" + Title(document) + "', during "
                         + policy.Operation + ".");
                }
            }
            catch (Exception failure)
            {
                _log("Run guard could not read a new document: " + failure.Message);
            }
        }

        private static void OnOpened(object sender, DocumentOpenedEventArgs e)
        {
            var policy = _policy;
            if (policy == null || e == null) return;

            try
            {
                if (e.Status != RevitAPIEventStatus.Succeeded) return;
                var title = Title(e.Document);
                policy.Opened(title);
                _log("Run guard saw '" + title + "' open during " + policy.Operation + ".");
            }
            catch (Exception failure)
            {
                _log("Run guard could not read an opened document: " + failure.Message);
            }
        }

        /// <summary>
        /// A transaction committed, undone or redone somewhere. Raised AFTER
        /// the fact, so nothing here can stop it: a change to another open
        /// model is recorded, and the job that made it is refused and rolled
        /// back in the model it ran in.
        ///
        /// A ROLLBACK IS NOT A CHANGE - nothing was kept - so it is passed
        /// over; that includes Heron's own group rolling back.
        ///
        /// IT FAILS QUIETLY, unlike Judge, and on purpose: an event it cannot
        /// read can be recorded only as a change to a model nobody can name,
        /// and a refusal telling a modeller to go and look at "another model"
        /// that may never have been touched is a false alarm with an
        /// instruction in it. The log keeps the failure.
        /// </summary>
        private static void OnChanged(object sender, DocumentChangedEventArgs e)
        {
            var policy = _policy;
            if (policy == null || e == null) return;

            try
            {
                var operation = e.Operation;
                if (operation == UndoOperation.TransactionRolledBack
                    || operation == UndoOperation.TransactionGroupRolledBack)
                {
                    return;
                }

                var document = e.GetDocument();
                if (!policy.IsElsewhere(OriginOf(document), IsTarget(document))) return;

                var title = Title(document);
                policy.ChangedElsewhere(title, e.GetTransactionNames());
                _log("Run guard saw '" + title + "' change during " + policy.Operation
                     + " - another open model, which no rollback of Heron's reaches.");
            }
            catch (Exception failure)
            {
                _log("Run guard could not read a change: " + failure.Message);
            }
        }

        // ------------------------------------------------------------- helpers

        private static void ListenForViewExports(UIApplication app)
        {
            if (_viewExports != null) return;

            // A run guard that cannot hear view exports still hears FileExporting,
            // which every Document.Export raises. Refusing every job over the
            // narrower of the two would be the guard costing Heron everything,
            // so it is logged and the rest stands.
            try
            {
                var application = app.Application;
                application.ViewExporting += OnViewExporting;
                _viewExports = application;
            }
            catch (Exception failure)
            {
                _log("Run guard cannot listen for view exports: " + failure.Message);
            }
        }

        /// <summary>
        /// Every background document the job left open, closed WITHOUT
        /// saving - one it opened or started itself, never one that was open
        /// when it began.
        ///
        /// KEPT: a linked model, which belongs to its host; and anything a
        /// window shows - OpenAndActivateDocument opens one on purpose, and
        /// closing what a person may be looking at is the one mistake here
        /// that loses somebody's work. A document whose windows cannot be
        /// counted is kept for the same reason.
        ///
        /// Close(false) discards only what the job did to a file it opened;
        /// the file on disk is as it was. That is save-document's rule -
        /// "nothing unsaved is ever closed" - kept, because nothing of the
        /// modeller's is in a document they never had open.
        /// </summary>
        private static void CloseLeftovers(UIApplication app, HeronRunPolicy policy)
        {
            var leftovers = new List<Document>();
            foreach (Document open in app.Application.Documents)
            {
                if (open == null || Contains(OpenAtStart, open)) continue;

                bool linked;
                try { linked = open.IsLinked; }
                catch (Exception) { linked = true; }
                if (linked || ShownInAWindow(open)) continue;

                leftovers.Add(open);
            }

            // Collected first, closed after: closing one changes the very
            // collection the loop above walks.
            foreach (var document in leftovers)
            {
                var title = Title(document);
                try
                {
                    if (document.Close(false))
                    {
                        policy.Closed(title);
                        _log("Run guard closed '" + title + "' unsaved - " + policy.Operation
                             + " left it open in the background, and no window showed it.");
                    }
                    else
                    {
                        policy.CouldNotClose(title, "Revit declined to close it");
                        _log("Run guard could not close '" + title + "': Revit declined.");
                    }
                }
                catch (Exception failure)
                {
                    policy.CouldNotClose(title, failure.Message);
                    _log("Run guard could not close '" + title + "': " + failure.Message);
                }
            }
        }

        private static bool ShownInAWindow(Document document)
        {
            try
            {
                var views = new UIDocument(document).GetOpenUIViews();
                return views != null && views.Count > 0;
            }
            catch (Exception)
            {
                return true;
            }
        }

        /// <summary>
        /// Where a document came from. One Heron cannot place - no document
        /// on the event at all - is the protected kind, never the job's own.
        /// </summary>
        private static HeronRunPolicy.Origin OriginOf(Document document)
        {
            if (document == null || Contains(OpenAtStart, document))
                return HeronRunPolicy.Origin.OpenAtStart;
            if (Contains(CreatedByJob, document))
                return HeronRunPolicy.Origin.CreatedByJob;
            return HeronRunPolicy.Origin.OpenedByJob;
        }

        private static bool IsTarget(Document document)
        {
            return _target != null && Same(_target, document);
        }

        private static bool Contains(List<Document> documents, Document document)
        {
            foreach (var one in documents)
            {
                if (Same(one, document)) return true;
            }
            return false;
        }

        /// <summary>
        /// Document.Equals - declared on Document itself in every release, so
        /// two handles on one model compare equal. Guarded, because a document
        /// the job has since closed is no longer a valid object to ask.
        /// </summary>
        private static bool Same(Document one, Document other)
        {
            if (one == null || other == null) return false;
            try { return one.Equals(other); }
            catch (Exception) { return false; }
        }

        private static string Title(Document document)
        {
            if (document == null) return null;
            try { return document.Title; }
            catch (Exception) { return null; }
        }

        private static void Forget()
        {
            _policy = null;
            _target = null;
            OpenAtStart.Clear();
            CreatedByJob.Clear();
        }
    }
}
