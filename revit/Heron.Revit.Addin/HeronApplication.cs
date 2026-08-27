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

        internal static BridgeServer Bridge { get; private set; }
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

            var connect = new PushButtonData(
                "HeronConnect",
                "Connect\nHeron",
                assemblyPath,
                typeof(ConnectCommand).FullName);
            connect.ToolTip = "Start the Heron bridge for this Revit session.";
            connect.LongDescription =
                "Opens a local named pipe so Heron can reach this Revit session, and " +
                "announces it so a chat can find it.\n\n" +
                "A Revit that has never been connected is invisible to every chat.";

            var status = new PushButtonData(
                "HeronStatus",
                "Bridge\nStatus",
                assemblyPath,
                typeof(StatusCommand).FullName);
            status.ToolTip = "Show whether the bridge is running, and on which pipe.";

            panel.AddItem(connect);
            panel.AddItem(status);
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
