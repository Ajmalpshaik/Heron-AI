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
            Console.WriteLine("IT HAS INSTALLED NOTHING. No file was written, no Revit was looked");
            Console.WriteLine("for, no PowerShell ran. That needs Windows and is not proved here.");
            return 0;
        }
    }
}
