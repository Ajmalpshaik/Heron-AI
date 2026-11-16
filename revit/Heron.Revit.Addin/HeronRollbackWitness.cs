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
    /// Whether rolling one job back took an EARLIER, KEPT job's work with it -
    /// and whether it took its own - as bookkeeping, with nothing Revit-shaped
    /// in it.
    ///
    /// WHY. A fragment's preview is the run itself, rolled back (D-55), so
    /// Heron rolls back far more often than it keeps, and every write proof
    /// rests on "the model was left exactly as it was". Section 1c of
    /// FRAGMENT-ISSUES holds three runs where it was not. The worst shape of
    /// that is a rollback that also removes what the modeller kept a minute
    /// earlier: the job on screen truly kept nothing, says so, and the stage
    /// before it has gone without a word. Golden Rule 16 and Article 8 promise
    /// one undo per job; they do not promise that undoing one job leaves the
    /// last one alone, so this checks it.
    ///
    /// WHAT IS WATCHED. When a job is KEPT, up to SampleLimit of the elements
    /// it added, by UniqueId, under the identity of the document it ran in -
    /// three strings, never the Document (revit-addin-conventions: never cache
    /// one). When the next write job starts, that sample is narrowed to what
    /// still resolves, so whatever the modeller deleted or undid between the
    /// two jobs is never blamed on Heron. When that job is rolled back, what is
    /// missing from the narrowed sample went DURING the job - on Revit's API
    /// thread, where nobody can press Undo - and that is said once, then the
    /// sample is forgotten.
    ///
    /// A SECOND WITNESS FOR THE JOB'S OWN ROLLBACK. SafeRollBack reports
    /// Revit's status after the attempt, and section 1c showed that a failed
    /// rollback and a clean one produced identical output. So the elements the
    /// job ADDED are looked for again afterwards: any still standing means the
    /// rollback did not hold, whatever the status said.
    ///
    /// AND WHAT IT CHANGED, SINCE 2026-11-16 - NEEDS-CHECKING A16b: a rename
    /// reported rolled back while 8 views still read HERONL2, and an addition
    /// count could never see it. The elements the job was HANDED and the
    /// modeller's selection are read before the job starts (ReadBefore), read
    /// again just before the rollback (ReadDuring) - which says what the job
    /// changed, value by value - and read a third time after it. A value the
    /// job changed that does not read as it did before withdraws "nothing was
    /// kept". The reading is D-156's (HeronReadBack.Values, paired by each
    /// parameter's own id); only the rule is here. A changed element that is
    /// not among those read - not handed, not selected, past the limit - is
    /// not seen, so this too can WITHDRAW "nothing was kept" and never
    /// stands in for one.
    ///
    /// AN ID THAT COULD NOT BE ASKED ABOUT IS NEITHER. Whether an id resolves
    /// is a question the caller answers with true, false or null, and null is
    /// counted as neither lost nor standing: a sentence here is said on what
    /// was checked and nothing else, which is WithVerdict's rule (Article 27).
    ///
    /// SPLIT OUT OF RevitFragment.cs SO IT CAN BE PROVED WITHOUT REVIT, the
    /// move HeronFailureNote.cs makes: this file names no Autodesk type, so
    /// tests/Heron.RollbackWitness.TestHost links it by source, with
    /// HeronReadBack.cs, and there is one copy of the rule. RevitFragment's
    /// DocumentChanged listener feeds it what Revit reports; RevitReadBack
    /// reads the values; RevitFragment.RollBackAndCheck asks it what to say.
    /// </summary>
    internal sealed class HeronRollbackWitness
    {
        /// <summary>
        /// How many of a job's added elements are kept to check. A sample, not
        /// a census: a job that pasted ten thousand elements is caught by any
        /// two hundred of them going, and looking each one up is the cost.
        /// </summary>
        internal const int SampleLimit = 200;

        /// <summary>
        /// The most documents a sample is kept for at once. The same
        /// housekeeping bound as the chain store's: a Revit left open for a
        /// week must not keep a list for every model it ever wrote to.
        /// </summary>
        internal const int MaxDocuments = 16;

        /// <summary>The last kept job in one document.</summary>
        private sealed class Kept
        {
            internal string Job;            // the fragment's name, for the sentence
            internal string Label;          // its undo entry, "Heron: <name>"
            internal List<string> Ids;      // up to SampleLimit UniqueIds
            internal int Added;             // how many it added in all
            internal bool Undone;           // the listener saw its undo entry undone
            internal long Touched;
        }

        private readonly Dictionary<string, Kept> _kept =
            new Dictionary<string, Kept>(StringComparer.Ordinal);

        private long _tick;

        /// <summary>
        /// Whether any document has a kept sample - the listener's first test,
        /// so a modeller's ordinary edits cost one comparison when there is
        /// nothing to watch.
        /// </summary>
        internal bool Any { get { return _kept.Count > 0; } }

        /// <summary>The write job being watched now, or null.</summary>
        internal Watch Armed { get; private set; }

        /// <summary>How many ids are kept for this document, 0 when none.</summary>
        internal int KeptCount(string document)
        {
            Kept kept;
            return document != null && _kept.TryGetValue(document, out kept) ? kept.Ids.Count : 0;
        }

        // ------------------------------------------------------------ one job

        /// <summary>
        /// One write job, from just before its group starts to its end. It
        /// holds strings only. Disposing it stops the listener feeding it.
        /// </summary>
        internal sealed class Watch : IDisposable
        {
            private readonly HeronRollbackWitness _owner;

            internal readonly string Document;
            internal readonly string Job;
            internal readonly string Label;

            // THE KEPT SAMPLE AS IT STOOD WHEN THIS JOB STARTED, already
            // narrowed to what resolved then, and copied, so what this job's
            // rollback is judged against cannot move under it.
            internal readonly string KeptJob;
            internal readonly List<string> KeptIds;
            internal readonly int KeptAdded;

            /// <summary>What the start of the job found to say, "" when nothing.</summary>
            internal readonly string Before;

            // EVERY id the job added, as the ElementId's own text, so the same
            // element reported twice is counted once and a rollback event's
            // list can be compared with it. The UniqueIds are the sample.
            private readonly HashSet<string> _addedIds = new HashSet<string>(StringComparer.Ordinal);
            private readonly List<string> _sample = new List<string>();

            // WHAT IT WAS HANDED, AS IT READ BEFORE THE JOB STARTED - see
            // ReadBefore - and, once ReadDuring has run, which of those the job
            // changed and how. Null until ReadDuring, so "not looked at" and
            // "looked at and unchanged" are never the same thing.
            private readonly List<HeronReadBack.Values> _before = new List<HeronReadBack.Values>();
            private readonly HashSet<long> _notTheChange = new HashSet<long>();
            private List<Change> _changed;

            /// <summary>One element the job changed: what it read before, and each value that moved.</summary>
            internal sealed class Change
            {
                internal string UniqueId;
                internal string Category;
                internal readonly List<Moved> Values = new List<Moved>();
            }

            /// <summary>
            /// One value the job changed: what it is paired by, what it is
            /// called, and what it read before the job - null when it was not
            /// there. What the job made it is not kept: the check is whether
            /// the value came BACK, so only the before is compared.
            /// </summary>
            internal sealed class Moved
            {
                internal string Key;
                internal string Name;
                internal string Was;
            }

            internal Watch(HeronRollbackWitness owner, string document, string job, string label,
                           string keptJob, List<string> keptIds, int keptAdded, string before)
            {
                _owner = owner;
                Document = document;
                Job = job;
                Label = label;
                KeptJob = keptJob;
                KeptIds = keptIds ?? new List<string>();
                KeptAdded = keptAdded;
                Before = before ?? "";
                EventNamed = -1;
            }

            /// <summary>How many different elements the job added.</summary>
            internal int AddedCount { get { return _addedIds.Count; } }

            /// <summary>The UniqueIds kept of them, at most SampleLimit.</summary>
            internal IList<string> Sample { get { return _sample.AsReadOnly(); } }

            /// <summary>
            /// How many of the job's added ids Revit's own group-rollback event
            /// named as removed, or -1 when no such event reached this watch.
            /// Carried for NEEDS-CHECKING, which asks whether that event names
            /// the right ids at all - nothing here decides on it.
            /// </summary>
            internal int EventNamed { get; private set; }

            /// <summary>The first rollback's outcome, null until there is one.</summary>
            internal Outcome Ended { get; set; }

            /// <summary>
            /// Count one added element. True when it is new AND the sample has
            /// room, which is the only time the caller need look up its
            /// UniqueId - so a paste of thousands costs one lookup per sampled
            /// element, not one per element.
            /// </summary>
            internal bool Counts(string elementId)
            {
                if (string.IsNullOrEmpty(elementId) || !_addedIds.Add(elementId)) return false;
                return _sample.Count < SampleLimit;
            }

            /// <summary>Keep one added element's UniqueId in the sample.</summary>
            internal void Sampled(string uniqueId)
            {
                if (string.IsNullOrEmpty(uniqueId) || _sample.Count >= SampleLimit) return;
                if (!_sample.Contains(uniqueId)) _sample.Add(uniqueId);
            }

            /// <summary>How many elements were read before the job, at most SampleLimit.</summary>
            internal int ReadBeforeCount { get { return _before.Count; } }

            /// <summary>
            /// Of those, how many the job changed, as ReadDuring found - 0 until
            /// it has run.
            /// </summary>
            internal int ChangedCount { get { return _changed == null ? 0 : _changed.Count; } }

            /// <summary>What ReadDuring found, or nothing when it has not run.</summary>
            internal IList<Change> Changed
            {
                get { return (_changed ?? new List<Change>()).AsReadOnly(); }
            }

            /// <summary>
            /// The elements this job was handed and the modeller's selection, as
            /// D-156's reader read them BEFORE the job started - the only time a
            /// "before" can be read without running anything twice (D-126). Only
            /// an element whose UniqueId was read can be found again, so one
            /// without is passed over. `notTheChange` names parameters, by their
            /// own id, whose value is not the job's change (RevitReadBack's
            /// NotTheChange). Called once per watch, before the group starts.
            /// </summary>
            internal void ReadBefore(IEnumerable<HeronReadBack.Values> before,
                                     IEnumerable<long> notTheChange)
            {
                _before.Clear();
                _changed = null;
                _notTheChange.Clear();
                if (notTheChange != null)
                    foreach (var id in notTheChange) _notTheChange.Add(id);
                if (before == null) return;

                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var was in before)
                {
                    if (_before.Count >= SampleLimit) break;
                    if (was == null || string.IsNullOrEmpty(was.UniqueId) || !seen.Add(was.UniqueId))
                        continue;
                    _before.Add(was);
                }
            }

            /// <summary>
            /// The same elements read again with the job's work still in the
            /// model, just before the rollback: what differs from ReadBefore is
            /// what the job changed. Once per watch, and never after the first
            /// rollback - a second look then would read the rolled-back model as
            /// the job's work. An element that could not be read now is not
            /// counted as changed: nobody knows whether it was.
            /// </summary>
            internal void ReadDuring(Func<string, HeronReadBack.Values> read)
            {
                if (_changed != null || Ended != null) return;
                _changed = new List<Change>();
                foreach (var was in _before)
                {
                    var now = Read(read, was.UniqueId);
                    if (now == null) continue;

                    var change = new Change
                    {
                        UniqueId = was.UniqueId,
                        Category = now.Category ?? was.Category,
                    };
                    foreach (var item in now.Items)
                    {
                        if (NotTheChange(item)) continue;
                        HeronReadBack.Values.Item old;
                        var had = was.TryGet(item.Key, out old);
                        if (had && string.Equals(old.Value, item.Value, StringComparison.Ordinal)) continue;
                        change.Values.Add(new Moved
                        {
                            Key = item.Key,
                            Name = HeronReadBack.Label(item, was, now),
                            Was = had ? old.Value : null,
                        });
                    }
                    foreach (var item in was.Items)
                    {
                        HeronReadBack.Values.Item kept;
                        if (NotTheChange(item) || now.TryGet(item.Key, out kept)) continue;
                        change.Values.Add(new Moved
                        {
                            Key = item.Key,
                            Name = HeronReadBack.Label(item, was, now),
                            Was = item.Value,
                        });
                    }
                    if (change.Values.Count > 0) _changed.Add(change);
                }
            }

            private bool NotTheChange(HeronReadBack.Values.Item item)
            {
                return item.Parameter && _notTheChange.Contains(item.Id);
            }

            /// <summary>What Revit's group-rollback event said it removed.</summary>
            internal void RollbackNamed(IEnumerable<string> removedElementIds)
            {
                var named = 0;
                if (removedElementIds != null)
                {
                    foreach (var id in removedElementIds)
                        if (id != null && _addedIds.Contains(id)) named++;
                }
                EventNamed = Math.Max(EventNamed, 0) + named;
            }

            public void Dispose()
            {
                if (_owner != null && ReferenceEquals(_owner.Armed, this)) _owner.Armed = null;
            }
        }

        /// <summary>
        /// A write job is about to start in this document: narrow the kept
        /// sample to what still resolves, and arm the listener for the job.
        ///
        /// ALL OF IT GONE ALREADY is said once and the sample forgotten - it was
        /// gone before this job touched anything, so this job's rollback cannot
        /// have taken it. SOME OF IT GONE is the modeller's own work and is
        /// narrowed away in silence.
        /// </summary>
        internal Watch Begin(string document, string job, string label, Func<string, bool?> resolves)
        {
            string keptJob = null;
            List<string> keptIds = null;
            var keptAdded = 0;
            var before = "";

            Kept kept;
            if (document != null && _kept.TryGetValue(document, out kept))
            {
                var present = Present(kept.Ids, resolves);
                if (present.Count == 0)
                {
                    before = kept.Undone ? UndoneSentence(kept.Job) : GoneSentence(kept.Job);
                    _kept.Remove(document);
                }
                else
                {
                    kept.Ids = present;
                    kept.Undone = false;
                    kept.Touched = ++_tick;
                    keptJob = kept.Job;
                    keptIds = new List<string>(present);
                    keptAdded = kept.Added;
                }
            }

            var watch = new Watch(this, document, job, label, keptJob, keptIds, keptAdded, before);
            Armed = watch;
            return watch;
        }

        /// <summary>
        /// The job's group has been rolled back and Revit answered `reported`.
        /// Look for what the job added, for what the last kept job made, and -
        /// with `read` - for the values the job changed (Watch.ReadDuring).
        /// Said once: a second call for the same watch returns the first answer.
        /// </summary>
        internal Outcome RolledBack(Watch watch, bool reported, Func<string, bool?> resolves,
                                    Func<string, HeronReadBack.Values> read = null)
        {
            if (watch == null) return new Outcome(reported);
            if (watch.Ended != null) return watch.Ended;

            var outcome = new Outcome(reported)
            {
                Added = watch.AddedCount,
                EventNamed = watch.EventNamed,
                KeptJob = watch.KeptJob,
                KeptAdded = watch.KeptAdded,
                Before = watch.Before,
            };

            // CHECKED MEANS ANSWERED. An id Revit could not be asked about is
            // left out of both counts, so no sentence below covers it.
            foreach (var id in watch.Sample)
            {
                var there = Ask(resolves, id);
                if (there == null) continue;
                outcome.Checked++;
                if (there == true) outcome.Standing++;
            }

            foreach (var id in watch.KeptIds)
            {
                var there = Ask(resolves, id);
                if (there == null) continue;
                outcome.KeptChecked++;
                if (there == false) outcome.Lost++;
            }

            // WHAT THE JOB CHANGED, READ A THIRD TIME. A value it changed that
            // does not read as it did before the job - the job's own value, or
            // a third one - did not come back. An element that cannot be read
            // now is counted in neither: Article 27, as the ids above.
            outcome.Changed = watch.ChangedCount;
            foreach (var change in watch.Changed)
            {
                var now = Read(read, change.UniqueId);
                if (now == null) continue;
                outcome.ChangedChecked++;

                foreach (var moved in change.Values)
                {
                    HeronReadBack.Values.Item item;
                    var value = now.TryGet(moved.Key, out item) ? item.Value : null;
                    if (string.Equals(value, moved.Was, StringComparison.Ordinal)) continue;

                    if (outcome.ChangedStanding == 0)
                    {
                        outcome.StoodIn = change.Category;
                        outcome.StoodName = moved.Name;
                        outcome.StoodWas = moved.Was;
                        outcome.StoodNow = value;
                    }
                    outcome.ChangedStanding++;
                    break;
                }
            }

            // SAID ONCE, THEN FORGOTTEN. What is gone is gone; checking for it
            // again on the next rollback would report the same loss as new.
            if (outcome.Lost > 0 && watch.Document != null)
            {
                Kept kept;
                if (_kept.TryGetValue(watch.Document, out kept) && kept.Job == watch.KeptJob)
                    _kept.Remove(watch.Document);
            }

            watch.Ended = outcome;
            return outcome;
        }

        /// <summary>
        /// The job's group was ASSIMILATED: what it added is now the kept work
        /// to watch. Returns what the start of the job found to say.
        ///
        /// A KEPT JOB THAT ADDED NOTHING LEAVES THE LAST SAMPLE WATCHED - a
        /// rename kept after a build does not make the build any less worth
        /// protecting - but narrowed now, so that elements this job deleted on
        /// purpose are never reported later as lost.
        /// </summary>
        internal string Assimilated(Watch watch, Func<string, bool?> resolves)
        {
            if (watch == null) return "";
            if (watch.Document == null) return watch.Before;

            var mine = Present(watch.Sample, resolves);
            if (mine.Count > 0)
            {
                _kept[watch.Document] = new Kept
                {
                    Job = watch.Job,
                    Label = watch.Label,
                    Ids = mine,
                    Added = watch.AddedCount,
                    Touched = ++_tick,
                };
                Trim();
            }
            else
            {
                Kept kept;
                if (_kept.TryGetValue(watch.Document, out kept))
                {
                    kept.Ids = Present(kept.Ids, resolves);
                    if (kept.Ids.Count == 0) _kept.Remove(watch.Document);
                }
            }

            return watch.Before;
        }

        /// <summary>
        /// Revit undid these transactions in this document. When one is the
        /// kept job's own undo entry, its elements are expected to go, and the
        /// next job's start says so rather than blaming anything.
        /// </summary>
        internal void Undone(string document, IEnumerable<string> names)
        {
            var kept = KeptFor(document, names);
            if (kept != null) kept.Undone = true;
        }

        /// <summary>Revit redid them: the kept job's elements are back.</summary>
        internal void Redone(string document, IEnumerable<string> names)
        {
            var kept = KeptFor(document, names);
            if (kept != null) kept.Undone = false;
        }

        private Kept KeptFor(string document, IEnumerable<string> names)
        {
            Kept kept;
            if (document == null || names == null || !_kept.TryGetValue(document, out kept))
                return null;
            foreach (var name in names)
                if (string.Equals(name, kept.Label, StringComparison.Ordinal)) return kept;
            return null;
        }

        private static List<string> Present(IEnumerable<string> ids, Func<string, bool?> resolves)
        {
            var present = new List<string>();
            foreach (var id in ids)
                if (Ask(resolves, id) != false) present.Add(id);
            return present;
        }

        // NULL WHEN IT COULD NOT BE ASKED - see the class comment.
        private static bool? Ask(Func<string, bool?> resolves, string id)
        {
            if (resolves == null) return null;
            try { return resolves(id); }
            catch (Exception) { return null; }
        }

        // AND THE SAME FOR A VALUE: null is "not read", never "unchanged".
        private static HeronReadBack.Values Read(Func<string, HeronReadBack.Values> read, string uniqueId)
        {
            if (read == null || string.IsNullOrEmpty(uniqueId)) return null;
            try { return read(uniqueId); }
            catch (Exception) { return null; }
        }

        private void Trim()
        {
            while (_kept.Count > MaxDocuments)
            {
                string oldest = null;
                var when = long.MaxValue;
                foreach (var entry in _kept)
                    if (entry.Value.Touched < when) { when = entry.Value.Touched; oldest = entry.Key; }
                if (oldest == null) break;
                _kept.Remove(oldest);
            }
        }

        // ------------------------------------------------------------ words

        /// <summary>
        /// A refusal, with what the rollback check found added to its message.
        ///
        /// NOTHING TO ADD RETURNS IT UNCHANGED, so an ordinary refusal is byte
        /// for byte what it was. A plain Json.Error is rebuilt with the
        /// sentence at the end of its message, which is the part the chat
        /// prints; anything else keeps its words and carries the sentence
        /// beside them as `rollbackNote`. Here rather than in RevitFragment so
        /// the round trip through the bridge's own reader and writer is proved
        /// by the host - Json names nothing outside System.
        /// </summary>
        internal static string Told(string reply, string more)
        {
            if (string.IsNullOrEmpty(more) || string.IsNullOrEmpty(reply)) return reply;

            var code = Heron.Bridge.Json.ReadString(reply, "error");
            var message = Heron.Bridge.Json.ReadString(reply, "message");
            if (code != null && message != null && Heron.Bridge.Json.Error(code, message) == reply)
                return Heron.Bridge.Json.Error(code, message + " " + more);

            return reply.EndsWith("}", StringComparison.Ordinal)
                ? reply.Substring(0, reply.Length - 1) + ", "
                  + Heron.Bridge.Json.Str("rollbackNote", more) + "}"
                : reply;
        }

        internal static string UndoneSentence(string job)
        {
            return "The last kept job ('" + job + "') was undone in Revit before this run "
                 + "started, so this run did not remove what it made.";
        }

        internal static string GoneSentence(string job)
        {
            return "What the last kept job ('" + job + "') made was already gone when this run "
                 + "started - removed in Revit, not by this run.";
        }

        private static string Elements(int count)
        {
            return count.ToString(CultureInfo.InvariantCulture)
                 + (count == 1 ? " element" : " elements");
        }

        private static string Number(int count)
        {
            return count.ToString(CultureInfo.InvariantCulture);
        }

        private static string Then(string first, string second)
        {
            if (string.IsNullOrEmpty(first)) return second ?? "";
            if (string.IsNullOrEmpty(second)) return first;
            return first + " " + second;
        }

        /// <summary>What one rollback left, as far as it was checked.</summary>
        internal sealed class Outcome
        {
            internal Outcome(bool reported)
            {
                Reported = reported;
                EventNamed = -1;
                Before = "";
            }

            /// <summary>Revit's status after the attempt said RolledBack.</summary>
            internal readonly bool Reported;

            internal int Added;         // how many different elements the job added
            internal int Checked;       // how many of them were looked for again
            internal int Standing;      // of those, how many are still in the model
            internal int EventNamed;    // -1, or how many Revit's rollback event named

            internal string KeptJob;    // the last kept job, null when none was watched
            internal int KeptChecked;   // how many of its elements were looked for
            internal int KeptAdded;     // how many it made in all
            internal int Lost;          // of those looked for, how many are gone

            internal string Before;     // what the start of the job found to say

            internal int Changed;         // elements read before the job that it changed
            internal int ChangedChecked;  // of those, how many were read again after
            internal int ChangedStanding; // of those, how many still do not read as before

            // THE FIRST VALUE THAT DID NOT COME BACK, for the sentence: the
            // element's category, the value's name, and what it read before the
            // job and reads now - null for a value that was not there.
            internal string StoodIn;
            internal string StoodName;
            internal string StoodWas;
            internal string StoodNow;

            /// <summary>
            /// Revit confirmed the rollback, nothing the job added is still
            /// standing, and every value it changed that was read back reads as
            /// before. Only what was handed or selected is read back, so this is
            /// the most a rollback can be shown to have done - never more.
            /// </summary>
            internal bool Held { get { return Reported && Standing == 0 && ChangedStanding == 0; } }

            /// <summary>Anything of the job seen still in the model, added or changed.</summary>
            internal bool Stands { get { return Standing > 0 || ChangedStanding > 0; } }

            /// <summary>
            /// What the rollback left, in one sentence. Said on what was
            /// checked: the status, the additions counted back, and the
            /// changes read back.
            /// </summary>
            internal string ModelSentence()
            {
                if (Held)
                {
                    var none = Checked == 0 ? "" : " - and " + NoneStanding();
                    if (ChangedChecked > 0)
                        none += (Checked == 0 ? " - and " : ", and ") + ChangedReturned();
                    return "Revit confirmed the rollback, so the model is exactly as it was" + none + ".";
                }

                if (Reported)
                {
                    return "Revit reported the rollback, but " + WhatStands("the model")
                         + ", so THE ROLLBACK DID NOT HOLD. Check what it would have changed "
                         + "before trusting anything here, and do not save until you have.";
                }

                return "Revit did NOT confirm the rollback, so THE MODEL MAY STILL HOLD PART OF THIS "
                     + "JOB" + (Stands ? " - " + WhatStands("it") : "")
                     + ". Check what it would have changed before trusting anything here, and do "
                     + "not save until you have.";
            }

            /// <summary>
            /// What was seen still in the model, in one clause: the additions
            /// still standing, the changes still holding, or both. `place` is
            /// "the model" or "it". "" when nothing was seen.
            /// </summary>
            internal string WhatStands(string place)
            {
                var added = Standing > 0 ? SomeStanding() + " still in " + place : "";
                var changed = ChangedStanding > 0 ? ChangedStill() : "";
                if (added.Length > 0 && changed.Length > 0) return added + ", and " + changed;
                return added + changed;
            }

            /// <summary>WhatStands as a sentence of its own, "" when nothing was seen.</summary>
            internal string StillThere()
            {
                var said = WhatStands("it");
                if (said.Length == 0) return "";
                return char.ToUpperInvariant(said[0]) + said.Substring(1) + ".";
            }

            /// <summary>
            /// "8 of the 11 elements this run changed that Heron read back
            /// still hold the change (Views: Name reads 'HERONL2', was 'L2')".
            /// The first value that did not come back is the one quoted.
            /// </summary>
            internal string ChangedStill()
            {
                string head;
                if (ChangedStanding >= ChangedChecked)
                    head = ChangedChecked == 1
                        ? "the one element this run changed that Heron read back still holds the change"
                        : (ChangedChecked == 2 ? "both elements" : "all " + Elements(ChangedChecked))
                          + " this run changed that Heron read back still hold the change";
                else
                    head = Number(ChangedStanding) + " of the " + Elements(ChangedChecked)
                         + " this run changed that Heron read back still "
                         + (ChangedStanding == 1 ? "holds" : "hold") + " the change";

                var where = string.IsNullOrEmpty(StoodIn) ? "" : StoodIn + ": ";
                return head + " (" + where + StoodName + " " + NowReads(StoodNow) + ", "
                     + WasRead(StoodWas) + ")";
            }

            /// <summary>
            /// "the 11 elements this run changed that Heron read back read as
            /// they did before it". Only meaningful when ChangedChecked is above 0.
            /// </summary>
            internal string ChangedReturned()
            {
                return ChangedChecked == 1
                    ? "the one element this run changed that Heron read back reads as it did before it"
                    : "the " + Elements(ChangedChecked) + " this run changed that Heron read back "
                      + "read as they did before it";
            }

            private static string NowReads(string value)
            {
                if (value == null) return "is not there now";
                if (value.Length == 0) return "is blank";
                return "reads '" + HeronReadBack.Cut(value) + "'";
            }

            private static string WasRead(string value)
            {
                if (value == null) return "was not there";
                if (value.Length == 0) return "was blank";
                return "was '" + HeronReadBack.Cut(value) + "'";
            }

            /// <summary>
            /// The earlier kept job's loss, "" when there was none. Said once:
            /// the sample is forgotten as this is written.
            /// </summary>
            internal string LossSentence()
            {
                if (Lost <= 0) return "";
                var said = "Rolling back this run removed " + Number(Lost) + " of "
                         + Elements(KeptChecked) + " the last kept job made ('" + KeptJob
                         + "') - rebuild that stage.";
                if (KeptAdded > SampleLimit)
                {
                    said += " Heron watches " + Number(SampleLimit) + " of the "
                          + Number(KeptAdded) + " it made, so more of it may be gone.";
                }
                return said;
            }

            /// <summary>
            /// What any reply carries beside its own words about the model: the
            /// loss, and what the start of the job found. "" in the ordinary
            /// case, so an ordinary reply reads exactly as it did.
            /// </summary>
            internal string Also()
            {
                return Then(LossSentence(), Before);
            }

            /// <summary>
            /// The whole account, for a reply whose own words say what happened
            /// to the model: the model sentence, then Also.
            /// </summary>
            internal string Account()
            {
                return Then(ModelSentence(), Also());
            }

            /// <summary>
            /// What a refusal that says NOTHING about the model must still
            /// carry: the rollback not holding, and Also. "" when it held and
            /// nothing was lost, so a clean refusal is unchanged.
            /// </summary>
            internal string Unsaid()
            {
                return Then(Held ? "" : ModelSentence(), Also());
            }

            /// <summary>
            /// "none of the 3 elements this run added is still in the model",
            /// sampled or not. Only meaningful when Checked is above 0.
            /// </summary>
            internal string NoneStanding()
            {
                if (Checked >= Added)
                    return Checked == 1 ? "the one element this run added is gone again"
                                        : "none of the " + Elements(Checked)
                                          + " this run added is still in the model";
                return "none of the " + Number(Checked) + " Heron looked for, of the "
                     + Elements(Added) + " this run added, is still in the model";
            }

            /// <summary>"2 of the 3 elements this run added are", sampled or not.</summary>
            internal string SomeStanding()
            {
                var verb = Standing == 1 ? " is" : " are";
                if (Checked >= Added)
                    return Number(Standing) + " of the " + Elements(Checked) + " this run added" + verb;
                return Number(Standing) + " of the " + Number(Checked) + " Heron looked for, of the "
                     + Elements(Added) + " this run added," + verb;
            }
        }
    }
}
