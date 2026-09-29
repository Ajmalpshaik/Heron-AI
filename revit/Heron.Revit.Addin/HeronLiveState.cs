// Heron-Agent:  HERON-REVIT-CTX-007
// Heron-Step:   6
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Heron.Bridge;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// WHAT REVIT IS SHOWING, WRITTEN WHERE THE COMPANION PAGE CAN READ IT
    /// WITHOUT ASKING REVIT ANYTHING (D-108, docs/40 section 6).
    ///
    /// One file per Revit process, %LOCALAPPDATA%\Heron\live\pid-N.json:
    /// the model in front, its active view, and how many elements are
    /// selected, by category. The page reads the file; nothing polls Revit
    /// through the pipe for it. That is the whole design choice - a page
    /// asking through the pipe every second would raise the READING banner
    /// every second, queue work on Revit's own thread, and displace the
    /// chat's pipe (docs/40 section 4.2).
    ///
    /// WRITTEN ONLY WHILE THE BRIDGE IS CONNECTED. A Revit that was never
    /// connected is invisible to Heron (docs/00e), and this keeps that true:
    /// the file appears when the Heron button connects and is deleted when it
    /// disconnects or Revit closes. There is no separate switch because the
    /// button already is one.
    ///
    /// THE SELECTION IS WATCHED ON IDLING, ON EVERY RELEASE, at most every
    /// 200 ms, and the file is written only when something differs. D-108
    /// amends D-09 for exactly that and nothing else.
    ///
    /// NOT SelectionChanged, although it is cheaper, and that was measured:
    /// SelectionChangedEventArgs is in the 2023 to 2027 reference assemblies
    /// and absent from 2020, 2021 and 2022. Using it needs a version branch,
    /// which is two code paths to keep true forever, and
    /// tools/check-api-surface.py reads the one 2024 build against every
    /// release and reports the event missing on the three older ones. The
    /// house rule is a member every release has over a branch that hides one
    /// that does not (revit-version-support skill), and Idling is on all eight.
    ///
    /// NO TRANSACTION, NO MODEL CHANGE, NO NETWORK. Reading a selection and
    /// the active view needs no transaction. Every entry point swallows and
    /// logs: a status file that cannot be written must never cost Revit
    /// anything, the rule the activity banner already follows (D-50).
    /// </summary>
    internal static class HeronLiveState
    {
        /// <summary>The format the page reads. Raised only when a field changes meaning.</summary>
        private const int Format = 1;

        /// <summary>Elements whose category is counted, then "5000+". Golden Rule 8.</summary>
        private const int TallyCap = 5000;

        /// <summary>Element ids written into the file, then idsTruncated.</summary>
        private const int IdCap = 200;

        /// <summary>How often the selection is looked at on Idling.</summary>
        private static readonly TimeSpan IdleInterval = TimeSpan.FromMilliseconds(200);
        private static readonly Stopwatch SinceIdleCheck = Stopwatch.StartNew();

        /// <summary>
        /// How often the Companion switch is re-read from heron.config: another
        /// Revit, or the ribbon of this one, may have flipped the shared
        /// setting. One small file read every two seconds while Revit is idle.
        /// </summary>
        private static readonly TimeSpan SwitchInterval = TimeSpan.FromSeconds(2);
        private static readonly Stopwatch SinceSwitchRead = Stopwatch.StartNew();

        private static readonly object Gate = new object();

        private static string _revitVersion;
        private static int _pid;

        // Revit's application object for this session. Not a Document and not
        // an Element - those are what D-09 forbids holding - and it lives as
        // long as Revit does. Taken from the first event that hands it over,
        // so the Heron button can write the file the moment it connects.
        private static UIApplication _uiApp;

        // What was last written, so an unchanged selection writes nothing.
        private static string _lastWritten;

        // A cheap fingerprint of what Idling last looked at - the model, the
        // view and the selected ids, hashed, without opening one element. The
        // file's full text, which reads every selected element's category, is
        // built only when this moves. Before it, a 5,000-element selection
        // was read element by element five times a second while nothing
        // changed.
        private static string _lastSeen;

        /// <summary>The setting the Companion switch on the ribbon flips (D-109).</summary>
        internal const string EnabledKey = "companion.enabled";

        // Read once at startup and changed only by SetEnabled, so the Idling
        // check never opens the settings file.
        private static volatile bool _enabled = true;

        /// <summary>Whether the Companion is switched on.</summary>
        internal static bool Enabled { get { return _enabled; } }

        /// <summary>Revit's own thread, from OnStartup.</summary>
        internal static void Attach(UIControlledApplication application)
        {
            try
            {
                _revitVersion = application.ControlledApplication.VersionNumber;
                _pid = Process.GetCurrentProcess().Id;
                _enabled = HeronConfig.Load().GetBool(EnabledKey, true);

                application.Idling += OnIdling;
                // AFTER a model has closed, not while it closes: while
                // DocumentClosing runs, the closing model is still the one in
                // front. Without this the file would go on naming the last
                // model closed until the next Idling check.
                application.ControlledApplication.DocumentClosed += OnDocumentClosed;

                // A file left by a Revit that crashed with this same process
                // id is not this session's.
                Delete();
            }
            catch (Exception ex)
            {
                HeronApplication.Log("Live state could not start: " + ex.Message);
            }
        }

        /// <summary>Revit's own thread, from OnShutdown.</summary>
        internal static void Detach(UIControlledApplication application)
        {
            try
            {
                application.Idling -= OnIdling;
                application.ControlledApplication.DocumentClosed -= OnDocumentClosed;
            }
            catch (Exception ex)
            {
                HeronApplication.Log("Live state could not stop cleanly: " + ex.Message);
            }
            Delete();
        }

        /// <summary>
        /// The bridge connected or disconnected. Called beside every
        /// SetBridgeIcon, which is every moment that happens.
        /// </summary>
        internal static void BridgeChanged(bool connected)
        {
            if (connected) Write(_uiApp);
            else Delete();
        }

        /// <summary>
        /// The Companion switch on the ribbon (D-109). Saved to heron.config so
        /// the chat's page follows it too; off deletes this Revit's live file at
        /// once. The bridge, and everything a chat asks of Heron, are untouched.
        /// </summary>
        internal static void SetEnabled(bool on)
        {
            var config = HeronConfig.Load();
            config.Set(EnabledKey, on ? "true" : "false");
            config.Save();
            _enabled = on;
            if (on) Write(_uiApp);
            else Delete();
        }

        /// <summary>The view or model in front changed. Revit's own thread.</summary>
        internal static void ViewActivated(object sender)
        {
            Remember(sender);
            Write(_uiApp);
        }

        private static void OnDocumentClosed(object sender,
            Autodesk.Revit.DB.Events.DocumentClosedEventArgs e)
        {
            Write(_uiApp);
        }

        private static void OnIdling(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
        {
            // Cheapest test first: most Idling calls end on this line.
            if (SinceIdleCheck.Elapsed < IdleInterval) return;
            SinceIdleCheck.Restart();

            Remember(sender);

            if (SinceSwitchRead.Elapsed >= SwitchInterval)
            {
                SinceSwitchRead.Restart();
                try
                {
                    var now = HeronConfig.Load().GetBool(EnabledKey, true);
                    if (now != _enabled)
                    {
                        _enabled = now;
                        if (!now) Delete();
                        HeronApplication.SetCompanionIcon(now);
                    }
                }
                catch (Exception ex)
                {
                    // Idling is Revit's own thread: nothing may escape it.
                    HeronApplication.Log("Companion switch could not be re-read: " + ex.Message);
                }
            }

            if (!Connected() || !_enabled) return;

            // Nothing moved since the last look: stop before reading a single
            // element. Any fault here falls through to Write, which logs.
            try
            {
                var seen = Fingerprint(_uiApp);
                if (seen == _lastSeen) return;
                _lastSeen = seen;
            }
            catch (Exception) { }
            Write(_uiApp);
        }

        private static string Fingerprint(UIApplication app)
        {
            var uiDoc = app == null ? null : app.ActiveUIDocument;
            var doc = uiDoc == null ? null : uiDoc.Document;
            if (doc == null) return "(none)";

            var ids = uiDoc.Selection.GetElementIds();
            var hash = 17;
            unchecked
            {
                foreach (var id in ids) hash = hash * 31 + id.GetHashCode();
            }
            var view = doc.ActiveView;
            // THE NAME TOO: a view renamed while it stays active raises no
            // ViewActivated, and the id alone would never see it (Codex review).
            return doc.Title + "|" + doc.PathName + "|" + (view == null ? "" : view.Id + "|" + view.Name)
                 + "|" + ids.Count.ToString(CultureInfo.InvariantCulture)
                 + "|" + hash.ToString(CultureInfo.InvariantCulture);
        }

        private static void Remember(object sender)
        {
            var app = sender as UIApplication;
            if (app != null) _uiApp = app;
        }

        private static bool Connected()
        {
            var bridge = HeronApplication.Bridge;
            return bridge != null && bridge.IsRunning;
        }

        private static string FilePath()
        {
            return Path.Combine(HeronPaths.Live,
                "pid-" + _pid.ToString(CultureInfo.InvariantCulture) + ".json");
        }

        /// <summary>Build the file's text and write it if it changed. Never throws.</summary>
        private static void Write(UIApplication app)
        {
            try
            {
                if (!Connected() || !_enabled) return;

                var text = Describe(app);
                lock (Gate)
                {
                    if (text == _lastWritten) return;
                    HeronAtomicWrite.WriteAllText(FilePath(), text);
                    _lastWritten = text;
                }
            }
            catch (Exception ex)
            {
                HeronApplication.Log("Live state could not be written: " + ex.Message);
            }
        }

        private static void Delete()
        {
            try
            {
                lock (Gate)
                {
                    _lastWritten = null;
                    _lastSeen = null;
                    var path = FilePath();
                    if (File.Exists(path)) File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                HeronApplication.Log("Live state could not be removed: " + ex.Message);
            }
        }

        /// <summary>
        /// The file's text. No time stamp inside it on purpose: two writes of
        /// the same state must produce the same text, or every Idling check
        /// on the older releases would rewrite the file. The page reads the
        /// file's own modified time for "updated N seconds ago".
        /// </summary>
        private static string Describe(UIApplication app)
        {
            var uiDoc = app == null ? null : app.ActiveUIDocument;
            var doc = uiDoc == null ? null : uiDoc.Document;

            string document = null, path = null, viewName = null, viewType = null, viewId = null;
            var count = 0;
            var counted = 0;
            var capped = false;
            var tally = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var ids = new List<string>();

            if (doc != null)
            {
                document = doc.Title;
                path = string.IsNullOrEmpty(doc.PathName) ? null : doc.PathName;

                var view = doc.ActiveView;
                if (view != null)
                {
                    viewName = view.Name;
                    viewType = view.ViewType.ToString();
                    viewId = view.Id.ToString();
                }

                var selected = uiDoc.Selection.GetElementIds();
                count = selected.Count;
                foreach (var id in selected)
                {
                    if (ids.Count < IdCap) ids.Add(id.ToString());

                    if (counted >= TallyCap) { capped = true; continue; }
                    counted++;
                    var element = doc.GetElement(id);
                    var name = element == null || element.Category == null
                        ? "(no category)"
                        : element.Category.Name;
                    int seen;
                    tally[name] = tally.TryGetValue(name, out seen) ? seen + 1 : 1;
                }
            }

            var categories = new List<string>();
            foreach (var kv in tally) categories.Add(Json.Num(kv.Key, kv.Value));

            return Json.Obj(
                Json.Num("format", Format),
                Json.Num("pid", _pid),
                Json.Str("revitVersion", _revitVersion),
                doc == null
                    ? "\"document\": null"
                    : "\"document\": " + Json.Obj(Json.Str("title", document), Json.Str("path", path)),
                viewName == null
                    ? "\"view\": null"
                    : "\"view\": " + Json.Obj(Json.Str("name", viewName), Json.Str("type", viewType),
                                              "\"id\": " + viewId),
                "\"selection\": " + Json.Obj(
                    Json.Num("count", count),
                    "\"categories\": " + Json.Obj(categories.ToArray()),
                    Json.Arr("ids", ids),
                    Json.Bool("idsTruncated", count > ids.Count),
                    Json.Bool("countCapped", capped)));
        }
    }
}
