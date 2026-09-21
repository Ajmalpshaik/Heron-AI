// Heron-Agent:  HERON-INS-ORC-001, HERON-INS-RVT-003
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;

namespace Heron.Installer
{
    /// <summary>What happened to one product, for one release.</summary>
    public sealed class InstallResult
    {
        public string ProductId { get; internal set; }
        public string ProductName { get; internal set; }
        public string Release { get; internal set; }
        public bool Succeeded { get; internal set; }
        public string Message { get; internal set; }
    }

    /// <summary>
    /// What the install did, per product, per release - R-19 and R-32.
    ///
    /// An install that says nothing cannot be checked by the person it
    /// happened to, and route 1 installs without them watching at all. So
    /// the report is not optional and not deferred.
    /// </summary>
    public sealed class InstallReport
    {
        internal readonly List<InstallResult> _results = new List<InstallResult>();
        internal readonly List<SkippedStep> _skipped = new List<SkippedStep>();

        public IReadOnlyList<InstallResult> Results { get { return _results; } }
        public IReadOnlyList<SkippedStep> Skipped { get { return _skipped; } }

        /// <summary>True when the engine stopped before doing all of its work.</summary>
        public bool Abandoned { get; internal set; }

        /// <summary>Why it stopped, or null. Set only when Abandoned.</summary>
        public string AbandonedBecause { get; internal set; }

        public int Installed
        {
            get
            {
                var n = 0;
                foreach (var r in _results) if (r.Succeeded) n++;
                return n;
            }
        }

        public int Failed
        {
            get
            {
                var n = 0;
                foreach (var r in _results) if (!r.Succeeded) n++;
                return n;
            }
        }
    }

    /// <summary>How long to wait for Revit, and how often to look again.</summary>
    public sealed class WaitPolicy
    {
        /// <summary>
        /// How many times to look before giving up.
        ///
        /// R-38a says WAIT rather than refuse once: the user closes Revit and
        /// the install carries on without being started again. But a wait
        /// with no end is a window that never comes back, so there is a
        /// ceiling - reached, it stops and says so, having changed nothing.
        /// </summary>
        public int MaxChecks { get; set; }

        public WaitPolicy() { MaxChecks = 600; }
    }

    /// <summary>
    /// The install engine. No window, no screen - Stage 3.
    ///
    /// WHAT IT DOES, IN ORDER, and every step is a requirement rather than a
    /// preference:
    ///
    ///   1  work out the plan               what, where, and what is skipped
    ///   2  WAIT until Revit is closed      R-38a - wait, never work around
    ///   3  deploy each product             one at a time, through the script
    ///   4  report, per product, per release  R-19, R-32
    ///
    /// WHY IT WAITS RATHER THAN WORKING AROUND. Revit holds every assembly it
    /// has loaded, so a delete fails while it is open. AJ Tools renames the
    /// folder aside and sweeps later; the owner refused that on 2026-09-21,
    /// because a best-effort sweep leaves .old folders piling up until nobody
    /// can tell which copy Revit is loading. Waiting removes the problem
    /// instead of routing around it.
    ///
    /// IT NEVER CLOSES REVIT. An open Revit has a model in it and that model
    /// very likely has unsaved work. There is no -Force and no "it looked
    /// idle" - see tools\HeronRevit.ps1, which takes the same position for
    /// the same reason.
    /// </summary>
    public sealed class InstallEngine
    {
        private readonly IRevitEnvironment _revit;
        private readonly IProductDeployer _deployer;
        private readonly WaitPolicy _wait;

        public InstallEngine(IRevitEnvironment revit, IProductDeployer deployer)
            : this(revit, deployer, new WaitPolicy())
        {
        }

        public InstallEngine(IRevitEnvironment revit, IProductDeployer deployer, WaitPolicy wait)
        {
            if (revit == null) throw new ArgumentNullException("revit");
            if (deployer == null) throw new ArgumentNullException("deployer");
            _revit = revit;
            _deployer = deployer;
            _wait = wait ?? new WaitPolicy();
        }

        /// <summary>
        /// Told while the engine waits, so a caller can say so on screen.
        ///
        /// A silent wait is indistinguishable from a hang, and the one thing
        /// the user has to do - close Revit - is the one thing they will not
        /// think of unless told.
        /// </summary>
        public Action<string> OnWaiting { get; set; }

        /// <summary>Called between checks. The real caller sleeps; a test does not.</summary>
        public Action Pause { get; set; }

        public InstallReport Install(ProductManifest manifest,
                                     IEnumerable<string> productIds,
                                     IEnumerable<string> releases)
        {
            var installed = _revit.InstalledReleases();
            var plan = InstallPlan.Build(manifest, productIds, releases, installed);

            var report = new InstallReport();
            report._skipped.AddRange(plan.Skipped);

            if (!plan.HasWork) return report;

            // EVERY RELEASE THIS RUN TOUCHES, and only those. Revit 2024
            // being open says nothing about whether it is safe to install for
            // 2020, and refusing everything because one Revit is open is a
            // needless obstacle on a machine with three installed.
            var touched = new List<string>();
            foreach (var step in plan.Steps)
                if (!touched.Contains(step.Release)) touched.Add(step.Release);

            string blocked;
            if (!WaitForRevitToClose(touched, out blocked))
            {
                report.Abandoned = true;
                report.AbandonedBecause = blocked;
                return report;
            }

            foreach (var step in plan.Steps)
            {
                DeployOutcome outcome;
                try
                {
                    outcome = _deployer.Deploy(step.Product, step.Release);
                }
                catch (Exception e)
                {
                    // ONE PRODUCT FAILING DOES NOT STOP THE REST - R-19. A
                    // modeller installing three products and hitting one bad
                    // download should get the other two, and a line saying
                    // which one did not arrive.
                    outcome = DeployOutcome.Failed(
                        "'" + step.Product.Name + "' could not be installed for Revit " +
                        step.Release + ". " + e.Message);
                }

                report._results.Add(new InstallResult
                {
                    ProductId = step.Product.Id,
                    ProductName = step.Product.Name,
                    Release = step.Release,
                    Succeeded = outcome != null && outcome.Succeeded,
                    Message = outcome == null ? "Nothing was reported." : outcome.Message,
                });
            }

            return report;
        }

        /// <summary>
        /// Keep looking until no Revit this run would touch is open.
        ///
        /// Returns false when the ceiling is reached, with `because` set to a
        /// sentence naming what is still open. Nothing has been changed at
        /// that point, which is the whole value of waiting BEFORE deploying
        /// rather than discovering it halfway through.
        /// </summary>
        private bool WaitForRevitToClose(IList<string> releases, out string because)
        {
            because = null;

            for (var check = 0; check < Math.Max(1, _wait.MaxChecks); check++)
            {
                var blocking = Blocking(releases);
                if (blocking == null) return true;

                because = blocking;
                if (OnWaiting != null) OnWaiting(blocking);
                if (Pause != null) Pause();
            }

            return false;
        }

        /// <summary>
        /// What is open that would stop this run, or null when nothing is.
        /// </summary>
        private string Blocking(IList<string> releases)
        {
            var running = _revit.RunningRevits();
            if (running == null) return null;

            foreach (var r in running)
            {
                // AN UNIDENTIFIABLE REVIT BLOCKS EVERYTHING. It might be one
                // of the releases about to be replaced, and a guess that
                // lands wrong half-updates an install silently.
                if (r == null) continue;
                if (r.Release == null)
                {
                    return "A Revit is open (process " + r.ProcessId + ") and which " +
                           "release it is could not be read, so none can be replaced " +
                           "safely. Close it and this will carry on by itself.";
                }
            }

            foreach (var r in running)
            {
                if (r == null || r.Release == null) continue;
                foreach (var release in releases)
                {
                    if (!string.Equals(r.Release, release, StringComparison.Ordinal)) continue;
                    return "Revit " + release + " is open (process " + r.ProcessId +
                           "). Close it and this will carry on by itself - nothing " +
                           "has been changed yet.";
                }
            }

            return null;
        }
    }
}
