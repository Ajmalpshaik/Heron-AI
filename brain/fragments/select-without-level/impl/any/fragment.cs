// NOT STANDALONE. Assumes `doc` and `categories` are in scope; leaves
// `elements`, `checked` and `findings`.
//
// A READ. It opens no transaction and needs none.
//
// SELECT_BY_LEVEL COUNTS THESE AND DROPS THEM - its `unresolvedLevel` is an int.
// This returns the set instead, which is the whole difference.
//
// THE LOOKUP HAS FOUR STEPS AND THE LAST ONE MATTERS MOST. An MEP curve keeps
// its level ONLY on RBS_START_LEVEL_PARAM and has no LevelId at all, so a lookup
// that stops early reports every duct in the model as unlevelled.
//
// MOST OF WHAT HAS NO LEVEL SHOULD HAVE NONE. Views, sheets, materials and
// annotation are all correct answers and all useless ones, which is why the
// category list is what makes this mean anything.

var elements = new List<Element>();
var examined = 0;
var findings = new List<string>();

var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();

if (categories != null && categories.Count > 0)
{
    var wanted = new List<BuiltInCategory>();
    foreach (var category in categories) wanted.Add(category);
    collector = collector.WherePasses(new ElementMulticategoryFilter(wanted));
}

foreach (var element in collector)
{
    if (element == null) continue;
    examined++;

    var levelId = ElementId.InvalidElementId;

    // 1. A wall keeps it here.
    var wall = element as Wall;
    if (wall != null) levelId = wall.LevelId;

    // 2. Most elements keep it here.
    if (levelId == ElementId.InvalidElementId) levelId = element.LevelId;

    // 3. A family instance may keep it on its own level parameter.
    if (levelId == ElementId.InvalidElementId)
    {
        var parameter = element.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
        if (parameter != null && parameter.HasValue) levelId = parameter.AsElementId();
    }

    // 4. AN MEP CURVE KEEPS IT ONLY HERE, and a lookup stopping above this one
    //    returns every duct in the model.
    if (levelId == ElementId.InvalidElementId)
    {
        var parameter = element.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);
        if (parameter != null && parameter.HasValue) levelId = parameter.AsElementId();
    }

    if (levelId == ElementId.InvalidElementId) elements.Add(element);
}

var bounded = categories != null && categories.Count > 0;
findings.Add(string.Format(
    "{0} of {1} element(s) examined have no level{2}",
    elements.Count, examined,
    bounded
        ? string.Format(", within {0} category/categories", categories.Count)
        : ". THIS WAS UNBOUNDED, so it includes views, sheets, materials and "
          + "annotation - all of which correctly have no level and none of "
          + "which is the answer to anything. Give a category list"));
