// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
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
    /// to is the PC's to prove: docs/needs-checking/group-ej.md.
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
            AboveAdmin();
            Elsewhere();
            Record();
            Sentences();
            Withdrawn();
            Splice();

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
    }
}
