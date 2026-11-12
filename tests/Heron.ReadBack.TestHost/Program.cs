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

        private static HeronReadBack.ParameterRead P(long id, string name, string value)
        {
            return new HeronReadBack.ParameterRead(id, name, value);
        }

        private static HeronReadBack.Values Read(params HeronReadBack.ParameterRead[] parameters)
        {
            var values = new HeronReadBack.Values("Ducts");
            values.AddById(new List<HeronReadBack.ParameterRead>(parameters));
            return values;
        }

        /// <summary>One element read twice, compared: "name: before -> after | ...", or "".</summary>
        private static string Said(HeronReadBack.Values was, HeronReadBack.Values now)
        {
            var before = Side();
            var after = Side();
            before["8"] = was;
            after["8"] = now;
            var compared = HeronReadBack.Compare(HeronReadBack.DryRun, new List<string> { "8" }, before, after);
            var said = new List<string>();
            foreach (var row in compared.WithDifference)
                foreach (var d in row.Differences)
                    said.Add(d.Name + ": " + d.Before + " -> " + d.After);
            return string.Join(" | ", said.ToArray());
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
            Console.WriteLine("2b. two parameters with one name are paired by their own ids");
            // THE SAME ELEMENT, READ TWICE, ITS PARAMETERS HANDED OVER IN A
            // DIFFERENT ORDER EACH TIME. Only the project parameter "Mark"
            // (id 5001) changed; the built-in "Mark" (id -1001203) did not.
            var paired = Said(
                Read(P(-1001203, "Mark", "A"), P(5001, "Mark", "B"), P(-1010106, "Comments", "same")),
                Read(P(-1010106, "Comments", "same"), P(5001, "Mark", "B2"), P(-1001203, "Mark", "A")));
            Check(paired == "Mark (id 5001): B -> B2",
                  "read in a different order on each side, ONE value differs, and it is named by "
                  + "its own id - found [" + paired + "]");

            // THE SET OF SAME-NAMED PARAMETERS DIFFERS BETWEEN THE TWO READS -
            // a type change. Pairing by ORDER after an id sort said the
            // unchanged X(200) had gone from a to b (review, 2026-11-12).
            var fewer = Said(Read(P(100, "X", "a"), P(200, "X", "b")), Read(P(200, "X", "b")));
            Check(fewer == "X (id 100): a -> (none)",
                  "one of two same-named parameters gone after: THAT one is said as removed, and "
                  + "the one that stayed shows no change - found [" + fewer + "]");
            var more = Said(Read(P(300, "X", "keep")), Read(P(150, "X", "other"), P(300, "X", "keep")));
            Check(more == "X (id 150): (none) -> other",
                  "a same-named parameter with a LOWER id appearing after is said as added, never "
                  + "as a change of the one already there - found [" + more + "]");
            var typeChange = Said(
                Read(P(-1002050, "Type", "Rect 400x200"), P(7001, "Width", "400 mm"), P(7002, "Depth", "200 mm")),
                Read(P(-1002050, "Type", "Round 300"), P(8001, "Diameter", "300 mm")));
            Check(typeChange == "Type: Rect 400x200 -> Round 300 | Diameter: (none) -> 300 mm | "
                                + "Width: 400 mm -> (none) | Depth: 200 mm -> (none)",
                  "a type change: the type's value changed, and each parameter the old type had "
                  + "and the new one has is said as removed or added - found [" + typeChange + "]");

            // A NAME THAT ALREADY ENDS IN "(2)" IS A PARAMETER'S OWN NAME, not
            // the mark of a second one.
            var literal = Said(
                Read(P(1, "Mark", "A"), P(2, "Mark (2)", "L"), P(3, "Mark", "B")),
                Read(P(1, "Mark", "A"), P(2, "Mark (2)", "L"), P(3, "Mark", "B2")));
            Check(literal == "Mark (id 3): B -> B2",
                  "a parameter really named 'Mark (2)' beside two Marks is not confused with "
                  + "either - found [" + literal + "]");

            // AN ID THAT COULD NOT BE READ HAS NOTHING TO PAIR BY. Two such
            // with one name are compared as a set of values, so the same two
            // values in another order are not a change.
            var unread = Said(
                Read(P(HeronReadBack.NoId, "Mark", "A"), P(HeronReadBack.NoId, "Mark", "B")),
                Read(P(HeronReadBack.NoId, "Mark", "B"), P(HeronReadBack.NoId, "Mark", "A")));
            Check(unread == "",
                  "two parameters whose ids could not be read, the same two values in another "
                  + "order: no difference - found [" + unread + "]");
            var unreadChanged = Said(
                Read(P(HeronReadBack.NoId, "Mark", "A"), P(HeronReadBack.NoId, "Mark", "B")),
                Read(P(HeronReadBack.NoId, "Mark", "C"), P(HeronReadBack.NoId, "Mark", "A")));
            Check(unreadChanged.Contains("id not read") && unreadChanged.Contains("B -> C")
                  && !unreadChanged.Contains("A ->") && !unreadChanged.Contains("-> A"),
                  "and when one of them did change, the line says its id was not read and never "
                  + "shows the unchanged value as changed - found [" + unreadChanged + "]");

            // A NAME ONLY ONE PARAMETER HAS IS PRINTED BARE, and Heron's own
            // "Type" keeps its name beside a parameter called Type.
            var plain = Said(Read(P(-1010106, "Comments", "")), Read(P(-1010106, "Comments", "moved")));
            Check(plain == "Comments:  -> moved",
                  "a name one parameter has is printed as it is, with no id - found [" + plain + "]");
            var ownBefore = new HeronReadBack.Values("Ducts");
            ownBefore.Add("Type", "Rect");
            ownBefore.AddById(new List<HeronReadBack.ParameterRead> { P(-1002050, "Type", "Rect") });
            var ownAfter = new HeronReadBack.Values("Ducts");
            ownAfter.Add("Type", "Rect");
            ownAfter.AddById(new List<HeronReadBack.ParameterRead> { P(-1002050, "Type", "Round") });
            var own = Said(ownBefore, ownAfter);
            Check(own == "Type (id -1002050): Rect -> Round",
                  "a parameter that shares a name with a value Heron reads itself is told apart "
                  + "by its id, and Heron's own keeps the bare name - found [" + own + "]");
            var tied = Said(Read(P(7, "Zeta", "z"), P(7, "Alpha", "a")), Read(P(7, "Alpha", "a2"), P(7, "Zeta", "z")));
            Check(tied == "Alpha: a -> a2",
                  "two that claim one id are still told apart by name - found [" + tied + "]");
            var noneRead = new HeronReadBack.Values(null);
            noneRead.AddById(null);
            Check(Said(noneRead, new HeronReadBack.Values(null)) == "",
                  "and no parameters at all is nothing, not a fault");

            Console.WriteLine();
            Console.WriteLine("2c. a text parameter with no shown text falls back to its stored text");
            Check(!HeronReadBack.HasText(null, true) && !HeronReadBack.HasText(null, false),
                  "no shown text at all - null - is never the value, for any parameter");
            Check(!HeronReadBack.HasText("", true),
                  "an EMPTY shown text for a parameter that stores text is not the value either: "
                  + "its stored text is read");
            Check(HeronReadBack.HasText("", false),
                  "an empty shown text for a number, a tick box or a reference is kept as Revit's answer");
            Check(HeronReadBack.HasText("Level 1", true) && HeronReadBack.HasText("2400 mm", false),
                  "and a shown text that says something is the value");

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
