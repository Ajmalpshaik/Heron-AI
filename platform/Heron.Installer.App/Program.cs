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
            var download = AnythingBuilt(manifest, releases, localBuilds)
                ? null
                : Downloader(manifest, root);

            var deployer = download == null
                ? localDeployer
                : new DeployScriptDeployer(root, localDeployer.Configuration, download);

            // AND THE GREYING FOLLOWS THE ROUTE. A release with no local build
            // is greyed only when there is nothing to download either -
            // otherwise the window would refuse an install that would have
            // worked, which is worse than the defect that rule was added for.
            var screen = InstallerScreen.Build(manifest,
                                               releases,
                                               revit.RunningRevits(),
                                               installed,
                                               download == null ? localBuilds : null);

            var window = new InstallerWindow(screen, VersionOf(manifest));
            window.InstallPressed += delegate (IReadOnlyList<string> products,
                                               IReadOnlyList<string> releases)
            {
                Install(window, manifest, revit, deployer, products, releases);
            };

            return new Application().Run(window);
        }

        /// <summary>
        /// Off the window's thread, because the engine WAITS - R-38a. A wait
        /// on the UI thread is a frozen window, and a frozen window is one a
        /// user kills halfway through an install.
        /// </summary>
        private static void Install(InstallerWindow window,
                                    ProductManifest manifest,
                                    IRevitEnvironment revit,
                                    IProductDeployer deployer,
                                    IReadOnlyList<string> products,
                                    IReadOnlyList<string> releases)
        {
            var engine = new InstallEngine(revit, deployer);
            engine.Pause = delegate { Thread.Sleep(1000); };
            engine.OnWaiting = delegate (string why)
            {
                window.Dispatcher.Invoke(delegate { window.Waiting(why); });
            };

            var worker = new Thread(delegate ()
            {
                var report = engine.Install(manifest, products, releases);
                window.Dispatcher.Invoke(delegate { window.Report(report); });
            });
            worker.IsBackground = true;
            worker.Start();
        }

        /// <summary>
        /// Whether anything at all has been built here, for any release.
        ///
        /// ONE BUILD IS ENOUGH to say this is a checkout somebody works in,
        /// and the deploy script's own refusal covers the releases that are
        /// missing - by name, with the command that fixes them. Downloading
        /// on top of a half-built checkout would install a published version
        /// over the one the developer just compiled, which is the opposite of
        /// what they asked for.
        /// </summary>
        private static bool AnythingBuilt(ProductManifest manifest,
                                          IReadOnlyList<string> releases,
                                          IProductBuilds builds)
        {
            foreach (var product in manifest.Products)
            {
                if (product.IsHeading || !product.MayBeOffered) continue;
                foreach (var release in releases)
                    if (builds.HasBuild(product, release)) return true;
            }
            return false;
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
