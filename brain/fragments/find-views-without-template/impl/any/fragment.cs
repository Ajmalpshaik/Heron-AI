// NOT STANDALONE. Assumes `doc` is in scope; leaves `elements`, `findings` and
// `onSheets` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// ON A SHEET OR NOT IS THE WHOLE DIFFERENCE. A working view somebody made to
// check something needs no template and never will; the same view placed on a
// sheet is being ISSUED, and an untemplated one there is a drawing that quietly
// stops matching the set. A flat list of every untemplated view in a real
// project is hundreds of rows and nobody reads it, so the ones on sheets are
// named separately and reported first.
//
// A SHEET IS A VIEW AND IS NOT ONE OF THESE. ViewSheet derives from View and
// comes back from the same collector; a sheet takes no view template, so
// listing sheets here would put a row against every drawing in the project.
//
// SO ARE SCHEDULES AND LEGENDS. Neither takes a view template either, and both
// are routinely on sheets - which would make them the loudest rows in a report
// about something they cannot have.
//
// WHICH VIEWS ARE ON SHEETS IS BUILT ONCE. Asking per view would mean a
// collector pass over every sheet for every view in the project.

var elements = new List<Element>();
var findings = new List<string>();
var onSheets = new List<ElementId>();

// view id -> the sheet number showing it, built in one pass.
var placedOn = new Dictionary<ElementId, string>();

foreach (var sheet in new FilteredElementCollector(doc)
                          .OfClass(typeof(ViewSheet))
                          .Cast<ViewSheet>())
{
    if (sheet == null || sheet.IsTemplate) continue;

    var placed = sheet.GetAllPlacedViews();
    if (placed == null) continue;

    foreach (var viewId in placed)
    {
        if (viewId == null || viewId == ElementId.InvalidElementId) continue;
        if (!placedOn.ContainsKey(viewId)) placedOn[viewId] = sheet.SheetNumber;
    }
}

var issued = new List<View>();
var working = new List<View>();

foreach (var view in new FilteredElementCollector(doc)
                         .OfClass(typeof(View))
                         .Cast<View>())
{
    if (view == null) continue;
    if (view.IsTemplate) continue;

    // None of these takes a view template, and all of them are routinely on
    // sheets - they would be the loudest rows in a report about something they
    // cannot have.
    if (view is ViewSheet) continue;
    if (view is ViewSchedule) continue;
    if (view.ViewType == ViewType.Legend) continue;

    if (view.ViewTemplateId != ElementId.InvalidElementId) continue;

    elements.Add(view);

    if (placedOn.ContainsKey(view.Id)) { issued.Add(view); onSheets.Add(view.Id); }
    else working.Add(view);
}

foreach (var view in issued)
{
    findings.Add(string.Format("{0}  - ON SHEET {1}, no view template",
                               view.Name, placedOn[view.Id]));
}

if (working.Count > 0)
{
    // Counted, not listed. These are working views and a project has hundreds;
    // naming them all buries the rows that matter.
    findings.Add(string.Format(
        "and {0} view(s) with no template that are on no sheet - normal for working "
        + "views, and not listed here because naming them buries the ones above",
        working.Count));
}
