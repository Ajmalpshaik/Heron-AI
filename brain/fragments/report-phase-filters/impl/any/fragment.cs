// NOT STANDALONE. Assumes `doc` and `filterName` are in scope; leaves
// `phaseFilterNames`, `phaseFilterIds`, `filterCount`, `report` and
// `findings` behind.
//
// READ ONLY - opens no transaction and changes nothing.
//
// EVERY PHASE FILTER IN THE PROJECT - the Phase Filters tab of Manage >
// Phasing - with how each shows its four phase statuses (New, Existing,
// Demolished, Temporary: By Category, Overridden or Not Displayed), whether it
// is one of Revit's own, and how many views and view templates use it.
//
// THE EXACT NAME IS THE POINT. SET_VIEW_PHASE takes a phase filter by name, and
// "Show Complete" against "Show complete" or "Show All" against "Show All
// Phases" is a near miss that either fails or picks another. This is where the
// name to use comes from.
//
// A NAME ASKED FOR THAT IS NOT THERE ANSWERS NOTHING, never the nearest
// filter's settings, and names the filters there are.
//
// The Graphic Overrides tab of the Phasing dialog - what "Overridden" draws -
// has no API on any release 2020 to 2027 (FRAGMENT-ISSUES row 5b-1350). It is
// not read here because it cannot be.

var phaseFilterNames = new List<string>();
var phaseFilterIds = new Dictionary<string, ElementId>();
var findings = new List<string>();
var lines = new List<string>();

Func<PhaseStatusPresentation, string> word = p =>
    p == PhaseStatusPresentation.ShowByCategory ? "By Category"
    : p == PhaseStatusPresentation.ShowOverriden ? "Overridden"
    : p == PhaseStatusPresentation.DontShow ? "Not Displayed"
    : p.ToString();
var statusNames = new[] { "New", "Existing", "Demolished", "Temporary" };
var statusEnums = new[] { ElementOnPhaseStatus.New, ElementOnPhaseStatus.Existing,
    ElementOnPhaseStatus.Demolished, ElementOnPhaseStatus.Temporary };

// Views and templates by the phase filter they carry. A schedule, a legend and
// a drafting view carry none, and are not counted.
var viewsUsing = new Dictionary<ElementId, int>();
var templatesUsing = new Dictionary<ElementId, int>();
foreach (var view in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
{
    var parameter = view.get_Parameter(BuiltInParameter.VIEW_PHASE_FILTER);
    if (parameter == null || parameter.StorageType != StorageType.ElementId) continue;
    var id = parameter.AsElementId();
    if (id == null || id == ElementId.InvalidElementId) continue;
    var tally = view.IsTemplate ? templatesUsing : viewsUsing;
    tally[id] = tally.ContainsKey(id) ? tally[id] + 1 : 1;
}

var all = new FilteredElementCollector(doc).OfClass(typeof(PhaseFilter)).Cast<PhaseFilter>().ToList();
var which = (filterName ?? "").Trim();
var everyOne = which == "" || string.Equals(which, "all", StringComparison.OrdinalIgnoreCase);
var chosen = everyOne ? all
    : all.Where(f => string.Equals((f.Name ?? "").Trim(), which, StringComparison.OrdinalIgnoreCase)).ToList();

foreach (var filter in chosen)
{
    phaseFilterNames.Add(filter.Name);
    if (!phaseFilterIds.ContainsKey(filter.Name)) phaseFilterIds[filter.Name] = filter.Id;
    var shows = string.Join(", ", Enumerable.Range(0, 4).Select(i =>
        statusNames[i] + " " + word(filter.GetPhaseStatusPresentation(statusEnums[i]))));
    var views = viewsUsing.ContainsKey(filter.Id) ? viewsUsing[filter.Id] : 0;
    var templates = templatesUsing.ContainsKey(filter.Id) ? templatesUsing[filter.Id] : 0;
    lines.Add("\"" + filter.Name + "\"" + (filter.IsDefault ? " (Revit's own)" : "")
        + ": " + shows + "; used by " + views + " view(s) and " + templates + " view template(s)");
}

var filterCount = phaseFilterNames.Count;
var report = string.Join(" | ", lines);

if (doc.IsFamilyDocument)
    findings.Add("This is a family file. Phase filters live in a project, so there are none to list here.");
else if (!everyOne && filterCount == 0)
    findings.Add("No phase filter is named \"" + which + "\". The phase filters in this project: "
        + string.Join(", ", all.Select(f => "\"" + f.Name + "\"")) + ".");
else
{
    findings.Add(filterCount + " phase filter(s)" + (everyOne ? " in this project, in the order Revit holds them." : "."));
    findings.AddRange(lines);
}
