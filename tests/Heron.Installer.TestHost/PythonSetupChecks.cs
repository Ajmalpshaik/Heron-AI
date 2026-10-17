// Heron-Agent:  none
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.IO;
using Heron.Installer;

namespace Heron.Installer.TestHost
{
    /// <summary>
    /// The one-step install's second half - Heron's Python side - and the
    /// Store-Claude refusal, against fakes.
    ///
    /// WHAT THIS PROVES: what PythonSetup runs, in what order, in which folder,
    /// with which pip flags; that it stops at the first failure and says what
    /// to do; that it runs only when a product that talks to a chat went in;
    /// and that ClaudeAppCopy says inside only when a name in the copy and the
    /// same name under AppData are one thing on disk.
    ///
    /// WHAT IT CANNOT PROVE: that pip installs anything on Windows, that
    /// heron-ready says READY there, or that the identity test answers the
    /// same inside a real Store-built Claude. ProcessProgramRunner is started
    /// for real below, on whatever runs this; the Windows half of
    /// WindowsClaudeAppLook is not. NEEDS-CHECKING Group EP owes both.
    /// </summary>
    internal static class PythonSetupChecks
    {
        /// <summary>Answers each run from a script, and remembers what it was asked.</summary>
        private sealed class ScriptedRunner : IProgramRunner
        {
            private readonly Func<string, ProgramRun> _answer;
            public readonly List<string> Kinds = new List<string>();
            public readonly List<string> Lines = new List<string>();
            public readonly List<string> Folders = new List<string>();
            public readonly List<IReadOnlyList<string>> Arguments = new List<IReadOnlyList<string>>();

            public ScriptedRunner(Func<string, ProgramRun> answer) { _answer = answer; }

            public ProgramRun Run(string program, IReadOnlyList<string> arguments, string folder, int timeoutSeconds)
            {
                var kind = KindOf(arguments);
                Kinds.Add(kind);
                Lines.Add(program + " " + string.Join(" ", new List<string>(arguments).ToArray()));
                Folders.Add(folder);
                Arguments.Add(arguments);
                return _answer(kind);
            }

            /// <summary>Which of the four steps a command line is.</summary>
            private static string KindOf(IReadOnlyList<string> a)
            {
                if (a.Count > 0 && a[0] == "-c") return "probe";
                if (a.Count > 1 && a[1] == "pip") return "pip";
                foreach (var one in a) if (one == "--check") return "check";
                return "warm";
            }
        }

        private static ProgramRun Ok(string output)
        {
            return new ProgramRun { Started = true, ExitCode = 0, Output = output };
        }

        private static ProgramRun Exit(int code, string output)
        {
            return new ProgramRun { Started = true, ExitCode = code, Output = output };
        }

        private const string Python312 = "HERON-PYTHON 3.12.4 C:\\Users\\m\\AppData\\Local\\Programs\\Python\\Python312\\python.exe";

        /// <summary>A Python that works and a heron-ready that says READY.</summary>
        private static ProgramRun AllGood(string kind)
        {
            if (kind == "probe") return Ok(Python312 + "\n");
            if (kind == "pip") return Ok("Successfully installed mcp-2.2.0 PyYAML-6.0.3\n");
            if (kind == "warm") return Exit(1, "...\nNOT READY - 1 thing(s) to fix\n");
            return Ok("...\nREADY in 1.1 s. Everything Heron needs is here.\n");
        }

        // ------------------------------------------------------------ fakes for ShouldRun

        private sealed class OneRevit : IRevitEnvironment
        {
            public IReadOnlyList<string> InstalledReleases() { return new[] { "2024" }; }
            public string AddinsFolder(string release) { return release == "2024" ? "/fake/Addins/2024" : null; }
            public IReadOnlyList<RunningRevit> RunningRevits() { return new RunningRevit[0]; }
        }

        private sealed class Deployer : IProductDeployer
        {
            private readonly bool _works;
            public Deployer(bool works) { _works = works; }
            public DeployOutcome Deploy(HeronProduct product, string release)
            {
                return _works ? DeployOutcome.Ok("installed") : DeployOutcome.Failed("refused");
            }
            public DeployOutcome Remove(HeronProduct product, string release) { return DeployOutcome.Ok("removed"); }
        }

        private const string Manifest = @"{
          ""products"": [
            { ""id"": ""talks"", ""name"": ""Talks"", ""description"": ""d"", ""tab"": ""T"",
              ""addin"": ""A.addin"", ""assembly"": ""A.dll"", ""folder"": ""A"",
              ""addInId"": ""11111111-1111-1111-1111-111111111111"",
              ""revit"": [""2024""], ""requires"": [], ""version"": ""0.1.0"",
              ""partOf"": null, ""state"": ""SHIPPED"", ""needsPython"": true },
            { ""id"": ""quiet"", ""name"": ""Quiet"", ""description"": ""d"", ""tab"": ""Q"",
              ""addin"": ""B.addin"", ""assembly"": ""B.dll"", ""folder"": ""B"",
              ""addInId"": ""22222222-2222-2222-2222-222222222222"",
              ""revit"": [""2024""], ""requires"": [], ""version"": ""0.1.0"",
              ""partOf"": null, ""state"": ""SHIPPED"", ""needsPython"": false },
            { ""id"": ""old"", ""name"": ""Old"", ""description"": ""d"", ""tab"": ""O"",
              ""addin"": ""C.addin"", ""assembly"": ""C.dll"", ""folder"": ""C"",
              ""addInId"": ""33333333-3333-3333-3333-333333333333"",
              ""revit"": [""2024""], ""requires"": [], ""version"": ""0.1.0"",
              ""partOf"": null, ""state"": ""SHIPPED"" },
            { ""id"": ""typo"", ""name"": ""Typo"", ""description"": ""d"", ""tab"": ""Y"",
              ""addin"": ""D.addin"", ""assembly"": ""D.dll"", ""folder"": ""D"",
              ""addInId"": ""44444444-4444-4444-4444-444444444444"",
              ""revit"": [""2024""], ""requires"": [], ""version"": ""0.1.0"",
              ""partOf"": null, ""state"": ""SHIPPED"", ""needsPython"": ""true"" }
          ]
        }";

        // ------------------------------------------------------------ fake for ClaudeAppCopy

        private sealed class Look : IClaudeAppLook
        {
            public string Roaming { get; set; }
            public string Local { get; set; }
            public readonly List<string> CopyList = new List<string>();
            public readonly Dictionary<string, string[]> Inside = new Dictionary<string, string[]>();
            public readonly HashSet<string> Same = new HashSet<string>();
            public bool CannotTell;

            public IReadOnlyList<string> Copies() { return CopyList; }

            public IReadOnlyList<string> Names(string folder)
            {
                string[] names;
                return Inside.TryGetValue(folder, out names) ? names : new string[0];
            }

            public bool? SameEntry(string first, string second)
            {
                if (CannotTell) return null;
                return Same.Contains(first + "|" + second);
            }
        }

        internal static void Run(Action<bool, string> check, Func<string, string[], bool> names)
        {
            var root = Path.Combine(Path.GetTempPath(), "heron-python-setup-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                Body(root, check, names);
            }
            finally
            {
                try { Directory.Delete(root, true); } catch (IOException) { }
            }
        }

        private static void Body(string root, Action<bool, string> check, Func<string, string[], bool> names)
        {
            // A DELEGATE CANNOT TAKE params, SO THIS ONE DOES IT FOR IT.
            Func<string, string[], bool> says = names;
            bool Says(string text, params string[] words) { return says(text, words); }

            Console.WriteLine();
            Console.WriteLine("ONE STEP - A FOLDER WITHOUT THE PINNED LIST SETS UP NOTHING, AND SAYS SO");
            var bare = new ScriptedRunner(AllGood);
            var none = new PythonSetup(root, bare).Run();
            check(bare.Kinds.Count == 0, "nothing is run in a folder with no requirements-lock.txt");
            check(none.Steps.Count == 1 && !none.Steps[0].Succeeded && !none.Ready,
                  "and the one step it reports failed");
            check(Says(none.Lines[0], "requirements-lock.txt", "heron-ready.py", "download"),
                  "naming both missing files and telling the person to download Heron again");

            File.WriteAllText(Path.Combine(root, PythonSetup.LockFile), "PyYAML==6.0.3 --hash=sha256:00\n");
            Directory.CreateDirectory(Path.Combine(root, "tools"));
            File.WriteAllText(Path.Combine(root, "tools", "heron-ready.py"), "# stand-in\n");
            check(PythonSetup.WhyNotHere(root) == null, "with both files there, the folder is accepted");

            Console.WriteLine();
            Console.WriteLine("ONE STEP - EVERYTHING WORKS, IN THIS ORDER, IN THE HERON FOLDER");
            var good = new ScriptedRunner(AllGood);
            var steps = new List<string>();
            var setup = new PythonSetup(root, good);
            setup.OnStep = delegate (string step) { steps.Add(step); };
            var ok = setup.Run();
            check(string.Join(",", good.Kinds.ToArray()) == "probe,pip,warm,check",
                  "find Python, install the packages, build, check: " + string.Join(",", good.Kinds.ToArray()));
            check(ok.Ready, "the final heron-ready --check said READY, so it is ready");
            check(ok.Steps.Count == 4, "four steps reported");
            var allHere = true;
            foreach (var folder in good.Folders) if (folder != root) allHere = false;
            check(allHere, "every command runs IN the Heron folder, where heron-ready finds its files");
            var commandOk = true;
            foreach (var line in good.Lines) if (!line.StartsWith(PythonSetup.Command + " ", StringComparison.Ordinal)) commandOk = false;
            check(commandOk && PythonSetup.Command == "python",
                  "and every one is the plain command `python` - the one .mcp.json starts Heron with");
            check(steps.Count == 4 && Says(steps[1], "packages") && Says(steps[2], "knowledge store"),
                  "the window is told each step as it starts, so a long pip is not a silent window");
            check(Says(ok.Lines[0], "3.12.4", "Python312"),
                  "it says which Python it found, and where");
            check(Says(ok.Lines[3], "READY"), "and ends by saying READY");

            Console.WriteLine();
            Console.WriteLine("THE PIP LINE - PINNED, HASHED, PER USER");
            var pip = new List<string>(good.Arguments[1]);
            check(pip.Contains("--user"), "--user: this Windows user only, no administrator rights (Q-39)");
            check(pip.Contains("--require-hashes"),
                  "--require-hashes: a file that is not the one resolved is refused");
            var at = pip.IndexOf("-r");
            check(at >= 0 && at + 1 < pip.Count && pip[at + 1] == Path.Combine(root, PythonSetup.LockFile),
                  "-r names requirements-lock.txt in this Heron folder, by its full path");
            check(!pip.Contains("--upgrade") && !pip.Contains("-U"),
                  "and never --upgrade: the versions are the pinned ones, not the newest");
            check(pip.Contains("--no-input"), "--no-input: pip asks nobody a question here");
            var check3 = new List<string>(good.Arguments[3]);
            check(check3.Count == 2 && check3[1] == "--check",
                  "the verdict is heron-ready --check, in a process of its own");
            check(new List<string>(good.Arguments[2]).Count == 1,
                  "and the build is heron-ready with no flag - the run that builds the store");
            check(Says(ok.Log, "pip", "--require-hashes", "Successfully installed", "exit 0"),
                  "the log keeps every command, what it printed and how it ended");

            Console.WriteLine();
            Console.WriteLine("NO PYTHON - NAMED, WITH THE ONE COMMAND, AND NOTHING ELSE RUN");
            var missing = new ScriptedRunner(delegate (string kind) { return new ProgramRun { Started = false }; });
            var gone = new PythonSetup(root, missing).Run();
            check(missing.Kinds.Count == 1, "only the look for Python ran");
            check(!gone.Ready && gone.Steps.Count == 1, "not ready, one step");
            check(Says(gone.Lines[0], "winget install Python.Python.3.12 --scope user", "python"),
                  "it names the per-user winget command and the plain command `python`");

            var store = new ScriptedRunner(delegate (string kind)
            {
                return Exit(9009, "Python was not found; run without arguments to install from the Microsoft Store");
            });
            var alias = new PythonSetup(root, store).Run();
            check(store.Kinds.Count == 1 && !alias.Ready, "the Store's stand-in for Python stops it too");
            check(Says(alias.Lines[0], "App execution aliases", "winget"),
                  "and it says that `python` is the Store shortcut, with both ways out");

            Console.WriteLine();
            Console.WriteLine("TOO OLD A PYTHON IS REFUSED BY NAME, BEFORE PIP FAILS ON IT");
            var old = new ScriptedRunner(delegate (string kind) { return Ok("HERON-PYTHON 3.10.11 C:\\Python310\\python.exe"); });
            var tooOld = new PythonSetup(root, old).Run();
            check(old.Kinds.Count == 1 && !tooOld.Ready, "pip is never asked");
            check(Says(tooOld.Lines[0], "3.10.11", "3." + PythonSetup.OldestMinor),
                  "the sentence names the Python found and the oldest that works");
            var two = new ScriptedRunner(delegate (string kind) { return Ok("HERON-PYTHON 2.7.18 C:\\Python27\\python.exe"); });
            check(!new PythonSetup(root, two).Run().Ready && two.Kinds.Count == 1, "and a Python 2 is refused too");
            var banner = new ScriptedRunner(delegate (string kind)
            {
                return kind == "probe" ? Ok("Welcome to the site Python 3.9\n" + Python312) : AllGood(kind);
            });
            check(new PythonSetup(root, banner).Run().Ready,
                  "a banner printed before the marked line is not read as the version");

            Console.WriteLine();
            Console.WriteLine("PIP FAILING STOPS IT THERE, WITH PIP'S OWN REASON");
            var hashes = new ScriptedRunner(delegate (string kind)
            {
                return kind == "pip"
                    ? Exit(1, "Collecting mcp==2.2.0\nERROR: THESE PACKAGES DO NOT MATCH THE HASHES FROM THE REQUIREMENTS FILE.\n\n")
                    : AllGood(kind);
            });
            var bad = new PythonSetup(root, hashes).Run();
            check(string.Join(",", hashes.Kinds.ToArray()) == "probe,pip",
                  "nothing after pip runs - a failure further down would only hide this one");
            check(!bad.Ready && !bad.Steps[1].Succeeded, "not ready, and the packages step failed");
            check(Says(bad.Lines[1], "DO NOT MATCH THE HASHES", "internet"),
                  "pip's own last line is carried, and the person is told what to try");
            check(Says(bad.Lines[1], "Revit add-in"),
                  "and told the add-in does not depend on it, so they do not reinstall it");
            var slow = new ScriptedRunner(delegate (string kind)
            {
                return kind == "pip" ? new ProgramRun { Started = true, ExitCode = -1, TimedOut = true } : AllGood(kind);
            });
            var stalled = new PythonSetup(root, slow).Run();
            check(!stalled.Ready && Says(stalled.Lines[1], "did not finish", "minutes"),
                  "a pip stopped at its ceiling says so in minutes");

            Console.WriteLine();
            Console.WriteLine("THE VERDICT IS heron-ready --check, AND ITS LIST IS CARRIED");
            var notReady = new ScriptedRunner(delegate (string kind)
            {
                return kind == "check"
                    ? Exit(1, "...\nNOT READY - 2 thing(s) to fix, each with its command above:\n  - PyYAML is missing\n  - the Heron add-in is not installed for any Revit\n")
                    : AllGood(kind);
            });
            var unready = new PythonSetup(root, notReady).Run();
            check(!unready.Ready, "a NOT READY check is not ready, whatever pip said");
            check(Says(unready.Lines[3], "PyYAML is missing", "add-in is not installed", "heron-ready.py"),
                  "each thing heron-ready listed is named, and the person told to run it");
            var warmBroke = new ScriptedRunner(delegate (string kind)
            {
                return kind == "warm" ? new ProgramRun { Started = false } : AllGood(kind);
            });
            var stillChecked = new PythonSetup(root, warmBroke).Run();
            check(string.Join(",", warmBroke.Kinds.ToArray()) == "probe,pip,warm,check" && stillChecked.Ready,
                  "the build step is not the verdict - the check still runs and decides");

            Console.WriteLine();
            Console.WriteLine("THE LOG IS WRITTEN, AND A LOG THAT CANNOT BE IS A SENTENCE, NOT A CRASH");
            var logPath = Path.Combine(root, "logs", "python-setup.log");
            var saved = ok.Save(logPath);
            check(File.Exists(logPath) && Says(File.ReadAllText(logPath), "--require-hashes"),
                  "the log is written where it was asked to be");
            check(Says(saved, logPath), "and the sentence says where");
            var blocked = Path.Combine(root, PythonSetup.LockFile, "under-a-file.log");
            check(Says(ok.Save(blocked), "could not be written"),
                  "a path under a FILE answers a sentence rather than throwing");
            check(Says(PythonSetup.LogPath(), "Heron", "python-setup.log"),
                  "the default log goes under the user's own temp, in a Heron folder");

            Console.WriteLine();
            Console.WriteLine("IT RUNS ONLY WHEN A PRODUCT THAT TALKS TO A CHAT WENT IN - R-3, Q-PE-2");
            var manifest = ProductManifest.Parse(Manifest);
            check(manifest.Find("talks").NeedsPython, "needsPython true is read");
            check(!manifest.Find("quiet").NeedsPython, "needsPython false is read");
            check(!manifest.Find("old").NeedsPython,
                  "a manifest from before the field asks for no Python - absent is false");
            check(!manifest.Find("typo").NeedsPython,
                  "and \"true\" in quotes is not true - tools/check-products.py refuses it");
            check(PythonSetup.Wanted(new[] { "quiet", "talks" }, manifest), "asked BEFORE: wanted when one ticked product needs it");
            check(!PythonSetup.Wanted(new[] { "quiet", "old" }, manifest), "and not when none does");

            var works = new InstallEngine(new OneRevit(), new Deployer(true));
            check(PythonSetup.ShouldRun(works.Install(manifest, new[] { "talks" }, new[] { "2024" }), manifest),
                  "asked AFTER: it runs when that product really went in");
            check(!PythonSetup.ShouldRun(works.Install(manifest, new[] { "quiet" }, new[] { "2024" }), manifest),
                  "it does not run for a tools-only install - that asked for no Python");
            var fails = new InstallEngine(new OneRevit(), new Deployer(false));
            check(!PythonSetup.ShouldRun(fails.Install(manifest, new[] { "talks" }, new[] { "2024" }), manifest),
                  "nor when the product failed to install - packages for nothing serve nothing");
            check(!PythonSetup.ShouldRun(null, manifest), "and a run with no report is not one to follow");

            Console.WriteLine();
            Console.WriteLine("IT IS ASKED BEFORE IT IS DONE - Q-39");
            check(Says(PythonSetup.WhatInstallAlsoDoes, "pinned", "no administrator", "heron-ready",
                        "3." + PythonSetup.OldestMinor, "internet"),
                  "the sentence shown before Install names what it installs, without admin, the check, " +
                  "the Python it needs and the internet");

            Console.WriteLine();
            Console.WriteLine("THE STORE CLAUDE'S PRIVATE COPY - Q-63");
            var look = new Look { Roaming = "R", Local = "L" };
            check(!ClaudeAppCopy.Inside(look), "no Store-built Claude on the PC: not inside");
            check(!ClaudeAppCopy.Inside(null), "and nothing to look with is not inside either");
            look.CopyList.Add("C");
            look.Inside[Path.Combine("C", "Roaming")] = new[] { "Python", "Heron" };
            look.Inside[Path.Combine("C", "Local")] = new[] { "Heron" };
            check(!ClaudeAppCopy.Inside(look),
                  "a copy whose names are other things than the same names under AppData: OUTSIDE - " +
                  "the person's own terminal on a PC that has the app");
            look.Same.Add(Path.Combine("R", "Heron") + "|" + Path.Combine("C", "Roaming", "Heron"));
            check(ClaudeAppCopy.Inside(look), "one name that is ONE thing on disk: INSIDE");
            look.CannotTell = true;
            check(!ClaudeAppCopy.Inside(look),
                  "cannot tell is outside - a refusal on a PC where the install would work is the worse mistake");
            var localOnly = new Look { Roaming = null, Local = "L" };
            localOnly.CopyList.Add("C");
            localOnly.Inside[Path.Combine("C", "Local")] = new[] { "Heron" };
            localOnly.Same.Add(Path.Combine("L", "Heron") + "|" + Path.Combine("C", "Local", "Heron"));
            check(ClaudeAppCopy.Inside(localOnly), "the Local side alone is enough");
            check(Says(ClaudeAppCopy.Refusal, "Nothing was changed", "File Explorer", "HeronInstaller.exe",
                        "outside Claude"),
                  "the refusal says nothing changed and names the one supported route");
            check(ClaudeAppCopy.PackagePattern == "AnthropicPBC.Claude_*",
                  "and the package pattern is heron-ready's CLAUDE_PACKAGES");

            Console.WriteLine();
            Console.WriteLine("THE REAL RUNNER, STARTED HERE - the stream reading, not Windows");
            var real = new ProcessProgramRunner();
            var dotnet = real.Run("dotnet", new[] { "--version" }, root, 60);
            check(dotnet.Started && dotnet.ExitCode == 0 && !dotnet.TimedOut,
                  "it starts a real program and reads its exit code");
            check(dotnet.Output != null && dotnet.Output.Trim().Length > 0 && char.IsDigit(dotnet.Output.Trim()[0]),
                  "and what it printed: " + (dotnet.Output ?? "").Trim());
            var nothing = real.Run("heron-there-is-no-such-program", new string[0], root, 10);
            check(!nothing.Started, "a command that does not exist is NOT STARTED, not a failure with a code");
            var windowsLook = new WindowsClaudeAppLook();
            if (!OperatingSystem.IsWindows())
                check(windowsLook.SameEntry(root, root) == null,
                      "off Windows the identity test answers cannot tell, which reads as outside");
        }
    }
}
