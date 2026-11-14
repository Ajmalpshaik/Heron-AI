// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.IO;
using Heron.Bridge;
using Heron.Revit.Addin;

namespace Heron.Activity.TestHost
{
    /// <summary>
    /// What the Activity pane lists (D-163), checked without Revit or WPF: the
    /// run records read into rows, newest first; each change's outcome in a
    /// modeller's words; which row may offer Undo; and the requests its
    /// buttons send. The last line is one request as JSON, for
    /// tests/test_activity_pane.py to parse with a reader that did not write it.
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

        private static string Record(string run, string fragment, string kind, params string[] elements)
        {
            var items = new List<string>();
            foreach (var element in elements)
            {
                var parts = element.Split(':');
                items.Add(Json.Obj(Json.Str("part", "fragment"), Json.Str("change", parts[0]),
                                   Json.Str("id", parts[1]), Json.Str("category", "Ducts")));
            }
            return Json.Obj(Json.Num("record", 1), Json.Str("workflow", run),
                            Json.Str("at", "2026-11-12T14:05:00.0000000Z"), Json.Str("op", "run_fragment_write"),
                            Json.Str("fragment", fragment), Json.Str("document", "Tower MEP"),
                            kind == null ? null : Json.Str("kind", kind),
                            Json.Arr("elements", items), "\"reply\": {\"ok\": true}");
        }

        private static int Main()
        {
            var folder = Path.Combine(Path.GetTempPath(), "heron-activity-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                File.WriteAllText(Path.Combine(folder, "wf-20261112-140000-0001-aaaa.json"),
                    Record("wf-20261112-140000-0001-aaaa", "rename-views", "rolledBack", "modified:11"));
                File.WriteAllText(Path.Combine(folder, "wf-20261112-140500-0002-bbbb.json"),
                    Record("wf-20261112-140500-0002-bbbb", "raise-ducts", "kept",
                           "modified:101", "modified:102", "added:103", "deleted:104"));
                File.WriteAllText(Path.Combine(folder, "wf-20261112-130000-0000-zzzz.json"),
                    Record("wf-20261112-130000-0000-zzzz", "old-one", null, "added:9"));
                File.WriteAllText(Path.Combine(folder, "wf-20261112-150000-0003-cccc.json"), "{not json");
                File.WriteAllText(Path.Combine(folder, "notes.json"), Record("x", "not a run file", "kept"));

                Console.WriteLine("1. the run records, read into rows, newest first");
                var rows = HeronActivity.Read(folder);
                Check(rows.Count == 3, "three records read; one that will not parse and a file that is not a run are left out");
                Check(rows.Count == 3 && rows[0].Fragment == "raise-ducts" && rows[1].Fragment == "rename-views"
                      && rows[2].Fragment == "old-one", "newest first, by the workflow id's own time");
                var newest = rows.Count > 0 ? rows[0] : new HeronActivity.Row();
                Check(newest.Added == 1 && newest.Modified == 2 && newest.Deleted == 1
                      && newest.Counts == "1 added, 2 changed, 1 deleted",
                      "each change's counts, in words");
                Check(string.Join(",", newest.Ids.ToArray()) == "101,102,103",
                      "the ids offered to select are the added and changed ones - never a deleted one");
                Check(newest.Document == "Tower MEP" && newest.AtUtc.HasValue
                      && newest.AtUtc.Value.Hour == 14 && newest.AtUtc.Value.Minute == 5,
                      "with its model and its time");

                Console.WriteLine();
                Console.WriteLine("2. the outcome in a modeller's words, never guessed");
                Check(newest.Outcome == "Kept" && rows[1].Outcome == "Tried, nothing kept",
                      "kept, and a rollback");
                Check(rows[2].Outcome == "Outcome not recorded",
                      "a record from an add-in before D-163 has no outcome at the top - said, not guessed");
                Check(new HeronActivity.Row { Kind = "unchanged" }.Outcome == "Nothing changed"
                      && new HeronActivity.Row { Kind = "notHeld" }.Outcome.Contains("look at the model"),
                      "D-160's empty keep, and a rollback that did not hold");

                Console.WriteLine();
                Console.WriteLine("3. Undo only on the newest change, and only when it was kept");
                Check(ReferenceEquals(HeronActivity.Undoable(rows), newest), "the newest, kept: Undo is offered");
                var older = new List<HeronActivity.Row> { rows[1], rows[0] };
                Check(HeronActivity.Undoable(older) == null,
                      "the newest was rolled back: no Undo on any row - an older one is not the top of the list");
                Check(HeronActivity.Undoable(new List<HeronActivity.Row>()) == null, "no rows: none");

                Console.WriteLine();
                Console.WriteLine("4. what the buttons send");
                var show = HeronActivity.ShowRequest(newest, true);
                Check(Json.ReadString(show, "op") == "show_elements" && Json.ReadString(show, "ids") == "101,102,103"
                      && Json.ReadString(show, "zoom") == "true" && Json.ReadString(show, "document") == "Tower MEP",
                      "Zoom asks show_elements for the change's ids, in its model");
                Check(Json.ReadString(show, "claim") == "no"
                      && Json.ReadString(HeronActivity.UndoRequest(), "claim") == "no"
                      && Json.ReadString(HeronActivity.UndoRequest(), "op") == "undo_last",
                      "and neither ever takes the model from a chat (D-141); Undo is undo_last (D-157)");
                Check(HeronActivity.Said(Json.Ok(Json.Str("said", "Selected 3."))) == "Selected 3."
                      && HeronActivity.Said(Json.Error("undo_refused", "Not on top.")) == "Not on top."
                      && HeronActivity.Said(null) == "Revit did not answer.",
                      "and the status line says the add-in's own words");

                var many = new List<string>();
                for (var i = 0; i < HeronActivity.MostIds + 20; i++) many.Add("added:" + (1000 + i));
                File.WriteAllText(Path.Combine(folder, "wf-20261112-160000-0004-dddd.json"),
                    Record("wf-20261112-160000-0004-dddd", "big", "kept", many.ToArray()));
                var big = HeronActivity.Read(folder)[0];
                Check(big.Ids.Count == HeronActivity.MostIds && big.IdsTotal == HeronActivity.MostIds + 20,
                      "at most " + HeronActivity.MostIds + " ids a row, the total kept beside them");

                Console.WriteLine();
                if (Failures.Count > 0)
                {
                    Console.WriteLine("FAILED  " + Failures.Count + " of " + _checks + " check(s)");
                    foreach (var f in Failures) Console.WriteLine("  - " + f);
                }
                else Console.WriteLine("PASS    " + _checks + " check(s)");
                Console.WriteLine("SHOW-JSON " + show);
                return Failures.Count == 0 ? 0 : 1;
            }
            finally
            {
                try { Directory.Delete(folder, true); } catch (IOException) { }
            }
        }
    }
}
