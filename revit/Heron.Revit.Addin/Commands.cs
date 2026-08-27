// Heron-Agent:  HERON-REVIT-CON-001, HERON-REVIT-HLT-025
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Starts the bridge for this Revit session.
    ///
    /// Connecting is deliberately explicit. A Revit that was never connected
    /// is invisible to every chat - which is the behaviour proven in the
    /// field (docs/00e) and worth keeping: nothing reaches a model the user
    /// did not offer up.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class ConnectCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var bridge = HeronApplication.Bridge;
            if (bridge == null)
            {
                // Asked for, never written out. A literal path here is exactly how the
                // %APPDATA% / %LOCALAPPDATA% drift got in the first time.
                message = "Heron did not start. The log that says why is in "
                        + HeronPaths.Logs + ".";
                return Result.Failed;
            }

            try
            {
                if (bridge.IsRunning)
                {
                    TaskDialog.Show("Heron AI",
                        "Already connected.\n\n" +
                        "Pipe: " + bridge.Identity.PipeName + "\n" +
                        "Process: " + bridge.Identity.ProcessId);
                    return Result.Succeeded;
                }

                bridge.Start();

                TaskDialog.Show("Heron AI",
                    "Connected.\n\n" +
                    "Revit " + bridge.Identity.RevitVersion +
                    ", process " + bridge.Identity.ProcessId + ".\n\n" +
                    "This session is now visible to Heron. Open another Revit and " +
                    "connect it too - each one gets its own bridge.");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                HeronApplication.Log("Connect failed: " + ex);
                message = "Could not start the Heron bridge: " + ex.Message;
                return Result.Failed;
            }
        }
    }

    /// <summary>
    /// Reports bridge state. Reads nothing from the model.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class StatusCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var bridge = HeronApplication.Bridge;

            if (bridge == null)
            {
                TaskDialog.Show("Heron AI",
                    "Heron did not start.\n\nThe log that says why is in\n"
                    + HeronPaths.Logs + ".");
                return Result.Succeeded;
            }

            if (!bridge.IsRunning)
            {
                TaskDialog.Show("Heron AI",
                    "Not connected.\n\nPress Connect Heron to make this session reachable.");
                return Result.Succeeded;
            }

            var id = bridge.Identity;
            TaskDialog.Show("Heron AI",
                "Connected.\n\n" +
                "Pipe:      " + id.PipeName + "\n" +
                "Process:   " + id.ProcessId + "\n" +
                "Revit:     " + id.RevitVersion + "\n" +
                "Add-in:    " + id.AddinVersion + "\n" +
                "Protocol:  " + Heron.Bridge.BridgeIdentity.ProtocolVersion + "\n\n" +
                "Announced in:\n" + id.DiscoveryFilePath);

            return Result.Succeeded;
        }
    }
}
