// Heron-Agent:  none
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.IO;
using System.Text.Json.Nodes;
using Heron.Installer;

namespace Heron.Installer.TestHost
{
    /// <summary>
    /// What the Setup of D-167 added to the engine and the command line, against
    /// text and a temporary folder - no Claude, no Revit.
    ///
    /// WHAT THIS PROVES: Heron's entry in Claude Code's settings is added with
    /// nothing else in the file touched; an entry pointing somewhere else is left
    /// alone; a file that is not JSON is never rewritten; removing takes out only
    /// an entry that points into this Heron folder; and --uninstall and
    /// --add-to-claude refuse every flag that would choose or install.
    ///
    /// WHAT IT CANNOT PROVE: that Claude Code reads the entry and /mcp lists
    /// Heron, or that the Setup's uninstaller shows the sentence. Those are
    /// NEEDS-CHECKING Group OA, on the owner's PC.
    /// </summary>
    internal static class SetupChecks
    {
        internal static void Run(Action<bool, string> check, Func<string, string[], bool> names)
        {
            var heron = Path.Combine(Path.GetTempPath(), "Heron AI " + Guid.NewGuid().ToString("N"));
            var other = Path.Combine(Path.GetTempPath(), "some-other-heron");
            var script = Path.Combine(heron, "mcp", "server", "heron_mcp_server.py");

            Console.WriteLine();
            Console.WriteLine("D-167 - HERON IN CLAUDE CODE'S SETTINGS, ONE KEY AND NOTHING ELSE");

            const string Theirs = "{\n  \"numStartups\": 41,\n  \"projects\": { \"D:/work\": { \"allowedTools\": [] } },\n" +
                                  "  \"mcpServers\": { \"github\": { \"type\": \"http\", \"url\": \"https://example.test/mcp\" } }\n}";
            string why;
            var added = ClaudeCodeEntry.Add(Theirs, heron, "someone-pc", out why);
            check(added != null && why == null, "an ordinary settings file takes Heron's entry");
            var root = JsonNode.Parse(added).AsObject();
            check((int)root["numStartups"] == 41 && root["projects"]?["D:/work"] != null,
                  "  and every other key is still there, unchanged");
            check(root["mcpServers"]?["github"]?["url"]?.ToString() == "https://example.test/mcp",
                  "  and the server that was there already is untouched");
            var entry = root["mcpServers"]?["heron"]?.AsObject();
            check(entry != null && entry["command"]?.ToString() == "python",
                  "  and 'heron' is started with the plain command python, as .mcp.json does");
            check(entry != null && entry["args"]?[0]?.ToString() == script,
                  "  with the ABSOLUTE path to the server, since a chat starts in its own folder");
            check(entry != null && entry["env"]?["HERON_CLIENT_ID"]?.ToString() == "someone-pc",
                  "  and the client id it was given - one person, one id (D-22)");
            check(entry != null && ClaudeCodeEntry.PointsInto(entry, heron),
                  "  and it points into the Heron folder");
            check(entry != null && !ClaudeCodeEntry.PointsInto(entry, other),
                  "  and not into another one");

            var again = ClaudeCodeEntry.Add(added, heron, "someone-pc", out why);
            check(again != null, "adding it twice replaces Heron's own entry rather than refusing");

            var empty = ClaudeCodeEntry.Add("", heron, null, out why);
            check(empty != null && JsonNode.Parse(empty)?["mcpServers"]?["heron"] != null,
                  "a PC with no settings file yet gets one holding Heron alone");
            check(JsonNode.Parse(empty)?["mcpServers"]?["heron"]?["env"] == null,
                  "  and with no client id known, no env is invented");

            Console.WriteLine();
            Console.WriteLine("  somebody else's 'heron', and a damaged file, are left exactly as they are");
            var someoneElses = ClaudeCodeEntry.Add(added, other, null, out why);
            check(someoneElses == null, "a 'heron' entry pointing at ANOTHER folder is not replaced");
            check(names(why, new[] { "already", "claude mcp remove" }),
                  "  and the sentence says it was left, and how to remove it: " + why);
            var broken = ClaudeCodeEntry.Add("{ \"numStartups\": 4", heron, null, out why);
            check(broken == null && names(why, new[] { "left it exactly as it is" }),
                  "a file that is not JSON is never rewritten: " + why);
            var oddServers = ClaudeCodeEntry.Add("{ \"mcpServers\": [1, 2] }", heron, null, out why);
            check(oddServers == null, "'mcpServers' that is not a list of servers is left alone");

            Console.WriteLine();
            Console.WriteLine("  removing takes out only an entry that points into THIS Heron folder");
            bool removed;
            var gone = ClaudeCodeEntry.Remove(added, heron, out removed, out why);
            check(removed && gone != null && JsonNode.Parse(gone)?["mcpServers"]?["heron"] == null,
                  "this folder's entry comes out");
            check(JsonNode.Parse(gone)?["mcpServers"]?["github"] != null && (int)JsonNode.Parse(gone)["numStartups"] == 41,
                  "  and everything else stays");
            var kept = ClaudeCodeEntry.Remove(added, other, out removed, out why);
            check(!removed && kept == added, "another folder's uninstall leaves it, and the text unchanged");
            var none = ClaudeCodeEntry.Remove(Theirs, heron, out removed, out why);
            check(!removed && none == Theirs, "a file with no Heron entry is handed back unchanged");

            Console.WriteLine();
            Console.WriteLine("  the client id is read from the Heron folder's own .mcp.json");
            check(ClaudeCodeEntry.ClientIdIn("{ \"mcpServers\": { \"heron\": { \"env\": { \"HERON_CLIENT_ID\": \"a-pc\" } } } }") == "a-pc",
                  "the id .mcp.json carries is the one used");
            check(ClaudeCodeEntry.ClientIdIn(null) == null && ClaudeCodeEntry.ClientIdIn("not json") == null,
                  "  and no file, or a damaged one, gives none rather than a guess");

            // THE WORDS BESIDE THE WINDOW'S TICK - D-167, appended 2026-11-16.
            // Ticking is the asking, so the line itself names what a modeller
            // must do first, and the hover names what it writes and how it
            // comes out again.
            Console.WriteLine();
            Console.WriteLine("  the window's Claude tick says what it needs, and the hover what it does");
            check(names(ClaudeCodeEntry.TickWords, new[] { "Claude Code", "any folder", "Close Claude first" }),
                  "the tick's own line names Claude Code, any folder, and closing Claude first");
            check(ClaudeCodeEntry.TickWords.Length <= 90,
                  "  and it is short - one line in a 620-wide window, the owner's 'make it simple'");
            check(names(ClaudeCodeEntry.WhatTheTickDoes,
                        new[] { ".claude.json", "Claude must be closed", "changes nothing", "Installed apps" }),
                  "  and the hover names the file, the closed Claude, the no-change refusal and the way out");

            Console.WriteLine();
            Console.WriteLine("D-167 - --uninstall CHOOSES NOTHING, AND --add-to-claude INSTALLS NOTHING");
            var un = Heron.Installer.Cli.Arguments.Read(new[] { "--uninstall" });
            check(un.Problem == null && un.Uninstall, "--uninstall alone is read");
            var unList = Heron.Installer.Cli.Arguments.Read(new[] { "--uninstall", "--list" });
            check(unList.Problem == null && unList.Uninstall && unList.ListOnly,
                  "  and with --list, to say what would go and change nothing");
            foreach (var choosing in new[]
            {
                new[] { "--uninstall", "--products", "heron-doc" },
                new[] { "--uninstall", "--releases", "2024" },
                new[] { "--uninstall", "--from", "x" },
                new[] { "--uninstall", "--add-to-claude" },
            })
            {
                var r = Heron.Installer.Cli.Arguments.Read(choosing);
                check(r.Problem != null && names(r.Problem, new[] { "--uninstall" }),
                      "  refused, never half-honoured: " + string.Join(" ", choosing));
            }
            var claude = Heron.Installer.Cli.Arguments.Read(new[] { "--add-to-claude" });
            check(claude.Problem == null && claude.AddToClaude, "--add-to-claude alone is read");
            var claudeInstall = Heron.Installer.Cli.Arguments.Read(new[] { "--add-to-claude", "--from", "x" });
            check(claudeInstall.Problem != null, "  and beside an install it is refused");
            check(names(Heron.Installer.Cli.Arguments.Usage(), new[] { "--uninstall", "--add-to-claude" }),
                  "the help names both");
        }
    }
}
