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
    /// Whether Heron's last kept change in a model is still the top of Revit's
    /// undo list - the bookkeeping behind `undo_last` (D-157), with nothing
    /// Revit-shaped in it.
    ///
    /// WHY IT HAS TO BE WATCHED. Revit's Undo takes the TOP of the list, and
    /// Revit has no API that names an entry and undoes that one (proposal
    /// F29, measured). The list belongs to the model, shared with the person
    /// working in it: if they moved a wall after Heron's change, that wall is
    /// on top, and an Undo sent "for Heron" would take their wall. So Heron
    /// sends Undo only when it SAW its own change land on top and has seen
    /// nothing land on it since.
    ///
    /// WHAT IT HEARS, from RevitFragment's permanent DocumentChanged listener:
    ///
    ///   Kept       a fragment job was assimilated - its entry is on top
    ///   Committed  any other transaction committed in that model - the
    ///              modeller's, another add-in's, or a Heron operation that
    ///              is not a kept fragment job (an isolate, a move). Heron's
    ///              entry is covered from then on, for good: a later undo of
    ///              the covering change cannot be counted back reliably, so
    ///              it is never tried
    ///   Undone     Revit undid something there. Heron's entry, if it was on
    ///              top, is what went - whoever pressed it
    ///   Redone     Revit redid something. Only Heron's own entry coming back
    ///              puts it on top again; anything else covers it
    ///   Synced     synchronized with central - Revit clears its undo list
    ///   Closed     the model closed - forgotten
    ///
    /// A COMMIT INSIDE HERON'S OWN JOB IS NOT A COVER, and the caller says
    /// which are (`ours`): the job's setup steps and its own transaction
    /// commit inside its group, and a group that rolls back leaves the list as
    /// it was. Only the job that is KEPT moves the top, and Kept says so.
    ///
    /// ONE ENTRY DEEP, ON PURPOSE. After an undo, the entry below Heron's is
    /// whatever was there - the modeller's, or an older Heron change - and
    /// whether Revit raises one undo event or several for one assimilated
    /// group is not known (NEEDS-CHECKING Group NU). So Heron undoes its last
    /// change once, and the next one is Ctrl+Z's.
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

        /// <summary>Heron's last kept change in one model, and where it stands.</summary>
        internal sealed class Entry
        {
            internal string Job;        // the fragment's name, for the sentence
            internal string Label;      // its undo entry, "Heron: <name>"
            internal DateTime KeptAt;   // local time, for "at 14:32"
            internal string State;      // OnTop, Covered, Undone, Sent or Synced
            internal string CoveredBy;  // the first transaction seen on top of it
            internal long Touched;
        }

        private readonly Dictionary<string, Entry> _byDocument =
            new Dictionary<string, Entry>(StringComparer.Ordinal);
        private long _clock;

        /// <summary>Any model remembered - the listener's one cheap question on every edit.</summary>
        internal bool Any { get { return _byDocument.Count > 0; } }

        /// <summary>What is remembered for a model, or null.</summary>
        internal Entry Of(string document)
        {
            Entry entry;
            return document != null && _byDocument.TryGetValue(document, out entry) ? entry : null;
        }

        /// <summary>A fragment job was kept: its entry is the top of the list now.</summary>
        internal void Kept(string document, string job, string label, DateTime keptAt)
        {
            if (string.IsNullOrEmpty(document) || string.IsNullOrEmpty(label)) return;
            _byDocument[document] = new Entry
            {
                Job = job, Label = label, KeptAt = keptAt, State = OnTop, Touched = ++_clock,
            };
            Trim();
        }

        /// <summary>
        /// A transaction committed in a model. `ours` is true while Heron's own
        /// write job is running in that model - its commits are inside its group.
        /// </summary>
        internal void Committed(string document, IEnumerable<string> names, bool ours)
        {
            if (ours) return;
            var entry = Of(document);
            if (entry == null || entry.State == Covered || entry.State == Synced) return;
            entry.State = Covered;
            entry.CoveredBy = First(names);
            entry.Touched = ++_clock;
        }

        /// <summary>
        /// Revit undid something in a model. When Heron's entry was on top -
        /// or an Undo was sent for it - Heron's entry is what went.
        /// </summary>
        internal void UndoneIn(string document, IEnumerable<string> names)
        {
            var entry = Of(document);
            if (entry == null) return;
            if (entry.State == OnTop || entry.State == Sent) entry.State = Undone;
            entry.Touched = ++_clock;
        }

        /// <summary>
        /// Revit redid something. Heron's own entry, redone straight after its
        /// undo, is on top again; any other redo is a change on top of it.
        /// </summary>
        internal void RedoneIn(string document, IEnumerable<string> names)
        {
            var entry = Of(document);
            if (entry == null || entry.State == Synced) return;
            if (entry.State == Undone && Names(names, entry.Label)) entry.State = OnTop;
            else if (entry.State != Covered)
            {
                entry.State = Covered;
                entry.CoveredBy = First(names);
            }
            entry.Touched = ++_clock;
        }

        /// <summary>Synchronized with central: Revit has cleared the undo list.</summary>
        internal void SyncedIn(string document)
        {
            var entry = Of(document);
            if (entry != null) entry.State = Synced;
        }

        /// <summary>The model closed: nothing about it is remembered.</summary>
        internal void Closed(string document)
        {
            if (document != null) _byDocument.Remove(document);
        }

        /// <summary>Undo was sent for Heron's entry; it is not sent again until Revit reports it.</summary>
        internal void SentFor(string document)
        {
            var entry = Of(document);
            if (entry != null && entry.State == OnTop) entry.State = Sent;
        }

        /// <summary>
        /// Why Undo must NOT be sent for this model now, in the modeller's
        /// words - or null when Heron's last change is on top and Revit can
        /// take the command. Every sentence says nothing was undone.
        /// </summary>
        internal string Refusal(string document, string title, bool inFront, bool canPost)
        {
            var model = string.IsNullOrEmpty(title) ? "this model" : "\"" + title + "\"";
            if (!inFront)
            {
                return "Revit's Undo works on the model on screen, and " + model + " is not it. "
                     + "Nothing was undone. Bring " + model + " to the front and ask again.";
            }

            var entry = Of(document);
            if (entry == null)
            {
                return "Heron has kept no change in " + model + " since Revit started, so there is "
                     + "nothing of Heron's to undo. Nothing was undone. Ctrl+Z in Revit undoes the "
                     + "last change, whoever made it.";
            }

            var what = "Heron's last change, '" + entry.Job + "' at " + At(entry.KeptAt);
            switch (entry.State)
            {
                case Covered:
                    return what + ", is no longer the top of Revit's undo list - "
                         + (string.IsNullOrEmpty(entry.CoveredBy)
                                ? "another change was made after it"
                                : "'" + entry.CoveredBy + "' was done after it")
                         + ", and Undo would take that first. Nothing was undone. Use the arrow "
                         + "beside Revit's Undo button to see the list and choose.";
                case Undone:
                    return what + ", has already been undone. Nothing more was undone. Ctrl+Z in "
                         + "Revit undoes whatever is next on the list.";
                case Sent:
                    return "Undo was already sent for " + what.Substring(0, 1).ToLowerInvariant()
                         + what.Substring(1) + ", and Revit has not reported it done yet. Nothing "
                         + "more was sent. Look at the model before asking again.";
                case Synced:
                    return what + ", was synchronized with central since, and Revit clears its undo "
                         + "list then. Nothing was undone.";
            }

            if (!canPost)
            {
                return "Revit is in the middle of a command of its own, so Undo cannot be sent now. "
                     + "Nothing was undone. Finish or cancel it, and ask again.";
            }
            return null;
        }

        /// <summary>The sentence for an Undo just sent - what Revit will do, and how to take it back.</summary>
        internal string SentSentence(string document)
        {
            var entry = Of(document);
            if (entry == null) return "";
            return "Revit's own Undo was sent for '" + entry.Job + "' (" + entry.Label + ", kept at "
                 + At(entry.KeptAt) + "), which was still the top of the undo list. Revit runs it "
                 + "as soon as this answer is back: look at the model. Ctrl+Y (Redo) puts it back.";
        }

        private static string At(DateTime when)
        {
            return when.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        private static string First(IEnumerable<string> names)
        {
            if (names == null) return null;
            foreach (var name in names)
                if (!string.IsNullOrEmpty(name)) return name;
            return null;
        }

        private static bool Names(IEnumerable<string> names, string label)
        {
            if (names == null) return false;
            foreach (var name in names)
                if (string.Equals(name, label, StringComparison.Ordinal)) return true;
            return false;
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
