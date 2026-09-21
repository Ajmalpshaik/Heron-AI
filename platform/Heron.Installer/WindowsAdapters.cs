// Heron-Agent:  HERON-INS-ENV-002, HERON-INS-RVT-003
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Heron.Installer
{
    /// <summary>
    /// THE ONLY TWO PLACES THIS ASSEMBLY REACHES WINDOWS, and neither has
    /// ever been run.
    ///
    /// Both adapters shell out to PowerShell rather than doing the work
    /// themselves, and that is R-31 rather than laziness. The two rules they
    /// need - which Revit is installed and open, and how to copy a product
    /// into the Addins folder - are already written and proven in
    /// tools\HeronRevit.ps1 and tools\deploy-addin.ps1. A second copy of
    /// either is two rules, and two rules is one that goes stale.
    ///
    /// NOT RUNNABLE ON THE MACHINE THIS WAS WRITTEN ON. It is Linux with no
    /// PowerShell, so not one line below has executed. Everything the engine
    /// DECIDES is tested against fakes; everything these two DO is owed a run
    /// on the owner's PC. Saying so here rather than in a note somewhere, so
    /// the next reader cannot mistake a compile for a proof.
    /// </summary>
    internal static class PowerShellRunner
    {
        /// <summary>
        /// Windows PowerShell 5.1, which is on every Windows since 2016.
        ///
        /// Not `pwsh`: PowerShell 7 is an optional install, and the target
        /// user is a BIM modeller on a locked-down contractor laptop who
        /// cannot add one. Heron's own scripts are written to run on 5.1 for
        /// the same reason - tools\check-structure.py even fails a .ps1 that
        /// 5.1 would misread, because 5.1 is the one that has to work.
        /// </summary>
        private const string Shell = "powershell.exe";

        internal sealed class Result
        {
            public int ExitCode;

            /// <summary>
            /// What the script said on BOTH streams, for a sentence to a
            /// person. Never parse this - see StandardOutput.
            /// </summary>
            public string Output;

            /// <summary>
            /// The standard output stream ALONE.
            ///
            /// KEPT APART BECAUSE ONE BUFFER CANNOT SERVE BOTH READERS.
            /// DeployScriptDeployer wants the transcript, the way the user
            /// would have seen it. PowerShellRevitEnvironment.Look() parses
            /// its answer as JSON, and a single line on standard error -
            /// which deploy-addin.ps1 guarantees on any failure, because it
            /// sets $ErrorActionPreference = "Stop" - lands in the middle of
            /// that JSON and makes it unreadable. The window then says no
            /// Revit was found on a PC that has three.
            /// </summary>
            public string StandardOutput;

            public bool Started;
        }

        internal static Result Run(IEnumerable<string> arguments, int timeoutSeconds)
        {
            var start = new ProcessStartInfo
            {
                FileName = Shell,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            // -NoProfile so a user's own profile cannot change what Heron's
            // scripts see. -ExecutionPolicy Bypass because the script being
            // run is Heron's own, shipped beside this assembly; it does not
            // widen what anything else may run, and nothing downloaded is
            // executed to decide what to install (R-13, Golden Rule 19).
            //
            // AND SINCE D-96 THAT SECOND FLAG IS LOAD-BEARING, NOT A
            // CONVENIENCE. AA9 was run on a real machine on 2026-09-21: a
            // Heron downloaded as a zip carries ZoneId=3 on all 2130 files,
            // and PowerShell at the Windows default of RemoteSigned refuses
            // an unsigned downloaded script BEFORE its first line runs - so
            // deploy-addin.ps1 cannot clear its own mark. Signing was
            // considered and refused (a yearly certificate that silently
            // breaks every install the day it lapses). The ruling was that
            // the installer is the only supported route for a downloaded
            // copy, which makes THIS LINE the thing that makes that route
            // work. Remove it and every downloaded install breaks, on every
            // machine left at the Windows default.
            //
            // tests/test_deploy_script.py holds it, because nothing else
            // did and the ruling is one day old. Row 5b-96.
            start.ArgumentList.Add("-NoProfile");
            start.ArgumentList.Add("-NonInteractive");
            start.ArgumentList.Add("-ExecutionPolicy");
            start.ArgumentList.Add("Bypass");
            foreach (var a in arguments) start.ArgumentList.Add(a);

            var result = new Result();
            var outText = new StringBuilder();
            var errText = new StringBuilder();

            try
            {
                using (var process = Process.Start(start))
                {
                    if (process == null) return result;
                    result.Started = true;

                    // BOTH STREAMS ARE READ AT ONCE, AND THAT IS NOT A STYLE
                    // CHOICE. Reading one to the end and then the other
                    // deadlocks: a pipe holds a few kilobytes, and a child
                    // that fills the one nobody is reading blocks there and
                    // never closes the other, so the read never returns.
                    //
                    // The timeout below does NOT save it, because the thread
                    // is stuck inside the read and has not reached the wait.
                    // MEASURED on 2026-09-21, same pattern, same runtime:
                    // 8 KB on standard error returned in 0.0s; 200 KB never
                    // returned at all and a 10-second ceiling never fired.
                    // See FRAGMENT-ISSUES row 5b-78.
                    //
                    // deploy-addin.ps1 is the caller that reaches it: 578
                    // lines, $ErrorActionPreference = "Stop", and a Revit
                    // that refuses a file produces a PowerShell error record
                    // on standard error while Write-Host is still filling
                    // standard output. That is an installer window hung with
                    // no ceiling, on the one path where something went wrong.
                    process.OutputDataReceived += delegate (object s, DataReceivedEventArgs e)
                    {
                        if (e.Data != null) outText.AppendLine(e.Data);
                    };
                    process.ErrorDataReceived += delegate (object s, DataReceivedEventArgs e)
                    {
                        if (e.Data != null) errText.AppendLine(e.Data);
                    };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (!process.WaitForExit(Math.Max(1, timeoutSeconds) * 1000))
                    {
                        try { process.Kill(true); } catch { /* already gone */ }
                        result.ExitCode = -1;
                        result.StandardOutput = outText.ToString();
                        result.Output = Both(outText, errText) + "\nIt did not finish in time.";
                        return result;
                    }

                    // THE SECOND WAIT IS REQUIRED, not belt and braces. The
                    // one that takes a timeout returns as soon as the process
                    // is gone, which can be before the reader has handed over
                    // the last of what it read; the one that takes none waits
                    // for the streams as well. Without it the tail of the
                    // answer goes missing at random - and a JSON answer with
                    // its tail missing does not parse.
                    process.WaitForExit();

                    result.ExitCode = process.ExitCode;
                }
            }
            catch (Exception e)
            {
                // Started stays false. The caller turns that into a sentence
                // about Windows rather than about a process.
                result.Output = e.Message;
                return result;
            }

            result.StandardOutput = outText.ToString();
            result.Output = Both(outText, errText);
            return result;
        }

        private static string Both(StringBuilder outText, StringBuilder errText)
        {
            return outText.ToString() + errText.ToString();
        }
    }

    /// <summary>
    /// Asks tools\HeronRevit.ps1 which Revit is installed and which is open.
    ///
    /// NOT RUN - see PowerShellRunner. Needs Windows.
    /// </summary>
    public sealed class PowerShellRevitEnvironment : IRevitEnvironment
    {
        private readonly string _repoRoot;

        public PowerShellRevitEnvironment(string repoRoot)
        {
            if (string.IsNullOrEmpty(repoRoot)) throw new ArgumentNullException("repoRoot");
            _repoRoot = repoRoot;
        }

        public IReadOnlyList<string> InstalledReleases()
        {
            var answer = Standing();
            return answer == null ? new string[0] : answer.Installed;
        }

        public string AddinsFolder(string release)
        {
            if (string.IsNullOrEmpty(release)) return null;

            var answer = Standing();
            if (answer == null || answer.Addins == null) return null;

            string folder;
            return answer.Addins.TryGetValue(release, out folder) ? folder : null;
        }

        public IReadOnlyList<RunningRevit> RunningRevits()
        {
            // FRESH, EVERY TIME. See Standing() above - this is what the
            // engine's wait loops on.
            var answer = Look();

            // COULD NOT ASK IS NOT "NOTHING IS OPEN". If the question failed,
            // reporting an empty list would let an install proceed over a
            // running Revit and half-update it. One entry with an unknown
            // release blocks everything, which is what the engine does with
            // a release it cannot read.
            if (answer == null)
                return new[] { new RunningRevit { Release = null, ProcessId = 0 } };

            return answer.Running;
        }

        private sealed class Answer
        {
            public string[] Installed = new string[0];
            public RunningRevit[] Running = new RunningRevit[0];
            public Dictionary<string, string> Addins = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        /// <summary>
        /// WHAT DOES NOT CHANGE WHILE THE WINDOW IS OPEN, asked once.
        ///
        /// Which Revits are installed, and where each one's add-ins go. Every
        /// row of the product list wants the second of those, and starting a
        /// PowerShell process per row would make the window take seconds to
        /// appear on a machine with three Revits.
        ///
        /// WHAT IS OPEN RIGHT NOW IS NEVER REMEMBERED. It is the one thing
        /// that changes by the second, and it is the thing the engine loops
        /// on: R-38a says the install carries on by itself once the user
        /// closes Revit, and a cached "still open" would wait for ever while
        /// the user stared at a closed Revit. RunningRevits() below looks
        /// every single time, and that is the reason.
        /// </summary>
        private Answer _standing;

        private Answer Standing()
        {
            if (_standing == null) _standing = Look();
            return _standing;
        }

        private Answer Look()
        {
            var script = Path.Combine(_repoRoot, "tools", "HeronRevit.ps1");

            // Dot-source the real thing and hand back its answer as JSON. No
            // second implementation of either question - that is the whole
            // point of this class.
            // ONE CALL FOR ALL THREE QUESTIONS. Starting PowerShell costs
            // the best part of a second, and a window that asks three times
            // is a window that takes three seconds to appear.
            //
            // The Addins folder is asked for HERE rather than built in C#:
            // this assembly may not resolve a special folder, and that script
            // has to know the path anyway. See IRevitEnvironment.AddinsFolder.
            var command =
                ". '" + script.Replace("'", "''") + "'; " +
                "$found = @(Get-InstalledRevit); " +
                "$where = @{}; " +
                "foreach ($r in $found) { $where[$r] = (Get-RevitAddinsFolder -RevitVersion $r) }; " +
                "@{ installed = $found; running = @(Get-RunningRevit); addins = $where } " +
                "| ConvertTo-Json -Depth 4 -Compress";

            var run = PowerShellRunner.Run(new[] { "-Command", command }, 60);
            if (!run.Started || run.ExitCode != 0) return null;

            try
            {
                using (var doc = JsonDocument.Parse(run.StandardOutput))
                {
                    var answer = new Answer();

                    JsonElement installed;
                    if (doc.RootElement.TryGetProperty("installed", out installed))
                        answer.Installed = Releases(installed);

                    JsonElement running;
                    if (doc.RootElement.TryGetProperty("running", out running))
                        answer.Running = Runners(running);

                    JsonElement addins;
                    if (doc.RootElement.TryGetProperty("addins", out addins)
                        && addins.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var entry in addins.EnumerateObject())
                            if (entry.Value.ValueKind == JsonValueKind.String)
                                answer.Addins[entry.Name] = entry.Value.GetString();
                    }

                    return answer;
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string[] Releases(JsonElement e)
        {
            var list = new List<string>();
            if (e.ValueKind == JsonValueKind.String) { list.Add(e.GetString()); return list.ToArray(); }
            if (e.ValueKind != JsonValueKind.Array) return new string[0];
            foreach (var item in e.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String) list.Add(item.GetString());
            return list.ToArray();
        }

        private static RunningRevit[] Runners(JsonElement e)
        {
            var list = new List<RunningRevit>();
            if (e.ValueKind == JsonValueKind.Object) { list.Add(One(e)); return list.ToArray(); }
            if (e.ValueKind != JsonValueKind.Array) return new RunningRevit[0];
            foreach (var item in e.EnumerateArray())
                if (item.ValueKind == JsonValueKind.Object) list.Add(One(item));
            return list.ToArray();
        }

        private static RunningRevit One(JsonElement e)
        {
            var revit = new RunningRevit();

            JsonElement version;
            if (e.TryGetProperty("Version", out version) && version.ValueKind == JsonValueKind.String)
                revit.Release = version.GetString();

            JsonElement pid;
            if (e.TryGetProperty("ProcessId", out pid) && pid.ValueKind == JsonValueKind.Number)
                revit.ProcessId = pid.GetInt32();

            return revit;
        }
    }

    /// <summary>
    /// Whether a product's files are where Revit looks, for one release.
    ///
    /// R-2 wants the window to show each product's REAL state rather than a
    /// remembered one: a folder deleted by hand, or a Revit uninstalled, must
    /// read as not installed the next time this window opens. So it is a look
    /// at the disk and never a record Heron kept.
    ///
    /// IT DOES NOT BUILD THE PATH. HeronPaths.RevitAddins does, because
    /// platform/README.md rule 3 says nothing else may - and this file tried
    /// to on 2026-09-21 and tools/check-structure.py refused it, which is the
    /// rule working rather than a rule being quoted.
    /// tools\deploy-addin.ps1 still spells the same path in PowerShell,
    /// which a .ps1 cannot avoid; that is Q-58 and is recorded, not closed.
    ///
    /// NOT RUN on Windows. The path is built from APPDATA, which resolves
    /// anywhere, but nobody has yet looked at whether it matches what Revit
    /// actually scans on a real machine - AB4 in docs/NEEDS-CHECKING.md.
    /// </summary>
    public sealed class InstalledProductsOnDisk : IInstalledProducts
    {
        private readonly IRevitEnvironment _revit;

        public InstalledProductsOnDisk(IRevitEnvironment revit)
        {
            if (revit == null) throw new ArgumentNullException("revit");
            _revit = revit;
        }

        public bool IsInstalled(HeronProduct product, string release)
        {
            if (product == null || string.IsNullOrEmpty(product.Folder)) return false;
            if (string.IsNullOrEmpty(product.Assembly)) return false;
            if (string.IsNullOrEmpty(release)) return false;

            // BOTH, not either. A manifest with no folder beside it is a Revit
            // that finds nothing; a folder with no manifest is a folder Revit
            // never looks in. Reporting Installed for half of that sends
            // somebody hunting for a tab that was never going to appear.
            // NOT BUILT HERE. Asked for, and a folder that could not be
            // found out reads as not installed rather than as a guess.
            var addins = _revit.AddinsFolder(release);
            if (string.IsNullOrEmpty(addins)) return false;
            var manifest = Path.Combine(addins, product.Addin ?? "");
            var assembly = Path.Combine(addins, product.Folder, product.Assembly);

            return File.Exists(manifest) && File.Exists(assembly);
        }
    }

    /// <summary>
    /// Drives tools\deploy-addin.ps1 for one product and one release.
    ///
    /// EVERY RULE ABOUT COPYING LIVES IN THAT SCRIPT and none of them is
    /// repeated here: the runtime check that refuses a 2027 build for Revit
    /// 2024, the backup that rollback restores, the resources folder, the
    /// deps.json that .NET 8 and .NET 10 fail without, the verify-what-was-
    /// written step. Each was found by something going wrong once.
    ///
    /// NOT RUN - see PowerShellRunner. Needs Windows.
    /// </summary>
    public sealed class DeployScriptDeployer : IProductDeployer
    {
        private readonly string _repoRoot;
        private readonly string _configuration;

        public DeployScriptDeployer(string repoRoot) : this(repoRoot, "Release") { }

        public DeployScriptDeployer(string repoRoot, string configuration)
        {
            if (string.IsNullOrEmpty(repoRoot)) throw new ArgumentNullException("repoRoot");
            _repoRoot = repoRoot;
            _configuration = string.IsNullOrEmpty(configuration) ? "Release" : configuration;
        }

        public DeployOutcome Deploy(HeronProduct product, string release)
        {
            if (product == null) throw new ArgumentNullException("product");

            var script = Path.Combine(_repoRoot, "tools", "deploy-addin.ps1");
            if (!File.Exists(script))
            {
                return DeployOutcome.Failed(
                    "'" + product.Name + "' could not be installed: the deploy script " +
                    "is missing from " + script + ". The Heron files are incomplete - " +
                    "fetch them again.");
            }

            var run = PowerShellRunner.Run(new[]
            {
                "-File", script,
                "-RevitVersion", release,
                "-Product", product.Id,
                "-Configuration", _configuration,
            }, 600);

            if (!run.Started)
            {
                return DeployOutcome.Failed(
                    "'" + product.Name + "' could not be installed: Windows PowerShell " +
                    "would not start. Heron installs by running a PowerShell script, so " +
                    "it cannot continue. (" + run.Output + ")");
            }

            if (run.ExitCode != 0)
            {
                return DeployOutcome.Failed(
                    "'" + product.Name + "' was not installed for Revit " + release +
                    ". " + LastMeaningfulLine(run.Output));
            }

            return DeployOutcome.Ok(
                "'" + product.Name + "' installed for Revit " + release + ".");
        }

        /// <summary>
        /// The script's own last complaint, which is the sentence written for
        /// a person. Handing back the whole transcript would bury it.
        /// </summary>
        private static string LastMeaningfulLine(string output)
        {
            if (string.IsNullOrEmpty(output)) return "It stopped without saying why.";

            var lines = output.Replace("\r", "").Split('\n');
            for (var i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i].Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("at ", StringComparison.Ordinal)) continue;
                if (line.StartsWith("+", StringComparison.Ordinal)) continue;
                return line;
            }

            return "It stopped without saying why.";
        }
    }
}
