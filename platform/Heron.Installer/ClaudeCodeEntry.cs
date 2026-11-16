// Heron-Agent:  HERON-INS-MCP-004
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Heron.Installer
{
    /// <summary>What adding Heron to Claude Code came to, and the sentence to show.</summary>
    public sealed class ClaudeStep
    {
        /// <summary>True when Heron's entry is in Claude's settings and read back.</summary>
        public bool Added { get; internal set; }

        /// <summary>True when nothing was tried because a Claude was running.</summary>
        public bool ClaudeWasOpen { get; internal set; }

        public string Said { get; internal set; }
    }

    /// <summary>
    /// Heron's one entry in Claude Code's own settings - D-167, item 5.
    ///
    /// WHY IT EXISTS. Until 2026-11-16 a modeller reached Heron by opening the
    /// Heron folder in Claude Code, whose `.mcp.json` starts it. That ties every
    /// chat to Heron's folder, when the folder a modeller opens should be their
    /// PROJECT (docs/07 section 1a, correction 2). A user-scope server is
    /// available in every folder.
    ///
    /// WHERE IT GOES, READ FROM CLAUDE CODE'S DOCUMENTATION ON 2026-11-16
    /// (code.claude.com/docs/en/mcp): *"User-scoped servers are stored in
    /// ~/.claude.json"*, and when one name is in several scopes the local and
    /// project ones win. So a Heron checkout opened in Claude Code still uses
    /// its own `.mcp.json`, and this entry is the fallback everywhere else.
    ///
    /// THE FILE IS CLAUDE'S, NOT HERON'S, and every rule below follows from that:
    ///
    ///   - one key is touched, `mcpServers.heron`, and nothing else;
    ///   - a `heron` entry that points somewhere ELSE is somebody's own and is
    ///     left alone, with a sentence saying so;
    ///   - a file that does not read as JSON is never rewritten;
    ///   - it is copied aside before it is written, and read back after;
    ///   - it is written only while no Claude is running, because Claude Code
    ///     rewrites this file itself while it runs and the later write wins.
    ///
    /// PURE HALF AND WINDOWS HALF. Add and Remove take the file's text and give
    /// text back, so they are tested with no Claude and no disk. Apply and
    /// Withdraw do the file, the backup and the running check.
    /// </summary>
    public static class ClaudeCodeEntry
    {
        /// <summary>The server's name in Claude Code - what /mcp lists.</summary>
        public const string ServerName = "heron";

        /// <summary>
        /// The server script, relative to the Heron folder - the same file the
        /// folder's own `.mcp.json` starts.
        /// </summary>
        public static readonly string ServerScript = Path.Combine("mcp", "server", "heron_mcp_server.py");

        /// <summary>
        /// The words beside the window's tick that asks for this - short, as the
        /// owner asked of that window on 2026-11-07, with the whole of it in
        /// WhatTheTickDoes on hover. HERE, not in the window, so the sentence is
        /// in the library its tests run against (D-167, appended 2026-11-16).
        /// </summary>
        public const string TickWords =
            "Add Heron to Claude Code, so a chat in any folder can use it. Close Claude first.";

        /// <summary>The whole of what that tick does, shown on hover.</summary>
        public const string WhatTheTickDoes =
            "Adds Heron's one entry to Claude Code's settings for your Windows user (~/.claude.json), " +
            "so a chat opened in ANY folder can use Heron. Claude must be closed while it is written, " +
            "because Claude rewrites that file itself while it runs - if it is open, Heron says so and " +
            "changes nothing. Each open chat then runs Heron in the background. Removing Heron AI in " +
            "Settings > Apps > Installed apps takes the entry out again.";

        /// <summary>
        /// Add or replace Heron's entry. Null, with `why` set, when it must not.
        ///
        /// `heronFolder` is the installed Heron folder, absolute - a user-scope
        /// server starts in whatever folder the modeller opened, so a relative
        /// path would point nowhere.
        /// </summary>
        public static string Add(string json, string heronFolder, string clientId, out string why)
        {
            why = null;
            JsonObject root;
            if (!TryRead(json, out root, out why)) return null;

            var servers = root["mcpServers"] as JsonObject;
            if (root["mcpServers"] != null && servers == null)
            {
                why = "Claude Code's settings hold 'mcpServers' as something other than a list of " +
                      "servers, so Heron left the file alone. Add Heron by hand with: claude mcp add " +
                      "--scope user " + ServerName + " -- python \"" + ScriptIn(heronFolder) + "\"";
                return null;
            }
            if (servers == null)
            {
                servers = new JsonObject();
                root["mcpServers"] = servers;
            }

            var existing = servers[ServerName] as JsonObject;
            if (existing != null && !PointsInto(existing, heronFolder))
            {
                why = "Claude Code already has a server called '" + ServerName + "' that starts " +
                      Describe(existing) + " - not this Heron folder - so it was left as it is. " +
                      "Remove it with 'claude mcp remove " + ServerName + " -s user' and run this " +
                      "again if this Heron is the one you want.";
                return null;
            }

            var entry = new JsonObject
            {
                ["type"] = "stdio",
                ["command"] = "python",
                ["args"] = new JsonArray(ScriptIn(heronFolder)),
            };
            if (!string.IsNullOrEmpty(clientId))
                entry["env"] = new JsonObject { ["HERON_CLIENT_ID"] = clientId };

            servers[ServerName] = entry;
            return Write(root);
        }

        /// <summary>
        /// Take Heron's entry out - only when it points into `heronFolder`.
        ///
        /// `removed` says whether anything came out. An entry pointing at another
        /// folder is somebody else's Heron and stays; so does everything that is
        /// not Heron's. Null, with `why` set, when the file cannot be read.
        /// </summary>
        public static string Remove(string json, string heronFolder, out bool removed, out string why)
        {
            removed = false;
            why = null;
            JsonObject root;
            if (!TryRead(json, out root, out why)) return null;

            var servers = root["mcpServers"] as JsonObject;
            var existing = servers == null ? null : servers[ServerName] as JsonObject;
            if (existing == null || !PointsInto(existing, heronFolder)) return json;

            servers.Remove(ServerName);
            removed = true;
            return Write(root);
        }

        /// <summary>
        /// Whether an entry starts a script inside `heronFolder` - compared as
        /// full paths, ignoring case, as Windows does.
        /// </summary>
        public static bool PointsInto(JsonObject entry, string heronFolder)
        {
            if (entry == null || string.IsNullOrEmpty(heronFolder)) return false;
            var folder = Normal(heronFolder);
            var args = entry["args"] as JsonArray;
            if (args == null) return false;
            foreach (var arg in args)
            {
                string text;
                if (arg == null || !TryText(arg, out text)) continue;
                if (!Path.IsPathRooted(text)) continue;
                var full = Normal(text);
                if (full.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// The HERON_CLIENT_ID the Heron folder's own `.mcp.json` carries, or
        /// null. One person, one id (D-22): a chat in the Heron folder and a chat
        /// anywhere else are the same person and must not race for one lease.
        /// </summary>
        public static string ClientIdIn(string mcpJson)
        {
            JsonObject root;
            string ignored;
            if (!TryRead(mcpJson, out root, out ignored)) return null;
            var heron = (root["mcpServers"] as JsonObject)?[ServerName] as JsonObject;
            var env = heron?["env"] as JsonObject;
            string id;
            return env != null && env["HERON_CLIENT_ID"] != null && TryText(env["HERON_CLIENT_ID"], out id)
                ? id : null;
        }

        // -----------------------------------------------------------------
        // The Windows half: the file, the backup, the running check.
        // -----------------------------------------------------------------

        /// <summary>
        /// Where Claude Code keeps its user settings: `CLAUDE_CONFIG_DIR` when
        /// set, else `~/.claude.json` - the same rule tools/heron-ready.py reads.
        ///
        /// CLAUDE'S PATH, NOT HERON'S, so it is not HeronPaths' to build - and
        /// this engine does not reference Heron.Core (its .csproj says why). It
        /// is read from the environment, as WindowsClaudeAppLook reads APPDATA.
        /// The user's folder is outside the Store Claude's private copy of
        /// AppData, so the chat and the modeller's own Explorer see one file.
        /// </summary>
        public static string SettingsPath()
        {
            var configured = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");
            return string.IsNullOrEmpty(configured)
                ? Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? "", ".claude.json")
                : Path.Combine(configured, ".claude.json");
        }

        /// <summary>
        /// Every running Claude - the desktop app and the command line are both
        /// `claude.exe` to Windows, whatever the case of the name.
        /// </summary>
        public static int RunningClaudes()
        {
            var found = Process.GetProcessesByName("claude");
            var n = found.Length;
            foreach (var p in found) p.Dispose();
            return n;
        }

        /// <summary>Add Heron to Claude Code, and say what came of it.</summary>
        public static ClaudeStep Apply(string heronFolder)
        {
            if (RunningClaudes() > 0)
            {
                return new ClaudeStep
                {
                    ClaudeWasOpen = true,
                    Said = "Heron was NOT added to Claude Code: Claude is open, and it rewrites its own " +
                           "settings while it runs. Close every Claude window, then run Heron's Setup " +
                           "again (or: heron-install --add-to-claude).",
                };
            }

            var path = SettingsPath();
            var before = File.Exists(path) ? File.ReadAllText(path) : "{}";
            var clientId = ClientIdIn(ReadIfThere(Path.Combine(heronFolder, ".mcp.json")));

            string why;
            var after = Add(before, heronFolder, clientId, out why);
            if (after == null) return new ClaudeStep { Said = "Heron was NOT added to Claude Code. " + why };

            var backup = File.Exists(path) ? Backup(path) : null;
            Replace(path, after);

            // READ BACK, NOT ASSUMED.
            JsonObject root;
            string unread;
            var back = TryRead(File.ReadAllText(path), out root, out unread)
                ? (root["mcpServers"] as JsonObject)?[ServerName] as JsonObject
                : null;
            if (back == null || !PointsInto(back, heronFolder))
            {
                return new ClaudeStep
                {
                    Said = "Heron's entry did not read back from " + path + ", so it may not be in " +
                           "Claude Code." + (backup == null ? "" : " The file as it was is kept at " + backup + "."),
                };
            }

            return new ClaudeStep
            {
                Added = true,
                Said = "Heron was added to Claude Code for your user: '" + ServerName + "' starts " +
                       ScriptIn(heronFolder) + ". In Claude, /mcp should list it in any folder." +
                       (backup == null ? "" : " Claude's settings as they were are kept at " + backup + "."),
            };
        }

        /// <summary>Take Heron out of Claude Code, if it points here. The sentence to show.</summary>
        public static string Withdraw(string heronFolder)
        {
            var path = SettingsPath();
            if (!File.Exists(path)) return null;

            bool removed;
            string why;
            var before = File.ReadAllText(path);
            var after = Remove(before, heronFolder, out removed, out why);
            if (after == null) return "Heron's entry in Claude Code was left: " + why;
            if (!removed) return null;

            if (RunningClaudes() > 0)
            {
                return "Heron's entry in Claude Code was left, because Claude is open. Close it and " +
                       "remove the entry with: claude mcp remove " + ServerName + " -s user";
            }

            var backup = Backup(path);
            Replace(path, after);
            return "Heron was taken out of Claude Code. Claude's settings as they were are kept at " +
                   backup + ".";
        }

        // -----------------------------------------------------------------

        private static string ScriptIn(string heronFolder)
        {
            return Path.Combine(Path.GetFullPath(heronFolder), ServerScript);
        }

        private static string Normal(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static string Describe(JsonObject entry)
        {
            var parts = new List<string>();
            string text;
            if (entry["command"] != null && TryText(entry["command"], out text)) parts.Add(text);
            var args = entry["args"] as JsonArray;
            if (args != null)
                foreach (var a in args)
                    if (a != null && TryText(a, out text)) parts.Add(text);
            return parts.Count == 0 ? "something Heron cannot read" : "'" + string.Join(" ", parts) + "'";
        }

        private static bool TryText(JsonNode node, out string text)
        {
            text = null;
            var value = node as JsonValue;
            return value != null && value.TryGetValue(out text);
        }

        /// <summary>
        /// The file as one JSON object, or false with a sentence. NEVER a guess:
        /// a file that does not parse is left exactly as it is.
        /// </summary>
        private static bool TryRead(string json, out JsonObject root, out string why)
        {
            root = null;
            why = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                root = new JsonObject();
                return true;
            }
            try
            {
                root = JsonNode.Parse(json) as JsonObject;
            }
            catch (JsonException e)
            {
                why = "Claude Code's settings file does not read as JSON (" + e.Message + "), so Heron " +
                      "left it exactly as it is.";
                return false;
            }
            if (root == null)
            {
                why = "Claude Code's settings file is not a JSON object, so Heron left it exactly as it is.";
                return false;
            }
            return true;
        }

        private static string Write(JsonObject root)
        {
            return root.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            });
        }

        private static string ReadIfThere(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        private static string Backup(string path)
        {
            var copy = path + ".heron-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Copy(path, copy, false);
            return copy;
        }

        /// <summary>
        /// Written beside, then moved over - so an interrupted write leaves the
        /// old file, never half of a new one.
        /// </summary>
        private static void Replace(string path, string text)
        {
            var temp = path + ".heron-writing";
            File.WriteAllText(temp, text);
            File.Move(temp, path, true);
        }
    }
}
