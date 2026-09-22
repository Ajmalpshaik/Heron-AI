// Heron-Agent:  HERON-INS-PKG-012
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Heron.Installer
{
    /// <summary>
    /// Fetches a product's files from a published release, checks them, and
    /// unpacks them where the deploy script can be pointed at them.
    ///
    /// STAGE 5. The installer reads the product list FROM A NAMED RELEASE and
    /// downloads each ticked product's asset - never from a branch, because
    /// docs/07 section 1a refused "whatever the default branch happens to say
    /// today" and asked for a versioned artefact instead.
    ///
    /// NOTHING DOWNLOADED IS EXECUTED - R-13, Golden Rule 19. This reads JSON
    /// as data and unzips files. It never runs one, never asks one what to
    /// install, and the product list it obeys is the one the release carries
    /// rather than anything inside a product's own zip.
    ///
    /// VERIFIED BEFORE USE, NOT AFTER - R-12. The bytes are checked against
    /// the published SHA-256 while they are still in memory. A file that does
    /// not match is never written to disk at all, so it cannot be half-
    /// extracted into somebody's Revit and then regretted.
    ///
    /// THIS ONE RUNS ANYWHERE, unlike the three adapters in WindowsAdapters.cs.
    /// It is HttpClient and System.IO.Compression, so tests drive it against a
    /// local server on Linux and the download path is genuinely exercised
    /// rather than read. What is still owed on Windows is whether GitHub is
    /// reachable from a contractor laptop at all - Q-PE-5, and no test
    /// anywhere can answer it.
    /// </summary>
    public sealed class ReleaseDownload : IProductFiles, IDisposable
    {
        private readonly string _baseUrl;
        private readonly string _workFolder;
        private readonly HttpClient _http;
        private readonly Dictionary<string, string> _unpacked =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private IDictionary<string, string> _checksums;
        private string _checksumsProblem;
        private bool _askedForChecksums;

        /// <summary>
        /// Where a GitHub release publishes its assets.
        ///
        /// BUILT ONCE, HERE, so a tag typed by a person never becomes part of
        /// a URL anywhere else. The shape is GitHub's and is not Heron's to
        /// choose.
        /// </summary>
        public static string UrlFor(string owner, string repo, string tag)
        {
            if (string.IsNullOrEmpty(owner)) throw new ArgumentNullException("owner");
            if (string.IsNullOrEmpty(repo)) throw new ArgumentNullException("repo");
            if (string.IsNullOrEmpty(tag)) throw new ArgumentNullException("tag");

            return "https://github.com/" + owner + "/" + repo +
                   "/releases/download/" + tag;
        }

        /// <summary>
        /// Where the NEWEST published release's assets are.
        ///
        /// STILL A RELEASE, WHICH IS THE POINT. docs/07 section 1a refused
        /// "whatever the default branch happens to say today" and asked for a
        /// versioned artefact; /releases/latest/ resolves to a published,
        /// tagged release and never to a branch. What it does not do is pin -
        /// two people installing a week apart can get two versions, which is
        /// what a user without a tag to type actually wants and is why
        /// UrlFor() above exists for when they do have one.
        ///
        /// A DRAFT IS NOT LATEST. GitHub skips drafts here, so the releases
        /// this repository's workflow creates are invisible to it until a
        /// person publishes one - which is the safety the draft was for.
        /// </summary>
        public static string LatestUrlFor(string owner, string repo)
        {
            if (string.IsNullOrEmpty(owner)) throw new ArgumentNullException("owner");
            if (string.IsNullOrEmpty(repo)) throw new ArgumentNullException("repo");

            return "https://github.com/" + owner + "/" + repo +
                   "/releases/latest/download";
        }

        /// <param name="baseUrl">
        /// The folder the assets sit in - UrlFor() builds the real one, and a
        /// test points this at its own server.
        /// </param>
        /// <param name="workFolder">Where verified assets are unpacked.</param>
        /// <param name="seconds">
        /// The ceiling on one file. A slow site connection is not a broken
        /// one, so this is generous - but it is not absent, because a window
        /// that waits for ever is one a user kills halfway through.
        /// </param>
        public ReleaseDownload(string baseUrl, string workFolder, int seconds)
        {
            if (string.IsNullOrEmpty(baseUrl)) throw new ArgumentNullException("baseUrl");
            if (string.IsNullOrEmpty(workFolder)) throw new ArgumentNullException("workFolder");

            _baseUrl = baseUrl.TrimEnd('/');
            _workFolder = workFolder;
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(Math.Max(5, seconds)) };

            // GitHub answers an anonymous download without this, but a named
            // agent is what lets somebody reading a proxy log tell what asked.
            _http.DefaultRequestHeaders.Add("User-Agent", "Heron-Installer");
        }

        public ReleaseDownload(string baseUrl, string workFolder)
            : this(baseUrl, workFolder, 300)
        {
        }

        /// <summary>
        /// The product list the RELEASE carries - Stage 5 item 1.
        ///
        /// Returns null with `why` set. The caller falls back to nothing: a
        /// release whose manifest cannot be read is one nothing should be
        /// installed from, because the alternative is installing the products
        /// a different version happened to describe.
        /// </summary>
        public ProductManifest Manifest(out string why)
        {
            byte[] bytes = Get(ReleaseAssets.ManifestName, out why);
            if (bytes == null) return null;

            var bad = ReleaseAssets.WhyNotTrusted(ReleaseAssets.ManifestName, bytes, Checksums());
            if (bad != null) { why = bad; return null; }

            try
            {
                // READ AS DATA - R-13, D-91. Parsed, never executed, and the
                // only thing taken from it is the answer to "which products".
                why = null;
                return ProductManifest.Parse(StripBom(bytes));
            }
            catch (InvalidDataException e)
            {
                // NARROWED TO WHAT Parse ACTUALLY THROWS - D-52. It turns bad
                // JSON and a missing products array into InvalidDataException
                // with a sentence already written for a person, so catching
                // wider here would swallow a fault in this file as if the
                // download were damaged.
                why = "The product list in that release could not be read - it is damaged. " +
                      "Fetch the release again, or pick a different version. Nothing was installed. (" +
                      e.Message + ")";
                return null;
            }
        }

        public string Folder(HeronProduct product, string release, out string why)
        {
            why = null;

            var name = ReleaseAssets.NameFor(product, release);
            if (name == null)
            {
                why = "Heron could not work out which file to fetch for that product and Revit release, so nothing was fetched.";
                return null;
            }

            string already;
            if (_unpacked.TryGetValue(name, out already)) return already;

            var bytes = Get(name, out why);
            if (bytes == null) return null;

            // BEFORE A BYTE IS WRITTEN. The check happens on what is in
            // memory, so a file that fails it never exists on the disk.
            var bad = ReleaseAssets.WhyNotTrusted(name, bytes, Checksums());
            if (bad != null) { why = bad; return null; }

            var into = Path.Combine(_workFolder, product.Id + "-" + release);
            try
            {
                if (Directory.Exists(into)) Directory.Delete(into, true);
                Directory.CreateDirectory(into);
                Unpack(bytes, into);
            }
            catch (InvalidDataException e)
            {
                // NAMED SEPARATELY, AND THAT IS NOT TIDINESS.
                // System.IO.InvalidDataException derives from SystemException,
                // NOT from IOException - despite the namespace. The first
                // draft of this catch said "a kind of IOException" in a
                // comment and caught only IOException, so the zip-escape
                // refusal below went straight past it and killed the process.
                // The suite found it on the next run, which is the one case
                // this guard exists for.
                why = "'" + name + "' arrived whole but could not be unpacked (" + e.Message +
                      "). Nothing was installed.";
                return null;
            }
            catch (IOException e)
            {
                // A disk that is full, or a file something else has open.
                why = "'" + name + "' arrived whole but could not be unpacked (" + e.Message +
                      "). Nothing was installed.";
                return null;
            }
            catch (UnauthorizedAccessException e)
            {
                why = "'" + name + "' arrived whole, but Heron is not allowed to write to the folder it " +
                      "unpacks into (" + e.Message + "). Nothing was installed.";
                return null;
            }

            _unpacked[name] = into;
            return into;
        }

        /// <summary>
        /// Unzip, and refuse an entry that would write outside the folder.
        ///
        /// A ZIP ENTRY IS A NAME SOMEBODY ELSE CHOSE. `..\..\Windows\System32`
        /// is a legal name in the format, and an extractor that joins it to a
        /// destination writes exactly there. It is old, it is well known, and
        /// it is still how archives are used to overwrite files nobody offered
        /// - so the destination is resolved and checked rather than trusted.
        ///
        /// THE WHOLE ENTRY IS REFUSED, NOT SKIPPED. A zip carrying one is not
        /// a Heron release with a mistake in it; it is a file that should not
        /// be unpacked at all, and taking the other 74 entries from it would
        /// be deciding that most of a suspect archive is fine.
        /// </summary>
        private static void Unpack(byte[] bytes, string into)
        {
            var root = Path.GetFullPath(into);
            if (!root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                root += Path.DirectorySeparatorChar;

            using (var stream = new MemoryStream(bytes, false))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;   // a folder

                    var target = Path.GetFullPath(Path.Combine(root, entry.FullName));
                    if (!target.StartsWith(root, StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(
                            "it holds a file that would be written outside the folder it is " +
                            "being unpacked into (" + entry.FullName + ")");
                    }

                    var folder = Path.GetDirectoryName(target);
                    if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
                    entry.ExtractToFile(target, true);
                }
            }
        }

        /// <summary>
        /// checksums.txt, fetched once.
        ///
        /// ASKED FOR EXACTLY ONCE, and the failure is remembered too. Every
        /// asset wants this answer, and a release whose checksums cannot be
        /// fetched should say so once rather than once per product.
        /// </summary>
        private IDictionary<string, string> Checksums()
        {
            if (_askedForChecksums) return _checksums;
            _askedForChecksums = true;

            var bytes = Get(ReleaseAssets.ChecksumsName, out _checksumsProblem);
            _checksums = bytes == null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : ReleaseAssets.ReadChecksums(StripBom(bytes));

            return _checksums;
        }

        /// <summary>
        /// One file, or null with a sentence naming the CAUSE - R-14.
        ///
        /// EVERY EXCEPTION BECOMES ONE OF FOUR CAUSES, because "an error
        /// occurred" sends a modeller to a colleague and "your network blocks
        /// GitHub" sends them to IT. The mapping is deliberately coarse: the
        /// difference between a DNS failure and a refused socket matters to
        /// nobody holding a Revit licence.
        /// </summary>
        private byte[] Get(string name, out string why)
        {
            why = null;
            var url = _baseUrl + "/" + name;

            try
            {
                using (var reply = _http.GetAsync(url).GetAwaiter().GetResult())
                {
                    if (reply.StatusCode == HttpStatusCode.NotFound)
                    {
                        why = ReleaseAssets.WhyNotFetched(FetchProblem.NotFound, name, null);
                        return null;
                    }

                    // A PROXY THAT WANTS A LOGIN ANSWERS 407, and a filtering
                    // one commonly answers 403. Both are somebody's policy
                    // rather than a broken release, and the sentence has to
                    // say so or the user files a bug against Heron.
                    if (reply.StatusCode == HttpStatusCode.Forbidden
                        || reply.StatusCode == HttpStatusCode.ProxyAuthenticationRequired
                        || reply.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        why = ReleaseAssets.WhyNotFetched(FetchProblem.Blocked, name, null);
                        return null;
                    }

                    if (!reply.IsSuccessStatusCode)
                    {
                        why = ReleaseAssets.WhyNotFetched(FetchProblem.Unknown, name,
                                                          "the server answered " + (int)reply.StatusCode);
                        return null;
                    }

                    return reply.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                // HttpClient THROWS THIS ON ITS OWN TIMEOUT, as
                // TaskCanceledException, which derives from this one. Catching
                // the base covers both that and a real cancellation.
                //
                // THE FIRST DRAFT OF THIS CAUGHT A CLASS OF ITS OWN that
                // nothing ever throws - it compiled, it read correctly, and
                // every timeout would have fallen through to the generic catch
                // below and been reported as "could not be fetched" with no
                // cause. The one message a slow site connection most needs was
                // the one it could never produce. Found by reading it back.
                why = ReleaseAssets.WhyNotFetched(FetchProblem.TooSlow, name, null);
                return null;
            }
            catch (HttpRequestException e)
            {
                // NARROWED, NOT SWALLOWED. A name that will not resolve and a
                // socket that was refused are different sentences, and D-52's
                // rule - narrow before you swallow - is the reason this reads
                // the inner cause rather than reporting one guess for both.
                var problem = LooksOffline(e) ? FetchProblem.NoNetwork : FetchProblem.Blocked;
                why = ReleaseAssets.WhyNotFetched(problem, name, null);
                return null;
            }
            catch (Exception e)
            {
                why = ReleaseAssets.WhyNotFetched(FetchProblem.Unknown, name, e.Message);
                return null;
            }
        }

        /// <summary>
        /// Whether this reads as "no network at all" rather than "refused".
        ///
        /// READ FROM THE SOCKET ERROR, not from the message text, because the
        /// message is localised and a check against English words is one that
        /// stops working on a machine set to Arabic.
        /// </summary>
        private static bool LooksOffline(HttpRequestException e)
        {
            for (Exception inner = e; inner != null; inner = inner.InnerException)
            {
                var socket = inner as System.Net.Sockets.SocketException;
                if (socket == null) continue;

                switch (socket.SocketErrorCode)
                {
                    case System.Net.Sockets.SocketError.HostNotFound:
                    case System.Net.Sockets.SocketError.NetworkDown:
                    case System.Net.Sockets.SocketError.NetworkUnreachable:
                    case System.Net.Sockets.SocketError.HostUnreachable:
                    case System.Net.Sockets.SocketError.TryAgain:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Text from bytes, with a UTF-8 byte-order mark taken off.
        ///
        /// heron-products.json IS WRITTEN WITH ONE, which is why every Python
        /// reader in this repository opens it as utf-8-sig. JsonDocument.Parse
        /// refuses a string that starts with U+FEFF, so the same allowance has
        /// to be made here or the manifest that works everywhere else fails
        /// the one time it arrives over the wire.
        /// </summary>
        private static string StripBom(byte[] bytes)
        {
            var text = Encoding.UTF8.GetString(bytes);
            return text.Length > 0 && text[0] == '﻿' ? text.Substring(1) : text;
        }

        public void Dispose()
        {
            _http.Dispose();
        }
    }
}
