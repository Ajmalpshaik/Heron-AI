// NOT STANDALONE. Assumes `elements` and `newLevel` are in scope; leaves
// `moved`, `alreadyThere`, `heightChanged` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE OFFSET MUST BE RECOMPUTED OR THE ELEMENT JUMPS A STOREY.
//
// Height is stored as an OFFSET FROM THE LEVEL. Set the level and leave the
// offset alone, and a duct 2800 above Level 1 becomes a duct 2800 above
// Level 2 - three metres higher, with nothing reported and nothing on screen
// to draw the eye until somebody sections through it.
//
// So: read the absolute height first, change the level, then write the offset
// that restores it. The arithmetic is a difference of two internal lengths,
// so no unit conversion arises anywhere in this fragment.
//
// AND VERIFIED AFTER. A recomputed offset that was written is not one that
// took - Revit refuses some offsets outright and clamps others. Any element
// whose absolute height moved is reported in `heightChanged`, which is the
// failure this fragment exists to prevent and the one number worth reading.

var moved = 0;
var alreadyThere = 0;
var heightChanged = new List<ElementId>();
var refused = new List<ElementId>();

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
};

foreach (var element in elements)
{
    if (element == null) continue;

    // Which parameter carries this element's level. MEP and family instances
    // differ, and an element with none cannot be reassigned at all.
    Parameter levelParameter = null;
    foreach (var which in levelParameters)
    {
        var candidate = element.get_Parameter(which);
        if (candidate == null || candidate.IsReadOnly) continue;
        if (candidate.StorageType != StorageType.ElementId) continue;
        levelParameter = candidate;
        break;
    }

    if (levelParameter == null)
    {
        refused.Add(element.Id);
        continue;
    }

    if (levelParameter.AsElementId() == newLevel.Id)
    {
        alreadyThere++;
        continue;
    }

    var oldLevel = element.Document.GetElement(levelParameter.AsElementId()) as Level;
    if (oldLevel == null)
    {
        refused.Add(element.Id);
        continue;
    }

    Parameter offsetParameter = null;
    foreach (var which in offsetParameters)
    {
        var candidate = element.get_Parameter(which);
        if (candidate == null || candidate.IsReadOnly) continue;
        if (candidate.StorageType != StorageType.Double) continue;
        offsetParameter = candidate;
        break;
    }

    if (offsetParameter == null)
    {
        // No offset to correct means no way to hold the height. Refused rather
        // than changing the level and letting it jump.
        refused.Add(element.Id);
        continue;
    }

    var wasAbsolute = oldLevel.Elevation + offsetParameter.AsDouble();

    try
    {
        levelParameter.Set(newLevel.Id);
        offsetParameter.Set(wasAbsolute - newLevel.Elevation);
    }
    catch (Exception)
    {
        refused.Add(element.Id);
        continue;
    }

    if (levelParameter.AsElementId() != newLevel.Id)
    {
        refused.Add(element.Id);
        continue;
    }

    moved++;

    // The check this fragment is for. A tenth of a millimetre in internal feet
    // is far below anything a modeller can draw and far above rounding noise.
    var nowAbsolute = newLevel.Elevation + offsetParameter.AsDouble();
    if (Math.Abs(nowAbsolute - wasAbsolute) > 0.0003) heightChanged.Add(element.Id);
}
