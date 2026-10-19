// Heron-Agent:  none
// Heron-Step:   14
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Stops a fragment that will not stop by itself, and says so before the
    /// dispatcher gives up waiting for it - D-129.
    ///
    /// THE HOLE THIS CLOSES. A fragment runs on Revit's own thread, inside
    /// Execute, and nothing else runs there until it returns. One that loops
    /// for ever never returns: Revit freezes, the dispatcher stops waiting
    /// after revit.operationTimeoutSeconds and answers `still_running`, and
    /// the loop goes on until somebody ends Revit - unsaved model and all.
    /// The stop flag could not help either: it was read only BEFORE a job
    /// started, and nothing could set it (D-46).
    ///
    /// HOW IT REACHES THE LOOP. Revit gives no way into its own thread, so
    /// the fragment is made to look. HeronStackGuard puts a call to Check()
    /// at the head of every loop body and at the entry of every block-bodied
    /// method and lambda, so a fragment cannot go round once more, or call
    /// once more, without asking this clock. Check() throws when the job's
    /// time is up or when Heron was told to stop while the job ran.
    ///
    /// A THROW IS NOT ENOUGH ON ITS OWN, because many fragments catch
    /// Exception. So the trip is STICKY: once tripped, every later Check() in
    /// the same job throws again, and RevitFragment asks Tripped after the
    /// script whether it threw or not - a fragment that swallowed the stop
    /// and returned normally is rolled back all the same.
    ///
    /// THE DEADLINE IS SHORTER THAN THE DISPATCHER'S WAIT, on purpose. It is
    /// the operation limit minus a margin (BudgetFor), counted from the
    /// moment the job reached RevitFragment, so the rollback and the answer
    /// "stopped, rolled back" arrive while somebody is still waiting for
    /// them. A clock that tripped after the dispatcher had answered
    /// `still_running` would be telling the truth to nobody.
    ///
    /// ONLY FRAGMENT CODE IS TIMED. Checks count only inside Watch(), which
    /// RunScript opens around the script. Afterwards the add-in may still
    /// enumerate a lazy sequence the fragment left, and a throw there - after
    /// the group was kept - would report a failure for a change already in
    /// the model. That read stays untimed, as it always was.
    ///
    /// WHAT IT HONESTLY CANNOT DO. It acts only at a check. A single long
    /// Revit API call - Regenerate, LoadFamily, an export - has no check
    /// inside it and finishes first. An expression-bodied lambda and a LINQ
    /// query carry no check either (HeronStackGuard says why), and nor does a
    /// loop made with goto. It times Heron's fragments, never Revit itself.
    ///
    /// AND ONE PLACE A THROW WOULD LAND BADLY: a fragment lambda that REVIT
    /// calls back, an event handler, would throw into Revit's own code once
    /// the clock had tripped. Read 2026-10-17: no fragment subscribes to a
    /// Revit event - two mention Idling in a comment - and a fragment cannot
    /// declare the types Revit's other callbacks need (D-28). The first that
    /// subscribes should be read with this paragraph.
    ///
    /// PUBLIC AND TOP LEVEL, like HeronFragmentGlobals and for its reason: a
    /// script compiles into its own assembly and can reach only a public type
    /// that is not nested inside an internal one. It touches no Revit type,
    /// so tests/Heron.StackGuard.TestHost links it by source and runs a real
    /// runaway script against it with no Revit at all.
    /// </summary>
    public static class HeronRunClock
    {
        private const int NotTripped = 0;
        private const int OutOfTime = 1;
        private const int StoppedByRequest = 2;

        // How long the dispatcher waits for one job, handed in by the
        // dispatcher so there is ONE reading of revit.operationTimeoutSeconds.
        // Two readers of one key can disagree after heron.config is edited
        // mid-session, and then the clock could trip after the wait it exists
        // to beat. In TimeSpan ticks; sixty seconds is HeronConfig's default.
        private static long _limit = TimeSpan.FromSeconds(60).Ticks;

        // The job being timed, if any. Every field is read on whatever thread
        // runs fragment code and written on Revit's, so all of them move
        // through Volatile or Interlocked.
        private static long _deadline = long.MaxValue;   // Stopwatch timestamp
        private static long _budget;                     // TimeSpan ticks, for the sentence
        private static long _stopsAtBegin;
        private static int _watching;
        private static int _paused;
        private static long _pausedAt;
        private static int _tripped;
        private static int _job;

        /// <summary>
        /// The dispatcher's own wait for one job. Set once, by the dispatcher,
        /// from the value it waits with.
        /// </summary>
        internal static TimeSpan OperationLimit
        {
            get { return TimeSpan.FromTicks(Interlocked.Read(ref _limit)); }
            set { Interlocked.Exchange(ref _limit, Math.Max(TimeSpan.FromSeconds(1).Ticks, value.Ticks)); }
        }

        /// <summary>
        /// The time a job is given: the limit, less a margin for rolling back
        /// and answering.
        ///
        /// A SIXTH OF THE LIMIT, AT LEAST TWO SECONDS, NEVER MORE THAN HALF.
        /// Sixty seconds gives a job fifty and leaves ten for the rollback and
        /// the reply - a large group can take seconds to undo, and the margin
        /// has to cover it. The floor keeps that room on a short limit; the
        /// ceiling keeps a one-second limit from giving a job nothing.
        /// Derived, not a setting: a second key would be a second thing to
        /// keep in step with the first.
        /// </summary>
        internal static TimeSpan BudgetFor(TimeSpan limit)
        {
            var margin = Math.Max(TimeSpan.FromSeconds(2).Ticks, limit.Ticks / 6);
            margin = Math.Min(margin, limit.Ticks / 2);
            return TimeSpan.FromTicks(limit.Ticks - margin);
        }

        /// <summary>
        /// A job begins: start its clock with the budget the operation limit
        /// allows. Disposing it ends the job and clears every trace of it.
        /// </summary>
        internal static IDisposable Begin()
        {
            return Begin(BudgetFor(OperationLimit));
        }

        /// <summary>
        /// A job begins with an explicit budget - the test host's 200 ms, and
        /// the shape the add-in's own Begin() reduces to.
        ///
        /// THE STOP COUNT IS TAKEN HERE, AND THAT IS WHAT LETS READS GO ON.
        /// A stop asked for BEFORE the job began is the gate's business: it
        /// refuses anything that changes the model and lets a read through,
        /// because reading is how somebody finds out what went wrong
        /// (RevitOperations.Gate). Only a stop asked for DURING the job is
        /// this clock's, so a read run while Heron is stopped is not tripped
        /// at its first check.
        /// </summary>
        internal static IDisposable Begin(TimeSpan budget)
        {
            var job = Interlocked.Increment(ref _job);
            Volatile.Write(ref _tripped, NotTripped);
            Volatile.Write(ref _watching, 0);
            Volatile.Write(ref _paused, 0);
            Interlocked.Exchange(ref _budget, budget.Ticks);
            Interlocked.Exchange(ref _stopsAtBegin, HeronStop.Requests);
            Interlocked.Exchange(ref _deadline, Stopwatch.GetTimestamp() + ToTimestamp(budget));
            return new Ending(job);
        }

        /// <summary>
        /// The same job runs its fragment a second time - D-142: the modeller
        /// pressed Delete in the delete box, and the change is run again to be
        /// kept. Its budget starts again from now, the same budget, for this job
        /// only. Safe because the dispatcher keeps a whole operation timeout of
        /// waiting after the last moment a Delete is taken (RevitDispatcher
        /// .AnswerBy), so this answer still arrives first. A trip already made
        /// is not undone: the caller never runs it again after one.
        /// </summary>
        internal static void Again()
        {
            if (Tripped) return;
            Interlocked.Exchange(ref _deadline,
                Stopwatch.GetTimestamp() + ToTimestamp(TimeSpan.FromTicks(Interlocked.Read(ref _budget))));
        }

        /// <summary>
        /// Fragment code is about to run: checks count from now until this is
        /// disposed. RunScript opens one around the script.
        /// </summary>
        internal static IDisposable Watch()
        {
            Interlocked.Increment(ref _watching);
            return new Unwatching(Volatile.Read(ref _job));
        }

        /// <summary>
        /// The one call HeronStackGuard injects. Returns at once while nothing
        /// is being timed, and throws once the job is out of time or Heron was
        /// told to stop - and goes on throwing, so a fragment that catches the
        /// first cannot simply loop on.
        /// </summary>
        public static void Check()
        {
            if (Volatile.Read(ref _watching) <= 0) return;

            var why = Volatile.Read(ref _tripped);
            if (why == NotTripped)
            {
                if (HeronStop.Requests != Interlocked.Read(ref _stopsAtBegin))
                    why = Trip(StoppedByRequest);
                else if (Volatile.Read(ref _paused) == 0
                         && Stopwatch.GetTimestamp() > Interlocked.Read(ref _deadline))
                    why = Trip(OutOfTime);
                else
                    return;
            }

            throw new RunStopped("Heron stopped this job: it " + Cause(why));
        }

        /// <summary>
        /// Hold the clock while a person is being asked something, from
        /// inside a job: a prompt, a Keep or Discard question, any window
        /// shown on Revit's thread. Dispose it when the answer is in.
        ///
        /// A PROMPT BLOCKS THE THREAD THE CHECKS RUN ON, so the deadline goes
        /// on passing while the person reads, and the first check after they
        /// answer would stop a job whose only delay was them. Time spent
        /// paused is added back to the deadline. A stop asked for meanwhile
        /// still trips the next check: a pause holds the time, never the stop.
        ///
        /// NOTHING ON THE RUN PATH SHOWS SUCH A PROMPT TODAY - read 2026-10-17,
        /// every TaskDialog and window in the add-in is a ribbon command's,
        /// and those cannot run while Execute holds the thread. It is public
        /// so that the first one, in the add-in or in a fragment, is a using
        /// block rather than a change to this class. And it does NOT pause the
        /// dispatcher, which stops waiting on its own clock - a long answer
        /// still reaches the chat as `still_running`.
        ///
        /// A KEEP OR DISCARD QUESTION ASKED AFTER THE SCRIPT NEEDS NO PAUSE.
        /// RevitFragment's own box for a DRAFT's one run (D-119) is shown once
        /// the fragment has returned and its transaction committed - outside
        /// Watch(), where no check counts - so it cannot trip the clock however
        /// long the person takes; and the dispatcher's longer wait for it is
        /// D-119's own, through AwaitingModeller. Read, not assumed.
        /// </summary>
        public static IDisposable Pause()
        {
            var job = Volatile.Read(ref _job);
            if (Interlocked.Increment(ref _paused) == 1)
                Interlocked.Exchange(ref _pausedAt, Stopwatch.GetTimestamp());
            return new Unpausing(job);
        }

        /// <summary>Did the clock stop this job, whatever the fragment did with the throw?</summary>
        internal static bool Tripped
        {
            get { return Volatile.Read(ref _tripped) != NotTripped; }
        }

        /// <summary>True when it was a stop that tripped it, rather than the time.</summary>
        internal static bool StoppedByPerson
        {
            get { return Volatile.Read(ref _tripped) == StoppedByRequest; }
        }

        /// <summary>
        /// Why, as the end of a sentence that begins with the job's name - or
        /// null when nothing tripped.
        /// </summary>
        internal static string Why
        {
            get
            {
                var why = Volatile.Read(ref _tripped);
                return why == NotTripped ? null : Cause(why);
            }
        }

        private static string Cause(int why)
        {
            if (why == StoppedByRequest)
                return "was stopped part way through, because Heron was told to stop while it ran.";

            var budget = TimeSpan.FromTicks(Interlocked.Read(ref _budget));
            var limit = OperationLimit;
            return "ran out of time and was stopped after " + Seconds(budget)
                 + " - its limit, set inside the " + Seconds(limit) + " Heron waits for Revit"
                 + " so that this answer arrives first. A loop that never ends is the usual"
                 + " cause; a job that really needs longer can be given it with"
                 + " revit.operationTimeoutSeconds in heron.config.";
        }

        private static string Seconds(TimeSpan span)
        {
            return span.TotalSeconds >= 10
                ? Math.Round(span.TotalSeconds).ToString(CultureInfo.InvariantCulture) + " seconds"
                : span.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture) + " seconds";
        }

        private static int Trip(int why)
        {
            Interlocked.CompareExchange(ref _tripped, why, NotTripped);
            return Volatile.Read(ref _tripped);
        }

        // Through double: TimeSpan ticks times Stopwatch.Frequency overflows a
        // long for a limit of under an hour where the frequency is 1 GHz.
        private static long ToTimestamp(TimeSpan span)
        {
            return (long)(span.TotalSeconds * Stopwatch.Frequency);
        }

        /// <summary>Ends the job it began, and only that job.</summary>
        private sealed class Ending : IDisposable
        {
            private readonly int _of;
            private int _done;

            public Ending(int job) { _of = job; }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _done, 1) != 0) return;
                if (Volatile.Read(ref _job) != _of) return;
                Volatile.Write(ref _watching, 0);
                Volatile.Write(ref _paused, 0);
                Volatile.Write(ref _tripped, NotTripped);
                Interlocked.Exchange(ref _deadline, long.MaxValue);
            }
        }

        private sealed class Unwatching : IDisposable
        {
            private readonly int _of;
            private int _done;

            public Unwatching(int job) { _of = job; }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _done, 1) != 0) return;
                if (Volatile.Read(ref _job) == _of) Interlocked.Decrement(ref _watching);
            }
        }

        private sealed class Unpausing : IDisposable
        {
            private readonly int _of;
            private int _done;

            public Unpausing(int job) { _of = job; }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _done, 1) != 0) return;
                if (Volatile.Read(ref _job) != _of) return;
                if (Interlocked.Decrement(ref _paused) != 0) return;

                var away = Stopwatch.GetTimestamp() - Interlocked.Read(ref _pausedAt);
                if (Interlocked.Read(ref _deadline) != long.MaxValue)
                    Interlocked.Add(ref _deadline, away);
            }
        }

        /// <summary>
        /// What Check() throws. An Exception, so a fragment's catch can take
        /// it - which is why the trip is sticky and Tripped is asked after the
        /// script. Never named by a fragment, so it need not be public.
        /// </summary>
        private sealed class RunStopped : Exception
        {
            public RunStopped(string message) : base(message) { }
        }
    }
}
