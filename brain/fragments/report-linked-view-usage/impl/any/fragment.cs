// NOT STANDALONE. Assumes `doc` and `linkName` are in scope; leaves `usage`,
// `notByHost`, `linkCount`, `skippedPairs` and `findings` behind.
//
// VERSION 2 (2026-11-21, D-64, row 5b-1909, work-note item WN-7 of issue
// #1005): NO VIEW-AND-LINK PAIR IS PASSED OVER WITHOUT A WORD. Version 1 passed
// over every pair whose instance read came back empty or threw. Empty is not a
// refusal: RevitAPI.xml (2024 to 2027) says GetLinkOverrides gives null for a
// link INSTANCE with no overrides of its own in the view, and the same call
// takes the link TYPE - the row the instance sits under in the Revit Links
// tab. So an empty instance read now reads the type's settings, and the pair
// is counted By Host View or listed like any other; version 1 counted it
// neither, so its "N are By Host View" was short, and a type set By Linked
// View or Custom was not listed. A pair whose read THROWS in a view that takes
// graphic overrides is counted in `skippedPairs`, and the sentence naming it
// rides only on the empty answer (no pair By Linked View or Custom); a view
// that takes no graphic override has no link settings to count.
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
var skippedPairs = 0;
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
        var thrown = false;
        try { settings = view.GetLinkOverrides(link.Id); } catch { thrown = true; }
        // VERSION 2: NULL IS NO OVERRIDE ON THE INSTANCE, NOT A REFUSAL - the
        // link type's settings are read instead (see the header).
        if (!thrown && settings == null)
        {
            try { settings = view.GetLinkOverrides(link.GetTypeId()); } catch { thrown = true; }
        }
        if (thrown || settings == null)
        {
            // VERSION 2 (D-64): counted where the view could hold link
            // settings at all, never silently dropped.
            var acceptsOverrides = false;
            try { acceptsOverrides = view.AreGraphicsOverridesAllowed(); } catch { }
            if (acceptsOverrides) skippedPairs++;
            continue;
        }
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
// VERSION 2 (D-64): ON THE EMPTY ANSWER ONLY, what was passed over - so "none
// is By Linked View or Custom" is never said of pairs that were not read.
if (linkCount > 0 && notByHost == 0 && skippedPairs > 0)
    findings.Add(skippedPairs + " view-and-link pair(s) were passed over: Revit would not give their link "
        + "settings, so they are counted neither By Host View nor otherwise.");
