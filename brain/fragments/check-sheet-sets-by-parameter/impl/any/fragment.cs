// NOT STANDALONE. Assumes `doc`, `parameterName` and `setPrefix` are in scope,
// and leaves `report`, `inStep`, `outOfStep`, `missingSets`, `findings`,
// `refused`, `ambiguous` and `unconfirmed` behind.
//
// A READ. The sets are read as elements - never through the print manager,
// whose current set is application state - so nothing about Revit changes.
//
// ONE VALUE, ONE SET. Every sheet and view that carries the parameter is
// grouped by its value; the set for a value is the one named prefix + value,
// compared case-blind. A sheet with the parameter empty belongs to no set and
// is counted in a finding, never guessed at.
//
// A NAME TWO PARAMETERS SHARE IS NOT READ (version 2; D-54 s3, FRAGMENT-ISSUES
// 5b-203). LookupParameter hands back "the first one encountered" when two
// parameters on one sheet share the name, and Autodesk's reference says that
// match "is determined at random" - a project parameter bound to Sheets beside
// a built-in or shared one of the same name does it. A sheet filed under the
// value of whichever came first would make its set read out of step, or in
// step, on a coin toss. So the name is counted on each sheet and view first:
// one carrying it twice is in `ambiguous`, belongs to no value's group, and
// is never listed as missing from a set or extra in one. IT FAILS CLOSED:
// a sheet Revit will not count the name on is not read either, and is said.
//
// AND NO SET IS CALLED "IN STEP" WHILE ANY SHEET WAS NOT READ. An unread
// sheet may carry a set's value and be missing from it, and an unread member
// may not belong, so a set that matches every sheet that WAS read is
// reported NOT CONFIRMED and counted in `unconfirmed`, never in `inStep`. A
// set out of step on the sheets that were read is still out of step.

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
// Sheets and views carrying the name twice - not read (5b-203). `ambiguous`
// as every reader of that row names it; the labels are for the finding.
var ambiguous = new List<ElementId>();
var ambiguousLabels = new List<string>();
// Sheets and views Revit would not count the name on - not read either.
var uncounted = new List<string>();
// Both kinds: whatever was not read.
var notReadIds = new HashSet<ElementId>();
int unconfirmed = 0;
if (refused.Length == 0)
{
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
    {
        var view = element as View;
        if (view == null || view.IsTemplate) continue;

        var sameName = -1;
        try { sameName = view.GetParameters(wanted).Count; } catch { }
        if (sameName > 1) { ambiguous.Add(view.Id); ambiguousLabels.Add(label(view)); notReadIds.Add(view.Id); continue; }
        if (sameName < 0) { uncounted.Add(label(view)); notReadIds.Add(view.Id); continue; }

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
    if (carriers == 0 && notReadIds.Count == 0)
        refused = "No sheet or view in " + (doc.Title ?? "this model") + " has a parameter called '" + wanted
            + "'. Name it exactly as the sheet's Properties show it.";
    else if (carriers == 0)
        refused = "Nothing was checked: '" + wanted + "' was not read on any sheet or view in "
            + (doc.Title ?? "this model") + " - each one that has it has two or more parameters of that name, "
            + "or Revit would not count them.";
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
        // A member whose value was not read is not called extra (5b-203).
        var extra = holds.Where(v => !wantIds.Contains(v.Id) && !notReadIds.Contains(v.Id)).Select(label).OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();

        if (lacking.Count == 0 && extra.Count == 0 && notReadIds.Count > 0)
        {
            // NOT "IN STEP" - see the header. It matches what was read.
            unconfirmed++;
            var unreadHere = holds.Count(v => notReadIds.Contains(v.Id));
            rows.Add(value + " -> set '" + set.Name + "': NOT CONFIRMED - it matches the " + group.Count
                + " sheets and views that were read, but " + notReadIds.Count + " sheet(s) and view(s) were not read"
                + (unreadHere > 0 ? ", " + unreadHere + " of them in this set" : "")
                + ", so whether it is in step is not known");
        }
        else if (lacking.Count == 0 && extra.Count == 0)
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
            rows.Add(value + " -> set '" + set.Name + "': OUT OF STEP - " + string.Join("; ", parts)
                + (notReadIds.Count > 0 ? " (among the sheets and views that were read)" : ""));
        }
    }

    report = string.Join(" || ", rows);
    missingSets = string.Join("; ", missing);
    findings.Add(values.Count + " value(s) of '" + wanted + "' in " + (doc.Title ?? "this model") + ": " + inStep
        + " set(s) in step, " + outOfStep + " out of step, " + missing.Count + " with no set"
        + (unconfirmed > 0 ? ", " + unconfirmed + " not confirmed because some sheets were not read" : "") + ".");
    if (blank > 0)
        findings.Add(blank + " sheet(s) and view(s) carry '" + wanted + "' with no value, so belong to no set.");
}
else findings.Add(refused);

if (ambiguous.Count > 0)
    findings.Add(ambiguous.Count + " sheet(s) and view(s) carry TWO OR MORE parameters called '" + wanted
        + "' - NOT READ, because Revit hands back whichever of them it meets first: "
        + string.Join(", ", ambiguousLabels.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).Take(20))
        + (ambiguousLabels.Count > 20 ? ", and " + (ambiguousLabels.Count - 20) + " more" : "")
        + ". They are in no value's count and are never listed as missing from a set or extra in one.");

if (uncounted.Count > 0)
    findings.Add(uncounted.Count + " sheet(s) and view(s) were NOT READ: Revit would not say how many parameters "
        + "called '" + wanted + "' they carry, so none was picked - "
        + string.Join(", ", uncounted.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).Take(20))
        + (uncounted.Count > 20 ? ", and " + (uncounted.Count - 20) + " more." : "."));
