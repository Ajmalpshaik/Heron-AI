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
            // 2026-11-01: AN OPTIONAL VIEW LIST BINDS EMPTY, so PLACE_VIEW_ON_SHEET
            // can take its views handed on as `elements` when none is named.
            Check(HeronBindingNote.AbsentValue("IList<View>", "true") == "new List<View>()",
                  "an absent IList<View> marked optional binds an empty list");
            Check(HeronBindingNote.AbsentValue(" IList<View> ", " True ") == "new List<View>()",
                  "however the card spells it");
            Check(HeronBindingNote.AbsentValue("IList<View>", null) == null,
                  "a view list NOT marked optional is still refused - the views are the caller's to name");
            Check(HeronBindingNote.AbsentValue("IList<View>", "false") == null,
                  "and so is one marked optional: false");
            // 2026-11-01: AN OPTIONAL ELEMENT BINDS NULL AND AN OPTIONAL POINT
            // LIST EMPTY, so CREATE_OPENING's family half - the template's own
            // wall or ceiling, found by the fragment - can be asked at all.
            Check(HeronBindingNote.AbsentValue("Element", "true") == "null",
                  "an absent Element marked optional binds null");
            Check(HeronBindingNote.AbsentValue(" Element ", " True ") == "null",
                  "however the card spells it");
            Check(HeronBindingNote.AbsentValue("Element", null) == null,
                  "an Element NOT marked optional is still refused - the element is the caller's to name");
            Check(HeronBindingNote.AbsentValue("IList<XYZ>", "true") == "new List<XYZ>()",
                  "an absent IList<XYZ> marked optional binds an empty list");
            Check(HeronBindingNote.AbsentValue("IList<XYZ>", "false") == null,
                  "and a point list marked optional: false is still refused");
            Check(HeronBindingNote.AbsentValue("XYZ", "true") == null,
                  "an optional single point is still refused - no point means 'not asked'");
            Check(HeronBindingNote.AbsentValue("IList<Element>", "true") == null,
                  "an element list is EmptyWhenAbsent's case, never this one");
            Check(HeronBindingNote.AbsentValue("IList<string>", "true") == null,
                  "and an optional list of names is still refused");
            Check(HeronBindingNote.AbsentValue("string", "true") == null,
                  "an optional string is still refused - row 5b-227's other two");
            Check(HeronBindingNote.AbsentValue("double", "true") == null,
                  "an optional number is still refused - no number means 'not asked'");
            // 2026-11-01: ONE ELEMENT OR ONE DUCT-OR-PIPE SYSTEM TYPE MARKED
            // OPTIONAL BINDS NULL - TRACE_CONNECTIVITY with no start, ALIGN_ELEMENTS
            // with a typed height and no reference, CREATE_MEP_RUNS drawing cable
            // tray, CREATE_MEP_SYSTEM making a circuit. Only when marked.
            Check(HeronBindingNote.AbsentValue("Element", "true") == "null",
                  "an absent Element marked optional binds null");
            Check(HeronBindingNote.AbsentValue(" Element ", " True ") == "null",
                  "however the card spells it");
            Check(HeronBindingNote.AbsentValue("Element", null) == null,
                  "an Element NOT marked optional is still refused - it is the caller's to name");
            Check(HeronBindingNote.AbsentValue("MEPSystemType", "true") == "null",
                  "an absent MEPSystemType marked optional binds null");
            Check(HeronBindingNote.AbsentValue("MEPSystemType", "false") == null,
                  "and one marked optional: false is still refused");
            Check(HeronBindingNote.AbsentValue("MechanicalSystemType", "true") == null,
                  "a narrower system type is not one of the two spellings, so still refused");
            Check(HeronBindingNote.AbsentValue("FamilyInstance", "true") == null,
                  "and neither is a family instance - its blank is the resolver's own");

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
            // Row 5b-1841, the owner's answer A (2026-11-20): an EMPTY
            // `absent: ""` is the empty text, bound as if "" were typed - it
            // returned null until then, and the seven cards that write it
            // were refused for the value they say may be left out.
            Check(HeronBindingNote.DeclaredAbsent("true", "") == "",
                  "an optional need whose card says absent: \"\" is read as the empty text (row 5b-1841)");
            Check(HeronBindingNote.DeclaredAbsent("true", "   ") == "",
                  "and a blank absent: is the empty text too, once trimmed");
            Check(HeronBindingNote.DeclaredAbsent(null, "") == null,
                  "an empty absent: on a need NOT marked optional is still ignored");

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
            // 2026-11-01, GROUP_AND_COUNT v2: an optional quantity per element
            // binds null - "nobody handed one on" - never an empty dictionary.
            Check(HeronBindingNote.EmptyWhenAbsent("IDictionary<ElementId, double>", "true") == "null",
                  "a quantity per element marked optional binds null");
            Check(HeronBindingNote.EmptyWhenAbsent("IDictionary<ElementId, double>", null) == null,
                  "and NOT marked optional it is still refused");
            Check(HeronBindingNote.EmptyWhenAbsent("IDictionary<ElementId, string>", "true") == null,
                  "a dictionary of text is not that case");
            Check(HeronBindingNote.EmptyWhenAbsent("Element", "true") == null,
                  "one element has no empty value, so it is still refused");
            Check(HeronBindingNote.EmptyWhenAbsent("bool", "true") == null,
                  "a bool is AbsentValue's case, not this one");
            // 2026-11-01: A CURVE LIST MARKED OPTIONAL BINDS EMPTY, so
            // SET_VIEW_CROP crops to a rectangle when no loop is handed on.
            Check(HeronBindingNote.EmptyWhenAbsent("IList<Curve>", "true") == "new List<Curve>()",
                  "a curve list marked optional binds an empty list");
            Check(HeronBindingNote.EmptyWhenAbsent(" IList< Curve > ", " True ") == "new List<Curve>()",
                  "however the card spaces it");
            Check(HeronBindingNote.EmptyWhenAbsent("IList<Curve>", null) == null,
                  "a curve list NOT marked optional is still refused - nothing was handed on");
            Check(HeronBindingNote.EmptyWhenAbsent("Curve", "true") == null,
                  "one curve has no empty value, so it is still refused");

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
            MixedIds();
            Typed();
            NamedList();
            Parts();
            TakenBack();

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
        /// FRAGMENT-ISSUES row 75, part (2), 2026-11-21: a list a fragment left
        /// as IDS carries no document, so the chain counts, when it keeps one,
        /// how many of its ids name an element in the model they were left in.
        /// Some that do and some that do not is a list holding another model's
        /// ids beside this one's - the 2-of-1128 bind - and it is refused whole.
        /// By reflection, for Foreign's reason.
        /// </summary>
        private static void MixedIds()
        {
            const BindingFlags Rule = BindingFlags.NonPublic | BindingFlags.Static;
            var recordRule = typeof(HeronBindingNote).GetMethod("IdsFromElsewhere", Rule);
            var refuseRule = typeof(HeronBindingNote).GetMethod("CarriedFromElsewhere", Rule);

            Check(recordRule != null,
                  "the chain has a rule for a carried list of IDS that names elements here and not here (row 75 (2))");

            Func<int, int, string> record = (here, nothing) =>
                recordRule == null ? null : (string)recordRule.Invoke(null, new object[] { here, nothing });
            Func<ICollection<string>, string> refuse = documents =>
                refuseRule == null ? null : (string)refuseRule.Invoke(null, new object[]
                {
                    "elements", "IList<ElementId>", "from report-door-room-links", documents,
                    "Snowdon-scratch_ajmal.al",
                });

            // WHAT IS RECORDED. Only a MIXED list - the one that binds wrong.
            Check(recordRule != null && record(1128, 0) == null,
                  "every id naming an element here records nothing - it binds as before");
            Check(recordRule != null && record(0, 1128) == null,
                  "no id naming anything here records nothing either - nothing wrong can bind from it");
            Check(recordRule != null && record(0, 0) == null,
                  "an empty list, or one of built-in ids only, records nothing");
            var mixed = record(2, 1126) ?? "";
            Check(mixed.Length > 0,
                  "2 ids naming something here and 1126 naming nothing - the row's walls, as ids - IS recorded");
            Check(mixed.IndexOf("1126 of 1128", StringComparison.Ordinal) >= 0,
                  "with how many of how many named nothing - got \"" + mixed + "\"");
            Check(mixed.IndexOf("'", StringComparison.Ordinal) < 0,
                  "in words with no quote of their own, so the refusal quoting them reads cleanly");

            // WHAT THE BINDER SAYS. Refused whole, in words for an id list.
            var said = mixed.Length == 0 ? "" : (refuse(new List<string> { mixed }) ?? "");
            Check(said.Length > 0,
                  "a mixed id list carried into the next step is REFUSED, not handed on");
            Check(said.IndexOf("'elements'", StringComparison.Ordinal) >= 0
                  && said.IndexOf("from report-door-room-links", StringComparison.Ordinal) >= 0,
                  "naming the need and the step that left it - got \"" + said + "\"");
            Check(said.IndexOf("1126 of 1128", StringComparison.Ordinal) >= 0
                  && said.IndexOf("'Snowdon-scratch_ajmal.al'", StringComparison.Ordinal) >= 0,
                  "and how many named nothing in the model being read, by name");
            Check(said.IndexOf("NOTHING WAS BOUND", StringComparison.Ordinal) >= 0,
                  "and says nothing was bound, as the chain's other refusals do");
            Check(said.IndexOf("belong to '", StringComparison.Ordinal) < 0,
                  "never 'belong to' a model it cannot name - an id list does not say whose it is");

            // A link's elements AND a mixed id list in one record: both said.
            var both = mixed.Length == 0 ? ""
                : (refuse(new List<string> { "Structural", mixed }) ?? "");
            Check(both.IndexOf("'Structural'", StringComparison.Ordinal) >= 0
                  && both.IndexOf("1126 of 1128", StringComparison.Ordinal) >= 0,
                  "a record naming a link beside a mixed id list says both");
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
        /// FRAGMENT-ISSUES row 5b-1994: a `name=value; name=value` list - the
        /// `values` of SET_FAMILY_TYPE_VALUES, and every other need typed as an
        /// IDictionary - was split on EVERY semicolon, so a text value that held
        /// one could not be written as the owner gave it. On 2026-11-23 (Revit
        /// 2020) a filter's efficiency, "...ASHRAE 52/76; design 81 % / 91 %",
        /// was refused with `"design 81 % / 91 %" is not an entry for 'values'`,
        /// and the only way round was to rewrite it with commas.
        ///
        /// A VALUE IN DOUBLE QUOTES KEEPS ITS SEMICOLONS AND ITS SPACES, and
        /// \" inside it is a quote. An unquoted list splits exactly as before.
        ///
        /// By reflection for Foreign's reason: on the code before the row the
        /// rule does not exist, and these fail as checks, not as a build error.
        /// </summary>
        private static void NamedList()
        {
            const BindingFlags Rule = BindingFlags.NonPublic | BindingFlags.Static;
            var rule = typeof(HeronBindingNote).GetMethod("NamedEntries", Rule);
            Check(rule != null, "the binder has ONE rule for splitting a name=value; list (row 5b-1994)");
            if (rule == null) return;

            const string Shown = "\"view=*-Mech*; sheet=A-*\"";
            Func<string, Tuple<List<KeyValuePair<string, string>>, string>> split = text =>
            {
                var args = new object[] { text, "values", Shown, null };
                var entries = (List<KeyValuePair<string, string>>)rule.Invoke(null, args);
                return Tuple.Create(entries, (string)args[3]);
            };
            Func<List<KeyValuePair<string, string>>, string> said = entries =>
            {
                if (entries == null) return "(null)";
                var parts = new List<string>();
                foreach (var e in entries) parts.Add("[" + e.Key + "]=[" + e.Value + "]");
                return string.Join(" ", parts.ToArray());
            };

            // THE ROW'S OWN TEXT, quoted. It was refused; now it is two entries.
            var row = split("Filter_Efficiency=\"92-94 % (EN 779 mod.), 70 % ASHRAE 52/76; "
                            + "design 81 % / 91 %\"; Filter_Cell=Bag, G4");
            Check(row.Item2 == null && row.Item1 != null && row.Item1.Count == 2
                  && row.Item1[0].Key == "Filter_Efficiency"
                  && row.Item1[0].Value == "92-94 % (EN 779 mod.), 70 % ASHRAE 52/76; design 81 % / 91 %"
                  && row.Item1[1].Key == "Filter_Cell" && row.Item1[1].Value == "Bag, G4",
                  "a quoted value keeps its semicolon, and the entry after it is its own - got "
                  + said(row.Item1) + " / " + row.Item2);

            // THE SAME TEXT UNQUOTED IS STILL REFUSED, in the words it was.
            var bare = split("Filter_Efficiency=70 % ASHRAE 52/76; design 81 % / 91 %");
            Check(bare.Item1 == null && bare.Item2 != null
                  && bare.Item2.IndexOf("\"design 81 % / 91 %\" is not an entry for 'values'",
                                        StringComparison.Ordinal) >= 0,
                  "unquoted, the second half is still not an entry - got \"" + bare.Item2 + "\"");
            Check(bare.Item2 != null && bare.Item2.IndexOf("double quotes", StringComparison.Ordinal) >= 0,
                  "and the refusal says a value holding a semicolon goes in double quotes");

            // UNQUOTED LISTS SPLIT EXACTLY AS BEFORE.
            var plain = split("  Width = 600 ;Depth=600;; Name=a=b ; ");
            Check(plain.Item2 == null && said(plain.Item1) == "[Width]=[600] [Depth]=[600] [Name]=[a=b]",
                  "an unquoted list: trimmed, empty pieces skipped, the first = splits - got "
                  + said(plain.Item1));
            var inch = split("Size=12\" duct; Note=it's \"fine\"");
            Check(inch.Item2 == null && said(inch.Item1) == "[Size]=[12\" duct] [Note]=[it's \"fine\"]",
                  "a quote that does not OPEN the value is text, as before - got " + said(inch.Item1));
            Check(split("").Item2 == null && split("").Item1 != null && split("").Item1.Count == 0
                  && split(null).Item1 != null && split(" ; ").Item1.Count == 0,
                  "an empty list is no entries, not a refusal");
            var noKey = split("=5");
            Check(noKey.Item1 == null && noKey.Item2 != null
                  && noKey.Item2.IndexOf("\"=5\" is not an entry", StringComparison.Ordinal) >= 0,
                  "a value with no name is refused as before - got \"" + noKey.Item2 + "\"");

            // QUOTED: SPACES KEPT, A BLANK, AN ESCAPED QUOTE, A BACKSLASH.
            var spaces = split("prefix=\" L1 - \"; blank=\"\"; size=\"12\\\" duct\"; path=\"C:\\temp\"");
            Check(spaces.Item2 == null && spaces.Item1 != null && spaces.Item1.Count == 4
                  && spaces.Item1[0].Value == " L1 - " && spaces.Item1[1].Value == ""
                  && spaces.Item1[2].Value == "12\" duct" && spaces.Item1[3].Value == "C:\\temp",
                  "quoted: spaces kept, \"\" is blank, \\\" is a quote, a lone backslash stays - got "
                  + said(spaces.Item1));
            var tail = split("a=\"x; y\"   ; b=1");
            Check(tail.Item2 == null && said(tail.Item1) == "[a]=[x; y] [b]=[1]",
                  "spaces between the closing quote and the semicolon are allowed - got " + said(tail.Item1));
            var last = split("a=1; b=\"x;y\"");
            Check(last.Item2 == null && said(last.Item1) == "[a]=[1] [b]=[x;y]",
                  "a quoted value can be the last entry - got " + said(last.Item1));

            // REFUSED, IN WORDS: A QUOTE NEVER CLOSED, OR TEXT AFTER IT.
            var open = split("Filter_Efficiency=\"92 %; design 81 %; Filter_Cell=G4");
            Check(open.Item1 == null && open.Item2 != null
                  && open.Item2.IndexOf("'Filter_Efficiency'", StringComparison.Ordinal) >= 0
                  && open.Item2.IndexOf("never closes", StringComparison.Ordinal) >= 0,
                  "a quote opened and never closed is refused, naming the entry - got \"" + open.Item2 + "\"");
            var after = split("Size=\"12\" duct; b=1");
            Check(after.Item1 == null && after.Item2 != null
                  && after.Item2.IndexOf("'Size'", StringComparison.Ordinal) >= 0
                  && after.Item2.IndexOf("after its closing quote", StringComparison.Ordinal) >= 0,
                  "text after a closing quote is refused, not guessed at - got \"" + after.Item2 + "\"");
        }

        /// <summary>
        /// FRAGMENT-ISSUES row 5b-1120: SET_ROUTING_PREFERENCE found a fitting
        /// by its TYPE NAME alone, so "Standard" - the type name of a mitred
        /// elbow and of a balancing damper in the row's project - bound
        /// whichever the collector met first, and "Family: Type" was not a
        /// name it took.
        ///
        /// THE RULE ASKED HERE IS THE FRAGMENT'S OWN TEXT. A fragment is
        /// compiled on its own inside Revit and cannot call HeronBindingNote,
        /// so tests/test_binding_note.py copies the lines between the
        /// fragment's two PART NAMES markers, unchanged, into
        /// FragmentPartNames and the project file compiles that in. One copy:
        /// what is checked here is what runs in Revit.
        ///
        /// BY REFLECTION, for Foreign's reason. Built without the fragment's
        /// block - the fragment as it stood before the row has none - the
        /// first check fails and VERSION 1'S RULE STANDS IN, the first part
        /// whose own name is the name asked for. So the checks below still
        /// run, and the ones that fail are exactly what version 1 got wrong.
        ///
        /// WHAT THIS CANNOT SEE: which parts a project holds, and that the
        /// fragment binds only when ONE fits. The suite reads the second off
        /// the fragment's code; the first needs a Revit. The same goes for
        /// the swap rule at the end: what it answers is checked here, that
        /// the fragment commits on nothing else is read by the suite, and
        /// what Revit does to the rules needs a Revit.
        /// </summary>
        private static void Parts()
        {
            Console.WriteLine();
            Console.WriteLine("SET_ROUTING_PREFERENCE names a fitting as Family: Type (row 5b-1120)");

            var handed = Type.GetType("Heron.BindingNote.TestHost.FragmentPartNames");
            var rule = handed == null
                ? null
                : handed.GetMethod("Named", BindingFlags.NonPublic | BindingFlags.Static);
            Check(rule != null,
                  "the fragment's own part-name rule was handed over and compiled in");

            Func<IList<string>, IList<string>, string, List<int>> fits = (inFamilies, inNames, wanted) =>
            {
                if (rule != null)
                    return (List<int>)rule.Invoke(null, new object[] { inFamilies, inNames, wanted });

                // VERSION 1, kept only so the checks below run without the
                // block: the first part whose own name is the one asked for.
                var first = new List<int>();
                for (var i = 0; i < inNames.Count; i++)
                {
                    if (!string.Equals(inNames[i], wanted, StringComparison.OrdinalIgnoreCase)) continue;
                    first.Add(i);
                    break;
                }
                return first;
            };
            Func<List<int>, int, bool> only = (answer, at) =>
                answer != null && answer.Count == 1 && answer[0] == at;

            // THE ROW'S OWN PROJECT: two loaded families with a type called
            // "Standard", and a third whose type name nothing else has.
            const string Elbow = "M_Rectangular Elbow - Mitered";
            const string Damper = "M_Balancing Damper - Rectangular";
            const string Radius = "M_Rectangular Elbow - Radius";
            var families = new List<string> { Elbow, Damper, Radius };
            var names = new List<string> { "Standard", "Standard", "1.5 W" };

            Check(fits(families, names, "Standard").Count == 2,
                  "\"Standard\" fits BOTH families that have that type, so the fragment refuses - "
                  + "version 1 took the first");
            Check(only(fits(families, names, Elbow + ": Standard"), 0),
                  "\"Family: Type\" names the elbow, and only the elbow");
            Check(only(fits(families, names, Damper + ": Standard"), 1),
                  "and the damper is named the same way");
            Check(only(fits(families, names, Elbow + " : Standard"), 0),
                  "\"Family : Type\", as SELECT_TYPES prints it, names it too");
            Check(only(fits(families, names, Elbow + ":Standard"), 0),
                  "and so does no space at all");
            Check(only(fits(families, names, "m_rectangular elbow - mitered: STANDARD"), 0),
                  "capitals do not count in either half, as they never did for a name alone");
            Check(only(fits(families, names, "1.5 W"), 2),
                  "a type name only ONE family has still names it alone");
            Check(only(fits(families, names, "1.5 w"), 2),
                  "capitals aside");
            Check(fits(families, names, Elbow).Count == 0,
                  "a family's name alone names no type - the fragment lists what came close");
            Check(fits(families, names, "M_Round Elbow: Standard").Count == 0,
                  "a family that is not loaded names nothing, whatever its type is called");
            Check(fits(families, names, Elbow + ": 1.5 W").Count == 0,
                  "and nor does a loaded family with another family's type");
            Check(fits(families, names, ": Standard").Count == 0,
                  "a colon with no family before it names nothing");

            // SEGMENTS HAVE NO FAMILY, and are found as before.
            var none = new List<string> { "", "", "" };
            var segments = new List<string> { "Copper - K", "Copper - L", "Carbon Steel - Schedule 40" };
            Check(only(fits(none, segments, "Copper - K"), 0),
                  "a segment is still named by its own name");
            Check(only(fits(none, segments, "copper - l"), 1),
                  "capitals aside, as before");
            Check(fits(none, segments, "Copper").Count == 0,
                  "part of a name is still not the name - \"Copper\" is not \"Copper - K\"");
            Check(fits(none, segments, "Pipe: Copper - K").Count == 0,
                  "and a segment has no family to be named with");

            // WHAT THE BINDER TAKES FOR A TYPE, THIS TAKES FOR A FITTING - the
            // name one Heron tool hands out is never refused by this one.
            var agree = true;
            for (var i = 0; i < families.Count; i++)
            {
                foreach (var spelling in new[] { ": ", " : ", ":" })
                {
                    var text = families[i] + spelling + names[i];
                    if (!HeronBindingNote.TypeNameMatches(families[i], names[i], text)
                        || !only(fits(families, names, text), i))
                        agree = false;
                }
            }
            Check(agree,
                  "every spelling HeronBindingNote.TypeNameMatches takes for a type names that "
                  + "one fitting here");

            // ROW 5b-1549, THE SWAP THAT IS KEPT. The old rule is removed and
            // the new one added at the same index. A review of that change
            // found the read-back asked only which part the rule AT THAT INDEX
            // pointed at: where AddRule added nothing and the next rule
            // already pointed at the part asked for, that reads as asked with
            // one rule deleted. The fragment's rule now takes the count too,
            // and it is the fragment's own line that is asked here. Without
            // it, the part alone stands in - the rule as it first stood.
            var heldRule = handed == null
                ? null
                : handed.GetMethod("Held", BindingFlags.NonPublic | BindingFlags.Static);
            Check(heldRule != null,
                  "the fragment's own rule for keeping a swap was handed over and compiled in");
            Func<int, int, bool, bool> held = (rulesBefore, rulesAfter, pointsAtWanted) =>
                heldRule != null
                    ? (bool)heldRule.Invoke(null, new object[] { rulesBefore, rulesAfter, pointsAtWanted })
                    : pointsAtWanted;

            Check(held(3, 3, true),
                  "a rule that reads back as asked, in a group of as many rules as before, is kept");
            Check(!held(3, 2, true),
                  "ONE RULE FEWER is not kept, though the rule at the index reads as asked - "
                  + "the next rule already pointed there");
            Check(!held(1, 0, true) && !held(3, 4, true),
                  "nor is a group left empty, or one rule longer");
            Check(!held(3, -1, true),
                  "a count that could not be read is not a count that held");
            Check(!held(3, 3, false),
                  "and the right count with the wrong part is still not kept");
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

        /// <summary>
        /// OPEN-QUESTIONS Q-89, decided 2026-11-15: what a run that was taken
        /// back left is never handed on. TakenBack says, from how a run ended,
        /// whether it was - a dry run, a proof's trial, a Discard - and in what
        /// words; CarriedFromTakenBack is the binder's refusal. Each kind of
        /// run that was taken back is asked, and each that was not.
        ///
        /// By reflection for Foreign's reason: on the code before Q-89 was
        /// built neither rule exists, and these fail as checks, not as a build
        /// error.
        /// </summary>
        private static void TakenBack()
        {
            Console.WriteLine();
            Console.WriteLine("What a run that was taken back left is never handed on (Q-89)");

            const BindingFlags Rule = BindingFlags.NonPublic | BindingFlags.Static;
            var howRule = typeof(HeronBindingNote).GetMethod("TakenBack", Rule);
            var refuseRule = typeof(HeronBindingNote).GetMethod("CarriedFromTakenBack", Rule);

            Check(howRule != null,
                  "the chain has a rule for whether the run that left it was taken back");
            Check(refuseRule != null,
                  "the binder has a refusal for what a run that was taken back left");

            // (writing, kept, asked, keptNothing)
            Func<bool, bool, bool, bool, string> how = (writing, kept, asked, nothing) =>
                howRule == null ? "(no rule)"
                    : (string)howRule.Invoke(null, new object[] { writing, kept, asked, nothing });
            Func<string, string, string, string> refuse = (wanted, by, words) =>
                refuseRule == null ? ""
                    : (string)refuseRule.Invoke(null, new object[]
                    {
                        wanted, by, words, "Project1",
                    }) ?? "";

            // EACH KIND OF RUN THAT WAS TAKEN BACK.
            var dry = how(true, false, false, false);
            Check(!string.IsNullOrEmpty(dry) && howRule != null,
                  "a change sent without apply - a DRY RUN, or a PROOF's trial - is taken back");
            Check((dry ?? "").IndexOf("dry run", StringComparison.Ordinal) >= 0
                  && (dry ?? "").IndexOf("proof", StringComparison.Ordinal) >= 0,
                  "and the words say it was one or the other - got \"" + dry + "\"");
            var discard = how(true, false, true, false);
            Check(!string.IsNullOrEmpty(discard) && howRule != null,
                  "a change the modeller was asked about and did not keep - a DISCARD - is taken back");
            Check((discard ?? "").IndexOf("Discard", StringComparison.Ordinal) >= 0,
                  "and the words say the modeller did not keep it - got \"" + discard + "\"");

            // AND EACH THAT WAS NOT, so what it left is still handed on.
            Check(how(true, true, false, false) == null,
                  "a change Revit kept is not taken back - what it left is handed on");
            Check(how(true, true, true, false) == null,
                  "nor one the modeller kept when asked");
            Check(how(true, false, false, true) == null,
                  "nor a keep Revit counted nothing in (D-160) - the model holds what it reported");
            Check(how(false, false, false, false) == null,
                  "nor a read, which opens nothing to take back");

            // THE REFUSAL, naming what was taken back and what to run instead.
            var own = refuse("rename-views", "rename-views", dry ?? "");
            Check(own.Length > 0,
                  "an expectation naming the run that was taken back is REFUSED");
            Check(own.IndexOf("'rename-views'", StringComparison.Ordinal) >= 0
                  && own.IndexOf("'Project1'", StringComparison.Ordinal) >= 0
                  && own.IndexOf(dry ?? "(none)", StringComparison.Ordinal) >= 0,
                  "and it says which run it was, on which model, and how it was taken back - got \""
                  + own + "\"");
            Check(own.IndexOf("no longer holds", StringComparison.Ordinal) >= 0,
                  "and why: it is of a change Revit no longer holds");
            Check(own.IndexOf("again and keep it", StringComparison.Ordinal) >= 0,
                  "and what to run instead: that run again, kept");
            Check(own.IndexOf("NOTHING WAS BOUND", StringComparison.Ordinal) >= 0,
                  "and that nothing was bound, as the chain's other refusals say");

            var earlier = refuse("find-views", "rename-views", discard ?? "");
            Check(earlier.IndexOf("'find-views' again, then send this", StringComparison.Ordinal) >= 0
                  && earlier.IndexOf("again and keep it", StringComparison.Ordinal) < 0,
                  "an expectation naming an EARLIER run is told to run THAT again - the run "
                  + "taken back replaced what it left - got \"" + earlier + "\"");
            Check(refuse("Rename-Views", "rename-views", dry ?? "")
                      .IndexOf("again and keep it", StringComparison.Ordinal) >= 0,
                  "and the run is matched however the expectation spells its case, as the binder matches it");
            Check(refuse("rename-views", null, "").IndexOf("(unknown)", StringComparison.Ordinal) >= 0
                  && refuse("rename-views", null, "")
                      .IndexOf("taken back", StringComparison.Ordinal) >= 0,
                  "a run with no name, or no words, is still refused, and says so");
        }
    }
}
