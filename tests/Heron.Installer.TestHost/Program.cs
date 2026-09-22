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
using Heron.Installer;

namespace Heron.Installer.TestHost
{
    /// <summary>
    /// Every rule the install engine decides, against a fake Revit.
    ///
    /// WHAT THIS PROVES: that the engine skips what it should skip and says
    /// why, waits while Revit is open, carries on when it closes, stops
    /// cleanly when it cannot, and lets one product fail without taking the
    /// others with it.
    ///
    /// WHAT IT CANNOT PROVE: that anything installs. Nothing is written
    /// outside one temporary folder of its own - which BuildsOnDisk is read
    /// against, and which is deleted after - no Revit is looked for and no
    /// PowerShell runs. Those need Windows, and a green run here is not an
    /// install.
    /// </summary>
    internal static class Program
    {
        private static readonly List<string> Failures = new List<string>();

        private static void Check(bool condition, string what)
        {
            Console.WriteLine("  " + (condition ? "ok  " : "FAIL") + "  " + what);
            if (!condition) Failures.Add(what);
        }

        // ------------------------------------------------------------ fakes

        private sealed class FakeRevit : IRevitEnvironment
        {
            private readonly List<string> _installed;
            private readonly List<RunningRevit>[] _sequence;
            private int _call;

            /// <summary>
            /// `sequence` is what RunningRevits() returns on each successive
            /// call - which is how "Revit was open, then the user closed it"
            /// is expressed without a clock. The last entry repeats for ever.
            /// </summary>
            public FakeRevit(IEnumerable<string> installed, params List<RunningRevit>[] sequence)
            {
                _installed = new List<string>(installed);
                _sequence = sequence.Length == 0
                    ? new[] { new List<RunningRevit>() }
                    : sequence;
            }

            public int Calls { get { return _call; } }

            public IReadOnlyList<string> InstalledReleases() { return _installed; }

            /// <summary>
            /// A believable answer with no Windows in it. The real one comes
            /// from tools\HeronRevit.ps1; what the engine and the screen do
            /// with it is the same either way, and a release this fake does
            /// not have answers null, which is what a machine without that
            /// Revit does.
            /// </summary>
            public string AddinsFolder(string release)
            {
                return _installed.Contains(release)
                    ? "/fake/Addins/" + release
                    : null;
            }

            public IReadOnlyList<RunningRevit> RunningRevits()
            {
                var index = Math.Min(_call, _sequence.Length - 1);
                _call++;
                return _sequence[index];
            }
        }

        private sealed class FakeDeployer : IProductDeployer
        {
            private readonly Func<HeronProduct, string, DeployOutcome> _behaviour;
            private readonly Func<HeronProduct, string, DeployOutcome> _removal;
            public readonly List<string> Deployed = new List<string>();

            /// <summary>
            /// Everything this was asked to do, IN ORDER and with the verb.
            ///
            /// The order is a rule rather than a detail - removals happen
            /// before installs, so a run that takes one product off and puts
            /// another on cannot leave both.
            /// </summary>
            public readonly List<string> Did = new List<string>();

            public FakeDeployer(Func<HeronProduct, string, DeployOutcome> behaviour)
                : this(behaviour, null)
            {
            }

            public FakeDeployer(Func<HeronProduct, string, DeployOutcome> behaviour,
                                Func<HeronProduct, string, DeployOutcome> removal)
            {
                _behaviour = behaviour;
                _removal = removal;
            }

            public DeployOutcome Deploy(HeronProduct product, string release)
            {
                Deployed.Add(product.Id + "@" + release);
                Did.Add("install " + product.Id + "@" + release);
                return _behaviour(product, release);
            }

            public DeployOutcome Remove(HeronProduct product, string release)
            {
                Did.Add("remove " + product.Id + "@" + release);
                return _removal != null
                    ? _removal(product, release)
                    : DeployOutcome.Ok("'" + product.Name + "' removed from Revit " + release + ".");
            }
        }

        private static List<RunningRevit> Open(params string[] releases)
        {
            var list = new List<RunningRevit>();
            var pid = 1000;
            foreach (var r in releases)
                list.Add(new RunningRevit { Release = r, ProcessId = pid++ });
            return list;
        }

        private static List<RunningRevit> Unknown()
        {
            return new List<RunningRevit> { new RunningRevit { Release = null, ProcessId = 4242 } };
        }

        // A manifest with the same SHAPE as the real one - a heading with two
        // pieces, a shipped tab, a proving one - but named nothing real, so
        // no product id is hardcoded anywhere outside this fixture.
        private const string Fixture = @"{
          ""heron-agent"": ""none"", ""heron-step"": 18, ""heron-status"": ""DRAFT"",
          ""heron-since"": ""0.1.0"", ""heron-layer"": ""platform"",
          ""products"": [
            { ""id"": ""tab-a"", ""name"": ""Tab A"", ""description"": ""d"", ""tab"": ""Tab A"",
              ""addin"": null, ""assembly"": null, ""addInId"": null, ""folder"": null,
              ""revit"": [""2024"",""2025""], ""requires"": [], ""version"": ""0.1.0"",
              ""partOf"": null, ""state"": ""SHIPPED"" },
            { ""id"": ""piece-one"", ""name"": ""Piece One"", ""description"": ""d"", ""tab"": ""Tab A"",
              ""addin"": ""One.addin"", ""assembly"": ""One.dll"", ""folder"": ""One"",
              ""addInId"": ""11111111-1111-1111-1111-111111111111"",
              ""revit"": [""2024"",""2025""], ""requires"": [], ""version"": ""0.1.0"",
              ""partOf"": ""tab-a"", ""state"": ""SHIPPED"" },
            { ""id"": ""piece-two"", ""name"": ""Piece Two"", ""description"": ""d"", ""tab"": ""Tab A"",
              ""addin"": ""Two.addin"", ""assembly"": ""Two.dll"", ""folder"": ""Two"",
              ""addInId"": ""22222222-2222-2222-2222-222222222222"",
              ""revit"": [""2024""], ""requires"": [], ""version"": ""0.1.0"",
              ""partOf"": ""tab-a"", ""state"": ""SHIPPED"" },
            { ""id"": ""tab-b"", ""name"": ""Tab B"", ""description"": ""d"", ""tab"": ""Tab B"",
              ""addin"": ""B.addin"", ""assembly"": ""B.dll"", ""folder"": ""B"",
              ""addInId"": ""33333333-3333-3333-3333-333333333333"",
              ""revit"": [""2024"",""2025""], ""requires"": [], ""version"": ""0.1.0"",
              ""partOf"": null, ""state"": ""PROVING"" }
          ]
        }";

        // A SECOND FIXTURE, for the screen rather than the engine. It carries
        // one more shape than the one above: a product that does not exist
        // yet. The window has to say something different about "being tested"
        // and "not built", and the engine never sees either.
        private const string ScreenFixture = @"{
          ""products"": [
            { ""id"": ""tab-a"", ""name"": ""Tab A"", ""description"": ""the first tab"",
              ""tab"": ""Tab A"", ""addin"": null, ""assembly"": null, ""addInId"": null,
              ""folder"": null, ""revit"": [""2024"",""2025""], ""requires"": [],
              ""version"": ""0.1.0"", ""partOf"": null, ""state"": ""SHIPPED"" },
            { ""id"": ""piece-one"", ""name"": ""Piece One"", ""description"": ""the connector"",
              ""tab"": ""Tab A"", ""addin"": ""One.addin"", ""assembly"": ""One.dll"",
              ""folder"": ""One"", ""addInId"": ""11111111-1111-1111-1111-111111111111"",
              ""revit"": [""2024"",""2025""], ""requires"": [], ""version"": ""0.1.0"",
              ""partOf"": ""tab-a"", ""state"": ""SHIPPED"" },
            { ""id"": ""piece-two"", ""name"": ""Piece Two"", ""description"": ""the tools"",
              ""tab"": ""Tab A"", ""addin"": ""Two.addin"", ""assembly"": ""Two.dll"",
              ""folder"": ""Two"", ""addInId"": ""22222222-2222-2222-2222-222222222222"",
              ""revit"": [""2024""], ""requires"": [], ""version"": ""0.1.0"",
              ""partOf"": ""tab-a"", ""state"": ""SHIPPED"" },
            { ""id"": ""tab-b"", ""name"": ""Tab B"", ""description"": ""being tested"",
              ""tab"": ""Tab B"", ""addin"": ""B.addin"", ""assembly"": ""B.dll"",
              ""folder"": ""B"", ""addInId"": ""33333333-3333-3333-3333-333333333333"",
              ""revit"": [""2024"",""2025""], ""requires"": [], ""version"": ""0.1.0"",
              ""partOf"": null, ""state"": ""PROVING"" },
            { ""id"": ""tab-c"", ""name"": ""Tab C"", ""description"": ""not built yet"",
              ""tab"": ""Tab C"", ""addin"": ""C.addin"", ""assembly"": ""C.dll"",
              ""folder"": ""C"", ""addInId"": ""44444444-4444-4444-4444-444444444444"",
              ""revit"": [""2024"",""2025""], ""requires"": [], ""version"": ""0.1.0"",
              ""partOf"": null, ""state"": ""PLANNED"" }
          ]
        }";

        /// <summary>Named product-and-release pairs, and nothing else.</summary>
        private sealed class FakeInstalled : IInstalledProducts
        {
            private readonly List<string> _pairs;

            public FakeInstalled(params string[] pairs)
            {
                _pairs = new List<string>(pairs);
            }

            public bool IsInstalled(HeronProduct product, string release)
            {
                return _pairs.Contains(product.Id + "@" + release);
            }
        }

        /// <summary>
        /// Named product-and-release pairs that have a build, and nothing
        /// else. The same shape as FakeInstalled above, and for the same
        /// reason: what is on a disk is not a rule, so it is handed in.
        /// </summary>
        private sealed class FakeBuilds : IProductBuilds
        {
            private readonly List<string> _pairs;

            public FakeBuilds(params string[] pairs)
            {
                _pairs = new List<string>(pairs);
            }

            public bool HasBuild(HeronProduct product, string release)
            {
                return _pairs.Contains(product.Id + "@" + release);
            }

            public string BuildCommand(HeronProduct product, string release)
            {
                return "dotnet build " + product.Assembly + " for " + release;
            }
        }

        /// <summary>
        /// One fake build on disk, at revit\&lt;project&gt;\bin\&lt;rest...&gt;.
        /// Written with Path.Combine so the separator is the machine's own -
        /// the adapter has to cope with both, and this suite runs on the one
        /// Windows does not use.
        /// </summary>
        private static void Lay(string root, string project, params string[] rest)
        {
            var path = Path.Combine(root, "revit", project, "bin");
            foreach (var part in rest) path = Path.Combine(path, part);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "not a real assembly");
        }

        private static bool Has(IReadOnlyList<string> list, string item)
        {
            foreach (var one in list) if (one == item) return true;
            return false;
        }

        private static ProductRow RowFor(InstallerScreen screen, string id)
        {
            foreach (var row in screen.Products) if (row.Id == id) return row;
            return null;
        }

        private static SkippedStep SkipFor(InstallPlan plan, string id, string release)
        {
            foreach (var s in plan.Skipped)
                if (s.ProductId == id && s.Release == release) return s;
            return null;
        }

        private static bool Names(string text, params string[] words)
        {
            if (text == null) return false;
            foreach (var w in words)
                if (text.IndexOf(w, StringComparison.OrdinalIgnoreCase) < 0) return false;
            return true;
        }

        // ------------------------------------------------------------- main

        private static int Main(string[] args)
        {
            var manifest = ProductManifest.Parse(Fixture);

            Console.WriteLine("Reading the product list");
            Check(manifest.Products.Count == 4, "four products");
            Check(manifest.Find("tab-a") != null, "a product is found by id");
            Check(manifest.Find("nope") == null, "an unknown id returns nothing, it does not throw");
            Check(manifest.Find("tab-a").IsHeading,
                  "tab-a is a HEADING - derived from partOf, never a field (D-93)");
            Check(!manifest.Find("piece-one").IsHeading, "a piece is not a heading");
            Check(!manifest.Find("tab-b").IsHeading,
                  "a tab nothing joins is not a heading either - it installs itself");
            Check(manifest.PiecesOf("tab-a").Count == 2, "the heading has two pieces");
            Check(manifest.Find("piece-one").MayBeOffered, "a SHIPPED product may be offered");
            Check(!manifest.Find("tab-b").MayBeOffered,
                  "a PROVING product may NOT be offered - it carries a dummy button");

            Console.WriteLine();
            Console.WriteLine("A damaged product list says so in words a modeller can act on");
            try
            {
                ProductManifest.Parse("{ not json");
                Check(false, "bad JSON is refused");
            }
            catch (InvalidDataException e)
            {
                Check(true, "bad JSON is refused");
                Check(Names(e.Message, "damaged", "again"),
                      "and the message says it is damaged and to fetch it again");
                Check(!Names(e.Message, "exception"), "and does not say 'exception'");
            }

            Console.WriteLine();
            Console.WriteLine("The plan skips what it must, and never silently");
            var plan = InstallPlan.Build(manifest,
                new[] { "tab-a", "tab-b", "ghost", "piece-two" },
                new[] { "2024", "2025" },
                new[] { "2024", "2025" });

            Check(SkipFor(plan, "ghost", null) != null, "an unknown product id is skipped");
            Check(Names(SkipFor(plan, "ghost", null).Explanation, "no product called"),
                  "and the message names it");

            var heading = SkipFor(plan, "tab-a", null);
            Check(heading != null && heading.Reason == SkipReason.IsAHeading,
                  "a HEADING installs nothing of its own");
            Check(Names(heading.Explanation, "piece-one", "piece-two"),
                  "and it lists the pieces to choose instead");

            var proving = SkipFor(plan, "tab-b", null);
            Check(proving != null && proving.Reason == SkipReason.NotOfferable,
                  "a PROVING product is never offered");

            var unsupported = SkipFor(plan, "piece-two", "2025");
            Check(unsupported != null && unsupported.Reason == SkipReason.ReleaseNotSupported,
                  "a product that does not support a release is skipped - R-10");
            Check(Names(unsupported.Explanation, "2024"),
                  "and the message says which releases it DOES support");

            Check(plan.Steps.Count == 1 && plan.Steps[0].ToString() == "piece-two for Revit 2024",
                  "leaving exactly the one pair that can really be installed");

            Console.WriteLine();
            Console.WriteLine("A release ticked but not on the PC, one the product cannot use,");
            Console.WriteLine("and one on the PC nobody ticked - told apart, in one plan");
            //
            // piece-one supports 2024 and 2025. This PC has 2024 and 2027.
            // Ticked: 2024, 2025, 2026. So one of each case falls out, and
            // 2026 is the one that used to come back wrong.
            var mixed = InstallPlan.Build(manifest, new[] { "piece-one" },
                                          new[] { "2024", "2025", "2026" },
                                          new[] { "2024", "2027" });

            var absent = SkipFor(mixed, "piece-one", "2025");
            Check(absent != null && absent.Reason == SkipReason.ReleaseNotInstalled,
                  "ticking a Revit that is not installed is skipped, not attempted");
            Check(Names(absent.Explanation, "Install Revit 2025 first"),
                  "and it says to install that Revit, which here really would help");

            // THE CASE THAT USED TO GIVE ADVICE THAT CANNOT WORK. 2026 is
            // neither on this PC nor supported by the product, and until
            // 2026-09-21 the plan answered "Revit 2026 is not installed on
            // this PC ... Install Revit 2026 first" - which would change
            // nothing, because piece-one does not run on 2026 either. The
            // old version of THIS suite pinned that sentence. Row 5b-80.
            var neither = SkipFor(mixed, "piece-one", "2026");
            Check(neither != null && neither.Reason == SkipReason.ReleaseNotSupported,
                  "a release that is BOTH absent and unsupported reads as "
                  + "unsupported - the stronger reason, and the only honest one");
            Check(Names(neither.Explanation, "does not support Revit 2026", "2024, 2025"),
                  "and it names what the product DOES support instead of "
                  + "telling somebody to install a Revit that will not help");
            Check(!Names(neither.Explanation, "Install Revit 2026 first"),
                  "so the instruction that cannot work is not given at all");

            var untouched = SkipFor(mixed, null, "2027");
            Check(untouched != null && untouched.Reason == SkipReason.ReleaseNotChosen,
                  "a Revit on the PC that was NOT ticked is reported too");
            Check(Names(untouched.Explanation, "not ticked"),
                  "so nobody opens it next week wondering where Heron went");

            Check(mixed.Steps.Count == 1 && mixed.Steps[0].ToString() == "piece-one for Revit 2024",
                  "and exactly one pair survives all three");

            Console.WriteLine();
            Console.WriteLine("It WAITS for Revit, it does not work around it - R-38a");
            var closing = new FakeRevit(new[] { "2024" },
                Open("2024"), Open("2024"), Open("2024"), Open());
            var deployer = new FakeDeployer((p, r) => DeployOutcome.Ok("installed"));
            var waited = new List<string>();
            var engine = new InstallEngine(closing, deployer)
            {
                OnWaiting = m => waited.Add(m),
                Pause = () => { },
            };

            var report = engine.Install(manifest, new[] { "piece-one" }, new[] { "2024" });
            Check(waited.Count == 3, "it looked again each time rather than giving up");
            Check(Names(waited[0], "Revit 2024 is open", "nothing has been changed"),
                  "and said which release, and that nothing had been changed yet");
            Check(report.Installed == 1 && !report.Abandoned,
                  "then carried on BY ITSELF once Revit closed - no restarting it");
            Check(deployer.Deployed.Count == 1 && deployer.Deployed[0] == "piece-one@2024",
                  "and deployed exactly once, after the wait");

            Console.WriteLine();
            Console.WriteLine("A Revit whose release cannot be read blocks everything");
            var murky = new InstallEngine(new FakeRevit(new[] { "2024" }, Unknown()),
                                          new FakeDeployer((p, r) => DeployOutcome.Ok("x")),
                                          new WaitPolicy { MaxChecks = 2 })
            { Pause = () => { } };
            var murkyReport = murky.Install(manifest, new[] { "piece-one" }, new[] { "2024" });
            Check(murkyReport.Abandoned, "it stops rather than guessing");
            Check(Names(murkyReport.AbandonedBecause, "could not be read"),
                  "and says why");
            Check(murkyReport.Results.Count == 0, "having installed nothing at all");

            Console.WriteLine();
            Console.WriteLine("Another release being open does NOT block this one");
            var elsewhere = new InstallEngine(
                new FakeRevit(new[] { "2020", "2024" }, Open("2024")),
                new FakeDeployer((p, r) => DeployOutcome.Ok("installed")),
                new WaitPolicy { MaxChecks = 2 })
            { Pause = () => { } };
            var elsewhereReport = elsewhere.Install(manifest, new[] { "piece-one" }, new[] { "2020" });
            Check(!elsewhereReport.Abandoned,
                  "Revit 2024 open says nothing about installing for 2020");

            Console.WriteLine();
            Console.WriteLine("Waiting has a ceiling, and reaching it changes nothing");
            var forever = new FakeRevit(new[] { "2024" }, Open("2024"));
            var patient = new InstallEngine(forever,
                                            new FakeDeployer((p, r) => DeployOutcome.Ok("x")),
                                            new WaitPolicy { MaxChecks = 5 })
            { Pause = () => { } };
            var gaveUp = patient.Install(manifest, new[] { "piece-one" }, new[] { "2024" });
            Check(gaveUp.Abandoned, "it stops instead of waiting for ever");
            Check(gaveUp.Results.Count == 0, "and nothing was installed");
            Check(Names(gaveUp.AbandonedBecause, "Revit 2024 is open"),
                  "and the last word is which Revit is still open");

            Console.WriteLine();
            Console.WriteLine("One product failing does not stop the others - R-19");
            var partial = new FakeDeployer((p, r) =>
                p.Id == "piece-two"
                    ? DeployOutcome.Failed("'" + p.Name + "' could not be copied. The download is damaged.")
                    : DeployOutcome.Ok("installed"));
            var mixedEngine = new InstallEngine(new FakeRevit(new[] { "2024" }), partial);
            var mixedReport = mixedEngine.Install(manifest,
                new[] { "piece-one", "piece-two" }, new[] { "2024" });

            Check(mixedReport.Results.Count == 2, "both were attempted");
            Check(mixedReport.Installed == 1 && mixedReport.Failed == 1,
                  "one worked and one did not");
            Check(partial.Deployed.Count == 2,
                  "the second was tried even though the first list entry failed");
            foreach (var r in mixedReport.Results)
                if (!r.Succeeded)
                    Check(Names(r.Message, "damaged"), "and the failure says why, by name");

            Console.WriteLine();
            Console.WriteLine("A deployer that throws is caught, and the rest still run");
            var throwing = new FakeDeployer((p, r) =>
            {
                if (p.Id == "piece-one") throw new IOException("the folder is in use");
                return DeployOutcome.Ok("installed");
            });
            var caught = new InstallEngine(new FakeRevit(new[] { "2024" }), throwing)
                .Install(manifest, new[] { "piece-one", "piece-two" }, new[] { "2024" });
            Check(caught.Results.Count == 2, "an exception did not abandon the run");
            Check(caught.Installed == 1 && caught.Failed == 1, "one failed, one worked");

            Console.WriteLine();
            Console.WriteLine("Nothing to do is not a failure");
            var nothing = new InstallEngine(new FakeRevit(new[] { "2024" }),
                                            new FakeDeployer((p, r) => DeployOutcome.Ok("x")))
                .Install(manifest, new string[0], new[] { "2024" });
            Check(nothing.Results.Count == 0 && !nothing.Abandoned,
                  "an empty choice installs nothing and reports no failure");

            // ================================================== STAGE 4
            var screenManifest = ProductManifest.Parse(ScreenFixture);

            Console.WriteLine();
            Console.WriteLine("THE WINDOW'S LIST IS THE FILE - R-3, the point of the whole design");
            var screen = InstallerScreen.Build(screenManifest, new[] { "2024", "2025" },
                                               null, null, null);
            Check(screen.Products.Count == screenManifest.Products.Count,
                  "every product in the list has a row, and no row has any other source");
            Check(RowFor(screen, "tab-c") != null,
                  "including one added to the file and never mentioned in code");

            Console.WriteLine();
            Console.WriteLine("The tab built by two pieces opens into two ticks - R-33, R-36");
            var tabRow = RowFor(screen, "tab-a");
            Check(tabRow != null && tabRow.IsHeading,
                  "the tab is a heading, DERIVED from partOf and never a field");
            Check(!RowFor(screen, "tab-b").IsHeading,
                  "and a tab nothing is part of is one tick, whole tab");
            Check(screen.Products[1].IsPiece && screen.Products[2].IsPiece,
                  "its pieces are drawn under it, indented");
            Check(screen.Products[0] == tabRow,
                  "and the heading comes first, not wherever the file listed it");

            Console.WriteLine();
            Console.WriteLine("Ticking the tab installs its pieces, because a tab installs nothing");
            Check(tabRow.Installs.Count == 2, "the heading's tick carries both pieces");
            var rolled = screen.ToInstall(new[] { "tab-a" });
            Check(rolled.Count == 2 && Has(rolled, "piece-one") && Has(rolled, "piece-two"),
                  "and the engine is handed the pieces, never the heading");
            var toolsOnly = screen.ToInstall(new[] { "piece-two" });
            Check(toolsOnly.Count == 1 && toolsOnly[0] == "piece-two",
                  "ONE PIECE WITHOUT THE OTHER IS A SUPPORTED INSTALL - R-34");

            Console.WriteLine();
            Console.WriteLine("A product that cannot be installed is GREYED WITH THE REASON - R-10");
            var provingRow = RowFor(screen, "tab-b");
            Check(!provingRow.CanBeTicked, "one being tested cannot be ticked");
            Check(Names(provingRow.WhyNot, "not ready", "does nothing"),
                  "and the row says why, in words a modeller uses: " + provingRow.WhyNot);
            var planned = RowFor(screen, "tab-c");
            Check(!planned.CanBeTicked, "one that does not exist yet cannot be ticked either");
            Check(Names(planned.WhyNot, "not built yet"),
                  "and it is told apart from the one being tested: " + planned.WhyNot);
            Check(!Names(provingRow.WhyNot, "PROVING") && !Names(planned.WhyNot, "PLANNED"),
                  "neither sentence uses this repository's own vocabulary");
            Check(screen.ToInstall(new[] { "tab-b", "tab-c" }).Count == 0,
                  "and a greyed row installs nothing even if its tick arrives set");

            Console.WriteLine();
            Console.WriteLine("A product that does not run on the Revit found here says SO");
            var only2025 = InstallerScreen.Build(screenManifest, new[] { "2025" }, null, null, null);
            var narrow = RowFor(only2025, "piece-two");
            Check(!narrow.CanBeTicked, "the piece that stops at 2024 is greyed on a 2025-only PC");
            Check(Names(narrow.WhyNot, "2025", "2024"),
                  "and the reason names both what is here and what it supports: " + narrow.WhyNot);
            Check(RowFor(only2025, "tab-a").Installs.Count == 1,
                  "so the tab's tick carries only the piece that can be installed");

            Console.WriteLine();
            Console.WriteLine("Install replaces, and the window says so BEFORE it is pressed - R-23a");
            var some = InstallerScreen.Build(screenManifest, new[] { "2024", "2025" }, null,
                                             new FakeInstalled("piece-one@2024", "piece-one@2025"),
                                             null);
            Check(RowFor(some, "piece-one").State == "Installed",
                  "an installed piece reads Installed");
            Check(Names(RowFor(some, "piece-one").IfYouInstallAgain,
                        "replaces", "no separate Update"),
                  "and it says what Install will do, because there is no Update button");
            Check(RowFor(some, "piece-two").State == "--",
                  "one that is not there reads --, not a guess");
            Check(RowFor(some, "piece-one").CanBeTicked,
                  "AND IT STAYS TICKABLE - an install is how you replace it");

            Console.WriteLine();
            Console.WriteLine("Installed for some releases is not Installed");
            var partly = InstallerScreen.Build(screenManifest, new[] { "2024", "2025" }, null,
                                               new FakeInstalled("piece-one@2024"), null);
            Check(Names(RowFor(partly, "piece-one").State, "Installed for Revit 2024"),
                  "it names the release, rather than rounding up to Installed: "
                  + RowFor(partly, "piece-one").State);

            Console.WriteLine();
            Console.WriteLine("Every Revit found is offered, and ticked to begin with - R-8");
            Check(screen.Releases.Count == 2, "both releases on the PC are offered");
            Check(screen.ChosenReleases().Count == 2, "and both start ticked");
            Check(screen.Releases[0].Release == "2024",
                  "in order, so the list does not shuffle between runs");

            Console.WriteLine();
            Console.WriteLine("A RELEASE WITH NOTHING BUILT FOR IT IS GREYED - before Install");
            //
            // FOUND BY WATCHING THE OWNER USE THE WINDOW, 2026-09-21. Every
            // Revit on the PC could be ticked whether or not anything had been
            // built for it, and the only way to find out was to press Install
            // and read the refusal. He hit that five times in one evening.
            // R-10 has said since Stage 4 that a row which cannot be installed
            // is greyed WITH THE REASON; the release row was the one row it
            // had never been applied to.
            var nothingBuilt = InstallerScreen.Build(screenManifest,
                                                     new[] { "2024", "2025" },
                                                     null, null,
                                                     new FakeBuilds("piece-one@2024"));
            var built2024 = nothingBuilt.Releases[0];
            var bare2025 = nothingBuilt.Releases[1];

            Check(built2024.Release == "2024" && built2024.CanBeTicked,
                  "a release with a build stays tickable");
            Check(built2024.WhyNot == null,
                  "and says nothing, because there is nothing to say");
            Check(!bare2025.CanBeTicked,
                  "a release with NO build for any product is greyed");
            Check(!bare2025.Chosen,
                  "and starts unticked, so it cannot be sent by a window that "
                  + "drew it before this rule existed");
            Check(Names(bare2025.WhyNot, "Nothing has been built for Revit 2025"),
                  "the reason names the release: " + bare2025.WhyNot);
            Check(Names(bare2025.WhyNot, "dotnet build"),
                  "and prints the command that fixes it, rather than leaving a "
                  + "modeller to find one");
            Check(!Names(bare2025.WhyNot, "error"),
                  "and never says the word 'error' - docs/14");
            Check(Has(nothingBuilt.ChosenReleases(), "2024")
                  && !Has(nothingBuilt.ChosenReleases(), "2025"),
                  "and the greyed one is not among the releases chosen");

            // THE TICK IS PUT BACK ON BY HAND, which is the only way this
            // check tests anything. Without it the release is already
            // unticked, so ChosenReleases drops it on Chosen alone and the
            // CanBeTicked guard could be deleted with nothing going red -
            // MEASURED on 2026-09-21 by deleting it, which is what this line
            // exists to make impossible. A window drawn before this rule
            // existed sends exactly this: a box that is ticked and should
            // not be offered.
            bare2025.Chosen = true;
            Check(!Has(nothingBuilt.ChosenReleases(), "2025"),
                  "and a greyed release installs nothing EVEN IF ITS TICK "
                  + "ARRIVES SET - the same rule a greyed product row has");
            Check(Has(nothingBuilt.ChosenReleases(), "2024"),
                  "while the one that can be installed is still chosen");

            Console.WriteLine();
            Console.WriteLine("ONE product with a build is enough to keep a release offered");
            // piece-two supports 2024 only, so on a 2024 PC both products are
            // candidates. A build for either means an install for that release
            // really would work, and the rows below say what else would not.
            var onlyOne = InstallerScreen.Build(screenManifest, new[] { "2024" },
                                                null, null,
                                                new FakeBuilds("piece-two@2024"));
            Check(onlyOne.Releases[0].CanBeTicked,
                  "the release is offered when any one product can be installed into it");

            Console.WriteLine();
            Console.WriteLine("A release nothing SUPPORTS is left alone, not greyed for the wrong reason");
            // Nothing in this fixture is both offerable and 2026-capable, so
            // there is no build to look for. Greying it would answer a
            // question nobody asked, in words that are not true - the product
            // rows already carry the real reason. Same ruling as row 5b-80.
            var future = InstallerScreen.Build(screenManifest, new[] { "2026" },
                                               null, null, new FakeBuilds());
            Check(future.Releases[0].CanBeTicked,
                  "it stays tickable rather than claiming nothing was built");
            Check(future.Releases[0].WhyNot == null, "and says nothing about builds");

            Console.WriteLine();
            Console.WriteLine("NOT KNOWING leaves every release tickable, which is the safe way round");
            // Greying a release that IS installable costs a modeller an
            // install they were entitled to, with a sentence telling them to
            // build something already built. Leaving one tickable costs a
            // refusal after the press - which is exactly where this stood
            // before, so it is no worse than nothing.
            var unknown = InstallerScreen.Build(screenManifest, new[] { "2024", "2025" },
                                                null, null, null);
            foreach (var r in unknown.Releases)
                Check(r.CanBeTicked && r.WhyNot == null,
                      "Revit " + r.Release + " is still offered when nothing could "
                      + "be found out about builds");

            Console.WriteLine();
            Console.WriteLine("No Revit at all is a sentence, not an empty window");
            var bare = InstallerScreen.Build(screenManifest, new string[0], null, null, null);
            Check(bare.Releases.Count == 0 && !bare.AnythingToOffer, "nothing can be offered");
            Check(Names(bare.NothingFound, "No Revit was found", "Install Revit first"),
                  "and it says so, and what to do: " + bare.NothingFound);

            Console.WriteLine();
            Console.WriteLine("Close Revit first is said BEFORE Install, not after it fails");
            Check(Names(screen.CloseRevitFirst, "Close Revit before installing"),
                  "the line is there with no Revit open at all");
            var busy = InstallerScreen.Build(screenManifest, new[] { "2024" },
                                             Open("2024"), null, null);
            Check(Names(busy.CloseRevitFirst, "2024", "open right now"),
                  "and it names the release when one is open: " + busy.CloseRevitFirst);
            var murkyScreen = InstallerScreen.Build(screenManifest, new[] { "2024" },
                                                    Unknown(), null, null);
            Check(Names(murkyScreen.CloseRevitFirst, "A Revit is open"),
                  "a Revit whose release cannot be read still says a Revit is open");

            Console.WriteLine();
            Console.WriteLine("The Addins folder is ASKED FOR, never built here");
            var asked = new FakeRevit(new[] { "2024" });
            Check(asked.AddinsFolder("2024") != null,
                  "a release that is installed has a folder");
            Check(asked.AddinsFolder("2026") == null,
                  "and one that is not installed has none, rather than a made-up path");
            var onDisk = new InstalledProductsOnDisk(asked);
            var aPiece = screenManifest.Find("piece-one");
            Check(!onDisk.IsInstalled(aPiece, "2026"),
                  "a folder that could not be found out reads as NOT installed");
            Check(!onDisk.IsInstalled(aPiece, "2024"),
                  "and a folder with nothing in it reads as not installed too");

            Console.WriteLine();
            Console.WriteLine("BuildsOnDisk looks where the DEPLOY SCRIPT looks, and nowhere else");
            //
            // THE ONE WINDOWS ADAPTER THAT CAN BE RUN HERE. It starts no
            // process and resolves no special folder - it is a directory read
            // under a root it is handed - so a real temporary folder is a
            // real test of it on any machine.
            //
            // WHAT IT MUST AGREE WITH. tools\deploy-addin.ps1 searches
            // revit\<project>\bin for the product's assembly, keeps the paths
            // carrying the configuration, then the ones carrying the release
            // as a WHOLE FOLDER. If this looked anywhere else the window would
            // grey a release the script would have installed - worse than the
            // defect it fixes, because it refuses work that was possible.
            var sandbox = Path.Combine(Path.GetTempPath(),
                                       "heron-builds-" + Guid.NewGuid().ToString("N"));
            try
            {
                var one = screenManifest.Find("piece-one");
                var two = screenManifest.Find("piece-two");

                // One.dll built for 2024 in Release, for 2025 in Debug only,
                // and a folder whose name merely STARTS with 2020.
                Lay(sandbox, "One", "x64", "Release", "2024", "One.dll");
                Lay(sandbox, "One", "x64", "Debug", "2025", "One.dll");
                Lay(sandbox, "One", "x64", "Release", "2020-old", "One.dll");

                var release = new BuildsOnDisk(sandbox, "Release");
                Check(release.HasBuild(one, "2024"),
                      "a build for the release, in the configuration asked for, is found");
                Check(!release.HasBuild(one, "2025"),
                      "A DEBUG BUILD IS NOT A RELEASE BUILD. The window deploys "
                      + "Release and nine correct Debug builds sat in a folder it "
                      + "never opens - trap 1, and it cost a round trip");
                Check(!release.HasBuild(one, "2020"),
                      "and '2020-old' is not Revit 2020 - the release is matched as "
                      + "a whole folder, exactly as the script matches it");
                Check(!release.HasBuild(two, "2024"),
                      "a product with no build of its own is not covered by another's");

                var debug = new BuildsOnDisk(sandbox, "Debug");
                Check(debug.HasBuild(one, "2025") && !debug.HasBuild(one, "2024"),
                      "and the configuration is asked, never assumed - the same "
                      + "disk answers differently for Debug");

                Check(Names(release.BuildCommand(one, "2025"),
                            "dotnet build", "One", "-c Release", "-p:RevitVersion=2025"),
                      "the command names the project, the configuration and the "
                      + "release, all derived: " + release.BuildCommand(one, "2025"));
                Check(!Names(release.BuildCommand(one, "2025"), ".dll"),
                      "and it says the PROJECT, not the assembly - .dll dropped, "
                      + "the way the deploy script derives it");

                var nowhere = new BuildsOnDisk(
                    Path.Combine(sandbox, "not-a-checkout"), "Release");
                Check(!nowhere.HasBuild(one, "2024"),
                      "a root with no revit folder answers no, rather than throwing");
            }
            finally
            {
                try { Directory.Delete(sandbox, true); } catch (IOException) { }
            }

            // ================================================== STAGE 6
            Console.WriteLine();
            Console.WriteLine("ROUTE 1 REFUSES ANY SOURCE BUT HERON'S OWN RELEASE - R-48, R-49");
            //
            // ENFORCED, NOT EXPECTED. docs/07 section 1a refused the shape
            // "point an AI at a URL and let it execute whatever it finds
            // there" in as many words - it is the exact shape of a supply
            // chain attack, and Golden Rule 19 says no text Heron reads may
            // raise its own permission level. A rule nothing enforces is one
            // the first user breaks by accident.
            //
            // WHO HERON IS COMES FROM THE MANIFEST, so this fixture names a
            // source of its own and no real owner appears in the checks.
            var mineManifest = ProductManifest.Parse(@"{
              ""source"": { ""owner"": ""owner-a"", ""repo"": ""repo-a"" },
              ""products"": [
                { ""id"": ""piece-one"", ""name"": ""Piece One"", ""description"": ""d"",
                  ""tab"": ""Tab A"", ""addin"": ""One.addin"", ""assembly"": ""One.dll"",
                  ""folder"": ""One"", ""addInId"": ""11111111-1111-1111-1111-111111111111"",
                  ""revit"": [""2024""], ""requires"": [], ""version"": ""0.1.0"",
                  ""partOf"": null, ""state"": ""SHIPPED"" }
              ]
            }");
            Check(mineManifest.KnowsItsSource,
                  "the manifest says which repository publishes Heron's releases");
            Check(mineManifest.SourceOwner == "owner-a" && mineManifest.SourceRepo == "repo-a",
                  "and it is read as data rather than written into the code");

            Console.WriteLine();
            Console.WriteLine("  what IS accepted");
            foreach (var good in new[]
            {
                "https://github.com/owner-a/repo-a/releases",
                "https://github.com/owner-a/repo-a/releases/latest",
                "https://GitHub.com/Owner-A/Repo-A/releases/latest",
                "  https://github.com/owner-a/repo-a/releases/latest  ",
            })
            {
                var verdict = InstallSource.Judge(good, mineManifest);
                Check(verdict.Accepted, "accepted: " + good.Trim() +
                                        (verdict.Accepted ? "" : " - " + verdict.Why));
                Check(verdict.Accepted && verdict.Tag == null,
                      "  and with no tag it means the newest published release");
                Check(verdict.Accepted && verdict.AssetsUrl != null
                      && verdict.AssetsUrl.Contains("releases/latest/download"),
                      "  and the assets URL is BUILT here, never taken from what was typed");
            }

            var tagged = InstallSource.Judge(
                "https://github.com/owner-a/repo-a/releases/tag/v0.1.0", mineManifest);
            Check(tagged.Accepted && tagged.Tag == "v0.1.0",
                  "a named release keeps its tag: " + (tagged.Tag ?? tagged.Why));
            Check(tagged.Accepted && tagged.AssetsUrl.EndsWith("/releases/download/v0.1.0"),
                  "and the assets URL points at that one: " + tagged.AssetsUrl);

            Console.WriteLine();
            Console.WriteLine("  and what is REFUSED - each one a real trick, not a typo");
            foreach (var bad in new[]
            {
                // somebody else's repository, on the right host
                "https://github.com/someone-else/repo-a/releases/latest",
                "https://github.com/owner-a/other-repo/releases/latest",
                // A NAME THAT ONLY STARTS THE SAME. "owner-a-evil" contains
                // the real owner, and a `StartsWith` check would pass it.
                "https://github.com/owner-a-evil/repo-a/releases/latest",
                "https://github.com/not-owner-a/repo-a/releases/latest",
                // A HOST THAT ONLY ENDS THE SAME, and one that only contains
                // it. Either passes a sloppy string test.
                "https://github.com.evil.example/owner-a/repo-a/releases/latest",
                "https://evil-github.com/owner-a/repo-a/releases/latest",
                "https://github.evil.example/owner-a/repo-a/releases/latest",
                // THE NAME BEFORE THE @ IS NOT THE HOST. This reads as
                // GitHub to a person and resolves to evil.example.
                "https://github.com@evil.example/owner-a/repo-a/releases/latest",
                "https://github.com:pass@evil.example/owner-a/repo-a/releases",
                // AND THE ONE CASE ONLY THE USER-INFO GUARD CATCHES. Here the
                // host really IS github.com, so the host check passes it -
                // deleting that guard was measured as breaking NOTHING until
                // this line existed, which made it a guard nobody could tell
                // was gone. No legitimate Heron link carries a name before
                // the @, and its only use in the wild is to mislead a reader.
                "https://evil@github.com/owner-a/repo-a/releases/latest",
                // not https - what arrives is whatever the network sent
                "http://github.com/owner-a/repo-a/releases/latest",
                "ftp://github.com/owner-a/repo-a/releases/latest",
                // the REPOSITORY rather than a release of it - R-48
                "https://github.com/owner-a/repo-a",
                "https://github.com/owner-a/repo-a/tree/main",
                "https://github.com/owner-a/repo-a/archive/refs/heads/main.zip",
                // not an address at all
                "install this repo",
                "",
                "   ",
                // a port of its own
                "https://github.com:8443/owner-a/repo-a/releases/latest",
            })
            {
                var verdict = InstallSource.Judge(bad, mineManifest);
                Check(!verdict.Accepted, "refused: " + (bad.Trim().Length == 0 ? "(nothing)" : bad));
                Check(!verdict.Accepted && !string.IsNullOrEmpty(verdict.Why),
                      "  and it says why rather than only saying no");
                Check(!verdict.Accepted && verdict.AssetsUrl == null,
                      "  and hands back no address at all, so nothing downstream can use one");
            }

            Console.WriteLine();
            Console.WriteLine("  every refusal names what WOULD be accepted");
            // A refusal that only says no leaves somebody guessing, and the
            // guess they make is usually to try harder rather than to try the
            // right thing.
            foreach (var bad in new[]
            {
                "https://github.com/someone-else/repo-a/releases/latest",
                "http://github.com/owner-a/repo-a/releases/latest",
                "https://github.com/owner-a/repo-a",
                "install this repo",
            })
            {
                var why = InstallSource.Judge(bad, mineManifest).Why;
                Check(Names(why, "owner-a/repo-a/releases"),
                      "the refusal for " + bad + " points at Heron's own release");
                Check(!Names(why, "error"), "  and never says 'error' - docs/14");
            }

            Console.WriteLine();
            Console.WriteLine("  HERON'S OWN repository is refused as the wrong THING, not the wrong OWNER");
            // FOUND BY RUNNING heron-install, not by reading the code. Asked
            // for https://github.com/owner-a/repo-a - Heron's own repository,
            // correct owner, correct name, just not a release - the refusal
            // came back saying "it is not Heron's own repository. Heron will
            // not install software from somebody else's".
            //
            // It IS his own. The refusal is right and the reason is false,
            // and a false reason sends somebody to check their address when
            // the address was never the problem. R-14 wants the cause named,
            // and this named a different one.
            //
            // WHY THE SUITE DID NOT CATCH IT: the case above checks the URL
            // is refused, and the case above that checks the refusal points
            // at owner-a/repo-a/releases - which the wrong sentence does too,
            // because every refusal ends with it. Nothing asked WHICH refusal
            // arrived. A check that cannot tell two answers apart is not
            // checking the difference between them.
            foreach (var mine in new[]
            {
                "https://github.com/owner-a/repo-a",
                "https://github.com/owner-a/repo-a/tree/main",
                "https://github.com/owner-a/repo-a/archive/refs/heads/main.zip",
            })
            {
                var verdict = InstallSource.Judge(mine, mineManifest);
                Check(!verdict.Accepted, "still refused: " + mine);
                Check(Names(verdict.Why, "repository rather than"),
                      "  and it says the REPOSITORY rather than a release: " + verdict.Why);
                Check(!Names(verdict.Why, "somebody else"),
                      "  and never calls the owner's own repository somebody else's");
            }

            // AND THE OTHER SENTENCE MUST STILL ARRIVE for a repository that
            // really is somebody else's - otherwise the fix above would have
            // been to delete a refusal rather than to correct one.
            var theirs = InstallSource.Judge("https://github.com/someone-else/repo-a/releases", mineManifest);
            Check(!theirs.Accepted && Names(theirs.Why, "somebody else"),
                  "somebody else's repository is still refused as somebody else's: " + theirs.Why);
            var renamed = InstallSource.Judge("https://github.com/owner-a/not-heron/releases", mineManifest);
            Check(!renamed.Accepted && Names(renamed.Why, "somebody else"),
                  "and so is the right owner with the wrong repository: " + renamed.Why);

            Console.WriteLine();
            Console.WriteLine("  a local folder is told apart from a hostile address");
            // Somebody who cloned the repository and pointed at it has done
            // something reasonable - that is route 2 - and a refusal that does
            // not say which door to use is a dead end.
            var local = InstallSource.Judge(@"D:\Heron-AI", mineManifest);
            Check(!local.Accepted, "a folder is not route 1");
            Check(Names(local.Why, "folder on this PC", "install"),
                  "and it points at the other door instead of just refusing: " + local.Why);
            var unc = InstallSource.Judge(@"\\server\share\Heron-AI", mineManifest);
            Check(!unc.Accepted && Names(unc.Why, "folder on this PC"),
                  "and a network share reads the same way: " + unc.Why);

            Console.WriteLine();
            Console.WriteLine("  a manifest that does not know its own source refuses EVERYTHING");
            // Inventing a repository name here would be the installer deciding
            // where to get software from, which is the one decision it must
            // never make.
            var anon = ProductManifest.Parse(@"{ ""products"": [] }");
            var guess = InstallSource.Judge("https://github.com/owner-a/repo-a/releases", anon);
            Check(!guess.Accepted, "with no source in the manifest, nothing is accepted");
            Check(Names(guess.Why, "does not know"),
                  "and it says so rather than guessing a repository: " + guess.Why);

            Console.WriteLine();
            Console.WriteLine("  it opens no socket and reads no repository - R-50");
            // Judge takes a string and the product list. It cannot fetch
            // anything, which is what makes "the manifest is read from the
            // release, after the source has been accepted" true by
            // construction rather than by discipline.
            Check(InstallSource.Judge("https://github.com/owner-a/repo-a/releases", mineManifest)
                      .Accepted,
                  "the same answer comes back with no network of any kind");

            // ================================================== STAGE 7
            Console.WriteLine();
            Console.WriteLine("WHAT IS INSTALLED STARTS TICKED - the safety property R-21 needs");
            // R-21 says unticking a product uninstalls it. Every product row
            // used to start EMPTY, so a user who opened this window and
            // pressed Install without touching anything would have unticked
            // everything they had - and the press meant to install would have
            // removed the lot.
            var here = InstallerScreen.Build(screenManifest, new[] { "2024" }, null,
                                             new FakeInstalled("piece-one@2024"), null);
            Check(RowFor(here, "piece-one").Chosen,
                  "a product that is on the machine starts ticked");
            Check(!RowFor(here, "piece-two").Chosen,
                  "and one that is not does not");
            Check(!RowFor(here, "tab-b").Chosen,
                  "a greyed row is never ticked - it cannot be installed, so it "
                  + "cannot be 'kept' either");

            Console.WriteLine();
            Console.WriteLine("UNTICKING SOMETHING INSTALLED IS AN UNINSTALL - R-21");
            var both = InstallerScreen.Build(screenManifest, new[] { "2024", "2025" }, null,
                                             new FakeInstalled("piece-one@2024", "piece-one@2025",
                                                               "piece-two@2024"), null);
            var dropped = both.ToRemove(new[] { "piece-two" });
            Check(dropped.Count == 2,
                  "the product left unticked comes off every release it is on");
            Check(dropped[0].Product.Id == "piece-one" && dropped[1].Product.Id == "piece-one",
                  "and it is the unticked one, not the ticked one");
            Check(both.ToRemove(new[] { "piece-one", "piece-two" }).Count == 0,
                  "nothing is removed while everything is still ticked");
            Check(both.ToRemove(new[] { "piece-one" }).Count == 1,
                  "and unticking the other takes only that one off");

            Console.WriteLine();
            Console.WriteLine("A PRODUCT THAT WAS NEVER THERE HAS NOTHING TO REMOVE");
            var never = InstallerScreen.Build(screenManifest, new[] { "2024" }, null, null, null);
            Check(never.ToRemove(new string[0]).Count == 0,
                  "an empty window removes nothing, whatever is unticked");

            Console.WriteLine();
            Console.WriteLine("A HEADING REMOVES NOTHING OF ITS OWN - D-93");
            // It installs nothing of its own either. Its pieces are rows in
            // the same list and are asked about on their own account, so
            // counting the heading too would delete each piece twice.
            var headingOff = both.ToRemove(new string[0]);
            foreach (var step in headingOff)
                Check(step.Product.Id != "tab-a",
                      "the tab itself is not in the removal list: " + step.Product.Id);
            Check(headingOff.Count == 3,
                  "three pairs come off - two for one piece, one for the other - "
                  + "and the heading adds none: " + headingOff.Count);

            Console.WriteLine();
            Console.WriteLine("THE CONFIRMATION NAMES EVERY PRODUCT AND EVERY RELEASE");
            // A safety gate, not an information message. "3 items will be
            // removed" is a number somebody clicks past.
            var ask = InstallerScreen.WhatWillBeRemoved(dropped);
            Check(Names(ask, "REMOVE", "Piece One", "2024", "2025"),
                  "it says what goes and from where: " + ask);
            Check(Names(ask, "not touched"),
                  "and that the user's own data survives - R-22, which is the "
                  + "thing somebody about to uninstall is actually worried about");
            Check(Names(ask, "Restart Revit"),
                  "and that Revit has to be restarted, because a loaded assembly "
                  + "goes on being loaded until it is");
            Check(!Names(ask, "error"), "and it never says 'error' - docs/14");
            Check(InstallerScreen.WhatWillBeRemoved(never.ToRemove(new string[0])) == null,
                  "AND THERE IS NO DIALOG WHEN NOTHING IS BEING REMOVED - a "
                  + "confirmation people meet every time is one they stop reading");
            Check(InstallerScreen.WhatWillBeRemoved(null) == null,
                  "nor when there is no list at all");

            Console.WriteLine();
            Console.WriteLine("REMOVALS HAPPEN BEFORE INSTALLS, and that order is a rule");
            // THE REMOVAL LISTS BELOW COME FROM A SCREEN, not from steps built
            // by hand. InstallStep's fields are internal on purpose - a step
            // is something the plan or the screen decided, never something a
            // caller fabricates - and asking the screen here means these
            // checks exercise the path the window actually takes.
            // A run that takes one product off and puts another on should
            // leave the machine with the second. Doing the delete last means
            // a failure part way through leaves BOTH.
            var order = new FakeDeployer((p, r) => DeployOutcome.Ok("installed"));
            var onMachine = InstallerScreen.Build(manifest, new[] { "2024" }, null,
                                                  new FakeInstalled("piece-two@2024"), null);
            var ordered = new InstallEngine(new FakeRevit(new[] { "2024" }), order)
                .Apply(manifest, new[] { "piece-one" }, new[] { "2024" },
                       onMachine.ToRemove(new[] { "piece-one" }));
            Check(order.Did.Count == 2, "both halves ran");
            Check(order.Did[0] == "remove piece-two@2024",
                  "the removal is first: " + order.Did[0]);
            Check(order.Did[1] == "install piece-one@2024",
                  "and the install second: " + order.Did[1]);
            Check(ordered.Removed == 1 && ordered.Installed == 1,
                  "and the report counts them apart - one removed, one installed");

            Console.WriteLine();
            Console.WriteLine("AN UNINSTALL WAITS FOR REVIT TOO - R-38a");
            // Revit holds a loaded assembly whether it is about to be
            // replaced or deleted, so a release being uninstalled is a
            // release this run touches.
            var closing7 = new FakeRevit(new[] { "2024" }, Open("2024"), Open("2024"), Open());
            var waited7 = new List<string>();
            var removeOnly = new FakeDeployer((p, r) => DeployOutcome.Ok("x"));
            var engine7 = new InstallEngine(closing7, removeOnly)
            { OnWaiting = m => waited7.Add(m), Pause = () => { } };
            var oneOn = InstallerScreen.Build(manifest, new[] { "2024" }, null,
                                              new FakeInstalled("piece-one@2024"), null);
            var report7 = engine7.Apply(manifest, new string[0], new string[0],
                                        oneOn.ToRemove(new string[0]));
            Check(waited7.Count == 2,
                  "it waited while Revit 2024 was open, with nothing to install at all");
            Check(removeOnly.Did.Count == 1 && removeOnly.Did[0] == "remove piece-one@2024",
                  "and removed only once Revit had closed");
            Check(report7.Removed == 1, "and says so");

            Console.WriteLine();
            Console.WriteLine("ONE REMOVAL FAILING DOES NOT STOP THE REST - R-19");
            var stubborn = new FakeDeployer(
                (p, r) => DeployOutcome.Ok("installed"),
                (p, r) => p.Id == "piece-one"
                    ? DeployOutcome.Failed("'" + p.Name + "' could not be removed - a file is in use.")
                    : DeployOutcome.Ok("'" + p.Name + "' removed from Revit " + r + "."));
            var twoOn = InstallerScreen.Build(manifest, new[] { "2024" }, null,
                                              new FakeInstalled("piece-one@2024", "piece-two@2024"),
                                              null);
            var partly7 = new InstallEngine(new FakeRevit(new[] { "2024" }), stubborn)
                .Apply(manifest, new string[0], new string[0], twoOn.ToRemove(new string[0]));
            Check(partly7.Results.Count == 2, "both were attempted");
            Check(partly7.Removed == 1 && partly7.Failed == 1, "one came off and one did not");
            Check(stubborn.Did.Count == 2,
                  "the second was tried even though the first failed");

            Console.WriteLine();
            Console.WriteLine("NOTHING TICKED AND NOTHING INSTALLED IS NOT A FAILURE");
            var idle7 = new InstallEngine(new FakeRevit(new[] { "2024" }),
                                          new FakeDeployer((p, r) => DeployOutcome.Ok("x")))
                .Apply(manifest, new string[0], new[] { "2024" }, null);
            Check(idle7.Results.Count == 0 && !idle7.Abandoned,
                  "an empty apply does nothing and reports no failure");

            // ================================================== STAGE 5
            // DRIVEN AGAINST A REAL SERVER ON A REAL PORT. See
            // ReleaseDownloadChecks - it is the one adapter reaching outside
            // this assembly that does not need Windows, so it is exercised
            // rather than read.
            ReleaseDownloadChecks.Run(Check, Names);

            Console.WriteLine();
            Console.WriteLine("The install location is per user, and says why that matters");
            Check(Names(InstallerScreen.InstallLocation, "APPDATA"),
                  "it is under %APPDATA%");
            Check(!Names(InstallerScreen.InstallLocation, "ProgramData")
                  && !Names(InstallerScreen.InstallLocation, "Program Files"),
                  "and nowhere that needs an administrator");
            Check(Names(InstallerScreen.InstallLocationNote, "no administrator"),
                  "and the window says so, which is the promise being kept");

            Console.WriteLine();
            Console.WriteLine("ROUTE 2 - A FOLDER ON THIS PC, CHECKED EXACTLY LIKE A DOWNLOAD");
            // R-15 and R-53, answered 2026-09-22: after one download nothing
            // needs the internet. What a person is handed is the folder the
            // release builder makes, on a stick or a share.
            //
            // A FOLDER IS NOT TRUSTED FOR BEING LOCAL. It got here because
            // somebody handed it over, which is exactly how a download gets
            // here. Being local removes the network, not the question of
            // whether the bytes are what the publisher published.
            var hand = Path.Combine(Path.GetTempPath(), "heron-hand-" + Guid.NewGuid().ToString("N"));
            var work = Path.Combine(Path.GetTempPath(), "heron-work-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(hand);
                Directory.CreateDirectory(work);

                Console.WriteLine();
                Console.WriteLine("  a folder that is not a handover is refused, and the sentence says which");
                Check(Names(ProductFolder.WhyNotAHeronFolder(Path.Combine(hand, "nope")), "no folder at"),
                      "a path that is not there");
                Check(Names(ProductFolder.WhyNotAHeronFolder(hand), "heron-products.json", "point at that one"),
                      "a folder with no product list - and it names the commonest mistake, "
                      + "pointing at the parent");

                File.WriteAllText(Path.Combine(hand, "heron-products.json"), "{\"products\":[]}");
                Check(Names(ProductFolder.WhyNotAHeronFolder(hand), "checksums.txt", "did not finish"),
                      "a folder with a product list but no checksums - written last, so its "
                      + "absence means the copy stopped half way");

                // A REAL ASSET, MADE HERE. Nothing below reads a fixture: the
                // zip is built, hashed, and handed to the same
                // ReleaseAssets.WhyNotTrusted a download goes through.
                var product = new HeronProduct { Id = "piece-one", Name = "One", Assembly = "One.dll" };
                var assetName = ReleaseAssets.NameFor(product, "2024");
                var zipPath = Path.Combine(hand, assetName);
                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    var entry = zip.CreateEntry("One.dll");
                    using (var writer = new StreamWriter(entry.Open()))
                        writer.Write("not a real assembly, and it does not need to be");
                }
                var good = ReleaseAssets.DigestOf(File.ReadAllBytes(zipPath));
                File.WriteAllText(Path.Combine(hand, "checksums.txt"), good + "  " + assetName + "\n");

                Check(ProductFolder.WhyNotAHeronFolder(hand) == null,
                      "and a folder with both is accepted as a handover");

                Console.WriteLine();
                Console.WriteLine("  a good folder unpacks, and NOTHING was downloaded");
                string why;
                var unpacked = new ProductFolder(hand, work).Folder(product, "2024", out why);
                Check(unpacked != null && why == null, "the asset came back: " + (why ?? "ok"));
                Check(unpacked != null && File.Exists(Path.Combine(unpacked, "One.dll")),
                      "and One.dll is really on the disk where it said");

                Console.WriteLine();
                Console.WriteLine("  A TAMPERED ZIP IS REFUSED, and this is the whole safety property");
                // Somebody swapped the file on the stick. The checksum beside
                // it still says what the publisher published, so it no longer
                // matches - and nothing is written.
                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Update))
                {
                    var entry = zip.CreateEntry("evil.dll");
                    using (var writer = new StreamWriter(entry.Open()))
                        writer.Write("this was not in the release");
                }
                var after = new ProductFolder(hand, work).Folder(product, "2024", out why);
                Check(after == null, "a zip that changed under the checksum installs nothing");
                Check(Names(why, "did not arrive whole"),
                      "and it says the file does not match what was published: " + why);

                Console.WriteLine();
                Console.WriteLine("  an asset the checksums do not list is refused too");
                var stranger = new HeronProduct { Id = "piece-two", Name = "Two", Assembly = "Two.dll" };
                var strangerName = ReleaseAssets.NameFor(stranger, "2024");
                using (var zip = ZipFile.Open(Path.Combine(hand, strangerName), ZipArchiveMode.Create))
                {
                    var entry = zip.CreateEntry("Two.dll");
                    using (var writer = new StreamWriter(entry.Open())) writer.Write("dropped in");
                }
                var unlisted = new ProductFolder(hand, work).Folder(stranger, "2024", out why);
                Check(unlisted == null && Names(why, "not listed"),
                      "a file nobody published is not installed because it is sitting there: " + why);

                Console.WriteLine();
                Console.WriteLine("  a release this folder does not carry says so, and names the way out");
                var missing = new ProductFolder(hand, work).Folder(product, "2027", out why);
                Check(missing == null, "a release the folder has no asset for installs nothing");
                Check(Names(why, "not in", "whole folder"),
                      "and it says the folder is partial rather than blaming the product: " + why);

                Console.WriteLine();
                Console.WriteLine("  checksums.txt unreadable FAILS CLOSED, never open");
                // An empty checksum list must refuse everything. Reading it as
                // "nothing to check, carry on" is how a damaged handover
                // installs whatever it likes.
                var noSums = Path.Combine(Path.GetTempPath(), "heron-nosum-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(noSums);
                File.Copy(Path.Combine(hand, "heron-products.json"), Path.Combine(noSums, "heron-products.json"));
                File.Copy(zipPath, Path.Combine(noSums, assetName));
                Directory.CreateDirectory(Path.Combine(noSums, "checksums.txt"));   // a folder, not a file
                var closed = new ProductFolder(noSums, work).Folder(product, "2024", out why);
                Check(closed == null, "an unreadable checksums.txt installs nothing at all");
                Check(Names(why, "no way to tell"),
                      "and it says it cannot tell rather than assuming: " + why);
                Directory.Delete(noSums, true);
            }
            finally
            {
                try { Directory.Delete(hand, true); } catch (IOException) { }
                try { Directory.Delete(work, true); } catch (IOException) { }
            }

            Console.WriteLine();
            Console.WriteLine("STAGE 6 - THE COMMAND LINE DOOR READS WHAT IT WAS GIVEN (Q-PE-16)");
            Console.WriteLine();
            Console.WriteLine("  an empty line is the plain case, not a refusal");
            // `heron-install` on its own means everything, for every Revit
            // found. It is the commonest thing anybody will type.
            var cliPlain = Heron.Installer.Cli.Arguments.Read(new string[0]);
            Check(cliPlain.Problem == null, "no arguments is not a problem");
            Check(cliPlain.Source == null && cliPlain.FromFolder == null,
                  "and it names no source, so the door decides by what is on the disk");
            Check(cliPlain.Products.Count == 0 && cliPlain.Releases.Count == 0,
                  "and filters nothing - empty means everything offered");
            Check(!cliPlain.ListOnly && !cliPlain.WantsHelp, "and asks to actually install");

            Console.WriteLine();
            Console.WriteLine("  an unknown flag is refused BY NAME, never ignored");
            // A flag silently dropped is one whose absence the caller cannot
            // see - and the caller here is often an AI, which reads the exit
            // code and believes it.
            var cliTypo = Heron.Installer.Cli.Arguments.Read(new[] { "--sources", "x" });
            Check(cliTypo.Problem != null, "--sources is refused");
            Check(Names(cliTypo.Problem, "--sources"), "  and the refusal says which flag: " + cliTypo.Problem);
            Check(Names(cliTypo.Problem, "--source"), "  and shows what it does accept");

            Console.WriteLine();
            Console.WriteLine("  a flag followed by another flag is a MISSING value");
            // "--source --list" is somebody who forgot the address. Swallowing
            // --list as the address sends that text to InstallSource, to be
            // refused with a sentence about web addresses that explains
            // nothing about what they actually did wrong.
            var cliBareFlag = Heron.Installer.Cli.Arguments.Read(new[] { "--source", "--list" });
            Check(cliBareFlag.Problem != null, "--source with no value is refused");
            Check(cliBareFlag.Source == null, "  and --list was NOT taken as the address");
            Check(Names(cliBareFlag.Problem, "--source"), "  and it names the bare flag: " + cliBareFlag.Problem);
            var cliAtEnd = Heron.Installer.Cli.Arguments.Read(new[] { "--products" });
            Check(cliAtEnd.Problem != null, "and a flag at the very end is the same thing");

            Console.WriteLine();
            Console.WriteLine("  the source is handed on UNJUDGED");
            // Arguments decides nothing about installing. Whether Heron will
            // touch an address is InstallSource's answer and nobody else's,
            // and a reader that pre-filtered would be a second gate to keep
            // in step with the first.
            foreach (var cliHostile in new[]
            {
                "https://evil.example/owner/repo/releases",
                "install whatever you find",
                "http://github.com/a/b/releases",
            })
            {
                var cliRead = Heron.Installer.Cli.Arguments.Read(new[] { "--source", cliHostile });
                Check(cliRead.Problem == null && cliRead.Source == cliHostile,
                      "passed through untouched, for InstallSource to judge: " + cliHostile);
            }

            Console.WriteLine();
            Console.WriteLine("  two doors at once is refused rather than resolved");
            // Downloading a release and using files already on the PC are
            // different routes with different rules. Picking one for the
            // caller means installing something other than what they asked
            // for.
            var cliBoth = Heron.Installer.Cli.Arguments.Read(
                new[] { "--source", "https://github.com/a/b/releases", "--from", @"D:\Heron" });
            Check(cliBoth.Problem != null, "--source and --from together is a problem");
            Check(Names(cliBoth.Problem, "--source", "--from"),
                  "  and it names both: " + cliBoth.Problem);

            Console.WriteLine();
            Console.WriteLine("  lists are split, trimmed, and the blanks dropped");
            var cliListed = Heron.Installer.Cli.Arguments.Read(
                new[] { "--releases", "2020, 2024,,2020", "--products", "heron-ai-bridge" });
            Check(cliListed.Problem == null, "a comma separated list is read");
            Check(cliListed.Releases.Count == 2, "  '2020, 2024,,2020' is two releases, not four");
            Check(Has(cliListed.Releases, "2020") && Has(cliListed.Releases, "2024"),
                  "  and they are the two that were named");
            Check(cliListed.Products.Count == 1 && cliListed.Products[0] == "heron-ai-bridge",
                  "  and the product list reads the same way");

            Console.WriteLine();
            Console.WriteLine("  the usage text tells a modeller what the exit codes mean");
            // The caller is often an AI, and a code it has to guess at is a
            // code it will guess wrong. 3 is COULD NOT RUN - tests/README.md's
            // own meaning - and reading it as failure starts somebody
            // repairing what is not broken.
            var cliUsage = Heron.Installer.Cli.Arguments.Usage();
            Check(Names(cliUsage, "0", "1", "2", "3"), "all four codes are listed");
            Check(Names(cliUsage, "Revit stayed open"), "  and 3 says Revit stayed open");
            Check(Names(cliUsage, "Nothing was changed"), "  and that nothing was changed");
            Check(Names(cliUsage, "never removes"),
                  "and it says plainly that this door never removes anything");
            Check(!Names(cliUsage, "error"), "and it never says 'error' - docs/14");

            Console.WriteLine();
            Console.WriteLine("IS THERE A NEWER VERSION? - R-54, R-55, R-56");
            Console.WriteLine();
            Console.WriteLine("  the ordinary answers");
            // The owner's reason, 2026-09-22: somebody who downloaded days ago
            // would otherwise never hear that a newer release exists.
            var newer = UpdateCheck.Compare("0.1.0", "0.2.0");
            Check(newer.State == UpdateState.NewerAvailable, "0.1.0 -> 0.2.0 is an update");
            Check(Names(newer.Say, "0.1.0", "0.2.0"),
                  "  and it says both versions, not just that there is one: " + newer.Say);
            Check(Names(newer.Say, "Nothing has been updated"),
                  "  and it says plainly that nothing happened - R-55, offered not applied");

            var same = UpdateCheck.Compare("0.2.0", "0.2.0");
            Check(same.State == UpdateState.UpToDate, "the same version is up to date");

            Check(UpdateCheck.Compare("1.2", "1.2.0").State == UpdateState.UpToDate,
                  "1.2 and 1.2.0 are the same thing - a short version is padded");
            Check(UpdateCheck.Compare("1.2", "1.2.1").State == UpdateState.NewerAvailable,
                  "and 1.2 is older than 1.2.1");
            Check(UpdateCheck.Compare("v0.1.0", "0.2.0").State == UpdateState.NewerAvailable,
                  "a leading v is a tag's habit, not a different version");
            Check(UpdateCheck.Compare("0.9.0", "0.10.0").State == UpdateState.NewerAvailable,
                  "0.10.0 is NEWER than 0.9.0 - compared as numbers, never as text");

            Console.WriteLine();
            Console.WriteLine("  AHEAD is not up to date, and saying so would be the comfortable lie");
            // A developer with a build newer than anything published. Telling
            // them they are current hides that they are about to install over
            // their own work.
            var ahead = UpdateCheck.Compare("0.3.0", "0.2.0");
            Check(ahead.State == UpdateState.Ahead, "0.3.0 against a published 0.2.0 is ahead");
            Check(!Names(ahead.Say, "newest published version"),
                  "and it is NOT reported as up to date");
            Check(Names(ahead.Say, "built rather than downloaded"),
                  "and it says what that usually means: " + ahead.Say);

            Console.WriteLine();
            Console.WriteLine("  CANNOT TELL is an answer, and it never reads as up to date");
            // The direction to be wrong in is the one that does not hide a
            // newer release from somebody.
            foreach (var pair in new[]
            {
                new[] { "0.1.0-rc1", "0.2.0" },   // a release candidate is not 0.1.0
                new[] { "banana", "0.2.0" },
                new[] { "0.1.0", "" },
                new[] { null, "0.2.0" },
            })
            {
                var cannot = UpdateCheck.Compare(pair[0], pair[1]);
                Check(cannot.State == UpdateState.CannotTell,
                      "'" + (pair[0] ?? "(null)") + "' against '" + pair[1] + "' cannot be compared");
                Check(cannot.State == UpdateState.CannotTell && !Names(cannot.Say, "up to date"),
                      "  and it does not claim to be up to date");
                Check(cannot.State == UpdateState.CannotTell && Names(cannot.Say, "installing works either way"),
                      "  and it says the install is unaffected");
            }

            Console.WriteLine();
            Console.WriteLine("  the version comes from the manifest, and it is the HIGHEST one");
            // Q-PE-8 - whether products share a version or carry their own - is
            // still open, so this has to be right either way.
            var versions = ProductManifest.Parse(@"{""products"":[
                {""id"":""a"",""name"":""A"",""addin"":""A.addin"",""assembly"":""A.dll"",
                 ""addInId"":""11111111-1111-1111-1111-111111111111"",""folder"":""A"",""version"":""0.1.0""},
                {""id"":""b"",""name"":""B"",""addin"":""B.addin"",""assembly"":""B.dll"",
                 ""addInId"":""22222222-2222-2222-2222-222222222222"",""folder"":""B"",""version"":""0.4.0""},
                {""id"":""c"",""name"":""C"",""addin"":""C.addin"",""assembly"":""C.dll"",
                 ""addInId"":""33333333-3333-3333-3333-333333333333"",""folder"":""C"",""version"":""0.2.0""}]}");
            Check(UpdateCheck.HighestVersion(versions) == "0.4.0",
                  "the highest, not the first: 0.4.0 out of 0.1.0, 0.4.0, 0.2.0");

            var noVersions = ProductManifest.Parse(@"{""products"":[
                {""id"":""a"",""name"":""A"",""addin"":""A.addin"",""assembly"":""A.dll"",
                 ""addInId"":""11111111-1111-1111-1111-111111111111"",""folder"":""A""}]}");
            Check(UpdateCheck.HighestVersion(noVersions) == null,
                  "and a manifest carrying no version at all answers null rather than guessing");
            Check(UpdateCheck.Compare(UpdateCheck.HighestVersion(noVersions), "0.2.0").State
                      == UpdateState.CannotTell,
                  "which Compare then reports as cannot tell, not as an update");

            Console.WriteLine();
            if (Failures.Count > 0)
            {
                Console.WriteLine("FAILED (" + Failures.Count + ")");
                foreach (var f in Failures) Console.WriteLine("  - " + f);
                return 1;
            }

            Console.WriteLine("The install engine decides correctly. It skips what it must and");
            Console.WriteLine("says why every time, it waits for Revit rather than working around");
            Console.WriteLine("it, and one product failing never takes the others with it.");
            Console.WriteLine();
            Console.WriteLine("The window offers what the FILE says and nothing it was built");
            Console.WriteLine("with. A tab opens into its pieces, either one on its own is a");
            Console.WriteLine("supported install, and a product that cannot be installed here is");
            Console.WriteLine("greyed with the reason on the row rather than quietly dropped.");
            Console.WriteLine();
            Console.WriteLine("The command line door reads what it was given and judges none");
            Console.WriteLine("of it: an unknown flag is refused by name, a bare flag is a");
            Console.WriteLine("missing value rather than a value, and the address goes to");
            Console.WriteLine("InstallSource exactly as it was typed.");
            Console.WriteLine();
            Console.WriteLine("IT HAS INSTALLED NOTHING. Nothing was written outside one");
            Console.WriteLine("temporary folder of its own, no Revit was looked for, no");
            Console.WriteLine("PowerShell ran, AND NO WINDOW WAS DRAWN. That needs Windows and");
            Console.WriteLine("is not proved here.");
            return 0;
        }
    }
}
