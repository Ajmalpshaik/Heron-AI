// Heron-Agent:  HERON-INS-ORC-001
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.IO;
using System.Text.Json.Nodes;

namespace Heron.Installer
{
    /// <summary>
    /// A developer's PC - where an INSTALLED Heron steps aside for the developer's
    /// own checkout. D-167, amended 2026-11-16 on the owner's word: *"for me only
    /// it will from the project"*, then *"Yes, build it"*.
    ///
    /// THE NOTE IS THE PYTHON SIDE'S. Every chat started from a git checkout
    /// leaves `developer-checkout.json` in Heron's data home naming that PC's
    /// main checkout (brain/heron_scope.py, record_developer_checkout). A
    /// modeller's PC never has a checkout, so never has the note, and nothing
    /// here changes anything for them. This reads the same note by the same
    /// rule - steps_aside_for() - so the window and heron-install say what the
    /// Python side will do.
    ///
    /// WHAT STEPPING ASIDE MEANS HERE: Heron is not added to Claude Code from the
    /// installed folder (ClaudeCodeEntry.Apply refuses), the window does not
    /// offer that tick, and it says before Install that the installed add-in
    /// takes the developer's place in Revit until they deploy again - Revit
    /// loads one Heron per year, whichever went in last.
    /// </summary>
    public static class DeveloperCopy
    {
        /// <summary>The note's name in Heron's data home - heron_scope.DEVELOPER_NOTE.</summary>
        public const string NoteName = "developer-checkout.json";

        /// <summary>
        /// Heron's data home: HERON_DATA when set, otherwise %USERPROFILE%\.heron -
        /// heron_paths.data_home()'s rule, read the same way. Null with neither.
        /// </summary>
        public static string Home()
        {
            var chosen = (Environment.GetEnvironmentVariable("HERON_DATA") ?? "").Trim();
            if (chosen.Length > 0) return chosen;
            var profile = (Environment.GetEnvironmentVariable("USERPROFILE") ?? "").Trim();
            if (profile.Length == 0 || !Path.IsPathRooted(profile)) return null;
            return Path.Combine(profile, ".heron");
        }

        /// <summary>The developer checkout the Heron at `heronFolder` steps aside for, or null.</summary>
        public static string StepsAsideFor(string heronFolder)
        {
            return StepsAsideFor(heronFolder, Home());
        }

        /// <summary>
        /// The developer checkout an INSTALLED Heron at `heronFolder` steps aside
        /// for, read from the note in `home` - or null. Only a Heron with no .git
        /// at all steps aside (a checkout or a worktree is the developer's own
        /// work), only for a folder that is still a Heron git checkout, and never
        /// for itself.
        /// </summary>
        public static string StepsAsideFor(string heronFolder, string home)
        {
            if (string.IsNullOrEmpty(heronFolder) || string.IsNullOrEmpty(home)) return null;
            var dotGit = Path.Combine(heronFolder, ".git");
            if (File.Exists(dotGit) || Directory.Exists(dotGit)) return null;

            string text;
            try
            {
                var note = Path.Combine(home, NoteName);
                if (!File.Exists(note)) return null;
                text = File.ReadAllText(note);
            }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }

            string checkout;
            try
            {
                checkout = JsonNode.Parse(text)?["checkout"]?.GetValue<string>();
            }
            catch (Exception e) when (e is System.Text.Json.JsonException || e is InvalidOperationException
                                      || e is FormatException)
            {
                return null;
            }

            if (string.IsNullOrEmpty(checkout) || !IsCheckout(checkout)) return null;
            if (SameFolder(checkout, heronFolder)) return null;
            return checkout;
        }

        /// <summary>The line the window shows above Install on a developer's PC.</summary>
        public static string Warning(string checkout)
        {
            return "This PC has a Heron developer copy at " + checkout + ". Its chats keep using that " +
                   "folder: the search index follows it, and Heron is not added to Claude Code from here. " +
                   "Installing does put this copy's add-in into Revit in place of the one deployed from " +
                   "there, until you deploy from that folder again.";
        }

        /// <summary>What adding Heron to Claude Code says on a developer's PC.</summary>
        public static string NotAddedToClaude(string checkout)
        {
            return "Heron was NOT added to Claude Code: this PC has a Heron developer copy at " + checkout +
                   ", and a chat opened there starts that copy's own Heron. Nothing was changed.";
        }

        private static bool IsCheckout(string folder)
        {
            return Directory.Exists(Path.Combine(folder, ".git"))
                   && Directory.Exists(Path.Combine(folder, "brain", "fragments"));
        }

        private static bool SameFolder(string a, string b)
        {
            try
            {
                return string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'),
                                     Path.GetFullPath(b).TrimEnd('\\', '/'),
                                     StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception e) when (e is ArgumentException || e is NotSupportedException
                                      || e is PathTooLongException)
            {
                return false;
            }
        }
    }
}
