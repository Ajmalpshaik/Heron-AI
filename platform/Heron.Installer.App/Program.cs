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
            var deployer = new DeployScriptDeployer(root);
            var installed = new InstalledProductsOnDisk(revit);

            // ASKED OF THE DEPLOYER, NEVER TYPED AGAIN. The window greys a
            // release nothing has been built for, and "built" only means
            // anything alongside a configuration - Debug and Release are two
            // different folders and the deploy script reads exactly one of
            // them. Writing "Release" here as well would be a second copy of
            // a word that has already cost a round trip once.
            var builds = new BuildsOnDisk(root, deployer.Configuration);

            var screen = InstallerScreen.Build(manifest,
                                               revit.InstalledReleases(),
                                               revit.RunningRevits(),
                                               installed,
                                               builds);

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
