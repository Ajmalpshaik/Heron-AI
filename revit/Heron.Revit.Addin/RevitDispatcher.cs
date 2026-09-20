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
        /// It is read and written from several threads, so it moves with
        /// Volatile rather than a plain assignment.
        /// </summary>
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
            Volatile.Write(ref _activeModel, string.IsNullOrEmpty(title) ? null : title);
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

            Interlocked.CompareExchange(ref _activeModel, null, title);
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
            var model = Volatile.Read(ref _activeModel);
            _banner.Begin(DescribeJob(op, request), HeronOperationRegistry.Writes(op), model);

            lock (_queueLock) { _queue.Enqueue(job); }

            try
            {
                raiser.Raise();
            }
            catch (Exception ex)
            {
                job.Abandon();
                if (job.TakeBannerEnd())
                    _banner.End(false, "Revit would not take the request", -1, model);
                return Json.Error("raise_failed", ex.Message);
            }

            if (!job.WaitForStart(_busyTimeout))
            {
                // Revit never became idle. Abandon it so the handler skips it
                // rather than doing work whose answer nobody is waiting for.
                job.Abandon();
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

            if (!job.WaitForFinish(_operationTimeout))
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

                if (job.IsAbandoned) continue;   // the caller already gave up

                job.MarkStarted();
                var clock = Stopwatch.StartNew();
                string response = null;
                string document = null;
                try
                {
                    var op = Json.ReadString(job.Request, "op") ?? "(none)";
                    try
                    {
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
                    clock.Stop();

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
                        Volatile.Write(ref _activeModel, document);

                    // THE HONEST END OF THE WORK, and the reason End is not
                    // called back in Dispatch: this is the moment Revit is
                    // free again. A caller that already gave up ("still
                    // running") is not the same event as the job finishing,
                    // and the screen must follow the model, not the client.
                    var failure = Json.ReadString(response, "error");
                    if (job.TakeBannerEnd())
                        _banner.End(failure == null, Explain(failure), clock.ElapsedMilliseconds, document);

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
                    HeronAudit.Record(job.WorkflowId, op,
                        failure == null,
                        new[]
                        {
                            new KeyValuePair<string, string>("session", _session),
                            new KeyValuePair<string, string>("document", document),
                            new KeyValuePair<string, string>("error", Json.ReadString(response, "error")),
                            new KeyValuePair<string, string>("fragment",
                                op == "run_fragment_read" || op == "run_fragment_write"
                                    ? Json.ReadString(job.Request, "name") : null),
                        },
                        new[]
                        {
                            new KeyValuePair<string, long>("ms", clock.ElapsedMilliseconds),
                        });
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

                case "run_fragment_read":
                    var name = Clean(Json.ReadString(request, "name"));
                    return name.Length == 0 ? "Running a job" : "Running a job: " + name;

                // THE BANNER MUST NOT SAY THE SAME THING FOR BOTH. Its colour
                // already differs - amber, from the tool registry - but the
                // words are what somebody actually reads, and "Running a job"
                // over a model being changed is the wrong sentence. Whether it
                // will be KEPT is not decided here: the caller says `apply`,
                // and a trial that rolls back is still Revit doing the work.
                case "run_fragment_write":
                    var writing = Clean(Json.ReadString(request, "name"));
                    var keeping = string.Equals(Json.ReadString(request, "apply"), "true",
                                                StringComparison.OrdinalIgnoreCase);
                    var verb = keeping ? "Changing the model" : "Trying a change";
                    return writing.Length == 0 ? verb : verb + ": " + writing;
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
        private static string Clean(string text)
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
                case "preview_expired":
                case "model_moved_on":  return "The model changed - ask again";
                case "fragment_threw":
                case "operation_failed": return "The job failed in Revit";
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
            private readonly ManualResetEventSlim _started = new ManualResetEventSlim(false);
            private readonly ManualResetEventSlim _finished = new ManualResetEventSlim(false);
            private volatile bool _abandoned;
            private int _bannerEnded;

            public RevitJob(string request, string workflowId)
            {
                Request = request;
                WorkflowId = workflowId;
            }

            public string Request { get; private set; }

            /// <summary>Follows this request through every layer that touches it.</summary>
            public string WorkflowId { get; private set; }
            public string Response { get; private set; }
            public bool IsAbandoned { get { return _abandoned; } }

            public void MarkStarted() { _started.Set(); }

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

            /// <summary>The caller stopped waiting. Skip it rather than run it.</summary>
            public void Abandon()
            {
                _abandoned = true;
                _started.Set();      // release anything still parked on it
                _finished.Set();
            }

            public bool WaitForStart(TimeSpan timeout) { return _started.Wait(timeout); }
            public bool WaitForFinish(TimeSpan timeout) { return _finished.Wait(timeout); }
        }
    }
}
