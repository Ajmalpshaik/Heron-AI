// NOT STANDALONE. Assumes `elements` is in scope; leaves `levelName`,
// `offsetMm` and `noLevel` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// TWO PLACES CARRY THE LEVEL AND NEITHER IS ALWAYS THE ONE. `Element.LevelId`
// is set for hosted and family instances and is INVALID on plenty of MEP
// elements, which carry theirs in a parameter instead. Both are read, LevelId
// first, and an element with neither is named rather than defaulted to the
// nearest level - a guess here reads exactly like a fact.
//
// THE OFFSET IS REPORTED BECAUSE THE LEVEL ALONE MISLEADS. A duct on Level 1
// with a 3800 mm offset is physically above Level 2's floor and still reports
// Level 1. Correct, and the opposite of what somebody asking "what level is
// this on" usually means. Giving both lets the answer say which question it
// answered.
//
// FEET TO MILLIMETRES IS ARITHMETIC (D-20). One foot is 304.8 mm exactly.
// UnitUtils was rewritten in 2021 and would put a version break into a
// fragment that needs none.

var levelName = new Dictionary<ElementId, string>();
var offsetMm = new Dictionary<ElementId, double>();
var noLevel = new List<ElementId>();

const double MillimetresPerFoot = 304.8;

// In order. The first that yields a level wins; MEP elements commonly answer
// on the second or third and have an invalid LevelId.
var levelParameters = new BuiltInParameter[]
{
    BuiltInParameter.RBS_START_LEVEL_PARAM,
    BuiltInParameter.FAMILY_LEVEL_PARAM,
    BuiltInParameter.SCHEDULE_LEVEL_PARAM,
};

var offsetParameters = new BuiltInParameter[]
{
    BuiltInParameter.RBS_OFFSET_PARAM,
    BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM,
    BuiltInParameter.INSTANCE_ELEVATION_PARAM,
};

foreach (var element in elements)
{
    if (element == null) continue;
    if (levelName.ContainsKey(element.Id) || noLevel.Contains(element.Id)) continue;

    Level level = null;

    var direct = element.LevelId;
    if (direct != null && direct != ElementId.InvalidElementId)
    {
        level = element.Document.GetElement(direct) as Level;
    }

    if (level == null)
    {
        foreach (var which in levelParameters)
        {
            var parameter = element.get_Parameter(which);
            if (parameter == null || !parameter.HasValue) continue;
            if (parameter.StorageType != StorageType.ElementId) continue;

            var found = parameter.AsElementId();
            if (found == null || found == ElementId.InvalidElementId) continue;

            level = element.Document.GetElement(found) as Level;
            if (level != null) break;
        }
    }

    if (level == null)
    {
        noLevel.Add(element.Id);
        continue;
    }

    levelName[element.Id] = level.Name;

    // No offset found is reported as 0.0 alongside a level that IS known, which
    // is different from the element having no level at all. An element sitting
    // exactly on its level is the commonest case there is.
    var offset = 0.0;
    foreach (var which in offsetParameters)
    {
        var parameter = element.get_Parameter(which);
        if (parameter == null || !parameter.HasValue) continue;
        if (parameter.StorageType != StorageType.Double) continue;

        offset = parameter.AsDouble();
        break;
    }

    offsetMm[element.Id] = offset * MillimetresPerFoot;
}
