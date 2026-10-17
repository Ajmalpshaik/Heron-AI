// Heron-Agent:  none
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Heron.Core;

namespace Heron.Kernel.TestHost
{
    /// <summary>
    /// RUNS the kernel classes that nothing had ever run.
    ///
    /// FRAGMENT-ISSUES section 5b, row 7: of the ten kernel classes, four
    /// were named by no test at all - HeronStop, HeronUnits, HeronIdentity
    /// and HeronAtomicWrite. Row 7 calls HeronUnits the one that matters,
    /// because row 3 is a units-guard defect and the proof for it had to be
    /// written in a throwaway console project for want of anywhere to put it.
    ///
    /// THIS CALLS THE CODE. Every other suite in this repository that tests
    /// C# reads the source as TEXT and checks a claim about it, which is the
    /// right tool for "does this file still say what it must" and the wrong
    /// one for "what does this function return for NaN". Both kinds are
    /// wanted; only one of them was here.
    ///
    /// No Revit, no Windows, no model. Every check below runs on a plain
    /// Linux container - which is also the honest limit: this says nothing
    /// about what Revit does with a value that gets past a guard.
    ///
    /// WHAT IT DELIBERATELY DOES NOT TOUCH:
    ///
    ///   HeronIdentity.InstallationId  reads, and on a first run WRITES, a
    ///     file in the user's own DATA folder. A test that creates something
    ///     in %APPDATA%\Heron on the owner's PC has changed his installation
    ///     to check it. The two functions that mint are covered instead.
    ///
    ///   HeronConfig.Load / Save       read and write the real heron.config.
    ///     GetBool is exercised through a config built in memory, which is
    ///     the part row 8 is about.
    ///
    ///   HeronAudit, HeronLease, HeronOperationRegistry
    ///     already have suites that name them. Row 7 counted them; they are
    ///     not this host's job.
    ///
    /// HeronPermissions WAS on that list, and came off it on 2026-09-28 with
    /// D-106. Its suites read it as TEXT, which was enough while it had one
    /// switch; with three, and a rule about which combinations let which
    /// level through, the question is what Allows() RETURNS for each of the
    /// eight - and only running it answers that. Section 8 does, through the
    /// overloads that take a config built in memory, so the owner's own
    /// heron.config is never read or written to check it.
    ///
    /// HeronOperationRegistry is RUN here too since 2026-10-17, in section 9,
    /// for one question its text suites cannot answer: whether the show door
    /// and the temporary isolate really pass the gate with every switch off.
    /// That is the registry's level and section 8's rule together, and only
    /// running both says it.
    /// </summary>
    internal static class Program
    {
        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private static void Check(bool condition, string what)
        {
            _checks++;
            Console.WriteLine("  {0}  {1}", condition ? "ok  " : "FAIL", what);
            if (!condition) Failures.Add(what);
        }

        private static int Main()
        {
            Console.WriteLine("Heron kernel, run rather than read - no Revit, no Windows.");
            Console.WriteLine();

            Units();
            Paths();
            Runs();
            Identity();
            Stop();
            AtomicWrite();
            Append();
            Config();
            Permissions();
            ShowDoor();
            SourceScan();

            Console.WriteLine();
            if (Failures.Count > 0)
            {
                Console.WriteLine("FAILED  {0} of {1} check(s)", Failures.Count, _checks);
                foreach (var failure in Failures) Console.WriteLine("  - {0}", failure);
                return 1;
            }
            Console.WriteLine("PASS    {0} checks, and four kernel classes are no longer", _checks);
            Console.WriteLine("        named by nothing.");
            return 0;
        }

        // ------------------------------------------------------------------
        // HeronUnits - row 7 calls this the one that matters, because row 3
        // is about a caller that wrote the bound out by hand instead of
        // asking this.
        // ------------------------------------------------------------------
        private static void Units()
        {
            Console.WriteLine("1. HeronUnits - the guard two callers used to hand-roll");

            Check(!HeronUnits.IsUsableMillimetres(double.NaN),
                  "NaN is refused");
            Check(!HeronUnits.IsUsableMillimetres(double.PositiveInfinity)
                  && !HeronUnits.IsUsableMillimetres(double.NegativeInfinity),
                  "both infinities are refused");
            Check(HeronUnits.IsUsableMillimetres(0.0),
                  "zero is allowed through - a legitimate number and a "
                  + "meaningless move, refused where the operation can say why");
            Check(HeronUnits.IsUsableMillimetres(HeronUnits.MaxMillimetres)
                  && HeronUnits.IsUsableMillimetres(-HeronUnits.MaxMillimetres),
                  "the bound itself is usable, at both signs");
            Check(!HeronUnits.IsUsableMillimetres(HeronUnits.MaxMillimetres * 1.000001)
                  && !HeronUnits.IsUsableMillimetres(-HeronUnits.MaxMillimetres * 1.000001),
                  "past the bound is refused, at both signs");

            // THE SHAPE OF ROW 3, DEMONSTRATED RATHER THAN DESCRIBED. Two
            // sites tested `mm > Max || mm < -Max` beside the call instead of
            // asking the guard. Every comparison against NaN is false, so
            // that pair waves NaN through - and the next line converted it
            // and handed it to Revit. This is here so that anybody tempted to
            // write the bound out again can watch it fail.
            var handRolled = double.NaN > HeronUnits.MaxMillimetres
                             || double.NaN < -HeronUnits.MaxMillimetres;
            Check(handRolled == false,
                  "the hand-rolled pair `mm > Max || mm < -Max` says NaN is "
                  + "FINE - which is why the bound lives in one place");

            Check(double.IsNaN(HeronUnits.MillimetresToFeet(double.NaN)),
                  "and nothing downstream rescues it: MillimetresToFeet(NaN) "
                  + "is NaN");

            // The conversion itself, because a guard on a wrong number is not
            // worth much. 304.8 mm is exactly one foot.
            var foot = HeronUnits.MillimetresToFeet(304.8);
            Check(Math.Abs(foot - 1.0) < 1e-9,
                  "304.8 mm is one foot (" + foot.ToString("R", CultureInfo.InvariantCulture) + ")");
        }

        // ------------------------------------------------------------------
        // HeronPaths.IsSafeToDelete - row 1. The sibling-folder case had
        // never been run by anything.
        // ------------------------------------------------------------------
        private static void Paths()
        {
            Console.WriteLine();
            Console.WriteLine("2. HeronPaths.IsSafeToDelete - a folder NAMED like Heron's is not inside it");

            var derived = Path.GetFullPath(HeronPaths.Derived);
            var data = Path.GetFullPath(HeronPaths.Data);

            Check(HeronPaths.IsSafeToDelete(Path.Combine(derived, "logs")),
                  "something inside DERIVED is safe to delete");
            Check(HeronPaths.IsSafeToDelete(derived),
                  "DERIVED itself is safe to delete - docs/06 says the whole "
                  + "folder is rebuildable");
            Check(!HeronPaths.IsSafeToDelete(data)
                  && !HeronPaths.IsSafeToDelete(Path.Combine(data, "audit")),
                  "DATA and everything under it is the user's, and is refused");

            // ROW 1, VERBATIM. %LOCALAPPDATA%\HeronBackup\anything used to
            // start with %LOCALAPPDATA%\Heron as plain text and read as SAFE.
            foreach (var sibling in new[] { "Backup", "-old", ".bak", " backup" })
            {
                var stranger = derived + sibling;
                Check(!HeronPaths.IsSafeToDelete(stranger)
                      && !HeronPaths.IsSafeToDelete(Path.Combine(stranger, "anything")),
                      "'" + Path.GetFileName(stranger) + "' is a different folder, and is refused");
            }

            Check(!HeronPaths.IsSafeToDelete(null) && !HeronPaths.IsSafeToDelete(""),
                  "nothing is not a path, and is refused");
            var parent = Path.GetDirectoryName(derived);
            Check(parent != null && !HeronPaths.IsSafeToDelete(parent),
                  "the folder DERIVED sits in is refused - deleting it would "
                  + "take everything else with it");
            Check(!HeronPaths.IsSafeToDelete(Path.Combine(derived, "..", "..")),
                  "and a path that climbs back out with .. is refused, "
                  + "because the comparison is made after it is resolved");
        }

        // ------------------------------------------------------------------
        // HeronPaths.Runs and HeronRuns - the run records a fragment write
        // leaves for heron_run_record (FRAGMENT-ISSUES 5b-407). DERIVED, so a
        // cleanup may take them; never DATA, which no cleanup may reach.
        //
        // THE ONLY CHECKS HERE THAT WRITE UNDER DERIVED, and they write into
        // a scratch folder of their own inside it and delete it after. A
        // prune is guarded by IsSafeToDelete, and that guard answers yes only
        // under DERIVED - so a prune can only be shown deleting anything from
        // there. DERIVED is the folder docs/06 calls safe to delete at any
        // moment; nothing here touches DATA or the real runs folder's files.
        // ------------------------------------------------------------------
        private static void Runs()
        {
            Console.WriteLine();
            Console.WriteLine("2b. HeronPaths.Runs and HeronRuns - the run records, DERIVED and pruned");

            var derived = Path.GetFullPath(HeronPaths.Derived);
            var data = Path.GetFullPath(HeronPaths.Data);
            var runs = Path.GetFullPath(HeronPaths.Runs);

            Check(runs == Path.Combine(derived, "runs"),
                  "Runs is the `runs` folder under DERIVED - the one the bridge client "
                  + "names as RUNS_DIR (" + runs + ")");
            Check(!runs.StartsWith(data + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                  && runs != data,
                  "and is not under DATA, where the audit trail it is linked to lives");
            Check(Directory.Exists(runs), "the path manager makes it, as it makes the others");
            Check(HeronPaths.IsSafeToDelete(runs)
                  && HeronPaths.IsSafeToDelete(Path.Combine(runs, "wf-20261009-133432-0042-a1b2.json")),
                  "IsSafeToDelete says yes to it and to a record in it - a prune is allowed");

            Check(HeronRuns.IsRunId("wf-20261009-133432-0042-a1b2")
                  && HeronRuns.IsRunId(HeronIdentity.NewWorkflowId()),
                  "a workflow id, as HeronIdentity mints one, is a run id");
            var refused = new[] { null, "", "wf-", "../wf-1", "wf-1/../../x", "wf-1.json",
                                  "C:wf-1", "wf 1", "run-1", "wf-1\\x" };
            var allRefused = true;
            foreach (var id in refused)
            {
                if (HeronRuns.IsRunId(id) || HeronRuns.PathFor(runs, id) != null) allRefused = false;
            }
            Check(allRefused,
                  "and nothing that could be a path out of the folder is one - a separator, a "
                  + "dot, a drive, a space, another prefix");

            var scratch = Path.Combine(derived, "kernel-runs-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            try
            {
                const string id = "wf-20261009-133432-0042-a1b2";
                Check(HeronRuns.Write(scratch, id, "{\"record\": 1}"),
                      "a record is written, folder and all");
                var path = HeronRuns.PathFor(scratch, id);
                Check(path != null && File.ReadAllText(path) == "{\"record\": 1}"
                      && !File.Exists(path + ".tmp"),
                      "whole, through HeronAtomicWrite, with no .tmp left behind");
                Check(!HeronRuns.Write(scratch, "../escape", "{}")
                      && !File.Exists(Path.Combine(derived, "escape.json")),
                      "an id that is not a run id writes nothing and says so");
                Check(!HeronRuns.Write(scratch, "wf-empty", ""),
                      "and an empty record is not written at all");

                var now = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);
                var old = HeronRuns.PathFor(scratch, "wf-20260901-000000-0001-aaaa");
                var fresh = HeronRuns.PathFor(scratch, "wf-20261016-000000-0002-bbbb");
                var stranger = Path.Combine(scratch, "notes.json");
                var oddName = Path.Combine(scratch, "wf-..json");
                foreach (var one in new[] { old, fresh, stranger, oddName })
                {
                    File.WriteAllText(one, "{}");
                }
                File.SetLastWriteTimeUtc(old, now.AddDays(-30));
                File.SetLastWriteTimeUtc(fresh, now.AddDays(-1));
                File.SetLastWriteTimeUtc(stranger, now.AddDays(-30));
                File.SetLastWriteTimeUtc(oddName, now.AddDays(-30));
                File.SetLastWriteTimeUtc(path, now.AddDays(-30));

                Check(HeronRuns.Prune(scratch, 0, now) == 0 && File.Exists(old),
                      "retainDays below 1 prunes nothing - zero must not mean keep nothing");
                var deleted = HeronRuns.Prune(scratch, 14, now);
                Check(deleted == 2 && !File.Exists(old) && !File.Exists(path),
                      "a prune at 14 days deletes the two records older than that - found "
                      + deleted);
                Check(File.Exists(fresh), "and keeps the one written yesterday");
                Check(File.Exists(stranger) && File.Exists(oddName),
                      "and never touches a file that is not a record, however old");

                // OUTSIDE DERIVED, A PRUNE DELETES NOTHING - the guard, not the
                // name, decides. The temp folder stands in for anywhere that is
                // not Heron's own: DATA is not written to check it.
                var outside = Path.Combine(Path.GetTempPath(),
                                           "heron-runs-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                try
                {
                    Check(HeronRuns.Write(outside, id, "{}"), "a record can be written anywhere it is told");
                    File.SetLastWriteTimeUtc(HeronRuns.PathFor(outside, id), now.AddDays(-30));
                    Check(HeronRuns.Prune(outside, 14, now) == 0
                          && File.Exists(HeronRuns.PathFor(outside, id)),
                          "but a prune outside DERIVED deletes nothing, because IsSafeToDelete says no");
                }
                finally
                {
                    try { Directory.Delete(outside, true); } catch (IOException) { }
                }
            }
            finally
            {
                try { Directory.Delete(scratch, true); } catch (IOException) { }
            }
        }

        // ------------------------------------------------------------------
        // HeronIdentity - row 6. One minter, and the property the format is
        // for.
        // ------------------------------------------------------------------
        private static void Identity()
        {
            Console.WriteLine();
            Console.WriteLine("3. HeronIdentity - the workflow id, and what its time prefix is FOR");

            var shape = new Regex(@"^wf-\d{8}-\d{6}-\d{4}-[0-9a-f]{4}$");
            var minted = new List<string>();
            for (var i = 0; i < 5; i++) minted.Add(HeronIdentity.NewWorkflowId());

            Check(minted.TrueForAll(id => shape.IsMatch(id)),
                  "every id has the documented shape wf-YYYYMMDD-HHMMSS-NNNN-xxxx "
                  + "(" + minted[0] + ")");
            Check(new List<string>(new HashSet<string>(minted)).Count == minted.Count,
                  "five in a row are five different ids");

            // THE ONE THING THE TIME PREFIX BUYS, which is why row 6 deleted
            // the other minter rather than keeping both: audit-YYYYMM.jsonl
            // can be sorted by workflow id and come out in the order the
            // requests arrived.
            var sorted = new List<string>(minted);
            sorted.Sort(StringComparer.Ordinal);
            Check(sorted.Equals(minted) || string.Join(",", sorted.ToArray())
                      == string.Join(",", minted.ToArray()),
                  "sorting them as text puts them back in the order they were "
                  + "minted - the property a bare GUID slice cannot give");

            var one = HeronIdentity.ForKnowledge("fragment", "count-elements");
            var two = HeronIdentity.ForKnowledge("fragment", "count-elements");
            Check(one == two && !string.IsNullOrEmpty(one),
                  "a knowledge id is derived from identity, so the same "
                  + "fragment gets the same id twice (" + one + ")");
            Check(one != HeronIdentity.ForKnowledge("fragment", "count-ducts"),
                  "and a different one does not");
        }

        // ------------------------------------------------------------------
        // HeronStop - row 2. The emergency stop, named by no test.
        // ------------------------------------------------------------------
        private static void Stop()
        {
            Console.WriteLine();
            Console.WriteLine("4. HeronStop - set by a person, cleared by a person");

            Check(!HeronStop.IsStopped, "starts clear");

            // D-129. The run clock reads the COUNT, not the flag, so a stop
            // asked while already stopped still reaches a running fragment,
            // and a read begun while stopped is not tripped by the old one.
            var asked = HeronStop.Requests;
            Check(HeronStop.Stop(), "Stop() reports that THIS call changed it");
            Check(HeronStop.Requests == asked + 1, "and the stop count moved by one");
            Check(!HeronStop.Stop(), "and a second Stop() reports that it did not");
            Check(HeronStop.Requests == asked + 2,
                  "while the count moves again - a second stop is still a stop asked for");
            Check(HeronStop.IsStopped, "stopped");

            // ROW 2. The message used to send the reader to "Heron AI >
            // Emergency Stop", a ribbon button removed on 2026-09-06 (D-46).
            // It names no button now, because the only thing that calls
            // Resume() is EmergencyStopCommand and that command has no ribbon
            // entry either - naming the Heron button instead would have been
            // the same defect with a newer name.
            Check(HeronStop.Message.IndexOf("Emergency Stop", StringComparison.OrdinalIgnoreCase) < 0,
                  "the refusal does not send anybody to a button that was removed");
            Check(HeronStop.Message.IndexOf("Ctrl+Z", StringComparison.Ordinal) >= 0,
                  "and it still says the one thing that helps: Ctrl+Z");
            Check(HeronStop.Message.IndexOf("Nothing was sent to Revit", StringComparison.Ordinal) >= 0,
                  "and says plainly that nothing was sent");

            // THE KILL SWITCH WAS BUILT ON 2026-10-17, SO THE WAY OUT IS
            // NAMED (D-129). HeronStop's own rule: whoever builds what sets
            // the flag owns this sentence, the same day. A restart is the one
            // thing that clears it - nothing over the bridge resumes.
            Check(HeronStop.Message.IndexOf("until Revit is restarted", StringComparison.Ordinal) >= 0,
                  "and it names the one way out there is: restarting Revit");
            Check(HeronStop.Message.EndsWith(HeronStop.StaysStopped, StringComparison.Ordinal),
                  "in the same words every other answer about a stop uses");
            Check(HeronStop.StaysStopped.IndexOf("Nothing was sent", StringComparison.OrdinalIgnoreCase) < 0,
                  "and that shared half claims nothing about this request - a job the run "
                  + "clock stopped part way DID reach Revit");

            Check(HeronStop.Resume(), "Resume() reports that THIS call changed it");
            Check(HeronStop.Requests == asked + 2, "and resuming leaves the stop count alone");
            Check(!HeronStop.Resume(), "and a second Resume() reports that it did not");
            Check(!HeronStop.IsStopped, "clear again, and left that way for whatever runs next");
        }

        // ------------------------------------------------------------------
        // HeronAtomicWrite - the class written BECAUSE two callers lost the
        // user's data, and named by no test.
        // ------------------------------------------------------------------
        private static void AtomicWrite()
        {
            Console.WriteLine();
            Console.WriteLine("5. HeronAtomicWrite - completely, or not at all");

            var folder = Path.Combine(Path.GetTempPath(),
                                      "heron-kernel-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            var file = Path.Combine(folder, "deeper", "thing.config");
            try
            {
                HeronAtomicWrite.WriteAllText(file, "first = 1\n");
                Check(File.Exists(file), "the first write creates the file");
                Check(File.ReadAllText(file) == "first = 1\n", "with the contents given");
                Check(Directory.Exists(Path.GetDirectoryName(file)),
                      "and the folder under it, which did not exist");

                HeronAtomicWrite.WriteAllText(file, "second = 2\n");
                Check(File.ReadAllText(file) == "second = 2\n", "the second write replaces it");
                Check(!File.Exists(file + ".tmp"),
                      "and leaves no .tmp behind - the backup argument is null "
                      + "so no third file survives");

                // NO BYTE ORDER MARK. Its own docstring is the claim: every
                // file written this way is read back by Python as well as by
                // C#, and a BOM turns the first key of a config file into one
                // nothing matches.
                var bytes = File.ReadAllBytes(file);
                Check(!(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF),
                      "UTF-8 with no byte order mark, so Python reads the first key");
            }
            finally
            {
                try { Directory.Delete(folder, true); } catch (IOException) { }
            }
        }

        // ------------------------------------------------------------------
        // HeronAppend - rows 5 and 19. The defect they record is a SECOND
        // PROCESS, which cannot be spawned from here - but the thing that
        // made the second process fatal can be reproduced exactly: a file
        // held open the way File.AppendAllText holds it.
        // ------------------------------------------------------------------
        private static void Append()
        {
            Console.WriteLine();
            Console.WriteLine("6. HeronAppend - a second writer is not a lost line");

            var folder = Path.Combine(Path.GetTempPath(),
                                      "heron-append-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, "audit-209901.jsonl");
            try
            {
                Check(HeronAppend.Line(file, "{\"first\": 1}"),
                      "the first line is written, and it says so");
                Check(File.ReadAllText(file).Trim() == "{\"first\": 1}",
                      "with the contents given, and a newline after it");

                // THE DEFECT, REPRODUCED - ON WINDOWS. File.AppendAllText
                // opens with FileShare.Read, so while one writer holds the
                // file NOBODY else may write, which is what a second Revit
                // is.
                //
                // AND IT IS A WINDOWS FACT, WHICH THIS FOUND BY TRYING IT.
                // Rows 5 and 19 measured the sharing violation on .NET, and
                // the measurement stands - on Windows, where FileShare is
                // enforced by the operating system. On Linux the same code
                // appends happily, because .NET's sharing flags are not
                // enforced between processes there. Revit is Windows-only so
                // the defect was real where it matters; saying WHERE it was
                // checked is the difference between a test and a claim.
                var onWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
                using (var theOldWay = new FileStream(file, FileMode.Append, FileAccess.Write,
                                                      FileShare.Read))
                {
                    var refused = false;
                    try { File.AppendAllText(file, "{\"lost\": true}" + Environment.NewLine); }
                    catch (IOException) { refused = true; }
                    if (onWindows)
                    {
                        Check(refused,
                              "a FileShare.Read holder refuses the next writer outright - "
                              + "which is the line that used to vanish");
                    }
                    else
                    {
                        Console.WriteLine("  n/a   the FileShare.Read refusal is a Windows "
                                          + "behaviour and cannot be reproduced here (it "
                                          + (refused ? "refused" : "did not refuse") + ")");
                    }
                    GC.KeepAlive(theOldWay);
                }

                // AND THE REPAIR. Held the new way, a second writer gets in.
                using (var other = new FileStream(file, FileMode.Append, FileAccess.Write,
                                                  FileShare.ReadWrite))
                {
                    Check(HeronAppend.Line(file, "{\"second\": 2}"),
                          "held open with FileShare.ReadWrite, the next writer gets in");
                    GC.KeepAlive(other);
                }

                // AND IT SAYS SO WHEN IT CANNOT. An honest false is what lets
                // HeronAudit report the hole rather than leaving one.
                Check(!HeronAppend.Line(Path.Combine(file, "under-a-file.log"), "nowhere"),
                      "a path that cannot be written answers false rather than throwing");
                Check(!HeronAppend.Line(null, "nowhere") && !HeronAppend.Line("", "nowhere"),
                      "and so does no path at all");

                // AND IT DOES NOT SLEEP THROUGH A FAILURE THAT CANNOT CLEAR.
                //
                // `Write` told the two apart in its comments from the first
                // day - "busy: worth another attempt" against "permissions:
                // retrying changes nothing" - and returned the same bare
                // `false` for both, so the retry loop spent 20 + 40 + 60 + 80
                // ms on a path that was never going to work, per line. A path
                // holding a NUL character is refused by the framework before
                // any I/O happens, so it is the one of those three this
                // machine can produce; the timing is what distinguishes "it
                // gave up" from "it tried five times".
                var refusedAt = DateTime.UtcNow;
                var gaveUp = HeronAppend.Line("bad\0path.log", "nowhere");
                var spent = (DateTime.UtcNow - refusedAt).TotalMilliseconds;
                Check(!gaveUp, "a path the framework refuses outright answers false");
                Check(spent < 100,
                      "and returns in " + (int)spent + " ms rather than sleeping "
                      + "through four backoffs for a failure that cannot clear");

                // NO TORN LINES. Ten threads, a hundred lines each: every one
                // of the thousand is whole and on its own line. The mutex is
                // what makes taking turns different from sharing the file.
                var beforeThreads = File.ReadAllLines(file).Length;
                var threads = new List<Thread>();
                for (var t = 0; t < 10; t++)
                {
                    var mine = t;
                    var thread = new Thread(delegate()
                    {
                        for (var i = 0; i < 100; i++)
                            HeronAppend.Line(file, "thread-" + mine + "-line-" + i);
                    });
                    threads.Add(thread);
                    thread.Start();
                }
                foreach (var thread in threads) thread.Join();

                var lines = File.ReadAllLines(file);
                var whole = 0;
                foreach (var line in lines)
                    if (line.StartsWith("thread-", StringComparison.Ordinal)
                        && line.Split('-').Length == 4) whole++;
                Check(whole == 1000,
                      "1000 lines from 10 threads arrive whole and separate (" + whole + ")");
                Check(lines.Length == beforeThreads + 1000,
                      "and nothing else was lost or duplicated - " + (beforeThreads + 1000)
                      + " lines in all (" + lines.Length + ")");
            }
            finally
            {
                try { Directory.Delete(folder, true); } catch (IOException) { }
            }
        }

        // ------------------------------------------------------------------
        // HeronConfig.GetBool - row 8.
        // ------------------------------------------------------------------
        private static void Config()
        {
            Console.WriteLine();
            Console.WriteLine("7. HeronConfig.GetBool - a value nobody can parse is a value nobody stated");

            // ui.activityBanner is the key that matters: it is the only one
            // read with a TRUE fallback, because a Revit frozen with no
            // explanation reads as a crash.
            const string Key = "ui.activityBanner";

            foreach (var yes in new[] { "true", "TRUE", "1", "yes", "on", " true " })
            {
                var config = new HeronConfig();
                config.Set(Key, yes);
                Check(config.GetBool(Key, false), "'" + yes + "' is true");
            }

            foreach (var no in new[] { "false", "FALSE", "0", "no", "off", " false " })
            {
                var config = new HeronConfig();
                config.Set(Key, no);
                Check(!config.GetBool(Key, true), "'" + no + "' is false");
            }

            // ROW 8. Each of these used to resolve to FALSE and silently turn
            // the banner off, with the word the user typed still sitting in
            // their own config file looking accepted.
            foreach (var nonsense in new[] { "enabled", "maybe", "oui", "2", "tru" })
            {
                var onByDefault = new HeronConfig();
                onByDefault.Set(Key, nonsense);
                Check(onByDefault.GetBool(Key, true),
                      "'" + nonsense + "' is not a word this understands, so the "
                      + "fallback stands (true)");

                var offByDefault = new HeronConfig();
                offByDefault.Set(Key, nonsense);
                Check(!offByDefault.GetBool(Key, false),
                      "'" + nonsense + "' with a false fallback stays false - the "
                      + "fallback is honoured in BOTH directions");
            }

            var empty = new HeronConfig();
            empty.Set(Key, "");
            Check(empty.GetBool(Key, true) && !empty.GetBool(Key, false),
                  "an empty value is the fallback, which it always was");

            // ITS SIBLING TEN LINES UP, which is where the rule came from.
            var ints = new HeronConfig();
            ints.Set("bridge.idleReleaseMinutes", "not a number");
            Check(ints.GetInt("bridge.idleReleaseMinutes", 42) == 42,
                  "GetInt already did this, and is what GetBool now matches");
        }

        // ------------------------------------------------------------------
        // HeronPermissions - D-106's three switches, every combination.
        // ------------------------------------------------------------------

        /// <summary>A stand-in for the settings path, so no real folder is asked for.</summary>
        private const string FakePath = "(test) heron.config";

        private static HeronConfig Switches(bool changes, bool admin, bool publish)
        {
            var config = new HeronConfig();
            config.Set(HeronPermissions.WriteEnabledKey, changes ? "true" : "false");
            config.Set(HeronPermissions.AdminEnabledKey, admin ? "true" : "false");
            config.Set(HeronPermissions.PublishEnabledKey, publish ? "true" : "false");
            return config;
        }

        private static string Said(bool changes, bool admin, bool publish)
        {
            return "Changes " + (changes ? "ON" : "off") + ", Admin " + (admin ? "ON" : "off")
                   + ", Publish " + (publish ? "ON" : "off");
        }

        private static void Permissions()
        {
            Console.WriteLine();
            Console.WriteLine("8. HeronPermissions - the three switches, every combination (D-106)");

            // THE TWO NEW KEYS ARE DECLARED. Set() throws for a key Defaults
            // does not hold, and a key Load() would silently ignore is the
            // failure write.enabled once had: set in the file, never honoured.
            var declared = new HeronConfig();
            var threw = false;
            try
            {
                declared.Set(HeronPermissions.AdminEnabledKey, "true");
                declared.Set(HeronPermissions.PublishEnabledKey, "true");
            }
            catch (ArgumentException) { threw = true; }
            Check(!threw, "admin.enabled and publish.enabled are declared settings - Set() "
                          + "accepts both, so Load() would honour them in a file");

            var fresh = new HeronConfig();
            Check(!fresh.GetBool(HeronPermissions.AdminEnabledKey, true)
                  && !fresh.GetBool(HeronPermissions.PublishEnabledKey, true),
                  "and both DEFAULT to false - read with a true fallback, the declared "
                  + "default still answers false");

            var levels = new[]
            {
                HeronRisk.Read, HeronRisk.Analyze, HeronRisk.Suggest, HeronRisk.Execute,
                HeronRisk.Modify, HeronRisk.Publish, HeronRisk.Admin,
            };

            var mismatches = 0;
            foreach (var changes in new[] { false, true })
            foreach (var admin in new[] { false, true })
            foreach (var publish in new[] { false, true })
            {
                var config = Switches(changes, admin, publish);
                foreach (var level in levels)
                {
                    // THE RULE, written once here from D-106 rather than read
                    // back out of the code under test.
                    bool expected;
                    if (level <= HeronRisk.Execute) expected = true;
                    else if (level == HeronRisk.Modify) expected = changes;
                    else if (level == HeronRisk.Publish) expected = changes && publish;
                    else expected = changes && admin;

                    var allowed = HeronPermissions.Allows(level, config);
                    var why = HeronPermissions.Explain(level, config, FakePath);
                    var by = HeronPermissions.RefusedBy(level, config);

                    var agrees = allowed == expected
                                 && (why == null) == allowed
                                 && (by == null) == allowed;
                    if (!agrees)
                    {
                        mismatches++;
                        Check(false, Said(changes, admin, publish) + ": " + level
                                     + " should be " + (expected ? "allowed" : "refused")
                                     + " - Allows " + allowed + ", Explain "
                                     + (why == null ? "null" : "a refusal")
                                     + ", RefusedBy " + (by ?? "null"));
                    }
                }
            }
            Check(mismatches == 0,
                  "all 8 combinations x 7 levels agree with D-106: reads always, Modify "
                  + "with Changes, Publish with Changes AND Publish, Admin with Changes AND "
                  + "Admin - and Explain and RefusedBy agree with Allows every time");

            // EACH SWITCH OFF -> REFUSED, BY NAME.
            var adminOff = Switches(true, false, true);
            var adminSaid = HeronPermissions.Explain(HeronRisk.Admin, adminOff, FakePath) ?? "";
            Check(HeronPermissions.RefusedBy(HeronRisk.Admin, adminOff)
                  == HeronPermissions.AdminEnabledKey,
                  "Admin off, Changes on: refused by admin.enabled");
            Check(adminSaid.Contains("turn on Admin in Revit's Heron ribbon")
                  && adminSaid.Contains("Heron > AI Bridge > Admin"),
                  "and the refusal names the switch and where it is: \"" + adminSaid + "\"");
            Check(adminSaid.Contains("nothing was sent to Revit"),
                  "and says nothing was sent to Revit");
            Check(adminSaid.Contains(HeronPermissions.AdminEnabledKey + " = true")
                  && adminSaid.Contains(FakePath),
                  "and names the setting and the file it lives in");

            var publishOff = Switches(true, true, false);
            var publishSaid = HeronPermissions.Explain(HeronRisk.Publish, publishOff, FakePath) ?? "";
            Check(HeronPermissions.RefusedBy(HeronRisk.Publish, publishOff)
                  == HeronPermissions.PublishEnabledKey,
                  "Publish off, Changes on: refused by publish.enabled");
            Check(publishSaid.Contains("turn on Publish in Revit's Heron ribbon")
                  && publishSaid.Contains("Heron > AI Bridge > Publish")
                  && publishSaid.Contains("nothing was sent to Revit"),
                  "and the refusal names Publish and where it is: \"" + publishSaid + "\"");

            // ON -> ALLOWED.
            var allOn = Switches(true, true, true);
            Check(HeronPermissions.Allows(HeronRisk.Admin, allOn)
                  && HeronPermissions.Explain(HeronRisk.Admin, allOn, FakePath) == null,
                  "Admin on with Changes on: allowed, and nothing to explain");
            Check(HeronPermissions.Allows(HeronRisk.Publish, allOn)
                  && HeronPermissions.Explain(HeronRisk.Publish, allOn, FakePath) == null,
                  "Publish on with Changes on: allowed, and nothing to explain");

            // THE TWO ARE INDEPENDENT OF EACH OTHER.
            var onlyAdmin = Switches(true, true, false);
            Check(HeronPermissions.Allows(HeronRisk.Admin, onlyAdmin)
                  && !HeronPermissions.Allows(HeronRisk.Publish, onlyAdmin),
                  "Admin on, Publish off: Admin allowed, Publish still refused");
            var onlyPublish = Switches(true, false, true);
            Check(HeronPermissions.Allows(HeronRisk.Publish, onlyPublish)
                  && !HeronPermissions.Allows(HeronRisk.Admin, onlyPublish),
                  "Publish on, Admin off: Publish allowed, Admin still refused");
            Check(HeronPermissions.Allows(HeronRisk.Modify, Switches(true, false, false)),
                  "and Modify needs neither - Changes alone, exactly as before D-106");

            // ADMIN OR PUBLISH ON WITH CHANGES OFF -> REFUSED, AND CHANGES IS NAMED.
            var changesOff = Switches(false, true, true);
            Check(!HeronPermissions.Allows(HeronRisk.Admin, changesOff)
                  && !HeronPermissions.Allows(HeronRisk.Publish, changesOff)
                  && !HeronPermissions.Allows(HeronRisk.Modify, changesOff),
                  "Admin and Publish ON with Changes off: Admin, Publish and Modify all refused");
            Check(HeronPermissions.RefusedBy(HeronRisk.Admin, changesOff)
                  == HeronPermissions.WriteEnabledKey
                  && HeronPermissions.RefusedBy(HeronRisk.Publish, changesOff)
                  == HeronPermissions.WriteEnabledKey,
                  "and what refused them is Changes - the switch that is actually off");
            var needsChanges = HeronPermissions.Explain(HeronRisk.Admin, changesOff, FakePath) ?? "";
            Check(needsChanges.Contains("Admin is on, but Changes is off")
                  && needsChanges.Contains("turn on Changes in Revit's Heron ribbon")
                  && needsChanges.Contains("nothing was sent to Revit"),
                  "and the refusal says so in those words: \"" + needsChanges + "\"");

            // BOTH OFF: THE LEVEL'S OWN SWITCH IS NAMED, AND CHANGES WITH IT.
            var bothOff = Switches(false, false, false);
            var bothSaid = HeronPermissions.Explain(HeronRisk.Admin, bothOff, FakePath) ?? "";
            Check(HeronPermissions.RefusedBy(HeronRisk.Admin, bothOff)
                  == HeronPermissions.AdminEnabledKey,
                  "everything off: an Admin request is refused by Admin, the switch it was about");
            Check(bothSaid.Contains("turn on both Changes and Admin")
                  && bothSaid.Contains(HeronPermissions.WriteEnabledKey + " = true")
                  && bothSaid.Contains(HeronPermissions.AdminEnabledKey + " = true"),
                  "and one refusal names BOTH switches, so one read gets both right");

            // MODIFY'S WORDS ARE UNCHANGED BY D-106.
            var modifySaid = HeronPermissions.Explain(HeronRisk.Modify, bothOff, FakePath) ?? "";
            Check(modifySaid.StartsWith("Heron's ability to change the model is switched off",
                                        StringComparison.Ordinal)
                  && HeronPermissions.RefusedBy(HeronRisk.Modify, bothOff)
                  == HeronPermissions.WriteEnabledKey,
                  "Modify with Changes off is refused in the words it always was");

            // FAILS CLOSED.
            Check(!HeronPermissions.Allows(HeronRisk.Modify, null)
                  && !HeronPermissions.Allows(HeronRisk.Admin, null)
                  && HeronPermissions.Allows(HeronRisk.Execute, null),
                  "no config at all refuses everything above Execute, and still reads");
            Check(!HeronPermissions.Allows((HeronRisk)7, allOn)
                  && HeronPermissions.RefusedBy((HeronRisk)7, allOn) == HeronPermissions.NoSwitch,
                  "a level above Admin - none exists yet - is refused with every switch on, "
                  + "and RefusedBy says no switch opens it rather than borrowing one");

            // THE ADMIN AND PUBLISH WORDS SAY WHAT KIND OF THING, IN PLAIN WORDS.
            var adminWhat = HeronPermissions.Explain(HeronRisk.Admin, Switches(true, false, false), FakePath) ?? "";
            var publishWhat = HeronPermissions.Explain(HeronRisk.Publish, Switches(true, false, false), FakePath) ?? "";
            Check(adminWhat.Contains("project") && adminWhat.Contains("parameter")
                  && adminWhat.Contains("workset"),
                  "Admin's refusal says what it covers - project parameters, worksets");
            Check(publishWhat.Contains("export") && publishWhat.Contains("save")
                  && publishWhat.Contains("sync with central"),
                  "Publish's refusal says what it covers - export, save, sync with central");
        }

        // ------------------------------------------------------------------
        // The show door and the temporary isolate - the registry's EXECUTE
        // operations through the gate's own rule, every combination.
        // ------------------------------------------------------------------

        /// <summary>
        /// "Show me / zoom to / isolate these" must work with Changes OFF,
        /// because it changes only what is shown. The add-in decides that from
        /// two facts: the level HeronOperationRegistry declares for the NAME,
        /// and HeronPermissions.ReadOnlyCeiling. Section 8 holds the levels;
        /// this holds the names to them, by running both - so a show operation
        /// re-declared at Modify, or the ceiling lowered under Execute, fails
        /// here rather than as "write_disabled" over somebody's selection.
        /// </summary>
        private static void ShowDoor()
        {
            Console.WriteLine();
            Console.WriteLine("9. The show door and the temporary isolate, through the gate - every combination");

            Check(HeronPermissions.ReadOnlyCeiling == HeronRisk.Execute,
                  "the read-only ceiling is Execute - \"changes what is shown, never what exists\"");

            var shows = new[] { "run_fragment_show", "isolate_temporary", "reset_temporary",
                                "select_by_category" };
            foreach (var op in shows)
            {
                var declared = HeronOperationRegistry.IsDeclared(op);
                Check(declared, op + " is declared in the tool registry");
                if (!declared) continue;
                Check(HeronOperationRegistry.RiskOf(op) == HeronRisk.Execute
                      && !HeronOperationRegistry.Writes(op),
                      op + " is Execute, and does not count as a write");
            }

            // EVERY SWITCH COMBINATION, EVERY DECLARED OPERATION. The rule is
            // written once here: at or below the ceiling, always; above it,
            // whatever section 8's rule says - so this only has to hold that
            // nothing at or below the ceiling is ever refused, and that the
            // write executor still is with Changes off.
            var wrong = 0;
            foreach (var changes in new[] { false, true })
            foreach (var admin in new[] { false, true })
            foreach (var publish in new[] { false, true })
            {
                var config = Switches(changes, admin, publish);
                foreach (var op in HeronOperationRegistry.Operations)
                {
                    var risk = HeronOperationRegistry.RiskOf(op);
                    if (risk > HeronPermissions.ReadOnlyCeiling) continue;
                    if (HeronPermissions.Allows(risk, config)
                        && HeronPermissions.RefusedBy(risk, config) == null) continue;
                    wrong++;
                    Check(false, Said(changes, admin, publish) + ": " + op + " (" + risk
                                 + ") was refused - it changes only what is shown");
                }
            }
            Check(wrong == 0,
                  "with every one of the 8 combinations, every operation at or below Execute - "
                  + "the show door, the temporary isolate and its reset among them - passes the gate");

            var allOff = Switches(false, false, false);
            foreach (var op in shows)
            {
                if (!HeronOperationRegistry.IsDeclared(op)) continue;
                Check(HeronPermissions.Allows(HeronOperationRegistry.RiskOf(op), allOff),
                      op + " runs with Changes, Admin and Publish all OFF");
            }
            Check(!HeronPermissions.Allows(HeronOperationRegistry.RiskOf("run_fragment_write"), allOff)
                  && HeronPermissions.RefusedBy(HeronOperationRegistry.RiskOf("run_fragment_write"),
                                                allOff) == HeronPermissions.WriteEnabledKey,
                  "while run_fragment_write is still refused with Changes off, by Changes - the "
                  + "same executor, one level up, and the switch still stands in front of it");
            // ASKED, NOT CALLED, on a registry that may not have the name:
            // RiskOf throws for an undeclared operation, and a throw here
            // would end the run instead of failing this one line.
            Check(HeronOperationRegistry.IsDeclared("run_fragment_show")
                  && HeronOperationRegistry.RiskOf("run_fragment_show")
                     > HeronOperationRegistry.RiskOf("run_fragment_read")
                  && HeronOperationRegistry.RiskOf("run_fragment_show")
                     < HeronOperationRegistry.RiskOf("run_fragment_write"),
                  "and run_fragment_show sits between the read door and the write door - above "
                  + "Analyze, below Modify");
        }

        // ------------------------------------------------------------------
        // HeronSourceScan - what a fragment opens, read before it runs. The
        // read and show paths refuse a fragment that opens a transaction of
        // its own, and so does any that declared noTransactionOpen (D-125).
        // ------------------------------------------------------------------
        private static void SourceScan()
        {
            Console.WriteLine();
            Console.WriteLine("10. A fragment that opens a transaction of its own is seen before it runs");

            Check(HeronSourceScan.OpensTransaction(
                      "using (var t = new Transaction(doc, \"x\")) { t.Start(); t.Commit(); }")
                  == "Transaction", "new Transaction(...) is seen");
            Check(HeronSourceScan.OpensTransaction("var g = new TransactionGroup(doc, \"g\");")
                  == "TransactionGroup", "a TransactionGroup is seen");
            Check(HeronSourceScan.OpensTransaction("var s = new  SubTransaction (doc);")
                  == "SubTransaction", "a SubTransaction is seen");
            Check(HeronSourceScan.OpensTransaction("Transaction t = new(doc, \"x\");")
                  == "Transaction", "and a target-typed new(...) is seen");

            Check(HeronSourceScan.OpensTransaction(
                      "// IT CANNOT RUN INSIDE A TRANSACTION.\n// new Transaction(doc, \"x\")\nvar a = 1;")
                  == null, "a line comment naming one is not one");
            Check(HeronSourceScan.OpensTransaction("/* new Transaction(doc, \"x\") */ var a = 1;")
                  == null, "nor is a block comment");
            Check(HeronSourceScan.OpensTransaction(
                      "var s = \"new Transaction(doc)\"; var v = @\"new TransactionGroup(\"\"g\"\")\";"
                      + " var i = $\"{1} new SubTransaction(doc)\";")
                  == null, "nor a regular, verbatim or interpolated string");
            Check(HeronSourceScan.OpensTransaction(
                      "if (transaction.Commit() == TransactionStatus.Committed) { }") == null,
                  "and using one's status, or a variable named for one, is not opening one");
            Check(HeronSourceScan.OpensTransaction(null) == null
                  && HeronSourceScan.OpensTransaction("") == null, "no source opens nothing");

            var lines = "a // c\nb /* x\ny */ c\n\"s\ns\"";
            Check(HeronSourceScan.CodeOnly(lines).Split('\n').Length == lines.Split('\n').Length,
                  "blanking keeps every line where it was");

            // OVER THE LIBRARY: every fragment the read or show door may run -
            // declared READ, ANALYZE, SUGGEST or EXECUTE, and not one of D-112's
            // edit-mode fragments - opens none, so the refusal costs nothing
            // that works today. Found from this file up; skipped, and said, when
            // the host runs somewhere the library is not.
            var root = AppContext.BaseDirectory;
            while (root != null && !Directory.Exists(Path.Combine(root, "brain", "fragments")))
                root = Path.GetDirectoryName(root.TrimEnd(Path.DirectorySeparatorChar));
            if (root == null)
            {
                Console.WriteLine("  NOT RUN  the fragment library was not found from {0}",
                                  AppContext.BaseDirectory);
                return;
            }

            var risky = new Regex(@"^risk:\s*(READ|ANALYZE|SUGGEST|EXECUTE)\s*$", RegexOptions.Multiline);
            var looked = 0;
            var opening = new List<string>();
            foreach (var card in Directory.GetFiles(Path.Combine(root, "brain", "fragments"),
                                                    "fragment.yaml", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(card);
                if (!risky.IsMatch(text) || text.Contains("editScopeFailures")) continue;
                var impl = Path.Combine(Path.GetDirectoryName(card), "impl");
                if (!Directory.Exists(impl)) continue;
                foreach (var code in Directory.GetFiles(impl, "fragment.cs", SearchOption.AllDirectories))
                {
                    looked++;
                    var opens = HeronSourceScan.OpensTransaction(File.ReadAllText(code));
                    if (opens != null)
                        opening.Add(Path.GetFileName(Path.GetDirectoryName(card)) + " (" + opens + ")");
                }
            }
            Check(looked > 0 && opening.Count == 0,
                  "every one of the " + looked + " READ to EXECUTE fragment sources opens no "
                  + "transaction of its own" + (opening.Count == 0 ? "" : " - " + string.Join(", ", opening)));
        }
    }
}
