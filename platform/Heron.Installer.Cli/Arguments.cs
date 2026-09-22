// Heron-Agent:  HERON-INS-ORC-001
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;

namespace Heron.Installer.Cli
{
    /// <summary>
    /// What was typed on the command line, and whether it can be acted on.
    ///
    /// PURE, AND SEPARATE FROM Program FOR ONE REASON: a command line is the
    /// untrusted input on this door, exactly as a pasted URL is on route 1,
    /// and reading it has to be testable on a machine with no Revit and no
    /// Windows. Everything in here is string handling. It opens nothing,
    /// reads no file and decides nothing about installing.
    ///
    /// IT NEVER GUESSES WHAT A WRONG LINE MEANT. An unknown flag is refused
    /// by name rather than ignored, because a flag that is silently dropped
    /// is one whose absence the caller cannot see - and the caller here is
    /// often an AI, which will read the exit code and believe it.
    /// </summary>
    public sealed class Arguments
    {
        private static readonly string[] Empty = new string[0];

        /// <summary>--help, -h, or nothing usable to do.</summary>
        public bool WantsHelp { get; private set; }

        /// <summary>
        /// Route 1 - what the user named to install from. Null when they
        /// named nothing, which is not the same as naming something empty.
        ///
        /// UNJUDGED. This is the raw text; InstallSource decides whether
        /// Heron will touch it. Nothing in here inspects it.
        /// </summary>
        public string Source { get; private set; }

        /// <summary>Route 2 - a folder of Heron's files already on this PC.</summary>
        public string FromFolder { get; private set; }

        /// <summary>
        /// Which products, or empty for every one the screen offers.
        ///
        /// EMPTY MEANS EVERYTHING OFFERED, not everything in the manifest.
        /// What may be offered is InstallerScreen's answer and stays so.
        /// </summary>
        public IReadOnlyList<string> Products { get; private set; }

        /// <summary>
        /// Which Revit releases, or empty for every one found on this PC.
        /// </summary>
        public IReadOnlyList<string> Releases { get; private set; }

        /// <summary>Say what would happen and do nothing.</summary>
        public bool ListOnly { get; private set; }

        /// <summary>
        /// Why the line cannot be acted on, or null when it can.
        ///
        /// Plain English, and it says what to do next - docs/14. "Error" is
        /// never one of the words.
        /// </summary>
        public string Problem { get; private set; }

        private Arguments()
        {
            Products = Empty;
            Releases = Empty;
        }

        public static Arguments Read(string[] args)
        {
            var read = new Arguments();
            if (args == null || args.Length == 0) return read;

            var products = new List<string>();
            var releases = new List<string>();

            for (var i = 0; i < args.Length; i++)
            {
                var flag = args[i];
                switch (flag)
                {
                    case "--help":
                    case "-h":
                        read.WantsHelp = true;
                        break;

                    case "--list":
                        read.ListOnly = true;
                        break;

                    case "--source":
                        read.Source = Value(args, ref i, flag, read);
                        break;

                    case "--from":
                        read.FromFolder = Value(args, ref i, flag, read);
                        break;

                    case "--products":
                        Split(Value(args, ref i, flag, read), products);
                        break;

                    case "--releases":
                        Split(Value(args, ref i, flag, read), releases);
                        break;

                    default:
                        // NAMED, NOT JUST REFUSED. A caller who typed
                        // --sources reads "--sources" back and fixes it; one
                        // who reads "bad arguments" tries the same line again.
                        if (read.Problem == null)
                        {
                            read.Problem = "'" + Shorten(flag) + "' is not something " +
                                           "heron-install understands." + Environment.NewLine +
                                           Environment.NewLine + Usage();
                        }
                        break;
                }

                if (read.Problem != null) break;
            }

            read.Products = products;
            read.Releases = releases;

            // TWO DOORS AT ONCE IS NOT A PREFERENCE TO RESOLVE. Downloading a
            // release and using files already on the PC are different routes
            // with different rules, and picking one for the caller would mean
            // installing something other than what they asked for.
            if (read.Problem == null && read.Source != null && read.FromFolder != null)
            {
                read.Problem = "--source and --from ask for two different things at once: " +
                               "one downloads Heron's published release, the other uses files " +
                               "already on this PC. Name one.";
            }

            return read;
        }

        /// <summary>
        /// The value after a flag, or a refusal saying which flag went bare.
        ///
        /// A FLAG FOLLOWED BY ANOTHER FLAG IS A MISSING VALUE, not a value
        /// that happens to start with two dashes. "--source --list" is
        /// somebody who forgot the address, and swallowing --list as the
        /// address would send that text to InstallSource to be refused with
        /// a sentence about web addresses that explains nothing.
        /// </summary>
        private static string Value(string[] args, ref int i, string flag, Arguments read)
        {
            if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                if (read.Problem == null)
                    read.Problem = flag + " needs something after it." +
                                   Environment.NewLine + Environment.NewLine + Usage();
                return null;
            }

            i++;
            return args[i];
        }

        /// <summary>
        /// A comma-separated list, with the blanks dropped.
        ///
        /// "2020,,2024" and "2020, 2024" are both somebody listing two
        /// releases. An empty entry is not a release named "", so it is
        /// dropped rather than passed on to be reported as not found.
        /// </summary>
        private static void Split(string text, List<string> into)
        {
            if (string.IsNullOrEmpty(text)) return;
            foreach (var part in text.Split(','))
            {
                var one = part.Trim();
                if (one.Length > 0 && !into.Contains(one)) into.Add(one);
            }
        }

        private static string Shorten(string text)
        {
            var one = text.Trim().Replace("\r", " ").Replace("\n", " ");
            return one.Length <= 60 ? one : one.Substring(0, 57) + "...";
        }

        /// <summary>
        /// What this accepts, written for somebody who models buildings.
        ///
        /// THE EXIT CODES ARE PART OF IT, because the caller is often an AI
        /// and a code it has to guess at is a code it will guess wrong.
        /// </summary>
        public static string Usage()
        {
            var n = Environment.NewLine;
            return
                "heron-install - install Heron into the Revit releases on this PC." + n +
                n +
                "  heron-install" + n +
                "      Install everything, for every Revit found. Uses what is built" + n +
                "      here if anything is; otherwise fetches Heron's own release." + n +
                n +
                "  heron-install --source <address>" + n +
                "      Install from a named release. Heron checks the address first" + n +
                "      and refuses anything that is not its own published release." + n +
                n +
                "  heron-install --from <folder>" + n +
                "      Install from Heron's files already on this PC, with no" + n +
                "      internet." + n +
                n +
                "  --products <a,b>    Only these. Default: everything offered." + n +
                "  --releases <a,b>    Only these Revit releases. Default: all found." + n +
                "  --list              Say what would happen and change nothing." + n +
                "  --help              This." + n +
                n +
                "Close Revit before installing. If it is open, heron-install says which" + n +
                "one and waits for it." + n +
                n +
                "It installs. It never removes anything - use the installer window for" + n +
                "that, where unticking is something a person did on purpose." + n +
                n +
                "When it finishes:" + n +
                "  0  did what was asked" + n +
                "  1  refused before touching anything" + n +
                "  2  ran, and something failed - the lines above say which" + n +
                "  3  could not run: Revit stayed open. Nothing was changed.";
        }
    }
}
