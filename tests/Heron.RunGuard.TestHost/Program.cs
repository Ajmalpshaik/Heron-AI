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
using Heron.Core;
using Heron.Revit.Addin;

namespace Heron.RunGuard.TestHost
{
    /// <summary>
    /// The run guard's policy, checked without Revit.
    ///
    /// Until this change a MODIFY fragment could call Save, a sync with
    /// central or an export and nothing in Heron stood in its way -
    /// Constitution Article 10 was a sentence. HeronRunGuard now listens for
    /// those while a job runs and asks HeronRunPolicy what to do. That
    /// judgement - risk x what happened x where the document came from - and
    /// the sentence the modeller reads, are what this checks. Whether Revit
    /// raises the events, lets them be cancelled and closes what it is told
    /// to is the PC's to prove: docs/needs-checking/group-ex.md.
    ///
    /// EVERY LEVEL IS WALKED, not a sample. HeronRisk is an ordered enum, and
    /// the policy's answers change at two of its values (Admin and Publish) -
    /// so a check on Modify alone would pass a rule that let Analyze, the
    /// read door, through.
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

        private static readonly HeronRunPolicy.Act[] Acts =
        {
            HeronRunPolicy.Act.Save, HeronRunPolicy.Act.SaveAs, HeronRunPolicy.Act.Sync,
            HeronRunPolicy.Act.Export, HeronRunPolicy.Act.ExportView, HeronRunPolicy.Act.Print,
        };

        // The levels below Admin that are not Publish: the reads, the
        // selection, and the change. Every one of them is held to the
        // strict rule.
        private static readonly HeronRisk[] Strict =
        {
            HeronRisk.Read, HeronRisk.Analyze, HeronRisk.Suggest, HeronRisk.Execute,
            HeronRisk.Modify,
        };

        private static int Main()
        {
            Console.WriteLine("Heron.RunGuard.TestHost - the run guard's policy, without Revit");

            Publish();
            Admin();
            StrictLevels();
            HeronsOwnExport();
            AboveAdmin();
            Elsewhere();
            Record();
            Sentences();
            Withdrawn();
            Splice();
            EditFamily();
            SavesOwnStep();

            Console.WriteLine();
            if (Failures.Count > 0)
            {
                Console.WriteLine("FAILED  " + Failures.Count + " of " + _checks + " check(s)");
                foreach (var line in Failures) Console.WriteLine("  - " + line);
                return 1;
            }
            Console.WriteLine("PASS    " + _checks + " check(s)");
            return 0;
        }

        private static HeronRunPolicy For(HeronRisk risk)
        {
            return new HeronRunPolicy("run_fragment_test", risk);
        }

        private static void Publish()
        {
            Console.WriteLine();
            Console.WriteLine("1. Publish stands aside - nothing is cancelled under run_fragment_publish");

            var policy = For(HeronRisk.Publish);
            var cancelled = 0;
            foreach (var act in Acts)
            {
                foreach (HeronRunPolicy.Origin origin in Enum.GetValues(typeof(HeronRunPolicy.Origin)))
                {
                    if (policy.Cancels(act, origin)) cancelled++;
                }
            }
            Check(cancelled == 0,
                  "no save, sync, export or print is cancelled, wherever the document came from - "
                  + cancelled + " were");
            Check(!policy.RefusesNewDocument, "a new document is not refused");
            Check(!policy.IsElsewhere(HeronRunPolicy.Origin.OpenAtStart, false),
                  "and a change to another open model is not refused either");
            Check(policy.StandsAside, "it says it stands aside");
        }

        private static void Admin()
        {
            Console.WriteLine();
            Console.WriteLine("2. Admin may start a file and save it, and nothing that was already open");

            var policy = For(HeronRisk.Admin);
            foreach (var act in Acts)
            {
                Check(policy.Cancels(act, HeronRunPolicy.Origin.OpenAtStart),
                      act + " of a model open when the job began is cancelled");
                Check(!policy.Cancels(act, HeronRunPolicy.Origin.CreatedByJob),
                      act + " of a document the job started is allowed - create-family-document's SaveAs");
                Check(!policy.Cancels(act, HeronRunPolicy.Origin.OpenedByJob),
                      act + " of a document the job opened is allowed");
            }
            Check(!policy.RefusesNewDocument, "a new family or project document is not refused");
            Check(!policy.StandsAside, "and it does not stand aside");
        }

        private static void StrictLevels()
        {
            Console.WriteLine();
            Console.WriteLine("3. Every level below Admin but Publish - a read, a selection, a change");

            foreach (var risk in Strict)
            {
                var policy = For(risk);
                var wrong = new List<string>();
                foreach (var act in Acts)
                {
                    if (!policy.Cancels(act, HeronRunPolicy.Origin.OpenAtStart))
                        wrong.Add(act + " of an open model allowed");
                    if (!policy.Cancels(act, HeronRunPolicy.Origin.CreatedByJob))
                        wrong.Add(act + " of a refused new document allowed");
                    if (policy.Cancels(act, HeronRunPolicy.Origin.OpenedByJob))
                        wrong.Add(act + " of a document the job opened cancelled");
                }
                Check(wrong.Count == 0,
                      risk + ": cancels an open model's and a refused new document's, allows the "
                      + "job's own opened file" + (wrong.Count == 0 ? "" : " - " + string.Join("; ", wrong)));
                Check(policy.RefusesNewDocument, risk + ": a new family or project document is refused");
            }
        }

        private static void HeronsOwnExport()
        {
            Console.WriteLine();
            Console.WriteLine("4a. Heron's own export into its own folder - capture_view - and nothing else");

            foreach (var risk in Strict)
            {
                var policy = For(risk);
                Check(!policy.Cancels(HeronRunPolicy.Act.ExportView, HeronRunPolicy.Origin.OpenAtStart, true)
                      && !policy.Cancels(HeronRunPolicy.Act.Export, HeronRunPolicy.Origin.OpenAtStart, true),
                      risk + ": an export inside HeronExports() is let through");
                Check(policy.Cancels(HeronRunPolicy.Act.Save, HeronRunPolicy.Origin.OpenAtStart, true)
                      && policy.Cancels(HeronRunPolicy.Act.Sync, HeronRunPolicy.Origin.OpenAtStart, true)
                      && policy.Cancels(HeronRunPolicy.Act.Print, HeronRunPolicy.Origin.OpenAtStart, true),
                      risk + ": a save, a sync or a print inside it is still cancelled");
                Check(policy.Cancels(HeronRunPolicy.Act.ExportView, HeronRunPolicy.Origin.OpenAtStart, false),
                      risk + ": and an export outside it still is");
            }
        }

        private static void AboveAdmin()
        {
            Console.WriteLine();
            Console.WriteLine("4. A level nobody has added yet is held to the strict rule");

            var policy = For((HeronRisk)7);
            Check(policy.Cancels(HeronRunPolicy.Act.Sync, HeronRunPolicy.Origin.OpenAtStart),
                  "a sync of an open model is cancelled");
            Check(policy.Cancels(HeronRunPolicy.Act.SaveAs, HeronRunPolicy.Origin.CreatedByJob),
                  "a Save As of a new document is cancelled");
            Check(policy.RefusesNewDocument, "and a new document is refused - the rule fails closed");
        }

        private static void Elsewhere()
        {
            Console.WriteLine();
            Console.WriteLine("5. A change to another open model, and only to another open model");

            foreach (var risk in new[] { HeronRisk.Analyze, HeronRisk.Modify, HeronRisk.Admin })
            {
                var policy = For(risk);
                Check(policy.IsElsewhere(HeronRunPolicy.Origin.OpenAtStart, false),
                      risk + ": a model open when the job began, not the one it ran in, is elsewhere");
                Check(!policy.IsElsewhere(HeronRunPolicy.Origin.OpenAtStart, true),
                      risk + ": the model it ran in never is - transfer-views-between-documents "
                      + "writes there and only reads the other");
                Check(!policy.IsElsewhere(HeronRunPolicy.Origin.OpenedByJob, false)
                      && !policy.IsElsewhere(HeronRunPolicy.Origin.CreatedByJob, false),
                      risk + ": nor is a document the job opened or started");
            }
        }

        private static void Record()
        {
            Console.WriteLine();
            Console.WriteLine("6. The record counts every event, and keeps stopped apart from not stopped");

            var policy = For(HeronRisk.Modify);
            Check(!policy.Refuses && !policy.HasReport && policy.BlockedCount == 0,
                  "a fresh record refuses nothing and has nothing to report");

            policy.Stopped(HeronRunPolicy.Act.Export, "Tower A", true);
            policy.Stopped(HeronRunPolicy.Act.Export, "Tower A", true);
            policy.Stopped(HeronRunPolicy.Act.ExportView, "Tower A", false);
            Check(policy.StoppedActs.Count == 2,
                  "two exports of one model are one line, the view export another - "
                  + policy.StoppedActs.Count + " line(s)");
            Check(policy.StoppedActs[0].Times == 2, "and that line says twice");
            Check(policy.BlockedCount == 3, "blocked counts every event Revit raised - "
                  + policy.BlockedCount);
            Check(policy.Refuses && !policy.ChangedOtherModel,
                  "a stopped export refuses the job, and is not a change to another model");

            Check(policy.Created("Family1"), "a new document under a change is recorded as refused");
            policy.ChangedElsewhere("Tower B", new[] { "Heron: rename" });
            policy.ChangedElsewhere("Tower B", new[] { "Heron: rename" });
            Check(policy.ChangedOtherModel && policy.ChangesElsewhere.Count == 1
                  && policy.ChangesElsewhere[0].Times == 2,
                  "two commits of one transaction in one other model are one line, twice");
            Check(policy.BlockedCount == 6,
                  "and blocked adds the refused document and both commits - " + policy.BlockedCount);

            var admin = For(HeronRisk.Admin);
            Check(!admin.Created("Family1") && admin.RefusedDocuments.Count == 0 && !admin.Refuses,
                  "under Admin a new document is recorded as nothing at all");

            var housekeeping = For(HeronRisk.Analyze);
            housekeeping.Opened("Probe.rfa");
            housekeeping.Closed("Probe.rfa");
            Check(housekeeping.HasReport && !housekeeping.Refuses && housekeeping.ClosedCount == 1,
                  "a background file opened and closed is reported, and refuses nothing");
            housekeeping.CouldNotClose(null, null);
            Check(housekeeping.HasReport && !housekeeping.Refuses,
                  "one that would not close is reported too - and still refuses nothing");

            var unnamed = For(HeronRisk.Modify);
            unnamed.Stopped(HeronRunPolicy.Act.Save, null, true);
            Check(unnamed.StoppedActs[0].Document == "(unnamed)",
                  "a document Revit could not name is still recorded, as (unnamed)");
        }

        private static void Sentences()
        {
            Console.WriteLine();
            Console.WriteLine("7. What the modeller reads");

            var state = "Heron rolled the whole job back and Revit confirmed it.";

            var cancelled = For(HeronRisk.Modify);
            cancelled.Stopped(HeronRunPolicy.Act.Save, "Tower A", true);
            var said = cancelled.Refusal(state, "Save was cancelled.");
            Check(said.Contains("Stopped before it happened") && said.Contains("a save of 'Tower A'"),
                  "a cancelled save is named, as stopped");
            Check(!said.Contains("COULD NOT BE STOPPED"),
                  "and nothing is said to have slipped through when nothing did");
            Check(said.Contains("Publish") && said.Contains("Article 10"),
                  "it says saving is a Publish job, and why");
            Check(said.Contains("a change to the model"), "it names the kind of job in plain words");
            Check(said.Contains(state), "the caller's account of the model is carried whole");
            Check(said.Contains("What the job itself said: Save was cancelled."),
                  "and the fragment's own words are quoted, not lost");
            Check(!said.Contains("CANNOT UNDO"),
                  "no other model is mentioned when none was touched");

            var slipped = For(HeronRisk.Analyze);
            slipped.Stopped(HeronRunPolicy.Act.ExportView, "Tower A", false);
            var warned = slipped.Refusal(null, null);
            Check(warned.Contains("COULD NOT BE STOPPED") && warned.Contains("may already have happened"),
                  "an event Revit would not cancel is said to have possibly happened");
            Check(!warned.Contains("Stopped before it happened"),
                  "and is never called stopped");
            Check(warned.Contains("a read"), "a read is called a read");

            var rolledBack = For(HeronRisk.Modify);
            rolledBack.Stopped(HeronRunPolicy.Act.Sync, "Tower A", true);
            Check(!rolledBack.Refusal(null, null).Contains("exactly as it was"),
                  "the policy never claims the model was put back - only the caller can know");

            var created = For(HeronRisk.Modify);
            created.Created("Family1");
            var started = created.Refusal(null, null);
            Check(started.Contains("'Family1'") && started.Contains("Admin"),
                  "a refused new document is named, with the switch that would allow it");

            var elsewhere = For(HeronRisk.Admin);
            elsewhere.ChangedElsewhere("Tower B", new[] { "Heron: rename" });
            var other = elsewhere.Refusal(state, null);
            Check(other.Contains("'Tower B'") && other.Contains("CANNOT UNDO")
                  && other.Contains("before saving it"),
                  "a change to another model is named, said to stand, and the modeller is told "
                  + "to look at it - Article 27");
            Check(other.Contains("'Heron: rename'"), "with the transaction Revit named");
            Check(other.Contains("a project setup change"), "an Admin job is called a project setup change");
        }

        private static HeronConfig Switches(bool changes, bool admin, bool publish)
        {
            var config = new HeronConfig();
            config.Set(HeronPermissions.WriteEnabledKey, changes ? "true" : "false");
            config.Set(HeronPermissions.AdminEnabledKey, admin ? "true" : "false");
            config.Set(HeronPermissions.PublishEnabledKey, publish ? "true" : "false");
            return config;
        }

        private static void Withdrawn()
        {
            Console.WriteLine();
            Console.WriteLine("8. The switches asked again before anything is kept - Article 13");

            var allOn = Switches(true, true, true);
            foreach (var risk in new[] { HeronRisk.Modify, HeronRisk.Publish, HeronRisk.Admin })
            {
                Check(HeronRunPolicy.Withdrawn(risk, allOn, false) == null,
                      risk + " with every switch still on is kept");
            }

            var changesOff = HeronRunPolicy.Withdrawn(HeronRisk.Modify, Switches(false, true, true), false);
            Check(changesOff != null && changesOff.Contains("Changes"),
                  "a change with Changes turned off mid-job is not kept, and the switch is named - "
                  + (changesOff ?? "(kept)"));

            var adminOff = HeronRunPolicy.Withdrawn(HeronRisk.Admin, Switches(true, false, true), false);
            Check(adminOff != null && adminOff.Contains("Admin"),
                  "an Admin job with Admin turned off is not kept - " + (adminOff ?? "(kept)"));

            var publishOff = HeronRunPolicy.Withdrawn(HeronRisk.Publish, Switches(true, true, false), false);
            Check(publishOff != null && publishOff.Contains("Publish"),
                  "a Publish job with Publish turned off is not kept - " + (publishOff ?? "(kept)"));

            var adminChangesOff = HeronRunPolicy.Withdrawn(HeronRisk.Admin, Switches(false, true, true), false);
            Check(adminChangesOff != null && adminChangesOff.Contains("Changes"),
                  "Admin on and Changes off is still not kept - Admin never works without Changes");

            Check(HeronRunPolicy.Withdrawn(HeronRisk.Modify, null, false) != null,
                  "no config at all keeps nothing - it fails closed, as the gate does");

            var stopped = HeronRunPolicy.Withdrawn(HeronRisk.Modify, allOn, true);
            Check(stopped != null && stopped.Contains("stopped"),
                  "Heron stopped mid-job keeps nothing - " + (stopped ?? "(kept)"));

            var offEverywhere = Switches(false, false, false);
            Check(HeronRunPolicy.Withdrawn(HeronRisk.Analyze, offEverywhere, true) == null
                  && HeronRunPolicy.Withdrawn(HeronRisk.Read, offEverywhere, true) == null,
                  "a read is never withdrawn - it keeps nothing, and the stop leaves reads alone");

            var state = "Heron rolled the whole job back and Revit confirmed it.";
            var refusal = HeronRunPolicy.WithdrawnRefusal("rename-views", changesOff, false, state);
            Check(refusal.Contains("'rename-views' ran") && refusal.Contains(changesOff ?? "?")
                  && refusal.Contains(state),
                  "the refusal says the job RAN, why it was not kept, and what Revit confirmed");
            // IndexOf rather than Contains(string, StringComparison), which
            // .NET Framework does not have - this host builds for 2020 too.
            Check(refusal.IndexOf("nothing was sent", StringComparison.OrdinalIgnoreCase) < 0,
                  "and never the gate's 'nothing was sent' - something was");
            Check(refusal.Contains("Turn the switch back on"),
                  "a switch turned off says how to get it back");
            Check(HeronRunPolicy.WithdrawnRefusal("rename-views", stopped, true, state)
                      .Contains("stays stopped"),
                  "a stop says Heron stays stopped - nothing here turns it back on");
        }

        private static void Splice()
        {
            Console.WriteLine();
            Console.WriteLine("9. The record on the reply, and the audit line's number off it");

            var reply = Json.Error("run_guard", "refused");
            var quiet = For(HeronRisk.Modify);
            Check(quiet.Splice(reply) == reply, "nothing to say leaves the reply exactly as it was");

            var policy = For(HeronRisk.Modify);
            policy.Stopped(HeronRunPolicy.Act.Save, "Tower \"A\"", true);
            policy.Stopped(HeronRunPolicy.Act.Save, "Tower \"A\"", true);
            policy.Closed("Probe.rfa");
            var spliced = policy.Splice(reply);

            Check(Json.ReadString(spliced, "error") == "run_guard"
                  && Json.ReadString(spliced, "message") == "refused",
                  "the reply's own fields survive the splice");
            Check(Json.ReadLong(spliced, "guardBlocked") == 2,
                  "guardBlocked reads back as a NUMBER, the way the dispatcher reads its numbers");
            Check(spliced.Contains("\"guardStopped\": [") && spliced.Contains("\"act\": \"save\"")
                  && spliced.Contains("\"cancelled\": true") && spliced.Contains("\"times\": 2"),
                  "every stopped act is listed, by its one-word name, with whether it was cancelled");
            Check(spliced.Contains("Tower \\\"A\\\""),
                  "a title with a quote in it is escaped, not allowed to end the string");
            Check(spliced.Contains("\"guardClosed\": [{\"document\": \"Probe.rfa\"}]"),
                  "what was closed is listed");
            Check(spliced.EndsWith("}", StringComparison.Ordinal) && spliced.StartsWith("{", StringComparison.Ordinal),
                  "and it is still one object");

            Check(policy.Splice("not json") == "not json" && policy.Splice(null) == null,
                  "a reply that is not an object comes back untouched");
        }

        // ------------------------------------------------- 5b-1091 and 5b-1092
        //
        // ASKED BY NAME, NOT CALLED. The members these sections test did not
        // exist when they were written, and a host that cannot build reports
        // one error instead of every check that failed - so each is looked up
        // first, and its absence is one clean failure that lets the rest run.

        private const BindingFlags Members =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static HeronRunPolicy.Origin? OriginNamed(string name)
        {
            HeronRunPolicy.Origin origin;
            return Enum.TryParse(name, out origin) ? origin : (HeronRunPolicy.Origin?)null;
        }

        private static readonly HeronRunPolicy.Origin? Edited = OriginNamed("EditedByJob");

        private static void ReadsCode(HeronRunPolicy policy, string code)
        {
            var reads = typeof(HeronRunPolicy).GetMethod("Reads", Members);
            if (reads != null) reads.Invoke(policy, new object[] { code });
        }

        /// <summary>Null when the policy has no such question to ask.</summary>
        private static bool? FromEditFamily(HeronRunPolicy policy, bool isFamilyDocument)
        {
            var asks = typeof(HeronRunPolicy).GetMethod("FromEditFamily", Members);
            if (asks == null) return null;
            return (bool)asks.Invoke(policy, new object[] { isFamilyDocument });
        }

        private static HeronRunPolicy Knowing(HeronRisk risk, params string[] code)
        {
            var policy = For(risk);
            foreach (var piece in code) ReadsCode(policy, piece);
            return policy;
        }

        /// <summary>
        /// A fragment's own C#, read off disk - the repository is found by
        /// walking up from this host until its constitution is there. Null
        /// when it cannot be found, which the caller fails on.
        /// </summary>
        private static string FragmentCode(string id)
        {
            var folder = new DirectoryInfo(AppContext.BaseDirectory);
            while (folder != null && !File.Exists(Path.Combine(folder.FullName, "HERON_CONSTITUTION.md")))
                folder = folder.Parent;
            if (folder == null) return null;

            var path = Path.Combine(folder.FullName, "brain", "fragments", id, "impl", "any", "fragment.cs");
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        private const string EditsOnly =
            "var familyDocument = doc.EditFamily(family); familyDocument.Close(false);";

        private static void EditFamily()
        {
            Console.WriteLine();
            Console.WriteLine("10. A family the project already holds, opened with EditFamily - 5b-1091");

            // Revit 2024.3 raised DocumentCreated for doc.EditFamily, with no
            // title yet, and the guard refused it as a new document below
            // Admin - REPORT_FAMILY_TABLES_IN_PROJECT and OPEN_FAMILY_FOR_EDITING
            // came back run_guard. Both are PROVEN.
            Check(Edited.HasValue, "the policy has an origin for a family EditFamily opened");
            Check(typeof(HeronRunPolicy).GetMethod("Reads", Members) != null
                  && typeof(HeronRunPolicy).GetMethod("FromEditFamily", Members) != null,
                  "and reads the job's own code to tell it from a file the job started");

            var levels = new[]
            {
                HeronRisk.Read, HeronRisk.Analyze, HeronRisk.Suggest, HeronRisk.Execute,
                HeronRisk.Modify, HeronRisk.Publish, HeronRisk.Admin, (HeronRisk)7,
            };
            foreach (var risk in levels)
            {
                Check(FromEditFamily(Knowing(risk, EditsOnly), true) == true,
                      risk + ": a new family document under code that calls EditFamily and starts "
                      + "no document is the family EditFamily opened");
            }

            // EX10 MUST STAY REFUSED: NewFamilyDocument + SaveAs below Admin.
            var starters = new[]
            {
                "NewFamilyDocument", "NewProjectDocument", "NewProjectTemplateDocument",
                "OpenIFCDocument", "OpenBuildingComponentDocument",
            };
            foreach (var starter in starters)
            {
                var both = Knowing(HeronRisk.Modify, EditsOnly + " var made = app." + starter + "(path);");
                Check(FromEditFamily(both, true) == false,
                      "code that also names " + starter + " is held to the strict rule - EX10");
            }

            Check(FromEditFamily(Knowing(HeronRisk.Modify, EditsOnly, "var made = app.NewFamilyDocument(t);"), true)
                  == false,
                  "a starter in a setup step counts as much as one in the fragment");
            Check(FromEditFamily(Knowing(HeronRisk.Modify, "var made = app.NewFamilyDocument(t);"), true)
                  == false,
                  "code that never calls EditFamily is not given the benefit");
            Check(FromEditFamily(Knowing(HeronRisk.Modify), true) == false,
                  "and neither is a job whose code nobody handed the guard - it fails closed");
            Check(FromEditFamily(Knowing(HeronRisk.Modify, EditsOnly), false) == false,
                  "a new PROJECT document is never EditFamily's, whatever the code calls");
            Check(FromEditFamily(Knowing(HeronRisk.Modify, null, EditsOnly), true) == true,
                  "a missing piece of code is passed over, not a crash");

            if (Edited.HasValue)
            {
                var origin = Edited.Value;
                foreach (var act in Acts)
                {
                    foreach (var risk in new[] { HeronRisk.Read, HeronRisk.Analyze, HeronRisk.Suggest, (HeronRisk)7 })
                    {
                        Check(For(risk).Cancels(act, origin),
                              risk + ": " + act + " of a family EditFamily opened is cancelled - a read "
                              + "never writes the project's family out to a file");
                    }
                    foreach (var risk in new[] { HeronRisk.Execute, HeronRisk.Modify, HeronRisk.Publish, HeronRisk.Admin })
                    {
                        Check(!For(risk).Cancels(act, origin),
                              risk + ": " + act + " of a family EditFamily opened is the job's own - "
                              + "open-family-for-editing's SaveAs to its folder");
                    }
                }

                foreach (var risk in new[] { HeronRisk.Analyze, HeronRisk.Modify, HeronRisk.Admin })
                {
                    Check(!For(risk).IsElsewhere(origin, false),
                          risk + ": a change inside that family is not a change to another model");
                }
            }
            else
            {
                Check(false, "what a family EditFamily opened may save cannot be asked without its origin");
            }

            // THE TWO PROVEN FRAGMENTS THE REGRESSION BROKE, read off disk,
            // and the Admin one that must keep its own rule.
            foreach (var id in new[] { "report-family-tables-in-project", "open-family-for-editing" })
            {
                var code = FragmentCode(id);
                Check(code != null, id + "'s code is found in the repository");
                Check(code != null && FromEditFamily(Knowing(HeronRisk.Analyze, code), true) == true,
                      id + ": the family it opens with EditFamily is taken as EditFamily's");
            }
            var creates = FragmentCode("create-family-document");
            Check(creates != null && FromEditFamily(Knowing(HeronRisk.Admin, creates), true) == false,
                  "create-family-document's new family is still a file the job started");

            // A new document under a read is refused exactly as before when
            // the code does not explain it - the record has no new path round.
            var refused = Knowing(HeronRisk.Analyze, "var made = app.NewFamilyDocument(t);");
            Check(refused.Created("Family1") && refused.Refuses,
                  "a NewFamilyDocument under a read is still refused, and refuses the job");
        }

        private static readonly string[] StoreDependencies = { "Store dependencies" };

        private static void SavesOwnStep()
        {
            Console.WriteLine();
            Console.WriteLine("11. Revit's own commit inside a cancelled save is the save's - 5b-1092");

            // Revit 2024.3: a probe called Save() on the open model B; Revit
            // committed "Store dependencies" in B and the guard cancelled the
            // save. The reply was changed_other_model - "THAT CHANGE IS
            // COMMITTED ... HERON CANNOT UNDO IT" - where EX2 asks for run_guard.
            var state = "Heron rolled the whole job back and Revit confirmed it.";

            var before = For(HeronRisk.Modify);
            before.ChangedElsewhere("Heron Guard B", StoreDependencies);
            before.Stopped(HeronRunPolicy.Act.Save, "Heron Guard B", true);
            Check(before.Refuses && !before.ChangedOtherModel && before.ChangesElsewhere.Count == 0,
                  "Store dependencies just before a cancelled save of that model is the save's - "
                  + "run_guard, not changed_other_model");
            Check(before.BlockedCount == 1, "blocked counts the save once - " + before.BlockedCount);
            var said = before.Refusal(state, null);
            Check(said.Contains("Stopped before it happened: a save of 'Heron Guard B'"),
                  "the save is still said to be stopped");
            Check(!said.Contains("CANNOT UNDO") && !said.Contains("before saving it"),
                  "and the modeller is not sent to look for a change the job never made");
            Check(said.Contains("'Store dependencies'") && said.Contains("Revit's own step")
                  && said.Contains("undo list"),
                  "but Revit's own entry is named as Revit's, and where it shows - "
                  + "the model's undo list does hold it");

            var spliced = before.Splice(Json.Error("run_guard", "refused"));
            Check(spliced.Contains("\"revitCommitted\": \"'Store dependencies'\""),
                  "the reply's guardStopped line carries what Revit committed for the save");
            Check(Json.ReadLong(spliced, "guardBlocked") == 1, "and guardBlocked is 1");

            var after = For(HeronRisk.Modify);
            after.Stopped(HeronRunPolicy.Act.Save, "Heron Guard B", true);
            after.ChangedElsewhere("Heron Guard B", StoreDependencies);
            Check(after.Refuses && !after.ChangedOtherModel,
                  "the same when Revit commits it just after the cancel");

            foreach (var act in new[] { HeronRunPolicy.Act.SaveAs, HeronRunPolicy.Act.Sync })
            {
                var other = For(HeronRisk.Modify);
                other.ChangedElsewhere("Heron Guard B", StoreDependencies);
                other.Stopped(act, "Heron Guard B", true);
                Check(!other.ChangedOtherModel, "and for a cancelled " + act);
            }

            // EX9 AND THEN A SAVE: the job's own commit in B stands, and is
            // still said to stand.
            var ownThenSave = For(HeronRisk.Modify);
            ownThenSave.ChangedElsewhere("Heron Guard B", new[] { "zz probe line in B" });
            ownThenSave.ChangedElsewhere("Heron Guard B", StoreDependencies);
            ownThenSave.Stopped(HeronRunPolicy.Act.Save, "Heron Guard B", true);
            Check(ownThenSave.ChangedOtherModel && ownThenSave.ChangesElsewhere.Count == 1
                  && ownThenSave.ChangesElsewhere[0].Transactions == "'zz probe line in B'",
                  "a change the job committed in B before saving it is still changed_other_model, "
                  + "and only that change is named");

            // THE FAIL-OPEN CASE: a job's own commit straight before a save
            // that raised no bookkeeping - EX3's Save As raised none. Adjacency
            // alone would call it the save's and hide a change nobody can undo.
            var ownStraightBefore = For(HeronRisk.Modify);
            ownStraightBefore.ChangedElsewhere("Heron Guard B", new[] { "zz probe line in B" });
            ownStraightBefore.Stopped(HeronRunPolicy.Act.Save, "Heron Guard B", true);
            Check(ownStraightBefore.ChangedOtherModel,
                  "a commit with any other name, straight before the save, is never the save's");

            var mixed = For(HeronRisk.Modify);
            mixed.ChangedElsewhere("Heron Guard B", new[] { "Store dependencies", "zz probe line in B" });
            mixed.Stopped(HeronRunPolicy.Act.Save, "Heron Guard B", true);
            Check(mixed.ChangedOtherModel, "nor is one that carries Store dependencies beside another name");

            var otherModel = For(HeronRisk.Modify);
            otherModel.ChangedElsewhere("Heron Guard C", StoreDependencies);
            otherModel.Stopped(HeronRunPolicy.Act.Save, "Heron Guard B", true);
            Check(otherModel.ChangedOtherModel, "nor a Store dependencies in a different model");

            var notAdjacent = For(HeronRisk.Modify);
            notAdjacent.ChangedElsewhere("Heron Guard B", StoreDependencies);
            notAdjacent.Opened("Probe.rfa");
            notAdjacent.Stopped(HeronRunPolicy.Act.Save, "Heron Guard B", true);
            Check(notAdjacent.ChangedOtherModel, "nor one with anything else recorded between it and the save");

            var notCancelled = For(HeronRisk.Modify);
            notCancelled.ChangedElsewhere("Heron Guard B", StoreDependencies);
            notCancelled.Stopped(HeronRunPolicy.Act.Save, "Heron Guard B", false);
            Check(notCancelled.ChangedOtherModel, "nor beside a save Revit would not let be cancelled");

            var export = For(HeronRisk.Modify);
            export.ChangedElsewhere("Heron Guard B", StoreDependencies);
            export.Stopped(HeronRunPolicy.Act.Export, "Heron Guard B", true);
            Check(export.ChangedOtherModel, "nor beside an export, which is not a save");

            var twice = For(HeronRisk.Modify);
            twice.ChangedElsewhere("Heron Guard B", StoreDependencies);
            twice.Stopped(HeronRunPolicy.Act.Save, "Heron Guard B", true);
            twice.ChangedElsewhere("Heron Guard B", StoreDependencies);
            twice.Stopped(HeronRunPolicy.Act.Save, "Heron Guard B", true);
            Check(!twice.ChangedOtherModel && twice.StoppedActs.Count == 1
                  && twice.StoppedActs[0].Times == 2 && twice.BlockedCount == 2,
                  "two saves, each with its own Store dependencies, are one line, twice, and nothing else");
        }
    }
}
