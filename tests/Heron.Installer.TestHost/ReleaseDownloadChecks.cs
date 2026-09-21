// Heron-Agent:  none
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;
using Heron.Installer;

namespace Heron.Installer.TestHost
{
    /// <summary>
    /// The download half of Stage 5, run for real against a local server.
    ///
    /// WHY THIS IS NOT A TEXT CHECK. Three of the four adapters that reach
    /// outside this assembly need Windows and cannot be run here, so they are
    /// read rather than exercised. ReleaseDownload is not one of them: it is
    /// HttpClient and System.IO.Compression, which run anywhere. So it is
    /// driven against a real HTTP server on a real port, with real zip bytes,
    /// and the refusals below are refusals that actually happened.
    ///
    /// WHAT IS STILL OWED. Whether GitHub is reachable from a contractor's
    /// laptop - Q-PE-5, and nothing in any repository can answer it.
    /// </summary>
    internal static class ReleaseDownloadChecks
    {
        /// <summary>A release folder served over HTTP, built in memory.</summary>
        private sealed class FakeRelease : IDisposable
        {
            private readonly HttpListener _listener = new HttpListener();
            private readonly Dictionary<string, byte[]> _files =
                new Dictionary<string, byte[]>(StringComparer.Ordinal);
            private readonly HashSet<string> _forbidden =
                new HashSet<string>(StringComparer.Ordinal);
            private readonly Thread _thread;

            public string BaseUrl { get; private set; }

            public FakeRelease(int port)
            {
                BaseUrl = "http://127.0.0.1:" + port + "/release";
                _listener.Prefixes.Add("http://127.0.0.1:" + port + "/");
                _listener.Start();

                _thread = new Thread(Serve) { IsBackground = true };
                _thread.Start();
            }

            public void Put(string name, byte[] bytes) { _files[name] = bytes; }
            public void Put(string name, string text) { Put(name, Encoding.UTF8.GetBytes(text)); }
            public void Forbid(string name) { _forbidden.Add(name); }

            /// <summary>checksums.txt over everything currently held.</summary>
            public void Checksum(params string[] skip)
            {
                var skipped = new HashSet<string>(skip, StringComparer.Ordinal);
                var lines = new List<string>();
                foreach (var pair in _files)
                {
                    if (pair.Key == ReleaseAssets.ChecksumsName) continue;
                    if (skipped.Contains(pair.Key)) continue;
                    lines.Add(ReleaseAssets.DigestOf(pair.Value) + "  " + pair.Key);
                }
                lines.Sort(StringComparer.Ordinal);
                Put(ReleaseAssets.ChecksumsName, string.Join("\n", lines.ToArray()) + "\n");
            }

            private void Serve()
            {
                while (_listener.IsListening)
                {
                    HttpListenerContext context;
                    try { context = _listener.GetContext(); }
                    catch { return; }

                    var name = Path.GetFileName(context.Request.Url.AbsolutePath);
                    byte[] body;

                    if (_forbidden.Contains(name))
                    {
                        context.Response.StatusCode = 403;
                        context.Response.Close();
                        continue;
                    }

                    if (!_files.TryGetValue(name, out body))
                    {
                        context.Response.StatusCode = 404;
                        context.Response.Close();
                        continue;
                    }

                    context.Response.StatusCode = 200;
                    context.Response.ContentLength64 = body.Length;
                    context.Response.OutputStream.Write(body, 0, body.Length);
                    context.Response.Close();
                }
            }

            public void Dispose()
            {
                try { _listener.Stop(); } catch { }
                try { _listener.Close(); } catch { }
            }
        }

        /// <summary>A zip holding one named file, built in memory.</summary>
        private static byte[] ZipOf(params string[] namesAndBodies)
        {
            using (var buffer = new MemoryStream())
            {
                using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, true))
                {
                    for (var i = 0; i + 1 < namesAndBodies.Length; i += 2)
                    {
                        var entry = zip.CreateEntry(namesAndBodies[i]);
                        using (var stream = entry.Open())
                        {
                            var bytes = Encoding.UTF8.GetBytes(namesAndBodies[i + 1]);
                            stream.Write(bytes, 0, bytes.Length);
                        }
                    }
                }
                return buffer.ToArray();
            }
        }

        private const string Fixture = @"{
          ""products"": [
            { ""id"": ""piece-one"", ""name"": ""Piece One"", ""description"": ""d"",
              ""tab"": ""Tab A"", ""addin"": ""One.addin"", ""assembly"": ""One.dll"",
              ""folder"": ""One"", ""addInId"": ""11111111-1111-1111-1111-111111111111"",
              ""revit"": [""2024""], ""requires"": [], ""version"": ""0.1.0"",
              ""partOf"": null, ""state"": ""SHIPPED"" }
          ]
        }";

        internal static void Run(Action<bool, string> check, Func<string, string[], bool> names)
        {
            // A PORT NOBODY ELSE IS LIKELY TO HOLD, and a real one: binding it
            // is part of what is being proved. If it is taken the whole
            // section says so rather than reporting a false pass.
            var port = 47_821;
            var work = Path.Combine(Path.GetTempPath(),
                                    "heron-release-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(work);

            FakeRelease release;
            try
            {
                release = new FakeRelease(port);
            }
            catch (Exception e)
            {
                check(false, "a local server could be started on port " + port +
                             " - it could not, so NOTHING in this section ran (" +
                             e.Message + ")");
                return;
            }

            try
            {
                var manifest = ProductManifest.Parse(Fixture);
                var product = manifest.Find("piece-one");

                var good = ZipOf("One.dll", "not a real assembly",
                                 "One.deps.json", "{}");
                release.Put(ReleaseAssets.ManifestName, Fixture);
                release.Put("piece-one-2024.zip", good);
                release.Checksum();

                Console.WriteLine();
                Console.WriteLine("A PUBLISHED RELEASE IS READ, VERIFIED AND UNPACKED - Stage 5");
                using (var download = new ReleaseDownload(release.BaseUrl, work, 30))
                {
                    string why;
                    var fromRelease = download.Manifest(out why);
                    check(fromRelease != null,
                          "the product list is read from the RELEASE, not from a branch: " +
                          (why ?? "read"));
                    check(fromRelease != null && fromRelease.Find("piece-one") != null,
                          "and it holds the product the release published");

                    var folder = download.Folder(product, "2024", out why);
                    check(folder != null, "the asset downloads and verifies: " + (why ?? "ok"));
                    check(folder != null && File.Exists(Path.Combine(folder, "One.dll")),
                          "and it is unpacked where the deploy script can be pointed at it");
                    check(folder != null && File.Exists(Path.Combine(folder, "One.deps.json")),
                          "with every file in the asset, not only the assembly");

                    var again = download.Folder(product, "2024", out why);
                    check(again == folder,
                          "asking twice does not download twice - a window with three "
                          + "Revits ticked fetches each asset once");
                }

                Console.WriteLine();
                Console.WriteLine("A FILE THAT DID NOT ARRIVE WHOLE IS REFUSED - R-12");
                // The bytes are checked while still in memory, so a file that
                // fails is never written to disk at all.
                var tampered = new FakeRelease(port + 1);
                try
                {
                    tampered.Put(ReleaseAssets.ManifestName, Fixture);
                    tampered.Put("piece-one-2024.zip", good);
                    tampered.Checksum();
                    tampered.Put("piece-one-2024.zip", ZipOf("One.dll", "something else entirely"));

                    var into = Path.Combine(work, "tampered");
                    Directory.CreateDirectory(into);
                    using (var download = new ReleaseDownload(tampered.BaseUrl, into, 30))
                    {
                        string why;
                        var folder = download.Folder(product, "2024", out why);
                        check(folder == null, "a changed file is not installed");
                        check(names(why, new[] { "did not arrive whole" }),
                              "and the message says what happened: " + why);
                        check(!names(why, new[] { "error" }),
                              "without the word 'error' - docs/14");
                        check(Directory.GetFileSystemEntries(into).Length == 0,
                              "AND NOTHING WAS WRITTEN TO DISK - the check happens "
                              + "before the first byte lands, so there is no half-"
                              + "unpacked folder to regret");
                    }
                }
                finally { tampered.Dispose(); }

                Console.WriteLine();
                Console.WriteLine("A FILE NOBODY PUBLISHED IS REFUSED");
                // checksums.txt is written last, over everything in the folder,
                // so a name missing from it was added afterwards by something
                // that is not the builder.
                var extra = new FakeRelease(port + 2);
                try
                {
                    extra.Put(ReleaseAssets.ManifestName, Fixture);
                    extra.Put("piece-one-2024.zip", good);
                    extra.Checksum("piece-one-2024.zip");

                    using (var download = new ReleaseDownload(extra.BaseUrl,
                                                              Path.Combine(work, "extra"), 30))
                    {
                        string why;
                        check(download.Folder(product, "2024", out why) == null,
                              "an asset missing from checksums.txt is not installed");
                        check(names(why, new[] { "not part of this release" }),
                              "and the message says why: " + why);
                    }
                }
                finally { extra.Dispose(); }

                Console.WriteLine();
                Console.WriteLine("A RELEASE WITH NO CHECKSUMS AT ALL IS REFUSED");
                var unchecked_ = new FakeRelease(port + 3);
                try
                {
                    unchecked_.Put(ReleaseAssets.ManifestName, Fixture);
                    unchecked_.Put("piece-one-2024.zip", good);

                    using (var download = new ReleaseDownload(unchecked_.BaseUrl,
                                                              Path.Combine(work, "none"), 30))
                    {
                        string why;
                        check(download.Folder(product, "2024", out why) == null,
                              "nothing is installed from a release that cannot be checked");
                        check(names(why, new[] { "no way to tell" }),
                              "and it says so rather than installing hopefully: " + why);
                    }
                }
                finally { unchecked_.Dispose(); }

                Console.WriteLine();
                Console.WriteLine("EVERY FAILURE NAMES ITS CAUSE - R-14, not the word 'error'");
                using (var download = new ReleaseDownload(release.BaseUrl, work, 30))
                {
                    string why;
                    var missing = manifest.Find("piece-one");
                    check(download.Folder(missing, "2099", out why) == null,
                          "a release with no asset for that Revit installs nothing");
                    check(names(why, new[] { "not in that release" }),
                          "and says the release is missing it: " + why);
                }

                release.Forbid("piece-one-2026.zip");
                using (var download = new ReleaseDownload(release.BaseUrl, work, 30))
                {
                    string why;
                    check(download.Folder(product, "2026", out why) == null,
                          "a refused connection installs nothing");
                    check(names(why, new[] { "firewall", "IT" }),
                          "and it points at the network rather than at Heron: " + why);
                }

                using (var download = new ReleaseDownload("http://127.0.0.1:1/nothing",
                                                          work, 30))
                {
                    string why;
                    check(download.Folder(product, "2024", out why) == null,
                          "a server that is not there installs nothing");
                    check(why != null && !names(why, new[] { "error" }),
                          "and still never says 'error': " + why);
                }

                Console.WriteLine();
                Console.WriteLine("A ZIP THAT WOULD WRITE OUTSIDE ITS FOLDER IS REFUSED WHOLE");
                // `..\..\somewhere` is a legal name in the zip format, and an
                // extractor that joins it to a destination writes exactly
                // there. The whole archive is refused rather than the one
                // entry skipped: a zip carrying one is not a release with a
                // mistake in it.
                var escaping = new FakeRelease(port + 4);
                try
                {
                    var evil = ZipOf("../escaped.txt", "should never be written",
                                     "One.dll", "not a real assembly");
                    escaping.Put(ReleaseAssets.ManifestName, Fixture);
                    escaping.Put("piece-one-2024.zip", evil);
                    escaping.Checksum();

                    var into = Path.Combine(work, "slip");
                    Directory.CreateDirectory(into);
                    using (var download = new ReleaseDownload(escaping.BaseUrl, into, 30))
                    {
                        string why;
                        check(download.Folder(product, "2024", out why) == null,
                              "the archive is refused");
                        check(names(why, new[] { "outside the folder" }),
                              "and says what was wrong with it: " + why);
                        check(!File.Exists(Path.Combine(work, "escaped.txt"))
                              && !File.Exists(Path.Combine(into, "..", "escaped.txt")),
                              "and the file it tried to place is not there");
                    }
                }
                finally { escaping.Dispose(); }
            }
            finally
            {
                release.Dispose();
                try { Directory.Delete(work, true); } catch (IOException) { }
            }
        }
    }
}
