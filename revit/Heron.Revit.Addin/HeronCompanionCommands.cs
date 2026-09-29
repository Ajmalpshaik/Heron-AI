// Heron-Agent:  HERON-REVIT-UI-022
// Heron-Step:   6
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Heron.Bridge;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// THE COMPANION BUTTON (D-109): open the chat's Companion page from Revit.
    ///
    /// The page's server lives in the chat's MCP process, never in Revit
    /// (D-108) - so this button does not serve anything. Each running chat
    /// whose Companion is up leaves a small file in HeronPaths.Companion naming
    /// its port and a one-time code; this reads it and asks Windows to open the
    /// default browser at that address. Revit opens no socket: handing an
    /// address to the shell is what a hyperlink in a dialog does.
    ///
    /// Which chat: the one bound to THIS Revit if there is one, otherwise the
    /// newest. The page itself still shows only the Revit its chat is bound to
    /// (Article 12a), so opening the "wrong" chat's page cannot show a guess.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class CompanionOpenCommand : IExternalCommand
    {
        private static readonly Regex Code = new Regex("^[A-Za-z0-9_-]{16,64}$");

        /// <summary>
        /// A running chat touches its note every two seconds. One older than
        /// this was left by a chat that crashed, whose process id Windows may
        /// already have given to something else (Codex review of #362).
        /// </summary>
        private const int NoteSeconds = 30;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (!HeronLiveState.Enabled)
                {
                    TaskDialog.Show("Heron Companion",
                        "The Companion is switched off, so there is no page to open. Heron itself is " +
                        "working as normal.\n\nTo turn it on, click the arrow under the Companion " +
                        "button and choose Turn Companion on.");
                    return Result.Succeeded;
                }

                var address = Pick(Process.GetCurrentProcess().Id);
                if (address == null)
                {
                    TaskDialog.Show("Heron Companion",
                        "No Claude chat with Heron is running on this PC, so there is no Companion " +
                        "page to open. Nothing was changed.\n\nOpen your Claude chat, then press " +
                        "this button again.");
                    return Result.Succeeded;
                }

                Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
                HeronApplication.Log("Companion opened from the ribbon.");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                HeronApplication.Log("Companion could not be opened: " + ex);
                message = "Heron could not open the Companion page. Your model is untouched. " +
                          "Windows said: " + ex.Message;
                return Result.Failed;
            }
        }

        /// <summary>The address to open, or null when no running chat offers one.</summary>
        private static string Pick(int revitPid)
        {
            string best = null, newest = null;
            var newestTime = DateTime.MinValue;
            var mine = revitPid.ToString(CultureInfo.InvariantCulture);
            var oldest = DateTime.UtcNow.AddSeconds(-NoteSeconds);

            foreach (var path in Directory.GetFiles(HeronPaths.Companion, "chat-*.json"))
            {
                if (File.GetLastWriteTimeUtc(path) < oldest) continue;

                string text;
                try { text = File.ReadAllText(path); }
                catch (IOException) { continue; }
                catch (UnauthorizedAccessException) { continue; }

                int port, owner;
                var code = Json.ReadString(text, "code");
                if (!int.TryParse(Json.ReadString(text, "port"), NumberStyles.Integer,
                                  CultureInfo.InvariantCulture, out port)
                    || port < 1024 || port > 65535
                    || code == null || !Code.IsMatch(code)
                    || !int.TryParse(Json.ReadString(text, "mcpPid"), NumberStyles.Integer,
                                     CultureInfo.InvariantCulture, out owner)
                    || !Alive(owner))
                {
                    continue;
                }

                var address = "http://127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture)
                            + "/?pair=" + code;
                if (Json.ReadString(text, "revitPid") == mine) best = address;

                // NEWEST BY WHEN THE CHAT STARTED, which the note carries and
                // never changes. The file's own time is a heartbeat every
                // chat refreshes each two seconds, so it names whichever
                // beat last (Codex's third review of #362).
                long started;
                var born = long.TryParse(Json.ReadString(text, "started"), NumberStyles.Integer,
                                         CultureInfo.InvariantCulture, out started)
                    ? DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc).AddSeconds(started)
                    : File.GetLastWriteTimeUtc(path);
                if (born > newestTime) { newestTime = born; newest = address; }
            }
            return best ?? newest;
        }

        /// <summary>A chat that crashed leaves its file behind; its process is gone.</summary>
        private static bool Alive(int pid)
        {
            try
            {
                using (var process = Process.GetProcessById(pid)) return !process.HasExited;
            }
            catch (ArgumentException) { return false; }
            catch (InvalidOperationException) { return false; }
        }
    }

    /// <summary>
    /// THE COMPANION SWITCH (D-109), under the Companion button's arrow. Off
    /// stops this Revit's live file and, within two seconds, every chat's
    /// page; Heron itself - the bridge, the chat, Changes - is untouched. No
    /// confirmation either way: the Companion only shows.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class CompanionToggleCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // THE SHARED SETTING, READ FRESH - not this Revit's cached
                // copy. With two Revits open, the other may have switched it
                // since this one started (Codex review of #362).
                var on = !HeronConfig.Load().GetBool(HeronLiveState.EnabledKey, true);
                HeronLiveState.SetEnabled(on);
                HeronApplication.SetCompanionIcon(on);
                HeronApplication.Log("Companion switched " + (on ? "on" : "off") + " from the ribbon.");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                HeronApplication.Log("Companion switch failed: " + ex);
                message = "Heron could not switch the Companion, so nothing changed. Your model is " +
                          "untouched. Windows said: " + ex.Message;
                return Result.Failed;
            }
        }
    }
}
