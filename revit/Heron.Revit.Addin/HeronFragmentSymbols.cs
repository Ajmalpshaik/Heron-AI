// Heron-Agent:  none
// Heron-Step:   14
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// The release symbols a fragment is compiled with - the SAME ones this
    /// add-in was compiled with, read back from its own #if constants.
    ///
    /// THE HOLE THIS CLOSES (FRAGMENT-ISSUES row 5b-181). A fragment is
    /// compiled by Roslyn inside Revit, and Roslyn was told nothing about the
    /// release. So `#if REVIT2020 || REVIT2021` was false on Revit 2020, every
    /// version branch in the library ran its #else there, and every one of
    /// those #else branches calls a member 2020 has not got. Meanwhile
    /// tools/check-fragments-compile.py compiled the same files WITH the
    /// symbols, through Directory.Build.props, and reported them compiling on
    /// every release: the gate and the add-in were compiling different code.
    ///
    /// READ BACK FROM THIS BUILD, NOT WORKED OUT FROM A YEAR. Each symbol below
    /// is added only under its own #if, so the list is exactly what
    /// Directory.Build.props handed the compiler that built THIS add-in - the
    /// same props the fragment gate compiles with. A rule typed here instead
    /// ("REVIT year, plus OR_GREATER from 2024") would be a second copy of the
    /// props file's, and the second copy is the one that goes stale.
    /// tests/test_fragment_symbols.py holds this list against the props file
    /// for every release, so a release or a symbol added there and not here
    /// fails a suite rather than silently taking the wrong branch.
    ///
    /// ONLY THE RELEASE SYMBOLS. The compiler also defined DEBUG, TRACE and the
    /// framework's own (NETFRAMEWORK, NET8_0_OR_GREATER and so on) for this
    /// build. A fragment's contract is the Revit release, not the runtime the
    /// add-in happens to run on, and the same suite fails on a fragment #if
    /// that names anything else.
    ///
    /// HANDED OVER AS TEXT, BECAUSE THE SCRIPTING API HAS NO OTHER DOOR.
    /// Microsoft.CodeAnalysis.Scripting 4.8.0 keeps ScriptOptions'
    /// WithParseOptions INTERNAL - measured by reflection on 2026-10-09, the
    /// only public parse setting is WithLanguageVersion - so the symbols cannot
    /// be passed beside the source. They go in FRONT of it instead, as one
    /// `#define` line each, which the C# language allows before the first
    /// token of a script. Text has a second property worth having: every
    /// parser that reads it sees the same symbols. HeronStackGuard parses the
    /// source before Roslyn compiles it, and a guard that read the fragment
    /// with no symbols would guard the branch that is NOT compiled.
    ///
    /// LINE NUMBERS DO NOT MOVE. The header ends with `#line 1`, so the line
    /// after it - the first generated prologue line, or the fragment's own
    /// first line - is reported as line 1, exactly as before. RevitFragment
    /// tells the reader to subtract its prologue offset to find a line in
    /// fragment.cs, and that sentence stays true. Shown, not assumed: the same
    /// broken script gives the same compiler error, character for character,
    /// with and without the header.
    ///
    /// No Revit type is named here, so the test host links this file by source
    /// the way it links HeronStackGuard.cs.
    /// </summary>
    internal static class HeronFragmentSymbols
    {
        /// <summary>The release symbols this build of the add-in was compiled with.</summary>
        public static readonly ReadOnlyCollection<string> Defined =
            new ReadOnlyCollection<string>(Collect());

        /// <summary>
        /// One `#define` per symbol and then `#line 1`, or nothing at all when
        /// this build defined no release symbol - in which case a fragment is
        /// compiled exactly as it was before this class existed.
        /// </summary>
        public static readonly string Header = HeaderFor(Defined);

        /// <summary>
        /// The text Roslyn should compile: the header, then the source. Put it
        /// in front of the WHOLE script - generated prologue included - because
        /// a `#define` after the first token is a compile error.
        /// </summary>
        public static string Prepend(string source)
        {
            if (Header.Length == 0) return source;
            return Header + (source ?? string.Empty);
        }

        private static string HeaderFor(IList<string> symbols)
        {
            if (symbols == null || symbols.Count == 0) return string.Empty;

            var header = new StringBuilder();
            foreach (var symbol in symbols)
                header.Append("#define ").Append(symbol).Append('\n');

            // The line after this one is line 1 again, so no reported line
            // number moves.
            header.Append("#line 1\n");
            return header.ToString();
        }

        private static List<string> Collect()
        {
            var symbols = new List<string>();

            // ONE #if PER SYMBOL, each adding its own name and nothing else -
            // tests/test_fragment_symbols.py reads these pairs and fails on one
            // that names a different symbol from the one it tests.
#if REVIT2020
            symbols.Add("REVIT2020");
#endif
#if REVIT2021
            symbols.Add("REVIT2021");
#endif
#if REVIT2022
            symbols.Add("REVIT2022");
#endif
#if REVIT2023
            symbols.Add("REVIT2023");
#endif
#if REVIT2024
            symbols.Add("REVIT2024");
#endif
#if REVIT2025
            symbols.Add("REVIT2025");
#endif
#if REVIT2026
            symbols.Add("REVIT2026");
#endif
#if REVIT2027
            symbols.Add("REVIT2027");
#endif
#if REVIT2024_OR_GREATER
            symbols.Add("REVIT2024_OR_GREATER");
#endif
#if REVIT2025_OR_GREATER
            symbols.Add("REVIT2025_OR_GREATER");
#endif
#if REVIT2026_OR_GREATER
            symbols.Add("REVIT2026_OR_GREATER");
#endif
#if REVIT2027_OR_GREATER
            symbols.Add("REVIT2027_OR_GREATER");
#endif

            return symbols;
        }
    }
}
