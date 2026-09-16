// Heron-Agent:  HERON-REVIT-GRP-033
// Heron-Step:   4
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Heron.Bridge;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Groups and assemblies - read.
    /// HERON-REVIT-GRP-033.
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit. It COMPILES on 2020 through 2027
    /// and that is the whole of what is known about it. Nothing below has
    /// been in front of a model.
    /// ===================================================================
    ///
    /// WHY A MODELLER CARES, IN ONE SENTENCE
    /// --------------------------------------
    /// Edit one duct inside a group and you have edited it in every place
    /// that group is put - which is the point of groups, and is a nasty
    /// surprise when you did not know the duct was in one.
    ///
    /// THIS TURNS AN EXISTING GUESS INTO A FACT
    /// ------------------------------------------
    /// `RevitWrite` already survives the case. When a move is asked for and
    /// a member does not shift, it compares positions either side rather
    /// than trusting that the call returned cleanly - because Revit's own
    /// `MoveElements` moves nothing for a group member and raises NO
    /// exception and NO warning, so "it did not throw" would have been
    /// reported as "it moved". That is `E10` in NEEDS-CHECKING, and it was
    /// proved against a real model.
    ///
    /// What it says afterwards is *"almost certainly inside a group"*. This
    /// agent is how that stops being a guess, and stops being afterwards:
    /// ask before the move and the answer names the group, the type, and
    /// how many placements share it.
    ///
    /// A GROUP MEMBER IS NOT PINNED, which is why the pinned-element skip
    /// does not catch one, and why `pinned` is reported beside `inAGroup`
    /// rather than folded into it. Two different reasons an edit will not
    /// land, and telling them apart is the difference between unpinning
    /// something and ungrouping it.
    ///
    /// THE NUMBER THAT IS NOT MULTIPLIED, AND WHY
    /// --------------------------------------------
    /// For an element in a group, `placementsOfItsGroup` is how many times
    /// that group's TYPE is placed - the number of other spots an edit
    /// reaches.
    ///
    /// When groups are NESTED, the honest answer is the chain and not a
    /// product. Whether an inner group's placement count already includes
    /// the copies carried inside an outer group's placements, or whether
    /// the two multiply, is a question about Revit that no compiler settles
    /// and this machine has no model to settle it on. So the whole chain is
    /// reported, innermost first, with each level's own count, and nothing
    /// is multiplied. A wrong multiplier would be D-52 exactly: a number
    /// nobody would think to doubt.
    ///
    /// WHAT IS ASSERTED ABOUT GROUPS AND WHAT IS NOT ASSERTED ABOUT ASSEMBLIES
    /// -----------------------------------------------------------------------
    /// That editing a group member reaches every instance of its type is
    /// stated plainly - it is what groups are for, and this repository's
    /// own `select-group-members` fragment already says it where the set is
    /// produced.
    ///
    /// Assemblies are REPORTED and not explained. Membership is a fact this
    /// reads off the element; whether an edit inside one assembly travels
    /// to another of the same type is a question nobody here has put to
    /// Revit, and inventing an answer for symmetry would be worse than the
    /// gap. Group S in NEEDS-CHECKING is where both get settled: S9 for
    /// the assembly question, S7 for whether nested counts multiply.
    ///
    /// Nothing here opens a transaction. The register gives this row
    /// MODIFY, which its own column header defines as "the HIGHEST
    /// permission level it can require"; reading requires none of it, and
    /// ungrouping anything from a machine that has never opened Revit is
    /// not something this file will attempt.
    /// </summary>
    internal static class RevitGroups
    {
        /// <summary>
        /// Past this many rows the answer is truncated and says by how
        /// much, rather than filling the pipe with rows nobody will read.
        /// </summary>
        private const int MaxRows = 500;

        /// <summary>
        /// A group nested inside a group inside a group is ordinary. A
        /// chain longer than this is not, and walking one forever because
        /// of a loop nobody expected is worse than stopping and saying so.
        /// </summary>
        private const int MaxDepth = 16;

        /// <summary>
        /// Every group and assembly in the model, or - with a category -
        /// which of those elements sit inside one and what an edit reaches.
        /// </summary>
        public static string List(UIApplication app, string category,
                                  string expectProject)
        {
            var wanted = category == null ? null : category.Trim();
            if (wanted != null && wanted.Length == 0) wanted = null;

            BuiltInCategory builtIn = BuiltInCategory.INVALID;
            if (wanted != null)
            {
                var unknown = RevitOperations.ResolveCategory(wanted, out builtIn);
                if (unknown != null) return unknown;
            }

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
                        + "\". NOTHING was read. Answering about groups in the "
                        + "wrong model is how an edit gets approved against the "
                        + "wrong count of placements.");
                }
            }

            return wanted == null
                ? Inventory(doc)
                : InsideOne(doc, builtIn, wanted);
        }

        // =================================================================
        // SHAPE ONE - no category. "What groups does this model have?"
        // =================================================================

        private static string Inventory(Document doc)
        {
            var rows = new List<string>();
            var placed = 0;
            var unplaced = 0;

            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(GroupType)))
            {
                var groupType = element as GroupType;
                if (groupType == null) continue;

                var instances = Instances(groupType);
                if (instances.Count == 0) unplaced++; else placed++;

                // MEMBERS ARE COUNTED OFF ONE INSTANCE, because every
                // instance of a group type holds the same content - that is
                // what makes it a type. A definition with nothing placed
                // has no instance to count off, and null says so rather
                // than reporting a zero that means "unknown".
                var members = instances.Count == 0
                    ? -1 : MemberCount(doc, instances[0]);

                var fields = new List<string>();
                fields.Add(Json.Str("name", SafeName(groupType)));
                fields.Add(Json.Str("kind", KindOf(groupType)));
                fields.Add(Json.Num("placements", instances.Count));
                if (members >= 0) fields.Add(Json.Num("members", members));

                if (rows.Count < MaxRows) rows.Add(Json.Obj(fields.ToArray()));
            }

            var assemblies = new List<string>();
            var assemblyCount = 0;
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(AssemblyInstance)))
            {
                var assembly = element as AssemblyInstance;
                if (assembly == null) continue;
                assemblyCount++;
                if (assemblies.Count >= MaxRows) continue;

                assemblies.Add(Json.Obj(
                    Json.Str("id", assembly.UniqueId),
                    Json.Str("name", SafeName(assembly)),
                    Json.Num("members", AssemblyMembers(assembly))));
                // `assemblyCount` above is the total and this list is
                // capped, so the two are deliberately different numbers.
                // Whoever prints them must subtract from the TOTAL.
            }

            return Json.Ok(
                Json.Arr("groupTypes", rows),
                Json.Arr("assemblies", assemblies),
                Json.Num("groupTypeCount", placed + unplaced),
                Json.Num("placedGroupTypes", placed),
                // A DEFINITION WITH NOTHING PLACED still sits in the browser
                // and still travels with the file. It is not a fault, and it
                // is the thing a purge would take.
                Json.Num("unplacedGroupTypes", unplaced),
                Json.Num("assemblyCount", assemblyCount),
                Json.Num("listed", rows.Count),
                Json.Num("notListed", (placed + unplaced) - rows.Count),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Str("reads",
                    "`placements` IS THE NUMBER THAT MATTERS: editing one "
                    + "member of a group reaches every placement of that "
                    + "group's type, so a type placed 12 times is 12 places "
                    + "one edit changes. `members` is counted off ONE "
                    + "instance because every instance of a type holds the "
                    + "same content, and is absent rather than zero for a "
                    + "definition with nothing placed - there is no instance "
                    + "to count. A group type with 0 placements is not a "
                    + "fault; it is what a purge would remove. Assemblies "
                    + "are LISTED and not explained: whether an edit inside "
                    + "one travels to another of the same type is a question "
                    + "this code has never put to Revit, and a symmetrical "
                    + "guess would read exactly as confident as a fact."));
        }

        // =================================================================
        // SHAPE TWO - a category. "Which of these will an edit multiply?"
        // =================================================================

        private static string InsideOne(Document doc, BuiltInCategory builtIn,
                                        string category)
        {
            var rows = new List<string>();

            var examined = 0;
            var inAGroup = 0;
            var inAnAssembly = 0;
            // AN ELEMENT CAN BE IN A GROUP AND IN AN ASSEMBLY, so
            // inAGroup + inAnAssembly is NOT the number of rows - it counts
            // that element twice and would report a remainder larger than
            // what is missing. Counted where the row is decided instead.
            var listable = 0;
            var pinnedCount = 0;
            var nestedCount = 0;
            var worst = 0;

            // THE PLACEMENT COUNT IS CACHED PER GROUP TYPE. `GroupType.Groups`
            // walks a set every time it is asked, and a category of five
            // thousand ducts inside four group types would walk it five
            // thousand times. Keyed by UniqueId because that is a string by
            // contract - ElementId is not, and `ElementId.IntegerValue` is
            // NOT IN 2026.
            var placements = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var element in new FilteredElementCollector(doc)
                                        .OfCategory(builtIn)
                                        .WhereElementIsNotElementType())
            {
                if (element == null) continue;
                examined++;

                var pinned = IsPinned(element);
                if (pinned) pinnedCount++;

                var assembly = AssemblyOf(doc, element);
                if (assembly != null) inAnAssembly++;

                var chain = new List<string>();
                var here = 0;
                var deep = Walk(doc, element, placements, chain, out here);

                if (chain.Count > 0) inAGroup++;
                if (chain.Count > 1) nestedCount++;
                if (here > worst) worst = here;

                // AN ELEMENT IN NOTHING IS THE COMMON CASE AND IS NOT
                // LISTED. The list answers "what would an edit multiply",
                // and an element that multiplies nothing is noise in it -
                // the counts above already say how many there are.
                if (chain.Count == 0 && assembly == null) continue;
                listable++;
                if (rows.Count >= MaxRows) continue;

                var fields = new List<string>();
                fields.Add(Json.Str("id", element.UniqueId));
                fields.Add(Json.Str("element", SafeName(element)));
                fields.Add(Json.Str("level", LevelOf(doc, element)));
                fields.Add(Json.Bool("inAGroup", chain.Count > 0));
                fields.Add(Json.Bool("pinned", pinned));
                if (chain.Count > 0)
                {
                    fields.Add(Json.Num("placementsOfItsGroup", here));
                    fields.Add(Json.Bool("nested", chain.Count > 1));
                    // INNERMOST FIRST, each level carrying its own count.
                    // Nothing is multiplied - see the note at the top of
                    // this file.
                    fields.Add(Json.Arr("chain", chain));
                    if (deep)
                    {
                        fields.Add(Json.Bool("chainTruncated", true));
                    }
                }
                if (assembly != null)
                {
                    fields.Add(Json.Str("assembly", SafeName(assembly)));
                }

                rows.Add(Json.Obj(fields.ToArray()));
            }

            var free = examined - inAGroup;

            return Json.Ok(
                Json.Arr("elements", rows),
                Json.Str("category", category),
                Json.Num("examined", examined),
                Json.Num("inAGroup", inAGroup),
                Json.Num("inAnAssembly", inAnAssembly),
                Json.Num("inNoGroup", free),
                Json.Num("nested", nestedCount),
                Json.Num("pinned", pinnedCount),
                // THE HEADLINE. The worst multiplier in this category: the
                // most placements any one of these elements would carry an
                // edit into.
                Json.Num("mostPlacements", worst),
                Json.Num("groupTypesInvolved", placements.Count),
                Json.Num("matched", listable),
                Json.Num("listed", rows.Count),
                Json.Num("notListed", listable - rows.Count),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Str("scope", "the whole model, not just the active view"),
                Json.Str("reads",
                    "ASK THIS BEFORE A CHANGE, NOT AFTER ONE. An element in "
                    + "a group carries an edit into every placement of that "
                    + "group's type, and `mostPlacements` is the worst case "
                    + "in this category - edit that element and that many "
                    + "spots move. Revit does NOT raise an error for this: a "
                    + "move of a group member returns cleanly and shifts "
                    + "nothing, which is why \"it did not throw\" has never "
                    + "been enough. `pinned` is reported SEPARATELY because "
                    + "a group member is not pinned - two different reasons "
                    + "an edit will not land, and one is fixed by unpinning "
                    + "and the other by ungrouping. `chain` is innermost "
                    + "first with each level's own count and NOTHING IS "
                    + "MULTIPLIED: whether a nested group's count already "
                    + "includes the copies inside its parent is a question "
                    + "about Revit that this code has never been able to "
                    + "ask. Elements in nothing are counted in `inNoGroup` "
                    + "and not listed - they are the ones an edit reaches "
                    + "once, which is what you expected."));
        }

        /// <summary>
        /// Walk from an element up through the groups containing it.
        ///
        /// Leaves one entry per level in `chain`, innermost first, and hands
        /// back the placement count of the IMMEDIATE group - the one number
        /// that can be stated without a claim about how nesting compounds.
        ///
        /// Returns true when the walk was cut short by the depth cap.
        /// </summary>
        private static bool Walk(Document doc, Element element,
                                 Dictionary<string, int> placements,
                                 List<string> chain, out int immediate)
        {
            immediate = 0;

            var current = element;
            for (var depth = 0; depth < MaxDepth; depth++)
            {
                var group = GroupOf(doc, current);
                if (group == null) return false;

                var type = TypeOf(doc, group);
                var count = 0;
                if (type != null)
                {
                    // UniqueId is a string by contract; the NAME is the
                    // fallback only so that a type which somehow reports no
                    // id still lands in the dictionary. Group type names are
                    // unique in a model, so the fallback cannot merge two
                    // real types - and without it `groupTypesInvolved` would
                    // quietly under-count instead of being wrong out loud.
                    var key = type.UniqueId;
                    if (string.IsNullOrEmpty(key)) key = "name:" + SafeName(type);

                    if (!placements.TryGetValue(key, out count))
                    {
                        count = Instances(type).Count;
                        placements[key] = count;
                    }
                }

                if (chain.Count == 0) immediate = count;

                chain.Add(Json.Obj(
                    Json.Str("group", SafeName(group)),
                    Json.Str("groupType", type == null ? null : SafeName(type)),
                    Json.Str("kind", type == null ? KindOf(group) : KindOf(type)),
                    Json.Num("placements", count)));

                current = group;
            }

            // THE CAP WAS REACHED. Revit does not let a group contain
            // itself, so this should be unreachable - but a walk that can
            // loop forever on a model nobody here can open is not a risk
            // worth taking, and saying the chain was cut is honest in a way
            // that silently stopping is not.
            return true;
        }

        // =================================================================
        // Asking Revit, never an exception mid-answer
        // =================================================================

        /// <summary>The group immediately containing this element, or null.</summary>
        private static Group GroupOf(Document doc, Element element)
        {
            try
            {
                var id = element.GroupId;
                if (id == null || id == ElementId.InvalidElementId) return null;
                return doc.GetElement(id) as Group;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return null;
            }
        }

        /// <summary>The assembly containing this element, or null.</summary>
        private static AssemblyInstance AssemblyOf(Document doc, Element element)
        {
            try
            {
                var id = element.AssemblyInstanceId;
                if (id == null || id == ElementId.InvalidElementId) return null;
                return doc.GetElement(id) as AssemblyInstance;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return null;
            }
        }

        private static GroupType TypeOf(Document doc, Group group)
        {
            try
            {
                var id = group.GetTypeId();
                if (id == null || id == ElementId.InvalidElementId) return null;
                return doc.GetElement(id) as GroupType;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return null;
            }
        }

        /// <summary>Every placed instance of a group type, never an exception.</summary>
        private static List<Group> Instances(GroupType type)
        {
            var found = new List<Group>();
            GroupSet set = null;
            try { set = type.Groups; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { set = null; }
            if (set == null) return found;

            try
            {
                foreach (Group one in set)
                {
                    if (one != null) found.Add(one);
                }
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                // What was gathered before the set gave out is still true.
            }
            return found;
        }

        /// <summary>
        /// How many elements one group instance holds.
        ///
        /// A member id that resolves to nothing is NOT counted. A group
        /// remembering a member Revit no longer has is the shape that makes
        /// a count disagree with what the browser shows, and the browser is
        /// what the modeller is looking at.
        /// </summary>
        private static int MemberCount(Document doc, Group group)
        {
            try
            {
                var ids = group.GetMemberIds();
                if (ids == null) return 0;
                var alive = 0;
                foreach (var id in ids)
                {
                    if (doc.GetElement(id) != null) alive++;
                }
                return alive;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return 0;
            }
        }

        private static int AssemblyMembers(AssemblyInstance assembly)
        {
            try
            {
                var ids = assembly.GetMemberIds();
                return ids == null ? 0 : ids.Count;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return 0;
            }
        }

        /// <summary>
        /// "Model Groups", "Detail Groups" - read off the category rather
        /// than decided here.
        ///
        /// A detail group lives in one view and a model group does not, so
        /// they are not the same thing to a modeller. Revit's own category
        /// name is used because it is what the browser shows; naming them
        /// here would be this file inventing vocabulary the user does not
        /// see anywhere else.
        /// </summary>
        private static string KindOf(Element element)
        {
            try
            {
                var category = element.Category;
                return category == null ? null : category.Name;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return null;
            }
        }

        private static bool IsPinned(Element element)
        {
            try { return element.Pinned; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
        }

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
    }
}
