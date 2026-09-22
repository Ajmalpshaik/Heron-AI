// Heron-Agent:  HERON-INS-PKG-012
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.IO;

namespace Heron.Installer
{
    /// <summary>
    /// Route 2 - Heron's files already on this PC, and no internet at all.
    ///
    /// R-15 and R-53: after one download, nothing needs the internet. What a
    /// person is handed is the folder the release builder produces - the same
    /// product zips, the same `heron-products.json`, the same `checksums.txt`
    /// - on a USB stick, a server share, or unzipped from a download they took
    /// weeks ago.
    ///
    /// IT IS THE SAME CLASS AS ReleaseDownload, MINUS THE WIRE. Both are
    /// IProductFiles; both verify a zip against the published checksum BEFORE
    /// a byte is written; both unpack through the one zip-slip guard in
    /// ReleaseAssets. The engine, the plan and the deploy script cannot tell
    /// which one handed them a folder, and that is R-31 doing its job.
    ///
    /// A FOLDER ON A STICK IS NOT TRUSTED FOR BEING LOCAL. It got here because
    /// somebody handed it over, which is exactly how a download gets here. The
    /// only thing being local removes is the network, not the question of
    /// whether the bytes are what the publisher published.
    ///
    /// AND HERE IS WHAT THE CHECKSUM CANNOT DO, said plainly rather than left
    /// for somebody to assume: it catches DAMAGE, not a determined tamperer.
    /// Anyone who can rewrite a zip in this folder can rewrite `checksums.txt`
    /// beside it, and both will then agree. What closes that is a signature
    /// over the release - Stage 8, not built. Until it is, route 2 is exactly
    /// as safe as the person who handed over the folder, and saying so is the
    /// point: docs/12's rule is that a limit nobody wrote down is one somebody
    /// relies on.
    /// </summary>
    public sealed class ProductFolder : IProductFiles
    {
        private readonly string _folder;
        private readonly string _workFolder;
        private readonly Dictionary<string, string> _unpacked =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private bool _askedForChecksums;
        private IDictionary<string, string> _checksums;

        /// <param name="folder">What the user was handed, and pointed at.</param>
        /// <param name="workFolder">Somewhere this may unpack into and delete.</param>
        public ProductFolder(string folder, string workFolder)
        {
            if (string.IsNullOrEmpty(folder)) throw new ArgumentNullException("folder");
            if (string.IsNullOrEmpty(workFolder)) throw new ArgumentNullException("workFolder");
            _folder = folder;
            _workFolder = workFolder;
        }

        /// <summary>The folder this was pointed at.</summary>
        public string Path { get { return _folder; } }

        /// <summary>
        /// Whether that folder is a Heron handover at all, or null when it is.
        ///
        /// THE COMMONEST WRONG ANSWER IS THE PARENT FOLDER, and the sentence
        /// says so rather than leaving somebody to guess which of the folders
        /// they have is the one meant. Somebody unzips a download into
        /// Downloads\heron and points at Downloads.
        ///
        /// IT READS TWO FILE NAMES AND NOTHING ELSE. R-13: the installer never
        /// executes what it was given in order to decide what to install, and
        /// R-50 says the same about reading a repository to decide. Asking
        /// whether two files exist is neither.
        /// </summary>
        public static string WhyNotAHeronFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return "No folder was named, so there is nothing to install from.";

            if (!Directory.Exists(folder))
            {
                return "There is no folder at '" + folder + "'. Check the path, or the " +
                       "drive letter if it is on a stick that has moved.";
            }

            var manifest = System.IO.Path.Combine(folder, ReleaseAssets.ManifestName);
            if (!File.Exists(manifest))
            {
                return "'" + folder + "' does not hold " + ReleaseAssets.ManifestName +
                       ", so it is not a Heron install folder. If you unzipped Heron into a " +
                       "folder inside this one, point at that one instead.";
            }

            var sums = System.IO.Path.Combine(folder, ReleaseAssets.ChecksumsName);
            if (!File.Exists(sums))
            {
                return "'" + folder + "' holds " + ReleaseAssets.ManifestName + " but no " +
                       ReleaseAssets.ChecksumsName + ". That file is written last, so a folder " +
                       "without it is one whose download or copy did not finish. Copy it again.";
            }

            return null;
        }

        /// <summary>
        /// The product list, read from the folder as data.
        ///
        /// AS DATA, AND ONLY AS DATA - R-50. It is parsed by the same
        /// ProductManifest the window uses; nothing in the folder is executed
        /// and nothing in it decides what may be installed.
        /// </summary>
        public ProductManifest Manifest(out string why)
        {
            why = WhyNotAHeronFolder(_folder);
            if (why != null) return null;

            try
            {
                return ProductManifest.Load(System.IO.Path.Combine(_folder, ReleaseAssets.ManifestName));
            }
            catch (IOException e)
            {
                why = "The product list in '" + _folder + "' could not be read (" + e.Message + ").";
                return null;
            }
            catch (InvalidDataException e)
            {
                why = e.Message;
                return null;
            }
            catch (UnauthorizedAccessException e)
            {
                why = "Heron is not allowed to read the product list in '" + _folder +
                      "' (" + e.Message + ").";
                return null;
            }
        }

        /// <summary>
        /// One product's files for one release, unpacked and verified.
        ///
        /// THE SAME ORDER AS A DOWNLOAD, and the order is the safety property:
        /// read the bytes, check them against the published checksum, and only
        /// then write anything. A file that fails never reaches the disk.
        /// </summary>
        public string Folder(HeronProduct product, string release, out string why)
        {
            why = null;

            var name = ReleaseAssets.NameFor(product, release);
            if (name == null)
            {
                why = "Heron could not work out which file to look for in that folder for that " +
                      "product and Revit release, so nothing was read.";
                return null;
            }

            string already;
            if (_unpacked.TryGetValue(name, out already)) return already;

            var from = System.IO.Path.Combine(_folder, name);
            if (!File.Exists(from))
            {
                why = "'" + name + "' is not in " + _folder + ". That folder holds Heron's files " +
                      "for some releases but not this one. Take the whole folder rather than part " +
                      "of it, or install this release from Heron's own release instead.";
                return null;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(from);
            }
            catch (IOException e)
            {
                why = "'" + name + "' is there but could not be read (" + e.Message +
                      "). Nothing was installed.";
                return null;
            }
            catch (UnauthorizedAccessException e)
            {
                why = "Heron is not allowed to read '" + name + "' in that folder (" + e.Message +
                      "). Nothing was installed.";
                return null;
            }

            // BEFORE A BYTE IS WRITTEN, exactly as on the wire. Local is not a
            // reason to skip this: the folder arrived from somewhere too.
            var bad = ReleaseAssets.WhyNotTrusted(name, bytes, Checksums());
            if (bad != null) { why = bad; return null; }

            var into = System.IO.Path.Combine(_workFolder, product.Id + "-" + release);
            try
            {
                if (Directory.Exists(into)) Directory.Delete(into, true);
                Directory.CreateDirectory(into);
                ReleaseAssets.Unpack(bytes, into);
            }
            catch (InvalidDataException e)
            {
                // InvalidDataException derives from SystemException, NOT from
                // IOException despite the namespace - which is how the
                // zip-escape refusal once went past a catch and killed the
                // process. Named separately here for that reason.
                why = "'" + name + "' passed its checksum but could not be unpacked (" + e.Message +
                      "). Nothing was installed.";
                return null;
            }
            catch (IOException e)
            {
                why = "'" + name + "' passed its checksum but could not be unpacked (" + e.Message +
                      "). Nothing was installed.";
                return null;
            }
            catch (UnauthorizedAccessException e)
            {
                why = "'" + name + "' passed its checksum, but Heron is not allowed to write to the " +
                      "folder it unpacks into (" + e.Message + "). Nothing was installed.";
                return null;
            }

            _unpacked[name] = into;
            return into;
        }

        /// <summary>
        /// checksums.txt, read once.
        ///
        /// ONCE, AND THE FAILURE IS REMEMBERED TOO. Every product wants this
        /// answer, and a folder whose checksums cannot be read should say so
        /// once rather than once per product.
        ///
        /// AN UNREADABLE FILE COMES BACK EMPTY, WHICH REFUSES EVERYTHING.
        /// ReleaseAssets.WhyNotTrusted treats an empty list as "this published
        /// no checksums" and installs nothing, so a damaged checksums.txt
        /// fails closed rather than open.
        /// </summary>
        private IDictionary<string, string> Checksums()
        {
            if (_askedForChecksums) return _checksums;
            _askedForChecksums = true;

            _checksums = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var text = File.ReadAllText(System.IO.Path.Combine(_folder, ReleaseAssets.ChecksumsName));
                _checksums = ReleaseAssets.ReadChecksums(text);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return _checksums;
        }
    }
}
