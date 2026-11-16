// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Reflection;
using Heron.Revit.Addin;

namespace Heron.UndoGuard.TestHost
{
    /// <summary>
    /// The undo guard (D-157), checked without Revit.
    ///
    /// RevitFragment's permanent DocumentChanged listener tells HeronUndoGuard
    /// what Revit reported - a kept job and what Revit counted in it, any
    /// other commit, an undo, a redo, a sync, a reload, a save under a new
    /// name, a close, and anything Heron missed - and RevitUndo asks it
    /// whether Revit's Undo may be sent. What it decides is what this checks:
    /// that Undo is sent only while Heron saw its last change land and has
    /// seen nothing since, that everything unclear refuses, and that every
    /// refusal says nothing was undone. Whether Revit reports those events as
    /// the guard expects is the PC's to prove, and NEEDS-CHECKING Group NU
    /// says so.
    ///
    /// SECTIONS 7 TO 14 ARE THE REVIEW OF 2026-11-12, each seen to fail on
    /// the guard as it stood before the repair. Some of what they call did
    /// not exist then, so those members are asked for by name (Shim): on the
    /// old guard each scenario still RUNS and fails on its own assertion,
    /// where a direct call would have been one compile error for all of them
    /// (heron-ship 2a).
    ///
    /// SECTION 16 IS Q-88 (2026-11-16): only the chat that kept Heron's last
    /// change may have Undo sent for it. Every keep above is made by one chat
    /// (`Mine`); on a guard that records no chat they fall back to the keep it
    /// had, and section 16 fails by assertion rather than by a compile error.
    ///
    /// SECTION 17 IS THE ACTIVITY PANE'S UNDO (row 5b-1741, the owner's answer
    /// of 2026-11-16): the modeller's own click in Revit, so ANY chat's change
    /// - but only the change the pane shows, by the run it was kept in, and
    /// only by D-157's rule. The keep that records the run and the pane's
    /// question are asked for by name, as section 16's were: on the guard
    /// before, every scenario runs and fails on its own assertion.
    /// </summary>
    internal static class Program
    {
        private const string Model = "guid|C:/models/Tower.rvt|Tower";
        private const string Other = "guid2|C:/models/Podium.rvt|Podium";
        private const string Label = "Heron: rename-views-by-level";

        // Two chats' conversations, as the bridge client makes them (D-141).
        private const string Mine = "a1b2c3d4e5f60718293a4b5c6d7e8f90";
        private const string Theirs = "f0e1d2c3b4a5968778695a4b3c2d1e0f";

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

        // ------------------------------------------------------------- shim

        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static
                                           | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly List<string> Absent = new List<string>();

        private static MethodInfo Find(string name, int arguments)
        {
            foreach (var method in typeof(HeronUndoGuard).GetMethods(Members))
                if (method.Name == name && method.GetParameters().Length == arguments) return method;
            return null;
        }

        /// <summary>Call a member the guard may not have; null, and noted once, when it has not.</summary>
        private static object Ask(HeronUndoGuard guard, string name, params object[] arguments)
        {
            var method = Find(name, arguments.Length);
            if (method != null) return method.Invoke(guard, arguments);
            var missing = name + " with " + arguments.Length + " argument(s)";
            if (!Absent.Contains(missing)) Absent.Add(missing);
            return null;
        }

        /// <summary>A keep by this chat, with what Revit counted in it.</summary>
        private static void Keep(HeronUndoGuard guard, string model, string job, string label,
                                 DateTime at, int counted, bool read)
        {
            KeepBy(guard, model, job, label, at, counted, read, Mine);
        }

        /// <summary>
        /// A keep by one chat (Q-88) - or, on a guard from before, the keep it
        /// had: with what Revit counted, else without.
        /// </summary>
        private static void KeepBy(HeronUndoGuard guard, string model, string job, string label,
                                   DateTime at, int counted, bool read, string chat)
        {
            // Since row 5b-1741 the keep records the run it was kept in too;
            // a keep with none is still a keep, by every rule before it.
            if (Find("Kept", 8) != null)
            {
                Ask(guard, "Kept", model, job, label, at, counted, read, chat, null);
                return;
            }
            if (Find("Kept", 7) != null)
            {
                Ask(guard, "Kept", model, job, label, at, counted, read, chat);
                return;
            }
            Ask(guard, "Kept", model, job, label, at, counted, read, chat);     // noted as absent
            if (Find("Kept", 6) != null)
                Find("Kept", 6).Invoke(guard, new object[] { model, job, label, at, counted, read });
            else
                Find("Kept", 4).Invoke(guard, new object[] { model, job, label, at });
        }

        /// <summary>
        /// A keep by one chat in one run - the dispatcher's workflow id, the
        /// one the run record the Activity pane reads carries (row 5b-1741).
        /// On a guard that records no run, the keep it had, and noted absent.
        /// </summary>
        private static void KeepRun(HeronUndoGuard guard, string model, string job, string label,
                                    DateTime at, int counted, bool read, string chat, string run)
        {
            if (Find("Kept", 8) != null)
            {
                Ask(guard, "Kept", model, job, label, at, counted, read, chat, run);
                return;
            }
            Ask(guard, "Kept", model, job, label, at, counted, read, chat, run);     // noted as absent
            KeepBy(guard, model, job, label, at, counted, read, chat);
        }

        /// <summary>
        /// The Activity pane's question (row 5b-1741): may Undo be sent for the
        /// change the pane shows - kept in `run` - in "Tower"? Null on a guard
        /// that cannot say, which every check below reads as a failure.
        /// </summary>
        private static string PaneRefusal(HeronUndoGuard guard, string run, string pressed)
        {
            return Ask(guard, "PaneRefusal", Model, "Tower", run, pressed) as string;
        }

        private static bool CanAskPane()
        {
            return Find("PaneRefusal", 4) != null;
        }

        /// <summary>Whose change it is (Q-88), asked for "Tower" by `chat`; null on a guard that cannot say.</summary>
        private static string ChatRefusal(HeronUndoGuard guard, string chat)
        {
            return Ask(guard, "ChatRefusal", Model, "Tower", chat) as string;
        }

        private static void Send(HeronUndoGuard guard, string model, DateTime now)
        {
            if (Find("SentFor", 2) != null) Ask(guard, "SentFor", model, now);
            else Find("SentFor", 1).Invoke(guard, new object[] { model });
        }

        /// <summary>
        /// Whether a job is held back now. The hold is asked of the guard as a
        /// whole since the second review (any model); the guard before it was
        /// asked model by model, and `model` is what that one is asked for.
        /// </summary>
        private static string Pending(HeronUndoGuard guard, string model, DateTime now)
        {
            if (Find("Pending", 1) != null) return Ask(guard, "Pending", now) as string;
            Ask(guard, "Pending", now);                     // noted as absent
            var old = Find("Pending", 2);
            return old == null ? null : old.Invoke(guard, new object[] { model, now }) as string;
        }

        private static HeronUndoGuard KeptOne()
        {
            var guard = new HeronUndoGuard();
            Keep(guard, Model, "rename-views-by-level", Label, At, 3, true);
            return guard;
        }

        private static string Refusal(HeronUndoGuard guard)
        {
            return guard.Refusal(Model, "Tower", true, true);
        }

        private static bool Says(string refusal, string part)
        {
            return refusal != null && refusal.IndexOf(part, StringComparison.Ordinal) >= 0;
        }

        private static int Main()
        {
            Console.WriteLine("1. Undo is sent only while Heron's last kept change has had nothing after it");
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
                  "the modeller's own Move covers it: refused, naming what was done after it");
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
            Send(guard, Model, At.AddMinutes(1));
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
                  "Heron's change undone, then redone: Undo may be sent again");
            guard = KeptOne();
            guard.RedoneIn(Model, Names("Move"));
            Check(Says(guard.Refusal(Model, "Tower", true, true), "'Move' was done after it"),
                  "any other redo puts something after it");

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
            Keep(guard, Model, "create-sheets", "Heron: create-sheets", At.AddMinutes(5), 2, true);
            Check(guard.Refusal(Model, "Tower", true, true) == null
                  && Says(guard.SentSentence(Model), "'create-sheets'"),
                  "Heron kept another change after it: that one is undoable, and named");
            guard = new HeronUndoGuard();
            for (var i = 0; i < HeronUndoGuard.MaxDocuments + 4; i++)
                Keep(guard, "model-" + i, "job", "Heron: job", At, 1, true);
            Check(guard.Of("model-0") == null && guard.Of("model-" + (HeronUndoGuard.MaxDocuments + 3)) != null,
                  "at most " + HeronUndoGuard.MaxDocuments + " models remembered, the oldest forgotten first");
            Check(new HeronUndoGuard().Any == false && KeptOne().Any,
                  "and the listener's cheap question answers 'nothing kept' until something is");

            Console.WriteLine();
            Console.WriteLine("7. a keep Revit counted nothing in left no undo entry: nothing of Heron's to undo");
            guard = new HeronUndoGuard();
            guard.Committed(Model, Names("Move"), false);       // the modeller's, before any keep
            Keep(guard, Model, "open-view", "Heron: open-view", At, 0, true);
            var empty = Refusal(guard);
            Check(Says(empty, "'open-view' at 14:32, changed nothing in this model that Revit counted")
                  && Says(empty, "nothing of Heron's to undo") && Says(empty, "Nothing was undone"),
                  "a job with no transaction (open-view), kept after the modeller's Move: REFUSED - "
                  + "Undo would have taken the Move");
            guard = KeptOne();
            Keep(guard, Model, "open-view", "Heron: open-view", At.AddMinutes(8), 0, true);
            Check(Says(Refusal(guard), "'open-view' at 14:40") && !Says(Refusal(guard), "rename-views"),
                  "and it is Heron's LAST job from then on: the keep before it is no longer offered");
            guard.Committed(Model, Names("Move"), false);
            Check(Says(Refusal(guard), "changed nothing in this model that Revit counted"),
                  "a later edit leaves that said as it was - it is still true");
            guard = new HeronUndoGuard();
            Keep(guard, Model, "set-marks", "Heron: set-marks", At, 0, false);
            Check(Says(Refusal(guard), "Heron could not read what that job changed")
                  && Says(Refusal(guard), "Nothing was undone"),
                  "nothing counted because a change could not be READ: refused, and not called nothing");
            guard = new HeronUndoGuard();
            Keep(guard, Model, "set-marks", "Heron: set-marks", At, 4, false);
            Check(Refusal(guard) == null,
                  "something counted, though not every change was read: its entry landed");
            guard = KeptOne();
            Keep(guard, null, "create-sheets", "Heron: create-sheets", At, 2, true);
            Check(Refusal(guard) != null,
                  "a keep in a model Heron could not name: every model it remembers refuses");

            Console.WriteLine();
            Console.WriteLine("8. while an Undo is sent and not reported, nothing of Heron's may land on top");
            guard = KeptOne();
            var posted = At.AddMinutes(1);
            Send(guard, Model, posted);
            var waiting = Pending(guard, Model, posted.AddSeconds(1));
            Check(Says(waiting, "has not seen Revit run it yet") && Says(waiting, "nothing was run")
                  && Says(waiting, "in 'Tower'"),
                  "another job is held back, told nothing was run and in which model the Undo is out");
            Check(Says(Pending(guard, Other, posted.AddSeconds(1)), "whichever model is on screen"),
                  "a job in ANOTHER model is held too: it could bring that model to the front, and "
                  + "Revit's Undo acts on the model on screen");
            Keep(guard, Model, "create-sheets", "Heron: create-sheets", posted.AddSeconds(2), 2, true);
            Check(Says(Refusal(guard), "it may still run and take this change")
                  && Says(Refusal(guard), "Nothing was undone"),
                  "a job kept anyway while the Undo waited is NOT offered: that Undo may take it");
            guard.UndoneIn(Model, Names("Heron: create-sheets"));
            Check(Refusal(guard) != null,
                  "and stays refused when Revit reports the undo");
            guard = KeptOne();
            Send(guard, Model, posted);
            guard.Committed(Model, Names("Move"), false);
            Check(Says(Refusal(guard), "may have taken it instead") && Says(Refusal(guard), "Ctrl+Y"),
                  "a change reported between the Undo being sent and done: said, with how to put it back");
            guard = KeptOne();
            Send(guard, Model, posted);
            var late = posted.AddMinutes(5);
            Check(Pending(guard, Model, late) == null
                  && Says(Refusal(guard), "Undo was already sent")
                  && Says(Refusal(guard), "has not seen Revit run it"),
                  "an Undo Revit never reported does not hold other jobs for ever - and is not "
                  + "sent a second time");

            Console.WriteLine();
            Console.WriteLine("9. two keeps can carry one name: a redo counts back, it does not match by name");
            guard = new HeronUndoGuard();
            var first = new DateTime(2026, 11, 12, 14, 0, 0);
            Keep(guard, Model, "rename-views", "Heron: rename-views", first, 3, true);                  // X1
            guard.Committed(Model, Names("Move"), false);
            Keep(guard, Model, "rename-views", "Heron: rename-views", first.AddMinutes(30), 3, true);   // X2
            guard.UndoneIn(Model, Names("Heron: rename-views"));      // X2
            guard.UndoneIn(Model, Names("Move"));
            guard.UndoneIn(Model, Names("Heron: rename-views"));      // X1
            guard.RedoneIn(Model, Names("Heron: rename-views"));      // X1 back: the top is X1, not X2
            Check(Refusal(guard) != null,
                  "undo x3, redo x1: the top is the OLDER keep of that name, so Undo is refused");
            guard.RedoneIn(Model, Names("Move"));
            Check(Refusal(guard) != null, "redo x2: the Move is on top, still refused");
            guard.RedoneIn(Model, Names("Heron: rename-views"));      // X2 back, if the events are one each
            Check(Says(Refusal(guard), "carry the name 'Heron: rename-views'")
                  && Says(Refusal(guard), "Nothing was undone"),
                  "redo x3: with TWO keeps under one name no redo is trusted to say which came "
                  + "back - refused, and said why");
            guard = new HeronUndoGuard();
            Keep(guard, Model, "rename-views", "Heron: rename-views", first, 3, true);                  // X1
            Keep(guard, Model, "rename-views", "Heron: rename-views", first.AddMinutes(30), 3, true);   // X2
            guard.UndoneIn(Model, Names("Heron: rename-views"));      // X2
            guard.UndoneIn(Model, Names("Heron: rename-views"));      // X1
            guard.RedoneIn(Model, Names("Heron: rename-views"));      // X1 back - as TWO redo events
            guard.RedoneIn(Model, Names("Heron: rename-views"));
            Check(Refusal(guard) != null,
                  "one undo event for each, two redo events for one: the top is the OLDER keep, "
                  + "and Undo is refused whatever the count came to");
            guard = new HeronUndoGuard();
            Keep(guard, Model, "rename-views", "Heron: rename-views", first, 3, true);
            Keep(guard, Model, "create-sheets", "Heron: create-sheets", first.AddMinutes(30), 2, true);
            guard.UndoneIn(Model, Names("Heron: create-sheets"));
            guard.RedoneIn(Model, Names("Heron: create-sheets"));
            Check(Refusal(guard) == null,
                  "two keeps under DIFFERENT names are not caught by that: undone, redone, vouched for");
            guard = new HeronUndoGuard();
            Keep(guard, Model, "rename-views", "Heron: rename-views", first, 3, true);
            guard.SyncedIn(Model);
            Keep(guard, Model, "rename-views", "Heron: rename-views", first.AddMinutes(30), 3, true);
            guard.UndoneIn(Model, Names("Heron: rename-views"));
            guard.RedoneIn(Model, Names("Heron: rename-views"));
            Check(Refusal(guard) == null,
                  "nor a second keep of one name after a sync: Revit's list no longer holds the first");
            guard = KeptOne();
            guard.UndoneIn(Model, Names(Label));
            guard.Committed(Model, Names("Move"), false);
            guard.RedoneIn(Model, Names(Label));
            Check(Refusal(guard) != null,
                  "undone, then a new change: nothing is left to redo, so it never comes back");

            Console.WriteLine();
            Console.WriteLine("10. an undo Revit names as something else is not taken for Heron's");
            guard = KeptOne();
            guard.UndoneIn(Model, Names("Move"));
            var other = Refusal(guard);
            Check(Says(other, "Revit reported an undo of 'Move'")
                  && Says(other, "not the name Heron's change carries") && !Says(other, "already been undone"),
                  "undo of 'Move' while Heron believed its own was next: recorded, and said");
            guard.RedoneIn(Model, Names(Label));
            Check(Refusal(guard) != null, "and no redo makes Heron sure again");
            guard = KeptOne();
            Send(guard, Model, posted);
            guard.UndoneIn(Model, Names("Move"));
            Check(Says(Refusal(guard), "the Undo Heron sent was reported by Revit as an undo of 'Move'")
                  && Says(Refusal(guard), "Ctrl+Y"),
                  "the Undo HERON sent, reported under another name: said, with how to put it back");
            guard = KeptOne();
            guard.UndoneIn(Model, Names());
            Check(Says(Refusal(guard), "has already been undone"),
                  "an undo Revit gives no name for is taken as Heron's - refused either way");
            guard = KeptOne();
            guard.UndoneIn(Model, Names(Label + " - select the views"));
            Check(Says(Refusal(guard), "has already been undone"),
                  "one of the job's own setup steps is Heron's");
            var read = new List<string>();
            guard = KeptOne();
            read.Add(guard.SentSentence(Model));
            guard.Committed(Model, Names("Move"), false);
            read.Add(Refusal(guard));
            guard = KeptOne();
            guard.SyncedIn(Model);
            read.Add(Refusal(guard));
            guard = KeptOne();
            guard.UndoneIn(Model, Names(Label));
            read.Add(Refusal(guard));
            var claims = read.FindAll(said => Says(said, "the top of"));
            Check(claims.Count == 0 && Says(read[0], "Heron has seen nothing change this model since"),
                  "no sentence says what is at the top of Revit's undo list - Heron cannot read it; "
                  + "it says what Heron has seen" + (claims.Count == 0 ? "" : " - found: " + claims[0]));

            Console.WriteLine();
            Console.WriteLine("11. what Heron missed, it does not guess: the guard fails closed");
            guard = KeptOne();
            Ask(guard, "Doubt", Model, "Heron could not read a change Revit reported here");
            var doubt = Refusal(guard);
            Check(Says(doubt, "Heron cannot tell what Revit's Undo would take in this model now")
                  && Says(doubt, "could not read a change Revit reported here")
                  && Says(doubt, "Nothing was undone"),
                  "a change it could not read: refused, saying what it missed");
            guard = KeptOne();
            Keep(guard, Other, "create-sheets", "Heron: create-sheets", At, 2, true);
            Ask(guard, "Doubt", null, "a Heron job ended in a way Heron did not expect");
            Check(Refusal(guard) != null && guard.Refusal(Other, "Podium", true, true) != null,
                  "missed in a model it cannot name: EVERY model it remembers refuses");
            guard = KeptOne();
            guard.UndoneIn(Model, Names(Label));
            Ask(guard, "Doubt", Model, "a rollback of Heron's job 'x' did not hold");
            guard.RedoneIn(Model, Names(Label));
            Check(Says(Refusal(guard), "did not hold"),
                  "an undone change that was doubted does not come back with a redo");
            guard = KeptOne();
            guard.Committed(Model, Names("Move"), false);
            Ask(guard, "Doubt", Model, "Heron's job 'x' left a transaction of its own open");
            Check(Says(Refusal(guard), "'Move' was done after it"),
                  "one already refusing for good keeps its own reason");
            Keep(guard, Model, "create-sheets", "Heron: create-sheets", At.AddMinutes(9), 2, true);
            Check(Refusal(guard) == null,
                  "and a NEW keep Revit counted is vouched for again: it landed after whatever was missed");

            Console.WriteLine();
            Console.WriteLine("12. Reload Latest clears the list as a sync does; a save under a new name moves the key");
            guard = KeptOne();
            Ask(guard, "ReloadedIn", Model);
            Check(Says(Refusal(guard), "Reload Latest") && Says(Refusal(guard), "clears its undo list")
                  && Says(Refusal(guard), "Nothing was undone"),
                  "reloaded from central: refused");
            const string Saved = "guid|C:/models/Tower-B.rvt|Tower-B";
            guard = KeptOne();
            Ask(guard, "Renamed", Model, Saved);
            var renamed = guard.Refusal(Saved, "Tower-B", true, true);
            Check(guard.Of(Model) == null && Says(renamed, "saved under a new name since")
                  && Says(renamed, "'rename-views-by-level' at 14:32"),
                  "Save As: what was remembered moves to the new name, and refuses - never "
                  + "'Heron has kept no change' of a model it did change");

            Console.WriteLine();
            Console.WriteLine("13. a check says what Undo would do, and changes nothing");
            guard = KeptOne();
            var yes = Ask(guard, "CheckSentence", Model, null) as string;
            Check(Says(yes, "Yes - Undo would be sent now, for 'rename-views-by-level' (" + Label)
                  && Says(yes, "nothing was sent to Revit"),
                  "yes: for which job, and that nothing was sent");
            Check(Refusal(guard) == null && guard.Of(Model).State == HeronUndoGuard.OnTop,
                  "and the guard is as it was: a check is not a send");
            guard.Committed(Model, Names("Move"), false);
            var no = Ask(guard, "CheckSentence", Model, Refusal(guard)) as string;
            Check(Says(no, "No - Undo would not be sent now.") && Says(no, "'Move' was done after it")
                  && Says(no, "nothing was sent to Revit"),
                  "no: the exact refusal, and that nothing was sent");

            Console.WriteLine();
            Console.WriteLine("14. every transition is written down: the event, the names, the state it left");
            var lines = new List<string>();
            HeronUndoGuard logged = null;
            try
            {
                logged = (HeronUndoGuard)Activator.CreateInstance(
                    typeof(HeronUndoGuard), Members, null,
                    new object[] { (Action<string>)lines.Add }, null);
            }
            catch (MissingMethodException) { logged = null; }
            Check(logged != null, "the guard takes somewhere to write to");
            if (logged == null) logged = new HeronUndoGuard();
            Keep(logged, Model, "rename-views-by-level", Label, At, 3, true);
            logged.Committed(Model, Names(Label), true);
            logged.Committed(Model, Names("Move"), false);
            logged.UndoneIn(Model, Names("Move"));
            Keep(logged, Model, "create-sheets", "Heron: create-sheets", At, 3, true);
            logged.UndoneIn(Model, Names("Heron: create-sheets"));
            logged.RedoneIn(Model, Names("Heron: create-sheets"));
            var log = string.Join("\n", lines.ToArray());
            Check(Says(log, "Undo guard: kept, 3 element(s) counted in 'Tower': nothing kept -> onTop"),
                  "a keep, with what Revit counted");
            Check(Says(log, "commit inside Heron's own job ['" + Label + "'] in 'Tower': onTop -> onTop")
                  && Says(log, "commit ['Move'] in 'Tower': onTop -> covered"),
                  "a commit, Heron's own and anybody else's, each with its transaction name");
            Check(Says(log, "undo ['Move'] in 'Tower': covered -> covered")
                  && Says(log, "undo ['Heron: create-sheets'] in 'Tower': onTop -> undone (depth 0)")
                  && Says(log, "redo ['Heron: create-sheets'] in 'Tower': undone -> onTop"),
                  "an undo and a redo with the names Revit gave - what Group NU needs from one real run");
            var deaf = new HeronUndoGuard();
            try
            {
                deaf = (HeronUndoGuard)Activator.CreateInstance(
                    typeof(HeronUndoGuard), Members, null,
                    new object[] { (Action<string>)(line => { throw new InvalidOperationException("no log"); }) },
                    null);
            }
            catch (MissingMethodException) { }
            Keep(deaf, Model, "rename-views-by-level", Label, At, 3, true);
            Check(deaf.Refusal(Model, "Tower", true, true) == null,
                  "and a log that cannot be written never costs the guard its state");

            Console.WriteLine();
            Console.WriteLine("15. an Undo that was sent and not seen to run is never forgotten");
            guard = KeptOne();
            Send(guard, Model, posted);
            Pending(guard, Model, posted.AddSeconds(31));       // the hold asks, as before every job
            Keep(guard, Model, "create-sheets", "Heron: create-sheets", posted.AddSeconds(32), 2, true);
            var still = Refusal(guard);
            Check(Says(still, "an Undo Heron sent earlier has not been seen to run; it may still "
                              + "run and take this change")
                  && Says(still, "'create-sheets'") && Says(still, "Nothing was undone"),
                  "sent, 31 s with no undo reported, then a keep: NOT vouched for - the first Undo "
                  + "may still run and take it");
            Check(guard.Of(Model) != null && guard.Of(Model).State == "unsure"
                  && guard.Of(Model).Why == "an Undo Heron sent earlier has not been seen to run; "
                                            + "it may still run and take this change",
                  "and the reason is kept for the kept reply to say");
            guard = KeptOne();
            Send(guard, Model, posted);
            guard.Committed(Model, Names("Move"), false);       // the modeller, while it was out
            Pending(guard, Model, posted.AddSeconds(31));
            Keep(guard, Model, "create-sheets", "Heron: create-sheets", posted.AddSeconds(32), 2, true);
            Check(Says(Refusal(guard), "it may still run and take this change"),
                  "the same after the modeller's own change in between");
            guard = KeptOne();
            Send(guard, Model, posted);
            guard.Doubt(Model, "Heron could not read a change Revit reported here");
            Keep(guard, Model, "create-sheets", "Heron: create-sheets", posted.AddSeconds(32), 2, true);
            Check(Says(Refusal(guard), "it may still run and take this change"),
                  "and after anything Heron missed in between: only an undo Revit reports clears it");
            guard = KeptOne();
            Send(guard, Model, posted);
            var seen = guard.Of(Model).State;
            Pending(guard, Model, posted.AddSeconds(31));
            Pending(guard, Model, posted.AddSeconds(1));
            Check(seen == "sent" && guard.Of(Model).State == "sent",
                  "asking whether a job is held changes nothing - a check may ask it too");
            guard = KeptOne();
            Send(guard, Model, posted);
            guard.UndoneIn(Model, Names(Label));
            Keep(guard, Model, "create-sheets", "Heron: create-sheets", posted.AddSeconds(32), 2, true);
            Check(Refusal(guard) == null && Pending(guard, Model, posted.AddSeconds(2)) == null,
                  "Revit reported an undo there: the Undo is accounted for, nothing is held, and "
                  + "the next keep is vouched for");
            guard = KeptOne();
            Send(guard, Model, posted);
            guard.SyncedIn(Model);
            Keep(guard, Model, "create-sheets", "Heron: create-sheets", posted.AddSeconds(32), 2, true);
            Check(Refusal(guard) == null, "a sync clears it too: nothing is left for that Undo to take");
            guard = KeptOne();
            Send(guard, Model, posted);
            guard.Closed(Model);
            Keep(guard, Model, "create-sheets", "Heron: create-sheets", posted.AddSeconds(32), 2, true);
            Check(Refusal(guard) == null, "and a close");
            guard = KeptOne();
            Send(guard, Model, posted);
            Ask(guard, "Renamed", Model, "guid|C:/models/Tower-B.rvt|Tower-B");
            Keep(guard, "guid|C:/models/Tower-B.rvt|Tower-B", "create-sheets", "Heron: create-sheets",
                 posted.AddSeconds(32), 2, true);
            Check(Says(guard.Refusal("guid|C:/models/Tower-B.rvt|Tower-B", "Tower-B", true, true),
                       "it may still run and take this change"),
                  "a save under a new name does not: the Undo still out goes with the model");

            Console.WriteLine();
            Console.WriteLine("16. only the chat that kept Heron's last change may have Undo sent for it (Q-88)");
            guard = new HeronUndoGuard();
            KeepBy(guard, Model, "rename-views-by-level", Label, At, 3, true, Mine);
            Check(Find("ChatRefusal", 3) != null && Refusal(guard) == null
                  && ChatRefusal(guard, Mine) == null,
                  "this chat kept it, and nothing since: Undo may be sent - for this chat");
            var theirs = ChatRefusal(guard, Theirs);
            Check(Says(theirs, "Heron's last change in \"Tower\", 'rename-views-by-level' at 14:32, "
                               + "was made in another chat, not this one")
                  && Says(theirs, "Nothing was sent to Revit and nothing was undone")
                  && Says(theirs, "Ctrl+Z in Revit still undoes it"),
                  "ANOTHER chat asking is refused: the change named, nothing sent, and Ctrl+Z "
                  + "said to undo it still - found: " + (theirs ?? "nothing"));
            Check(guard.Of(Model) != null && guard.Of(Model).State == HeronUndoGuard.OnTop
                  && Refusal(guard) == null && ChatRefusal(guard, Mine) == null,
                  "and asking changed nothing: the chat that kept it still may");
            Check(ChatRefusal(guard, null) != null && ChatRefusal(guard, "") != null,
                  "a request that names no chat is refused - it cannot be shown to be the one "
                  + "that kept it");
            Check(ChatRefusal(guard, Mine.ToUpperInvariant()) != null
                  && ChatRefusal(guard, Mine.Substring(0, 8)) != null,
                  "the chat is compared exactly - never by case, never by its first characters");
            guard = new HeronUndoGuard();
            KeepBy(guard, Model, "rename-views-by-level", Label, At, 3, true, null);
            Check(ChatRefusal(guard, Mine) != null && ChatRefusal(guard, null) != null,
                  "a keep that recorded no chat is nobody's: every chat is refused, even one that "
                  + "names none");
            guard = new HeronUndoGuard();
            KeepBy(guard, Model, "rename-views-by-level", Label, At, 3, true, Mine);
            KeepBy(guard, Model, "create-sheets", "Heron: create-sheets", At.AddMinutes(4), 2, true, Theirs);
            Check(Says(ChatRefusal(guard, Mine), "'create-sheets' at 14:36, was made in another chat")
                  && ChatRefusal(guard, Theirs) == null && Refusal(guard) == null,
                  "another chat kept a change after this one's: that is Heron's last change, so "
                  + "this chat is refused and the other is not");
            guard = new HeronUndoGuard();
            KeepBy(guard, Model, "rename-views-by-level", Label, At, 3, true, Mine);
            KeepBy(guard, Other, "create-sheets", "Heron: create-sheets", At, 2, true, Theirs);
            Check(ChatRefusal(guard, Mine) == null
                  && Ask(guard, "ChatRefusal", Other, "Podium", Mine) != null,
                  "another chat's keep in ANOTHER model changes nothing here - each model's last "
                  + "change is the chat's that kept it");
            Check(Says(Ask(new HeronUndoGuard(), "ChatRefusal", Model, "Tower", Mine) as string,
                       "Nothing was sent to Revit"),
                  "and with nothing kept at all it refuses too - it never lets an Undo through "
                  + "on its own");
            var chats = new List<string>();
            HeronUndoGuard told = null;
            try
            {
                told = (HeronUndoGuard)Activator.CreateInstance(
                    typeof(HeronUndoGuard), Members, null,
                    new object[] { (Action<string>)chats.Add }, null);
            }
            catch (MissingMethodException) { told = null; }
            if (told != null) KeepBy(told, Model, "rename-views-by-level", Label, At, 3, true, Mine);
            Check(Says(string.Join("\n", chats.ToArray()), "job 'rename-views-by-level', chat a1b2c3d4"),
                  "the add-in's log names the chat that kept it, as it names a chat elsewhere - for "
                  + "a run with two chats to read");

            Console.WriteLine();
            Console.WriteLine("17. the Activity pane's Undo: the modeller's own click, any chat's change, "
                              + "only the change it shows (row 5b-1741)");
            const string Run = "wf-20261116-143200-0007-abcd";
            const string Older = "wf-20261116-140000-0003-0f0f";
            guard = new HeronUndoGuard();
            KeepRun(guard, Model, "rename-views-by-level", Label, At, 3, true, Theirs, Run);
            Check(CanAskPane() && Refusal(guard) == null && PaneRefusal(guard, Run, "rename-views-by-level") == null,
                  "another chat kept Heron's last change here, and nothing since: the pane may send "
                  + "Undo for it - no chat is asked, it is the modeller's own button");
            Check(ChatRefusal(guard, Mine) != null && ChatRefusal(guard, Theirs) == null,
                  "and what a CHAT may undo is unchanged (Q-88): only the chat that kept it");
            Check(CanAskPane() && guard.Of(Model) != null && guard.Of(Model).State == HeronUndoGuard.OnTop
                  && PaneRefusal(guard, Run, "rename-views-by-level") == null,
                  "asking changed nothing: Heron's change is still on top, and the pane still may");
            var stale = PaneRefusal(guard, Older, "create-sheets");
            Check(Says(stale, "'create-sheets'") && Says(stale, "Heron's last change in \"Tower\" is "
                               + "'rename-views-by-level' at 14:32")
                  && Says(stale, "Nothing was sent to Revit and nothing was undone"),
                  "the pane showing ANOTHER change than Heron's last one here is refused, both named, "
                  + "nothing sent - Revit's Undo would take the last one, not the one pressed - found: "
                  + (stale ?? "nothing"));
            Check(CanAskPane() && PaneRefusal(guard, null, "x") != null && PaneRefusal(guard, "", "x") != null
                  && PaneRefusal(guard, Run.ToUpperInvariant(), "x") != null
                  && PaneRefusal(guard, Run.Substring(0, 12), "x") != null,
                  "the run is compared exactly - none, another case or its first characters are refused");
            guard = new HeronUndoGuard();
            KeepRun(guard, Model, "rename-views-by-level", Label, At, 3, true, Mine, null);
            Check(CanAskPane() && PaneRefusal(guard, Run, "rename-views-by-level") != null
                  && PaneRefusal(guard, null, "rename-views-by-level") != null,
                  "a keep that recorded no run is not the pane's to undo, whatever the pane names");
            Check(CanAskPane() && Says(PaneRefusal(new HeronUndoGuard(), Run, "rename-views-by-level"),
                                       "Nothing was sent to Revit"),
                  "and with nothing kept at all it refuses too - it never lets an Undo through on its own");
            guard = new HeronUndoGuard();
            KeepRun(guard, Model, "rename-views-by-level", Label, At, 3, true, Theirs, Run);
            guard.Committed(Model, Names("Move"), false);
            var mine = Refusal(guard);
            Check(CanAskPane() && PaneRefusal(guard, Run, "rename-views-by-level") == null
                  && Says(mine, "'Move' was done after it") && Says(mine, "Nothing was undone"),
                  "the modeller's own edit on top: D-157's rule refuses the pane exactly as it refuses "
                  + "a chat - the pane's change is still the one it shows, and nothing is sent");
            guard = new HeronUndoGuard();
            KeepRun(guard, Model, "rename-views-by-level", Label, At, 3, true, Theirs, Run);
            Send(guard, Model, At.AddMinutes(1));
            Check(CanAskPane() && Says(Refusal(guard), "Undo was already sent"),
                  "pressed twice before Revit reports the first: the second is refused");
            guard.UndoneIn(Model, Names(Label));
            Check(Says(Refusal(guard), "has already been undone"),
                  "Revit reported it undone: pressed again, refused - the next undo is Ctrl+Z's");
            guard.RedoneIn(Model, Names(Label));
            Check(CanAskPane() && Refusal(guard) == null && PaneRefusal(guard, Run, "rename-views-by-level") == null,
                  "Ctrl+Y (Redo) put it back on top: the pane may undo it again");
            guard = new HeronUndoGuard();
            KeepRun(guard, Model, "rename-views-by-level", Label, At, 3, true, Theirs, Run);
            KeepRun(guard, Model, "create-sheets", "Heron: create-sheets", At.AddMinutes(4), 2, true, Mine,
                    "wf-20261116-143600-0008-beef");
            Check(Says(PaneRefusal(guard, Run, "rename-views-by-level"), "'create-sheets' at 14:36"),
                  "a later keep in this model is Heron's last change now: the row the pane showed "
                  + "before it is refused, naming the later one");

            Console.WriteLine();
            foreach (var missing in Absent)
                Check(false, "the guard has " + missing);
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
