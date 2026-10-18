// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Heron.Revit.Addin
{
    /// <summary>The answer to "may this chat use this model now?".</summary>
    internal sealed class HeronModelHoldDecision
    {
        /// <summary>True: run it. False: refused, nothing sent to the model.</summary>
        public bool Granted;

        /// <summary>True when the model was free and this chat holds it from now on.</summary>
        public bool NewlyHeld;

        /// <summary>The refusal, in the modeller's words. Null when granted.</summary>
        public string Message;
    }

    /// <summary>
    /// One model, one chat at a time - D-141.
    ///
    /// THE OWNER'S DESIGN, 2026-10-18, in his words: "One chat can access
    /// multiple models. The same model cannot be accessed by multiple chats at
    /// the same time. After one finishes, then the next goes; then the first
    /// can come back." Chat 2 never has to close, archive or cancel chat 1.
    ///
    /// WHY THE D-22 LEASE DID NOT ALREADY DO THIS. The lease is per Revit and
    /// per PERSON: every chat on the owner's PC sends the same client id, so it
    /// never told his own chats apart, and two of them could work one model at
    /// once and share what the add-in kept for one. The lease stays exactly as
    /// it is; this holds a MODEL for a CONVERSATION (HeronConversation).
    ///
    /// HELD when a chat's request runs against the model - a read or a change
    /// - and renewed by every request it sends there. A request from another
    /// chat for a held model is REFUSED BEFORE ANYTHING RUNS, read or change,
    /// and told when it frees. One chat may hold several models at once.
    ///
    /// FREED:
    ///   * after QUIET minutes with no request from the holding chat
    ///     (bridge.modelHoldMinutes, 3 by default) - which is also what frees
    ///     a chat that was closed or archived;
    ///   * when the chat says it is done - revit_release, Release;
    ///   * with Free this model on the Heron ribbon - Free, whoever holds it;
    ///   * when the model closes - Free, from DocumentClosing;
    ///   * when Revit closes - held in memory only, never in heron.config.
    ///
    /// THE MODEL IS NAMED BY ITS KEY, never its title: RevitOperations.DocumentKey,
    /// the CreationGUID, path and title the rollback witness and the Keep grant
    /// already key a model by. A title is display text (RevitFragment's
    /// document choice says why).
    ///
    /// REVIT'S THREAD STILL RUNS ONE THING AT A TIME. This decides who may ask;
    /// it does not make two jobs run together.
    ///
    /// THIS FILE NAMES NO AUTODESK TYPE: the clock and the quiet time are
    /// passed in, so tests/Heron.ModelHold.TestHost runs every rule here on any
    /// machine. Whether Revit honours it is NEEDS-CHECKING Group HK's.
    /// </summary>
    internal sealed class HeronModelHold
    {
        /// <summary>The heron.config key for the quiet time, in minutes.</summary>
        public const string MinutesKey = "bridge.modelHoldMinutes";

        /// <summary>The owner's three minutes.</summary>
        public const int DefaultMinutes = 3;

        /// <summary>The ribbon button's words - one place, so the refusal and the button agree.</summary>
        public const string ButtonName = "Free this model";

        /// <summary>
        /// The holds this Revit has. Read and changed on Revit's own thread - a
        /// job, a closing model, the ribbon button - and locked all the same,
        /// as HeronKeepGrant is: a lock costs nothing here.
        /// </summary>
        internal static readonly HeronModelHold Session = new HeronModelHold();

        private sealed class Hold
        {
            public string Conversation;
            public string Title;
            public DateTime LastUtc;
        }

        private readonly object _gate = new object();
        private readonly Dictionary<string, Hold> _holds =
            new Dictionary<string, Hold>(StringComparer.Ordinal);

        /// <summary>The quiet time for a configured number of minutes - never under one.</summary>
        public static TimeSpan QuietFor(int minutes)
        {
            return TimeSpan.FromMinutes(Math.Max(1, minutes));
        }

        /// <summary>
        /// Claims the model for this chat, or renews its hold - asked once per
        /// request, before it runs.
        ///
        /// GRANTED, NOTHING HELD: a request with no model (the operation says
        /// what is missing), and a request from an older client with no
        /// conversation on a FREE model - there is nobody to hold it for.
        /// REFUSED: anybody but the holder, while the hold stands - an older
        /// client included, because it cannot be shown to be the holder.
        /// </summary>
        /// <param name="conversation">The request's conversation, or null.</param>
        /// <param name="model">RevitOperations.DocumentKey, or null.</param>
        /// <param name="title">The model's title, for the refusal and the log.</param>
        public HeronModelHoldDecision Claim(string conversation, string model, string title,
                                            DateTime nowUtc, TimeSpan quiet)
        {
            if (string.IsNullOrEmpty(model)) return new HeronModelHoldDecision { Granted = true };

            lock (_gate)
            {
                Sweep(nowUtc, quiet);

                Hold hold;
                _holds.TryGetValue(model, out hold);

                var asker = Usable(conversation);
                if (hold != null && !string.Equals(hold.Conversation, asker, StringComparison.Ordinal))
                {
                    var left = quiet - (nowUtc - hold.LastUtc);
                    return new HeronModelHoldDecision
                    {
                        Granted = false,
                        Message = InUse(string.IsNullOrEmpty(title) ? hold.Title : title, quiet, left),
                    };
                }

                if (asker == null) return new HeronModelHoldDecision { Granted = true };

                var fresh = hold == null;
                if (fresh)
                {
                    hold = new Hold { Conversation = asker };
                    _holds[model] = hold;
                }
                hold.LastUtc = nowUtc;
                if (!string.IsNullOrEmpty(title)) hold.Title = title;
                return new HeronModelHoldDecision { Granted = true, NewlyHeld = fresh };
            }
        }

        /// <summary>
        /// The same question as Claim, ASKED WITHOUT CLAIMING - for a request
        /// that only looks, `revit_health`'s count. Refused exactly as Claim
        /// refuses, so looking is never a way round another chat's hold; on a
        /// free model it runs and holds nothing, and a hold this chat already
        /// has is not renewed. The lease's own rule (BridgeServer's `info`):
        /// asking "who has this?" must not be the act of claiming it.
        /// </summary>
        public HeronModelHoldDecision Check(string conversation, string model, string title,
                                            DateTime nowUtc, TimeSpan quiet)
        {
            if (string.IsNullOrEmpty(model)) return new HeronModelHoldDecision { Granted = true };

            lock (_gate)
            {
                Sweep(nowUtc, quiet);

                Hold hold;
                _holds.TryGetValue(model, out hold);
                if (hold != null
                    && !string.Equals(hold.Conversation, Usable(conversation), StringComparison.Ordinal))
                {
                    return new HeronModelHoldDecision
                    {
                        Granted = false,
                        Message = InUse(string.IsNullOrEmpty(title) ? hold.Title : title, quiet,
                                        quiet - (nowUtc - hold.LastUtc)),
                    };
                }
                return new HeronModelHoldDecision { Granted = true };
            }
        }

        /// <summary>
        /// The request has ended: the quiet time counts from now, not from when
        /// it started, so a job that took four minutes is not a quiet four.
        /// Only the holder's own request renews it.
        /// </summary>
        public void Touch(string conversation, string model, DateTime nowUtc)
        {
            var asker = Usable(conversation);
            if (asker == null || string.IsNullOrEmpty(model)) return;
            lock (_gate)
            {
                Hold hold;
                if (_holds.TryGetValue(model, out hold)
                    && string.Equals(hold.Conversation, asker, StringComparison.Ordinal))
                {
                    hold.LastUtc = nowUtc;
                }
            }
        }

        /// <summary>
        /// The chat says it is done - revit_release. Frees THIS chat's hold on
        /// one model, or on every model when `model` is null. Never another
        /// chat's: the same rule as HeronLease.Release. Returns the titles
        /// freed, empty when there was nothing of this chat's to free - a hold
        /// already past its quiet time is not named as freed now.
        /// </summary>
        public List<string> Release(string conversation, string model, DateTime nowUtc, TimeSpan quiet)
        {
            var freed = new List<string>();
            var asker = Usable(conversation);
            if (asker == null) return freed;

            lock (_gate)
            {
                Sweep(nowUtc, quiet);
                var mine = new List<string>();
                foreach (var entry in _holds)
                {
                    if (!string.Equals(entry.Value.Conversation, asker, StringComparison.Ordinal)) continue;
                    if (model != null && !string.Equals(entry.Key, model, StringComparison.Ordinal)) continue;
                    mine.Add(entry.Key);
                }
                foreach (var key in mine)
                {
                    freed.Add(Named(_holds[key].Title));
                    _holds.Remove(key);
                }
            }
            freed.Sort(StringComparer.OrdinalIgnoreCase);
            return freed;
        }

        /// <summary>
        /// Frees the model WHOEVER holds it - Free this model on the ribbon,
        /// and a model closing. Returns the holder's conversation, or null when
        /// it was not held (or its hold had already lapsed).
        /// </summary>
        public string Free(string model, DateTime nowUtc, TimeSpan quiet)
        {
            if (string.IsNullOrEmpty(model)) return null;
            lock (_gate)
            {
                Sweep(nowUtc, quiet);
                Hold hold;
                if (!_holds.TryGetValue(model, out hold)) return null;
                _holds.Remove(model);
                return hold.Conversation;
            }
        }

        /// <summary>Every hold gone - the bridge disconnected from the ribbon.</summary>
        public void Clear()
        {
            lock (_gate) { _holds.Clear(); }
        }

        /// <summary>Who holds the model now, or null when it is free.</summary>
        public string Holder(string model, DateTime nowUtc, TimeSpan quiet)
        {
            if (string.IsNullOrEmpty(model)) return null;
            lock (_gate)
            {
                Sweep(nowUtc, quiet);
                Hold hold;
                return _holds.TryGetValue(model, out hold) ? hold.Conversation : null;
            }
        }

        /// <summary>How many models are held now.</summary>
        public int Count(DateTime nowUtc, TimeSpan quiet)
        {
            lock (_gate)
            {
                Sweep(nowUtc, quiet);
                return _holds.Count;
            }
        }

        /// <summary>
        /// The refusal - the owner's sentence, with when it frees at the latest.
        /// It says nothing was sent, because "did it half-do something?" is the
        /// first thing anybody refused wants to know (docs/25 section 3).
        /// </summary>
        public static string InUse(string title, TimeSpan quiet, TimeSpan left)
        {
            var minutes = (int)Math.Ceiling(Math.Max(0.0, left.TotalMinutes));
            if (minutes < 1) minutes = 1;
            return "Model '" + Named(title) + "' is in use by another chat. It is free when that chat "
                 + "says it is done, after " + Minutes(quiet) + " quiet minutes, or with "
                 + ButtonName + " on the Heron ribbon - at the latest in about "
                 + minutes.ToString(CultureInfo.InvariantCulture) + " minute"
                 + (minutes == 1 ? "" : "s") + " from now. Nothing was sent to the model.";
        }

        /// <summary>The quiet time as the modeller reads it: "3".</summary>
        public static string Minutes(TimeSpan quiet)
        {
            return ((int)Math.Round(quiet.TotalMinutes)).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// A conversation that can hold a model: one a client sent. A key made
        /// for one request (HeronConversation.ChatOf) holds nothing - its chat
        /// ends with the request.
        /// </summary>
        private static string Usable(string conversation)
        {
            if (string.IsNullOrEmpty(conversation) || HeronConversation.IsOneRequest(conversation))
                return null;
            return conversation;
        }

        /// <summary>
        /// Drops every hold past its quiet time. Caller holds _gate.
        ///
        /// A CLOCK PUT BACK restarts a hold's quiet time rather than stretching
        /// it: one quiet period from now is the most it can then last, so a PC
        /// clock moved an hour back cannot keep a model held for an hour.
        /// </summary>
        private void Sweep(DateTime nowUtc, TimeSpan quiet)
        {
            List<string> lapsed = null;
            foreach (var entry in _holds)
            {
                var idle = nowUtc - entry.Value.LastUtc;
                if (idle < TimeSpan.Zero)
                {
                    entry.Value.LastUtc = nowUtc;
                    continue;
                }
                if (idle <= quiet) continue;
                if (lapsed == null) lapsed = new List<string>();
                lapsed.Add(entry.Key);
            }
            if (lapsed == null) return;
            foreach (var key in lapsed) _holds.Remove(key);
        }

        private static string Named(string title)
        {
            return string.IsNullOrEmpty(title) ? "(unnamed)" : title;
        }
    }
}
