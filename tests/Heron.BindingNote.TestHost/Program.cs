// Heron-Agent:  none
// Heron-Step:   14
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Reflection;
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

            // FRAGMENT-ISSUES row 5b-252: WHAT AN ABSENT REQUEST NEED BINDS.
            // D-59's switch left out is a switch left off, and nothing else
            // gains a value it was not given.
            Check(HeronBindingNote.AbsentValue("bool", "true") == "false",
                  "an absent bool marked optional binds false");
            Check(HeronBindingNote.AbsentValue("bool", " True ") == "false",
                  "\"optional\" is read however the card spells true");
            Check(HeronBindingNote.AbsentValue("bool", null) == null,
                  "a bool NOT marked optional is still refused");
            Check(HeronBindingNote.AbsentValue("bool", "false") == null,
                  "and so is one marked optional: false");
            Check(HeronBindingNote.AbsentValue("View", "true") == null,
                  "an optional View is still refused - row 5b-227's create-line case");
            Check(HeronBindingNote.AbsentValue("string", "true") == null,
                  "an optional string is still refused - row 5b-227's other two");
            Check(HeronBindingNote.AbsentValue("double", "true") == null,
                  "an optional number is still refused - no number means 'not asked'");

            // FRAGMENT-ISSUES row 5b-322: WHAT AN UNFILLED ELEMENT LIST BINDS.
            // A card that says nothing selected is a fine answer gets an empty
            // list; every other unfilled need is still refused.
            Check(HeronBindingNote.EmptyWhenAbsent("IList<Element>", "true") == "new List<Element>()",
                  "an element list marked optional binds an empty list");
            Check(HeronBindingNote.EmptyWhenAbsent("IList< Element >", " True ") == "new List<Element>()",
                  "however the card spaces the type and spells true");
            Check(HeronBindingNote.EmptyWhenAbsent("IEnumerable<Element>", "true") == "new List<Element>()",
                  "an IEnumerable of elements too");
            Check(HeronBindingNote.EmptyWhenAbsent("IList<Element>", null) == null,
                  "an element list NOT marked optional is still refused - nobody was asked");
            Check(HeronBindingNote.EmptyWhenAbsent("IList<Element>", "false") == null,
                  "and so is one marked optional: false");
            Check(HeronBindingNote.EmptyWhenAbsent("IList<ElementId>", "true") == null,
                  "a list of ids is not an element list here, as in IsElementList");
            Check(HeronBindingNote.EmptyWhenAbsent("Element", "true") == null,
                  "one element has no empty value, so it is still refused");
            Check(HeronBindingNote.EmptyWhenAbsent("bool", "true") == null,
                  "a bool is AbsentValue's case, not this one");

            // FRAGMENT-ISSUES row 5b-292: the name SELECT_TYPES prints must be
            // a name the binder takes.
            Check(HeronBindingNote.TypeNameMatches("M_Single-Flush", "0915 x 2134mm",
                      "M_Single-Flush : 0915 x 2134mm"),
                  "\"Family : Type\", as SELECT_TYPES prints it, names the type");
            Check(HeronBindingNote.TypeNameMatches("Basic Wall", "Generic - 200mm",
                      "Basic Wall: Generic - 200mm"),
                  "\"Family: Type\", as the Properties palette writes it, still does");
            Check(HeronBindingNote.TypeNameMatches("Basic Wall", "Generic - 200mm",
                      "Basic Wall:Generic - 200mm"),
                  "and so does no space at all");
            Check(!HeronBindingNote.TypeNameMatches("Basic Wall", "Generic - 200mm",
                      "basic wall: Generic - 200mm"),
                  "capitals still count");
            Check(!HeronBindingNote.TypeNameMatches("Basic Wall", "Generic - 200mm",
                      "Generic - 200mm"),
                  "a type name alone is ElementsNamed's other spelling, not this one");
            Check(!HeronBindingNote.TypeNameMatches("", "Generic - 200mm", ": Generic - 200mm"),
                  "and an element with no family never matches");

            Foreign();

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

        /// <summary>
        /// FRAGMENT-ISSUES row 75: a linked element carried to the next
        /// fragment is refused by name, not bound as a host element id.
        ///
        /// ASKED BY REFLECTION, NOT CALLED, and on purpose: against the code
        /// as it stood these two rules did not exist, and a direct call would
        /// stop the host COMPILING - one build error in place of every check
        /// below. Looked up by name, a missing rule is one clean failure and
        /// the rest still run and fail, which is what shows they test the fix.
        /// </summary>
        private static void Foreign()
        {
            const BindingFlags Rule = BindingFlags.NonPublic | BindingFlags.Static;
            var otherRule = typeof(HeronBindingNote).GetMethod("OtherDocument", Rule);
            var refuseRule = typeof(HeronBindingNote).GetMethod("CarriedFromElsewhere", Rule);

            Check(otherRule != null,
                  "the chain has a rule for WHICH document a carried element came from");
            Check(refuseRule != null,
                  "the binder has a rule for a carry that came from another document");

            Func<bool, string, string> other = (same, title) =>
                otherRule == null ? null : (string)otherRule.Invoke(null, new object[] { same, title });
            Func<ICollection<string>, string> refuse = documents =>
                refuseRule == null ? null : (string)refuseRule.Invoke(null, new object[]
                {
                    "elements", "IList<Element>", "from select-from-link", documents,
                    "Snowdon-scratch_ajmal.al",
                });

            const string Link = "Snowdon Towers Sample Architectural";

            // WHAT THE CARRY WRITES DOWN.
            Check(other(true, "Snowdon-scratch_ajmal.al") == null,
                  "an element of the document being read records nothing - it binds as before");
            Check(other(false, Link) == Link,
                  "an element of a LINK records the link's name");
            Check(other(false, "  " + Link + " ") == Link,
                  "trimmed, so one link is one name");
            var unnamed = other(false, null);
            Check(!string.IsNullOrEmpty(unnamed),
                  "a document with no readable name is STILL recorded - dropping it would read as 'came from here'");
            Check(!string.IsNullOrEmpty(other(false, "  ")),
                  "and so is a blank one");

            // WHAT THE BINDER DOES WITH IT. Nothing recorded binds as before.
            Check(refuse(null) == null && refuseRule != null,
                  "nothing recorded is not refused");
            Check(refuse(new List<string>()) == null && refuseRule != null,
                  "an empty record is not refused");

            // THE ROW'S OWN CASE: walls from the Architectural link, carried
            // into a fragment reading Snowdon-scratch.
            var said = refuse(new List<string> { Link }) ?? "";
            Check(said.Length > 0,
                  "linked walls carried into the host are REFUSED, not looked up there");
            Check(said.IndexOf("'" + Link + "'", StringComparison.Ordinal) >= 0,
                  "the refusal names the linked document - got \"" + said + "\"");
            Check(said.IndexOf("'elements'", StringComparison.Ordinal) >= 0,
                  "and the need it would not fill");
            Check(said.IndexOf("'Snowdon-scratch_ajmal.al'", StringComparison.Ordinal) >= 0,
                  "and the model being read, so the two are told apart");
            Check(said.IndexOf("from select-from-link", StringComparison.Ordinal) >= 0,
                  "and which step left them");
            Check(said.IndexOf("NOTHING WAS BOUND", StringComparison.Ordinal) >= 0,
                  "and says nothing was bound, as the chain's other refusals do");

            // SEVERAL LINKS, in a fixed order, each named once.
            var two = refuse(new List<string> { "Structural", Link, "Structural" }) ?? "";
            var a = two.IndexOf("'" + Link + "'", StringComparison.Ordinal);
            var s = two.IndexOf("'Structural'", StringComparison.Ordinal);
            Check(a >= 0 && s >= 0 && a < s,
                  "two links are both named, sorted, so one carry always refuses the same way");
            Check(s >= 0 && two.IndexOf("'Structural'", s + 1, StringComparison.Ordinal) < 0,
                  "and a link named twice is named once");

            // A record that is there but blank still refuses - see above.
            Check(!string.IsNullOrEmpty(refuse(new List<string> { "" })),
                  "a recorded document with no name still refuses");
        }
    }
}
