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
    /// <summary>What comparing two versions concluded.</summary>
    public enum UpdateState
    {
        /// <summary>The two versions could not be compared. Never a guess.</summary>
        CannotTell,

        /// <summary>What is here is what is published.</summary>
        UpToDate,

        /// <summary>A newer release exists. The user is told, and nothing else happens.</summary>
        NewerAvailable,

        /// <summary>What is here is NEWER than what is published - a developer, or a pre-release.</summary>
        Ahead,
    }

    /// <summary>The answer, and the sentence a person reads.</summary>
    public sealed class UpdateVerdict
    {
        public UpdateState State { get; internal set; }
        public string Installed { get; internal set; }
        public string Available { get; internal set; }

        /// <summary>Plain English. Never "error", and it says what to do next.</summary>
        public string Say { get; internal set; }
    }

    /// <summary>
    /// Is there a newer release? - R-54, R-55, R-56.
    ///
    /// THE OWNER'S REASON, 2026-09-22, and it is the whole point: *"if someone
    /// downloaded this repo some days before and we made update, that time he
    /// will not receive update"*. Somebody installs once and never hears about
    /// anything again. This is the one thing the internet is for after the
    /// first download - R-53 says everything else works without it.
    ///
    /// IT COMPARES VERSIONS. IT DOES NOT FETCH THE PLUGIN TO FIND OUT - R-54
    /// in as many words: *"no need to download again"*. The caller fetches
    /// `heron-products.json` alone, which is a few kilobytes beside 54 MB of
    /// assemblies, and hands both versions here.
    ///
    /// AND IT DECIDES NOTHING ABOUT INSTALLING - R-55: offered, never applied.
    /// This returns a sentence. Whether anything is taken is a person's press,
    /// and "never auto-update without consent" is rule 1 of
    /// brain/heron_update.py, which exists because a tool that writes to live
    /// client models must never change itself while somebody is mid-delivery.
    ///
    /// CANNOT TELL IS AN ANSWER, not a failure to produce one. A version this
    /// cannot parse comes back as CannotTell rather than as "up to date",
    /// because the direction to be wrong in is the one that does not hide a
    /// newer release from somebody.
    /// </summary>
    public static class UpdateCheck
    {
        /// <summary>
        /// Compare what is here with what is published.
        /// </summary>
        /// <param name="installed">The version in the folder being installed from.</param>
        /// <param name="available">The version the published release carries.</param>
        public static UpdateVerdict Compare(string installed, string available)
        {
            var verdict = new UpdateVerdict { Installed = installed, Available = available };

            int[] here, there;
            if (!Parts(installed, out here) || !Parts(available, out there))
            {
                verdict.State = UpdateState.CannotTell;
                verdict.Say = "Heron could not tell whether there is a newer version - " +
                              Unreadable(installed, available) +
                              " Nothing was changed, and installing works either way.";
                return verdict;
            }

            var order = Order(here, there);

            if (order == 0)
            {
                verdict.State = UpdateState.UpToDate;
                verdict.Say = "This is the newest published version (" + available + ").";
                return verdict;
            }

            if (order > 0)
            {
                // AHEAD IS NOT AN UPDATE, AND SAYING "up to date" WOULD BE A
                // LIE OF THE COMFORTABLE KIND. It is a developer with a build
                // newer than anything published, and telling them they are
                // current would hide that they are about to install over it.
                verdict.State = UpdateState.Ahead;
                verdict.Say = "What is here (" + installed + ") is NEWER than the newest " +
                              "published release (" + available + "). That usually means this " +
                              "was built rather than downloaded. Nothing was changed.";
                return verdict;
            }

            verdict.State = UpdateState.NewerAvailable;
            verdict.Say = "A newer version of Heron is published: " + installed + " -> " +
                          available + "." + Environment.NewLine +
                          "Nothing has been updated. To take it, run heron-install again with " +
                          "--source and Heron's releases page.";
            return verdict;
        }

        /// <summary>
        /// The highest version any product in a manifest carries.
        ///
        /// HIGHEST, NOT FIRST. Q-PE-8 - whether products share one version or
        /// carry their own - is still open, so this must be right either way.
        /// With one shared version every product answers the same; with
        /// separate ones, the newest thing published is what "is there an
        /// update" is asking about.
        ///
        /// NULL WHEN NOTHING CARRIES ONE, which Compare then reports as
        /// CannotTell rather than as up to date.
        /// </summary>
        public static string HighestVersion(ProductManifest manifest)
        {
            if (manifest == null) return null;

            string best = null;
            int[] bestParts = null;

            foreach (var product in manifest.Products)
            {
                if (string.IsNullOrEmpty(product.Version)) continue;

                int[] parts;
                if (!Parts(product.Version, out parts)) continue;

                if (bestParts == null || Order(parts, bestParts) > 0)
                {
                    best = product.Version;
                    bestParts = parts;
                }
            }

            return best;
        }

        /// <summary>
        /// Split a version into numbers, or fail.
        ///
        /// DIGITS ONLY, AND A NON-NUMBER FAILS THE WHOLE THING rather than
        /// being read as zero. "0.1.0-rc1" read as 0.1.0 would call a release
        /// candidate identical to the release, which is exactly the comparison
        /// somebody would be relying on this to get right.
        /// </summary>
        private static bool Parts(string version, out int[] parts)
        {
            parts = null;
            if (string.IsNullOrWhiteSpace(version)) return false;

            var text = version.Trim();
            if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase)) text = text.Substring(1);
            if (text.Length == 0) return false;

            var pieces = text.Split('.');
            var found = new List<int>();
            foreach (var piece in pieces)
            {
                int number;
                if (!int.TryParse(piece, System.Globalization.NumberStyles.None,
                                  System.Globalization.CultureInfo.InvariantCulture, out number))
                {
                    return false;
                }
                found.Add(number);
            }

            parts = found.ToArray();
            return true;
        }

        /// <summary>
        /// Negative when the first is older, 0 when they match, positive when
        /// it is newer.
        ///
        /// A SHORTER VERSION IS PADDED WITH ZEROES, so 1.2 and 1.2.0 are the
        /// same thing and 1.2 is older than 1.2.1.
        /// </summary>
        private static int Order(int[] left, int[] right)
        {
            var length = Math.Max(left.Length, right.Length);
            for (var i = 0; i < length; i++)
            {
                var a = i < left.Length ? left[i] : 0;
                var b = i < right.Length ? right[i] : 0;
                if (a != b) return a < b ? -1 : 1;
            }
            return 0;
        }

        /// <summary>Which of the two could not be read, named rather than implied.</summary>
        private static string Unreadable(string installed, string available)
        {
            int[] ignored;
            var hereBad = !Parts(installed, out ignored);
            var thereBad = !Parts(available, out ignored);

            if (hereBad && thereBad)
                return "neither '" + Show(installed) + "' nor '" + Show(available) +
                       "' is a version it can compare.";
            if (hereBad)
                return "'" + Show(installed) + "', which is what is here, is not a version it " +
                       "can compare.";
            return "'" + Show(available) + "', which is what the release publishes, is not a " +
                   "version it can compare.";
        }

        private static string Show(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? "(nothing)" : text.Trim();
        }
    }
}
