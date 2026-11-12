// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Heron.Revit.Addin;

namespace Heron.ReadBack.TestHost
{
    /// <summary>
    /// The before and after read-back (D-156), checked without Revit.
    ///
    /// RevitReadBack reads each element's values as text; HeronReadBack
    /// compares the two sides and writes the reply's `readBack` field. What
    /// that comparison decides - which values differ, what is NOT READ rather
    /// than unchanged, what the reply carries and what it counts instead - is
    /// what this checks. Whether Revit's values read the same way before and
    /// after a real change is the PC's to prove, and NEEDS-CHECKING says so.
    ///
    /// The last lines it prints are reply fields as JSON, for
    /// tests/test_read_back.py to parse with a real JSON reader - the
    /// hand-written writer is checked by something that did not write it.
    /// </summary>
    internal static class Program
    {
        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private static void Check(bool condition, string what)
        {
            _checks++;
            Console.WriteLine((condition ? "  ok    " : "  FAIL  ") + what);
            if (!condition) Failures.Add(what);
        }

        private static HeronReadBack.Values Element(string category, params string[] pairs)
        {
            var values = new HeronReadBack.Values(category);
            for (var i = 0; i + 1 < pairs.Length; i += 2) values.Add(pairs[i], pairs[i + 1]);
            return values;
        }

        private static Dictionary<string, HeronReadBack.Values> Side()
        {
            return new Dictionary<string, HeronReadBack.Values>(StringComparer.Ordinal);
        }

        private static int Main()
        {
            Console.WriteLine("1. a kept change: the selection was read before, every changed element after");
            var before = Side();
            before["101"] = Element("Ducts", "Offset", "2400 mm", "Comments", "", "Type", "Rect 400x200");
            before["102"] = Element("Ducts", "Offset", "2400 mm", "Comments", "");
            var after = Side();
            after["101"] = Element("Ducts", "Offset", "2700 mm", "Comments", "moved", "Type", "Rect 400x200");
            after["102"] = Element("Ducts", "Offset", "2400 mm", "Comments", "");
            after["103"] = Element("Walls", "Height", "3000 mm");
            var changed = new List<string> { "101", "102", "103", "104" };

            var kept = HeronReadBack.Compare(HeronReadBack.Selection, changed, before, after);
            Check(kept.Changed == 4 && kept.Read == 2,
                  "four counted as changed, two read on both sides");
            Check(kept.NotRead == 2,
                  "and the two with no 'before' are NOT READ - 103 was read after only, 104 not at all");
            Check(kept.WithDifference.Count == 1 && kept.WithDifference[0].Id == "101"
                  && kept.WithDifference[0].Category == "Ducts",
                  "one element has a value that differs, with its category");
            var diffs = kept.WithDifference[0].Differences;
            Check(diffs.Count == 2 && diffs[0].Name == "Offset" && diffs[0].Before == "2400 mm"
                  && diffs[0].After == "2700 mm" && diffs[1].Name == "Comments"
                  && diffs[1].Before == "" && diffs[1].After == "moved",
                  "exactly its two values that moved, in the order they were read, old then new");
            Check(kept.NoValueChanged == 1,
                  "and 102, which Revit counted as changed with no value moved, is counted apart - "
                  + "not dropped, and not called not read");

            Console.WriteLine();
            Console.WriteLine("2. a value on one side only, and a name read twice");
            before = Side();
            before["7"] = Element(null, "Mark", "A", "Mark", "B", "Gone", "x");
            after = Side();
            after["7"] = Element(null, "Mark", "A", "Mark", "C", "New", "y");
            var odd = HeronReadBack.Compare(HeronReadBack.DryRun, new List<string> { "7" }, before, after);
            var seen = odd.WithDifference.Count == 1 ? odd.WithDifference[0].Differences
                                                     : new List<HeronReadBack.Difference>();
            Check(seen.Count == 3 && seen[0].Name == "Mark (2)" && seen[0].Before == "B"
                  && seen[0].After == "C",
                  "a second parameter of the same name is kept as 'Mark (2)', so it cannot hide the first");
            Check(seen.Count == 3 && seen[1].Name == "New" && seen[1].Before == HeronReadBack.None
                  && seen[2].Name == "Gone" && seen[2].After == HeronReadBack.None,
                  "a value present on one side only is a difference, '(none)' on the side that lacked it");

            Console.WriteLine();
            Console.WriteLine("3. nothing read is never 'unchanged'");
            var none = HeronReadBack.Compare(HeronReadBack.DryRun, changed, null, after);
            Check(none.Read == 0 && none.NotRead == 4 && none.NoValueChanged == 0,
                  "with no 'before' at all - a rollback that did not hold - all four are NOT READ");
            var empty = HeronReadBack.Compare(HeronReadBack.Selection, new List<string>(), Side(), Side());
            Check(empty.Changed == 0 && empty.NotRead == 0,
                  "and a run Revit counted nothing changed in reports nothing either way");

            Console.WriteLine();
            Console.WriteLine("4. what the reply carries, and what it counts instead");
            before = Side();
            after = Side();
            var many = new List<string>();
            for (var i = 0; i < 25; i++)
            {
                var id = (1000 + i).ToString();
                many.Add(id);
                before[id] = Element("Pipes", "Offset", "0 mm");
                after[id] = Element("Pipes", "Offset", "100 mm");
            }
            var wide = Element("Pipes");
            var wideBefore = Element("Pipes");
            for (var i = 0; i < 15; i++)
            {
                wideBefore.Add("P" + i, "old");
                wide.Add("P" + i, "new");
            }
            before["2000"] = wideBefore;
            after["2000"] = wide;
            many.Insert(0, "2000");
            var capped = HeronReadBack.Compare(HeronReadBack.DryRun, many, before, after);
            Check(capped.WithDifference.Count == 26,
                  "26 elements differ, and all 26 are counted");
            var field = capped.ReplyField();
            Check(field.Contains("\"elementsMore\": 6") && field.Contains("\"valuesMore\": 3"),
                  "the reply carries " + HeronReadBack.MostShown + " elements and "
                  + HeronReadBack.MostValues + " values each, and counts the rest");
            var longOne = new string('x', 200);
            Check(HeronReadBack.Cut(longOne).EndsWith("... (200 characters)", StringComparison.Ordinal)
                  && HeronReadBack.Cut(longOne).Length < 200,
                  "a value over " + HeronReadBack.LongestValue + " characters is cut, and says how long it was");
            Check(HeronReadBack.Cut("short") == "short" && HeronReadBack.Cut(null) == "",
                  "and a short one, or none, is carried as it is");

            Console.WriteLine();
            Console.WriteLine("5. the reply's fields, for a reader that did not write them");
            before = Side();
            after = Side();
            before["9\"9"] = Element("Cat \"q\"", "Comments", "a\\b");
            after["9\"9"] = Element("Cat \"q\"", "Comments", "line\nnext \"quoted\"");
            var quoted = HeronReadBack.Compare(HeronReadBack.DryRun, new List<string> { "9\"9" }, before, after);

            Console.WriteLine();
            if (Failures.Count > 0)
            {
                Console.WriteLine("FAILED  " + Failures.Count + " of " + _checks + " check(s)");
                foreach (var f in Failures) Console.WriteLine("  - " + f);
            }
            else
            {
                Console.WriteLine("PASS    " + _checks + " check(s)");
            }
            Console.WriteLine("KEPT-JSON {" + kept.ReplyField() + "}");
            Console.WriteLine("CAPPED-JSON {" + field + "}");
            Console.WriteLine("ODD-JSON {" + quoted.ReplyField() + "}");
            return Failures.Count == 0 ? 0 : 1;
        }
    }
}
