// Heron-Agent:  HERON-REVIT-APP-003
// Heron-Step:   2
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
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

        private ExternalEvent _event;

        public RevitDispatcher(Action<string> log)
        {
            _log = log ?? delegate { };

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

            var job = new RevitJob(request);
            lock (_queueLock) { _queue.Enqueue(job); }

            try
            {
                raiser.Raise();
            }
            catch (Exception ex)
            {
                job.Abandon();
                return Json.Error("raise_failed", ex.Message);
            }

            if (!job.WaitForStart(_busyTimeout))
            {
                // Revit never became idle. Abandon it so the handler skips it
                // rather than doing work whose answer nobody is waiting for.
                job.Abandon();
                return Json.Error("revit_busy",
                    "Revit is busy and did not take the request. A dialog may be open, " +
                    "or a command may be running. Finish what is open in Revit and ask again.");
            }

            if (!job.WaitForFinish(_operationTimeout))
            {
                // It IS running - Revit picked it up. Nothing can safely
                // interrupt it, and calling this "busy" would invite a retry of
                // something already in progress.
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
                try
                {
                    job.Finish(RevitOperations.Run(app, job.Request));
                }
                catch (Exception ex)
                {
                    // Never let one bad request throw out of here: this runs on
                    // Revit's own thread, and an escaping exception is Revit's
                    // problem, not just Heron's.
                    _log("Operation failed: " + ex);
                    job.Finish(Json.Error("operation_failed", ex.Message));
                }
            }
        }

        /// <summary>One request, and the two signals its caller waits on.</summary>
        private sealed class RevitJob
        {
            private readonly ManualResetEventSlim _started = new ManualResetEventSlim(false);
            private readonly ManualResetEventSlim _finished = new ManualResetEventSlim(false);
            private volatile bool _abandoned;

            public RevitJob(string request) { Request = request; }

            public string Request { get; private set; }
            public string Response { get; private set; }
            public bool IsAbandoned { get { return _abandoned; } }

            public void MarkStarted() { _started.Set(); }

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
