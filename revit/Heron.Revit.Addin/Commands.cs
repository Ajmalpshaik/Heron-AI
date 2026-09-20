// Heron-Agent:  HERON-REVIT-CON-001, HERON-REVIT-HLT-025, HERON-REVIT-UI-022, HERON-OPS-STP-007
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Globalization;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Connecting and disconnecting, in the one place that does it.
    ///
    /// TWO THINGS NOW ASK FOR IT - the ribbon button and the Bridge Status
    /// window - and a second copy of these six lines is how the ribbon
    /// picture ends up disagreeing with the bridge. The picture is the state
    /// (see the ribbon skill), so whatever flips the bridge has to be the
    /// thing that repaints the button, every time, including when it fails.
    ///
    /// IT THROWS RATHER THAN REPORTING. The two callers say it differently -
    /// a ribbon command hands Revit a `message`, a window writes a line in
    /// its own footer - so the wording belongs to them and only the doing
    /// belongs here.
    /// </summary>
    internal static class HeronBridgeToggle
    {
        /// <summary>
        /// Flips the bridge, and leaves the ribbon picture telling the truth.
        ///
        /// <paramref name="where"/> is for the log alone, and it earns its
        /// place: "Connected from the ribbon" and "Connected from Bridge
        /// Status" answer different questions on the day a session turns out
        /// to have been connected by somebody who does not remember doing it.
        /// </summary>
        internal static void Toggle(string where)
        {
            var bridge = HeronApplication.Bridge;
            if (bridge == null)
                throw new InvalidOperationException(
                    "Heron did not start, so there is nothing to connect. The log that says why is in "
                    + HeronPaths.Logs + ".");

            try
            {
                if (bridge.IsRunning)
                {
                    bridge.Stop();
                    HeronApplication.SetBridgeIcon(false);
                    HeronApplication.Log("Disconnected from " + where + ".");
                    return;
                }

                bridge.Start();
                HeronApplication.SetBridgeIcon(true);
                HeronApplication.Log("Connected from " + where + ".");
            }
            catch
            {
                // The icon follows what the bridge ACTUALLY is, not what was
                // attempted. A half-started bridge rolls itself back, so this
                // reads false - and the button must not claim otherwise.
                HeronApplication.SetBridgeIcon(bridge.IsRunning);
                throw;
            }
        }
    }

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
                HeronBridgeToggle.Toggle("the ribbon");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // NO SetBridgeIcon HERE ANY MORE. It used to be, and it had
                // to be - but the rule it enforced moved into Toggle, which
                // repaints from what the bridge ACTUALLY is before it
                // rethrows. Doing it twice would not be wrong, only a second
                // place that has to be remembered, which is how the first
                // copy of this rule went stale.
                HeronApplication.Log("Bridge toggle failed: " + ex);
                message = "Could not change the Heron bridge: " + ex.Message;
                return Result.Failed;
            }
        }
    }

    /// <summary>
    /// Turns Heron's ability to change the model on, or off. One button,
    /// both ways - the same shape as ConnectCommand, for the same reason.
    ///
    /// This writes the SAME setting D-19 defined (write.enabled) and changes
    /// nothing about how it is enforced: HeronPermissions.Allows still reads
    /// the file fresh on every call, and the default is still false. What it
    /// changes is that the state is now VISIBLE - a session somebody left
    /// writable no longer looks identical to a safe one.
    ///
    /// Turning it ON asks first. Turning it OFF never does: making the safe
    /// direction slower is how people learn to click through warnings, and
    /// there is nothing to confirm about becoming read-only.
    ///
    /// NO TRANSACTION, and it touches no element. It edits a file in the
    /// user's own data folder.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class WriteToggleCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var enabled = HeronPermissions.WriteEnabled();

                if (!enabled)
                {
                    // The one dialog this command is allowed: a confirmation
                    // BEFORE a risky change, which is what turning this on is.
                    // It names what becomes possible rather than asking "are
                    // you sure" about nothing in particular.
                    //
                    // A WINDOW SINCE 2026-09-20, asked for by Ajmal in the
                    // same breath as Bridge Status. What changed and why is
                    // written up on HeronChangesWindow; the short version is
                    // that the sentence which matters - Heron will be able to
                    // move, edit and create elements - sat at the same weight
                    // as the sentence about heron.config.
                    var answer = HeronChangesWindow.Ask(HeronApplication.Log);

                    // NULL IS NOT NO. The window could not be drawn, so
                    // nobody has been asked yet - and a permission question
                    // that answers itself is the one outcome this must never
                    // have, in either direction. Saying no here would be
                    // safe and would also be a lie about what the user
                    // chose, and they would press the padlock again and
                    // watch nothing happen.
                    if (!answer.HasValue) answer = AskPlainly();

                    if (answer != true)
                    {
                        HeronApplication.Log("Write toggle: offered, declined. Still off.");
                        return Result.Cancelled;
                    }
                }

                var now = HeronPermissions.SetWriteEnabled(!enabled);
                HeronApplication.SetWriteIcon(now);
                HeronApplication.Log("Write permission set to " + now + " from the ribbon.");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // The picture follows what the SETTING actually is, read back
                // rather than assumed - the same rule ConnectCommand follows.
                // A failed save that left the file unchanged must not leave a
                // button claiming the change happened.
                try { HeronApplication.SetWriteIcon(HeronPermissions.WriteEnabled()); }
                catch { /* the icon is the lesser problem; report the real one */ }

                HeronApplication.Log("Write toggle failed: " + ex);
                message = "Could not change whether Heron may edit models. The setting is in "
                        + HeronConfig.FilePath + " and can be changed there instead. Revit said: "
                        + ex.Message;
                return Result.Failed;
            }
        }

        /// <summary>
        /// The same question with no window around it. TRUE to turn changes
        /// on.
        ///
        /// Word for word what the ribbon padlock asked before the window
        /// existed, deliberately unchanged: a fallback that has already been
        /// used in Revit is worth more than a better sentence nobody has ever
        /// read. It is reached only when the window will not draw, which a
        /// WPF window inside a host application has more ways to do than a
        /// TaskDialog has.
        /// </summary>
        private static bool AskPlainly()
        {
            var ask = new TaskDialog("Let Heron change this model?")
            {
                MainInstruction = "Allow Heron to change models?",
                MainContent =
                    "Heron will be able to move, edit and create elements when you ask it to. "
                    + "It still previews first and still asks before keeping anything, and "
                    + "every change is one Ctrl+Z.\n\n"
                    + "This stays on until you turn it off, including after Revit restarts. "
                    + "The ribbon padlock shows which state you are in.",
                CommonButtons = TaskDialogCommonButtons.None,
                AllowCancellation = true
            };
            ask.AddCommandLink(TaskDialogCommandLinkId.CommandLink1,
                "Turn changes on", "Heron may change models until you turn this off.");
            ask.AddCommandLink(TaskDialogCommandLinkId.CommandLink2,
                "Leave it off", "Heron keeps reading only. Nothing changes.");

            return ask.Show() == TaskDialogResult.CommandLink1;
        }    }

    /// <summary>
    /// Reports bridge state. Reads nothing from the model, opens no
    /// transaction, and needs no document - it answers on the start screen
    /// too, which is exactly where somebody whose ribbon looks wrong will go
    /// looking.
    ///
    /// SINCE 2026-09-20 THIS IS A WINDOW, not a TaskDialog. What changed and
    /// why is written up on HeronBridgeStatusWindow; the short version is
    /// that six aligned lines of pipe names and file paths answered the
    /// question in their first word and then buried it under the evidence.
    ///
    /// THE OLD DIALOG IS STILL HERE, as the fallback. A WPF window inside a
    /// host application has more ways to fail than a TaskDialog does, and if
    /// it will not draw the answer still has to arrive - in the plainest form
    /// Revit has. Somebody who pressed Bridge Status and got nothing at all
    /// has not learnt that a window failed; they have learnt that the button
    /// does not work.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class StatusCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var shown = HeronBridgeStatusWindow.Show(
                ReadStatus,
                delegate { HeronBridgeToggle.Toggle("Bridge Status"); },
                HeronApplication.Log);

            if (!shown) ShowPlainStatus();
            return Result.Succeeded;
        }

        /// <summary>
        /// A snapshot of the bridge as text, which is all the window is
        /// allowed to know. Handed over as a delegate rather than called once,
        /// because the window calls it again after every connect or
        /// disconnect - a snapshot taken once and trusted for ever is how a
        /// window ends up describing a session that has moved on.
        /// </summary>
        private static HeronBridgeStatus ReadStatus()
        {
            var status = new HeronBridgeStatus
            {
                LogFolder = HeronPaths.Logs,

                // Read fresh, never remembered. This is the same file the
                // permission gate reads on every single call (D-19), so
                // caching it here would be a second answer to a question that
                // is only allowed one.
                WriteEnabled = HeronPermissions.WriteEnabled(),
            };

            var bridge = HeronApplication.Bridge;
            if (bridge == null)
            {
                status.State = HeronBridgeState.DidNotStart;
                return status;
            }

            var id = bridge.Identity;
            status.State = bridge.IsRunning
                ? HeronBridgeState.Connected
                : HeronBridgeState.NotConnected;
            status.PipeName = id.PipeName;
            status.ProcessId = id.ProcessId.ToString(CultureInfo.InvariantCulture);
            status.RevitVersion = id.RevitVersion;
            status.AddinVersion = id.AddinVersion;
            status.ProtocolVersion = Heron.Bridge.BridgeIdentity.ProtocolVersion
                .ToString(CultureInfo.InvariantCulture);
            status.DiscoveryFilePath = id.DiscoveryFilePath;
            return status;
        }

        /// <summary>
        /// The answer with no window around it.
        ///
        /// Word for word what Bridge Status said before the window existed,
        /// deliberately unchanged: a fallback that has already been run in
        /// Revit is worth more than a better sentence nobody has ever seen.
        /// Only the title moved, and it moved to follow the ribbon tab.
        /// </summary>
        private static void ShowPlainStatus()
        {
            var bridge = HeronApplication.Bridge;

            if (bridge == null)
            {
                TaskDialog.Show("Heron",
                    "Heron did not start.\n\nThe log that says why is in\n"
                    + HeronPaths.Logs + ".");
                return;
            }

            if (!bridge.IsRunning)
            {
                TaskDialog.Show("Heron",
                    "Not connected.\n\nPress Heron on the ribbon to make this session reachable.");
                return;
            }

            var id = bridge.Identity;
            TaskDialog.Show("Heron",
                "Connected.\n\n" +
                "Pipe:      " + id.PipeName + "\n" +
                "Process:   " + id.ProcessId + "\n" +
                "Revit:     " + id.RevitVersion + "\n" +
                "Add-in:    " + id.AddinVersion + "\n" +
                "Protocol:  " + Heron.Bridge.BridgeIdentity.ProtocolVersion + "\n\n" +
                "Announced in:\n" + id.DiscoveryFilePath);
        }
    }

    /// <summary>
    /// Emergency Stop. Stops Heron changing anything further, and lets it
    /// work again on a second press.
    ///
    /// NOTHING CALLS THIS TODAY. The ribbon button was removed on
    /// 2026-09-06 (D-46), so this command is reachable from nowhere and
    /// HeronStop can no longer be switched on by anybody. The class is
    /// kept deliberately, not by oversight: it is the whole trigger side
    /// of the stop, and one PushButtonData in HeronApplication.BuildRibbon
    /// puts it back. Read D-46 before deleting it.
    ///
    /// It was a ribbon button rather than a chat command because of WHEN it
    /// is needed: the user is looking at Revit, something is happening they
    /// did not mean, and the last thing that should stand between them and
    /// stopping it is finding a window and composing a sentence. That
    /// argument is unchanged; what changed is that the owner decided he did
    /// not want the button.
    ///
    /// The dialog is honest about the limit. This stops the NEXT thing Heron
    /// would do; it cannot reach into a Revit API call already running. If
    /// something has already changed, Ctrl+Z is the answer, and the user needs
    /// to know that in the same breath - a stop button they believe is an
    /// abort button is worse than none, because they stop reaching for undo.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class EmergencyStopCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (HeronStop.IsStopped)
            {
                HeronStop.Resume();
                TaskDialog.Show("Heron",
                    "Heron can work again.\n\n" +
                    "It will still ask you to approve anything that changes the model.");
                return Result.Succeeded;
            }

            HeronStop.Stop();
            TaskDialog.Show("Heron",
                "Heron is stopped.\n\n" +
                "Nothing more will be sent to the model until you press this again.\n\n" +
                "This stops what comes NEXT. It cannot interrupt something Revit has already " +
                "started - if a change has already happened, Ctrl+Z in Revit is what puts it back.");
            return Result.Succeeded;
        }
    }
}
