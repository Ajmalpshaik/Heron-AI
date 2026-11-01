// NOT STANDALONE. Assumes `doc`, `parameterName` and `setPrefix` are in scope,
// and leaves `report`, `inStep`, `outOfStep`, `missingSets`, `findings` and
// `refused` behind.
//
// A READ. The sets are read as elements - never through the print manager,
// whose current set is application state - so nothing about Revit changes.
//
// ONE VALUE, ONE SET. Every sheet and view that carries the parameter is
// grouped by its value; the set for a value is the one named prefix + value,
// compared case-blind. A sheet with the parameter empty belongs to no set and
// is counted in a finding, never guessed at.

string report = "";
int inStep = 0;
int outOfStep = 0;
string missingSets = "";
var findings = new List<string>();
string refused = "";

string wanted = (parameterName ?? "").Trim();
string prefix = (setPrefix ?? "");
if (string.Equals(prefix.Trim(), "none", StringComparison.OrdinalIgnoreCase)) prefix = "";

Func<View, string> label = view =>
{
    var sheet = view as ViewSheet;
    if (sheet != null) return (sheet.SheetNumber ?? "") + " - " + (sheet.Name ?? "");
    return (view.Name ?? "") + " (" + view.ViewType + ")";
};
Func<Parameter, string> valueOf = parameter =>
{
    if (parameter == null || !parameter.HasValue) return "";
    string text = "";
    try { text = parameter.StorageType == StorageType.String ? parameter.AsString() : parameter.AsValueString(); } catch { }
    return (text ?? "").Trim();
};

if (wanted.Length == 0) refused = "No parameter name was given.";

// Every sheet and view that carries the parameter, by value.
var byValue = new Dictionary<string, List<View>>(StringComparer.OrdinalIgnoreCase);
int carriers = 0;
int blank = 0;
if (refused.Length == 0)
{
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
    {
        var view = element as View;
        if (view == null || view.IsTemplate) continue;
        Parameter parameter = null;
        try { parameter = view.LookupParameter(wanted); } catch { }
        if (parameter == null) continue;
        carriers++;
        string value = valueOf(parameter);
        if (value.Length == 0) { blank++; continue; }
        List<View> group;
        if (!byValue.TryGetValue(value, out group)) { group = new List<View>(); byValue[value] = group; }
        group.Add(view);
    }
    if (carriers == 0)
        refused = "No sheet or view in " + (doc.Title ?? "this model") + " has a parameter called '" + wanted
            + "'. Name it exactly as the sheet's Properties show it.";
}

if (refused.Length == 0)
{
    var sets = new Dictionary<string, ViewSheetSet>(StringComparer.OrdinalIgnoreCase);
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheetSet)))
    {
        var set = element as ViewSheetSet;
        if (set == null) continue;
        string name = "";
        try { name = (set.Name ?? "").Trim(); } catch { }
        if (name.Length > 0 && !sets.ContainsKey(name)) sets[name] = set;
    }

    var rows = new List<string>();
    var missing = new List<string>();
    var values = byValue.Keys.ToList();
    values.Sort(StringComparer.OrdinalIgnoreCase);
    foreach (var value in values)
    {
        var group = byValue[value];
        string setName = (prefix + value).Trim();
        ViewSheetSet set;
        if (!sets.TryGetValue(setName, out set))
        {
            missing.Add(setName);
            rows.Add(value + " -> NO SET called '" + setName + "' (" + group.Count + " sheets and views carry it)");
            continue;
        }

        var holds = new List<View>();
        bool read = true;
        try { foreach (View member in set.Views) if (member != null) holds.Add(member); }
        catch { read = false; }
        if (!read) { rows.Add(value + " -> set '" + set.Name + "': its members could not be read"); continue; }

        var heldIds = new HashSet<ElementId>(holds.Select(v => v.Id));
        var wantIds = new HashSet<ElementId>(group.Select(v => v.Id));
        var lacking = group.Where(v => !heldIds.Contains(v.Id)).Select(label).OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();
        var extra = holds.Where(v => !wantIds.Contains(v.Id)).Select(label).OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();

        if (lacking.Count == 0 && extra.Count == 0)
        {
            inStep++;
            rows.Add(value + " -> set '" + set.Name + "': in step, " + group.Count + " sheets and views");
        }
        else
        {
            outOfStep++;
            var parts = new List<string>();
            if (lacking.Count > 0) parts.Add("missing " + string.Join(", ", lacking));
            if (extra.Count > 0) parts.Add("extra " + string.Join(", ", extra));
            rows.Add(value + " -> set '" + set.Name + "': OUT OF STEP - " + string.Join("; ", parts));
        }
    }

    report = string.Join(" || ", rows);
    missingSets = string.Join("; ", missing);
    findings.Add(values.Count + " value(s) of '" + wanted + "' in " + (doc.Title ?? "this model") + ": " + inStep
        + " set(s) in step, " + outOfStep + " out of step, " + missing.Count + " with no set.");
    if (blank > 0)
        findings.Add(blank + " sheet(s) and view(s) carry '" + wanted + "' with no value, so belong to no set.");
}
else findings.Add(refused);
