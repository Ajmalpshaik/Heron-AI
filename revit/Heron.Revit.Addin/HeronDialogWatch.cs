// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Answers a Revit dialog that opens while a Heron job is running, so a
    /// job never hangs behind a popup nobody is there to click - and records
    /// it, so the chat is told what Revit asked.
    ///
    /// FRAGMENT-ISSUES section 5b row 300: LOAD_FAMILY with a family saved in
    /// a later release opened Revit's modal "saved in a later version" dialog
    /// inside the job, and the chat read unknown_outcome, revit_busy and
    /// still_running for over 90 seconds. Revit raises DialogBoxShowing before
    /// it shows a dialog, and a handler that calls OverrideResult closes it
    /// with that answer instead of showing it.
    ///
    /// ARMED ONLY AROUND A JOB. RevitDispatcher arms this around
    /// RevitOperations.Run and disarms it after, so a dialog the modeller
    /// meets in their own work is never touched. And a dialog already open
    /// BEFORE a job is never seen here at all: Revit does not run the job's
    /// ExternalEvent while a modal dialog is up, so nothing is armed and the
    /// caller still gets revit_busy - which is the right answer, because that
    /// dialog is the modeller's question, not the job's.
    ///
    /// WHAT IT ANSWERS COMES FROM HeronDialogRule, and the default is to
    /// decline - Cancel, or Close - and fail the run, so the job's group is
    /// rolled back (Article 7). Every write path checks Refused before it
    /// keeps anything: RevitFragment and RevitWrite.
    ///
    /// THE API'S OWN ANSWER COMES FIRST, wherever Revit offers one. A family
    /// already loaded is answered by IFamilyLoadOptions (RevitFragment's
    /// FamilyReload, D-114) and never reaches a dialog; duplicate type names
    /// in a copy are CopyPasteOptions' handler to answer, and a fragment that
    /// sets none meets "Duplicate Types" here and is cancelled. Revit's
    /// FAILURES are not dialogs to this class: FailureDiscipline answers them
    /// on every transaction, so none is left for Revit to show.
    ///
    /// HERON NEVER ANSWERS ITS OWN QUESTION. A prompt Heron puts up for the
    /// modeller during a job - the Keep or Discard question, RevitFragment's
    /// AskTheModeller (D-119, D-126) - must be shown inside HeronAsks(), which stands this down until the
    /// prompt has its answer. Whether Revit raises this event for an
    /// add-in's own TaskDialog at all is not known yet (NEEDS-CHECKING EL3);
    /// HeronAsks makes the answer not matter.
    ///
    /// ONE DIALOG MAY BE THE CALLER'S TO ANSWER, FOR ONE JOB (D-145). A card
    /// whose request value carries `dialog:` hands the caller's word to
    /// AnswerForJob while the job binds its inputs; HeronDialogRule.CallerAnswer
    /// says which dialogs a caller may answer at all, and with which words.
    /// That answer is given to that DialogId alone, for that job alone - Arm
    /// and Disarm both clear it - and every other dialog is still declined.
    /// The measured case is a level's name: FRAGMENT-ISSUES 5b-1221.
    ///
    /// REVIT'S THREAD ONLY, and so no lock. DialogBoxShowing is raised on
    /// Revit's main thread, the dispatcher arms and disarms on it inside
    /// Execute, and a write path asks Refused on it mid-job.
    /// </summary>
    internal static class HeronDialogWatch
    {
        private static HeronDialogNote _armed;
        private static Dictionary<string, int> _callerAnswers;
        private static int _heronAsking;
        private static Action<string> _log = delegate { };

        /// <summary>
        /// Revit's thread, during OnStartup: listen for dialogs. Inert until
        /// a job arms it.
        /// </summary>
        internal static void Attach(UIControlledApplication application, Action<string> log)
        {
            _log = log ?? delegate { };
            application.DialogBoxShowing += OnDialogBoxShowing;
        }

        internal static void Detach(UIControlledApplication application)
        {
            application.DialogBoxShowing -= OnDialogBoxShowing;
            _armed = null;
            _callerAnswers = null;
        }

        /// <summary>A job is about to run: answer what it meets.</summary>
        internal static void Arm()
        {
            _armed = new HeronDialogNote();
            _callerAnswers = null;
        }

        /// <summary>
        /// The job is over: stop answering, and hand back what it met. Never
        /// null, so the caller can read it without asking.
        /// </summary>
        internal static HeronDialogNote Disarm()
        {
            var met = _armed;
            _armed = null;
            _callerAnswers = null;
            return met ?? new HeronDialogNote();
        }

        /// <summary>
        /// The caller's answer to one dialog, for the job now running (D-145).
        /// Only a code HeronDialogRule.CallerAnswer gave, and only while a job
        /// is armed: false, and nothing kept, otherwise - so an answer can
        /// never outlive the job it was given for.
        /// </summary>
        internal static bool AnswerForJob(string dialogId, int code)
        {
            if (_armed == null || string.IsNullOrEmpty(dialogId) || code == 0) return false;
            if (_callerAnswers == null) _callerAnswers = new Dictionary<string, int>(StringComparer.Ordinal);
            _callerAnswers[dialogId] = code;
            return true;
        }

        /// <summary>What the running job has met so far; null when no job is armed.</summary>
        internal static HeronDialogNote Armed
        {
            get { return _armed; }
        }

        /// <summary>
        /// Whether the running job met a dialog that stops it. A write path
        /// asks this before it keeps anything, and rolls back on true.
        /// </summary>
        internal static bool Refused
        {
            get
            {
                var met = _armed;
                return met != null && met.Refused;
            }
        }

        /// <summary>
        /// Stands the watch down while Heron itself asks the modeller
        /// something. Dispose it once the prompt has its answer:
        ///
        ///     using (HeronDialogWatch.HeronAsks()) { answer = ask.Show(); }
        /// </summary>
        internal static IDisposable HeronAsks()
        {
            _heronAsking++;
            return new Asking();
        }

        private sealed class Asking : IDisposable
        {
            private bool _done;

            public void Dispose()
            {
                if (_done) return;
                _done = true;
                if (_heronAsking > 0) _heronAsking--;
            }
        }

        /// <summary>
        /// Revit is about to show a dialog. Answered only while a job is armed
        /// and Heron is not asking a question of its own.
        /// </summary>
        private static void OnDialogBoxShowing(object sender, DialogBoxShowingEventArgs e)
        {
            var met = _armed;
            if (met == null || _heronAsking > 0 || e == null) return;

            var id = "";
            var kind = "dialog";
            var message = "";
            var answered = 0;
            var failsRun = true;
            try
            {
                id = e.DialogId ?? "";

                var task = e as TaskDialogShowingEventArgs;
                var box = e as MessageBoxShowingEventArgs;
                if (task != null) { kind = "task dialog"; message = task.Message; }
                else if (box != null) { kind = "message box"; message = box.Message; }

                // THE CALLER'S ANSWER FIRST, for the one id it was given for
                // (D-145). Revit refusing it leaves answered at 0, and the job
                // stops as for any dialog Heron could not answer.
                int chosen;
                var callers = _callerAnswers;
                if (callers != null && callers.TryGetValue(id, out chosen))
                {
                    failsRun = false;
                    if (e.OverrideResult(chosen)) answered = chosen;
                }
                else
                {
                    var row = HeronDialogRule.For(id);
                    failsRun = row.FailsRun;
                    foreach (var code in row.Codes)
                    {
                        if (e.OverrideResult(code)) { answered = code; break; }
                    }
                }
            }
            catch (Exception ex)
            {
                // RECORDED AS UNANSWERED, never dropped. This runs on Revit's
                // own thread, where an escaping exception is Revit's problem,
                // and a dialog Heron could not answer still stops the job.
                _log("Could not answer a Revit dialog during a Heron job: " + ex.Message);
                answered = 0;
            }

            met.Add(id, kind, message, answered, failsRun);

            // The note's copy of the words: one line, so the log stays one too.
            var said = met.Dialogs[met.Count - 1].Message;
            _log("Revit dialog during a Heron job: id '" + id + "', " + kind + ", answered "
                 + HeronDialogNote.AnswerName(answered) + (failsRun ? ", the job stops" : "")
                 + (said.Length == 0 ? "" : ": " + said));
        }
    }
}
