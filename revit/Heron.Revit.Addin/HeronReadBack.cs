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
    /// What each changed element's values were before a write and what they
    /// are after it - the comparing and the wording, with nothing Revit-shaped
    /// in it. D-156.
    ///
    /// WHY IT EXISTS. The change report (HeronChangeReport, 5b-407) says WHICH
    /// elements Revit counted as changed, and nothing about HOW. "12 ducts
    /// modified" reads the same whether their offset moved 300 mm or Revit
    /// only regenerated them because a joined wall moved. This says, element
    /// by element, which value went from what to what - and says plainly when
    /// an element Revit counted as changed shows no difference in any value
    /// Heron reads, which is how a hosted, joined or pinned element that did
    /// not really move is told apart from one that did.
    ///
    /// TWO WAYS THE "BEFORE" IS READ, AND THE REPLY SAYS WHICH (mode):
    ///
    ///   dryRun     The run is rolled back anyway (D-55, D-155), so every
    ///              element Revit counted as changed is read inside the group
    ///              (after), and read again once the rollback has held
    ///              (before). Complete up to MostRead, and exact - nothing is
    ///              predicted.
    ///   selection  A run that keeps cannot read its "before" afterwards: the
    ///              old values are gone the moment it runs, and running twice
    ///              is what D-126 refused. So the modeller's selection is read
    ///              before the run starts, and only those elements can be
    ///              compared. Every other changed element is counted as not
    ///              read, and the chat is told a dry run would show them.
    ///
    /// SPLIT OUT SO IT CAN BE PROVED WITHOUT REVIT, the move HeronChangeReport
    /// makes: this names no Autodesk type, and tests/Heron.ReadBack.TestHost
    /// links it BY SOURCE with the bridge's Json.cs. RevitReadBack.cs is the
    /// half that reads the elements.
    /// </summary>
    internal static class HeronReadBack
    {
        /// <summary>Elements read on each side, at most. A read is every parameter of the element.</summary>
        internal const int MostRead = 50;

        /// <summary>Elements with a difference the reply carries, at most.</summary>
        internal const int MostShown = 20;

        /// <summary>Changed values per element the reply carries, at most.</summary>
        internal const int MostValues = 12;

        /// <summary>The longest value carried whole; a longer one is cut and says so.</summary>
        internal const int LongestValue = 120;

        internal const string DryRun = "dryRun";
        internal const string Selection = "selection";

        /// <summary>
        /// One element's values, in the order they were read, by name. A name
        /// read twice - two parameters Revit gives the same name - keeps both,
        /// the second as "Name (2)", so neither hides the other.
        /// </summary>
        internal sealed class Values
        {
            private readonly List<string> _order = new List<string>();
            private readonly Dictionary<string, string> _byName =
                new Dictionary<string, string>(StringComparer.Ordinal);

            internal Values(string category)
            {
                Category = category;
            }

            internal string Category { get; private set; }

            internal void Add(string name, string value)
            {
                if (string.IsNullOrEmpty(name)) return;
                var key = name;
                for (var n = 2; _byName.ContainsKey(key); n++) key = name + " (" + n + ")";
                _order.Add(key);
                _byName[key] = value ?? "";
            }

            internal IList<string> Names { get { return _order; } }

            internal bool TryGet(string name, out string value)
            {
                return _byName.TryGetValue(name, out value);
            }
        }

        /// <summary>One value that differs: its name, and what it was and is.</summary>
        internal struct Difference
        {
            internal readonly string Name;
            internal readonly string Before;
            internal readonly string After;

            internal Difference(string name, string before, string after)
            {
                Name = name;
                Before = before;
                After = after;
            }
        }

        /// <summary>One element read on both sides, and every value that differs.</summary>
        internal sealed class Row
        {
            internal readonly string Id;
            internal readonly string Category;
            internal readonly List<Difference> Differences = new List<Difference>();

            internal Row(string id, string category)
            {
                Id = id;
                Category = category;
            }
        }

        /// <summary>The comparison, sealed, and the one way it is written.</summary>
        internal sealed class Compared
        {
            internal string Mode;

            /// <summary>Elements Revit counted as changed - the change report's number.</summary>
            internal int Changed;

            /// <summary>Read on both sides.</summary>
            internal int Read;

            /// <summary>Read on both sides, and at least one value differs - in Revit's order.</summary>
            internal readonly List<Row> WithDifference = new List<Row>();

            /// <summary>Read on both sides, and no value Heron reads differs.</summary>
            internal int NoValueChanged;

            /// <summary>Counted as changed, and not read on one side or the other.</summary>
            internal int NotRead { get { return Math.Max(0, Changed - Read); } }

            /// <summary>
            /// The reply's field: `"readBack": {...}`. The rows with a
            /// difference, up to MostShown, each with up to MostValues values;
            /// what was left out is counted beside them, never dropped silently.
            /// </summary>
            internal string ReplyField()
            {
                var rows = new List<string>();
                for (var i = 0; i < WithDifference.Count && i < MostShown; i++)
                {
                    var row = WithDifference[i];
                    var values = new List<string>();
                    for (var j = 0; j < row.Differences.Count && j < MostValues; j++)
                    {
                        var d = row.Differences[j];
                        values.Add(Json.Obj(Json.Str("name", d.Name),
                                            Json.Str("before", Cut(d.Before)),
                                            Json.Str("after", Cut(d.After))));
                    }
                    rows.Add(Json.Obj(
                        Json.Str("id", row.Id),
                        string.IsNullOrEmpty(row.Category) ? null : Json.Str("category", row.Category),
                        Json.Arr("values", values),
                        Json.Num("valuesMore", Math.Max(0, row.Differences.Count - MostValues))));
                }

                return "\"readBack\": " + Json.Obj(
                    Json.Str("mode", Mode),
                    Json.Num("changed", Changed),
                    Json.Num("read", Read),
                    Json.Num("withDifference", WithDifference.Count),
                    Json.Num("noValueChanged", NoValueChanged),
                    Json.Num("notRead", NotRead),
                    Json.Arr("elements", rows),
                    Json.Num("elementsMore", Math.Max(0, WithDifference.Count - MostShown)));
            }
        }

        /// <summary>
        /// Compare what was read before the write with what was read after it.
        ///
        /// `changed` is every element Revit counted as modified, as text, in
        /// the order it named them; `before` and `after` are what each side
        /// managed to read, by the same text. An element missing from either
        /// side is NOT READ - never "unchanged", which would be a claim about
        /// a value nobody looked at. A value present on one side only (a
        /// parameter the change added or removed) is a difference, shown as
        /// "(none)" on the side that lacked it.
        /// </summary>
        internal static Compared Compare(string mode, IList<string> changed,
                                         IDictionary<string, Values> before,
                                         IDictionary<string, Values> after)
        {
            var result = new Compared { Mode = mode, Changed = changed == null ? 0 : changed.Count };
            if (changed == null || before == null || after == null) return result;

            foreach (var id in changed)
            {
                Values was, now;
                if (id == null || !before.TryGetValue(id, out was) || was == null
                    || !after.TryGetValue(id, out now) || now == null)
                    continue;

                result.Read++;
                var row = new Row(id, now.Category ?? was.Category);

                foreach (var name in now.Names)
                {
                    string a, b;
                    now.TryGet(name, out a);
                    if (!was.TryGet(name, out b)) b = null;
                    if (b == null) row.Differences.Add(new Difference(name, None, a));
                    else if (!string.Equals(a, b, StringComparison.Ordinal))
                        row.Differences.Add(new Difference(name, b, a));
                }
                foreach (var name in was.Names)
                {
                    string ignored;
                    if (now.TryGet(name, out ignored)) continue;
                    string b;
                    was.TryGet(name, out b);
                    row.Differences.Add(new Difference(name, b, None));
                }

                if (row.Differences.Count == 0) result.NoValueChanged++;
                else result.WithDifference.Add(row);
            }
            return result;
        }

        /// <summary>The side a value was missing from.</summary>
        internal const string None = "(none)";

        /// <summary>A value cut to LongestValue, saying it was cut.</summary>
        internal static string Cut(string value)
        {
            if (value == null) return "";
            if (value.Length <= LongestValue) return value;
            return value.Substring(0, LongestValue) + "... (" + value.Length + " characters)";
        }
    }
}
