// NOT STANDALONE. Assumes `doc`, `levelId` and `categories` are in scope;
// leaves `elements`, `unresolvedLevel` and `findings` behind.
//
// THIS IS FILTER_ELEMENTS_BY_CATEGORY'S LEVEL LOOKUP, DELIBERATELY IDENTICAL.
// There is no single "which level is this on" call, and where an element keeps
// its level depends on what it is:
//
//   Wall                     -> Wall.LevelId
//   many elements            -> Element.LevelId
//   FamilyInstance           -> FAMILY_LEVEL_PARAM / SCHEDULE_LEVEL_PARAM
//   MEP CURVE (duct, pipe,   -> RBS_START_LEVEL_PARAM, and the others are
//   cable tray, conduit)        ABSENT on it rather than merely empty
//
// Stop before that last entry and every duct in the model resolves to nothing,
// fails the comparison, and the filter returns a confident, successful ZERO.
//
// THE DIFFERENCE FROM THAT FRAGMENT is only that no category is required here.
// When one IS known it should be used, because it bounds the scan to that
// category instead of walking the model.
//
// AN UNRESOLVED LEVEL IS COUNTED, NOT DROPPED. Unbounded, that count is large
// and correct - views, sheets, materials and most annotation have no level at
// all. What it protects against is the other case: a lookup that broke,
// reporting a plausible small number with no reason attached.

var levelParameterOrder = new[]
{
    BuiltInParameter.FAMILY_LEVEL_PARAM,
    BuiltInParameter.SCHEDULE_LEVEL_PARAM,
    BuiltInParameter.LEVEL_PARAM,
    BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
    BuiltInParameter.RBS_START_LEVEL_PARAM,   // MEP curves. Never drop this one.
};

var elements = new List<Element>();
var unresolvedLevel = 0;
var findings = new List<string>();

var level = levelId == ElementId.InvalidElementId ? null : doc.GetElement(levelId) as Level;

if (level == null)
{
    findings.Add("No level was given, or that id is not a Level. Name the level to search - unlike "
        + "FILTER_ELEMENTS_BY_CATEGORY, this fragment has no whole-model meaning without one");
}
else
{
    Func<Element, ElementId> levelOf = element =>
    {
        var wall = element as Wall;
        if (wall != null) return wall.LevelId;

        if (element.LevelId != ElementId.InvalidElementId) return element.LevelId;

        foreach (var candidate in levelParameterOrder)
        {
            var p = element.get_Parameter(candidate);
            if (p != null) return p.AsElementId();
        }
        return ElementId.InvalidElementId;
    };

    var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
    if (categories != null && categories.Count > 0)
        collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

    var scanned = 0;
    foreach (var element in collector)
    {
        scanned++;

        var found = ElementId.InvalidElementId;
        try { found = levelOf(element); }
        catch { found = ElementId.InvalidElementId; }

        if (found == ElementId.InvalidElementId) unresolvedLevel++;
        else if (found == level.Id) elements.Add(element);
    }

    var bounded = categories != null && categories.Count > 0;

    findings.Add(string.Format("{0} of {1} scanned element(s) are on level '{2}'{3}. {4} carry no "
        + "level at all",
        elements.Count, scanned, level.Name,
        bounded ? string.Format(", within {0} category/categories", categories.Count)
                : ", across every category",
        unresolvedLevel));

    if (!bounded)
        findings.Add("This walked the whole model. A category list bounds it, and where a single "
            + "category is known FILTER_ELEMENTS_BY_CATEGORY takes a level and is cheaper");
}
