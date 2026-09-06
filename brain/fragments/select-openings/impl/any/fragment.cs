// NOT STANDALONE. Assumes `doc` and `inViewOnly` are in scope, and leaves
// `elements`, `byKind` and `found` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// OPENINGS ARE NOT ONE CATEGORY.
//
// A shaft cutting several levels, a hole in a slab, a rectangular opening in a
// wall and one in a roof are four different categories with four different
// names. Asking for the obvious one returns a fraction of what is there and
// looks like a complete answer, which is the failure this exists to avoid.
//
// AN OPENING CUT BY A FAMILY IS NOT ONE OF THESE.
//
// The hole a door or a window makes belongs to that family and is in none of
// these categories. That is correct - it is the door's hole - but "show me
// every hole" means both to a person, so it is said rather than left to be
// discovered.

var elements = new List<Element>();
var byKind = new Dictionary<string, int>();

var openingCategories = new List<BuiltInCategory>();
openingCategories.Add(BuiltInCategory.OST_ShaftOpening);
openingCategories.Add(BuiltInCategory.OST_FloorOpening);
openingCategories.Add(BuiltInCategory.OST_SWallRectOpening);
openingCategories.Add(BuiltInCategory.OST_RoofOpening);
openingCategories.Add(BuiltInCategory.OST_ColumnOpening);

// Every category starts at zero, so a kind with none is still in the report -
// no shafts at all is a finding on a building with risers in it.
foreach (var category in openingCategories)
{
    var definition = Category.GetCategory(doc, category);
    string name = definition != null ? definition.Name : category.ToString();
    if (!byKind.ContainsKey(name)) byKind[name] = 0;
}

var collector = inViewOnly != null
    ? new FilteredElementCollector(doc, inViewOnly.Id)
    : new FilteredElementCollector(doc);

foreach (var element in collector
             .WhereElementIsNotElementType()
             .WherePasses(new ElementMulticategoryFilter(openingCategories)))
{
    if (element == null) continue;
    elements.Add(element);

    string name = "(no category)";
    try { if (element.Category != null) name = element.Category.Name ?? name; } catch { }
    if (byKind.ContainsKey(name)) byKind[name] = byKind[name] + 1;
    else byKind[name] = 1;
}

int found = elements.Count;
