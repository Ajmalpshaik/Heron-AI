// NOT STANDALONE. Assumes `doc` is in scope; leaves `organization`,
// `hiddenByFilter`, `unresolvedItems` and `findings` behind.
//
// VERSION 2 (2026-11-24, D-64, row 5b-1909, work-note item WN-7 of issue
// #1005) - A VIEW OR SHEET WHOSE PLACE IN THE BROWSER WILL NOT READ IS
// COUNTED. Version 1 read a filter test that threw as "shown", and passed over
// one whose folders would not read without a word, so "Every view and sheet
// shows in the Project Browser" could be said of views it never read.
// `unresolvedItems` carries the count every time, in neither the hidden nor
// the shown count; the sentence naming it is added only when it is above 0, so
// every other answer reads as version 1's.
//
// READ ONLY. Opens no transaction and needs none.
//
// HOW THE PROJECT BROWSER IS ORGANISED, for views and for sheets: the
// organisation in use, the parameters its folders group by (in order, outer
// first), what it sorts by and which way, and how many views and sheets its
// filter hides from the browser altogether.
//
// THE GROUPING IS READ OFF REAL ITEMS. The API gives no list of an
// organisation's grouping rules; it gives, for one view or sheet, the folders
// it sits in. So the folder path of every view is read, and the parameter of
// each folder level is named from the item's own parameters - matched by id,
// never built from a number.
//
// A VIEW THE FILTER HIDES IS THE FINDING: it exists, it may be on a sheet, and
// nobody can find it in the browser.

var lines = new List<string>();
var findings = new List<string>();
var hiddenByFilter = 0;
// VERSION 2 (D-64): views and sheets whose filter test or folders would not read.
var unresolvedItems = 0;

Func<Element, ElementId, string> parameterName = (item, id) =>
{
    try
    {
        foreach (Parameter p in item.Parameters)
            if (p.Id == id) return p.Definition == null ? "(unnamed)" : p.Definition.Name;
    }
    catch { }
    return "(parameter " + id + ")";
};

Action<BrowserOrganization, IEnumerable<Element>, string> describe = (organization, items, kind) =>
{
    if (organization == null)
    {
        lines.Add(kind + ": no browser organisation could be read.");
        return;
    }
    var levels = new List<string>();
    var hidden = 0;
    var read = 0;
    foreach (var item in items)
    {
        bool shown = true;
        try { shown = organization.AreFiltersSatisfied(item.Id); } catch { unresolvedItems++; continue; }
        if (!shown) { hidden++; continue; }
        IList<FolderItemInfo> path = null;
        try { path = organization.GetFolderItems(item.Id); } catch { }
        if (path == null) { unresolvedItems++; continue; }
        read++;
        for (var i = 0; i < path.Count; i++)
        {
            var name = parameterName(item, path[i].ElementId);
            if (i >= levels.Count) levels.Add(name);
            else if (levels[i] != name && !levels[i].Contains(name)) levels[i] = levels[i] + " or " + name;
        }
    }
    hiddenByFilter += hidden;

    var sortBy = "(none)";
    try
    {
        var first = items.FirstOrDefault();
        if (first != null && organization.SortingParameterId != ElementId.InvalidElementId)
            sortBy = parameterName(first, organization.SortingParameterId);
    }
    catch { }
    var order = "";
    try { order = organization.SortingOrder.ToString(); } catch { }

    lines.Add(kind + ": organised by \"" + organization.Name + "\" | folders "
        + (levels.Count == 0 ? "(none - a flat list)" : string.Join(" > ", levels))
        + " | sorted by " + sortBy + (order == "" ? "" : " " + order)
        + " | " + hidden + " hidden by its filter, " + read + " shown");
};

var views = new List<Element>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
{
    var v = element as View;
    if (v == null || v.IsTemplate || v is ViewSheet) continue;
    if (v.ViewType == ViewType.ProjectBrowser || v.ViewType == ViewType.SystemBrowser || v.ViewType == ViewType.Internal) continue;
    views.Add(v);
}
var sheets = new List<Element>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheet))) sheets.Add(element);

BrowserOrganization forViews = null, forSheets = null;
try { forViews = BrowserOrganization.GetCurrentBrowserOrganizationForViews(doc); } catch { }
try { forSheets = BrowserOrganization.GetCurrentBrowserOrganizationForSheets(doc); } catch { }
describe(forViews, views, "Views");
describe(forSheets, sheets, "Sheets");

var organization = string.Join("\n", lines);
// VERSION 2 (D-64): "every view and sheet shows" only when every one was read.
findings.Add(hiddenByFilter == 0 && unresolvedItems == 0
    ? "Every view and sheet shows in the Project Browser."
    : hiddenByFilter == 0
    ? "No view or sheet that was read is hidden from the Project Browser by its organisation's filter."
    : hiddenByFilter + " view(s) or sheet(s) are hidden from the Project Browser by its organisation's filter.");
// VERSION 2 (D-64): said only when a view or sheet was passed over.
if (unresolvedItems > 0)
    findings.Add(unresolvedItems + " view(s) or sheet(s) would not say whether the organisation's filter hides "
        + "them, or which folder they sit in, so they are in neither the hidden nor the shown count.");
