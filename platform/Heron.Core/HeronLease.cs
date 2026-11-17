// Heron-Agent:  HERON-SES-LEA-004
// Heron-Step:   6
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Threading;

namespace Heron.Core
{
    /// <summary>The answer to "may I use this Revit?".</summary>
    public sealed class LeaseDecision
    {
        public bool Granted;
        public string Holder;
        public double SecondsRemaining;

        /// <summary>
        /// Granted to a caller that did not hold it a moment ago - taken, not
        /// renewed. A request refused further in (the model hold, D-141) gives
        /// back only a lease it took this way (row 5b-1662).
        /// </summary>
        public bool Fresh;

        /// <summary>The refusal, in the user's terms.</summary>
        public string Message;
    }

    /// <summary>
    /// One Revit is one door. This is the lock on it.
    ///
    /// THE PROBLEM IT REPLACES, and it was the working bridge's known
    /// limitation before it was Heron's: two chats on one Revit FIGHT.
    /// Whichever speaks last takes the session and cuts the other off, mid-job.
    /// The user's own standing rule was to work around it by hand - *"don't go
    /// to Revit, another session is running"* - which is a person being used as
    /// a lock because the information was never written down anywhere they
    /// could see it.
    ///
    /// Chopping a READ is harmless; each chat simply reconnects. Chopping a
    /// MODIFY mid-transaction is not, and that is why docs/25 defers this to
    /// "Phase 1 with writes" - it becomes a hazard exactly when Heron can
    /// change a model, which is now.
    ///
    /// PER PROCESS, NOT PER DOCUMENT. The contention is at the pipe, not at the
    /// model, so two chats on one Revit collide even when each is discussing a
    /// different project. Two models open does not make two sessions. The
    /// lease is therefore scoped to the Revit process - the same unit as the
    /// pipe, the discovery file and the session binding.
    ///
    /// REFUSED, NOT QUEUED. docs/25 weighs all three options and rejects a
    /// queue on purpose: an invisible queue means a command runs minutes later
    /// against a model that has since changed, which is the stale-read problem
    /// with extra steps. A refusal is immediate and truthful.
    ///
    /// SHORT AND RENEWABLE, so a chat that dies does not hold a Revit hostage.
    /// Every request renews it, so an active chat never loses it; a chat that
    /// stops asking releases it by simply going quiet.
    ///
    /// AND HELD WHILE A JOB WAITS FOR THE MODELLER - row 5b-1745. A request
    /// renews the lease as it ARRIVES, and a job that asks the modeller - the
    /// Keep or Discard box, the delete box, a pick (Q-86) - can wait longer
    /// than the lease lives: revit.busyTimeoutSeconds + operationTimeoutSeconds
    /// + askTimeoutSeconds, over six minutes as installed, against five. So a
    /// box left open lapsed the lease under the very chat it was asking for,
    /// and a chat with another client id could take this Revit mid-wait. The
    /// add-in now Holds the lease for the job's own client from the moment the
    /// job asks (RevitDispatcher.AwaitingModeller) until the job ends, and
    /// while it is held it does not lapse. When the hold ends the lease's
    /// clock starts again from that moment, as a request arriving starts it:
    /// the quiet time is counted from the end of the wait, not from before it.
    /// A hold never TAKES a lease - only one its client holds already - and
    /// changes nothing about who may: Release and Clear end it as they always
    /// have, a hold included.
    ///
    /// IT MUST NEVER BLOCK A ROLLBACK. docs/25 is explicit - cleanup always
    /// wins over the lease, or an interrupted transaction is stranded. That
    /// holds here by construction rather than by a special case: the lease is
    /// checked once, when a request arrives, and a rollback happens INSIDE a
    /// request that has already been admitted. There is no path by which
    /// cleanup asks permission - a hold only keeps the lease, it asks nothing.
    ///
    /// THE RULE IS HeronLeaseState, with the clock and the lifetime passed
    /// in; this class is the one Revit's process uses, on the real clock and
    /// bridge.leaseMinutes. tests/Heron.Kernel.TestHost runs the rule on a
    /// clock of its own - a lease outlasting its lifetime cannot be shown in
    /// a test that has to wait five real minutes for it.
    /// </summary>
    public static class HeronLease
    {
        /// <summary>Config key for how long a lease survives without renewal.</summary>
        public const string LeaseMinutesKey = "bridge.leaseMinutes";

        /// <summary>The minutes a lease lives when heron.config says nothing.</summary>
        public const int DefaultMinutes = 5;

        /// <summary>The one lease this Revit process has.</summary>
        private static readonly HeronLeaseState Session = new HeronLeaseState();

        /// <summary>
        /// How long a lease lives without being renewed.
        ///
        /// The tradeoff, stated so the number can be argued with: too LONG and
        /// a chat that died holds a Revit nobody can use; too SHORT and a chat
        /// loses its hold between two of the user's own messages, which is the
        /// takeover this exists to prevent, arriving on a timer instead.
        ///
        /// Five minutes is chosen for the human gap - somebody reads an answer,
        /// thinks, and asks the next thing. It is longer than
        /// bridge.idleReleaseMinutes on purpose: the pipe closing when idle
        /// does not mean the chat has gone.
        /// </summary>
        public static TimeSpan Lifetime
        {
            get
            {
                var minutes = HeronConfig.Load().GetInt(LeaseMinutesKey, DefaultMinutes);
                return TimeSpan.FromMinutes(Math.Max(1, minutes));
            }
        }

        /// <summary>Who holds it, or null if it is free. Expiry counts as free.</summary>
        public static string Holder
        {
            get { return Session.Holder(DateTime.UtcNow); }
        }

        /// <summary>
        /// Seconds until it lapses. Zero when free. While a job waits for the
        /// modeller it cannot lapse at all, and this says the least it can then
        /// last: a whole lifetime, counted from the end of the wait.
        /// </summary>
        public static double SecondsRemaining
        {
            get { return Session.SecondsRemaining(DateTime.UtcNow, Lifetime); }
        }

        /// <summary>
        /// Claim the lease, or renew it if it is already yours.
        ///
        /// An anonymous caller - no client id - is granted nothing and told
        /// why. That is not pedantry: without an identity that survives
        /// reconnection there is no way to tell "the same chat coming back"
        /// from "a second chat arriving", and those two must not be confused.
        /// </summary>
        public static LeaseDecision Claim(string clientId)
        {
            return Session.Claim(clientId, DateTime.UtcNow, Lifetime);
        }

        /// <summary>
        /// Give it up. Only the holder may, so a second chat cannot release
        /// somebody else's lease and then take it. A hold standing on it ends
        /// with it.
        /// </summary>
        public static bool Release(string clientId)
        {
            return Session.Release(clientId, DateTime.UtcNow);
        }

        /// <summary>
        /// Drop it unconditionally. For the add-in disconnecting this Revit -
        /// the user pressed the button, so nothing should still be holding it,
        /// a job waiting for the modeller included.
        /// </summary>
        public static void Clear()
        {
            Session.Clear();
        }

        /// <summary>
        /// A job waiting for the modeller keeps its client's lease from lapsing
        /// until EndHold - row 5b-1745. Returns the hold, or 0 when nothing was
        /// held: no client id, or a lease that is free or another's at this
        /// moment. A hold never takes a lease.
        /// </summary>
        public static long Hold(string clientId)
        {
            return Session.Hold(clientId, DateTime.UtcNow);
        }

        /// <summary>
        /// The wait is over: the lease's clock starts again from now. A hold
        /// that Release or Clear already ended, or 0, changes nothing.
        ///
        /// NEVER THROWS, because a hold left standing is a lease that never
        /// lapses - the hostage this class exists to rule out. A lifetime that
        /// cannot be read is the default one.
        /// </summary>
        public static bool EndHold(long hold)
        {
            if (hold <= 0) return false;
            TimeSpan lifetime;
            try
            {
                lifetime = Lifetime;
            }
            catch (Exception)
            {
                lifetime = TimeSpan.FromMinutes(DefaultMinutes);
            }
            return Session.EndHold(hold, DateTime.UtcNow, lifetime);
        }
    }

    /// <summary>
    /// The lease's rule, with the clock and the lifetime passed in, so it can
    /// be run without waiting minutes of real time for it. HeronLease keeps the
    /// one instance a Revit process has; a test makes its own.
    ///
    /// Every member takes the lock: the lease is claimed on the bridge's
    /// listener threads and held and ended on Revit's own.
    /// </summary>
    public sealed class HeronLeaseState
    {
        private readonly object _gate = new object();

        private string _holder;
        private DateTime _expiresUtc;

        // How many jobs are waiting for the modeller on _holder's lease now -
        // 0 or 1 in practice, Revit's thread running one job at a time.
        private int _holds;

        // Which taking of the lease a hold belongs to: counted up each time the
        // lease is taken by a caller that did not hold it. A hold from an
        // earlier term - released, cleared or lapsed since - ends nothing now.
        private long _term;

        /// <summary>Who holds it, or null if it is free. Expiry counts as free.</summary>
        public string Holder(DateTime nowUtc)
        {
            lock (_gate)
            {
                Expire(nowUtc);
                return _holder;
            }
        }

        /// <summary>Whether a job is waiting for the modeller on this lease now.</summary>
        public bool Held(DateTime nowUtc)
        {
            lock (_gate)
            {
                Expire(nowUtc);
                return _holder != null && _holds > 0;
            }
        }

        /// <summary>
        /// Seconds until it lapses. Zero when free; while held, the lifetime -
        /// the least it can last, counted from the end of the wait.
        /// </summary>
        public double SecondsRemaining(DateTime nowUtc, TimeSpan lifetime)
        {
            lock (_gate)
            {
                Expire(nowUtc);
                if (_holder == null) return 0;
                if (_holds > 0) return lifetime.TotalSeconds;
                var left = (_expiresUtc - nowUtc).TotalSeconds;
                return left < 0 ? 0 : left;
            }
        }

        /// <summary>Claim the lease, or renew it if it is already yours - HeronLease.Claim.</summary>
        public LeaseDecision Claim(string clientId, DateTime nowUtc, TimeSpan lifetime)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return new LeaseDecision
                {
                    Granted = false,
                    Message = "This request carries no session id, so Heron cannot tell whether " +
                              "it is from the chat already using this Revit. Nothing was sent to " +
                              "Revit. Restart the chat's Heron connection.",
                };
            }

            lock (_gate)
            {
                Expire(nowUtc);

                if (_holder == null || _holder == clientId)
                {
                    var fresh = _holder == null;
                    if (fresh) _term++;
                    _holder = clientId;
                    _expiresUtc = nowUtc + lifetime;
                    return new LeaseDecision
                    {
                        Granted = true,
                        Holder = clientId,
                        SecondsRemaining = (_expiresUtc - nowUtc).TotalSeconds,
                        Fresh = fresh,
                    };
                }

                if (_holds > 0)
                {
                    // THE OTHER CHAT'S JOB IS WAITING FOR AN ANSWER IN REVIT,
                    // so it has no end time to give. Said rather than a number
                    // that would be wrong: what frees it is that answer, and
                    // then the lifetime with nothing more from that chat.
                    return new LeaseDecision
                    {
                        Granted = false,
                        Holder = _holder,
                        SecondsRemaining = lifetime.TotalSeconds,
                        Message = "This Revit is in use by another chat, so Heron has refused rather " +
                                  "than taking it over mid-job. Nothing was sent to Revit.\n" +
                                  "That chat's job is waiting for an answer in Revit - a box to " +
                                  "answer, or a click - so it frees up about " +
                                  Math.Ceiling(lifetime.TotalSeconds / 60.0) +
                                  " minute(s) after that is answered with no more activity there, " +
                                  "or immediately if that chat disconnects from the Heron button.",
                    };
                }

                var left = (_expiresUtc - nowUtc).TotalSeconds;
                if (left < 0) left = 0;

                return new LeaseDecision
                {
                    Granted = false,
                    Holder = _holder,
                    SecondsRemaining = left,
                    // Says what is happening, who to, and when it clears. The
                    // old behaviour said nothing at all - the other chat simply
                    // stopped working.
                    Message = "This Revit is in use by another chat, so Heron has refused rather " +
                              "than taking it over mid-job. Nothing was sent to Revit.\n" +
                              "It frees up after about " + Math.Ceiling(left / 60.0) +
                              " minute(s) without activity there, or immediately if that chat " +
                              "disconnects from the Heron button.",
                };
            }
        }

        /// <summary>
        /// Give it up - HeronLease.Release. Only the holder may; a hold standing
        /// on it ends with it.
        /// </summary>
        public bool Release(string clientId, DateTime nowUtc)
        {
            if (string.IsNullOrEmpty(clientId)) return false;

            lock (_gate)
            {
                Expire(nowUtc);
                if (_holder != clientId) return false;
                Free();
                return true;
            }
        }

        /// <summary>Drop it unconditionally, a hold included - HeronLease.Clear.</summary>
        public void Clear()
        {
            lock (_gate) { Free(); }
        }

        /// <summary>
        /// Keep the lease from lapsing while a job of this client's waits for
        /// the modeller. Returns the hold, or 0 when nothing was held - no
        /// client id, or a lease that is free or another's at this moment.
        /// NEVER TAKES A LEASE: a free one stays free, so this cannot become a
        /// way in that Claim does not already give.
        /// </summary>
        public long Hold(string clientId, DateTime nowUtc)
        {
            if (string.IsNullOrEmpty(clientId)) return 0;

            lock (_gate)
            {
                Expire(nowUtc);
                if (_holder == null || _holder != clientId) return 0;
                _holds++;
                return _term;
            }
        }

        /// <summary>
        /// The wait is over. When the last hold on the lease ends, its clock
        /// starts again from now, as a request arriving starts it. A hold from
        /// an earlier term - the lease released, cleared or taken anew since -
        /// changes nothing, so it can never lengthen somebody else's lease.
        /// </summary>
        public bool EndHold(long hold, DateTime nowUtc, TimeSpan lifetime)
        {
            if (hold <= 0) return false;

            lock (_gate)
            {
                if (_holder == null || hold != _term || _holds == 0) return false;
                _holds--;
                if (_holds == 0) _expiresUtc = nowUtc + lifetime;
                return true;
            }
        }

        /// <summary>Caller holds _gate. A held lease does not lapse.</summary>
        private void Expire(DateTime nowUtc)
        {
            if (_holder != null && _holds == 0 && nowUtc >= _expiresUtc) Free();
        }

        /// <summary>Caller holds _gate.</summary>
        private void Free()
        {
            _holder = null;
            _expiresUtc = DateTime.MinValue;
            _holds = 0;
        }
    }
}
