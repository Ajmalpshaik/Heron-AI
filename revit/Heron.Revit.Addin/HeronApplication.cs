// Heron-Agent:  HERON-REVIT-RIB-023, HERON-REVIT-VER-002
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using Heron.Bridge;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Heron's entry point into Revit.
    ///
    /// STEP 1 - see docs/27-build-order.md.
    ///
    /// This deliberately does NOT touch the Revit API beyond building a
    /// ribbon. No document, no elements, no transactions. The single question
    /// this step answers is: does the add-in load, and can something outside
    /// Revit talk to it?
    ///
    /// Step 2 adds the ExternalEvent that lets the bridge reach the Revit
    /// API at all - the constraint absent from all four specification parts
    /// and confirmed by the field notes (docs/00e).
    /// </summary>
    public sealed class HeronApplication : IExternalApplication
    {
        private const string TabName = "Heron AI";
        private const string PanelName = "Bridge";

        /// <summary>The pictures that ARE the connected / disconnected state.</summary>
        internal const string ConnectedIcon = "BridgeConnected.png";
        internal const string DisconnectedIcon = "BridgeDisconnected.png";

        internal static BridgeServer Bridge { get; private set; }

        /// <summary>
        /// The one bridge button, captured as the ribbon is built.
        ///
        /// A Revit PushButton carries no on/off state of its own, so the
        /// button's own picture is the state and the command swaps it after
        /// every toggle. Capturing the instance here is the only way to reach
        /// it again - the ribbon API offers no way to look a button up later.
        /// </summary>
        internal static PushButton BridgeButton { get; private set; }

        /// <summary>
        /// The one way into the Revit API. Created on Revit's own thread
        /// during startup, because ExternalEvent.Create is not valid from a
        /// background thread.
        /// </summary>
        private static RevitDispatcher Dispatcher;

        /// <summary>
        /// What Revit shows while Heron is working - HERON-REVIT-UI-022.
        ///
        /// Built HERE and nowhere else, because it captures Revit's own WPF
        /// dispatcher from whichever thread constructs it, and OnStartup is
        /// the one place guaranteed to be Revit's. Built from a background
        /// thread it would post its work to a thread that draws nothing, and
        /// nothing would ever appear - with no error to say why.
        /// </summary>
        private static HeronActivityBanner Banner;

        internal static string LogDirectory { get; private set; }

        // Two listener threads and the Revit thread all log. AppendAllText from
        // several at once throws a sharing violation, and the catch below would
        // swallow it - losing the line silently. For something Golden Rule 14
        // calls evidence, that is not acceptable.
        private static readonly object LogLock = new object();

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Paths come from the Path Manager, never built here. An
                // earlier version of this line hardcoded %APPDATA% and quietly
                // disagreed with HeronPaths - which is the exact drift the
                // Path Manager exists to prevent (docs/06 section 2).
                LogDirectory = HeronPaths.Logs;

                var config = HeronConfig.Load();
                PruneLogs(config.GetInt("log.retainDays", 14));

                var revitVersion = application.ControlledApplication.VersionNumber;
                var addinVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                Bridge = new BridgeServer(
                    new BridgeIdentity(revitVersion, addinVersion), Log);

                // Revit's thread, so the banner captures the right
                // dispatcher. On by default: a frozen Revit with no
                // explanation reads as a crash, and the person who most needs
                // telling is the one who has not gone looking for a setting.
                Banner = new HeronActivityBanner(
                    config.GetBool("ui.activityBanner", true), Log);

                // The thread hop. OnStartup runs on Revit's thread, which is
                // the only place the event may be created - and it is wired in
                // before the bridge can start, so no request can ever arrive
                // to find it missing.
                Dispatcher = new RevitDispatcher(Log,
                    revitVersion + "/" + Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture),
                    Banner);
                Dispatcher.Register();
                Bridge.RequestHandler = Dispatcher.Dispatch;

                BuildRibbon(application);

                Log(string.Format(
                    "Heron loaded. Revit {0}, add-in {1}, pid {2}.",
                    revitVersion, addinVersion, Process.GetCurrentProcess().Id));

                // Connecting is explicit by default - a Revit that was never
                // connected is invisible to every chat, and that is a safety
                // property worth keeping (docs/00e).
                //
                // bridge.autoConnect exists for unattended testing and for
                // users who have decided they want it. It only starts the
                // pipe; it grants nothing. Which model may be touched is
                // decided later, by session binding and document pinning.
                if (config.GetBool("bridge.autoConnect", false))
                {
                    Bridge.Start();
                    SetBridgeIcon(true);
                    Log("bridge.autoConnect is on - bridge started without a button press.");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Never take Revit down. A failed add-in should be visible and
                // inert, not fatal.
                TaskDialog.Show("Heron AI", "Heron failed to start:\n\n" + ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                Dispatcher = null;

                // Before the bridge, so a request arriving mid-shutdown cannot
                // raise a banner onto a dispatcher that is going away.
                if (Banner != null)
                {
                    Banner.Shutdown();
                    Banner = null;
                }

                if (Bridge != null)
                {
                    Bridge.Stop();
                    Bridge.Dispose();
                    Bridge = null;
                }
            }
            catch (Exception ex)
            {
                Log("Shutdown error: " + ex.Message);
            }
            return Result.Succeeded;
        }

        private static void BuildRibbon(UIControlledApplication application)
        {
            try { application.CreateRibbonTab(TabName); }
            catch (Autodesk.Revit.Exceptions.ArgumentException) { /* tab exists */ }

            var panel = application.CreateRibbonPanel(TabName, PanelName);
            var assemblyPath = Assembly.GetExecutingAssembly().Location;

            // One button, both directions. Click to connect, click again to
            // disconnect - and the picture on it says which state you are in, so
            // "am I connected?" is answered without clicking anything.
            var toggle = new PushButtonData(
                "HeronBridgeToggle",
                "Heron",
                assemblyPath,
                typeof(ConnectCommand).FullName);
            toggle.ToolTip = "Connect or disconnect this Revit session. Click again to reverse it.";
            toggle.LongDescription =
                "Opens a local named pipe so Heron can reach this Revit session, and announces " +
                "it so a chat can find it. Click again to close it.\n\n" +
                "The picture shows the state: lit when connected, dark when not. A Revit that " +
                "was never connected is invisible to every chat, by design.";

            // No "\n" in this label, unlike the others: it is a row in a list
            // now, not a ribbon button, and the wrap it used to force there
            // would split it across two lines here for no reason.
            var status = new PushButtonData(
                "HeronStatus",
                "Bridge Status",
                assemblyPath,
                typeof(StatusCommand).FullName);
            status.ToolTip = "Show whether the bridge is running, and on which pipe.";

            // Connecting and asking about the connection are the same subject
            // at two depths, so they are one control rather than two buttons.
            // The top half is the toggle, pressed constantly; the arrow holds
            // the detail, wanted only when something looks wrong.
            var bridgeGroup = panel.AddItem(
                new SplitButtonData("HeronBridge", "Heron")) as SplitButton;

            // AddPushButton hands back the live button, which is the only way
            // to reach it again - the ribbon API offers no lookup, and this
            // one has to be found after every toggle to change its picture.
            BridgeButton = bridgeGroup.AddPushButton(toggle);
            bridgeGroup.AddPushButton(status);

            // A split button normally promotes whatever was last picked from
            // the list to the top. Here the top button's picture IS the
            // connected / disconnected state, so one look at Bridge Status
            // would replace the state indicator with something that has no
            // state - and it would never come back.
            bridgeGroup.IsSynchronizedWithCurrentItem = false;

            // Starts on the disconnected picture - the bridge never connects on
            // its own unless bridge.autoConnect says so, and OnStartup corrects
            // this afterwards when it does.
            SetBridgeIcon(false);

            // NO EMERGENCY STOP BUTTON - removed on Ajmal's instruction,
            // 2026-09-06 (D-46 in docs/DECISIONS.md). The switch behind it
            // survives on purpose: HeronStop and both gates that read it are
            // untouched, and EmergencyStopCommand is still a working command
            // that one AddItem here would put back.
            //
            // Be honest about what that leaves. Nothing can SET the stop any
            // more, so today it is a gate that will never close. It is kept
            // for the file-based kill switch docs/21 section 4 describes and
            // which was never built - NOT because there is a working
            // emergency stop. Do not delete it as dead code without reading
            // D-46 first.
        }

        /// <summary>
        /// Today's log file. One per day, so log.retainDays can mean what it
        /// says - a single ever-growing file cannot be retained for 14 days.
        /// </summary>
        private static string CurrentLogPath()
        {
            return Path.Combine(
                LogDirectory ?? HeronPaths.Logs,
                "addin-" + DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log");
        }

        /// <summary>
        /// Deletes log files past their retention. Logs are DERIVED state, so
        /// removing them is always a valid recovery action - never touch
        /// anything under DATA from here.
        /// </summary>
        private static void PruneLogs(int retainDays)
        {
            if (retainDays < 1) return;
            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-retainDays);
                foreach (var file in Directory.GetFiles(LogDirectory, "addin-*.log"))
                {
                    if (File.GetLastWriteTimeUtc(file) >= cutoff) continue;
                    if (!HeronPaths.IsSafeToDelete(file)) continue;
                    File.Delete(file);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        /// <summary>
        /// Puts the button's picture in step with the bridge. After every
        /// toggle, and once at startup.
        /// </summary>
        internal static void SetBridgeIcon(bool connected)
        {
            var button = BridgeButton;
            if (button == null) return;

            var loader = new IconLoader(Assembly.GetExecutingAssembly().Location);
            var fileName = connected ? ConnectedIcon : DisconnectedIcon;

            var large = loader.LoadLarge(fileName);
            if (large != null) button.LargeImage = large;

            var small = loader.LoadSmall(fileName);
            if (small != null) button.Image = small;
        }

        internal static void Log(string message)
        {
            try
            {
                lock (LogLock)
                {
                    File.AppendAllText(
                        CurrentLogPath(),
                        // UtcNow, not Now: the "u" format stamps a trailing Z, so local time
                        // here would label every line UTC while being hours out locally. UTC
                        // also matches startedAt in the discovery file, so the two line up.
                        string.Format("{0:u}  {1}{2}", DateTime.UtcNow, message, Environment.NewLine));
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
