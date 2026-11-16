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

        // ------------------------------------------------------------ values

        // PARAMETER IDS MADE UP FOR THE TEST, not Revit's: what is checked is
        // that the two reads are paired by id, whatever the ids are. Two
        // parameters share the name "Mark" on purpose (D-156's pairing rule).
        private const long CommentsId = 5001;
        private const long MarkId = 5002;
        private const long EditedById = 5003;
        private const long OtherMarkId = 5004;

        /// <summary>One element's values, as the reader would find them.</summary>
        private sealed class Thing
        {
            internal string Category;
            internal string Name;
            internal string Comments = "";
            internal string Mark = "A";          // null: the parameter is not there
            internal string OtherMark = "A";
            internal string EditedBy = "";
        }

        /// <summary>
        /// A model of values: what each UniqueId reads now, and which cannot be
        /// read (null) or throw when read. Read is the reader RevitFragment
        /// hands the witness, in miniature.
        /// </summary>
        private sealed class Values
        {
            internal readonly Dictionary<string, Thing> Things = new Dictionary<string, Thing>(StringComparer.Ordinal);
            internal readonly HashSet<string> Unreadable = new HashSet<string>(StringComparer.Ordinal);
            internal readonly HashSet<string> Throws = new HashSet<string>(StringComparer.Ordinal);

            internal Thing Put(string uid, string category, string name)
            {
                var thing = new Thing { Category = category, Name = name };
                Things[uid] = thing;
                return thing;
            }

            internal Func<string, HeronReadBack.Values> Read
            {
                get
                {
                    return uid =>
                    {
                        if (Throws.Contains(uid)) throw new InvalidOperationException("cannot be read");
                        Thing thing;
                        if (Unreadable.Contains(uid) || !Things.TryGetValue(uid, out thing)) return null;
                        var values = new HeronReadBack.Values(thing.Category) { UniqueId = uid };
                        values.Add("Name", thing.Name);
                        var parameters = new List<HeronReadBack.ParameterRead>
                        {
                            new HeronReadBack.ParameterRead(CommentsId, "Comments", thing.Comments),
                            new HeronReadBack.ParameterRead(EditedById, "Edited by", thing.EditedBy),
                            new HeronReadBack.ParameterRead(OtherMarkId, "Mark", thing.OtherMark),
                        };
                        if (thing.Mark != null)
                            parameters.Add(new HeronReadBack.ParameterRead(MarkId, "Mark", thing.Mark));
                        values.AddById(parameters);
                        return values;
                    };
                }
            }

            /// <summary>Every element read, as RevitFragment reads them before the job.</summary>
            internal List<HeronReadBack.Values> ReadAll()
            {
                var read = new List<HeronReadBack.Values>();
                foreach (var uid in Things.Keys)
                {
                    var one = Read(uid);
                    if (one != null) read.Add(one);
                }
                return read;
            }

            /// <summary>What each element reads now, to put back - a clean rollback.</summary>
            internal Dictionary<string, Thing> Copy()
            {
                var copy = new Dictionary<string, Thing>(StringComparer.Ordinal);
                foreach (var pair in Things)
                {
                    var t = pair.Value;
                    copy[pair.Key] = new Thing
                    {
                        Category = t.Category, Name = t.Name, Comments = t.Comments,
                        Mark = t.Mark, OtherMark = t.OtherMark, EditedBy = t.EditedBy
                    };
                }
                return copy;
            }

            internal void PutBack(Dictionary<string, Thing> copy, params string[] only)
            {
                foreach (var uid in only) Things[uid] = copy[uid];
            }
        }

        /// <summary>Eleven floor plans, as A16b's job found them: eight with an L in the name.</summary>
        private static Values ElevenPlans()
        {
            var values = new Values();
            var names = new[] { "L2", "L1 - Block 35", "L1 - Block 37", "L3", "L4", "L5", "L6", "L7",
                                "Parking", "Site", "Roof" };
            for (var n = 0; n < names.Length; n++) values.Put(Uid(100 + n), "Views", names[n]);
            return values;
        }

        /// <summary>The job's rename: "L" becomes "HERONL" wherever it appears.</summary>
        private static void Rename(Values values)
        {
            foreach (var thing in values.Things.Values)
                if (thing.Name.Contains("L")) thing.Name = thing.Name.Replace("L", "HERONL");
        }

        /// <summary>
        /// What a modified element that survives a rollback looks like to the
        /// check, and that one that did not survive, or could not be read, is
        /// never called either. NEEDS-CHECKING A16b, the shape it was seen in.
        /// </summary>
        private static void Changes()
        {
            Console.WriteLine();
            Console.WriteLine("A value a job changed that a rollback did not put back is said");
            HeronRollbackWitness.Outcome outcome;
            var none = new long[0];

            // 16. A RENAME THAT STOOD - A16b's own shape: Revit reported the
            // group rolled back, nothing was added, and 8 views still read
            // HERONL... The addition count alone called this clean.
            var witness = new HeronRollbackWitness();
            var model = new Model();
            var values = ElevenPlans();
            using (var job = Job(witness, model, "rename-elements"))
            {
                job.ReadBefore(values.ReadAll(), none);
                Check(job.ReadBeforeCount == 11, "the 11 views handed to the job are read before it (" + job.ReadBeforeCount + ")");
                Rename(values);
                job.ReadDuring(values.Read);
                Check(job.ChangedCount == 8, "and the 8 it renamed are what it changed (" + job.ChangedCount + ")");
                outcome = witness.RolledBack(job, true, model.Resolves, values.Read);   // the names stay
            }
            Check(outcome.Reported && !outcome.Held && outcome.Stands && outcome.Standing == 0,
                  "Revit's \"rolled back\" with no addition standing is NOT a rollback that held");
            Check(outcome.Changed == 8 && outcome.ChangedChecked == 8 && outcome.ChangedStanding == 8,
                  "all 8 renamed views were read back and all 8 still hold the change");
            Check(outcome.ModelSentence() == "Revit reported the rollback, but all 8 elements this run changed "
                      + "that Heron read back still hold the change (Views: Name reads 'HERONL2', was 'L2'), so "
                      + "THE ROLLBACK DID NOT HOLD. Check what it would have changed before trusting anything "
                      + "here, and do not save until you have.",
                  "and it says so in Revit words: \"" + outcome.ModelSentence() + "\"");
            Check(outcome.Unsaid() == outcome.ModelSentence() && outcome.Account() == outcome.ModelSentence(),
                  "a refusal that said nothing about the model now carries it");
            Check(outcome.StillThere().StartsWith("All 8 elements this run changed", StringComparison.Ordinal),
                  "and the Discard's sentence starts as a sentence: \"" + outcome.StillThere() + "\"");

            // 17. SOME CAME BACK: only the ones that did not are counted.
            witness = new HeronRollbackWitness();
            values = ElevenPlans();
            var was = values.Copy();
            using (var job = Job(witness, model, "rename-elements"))
            {
                job.ReadBefore(values.ReadAll(), none);
                Rename(values);
                job.ReadDuring(values.Read);
                values.PutBack(was, Uid(101), Uid(102), Uid(103), Uid(104), Uid(105));
                outcome = witness.RolledBack(job, true, model.Resolves, values.Read);
            }
            Check(!outcome.Held && outcome.ChangedChecked == 8 && outcome.ChangedStanding == 3,
                  "5 of 8 put back leaves 3 still changed (" + outcome.ChangedStanding + ")");
            Check(outcome.ChangedStill() == "3 of the 8 elements this run changed that Heron read back still "
                      + "hold the change (Views: Name reads 'HERONL2', was 'L2')",
                  "\"" + outcome.ChangedStill() + "\"");

            // 18. A PARAMETER THAT STOOD: Comments on three ducts, two put back.
            witness = new HeronRollbackWitness();
            values = new Values();
            for (var n = 0; n < 3; n++) values.Put(Uid(200 + n), "Ducts", "Rectangular Duct");
            was = values.Copy();
            using (var job = Job(witness, model, "write-element-parameters"))
            {
                job.ReadBefore(values.ReadAll(), none);
                foreach (var thing in values.Things.Values) thing.Comments = "X";
                job.ReadDuring(values.Read);
                values.PutBack(was, Uid(201), Uid(202));
                outcome = witness.RolledBack(job, true, model.Resolves, values.Read);
            }
            Check(!outcome.Held && outcome.ChangedChecked == 3 && outcome.ChangedStanding == 1,
                  "a parameter value left behind withdraws the rollback");
            Check(outcome.ChangedStill() == "1 of the 3 elements this run changed that Heron read back still "
                      + "holds the change (Ducts: Comments reads 'X', was blank)",
                  "\"" + outcome.ChangedStill() + "\"");

            // 19. TWO PARAMETERS CALLED Mark ARE TOLD APART BY ID - the one that
            // stood is named, and the one the job never touched is not checked.
            witness = new HeronRollbackWitness();
            values = new Values();
            values.Put(Uid(300), "Doors", "Single-Flush");
            using (var job = Job(witness, model, "write-element-parameters"))
            {
                job.ReadBefore(values.ReadAll(), none);
                values.Things[Uid(300)].Mark = "B";
                job.ReadDuring(values.Read);
                outcome = witness.RolledBack(job, true, model.Resolves, values.Read);
            }
            Check(!outcome.Held && outcome.StoodName == "Mark (id " + MarkId + ")"
                  && outcome.StoodWas == "A" && outcome.StoodNow == "B",
                  "the Mark that stood is named by its own id: \"" + outcome.ChangedStill() + "\"");

            // 20. A CLEAN ROLLBACK: everything read back as before, and the
            // sentence says what was looked at.
            witness = new HeronRollbackWitness();
            model = new Model();
            values = ElevenPlans();
            was = values.Copy();
            using (var job = Job(witness, model, "rename-elements"))
            {
                job.ReadBefore(values.ReadAll(), none);
                Rename(values);
                model.Add(job, 10, 2);
                job.ReadDuring(values.Read);
                values.PutBack(was, new List<string>(was.Keys).ToArray());
                model.Remove(10, 2);
                outcome = witness.RolledBack(job, true, model.Resolves, values.Read);
            }
            Check(outcome.Held && !outcome.Stands && outcome.ChangedChecked == 8 && outcome.ChangedStanding == 0,
                  "a rename put back by the rollback holds");
            Check(outcome.ModelSentence() == "Revit confirmed the rollback, so the model is exactly as it was"
                      + " - and none of the 2 elements this run added is still in the model, and the 8 elements "
                      + "this run changed that Heron read back read as they did before it.",
                  "and says only what it checked: \"" + outcome.ModelSentence() + "\"");
            Check(outcome.Unsaid() == "" && outcome.Also() == "",
                  "a clean refusal is unchanged by it");

            // 21. AN ELEMENT THAT COULD NOT BE READ IS NEITHER - not standing,
            // not put back (Article 27). One cannot be read after, one throws,
            // one cannot be read during the job and so is not counted changed.
            witness = new HeronRollbackWitness();
            values = new Values();
            for (var n = 0; n < 4; n++) values.Put(Uid(400 + n), "Views", "L" + n);
            was = values.Copy();
            using (var job = Job(witness, model, "rename-elements"))
            {
                job.ReadBefore(values.ReadAll(), none);
                Rename(values);
                values.Unreadable.Add(Uid(403));
                job.ReadDuring(values.Read);
                Check(job.ChangedCount == 3, "one that cannot be read during the job is not counted changed");
                values.Unreadable.Remove(Uid(403));
                values.Unreadable.Add(Uid(400));
                values.Throws.Add(Uid(401));
                values.PutBack(was, Uid(402));
                outcome = witness.RolledBack(job, true, model.Resolves, values.Read);
            }
            Check(outcome.Held && outcome.Changed == 3 && outcome.ChangedChecked == 1
                  && outcome.ChangedStanding == 0,
                  "one unread and one that threw count as neither; the one read back held ("
                  + outcome.ChangedChecked + " checked)");
            Check(outcome.ModelSentence() == "Revit confirmed the rollback, so the model is exactly as it was"
                      + " - and the one element this run changed that Heron read back reads as it did before it.",
                  "\"" + outcome.ModelSentence() + "\"");
            witness = new HeronRollbackWitness();
            values = ElevenPlans();
            using (var job = Job(witness, model, "rename-elements"))
            {
                job.ReadBefore(values.ReadAll(), none);
                Rename(values);
                job.ReadDuring(values.Read);
                foreach (var uid in values.Things.Keys) values.Unreadable.Add(uid);
                outcome = witness.RolledBack(job, true, model.Resolves, values.Read);
            }
            Check(outcome.Held && outcome.ChangedChecked == 0
                  && outcome.ModelSentence() == "Revit confirmed the rollback, so the model is exactly as it was.",
                  "none read back says nothing about them, either way");

            // 22. "Edited by" IS NOT THE CHANGE: a borrow alone is not counted.
            witness = new HeronRollbackWitness();
            values = new Values();
            values.Put(Uid(500), "Ducts", "Rectangular Duct");
            using (var job = Job(witness, model, "write-element-parameters"))
            {
                job.ReadBefore(values.ReadAll(), new[] { EditedById });
                values.Things[Uid(500)].EditedBy = "modeller";
                job.ReadDuring(values.Read);
                Check(job.ChangedCount == 0, "a value named as not the change is not counted changed");
                outcome = witness.RolledBack(job, true, model.Resolves, values.Read);
            }
            Check(outcome.Held && outcome.Changed == 0, "so a borrow that outlives the rollback is no alarm");

            // 23. A THIRD VALUE, A PARAMETER ADDED, A PARAMETER REMOVED - each
            // is a value that did not come back.
            witness = new HeronRollbackWitness();
            values = new Values();
            values.Put(Uid(600), "Views", "L2");
            values.Put(Uid(601), "Views", "L3").Mark = null;
            values.Put(Uid(602), "Views", "L4");
            using (var job = Job(witness, model, "rename-elements"))
            {
                job.ReadBefore(values.ReadAll(), none);
                values.Things[Uid(600)].Name = "HERONL2";
                values.Things[Uid(601)].Mark = "M1";
                values.Things[Uid(602)].Mark = null;
                job.ReadDuring(values.Read);
                values.Things[Uid(600)].Name = "XL2";
                outcome = witness.RolledBack(job, true, model.Resolves, values.Read);
            }
            Check(!outcome.Held && outcome.ChangedStanding == 3 && outcome.StoodNow == "XL2",
                  "a value that came back as neither before nor the job's still counts: \"" + outcome.ChangedStill() + "\"");
            witness = new HeronRollbackWitness();
            values = new Values();
            values.Put(Uid(601), "Views", "L3").Mark = null;
            using (var job = Job(witness, model, "j"))
            {
                job.ReadBefore(values.ReadAll(), none);
                values.Things[Uid(601)].Mark = "M1";
                job.ReadDuring(values.Read);
                outcome = witness.RolledBack(job, true, model.Resolves, values.Read);
            }
            Check(outcome.ChangedStill().EndsWith("(Views: Mark (id " + MarkId + ") reads 'M1', was not there)", StringComparison.Ordinal),
                  "\"" + outcome.ChangedStill() + "\"");
            witness = new HeronRollbackWitness();
            values = new Values();
            values.Put(Uid(602), "Views", "L4");
            using (var job = Job(witness, model, "j"))
            {
                job.ReadBefore(values.ReadAll(), none);
                values.Things[Uid(602)].Mark = null;
                job.ReadDuring(values.Read);
                outcome = witness.RolledBack(job, true, model.Resolves, values.Read);
            }
            Check(outcome.ChangedStill().EndsWith("(Views: Mark (id " + MarkId + ") is not there now, was 'A')", StringComparison.Ordinal),
                  "\"" + outcome.ChangedStill() + "\"");

            // 24. ADDED AND CHANGED BOTH STANDING, and a rollback Revit never
            // confirmed: one clause each, in both sentences.
            witness = new HeronRollbackWitness();
            model = new Model();
            values = new Values();
            values.Put(Uid(700), "Views", "L2");
            using (var job = Job(witness, model, "rename-elements"))
            {
                job.ReadBefore(values.ReadAll(), none);
                values.Things[Uid(700)].Name = "HERONL2";
                model.Add(job, 10, 2);
                job.ReadDuring(values.Read);
                model.Remove(10, 1);
                outcome = witness.RolledBack(job, true, model.Resolves, values.Read);
            }
            Check(outcome.ModelSentence().StartsWith("Revit reported the rollback, but 1 of the 2 elements this "
                      + "run added is still in the model, and the one element this run changed that Heron read "
                      + "back still holds the change (Views: Name reads 'HERONL2', was 'L2'), so THE ROLLBACK "
                      + "DID NOT HOLD.", StringComparison.Ordinal),
                  "\"" + outcome.ModelSentence() + "\"");
            witness = new HeronRollbackWitness();
            values = new Values();
            values.Put(Uid(700), "Views", "L2");
            values.Put(Uid(701), "Views", "L3");
            using (var job = Job(witness, model, "rename-elements"))
            {
                job.ReadBefore(values.ReadAll(), none);
                Rename(values);
                job.ReadDuring(values.Read);
                outcome = witness.RolledBack(job, false, model.Resolves, values.Read);
            }
            Check(outcome.ModelSentence().StartsWith("Revit did NOT confirm the rollback, so THE MODEL MAY STILL "
                      + "HOLD PART OF THIS JOB - both elements this run changed that Heron read back still hold "
                      + "the change (Views: Name reads 'HERONL2', was 'L2').", StringComparison.Ordinal),
                  "\"" + outcome.ModelSentence() + "\"");

            // 25. ONCE, AND NEVER AFTER THE ROLLBACK: a second look would read
            // the rolled-back model as the job's work. And a caller that hands
            // no reader - RevitWrite, RevitCapture - says what it always said.
            witness = new HeronRollbackWitness();
            values = ElevenPlans();
            was = values.Copy();
            using (var job = Job(witness, model, "rename-elements"))
            {
                job.ReadBefore(values.ReadAll(), none);
                Rename(values);
                job.ReadDuring(values.Read);
                values.PutBack(was, new List<string>(was.Keys).ToArray());
                job.ReadDuring(values.Read);
                Check(job.ChangedCount == 8, "a second ReadDuring changes nothing");
                outcome = witness.RolledBack(job, true, model.Resolves, values.Read);
                Rename(values);
                job.ReadDuring(values.Read);
                Check(ReferenceEquals(witness.RolledBack(job, true, model.Resolves, values.Read), outcome)
                      && outcome.Held,
                      "and the first answer stands");
            }
            witness = new HeronRollbackWitness();
            values = ElevenPlans();
            using (var job = Job(witness, model, "rename-elements"))
            {
                job.ReadBefore(values.ReadAll(), none);
                Rename(values);
                job.ReadDuring(values.Read);
                outcome = witness.RolledBack(job, true, model.Resolves);
            }
            Check(outcome.Held && outcome.ChangedChecked == 0
                  && outcome.ModelSentence() == "Revit confirmed the rollback, so the model is exactly as it was.",
                  "no reader after the rollback reads as unchecked, never as held or stood");
            witness = new HeronRollbackWitness();
            using (var job = Job(witness, model, "rename-elements"))
            {
                var nameless = new HeronReadBack.Values("Views");
                nameless.Add("Name", "L2");
                job.ReadBefore(new[] { nameless, null }, null);
                Check(job.ReadBeforeCount == 0, "an element whose UniqueId was not read cannot be found again, so is not kept");
            }
        }

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

            Changes();

            Console.WriteLine();
            if (Failures.Count > 0)
            {
                Console.WriteLine("FAILED - " + Failures.Count + " of " + _checks);
                return 1;
            }
            Console.WriteLine("PASSED - " + _checks + " checks. A loss during a rollback is said once,");
            Console.WriteLine("what the modeller removed is never blamed on one, and a rollback whose");
            Console.WriteLine("additions still stand, or whose changes did not come back, is never");
            Console.WriteLine("called complete.");
            return 0;
        }
    }
}
