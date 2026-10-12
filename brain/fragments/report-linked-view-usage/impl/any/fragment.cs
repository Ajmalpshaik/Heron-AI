// NOT STANDALONE. Assumes `doc` and `linkName` are in scope; leaves `usage`,
// `notByHost`, `linkCount` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// HOW EACH VIEW SHOWS EACH LINKED MODEL - the Revit Links tab of
// Visibility/Graphics, for every view at once. A view shows a link By Host
// View (the default), By Linked View (one of the link's own views stands in),
// or Custom (its own settings). Only the views that are NOT By Host View are
// listed one by one, with the linked view they borrow, because those are the
// ones that surprise: a plan whose link shows a different level, a template
// that does not reach the link.
//
// REVIT 2024 opened a view's link settings to code; the card declares 2024 on.
//
// linkName narrows it to one link (its name, or the start of it); empty reads
// every link.

var lines = new List<string>();
var notByHost = 0;
var findings = new List<string>();

var wanted = (linkName ?? "").Trim();
var links = new List<RevitLinkInstance>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)))
{
    var link = element as RevitLinkInstance;
    if (link == null) continue;
    if (wanted != "" && !link.Name.StartsWith(wanted, StringComparison.OrdinalIgnoreCase)) continue;
    links.Add(link);
}
var linkCount = links.Count;

var byHost = 0;
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
{
    var view = element as View;
    if (view == null || view.IsTemplate || view is ViewSheet || view is ViewSchedule) continue;
    foreach (var link in links)
    {
        RevitLinkGraphicsSettings settings = null;
        try { settings = view.GetLinkOverrides(link.Id); } catch { continue; }
        if (settings == null) continue;
        var kind = settings.LinkVisibilityType;
        if (kind == LinkVisibility.ByHostView) { byHost++; continue; }
        notByHost++;
        var borrowed = "";
        if (kind == LinkVisibility.ByLinkView)
        {
            try
            {
                var linked = link.GetLinkDocument();
                var linkedView = linked == null ? null : linked.GetElement(settings.LinkedViewId) as View;
                borrowed = linkedView == null ? " (its linked view could not be read)" : " - shows the link's view \"" + linkedView.Name + "\"";
            }
            catch { }
        }
        lines.Add(view.ViewType + ": " + view.Name + " | " + link.Name + " | " + kind + borrowed);
    }
}

var usage = string.Join("\n", lines);
findings.Add(linkCount == 0
    ? (wanted == "" ? "The model has no Revit links." : "No link's name starts \"" + wanted + "\".")
    : notByHost + " view-and-link pair(s) are By Linked View or Custom; " + byHost + " are By Host View.");
