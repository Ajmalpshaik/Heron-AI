// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Heron.Bridge;
using Heron.Revit.Addin;

namespace Heron.ChangeReport.TestHost
{
    /// <summary>
    /// The fragment write path's change report, checked without Revit.
    ///
    /// Until this report Heron counted no element a write touched: a reply
    /// carried the fragment's own variables and nothing Revit said, and the
    /// chat was handed every one of them (FRAGMENT-ISSUES 5b-407). Now
    /// RevitFragment listens to DocumentChanged for the length of one write
    /// and hands each committed transaction to HeronChangeReport as text. What
    /// that class decides - whose change it was, how each element is counted,
    /// what the reply and the run record hold - is what this checks. Whether
    /// Revit raises the event where the write path needs it is the PC's to
    /// prove, and NEEDS-CHECKING says so.
    ///
    /// The last two lines it prints are the reply's fields and the record's
    /// element list as JSON, for tests/test_change_report.py to parse with a
    /// real JSON reader - the hand-written writer is checked by something
    /// that did not write it.
    /// </summary>
    internal static class Program
    {
        private const string Label = "Heron: create-family-extrusion";

        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private static void Check(bool condition, string what)
        {
            _checks++;
            Console.WriteLine((condition ? "  ok    " : "  FAIL  ") + what);
            if (!condition) Failures.Add(what);
        }

        private static string[] Ids(params string[] ids)
        {
            return ids;
        }

        private static string[] Names(params string[] names)
        {
            return names;
        }

        private static readonly string[] None = new string[0];

        private static int Main()
        {
            Console.WriteLine("The fragment write path's change report, run rather than read - no Revit.");

            Whose();
            Counting();
            Sealing();
            Categories();
            FirstIds();
            Written();
            Witnessed();
            Vanished();

            Console.WriteLine();
            Console.WriteLine(_checks + " check(s), " + Failures.Count + " failed.");
            if (Failures.Count == 0) return 0;
            foreach (var failure in Failures) Console.WriteLine("  - " + failure);
            return 1;
        }

        // ------------------------------------------------------------------
        private static void Whose()
        {
            Console.WriteLine();
            Console.WriteLine("1. whose change it was is read off the transaction's name");
            var report = new HeronChangeReport(Label);

            Check(!report.IsSetup(Names(Label)),
                  "the fragment's own transaction, named `label`, is the fragment's");
            Check(report.IsSetup(Names(Label + " - make-plane")),
                  "`label - stepName` is a setup step's - RunSetupSteps names them so");
            Check(!report.IsSetup(Names("Heron: other-fragment - make-plane")),
                  "another fragment's setup name is not this one's");
            Check(!report.IsSetup(Names("Extrusion")) && !report.IsSetup(null)
                  && !report.IsSetup(None),
                  "a name an edit-scope fragment chose itself, or none at all, is the fragment's");
            Check(report.IsSetup(Names("Extrusion", Label + " - make-plane")),
                  "and any name of a setup step's files the whole commit under setup");

            // A FRAGMENT WHOSE OWN NAME HOLDS " - " is still not its own setup.
            var dashed = new HeronChangeReport("Heron: a - b");
            Check(!dashed.IsSetup(Names("Heron: a - b")),
                  "a fragment named with ' - ' in it is not mistaken for a setup step");
            Check(dashed.IsSetup(Names("Heron: a - b - step")),
                  "while its own setup steps still are");
        }

        // ------------------------------------------------------------------
        private static void Counting()
        {
            Console.WriteLine();
            Console.WriteLine("2. within one part an element is counted once, by what happened to it overall");
            var report = new HeronChangeReport(Label);

            report.Committed(Names(Label), Ids("1", "2", "3"), None, None);
            report.Committed(Names(Label), None, Ids("1", "4"), None);
            report.Committed(Names(Label), None, Ids("4"), None);
            report.Committed(Names(Label), None, None, Ids("2", "5"));
            report.Committed(Names(Label), None, Ids("6"), Ids("6"));
            var described = report.Describe(id => "Walls");
            var fragment = described.Fragment;

            Check(fragment.Added == 2,
                  "added then modified is ADDED, and added then deleted is nothing - 2 added, found "
                  + fragment.Added);
            Check(fragment.Modified == 1,
                  "an element modified twice is modified ONCE - 1, found " + fragment.Modified);
            Check(fragment.Deleted == 2,
                  "deleted, and modified-then-deleted, are DELETED - 2, found " + fragment.Deleted);
            var order = new List<string>();
            foreach (var one in fragment.Elements) order.Add(one.Id + ":" + one.Change);
            Check(string.Join(",", order) == "1:added,3:added,4:modified,5:deleted,6:deleted",
                  "in the order Revit first named them, the transient one gone - " + string.Join(",", order));
            Check(fragment.Total == 5 && described.Total == 5,
                  "and the total is what is left: 5, found " + described.Total);
            Check(described.Events == 5, "every commit counted as an event - 5, found " + described.Events);

            Console.WriteLine();
            Console.WriteLine("3. a setup step's changes are counted apart, and each part keeps its own");
            var both = new HeronChangeReport(Label);
            both.Committed(Names(Label + " - make-plane"), Ids("10", "11"), None, None);
            both.Committed(Names(Label), Ids("12"), Ids("10"), Ids("11"));
            var split = both.Describe(id => "Reference Planes");
            Check(split.Setup.Added == 2 && split.Setup.Total == 2,
                  "what the setup step made is the setup's: 2 added");
            Check(split.Fragment.Added == 1 && split.Fragment.Modified == 1
                  && split.Fragment.Deleted == 1,
                  "and an element the setup made and the fragment then changed or deleted is the "
                  + "FRAGMENT's modification or deletion - exactly what happened");
            Check(split.Added == 3 && split.Modified == 1 && split.Deleted == 1 && split.Total == 5,
                  "the whole job's counts are the two parts added together");
        }

        // ------------------------------------------------------------------
        private static void Vanished()
        {
            Console.WriteLine();
            Console.WriteLine("9. an element the model no longer holds, with no commit saying so (5b-1241)");
            var report = new HeronChangeReport(Label);
            // A stair edit scope: Start adds the stair and a type, Cancel takes
            // both back with no committed transaction - no Deleted ever comes.
            report.Committed(Names("Heron: create stairs"), Ids("345908", "345909"), None, None);
            report.Committed(Names(Label), Ids("20"), Ids("30", "31"), None);
            report.Vanished("345908");
            report.Vanished("345909");
            report.Vanished("31");
            report.Vanished("99");
            var described = report.Describe(id => "Stairs");
            Check(described.Fragment.Added == 1 && described.Fragment.Modified == 1,
                  "added and gone is NOTHING, as added-then-deleted is - 1 added, 1 modified left, found "
                  + described.Fragment.Added + " added, " + described.Fragment.Modified + " modified");
            Check(described.Fragment.Deleted == 1,
                  "modified and gone is DELETED - 1, found " + described.Fragment.Deleted);
            Check(described.Total == 3,
                  "and an id the run never named changes nothing - 3 in all, found " + described.Total);

            var late = new HeronChangeReport(Label);
            late.Committed(Names(Label), Ids("1"), None, None);
            late.Describe(id => "Walls");
            late.Vanished("1");
            Check(late.Describe(id => "Walls").Fragment.Added == 1,
                  "once described the report is sealed, and a later Vanished changes nothing");
        }

        // ------------------------------------------------------------------
        private static void Sealing()
        {
            Console.WriteLine();
            Console.WriteLine("4. nothing after Describe is counted - a rollback names the same elements reversed");
            var report = new HeronChangeReport(Label);
            report.Committed(Names(Label), Ids("1"), None, None);
            var first = report.Describe(id => "Walls");
            report.Committed(Names(Label), None, None, Ids("1"));
            report.CouldNotRead();
            var again = report.Describe(id => "Walls");
            Check(first.Fragment.Added == 1 && again.Fragment.Added == 1 && again.Fragment.Deleted == 0,
                  "an event after the report was described changes nothing in it");
            Check(again.Events == 1 && again.Unreadable == 0,
                  "and is not counted as an event, readable or not");

            var stopped = new HeronChangeReport(Label);
            stopped.Seal();
            stopped.Committed(Names(Label), Ids("9"), None, None);
            Check(stopped.Describe(null).Total == 0 && stopped.Events == 0,
                  "Seal alone stops the counting too");
        }

        // ------------------------------------------------------------------
        private static void Categories()
        {
            Console.WriteLine();
            Console.WriteLine("5. categories are read once per element, after the work, and never for a deleted one");
            var asked = new List<string>();
            var report = new HeronChangeReport(Label);
            report.Committed(Names(Label), Ids("1", "2", "3", "4"), Ids("5"), Ids("6"));
            report.Committed(Names(Label), None, Ids("1", "5"), None);
            var described = report.Describe(id =>
            {
                asked.Add(id);
                switch (id)
                {
                    case "1": return "Generic Models";
                    case "2": return "Lines";
                    case "3": return "Lines";
                    case "4": return null;
                    case "5": throw new InvalidOperationException("not readable");
                    default: return "Doors";
                }
            });
            asked.Sort(string.CompareOrdinal);
            Check(string.Join(",", asked) == "1,2,3,4,5",
                  "asked once for each added and modified element and never for the deleted one - "
                  + string.Join(",", asked));

            var rows = described.Fragment.ByCategory();
            var said = new List<string>();
            foreach (var row in rows) said.Add(row.Key + " " + row.Value[0] + "/" + row.Value[1]);
            Check(string.Join("; ", said) == "(no category) 1/1; Lines 2/0; Generic Models 1/0",
                  "an element with no category, or one that threw, is counted under "
                  + HeronChangeReport.NoCategory + "; most elements first, then by name - "
                  + string.Join("; ", said));
            Check(described.Fragment.Deleted == 1,
                  "and the deleted one is still counted");
        }

        // ------------------------------------------------------------------
        private static void FirstIds()
        {
            Console.WriteLine();
            Console.WriteLine("6. the reply carries the first " + HeronChangeReport.FirstIds
                              + " ids of a part; the record carries every one");
            var report = new HeronChangeReport(Label);
            var many = new List<string>();
            for (var i = 1; i <= 25; i++) many.Add("deleted-" + i);
            report.Committed(Names(Label), None, None, many);
            var added = new List<string>();
            for (var i = 1; i <= 25; i++) added.Add((100 + i).ToString());
            report.Committed(Names(Label), added, None, None);
            var described = report.Describe(id => "Walls");

            var first = described.Fragment.FirstIds(HeronChangeReport.FirstIds);
            Check(first.Count == HeronChangeReport.FirstIds,
                  "25 added elements give " + HeronChangeReport.FirstIds + " ids, found " + first.Count);
            Check(first.Count > 0 && first[0] == "101" && !string.Join(",", first).Contains("deleted"),
                  "and a deleted id is never among them, even when it came first");
            Check(described.Fragment.Elements.Count == 50,
                  "while every one of the 50 is kept for the record - found "
                  + described.Fragment.Elements.Count);
        }

        // ------------------------------------------------------------------
        private static void Written()
        {
            Console.WriteLine();
            Console.WriteLine("7. the reply's whole-job counts reach the audit line's reader; the record lists every element");
            var report = new HeronChangeReport(Label);
            report.Committed(Names(Label + " - make-plane"), Ids("7"), None, None);
            report.Committed(Names(Label), Ids("8", "9"), Ids("7"), Ids("4"));
            var described = report.Describe(id => id == "7" ? "Reference Planes" : "Generic Models");

            var top = "{\"ok\": true, " + string.Join(", ", described.ReplyFields()) + "}";
            Check(Heron.Bridge.Json.ReadLong(top, "elementsAdded") == 3
                  && Heron.Bridge.Json.ReadLong(top, "elementsModified") == 1
                  && Heron.Bridge.Json.ReadLong(top, "elementsDeleted") == 1
                  && Heron.Bridge.Json.ReadLong(top, "changeEvents") == 2,
                  "Json.ReadLong reads all four whole-job numbers off the reply");
            Check(Heron.Bridge.Json.ReadLong(top, "added") == null
                  && Heron.Bridge.Json.ReadLong(top, "total") == null,
                  "and never a count nested inside `changes` - the reader walks only the "
                  + "outermost object, which is why the four sit beside it");

            var elements = Heron.Bridge.Json.ReadObjectArray("{" + described.ElementsField() + "}",
                                                            "elements");
            Check(elements != null && elements.Count == 5,
                  "the record's element list reads back as 5 flat objects - found "
                  + (elements == null ? "none" : elements.Count.ToString()));
            if (elements != null && elements.Count == 5)
            {
                Check(elements[0]["part"] == "fragment" && elements[0]["change"] == "added"
                      && elements[0]["id"] == "8" && elements[0]["category"] == "Generic Models",
                      "each with its part, what happened, its id and its category");
                Check(elements[2]["change"] == "modified" && elements[2]["id"] == "7",
                      "an element the setup made and the fragment changed is the fragment's "
                      + "modification");
                Check(!elements[3].ContainsKey("category") && elements[3]["change"] == "deleted",
                      "a deleted one with no category at all - nothing to read it from");
                Check(elements[4]["part"] == "setup" && elements[4]["id"] == "7"
                      && elements[4]["change"] == "added",
                      "and the setup step's own addition after the fragment's");
            }

            // For the Python half to parse with a JSON reader of its own.
            var quoted = new HeronChangeReport(Label);
            quoted.Committed(Names(Label), Ids("a\"b\\c"), None, None);
            var odd = quoted.Describe(id => "Cat \"quoted\"");
            Console.WriteLine("REPLY-JSON {" + string.Join(", ", described.ReplyFields()) + "}");
            Console.WriteLine("ODD-JSON {" + string.Join(", ", odd.ReplyFields()) + ", "
                              + odd.ElementsField() + "}");
        }

        // ------------------------------------------------------------------
        private static void Witnessed()
        {
            Console.WriteLine();
            Console.WriteLine("8. no event at all is NOT 'nothing changed'");
            var silent = new HeronChangeReport(Label).Describe(null);
            Check(!silent.Witnessed && silent.Total == 0,
                  "a job that saw no committed transaction is not witnessed, so its zero "
                  + "speaks for nothing");
            var top = "{" + string.Join(", ", silent.ReplyFields()) + "}";
            Check(top.Contains("\"witnessed\": false"),
                  "and the reply says so in a field a reader can branch on");

            var torn = new HeronChangeReport(Label);
            torn.Committed(Names(Label), Ids("1"), None, None);
            torn.CouldNotRead();
            var described = torn.Describe(id => "Walls");
            Check(!described.Witnessed && described.Unreadable == 1,
                  "and one event that could not be read makes every count incomplete, said as such");

            var seen = new HeronChangeReport(Label);
            seen.Committed(Names(Label), None, None, None);
            Check(seen.Describe(null).Witnessed,
                  "while a commit that changed no element is witnessed: Revit said, and it was nothing");
        }
    }
}
