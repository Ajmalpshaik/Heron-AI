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

namespace Heron.Installer.Cli
{
    /// <summary>
    /// Routes 1 and 2 - the AI installing, and the same engine door three
    /// uses.
    ///
    /// 01-requirements.md section 2: three routes, one engine. Route 3 is
    /// Heron.Installer.App, where a person ticks boxes. Here the choices
    /// arrive already made, on a command line, and everything after that is
    /// identical - InstallerScreen decides what may be offered, InstallPlan
    /// decides what to do, InstallEngine does it.
    ///
    /// IT WIRES THINGS TOGETHER AND NOTHING ELSE, exactly as the window's
    /// Program.cs says of itself. A rule that appears in this file is a rule
    /// outside everything that tests it. The two things that look like rules
    /// in here are not: turning --products into row ids is asking the screen,
    /// and turning --source into somewhere to fetch from is asking
    /// InstallSource.
    ///
    /// IT INSTALLS. IT NEVER REMOVES - and that is a safety property rather
    /// than an omission. R-21 makes unticking mean uninstall, and unticking
    /// is a gesture a person makes in a window, having seen what is already
    /// there and been asked to confirm. There is nothing on a command line
    /// that could mean it: a product left out of --products was not unticked,
    /// it was simply not mentioned. So Apply is never handed removals here,
    /// and a caller wanting something gone uses the window, where the
    /// confirmation naming every product and release lives.
    ///
    /// NOT RUN. It needs Windows, PowerShell and a Revit; this repository is
    /// developed on Linux. Everything it decides is in Arguments and in the
    /// engine, both of which run here. See docs/NEEDS-CHECKING.md.
    /// </summary>
    internal static class Program
    {
        /// <summary>Did what was asked.</summary>
        private const int Done = 0;

        /// <summary>Refused before touching anything.</summary>
        private const int Refused = 1;

        /// <summary>Ran, and something failed.</summary>
        private const int SomethingFailed = 2;

        /// <summary>
        /// Could not run: Revit stayed open. Nothing was changed.
        ///
        /// THREE IS THE HOUSE CODE FOR "COULD NOT RUN", not a third kind of
        /// failure - tests/README.md, where a suite exiting 3 is waiting on
        /// something the machine has not got and is explicitly NOT a pass and
        /// NOT a failure. An install that stopped because Revit was open is
        /// the same shape: nothing was proved either way and nothing was
        /// changed. A caller that reads it as failure will start repairing
        /// something that is not broken.
        /// </summary>
        private const int CouldNotRun = 3;

        /// <summary>
        /// How long the update check may take before it gives up.
        ///
        /// SHORT, BECAUSE IT IS NEWS RATHER THAN WORK. The install is already
        /// done by the time this runs; a blocked corporate network that never
        /// answers must cost seconds, not a minute of somebody watching a
        /// finished install appear to hang.
        /// </summary>
        private const int UpdateCheckSeconds = 10;

        private static int Main(string[] args)
        {
            var asked = Arguments.Read(args);

            if (asked.WantsHelp)
            {
                Console.Out.WriteLine(Arguments.Usage());
                return Done;
            }

            if (asked.Problem != null)
            {
                Console.Error.WriteLine(asked.Problem);
                return Refused;
            }

            var root = RepositoryRoot();

            ProductManifest manifest;
            try
            {
                manifest = ProductManifest.Load(Path.Combine(root, "platform", "heron-products.json"));
            }
            // NARROW, AND THE LIST IS ProductManifest.Load's OWN - D-52, "narrow
            // before you swallow". The first draft had one catch (Exception),
            // and tests/test_csharp.py went red the same minute: it measures
            // which kind of handler is commonest in this repository, and one
            // more broad one tied the count. The suite was not touched.
            //
            //   IOException                 the file is not there, or the read
            //                               failed. FileNotFoundException, which
            //                               Load throws by name, is one of these.
            //   InvalidDataException        not valid JSON, or a product list
            //                               that breaks its own rules. It derives
            //                               from SystemException, NOT IOException
            //                               despite the namespace - assuming
            //                               otherwise has already cost this
            //                               branch a killed process once.
            //   UnauthorizedAccessException a folder this user may not read.
            //
            // Anything else is a fault rather than a bad install, and a fault
            // that reaches the top of this door with its stack intact is worth
            // far more than one flattened into a tidy sentence.
            catch (IOException e)
            {
                // WHAT HAPPENED AND WHAT TO DO NEXT - docs/14, the same rule
                // the window's dialog keeps. A caller left with "failed to
                // load" cannot tell a broken download from a wrong folder.
                Console.Error.WriteLine(e.Message);
                return Refused;
            }
            catch (InvalidDataException e)
            {
                Console.Error.WriteLine(e.Message);
                return Refused;
            }
            catch (UnauthorizedAccessException e)
            {
                Console.Error.WriteLine(e.Message);
                return Refused;
            }

            // ROUTE 2 - HERON'S FILES ALREADY ON THIS PC, AND NO INTERNET.
            //
            // Q-PE-12 was answered on 2026-09-22: what a person is handed is
            // the folder the release builder produces, carrying the built
            // plugin AND the brain (R-51). R-30's old second half - "the files
            // are already in the repo" - was never true, and a copy of the
            // repository is still not this.
            //
            // REFUSED EARLY, AND BY NAME. Whether that folder is a Heron
            // handover at all is two file names, so it is answered before any
            // Revit is looked for - somebody who pointed at the parent folder
            // should be told so on a PC with no Revit on it.
            ProductFolder handover = null;
            if (asked.FromFolder != null)
            {
                var notAFolder = ProductFolder.WhyNotAHeronFolder(asked.FromFolder);
                if (notAFolder != null)
                {
                    Console.Error.WriteLine(notAFolder);
                    return Refused;
                }
                handover = new ProductFolder(asked.FromFolder, Workspace());
            }

            // THE GATE RUNS BEFORE ANYTHING IS TOUCHED, and the order is the
            // point rather than a tidiness.
            //
            // Judging a source is pure string work - InstallSource opens no
            // socket and reads no file - so there is no reason for it to wait
            // behind finding Revit, and two reasons not to. A hostile address
            // should be refused on a PC with no Revit on it at all; and a
            // refusal that only arrives after PowerShell has been run is a
            // refusal that ran something first.
            //
            // The first draft had this below, because that is the order the
            // window does things in. The window has no untrusted input at
            // this point, and this door's whole reason for existing is that
            // it does.
            SourceVerdict verdict = null;
            if (asked.Source != null)
            {
                verdict = InstallSource.Judge(asked.Source, manifest);
                if (!verdict.Accepted)
                {
                    Console.Error.WriteLine(verdict.Why);
                    return Refused;
                }
            }

            var revit = new PowerShellRevitEnvironment(root);
            var releases = revit.InstalledReleases();

            // ASKED OF THE DEPLOYER, NEVER TYPED AGAIN. "Built" means nothing
            // without a configuration - Debug and Release are two folders and
            // the deploy script reads exactly one - so the configuration comes
            // from the deployer rather than being written out a second time
            // here. That second copy has already cost a round trip once.
            var localDeployer = new DeployScriptDeployer(root);
            var localBuilds = new BuildsOnDisk(root, localDeployer.Configuration);

            string why;
            IProductFiles files = handover;
            if (files != null)
            {
                Console.Out.WriteLine("Installing from " + handover.Path + ". Nothing will be downloaded.");
            }
            else
            {
                files = Fetcher(verdict, manifest, localBuilds, releases, out why);
                if (why != null)
                {
                    Console.Error.WriteLine(why);
                    return Refused;
                }
            }

            var deployer = files == null
                ? localDeployer
                : new DeployScriptDeployer(root, localDeployer.Configuration, files);

            // AND THE GREYING FOLLOWS THE ROUTE, as it does in the window: a
            // release with no local build is greyed only when there is nothing
            // to download either.
            var screen = InstallerScreen.Build(manifest,
                                               releases,
                                               revit.RunningRevits(),
                                               new InstalledProductsOnDisk(revit),
                                               files == null ? localBuilds : null);

            if (screen.NothingFound != null)
            {
                Console.Error.WriteLine(screen.NothingFound);
                return Refused;
            }

            List<string> rows;
            if (!Chosen(screen, asked, out rows, out why))
            {
                Console.Error.WriteLine(why);
                return Refused;
            }

            var products = screen.ToInstall(rows);
            var forReleases = screen.ChosenReleases();

            if (products.Count == 0 || forReleases.Count == 0)
            {
                Console.Error.WriteLine(
                    "There is nothing for heron-install to do: " +
                    Nothing(products.Count, forReleases.Count) + Environment.NewLine +
                    "Run heron-install --list to see what is on offer.");
                return Refused;
            }

            Console.Out.WriteLine(Listing(screen, products, forReleases, files, handover));

            if (asked.ListOnly)
            {
                Console.Out.WriteLine();
                Console.Out.WriteLine("--list was asked for, so nothing was changed.");
                return Done;
            }

            if (screen.CloseRevitFirst != null) Console.Out.WriteLine(screen.CloseRevitFirst);

            var engine = new InstallEngine(revit, deployer);
            engine.Pause = delegate { Thread.Sleep(1000); };

            // SAID OUT LOUD, EVERY TIME IT CHANGES. R-38a has the engine wait
            // for Revit to close, and a command that waits ten minutes in
            // silence is one somebody kills. The window shows this in a label;
            // here it is a line.
            var last = "";
            engine.OnWaiting = delegate (string waiting)
            {
                if (waiting == last) return;
                last = waiting;
                Console.Out.WriteLine(waiting);
            };

            // NEVER REMOVALS - see the class comment. Install, not Apply, and
            // Install is the overload that hands Apply a null removal list.
            var report = engine.Install(manifest, products, forReleases);

            Console.Out.WriteLine();
            Console.Out.WriteLine(Told(report, manifest));

            // AFTERWARDS, NEVER BEFORE - R-55, and the ordering is the rule
            // rather than a preference. An update notice printed first reads
            // as a condition of installing, and somebody mid-delivery would
            // take it to mean they should update before carrying on. It is
            // news, not a gate: what they asked for has already happened by
            // the time they read it.
            //
            // AND IT NEVER CHANGES THE EXIT CODE. A newer release existing is
            // not a failure of this run - a caller that treated it as one
            // would start repairing an install that worked.
            SayIfNewer(asked, manifest, files);

            if (report.Abandoned) return CouldNotRun;
            return report.Failed > 0 ? SomethingFailed : Done;
        }

        /// <summary>
        /// Say whether a newer release is published - R-54, R-55, R-56.
        ///
        /// IT READS A VERSION. IT DOES NOT DOWNLOAD HERON TO FIND OUT, which
        /// is R-54 in the owner's own words: *"no need to download again"*.
        /// Only heron-products.json is fetched - a few kilobytes beside 54 MB
        /// of assemblies - and only its version is used.
        ///
        /// IT CANNOT FAIL THE RUN, AND IT CANNOT HANG IT. Offline is the
        /// ordinary case for the machine this was built for (R-53), so a check
        /// that could not reach GitHub says one line and stops. Every
        /// failure is swallowed here ON PURPOSE: the install already happened,
        /// and news about a later version must never be able to turn a
        /// successful install into a failed one.
        ///
        /// SKIPPED WHEN --source WAS USED, because that run just fetched the
        /// newest release. Asking whether the newest is newer than itself is
        /// a network call for an answer already on the screen.
        /// </summary>
        private static void SayIfNewer(Arguments asked, ProductManifest manifest, IProductFiles files)
        {
            if (asked.NoCheck) return;
            if (asked.Source != null) return;
            if (manifest == null || !manifest.KnowsItsSource) return;

            var here = UpdateCheck.HighestVersion(manifest);

            try
            {
                // A SHORT CEILING. Nobody waits a minute to be told there is
                // nothing new, and a blocked corporate network does not answer
                // at all - it just stops replying, which is the case this
                // number exists for.
                using (var look = new ReleaseDownload(
                    ReleaseDownload.LatestUrlFor(manifest.SourceOwner, manifest.SourceRepo),
                    Workspace(), UpdateCheckSeconds))
                {
                    string why;
                    var published = look.Manifest(out why);
                    if (published == null)
                    {
                        Console.Out.WriteLine("Could not check for a newer version: " + why);
                        return;
                    }

                    var verdict = UpdateCheck.Compare(here, UpdateCheck.HighestVersion(published));
                    Console.Out.WriteLine(verdict.Say);
                }
            }
            // NARROW, AND THE FIRST DRAFT WAS WRONG ABOUT WHY.
            //
            // It caught Exception and argued for it: the install is finished,
            // so any fault in fetching news must lose to the install having
            // worked. tests/test_csharp.py went red on the handler balance -
            // the fourth time on this branch - and reading ReleaseDownload
            // showed the argument was false as well as over-broad.
            //
            // Manifest(out why) does not throw for a network failure. Get()
            // maps every one of them to a FetchProblem and returns null with a
            // sentence, and Parse's bad JSON is caught inside it. So there is
            // nothing here for a broad catch to be protecting against EXCEPT a
            // fault in Heron's own code - and D-52's point is that a fault
            // flattened into a tidy line is a fault nobody ever fixes.
            //
            // What can genuinely reach here is the workspace folder: a disk
            // that is full, or one this user may not write to. Those two are
            // caught. Anything else surfaces with its stack, which is worth
            // more than a sentence - the files are already installed by then.
            catch (IOException e)
            {
                Console.Out.WriteLine("Could not check for a newer version (" + e.Message +
                                      "). What was installed is unaffected.");
            }
            catch (UnauthorizedAccessException e)
            {
                Console.Out.WriteLine("Could not check for a newer version (" + e.Message +
                                      "). What was installed is unaffected.");
            }
        }

        /// <summary>
        /// Where the files come from, or null to use what is built here.
        ///
        /// THE THREE CASES, AND ONLY THE MIDDLE ONE IS ROUTE 1.
        ///
        ///   a verdict       the user named a source and InstallSource
        ///                    ACCEPTED it, above, before anything on this PC
        ///                    was touched. This obeys that verdict and builds
        ///                    no URL of its own: the accepted one comes back
        ///                    from the verdict. A refused one never reaches
        ///                    here at all.
        ///
        ///   no verdict       nothing was typed, so there was nothing to
        ///                    judge. What is built here wins; failing that,
        ///                    Heron's own release from its own product list.
        ///                    That is not route 1 with the check skipped -
        ///                    R-48 and R-49 are about a source a user named,
        ///                    and the manifest's `source` block is Heron's
        ///                    own statement of who it is.
        ///
        ///   --from named     handled before this is reached. Q-PE-12.
        /// </summary>
        private static ReleaseDownload Fetcher(SourceVerdict verdict,
                                               ProductManifest manifest,
                                               IProductBuilds localBuilds,
                                               IReadOnlyList<string> releases,
                                               out string why)
        {
            why = null;

            if (verdict != null)
            {
                Console.Out.WriteLine("Fetching from " + verdict.AssetsUrl);
                return new ReleaseDownload(verdict.AssetsUrl, Workspace());
            }

            if (InstallerScreen.AnythingBuilt(manifest, releases, localBuilds)) return null;

            // NULL IS NOT A CRASH AND NOT A GUESS - R-13, the same as in the
            // window. A product list with no source is one nothing can be
            // downloaded from; inventing a repository name here would be the
            // installer deciding where to get software from, which is the one
            // decision it must never make.
            if (!manifest.KnowsItsSource)
            {
                why = "Nothing is built on this PC, and Heron's product list does not say " +
                      "which repository publishes its releases, so there is nowhere to fetch " +
                      "from. Name one with --source, or build Heron here first.";
                return null;
            }

            var url = ReleaseDownload.LatestUrlFor(manifest.SourceOwner, manifest.SourceRepo);
            Console.Out.WriteLine("Nothing is built here, so fetching Heron's own release: " + url);
            return new ReleaseDownload(url, Workspace());
        }

        /// <summary>
        /// Somewhere to unpack into, under the user's own temp.
        ///
        /// NOT BESIDE THE EXECUTABLE. A modeller's Downloads folder is not
        /// somewhere to unpack 24 assemblies into, and on a managed laptop the
        /// folder the exe sits in may not be writable at all.
        /// </summary>
        private static string Workspace()
        {
            var work = Path.Combine(Path.GetTempPath(), "Heron", "download");
            Directory.CreateDirectory(work);
            return work;
        }

        /// <summary>
        /// Turn --products and --releases into the screen's own choices.
        ///
        /// IT REFUSES BY NAME - R-14. A caller who asked for a product that
        /// is not there, or for a Revit that is not on this PC, gets told
        /// which one and what there is instead. Quietly installing the subset
        /// that happened to match is how somebody believes they installed
        /// something they did not.
        ///
        /// AND A GREYED ROW IS REFUSED WITH ITS OWN REASON, the same sentence
        /// the window prints under it, rather than a second one written here.
        /// </summary>
        private static bool Chosen(InstallerScreen screen,
                                   Arguments asked,
                                   out List<string> rows,
                                   out string why)
        {
            rows = new List<string>();
            why = null;

            foreach (var row in screen.Products)
            {
                if (asked.Products.Count == 0)
                {
                    if (row.CanBeTicked && !row.IsHeading) rows.Add(row.Id);
                    continue;
                }

                if (!Names(asked.Products, row.Id)) continue;

                if (!row.CanBeTicked)
                {
                    why = "'" + row.Id + "' cannot be installed. " + row.WhyNot;
                    return false;
                }
                rows.Add(row.Id);
            }

            foreach (var wanted in asked.Products)
            {
                if (Known(screen, wanted)) continue;
                why = "There is no Heron product called '" + wanted + "'." +
                      Environment.NewLine + "There is: " + Offered(screen);
                return false;
            }

            foreach (var choice in screen.Releases)
            {
                if (asked.Releases.Count == 0)
                {
                    choice.Chosen = choice.CanBeTicked;
                    continue;
                }

                var wanted = Names(asked.Releases, choice.Release);
                if (wanted && !choice.CanBeTicked)
                {
                    why = "Revit " + choice.Release + " cannot be installed into. " + choice.WhyNot;
                    return false;
                }
                choice.Chosen = wanted;
            }

            foreach (var wanted in asked.Releases)
            {
                if (Found(screen, wanted)) continue;
                why = "Revit " + wanted + " was not found on this PC." +
                      Environment.NewLine + "What is here: " + Present(screen);
                return false;
            }

            return true;
        }

        private static bool Names(IReadOnlyList<string> wanted, string id)
        {
            foreach (var one in wanted)
                if (string.Equals(one, id, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static bool Known(InstallerScreen screen, string id)
        {
            foreach (var row in screen.Products)
                if (string.Equals(row.Id, id, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static bool Found(InstallerScreen screen, string release)
        {
            foreach (var choice in screen.Releases)
                if (string.Equals(choice.Release, release, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string Offered(InstallerScreen screen)
        {
            var names = new List<string>();
            foreach (var row in screen.Products)
                if (!row.IsHeading) names.Add(row.Id);
            return names.Count == 0 ? "nothing" : string.Join(", ", names.ToArray());
        }

        private static string Present(InstallerScreen screen)
        {
            var names = new List<string>();
            foreach (var choice in screen.Releases) names.Add(choice.Release);
            return names.Count == 0 ? "no Revit at all" : string.Join(", ", names.ToArray());
        }

        private static string Nothing(int products, int releases)
        {
            if (products == 0 && releases == 0)
                return "nothing was chosen to install, and no Revit release was chosen either.";
            if (products == 0)
                return "nothing was chosen to install.";
            return "no Revit release was chosen.";
        }

        /// <summary>
        /// What is about to happen, before it happens.
        ///
        /// PRINTED WHETHER OR NOT --list WAS ASKED FOR. The window shows the
        /// ticks; here the only way to see what a command is about to do is to
        /// be told, and being told afterwards is too late to stop it.
        /// </summary>
        private static string Listing(InstallerScreen screen,
                                      IReadOnlyList<string> products,
                                      IReadOnlyList<string> releases,
                                      IProductFiles files,
                                      ProductFolder handover)
        {
            var n = Environment.NewLine;
            var said = "Installing into Revit " + string.Join(", ", Array(releases)) + ":" + n;

            foreach (var id in products)
            {
                var name = id;
                foreach (var row in screen.Products)
                    if (row.Id == id && !string.IsNullOrEmpty(row.Name)) name = row.Name;
                said += "  " + name + "  (" + id + ")" + n;
            }

            said += n + "Into " + InstallerScreen.InstallLocation + "  " +
                    InstallerScreen.InstallLocationNote + n;
            if (handover != null)
            {
                said += "Files come from " + handover.Path + ", and each one is checked against " +
                        "the checksum published beside it before anything is written. " +
                        "Nothing is downloaded.";
            }
            else if (files != null)
            {
                said += "Files come from Heron's published release, and each one is checked " +
                        "against its published checksum before anything is written.";
            }
            else
            {
                said += "Files come from what is built on this PC.";
            }
            return said;
        }

        /// <summary>
        /// What happened, in the order somebody reads it: the bad news first.
        ///
        /// IT NEVER SAYS ONLY A NUMBER. "2 installed, 1 failed" leaves a
        /// caller with nothing to act on; every failure names its product,
        /// its release and what the deployer said.
        /// </summary>
        private static string Told(InstallReport report, ProductManifest manifest)
        {
            var n = Environment.NewLine;
            var said = "";

            if (report.Abandoned)
            {
                return "Nothing was changed. " + report.AbandonedBecause;
            }

            foreach (var result in report.Results)
            {
                if (result.Succeeded) continue;
                said += "FAILED  " + result.ProductName + " for Revit " + result.Release +
                        n + "        " + result.Message + n;
            }

            foreach (var skipped in report.Skipped)
                said += "skipped " + skipped.ProductId + " for Revit " + skipped.Release +
                        n + "        " + skipped.Explanation + n;

            foreach (var result in report.Results)
            {
                if (!result.Succeeded) continue;
                said += "done    " + result.ProductName + " for Revit " + result.Release + n;
            }

            said += n + report.Installed + " installed, " + report.Failed + " failed, " +
                    report.Skipped.Count + " skipped.";

            if (report.Failed == 0 && report.Installed > 0)
            {
                var tabs = TabsFor(report, manifest);
                said += n + (tabs == null
                    ? "Start Revit to see it."
                    : "Start Revit and look for " + tabs + ".");
            }

            return said;
        }

        /// <summary>
        /// The ribbon tabs the products that installed will appear on, READ
        /// FROM THE MANIFEST, or null when it does not say.
        ///
        /// THE FIRST DRAFT TYPED "the Heron tab", AND THAT IS A KNOWN BUG IN
        /// THIS REPOSITORY RATHER THAN A STYLE POINT. D-85 renamed the tab on
        /// 2026-09-20 - "Heron AI" to "Heron" - and its own consequences
        /// section counted EIGHT printed strings that had to move with it. One
        /// did not: tools/setup.ps1 line 183 went on sending every new user to
        /// a tab that does not exist, for two days, until row 5b-138 caught it
        /// (#273). A typed tab name is a ninth string waiting to do the same.
        ///
        /// SO IT IS NOT TYPED. `tab` is a field on every product in
        /// heron-products.json and HeronProduct.Tab already reads it, so
        /// renaming a tab stays a line in a file - R-3, the same rule that
        /// keeps product names out of this door.
        ///
        /// AND IT IS MORE USE THIS WAY. Install Heron Doc and it says Heron
        /// Doc, rather than sending somebody to a tab their install did not
        /// create.
        ///
        /// NULL WHEN THE MANIFEST DOES NOT SAY, and the caller then names no
        /// tab at all. Guessing one is how this bug happens; saying "start
        /// Revit" and nothing more is merely less helpful.
        /// </summary>
        private static string TabsFor(InstallReport report, ProductManifest manifest)
        {
            var tabs = new List<string>();
            foreach (var result in report.Results)
            {
                if (!result.Succeeded || result.Removed) continue;
                foreach (var product in manifest.Products)
                {
                    if (product.Id != result.ProductId) continue;
                    if (string.IsNullOrEmpty(product.Tab)) continue;
                    if (!tabs.Contains(product.Tab)) tabs.Add(product.Tab);
                }
            }

            if (tabs.Count == 0) return null;
            if (tabs.Count == 1) return "the " + tabs[0] + " tab";
            return "these tabs: " + string.Join(", ", tabs.ToArray());
        }

        private static string[] Array(IReadOnlyList<string> from)
        {
            var made = new string[from.Count];
            for (var i = 0; i < from.Count; i++) made[i] = from[i];
            return made;
        }

        /// <summary>
        /// The folder holding platform\ and tools\.
        ///
        /// Beside the executable in a release, and further up in a build tree.
        /// Both are tried rather than assumed, because assuming the wrong one
        /// produces "the product list is missing" on a machine where it is not.
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
    }
}
