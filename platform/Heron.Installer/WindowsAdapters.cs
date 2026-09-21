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
            public string Output;
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
            start.ArgumentList.Add("-NoProfile");
            start.ArgumentList.Add("-NonInteractive");
            start.ArgumentList.Add("-ExecutionPolicy");
            start.ArgumentList.Add("Bypass");
            foreach (var a in arguments) start.ArgumentList.Add(a);

            var result = new Result();
            var text = new StringBuilder();

            try
            {
                using (var process = Process.Start(start))
                {
                    if (process == null) return result;
                    result.Started = true;

                    text.Append(process.StandardOutput.ReadToEnd());
                    text.Append(process.StandardError.ReadToEnd());

                    if (!process.WaitForExit(Math.Max(1, timeoutSeconds) * 1000))
                    {
                        try { process.Kill(true); } catch { /* already gone */ }
                        result.ExitCode = -1;
                        result.Output = text + "\nIt did not finish in time.";
                        return result;
                    }

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

            result.Output = text.ToString();
            return result;
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
            var answer = Ask();
            return answer == null ? new string[0] : answer.Installed;
        }

        public IReadOnlyList<RunningRevit> RunningRevits()
        {
            var answer = Ask();

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
        }

        private Answer Ask()
        {
            var script = Path.Combine(_repoRoot, "tools", "HeronRevit.ps1");

            // Dot-source the real thing and hand back its answer as JSON. No
            // second implementation of either question - that is the whole
            // point of this class.
            var command =
                ". '" + script.Replace("'", "''") + "'; " +
                "@{ installed = @(Get-InstalledRevit); running = @(Get-RunningRevit) } " +
                "| ConvertTo-Json -Depth 4 -Compress";

            var run = PowerShellRunner.Run(new[] { "-Command", command }, 60);
            if (!run.Started || run.ExitCode != 0) return null;

            try
            {
                using (var doc = JsonDocument.Parse(run.Output))
                {
                    var answer = new Answer();

                    JsonElement installed;
                    if (doc.RootElement.TryGetProperty("installed", out installed))
                        answer.Installed = Releases(installed);

                    JsonElement running;
                    if (doc.RootElement.TryGetProperty("running", out running))
                        answer.Running = Runners(running);

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
