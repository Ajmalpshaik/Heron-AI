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
            // FRAGMENT-ISSUES row 5b-1320: A VIEW MARKED OPTIONAL BINDS NULL,
            // which is the fragment's own "whole model". Row 5b-227 left the
            // case open because nothing read the word; the binder reads it
            // now, for a View alone - a view has no empty form but null, and
            // every card that marks one optional reads null as the whole
            // model. Nothing else gains a value it was not given.
            Check(HeronBindingNote.AbsentValue("View", "true") == "null",
                  "an absent View marked optional binds null - the whole model (row 5b-1320)");
            Check(HeronBindingNote.AbsentValue("View", " True ") == "null",
                  "however the card spells true");
            Check(HeronBindingNote.AbsentValue("View", null) == null,
                  "a View NOT marked optional is still refused - the view is the caller's to name");
            Check(HeronBindingNote.AbsentValue("View", "false") == null,
                  "and so is one marked optional: false");
            Check(HeronBindingNote.AbsentValue("string", "true") == null,
                  "an optional string is still refused - row 5b-227's other two");
            Check(HeronBindingNote.AbsentValue("double", "true") == null,
                  "an optional number is still refused - no number means 'not asked'");

            // FRAGMENT-ISSUES row 5b-1320, the other half: A VALUE LEFT EMPTY
            // IS A VALUE LEFT OUT, when the card says it may be. inViewOnly=
            // with nothing after it was looked up as a view named "" and
            // refused "No view called """; the binder now asks this before
            // converting, and reads yes as absent. A need the card does not
            // mark optional converts its empty text as before, and is refused
            // by the type's own rule. Asked by reflection so that the method's
            // absence is ONE clean failure here rather than a host that will
            // not build (heron-ship 2a).
            var empty = typeof(HeronBindingNote).GetMethod("EmptyMeansAbsent",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Check(empty != null, "HeronBindingNote has EmptyMeansAbsent at all (row 5b-1320)");
            Func<string, string, bool> emptyMeansAbsent = (given, optional) =>
                empty != null && (bool)empty.Invoke(null, new object[] { given, optional });
            Check(emptyMeansAbsent("", "true"),
                  "an empty value on a need marked optional is read as absent");
            Check(emptyMeansAbsent("   ", " True "),
                  "so is a blank one, however the card spells true");
            Check(emptyMeansAbsent(null, "true"),
                  "and a null one");
            Check(empty != null && !emptyMeansAbsent("", null),
                  "an empty value on a need NOT marked optional is converted as typed - the type's own refusal");
            Check(empty != null && !emptyMeansAbsent("", "false"),
                  "and so with optional: false");
            Check(empty != null && !emptyMeansAbsent("Level 1", "true"),
                  "a value actually given is never read as absent");

            // FRAGMENT-ISSUES row 5b-488: A CARD MAY NAME WHAT LEAVING A VALUE
            // OUT MEANS. `absent:` beside `optional: true` is read as typed;
            // without both, nothing is invented.
            Check(HeronBindingNote.DeclaredAbsent("true", "all") == "all",
                  "an optional need whose card says absent: all is read as \"all\"");
            Check(HeronBindingNote.DeclaredAbsent(" True ", "  all ") == "all",
                  "however the card spells true and spaces the text");
            Check(HeronBindingNote.DeclaredAbsent(null, "all") == null,
                  "absent: on a need NOT marked optional is ignored - the card never said it may be left out");
            Check(HeronBindingNote.DeclaredAbsent("false", "all") == null,
                  "and so is one marked optional: false");
            Check(HeronBindingNote.DeclaredAbsent("true", null) == null,
                  "an optional need with no absent: is AbsentValue's to decide, as before");
            Check(HeronBindingNote.DeclaredAbsent("true", "   ") == null,
                  "and a blank absent: says nothing, so it is not an empty default");

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
            Typed();

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
        /// FRAGMENT-ISSUES 5b-441: element ids TYPED as digits for
        /// `elementIds`, kept as TEXT and refused whole when any part is wrong.
        ///
        /// By reflection for Foreign's reason: on the code before the row the
        /// rules do not exist, and these fail as checks, not as a build error.
        /// </summary>
        private static void Typed()
        {
            const BindingFlags Rule = BindingFlags.NonPublic | BindingFlags.Static;
            var typedRule = typeof(HeronBindingNote).GetMethod("TypedIds", Rule);
            var notFoundRule = typeof(HeronBindingNote).GetMethod("TypedIdsNotFound", Rule);
            var writeRule = typeof(HeronBindingNote).GetMethod("TypedIdsOnAWrite", Rule);

            Check(typedRule != null, "the binder has a rule for ids typed as digits");
            Check(notFoundRule != null, "and a refusal naming every typed id that names nothing");
            Check(writeRule != null, "and a refusal for typed ids on a write");
            if (typedRule == null || notFoundRule == null || writeRule == null) return;

            Func<string, Tuple<List<string>, string>> typed = text =>
            {
                var args = new object[] { text, null };
                var ids = (List<string>)typedRule.Invoke(null, args);
                return Tuple.Create(ids, (string)args[1]);
            };

            // THE ROW'S OWN CASE - the three ids PLACE_FAMILY_INSTANCES gave.
            var three = typed("8453537, 8453556,8453557");
            Check(three.Item2 == null && three.Item1 != null && three.Item1.Count == 3
                  && three.Item1[0] == "8453537" && three.Item1[2] == "8453557",
                  "three ids typed are three TEXTS, in the order typed, spaces trimmed");
            var wide = typed("2147483648");
            Check(wide.Item2 == null && wide.Item1 != null && wide.Item1[0] == "2147483648",
                  "an id past int's range is kept as text - nothing is parsed (Revit 2024's long ids)");

            // NOT AN ID LIST - the binder's other rules decide, as before.
            Check(typed("selected").Item1 == null && typed("selected").Item2 == null,
                  "`selected` is not typed ids and is not refused here");
            Check(typed("").Item1 == null && typed("  ").Item2 == null,
                  "a blank is not typed ids - it binds an empty list as before");
            Check(typed("Level 1").Item1 == null && typed("Level 1").Item2 == null,
                  "a name is left to OneIdNamed, which refuses it as before");

            // REFUSED WHOLE.
            var mixed = typed("8453537, Level 1");
            Check(mixed.Item1 == null && mixed.Item2 != null
                  && mixed.Item2.IndexOf("\"Level 1\"", StringComparison.Ordinal) >= 0,
                  "an id beside a name is refused, naming the name - got \"" + mixed.Item2 + "\"");
            Check(typed("-1").Item1 == null && typed("-1").Item2 == null,
                  "a minus sign is not an id - InvalidElementId is never typed in");
            Check(typed("8453537, -1").Item2 != null,
                  "and beside a real id it refuses the list");
            Check(typed("٣٤").Item1 == null,
                  "digits other than 0-9 are not an id");
            var twice = typed("8453537, 8453537");
            Check(twice.Item1 == null && twice.Item2 != null
                  && twice.Item2.IndexOf("8453537", StringComparison.Ordinal) >= 0,
                  "an id typed twice is refused - an element bound twice is turned twice");

            // EVERY ONE NOT FOUND IS NAMED, AND NOTHING IS BOUND.
            var gone = (string)notFoundRule.Invoke(null, new object[]
                { new List<string> { "99999999", "88888888" }, 3, "Project1" }) ?? "";
            Check(gone.IndexOf("99999999", StringComparison.Ordinal) >= 0
                  && gone.IndexOf("88888888", StringComparison.Ordinal) >= 0,
                  "both ids that name nothing are named - got \"" + gone + "\"");
            Check(gone.IndexOf("2 of the 3", StringComparison.Ordinal) >= 0,
                  "and how many of how many");
            Check(gone.IndexOf("'Project1'", StringComparison.Ordinal) >= 0,
                  "and the model they were looked for in");
            Check(gone.IndexOf("NOTHING WAS BOUND", StringComparison.Ordinal) >= 0,
                  "and that nothing was bound - the found one is not bound alone");
            Check(notFoundRule.Invoke(null, new object[] { new List<string>(), 3, "Project1" }) == null,
                  "nothing missing is no refusal");

            var write = (string)writeRule.Invoke(null, new object[] { "elementIds" }) ?? "";
            Check(write.IndexOf("FILTER_ELEMENTS_BY_ID", StringComparison.Ordinal) >= 0
                  && write.IndexOf("expect_from", StringComparison.Ordinal) >= 0
                  && write.IndexOf("NOTHING WAS BOUND", StringComparison.Ordinal) >= 0,
                  "a write is sent to the read first, then expect_from - got \"" + write + "\"");
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
