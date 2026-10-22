// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Heron.Bridge;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// What a fragment write changed in the model, element by element - the
    /// counting and the wording, with nothing Revit-shaped in it.
    ///
    /// WHY IT EXISTS. Until this file Heron tracked no change at all: no
    /// DocumentChanged, no updater, nothing in revit/ or platform/. A write's
    /// reply said what the fragment's own variables held and nothing about
    /// which elements it touched, so the only measure of "what did this do"
    /// was the fragment's own word - and `revit_change` sent every word of it
    /// back into the chat, 322,725 characters across two family builds
    /// (FRAGMENT-ISSUES 5b-407). Article 22 asks the record for "elements
    /// touched", and nothing recorded them.
    ///
    /// SPLIT OUT OF RevitFragment.cs SO IT CAN BE PROVED WITHOUT REVIT, the
    /// move HeronFailureNote.cs makes: this names no Autodesk type, so
    /// tests/Heron.ChangeReport.TestHost links it BY SOURCE and there is one
    /// copy of the rule. RevitFragment's DocumentChanged handler hands each
    /// committed transaction here as text - its names and the ids it added,
    /// modified and deleted - and reads nothing else inside the event.
    ///
    /// WHOSE CHANGE IT WAS IS READ OFF THE TRANSACTION'S NAME. The fragment's
    /// own transaction is named `label`; each write setup step's is
    /// `label + " - " + stepName` (RunSetupSteps). So a name that starts with
    /// that prefix is a setup step's, and every other name - the fragment's
    /// own, or one an edit-scope fragment opened itself (D-112) - is the
    /// fragment's. That is what lets the verdict stop saying setup changes are
    /// not counted: they are counted, apart.
    ///
    /// WITHIN ONE PART, AN ELEMENT IS COUNTED ONCE, by what happened to it
    /// overall. Added then modified is added. Added then deleted is nothing -
    /// it came and went inside the job, and naming it would describe a model
    /// that never existed. Modified then deleted is deleted. Across the two
    /// parts each keeps its own: an element a setup step made and the
    /// fragment then changed is added by the setup and modified by the
    /// fragment, which is exactly what happened.
    ///
    /// CATEGORIES ARE READ ONCE, AFTER THE WORK AND BEFORE THE GROUP CLOSES,
    /// through the callback Describe is given - never inside the event, which
    /// Revit raises while it is still finishing the transaction. A deleted
    /// element has no category left to read, so it is counted and never
    /// categorised.
    /// </summary>
    internal sealed class HeronChangeReport
    {
        /// <summary>How many ids the reply carries per part. The rest are on disk.</summary>
        internal const int FirstIds = 20;

        /// <summary>The category an element with none - or none Revit would say - is counted under.</summary>
        internal const string NoCategory = "(no category)";

        internal const string FragmentPart = "fragment";
        internal const string SetupPart = "setup";

        private readonly string _setupPrefix;
        private readonly Part _fragment = new Part();
        private readonly Part _setup = new Part();
        private bool _sealed;

        /// <summary>Committed transactions counted - the evidence the event fired at all.</summary>
        internal int Events { get; private set; }

        /// <summary>Events whose content could not be read. Said, never guessed at.</summary>
        internal int Unreadable { get; private set; }

        /// <param name="label">The fragment's transaction name, `"Heron: " + name`.</param>
        internal HeronChangeReport(string label)
        {
            _setupPrefix = (label ?? "") + " - ";
        }

        /// <summary>
        /// Is a transaction with these names one of the setup steps'?
        ///
        /// A commit carries one name. Asked of every name anyway, because the
        /// call takes Revit's list as it comes and "any of them is a setup
        /// step's" is the reading that never files a setup change under the
        /// fragment.
        /// </summary>
        internal bool IsSetup(IEnumerable<string> names)
        {
            if (names == null) return false;
            foreach (var name in names)
            {
                if (name != null && name.StartsWith(_setupPrefix, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// One committed transaction. Ignored once the report is sealed: what
        /// happens after Describe - the group assimilated or rolled back - is
        /// not the fragment's work, and a rollback reports the same elements
        /// again with the meaning reversed.
        /// </summary>
        internal void Committed(IEnumerable<string> transactionNames,
                                IEnumerable<string> added,
                                IEnumerable<string> modified,
                                IEnumerable<string> deleted)
        {
            if (_sealed) return;
            Events++;

            var part = IsSetup(transactionNames) ? _setup : _fragment;
            if (added != null) foreach (var id in added) part.Added(id);
            if (modified != null) foreach (var id in modified) part.Modified(id);
            if (deleted != null) foreach (var id in deleted) part.Deleted(id);
        }

        /// <summary>An event whose content could not be read - counted, so the report can say so.</summary>
        internal void CouldNotRead()
        {
            if (_sealed) return;
            Unreadable++;
        }

        /// <summary>Stop counting. Describe seals too; this is for a caller that only wants to stop.</summary>
        internal void Seal()
        {
            _sealed = true;
        }

        /// <summary>
        /// An element the model no longer holds, though no commit said it was
        /// deleted - row 5b-1241. Revit's StairsEditScope.Cancel takes back the
        /// stair and the type that Start made without a committed transaction,
        /// so no DocumentChanged ever names them deleted: every refused stair
        /// was reported "2 added ((no category) 2)" and the job kept two
        /// elements that did not exist. Added and gone is nothing, as Deleted
        /// rules; modified and gone is deleted. Called before Describe, by a
        /// caller that has asked the model; ignored once sealed.
        /// </summary>
        internal void Vanished(string id)
        {
            if (_sealed) return;
            _fragment.Vanished(id);
            _setup.Vanished(id);
        }

        /// <summary>
        /// Seal, and read each surviving element's category once.
        ///
        /// `categoryOf` is handed an id as text and answers its category's
        /// name, or null for an element with none. It is called for added and
        /// modified elements only, and once per element per part.
        /// </summary>
        internal Described Describe(Func<string, string> categoryOf)
        {
            _sealed = true;
            return new Described(Events, Unreadable,
                                 _fragment.Read(categoryOf), _setup.Read(categoryOf));
        }

        // -------------------------------------------------------------------

        /// <summary>One part's elements, in the order Revit first named them.</summary>
        private sealed class Part
        {
            private readonly List<string> _order = new List<string>();
            private readonly Dictionary<string, string> _state =
                new Dictionary<string, string>(StringComparer.Ordinal);

            internal void Added(string id)
            {
                if (string.IsNullOrEmpty(id)) return;
                if (!_state.ContainsKey(id)) _order.Add(id);
                _state[id] = "added";
            }

            internal void Modified(string id)
            {
                if (string.IsNullOrEmpty(id)) return;

                // Added stays added, and a deleted id coming back as modified
                // is not a thing Revit does - leave it deleted.
                if (_state.ContainsKey(id)) return;

                _order.Add(id);
                _state[id] = "modified";
            }

            internal void Deleted(string id)
            {
                if (string.IsNullOrEmpty(id)) return;
                string was;
                if (_state.TryGetValue(id, out was) && was == "added")
                {
                    // CAME AND WENT inside the job: nothing to report.
                    _state.Remove(id);
                    _order.Remove(id);
                    return;
                }
                if (!_state.ContainsKey(id)) _order.Add(id);
                _state[id] = "deleted";
            }

            internal void Vanished(string id)
            {
                if (string.IsNullOrEmpty(id) || !_state.ContainsKey(id)) return;
                Deleted(id);
            }

            internal Counted Read(Func<string, string> categoryOf)
            {
                var counted = new Counted();
                foreach (var id in _order)
                {
                    string state;
                    if (!_state.TryGetValue(id, out state)) continue;

                    if (state == "deleted")
                    {
                        counted.Elements.Add(new Element(id, state, null));
                        counted.Deleted++;
                        continue;
                    }

                    string category = null;
                    if (categoryOf != null)
                    {
                        try { category = categoryOf(id); }
                        catch (Exception) { category = null; }
                    }
                    if (string.IsNullOrEmpty(category)) category = NoCategory;

                    counted.Elements.Add(new Element(id, state, category));
                    if (state == "added") counted.Added++;
                    else counted.Modified++;
                }
                return counted;
            }
        }

        /// <summary>One element and what happened to it. Category is null for a deleted one.</summary>
        internal struct Element
        {
            internal readonly string Id;
            internal readonly string Change;
            internal readonly string Category;

            internal Element(string id, string change, string category)
            {
                Id = id;
                Change = change;
                Category = category;
            }
        }

        /// <summary>One part's counts, and every element behind them.</summary>
        internal sealed class Counted
        {
            internal readonly List<Element> Elements = new List<Element>();
            internal int Added;
            internal int Modified;
            internal int Deleted;

            internal int Total { get { return Added + Modified + Deleted; } }

            /// <summary>
            /// Category, added, modified - most elements first, then by name,
            /// so two identical runs describe themselves identically.
            /// </summary>
            internal List<KeyValuePair<string, int[]>> ByCategory()
            {
                var tally = new Dictionary<string, int[]>(StringComparer.Ordinal);
                foreach (var one in Elements)
                {
                    if (one.Category == null) continue;
                    int[] pair;
                    if (!tally.TryGetValue(one.Category, out pair))
                    {
                        pair = new int[2];
                        tally[one.Category] = pair;
                    }
                    if (one.Change == "added") pair[0]++;
                    else pair[1]++;
                }
                var rows = new List<KeyValuePair<string, int[]>>(tally);
                rows.Sort((a, b) =>
                {
                    var bySize = (b.Value[0] + b.Value[1]).CompareTo(a.Value[0] + a.Value[1]);
                    return bySize != 0 ? bySize : string.CompareOrdinal(a.Key, b.Key);
                });
                return rows;
            }

            /// <summary>The first ids the reply carries: added and modified, never deleted.</summary>
            internal List<string> FirstIds(int how)
            {
                var ids = new List<string>();
                foreach (var one in Elements)
                {
                    if (ids.Count >= how) break;
                    if (one.Change == "deleted") continue;
                    ids.Add(one.Id);
                }
                return ids;
            }
        }

        /// <summary>The sealed report: both parts, read, and the two ways it can be written.</summary>
        internal sealed class Described
        {
            internal readonly int Events;
            internal readonly int Unreadable;
            internal readonly Counted Fragment;
            internal readonly Counted Setup;

            internal Described(int events, int unreadable, Counted fragment, Counted setup)
            {
                Events = events;
                Unreadable = unreadable;
                Fragment = fragment;
                Setup = setup;
            }

            internal int Added { get { return Fragment.Added + Setup.Added; } }
            internal int Modified { get { return Fragment.Modified + Setup.Modified; } }
            internal int Deleted { get { return Fragment.Deleted + Setup.Deleted; } }
            internal int Total { get { return Fragment.Total + Setup.Total; } }

            /// <summary>
            /// Whether the counts can be taken as Revit's account of the job.
            ///
            /// NO EVENT AT ALL IS NOT "NOTHING CHANGED". A run that saw no
            /// committed transaction either wrote nothing or ran where the
            /// event did not reach - and which of the two is what
            /// NEEDS-CHECKING owes for a group's inner commits. So the counts
            /// speak only when at least one event arrived and none was
            /// unreadable.
            /// </summary>
            internal bool Witnessed { get { return Events > 0 && Unreadable == 0; } }

            /// <summary>
            /// The reply's fields, already written: the short `changes` object
            /// and the three whole-job numbers beside it, which the dispatcher
            /// reads with Json.ReadLong for the audit line - it reads only the
            /// outermost object, so they cannot live inside `changes`.
            /// </summary>
            internal string[] ReplyFields()
            {
                return new[]
                {
                    "\"changes\": " + Json.Obj(
                        Json.Num("events", Events),
                        Json.Num("unreadable", Unreadable),
                        Json.Bool("witnessed", Witnessed),
                        Json.Num("total", Total),
                        "\"" + FragmentPart + "\": " + PartJson(Fragment),
                        "\"" + SetupPart + "\": " + PartJson(Setup)),
                    Json.Num("elementsAdded", Added),
                    Json.Num("elementsModified", Modified),
                    Json.Num("elementsDeleted", Deleted),
                    Json.Num("changeEvents", Events),
                };
            }

            /// <summary>
            /// Every element, for the run record on disk: the full list the
            /// reply only counts. One flat object per element so a reader can
            /// page it without knowing anything about parts.
            /// </summary>
            internal string ElementsField()
            {
                var items = new List<string>();
                Add(items, FragmentPart, Fragment);
                Add(items, SetupPart, Setup);
                return Json.Arr("elements", items);
            }

            private static void Add(List<string> items, string part, Counted counted)
            {
                foreach (var one in counted.Elements)
                {
                    items.Add(Json.Obj(Json.Str("part", part),
                                       Json.Str("change", one.Change),
                                       Json.Str("id", one.Id),
                                       one.Category == null ? null : Json.Str("category", one.Category)));
                }
            }

            private static string PartJson(Counted counted)
            {
                var rows = new List<string>();
                foreach (var row in counted.ByCategory())
                {
                    rows.Add(Json.Obj(Json.Str("category", row.Key),
                                      Json.Num("added", row.Value[0]),
                                      Json.Num("modified", row.Value[1])));
                }
                var first = new List<string>();
                foreach (var id in counted.FirstIds(FirstIds)) first.Add("\"" + Escaped(id) + "\"");

                return Json.Obj(Json.Num("added", counted.Added),
                                Json.Num("modified", counted.Modified),
                                Json.Num("deleted", counted.Deleted),
                                Json.Num("total", counted.Total),
                                Json.Arr("byCategory", rows),
                                Json.Arr("firstIds", first));
            }

            /// <summary>
            /// An id as a JSON string body. Json.Str writes a name AND a value,
            /// and an array holds bare values - so the escaping is borrowed
            /// from Str by writing a throwaway pair and keeping its value half.
            /// An id is digits from ElementId.ToString(), so this is a guard,
            /// not a path any real id takes.
            /// </summary>
            private static string Escaped(string id)
            {
                var pair = Json.Str("x", id ?? "");
                var at = pair.IndexOf(": \"", StringComparison.Ordinal);
                return at < 0 ? "" : pair.Substring(at + 3, pair.Length - at - 4);
            }
        }
    }
}
