// Heron-Agent:  HERON-INS-DEP-005, HERON-INS-BRN-007, HERON-INS-HLT-009
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Heron.Installer
{
    /// <summary>What one run of a program answered. The Windows half fills it in.</summary>
    public sealed class ProgramRun
    {
        /// <summary>
        /// False when the program could not be started at all - most often
        /// because there is no such command on this PC. That is a different
        /// answer from a program that started and failed, and the sentence a
        /// person reads is different too.
        /// </summary>
        public bool Started { get; set; }

        public int ExitCode { get; set; }

        /// <summary>Standard output and standard error, as a person would have seen them.</summary>
        public string Output { get; set; }

        /// <summary>It was stopped at its ceiling rather than finishing.</summary>
        public bool TimedOut { get; set; }
    }

    /// <summary>
    /// Runs a program and waits for it, with a ceiling.
    ///
    /// AN INTERFACE FOR THE SAME REASON IRevitEnvironment IS ONE: everything
    /// PythonSetup decides - what to run, in what order, when to stop, and
    /// what to tell a person - is tested against a fake on a machine with no
    /// Windows and no Python of the right shape. The real runner is in
    /// WindowsAdapters.cs beside the PowerShell one, and shares its reading
    /// of the two streams.
    /// </summary>
    public interface IProgramRunner
    {
        ProgramRun Run(string program, IReadOnlyList<string> arguments, string folder, int timeoutSeconds);
    }

    /// <summary>One step of setting up Heron's Python side, and how it went.</summary>
    public sealed class PythonStep
    {
        public string Name { get; internal set; }
        public bool Succeeded { get; internal set; }

        /// <summary>The sentence for a person. What happened, and what to do next when it failed.</summary>
        public string Said { get; internal set; }
    }

    /// <summary>Everything PythonSetup did, for the window and the command line to print.</summary>
    public sealed class PythonSetupReport
    {
        internal readonly List<PythonStep> _steps = new List<PythonStep>();
        internal readonly StringBuilder _log = new StringBuilder();
        internal readonly List<string> _show = new List<string>();

        /// <summary>
        /// What a door prints after RunAndSave: the steps' lines, and where the
        /// log is when it did not say READY. Empty after a bare Run().
        /// </summary>
        public IReadOnlyList<string> Shown { get { return _show; } }

        public IReadOnlyList<PythonStep> Steps { get { return _steps; } }

        /// <summary>
        /// The final `heron-ready --check` said READY.
        ///
        /// NOTHING ELSE MAKES THIS TRUE. Packages that installed cleanly are
        /// not a Heron that starts, and the check that a chat's own start-up
        /// would pass is the only thing that says it does.
        /// </summary>
        public bool Ready { get; internal set; }

        /// <summary>Every command and everything it printed - for a log file, not for the screen.</summary>
        public string Log { get { return _log.ToString(); } }

        /// <summary>What a person reads, one line per step, in order.</summary>
        public IReadOnlyList<string> Lines
        {
            get
            {
                var lines = new List<string>();
                foreach (var step in _steps) lines.Add(step.Said);
                return lines;
            }
        }

        /// <summary>
        /// Writes the log, and says where - or why it could not. Never throws:
        /// the setup is finished by the time this runs, and a full disk must
        /// not turn what it did into a crash.
        /// </summary>
        public string Save(string path)
        {
            try
            {
                var folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
                File.WriteAllText(path, Log, new UTF8Encoding(false));
                return "Everything it printed is in " + path;
            }
            catch (IOException e)
            {
                return "The log could not be written (" + e.Message + ").";
            }
            catch (UnauthorizedAccessException e)
            {
                return "The log could not be written (" + e.Message + ").";
            }
        }
    }

    /// <summary>
    /// The half of an install the Revit add-in cannot do for itself: Heron's
    /// Python side - its packages, its knowledge store, and a check that a
    /// chat could start it.
    ///
    /// WHY IT IS PART OF ONE INSTALL NOW. Until 2026-10-17 the installer put
    /// the add-in in place and docs/WHAT-TO-INSTALL.md left a person to run
    /// two pip commands and `tools/heron-ready.py` by hand. On the owner's new
    /// laptop on 2026-10-08 the first "create a family" took over seven minutes
    /// where the office PC took one or two, and the missing half was exactly
    /// this. docs/07 section 1 has always listed it - step 6, "Heron installs
    /// required packages", step 11, the brain initialised, step 15, the health
    /// check - and the owner asked for it by name on 2026-10-17.
    ///
    /// IT IS ASKED, NOT DONE UNASKED - Q-39, and the Dependency Agent's own row
    /// in docs/28: "Confirm, never automatic". The window says what Install
    /// will do before it is pressed (WhatInstallAlsoDoes), heron-install says
    /// it in its listing before it acts, and `--no-python` leaves it out. It
    /// installs Python PACKAGES for this user only, with no administrator
    /// rights. It never installs Python itself: a missing Python is named,
    /// with the one command that installs it.
    ///
    /// PINNED, AND HASHED. The packages come from requirements-lock.txt with
    /// --require-hashes, so pip refuses any file that is not byte-for-byte the
    /// one resolved there. `pip install mcp` unpinned is what pulled a broken
    /// `cryptography` on 2026-09-16.
    ///
    /// IT REPEATS NONE OF heron-ready's RULES. Whether the knowledge store may
    /// be built here, whether the search model is installed, whether the plain
    /// command `python` is the one Claude Code will start - all of that is
    /// tools/heron-ready.py's, which a person can also run by hand. This runs
    /// it twice: once to build what a first chat would otherwise build, and
    /// once with --check, in a fresh process, as the verdict.
    ///
    /// NOT RUN ON WINDOWS. Everything it DECIDES is tested against a fake
    /// runner in tests/Heron.Installer.TestHost; what it DOES is NEEDS-CHECKING
    /// Group EP.
    /// </summary>
    public sealed class PythonSetup
    {
        /// <summary>
        /// The plain command, and only that. Claude Code starts Heron's server
        /// with `python` (.mcp.json), so a Python found any other way would set
        /// up a Python the chat never runs.
        /// </summary>
        public const string Command = "python";

        /// <summary>The pinned list, at the root of the Heron folder.</summary>
        public const string LockFile = "requirements-lock.txt";

        /// <summary>
        /// Python 3.11 is the oldest every pinned package installs on -
        /// rpds-py, which jsonschema needs, starts there. requirements-lock.txt
        /// says the same at its top, where it was measured.
        /// </summary>
        public const int OldestMinor = 11;

        /// <summary>
        /// Python 3.14 is the newest the lock was measured on: every pinned
        /// package arrived as a Windows wheel for 3.11 to 3.14, and for a newer
        /// Python some would not. With --only-binary pip would refuse those
        /// anyway; refusing here first says why in Heron's words rather than
        /// pip's. Raise it with the lock, never alone (review of #558).
        /// </summary>
        public const int NewestMinor = 14;

        /// <summary>What a run with no Python, or too old a one, is told to do.</summary>
        public const string InstallPython = "winget install Python.Python.3.12 --scope user";

        // CEILINGS, AND WHY EACH IS THE SIZE IT IS. Nothing here should ever be
        // stopped at its ceiling on a working PC - they exist so a PC where
        // something hangs gets a sentence instead of a window that never ends.
        //
        // pip fetches about thirty files from PyPI; a slow office line is
        // minutes. The first heron-ready builds the knowledge store, and
        // downloads the search model when model2vec is installed - the part of
        // the owner's seven minutes this exists to move out of the first chat.
        private const int AskSeconds = 60;
        private const int PipSeconds = 15 * 60;
        private const int WarmSeconds = 30 * 60;
        private const int CheckSeconds = 5 * 60;

        /// <summary>
        /// Printed before Install is pressed, in the window and in the command
        /// line's listing. It is what makes this ASKED rather than done.
        ///
        /// Read-only rather than const only because C# will not fold the
        /// number into a constant string.
        /// </summary>
        public static readonly string WhatInstallAlsoDoes =
            "When a product that talks to a chat is installed, Install also sets up " +
            "Heron's Python side for your Windows user: it installs Heron's Python " +
            "packages at pinned versions (no administrator rights), builds Heron's " +
            "knowledge store, and checks the whole set-up with heron-ready. It needs " +
            "Python 3." + OldestMinor + " to 3." + NewestMinor + ", and the internet once for the packages.";

        private readonly string _root;
        private readonly IProgramRunner _runner;

        /// <param name="root">The Heron folder - the one holding requirements-lock.txt and tools\.</param>
        public PythonSetup(string root, IProgramRunner runner)
        {
            if (string.IsNullOrEmpty(root)) throw new ArgumentNullException("root");
            if (runner == null) throw new ArgumentNullException("runner");
            _root = root;
            _runner = runner;
        }

        /// <summary>Told as each step starts, so the window can say what it is waiting for.</summary>
        public Action<string> OnStep { get; set; }

        /// <summary>
        /// Whether this install needs Heron's Python side at all.
        ///
        /// ONLY WHEN A PRODUCT THAT TALKS TO A CHAT WENT IN, and the manifest
        /// says which - `needsPython`, never a product id written here (R-3).
        /// Q-PE-2 made the AI connector optional: somebody who installs the
        /// tools alone asked for no Python, and a run that failed for want of
        /// one would fail an install that worked.
        ///
        /// AND ONLY WHEN IT WENT IN. An abandoned run changed nothing, and a
        /// run that only removed products has nothing new to serve.
        /// </summary>
        public static bool ShouldRun(InstallReport report, ProductManifest manifest)
        {
            if (report == null || manifest == null || report.Abandoned) return false;
            foreach (var result in report.Results)
            {
                if (!result.Succeeded || result.Removed) continue;
                var product = manifest.Find(result.ProductId);
                if (product != null && product.NeedsPython) return true;
            }
            return false;
        }

        /// <summary>
        /// Whether any of these products, once installed, will want it - for
        /// saying so BEFORE anything is done. ShouldRun is the same question
        /// asked of what actually went in.
        /// </summary>
        public static bool Wanted(IEnumerable<string> productIds, ProductManifest manifest)
        {
            if (productIds == null || manifest == null) return false;
            foreach (var id in productIds)
            {
                var product = manifest.Find(id);
                if (product != null && product.NeedsPython) return true;
            }
            return false;
        }

        /// <summary>
        /// Where the log of the last run goes: the user's own temp, beside the
        /// download folder both doors already use, and replaced every run.
        /// </summary>
        public static string LogPath()
        {
            return Path.Combine(Path.GetTempPath(), "Heron", "python-setup.log");
        }

        /// <summary>
        /// Run, write the log, and hand back what a person should read - the
        /// steps' own lines, and where the log is when it did not say READY.
        ///
        /// ONE METHOD FOR BOTH DOORS. The window and heron-install wired these
        /// four lines each until the review of #558 found two copies of one
        /// rule outside everything that tests it.
        /// </summary>
        public PythonSetupReport RunAndSave(string logPath)
        {
            var report = Run();
            var saved = report.Save(logPath);
            report._show.AddRange(report.Lines);
            if (!report.Ready) report._show.Add(saved);
            return report;
        }

        /// <summary>
        /// Why this folder cannot set up Heron's Python side, or null when it can.
        ///
        /// Two file names, answered before anything is run. A Heron folder
        /// downloaded before 2026-10-17 carries neither, and the honest answer
        /// to that is to download it again rather than to guess at packages.
        /// </summary>
        public static string WhyNotHere(string root)
        {
            var missing = new List<string>();
            if (!File.Exists(Path.Combine(root, LockFile))) missing.Add(LockFile);
            if (!File.Exists(ReadyScript(root))) missing.Add(Path.Combine("tools", "heron-ready.py"));
            if (missing.Count == 0) return null;
            return "Heron's Python side was not set up: this Heron folder has no " +
                   string.Join(" and no ", missing.ToArray()) + ". Download heron-project.zip " +
                   "again from Heron's Releases page and run the installer from the new folder.";
        }

        /// <summary>
        /// The pip line, exactly. Public so a test can hold every flag in place.
        ///
        ///   --user              this Windows user only, no administrator - Q-39
        ///   --require-hashes    a file that is not the one resolved is refused
        ///   --only-binary=:all: a wheel or nothing. The lock lists source
        ///                       archives too, and building one needs a
        ///                       compiler a modeller has not got - and fetches
        ///                       build tools no hash checks (review of #558)
        ///   --no-input          a question pip asks nobody can answer here
        ///   --disable-pip-version-check, --no-warn-script-location
        ///                       two warnings a modeller cannot act on, which
        ///                       would otherwise be the last lines on the screen
        /// </summary>
        public static IReadOnlyList<string> PipArguments(string root)
        {
            return new[]
            {
                "-m", "pip", "install",
                "--user",
                "--require-hashes",
                "--only-binary=:all:",
                "--no-input",
                "--disable-pip-version-check",
                "--no-warn-script-location",
                "-r", Path.Combine(root, LockFile),
            };
        }

        private static string ReadyScript(string root)
        {
            return Path.Combine(root, "tools", "heron-ready.py");
        }

        /// <summary>
        /// Find Python, install the packages, build what a first chat would
        /// build, then check. Finding Python and installing the packages each
        /// stop the run when they fail, because a failure further down would
        /// only hide the first. The build never stops it: the check is the
        /// verdict, and it runs whatever the build did.
        /// </summary>
        public PythonSetupReport Run()
        {
            var report = new PythonSetupReport();

            var notHere = WhyNotHere(_root);
            if (notHere != null)
            {
                Add(report, "files", false, notHere);
                return report;
            }

            if (!FindPython(report)) return report;
            if (!InstallPackages(report)) return report;
            Warm(report);
            Check(report);
            return report;
        }

        private bool FindPython(PythonSetupReport report)
        {
            Say("Looking for Python");

            // ONE LINE WITH A MARK ON IT, so a banner a site's Python prints
            // first cannot be read as the version.
            var run = Do(report, Command, new[]
            {
                "-c",
                "import sys; print('HERON-PYTHON %d.%d.%d %s' % (sys.version_info[:3] + (sys.executable,)))",
            }, AskSeconds);

            int major, minor;
            string version, where;
            if (!run.Started || run.ExitCode != 0 || !ReadVersion(run.Output, out major, out minor, out version, out where))
            {
                // THE STORE'S STAND-IN FOR PYTHON ANSWERS 9009. Windows ships
                // `python` as an App execution alias that opens the Microsoft
                // Store when no Python is installed; it starts, prints a line
                // about the Store, and exits 9009. Told apart from "nothing
                // found at all" because the fix has one more step in it.
                //
                // AND A PYTHON THAT STARTED IS NOT A MISSING ONE - review of
                // #558. Told to install Python with winget, the owner of a
                // Python with a broken set-up would install a second one.
                string sentence;
                if (!run.Started)
                    sentence = "No Python answered to the plain command `python`, which is the one Claude Code " +
                               "starts Heron with. Install Python for your user (" + InstallPython + "), open a " +
                               "new window, and run the installer again.";
                else if (run.ExitCode == 9009)
                    sentence = "`python` on this PC is Windows' shortcut to the Microsoft Store, not a Python. " +
                               "Install Python for your user (" + InstallPython + "), or turn the shortcut off " +
                               "in Settings > Apps > Advanced app settings > App execution aliases, then run " +
                               "the installer again.";
                else if (run.TimedOut)
                    sentence = "`python` did not answer within " + AskSeconds + " seconds. Run python --version " +
                               "in a new window to see what it does, then run the installer again.";
                else
                    sentence = "`python` started but did not say which Python it is (exit " + run.ExitCode +
                               "): " + LastMeaningfulLine(run.Output) + ". Run python --version in a new " +
                               "window to see what is wrong with it, then run the installer again.";
                Add(report, "python", false, "Heron's Python side was not set up. " + sentence);
                return false;
            }

            if (major != 3 || minor < OldestMinor || minor > NewestMinor)
            {
                Add(report, "python", false,
                    "Heron's Python side was not set up: `python` is Python " + version + " (" + where +
                    "), and Heron's packages are measured on Python 3." + OldestMinor + " to 3." + NewestMinor +
                    ". Install Python 3.12 for your user (" + InstallPython + "), make it the one `python` " +
                    "starts, and run the installer again.");
                return false;
            }

            Add(report, "python", true, "Python " + version + " found at " + where + ".");
            return true;
        }

        private bool InstallPackages(PythonSetupReport report)
        {
            Say("Installing Heron's Python packages - this can take a few minutes");
            var run = Do(report, Command, PipArguments(_root), PipSeconds);

            if (run.Started && run.ExitCode == 0 && !run.TimedOut)
            {
                Add(report, "packages", true,
                    "Heron's Python packages are installed for your user, at the versions in " + LockFile + ".");
                return true;
            }

            // PIP'S OWN ERROR LINE, because it names the package and the cause
            // - a file it could not reach, a hash that did not match - and a
            // sentence of Heron's own could only guess at which. Its LAST line
            // is usually a detail under it ("Got 5f3e...") or a hint, so the
            // last line starting ERROR: is taken first (review of #558).
            var said = run.TimedOut
                ? "pip did not finish in " + (PipSeconds / 60) + " minutes."
                : !run.Started
                    ? "pip could not be started."
                    : "pip stopped: " + PipCause(run.Output);
            Add(report, "packages", false,
                "Heron's Python packages were not installed. " + said + " " + PipAdvice(run) +
                " Nothing about the Revit add-in depends on this.");
            return false;
        }

        private void Warm(PythonSetupReport report)
        {
            Say("Building Heron's knowledge store - the first time takes the longest");
            var run = Do(report, Command, new[] { ReadyScript(_root) }, WarmSeconds);

            // NOT A VERDICT - the check below decides. But what is SAID follows
            // what it answered: until the review of #558 this said "built" for
            // a run that crashed, or that left the store alone. heron-ready
            // exits 0 when everything is here and built, 1 when it listed
            // something to fix, and anything else is a stop it did not plan.
            string said;
            if (run.TimedOut)
                said = "Building Heron's knowledge store did not finish in " + (WarmSeconds / 60) +
                       " minutes. A chat will finish it the first time it starts.";
            else if (!run.Started)
                said = "heron-ready could not be started to build Heron's knowledge store.";
            else if (run.ExitCode == 0)
                said = "heron-ready built what a first chat would otherwise build while you wait.";
            else if (run.ExitCode == 1)
                said = "heron-ready ran to build Heron's knowledge store and listed something to fix - " +
                       "the check below says what.";
            else
                said = "heron-ready stopped unexpectedly (exit " + run.ExitCode + ") while building Heron's " +
                       "knowledge store: " + LastMeaningfulLine(run.Output);
            Add(report, "warm", run.Started && !run.TimedOut && run.ExitCode == 0, said);
        }

        private void Check(PythonSetupReport report)
        {
            Say("Checking the whole set-up");
            var run = Do(report, Command, new[] { ReadyScript(_root), "--check" }, CheckSeconds);

            if (run.Started && run.ExitCode == 0 && !run.TimedOut)
            {
                report.Ready = true;
                Add(report, "check", true, "heron-ready says READY: a chat can start Heron on this PC.");
                return;
            }

            var missing = NotReadyItems(run.Output);
            var said = missing.Count > 0
                ? "heron-ready says NOT READY: " + string.Join("; ", missing.ToArray()) + "."
                : run.TimedOut
                    ? "heron-ready did not finish in " + (CheckSeconds / 60) + " minutes."
                    : "heron-ready did not say READY.";
            Add(report, "check", false,
                said + " Run python tools/heron-ready.py in the Heron folder: it names each " +
                "thing with the command that fixes it.");
        }

        // ------------------------------------------------------------ reading

        /// <summary>The line FindPython asks for, read back. False when it is not there.</summary>
        internal static bool ReadVersion(string output, out int major, out int minor,
                                         out string version, out string where)
        {
            major = minor = 0;
            version = where = null;
            foreach (var raw in (output ?? "").Split('\n'))
            {
                var line = raw.Trim();
                if (!line.StartsWith("HERON-PYTHON ", StringComparison.Ordinal)) continue;

                var rest = line.Substring("HERON-PYTHON ".Length);
                var space = rest.IndexOf(' ');
                version = space < 0 ? rest : rest.Substring(0, space);
                where = space < 0 ? "" : rest.Substring(space + 1).Trim();

                var parts = version.Split('.');
                return parts.Length >= 2
                       && int.TryParse(parts[0], out major)
                       && int.TryParse(parts[1], out minor);
            }
            return false;
        }

        /// <summary>
        /// The things heron-ready listed under NOT READY - its "  - " lines
        /// after that heading, which is the shape tools/heron-ready.py prints.
        /// </summary>
        internal static List<string> NotReadyItems(string output)
        {
            var items = new List<string>();
            var under = false;
            foreach (var raw in (output ?? "").Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (line.StartsWith("NOT READY", StringComparison.Ordinal))
                {
                    under = true;
                    continue;
                }
                if (under && line.StartsWith("  - ", StringComparison.Ordinal))
                    items.Add(line.Substring(4).Trim());
            }
            return items;
        }

        /// <summary>The last line that says something - pip's cause is usually there.</summary>
        internal static string LastMeaningfulLine(string output)
        {
            var lines = (output ?? "").Split('\n');
            for (var i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i].Trim();
                if (line.Length > 0) return line.Length <= 300 ? line : line.Substring(0, 297) + "...";
            }
            return "it printed nothing";
        }

        /// <summary>
        /// pip's own cause: the last line that starts ERROR:, else its last
        /// line that says anything.
        /// </summary>
        internal static string PipCause(string output)
        {
            var lines = (output ?? "").Split('\n');
            for (var i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("ERROR:", StringComparison.Ordinal))
                    return line.Length <= 300 ? line : line.Substring(0, 297) + "...";
            }
            return LastMeaningfulLine(output);
        }

        /// <summary>
        /// What to try, matched to what pip said. "Check the internet" was the
        /// only advice until the review of #558, and it is wrong for a hash that
        /// did not match and for a Python inside a virtual environment.
        /// </summary>
        internal static string PipAdvice(ProgramRun run)
        {
            var text = (run == null ? "" : run.Output ?? "");
            if (text.IndexOf("User site-packages are not visible", StringComparison.OrdinalIgnoreCase) >= 0)
                return "`python` is inside a virtual environment, where --user installs nothing. Start the " +
                       "installer from a window with no environment active, then run it again.";
            if (text.IndexOf("DO NOT MATCH THE HASHES", StringComparison.OrdinalIgnoreCase) >= 0)
                return "A downloaded file is not the one Heron pinned, so nothing was installed. Try again " +
                       "later; if it says the same, report it - do not install the package another way.";
            foreach (var sign in new[] { "Could not fetch", "NewConnectionError", "Max retries", "ProxyError",
                                         "SSLError", "Temporary failure in name resolution", "timed out",
                                         "No matching distribution" })
                if (text.IndexOf(sign, StringComparison.OrdinalIgnoreCase) >= 0)
                    return "pip could not reach the files. Check the internet connection - a company proxy " +
                           "may need to allow pypi.org and files.pythonhosted.org - and run the installer again.";
            return "Run the installer again after fixing what pip says; the log has its whole output.";
        }

        // ------------------------------------------------------------ small

        private ProgramRun Do(PythonSetupReport report, string program,
                              IReadOnlyList<string> arguments, int seconds)
        {
            report._log.AppendLine("> " + program + " " + string.Join(" ", arguments));
            var run = _runner.Run(program, arguments, _root, seconds) ?? new ProgramRun();
            report._log.AppendLine(run.Output ?? "");
            report._log.AppendLine("(" + (!run.Started ? "not started"
                                          : run.TimedOut ? "stopped at its ceiling"
                                          : "exit " + run.ExitCode) + ")");
            report._log.AppendLine();
            return run;
        }

        private static void Add(PythonSetupReport report, string name, bool succeeded, string said)
        {
            report._steps.Add(new PythonStep { Name = name, Succeeded = succeeded, Said = said });
        }

        private void Say(string what)
        {
            var handler = OnStep;
            if (handler != null) handler(what + "...");
        }
    }
}
