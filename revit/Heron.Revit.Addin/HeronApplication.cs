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
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
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
        // THE RIBBON LABELS, and only the labels.
        //
        // D-37 settled that the product is called Heron AI and said the name
        // "stays in the ribbon tab". D-85 narrows that one sentence on
        // Ajmal's instruction, 2026-09-20: the TAB reads "Heron", and the
        // "AI" moves onto the panel that actually carries it - the bridge is
        // the thing an AI talks through, and nothing else on this tab is.
        //
        // The product name is untouched: assemblies, folders, the manifest
        // and every document still say Heron AI. Only what Revit draws
        // changed, so nothing installed or written down breaks.
        //
        // These two strings are the ribbon path users are TOLD, so anything
        // that prints it - the MCP server, the health report, the deploy
        // script - has to agree with them. "Heron > AI Bridge > Heron".
        private const string TabName = "Heron";
        private const string PanelName = "AI Bridge";

        /// <summary>The pictures that ARE the connected / disconnected state.</summary>
        internal const string ConnectedIcon = "BridgeConnected.png";
        internal const string DisconnectedIcon = "BridgeDisconnected.png";

        internal const string WriteLockedIcon = "WriteLocked.png";
        internal const string WriteUnlockedIcon = "WriteUnlocked.png";

        // The Admin and Publish switches (D-106). The Changes padlock with a
        // mark where its keyhole was - a gear for Admin, an arrow leaving a
        // tray for Publish - in the same two colours, so all three read as
        // one family: Mist and closed is off, Error red and open is on (the
        // brand's colours, D-149; blue and red until 2026-11-07).
        internal const string AdminLockedIcon = "AdminLocked.png";
        internal const string AdminUnlockedIcon = "AdminUnlocked.png";
        internal const string PublishLockedIcon = "PublishLocked.png";
        internal const string PublishUnlockedIcon = "PublishUnlocked.png";

        // The Ask to Keep switch (D-138): the same padlock family, with a
        // question mark where the keyhole was. Mist and closed is the safe
        // side - Revit asks before a change is kept - and Error red and open is
        // kept without asking, whether by the switch or by one job's grant.
        internal const string KeepAskingIcon = "KeepAsking.png";
        internal const string KeepNotAskingIcon = "KeepNotAsking.png";

        // Free this model (D-141): one picture, because the button is an act
        // and not a state - which chat holds a model changes with every
        // request, and the log says what each press freed.
        internal const string FreeModelIcon = "FreeModel.png";

        // The Settings button (D-150): a gear in the brand's colours. One
        // picture - the button opens a window and is not itself a state.
        internal const string SettingsIcon = "Settings.png";

        // The Companion button (D-109): lit in the brand's orange while the
        // Companion is on, grey while it is off.
        internal const string CompanionOnIcon = "CompanionOn.png";
        internal const string CompanionOffIcon = "CompanionOff.png";

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
        /// The write-permission button, captured for the same reason the
        /// bridge button is: its picture IS the state, and the ribbon API
        /// offers no way to look a button up again.
        /// </summary>
        internal static PushButton WriteButton { get; private set; }

        /// <summary>
        /// The Admin and Publish buttons (D-106), captured for the same
        /// reason as the other two: the picture is the switch, and there is
        /// no way to look a button up again.
        /// </summary>
        internal static PushButton AdminButton { get; private set; }

        internal static PushButton PublishButton { get; private set; }

        /// <summary>
        /// The Ask to Keep button (D-138), captured for the same reason: its
        /// picture is the switch - and, while a job's grant is held, says so.
        /// </summary>
        internal static PushButton AskToKeepButton { get; private set; }

        // Whether the button was last painted showing a job's grant. Idling
        // looks at the grant again only while this is true, so with no grant
        // every Idling call costs one test of a bool.
        private static bool _keepGrantPainted;

        // How often Idling looks whether a painted grant has run out.
        private static readonly TimeSpan GrantLookInterval = TimeSpan.FromSeconds(15);
        private static readonly Stopwatch SinceGrantLook = Stopwatch.StartNew();

        internal static PushButton CompanionButton { get; private set; }

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
        //
        // AND THE ANALYSIS WAS RIGHT WITH ITS SCOPE ONE SHORT. This lock is
        // static, so it serialises the threads of ONE process - and
        // addin-YYYYMMDD.log is one file per machine per day, which every
        // Revit session on the machine appends to. A second Revit is a second
        // process with its own LogLock that this one cannot see, and the
        // sharing violation it describes was exactly what happened. The lock
        // stays and HeronAppend adds what it cannot reach: FileShare.ReadWrite
        // so the writers are not refused, a system-wide mutex so they take
        // turns, and an answer when the line did not land.
        // FRAGMENT-ISSUES section 5b, row 19.
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

                // A LOST AUDIT LINE GOES SOMEWHERE. The kernel depends on
                // nothing, so HeronAudit cannot call this logger - it offers
                // a hook instead, and this is the one place that has both.
                // The audit trail is evidence and is never pruned; the
                // verbose log is disposable and rotated, which is exactly
                // where the note about a hole belongs.
                // FRAGMENT-ISSUES section 5b, row 5.
                HeronAudit.ReportLoss = Log;

                var config = HeronConfig.Load();
                PruneLogs(config.GetInt("log.retainDays", 14));

                // AND THE RUN RECORDS, by the same rule: DERIVED, so deleting
                // an old one is always a valid recovery action, and every
                // delete asks HeronPaths.IsSafeToDelete first (HeronRuns).
                HeronRuns.Prune(config.GetInt("runs.retainDays", 14));

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

                // WHICH MODEL THE BANNER NAMES, kept current by Revit
                // itself. The banner is raised from a listener thread,
                // where the Revit API may not be touched, so the name it
                // announces can only be one Revit pushed out earlier -
                // and these two events are every moment it changes.
                //
                // Subscribed AFTER the dispatcher exists, because the
                // handlers hand what they read straight to it. Revit can
                // activate a view before OnStartup returns.
                application.ViewActivated += OnViewActivated;
                application.ControlledApplication.DocumentClosing += OnDocumentClosing;

                // A JOB'S KEEP-WITHOUT-ASKING GRANT RUNS OUT AFTER TEN QUIET
                // MINUTES (D-138), and the ribbon must stop saying it is held
                // when it has. Idling is Revit's own thread, as the ribbon needs.
                application.Idling += OnIdling;

                // THE RUN GUARD (Constitution Article 10): saves, syncs,
                // exports and prints, new and opened documents, and changes
                // to any open model - heard for the whole session, acted on
                // only while the dispatcher has a job armed. Before the
                // bridge can start, so no job can ever run unguarded.
                HeronRunGuard.Attach(application, Log);

                // WHAT A ROLLBACK MUST NOT TAKE: the elements each write job
                // adds, and the modeller's own undos, as Revit reports them.
                // Permanent, because a Ctrl+Z between two jobs is exactly the
                // moment no request is running. See RevitFragment.Listen.
                RevitFragment.Listen(application.ControlledApplication);

                // A REVIT DIALOG THAT OPENS INSIDE A JOB IS ANSWERED, not left
                // for a modeller who is not there - FRAGMENT-ISSUES 5b row 300.
                // Inert until the dispatcher arms it around a job, so a dialog
                // in the modeller's own work is never touched. HeronDialogWatch.
                HeronDialogWatch.Attach(application, Log);

                // WHAT THE COMPANION PAGE READS (D-108): the model, view and
                // selection, written to a file only while the bridge is
                // connected. Before BuildRibbon, whose SetBridgeIcon(false)
                // is what clears a file a crashed session left behind.
                HeronLiveState.Attach(application);

                BuildRibbon(application);

                // THE HERON PANEL (docs/49). Revit takes a dockable pane only
                // during OnStartup. Its own fault costs the panel, never the
                // add-in: the bridge and the ribbon above are already up.
                try
                {
                    application.RegisterDockablePane(HeronCommandsPane.Id, HeronCommandsPane.Title,
                        new HeronCommandsPane(Log));
                }
                catch (Exception paneFailure)
                {
                    Log("The Heron Commands panel could not be registered: " + paneFailure);
                }

                // ROSLYN'S FIRST-USE COST, PAID NOW rather than on the
                // modeller's first request (D-56 measured ~4.5 s of it). A
                // background thread that touches no Revit API - see WarmUp.
                if (config.GetBool("fragments.warmUp", true))
                    StartFragmentWarmUp();

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
                //
                // WHAT HAPPENED, THEN WHAT TO DO NEXT - and the second half
                // used to be missing. This showed ex.Message on its own, so a
                // null reference inside BuildRibbon read as "Object reference
                // not set to an instance of an object." to a BIM modeller.
                // The conventions carry the worked example for this exact
                // dialog, and LogDirectory is assigned on the first line of
                // this method, before anything that can realistically fail,
                // so the path is always there to name. Revit's own words are
                // kept and ATTRIBUTED rather than presented as Heron's, which
                // is the pattern RevitWrite already uses.
                // FRAGMENT-ISSUES section 5b, row 20.
                TaskDialog.Show("Heron",
                    "Heron did not start, so its ribbon will be missing or unusable. " +
                    "Revit itself is fine and your model is untouched.\n\n" +
                    "The log that says why is in " + (LogDirectory ?? HeronPaths.Logs) + "\n\n" +
                    "Windows said: " + ex.Message);
                return Result.Failed;
            }
        }

        /// <summary>
        /// The warm-up is an optimisation, so it may never be the reason
        /// Heron does not start. Its own try cannot cover loading RevitFragment
        /// and Roslyn - that happens before WarmUp's first line - so the call
        /// is caught here, and the type is named only in CallFragmentWarmUp,
        /// so the JIT resolves it inside this try and not before it.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void StartFragmentWarmUp()
        {
            try
            {
                CallFragmentWarmUp();
            }
            catch (Exception failure)
            {
                Log("Fragment warm-up could not load, so the first fragment will be slow as before: " +
                    failure.Message);
            }
        }

        /// <summary>
        /// The only method that names RevitFragment, so a Roslyn that fails to
        /// load fails while THIS is JIT-compiled - which happens at the call
        /// inside StartFragmentWarmUp's try, not before it.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void CallFragmentWarmUp()
        {
            RevitFragment.WarmUp(Log,
                HeronConfig.Load().GetInt("revit.operationTimeoutSeconds", 60));
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                // Off the events before the dispatcher goes, so a view
                // activating mid-shutdown cannot reach a handler whose
                // dispatcher has already been dropped.
                application.ViewActivated -= OnViewActivated;
                application.ControlledApplication.DocumentClosing -= OnDocumentClosing;
                application.Idling -= OnIdling;
                HeronRunGuard.Detach(application);
                RevitFragment.StopListening(application.ControlledApplication);
                HeronDialogWatch.Detach(application);
                HeronLiveState.Detach(application);

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
            // Hover texts are one short sentence each - the owner's word of
            // 2026-11-07 (D-150): simple, understandable, no detail.
            toggle.ToolTip = "Connect Heron to this Revit. Orange heron: connected. Click again to disconnect.";

            // No "\n" in this label, unlike the others: it is a row in a list
            // now, not a ribbon button, and the wrap it used to force there
            // would split it across two lines here for no reason.
            var status = new PushButtonData(
                "HeronStatus",
                "Bridge Status",
                assemblyPath,
                typeof(StatusCommand).FullName);
            status.ToolTip = "See whether Heron is connected to this Revit.";

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

            // FREE THIS MODEL - D-141, and since D-150 a row in this menu with
            // no picture, like Bridge Status, rather than a button of its own.
            // One model is used by one chat at a time; this frees the model in
            // front at once, whichever chat holds it, without closing that
            // chat. It changes no element, so it asks nothing and announces
            // nothing - the add-in log says what it freed. No availability
            // class: Revit greys it on the start screen, where there is no
            // model to free.
            var freeModel = new PushButtonData(
                "HeronFreeModel",
                "Free this model",
                assemblyPath,
                typeof(FreeModelCommand).FullName);
            freeModel.ToolTip = "Let another chat use the model in front now.";
            bridgeGroup.AddPushButton(freeModel);

            // COMMANDS (docs/49) - the panel of saved commands, a text row in
            // this menu like Free this model, so the ribbon keeps its three
            // things (D-150). Shows or hides the docked pane registered below.
            var commandsRow = new PushButtonData(
                "HeronCommandsPane",
                "Commands",
                assemblyPath,
                typeof(CommandsPaneCommand).FullName);
            commandsRow.ToolTip = "Show or hide your saved Heron commands.";
            bridgeGroup.AddPushButton(commandsRow);

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

            // SETTINGS - D-150, the owner's word of 2026-11-07. The four
            // switches - Changes, Admin, Publish, Ask to Keep - left the ribbon
            // for one window, where each switch's state is its own button and
            // its padlock picture sits beside it. Each still runs its own
            // command (WriteToggleCommand and the rest), so the question on the
            // way to the risky side, the fallback and the file are unchanged.
            // The ribbon no longer shows at a glance whether Changes is on:
            // the owner chose that, knowing it (D-150 says so).
            var settings = new PushButtonData(
                "HeronSettings",
                "Settings",
                assemblyPath,
                typeof(SettingsCommand).FullName);
            settings.ToolTip = "Turn Heron's switches on or off: Changes, Admin, Publish, Ask to Keep, Companion.";

            var settingsButton = panel.AddItem(settings) as PushButton;
            if (settingsButton != null)
            {
                var settingsLoader = new IconLoader(assemblyPath);
                var settingsLarge = settingsLoader.LoadLarge(SettingsIcon);
                if (settingsLarge != null) settingsButton.LargeImage = settingsLarge;
                var settingsSmall = settingsLoader.LoadSmall(SettingsIcon);
                if (settingsSmall != null) settingsButton.Image = settingsSmall;
            }

            // THE COMPANION (D-108, D-109) - ONE PUSH BUTTON since 2026-11-08
            // (D-150's amendment). It opens the chat's page in the browser; its
            // picture is the switch's state, orange on and grey off. The switch
            // itself lives in Settings with the other four - the arrow that
            // held it is gone. It shows and never changes the model, so it
            // asks for no confirmation - unlike the three padlocks.
            var companionOpen = new PushButtonData(
                "HeronCompanionOpen",
                "Companion",
                assemblyPath,
                typeof(CompanionOpenCommand).FullName);
            companionOpen.ToolTip = "Open the Companion page in your browser. It only shows, never changes.";

            CompanionButton = panel.AddItem(companionOpen) as PushButton;
            SetCompanionIcon(HeronLiveState.Enabled);

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
            // The Companion's live file follows the bridge, and this is
            // called at every moment the bridge changes (D-108).
            HeronLiveState.BridgeChanged(connected);

            var button = BridgeButton;
            if (button == null) return;

            var loader = new IconLoader(Assembly.GetExecutingAssembly().Location);
            var fileName = connected ? ConnectedIcon : DisconnectedIcon;

            var large = loader.LoadLarge(fileName);
            if (large != null) button.LargeImage = large;

            var small = loader.LoadSmall(fileName);
            if (small != null) button.Image = small;
        }

        /// <summary>
        /// Puts the padlock in step with the write setting. After every
        /// toggle, and once at startup.
        ///
        /// The text changes with the picture. An icon alone is ambiguous to
        /// somebody who has not met it before - "locked" could as easily mean
        /// the button is disabled - and the ribbon label is the cheapest place
        /// to say which way round it is.
        /// </summary>
        internal static void SetWriteIcon(bool enabled)
        {
            var button = WriteButton;
            if (button == null) return;

            var loader = new IconLoader(Assembly.GetExecutingAssembly().Location);
            var fileName = enabled ? WriteUnlockedIcon : WriteLockedIcon;

            var large = loader.LoadLarge(fileName);
            if (large != null) button.LargeImage = large;

            var small = loader.LoadSmall(fileName);
            if (small != null) button.Image = small;

            button.ItemText = enabled ? "Changes ON" : "Changes off";
        }

        /// <summary>
        /// Puts the Admin button in step with admin.enabled. After every
        /// toggle, and once at startup - the SETTING, which can read ON while
        /// Changes is off; the refusal is what explains that case.
        /// </summary>
        internal static void SetAdminIcon(bool enabled)
        {
            PaintSwitch(AdminButton, enabled, AdminUnlockedIcon, AdminLockedIcon,
                        enabled ? "Admin ON" : "Admin off");
        }

        /// <summary>Puts the Publish button in step with publish.enabled. See SetAdminIcon.</summary>
        /// <summary>The Companion button's picture and words (D-109): orange on, grey off.</summary>
        internal static void SetCompanionIcon(bool on)
        {
            PaintSwitch(CompanionButton, on, CompanionOnIcon, CompanionOffIcon,
                        on ? "Companion" : "Companion off");
        }

        internal static void SetPublishIcon(bool enabled)
        {
            PaintSwitch(PublishButton, enabled, PublishUnlockedIcon, PublishLockedIcon,
                        enabled ? "Publish ON" : "Publish off");
        }

        /// <summary>
        /// Puts the Ask to Keep button in step with write.askBeforeKeeping and
        /// with a job's grant (D-138). Three states, read from what they ARE:
        /// asking, kept without asking by the switch, and kept without asking
        /// for the rest of one job - the open padlock, and the words say which.
        /// </summary>
        internal static void SetAskToKeepIcon(bool asks)
        {
            var job = asks && HeronKeepGrant.Session.Active(DateTime.UtcNow);
            _keepGrantPainted = job;
            PaintSwitch(AskToKeepButton, asks && !job, KeepAskingIcon, KeepNotAskingIcon,
                        !asks ? "Ask to Keep off" : job ? "Keep: rest of job" : "Ask to Keep ON");
        }

        /// <summary>
        /// The same, with the switch read fresh - for a job's grant given or
        /// ended inside a run, where a picture that failed to change must
        /// never become the run's failure.
        /// </summary>
        internal static void RepaintAskToKeep()
        {
            try
            {
                SetAskToKeepIcon(HeronPermissions.AsksBeforeKeeping());
            }
            catch (Exception ex)
            {
                Log("The Ask to Keep button could not be repainted: " + ex.Message);
            }
        }

        /// <summary>
        /// Revit's own thread, whenever it is idle. Only while the button shows
        /// a job's grant, and at most every GrantLookInterval: has the grant
        /// gone ten minutes with no change? Then it ends here, and the button
        /// stops saying it is held.
        /// </summary>
        private static void OnIdling(object sender, IdlingEventArgs e)
        {
            // Cheapest test first: with no grant painted, almost every Idling
            // call ends on this line.
            if (!_keepGrantPainted) return;
            if (SinceGrantLook.Elapsed < GrantLookInterval) return;
            SinceGrantLook.Restart();

            try
            {
                if (HeronKeepGrant.Session.Active(DateTime.UtcNow)) return;
                var ended = HeronKeepGrant.Session.End();
                if (ended != null) Log(HeronKeepGrant.EndedLine(HeronKeepGrant.EndedIdle, ended));
                RepaintAskToKeep();
            }
            catch (Exception ex)
            {
                // Idling is Revit's own thread: nothing may escape it.
                Log("The keep-the-rest-of-this-job grant could not be checked: " + ex.Message);
            }
        }

        /// <summary>
        /// One switch button's picture and words - SetWriteIcon's shape, for
        /// the two switches D-106 added. SetWriteIcon itself is left as it
        /// was: it has been seen in Revit, and these two have not.
        /// </summary>
        private static void PaintSwitch(PushButton button, bool on, string onIcon,
                                        string offIcon, string text)
        {
            if (button == null) return;

            var loader = new IconLoader(Assembly.GetExecutingAssembly().Location);
            var fileName = on ? onIcon : offIcon;

            var large = loader.LoadLarge(fileName);
            if (large != null) button.LargeImage = large;

            var small = loader.LoadSmall(fileName);
            if (small != null) button.Image = small;

            button.ItemText = text;
        }

        /// <summary>
        /// Revit's own thread: the model in front has changed.
        ///
        /// ViewActivated rather than DocumentOpened, because the question
        /// the banner answers is which model is IN FRONT, not which ones
        /// are loaded. Opening a second model and switching between them
        /// fires this each time; opening alone would leave the name on
        /// whichever was loaded last rather than the one being looked at.
        ///
        /// What is taken from the document is its NAME and nothing else.
        /// Holding the Document itself is what the conventions forbid,
        /// and for good reason - it would outlive the model it points at.
        /// A string cannot.
        /// </summary>
        private static void OnViewActivated(object sender, ViewActivatedEventArgs e)
        {
            try
            {
                var dispatcher = Dispatcher;
                if (dispatcher == null) return;

                var document = e == null ? null : e.Document;
                dispatcher.NoteActiveModel(document == null ? null : document.Title);

                HeronLiveState.ViewActivated(sender);
            }
            catch (Exception ex)
            {
                // A name the banner cannot show is worth nothing and must
                // cost nothing. This runs on Revit's own thread, where an
                // escaping exception is Revit's problem, not just Heron's.
                Log("Could not note which model is in front: " + ex.Message);
            }
        }

        /// <summary>
        /// Revit's own thread: a model is closing. See
        /// RevitDispatcher.ForgetActiveModel for why closing the last one
        /// would otherwise leave its name on screen for ever.
        /// </summary>
        private static void OnDocumentClosing(object sender, DocumentClosingEventArgs e)
        {
            // A JOB'S GRANT DOES NOT OUTLIVE ITS MODEL (D-138). The same model
            // opened again is a new start, and its first change is asked.
            try
            {
                var closing = e == null ? null : e.Document;
                if (closing != null
                    && HeronKeepGrant.Session.HeldFor(RevitOperations.DocumentKey(closing)))
                {
                    var ended = HeronKeepGrant.Session.End();
                    if (ended != null) Log(HeronKeepGrant.EndedLine(HeronKeepGrant.EndedClosed, ended));
                    RepaintAskToKeep();
                }
            }
            catch (Exception ex)
            {
                Log("Could not end the keep-the-rest-of-this-job grant: " + ex.Message);
            }

            // NOR DOES A CHAT'S HOLD ON IT (D-141). The same model opened
            // again is free, for whichever chat asks first.
            try
            {
                var closing = e == null ? null : e.Document;
                if (closing != null)
                {
                    var holder = HeronModelHold.Session.Free(
                        RevitOperations.DocumentKey(closing), DateTime.UtcNow, RevitOperations.Quiet());
                    if (holder != null)
                        Log("Model hold: '" + closing.Title + "' closed, so chat "
                            + HeronConversation.Short(holder) + " no longer holds it.");
                }
            }
            catch (Exception ex)
            {
                Log("Could not free a closing model's hold: " + ex.Message);
            }

            try
            {
                var dispatcher = Dispatcher;
                if (dispatcher == null) return;

                var document = e == null ? null : e.Document;
                if (document != null) dispatcher.ForgetActiveModel(document.Title);
            }
            catch (Exception ex)
            {
                Log("Could not clear the model name: " + ex.Message);
            }
        }

        internal static void Log(string message)
        {
            try
            {
                lock (LogLock)
                {
                    HeronAppend.Line(
                        CurrentLogPath(),
                        // UtcNow, not Now: the "u" format stamps a trailing Z, so local time
                        // here would label every line UTC while being hours out locally. UTC
                        // also matches startedAt in the discovery file, so the two line up.
                        string.Format("{0:u}  {1}", DateTime.UtcNow, message));
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
