// NOT STANDALONE. Assumes `elements` and `doc` are in scope, and leaves
// `shownIn`, `onSheets`, `shownNowhere`, `viewsSearched` and
// `schedulesSkipped` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THERE IS NO "WHICH VIEWS SHOW THIS" PROPERTY, AND THERE CANNOT BE.
//
// Visibility is not something an element carries. It is the result of a view's
// crop, view range, filters, category settings, phase and discipline applied
// together. The only truthful way to ask is a collector SCOPED TO THE VIEW:
//
//     new FilteredElementCollector(doc, view.Id)
//
// which is Revit answering with the machinery it uses to draw, so every one of
// those rules is accounted for at once. It is also why this is slow: it is one
// collector per view, and a model can hold eight hundred.
//
// WHAT "SHOWN" MEANS HERE, SAID RATHER THAN IMPLIED.
//
// In that view's element set. Temporarily hidden still counts - that state is
// per-session and is not saved. Drawn behind something else counts - it is
// drawn. Cropped out, filtered out, category off, wrong phase, missed by the
// view range: all correctly excluded.
//
// SCHEDULES CANNOT ANSWER AND ARE COUNTED, NOT SKIPPED QUIETLY.
//
// A schedule has no geometric element set, so an element scheduled but not
// drawn does not appear. Counting them makes the gap visible; leaving them out
// silently would make "not on any drawing" mean two different things.

var shownIn = new Dictionary<ElementId, IList<ElementId>>();
var onSheets = new Dictionary<ElementId, IList<ElementId>>();
var shownNowhere = new List<ElementId>();
int viewsSearched = 0;
int schedulesSkipped = 0;

// Which sheet each view sits on. Built once from the viewports rather than
// asked per view - a viewport IS the placement of a view on a sheet, on every
// release in the range, and asking it once turns a per-view lookup into a
// dictionary read.
var sheetOfView = new Dictionary<ElementId, ElementId>();
foreach (var viewport in new FilteredElementCollector(doc)
             .OfClass(typeof(Viewport))
             .Cast<Viewport>())
{
    try { sheetOfView[viewport.ViewId] = viewport.SheetId; } catch { }
}

var searching = new List<View>();
foreach (var view in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
{
    if (view.IsTemplate) continue;

    if (view is ViewSchedule) { schedulesSkipped++; continue; }

    // A view nobody placed on a sheet is rarely the answer to "which drawings
    // have to be reissued", and it is most of the count on a real model.
    if (!sheetOfView.ContainsKey(view.Id)) continue;

    searching.Add(view);
}

viewsSearched = searching.Count;

var wanted = new HashSet<ElementId>();
foreach (var element in elements)
{
    if (element == null) continue;
    wanted.Add(element.Id);
    shownIn[element.Id] = new List<ElementId>();
    onSheets[element.Id] = new List<ElementId>();
}

foreach (var view in searching)
{
    ICollection<ElementId> present = null;
    try
    {
        present = new FilteredElementCollector(doc, view.Id)
            .WhereElementIsNotElementType()
            .ToElementIds();
    }
    catch { }
    if (present == null) continue;

    foreach (var id in present)
    {
        if (!wanted.Contains(id)) continue;

        shownIn[id].Add(view.Id);

        ElementId sheetId;
        if (sheetOfView.TryGetValue(view.Id, out sheetId) && !onSheets[id].Contains(sheetId))
            onSheets[id].Add(sheetId);
    }
}

foreach (var id in wanted)
{
    if (shownIn[id].Count == 0) shownNowhere.Add(id);
}
