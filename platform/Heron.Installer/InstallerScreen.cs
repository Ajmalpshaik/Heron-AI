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

        /// <summary>
        /// False greys the box out. Never hidden, never dropped - a release
        /// that vanished from the list would read as a Revit Heron could not
        /// find, which is a different and wrong answer.
        /// </summary>
        public bool CanBeTicked { get; internal set; }

        /// <summary>
        /// Why it is greyed out, in plain English, or null when it is not.
        ///
        /// SAID BEFORE INSTALL IS PRESSED - R-10, and the reason this field
        /// exists at all. A release with no build on this PC could be ticked
        /// until 2026-09-21, and the only way to find out was to press
        /// Install and read the refusal. The owner hit that five times in one
        /// evening. The product rows have carried their reason since Stage 4;
        /// this is the same rule applied to the row above them.
        /// </summary>
        public string WhyNot { get; internal set; }
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
        /// The releases this product's files are actually on this PC for.
        ///
        /// KEPT AS A LIST RATHER THAN ROLLED INTO `State`, because uninstall
        /// needs to know WHICH ones - R-21 removes a product from the releases
        /// it is on, and "Installed for Revit 2024" is a sentence rather than
        /// an answer.
        /// </summary>
        public IReadOnlyList<string> InstalledFor { get; internal set; }

        /// <summary>
        /// Whether this row starts TICKED, and it is a safety property rather
        /// than a convenience.
        ///
        /// R-21 SAYS UNTICKING A PRODUCT UNINSTALLS IT. Every product row
        /// used to start empty, so a user who opened this window and pressed
        /// Install without touching anything would have been unticking
        /// everything they had - and the press that was meant to install
        /// would have removed the lot.
        ///
        /// So what is installed starts ticked. Unticking is then something a
        /// person did on purpose, which is the only ground on which a delete
        /// may be offered at all.
        /// </summary>
        public bool Chosen { get; internal set; }

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
    /// Whether there is anything to install for a product and a release -
    /// asked BEFORE Install is pressed.
    ///
    /// WHY THIS IS A QUESTION AND NOT AN ACTION. The window must never build
    /// anything: a build takes minutes, needs an SDK a modeller has no reason
    /// to own, and a window that starts one on opening is a window that looks
    /// frozen. So it asks the only thing that costs nothing - is the built
    /// file already on this disk - and greys the release when the answer is no.
    ///
    /// IT MUST ASK EXACTLY WHAT THE DEPLOY SCRIPT ASKS. tools\deploy-addin.ps1
    /// is what actually installs, and if this looked in a different place the
    /// window would grey a release the script would have installed happily -
    /// which is worse than the defect it fixes. The implementation mirrors
    /// that script's own search, including its configuration.
    /// </summary>
    public interface IProductBuilds
    {
        /// <summary>A build for that product and release is on this PC now.</summary>
        bool HasBuild(HeronProduct product, string release);

        /// <summary>
        /// The one command that makes it, for the sentence on the greyed row.
        ///
        /// DERIVED FROM THE PRODUCT, never written out: R-3 says adding a
        /// product is a line in the manifest, and a command with a project
        /// name typed into it would be that rule broken in the one place
        /// nobody looks - a sentence.
        /// </summary>
        string BuildCommand(HeronProduct product, string release);
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
        /// The product list this screen was built from.
        ///
        /// KEPT so ToRemove can hand the engine a real product rather than an
        /// id it would have to look up again. A row is what is drawn; a
        /// HeronProduct is what gets installed or removed, and turning one
        /// into the other twice in two places is how they come to disagree.
        /// </summary>
        private ProductManifest _manifest;

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
        /// <param name="builds">
        /// Which releases there is anything to install FOR, or null when that
        /// could not be found out.
        ///
        /// NULL LEAVES EVERY RELEASE TICKABLE, and that is the deliberate
        /// direction to be wrong in. Greying a release that could have been
        /// installed costs a modeller an install they were entitled to, with
        /// a sentence telling them to build something that is already built.
        /// Leaving one tickable costs a refusal after the press - which is
        /// exactly where this stood before, so it is no worse than nothing.
        /// </param>
        public static InstallerScreen Build(ProductManifest manifest,
                                            IEnumerable<string> installedReleases,
                                            IEnumerable<RunningRevit> openRevits,
                                            IInstalledProducts installed,
                                            IProductBuilds builds)
        {
            if (manifest == null) throw new ArgumentNullException("manifest");

            var screen = new InstallerScreen();
            screen._manifest = manifest;
            var releases = Sorted(installedReleases);

            var choices = new List<ReleaseChoice>();
            foreach (var release in releases)
                choices.Add(Choice(manifest, release, builds));
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
            foreach (var r in Releases)
            {
                // A GREYED RELEASE INSTALLS NOTHING EVEN IF ITS TICK ARRIVES
                // SET, exactly as ToInstall above says of a greyed product
                // row. The window should not be able to send one, and this
                // is the place that makes that true rather than trusting it.
                if (!r.CanBeTicked) continue;
                if (r.Chosen) chosen.Add(r.Release);
            }
            return chosen;
        }

        // -------------------------------------------------------- releases
        /// <summary>
        /// One release tick box, and whether there is anything to put in it.
        ///
        /// THE RULE, AND WHY IT IS THIS ONE. A release is greyed when every
        /// product that could be installed into it has no build on this PC.
        /// One product with a build is enough to keep it tickable, because
        /// that install really would work - the rows below say what else
        /// would not.
        ///
        /// A RELEASE NO PRODUCT SUPPORTS IS LEFT ALONE. It cannot be
        /// installed either, but the honest reason is not "nothing has been
        /// built" and the product rows already carry the right one. Answering
        /// the wrong question loudly is how a window teaches a modeller to
        /// stop reading it.
        /// </summary>
        private static ReleaseChoice Choice(ProductManifest manifest,
                                            string release,
                                            IProductBuilds builds)
        {
            var choice = new ReleaseChoice
            {
                Release = release,
                Chosen = true,
                CanBeTicked = true,
            };

            if (builds == null) return choice;

            var missing = new List<HeronProduct>();
            foreach (var product in manifest.Products)
            {
                // A HEADING HAS NO FILES OF ITS OWN, so it has no build to
                // look for - D-93. Its pieces are in this same list and are
                // asked about on their own account.
                if (product.IsHeading) continue;
                if (!product.MayBeOffered) continue;
                if (!product.SupportsRevit(release)) continue;

                if (builds.HasBuild(product, release)) return choice;
                missing.Add(product);
            }

            if (missing.Count == 0) return choice;

            var how = new List<string>();
            foreach (var product in missing)
            {
                var command = builds.BuildCommand(product, release);
                if (!string.IsNullOrEmpty(command) && !how.Contains(command))
                    how.Add(command);
            }

            choice.CanBeTicked = false;
            choice.Chosen = false;
            choice.WhyNot =
                "Nothing has been built for Revit " + release + " on this PC, so " +
                "there is nothing to install into it. Ticking it would only get " +
                "as far as the same answer after pressing Install." +
                (how.Count == 0
                    ? " Build it first, then open this window again."
                    : " Build it first, then open this window again:" +
                      Environment.NewLine + "    " +
                      string.Join(Environment.NewLine + "    ", how.ToArray()));

            return choice;
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
                InstalledFor = Present(offerable, releases, installed),
            };

            // A HEADING IS TICKED WHEN EVERY PIECE UNDER IT IS. Ticked while
            // one piece was missing would read as "all of this is here", and
            // the roll-up below would then carry that piece into an install
            // the user did not ask for.
            row.Chosen = row.CanBeTicked
                         && row.InstalledFor.Count > 0
                         && row.InstalledFor.Count == releases.Count;

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
                InstalledFor = Present(new[] { product }, releases, installed),
            };

            row.Chosen = row.CanBeTicked && row.InstalledFor.Count > 0;

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

        /// <summary>
        /// Which of the releases on this PC these products are all present on.
        ///
        /// ALL OF THEM, NOT ANY. A heading is installed for a release only
        /// when every piece under it is, for the same reason its tick is only
        /// set then.
        /// </summary>
        private static IReadOnlyList<string> Present(IList<HeronProduct> products,
                                                     IList<string> releases,
                                                     IInstalledProducts installed)
        {
            var found = new List<string>();
            if (installed == null || products.Count == 0) return found;

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

            return found;
        }

        /// <summary>
        /// What an apply would REMOVE - R-21, uninstall is the same window.
        ///
        /// A product that is on this PC and is no longer ticked. Nothing else
        /// qualifies: a row that was never installed has nothing to remove,
        /// and a row that is still ticked is being kept or replaced.
        ///
        /// THE RELEASES ARE THE ONES IT IS ACTUALLY ON, not the ones ticked
        /// at the top of the window. Unticking a product means "I do not want
        /// this", not "I do not want this on the releases I happen to have
        /// highlighted" - and removing it from two of three Revits would
        /// leave a state nobody asked for and nothing would say so.
        /// </summary>
        public IReadOnlyList<InstallStep> ToRemove(IEnumerable<string> tickedRowIds)
        {
            var going = new List<InstallStep>();
            var ticked = tickedRowIds == null
                ? new List<string>()
                : new List<string>(tickedRowIds);

            foreach (var row in Products)
            {
                // A HEADING REMOVES NOTHING OF ITS OWN, exactly as it installs
                // nothing of its own - D-93. Its pieces are rows in this same
                // list and are asked about on their own account.
                if (row.IsHeading) continue;
                if (ticked.Contains(row.Id)) continue;
                if (row.InstalledFor == null) continue;

                var product = _manifest.Find(row.Id);
                if (product == null) continue;

                foreach (var release in row.InstalledFor)
                    going.Add(new InstallStep { Product = product, Release = release });
            }

            return going;
        }

        /// <summary>
        /// What to ask before anything is deleted, or null when nothing is.
        ///
        /// A SAFETY GATE, NOT AN INFORMATION MESSAGE. The house rule is that
        /// anything which deletes must confirm first and say what will happen
        /// and to how much - and this is one of the very few dialogs Heron is
        /// allowed to show at all.
        ///
        /// IT NAMES EVERY PRODUCT AND EVERY RELEASE, rather than counting
        /// them. "3 items will be removed" is a number somebody clicks past;
        /// "'AI Bridge connector' from Revit 2020, 2024 and 2027" is a list
        /// they can check against what they meant to do.
        ///
        /// AND IT SAYS WHAT SURVIVES. R-22: the product's files go and the
        /// user's own data does not. Somebody about to uninstall is somebody
        /// worried about losing work, and the sentence that answers that has
        /// to be in front of them at the moment they decide.
        /// </summary>
        public static string WhatWillBeRemoved(IEnumerable<InstallStep> removals)
        {
            if (removals == null) return null;

            var byProduct = new List<string>();
            var names = new List<string>();
            var releasesOf = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var step in removals)
            {
                if (step == null || step.Product == null) continue;
                var name = step.Product.Name ?? step.Product.Id;
                if (!releasesOf.ContainsKey(name))
                {
                    releasesOf[name] = new List<string>();
                    names.Add(name);
                }
                if (!releasesOf[name].Contains(step.Release)) releasesOf[name].Add(step.Release);
            }

            if (names.Count == 0) return null;

            foreach (var name in names)
                byProduct.Add("'" + name + "' from Revit " + Join(releasesOf[name]));

            return "This will REMOVE " + string.Join("; ", byProduct.ToArray()) + "." +
                   Environment.NewLine + Environment.NewLine +
                   "Restart Revit afterwards to unload it." +
                   Environment.NewLine + Environment.NewLine +
                   "Your own Heron data is not touched - settings, the audit log and " +
                   "anything Heron has learned all stay where they are.";
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
