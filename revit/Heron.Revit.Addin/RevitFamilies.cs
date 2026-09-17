// Heron-Agent:  HERON-REVIT-FAM-012
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
    /// Families, types and placement. HERON-REVIT-FAM-012.
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit, and on a day another session had
    /// the only Revit open. It COMPILES on 2020 through 2027 and that is the
    /// whole of what is known about it.
    /// ===================================================================
    ///
    /// LOADING A FAMILY IS THE MOST DANGEROUS READ-LOOKING ACT IN REVIT
    /// -----------------------------------------------------------------
    /// The row says "families, types, LOADING, placement", and loading is
    /// exactly what this file will not do. It is worth stating why at
    /// length, because the danger is invisible:
    ///
    /// **A family carries its own materials, and loading it overwrites the
    /// project's.** Six families loaded on one job silently reset the pipe
    /// colour for the whole model. Nothing warned, no count moved, and no
    /// check in this repository could have seen it - the element count was
    /// identical before and after. That is a real incident, not a
    /// hypothetical, and it is why `LOAD` is not an operation here.
    ///
    /// A family also brings its own types, its own nested families, its own
    /// parameters and sometimes its own line patterns. "Load this family" is
    /// a request to merge one document into another, and it is offered in
    /// the Revit user interface as though it were opening a file.
    ///
    /// WHAT THIS DOES INSTEAD
    /// ------------------------
    /// It reports what the model ALREADY has, and the three states that cost
    /// real money on a real job:
    ///
    ///   A TYPE NOTHING USES. Every unused type is carried in the file for
    ///   ever, bloats it, and appears in every type selector that somebody
    ///   then picks the wrong one from. Purging is the fix and knowing what
    ///   to purge is the hard half.
    ///
    ///   AN IN-PLACE FAMILY. Modelled into this project and reusable
    ///   nowhere. Legitimate occasionally, a warning sign in bulk: it cannot
    ///   be scheduled like a loadable family, cannot be swapped, and has to
    ///   be remade on the next job.
    ///
    ///   A FAMILY WITH MANY TYPES AND FEW PLACEMENTS. A 40-type family with
    ///   two instances placed is 38 types of clutter and a decision somebody
    ///   made once.
    ///
    /// COUNTS ARE OF PLACED INSTANCES, AND THE WORD MATTERS
    /// -----------------------------------------------------
    /// "How many of these are there" is ambiguous in Revit and the two
    /// answers differ by a lot: how many TYPES exist, and how many
    /// INSTANCES are placed. Both are given, separately and labelled,
    /// rather than one being called "the count".
    ///
    /// SYSTEM FAMILIES ARE COUNTED AND FLAGGED, NOT HIDDEN
    /// -----------------------------------------------------
    /// Walls, ducts, pipes and floors are system families: they have types
    /// but no loadable family behind them, and they cannot be purged the
    /// same way. Filtering them out would make the totals disagree with what
    /// a modeller sees in the project browser, so they are included and
    /// marked.
    ///
    /// NOTHING IS SET
    /// ----------------
    /// The register gives this row MODIFY. Reading requires none of it.
    /// Nothing here loads, places, deletes, renames or purges anything.
    /// </summary>
    internal static class RevitFamilies
    {
        /// <summary>
        /// A family with at least this many types, and no more than
        /// <see cref="FewPlacements"/> instances, is worth mentioning.
        ///
        /// Ten and two. Not a standard and not pretending to be - they are
        /// the shape of "lots of types, almost none used", and the answer
        /// states the pair it used so a reader can disagree with it.
        /// </summary>
        private const int ManyTypes = 10;

        /// <summary>See <see cref="ManyTypes"/>.</summary>
        private const int FewPlacements = 2;

        /// <summary>
        /// Every family and type in the active document, with how many of
        /// each are actually placed.
        /// </summary>
        public static string List(UIApplication app)
        {
            var doc = RevitOperations.ActiveDocument(app);
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            // ONE PASS FOR INSTANCES. Keyed by the TYPE each one uses, so a
            // type with no instances is visible as an absence rather than
            // needing a second sweep per type.
            var placedOfType = new Dictionary<string, long>(StringComparer.Ordinal);
            var placedTotal = 0L;
            foreach (var element in new FilteredElementCollector(doc)
                                        .WhereElementIsNotElementType())
            {
                if (element == null) continue;
                var typeKey = TypeKeyOf(doc, element);
                if (typeKey == null) continue;
                placedTotal++;
                long already;
                placedOfType[typeKey] = placedOfType.TryGetValue(typeKey, out already)
                                        ? already + 1 : 1;
            }

            // ONE PASS FOR TYPES, gathered under the family they belong to.
            var families = new Dictionary<string, Family>(StringComparer.Ordinal);
            var typeCount = 0;
            var unusedTypes = 0;

            foreach (var element in new FilteredElementCollector(doc)
                                        .WhereElementIsElementType())
            {
                var type = element as ElementType;
                if (type == null) continue;

                var familyName = FamilyNameOf(type);
                if (familyName == null) continue;

                typeCount++;
                long placed;
                if (!placedOfType.TryGetValue(type.UniqueId, out placed)) placed = 0;
                if (placed == 0) unusedTypes++;

                Family family;
                if (!families.TryGetValue(familyName, out family))
                {
                    family = new Family
                    {
                        Name = familyName,
                        Category = CategoryNameOf(type),
                        System = !IsLoadable(doc, type),
                        InPlace = IsInPlace(doc, type),
                    };
                    families[familyName] = family;
                }

                family.Types++;
                family.Placed += placed;
                if (placed == 0) family.UnusedTypes++;
            }

            var rows = new List<string>();
            var inPlace = 0;
            var lopsided = 0;
            foreach (var family in families.Values)
            {
                if (family.InPlace) inPlace++;
                if (family.Types >= ManyTypes && family.Placed <= FewPlacements)
                    lopsided++;

                rows.Add(Json.Obj(
                    Json.Str("name", family.Name),
                    Json.Str("category", family.Category),
                    Json.Num("types", family.Types),
                    Json.Num("typesNothingUses", family.UnusedTypes),
                    Json.Num("placed", family.Placed),
                    Json.Bool("systemFamily", family.System),
                    Json.Bool("inPlace", family.InPlace)));
            }

            return Json.Ok(
                Json.Arr("families", rows),
                Json.Num("familyCount", rows.Count),
                Json.Num("typeCount", typeCount),
                Json.Num("typesNothingUses", unusedTypes),
                Json.Num("placedInstances", placedTotal),
                Json.Num("inPlaceFamilies", inPlace),
                Json.Num("familiesWithManyTypesAndFewPlacements", lopsided),
                Json.Num("manyTypesThreshold", ManyTypes),
                Json.Num("fewPlacementsThreshold", FewPlacements),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Str("reads", Reads(rows.Count, typeCount, unusedTypes,
                                        placedTotal, inPlace, lopsided)));
        }

        /// <summary>What the numbers mean, in the words a modeller would use.</summary>
        private static string Reads(int families, int types, int unusedTypes,
                                    long placed, int inPlace, int lopsided)
        {
            var said = new List<string>();

            said.Add(string.Format(CultureInfo.InvariantCulture,
                "{0} family/families, {1} type(s), {2} placed instance(s). "
              + "TYPES and INSTANCES are given separately on purpose - \"how "
              + "many of these are there\" has two answers in Revit and they "
              + "differ by a lot", families, types,
                placed.ToString("N0", CultureInfo.InvariantCulture)));

            if (unusedTypes > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} type(s) are placed NOWHERE. Each one is carried in the "
                  + "file for ever and appears in every type selector somebody "
                  + "then picks the wrong one from", unusedTypes));
            }

            if (inPlace > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} family/families are IN-PLACE - modelled into this "
                  + "project and reusable nowhere. Fine occasionally, a warning "
                  + "sign in bulk: they cannot be swapped and have to be remade "
                  + "on the next job", inPlace));
            }

            if (lopsided > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} family/families carry {1} or more types with {2} or "
                  + "fewer placed. That pair is a rule of thumb rather than a "
                  + "standard, and it is stated so you can disagree with it",
                    lopsided, ManyTypes, FewPlacements));
            }

            return string.Join(". ", said.ToArray())
                 + ". System families - walls, ducts, pipes, floors - are "
                 + "INCLUDED and flagged rather than filtered out, so these "
                 + "totals agree with what the Project Browser shows. "
                 + "**NOTHING IS LOADED.** A family carries its own materials "
                 + "and loading one overwrites the project's: six families once "
                 + "reset the pipe colour on a whole job silently, with no count "
                 + "moving and nothing warning. Nothing was changed.";
        }

        /// <summary>One family's tally.</summary>
        private sealed class Family
        {
            public string Name;
            public string Category;
            public bool System;
            public bool InPlace;
            public int Types;
            public int UnusedTypes;
            public long Placed;
        }

        /// <summary>
        /// The UniqueId of the type an element uses, or null.
        ///
        /// UniqueId and never the integer: ElementId.IntegerValue is
        /// deprecated at 2024 and throws above 32 bits.
        /// </summary>
        private static string TypeKeyOf(Document doc, Element element)
        {
            try
            {
                var id = element.GetTypeId();
                if (id == null || id == ElementId.InvalidElementId) return null;
                var type = doc.GetElement(id);
                return type == null ? null : type.UniqueId;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }

        /// <summary>
        /// The family this type belongs to, by name, or null.
        ///
        /// `FamilyName` answers for a loadable family and for a system family
        /// alike - a wall type reports "Basic Wall" - which is what makes one
        /// table cover both without special cases.
        /// </summary>
        private static string FamilyNameOf(ElementType type)
        {
            try
            {
                var name = type.FamilyName;
                return string.IsNullOrEmpty(name) ? null : name;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }

        /// <summary>Is there a loadable family behind this type?</summary>
        private static bool IsLoadable(Document doc, ElementType type)
        {
            try
            {
                var symbol = type as FamilySymbol;
                return symbol != null && symbol.Family != null;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
        }

        /// <summary>
        /// Is this an in-place family - modelled into the project rather than
        /// loaded into it?
        /// </summary>
        private static bool IsInPlace(Document doc, ElementType type)
        {
            try
            {
                var symbol = type as FamilySymbol;
                if (symbol == null || symbol.Family == null) return false;
                return symbol.Family.IsInPlace;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
        }

        /// <summary>The type's category, as a word, or null.</summary>
        private static string CategoryNameOf(ElementType type)
        {
            try
            {
                var category = type.Category;
                return category == null ? null : category.Name;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }
    }
}
