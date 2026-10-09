// Heron-Agent:  none
// Heron-Step:   14
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Heron.Revit.Addin;

namespace Heron.StackGuard.TestHost
{
    /// <summary>
    /// Runs HeronStackGuard over the WHOLE fragment library and checks the
    /// three things that decide whether it is safe to deploy.
    ///
    /// It is deliberately not a unit test over invented snippets. The guard's
    /// risk is not that it mishandles a case somebody thought of - it is that
    /// it changes the meaning of one of 372 real fragments that work today.
    /// So the input is the library itself.
    /// </summary>
    internal static class Program
    {
        private const string GuardCall = "EnsureSufficientExecutionStack";

        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private static void Check(bool condition, string what)
        {
            _checks++;
            if (!condition) Failures.Add(what);
        }

        private static string RepoRoot()
        {
            // Walk up from the binary until the library is underneath. Not
            // built from a constant, because the output folder depth changes
            // with the target framework.
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "brain", "fragments")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException(
                "Could not find brain/fragments above " + AppContext.BaseDirectory);
        }

        private static int Main(string[] args)
        {
            string root;
            try { root = RepoRoot(); }
            catch (Exception failure)
            {
                Console.WriteLine("FAILED before it began: " + failure.Message);
                return 1;
            }

            var library = Path.Combine(root, "brain", "fragments");
            var sources = Directory
                .GetFiles(library, "fragment.cs", SearchOption.AllDirectories)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();

            // A SEPARATE QUESTION, ASKED SEPARATELY, so the guard's own run
            // below - and tests/test_stack_guard.py, which reads it - is
            // exactly what it was.
            if (args != null && args.Contains("--symbols"))
                return Symbols(sources);

            Console.WriteLine("Heron stack guard, against the real library");
            Console.WriteLine(new string('=', 62));
            Console.WriteLine();
            Console.WriteLine("  fragments found: " + sources.Count);
            Console.WriteLine();

            if (sources.Count == 0)
            {
                // A sweep that finds nothing must never report success. The
                // repository has been bitten by a pattern that could not see
                // what was there; this is the same failure wearing a different
                // hat.
                Console.WriteLine("FAILED: no fragment.cs found under " + library);
                return 1;
            }

            var guardedCount = 0;
            var options = new CSharpParseOptions(kind: SourceCodeKind.Script);

            foreach (var path in sources)
            {
                var name = Path.GetFileName(Path.GetDirectoryName(
                    Path.GetDirectoryName(Path.GetDirectoryName(path))) ?? path);
                var source = File.ReadAllText(path);
                var guarded = HeronStackGuard.Apply(source);

                // 1. LINE NUMBERS SURVIVE. Compile() tells the reader to
                //    subtract a prologue offset to find the line in
                //    fragment.cs, so a rewriter that adds a line makes every
                //    error message point one line wrong - and only for
                //    fragments that happen to contain a lambda, which is the
                //    worst kind of inconsistency to debug.
                Check(CountLines(source) == CountLines(guarded),
                      name + ": line count changed ("
                      + CountLines(source) + " -> " + CountLines(guarded) + ")");

                // 2. IT STILL PARSES, and introduces no diagnostic the
                //    original did not already have. Compared as a SET
                //    difference rather than a count, for the reason
                //    gates.yml already compares failing suites that way: a
                //    total hides a new problem arriving beside a fixed one.
                var before = Diagnostics(source, options);
                var after = Diagnostics(guarded, options);
                var added = after.Except(before).ToList();
                Check(added.Count == 0,
                      name + ": rewriting introduced " + added.Count
                      + " diagnostic(s): " + string.Join("; ", added.Take(2)));

                // 3. WHERE THERE IS A BLOCK-BODIED LAMBDA, THE GUARD IS IN
                //    IT. This is the check that would have caught taking the
                //    original idea unexamined - its rewriter skips lambdas,
                //    and lambdas are the only shape a fragment has.
                if (HasBlockBodiedAnonymousFunction(source, options))
                {
                    guardedCount++;
                    Check(guarded.Contains(GuardCall),
                          name + ": has a block-bodied lambda and was NOT guarded");
                }
            }

            Console.WriteLine("  fragments with a block-bodied lambda: " + guardedCount);
            Console.WriteLine("  checks run: " + _checks);
            Console.WriteLine();

            // The guard must actually be reached by the library as it stands.
            // A rewriter that is correct and never fires protects nothing, and
            // would pass every check above.
            Check(guardedCount > 0,
                  "NOT ONE fragment was guarded - the rewriter never fires, so it "
                  + "protects nothing. Check the shapes it visits.");

            if (Failures.Count == 0)
            {
                Console.WriteLine("PASSED - " + _checks + " checks, "
                                  + sources.Count + " fragments, "
                                  + guardedCount + " carrying a guard.");
                Console.WriteLine();
                Console.WriteLine("What this does NOT say: that a guarded fragment "
                                  + "survives runaway recursion in front of Revit.");
                Console.WriteLine("That needs a model and is NEEDS-CHECKING's to hold.");
                return 0;
            }

            Console.WriteLine("FAILED - " + Failures.Count + " of " + _checks + ":");
            foreach (var failure in Failures.Take(25))
                Console.WriteLine("  " + failure);
            if (Failures.Count > 25)
                Console.WriteLine("  ... and " + (Failures.Count - 25) + " more");
            return 1;
        }

        /// <summary>
        /// The release symbols a fragment is compiled with, for the release
        /// THIS host was built for (FRAGMENT-ISSUES row 5b-181).
        ///
        /// Two lines are for tests/test_fragment_symbols.py to read, and it
        /// holds them against Directory.Build.props and the library:
        ///   defined: what HeronFragmentSymbols read back from this build
        ///   changed: the fragments Roslyn now reads differently on this
        ///            release than it did with no symbols
        /// Everything else is checked here and reported the way the guard's
        /// run reports - a FAIL line each, and exit 1 if any.
        /// </summary>
        private static int Symbols(List<string> sources)
        {
            Console.WriteLine("Heron fragment release symbols, for the release this host was built for");
            Console.WriteLine(new string('=', 62));
            Console.WriteLine();

            var defined = HeronFragmentSymbols.Defined;
            var header = HeronFragmentSymbols.Header;
            Console.WriteLine("defined: " + string.Join(";", defined));

            // 1. ONE RELEASE. A build defines REVIT<year> for the release it
            //    is for and no other; none means the helper missed this
            //    release, two means it read a symbol it should not have.
            var years = defined.Where(s => Regex.IsMatch(s, "^REVIT[0-9]{4}$")).ToList();
            Check(years.Count == 1,
                  "exactly one REVIT<year> symbol is defined - got " + years.Count
                  + " (" + string.Join(", ", years) + ")");
            var release = years.Count > 0 ? years[0] : "REVIT0000";

            // 2. THE HEADER IS ONE #define EACH AND THEN #line 1, and nothing
            //    else - anything more is text Roslyn compiles as the fragment.
            var expected = string.Concat(defined.Select(s => "#define " + s + "\n"))
                           + (defined.Count > 0 ? "#line 1\n" : "");
            Check(header == expected,
                  "the header is one #define per symbol and then #line 1 - got "
                  + header.Replace("\n", "\\n"));

            // 3. THE ROW'S OWN CLAIM, RUN RATHER THAN READ: a one-line #if
            //    script answers differently with the header and without. The
            //    "without" half is what every fragment got before this fix.
            foreach (var symbol in defined)
            {
                var probe = "#if " + symbol + "\ntrue\n#else\nfalse\n#endif\n";
                var withSymbols = Evaluate(HeronFragmentSymbols.Prepend(probe));
                var withoutSymbols = Evaluate(probe);
                Check(withSymbols == true,
                      "`#if " + symbol + "` is TRUE in a script given the header - got "
                      + Said(withSymbols));
                Check(withoutSymbols == false,
                      "`#if " + symbol + "` is false in a script given nothing, which is "
                      + "the add-in before this fix - got " + Said(withoutSymbols));
            }

            // And it does not over-define: another release's symbol stays
            // false with the header in front.
            var other = release == "REVIT2020" ? "REVIT2021" : "REVIT2020";
            var otherProbe = "#if " + other + "\ntrue\n#else\nfalse\n#endif\n";
            Check(Evaluate(HeronFragmentSymbols.Prepend(otherProbe)) == false,
                  "`#if " + other + "` stays false on a build for " + release);

            // 4. LINE NUMBERS DO NOT MOVE. The same semantic error, with a
            //    stand-in for the generated prologue in front, reads the same
            //    character for character - directly, and through the guard as
            //    Compile() does it.
            var broken = "var a = 1;\nvar b = 2;\n  var c = nope + a;\n";
            var plainErrors = Errors(broken);
            var headedErrors = Errors(HeronFragmentSymbols.Prepend(broken));
            var guardedErrors = Errors(HeronStackGuard.Apply(HeronFragmentSymbols.Prepend(broken)));
            Check(plainErrors.Count == 1 && plainErrors[0].StartsWith("(3,11)", StringComparison.Ordinal),
                  "the stand-in error is reported at (3,11) with no header - got "
                  + string.Join(" | ", plainErrors));
            Check(plainErrors.SequenceEqual(headedErrors),
                  "the header moves no reported position - with it: "
                  + string.Join(" | ", headedErrors));
            Check(plainErrors.SequenceEqual(guardedErrors),
                  "nor does the guard on top of it - got " + string.Join(" | ", guardedErrors));

            // 5. THE GUARD READS THE BRANCH THAT IS COMPILED. A block lambda
            //    on this release's side of an #if, nothing on the other. With
            //    the header in front of the guard it is guarded; without, the
            //    guard reads the #else and leaves the compiled lambda bare.
            var branchy = "#if " + release + "\n"
                          + "System.Func<int, int> f = x => { return x + 1; };\n"
                          + "#else\n"
                          + "int f = 0;\n"
                          + "#endif\n";
            var guardedBranch = HeronStackGuard.Apply(HeronFragmentSymbols.Prepend(branchy));
            Check(guardedBranch.Contains(GuardCall),
                  "the guard reaches a lambda that only this release's branch holds");
            Check(Errors(guardedBranch).Count == 0,
                  "and the guarded script compiles - got "
                  + string.Join(" | ", Errors(guardedBranch)));
            Check(!HeronStackGuard.Apply(branchy).Contains(GuardCall),
                  "given NO header the guard misses it - which is why the header goes "
                  + "in front of the guard and not only in front of Roslyn");

            // 6. THE LIBRARY, AS THE ADD-IN NOW READS IT. Every fragment keeps
            //    the header intact through the guard, gains no line and no
            //    parse diagnostic, and those whose compiled code changes on
            //    this release are named.
            Check(sources.Count > 0, "fragments were found to read at all");
            var options = new CSharpParseOptions(kind: SourceCodeKind.Script);
            var changed = new List<string>();
            foreach (var path in sources)
            {
                var name = Path.GetFileName(Path.GetDirectoryName(
                    Path.GetDirectoryName(Path.GetDirectoryName(path))) ?? path);
                var source = File.ReadAllText(path);
                var headed = HeronFragmentSymbols.Prepend(source);
                var guarded = HeronStackGuard.Apply(headed);

                Check(guarded.StartsWith(header, StringComparison.Ordinal),
                      name + ": the guard kept the header intact");
                Check(CountLines(guarded) == CountLines(headed),
                      name + ": the guard changed the line count with the header in front ("
                      + CountLines(headed) + " -> " + CountLines(guarded) + ")");
                var added = Diagnostics(guarded, options)
                    .Except(Diagnostics(headed, options)).ToList();
                Check(added.Count == 0,
                      name + ": rewriting with the header introduced " + added.Count
                      + " diagnostic(s): " + string.Join("; ", added.Take(2)));

                if (Tokens(headed, options) != Tokens(source, options))
                    changed.Add(name);
            }

            Console.WriteLine("changed: " + string.Join(";", changed));
            Console.WriteLine("  fragments read: " + sources.Count);
            Console.WriteLine("  checks run: " + _checks);
            Console.WriteLine();

            if (Failures.Count == 0)
            {
                Console.WriteLine("PASSED - " + _checks + " checks for " + release + ", "
                                  + changed.Count + " fragment(s) compile a different branch "
                                  + "than they did with no symbols.");
                Console.WriteLine();
                Console.WriteLine("What this does NOT say: that the branch now compiled works "
                                  + "in front of that Revit.");
                Console.WriteLine("That needs the release itself, and a model.");
                return 0;
            }

            Console.WriteLine("FAILED - " + Failures.Count + " of " + _checks + ":");
            foreach (var failure in Failures.Take(25))
                Console.WriteLine("  " + failure);
            if (Failures.Count > 25)
                Console.WriteLine("  ... and " + (Failures.Count - 25) + " more");
            return 1;
        }

        /// <summary>
        /// A script's bool answer, or null when it would not compile or run -
        /// so a broken probe is one FAIL line, never a traceback that ends the
        /// run.
        /// </summary>
        private static bool? Evaluate(string code)
        {
            try
            {
                return CSharpScript.EvaluateAsync<bool>(code).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string Said(bool? value)
        {
            return value == null ? "no answer (it did not compile or run)" : value.ToString();
        }

        /// <summary>The compiler errors, as RevitFragment.Compile words them.</summary>
        private static List<string> Errors(string code)
        {
            return CSharpScript.Create<object>(code).Compile()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString())
                .ToList();
        }

        /// <summary>
        /// The code a parser would compile: every token, and no trivia - so a
        /// #define, a #line and the text of an inactive #if branch are all
        /// left out, and two texts that compile the same code agree.
        /// </summary>
        private static string Tokens(string text, CSharpParseOptions options)
        {
            return string.Join(" ", CSharpSyntaxTree.ParseText(text, options)
                .GetRoot().DescendantTokens().Select(t => t.Text));
        }

        private static int CountLines(string text)
        {
            return text.Split('\n').Length;
        }

        private static List<string> Diagnostics(string text, CSharpParseOptions options)
        {
            return CSharpSyntaxTree.ParseText(text, options)
                .GetDiagnostics()
                .Select(d => d.Id + " " + d.GetMessage())
                .ToList();
        }

        private static bool HasBlockBodiedAnonymousFunction(
            string text, CSharpParseOptions options)
        {
            var root = CSharpSyntaxTree.ParseText(text, options).GetRoot();
            return root.DescendantNodes().Any(node =>
            {
                var lambda = node as Microsoft.CodeAnalysis.CSharp.Syntax.AnonymousFunctionExpressionSyntax;
                return lambda != null && lambda.Block != null;
            });
        }
    }
}
