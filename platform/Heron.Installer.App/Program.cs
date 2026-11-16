// Heron-Agent:  HERON-INS-ORC-001
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;

namespace Heron.Installer.App
{
    /// <summary>
    /// Route C - the one a modeller double-clicks.
    ///
    /// 01-requirements.md section 2: three routes, one engine. Routes A and B
    /// reach the same InstallEngine with their choices already made and never
    /// come through here. This is the only one with a window, and all it adds
    /// is a person choosing.
    ///
    /// IT WIRES THINGS TOGETHER AND NOTHING ELSE. The manifest is read by
    /// ProductManifest, what to offer is decided by InstallerScreen, what to
    /// install is decided by InstallPlan and done by InstallEngine. A rule
    /// that appears in this file is a rule outside everything that tests it.
    ///
    /// NOT RUN. WPF, and this repository is developed on Linux.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            var root = RepositoryRoot();
            var manifestPath = Path.Combine(root, "platform", "heron-products.json");

            ProductManifest manifest;
            try
            {
                manifest = ProductManifest.Load(manifestPath);
            }
            catch (Exception e)
            {
                // WHAT HAPPENED AND WHAT TO DO NEXT - docs/14. A dialog saying
                // only that something went wrong leaves a modeller with a
                // download they cannot tell apart from a working one.
                MessageBox.Show(e.Message, "Heron Installer",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return 1;
            }

            // OPENED FROM INSIDE THE STORE CLAUDE, IT WOULD INSTALL WHERE REVIT
            // NEVER LOOKS - Q-63, ClaudeAppCopy. A person double-clicking it in
            // Explorer never meets this; an AI starting it from Claude's shell
            // does, and is told where to start it instead.
            if (ClaudeAppCopy.Inside(new WindowsClaudeAppLook()))
            {
                MessageBox.Show(ClaudeAppCopy.Refusal, "Heron Installer",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return 1;
            }

            // OPENED BY THE SETUP - D-167, appended 2026-11-16. The Setup puts
            // Heron's folder in place with every product's zip under payload\,
            // then opens this window with `--from` that folder, so the window a
            // modeller sees is this one and nothing is downloaded. The branch
            // is heron-install's own --from, line for line: the same refusal
            // by name, and the same ProductFolder checking every zip's
            // checksum. AFTER the Store-Claude refusal above, because the work
            // folder ProductFolder unpacks into is a write.
            ProductFolder handover = null;
            if (args.Length > 0)
            {
                if (args.Length != 2 || args[0] != "--from")
                {
                    MessageBox.Show("Heron Installer takes one choice: --from and a Heron folder, " +
                                    "as the Setup opens it. Nothing was changed.",
                                    "Heron Installer", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return 1;
                }
                var notAFolder = ProductFolder.WhyNotAHeronFolder(args[1]);
                if (notAFolder != null)
                {
                    MessageBox.Show(notAFolder, "Heron Installer",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return 1;
                }
                handover = new ProductFolder(args[1], Workspace());
            }

            var revit = new PowerShellRevitEnvironment(root);
            var installed = new InstalledProductsOnDisk(revit);

            // WHICH ROUTE THIS IS, DECIDED BY WHAT IS ON THE DISK - Stage 5.
            //
            // On a checkout with builds in it, the deploy script finds them
            // exactly as it always has and nothing is downloaded. That is the
            // developer, and it is also the offline install.
            //
            // With no build for any release, the files have to come from
            // somewhere, and the only somewhere is the published release -
            // which is the case a modeller who downloaded one exe is in.
            //
            // ASKED OF THE DEPLOYER, NEVER TYPED AGAIN. "Built" only means
            // anything alongside a configuration: Debug and Release are two
            // different folders and the script reads exactly one of them.
            // Writing "Release" out a second time here is how the two come to
            // disagree, and that has already cost a round trip once.
            var localDeployer = new DeployScriptDeployer(root);
            var localBuilds = new BuildsOnDisk(root, localDeployer.Configuration);

            var releases = revit.InstalledReleases();

            // THE SETUP'S FOLDER IS THE FILES, WHEN THERE IS ONE - nothing is
            // looked for on the disk and nothing is downloaded.
            IProductFiles files = handover;
            if (files == null)
            {
                files = InstallerScreen.AnythingBuilt(manifest, releases, localBuilds)
                    ? null
                    : Downloader(manifest, root);
            }

            var deployer = files == null
                ? localDeployer
                : new DeployScriptDeployer(root, localDeployer.Configuration, files);

            // AND THE GREYING FOLLOWS THE ROUTE. A release with no local build
            // is greyed only when there is nothing to download either -
            // otherwise the window would refuse an install that would have
            // worked, which is worse than the defect that rule was added for.
            var screen = InstallerScreen.Build(manifest,
                                               releases,
                                               revit.RunningRevits(),
                                               installed,
                                               files == null ? localBuilds : null);

            // THE CLAUDE TICK IS OFFERED ONLY BY THE SETUP'S ROUTE. That is the
            // installed Heron a modeller keeps; a checkout or an unzipped folder
            // opened by hand is somebody working on Heron, whose chats already
            // reach it through the folder's own .mcp.json.
            //
            // AND NOT ON A DEVELOPER'S PC - DeveloperCopy, D-167 amended
            // 2026-11-16. There the installed Heron steps aside: no Claude tick,
            // and a line before Install saying what it does to Revit.
            var developer = DeveloperCopy.StepsAsideFor(root);
            var window = new InstallerWindow(screen, VersionOf(manifest),
                                             InstallerScreen.InstallLocation(root),
                                             handover != null && developer == null,
                                             developer == null ? null : DeveloperCopy.Warning(developer));
            window.InstallPressed += delegate (IReadOnlyList<string> products,
                                               IReadOnlyList<string> releases,
                                               IReadOnlyList<InstallStep> removals)
            {
                // READ HERE, ON THE WINDOW'S THREAD - a tick box asked from the
                // worker below throws.
                var python = window.SetUpAiSide;
                var claude = window.AddToClaude;
                Install(window, screen, manifest, root, revit, deployer,
                        products, releases, removals, python, claude, handover != null);
            };

            return new Application().Run(window);
        }

        /// <summary>
        /// Where the Setup's zips are unpacked and checked, under the user's own
        /// temp - heron-install's Workspace, for the same reason: the folder the
        /// exe sits in is not somewhere to unpack into.
        /// </summary>
        private static string Workspace()
        {
            var work = Path.Combine(Path.GetTempPath(), "Heron", "download");
            Directory.CreateDirectory(work);
            return work;
        }

        /// <summary>
        /// Off the window's thread, because the engine WAITS - R-38a. A wait
        /// on the UI thread is a frozen window, and a frozen window is one a
        /// user kills halfway through an install.
        /// </summary>
        private static void Install(InstallerWindow window,
                                    InstallerScreen screen,
                                    ProductManifest manifest,
                                    string root,
                                    IRevitEnvironment revit,
                                    IProductDeployer deployer,
                                    IReadOnlyList<string> products,
                                    IReadOnlyList<string> releases,
                                    IReadOnlyList<InstallStep> removals,
                                    bool pythonTicked,
                                    bool claudeTicked,
                                    bool fromSetup)
        {
            var engine = new InstallEngine(revit, deployer);
            engine.Pause = delegate { Thread.Sleep(1000); };
            engine.OnWaiting = delegate (string why)
            {
                window.Dispatcher.Invoke(delegate { window.Waiting(why); });
            };

            var worker = new Thread(delegate ()
            {
                ShowWhatIsOpen(window, screen, revit);
                var report = engine.Apply(manifest, products, releases, removals);
                ShowWhatIsOpen(window, screen, revit);

                // THE OTHER HALF OF ONE INSTALL, ON THE SAME THREAD - pip and
                // the knowledge store take minutes, and the window says each
                // step as it starts. What runs and what it says is
                // PythonSetup's; this only carries it to the screen.
                // UNTICKED IS heron-install's --no-python: the add-in goes in and
                // the Python side is left alone, said so in the report.
                IReadOnlyList<string> python = new string[0];
                var pythonWanted = PythonSetup.ShouldRun(report, manifest);
                if (pythonWanted && !pythonTicked)
                {
                    python = new[] { "Heron's AI side was left out, as asked. " +
                                     "Open this window again with that tick on to add it." };
                }
                else if (pythonWanted)
                {
                    var setup = new PythonSetup(root, new ProcessProgramRunner());
                    setup.OnStep = delegate (string step)
                    {
                        window.Dispatcher.Invoke(delegate { window.Waiting(step); });
                    };
                    python = setup.RunAndSave(PythonSetup.LogPath()).Shown;
                }

                ShowWhatIsOpen(window, screen, revit);
                window.Dispatcher.Invoke(delegate { window.Report(report, python); });

                // HERON INTO CLAUDE CODE, LAST, AND ONLY AFTER AN INSTALL THAT
                // WORKED - heron-install --add-to-claude's step. An entry beside
                // an add-in that failed is a chat pointed at a Heron that is not
                // there. What it writes and what it says are ClaudeCodeEntry's.
                if (claudeTicked && products.Count > 0 && !report.Abandoned && report.Failed == 0)
                {
                    var step = ClaudeCodeEntry.Apply(root);
                    window.Dispatcher.Invoke(delegate { window.AlsoSay(step.Said); });
                }

                // REMOVE HERON TAKES IT OUT OF REVIT, AND ONLY THAT - the
                // button's own words. Installed by the Setup, the rest of it -
                // the program folder and its Claude Code entry - has one door
                // out, Installed apps, which takes everything (D-167 item 2).
                if (fromSetup && products.Count == 0 && report.Removed > 0)
                {
                    window.Dispatcher.Invoke(delegate
                    {
                        window.AlsoSay("Heron's program folder, and its entry in Claude Code if it has one, " +
                                       "stay until you remove Heron AI in Settings > Apps > Installed apps.");
                    });
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        /// <summary>
        /// The line above the report, worked out again from the Revits open
        /// now and drawn - when a run starts, when the add-in half is done,
        /// and before the report. Row 5b-1068: worked out once, when the
        /// window opened, it still said Revit 2026 was open after Revit had
        /// closed and the install had ended READY.
        ///
        /// ON THE WORKER THREAD, because asking Windows starts PowerShell;
        /// only the drawing goes back onto the window's.
        /// </summary>
        private static void ShowWhatIsOpen(InstallerWindow window,
                                           InstallerScreen screen,
                                           IRevitEnvironment revit)
        {
            screen.RefreshCloseRevitFirst(revit.RunningRevits());
            window.Dispatcher.Invoke(delegate { window.RedrawCloseRevitFirst(); });
        }

        /// <summary>
        /// Where to fetch from, or null when the manifest does not say.
        ///
        /// NULL IS NOT A CRASH AND NOT A GUESS. A product list with no source
        /// is one nothing can be downloaded from; the window then offers only
        /// what is built, and the release rows say so. Inventing a repository
        /// name here would be the installer deciding where to get software
        /// from, which is the one decision it must never make - R-13.
        /// </summary>
        private static ReleaseDownload Downloader(ProductManifest manifest, string root)
        {
            if (!manifest.KnowsItsSource) return null;

            // UNDER THE USER'S OWN TEMP, not beside the exe. A modeller's
            // Downloads folder is not somewhere to unpack 24 assemblies into,
            // and the exe may sit somewhere they cannot write at all.
            var work = Path.Combine(Path.GetTempPath(), "Heron", "download");
            Directory.CreateDirectory(work);

            return new ReleaseDownload(
                ReleaseDownload.LatestUrlFor(manifest.SourceOwner, manifest.SourceRepo),
                work);
        }

        /// <summary>
        /// The folder holding platform\ and tools\.
        ///
        /// Beside the executable in a release, and three levels up in a
        /// build tree. Both are tried rather than assumed, because assuming
        /// the wrong one produces "the product list is missing" on a machine
        /// where it is not.
        /// </summary>
        private static string RepositoryRoot()
        {
            var here = AppContext.BaseDirectory;
            for (var folder = new DirectoryInfo(here); folder != null; folder = folder.Parent)
            {
                if (File.Exists(Path.Combine(folder.FullName, "platform", "heron-products.json")))
                    return folder.FullName;
            }
            return here;
        }

        /// <summary>
        /// The version shown in the title, taken from the product list rather
        /// than from this assembly - R-3 again. The installer is not a product
        /// with a version of its own; it installs ones that have.
        /// </summary>
        private static string VersionOf(ProductManifest manifest)
        {
            foreach (var product in manifest.Products)
                if (!string.IsNullOrEmpty(product.Version)) return "v" + product.Version;
            return "";
        }
    }
}
