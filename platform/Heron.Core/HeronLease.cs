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
    /// IT MUST NEVER BLOCK A ROLLBACK. docs/25 is explicit - cleanup always
    /// wins over the lease, or an interrupted transaction is stranded. That
    /// holds here by construction rather than by a special case: the lease is
    /// checked once, when a request arrives, and a rollback happens INSIDE a
    /// request that has already been admitted. There is no path by which
    /// cleanup asks permission.
    /// </summary>
    public static class HeronLease
    {
        /// <summary>Config key for how long a lease survives without renewal.</summary>
        public const string LeaseMinutesKey = "bridge.leaseMinutes";

        private static readonly object Gate = new object();

        private static string _holder;
        private static DateTime _expiresUtc;

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
                var minutes = HeronConfig.Load().GetInt(LeaseMinutesKey, 5);
                return TimeSpan.FromMinutes(Math.Max(1, minutes));
            }
        }

        /// <summary>Who holds it, or null if it is free. Expiry counts as free.</summary>
        public static string Holder
        {
            get
            {
                lock (Gate)
                {
                    Expire();
                    return _holder;
                }
            }
        }

        /// <summary>Seconds until it lapses. Zero when free.</summary>
        public static double SecondsRemaining
        {
            get
            {
                lock (Gate)
                {
                    Expire();
                    if (_holder == null) return 0;
                    var left = (_expiresUtc - DateTime.UtcNow).TotalSeconds;
                    return left < 0 ? 0 : left;
                }
            }
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

            lock (Gate)
            {
                Expire();

                if (_holder == null || _holder == clientId)
                {
                    _holder = clientId;
                    _expiresUtc = DateTime.UtcNow + Lifetime;
                    return new LeaseDecision
                    {
                        Granted = true,
                        Holder = clientId,
                        SecondsRemaining = (_expiresUtc - DateTime.UtcNow).TotalSeconds,
                    };
                }

                var left = (_expiresUtc - DateTime.UtcNow).TotalSeconds;
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
        /// Give it up. Only the holder may, so a second chat cannot release
        /// somebody else's lease and then take it.
        /// </summary>
        public static bool Release(string clientId)
        {
            if (string.IsNullOrEmpty(clientId)) return false;

            lock (Gate)
            {
                Expire();
                if (_holder != clientId) return false;
                _holder = null;
                _expiresUtc = DateTime.MinValue;
                return true;
            }
        }

        /// <summary>
        /// Drop it unconditionally. For the add-in disconnecting this Revit -
        /// the user pressed the button, so nothing should still be holding it.
        /// </summary>
        public static void Clear()
        {
            lock (Gate)
            {
                _holder = null;
                _expiresUtc = DateTime.MinValue;
            }
        }

        /// <summary>Caller must hold Gate.</summary>
        private static void Expire()
        {
            if (_holder != null && DateTime.UtcNow >= _expiresUtc)
            {
                _holder = null;
                _expiresUtc = DateTime.MinValue;
            }
        }
    }
}
