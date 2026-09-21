// Heron-Agent:  HERON-INS-ORC-001
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;

namespace Heron.Installer
{
    /// <summary>Why a product and release pair is not going to be installed.</summary>
    public enum SkipReason
    {
        None = 0,

        /// <summary>No product in the manifest carries that id.</summary>
        NoSuchProduct,

        /// <summary>A heading is a tab built by its pieces. Tick the pieces.</summary>
        IsAHeading,

        /// <summary>PROVING or PLANNED - it exists, but not for a user.</summary>
        NotOfferable,

        /// <summary>The product's own `revit` list does not carry this release.</summary>
        ReleaseNotSupported,

        /// <summary>Revit is on the machine, but the user did not tick it.</summary>
        ReleaseNotChosen,

        /// <summary>The release is not installed on this PC at all.</summary>
        ReleaseNotInstalled,
    }

    /// <summary>One product, one Revit release - the unit an install succeeds or fails at.</summary>
    public sealed class InstallStep
    {
        public HeronProduct Product { get; internal set; }
        public string Release { get; internal set; }

        public override string ToString()
        {
            return Product.Id + " for Revit " + Release;
        }
    }

    /// <summary>A pair that will NOT be installed, and the sentence saying why.</summary>
    public sealed class SkippedStep
    {
        public string ProductId { get; internal set; }
        public string Release { get; internal set; }
        public SkipReason Reason { get; internal set; }

        /// <summary>
        /// Plain English, and it says what to do next where there is anything
        /// to do - docs/14. "Error" is never one of the words.
        /// </summary>
        public string Explanation { get; internal set; }
    }

    /// <summary>
    /// What an install is going to do, worked out before it does any of it.
    ///
    /// PURE. Nothing here touches a disk, a process or a registry, which is
    /// what makes every rule below testable on a machine with no Revit and no
    /// Windows. The engine performs this; it does not decide it.
    /// </summary>
    public sealed class InstallPlan
    {
        private InstallPlan(IReadOnlyList<InstallStep> steps,
                            IReadOnlyList<SkippedStep> skipped)
        {
            Steps = steps;
            Skipped = skipped;
        }

        /// <summary>In the order they will be attempted.</summary>
        public IReadOnlyList<InstallStep> Steps { get; private set; }

        /// <summary>
        /// Never silent. R-10 and Golden Rule: a product that cannot be
        /// installed is greyed out WITH THE REASON ON IT, never quietly
        /// dropped. AJ Tools' lesson L3 is the same failure seen from the
        /// other end - it installed nothing on three releases and said
        /// nothing, and nobody noticed for months.
        /// </summary>
        public IReadOnlyList<SkippedStep> Skipped { get; private set; }

        public bool HasWork { get { return Steps.Count > 0; } }

        /// <summary>
        /// Work out what to install.
        /// </summary>
        /// <param name="manifest">The product list, read as data.</param>
        /// <param name="productIds">What the user ticked.</param>
        /// <param name="releases">The Revit releases the user ticked.</param>
        /// <param name="installedReleases">
        /// What is actually on this PC. A release ticked but not installed is
        /// skipped with a reason rather than attempted - copying into a
        /// folder for a Revit nobody has is a silent no-op.
        /// </param>
        public static InstallPlan Build(ProductManifest manifest,
                                        IEnumerable<string> productIds,
                                        IEnumerable<string> releases,
                                        IEnumerable<string> installedReleases)
        {
            if (manifest == null) throw new ArgumentNullException("manifest");

            var wanted = Distinct(releases);
            var present = new HashSet<string>(Distinct(installedReleases), StringComparer.Ordinal);

            var steps = new List<InstallStep>();
            var skipped = new List<SkippedStep>();

            foreach (var id in Distinct(productIds))
            {
                var product = manifest.Find(id);

                if (product == null)
                {
                    skipped.Add(Skip(id, null, SkipReason.NoSuchProduct,
                        "There is no product called '" + id + "' in the Heron " +
                        "product list. Check the spelling, or fetch the list again."));
                    continue;
                }

                // TICKING A HEADING MEANS TICKING ITS PIECES, and the caller
                // is told so rather than left with a silent nothing. The
                // Heron tab is the only heading today - S3 / D-89 - and it is
                // built by the AI Bridge, the tools, or both.
                if (product.IsHeading)
                {
                    var pieces = manifest.PiecesOf(product.Id);
                    var names = new List<string>();
                    foreach (var piece in pieces) names.Add(piece.Id);

                    skipped.Add(Skip(id, null, SkipReason.IsAHeading,
                        "'" + product.Name + "' is a tab built by its pieces, so " +
                        "there is nothing to install under that name. Choose " +
                        (names.Count == 0
                            ? "the pieces that build it."
                            : "from: " + string.Join(", ", names.ToArray()) + ".")));
                    continue;
                }

                if (!product.MayBeOffered)
                {
                    skipped.Add(Skip(id, null, SkipReason.NotOfferable,
                        "'" + product.Name + "' is not ready to install yet. It is " +
                        "listed so the shape is known, not so it can be used."));
                    continue;
                }

                foreach (var release in wanted)
                {
                    if (!present.Contains(release))
                    {
                        skipped.Add(Skip(id, release, SkipReason.ReleaseNotInstalled,
                            "Revit " + release + " is not installed on this PC, so " +
                            "'" + product.Name + "' has nowhere to go. Install Revit " +
                            release + " first, or untick it."));
                        continue;
                    }

                    if (!product.SupportsRevit(release))
                    {
                        skipped.Add(Skip(id, release, SkipReason.ReleaseNotSupported,
                            "'" + product.Name + "' does not support Revit " + release +
                            ". It supports " + Join(product.Revit) + "."));
                        continue;
                    }

                    steps.Add(new InstallStep { Product = product, Release = release });
                }
            }

            // A release on the PC that the user did not tick is reported too.
            // Saying "2025 was left out" beats a modeller opening Revit 2025
            // next week and finding no Heron with nothing to explain it.
            foreach (var release in present)
            {
                if (Contains(wanted, release)) continue;
                skipped.Add(Skip(null, release, SkipReason.ReleaseNotChosen,
                    "Revit " + release + " is on this PC but was not ticked, so " +
                    "nothing was installed for it. Run this again and tick it if " +
                    "that was not meant."));
            }

            return new InstallPlan(steps, skipped);
        }

        private static SkippedStep Skip(string id, string release, SkipReason reason, string why)
        {
            return new SkippedStep
            {
                ProductId = id,
                Release = release,
                Reason = reason,
                Explanation = why,
            };
        }

        private static List<string> Distinct(IEnumerable<string> items)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var order = new List<string>();
            if (items == null) return order;
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item)) continue;
                if (seen.Add(item)) order.Add(item);
            }
            return order;
        }

        private static bool Contains(List<string> items, string value)
        {
            foreach (var item in items)
                if (string.Equals(item, value, StringComparison.Ordinal)) return true;
            return false;
        }

        private static string Join(IReadOnlyList<string> items)
        {
            if (items == null || items.Count == 0) return "no releases";
            var copy = new string[items.Count];
            for (var i = 0; i < items.Count; i++) copy[i] = items[i];
            return string.Join(", ", copy);
        }
    }
}
