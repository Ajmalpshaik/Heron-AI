// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Heron.Bridge;
using Heron.Revit.Addin;

namespace Heron.Activity.TestHost
{
    /// <summary>
    /// What the Activity pane lists (D-163), checked without Revit or WPF: the
    /// run records read into rows, newest first; each change's outcome in a
    /// modeller's words; which row may offer Undo; and the requests its
    /// buttons send. The last two lines are requests as JSON, for
    /// tests/test_activity_pane.py to parse with a reader that did not write it.
    ///
    /// UNDO, SINCE ROW 5b-1741 (the owner's answer of 2026-11-16): the button
    /// asks the add-in's pane-only operation for THE CHANGE IT SHOWS - its run,
    /// its model, its name - and names no chat, because it is the modeller's own
    /// click. Asked for by name (UndoFor), so on the HeronActivity before, which
    /// had only `UndoRequest()` sending `undo_last`, the host still builds and
    /// each check fails on its own assertion (heron-ship 2a).
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

        /// <summary>
        /// What Undo sends for `row` - by the one-row request where it exists,
        /// else, noted as a failure, the request the HeronActivity before had.
        /// </summary>
        private static string UndoFor(HeronActivity.Row row)
        {
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var one = typeof(HeronActivity).GetMethod("UndoRequest", flags, null,
                                                      new[] { typeof(HeronActivity.Row) }, null);
            if (one != null) return (string)one.Invoke(null, new object[] { row });
            Check(false, "HeronActivity has UndoRequest(Row) - the request names the change it is for");
            var none = typeof(HeronActivity).GetMethod("UndoRequest", flags, null, Type.EmptyTypes, null);
            return none == null ? "{}" : (string)none.Invoke(null, new object[0]);
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
                var undo = UndoFor(newest);
                Check(Json.ReadString(show, "claim") == "no" && Json.ReadString(undo, "claim") == "no",
                      "and neither ever takes the model from a chat (D-141)");
                Check(Json.ReadString(undo, "op") == "undo_from_pane",
                      "Undo asks the add-in's pane-only operation, undo_from_pane - never undo_last, "
                      + "which nothing answers (row 5b-1741), nor a chat's undo_own - found: "
                      + (Json.ReadString(undo, "op") ?? "none"));
                Check(Json.ReadString(undo, "run") == "wf-20261112-140500-0002-bbbb"
                      && Json.ReadString(undo, "document") == "Tower MEP"
                      && Json.ReadString(undo, "fragment") == "raise-ducts",
                      "for THE CHANGE IT SHOWS: the run it was kept in, its model and its name");
                Check(undo.IndexOf("conversation", StringComparison.Ordinal) < 0,
                      "and it names no chat - it is the modeller's own click, so no chat is asked");
                Check(HeronActivity.Said(Json.Ok(Json.Str("said", "Selected 3."))) == "Selected 3."
                      && HeronActivity.Said(Json.Error("undo_refused", "Not on top.")) == "Not on top."
                      && HeronActivity.Said(null) == "Revit did not answer.",
                      "and the status line says the add-in's own words");

                Console.WriteLine();
                Console.WriteLine("5. the status line in Revit words, never an operation's name (row 5b-1741)");
                var raw = HeronActivity.Said(Json.Error("unknown_op",
                    "Heron has no operation called 'undo_last'. It has: ping, info, undo_own."));
                Check(raw.IndexOf("undo_last", StringComparison.Ordinal) < 0
                      && raw.IndexOf("undo_own", StringComparison.Ordinal) < 0
                      && raw.IndexOf("operation", StringComparison.OrdinalIgnoreCase) < 0
                      && raw.IndexOf("Nothing was sent to Revit", StringComparison.Ordinal) >= 0,
                      "an add-in that does not know the request: said in Revit words, that nothing "
                      + "was sent - never its list of operations - found: " + raw);
                var unhandled = HeronActivity.Said(Json.Error("not_implemented",
                    "'undo_from_pane' is declared but Heron has no handler for it."));
                Check(unhandled.IndexOf("undo_from_pane", StringComparison.Ordinal) < 0
                      && unhandled.IndexOf("Nothing was sent to Revit", StringComparison.Ordinal) >= 0,
                      "nor one it has no handler for - found: " + unhandled);
                var bare = HeronActivity.Said("{\"ok\": false, \"error\": \"write_disabled\"}");
                Check(bare.IndexOf("write_disabled", StringComparison.Ordinal) < 0 && bare.Length > 20,
                      "a refusal that carries only its code is said in words, never the code - found: " + bare);

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
                Console.WriteLine("UNDO-JSON " + undo);
                return Failures.Count == 0 ? 0 : 1;
            }
            finally
            {
                try { Directory.Delete(folder, true); } catch (IOException) { }
            }
        }
    }
}
