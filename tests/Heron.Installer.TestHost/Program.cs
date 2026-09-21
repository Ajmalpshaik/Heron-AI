// Heron-Agent:  none
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.IO;
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
    /// WHAT IT CANNOT PROVE: that anything installs. No file is written, no
    /// Revit is looked for, no PowerShell runs. Those need Windows, and a
    /// green run here is not an install.
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
            public readonly List<string> Deployed = new List<string>();

            public FakeDeployer(Func<HeronProduct, string, DeployOutcome> behaviour)
            {
                _behaviour = behaviour;
            }

            public DeployOutcome Deploy(HeronProduct product, string release)
            {
                Deployed.Add(product.Id + "@" + release);
                return _behaviour(product, release);
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
            Console.WriteLine("A release that is ticked but not on the PC, and one that is not ticked");
            var mixed = InstallPlan.Build(manifest, new[] { "piece-one" },
                                          new[] { "2024", "2026" },
                                          new[] { "2024", "2025" });
            var absent = SkipFor(mixed, "piece-one", "2026");
            Check(absent != null && absent.Reason == SkipReason.ReleaseNotInstalled,
                  "ticking a Revit that is not installed is skipped, not attempted");
            var untouched = SkipFor(mixed, null, "2025");
            Check(untouched != null && untouched.Reason == SkipReason.ReleaseNotChosen,
                  "a Revit on the PC that was NOT ticked is reported too");
            Check(Names(untouched.Explanation, "not ticked"),
                  "so nobody opens it next week wondering where Heron went");

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
                                               null, null);
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
            var only2025 = InstallerScreen.Build(screenManifest, new[] { "2025" }, null, null);
            var narrow = RowFor(only2025, "piece-two");
            Check(!narrow.CanBeTicked, "the piece that stops at 2024 is greyed on a 2025-only PC");
            Check(Names(narrow.WhyNot, "2025", "2024"),
                  "and the reason names both what is here and what it supports: " + narrow.WhyNot);
            Check(RowFor(only2025, "tab-a").Installs.Count == 1,
                  "so the tab's tick carries only the piece that can be installed");

            Console.WriteLine();
            Console.WriteLine("Install replaces, and the window says so BEFORE it is pressed - R-23a");
            var some = InstallerScreen.Build(screenManifest, new[] { "2024", "2025" }, null,
                                             new FakeInstalled("piece-one@2024", "piece-one@2025"));
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
                                               new FakeInstalled("piece-one@2024"));
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
            Console.WriteLine("No Revit at all is a sentence, not an empty window");
            var bare = InstallerScreen.Build(screenManifest, new string[0], null, null);
            Check(bare.Releases.Count == 0 && !bare.AnythingToOffer, "nothing can be offered");
            Check(Names(bare.NothingFound, "No Revit was found", "Install Revit first"),
                  "and it says so, and what to do: " + bare.NothingFound);

            Console.WriteLine();
            Console.WriteLine("Close Revit first is said BEFORE Install, not after it fails");
            Check(Names(screen.CloseRevitFirst, "Close Revit before installing"),
                  "the line is there with no Revit open at all");
            var busy = InstallerScreen.Build(screenManifest, new[] { "2024" },
                                             Open("2024"), null);
            Check(Names(busy.CloseRevitFirst, "2024", "open right now"),
                  "and it names the release when one is open: " + busy.CloseRevitFirst);
            var murkyScreen = InstallerScreen.Build(screenManifest, new[] { "2024" },
                                                    Unknown(), null);
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
            Console.WriteLine("The install location is per user, and says why that matters");
            Check(Names(InstallerScreen.InstallLocation, "APPDATA"),
                  "it is under %APPDATA%");
            Check(!Names(InstallerScreen.InstallLocation, "ProgramData")
                  && !Names(InstallerScreen.InstallLocation, "Program Files"),
                  "and nowhere that needs an administrator");
            Check(Names(InstallerScreen.InstallLocationNote, "no administrator"),
                  "and the window says so, which is the promise being kept");

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
            Console.WriteLine("IT HAS INSTALLED NOTHING. No file was written, no Revit was looked");
            Console.WriteLine("for, no PowerShell ran, AND NO WINDOW WAS DRAWN. That needs Windows");
            Console.WriteLine("and is not proved here.");
            return 0;
        }
    }
}
