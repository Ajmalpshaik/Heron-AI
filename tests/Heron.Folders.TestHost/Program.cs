// Heron-Agent:  none
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Text;
using Heron.Core;

namespace Heron.Folders.TestHost
{
    /// <summary>
    /// Prints platform/heron-folders.json as HeronPaths says it should read
    /// (Q-58). tools/heron-folders.py writes what this prints, or compares it
    /// with the committed file; tests/test_heron_folders.py runs both.
    ///
    /// IT ASKS, IT NEVER SPELLS AND NEVER RESOLVES. Each folder below is
    /// HeronPaths' own answer, with the blanks the script fills passed in as if
    /// they were a release and a product folder, and HeronPaths.TryTemplate
    /// says which Windows special folder it starts at and what lies under it -
    /// or, for Revit's all-user folders, HeronPaths' own declaration does.
    /// Nothing here names a part of any path or a special folder, and
    /// tools/check-structure.py and the suite fail this file if it does.
    ///
    /// A TEMPLATE, NOT THIS MACHINE'S PATH. The same code run by any user on
    /// any machine writes the same file, which is what lets a committed file
    /// be compared with the code at all.
    /// </summary>
    internal static class Program
    {
        /// <summary>The blanks the install scripts fill in.</summary>
        private const string Release = "{release}";
        private const string ProductFolder = "{folder}";

        private static int Main(string[] args)
        {
            Console.OutputEncoding = new UTF8Encoding(false);

            // WHAT THE INSTALL SCRIPTS NEED, and nothing more: a folder in
            // this list is a folder somebody can make depend on it.
            var asked = new List<string[]>
            {
                new[] { "InstallBackup", "HeronPaths.InstallBackup",
                        HeronPaths.InstallBackup(Release, ProductFolder) },
                new[] { "RevitAddins", "HeronPaths.RevitAddins",
                        HeronPaths.RevitAddins(Release) },
            };

            var json = new StringBuilder();
            json.Append("{\n");
            json.Append("  \"about\": [\n");
            var about = new[]
            {
                "The folders the install scripts need, written by the add-in's own path code",
                "(platform/Heron.Core/HeronPaths.cs) through tests/Heron.Folders.TestHost - Q-58.",
                "NEVER EDITED BY HAND. Regenerate it: python tools/heron-folders.py --write",
                "tests/test_heron_folders.py fails when it and the code disagree.",
                "Each folder starts at the Windows special folder named by 'under' - the name",
                "HeronPaths and PowerShell both hand to Windows - then 'parts' in order, with",
                "{release} and {folder} filled in by the script. tools/HeronRevit.ps1 reads it.",
            };
            for (var i = 0; i < about.Length; i++)
            {
                json.Append("    ").Append(Quote(about[i]));
                json.Append(i + 1 < about.Length ? ",\n" : "\n");
            }
            json.Append("  ],\n");
            json.Append("  \"folders\": {\n");

            // Each entry: its name in the list, the HeronPaths member it comes
            // from, the special folder it starts at by name, and the parts.
            var written = new List<Tuple<string, string, string, string[]>>();
            for (var i = 0; i < asked.Count; i++)
            {
                string under;
                string[] parts;
                string why;
                if (!HeronPaths.TryTemplate(asked[i][2], out under, out parts, out why))
                {
                    Console.Error.WriteLine(asked[i][1] + " could not be written down for the install scripts: " + why);
                    return 1;
                }
                written.Add(Tuple.Create(asked[i][0], asked[i][1], under, parts));
            }

            // THE ALL-USER ADDINS FOLDERS, READ AND NEVER WRITTEN (D-167's
            // second-copy check). DECLARED BY HeronPaths, because one of their
            // special folders does not exist off Windows and TryTemplate could
            // not read it back here - RevitAllUserAddinsTemplate says so. Each
            // is named in the list by the special folder it starts at.
            string[] allUserRoots;
            var allUserParts = HeronPaths.RevitAllUserAddinsTemplate(Release, out allUserRoots);
            var allUserPaths = HeronPaths.RevitAllUserAddins(Release);
            for (var i = 0; i < allUserRoots.Length; i++)
            {
                // AND WHERE THE FOLDER EXISTS, THE PATH HeronPaths RESOLVES IS
                // READ BACK AND MUST SAY THE SAME - so the declaration and the
                // resolution cannot drift apart unseen. A relative path is a
                // special folder this machine has not got, and is not asked.
                if (System.IO.Path.IsPathRooted(allUserPaths[i]))
                {
                    string under;
                    string[] parts;
                    string why;
                    if (!HeronPaths.TryTemplate(allUserPaths[i], out under, out parts, out why))
                    {
                        Console.Error.WriteLine("HeronPaths.RevitAllUserAddins could not be read back for the install scripts: " + why);
                        return 1;
                    }
                    if (under != allUserRoots[i] || string.Join("|", parts) != string.Join("|", allUserParts))
                    {
                        Console.Error.WriteLine("HeronPaths.RevitAllUserAddins resolves to a folder under " + under
                            + " and declares one under " + allUserRoots[i] + " - the two must be one folder.");
                        return 1;
                    }
                }
                written.Add(Tuple.Create("RevitAllUserAddins" + allUserRoots[i], "HeronPaths.RevitAllUserAddins",
                                         allUserRoots[i], allUserParts));
            }

            for (var i = 0; i < written.Count; i++)
            {
                json.Append("    ").Append(Quote(written[i].Item1)).Append(": {\n");
                json.Append("      \"from\": ").Append(Quote(written[i].Item2)).Append(",\n");
                json.Append("      \"under\": ").Append(Quote(written[i].Item3)).Append(",\n");
                json.Append("      \"parts\": [");
                var parts = written[i].Item4;
                for (var p = 0; p < parts.Length; p++)
                {
                    if (p > 0) json.Append(", ");
                    json.Append(Quote(parts[p]));
                }
                json.Append("]\n");
                json.Append(i + 1 < written.Count ? "    },\n" : "    }\n");
            }

            json.Append("  }\n");
            json.Append("}\n");

            // "\n", never Environment.NewLine: the file is compared byte for
            // byte, and Windows and Linux must write the same one.
            Console.Out.Write(json.ToString());
            Console.Out.Flush();
            return 0;
        }

        /// <summary>
        /// A JSON string, ASCII only: backslash, quote and anything outside
        /// printable ASCII escaped, so Windows PowerShell 5.1 reads the file the
        /// same whatever its code page.
        /// </summary>
        private static string Quote(string text)
        {
            var s = new StringBuilder("\"");
            foreach (var c in text)
            {
                if (c == '\\') s.Append("\\\\");
                else if (c == '"') s.Append("\\\"");
                else if (c < 0x20 || c > 0x7e) s.Append("\\u").Append(((int)c).ToString("x4"));
                else s.Append(c);
            }
            return s.Append('"').ToString();
        }
    }
}
