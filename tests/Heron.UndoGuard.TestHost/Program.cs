// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Heron.Revit.Addin;

namespace Heron.UndoGuard.TestHost
{
    /// <summary>
    /// The undo guard (D-157), checked without Revit.
    ///
    /// RevitFragment's permanent DocumentChanged listener tells HeronUndoGuard
    /// what Revit reported - a kept job, any other commit, an undo, a redo, a
    /// sync, a close - and RevitUndo asks it whether Revit's Undo may be sent.
    /// What it decides is what this checks: that Undo is sent only while
    /// Heron's last kept change is on top, and that every refusal says nothing
    /// was undone. Whether Revit reports those events as the guard expects is
    /// the PC's to prove, and NEEDS-CHECKING says so.
    /// </summary>
    internal static class Program
    {
        private const string Model = "guid|C:/models/Tower.rvt|Tower";
        private const string Other = "guid2|C:/models/Podium.rvt|Podium";
        private const string Label = "Heron: rename-views-by-level";

        private static readonly DateTime At = new DateTime(2026, 11, 12, 14, 32, 0);

        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private static void Check(bool condition, string what)
        {
            _checks++;
            Console.WriteLine((condition ? "  ok    " : "  FAIL  ") + what);
            if (!condition) Failures.Add(what);
        }

        private static string[] Names(params string[] names)
        {
            return names;
        }

        private static HeronUndoGuard KeptOne()
        {
            var guard = new HeronUndoGuard();
            guard.Kept(Model, "rename-views-by-level", Label, At);
            return guard;
        }

        private static bool Says(string refusal, string part)
        {
            return refusal != null && refusal.IndexOf(part, StringComparison.Ordinal) >= 0;
        }

        private static int Main()
        {
            Console.WriteLine("1. Undo is sent only while Heron's last kept change is on top");
            var guard = new HeronUndoGuard();
            var none = guard.Refusal(Model, "Tower", true, true);
            Check(Says(none, "Heron has kept no change in \"Tower\"") && Says(none, "Nothing was undone"),
                  "nothing kept: refused, and Ctrl+Z is named as the way to undo anybody's change");
            guard = KeptOne();
            Check(guard.Refusal(Model, "Tower", true, true) == null,
                  "kept, and nothing since: Undo may be sent");
            Check(Says(guard.SentSentence(Model), "'rename-views-by-level' (" + Label + ", kept at 14:32)")
                  && Says(guard.SentSentence(Model), "Ctrl+Y"),
                  "and the sentence names the change, its undo entry and time, and Redo");

            Console.WriteLine();
            Console.WriteLine("2. Heron's own job does not cover it; anything else does, for good");
            guard = KeptOne();
            guard.Committed(Model, Names("Heron: create-sheets"), true);
            Check(guard.Refusal(Model, "Tower", true, true) == null,
                  "a commit inside Heron's next job - a dry run, rolled back - covers nothing");
            guard.Committed(Model, Names("Move"), false);
            var covered = guard.Refusal(Model, "Tower", true, true);
            Check(Says(covered, "'Move' was done after it") && Says(covered, "Nothing was undone"),
                  "the modeller's own Move covers it: refused, naming what is on top");
            guard.UndoneIn(Model, Names("Move"));
            Check(guard.Refusal(Model, "Tower", true, true) != null,
                  "and stays covered after that Move is undone - what is on top now cannot be "
                  + "counted back, so it is never guessed");
            guard = KeptOne();
            guard.Committed(Other, Names("Move"), false);
            Check(guard.Refusal(Model, "Tower", true, true) == null,
                  "a change in ANOTHER model covers nothing here");

            Console.WriteLine();
            Console.WriteLine("3. once undone, it is not undone again");
            guard = KeptOne();
            guard.SentFor(Model);
            var sent = guard.Refusal(Model, "Tower", true, true);
            Check(Says(sent, "Undo was already sent") && Says(sent, "Nothing more was sent"),
                  "sent and not yet reported: a second request is refused");
            guard.UndoneIn(Model, Names(Label));
            Check(Says(guard.Refusal(Model, "Tower", true, true), "has already been undone"),
                  "Revit reported it undone: refused, and the next undo is Ctrl+Z's");
            guard = KeptOne();
            guard.UndoneIn(Model, Names(Label));
            Check(Says(guard.Refusal(Model, "Tower", true, true), "has already been undone"),
                  "the modeller's own Ctrl+Z took it too - whoever pressed it, it went");

            Console.WriteLine();
            Console.WriteLine("4. a redo puts back only Heron's own entry");
            guard = KeptOne();
            guard.UndoneIn(Model, Names(Label));
            guard.RedoneIn(Model, Names(Label));
            Check(guard.Refusal(Model, "Tower", true, true) == null,
                  "Heron's change undone, then redone: it is on top again");
            guard = KeptOne();
            guard.RedoneIn(Model, Names("Move"));
            Check(Says(guard.Refusal(Model, "Tower", true, true), "'Move' was done after it"),
                  "any other redo puts something on top of it");

            Console.WriteLine();
            Console.WriteLine("5. a sync, a close, the model in front and Revit being busy");
            guard = KeptOne();
            guard.SyncedIn(Model);
            Check(Says(guard.Refusal(Model, "Tower", true, true), "synchronized with central"),
                  "synchronized with central: Revit cleared the list, refused");
            guard.Committed(Model, Names("Move"), false);
            Check(Says(guard.Refusal(Model, "Tower", true, true), "synchronized with central"),
                  "and stays said so, rather than becoming 'covered'");
            guard = KeptOne();
            guard.Closed(Model);
            Check(Says(guard.Refusal(Model, "Tower", true, true), "Heron has kept no change"),
                  "closed: forgotten - a model opened again has an undo list of its own");
            guard = KeptOne();
            Check(Says(guard.Refusal(Model, "Tower", false, true), "is not it")
                  && Says(guard.Refusal(Model, "Tower", false, true), "Nothing was undone"),
                  "not the model on screen: refused, since Revit's Undo acts on the model in front");
            Check(Says(guard.Refusal(Model, "Tower", true, false), "middle of a command"),
                  "Revit busy with a command of its own: refused, and asked to finish it");

            Console.WriteLine();
            Console.WriteLine("6. a newer keep replaces the older, and the list stays bounded");
            guard = KeptOne();
            guard.Committed(Model, Names("Move"), false);
            guard.Kept(Model, "create-sheets", "Heron: create-sheets", At.AddMinutes(5));
            Check(guard.Refusal(Model, "Tower", true, true) == null
                  && Says(guard.SentSentence(Model), "'create-sheets'"),
                  "Heron kept another change on top: that one is undoable, and named");
            guard = new HeronUndoGuard();
            for (var i = 0; i < HeronUndoGuard.MaxDocuments + 4; i++)
                guard.Kept("model-" + i, "job", "Heron: job", At);
            Check(guard.Of("model-0") == null && guard.Of("model-" + (HeronUndoGuard.MaxDocuments + 3)) != null,
                  "at most " + HeronUndoGuard.MaxDocuments + " models remembered, the oldest forgotten first");
            Check(new HeronUndoGuard().Any == false && KeptOne().Any,
                  "and the listener's cheap question answers 'nothing kept' until something is");

            Console.WriteLine();
            if (Failures.Count > 0)
            {
                Console.WriteLine("FAILED  " + Failures.Count + " of " + _checks + " check(s)");
                foreach (var f in Failures) Console.WriteLine("  - " + f);
                return 1;
            }
            Console.WriteLine("PASS    " + _checks + " check(s)");
            return 0;
        }
    }
}
