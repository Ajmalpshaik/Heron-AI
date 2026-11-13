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
    /// <summary>
    /// Whether Heron has seen anything change a model since its last kept
    /// change there - the bookkeeping behind `undo_guarded` (D-157), with nothing
    /// Revit-shaped in it.
    ///
    /// WHY IT HAS TO BE WATCHED. Revit's Undo takes the TOP of the list, and
    /// Revit has no API that names an entry and undoes that one, or that reads
    /// the list at all (proposal F29, measured). The list belongs to the
    /// model, shared with the person working in it: if they moved a wall after
    /// Heron's change, that wall is on top, and an Undo sent "for Heron" would
    /// take their wall. So Heron sends Undo only when it SAW its own change
    /// land - Revit counted elements in it - and has seen nothing reported in
    /// that model since. What it says is always what it SAW, never what the
    /// list holds: it cannot read the list.
    ///
    /// WHAT IT HEARS, from RevitFragment's permanent DocumentChanged listener
    /// and from RevitUndo:
    ///
    ///   Kept       a write job was assimilated, with what Revit counted in
    ///              it. One or more elements: its entry landed. NONE - a job
    ///              that opened no transaction, or one Revit counted nothing
    ///              in - left no entry Heron can vouch for, so there is
    ///              nothing of Heron's to undo (row 5b-1603). It is still
    ///              Heron's LAST job, so an older keep is no longer offered:
    ///              whether an empty group leaves an entry is not measured
    ///              (NEEDS-CHECKING Group NU), and "undo that" after it does
    ///              not mean the job before
    ///   Committed  any other transaction committed in that model - the
    ///              modeller's, another add-in's, or a Heron operation that
    ///              is not a kept write job (an isolate). Heron's entry is
    ///              covered from then on, for good: a later undo of the
    ///              covering change cannot be counted back reliably, so it is
    ///              never tried
    ///   Undone     Revit undid something there. Named as Heron's own entry
    ///              (or not named at all), Heron's is what went - whoever
    ///              pressed it. Named as anything else, Heron was wrong about
    ///              what was next, and says so from then on (row 5b-1608)
    ///   Redone     Revit redid something. Every undo after Heron's went is
    ///              counted, and only a redo that brings the count back to
    ///              none AND carries Heron's own name puts it back - two keeps
    ///              can share one name (row 5b-1606)
    ///   Synced     synchronized with central - Revit clears its undo list
    ///   Reloaded   Reload Latest - the same (row 5b-1609)
    ///   Renamed    saved under a new name or for the first time, so the
    ///              model's key moved: the entry moves with it and refuses
    ///   Doubt      Heron missed something - a change it could not read, a
    ///              rollback that did not hold, a job that left a transaction
    ///              open. It no longer knows what is on top, and refuses
    ///              (row 5b-1607)
    ///   Closed     the model closed - forgotten
    ///   Sent       Heron posted Undo. From then until Revit reports an undo
    ///              in that model the Undo is OUTSTANDING: it may still run,
    ///              and it takes whatever is on top when it does. So no keep
    ///              made there meanwhile is vouched for, no second Undo is
    ///              sent, and for the first SentWait no other Heron job starts
    ///              in any model. Time alone never clears it
    ///
    /// A COMMIT INSIDE HERON'S OWN JOB IS NOT A COVER, and the caller says
    /// which are (`ours`): the job's setup steps and its own transaction
    /// commit inside its group, and a group that rolls back leaves the list as
    /// it was. Only the job that is KEPT moves the top, and Kept says so.
    ///
    /// IT FAILS CLOSED. Every state but one refuses, and nothing that is
    /// unclear ever leads back to that one.
    ///
    /// EVERY TRANSITION IS LOGGED when a log is given - the event, the
    /// transaction names Revit reported and the state it left - so one run in
    /// a real Revit answers what Group NU asks: one undo event for an
    /// assimilated group or several, and under which names.
    ///
    /// SPLIT OUT SO IT CAN BE PROVED WITHOUT REVIT, the move
    /// HeronRollbackWitness makes: tests/Heron.UndoGuard.TestHost links it by
    /// source. Single-threaded by construction: every request and every
    /// DocumentChanged event arrives on Revit's own thread.
    /// </summary>
    internal sealed class HeronUndoGuard
    {
        /// <summary>The most models remembered at once; the oldest is forgotten first.</summary>
        internal const int MaxDocuments = 16;

        internal const string OnTop = "onTop";
        internal const string Covered = "covered";
        internal const string Undone = "undone";
        internal const string Sent = "sent";
        internal const string Synced = "synced";
        internal const string Reloaded = "reloaded";
        internal const string Nothing = "nothing";
        internal const string Unsure = "unsure";

        /// <summary>
        /// How long every other Heron job is HELD BACK after an Undo is sent.
        /// Revit runs a posted command once Heron's request returns; within
        /// this, a job in ANY model is refused before it runs - it could land
        /// on top of the undo list, or bring another model to the front, and
        /// Revit's Undo acts on whichever model is on screen. Past it jobs
        /// run again, but the Undo is not forgotten: see `Outstanding`.
        /// </summary>
        internal static readonly TimeSpan SentWait = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Why a job kept while an Undo is outstanding is not vouched for -
        /// said by `undo_guarded`'s refusal and on that job's kept reply.
        /// </summary>
        internal const string OutstandingWhy =
            "an Undo Heron sent earlier has not been seen to run; it may still run and take "
            + "this change";

        /// <summary>Heron's last kept job in one model, and where it stands.</summary>
        internal sealed class Entry
        {
            internal string Job;        // the fragment's name, for the sentence
            internal string Label;      // its undo entry, "Heron: <name>"
            internal DateTime KeptAt;   // local time, for "at 14:32"
            internal string State;
            internal string CoveredBy;  // the first transaction seen after it
            internal string Why;        // Unsure: what Heron missed, in a clause
            internal int Depth;         // Undone: undos Revit reported after Heron's own went
            internal bool Redoable;     // Undone: nothing has committed since, so Redo still holds it
            internal DateTime SentAt;   // Sent: when Undo was posted
            internal bool Twin;         // an earlier keep here carried the same Label
            internal HashSet<string> Earlier;   // the labels of the keeps before this one
            internal long Touched;
        }

        /// <summary>An Undo that was posted and has not been seen to run.</summary>
        private sealed class Posted
        {
            internal string Job;
            internal DateTime At;
        }

        private readonly Dictionary<string, Entry> _byDocument =
            new Dictionary<string, Entry>(StringComparer.Ordinal);

        // AN UNDO SENT AND NOT SEEN TO RUN, by model - apart from the entry,
        // because a keep REPLACES the entry and must not replace this (row
        // 5b-1605, the second review). Set when Undo is posted. Cleared ONLY
        // by an undo Revit reports there, a sync, a reload or a close - never
        // by time, a commit or a keep: an Undo that fires late takes whatever
        // is on top then, so every keep made while one stands is unsure.
        private readonly Dictionary<string, Posted> _outstanding =
            new Dictionary<string, Posted>(StringComparer.Ordinal);

        private readonly Action<string> _log;
        private long _clock;

        internal HeronUndoGuard() : this(null) { }

        /// <param name="log">Where each transition is written, or null for nowhere.</param>
        internal HeronUndoGuard(Action<string> log)
        {
            _log = log;
        }

        /// <summary>Any model remembered - the listener's one cheap question on every edit.</summary>
        internal bool Any { get { return _byDocument.Count > 0 || _outstanding.Count > 0; } }

        /// <summary>Whether an Undo Heron sent in this model has not been seen to run.</summary>
        internal bool Outstanding(string document)
        {
            return document != null && _outstanding.ContainsKey(document);
        }

        /// <summary>What is remembered for a model, or null.</summary>
        internal Entry Of(string document)
        {
            Entry entry;
            return document != null && _byDocument.TryGetValue(document, out entry) ? entry : null;
        }

        /// <summary>
        /// A write job was kept, and Revit counted `counted` elements added,
        /// changed or deleted in it; `read` is false when a change Revit
        /// reported could not be read, so the count may be short.
        ///
        /// ONLY A JOB REVIT COUNTED SOMETHING IN IS VOUCHED FOR. A job that ran
        /// with no transaction (`noTransactionOpen`: open a view), or a MODIFY
        /// that changed nothing, leaves no entry in Revit's undo list - and the
        /// next Undo would take whatever the modeller did before it.
        /// </summary>
        internal void Kept(string document, string job, string label, DateTime keptAt,
                           int counted, bool read)
        {
            if (string.IsNullOrEmpty(document) || string.IsNullOrEmpty(label))
            {
                // A keep Heron cannot place is a change it cannot account for.
                Doubt(null, "Heron kept a job in a model it could not name");
                return;
            }

            var before = Of(document);
            var entry = new Entry { Job = job, Label = label, KeptAt = keptAt, Touched = ++_clock };

            // THE NAMES OF THE KEEPS BEFORE IT, while Revit's list can still
            // hold them. A second keep under one name makes an undo or a redo
            // that carries that name ambiguous - see RedoneIn.
            entry.Earlier = new HashSet<string>(StringComparer.Ordinal);
            if (before != null && before.State != Synced && before.State != Reloaded)
            {
                if (before.Earlier != null) entry.Earlier.UnionWith(before.Earlier);
                if (!string.IsNullOrEmpty(before.Label)) entry.Earlier.Add(before.Label);
            }
            entry.Twin = entry.Earlier.Contains(label);

            // WHILE AN UNDO IS OUTSTANDING NO KEEP IS VOUCHED FOR, whatever the
            // entry before it says by now - it may have been covered, doubted
            // or replaced since the Undo was posted, and the Undo is still out.
            if (Outstanding(document))
            {
                entry.State = Unsure;
                entry.Why = OutstandingWhy;
            }
            else if (counted > 0) entry.State = OnTop;
            else if (read) entry.State = Nothing;
            else
            {
                entry.State = Unsure;
                entry.Why = "Heron could not read what that job changed";
            }

            _byDocument[document] = entry;
            Trim();
            Say("kept, " + counted.ToString(CultureInfo.InvariantCulture) + " element(s) counted"
                + (read ? "" : ", not all read"), null, document, before, entry);
        }

        /// <summary>
        /// A transaction committed in a model. `ours` is true while Heron's own
        /// write job is running in that model - its commits are inside its group.
        /// </summary>
        internal void Committed(string document, IEnumerable<string> names, bool ours)
        {
            var entry = Of(document);
            if (entry == null) return;
            if (ours)
            {
                Say("commit inside Heron's own job", names, document, entry, entry);
                return;
            }

            var was = entry.State;
            switch (entry.State)
            {
                case OnTop:
                    entry.State = Covered;
                    entry.CoveredBy = First(names);
                    break;
                case Sent:
                    // Between the Undo being posted and Revit reporting it. If
                    // this landed first, it is what that Undo takes.
                    entry.State = Unsure;
                    entry.Why = Quoted(First(names)) + " was reported in this model after Heron "
                              + "sent Undo and before Revit reported that Undo done, so the Undo "
                              + "may have taken it instead - if it did, Ctrl+Y (Redo) in Revit "
                              + "puts it back";
                    break;
                case Undone:
                    // A new change clears what Redo held: Heron's entry cannot come back.
                    if (!entry.Redoable) return;
                    entry.Redoable = false;
                    break;
                default:
                    return;     // already refusing, and nothing here changes why
            }
            entry.Touched = ++_clock;
            Say("commit", names, document, was, entry);
        }

        /// <summary>
        /// Revit undid something in a model. Heron's own entry is what went
        /// only when Revit names it - or names nothing. Any other name while
        /// Heron believed its own change was next means Heron missed something.
        /// </summary>
        internal void UndoneIn(string document, IEnumerable<string> names)
        {
            // AN UNDO RAN HERE, so the one Heron sent - if one was out - is no
            // longer waited for. Which entry it took is what the state below
            // records; a keep made while it was out stays unsure.
            var ran = document != null && _outstanding.Remove(document);

            var entry = Of(document);
            if (entry == null)
            {
                if (ran) Say("undo, and the Undo Heron sent is accounted for", names, document, (string)null, null);
                return;
            }

            var was = entry.State;
            switch (entry.State)
            {
                case OnTop:
                case Sent:
                    if (Mine(names, entry.Label) || First(names) == null)
                    {
                        entry.State = Undone;
                        entry.Depth = 0;
                        entry.Redoable = true;
                    }
                    else
                    {
                        entry.State = Unsure;
                        entry.Why = was == Sent
                            ? "the Undo Heron sent was reported by Revit as an undo of "
                              + Quoted(First(names)) + ", not of Heron's own '" + entry.Label
                              + "' - if it took work of yours, Ctrl+Y (Redo) in Revit puts it back"
                            : "Revit reported an undo of " + Quoted(First(names)) + " here, which "
                              + "is not the name Heron's change carries ('" + entry.Label + "')";
                    }
                    break;
                case Undone:
                    entry.Depth++;      // one more entry now sits above Heron's in Redo
                    break;
            }
            entry.Touched = ++_clock;
            Say(ran ? "undo, and the Undo Heron sent is accounted for" : "undo",
                names, document, was, entry);
        }

        /// <summary>
        /// Revit redid something. Heron's own entry is on top again only when
        /// every undo after it has been redone first AND this redo carries its
        /// name. A redo at any other moment is a change after it.
        ///
        /// AND NEVER WHEN AN EARLIER KEEP HERE CARRIED THE SAME NAME (`Twin`).
        /// The count is only as good as the events: whether Revit raises one
        /// undo or redo event for an entry or several is not known (Group NU),
        /// and with two keeps under one name a redo's name cannot say which of
        /// them came back. So such a keep, once undone, is never vouched for
        /// again - the modeller's Ctrl+Z still takes it.
        /// </summary>
        internal void RedoneIn(string document, IEnumerable<string> names)
        {
            var entry = Of(document);
            if (entry == null) return;

            var was = entry.State;
            switch (entry.State)
            {
                case Undone:
                    if (!entry.Redoable)
                    {
                        entry.State = Unsure;
                        entry.Why = "Revit reported a redo of " + Quoted(First(names))
                                  + " after a change that should have left nothing to redo";
                    }
                    else if (entry.Twin)
                    {
                        entry.State = Unsure;
                        entry.Why = "two of Heron's changes in this model carry the name '"
                                  + entry.Label + "', so Heron cannot tell which of them a "
                                  + "redo brought back";
                    }
                    else if (entry.Depth > 0) entry.Depth--;
                    else if (Mine(names, entry.Label)) entry.State = OnTop;
                    else
                    {
                        entry.State = Unsure;
                        entry.Why = "Revit reported a redo of " + Quoted(First(names))
                                  + " where Heron's own change was the next to be redone";
                    }
                    break;
                case OnTop:
                case Sent:
                    entry.State = Covered;
                    entry.CoveredBy = First(names);
                    break;
            }
            entry.Touched = ++_clock;
            Say("redo", names, document, was, entry);
        }

        /// <summary>Synchronized with central: Revit has cleared the undo list.</summary>
        internal void SyncedIn(string document)
        {
            if (document != null) _outstanding.Remove(document);    // nothing left for it to take
            var entry = Of(document);
            if (entry == null) return;
            var was = entry.State;
            entry.State = Synced;
            Say("sync with central", null, document, was, entry);
        }

        /// <summary>Reload Latest: Revit has cleared the undo list, as a sync does.</summary>
        internal void ReloadedIn(string document)
        {
            if (document != null) _outstanding.Remove(document);
            var entry = Of(document);
            if (entry == null) return;
            var was = entry.State;
            if (entry.State != Synced) entry.State = Reloaded;
            Say("reload latest", null, document, was, entry);
        }

        /// <summary>
        /// The model's key moved - a first save, or Save As. What was
        /// remembered moves with it and refuses: whether Revit's undo list
        /// still starts with Heron's change after that has not been measured.
        /// </summary>
        internal void Renamed(string before, string after)
        {
            if (string.IsNullOrEmpty(before) || string.IsNullOrEmpty(after) || before == after) return;

            // An Undo still out goes with the model, not with its old name.
            Posted posted;
            if (_outstanding.TryGetValue(before, out posted))
            {
                _outstanding.Remove(before);
                _outstanding[after] = posted;
            }

            var entry = Of(before);
            if (entry == null) return;
            var was = entry.State;
            _byDocument.Remove(before);
            if (Reopens(entry.State))
            {
                entry.State = Unsure;
                entry.Why = "the model was saved under a new name since";
            }
            _byDocument[after] = entry;
            Say("saved under a new name", null, after, was, entry);
        }

        /// <summary>
        /// Heron missed something in a model - or, with no model named, cannot
        /// tell in which - so it no longer knows what is on top there. Every
        /// entry that could still lead to an Undo refuses from here on.
        /// </summary>
        internal void Doubt(string document, string why)
        {
            if (document != null)
            {
                Doubt(document, Of(document), why);
                return;
            }
            foreach (var pair in new List<KeyValuePair<string, Entry>>(_byDocument))
                Doubt(pair.Key, pair.Value, why);
        }

        private void Doubt(string document, Entry entry, string why)
        {
            if (entry == null || !Reopens(entry.State)) return;
            var was = entry.State;
            entry.State = Unsure;
            entry.Why = why;
            Say("doubt", null, document, was, entry);
        }

        /// <summary>The model closed: nothing about it is remembered.</summary>
        internal void Closed(string document)
        {
            if (document != null) _outstanding.Remove(document);
            var entry = Of(document);
            if (entry == null) return;
            _byDocument.Remove(document);
            Say("closed, forgotten", null, document, entry.State, null);
        }

        /// <summary>
        /// Undo was sent for Heron's entry. It is not sent again, and no keep
        /// in this model is vouched for, until Revit reports an undo there.
        /// </summary>
        internal void SentFor(string document, DateTime now)
        {
            var entry = Of(document);
            if (entry == null || entry.State != OnTop) return;
            entry.State = Sent;
            entry.SentAt = now;
            _outstanding[document] = new Posted { Job = entry.Job, At = now };
            Say("Undo posted", null, document, OnTop, entry);
        }

        /// <summary>
        /// Why another Heron job must NOT start now - in ANY model - or null.
        /// An Undo was sent within SentWait and has not been seen to run: a
        /// change kept now could be on top when it runs, and a job that
        /// brings another model to the front would send it there.
        ///
        /// IT ONLY ANSWERS. Nothing is changed by asking, so a check
        /// (`undo_check`) and the hold can both ask it. Past SentWait it
        /// answers null and jobs run again - the Undo is still outstanding,
        /// and what that means is `Kept`'s and `Refusal`'s to say.
        /// </summary>
        internal string Pending(DateTime now)
        {
            foreach (var pair in _outstanding)
            {
                if (now - pair.Value.At > SentWait) continue;
                return "Heron sent Revit's Undo for '" + pair.Value.Job + "' in '" + TitleOf(pair.Key)
                     + "' at " + At(pair.Value.At) + " and has not seen Revit run it yet, so "
                     + "nothing was run: Revit's Undo acts on whichever model is on screen, and a "
                     + "change kept now could be what it takes. Look at the model, and ask again.";
            }
            return null;
        }

        /// <summary>
        /// Why Undo must NOT be sent for this model now, in the modeller's
        /// words - or null when Heron saw its last change land, has seen
        /// nothing since, and Revit can take the command. Every sentence says
        /// nothing was undone.
        /// </summary>
        internal string Refusal(string document, string title, bool inFront, bool canPost)
        {
            var model = string.IsNullOrEmpty(title) ? "this model" : "\"" + title + "\"";
            if (!inFront)
            {
                return "Revit's Undo works on the model on screen, and " + model + " is not it. "
                     + "Nothing was undone. Bring " + model + " to the front and ask again.";
            }

            // AN UNDO STILL OUT IN THIS MODEL REFUSES A SECOND, whatever else is
            // remembered - said here for the two cases no state below covers.
            const string StillOut = "Heron sent Revit's Undo in this model earlier and has not seen "
                                  + "Revit run it, so it sends no other: the first may still run. "
                                  + "Nothing was undone. Look at the arrows beside Revit's Undo and "
                                  + "Redo buttons to see what Revit holds.";

            var entry = Of(document);
            if (entry == null)
            {
                if (Outstanding(document)) return StillOut;
                return "Heron has kept no change in " + model + " since Revit started, so there is "
                     + "nothing of Heron's to undo. Nothing was undone. Ctrl+Z in Revit undoes the "
                     + "last change, whoever made it.";
            }

            var what = "Heron's last change, '" + entry.Job + "' at " + At(entry.KeptAt);
            const string Look = " Use the arrow beside Revit's Undo button to see the list and choose.";
            switch (entry.State)
            {
                case Nothing:
                    return "Heron's last job here, '" + entry.Job + "' at " + At(entry.KeptAt)
                         + ", changed nothing in this model that Revit counted, so there is "
                         + "nothing of Heron's to undo. Nothing was undone. Ctrl+Z in Revit undoes "
                         + "the last change, whoever made it.";
                case Covered:
                    return what + ": "
                         + (string.IsNullOrEmpty(entry.CoveredBy)
                                ? "another change was made after it"
                                : "'" + entry.CoveredBy + "' was done after it")
                         + " in this model, so Heron does not send Undo - it could take that and "
                         + "not Heron's change. Nothing was undone." + Look;
                case Undone:
                    return what + ", has already been undone. Nothing more was undone. Ctrl+Z in "
                         + "Revit undoes whatever is next on the list.";
                case Sent:
                    return "Undo was already sent for " + what + ", and Heron has not seen Revit "
                         + "run it. Nothing more was sent. Look at the model before asking again.";
                case Synced:
                    return what + ", was synchronized with central since, and Revit clears its undo "
                         + "list then. Nothing was undone.";
                case Reloaded:
                    return what + ", was followed by Reload Latest in this model, and Revit "
                         + "clears its undo list then. Nothing was undone.";
                case Unsure:
                    return "Heron cannot tell what Revit's Undo would take in this model now: "
                         + (string.IsNullOrEmpty(entry.Why) ? "it missed a change here" : entry.Why)
                         + ". So Undo is not sent for " + what + ". Nothing was undone." + Look;
            }

            if (Outstanding(document)) return StillOut;

            if (!canPost)
            {
                return "Revit will not take the Undo command just now - it may be in the middle "
                     + "of a command of its own. Nothing was undone. Finish or cancel it, and "
                     + "ask again.";
            }
            return null;
        }

        /// <summary>The sentence for an Undo just sent - what Revit will do, and how to take it back.</summary>
        internal string SentSentence(string document)
        {
            var entry = Of(document);
            if (entry == null) return "";
            return "Revit's own Undo was sent for " + Named(entry) + ". Heron has seen nothing "
                 + "change this model since. Revit runs it as soon as this answer is back: look "
                 + "at the model. Ctrl+Y (Redo) puts it back.";
        }

        /// <summary>
        /// A check's answer (`undo_check`): whether Undo would be sent now and
        /// for which job, or the exact refusal - and that nothing was sent.
        /// </summary>
        internal string CheckSentence(string document, string refusal)
        {
            const string OnlyChecked = " This was only a check: nothing was sent to Revit.";
            var entry = Of(document);
            if (refusal != null || entry == null)
                return "No - Undo would not be sent now. " + (refusal ?? "") + OnlyChecked;
            return "Yes - Undo would be sent now, for " + Named(entry) + ": Heron has seen "
                 + "nothing change this model since." + OnlyChecked;
        }

        private static string Named(Entry entry)
        {
            return "'" + entry.Job + "' (" + entry.Label + ", kept at " + At(entry.KeptAt) + ")";
        }

        /// <summary>A state from which an Undo could still be sent, now or after a redo.</summary>
        private static bool Reopens(string state)
        {
            return state == OnTop || state == Sent || state == Undone;
        }

        private static string At(DateTime when)
        {
            return when.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        /// <summary>A model's title, the last of the three parts its key is made of.</summary>
        private static string TitleOf(string document)
        {
            var title = document ?? "";
            var bar = title.LastIndexOf('|');
            return bar >= 0 ? title.Substring(bar + 1) : title;
        }

        private static string Quoted(string name)
        {
            return string.IsNullOrEmpty(name) ? "a change Revit did not name" : "'" + name + "'";
        }

        private static string First(IEnumerable<string> names)
        {
            if (names == null) return null;
            foreach (var name in names)
                if (!string.IsNullOrEmpty(name)) return name;
            return null;
        }

        /// <summary>
        /// Whether Revit named Heron's own entry: the label itself, or one of
        /// the job's setup steps, which commit under "label - step".
        /// </summary>
        private static bool Mine(IEnumerable<string> names, string label)
        {
            if (names == null || string.IsNullOrEmpty(label)) return false;
            foreach (var name in names)
            {
                if (name == null) continue;
                if (string.Equals(name, label, StringComparison.Ordinal)
                    || name.StartsWith(label + " - ", StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private void Say(string heard, IEnumerable<string> names, string document,
                         Entry before, Entry after)
        {
            Say(heard, names, document, before == null ? null : before.State, after);
        }

        /// <summary>
        /// One line for the add-in's log: what was heard, under which
        /// transaction names, in which model, and the state it left.
        /// </summary>
        private void Say(string heard, IEnumerable<string> names, string document,
                         string was, Entry after)
        {
            if (_log == null) return;
            try
            {
                var named = new List<string>();
                if (names != null)
                    foreach (var name in names) named.Add("'" + name + "'");

                var title = document ?? "";
                var bar = title.LastIndexOf('|');
                if (bar >= 0) title = title.Substring(bar + 1);

                _log("Undo guard: " + heard
                     + (named.Count == 0 ? "" : " [" + string.Join(", ", named.ToArray()) + "]")
                     + " in '" + title + "': " + (was ?? "nothing kept") + " -> "
                     + (after == null ? "forgotten"
                        : after.State
                          + (after.State == Undone
                                ? " (depth " + after.Depth.ToString(CultureInfo.InvariantCulture)
                                  + (after.Redoable ? "" : ", not redoable") + ")"
                                : "")
                          + ", job '" + after.Job + "'"
                          + (after.State == Unsure ? " - " + after.Why : "")));
            }
            catch (Exception)
            {
                // A line the log could not take must never cost the guard its state.
            }
        }

        private void Trim()
        {
            while (_byDocument.Count > MaxDocuments)
            {
                string oldest = null;
                var when = long.MaxValue;
                foreach (var pair in _byDocument)
                    if (pair.Value.Touched < when) { when = pair.Value.Touched; oldest = pair.Key; }
                if (oldest == null) break;
                _byDocument.Remove(oldest);
            }
        }
    }
}
