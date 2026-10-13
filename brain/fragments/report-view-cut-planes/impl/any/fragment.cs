// NOT STANDALONE. Assumes `doc` and `viewName` are in scope; leaves
// `cutPlanes`, `viewCount` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHERE EACH SECTION AND ELEVATION CUTS THE MODEL. For every section,
// elevation and detail view - or the one named - a point on its cut plane in
// millimetres, the direction it looks, and its far clip: on or off, and how
// deep, as Revit prints it.
//
// "LOOKING" IS THE OPPOSITE OF ViewDirection, and that is said here because
// it is the mistake to make: Revit's ViewDirection points from the model OUT
// TOWARDS the viewer. A section looking north has a ViewDirection of south.
//
// A VIEW NAME MAY CARRY ITS KIND - "Section: Section 1" - and an empty name
// reads every section, elevation and detail view.

const double MmPerFoot = 304.8;
var lines = new List<string>();
var findings = new List<string>();
var viewCount = 0;

var wanted = (viewName ?? "").Trim();
var wantedKind = "";
var bare = wanted;
var colon = wanted.IndexOf(':');
if (colon > 0)
{
    wantedKind = wanted.Substring(0, colon).Replace(" ", "").Trim();
    bare = wanted.Substring(colon + 1).Trim();
}

Func<double, string> mm = v => Math.Round(v * MmPerFoot).ToString(System.Globalization.CultureInfo.InvariantCulture);
Func<double, string> unit = v => Math.Round(v, 3).ToString(System.Globalization.CultureInfo.InvariantCulture);

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
{
    var view = element as View;
    if (view == null || view.IsTemplate) continue;
    if (view.ViewType != ViewType.Section && view.ViewType != ViewType.Elevation && view.ViewType != ViewType.Detail)
        continue;
    if (bare != "" && !string.Equals(view.Name, bare, StringComparison.OrdinalIgnoreCase)) continue;
    if (wantedKind != "" && !string.Equals(view.ViewType.ToString(), wantedKind, StringComparison.OrdinalIgnoreCase)) continue;

    viewCount++;
    var origin = view.Origin;
    var looking = view.ViewDirection.Negate();

    var farOn = "";
    var farDepth = "";
    try
    {
        var clip = view.get_Parameter(BuiltInParameter.VIEWER_BOUND_FAR_CLIPPING);
        if (clip != null) farOn = clip.AsValueString() ?? "";
        var depth = view.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR);
        if (depth != null) farDepth = depth.AsValueString() ?? "";
    }
    catch { }

    lines.Add(view.ViewType + ": " + view.Name + " | cuts through " + mm(origin.X) + "," + mm(origin.Y) + ","
        + mm(origin.Z) + " mm | looking " + unit(looking.X) + "," + unit(looking.Y) + "," + unit(looking.Z)
        + " | far clip " + (farOn == "" ? "(not read)" : farOn) + (farDepth == "" ? "" : ", " + farDepth));
}

var cutPlanes = string.Join("\n", lines);
if (wanted != "" && viewCount == 0)
    findings.Add("No section, elevation or detail view is named \"" + wanted + "\".");
else
    findings.Add(viewCount + " section, elevation or detail view(s) read.");
