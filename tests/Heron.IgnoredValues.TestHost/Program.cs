// Heron-Agent:  none
// Heron-Step:   14
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Heron.Revit.Addin;

namespace Heron.IgnoredValues.TestHost
{
    /// <summary>
    /// The ignored-values wording, checked on the cases it was written for.
    ///
    /// BindNeeds walks the needs it EXPECTS and looks each one up in what the
    /// caller supplied. It never walked the other way, so a supplied name
    /// matching no need was read by nothing and reported by nothing, and the
    /// reply said the whole thing had been applied. `categoryName` for
    /// `categories` is one character wrong and a clean success.
    ///
    /// EVERY CASE HERE FAILS AGAINST THE PREVIOUS BEHAVIOUR or holds it
    /// unchanged. There was no previous implementation - the silence WAS the
    /// implementation - so the cases that matter are the two directions of
    /// wrongness: a stray name must be named, and a clean run must stay
    /// silent. A formatter that spoke on every run would be worse than the
    /// silence it replaced, because a warning that is usually wrong is one
    /// nobody reads.
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
            Console.WriteLine("A supplied value nothing read is NAMED, and a clean run stays silent");
            Console.WriteLine();

            var takes = new List<string> { "view", "categories", "overrides" };

            // 1. THE MISSPELLING, which is the whole reason this exists.
            var typo = HeronIgnoredValues.Describe(
                new List<string> { "view", "categoryName", "overrides" },
                new List<string> { "view", "overrides" },
                takes);
            Check(typo != null && typo.IndexOf("'categoryName'", StringComparison.Ordinal) >= 0,
                  "a misspelt name is named - got " + Show(typo));
            Check(typo != null && typo.IndexOf("'categories'", StringComparison.Ordinal) >= 0,
                  "and the message says what the run DOES take");

            // 2. THE CLEAN RUN, which must say nothing at all. A formatter
            //    that speaks here trains the reader to ignore it.
            var clean = HeronIgnoredValues.Describe(
                new List<string> { "view", "categories" },
                new List<string> { "view", "categories" },
                takes);
            Check(clean == null, "everything read -> null, not an empty-ish sentence");

            // 3. A CHAINED RUN. The setup step and the fragment share ONE
            //    supplied dictionary, so a value THIS fragment never touched
            //    may have been read by the step before it. Consumed is the
            //    union across every step, so this must stay silent too.
            var chained = HeronIgnoredValues.Describe(
                new List<string> { "view", "categories", "categoryName" },
                new List<string> { "view", "categories", "categoryName" },
                takes);
            Check(chained == null,
                  "a value read by a SETUP step is not reported against the fragment");

            // 4. SINGULAR AND PLURAL, because "'x' were supplied" is the kind
            //    of sentence that makes a reader doubt the rest of the reply.
            var one = HeronIgnoredValues.Describe(
                new List<string> { "wrong" }, new List<string>(), takes);
            Check(one != null && one.IndexOf("'wrong' was supplied", StringComparison.Ordinal) == 0,
                  "one stray name reads \"was supplied\" - got " + Show(one));

            var two = HeronIgnoredValues.Describe(
                new List<string> { "wrongB", "wrongA" }, new List<string>(), takes);
            Check(two != null && two.IndexOf("'wrongA', 'wrongB' were supplied",
                                             StringComparison.Ordinal) == 0,
                  "two read \"were supplied\", sorted so the message is stable - got " + Show(two));

            // 5. A RUN THAT ASKS FOR NOTHING still has to say something useful
            //    about a value it was handed, rather than name an empty list.
            var none = HeronIgnoredValues.Describe(
                new List<string> { "stray" }, new List<string>(), new List<string>());
            Check(none != null && none.IndexOf("takes no values from the caller",
                                               StringComparison.Ordinal) > 0,
                  "with nothing on offer it says so instead of printing ''");

            // 6. IT MUST NOT READ AS A FAILURE. The run happened; this is a
            //    note about one value, and the wording has to keep both true.
            Check(typo != null && typo.IndexOf("the rest of the run still did",
                                               StringComparison.Ordinal) > 0,
                  "the message says the rest of the run still happened");

            // 7. NOTHING SUPPLIED AT ALL - the ordinary read-only call.
            Check(HeronIgnoredValues.Describe(new List<string>(), new List<string>(), takes) == null,
                  "no supplied values -> null");

            Console.WriteLine();
            if (Failures.Count == 0)
            {
                Console.WriteLine("All " + _checks + " checks passed.");
                return 0;
            }

            Console.WriteLine(Failures.Count + " of " + _checks + " FAILED:");
            foreach (var failure in Failures) Console.WriteLine("  - " + failure);
            return 1;
        }

        private static string Show(string value)
        {
            return value == null ? "null" : "\"" + value + "\"";
        }
    }
}
