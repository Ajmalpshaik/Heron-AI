// Heron-Agent:  HERON-REVIT-APP-003
// Heron-Step:   2
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using Autodesk.Revit.UI;
using Heron.Bridge;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// The thread hop. Everything Heron will ever do to a model passes through
    /// this one class, which is why it is small and why it is worth reading.
    ///
    /// THE CONSTRAINT (docs/03 section 4): the Revit API can only be called on
    /// Revit's own thread, inside an API context. The bridge listens on
    /// background threads and the AI host is a separate process entirely.
    /// Neither may touch a model. So a request is queued, an ExternalEvent is
    /// raised, and Revit runs the handler when it is ready.
    ///
    ///     listener thread            Revit main thread
    ///     ---------------            -----------------
    ///     enqueue job
    ///     Raise()             --->   (when Revit is idle)
    ///     wait for start             Execute() drains the queue
    ///     wait for finish            runs the operation IN context
    ///     read the answer     <---   publishes the answer
    ///
    /// ONE event, ONE queue, ONE handler - D-09. Not one per operation: Revit
    /// has a limited appetite for registered external events, and a single
    /// queue is what makes ordering and timeouts tractable at all.
    ///
    /// THE TWO WAITS ARE DIFFERENT QUESTIONS, and conflating them produces the
    /// worst error message in the system:
    ///
    ///   * Did Revit ever pick this up?  If not, Revit is busy - a dialog is
    ///     open, or the user is mid-command. Raise() is a request, not a
    ///     guarantee, and this is the normal, expected answer. Short wait.
    ///
    ///   * Having started, did it finish?  If not, the work itself is slow.
    ///     Saying "Revit is busy" here would be a lie, and would invite the
    ///     user to retry something already running. Longer wait.
    ///
    /// Both stay below the client's own deadline, so the bridge is what
    /// answers. A client that gives up first can only say "no reply", which
    /// tells the user nothing about what to do next.
    /// </summary>
    internal sealed class RevitDispatcher : IExternalEventHandler
    {
        private readonly Queue<RevitJob> _queue = new Queue<RevitJob>();
        private readonly object _queueLock = new object();
        private readonly TimeSpan _busyTimeout;
        private readonly TimeSpan _operationTimeout;

        /// <summary>
        /// The extra wait for a job that has asked the modeller to keep or
        /// discard a DRAFT's change (D-119). See AwaitingModeller.
        /// </summary>
        private readonly TimeSpan _askTimeout;

        /// <summary>
        /// The job Revit's thread is running now, so code deep inside it can
        /// say "I am waiting for a person" without being handed the job.
        ///
        /// [ThreadStatic] ON PURPOSE: only Revit's own thread runs a job, so
        /// only that thread ever finds one here. A call from anywhere else
        /// finds null and changes nothing, which is the safe answer.
        /// </summary>
        [ThreadStatic]
        private static RevitJob _running;

        /// <summary>
        /// The banner of the job running now, for the same reason as _running:
        /// AwaitingModeller has to move it out of the way of the box, and the
        /// box's code is never handed the dispatcher (D-126).
        /// </summary>
        [ThreadStatic]
        private static HeronActivityBanner _runningBanner;

        private readonly Action<string> _log;
        private readonly string _session;

        /// <summary>
        /// What Revit SHOWS while this is happening - HERON-REVIT-UI-022.
        ///
        /// It belongs here rather than deeper in because this class is the one
        /// place that knows both halves of a request: the moment before Revit
        /// is asked, and the moment it is done. Anything further in runs on
        /// the blocked thread and could not raise a banner in time; anything
        /// further out does not know when the work truly ends.
        /// </summary>
        private readonly HeronActivityBanner _banner;

        /// <summary>
        /// The name of the model Revit last had in front, for the banner
        /// to announce BEFORE the work starts.
        ///
        /// It has to be cached, and the reason is the same constraint
        /// this whole class exists for: Begin runs on a listener thread,
        /// where the Revit API may not be touched, so the document cannot
        /// be asked for at the moment it is needed. What is cached is a
        /// STRING and never a Document - a held Document goes stale the
        /// moment the model closes and is the thing the conventions
        /// forbid outright; a name is inert and the worst it can be is
        /// out of date.
        ///
        /// Two things keep it honest. Revit pushes every document switch
        /// in through NoteActiveModel as it happens, and every finished
        /// job overwrites it with the model the work actually reported.
        /// It is read and written from several threads, so every access
        /// goes through _modelLock.
        ///
        /// IT USED TO MOVE WITH Volatile AND ONE Interlocked.CompareExchange,
        /// and that one comparison was the defect: CompareExchange on a
        /// reference type matches by REFERENCE, never by value, while
        /// ForgetActiveModel's own docstring says it compares the names. The
        /// two strings are always distinct instances in practice and one
        /// provably so - the name written at line 320 below is parsed out of
        /// JSON, a fresh allocation every time - so after any job has run,
        /// the clear could not fire at all. A lock is what makes a
        /// read-compare-write atomic AND by value; a second Interlocked form
        /// would not. FRAGMENT-ISSUES section 5b, row 18.
        /// </summary>
        private readonly object _modelLock = new object();
        private string _activeModel;

        private ExternalEvent _event;

        public RevitDispatcher(Action<string> log, string session, HeronActivityBanner banner)
        {
            _log = log ?? delegate { };
            _session = session;

            // A silent do-nothing banner rather than a null, so that every
            // call site below is a plain call. A cosmetic feature is not
            // worth a null check on the path that reaches the model.
            _banner = banner ?? new HeronActivityBanner(false, log);

            var config = HeronConfig.Load();
            _busyTimeout = TimeSpan.FromSeconds(
                Math.Max(1, config.GetInt("revit.busyTimeoutSeconds", 10)));
            _operationTimeout = TimeSpan.FromSeconds(
                Math.Max(1, config.GetInt("revit.operationTimeoutSeconds", 60)));
            _askTimeout = TimeSpan.FromSeconds(
                Math.Max(1, config.GetInt("revit.askTimeoutSeconds", 300)));
        }

        /// <summary>
        /// Revit's own thread, from inside a running job: the job is about to
        /// show the modeller a Keep or Discard box and wait for them (D-119).
        ///
        /// THE AWAITING-THE-MODELLER EXEMPTION. Dispatch gives up on a job
        /// that has not finished within revit.operationTimeoutSeconds and
        /// answers "still running" - right for slow work, wrong for a person
        /// reading their model before they choose. A job marked here is given
        /// revit.askTimeoutSeconds more before the caller stops waiting.
        ///
        /// It only ever LENGTHENS a wait, and only for the job running now.
        /// It cannot shorten one, start one, or answer the box.
        /// </summary>
        internal static void AwaitingModeller()
        {
            var job = _running;
            if (job != null) job.MarkAskedModeller();

            // AND THE BANNER STEPS ASIDE (D-126). It is Topmost, so it would
            // sit over the box it is waiting on; Execute brings it back the
            // moment the job returns, before it says how the job ended.
            var banner = _runningBanner;
            if (banner != null) banner.StepAside(true);
        }

        /// <summary>
        /// Must be called from Revit's own thread, during OnStartup. Creating
        /// an ExternalEvent from a background thread is not valid.
        /// </summary>
        public void Register()
        {
            _event = ExternalEvent.Create(this);
        }

        public string GetName()
        {
            return "Heron";
        }

        /// <summary>
        /// Revit's own thread, from the view-activated handler: the model
        /// in front has changed.
        ///
        /// This is what makes the name right on the FIRST job of a chat,
        /// rather than only from the second onwards - without it the
        /// cache is empty until some job has already been announced
        /// unnamed, and the first job is the one a person is most likely
        /// to be watching.
        ///
        /// Null is accepted and stored: closing the last model leaves
        /// Revit on its start screen, and an empty name is the truth
        /// there. Naming a model that is no longer open would be worse
        /// than naming none.
        /// </summary>
        public void NoteActiveModel(string title)
        {
            lock (_modelLock) { _activeModel = string.IsNullOrEmpty(title) ? null : title; }
        }

        /// <summary>
        /// Revit's own thread, from the document-closing handler: this
        /// model is going away.
        ///
        /// Closing the LAST model activates no other view, so nothing
        /// else would ever clear the name - and the banner would go on
        /// announcing a document nobody has open, while the operation
        /// underneath answered that there is no model at all. Two halves
        /// of one card contradicting each other is worse than a card
        /// naming nothing.
        ///
        /// Cleared only when the model closing IS the one being named.
        /// Closing a background model while working in another must
        /// leave the foreground name alone, and comparing the names is
        /// the only way to tell those two apart from here.
        /// </summary>
        public void ForgetActiveModel(string title)
        {
            if (string.IsNullOrEmpty(title)) return;

            // BY VALUE. The same API is used correctly forty lines below -
            // TakeBannerEnd compares an int, where CompareExchange means
            // exactly what it reads as. On a string it does not.
            lock (_modelLock)
            {
                if (string.Equals(_activeModel, title, StringComparison.Ordinal))
                    _activeModel = null;
            }
        }

        /// <summary>
        /// Called on a bridge listener thread. Hands the work to Revit and
        /// waits for it, without ever touching the Revit API here.
        /// </summary>
        public string Dispatch(string request)
        {
            var raiser = _event;
            if (raiser == null)
            {
                return Json.Error("not_ready",
                    "Heron has not finished starting. Try again in a moment.");
            }

            var job = new RevitJob(request, HeronIdentity.NewWorkflowId());

            // THE BANNER GOES UP HERE, BEFORE Raise, AND THAT ORDER IS THE
            // WHOLE TRICK. Revit draws on the same thread it works on, so the
            // instant Execute starts nothing can be painted - a banner posted
            // any later would appear only once the work it announces is over.
            // Posting it first puts it in the queue ahead of the idle pass
            // that runs the ExternalEvent.
            //
            // AND BEFORE THE ENQUEUE, which is the half that was missing. A
            // queued job can be taken by an Execute already running for an
            // earlier Raise, so between the enqueue and this line the job
            // could be finished and ENDED before it had ever been begun. The
            // banner counts what is in flight, and a count that goes down
            // before it goes up leaves a banner over an idle Revit (D-56).
            //
            // The read/change word comes from the tool registry, looked up by
            // operation name (Golden Rule 19). Nothing in the request decides
            // it, so a write can never wear the reading colour.
            // The name is the last one Revit reported, not one asked for
            // here - there is no asking from this thread. Execute
            // replaces it with the model the work truly ran against, so a
            // switch made in the gap between these two lines and Revit
            // picking the job up is corrected when the banner settles
            // rather than left standing.
            var op = Json.ReadString(request, "op");
            string model;
            lock (_modelLock) { model = _activeModel; }
            _banner.Begin(DescribeJob(op, request), HeronOperationRegistry.Writes(op), model);

            lock (_queueLock) { _queue.Enqueue(job); }

            // ABANDONED ONLY IF REVIT HAS NOT TAKEN IT, in both places below.
            // The check and the start used to be two steps on two threads -
            // Execute asked IsAbandoned and then marked the job started - so a
            // wait that ran out between them reported "nothing was sent" for a
            // job Revit then ran. Behind a queue of other requests that wait
            // runs out far more often, because the busy clock starts at the
            // enqueue. One compare-and-swap now decides: whichever side moves
            // the job first wins, and the other follows what really happened.
            // FRAGMENT-ISSUES section 5b, row 458.
            var raised = true;
            try
            {
                raiser.Raise();
            }
            catch (Exception ex)
            {
                // An Execute already draining the queue for an earlier request
                // can still take this one; if it has, it is running - wait for
                // it like any other.
                if (job.TryAbandon())
                {
                    if (job.TakeBannerEnd())
                        _banner.End(false, "Revit would not take the request", -1, model);
                    return Json.Error("raise_failed", ex.Message);
                }
                raised = false;
            }

            if (raised && !job.WaitForStart(_busyTimeout) && job.TryAbandon())
            {
                // Revit never became idle. Abandoned, so the handler skips it
                // rather than doing work whose answer nobody is waiting for.
                RecordRefusal(job.WorkflowId, request, "revit_busy");

                // Execute will skip this one, so nobody else will lower the
                // banner. It is also the case the banner is worth the most:
                // the reason Revit did not take it is that something is open
                // ON SCREEN, which is where the person is already looking.
                if (job.TakeBannerEnd())
                    _banner.End(false, "Revit was busy - nothing was sent", -1, model);

                return Json.Error("revit_busy",
                    "Revit is busy and did not take the request. A dialog may be open, " +
                    "or a command may be running. Finish what is open in Revit and ask again.");
            }

            // THE MODELLER IS NOT A TIMEOUT (D-119). A job that put a Keep or
            // Discard box up is waiting for a person, so it gets the ask
            // timeout on top - asked AFTER the first wait ran out, because the
            // box can go up at any moment of it.
            var finished = job.WaitForFinish(_operationTimeout)
                || (job.AskedModeller && job.WaitForFinish(_askTimeout));

            if (!finished && job.AskedModeller)
            {
                // The box is still open in Revit, and it still decides. This
                // caller stops waiting; whatever the modeller presses stands,
                // and the banner comes down when they do.
                RecordRefusal(job.WorkflowId, request, "awaiting_modeller");
                return Json.Error("awaiting_modeller",
                    "Revit is still waiting for you to choose Keep or Discard, and Heron " +
                    "has stopped waiting for the answer. Whatever you press in Revit stands - " +
                    "Discard puts the model back exactly as it was. This run is not recorded; " +
                    "do not repeat the request while the box is open.");
            }

            if (!finished)
            {
                // It IS running - Revit picked it up. Nothing can safely
                // interrupt it, and calling this "busy" would invite a retry of
                // something already in progress.
                //
                // THE BANNER IS DELIBERATELY LEFT UP. This caller has stopped
                // waiting; the work has not stopped, and Revit is still frozen
                // because of it. Execute lowers it when the job genuinely
                // ends, which is the only honest moment - and until then the
                // screen keeps saying what the freeze is.
                RecordRefusal(job.WorkflowId, request, "still_running");
                return Json.Error("still_running",
                    "Revit started the request but has not finished within " +
                    _operationTimeout.TotalSeconds.ToString(CultureInfo.InvariantCulture) +
                    " seconds. It is still working; do not repeat the request.");
            }

            return job.Response;
        }

        /// <summary>
        /// Revit's own thread, inside a valid API context. The only place in
        /// Heron where the Revit API may be touched.
        /// </summary>
        public void Execute(UIApplication app)
        {
            while (true)
            {
                RevitJob job;
                lock (_queueLock)
                {
                    if (_queue.Count == 0) return;
                    job = _queue.Dequeue();
                }

                if (!job.TryStart()) continue;   // the caller already gave up

                var clock = Stopwatch.StartNew();
                string response = null;
                string document = null;
                try
                {
                    var op = Json.ReadString(job.Request, "op") ?? "(none)";

                    // A REVIT DIALOG THE JOB OPENS IS ANSWERED, NOT WAITED ON -
                    // FRAGMENT-ISSUES section 5b row 300. Armed here, after
                    // TryStart and around Run alone, so it answers only what
                    // THIS job raises. A dialog already open before the job
                    // never gets this far: Revit runs no ExternalEvent behind
                    // a modal dialog, Dispatch's busy wait runs out, and the
                    // caller is told revit_busy as before.
                    HeronDialogWatch.Arm();
                    HeronDialogNote dialogs = null;
                    try
                    {
                        try
                        {
                            // So AwaitingModeller can find this job - see _running.
                            _running = job;
                            _runningBanner = _banner;
                            response = RevitOperations.Run(app, job.Request);
                        }
                        catch (Exception ex)
                        {
                            // Never let one bad request throw out of here: this
                            // runs on Revit's own thread, and an escaping
                            // exception is Revit's problem, not just Heron's.
                            _log("Operation failed: " + ex);
                            response = Json.Error("operation_failed", ex.Message);
                        }
                        finally
                        {
                            _running = null;
                            _runningBanner = null;

                            // The box is answered or the job is over: the
                            // banner comes back to say how it ended (D-126).
                            if (job.AskedModeller) _banner.StepAside(false);
                        }
                    }
                    finally
                    {
                        dialogs = HeronDialogWatch.Disarm();
                    }
                    clock.Stop();

                    // WHAT IT MET GOES ON THE REPLY - `dialogsAnswered` and
                    // each dialog's id, words and answer - and nothing at all
                    // when it met none. A path that came back a success from a
                    // job a dialog stopped is turned into the refusal here.
                    response = HeronDialogNote.Attach(response, dialogs,
                                                      HeronOperationRegistry.Writes(op));

                    // WHICH MODEL THE WORK ACTUALLY RAN AGAINST, read out
                    // of its own answer. This is the only authoritative
                    // name anywhere on the path: it is the one place with
                    // both a valid API context and a finished job, and it
                    // costs nothing - the audit line below has been
                    // reading exactly this all along.
                    //
                    // Kept for the next Begin as well as spent on this
                    // End, so a chat that never switches view still has a
                    // fresh name to announce.
                    document = Json.ReadString(response, "document");
                    if (!string.IsNullOrEmpty(document))
                        lock (_modelLock) { _activeModel = document; }

                    // THE HONEST END OF THE WORK, and the reason End is not
                    // called back in Dispatch: this is the moment Revit is
                    // free again. A caller that already gave up ("still
                    // running") is not the same event as the job finishing,
                    // and the screen must follow the model, not the client.
                    var failure = Json.ReadString(response, "error");
                    if (job.TakeBannerEnd())
                        _banner.End(failure == null, Ended(failure, response),
                                    clock.ElapsedMilliseconds, document);

                    // One line per request, whatever happened. A trail that
                    // only records successes answers the wrong question later.
                    //
                    // THE FRAGMENT IS NAMED, because without it this trail
                    // cannot answer the question it exists to answer. Every
                    // fragment run was logged as "run_fragment_read" and
                    // nothing else, so 564 entries said a fragment ran and not
                    // one said WHICH - and the Capability Gap report (docs/06
                    // s6) is precisely a ranking by which. It could see that
                    // something took 37 seconds against a 7 ms median and
                    // could not say what.
                    //
                    // Read from the REQUEST rather than the response: a run
                    // that failed to compile has no answer to name itself in,
                    // and those are the entries the report most needs. Guarded
                    // by the op so the field means one thing - a later
                    // operation carrying its own identity adds its own line
                    // here rather than borrowing this one, which is how a
                    // field ends up meaning two things.
                    //
                    // The two operations above Modify run the same executor
                    // (D-106), and a trail that named the fragment for a
                    // Modify run and not for an Admin one would lose exactly
                    // the runs worth finding. One test, so the fragment name
                    // and the warning count below can never disagree about
                    // which runs are fragment runs.
                    var fragmentRun =
                        op == "run_fragment_read" || op == "run_fragment_show"
                        || op == "run_fragment_write"
                        || op == "run_fragment_publish" || op == "run_fragment_admin";

                    // AND WHAT REVIT RAISED, read off the reply as a NUMBER.
                    // A fragment write's reply carries `warnings` - how many
                    // Revit raised and Heron dismissed - and this line carried
                    // the error and not that, so the trail recorded that a
                    // fragment ran and not that Revit warned about what it did.
                    // The chat heard it; the trail did not. FRAGMENT-ISSUES
                    // section 5b, row 159.
                    //
                    // ABSENT IS LEFT ABSENT, never written as 0. A read opens
                    // no transaction and a refusal carries no count, and "Revit
                    // raised none" is a claim only a reply that said 0 makes.
                    var numbers = new List<KeyValuePair<string, long>>
                    {
                        new KeyValuePair<string, long>("ms", clock.ElapsedMilliseconds),
                    };
                    var warnings = fragmentRun ? Json.ReadLong(response, "warnings") : null;
                    if (warnings.HasValue)
                        numbers.Add(new KeyValuePair<string, long>("warnings", warnings.Value));

                    // AND WHAT A READ LEFT IN THE MODEL - row 5b-350. A read
                    // run that changed the model comes back read_changed_model
                    // with the count; the error alone says it happened, the
                    // count says how much, so the trail can answer "what did
                    // Heron change" for a run that was meant to change nothing.
                    // Absent on every other reply, and left absent.
                    var appeared = fragmentRun ? Json.ReadLong(response, "elementsAppeared") : null;
                    if (appeared.HasValue)
                        numbers.Add(new KeyValuePair<string, long>("elementsAppeared", appeared.Value));
                    var typesAppeared = fragmentRun ? Json.ReadLong(response, "typesAppeared") : null;
                    if (typesAppeared.HasValue)
                        numbers.Add(new KeyValuePair<string, long>("typesAppeared", typesAppeared.Value));

                    // AND HOW MANY REVIT DIALOGS IT MET, on every operation -
                    // a dialog is not a fragment's alone. Absent when it met
                    // none, like the reply: every job is armed, so absent here
                    // means none opened, never that nobody looked.
                    if (dialogs.Count > 0)
                        numbers.Add(new KeyValuePair<string, long>("dialogs", dialogs.Count));

                    HeronAudit.Record(job.WorkflowId, op,
                        failure == null,
                        new[]
                        {
                            new KeyValuePair<string, string>("session", _session),
                            new KeyValuePair<string, string>("document", document),
                            new KeyValuePair<string, string>("error", Json.ReadString(response, "error")),
                            new KeyValuePair<string, string>("fragment",
                                fragmentRun ? Json.ReadString(job.Request, "name") : null),
                        },
                        numbers);
                }
                finally
                {
                    // THE TWO PROMISES OF A JOB, kept whatever went wrong
                    // above. Only Run is wrapped in a catch of its own; the
                    // audit write and the reads around it are not, and
                    // anything throwing there escaped this loop entirely -
                    // leaving the banner up over an idle Revit and the caller
                    // waiting out its whole timeout for an answer that was
                    // never coming.
                    //
                    // Both are safe to reach twice, which is why they can sit
                    // here as well as above. TakeBannerEnd is the one
                    // interlocked flag from D-50, and on the normal path it
                    // has already been taken, so this says nothing; Finish on
                    // an answered job re-sets an event that is already set.
                    clock.Stop();
                    if (response == null)
                    {
                        response = Json.Error("operation_failed",
                            "The job ended without an answer. Nothing came back from Revit, " +
                            "and nothing further was done to the model.");
                    }

                    // No model named here on purpose. Reaching this means
                    // the answer never arrived, so there is nothing to
                    // read a name out of - and the banner falls back to
                    // the one it announced, which is the last thing about
                    // this job that was ever true.
                    if (job.TakeBannerEnd())
                        _banner.End(false, "The job ended without an answer", clock.ElapsedMilliseconds, document);

                    job.Finish(response);
                }
            }
        }

        /// <summary>
        /// What the banner says Heron is doing, in the words a modeller would
        /// use rather than the words the wire uses.
        ///
        /// An operation name is a protocol token: "run_fragment_read" tells
        /// somebody watching their model precisely nothing. This is the one
        /// place that translation lives, so a new operation that forgets to
        /// add itself degrades to a readable version of its own name rather
        /// than to a blank card.
        /// </summary>
        private static string DescribeJob(string op, string request)
        {
            switch (op)
            {
                case "count_elements":
                    return "Counting what is in the model";

                case "select_by_category":
                    return "Selecting elements on screen";

                case "preview_move":
                    return "Working out what a move would do";

                case "move_elements":
                    return "Moving elements";

                // THE READING COLOUR, AND THE WORDS SAY WHY. Given element ids
                // it makes a 3D view somebody watching may glimpse in the
                // Project Browser, and the group around it is always undone
                // (D-123) - so the card says a picture, never a change.
                case "capture_view":
                    return "Taking a picture of a view for the chat";

                case "run_fragment_read":
                    var name = Clean(Json.ReadString(request, "name"));
                    return name.Length == 0 ? "Running a job" : "Running a job: " + name;

                // WHAT IS SHOWN, NOT WHAT EXISTS - the show door changes the
                // selection, the camera or the view, and the banner says that
                // rather than the read's "Running a job".
                case "run_fragment_show":
                    var showing = Clean(Json.ReadString(request, "name"));
                    return showing.Length == 0 ? "Showing on screen" : "Showing on screen: " + showing;

                case "isolate_temporary":
                    return "Isolating in the active view, temporarily";

                case "reset_temporary":
                    return "Resetting the temporary hide/isolate";

                // THE BANNER MUST NOT SAY THE SAME THING FOR BOTH. Its colour
                // already differs - amber, from the tool registry - but the
                // words are what somebody actually reads, and "Running a job"
                // over a model being changed is the wrong sentence. Whether it
                // will be KEPT is not decided here: the caller says `apply`,
                // and a trial that rolls back is still Revit doing the work.
                case "run_fragment_write":
                    var writing = Clean(Json.ReadString(request, "name"));
                    var applyWord = Json.ReadString(request, "apply");
                    var keeping = string.Equals(applyWord, "true",
                                                StringComparison.OrdinalIgnoreCase);
                    // D-119: it runs, and the modeller decides in Revit.
                    var asking = string.Equals(applyWord, "ask",
                                               StringComparison.OrdinalIgnoreCase);
                    var verb = asking ? "Trying a new tool - you choose Keep or Discard"
                             : keeping ? "Changing the model" : "Trying a change";
                    return writing.Length == 0 ? verb : verb + ": " + writing;

                // THE TWO ABOVE MODIFY SAY WHICH KIND OF CHANGE, because the
                // owner turned a separate switch on for each (D-106) and the
                // banner is where he sees which one is being used. Whether it
                // is kept is still the caller's `apply`, as above.
                case "run_fragment_publish":
                case "run_fragment_admin":
                    var named = Clean(Json.ReadString(request, "name"));
                    var kept = string.Equals(Json.ReadString(request, "apply"), "true",
                                             StringComparison.OrdinalIgnoreCase);
                    var kind = op == "run_fragment_admin"
                        ? (kept ? "Changing the project setup" : "Trying a project setup change")
                        : (kept ? "Publishing from the model" : "Trying a publish");
                    return named.Length == 0 ? kind : kind + ": " + named;
            }

            var readable = Clean(op);
            return readable.Length == 0 ? "Working" : readable.Replace('_', ' ');
        }

        /// <summary>
        /// Text off the wire, made safe to put on screen.
        ///
        /// A fragment name arrives from the client, and this banner sits over
        /// Revit looking like part of Revit. Newlines and control characters
        /// would let a name spread down the card, and an unbounded one would
        /// push everything else off it - so the name is a single short line or
        /// it is nothing. It can never say more than a name, whatever it
        /// contains.
        /// </summary>
        internal static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var kept = new StringBuilder(48);
            foreach (var c in text)
            {
                if (char.IsControl(c)) continue;
                kept.Append(c);
                if (kept.Length == 44) { kept.Append('\u2026'); break; }
            }
            return kept.ToString().Trim();
        }

        /// <summary>
        /// The banner's last line for a finished job: the modeller's own Keep
        /// or Discard when the job asked them one (D-126), otherwise Explain's
        /// line for the error code.
        /// </summary>
        private static string Ended(string failure, string response)
        {
            // A KEEP OR DISCARD THE MODELLER ANSWERED IS NO FAILURE, and "Done"
            // over a change they just discarded would read as kept (D-126).
            if (failure == null)
            {
                var answered = Json.ReadString(response, "modellerAnswer");
                if (answered == "discard" || answered == "closed") return "Discarded - nothing was kept";
                if (answered == "keep") return "Kept - one undo step";
            }
            return Explain(failure);
        }

        /// <summary>
        /// An error code turned into the one line the banner has room for.
        ///
        /// The full sentence still goes back to the chat, which is where the
        /// user can read and act on it. This is the glance version, for
        /// somebody who is looking at Revit rather than at the chat - and the
        /// refusals are the ones worth showing there, because until now a
        /// refusal was completely invisible from Revit.
        /// </summary>
        private static string Explain(string error)
        {
            switch (error)
            {
                case null:              return "Done";
                case "revit_busy":      return "Revit was busy - nothing was sent";
                case "stopped":         return "Heron is stopped";
                case "write_disabled":  return "Not allowed to change the model";
                case "admin_disabled":  return "Admin is off - nothing was sent";
                case "publish_disabled": return "Publish is off - nothing was sent";
                case "session_in_use":  return "Another chat is using this Revit";
                case "unknown_op":      return "Heron does not know that job";
                case "not_implemented": return "Heron has no handler for that yet";
                case "no_source":       return "No job was sent";
                case "needs_unbound":   return "Nothing to work on - check the selection";
                case "needs_request_values": return "The job is missing a value";
                case "compile_failed":  return "The job would not compile";
                case "no_document":     return "No model is open";
                case "document_closed": return "That model was closed";
                case "no_such_document":
                case "document_not_in_front": return "That model is not open here";
                case "read_only":       return "That model is read-only";
                case "nothing_to_move": return "Nothing to move";
                case "no_such_view":
                case "ambiguous_view":  return "No single view by that name";
                case "picture_too_large": return "The picture was too large to send";
                case "preview_expired":
                case "model_moved_on":  return "The model changed - ask again";
                case "fragment_threw":
                case "operation_failed": return "The job failed in Revit";
                case "read_changed_model": return "A read changed the model - look at it";
                case "read_unverified": return "Could not check the model after a read";
                case "ask_needs_change_path": return "That job cannot be tried this way";
                case "ask_needs_model_in_front": return "Bring that model to the front first";
                case "ask_needs_saved_model": return "Save the model first - nothing was run";
                case "dialog_refused":  return "A Revit dialog stopped the job - nothing kept";
                case "dialog_unconfirmed": return "A Revit dialog stopped the job - check the model";
                default:                return "Did not finish";
            }
        }

        /// <summary>
        /// A request Revit never ran. It still happened, so it is still
        /// recorded - "Heron asked and Revit was busy" is exactly the kind of
        /// thing the trail exists to be able to answer later.
        /// </summary>
        private void RecordRefusal(string workflowId, string request, string reason)
        {
            HeronAudit.Record(workflowId, Json.ReadString(request, "op") ?? "(none)", false,
                new[]
                {
                    new KeyValuePair<string, string>("session", _session),
                    new KeyValuePair<string, string>("error", reason),
                });
        }

        /// <summary>One request, and the two signals its caller waits on.</summary>
        private sealed class RevitJob
        {
            private const int Queued = 0;
            private const int Started = 1;
            private const int Abandoned = 2;

            private readonly ManualResetEventSlim _started = new ManualResetEventSlim(false);
            private readonly ManualResetEventSlim _finished = new ManualResetEventSlim(false);
            private int _state = Queued;
            private int _bannerEnded;
            private int _askedModeller;

            public RevitJob(string request, string workflowId)
            {
                Request = request;
                WorkflowId = workflowId;
            }

            public string Request { get; private set; }

            /// <summary>Follows this request through every layer that touches it.</summary>
            public string WorkflowId { get; private set; }
            public string Response { get; private set; }

            /// <summary>
            /// Revit's thread takes the job. False when the caller abandoned it
            /// first - skip it. Never true for a job its caller was told Revit
            /// did not take: that is the whole of row 458's second half.
            /// </summary>
            public bool TryStart()
            {
                if (Interlocked.CompareExchange(ref _state, Started, Queued) != Queued) return false;
                _started.Set();
                return true;
            }

            /// <summary>
            /// Claims the right to lower the banner for this job, once.
            ///
            /// Two threads can both believe the job is theirs to finish: the
            /// listener abandons it on the busy timeout at the same instant
            /// Revit picks it up, and both would then call End - leaving the
            /// active count one short and the banner up over a Revit doing
            /// nothing. Whoever gets here first wins, and the other says
            /// nothing.
            /// </summary>
            public bool TakeBannerEnd()
            {
                return Interlocked.CompareExchange(ref _bannerEnded, 1, 0) == 0;
            }

            public void Finish(string response)
            {
                Response = response;
                _finished.Set();
            }

            /// <summary>
            /// The caller stops waiting, so Revit must skip it rather than run
            /// it. False when Revit has already taken it: it is running, and
            /// the caller must wait for its answer instead of saying it was not
            /// sent.
            /// </summary>
            public bool TryAbandon()
            {
                if (Interlocked.CompareExchange(ref _state, Abandoned, Queued) != Queued) return false;
                _started.Set();      // release anything still parked on it
                _finished.Set();
                return true;
            }

            /// <summary>
            /// Set on Revit's thread when the job puts a Keep or Discard box
            /// up, read on the listener thread that is waiting for it. Never
            /// cleared: a job that asked once is a job a person is part of.
            /// </summary>
            public void MarkAskedModeller() { Interlocked.Exchange(ref _askedModeller, 1); }

            public bool AskedModeller { get { return Volatile.Read(ref _askedModeller) == 1; } }

            public bool WaitForStart(TimeSpan timeout) { return _started.Wait(timeout); }
            public bool WaitForFinish(TimeSpan timeout) { return _finished.Wait(timeout); }
        }
    }
}
