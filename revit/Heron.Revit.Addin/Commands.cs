// Heron-Agent:  HERON-REVIT-CON-001, HERON-REVIT-HLT-025, HERON-REVIT-UI-022
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
    /// Connects this Revit session, or disconnects it. One button, both ways.
    ///
    /// Connecting is deliberately explicit. A Revit that was never connected
    /// is invisible to every chat - the behaviour proven in the field
    /// (docs/00e) and worth keeping: nothing reaches a model the user did not
    /// offer up. Disconnecting is the same property in reverse, and it is the
    /// only way to take a session back without closing Revit.
    ///
    /// There is no dialog on success. The button's own picture changes - lit
    /// when connected, dark when not - so the state is visible from across the
    /// room and stays visible, which a dialog that has been dismissed does not.
    /// Only a real failure interrupts, through Revit's own message mechanism.
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
                    bridge.Stop();
                    HeronApplication.SetBridgeIcon(false);
                    HeronApplication.Log("Disconnected from the ribbon.");
                    return Result.Succeeded;
                }

                bridge.Start();
                HeronApplication.SetBridgeIcon(true);
                HeronApplication.Log("Connected from the ribbon.");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // The icon follows what the bridge actually is, not what was
                // attempted. A half-started bridge rolls itself back, so this
                // reads false - and the button must not claim otherwise.
                HeronApplication.SetBridgeIcon(bridge.IsRunning);
                HeronApplication.Log("Bridge toggle failed: " + ex);
                message = "Could not change the Heron bridge: " + ex.Message;
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
                    "Not connected.\n\nPress Heron on the ribbon to make this session reachable.");
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
