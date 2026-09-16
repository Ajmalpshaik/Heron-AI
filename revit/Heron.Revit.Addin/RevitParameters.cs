// Heron-Agent:  HERON-REVIT-PAR-011
// Heron-Step:   4
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Heron.Bridge;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Instance, type and shared parameters - read.
    /// HERON-REVIT-PAR-011.
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit. It COMPILES on 2020 through 2027
    /// and that is the whole of what is known about it. Nothing below has
    /// been in front of a model.
    /// ===================================================================
    ///
    /// WHY A MODELLER CARES, IN ONE SENTENCE
    /// --------------------------------------
    /// Before a schedule, an IFC export or a QA hand-over the question is
    /// always the same - "which of these are actually filled in?" - and the
    /// only way to answer it today is to make a throwaway schedule, sort by
    /// the column and count the blanks by eye.
    ///
    /// THE THREE WAYS THIS ANSWER GOES WRONG, AND WHAT IS DONE ABOUT EACH
    /// ===================================================================
    ///
    /// 1. READING ONLY THE INSTANCE. Fire Rating, Assembly Code, and most
    ///    classification and fire data live on the TYPE. Ask 340 doors for
    ///    a parameter that sits on their type and every one answers "not
    ///    here" - "0 of 340 filled in", confident, formatted, and
    ///    completely wrong. That is D-52's plausible zero with a schedule's
    ///    face on it. So BOTH are read, and every row says which it came
    ///    from.
    ///
    /// 2. A BARE NUMBER. Revit holds lengths in decimal feet whatever the
    ///    project is set to. A duct width handed back as `0.656` is read as
    ///    millimetres by the next person who sees it and is out by a factor
    ///    of five hundred. So a value is reported as Revit's OWN formatted
    ///    string, in the project's units, and a raw double never appears
    ///    without `internalUnconverted` sitting beside it on the same
    ///    object. Converting is HERON-REVIT-UNI-035's job, not this one's.
    ///
    /// 3. TWO PARAMETERS WITH ONE NAME. A built-in "Comments" and a shared
    ///    "Comments" can both sit on one element. Picking whichever came
    ///    back first is a coin toss that reads like a fact. So when a name
    ///    answers more than once on one element, every match is reported
    ///    and the row is marked `ambiguous` - the question was bad, and
    ///    saying so beats answering half of it.
    ///
    /// "EMPTY" IS THREE DIFFERENT THINGS AND A SCHEDULE SHOWS ALL THREE BLANK
    /// ----------------------------------------------------------------------
    ///   absent   - the element has no parameter by that name at all
    ///   noValue  - it is there, and Revit reports nothing set
    ///   blank    - it is set, and what it is set to formats to nothing
    /// They are counted apart because they are fixed differently: the first
    /// is a missing project parameter or the wrong family, the second is
    /// data entry, and the third is usually a space somebody typed.
    ///
    /// WHAT THIS DOES NOT DO
    /// ----------------------
    /// It does not write. The register gives this row MODIFY, which its own
    /// column header defines as "the HIGHEST permission level it can
    /// require"; reading requires none of it, and the first parameter write
    /// from a machine that has never opened Revit is not something this
    /// file will attempt. Nothing here opens a transaction.
    ///
    /// It does not report a parameter's data type. `Definition.ParameterType`
    /// is NOT IN 2023 and `Definition.GetDataType()` is not in 2020, so no
    /// one member answers across the eight releases this project supports,
    /// and D-05 does not extrapolate. `storageType` is reported instead -
    /// present on all eight - and it is the narrower fact: how the value is
    /// held, not what it means.
    /// </summary>
    internal static class RevitParameters
    {
        /// <summary>
        /// Neither one row per parameter name nor one row per element can be
        /// unbounded. Past this many the answer is truncated and says by how
        /// much, rather than filling the pipe with rows nobody will read.
        /// </summary>
        private const int MaxRows = 500;

        /// <summary>
        /// The parameters of everything in a category - either a coverage
        /// summary across the whole category, or the values of one named
        /// parameter element by element.
        /// </summary>
        public static string Read(UIApplication app, string category,
                                  string parameter, string expectProject)
        {
            BuiltInCategory builtIn;
            var unknown = RevitOperations.ResolveCategory(category, out builtIn);
            if (unknown != null) return unknown;

            var doc = RevitOperations.ActiveDocument(app);
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            // THE SAME GUARD select_by_category CARRIES. A reply about the
            // wrong model reads exactly like a reply about the right one.
            if (!string.IsNullOrEmpty(expectProject))
            {
                var here = RevitOperations.ProjectKey(doc);
                if (here != expectProject)
                {
                    return Json.Error("wrong_document",
                        "This chat has been working on another model, and the "
                        + "one in front of Revit now is \"" + doc.Title
                        + "\". NOTHING was read, because a parameter report "
                        + "from the wrong model reads exactly like one from "
                        + "the right model.");
                }
            }

            var wanted = parameter == null ? null : parameter.Trim();
            if (wanted != null && wanted.Length == 0) wanted = null;

            var found = new List<Element>();
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfCategory(builtIn)
                                        .WhereElementIsNotElementType())
            {
                if (element != null) found.Add(element);
            }

            return wanted == null
                ? Coverage(doc, found, category)
                : Values(doc, found, category, wanted);
        }

        // =================================================================
        // SHAPE ONE - no parameter named. "Which of these are filled in?"
        // =================================================================

        /// <summary>
        /// One row per (name, where) across the category: how many elements
        /// carry it, and how many of those have something in it.
        /// </summary>
        private static string Coverage(Document doc, List<Element> found,
                                       string category)
        {
            var seen = new Dictionary<string, Tally>(StringComparer.Ordinal);
            var order = new List<string>();
            var types = new Dictionary<string, Element>(StringComparer.Ordinal);

            foreach (var element in found)
            {
                Count(element, "instance", seen, order);

                // THE TYPE IS COLLECTED HERE AND COUNTED BELOW, ONCE EACH.
                // Counting it inside this loop would read the same four duct
                // types five thousand times - the difference between an
                // answer and a hang - and would make a type row say 5,000
                // where the honest figure is 4.
                var type = TypeOf(doc, element);
                if (type == null) continue;
                var key = type.UniqueId;
                if (!string.IsNullOrEmpty(key)) types[key] = type;
            }

            foreach (var type in types.Values)
            {
                Count(type, "type", seen, order);
            }

            // TYPE ROWS ARE WRITTEN FIRST, and that is about the cap rather
            // than about importance. `order` is filled instance-first, so a
            // model with five hundred distinct instance parameter names
            // would truncate away EVERY type row - the exact rows this
            // agent exists to surface, gone, with a `notListed` figure that
            // does not say which kind went. Type rows are the smaller group
            // on any real category, so putting them first costs the
            // instance rows almost nothing and cannot lose a whole kind.
            var listing = new List<string>();
            foreach (var key in order) if (key[0] == 't') listing.Add(key);
            foreach (var key in order) if (key[0] != 't') listing.Add(key);

            var droppedType = 0;
            var droppedInstance = 0;

            var rows = new List<string>();
            foreach (var key in listing)
            {
                if (rows.Count >= MaxRows)
                {
                    if (key[0] == 't') droppedType++; else droppedInstance++;
                    continue;
                }
                var tally = seen[key];
                rows.Add(Json.Obj(
                    Json.Str("name", tally.Name),
                    Json.Str("where", tally.Where),
                    Json.Str("storageType", tally.Storage),
                    Json.Bool("readOnly", tally.ReadOnly),
                    Json.Bool("shared", tally.Shared),
                    Json.Num("onElements", tally.On),
                    Json.Num("withAValue", tally.WithAValue),
                    Json.Num("noValue", tally.NoValue),
                    Json.Num("blank", tally.Blank),
                    // ABOVE 1 AND THE NAME IS NOT A WAY TO ASK. More than
                    // one parameter on a single element answered to it, so
                    // "set Comments to X" has two meanings.
                    Json.Num("sameNameOnOneElement", tally.MostOnOne)));
            }

            return Json.Ok(
                Json.Arr("parameters", rows),
                Json.Str("category", category.Trim()),
                Json.Num("elements", found.Count),
                Json.Num("distinctParameters", order.Count),
                Json.Num("listed", rows.Count),
                Json.Num("notListed", order.Count - rows.Count),
                // WHICH KIND WENT, not just how many. A truncated answer
                // that does not say what it dropped is the one a reader
                // trusts by mistake.
                Json.Num("notListedInstance", droppedInstance),
                Json.Num("notListedType", droppedType),
                Json.Num("typesRead", types.Count),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Str("scope", "the whole model, not just the active view"),
                Json.Str("reads",
                    "ONE ROW PER PARAMETER NAME, NOT PER ELEMENT, and `where` "
                    + "says whether it sits on the instance or on the type - "
                    + "the same name can appear twice, once as each, and they "
                    + "are two different parameters. `withAValue` plus "
                    + "`noValue` plus `blank` is `onElements`: a schedule "
                    + "prints the last two identically and they are not the "
                    + "same fault. `onElements` COUNTS TYPES on a type row, "
                    + "so 4 against 900 doors means four door types, not four "
                    + "doors. `sameNameOnOneElement` above 1 means the name "
                    + "is ambiguous on this model and asking by it would hit "
                    + "whichever Revit returned first. No data type is "
                    + "reported: no single member answers for that across "
                    + "2020 to 2027, and D-05 does not extrapolate one."));
        }

        /// <summary>Fold one element's parameters into the tallies.</summary>
        private static void Count(Element element, string where,
                                  Dictionary<string, Tally> seen,
                                  List<string> order)
        {
            var onThisElement = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var p in Safe(element))
            {
                if (p == null) continue;

                var name = NameOf(p);
                if (name == null) continue;

                // `where` is this file's own literal and never user data, so
                // a fixed two-character prefix cannot collide with anything
                // a parameter name could contain.
                var key = (where == "type" ? "t:" : "i:") + name;

                Tally tally;
                if (!seen.TryGetValue(key, out tally))
                {
                    tally = new Tally(name, where, StorageOf(p),
                                      IsReadOnly(p), IsShared(p));
                    seen[key] = tally;
                    order.Add(key);
                }

                int already;
                onThisElement.TryGetValue(key, out already);
                onThisElement[key] = already + 1;

                // THE SECOND PARAMETER OF THIS NAME ON THIS ELEMENT IS NOT
                // COUNTED AGAIN. It is recorded as a clash below, and that
                // is all. Counting it would put the element in two state
                // buckets at once and break the arithmetic this answer
                // states out loud - withAValue plus noValue plus blank IS
                // onElements, and a reader who cannot add the columns up
                // has no way to tell which figure to trust.
                //
                // Which of the two is counted matches what the by-name
                // shape reports - the first Revit hands back - so the two
                // answers cannot contradict each other about one element.
                if (already > 0) continue;

                tally.On++;

                var state = StateOf(p);
                if (state == "value") tally.WithAValue++;
                else if (state == "noValue") tally.NoValue++;
                else tally.Blank++;
            }

            // TWO PARAMETERS, ONE NAME, ONE ELEMENT. Recorded per element
            // and kept as the worst case, because one element carrying the
            // clash is enough to make the name unusable as a question.
            foreach (var pair in onThisElement)
            {
                var tally = seen[pair.Key];
                if (pair.Value > tally.MostOnOne) tally.MostOnOne = pair.Value;
            }
        }

        // =================================================================
        // SHAPE TWO - one parameter named. "What does it say, element by
        // element?"
        // =================================================================

        private static string Values(Document doc, List<Element> found,
                                     string category, string wanted)
        {
            var rows = new List<string>();

            // THE ANSWER IS CACHED, NOT THE TYPE. Fetching a type is a
            // lookup; reading its forty parameters is not, and doing that
            // once per element is how five thousand ducts become a hang.
            // What is kept is the FINDING - which parameters on this type
            // answer to this name - because that is the part that costs.
            var byType = new Dictionary<string, List<Parameter>>(StringComparer.Ordinal);

            var withAValue = 0;
            var noValue = 0;
            var blank = 0;
            var absent = 0;
            var ambiguous = 0;
            var matched = 0;

            foreach (var element in found)
            {
                var hits = new List<Parameter>();
                Match(element, wanted, hits);
                var where = "instance";

                // THE TYPE IS ASKED ONLY WHEN THE INSTANCE DID NOT CARRY
                // THE NAME. Not because the type matters less, but because
                // an instance parameter overrides nothing - if both exist,
                // the one on the element is the one the modeller edits and
                // the one a schedule of instances shows.
                if (hits.Count == 0)
                {
                    hits = OnTheType(doc, element, wanted, byType);
                    where = "type";
                }

                if (hits.Count == 0) { absent++; continue; }

                matched++;
                if (hits.Count > 1) ambiguous++;

                var state = StateOf(hits[0]);
                if (state == "value") withAValue++;
                else if (state == "noValue") noValue++;
                else blank++;

                if (rows.Count >= MaxRows) continue;

                var shown = new List<string>();
                foreach (var p in hits) shown.Add(One(doc, p));

                rows.Add(Json.Obj(
                    Json.Str("id", element.UniqueId),
                    Json.Str("element", SafeName(element)),
                    Json.Str("where", where),
                    Json.Str("level", LevelOf(doc, element)),
                    Json.Str("state", state),
                    // MORE THAN ONE ENTRY HERE MEANS THE NAME MATCHED MORE
                    // THAN ONE PARAMETER. Every match is shown rather than
                    // the first, because choosing would be a guess wearing
                    // a fact's clothes.
                    Json.Bool("ambiguous", hits.Count > 1),
                    Json.Arr("matches", shown)));
            }

            return Json.Ok(
                Json.Arr("elements", rows),
                Json.Str("category", category.Trim()),
                Json.Str("parameter", wanted),
                Json.Num("examined", found.Count),
                Json.Num("matched", matched),
                Json.Num("withAValue", withAValue),
                Json.Num("noValue", noValue),
                Json.Num("blank", blank),
                Json.Num("withoutTheParameter", absent),
                Json.Num("ambiguousElements", ambiguous),
                Json.Num("listed", rows.Count),
                Json.Num("notListed", matched - rows.Count),
                Json.Num("typesRead", byType.Count),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Str("scope", "the whole model, not just the active view"),
                Json.Str("reads",
                    "THE INSTANCE WAS ASKED FIRST AND THE TYPE ONLY WHERE THE "
                    + "INSTANCE DID NOT CARRY THE NAME, so `where` says which "
                    + "answered. An element counted in `withoutTheParameter` "
                    + "has it on neither - usually a different family, or a "
                    + "project parameter never bound to this category, rather "
                    + "than data somebody forgot to type. `value` is REVIT'S "
                    + "OWN formatting in the project's units; "
                    + "`internalUnconverted` is the raw number Revit holds, "
                    + "which for a length is decimal feet whatever the "
                    + "project is set to - it never travels without that "
                    + "field beside it, and converting it belongs to "
                    + "HERON-REVIT-UNI-035. Where Revit would not format a "
                    + "number at all, `value` is null rather than a bare "
                    + "figure. `ambiguous` means the name matched more than "
                    + "one parameter on that element, so writing by this name "
                    + "would hit whichever Revit returned first."));
        }

        /// <summary>One parameter, as far as it can be reported honestly.</summary>
        private static string One(Document doc, Parameter p)
        {
            var storage = StorageOf(p);
            var fields = new List<string>();
            fields.Add(Json.Str("storageType", storage));
            fields.Add(Json.Bool("readOnly", IsReadOnly(p)));
            fields.Add(Json.Bool("shared", IsShared(p)));
            fields.Add(Json.Str("value", Formatted(doc, p, storage)));

            // A RAW DOUBLE NEVER TRAVELS ALONE. It goes out with the field
            // that says it has not been converted, on the same object, so
            // there is no arrangement in which a reader gets the number
            // without the warning.
            if (storage == "Double")
            {
                double raw;
                if (TryDouble(p, out raw))
                {
                    fields.Add(Json.Str("internalUnconverted",
                        raw.ToString("R", CultureInfo.InvariantCulture)));
                }
            }

            return Json.Obj(fields.ToArray());
        }

        /// <summary>
        /// Every parameter on this element answering to this name.
        ///
        /// Case-insensitive, because a modeller types "fire rating" and the
        /// parameter is "Fire Rating"; refusing over that would be a refusal
        /// about typing rather than about the model.
        /// </summary>
        private static void Match(Element element, string wanted,
                                  List<Parameter> into)
        {
            foreach (var p in Safe(element))
            {
                if (p == null) continue;
                var name = NameOf(p);
                if (name == null) continue;
                if (string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    into.Add(p);
                }
            }
        }

        // =================================================================
        // Asking Revit, never an exception mid-answer
        // =================================================================

        private static IEnumerable<Parameter> Safe(Element element)
        {
            ParameterSet set = null;
            try { set = element.Parameters; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { set = null; }
            if (set == null) yield break;
            foreach (Parameter one in set) yield return one;
        }

        /// <summary>The element's type, or null. No caching - this is a lookup.</summary>
        private static Element TypeOf(Document doc, Element element)
        {
            try
            {
                var typeId = element.GetTypeId();
                if (typeId == null || typeId == ElementId.InvalidElementId) return null;
                return doc.GetElement(typeId);
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return null;
            }
        }

        /// <summary>
        /// The parameters on this element's TYPE answering to this name,
        /// worked out once per type rather than once per element.
        ///
        /// Keyed by the type's UniqueId because that is a string by
        /// contract. ElementId is not, and `ElementId.IntegerValue` - the
        /// obvious way to get a number out of one - is NOT IN 2026.
        ///
        /// An EMPTY list is cached as readily as a full one. A type that
        /// does not carry the parameter is the common case on a real model,
        /// and a cache that only remembers the hits re-reads every type that
        /// misses - which is most of them, once per element.
        ///
        /// The cache lives for ONE answer and is passed in rather than held
        /// in a field, for the reason RevitOperations.ActiveDocument gives:
        /// nothing read out of a Document outlives the call that read it.
        ///
        /// WHAT COMES BACK IS THE CACHED LIST ITSELF, not a copy. Every
        /// caller reads it and none adds to it; one that did would be
        /// editing the answer for every other element of the same type.
        /// </summary>
        private static List<Parameter> OnTheType(Document doc, Element element,
                                                 string wanted,
                                                 Dictionary<string, List<Parameter>> cache)
        {
            var hits = new List<Parameter>();

            var type = TypeOf(doc, element);
            if (type == null) return hits;

            var key = type.UniqueId;
            if (string.IsNullOrEmpty(key))
            {
                Match(type, wanted, hits);
                return hits;
            }

            List<Parameter> already;
            if (cache.TryGetValue(key, out already)) return already;

            Match(type, wanted, hits);
            cache[key] = hits;
            return hits;
        }

        /// <summary>
        /// "value", "noValue" or "blank" - and they are not the same fault.
        /// </summary>
        private static string StateOf(Parameter p)
        {
            try
            {
                if (!p.HasValue) return "noValue";
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return "noValue";
            }

            var storage = StorageOf(p);

            // A STRING PARAMETER SET TO A SPACE reports HasValue true and
            // schedules blank. Counting it as filled in is how a hand-over
            // passes a completeness check it should have failed.
            if (storage == "String")
            {
                string text = null;
                try { text = p.AsString(); }
                catch (Autodesk.Revit.Exceptions.ApplicationException) { text = null; }
                if (text == null || text.Trim().Length == 0) return "blank";
                return "value";
            }

            if (storage == "ElementId")
            {
                // AN ELEMENTID PARAMETER POINTING AT NOTHING is the same
                // blank as an empty string - the material, the level or the
                // type it should have named was never set.
                ElementId id = null;
                try { id = p.AsElementId(); }
                catch (Autodesk.Revit.Exceptions.ApplicationException) { id = null; }
                if (id == null || id == ElementId.InvalidElementId) return "blank";
                return "value";
            }

            return "value";
        }

        /// <summary>
        /// The value as Revit itself would print it, in the project's units.
        ///
        /// `AsValueString` is asked FIRST for every storage type, because it
        /// is the only one that applies the project's unit settings and the
        /// only one whose answer a modeller would recognise from a schedule.
        ///
        /// WHEN IT GIVES NOTHING FOR A DOUBLE, NULL IS RETURNED AND NO
        /// NUMBER IS SUBSTITUTED. `AsDouble()` would answer - in decimal
        /// feet, whatever the project is set to - and a length reported as
        /// 0.656 with no unit anywhere near it is read as millimetres by the
        /// next person to see it. Nothing here is willing to be the place
        /// that produces that number.
        /// </summary>
        private static string Formatted(Document doc, Parameter p, string storage)
        {
            try
            {
                var shown = p.AsValueString();
                if (!string.IsNullOrEmpty(shown)) return shown;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                // fall through to the storage-specific readings
            }

            if (storage == "String")
            {
                try { return p.AsString(); }
                catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
            }

            if (storage == "Integer")
            {
                // AN INTEGER CARRIES NO UNIT, so printing it is safe in a
                // way printing a double is not. Yes/No parameters are held
                // as integers, and Revit formats those itself above.
                try
                {
                    return p.AsInteger().ToString(CultureInfo.InvariantCulture);
                }
                catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
            }

            if (storage == "ElementId")
            {
                try
                {
                    var id = p.AsElementId();
                    if (id == null || id == ElementId.InvalidElementId) return null;
                    var pointed = doc.GetElement(id);
                    return pointed == null ? null : SafeName(pointed);
                }
                catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
            }

            return null;
        }

        private static bool TryDouble(Parameter p, out double value)
        {
            value = 0;
            try { value = p.AsDouble(); return true; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
        }

        private static string NameOf(Parameter p)
        {
            try
            {
                var definition = p.Definition;
                if (definition == null) return null;
                var name = definition.Name;
                return string.IsNullOrEmpty(name) ? null : name;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return null;
            }
        }

        /// <summary>
        /// How the value is held - NOT what it means.
        ///
        /// Reported as the narrower fact on purpose: `Definition.ParameterType`
        /// is not in 2023 and `Definition.GetDataType()` is not in 2020, so
        /// the wider one cannot be answered across the eight releases without
        /// extrapolating, and D-05 forbids that.
        /// </summary>
        private static string StorageOf(Parameter p)
        {
            try
            {
                switch (p.StorageType)
                {
                    case StorageType.Double: return "Double";
                    case StorageType.Integer: return "Integer";
                    case StorageType.String: return "String";
                    case StorageType.ElementId: return "ElementId";
                    default: return "None";
                }
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return "None";
            }
        }

        private static bool IsReadOnly(Parameter p)
        {
            try { return p.IsReadOnly; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return true; }
        }

        private static bool IsShared(Parameter p)
        {
            try { return p.IsShared; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
        }

        /// <summary>
        /// The level an element sits on, or null.
        ///
        /// A modeller chasing an empty parameter needs to know WHERE, and a
        /// name on its own sends them looking through the whole tower.
        /// </summary>
        private static string LevelOf(Document doc, Element element)
        {
            try
            {
                var id = element.LevelId;
                if (id == null || id == ElementId.InvalidElementId) return null;
                var level = doc.GetElement(id);
                return level == null ? null : SafeName(level);
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return null;
            }
        }

        /// <summary>A name, never an exception mid-answer.</summary>
        private static string SafeName(Element element)
        {
            try
            {
                return string.IsNullOrEmpty(element.Name) ? "(unnamed)" : element.Name;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return "(unnamed)";
            }
        }

        /// <summary>One parameter name, counted across a category.</summary>
        private sealed class Tally
        {
            public readonly string Name;
            public readonly string Where;
            public readonly string Storage;
            public readonly bool ReadOnly;
            public readonly bool Shared;

            public int On;
            public int WithAValue;
            public int NoValue;
            public int Blank;
            public int MostOnOne = 1;

            public Tally(string name, string where, string storage,
                         bool readOnly, bool shared)
            {
                Name = name;
                Where = where;
                Storage = storage;
                ReadOnly = readOnly;
                Shared = shared;
            }
        }
    }
}
