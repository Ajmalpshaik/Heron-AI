// Heron-Agent:  none
// Heron-Step:   14
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using Heron.Core;
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
    ///
    /// SINCE 2026-10-17 IT ALSO HOLDS THE RUN CLOCK (D-129): over the library,
    /// that every loop and every body asks the clock and that the rewrite adds
    /// nothing but its two calls and the braces a one-statement loop body
    /// needs; and then, with invented snippets after all, because the claim
    /// is about a script that will not stop, that a guarded runaway IS stopped
    /// within its deadline - caught or not - and that a stop asked for while
    /// it runs stops it too.
    /// </summary>
    internal static class Program
    {
        private const string GuardCall = "EnsureSufficientExecutionStack";

        // The exact statements HeronStackGuard injects, as it writes them.
        private const string GuardStatement =
            "global::System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();";
        private const string ClockStatement = "global::Heron.Revit.Addin.HeronRunClock.Check();";

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
            var loopCount = 0;
            var loopFragments = 0;
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

                // 4. EVERY LOOP IS CHECKED (D-129). Read off the guarded tree
                //    rather than the rewriter, so a loop shape the rewriter
                //    forgot is found here: each loop's body is a block whose
                //    FIRST statement is the clock's check, and each body the
                //    stack guard enters carries it straight after the guard.
                var loops = Loops(source, options).Count;
                if (loops > 0) loopFragments++;
                loopCount += loops;
                var guardedTree = CSharpSyntaxTree.ParseText(guarded, options).GetRoot();
                var unasked = Loops(guardedTree)
                    .Where(body => !StartsWith(body, ClockStatement, 0))
                    .ToList();
                Check(unasked.Count == 0,
                      name + ": " + unasked.Count + " loop(s) do not ask the clock first - at "
                      + string.Join(", ", unasked.Take(3).Select(Where)));
                Check(Loops(guardedTree).Count == loops,
                      name + ": the rewrite changed how many loops there are");
                var bare = Bodies(guardedTree)
                    .Where(body => !(StartsWith(body, GuardStatement, 0)
                                     && StartsWith(body, ClockStatement, 1)))
                    .ToList();
                Check(bare.Count == 0,
                      name + ": " + bare.Count + " lambda or method body(ies) do not ask the "
                      + "clock on entry - at " + string.Join(", ", bare.Take(3).Select(Where)));

                // 5. AND IT ADDS NOTHING ELSE. With its two statements taken
                //    out, the guarded code is the original token for token,
                //    plus one pair of braces for each loop whose body was one
                //    statement. So nothing a fragment wrote can mean anything
                //    different - the only new code is the two calls - which
                //    is what a compile gate that never sees the rewrite has
                //    to be able to lean on (D-28; see D-129's limits).
                string drift;
                var braces = AddedBraces(source, guarded, options, out drift);
                var oneStatement = Loops(source, options).Count(body => !(body is BlockSyntax));
                Check(drift == null,
                      name + ": the rewrite changed the fragment's own code - " + drift);
                Check(drift != null || braces == 2 * oneStatement,
                      name + ": expected one pair of braces for each of " + oneStatement
                      + " one-statement loop body(ies), found " + braces + " brace(s)");
            }

            Console.WriteLine("  fragments with a block-bodied lambda: " + guardedCount);
            Console.WriteLine("  loops found: " + loopCount + ", in " + loopFragments
                              + " fragments - every one asks the clock");
            Console.WriteLine("  checks run: " + _checks);
            Console.WriteLine();

            // The clock must be reached by the library too, for the reason
            // the guard must: correct and never fired protects nothing.
            Check(loopCount > 0,
                  "NOT ONE loop was found in the library, so 'every loop is checked' "
                  + "checked nothing. Check the shapes Loops() reads.");

            RunClock();
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
                                  + guardedCount + " carrying a guard, "
                                  + loopCount + " loops asking the clock.");
                Console.WriteLine();
                Console.WriteLine("What this does NOT say: that a guarded fragment "
                                  + "survives runaway recursion in front of Revit,");
                Console.WriteLine("or that a runaway loop there is stopped and rolled back "
                                  + "while Revit stays responsive.");
                Console.WriteLine("Those need a model and are NEEDS-CHECKING's to hold "
                                  + "(J9, and Group EZ).");
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

        /// <summary>
        /// The compiler errors, as RevitFragment.Compile words them.
        ///
        /// WITH THIS HOST'S OWN ASSEMBLY REFERENCED, because a guarded script
        /// names HeronRunClock and this host is where the linked copy lives -
        /// the stand-in for the add-in's assembly, which a real script
        /// references through its globals type.
        /// </summary>
        private static List<string> Errors(string code)
        {
            return CSharpScript.Create<object>(code, Referencing()).Compile()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString())
                .ToList();
        }

        private static ScriptOptions Referencing()
        {
            return ScriptOptions.Default
                .AddReferences(typeof(HeronRunClock).Assembly)
                .AddImports("System");
        }

        // ------------------------------------------------------------------
        // The run clock, run (D-129). Revit-free: the scripts below are
        // guarded exactly as RevitFragment guards a fragment and run through
        // CSharpScript with the clock begun and watched exactly as RunScript
        // runs one - on a 200 ms deadline instead of fifty seconds.
        // ------------------------------------------------------------------

        private static readonly TimeSpan Deadline = TimeSpan.FromMilliseconds(200);

        // How late "within the deadline" may be. A guarded loop checks on
        // every pass, so the stop lands a few microseconds after the
        // deadline on an idle machine; the rest is for a busy CI runner.
        private static readonly TimeSpan Slack = TimeSpan.FromMilliseconds(800);

        // A runaway that the clock FAILED to stop is still a runaway, so
        // every script runs on a background thread given this long, and one
        // still going is a FAIL line rather than a host that never exits.
        private static readonly TimeSpan GiveUp = TimeSpan.FromSeconds(15);

        private sealed class Ran
        {
            public bool Finished;
            public bool Threw;
            public string Message;
            public bool Tripped;
            public bool ByPerson;
            public string Why;
            public TimeSpan Took;
        }

        private static void RunClock()
        {
            Console.WriteLine("The run clock, run against guarded runaway scripts ("
                              + Deadline.TotalMilliseconds + " ms deadline)");

            // THE TWO THE TASK NAMES: the plain runaway, and the one that
            // catches Exception - which is most of the library's shape.
            var plain = Run("while (true) { }", Deadline, null);
            Stops("a guarded while(true){} throws", plain, true);

            var caught = Run("try { while (true) { } } catch (Exception) { }", Deadline, null);
            Stops("a guarded try{while(true){}}catch(Exception){} returns normally, swallowing "
                  + "the throw - and the clock still says it tripped, which is what RevitFragment "
                  + "asks after the script", caught, false);

            // STICKY: catching the first throw and looping on is caught at the
            // outer loop's own check, outside the try.
            var again = Run("while (true) { try { while (true) { } } catch (Exception) { } }",
                            Deadline, null);
            Stops("a runaway that catches the stop and loops on is stopped by its outer "
                  + "loop's own check", again, true);

            // A ONE-STATEMENT BODY, which the guard wraps in braces.
            var single = Run("long n = 0; for (;;) n++;", Deadline, null);
            Stops("a one-statement loop body is checked once it has braces", single, true);

            var each = Run("var xs = new int[] { 1 }; while (true) foreach (var x in xs) { }",
                           Deadline, null);
            Stops("so is a foreach inside a loop with no braces of its own", each, true);

            // RECURSION THAT NEVER RUNS OUT OF STACK but never ends either:
            // depth 40, two calls each, 2^40 calls. No loop anywhere - only
            // the lambda's entry check can stop it.
            var calls = Run("Action<int> f = null; "
                            + "f = n => { if (n > 40) return; f(n + 1); f(n + 1); }; f(0);",
                            Deadline, null);
            Stops("recursion with no loop is stopped at the lambda's entry check", calls, true);

            // THE STOP, asked from another thread while the job runs - the
            // shape of `stop` arriving on a bridge listener thread. A long
            // deadline, so only the stop can be what ends it.
            HeronStop.Resume();
            var stopped = Run("while (true) { }", TimeSpan.FromSeconds(10),
                              () => { Thread.Sleep(50); HeronStop.Stop(); });
            Check(stopped.Finished && stopped.Tripped && stopped.ByPerson,
                  "a stop asked from another thread mid-run trips the clock as a STOP");
            Check(stopped.Finished && stopped.Took < TimeSpan.FromMilliseconds(50) + Slack,
                  "and it lands at the next check, not at the deadline - took "
                  + Ms(stopped.Took));
            Say("stop asked mid-run", stopped);

            // A READ BEGUN WHILE STOPPED IS NOT TRIPPED BY THAT STOP. The gate
            // lets reads through while stopped; only a stop asked during the
            // job is the clock's.
            var reading = Run("long n = 0; for (var i = 0; i < 1000; i++) { n += i; }",
                              Deadline, null);
            Check(reading.Finished && !reading.Threw && !reading.Tripped,
                  "a short job begun while Heron is ALREADY stopped runs to the end - "
                  + (reading.Message ?? "it did"));
            HeronStop.Resume();

            // PAUSED WHILE A PERSON IS ASKED. Twice the deadline spent paused,
            // and the job still finishes; the same wait unpaused trips it.
            var paused = Run("using (global::Heron.Revit.Addin.HeronRunClock.Pause()) "
                             + "{ System.Threading.Thread.Sleep(400); } "
                             + "for (var i = 0; i < 3; i++) { }",
                             Deadline, null);
            Check(paused.Finished && !paused.Threw && !paused.Tripped,
                  "time spent in Pause() is not the job's: 400 ms paused on a 200 ms "
                  + "deadline, and it finishes - " + (paused.Message ?? "it did"));
            var unpaused = Run("System.Threading.Thread.Sleep(400); "
                               + "for (var i = 0; i < 3; i++) { }",
                               Deadline, null);
            Check(unpaused.Finished && unpaused.Tripped,
                  "while the same wait NOT paused trips at the next check after it");

            // ONLY FRAGMENT CODE IS TIMED. After the script, a check - from a
            // lazy sequence the add-in reads afterwards - never throws.
            string afterwards = null;
            using (HeronRunClock.Begin(TimeSpan.FromMilliseconds(1)))
            {
                Thread.Sleep(20);
                try { HeronRunClock.Check(); }
                catch (Exception failure) { afterwards = failure.Message; }
            }
            Check(afterwards == null,
                  "a check outside Watch() never throws, deadline or not - got "
                  + (afterwards ?? "nothing"));
            string idle = null;
            try { HeronRunClock.Check(); }
            catch (Exception failure) { idle = failure.Message; }
            Check(idle == null && !HeronRunClock.Tripped,
                  "and with no job at all a check returns, and nothing is left tripped");

            // THE MARGIN, so the clock's answer beats the dispatcher's wait.
            Check(HeronRunClock.BudgetFor(TimeSpan.FromSeconds(60)) == TimeSpan.FromSeconds(50),
                  "60 s waited gives a job 50 s - a sixth kept back for the rollback and the answer");
            Check(HeronRunClock.BudgetFor(TimeSpan.FromSeconds(6)) == TimeSpan.FromSeconds(4),
                  "6 s gives 4 s - at least two seconds kept back");
            Check(HeronRunClock.BudgetFor(TimeSpan.FromSeconds(1)) == TimeSpan.FromMilliseconds(500),
                  "1 s gives half - never more than half kept back");
            Check(HeronRunClock.BudgetFor(TimeSpan.FromSeconds(600)) == TimeSpan.FromSeconds(500),
                  "600 s gives 500 s");
        }

        private static void Stops(string what, Ran ran, bool throws)
        {
            Check(ran.Finished, what + " - it did not stop within " + GiveUp.TotalSeconds + " s");
            if (!ran.Finished) return;
            Check(ran.Tripped && !ran.ByPerson, what + " - the clock tripped, on time");
            Check(ran.Threw == throws,
                  what + " - " + (throws ? "expected it to throw" : "expected it to return")
                  + ", it " + (ran.Threw ? "threw: " + ran.Message : "returned"));
            Check(ran.Took >= Deadline - TimeSpan.FromMilliseconds(5),
                  what + " - and not before the deadline: took " + Ms(ran.Took));
            Check(ran.Took < Deadline + Slack,
                  what + " - within the deadline: took " + Ms(ran.Took));
            Check(!ran.Threw || ran.Message.StartsWith("Heron stopped this job", StringComparison.Ordinal),
                  what + " - and the throw says who stopped it: " + ran.Message);
            Check(ran.Why != null && ran.Why.Contains("0.2 seconds"),
                  what + " - and the reason names the time it was given: " + ran.Why);
            Say(what, ran);
        }

        private static void Say(string what, Ran ran)
        {
            Console.WriteLine("  " + (ran.Finished ? Ms(ran.Took) : "never stopped") + "  " + what);
        }

        private static string Ms(TimeSpan span)
        {
            return ((int)span.TotalMilliseconds) + " ms";
        }

        /// <summary>
        /// Guard the script, compile it, then run it on the clock - Begin for
        /// the job, Watch around the script - on a background thread, with
        /// `meanwhile` started beside it when given.
        /// </summary>
        private static Ran Run(string code, TimeSpan budget, Action meanwhile)
        {
            var ran = new Ran();
            var guarded = HeronStackGuard.Apply(code);
            Check(guarded.Contains(ClockStatement), "the guard put the clock into: " + code);

            var script = CSharpScript.Create<object>(guarded, Referencing());
            var errors = script.Compile().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            Check(errors.Count == 0, "the guarded script compiles - " + string.Join("; ", errors));
            if (errors.Count > 0) return ran;
            script.CreateDelegate();   // emitted now, so the clock times the run alone

            var worker = new Thread(() =>
            {
                var clock = Stopwatch.StartNew();
                using (HeronRunClock.Begin(budget))
                {
                    try
                    {
                        using (HeronRunClock.Watch())
                        {
                            script.RunAsync().GetAwaiter().GetResult();
                        }
                    }
                    catch (Exception failure)
                    {
                        ran.Threw = true;
                        ran.Message = failure.Message;
                    }
                    ran.Took = clock.Elapsed;
                    ran.Tripped = HeronRunClock.Tripped;
                    ran.ByPerson = HeronRunClock.StoppedByPerson;
                    ran.Why = HeronRunClock.Why;
                }
            });
            worker.IsBackground = true;
            worker.Start();

            Thread helper = null;
            if (meanwhile != null)
            {
                helper = new Thread(() => meanwhile());
                helper.IsBackground = true;
                helper.Start();
            }

            ran.Finished = worker.Join(GiveUp);
            if (helper != null) helper.Join(GiveUp);
            return ran;
        }

        // ------------------------------------------------------------------
        // Reading the guarded tree back.
        // ------------------------------------------------------------------

        private static List<StatementSyntax> Loops(string text, CSharpParseOptions options)
        {
            return Loops(CSharpSyntaxTree.ParseText(text, options).GetRoot());
        }

        /// <summary>The body of every loop, of every kind the guard visits.</summary>
        private static List<StatementSyntax> Loops(SyntaxNode root)
        {
            var bodies = new List<StatementSyntax>();
            foreach (var node in root.DescendantNodes())
            {
                var forLoop = node as ForStatementSyntax;
                if (forLoop != null) { bodies.Add(forLoop.Statement); continue; }
                var each = node as CommonForEachStatementSyntax;
                if (each != null) { bodies.Add(each.Statement); continue; }
                var whileLoop = node as WhileStatementSyntax;
                if (whileLoop != null) { bodies.Add(whileLoop.Statement); continue; }
                var doLoop = node as DoStatementSyntax;
                if (doLoop != null) bodies.Add(doLoop.Statement);
            }
            return bodies;
        }

        /// <summary>Every body the stack guard enters, of every kind it visits.</summary>
        private static List<StatementSyntax> Bodies(SyntaxNode root)
        {
            var bodies = new List<StatementSyntax>();
            foreach (var node in root.DescendantNodes())
            {
                var lambda = node as AnonymousFunctionExpressionSyntax;
                if (lambda != null && lambda.Block != null) { bodies.Add(lambda.Block); continue; }
                var local = node as LocalFunctionStatementSyntax;
                if (local != null && local.Body != null) { bodies.Add(local.Body); continue; }
                var method = node as MethodDeclarationSyntax;
                if (method != null && method.Body != null) bodies.Add(method.Body);
            }
            return bodies;
        }

        private static bool StartsWith(StatementSyntax body, string statement, int index)
        {
            var block = body as BlockSyntax;
            return block != null
                && block.Statements.Count > index
                && block.Statements[index].ToString() == statement;
        }

        private static string Where(SyntaxNode node)
        {
            return "line " + (node.GetLocation().GetLineSpan().StartLinePosition.Line + 1);
        }

        /// <summary>
        /// How many braces the rewrite added, or null drift when it changed
        /// nothing else. The guard's two statements are taken out of the
        /// guarded tokens first; what is left must be the original's tokens
        /// in order, with only `{` and `}` between them.
        /// </summary>
        private static int AddedBraces(string original, string guarded,
                                       CSharpParseOptions options, out string drift)
        {
            drift = null;
            var before = TokenList(original, options);
            var after = TokenList(guarded, options);
            var injected = new[]
            {
                TokenList(GuardStatement, options),
                TokenList(ClockStatement, options),
            };

            var kept = new List<string>();
            for (var j = 0; j < after.Count;)
            {
                var run = injected.FirstOrDefault(seq => seq.Count > 0 && Matches(after, j, seq));
                if (run != null) { j += run.Count; continue; }
                kept.Add(after[j]);
                j++;
            }

            var braces = 0;
            var i = 0;
            foreach (var token in kept)
            {
                if (i < before.Count && token == before[i]) { i++; continue; }
                if (token == "{" || token == "}") { braces++; continue; }
                drift = "'" + token + "' where the original has '"
                      + (i < before.Count ? before[i] : "(end)") + "'";
                return braces;
            }
            if (i != before.Count)
                drift = "the original's '" + before[i] + "' and what follows it are missing";
            return braces;
        }

        private static bool Matches(List<string> tokens, int at, List<string> run)
        {
            if (at + run.Count > tokens.Count) return false;
            for (var k = 0; k < run.Count; k++)
                if (tokens[at + k] != run[k]) return false;
            return true;
        }

        private static List<string> TokenList(string text, CSharpParseOptions options)
        {
            return CSharpSyntaxTree.ParseText(text, options).GetRoot()
                .DescendantTokens()
                .Where(t => t.Text.Length > 0)
                .Select(t => t.Text)
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
