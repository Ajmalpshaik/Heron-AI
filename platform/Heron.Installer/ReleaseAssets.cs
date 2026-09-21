// Heron-Agent:  HERON-INS-PKG-012
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Heron.Installer
{
    /// <summary>
    /// What a release carries, what each file is called, and how a downloaded
    /// one is checked before anything is done with it.
    ///
    /// PURE. Nothing here opens a socket, writes a file or starts a process,
    /// which is what makes every rule below testable on a machine with no
    /// network at all. The fetching is somewhere else and does none of the
    /// deciding.
    ///
    /// IT AGREES WITH tools/build-release-assets.py OR THE WHOLE THING IS
    /// USELESS. That tool names the assets and writes the checksums; this
    /// reads them back. Two spellings of one name is a download that 404s
    /// forever, so tests/test_release_download.py holds the two together
    /// rather than trusting them to stay in step.
    /// </summary>
    public static class ReleaseAssets
    {
        /// <summary>The product list, carried by the release itself.</summary>
        public const string ManifestName = "heron-products.json";

        /// <summary>SHA-256 of every other file, written last by the builder.</summary>
        public const string ChecksumsName = "checksums.txt";

        /// <summary>
        /// One product's files for one Revit release.
        ///
        /// DERIVED FROM THE MANIFEST, never written out - R-3. The id comes
        /// from platform/heron-products.json and the release from the machine,
        /// so adding a product changes nothing in here.
        /// </summary>
        public static string NameFor(HeronProduct product, string release)
        {
            if (product == null) throw new ArgumentNullException("product");
            if (string.IsNullOrEmpty(product.Id)) return null;
            if (string.IsNullOrEmpty(release)) return null;
            return product.Id + "-" + release + ".zip";
        }

        /// <summary>
        /// Read checksums.txt into name -> digest.
        ///
        /// THE FORMAT IS sha256sum's, because that is what the builder writes
        /// and what a person can check by hand with a tool they already have.
        /// Two spaces between the digest and the name; anything else on the
        /// line is not a checksum and is skipped rather than guessed at.
        /// </summary>
        public static IDictionary<string, string> ReadChecksums(string text)
        {
            var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(text)) return found;

            foreach (var raw in text.Replace("\r", "").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;

                var gap = line.IndexOf("  ", StringComparison.Ordinal);
                if (gap <= 0) continue;

                var digest = line.Substring(0, gap).Trim();
                var name = line.Substring(gap + 2).Trim();
                if (digest.Length != 64 || name.Length == 0) continue;
                if (!IsHex(digest)) continue;

                found[name] = digest.ToLowerInvariant();
            }

            return found;
        }

        /// <summary>The SHA-256 of some bytes, lower-case hex.</summary>
        public static string DigestOf(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                var text = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) text.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return text.ToString();
            }
        }

        /// <summary>
        /// Whether a downloaded file is the one the release published.
        ///
        /// Returns null when it is, or the sentence to show when it is not -
        /// R-12, and R-14 for the wording. "Error" is never one of the words.
        ///
        /// A FILE NOT LISTED IS REFUSED, and that is not pedantry. checksums.txt
        /// is written LAST by the builder, over everything in the folder, so a
        /// name missing from it means either the release was assembled by
        /// something else or the file was added afterwards. Neither is a file
        /// to unzip into somebody's Revit.
        /// </summary>
        public static string WhyNotTrusted(string name,
                                           byte[] bytes,
                                           IDictionary<string, string> checksums)
        {
            if (string.IsNullOrEmpty(name)) return "A downloaded file arrived with no name, so there is nothing to check it against. Nothing was installed.";
            if (bytes == null || bytes.Length == 0)
                return "'" + name + "' downloaded as an empty file. The download did not finish - check the connection and run this again. Nothing was installed.";

            if (checksums == null || checksums.Count == 0)
                return "This release published no " + ChecksumsName + ", so there is no way to tell whether '" + name + "' arrived whole. Heron will not install a file it cannot check. Fetch the release again, or install from a build made on this PC.";

            string expected;
            if (!checksums.TryGetValue(name, out expected))
                return "'" + name + "' is not listed in " + ChecksumsName + ", so it is not part of this release. Rather than install a file nobody published, nothing was changed.";

            var actual = DigestOf(bytes);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                return "'" + name + "' did not arrive whole - what was downloaded does not match what the release published. This is usually a connection that dropped part way. Run this again; if it keeps happening, the release itself is damaged. Nothing was installed.";

            return null;
        }

        /// <summary>
        /// What to say when a download did not happen at all - R-14.
        ///
        /// THE CAUSE, NOT THE SYMPTOM. "Error" tells a modeller nothing they
        /// can act on; "your network blocks GitHub" tells them who to ask.
        /// Four causes, because those are the four that actually happen, and
        /// a contractor laptop meets the middle two far more than anyone
        /// writing this would guess.
        /// </summary>
        public static string WhyNotFetched(FetchProblem problem, string what, string detail)
        {
            var thing = string.IsNullOrEmpty(what) ? "the download" : "'" + what + "'";

            switch (problem)
            {
                case FetchProblem.NoNetwork:
                    return "Heron could not reach the internet, so " + thing + " could not be fetched. Check the connection and run this again. Nothing was installed.";

                case FetchProblem.Blocked:
                    return "Something on this network refused the connection to GitHub, so " + thing + " could not be fetched. On a company laptop this is usually the firewall or a proxy - ask IT to allow github.com, or install from a copy of Heron already on the machine. Nothing was installed.";

                case FetchProblem.NotFound:
                    return thing + " is not in that release. Either the release is older than this installer expects, or it was published without that product. Pick a different version, or install from a build made on this PC. Nothing was installed.";

                case FetchProblem.TooSlow:
                    return thing + " did not finish in time. The connection may be very slow rather than broken - run this again when it is better. Nothing was installed.";

                default:
                    return thing + " could not be fetched" +
                           (string.IsNullOrEmpty(detail) ? "." : " (" + detail + ").") +
                           " Nothing was installed.";
            }
        }

        private static bool IsHex(string text)
        {
            foreach (var c in text)
            {
                var hex = (c >= '0' && c <= '9')
                          || (c >= 'a' && c <= 'f')
                          || (c >= 'A' && c <= 'F');
                if (!hex) return false;
            }
            return true;
        }
    }

    /// <summary>Why a fetch did not happen. Each one is a different sentence.</summary>
    public enum FetchProblem
    {
        /// <summary>Something else, and the detail is all there is.</summary>
        Unknown = 0,

        /// <summary>No route to anywhere - the machine is offline.</summary>
        NoNetwork,

        /// <summary>Reached something, and it refused. A firewall or a proxy.</summary>
        Blocked,

        /// <summary>GitHub answered, and that file is not in that release.</summary>
        NotFound,

        /// <summary>It started and did not finish inside the ceiling.</summary>
        TooSlow,
    }

    /// <summary>
    /// A local folder holding one product's built files, however it got there.
    ///
    /// TWO WAYS IN, ONE COPY RULE. A build on this PC and a verified download
    /// are the same thing by the time they reach tools\deploy-addin.ps1, so
    /// that script keeps every rule about copying and neither route repeats
    /// any of them - R-31.
    /// </summary>
    public interface IProductFiles
    {
        /// <summary>
        /// The folder, or null with `why` set to a sentence for a person.
        ///
        /// NEVER A GUESS AND NEVER A HALF. A folder returned here is about to
        /// be copied into somebody's Revit, so "could not tell" must come back
        /// as null rather than as a path that might be right.
        /// </summary>
        string Folder(HeronProduct product, string release, out string why);
    }
}
