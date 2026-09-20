// Heron-Agent:  none
// Heron-Step:   14
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Heron.Revit.Addin;

namespace Heron.BindingNote.TestHost
{
    /// <summary>
    /// The binding note's counting, checked on the case it was written for.
    ///
    /// FRAGMENT-ISSUES row 75: a carried IList of ElementId partially revives
    /// against the HOST document, and the note used to print "(2)" where 1128
    /// were offered - indistinguishable from a deliberate narrowing to two.
    /// The formatter is now the ONLY thing that tells those apart, and a
    /// compiler cannot catch a regression in its wording or in the order of
    /// its two arguments. That is what this checks.
    ///
    /// EVERY CASE HERE FAILS AGAINST THE PREVIOUS IMPLEMENTATION or holds it
    /// unchanged, deliberately: the old body ignored what was offered and
    /// always printed the survivor count, so the partial case is the one that
    /// moves and the other three must not.
    /// </summary>
    internal static class Program
    {
        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private static void Check(bool condition, string what)
        {
            _checks++;
            Console.WriteLine((condition ? "  ok    " : "  FAIL  ") + what);
            if (!condition) Failures.Add(what);
        }

        private static int Main()
        {
            Console.WriteLine("The binding note counts what was OFFERED, not just what survived");
            Console.WriteLine();

            var offered = new List<int>();
            for (var i = 0; i < 1128; i++) offered.Add(i);
            var survived = new List<int> { 1, 2 };

            // THE ROW'S OWN NUMBERS. This is the case that fails against the
            // previous implementation, which printed " (2)".
            var partial = HeronBindingNote.Size(survived, offered);
            Check(partial == " (2 of 1128)",
                  "1128 offered and 2 survived reads \" (2 of 1128)\", not \" (2)\" - got \""
                  + partial + "\"");

            // ARGUMENT ORDER, which a compiler cannot catch because both are
            // object. Swapped, this would read " (1128 of 2)".
            Check(partial.IndexOf(" (2 of ", StringComparison.Ordinal) == 0,
                  "the SURVIVOR count comes first and the offered count second");

            // EQUAL COUNTS ARE UNCHANGED, so a need that lost nothing gains no
            // second number and every existing note reads as it did.
            var whole = HeronBindingNote.Size(survived, new List<int> { 7, 9 });
            Check(whole == " (2)",
                  "nothing lost reads \" (2)\" with no \"of\" - got \"" + whole + "\"");

            // NO CARRIED VALUE TO COMPARE AGAINST is the as-given path, and it
            // must read exactly as it always did.
            Check(HeronBindingNote.Size(survived, null) == " (2)",
                  "no offered collection reads \" (2)\"");

            // A SCALAR GAINS NO PARENTHESIS IT DID NOT HAVE.
            Check(HeronBindingNote.Size("a duct", offered) == "",
                  "a non-collection answers \"\", whatever it is compared with");
            Check(HeronBindingNote.Size(null, offered) == "",
                  "and so does null");

            // THE ZERO CASE NEVER REACHES THIS FORMATTER - Shape() returns
            // null and the caller says "nothing usable survived" - but if it
            // ever did, it must not read as a clean empty narrowing.
            Check(HeronBindingNote.Size(new List<int>(), offered) == " (0 of 1128)",
                  "an empty survivor list against 1128 offered still names both");

            Console.WriteLine();
            if (Failures.Count > 0)
            {
                Console.WriteLine("FAILED - " + Failures.Count + " of " + _checks);
                return 1;
            }
            Console.WriteLine("PASSED - " + _checks + " checks. A partial revival no longer");
            Console.WriteLine("reads like a deliberate narrowing.");
            return 0;
        }
    }
}
