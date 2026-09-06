// NOT STANDALONE. Assumes `doc` is in scope, and leaves `elements`, `byType`
// and `placedCount` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// A SCHEDULE IS NOT PLACED THROUGH A VIEWPORT.
//
// Sheets carry schedules as a different element kind altogether. Read the
// viewports only and every schedule in the project reports as unplaced - so a
// tidy-up built on that offers to delete the drawing set's schedules, which is
// a very confident piece of destruction. Both routes are read here.
//
// A TEMPLATE IS NOT AN UNPLACED VIEW.
//
// Templates are views by type and belong on no sheet by definition. So are the
// browser views Revit keeps for itself. Counting either would put them in a
// list somebody is about to delete from.
//
// IT DELETES NOTHING, DELIBERATELY.
//
// An unplaced view today is on a sheet tomorrow, and some of them are
// somebody's live workspace. Showing what would go is the honest half of
// "purge the unplaced views".

var elements = new List<Element>();
var byType = new Dictionary<string, int>();
int placedCount = 0;

// Every view that IS placed, by either route.
var placed = new HashSet<ElementId>();

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Viewport)))
{
    var viewport = element as Viewport;
    if (viewport == null) continue;
    try { placed.Add(viewport.ViewId); } catch { }
}

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ScheduleSheetInstance)))
{
    var instance = element as ScheduleSheetInstance;
    if (instance == null) continue;
    try { placed.Add(instance.ScheduleId); } catch { }
}

placedCount = placed.Count;

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
{
    var view = element as View;
    if (view == null) continue;

    bool isTemplate = false;
    try { isTemplate = view.IsTemplate; } catch { }
    if (isTemplate) continue;

    // The browser views Revit keeps for itself, and sheets - a sheet is a view
    // and is not something that goes ON a sheet.
    var type = view.ViewType;
    if (type == ViewType.ProjectBrowser || type == ViewType.SystemBrowser ||
        type == ViewType.DrawingSheet || type == ViewType.Internal ||
        type == ViewType.Undefined) continue;

    if (placed.Contains(view.Id)) continue;

    elements.Add(view);

    string kind = type.ToString();
    if (byType.ContainsKey(kind)) byType[kind] = byType[kind] + 1;
    else byType[kind] = 1;
}
