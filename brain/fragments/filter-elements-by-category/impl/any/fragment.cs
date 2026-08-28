// NOT STANDALONE. Assumes `doc` and `category` are already in scope, and leaves
// `elements` in scope for whatever is composed after it. The wrapper supplies
// `doc`; `category` and the optional `levelId` come from the request.
//
// WHY THE LEVEL LOOKUP IS A CHAIN AND NOT A PROPERTY.
//
// Revit has no single "which level is this on" API. Where an element records
// its level depends on what kind of element it is, and the storage is genuinely
// different rather than merely inconsistent:
//
//   - a Wall exposes LevelId directly
//   - many elements populate Element.LevelId
//   - a FamilyInstance uses FAMILY_LEVEL_PARAM or SCHEDULE_LEVEL_PARAM
//   - an MEP CURVE - duct, pipe, cable tray, conduit - uses NONE of those.
//     It records the level on RBS_START_LEVEL_PARAM and the others are not
//     merely empty on it, they are ABSENT.
//
// That last line is why this is a chain and why RBS_START_LEVEL_PARAM is in it.
// A lookup that stops earlier does not throw and does not warn on a duct: it
// returns nothing, every duct fails the comparison, and the filter reports a
// confident, successful ZERO. That is the failure this whole library is built
// to make impossible, and it is invisible to any number of successful runs -
// which is exactly why this fragment's proof must include a level scope that
// SHOULD match and does. See D-30.

var found = new FilteredElementCollector(doc)
    .OfCategory(category)
    .WhereElementIsNotElementType()
    .AsEnumerable();

if (levelId != ElementId.InvalidElementId)
{
    Func<Element, ElementId> levelOf = e =>
    {
        if (e is Wall wall) return wall.LevelId;
        if (e.LevelId != ElementId.InvalidElementId) return e.LevelId;

        var p = e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)
             ?? e.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM)
             ?? e.get_Parameter(BuiltInParameter.LEVEL_PARAM)
             ?? e.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)
             ?? e.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);

        return p?.AsElementId() ?? ElementId.InvalidElementId;
    };

    found = found.Where(e => levelOf(e) == levelId);
}

var elements = found.ToList();
