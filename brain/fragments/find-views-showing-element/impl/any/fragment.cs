// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `inViews`,
// `onSheets` and `nowhere` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// ONE COLLECTOR PASS PER VIEW, AND THERE IS NO CHEAPER TRUE ANSWER. Revit
// keeps no reverse index from element to view, so the only way to know is to
// ask each view what it contains. The cost is real and it is the reason this
// is a deliberate check rather than something to call in a loop.
//
// EVERY VIEW IS ASKED ONCE and the answer reused for all the elements, rather
// than a pass per element per view. That is the difference between a check
// somebody runs and one they abandon.
//
// TEMPLATES AND UNPRINTABLE VIEWS ARE SKIPPED. A template contains nothing and
// a schedule or legend is not where an element is "shown" in the sense the
// question means.
//
// THE SHEET ANSWER IS THE ONE THAT MATTERS. A view nobody placed can be
// changed freely; a view on a sheet is a drawing somebody will receive. The
// viewports are read once and mapped view -> sheet, so the sheet answer costs
// nothing extra.

var inViews = new Dictionary<ElementId, ICollection<ElementId>>();
var onSheets = new Dictionary<ElementId, ICollection<ElementId>>();
var nowhere = new List<ElementId>();

var wanted = new HashSet<ElementId>();
foreach (var element in elements)
{
    if (element != null) wanted.Add(element.Id);
}

// view -> sheet, read once.
var sheetOf = new Dictionary<ElementId, ElementId>();
foreach (var viewport in new FilteredElementCollector(doc)
             .OfClass(typeof(Viewport)).Cast<Viewport>())
{
    if (!sheetOf.ContainsKey(viewport.ViewId)) sheetOf[viewport.ViewId] = viewport.SheetId;
}

foreach (var view in new FilteredElementCollector(doc)
             .OfClass(typeof(View)).Cast<View>())
{
    if (view.IsTemplate) continue;
    if (!view.CanBePrinted) continue;

    ICollection<ElementId> contains;
    try
    {
        contains = new FilteredElementCollector(doc, view.Id).ToElementIds();
    }
    catch (Exception)
    {
        // Some views refuse a collector. Skipped rather than abandoning the
        // whole sweep - the answer is then incomplete for that view only.
        continue;
    }

    foreach (var id in contains)
    {
        if (!wanted.Contains(id)) continue;

        if (!inViews.ContainsKey(id)) inViews[id] = new List<ElementId>();
        inViews[id].Add(view.Id);

        ElementId sheet;
        if (!sheetOf.TryGetValue(view.Id, out sheet)) continue;

        if (!onSheets.ContainsKey(id)) onSheets[id] = new List<ElementId>();
        if (!onSheets[id].Contains(sheet)) onSheets[id].Add(sheet);
    }
}

foreach (var id in wanted)
{
    if (!inViews.ContainsKey(id)) nowhere.Add(id);
}
