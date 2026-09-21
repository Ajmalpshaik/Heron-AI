// Heron-Agent:  HERON-INS-ENV-002
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System.Collections.Generic;

namespace Heron.Installer
{
    /// <summary>One Revit that is open right now.</summary>
    public sealed class RunningRevit
    {
        /// <summary>
        /// The release, or null when it could not be read.
        ///
        /// NULL IS NOT "SAFE". A Revit whose release cannot be determined
        /// might be the one about to be replaced, and a guess that lands
        /// wrong half-updates an install with no error to explain it. It
        /// blocks everything - tools\HeronRevit.ps1 already takes that
        /// position and this agrees with it rather than softening it.
        /// </summary>
        public string Release { get; set; }

        public int ProcessId { get; set; }

        public override string ToString()
        {
            return Release == null
                ? "Revit (release unknown), process " + ProcessId
                : "Revit " + Release + ", process " + ProcessId;
        }
    }

    /// <summary>
    /// What Revit is on this machine, and what is open.
    ///
    /// AN INTERFACE BECAUSE THE ANSWER LIVES IN POWERSHELL, not because a
    /// second implementation was wanted. tools\HeronRevit.ps1 already holds
    /// Get-InstalledRevit and Get-RunningRevit, and its own header says why
    /// two scripts must not ask the same question in two slightly different
    /// ways. Re-implementing either here would be exactly that.
    ///
    /// So the real one shells out, and a fake one lets every rule that
    /// depends on the answer be tested on a machine with no Revit at all.
    /// </summary>
    public interface IRevitEnvironment
    {
        /// <summary>
        /// Every Revit release installed, discovered from the machine.
        ///
        /// NEVER a fixed list of years. A hardcoded upper bound makes the
        /// next Revit release invisible to Heron's own installer, and D-05
        /// promises 2020 to latest, permanently. This is also AJ Tools'
        /// lesson L3, which cost it three releases installing nothing while
        /// its own documentation advertised them.
        /// </summary>
        IReadOnlyList<string> InstalledReleases();

        /// <summary>Every Revit open right now. Empty when none are.</summary>
        IReadOnlyList<RunningRevit> RunningRevits();
    }

    /// <summary>The outcome of deploying one product for one release.</summary>
    public sealed class DeployOutcome
    {
        public bool Succeeded { get; set; }

        /// <summary>
        /// What happened, then what to do next. Never a stack trace and never
        /// the word "error" on its own - docs/14. The reader is a modeller.
        /// </summary>
        public string Message { get; set; }

        public static DeployOutcome Ok(string message)
        {
            return new DeployOutcome { Succeeded = true, Message = message };
        }

        public static DeployOutcome Failed(string message)
        {
            return new DeployOutcome { Succeeded = false, Message = message };
        }
    }

    /// <summary>
    /// Puts one product's files where Revit reads them.
    ///
    /// AN INTERFACE FOR THE SAME REASON as IRevitEnvironment: the copy rule
    /// is tools\deploy-addin.ps1 and it is proven, rollback included. R-31
    /// refuses a second copy of it, so the real implementation drives that
    /// script and this engine never writes a file itself.
    /// </summary>
    public interface IProductDeployer
    {
        DeployOutcome Deploy(HeronProduct product, string release);
    }
}
