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

namespace Heron.RollbackWitness.TestHost
{
    /// <summary>
    /// The rollback check's bookkeeping, checked without Revit.
    ///
    /// A MODEL HERE IS A SET OF UniqueIds. Adding an element puts its id in
    /// the set and tells the watch, the way RevitFragment's DocumentChanged
    /// listener does; a rollback, a delete or an undo takes ids out. Whether
    /// Revit really removes what a rollback should, keeps what it should, and
    /// reports both through DocumentChanged is the PC's to prove -
    /// NEEDS-CHECKING says what it owes. What is checked here is what Heron
    /// concludes from what it is told, and the words the modeller reads.
    /// </summary>
    internal static class Program
    {
        private const string Doc = "c95db604-0abd-4bc5-9ebd-3ece37b3ce52|C:\\Jobs\\Tower.rvt|Tower";
        private const string Other = "|C:\\Jobs\\Podium.rvt|Podium";

        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private static void Check(bool condition, string what)
        {
            _checks++;
            Console.WriteLine((condition ? "  ok    " : "  FAIL  ") + what);
            if (!condition) Failures.Add(what);
        }

        /// <summary>A model: which UniqueIds resolve, and which cannot be asked about.</summary>
        private sealed class Model
        {
            internal readonly HashSet<string> Present = new HashSet<string>(StringComparer.Ordinal);
            internal readonly HashSet<string> Unaskable = new HashSet<string>(StringComparer.Ordinal);

            internal Func<string, bool?> Resolves
            {
                get { return id => Unaskable.Contains(id) ? (bool?)null : Present.Contains(id); }
            }

            /// <summary>The job adds elements, and the listener reports them.</summary>
            internal void Add(HeronRollbackWitness.Watch watch, int from, int count)
            {
                for (var n = from; n < from + count; n++)
                {
                    Present.Add(Uid(n));
                    if (watch.Counts(Id(n))) watch.Sampled(Uid(n));
                }
            }

            internal void Remove(int from, int count)
            {
                for (var n = from; n < from + count; n++) Present.Remove(Uid(n));
            }
        }

        private static string Uid(int n) { return "uid-" + n; }
        private static string Id(int n) { return n.ToString(); }

        private static HeronRollbackWitness.Watch Job(HeronRollbackWitness witness, Model model,
                                                      string name)
        {
            return witness.Begin(Doc, name, "Heron: " + name, model.Resolves);
        }

        /// <summary>A job that adds `count` elements and is KEPT.</summary>
        private static string KeptJob(HeronRollbackWitness witness, Model model, string name,
                                      int from, int count)
        {
            using (var watch = Job(witness, model, name))
            {
                model.Add(watch, from, count);
                return witness.Assimilated(watch, model.Resolves);
            }
        }

        private static int Main()
        {
            Console.WriteLine("A rollback never takes earlier kept work with it silently");
            Console.WriteLine();

            // 1. NOTHING KEPT, A CLEAN ROLLBACK: the words a reply carried
            // before this check are unchanged, byte for byte.
            var witness = new HeronRollbackWitness();
            var model = new Model();
            HeronRollbackWitness.Outcome outcome;
            using (var watch = Job(witness, model, "duct-offset"))
            {
                outcome = witness.RolledBack(watch, true, model.Resolves);
            }
            Check(outcome.Held && outcome.Lost == 0 && outcome.Also() == "" && outcome.Unsaid() == "",
                  "a clean rollback with nothing kept adds no words at all");
            Check(outcome.ModelSentence() == "Revit confirmed the rollback, so the model is exactly as it was.",
                  "and its model sentence is the one RefusedBy always said: \"" + outcome.ModelSentence() + "\"");
            outcome = witness.RolledBack(null, false, model.Resolves);
            Check(outcome.ModelSentence() == "Revit did NOT confirm the rollback, so THE MODEL MAY STILL HOLD "
                      + "PART OF THIS JOB. Check what it would have changed before trusting anything here, "
                      + "and do not save until you have.",
                  "an unconfirmed rollback says what it always said: \"" + outcome.ModelSentence() + "\"");
            Check(!outcome.Held && outcome.Unsaid() == outcome.ModelSentence(),
                  "and a refusal that made no claim about the model now carries it");

            // 2. KEEP A, PREVIEW B, B ROLLS BACK CLEANLY: silent, and A is still
            // watched. NEEDS-CHECKING's first rollback-check row.
            witness = new HeronRollbackWitness();
            model = new Model();
            var note = KeptJob(witness, model, "create-walls", 1, 3);
            Check(note == "" && witness.KeptCount(Doc) == 3, "a kept job's three elements are watched (" + witness.KeptCount(Doc) + ")");
            using (var b = Job(witness, model, "create-floors"))
            {
                Check(ReferenceEquals(witness.Armed, b), "the job being run is the one the listener feeds");
                model.Add(b, 10, 2);
                model.Remove(10, 2);                                // the rollback
                outcome = witness.RolledBack(b, true, model.Resolves);
            }
            Check(witness.Armed == null, "and the listener stops feeding it when the job ends");
            Check(outcome.Held && outcome.Lost == 0 && outcome.Also() == "",
                  "B's clean rollback says nothing about A");
            Check(outcome.KeptChecked == 3 && outcome.Checked == 2 && outcome.Standing == 0,
                  "having looked for A's 3 and B's 2 (" + outcome.KeptChecked + ", " + outcome.Checked + ")");
            Check(outcome.ModelSentence() == "Revit confirmed the rollback, so the model is exactly as it was"
                      + " - and none of the 2 elements this run added is still in the model.",
                  "and says only what it checked: \"" + outcome.ModelSentence() + "\"");
            Check(witness.KeptCount(Doc) == 3, "A is still watched for the next one");

            // 3. B'S ROLLBACK TAKES TWO OF A'S: warned once, in the words asked
            // for, and then forgotten.
            using (var b = Job(witness, model, "create-floors"))
            {
                model.Add(b, 10, 2);
                model.Remove(10, 2);
                model.Remove(1, 2);                                 // taken with it
                outcome = witness.RolledBack(b, true, model.Resolves);
                Check(ReferenceEquals(witness.RolledBack(b, true, model.Resolves), outcome),
                      "asked twice for one job, it gives the first answer - said once");
            }
            Check(outcome.Lost == 2 && outcome.KeptChecked == 3, "two of A's three are found gone (" + outcome.Lost + ")");
            Check(outcome.LossSentence() == "Rolling back this run removed 2 of 3 elements the last kept "
                      + "job made ('create-walls') - rebuild that stage.",
                  "and the reply says so: \"" + outcome.LossSentence() + "\"");
            Check(outcome.Held, "while B's own rollback still held - the two are separate findings");
            Check(outcome.Unsaid() == outcome.LossSentence() && outcome.Also() == outcome.LossSentence(),
                  "the loss rides on a refusal too, whose own words say nothing about it");
            Check(witness.KeptCount(Doc) == 0, "A is forgotten once the loss is said");
            using (var c = Job(witness, model, "create-floors"))
            {
                outcome = witness.RolledBack(c, true, model.Resolves);
            }
            Check(outcome.Also() == "", "so the next rollback does not report the same loss again");

            // 4. THE MODELLER DELETES ONE OF A'S BY HAND BETWEEN JOBS: narrowed in
            // silence, never blamed on B.
            witness = new HeronRollbackWitness();
            model = new Model();
            KeptJob(witness, model, "create-walls", 1, 3);
            model.Remove(2, 1);
            using (var b = Job(witness, model, "create-floors"))
            {
                Check(b.Before == "" && witness.KeptCount(Doc) == 2,
                      "a hand deletion before the job narrows A to 2, and says nothing");
                outcome = witness.RolledBack(b, true, model.Resolves);
            }
            Check(outcome.Lost == 0 && outcome.Also() == "", "and B's rollback reports no loss for it");

            // 5. ALL OF A GONE BEFORE B STARTS, NO UNDO SEEN: said once, at the
            // start, as what it is - not B's doing.
            model.Remove(1, 3);
            using (var b = Job(witness, model, "create-floors"))
            {
                Check(b.Before == HeronRollbackWitness.GoneSentence("create-walls"),
                      "everything A made gone before B: \"" + b.Before + "\"");
                Check(witness.KeptCount(Doc) == 0, "and A is forgotten");
                outcome = witness.RolledBack(b, true, model.Resolves);
            }
            Check(outcome.Lost == 0 && outcome.Also() == HeronRollbackWitness.GoneSentence("create-walls"),
                  "B's rollback blames nothing, and carries the start's note once");
            using (var c = Job(witness, model, "create-floors"))
            {
                Check(c.Before == "", "and the note is not said a second time");
            }

            // 6. CTRL+Z ON A, THEN B: reported as the modeller's undo. NEEDS-CHECKING's
            // second rollback-check row.
            witness = new HeronRollbackWitness();
            model = new Model();
            KeptJob(witness, model, "create-walls", 1, 3);
            witness.Undone(Doc, new[] { "Heron: create-walls" });
            model.Remove(1, 3);
            using (var b = Job(witness, model, "create-floors"))
            {
                Check(b.Before == HeronRollbackWitness.UndoneSentence("create-walls"),
                      "an undo of A's own entry is named as an undo: \"" + b.Before + "\"");
                outcome = witness.RolledBack(b, true, model.Resolves);
            }
            Check(outcome.Lost == 0, "and B's rollback is not blamed for it");

            // ...only A's OWN entry, in A's own document, marks it.
            witness = new HeronRollbackWitness();
            model = new Model();
            KeptJob(witness, model, "create-walls", 1, 3);
            witness.Undone(Doc, new[] { "Heron: create-floors", "Move" });
            witness.Undone(Other, new[] { "Heron: create-walls" });
            witness.Undone(Doc, null);
            model.Remove(1, 3);
            using (var b = Job(witness, model, "create-floors"))
            {
                Check(b.Before == HeronRollbackWitness.GoneSentence("create-walls"),
                      "another entry undone, or the same name undone in another model, is not A's undo");
            }

            // ...and a redo takes the mark back off.
            witness = new HeronRollbackWitness();
            model = new Model();
            KeptJob(witness, model, "create-walls", 1, 3);
            witness.Undone(Doc, new[] { "Heron: create-walls" });
            witness.Redone(Doc, new[] { "Heron: create-walls" });
            model.Remove(1, 3);
            using (var b = Job(witness, model, "create-floors"))
            {
                Check(b.Before == HeronRollbackWitness.GoneSentence("create-walls"),
                      "undone then redone, A is no longer expected to go");
            }
            // ...and a mark is not a verdict: what resolves is what counts.
            witness = new HeronRollbackWitness();
            model = new Model();
            KeptJob(witness, model, "create-walls", 1, 3);
            witness.Undone(Doc, new[] { "Heron: create-walls" });
            using (var b = Job(witness, model, "create-floors"))
            {
                Check(b.Before == "" && witness.KeptCount(Doc) == 3,
                      "an undo mark with A's elements all still there says nothing and keeps watching");
                model.Remove(1, 1);
                outcome = witness.RolledBack(b, true, model.Resolves);
            }
            Check(outcome.Lost == 1, "and a loss DURING the job is still a loss");

            // 7. THE SECOND WITNESS: Revit says RolledBack, and what the job added
            // is still there.
            witness = new HeronRollbackWitness();
            model = new Model();
            using (var b = Job(witness, model, "create-floors"))
            {
                model.Add(b, 10, 3);
                model.Remove(10, 1);                                // only one went
                outcome = witness.RolledBack(b, true, model.Resolves);
            }
            Check(outcome.Reported && !outcome.Held && outcome.Standing == 2,
                  "two of the three added still standing withdraw the rollback (" + outcome.Standing + ")");
            Check(outcome.ModelSentence().StartsWith("Revit reported the rollback, but 2 of the 3 elements "
                      + "this run added are still in the model, so THE ROLLBACK DID NOT HOLD.", StringComparison.Ordinal),
                  "and say so, whatever the status said: \"" + outcome.ModelSentence() + "\"");
            Check(outcome.Unsaid() == outcome.ModelSentence(),
                  "on a refusal that said nothing about the model as well");
            using (var b = Job(witness, model, "create-floors"))
            {
                model.Add(b, 20, 1);
                outcome = witness.RolledBack(b, false, model.Resolves);
            }
            Check(outcome.ModelSentence().Contains("THE MODEL MAY STILL HOLD PART OF THIS JOB - 1 of the "
                      + "1 element this run added is still in it."),
                  "unconfirmed AND standing names both: \"" + outcome.ModelSentence() + "\"");
            Check(witness.KeptCount(Doc) == 0, "and what a failed rollback left is never taken as kept work");

            // 8. AN ID THAT COULD NOT BE ASKED ABOUT IS NEITHER LOST NOR STANDING.
            witness = new HeronRollbackWitness();
            model = new Model();
            KeptJob(witness, model, "create-walls", 1, 3);
            model.Unaskable.Add(Uid(1));
            model.Remove(1, 1);
            using (var b = Job(witness, model, "create-floors"))
            {
                Check(witness.KeptCount(Doc) == 3, "an unaskable id is not narrowed away as deleted");
                model.Add(b, 10, 2);
                model.Unaskable.Add(Uid(10));
                outcome = witness.RolledBack(b, true, model.Resolves);
            }
            Check(outcome.Lost == 0 && outcome.KeptChecked == 2,
                  "and is not counted as lost - 2 of A's 3 were checked (" + outcome.KeptChecked + ")");
            Check(outcome.Checked == 1 && outcome.Standing == 1 && !outcome.Held,
                  "nor as checked among B's: the one answered is standing (" + outcome.Checked + ")");
            witness = new HeronRollbackWitness();
            model = new Model();
            using (var b = Job(witness, model, "create-floors"))
            {
                model.Add(b, 10, 2);
                outcome = witness.RolledBack(b, true, id => { throw new InvalidOperationException("no"); });
            }
            Check(outcome.Held && outcome.Checked == 0
                  && outcome.ModelSentence() == "Revit confirmed the rollback, so the model is exactly as it was.",
                  "a lookup that throws checks nothing, and nothing is said about the additions");

            // 9. A KEPT JOB THAT ADDS NOTHING keeps A watched - but narrowed, so
            // what it deleted on purpose is never reported later as lost.
            witness = new HeronRollbackWitness();
            model = new Model();
            KeptJob(witness, model, "create-walls", 1, 3);
            using (var b = Job(witness, model, "delete-elements"))
            {
                model.Remove(3, 1);
                witness.Assimilated(b, model.Resolves);
            }
            Check(witness.KeptCount(Doc) == 2, "a kept job that deletes one of A's elements narrows A to 2");
            using (var c = Job(witness, model, "create-floors"))
            {
                outcome = witness.RolledBack(c, true, model.Resolves);
            }
            Check(outcome.Lost == 0 && outcome.KeptJob == "create-walls",
                  "and the next rollback does not report the deliberate delete");
            // ...and a kept job that ADDS becomes the one watched.
            KeptJob(witness, model, "create-doors", 30, 4);
            using (var c = Job(witness, model, "create-floors"))
            {
                outcome = witness.RolledBack(c, true, model.Resolves);
            }
            Check(outcome.KeptJob == "create-doors" && outcome.KeptChecked == 4,
                  "a kept job that added is the one watched next (" + outcome.KeptJob + ")");

            // 10. THE SAMPLE: capped, counted whole, and the cap is said.
            witness = new HeronRollbackWitness();
            model = new Model();
            using (var a = Job(witness, model, "paste-views"))
            {
                model.Add(a, 1, 950);
                model.Add(a, 1, 10);                                // reported twice
                Check(a.AddedCount == 950, "950 different elements are counted once each (" + a.AddedCount + ")");
                Check(a.Sample.Count == HeronRollbackWitness.SampleLimit,
                      "and " + HeronRollbackWitness.SampleLimit + " are sampled (" + a.Sample.Count + ")");
                witness.Assimilated(a, model.Resolves);
            }
            Check(witness.KeptCount(Doc) == HeronRollbackWitness.SampleLimit,
                  "and the sample is what is kept (" + witness.KeptCount(Doc) + ")");
            witness = new HeronRollbackWitness();
            model = new Model();
            KeptJob(witness, model, "paste-views", 1, 950);
            using (var b = Job(witness, model, "create-floors"))
            {
                model.Remove(1, 5);
                outcome = witness.RolledBack(b, true, model.Resolves);
            }
            Check(outcome.LossSentence() == "Rolling back this run removed 5 of 200 elements the last kept "
                      + "job made ('paste-views') - rebuild that stage. Heron watches 200 of the 950 it made, "
                      + "so more of it may be gone.",
                  "a sampled job's loss says it was a sample: \"" + outcome.LossSentence() + "\"");
            witness = new HeronRollbackWitness();
            model = new Model();
            using (var b = Job(witness, model, "paste-views"))
            {
                model.Add(b, 1, 950);
                model.Remove(1, 950);
                outcome = witness.RolledBack(b, true, model.Resolves);
            }
            Check(outcome.NoneStanding() == "none of the 200 Heron looked for, of the 950 elements this run "
                      + "added, is still in the model",
                  "and so does a sampled job's own rollback: \"" + outcome.NoneStanding() + "\"");

            // 11. ONE ELEMENT READS AS ONE.
            witness = new HeronRollbackWitness();
            model = new Model();
            KeptJob(witness, model, "create-wall", 1, 1);
            using (var b = Job(witness, model, "create-floor"))
            {
                model.Add(b, 10, 1);
                model.Remove(10, 1);
                model.Remove(1, 1);
                outcome = witness.RolledBack(b, true, model.Resolves);
            }
            Check(outcome.LossSentence().StartsWith("Rolling back this run removed 1 of 1 element the last",
                      StringComparison.Ordinal)
                  && outcome.ModelSentence().EndsWith("- and the one element this run added is gone again.",
                      StringComparison.Ordinal),
                  "\"" + outcome.LossSentence() + "\" / \"" + outcome.ModelSentence() + "\"");

            // 12. DOCUMENTS ARE KEPT APART, AND BOUNDED.
            witness = new HeronRollbackWitness();
            model = new Model();
            KeptJob(witness, model, "create-walls", 1, 3);
            using (var b = witness.Begin(Other, "create-floors", "Heron: create-floors", model.Resolves))
            {
                model.Remove(1, 3);
                outcome = witness.RolledBack(b, true, model.Resolves);
            }
            Check(outcome.Lost == 0 && outcome.KeptJob == null && witness.KeptCount(Doc) == 3,
                  "a rollback in another model is not judged against this one's kept work");
            witness = new HeronRollbackWitness();
            model = new Model();
            for (var d = 0; d <= HeronRollbackWitness.MaxDocuments; d++)
            {
                using (var a = witness.Begin("doc-" + d, "j", "Heron: j", model.Resolves))
                {
                    model.Add(a, 1000 * (d + 1), 1);
                    witness.Assimilated(a, model.Resolves);
                }
            }
            Check(witness.KeptCount("doc-0") == 0 && witness.KeptCount("doc-1") == 1
                  && witness.KeptCount("doc-" + HeronRollbackWitness.MaxDocuments) == 1,
                  "past " + HeronRollbackWitness.MaxDocuments + " documents the longest untouched is dropped");
            using (var a = witness.Begin(null, "j", "Heron: j", null))
            {
                Check(a.Before == "" && witness.Assimilated(a, null) == "",
                      "a job with no document identity is watched for nothing, and says nothing");
            }

            // 13. ARMING: one job at a time, and an old job's end never disarms a new one.
            witness = new HeronRollbackWitness();
            model = new Model();
            var first = Job(witness, model, "one");
            var second = Job(witness, model, "two");
            first.Dispose();
            Check(ReferenceEquals(witness.Armed, second), "ending an earlier watch leaves the current one armed");
            second.Dispose();
            Check(witness.Armed == null, "and ending the current one disarms");

            // 14. WHAT REVIT'S ROLLBACK EVENT NAMED, carried for NEEDS-CHECKING.
            witness = new HeronRollbackWitness();
            model = new Model();
            using (var b = Job(witness, model, "create-floors"))
            {
                Check(b.EventNamed == -1, "no rollback event yet reads -1, not 0");
                model.Add(b, 10, 3);
                b.RollbackNamed(new[] { Id(10), Id(11), Id(99), null });
                model.Remove(10, 3);
                outcome = witness.RolledBack(b, true, model.Resolves);
            }
            Check(outcome.EventNamed == 2, "the event's ids are matched against what the job added (" + outcome.EventNamed + ")");

            // 15. A REFUSAL CARRIES THE SENTENCE IN THE MESSAGE THE CHAT PRINTS,
            // through the bridge's own writer and reader, and an ordinary one is
            // untouched. The message holds what Revit's own words can hold.
            var threw = Json.Error("fragment_threw",
                "'create-walls' threw while running: \"Level 1\" is not\nvalid at C:\\Jobs\\Tower.rvt - 50% done");
            var loss = "Rolling back this run removed 2 of 3 elements the last kept job made ('create-walls') - rebuild that stage.";
            Check(ReferenceEquals(HeronRollbackWitness.Told(threw, ""), threw)
                  && HeronRollbackWitness.Told(threw, null) == threw,
                  "nothing to add leaves a refusal exactly as it was");
            var told = HeronRollbackWitness.Told(threw, loss);
            Check(Json.ReadString(told, "error") == "fragment_threw"
                  && told.StartsWith("{\"ok\": false", StringComparison.Ordinal),
                  "a refusal stays the same refusal");
            Check(Json.ReadString(told, "message") == Json.ReadString(threw, "message") + " " + loss,
                  "and its message ends with the sentence, quotes, line break and path intact");
            var odd = Json.Ok(Json.Str("ran", "create-walls"), Json.Num("created", 2));
            told = HeronRollbackWitness.Told(odd, loss);
            Check(Json.ReadString(told, "rollbackNote") == loss && Json.ReadString(told, "ran") == "create-walls"
                  && Json.ReadLong(told, "created") == 2,
                  "a reply that is not a plain refusal keeps its fields and carries the sentence beside them");
            Check(HeronRollbackWitness.Told(null, loss) == null && HeronRollbackWitness.Told("", loss) == "",
                  "and no reply is never invented into one");

            Console.WriteLine();
            if (Failures.Count > 0)
            {
                Console.WriteLine("FAILED - " + Failures.Count + " of " + _checks);
                return 1;
            }
            Console.WriteLine("PASSED - " + _checks + " checks. A loss during a rollback is said once,");
            Console.WriteLine("what the modeller removed is never blamed on one, and a rollback whose");
            Console.WriteLine("additions still stand is never called complete.");
            return 0;
        }
    }
}
