// Heron-Agent:  HERON-INS-ORC-001, HERON-INS-PKG-012
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;

namespace Heron.Installer
{
    /// <summary>One Revit release the user can tick.</summary>
    public sealed class ReleaseChoice
    {
        public string Release { get; internal set; }

        /// <summary>
        /// Ticked. Every release found on the PC starts ticked, because the
        /// common case is a modeller who wants Heron in all of their Revits
        /// and the uncommon one is easier to say by unticking than by
        /// hunting for the right box.
        /// </summary>
        public bool Chosen { get; set; }
    }

    /// <summary>One line of the product list, as it appears on screen.</summary>
    public sealed class ProductRow
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string Description { get; internal set; }

        /// <summary>Indented under its heading - the AI Bridge under Heron.</summary>
        public bool IsPiece { get; internal set; }

        /// <summary>
        /// A tab built by its pieces, so its tick is a roll-up for the rows
        /// underneath it and installs nothing of its own - D-93, R-33.
        /// </summary>
        public bool IsHeading { get; internal set; }

        /// <summary>The product ids this row's tick actually installs.</summary>
        public IReadOnlyList<string> Installs { get; internal set; }

        /// <summary>False greys the row out. Never hidden, never dropped.</summary>
        public bool CanBeTicked { get; internal set; }

        /// <summary>
        /// Why it is greyed out, in plain English, or null when it is not.
        ///
        /// R-10: the reason is ON THE ROW. AJ Tools' lesson L3 is this failure
        /// seen from the other end - its installer quietly installed nothing
        /// at all on three releases, reported no error, and nobody noticed for
        /// months. A row that cannot be ticked and does not say why is the
        /// same silence with a tick box in front of it.
        /// </summary>
        public string WhyNot { get; internal set; }

        /// <summary>`Installed`, `Installed for Revit 2024`, or `--`.</summary>
        public string State { get; internal set; }

        /// <summary>
        /// Said on a row that is already installed, BEFORE Install is pressed
        /// - R-23a and Stage 4 item 3b. There is no Update button and no
        /// Repair button, so this is the only place a user is told what
        /// pressing Install on something already there does. Without it they
        /// are left guessing whether they are about to end up with two.
        /// </summary>
        public string IfYouInstallAgain { get; internal set; }
    }

    /// <summary>Whether a product's files are on this PC for a release.</summary>
    public interface IInstalledProducts
    {
        bool IsInstalled(HeronProduct product, string release);
    }

    /// <summary>
    /// Everything the installer window shows, worked out with no window.
    ///
    /// WHY THIS IS NOT IN THE WINDOW. A window cannot be run on a machine with
    /// no Windows, and every rule below is a rule about what a modeller is
    /// offered: which product may be ticked, which is greyed out, and what the
    /// grey says. Written in XAML or in a code-behind they would be unprovable
    /// here, and the hardest part of this stage would rest on somebody looking
    /// at a screenshot and thinking it looked right.
    ///
    /// NOTHING IN THIS FILE NAMES A PRODUCT - R-3, D-93. Adding Heron
    /// Structure next year is a line in platform/heron-products.json and never
    /// a rebuild of the installer. If a product id appears anywhere in this
    /// assembly outside a test, that rule is already broken.
    /// </summary>
    public sealed class InstallerScreen
    {
        /// <summary>Where the files go, and the promise that comes with it.</summary>
        public const string InstallLocation = @"%APPDATA%\Autodesk\Revit\Addins\";

        public const string InstallLocationNote = "(per user - no administrator needed)";

        public IReadOnlyList<ReleaseChoice> Releases { get; private set; }
        public IReadOnlyList<ProductRow> Products { get; private set; }

        /// <summary>
        /// Said BEFORE Install is pressed, never after it fails - Stage 4
        /// item 6. Closing Revit is the one thing the user has to do, and the
        /// one thing they will not think of unless they are told.
        /// </summary>
        public string CloseRevitFirst { get; private set; }

        /// <summary>
        /// No Revit at all is a sentence, not an empty list. A window offering
        /// nothing, with no reason given, reads as broken software.
        /// </summary>
        public string NothingFound { get; private set; }

        public bool AnythingToOffer
        {
            get
            {
                foreach (var row in Products) if (row.CanBeTicked) return true;
                return false;
            }
        }

        /// <summary>
        /// Build the screen.
        /// </summary>
        /// <param name="manifest">The product list, read as data.</param>
        /// <param name="installedReleases">Which Revits are on this PC.</param>
        /// <param name="openRevits">Which are open right now, or null.</param>
        /// <param name="installed">Which products are already there, or null.</param>
        public static InstallerScreen Build(ProductManifest manifest,
                                            IEnumerable<string> installedReleases,
                                            IEnumerable<RunningRevit> openRevits,
                                            IInstalledProducts installed)
        {
            if (manifest == null) throw new ArgumentNullException("manifest");

            var screen = new InstallerScreen();
            var releases = Sorted(installedReleases);

            var choices = new List<ReleaseChoice>();
            foreach (var release in releases)
                choices.Add(new ReleaseChoice { Release = release, Chosen = true });
            screen.Releases = choices;

            screen.NothingFound = releases.Count > 0 ? null
                : "No Revit was found on this PC. Heron installs into Revit, so " +
                  "there is nothing for it to install into. Install Revit first, " +
                  "then run this again.";

            screen.Products = Rows(manifest, releases, installed);
            screen.CloseRevitFirst = Warning(openRevits);
            return screen;
        }

        /// <summary>
        /// The product ids to hand the engine, for a set of ticked rows.
        ///
        /// A HEADING'S TICK BECOMES ITS PIECES. The engine installs products
        /// and a heading is not one, so the roll-up is resolved here rather
        /// than sent down to be skipped with a reason nobody asked for.
        /// </summary>
        public IReadOnlyList<string> ToInstall(IEnumerable<string> tickedRowIds)
        {
            var chosen = new List<string>();
            if (tickedRowIds == null) return chosen;

            var ticked = new List<string>(tickedRowIds);
            foreach (var row in Products)
            {
                // A GREYED ROW INSTALLS NOTHING EVEN IF ITS TICK ARRIVES SET.
                // The window should not be able to send one, and this is the
                // place that makes that true rather than trusting it.
                if (!row.CanBeTicked) continue;
                if (!ticked.Contains(row.Id)) continue;
                foreach (var id in row.Installs)
                    if (!chosen.Contains(id)) chosen.Add(id);
            }
            return chosen;
        }

        public IReadOnlyList<string> ChosenReleases()
        {
            var chosen = new List<string>();
            foreach (var r in Releases) if (r.Chosen) chosen.Add(r.Release);
            return chosen;
        }

        // ------------------------------------------------------------- rows
        private static IReadOnlyList<ProductRow> Rows(ProductManifest manifest,
                                                      IList<string> releases,
                                                      IInstalledProducts installed)
        {
            var rows = new List<ProductRow>();

            foreach (var product in manifest.Products)
            {
                // A PIECE IS DRAWN UNDER ITS HEADING, not wherever the file
                // happens to list it. The indent is the only thing on screen
                // that says ticking the tools WITHOUT the connector is a
                // supported install rather than half of one - R-34.
                if (!string.IsNullOrEmpty(product.PartOf)) continue;

                if (product.IsHeading)
                {
                    var pieces = manifest.PiecesOf(product.Id);
                    rows.Add(Heading(product, pieces, releases, installed));
                    foreach (var piece in pieces)
                        rows.Add(Row(piece, releases, installed, true));
                    continue;
                }

                rows.Add(Row(product, releases, installed, false));
            }

            return rows;
        }

        private static ProductRow Heading(HeronProduct heading,
                                          IReadOnlyList<HeronProduct> pieces,
                                          IList<string> releases,
                                          IInstalledProducts installed)
        {
            // ONLY THE PIECES THAT CAN ACTUALLY BE INSTALLED. A heading tick
            // that carried a piece nobody may have would install it anyway,
            // through a tick box the user never saw set.
            var ids = new List<string>();
            var offerable = new List<HeronProduct>();
            foreach (var piece in pieces)
            {
                if (!CanOffer(piece, releases)) continue;
                offerable.Add(piece);
                ids.Add(piece.Id);
            }

            var row = new ProductRow
            {
                Id = heading.Id,
                Name = heading.Name,
                Description = heading.Description,
                IsHeading = true,
                IsPiece = false,
                Installs = ids,
                CanBeTicked = ids.Count > 0,
                State = StateOf(offerable, releases, installed),
            };

            if (!row.CanBeTicked)
                row.WhyNot = "None of the pieces of the " + heading.Name +
                             " tab can be installed on this PC yet. The rows " +
                             "below say why.";

            row.IfYouInstallAgain = Replaces(row.State);
            return row;
        }

        private static ProductRow Row(HeronProduct product,
                                      IList<string> releases,
                                      IInstalledProducts installed,
                                      bool isPiece)
        {
            var row = new ProductRow
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                IsPiece = isPiece,
                IsHeading = false,
                Installs = new[] { product.Id },
                CanBeTicked = CanOffer(product, releases),
                State = StateOf(new[] { product }, releases, installed),
            };

            if (!row.CanBeTicked) row.WhyNot = WhyNotOffered(product, releases);
            row.IfYouInstallAgain = Replaces(row.State);
            return row;
        }

        private static bool CanOffer(HeronProduct product, IList<string> releases)
        {
            if (!product.MayBeOffered) return false;
            foreach (var release in releases)
                if (product.SupportsRevit(release)) return true;
            return false;
        }

        /// <summary>
        /// The sentence on a greyed row. It says what, and where there is
        /// anything to do about it, what to do - docs/14. "Error" is never
        /// one of the words, and neither is a state name: SHIPPED, PROVING
        /// and PLANNED are this repository's vocabulary, not a modeller's.
        /// </summary>
        private static string WhyNotOffered(HeronProduct product, IList<string> releases)
        {
            if (!product.MayBeOffered)
            {
                // STATE FIRST, because it is the stronger reason. Telling
                // somebody which Revit releases a product that does not exist
                // yet would support is an answer to a question they cannot
                // have asked.
                return string.Equals(product.State, "PLANNED", StringComparison.Ordinal)
                    ? "Not built yet. It is in the plan, and it will appear here " +
                      "on its own when it is ready - this window reads the list " +
                      "rather than carrying it."
                    : "Not ready yet. It is being built and tested, and " +
                      "installing it now would put a button in your ribbon that " +
                      "does nothing.";
            }

            if (releases.Count == 0)
                return "No Revit was found on this PC.";

            var supports = product.Revit == null || product.Revit.Count == 0
                ? "no Revit release yet"
                : "Revit " + Join(product.Revit);

            return "'" + product.Name + "' does not run on the Revit found here (" +
                   Join(releases) + "). It supports " + supports + ".";
        }

        private static string StateOf(IList<HeronProduct> products,
                                      IList<string> releases,
                                      IInstalledProducts installed)
        {
            if (installed == null || releases.Count == 0 || products.Count == 0)
                return "--";

            var found = new List<string>();
            foreach (var release in releases)
            {
                var all = true;
                foreach (var product in products)
                {
                    if (!product.SupportsRevit(release)) { all = false; break; }
                    if (!installed.IsInstalled(product, release)) { all = false; break; }
                }
                if (all) found.Add(release);
            }

            if (found.Count == 0) return "--";
            if (found.Count == releases.Count) return "Installed";
            return "Installed for Revit " + Join(found);
        }

        /// <summary>
        /// R-23a, Stage 4 item 3b. Install replaces whatever is there, and
        /// there is no Update button and no Repair button to say so instead.
        /// </summary>
        private static string Replaces(string state)
        {
            if (string.IsNullOrEmpty(state) || state == "--") return null;
            return "Already installed. Pressing Install replaces it - there is no " +
                   "separate Update or Repair.";
        }

        private static string Warning(IEnumerable<RunningRevit> openRevits)
        {
            const string always =
                "Close Revit before installing. Heron cannot replace a file that " +
                "Revit has open, so it waits for you rather than working around it.";

            if (openRevits == null) return always;

            var open = new List<string>();
            var unknown = false;
            foreach (var revit in openRevits)
            {
                if (revit == null) continue;
                if (revit.Release == null) { unknown = true; continue; }
                if (!open.Contains(revit.Release)) open.Add(revit.Release);
            }

            if (open.Count > 0) return "Revit " + Join(open) + " is open right now. " + always;
            if (unknown) return "A Revit is open right now. " + always;
            return always;
        }

        // ------------------------------------------------------------ small
        private static List<string> Sorted(IEnumerable<string> releases)
        {
            var sorted = new List<string>();
            if (releases == null) return sorted;
            foreach (var r in releases)
                if (!string.IsNullOrEmpty(r) && !sorted.Contains(r)) sorted.Add(r);
            sorted.Sort(StringComparer.Ordinal);
            return sorted;
        }

        private static string Join(IEnumerable<string> items)
        {
            var list = new List<string>(items);
            if (list.Count == 0) return "none";
            if (list.Count == 1) return list[0];
            return string.Join(", ", list.ToArray(), 0, list.Count - 1)
                   + " and " + list[list.Count - 1];
        }
    }
}
