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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

        private static int Main()
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
