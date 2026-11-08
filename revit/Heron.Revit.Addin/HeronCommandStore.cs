// Heron-Agent:  HERON-USR-SKL-003
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Heron.Bridge;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>One input a saved command asks for, as the chat saved it.</summary>
    internal sealed class SavedCommandInput
    {
        internal string Name { get; set; }
        internal string Label { get; set; }
        internal string Default { get; set; }
    }

    /// <summary>One saved command (docs/49), as the panel shows it.</summary>
    internal sealed class SavedCommand
    {
        internal string Name { get; set; }
        internal string Description { get; set; }
        internal bool Changes { get; set; }
        internal string Capability { get; set; }
        internal List<SavedCommandInput> Inputs { get; set; }
    }

    /// <summary>
    /// THE SAVED COMMANDS, READ FOR THE HERON PANEL - docs/49. The chat writes
    /// them (brain/heron_commands.py); this only reads. And it runs one by
    /// starting the Python runner the chat named in its runner note.
    ///
    /// IT TOUCHES NO REVIT API, and that is the design rather than a
    /// convenience: every check a change goes through - the fragment's own
    /// status, D-117's PROVEN rule, the switches, the pin - lives in the
    /// Python door, and the runner calls that door's own body. A second copy
    /// of those rules in C# would drift. The run reaches Revit the same way a
    /// chat's does: over the pipe, through the gate, behind Keep or Discard.
    /// </summary>
    internal static class HeronCommandStore
    {
        /// <summary>
        /// Every readable saved command, sorted by name, and the names of the
        /// files that could not be read - named, never hidden.
        /// </summary>
        internal static List<SavedCommand> Load(out List<string> unreadable)
        {
            unreadable = new List<string>();
            var found = new List<SavedCommand>();
            var folder = HeronPaths.CommandsPersonal;
            if (folder == null || !Directory.Exists(folder)) return found;

            foreach (var path in Directory.GetFiles(folder, "*.json"))
            {
                var command = Read(path);
                if (command == null) unreadable.Add(Path.GetFileName(path));
                else found.Add(command);
            }
            found.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return found;
        }

        /// <summary>One command file, or null when it is not one.</summary>
        internal static SavedCommand Read(string path)
        {
            string text;
            try { text = File.ReadAllText(path, Encoding.UTF8); }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
            return Parse(text);
        }

        /// <summary>A command's JSON, or null when it is not format 1.</summary>
        internal static SavedCommand Parse(string json)
        {
            // FORMAT 1 ONLY. A number is not a string to Json.ReadString, so
            // the marker is read as a whole number.
            if (Json.ReadLong(json, "heron-command") != 1) return null;
            var name = Json.ReadString(json, "name");
            var capability = Json.ReadString(json, "capability");
            var tool = Json.ReadString(json, "tool");
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(capability)) return null;
            if (tool != "change" && tool != "read") return null;

            var inputs = new List<SavedCommandInput>();
            var rows = Json.ReadObjectArray(json, "inputs");
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    string key, label, fallback;
                    row.TryGetValue("name", out key);
                    row.TryGetValue("label", out label);
                    row.TryGetValue("default", out fallback);
                    if (string.IsNullOrEmpty(key)) return null;
                    inputs.Add(new SavedCommandInput
                    {
                        Name = key,
                        Label = string.IsNullOrEmpty(label) ? key : label,
                        Default = fallback ?? "",
                    });
                }
            }

            return new SavedCommand
            {
                Name = name,
                Description = Json.ReadString(json, "description") ?? "",
                Changes = tool == "change",
                Capability = capability,
                Inputs = inputs,
            };
        }

        /// <summary>
        /// How to start the runner: the Python and runner script the chat
        /// named, or null with `why` saying what is missing.
        /// </summary>
        internal static ProcessStartInfo RunnerStart(string commandName, int revitPid,
                                                     IList<KeyValuePair<string, string>> inputs,
                                                     out string why)
        {
            why = null;
            var notePath = HeronPaths.CommandRunnerNote;
            string note = null;
            try
            {
                if (notePath != null && File.Exists(notePath))
                    note = File.ReadAllText(notePath, Encoding.UTF8);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            var python = note == null ? null : Json.ReadString(note, "python");
            var runner = note == null ? null : Json.ReadString(note, "runner");
            var root = note == null ? null : Json.ReadString(note, "root");
            if (string.IsNullOrEmpty(python) || string.IsNullOrEmpty(runner) ||
                !File.Exists(python) || !File.Exists(runner))
            {
                why = "Heron does not know where its Python is yet. Open a Claude chat with " +
                      "Heron once, then try again. Nothing was sent to Revit.";
                return null;
            }

            var args = new StringBuilder();
            Append(args, runner);
            Append(args, "--name");
            Append(args, commandName);
            Append(args, "--revit-pid");
            Append(args, revitPid.ToString(System.Globalization.CultureInfo.InvariantCulture));
            foreach (var input in inputs)
            {
                Append(args, "--set");
                Append(args, input.Key + "=" + input.Value);
            }

            var start = new ProcessStartInfo
            {
                FileName = python,
                Arguments = args.ToString(),
                WorkingDirectory = string.IsNullOrEmpty(root) ? Path.GetDirectoryName(runner) : root,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            start.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            return start;
        }

        /// <summary>
        /// One argument, quoted the way Windows splits a command line
        /// (CommandLineToArgvW): backslashes double only before a quote, and a
        /// quote inside is escaped - so a level called `Level "A"` arrives whole.
        /// </summary>
        internal static void Append(StringBuilder line, string argument)
        {
            if (line.Length > 0) line.Append(' ');
            var value = argument ?? "";
            if (value.Length > 0 && value.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
            {
                line.Append(value);
                return;
            }
            line.Append('"');
            var slashes = 0;
            foreach (var c in value)
            {
                if (c == '\\') { slashes++; continue; }
                if (c == '"')
                {
                    line.Append('\\', slashes * 2 + 1);
                    line.Append('"');
                }
                else
                {
                    line.Append('\\', slashes);
                    line.Append(c);
                }
                slashes = 0;
            }
            line.Append('\\', slashes * 2);
            line.Append('"');
        }
    }
}
