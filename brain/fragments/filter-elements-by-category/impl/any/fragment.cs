// NOT STANDALONE. Assumes `doc` and `category` are already in scope, and leaves
// `elements` and `unresolvedLevel` in scope for whatever is composed after it.
//
// WHAT REVIT ACTUALLY DOES WITH LEVELS, AND WHY THIS IS NOT A PROPERTY READ.
//
// There is no single "which level is this on" call. Where an element records
// its level depends on what kind of element it is, and these are genuinely
// different storage locations rather than aliases of one:
//
//   Wall                     -> Wall.LevelId
//   many elements            -> Element.LevelId
//   FamilyInstance           -> FAMILY_LEVEL_PARAM / SCHEDULE_LEVEL_PARAM
//   MEP CURVE (duct, pipe,   -> RBS_START_LEVEL_PARAM, and the others above are
//   cable tray, conduit)        NOT MERELY EMPTY ON IT - THEY ARE ABSENT
//
// The last row is the whole reason this code exists. A lookup that stops before
// RBS_START_LEVEL_PARAM does not throw on a duct and does not warn: every duct
// resolves to nothing, fails the comparison, and the filter returns a confident,
// successful ZERO.
//
// HERON'S DECISION, WHICH IS NOT THE OBVIOUS ONE: an element whose level cannot
// be resolved at all is COUNTED AND REPORTED, not silently dropped. Silently
// dropping it is what makes the failure above invisible - the caller sees a
// smaller number and no reason for it. Reporting it means a broken lookup shows
// up as "12 elements, 12 with no level found" instead of as a plausible zero.
// That is D-30's rule applied to code rather than to a proof: a thing that does
// nothing must never be able to look like a thing that worked.
//
// The order below is data rather than a chain of ?? operators, because the order
// IS the knowledge here and it should be readable as a list, changeable in one
// place, and quotable in the fragment's own documentation.

var levelParameterOrder = new[]
{
    BuiltInParameter.FAMILY_LEVEL_PARAM,
    BuiltInParameter.SCHEDULE_LEVEL_PARAM,
    BuiltInParameter.LEVEL_PARAM,
    BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
    BuiltInParameter.RBS_START_LEVEL_PARAM,   // MEP curves. Never drop this one.
};

Func<Element, ElementId> levelOf = e =>
{
    var wall = e as Wall;
    if (wall != null) return wall.LevelId;

    if (e.LevelId != ElementId.InvalidElementId) return e.LevelId;

    foreach (var candidate in levelParameterOrder)
    {
        var p = e.get_Parameter(candidate);
        if (p != null) return p.AsElementId();
    }
    return ElementId.InvalidElementId;
};

var matching = new FilteredElementCollector(doc)
    .OfCategory(category)
    .WhereElementIsNotElementType()
    .ToElements();

var unresolvedLevel = 0;
var elements = new List<Element>();

foreach (var e in matching)
{
    if (levelId == ElementId.InvalidElementId)
    {
        elements.Add(e);
        continue;
    }

    var found = levelOf(e);
    if (found == ElementId.InvalidElementId) unresolvedLevel++;
    else if (found == levelId) elements.Add(e);
}
