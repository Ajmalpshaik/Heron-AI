// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
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
    ///              read, and the chat is told so - never that it is unchanged.
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

        /// <summary>The id of a parameter whose id could not be read as a number.</summary>
        internal const long NoId = long.MaxValue;

        /// <summary>
        /// One parameter as it was read: the number Revit gives the parameter
        /// itself (Parameter.Id - a built-in's own number, or the id of the
        /// project or shared parameter behind it), its name and its text.
        /// </summary>
        internal struct ParameterRead
        {
            internal readonly long Id;
            internal readonly string Name;
            internal readonly string Value;

            internal ParameterRead(long id, string name, string value)
            {
                Id = id;
                Name = name;
                Value = value;
            }
        }

        /// <summary>
        /// One element's values as one side of a comparison read them.
        ///
        /// A VALUE HERON READS ITSELF - the element's name, its type's name,
        /// where it sits - is held by its name (Add). A name added twice keeps
        /// both, the second as "Name (2)", so neither hides the other.
        ///
        /// A PARAMETER IS HELD BY ITS OWN ID, AND THE TWO SIDES ARE PAIRED BY
        /// THAT ID - NEVER BY A NAME, AND NEVER BY AN ORDER (AddById). Two
        /// parameters may share a name: a built-in and a project parameter
        /// both called "Mark". The two sides of a comparison are two separate
        /// reads of the element, nothing documents that ParameterSet lists its
        /// parameters in the same order twice, and the SET of them can differ
        /// - a type change takes the old type's parameters away and brings the
        /// new one's. Three ways this was wrong before it was right:
        ///   - numbered by read order ("Mark", "Mark (2)"): a different order
        ///     on the second read paired two different parameters;
        ///   - numbered by order AFTER a sort by id (the first repair): with
        ///     X(100)=a and X(200)=b before and only X(200)=b after, the
        ///     unchanged one printed "X: a -> b" and "X (2): b -> (none)";
        ///   - both: a parameter really named "Mark (2)" pushed the second
        ///     Mark to "Mark (3)".
        /// Found in two reviews, 2026-11-12; none seen in Revit. Now the key
        /// is the id, so a parameter on one side only is said as added or
        /// removed ("(none)" on the side that lacked it) and never as a change
        /// of another.
        ///
        /// AN ID THAT COULD NOT BE READ (NoId) HAS NOTHING TO PAIR BY. Two such
        /// with one name are paired in the order of their VALUES, so the same
        /// values in another order are no difference; when one did change the
        /// pairing is a guess, and its label says the id was not read.
        /// </summary>
        internal sealed class Values
        {
            /// <summary>One value held: what it is paired by, what it is called, its text.</summary>
            internal sealed class Item
            {
                internal string Key;
                internal string Name;
                internal string Value;

                /// <summary>True for a parameter; false for a value Heron reads itself.</summary>
                internal bool Parameter;
                internal long Id;

                /// <summary>Which of the parameters sharing this id AND name it is, from 1.</summary>
                internal int Nth;
            }

            private readonly List<Item> _items = new List<Item>();
            private readonly Dictionary<string, Item> _byKey =
                new Dictionary<string, Item>(StringComparer.Ordinal);
            private readonly Dictionary<string, int> _named =
                new Dictionary<string, int>(StringComparer.Ordinal);

            internal Values(string category)
            {
                Category = category;
            }

            internal string Category { get; private set; }

            internal void Add(string name, string value)
            {
                if (string.IsNullOrEmpty(name)) return;
                var shown = name;
                for (var n = 2; _byKey.ContainsKey("n:" + shown); n++) shown = name + " (" + n + ")";
                Hold(new Item { Key = "n:" + shown, Name = shown, Value = value ?? "" });
            }

            /// <summary>
            /// Every parameter of one element, each held by its own id. The
            /// order they are held in is the order of their ids - lowest
            /// first, then by name, then by value - so a reply lists them the
            /// same way whatever order a read came in; the PAIRING does not
            /// depend on it.
            /// </summary>
            internal void AddById(IList<ParameterRead> parameters)
            {
                if (parameters == null) return;

                // List.Sort is not a stable sort, so where each one stood in
                // the read is the last word of the comparison rather than
                // something the sort is trusted to keep.
                var sorted = new List<KeyValuePair<int, ParameterRead>>();
                for (var i = 0; i < parameters.Count; i++)
                    sorted.Add(new KeyValuePair<int, ParameterRead>(i, parameters[i]));
                sorted.Sort((a, b) =>
                {
                    var byId = a.Value.Id.CompareTo(b.Value.Id);
                    if (byId != 0) return byId;
                    var byName = string.CompareOrdinal(a.Value.Name, b.Value.Name);
                    if (byName != 0) return byName;
                    var byValue = string.CompareOrdinal(a.Value.Value ?? "", b.Value.Value ?? "");
                    return byValue != 0 ? byValue : a.Key.CompareTo(b.Key);
                });

                foreach (var one in sorted)
                {
                    var read = one.Value;
                    if (string.IsNullOrEmpty(read.Name)) continue;
                    var id = read.Id.ToString(CultureInfo.InvariantCulture);
                    var nth = 1;
                    while (_byKey.ContainsKey("p:" + id + ":" + nth + ":" + read.Name)) nth++;
                    Hold(new Item
                    {
                        Key = "p:" + id + ":" + nth + ":" + read.Name,
                        Name = read.Name,
                        Value = read.Value ?? "",
                        Parameter = true,
                        Id = read.Id,
                        Nth = nth
                    });
                }
            }

            private void Hold(Item item)
            {
                _items.Add(item);
                _byKey[item.Key] = item;
                int held;
                _named.TryGetValue(item.Name, out held);
                _named[item.Name] = held + 1;
            }

            /// <summary>Every value, in the order it was held.</summary>
            internal IList<Item> Items { get { return _items; } }

            internal bool TryGet(string key, out Item item)
            {
                return _byKey.TryGetValue(key, out item);
            }

            /// <summary>How many values on this side go by this name.</summary>
            internal int CountOf(string name)
            {
                int held;
                return name != null && _named.TryGetValue(name, out held) ? held : 0;
            }
        }

        /// <summary>
        /// What a value is called on the reply. Its name alone, when no other
        /// value on EITHER side goes by it - which is nearly every value. A
        /// parameter whose name is shared says which one it is by its id,
        /// "Mark (id 5001)", because the name cannot; a value Heron reads
        /// itself keeps its name. Decided from both sides, so the same
        /// parameter is called the same thing whether or not its namesake is
        /// there on the other.
        /// </summary>
        internal static string Label(Values.Item item, Values was, Values now)
        {
            if (!item.Parameter) return item.Name;
            var shared = Math.Max(was == null ? 0 : was.CountOf(item.Name),
                                  now == null ? 0 : now.CountOf(item.Name)) > 1;
            if (!shared) return item.Name;
            return item.Name + " (id "
                 + (item.Id == NoId ? "not read" : item.Id.ToString(CultureInfo.InvariantCulture))
                 + (item.Nth > 1 ? ", " + item.Nth.ToString(CultureInfo.InvariantCulture) : "")
                 + ")";
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

                // PAIRED BY KEY - a parameter's own id - never by name or by
                // place (Values). What only one side holds is added or removed.
                foreach (var item in now.Items)
                {
                    Values.Item old;
                    if (!was.TryGet(item.Key, out old))
                        row.Differences.Add(new Difference(Label(item, was, now), None, item.Value));
                    else if (!string.Equals(item.Value, old.Value, StringComparison.Ordinal))
                        row.Differences.Add(new Difference(Label(item, was, now), old.Value, item.Value));
                }
                foreach (var item in was.Items)
                {
                    Values.Item kept;
                    if (now.TryGet(item.Key, out kept)) continue;
                    row.Differences.Add(new Difference(Label(item, was, now), item.Value, None));
                }

                if (row.Differences.Count == 0) result.NoValueChanged++;
                else result.WithDifference.Add(row);
            }
            return result;
        }

        /// <summary>
        /// Whether the text Revit shows for a parameter (AsValueString) is the
        /// text to compare, or its stored value must be read instead.
        ///
        /// Null always falls back. AND SO DOES AN EMPTY TEXT FOR A PARAMETER
        /// THAT STORES TEXT: where AsValueString gives "" for a text
        /// parameter that holds something, "" before and "" after would hide
        /// a Comments or a Mark that changed. For a number, a tick box or a
        /// reference an empty text is kept as Revit's own answer - what its
        /// stored value means is not Heron's to format. Which releases answer
        /// "" there has not been seen in Revit: NEEDS-CHECKING Group NS.
        /// </summary>
        internal static bool HasText(string shown, bool isText)
        {
            return shown != null && !(isText && shown.Length == 0);
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
