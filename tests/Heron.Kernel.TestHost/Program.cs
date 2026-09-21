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
    ///   HeronAudit, HeronLease, HeronPermissions, HeronOperationRegistry
    ///     already have suites that name them. Row 7 counted them; they are
    ///     not this host's job.
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
            Identity();
            Stop();
            AtomicWrite();
            Append();
            Config();

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
            Check(HeronStop.Stop(), "Stop() reports that THIS call changed it");
            Check(!HeronStop.Stop(), "and a second Stop() reports that it did not");
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

            Check(HeronStop.Resume(), "Resume() reports that THIS call changed it");
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
    }
}
