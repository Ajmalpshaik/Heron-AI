// NOT STANDALONE. Assumes `doc`, `viewName`, `offsetMm` and `throughMm` are
// in scope; leaves `movedMm`, `newOrigin`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A SECTION OR ELEVATION'S CUT PLANE MOVED ALONG THE WAY IT LOOKS - forward by
// offsetMm (negative is back towards the viewer), or so that it passes through
// a point. Only the depth changes; the cut plane never turns.
//
// WHAT IS MOVED:
//
//   a section or detail   its viewer - the section line drawn in plan - found
//                         among the Viewers by the view's own name
//   an elevation          its elevation marker, but ONLY when that marker
//                         carries this one view: a marker with four views
//                         would move all four, so it is refused, naming them
//
// ONLY THE MODEL SAYS IT MOVED: the view's origin is read again after a
// regeneration and the distance it moved along the looking direction is what
// is reported - not what was asked.

const double MmPerFoot = 304.8;
var findings = new List<string>();
string refused = null;
var movedMm = 0.0;
var newOrigin = "";

var wanted = (viewName ?? "").Trim();
var wantedKind = "";
var bare = wanted;
var colon = wanted.IndexOf(':');
if (colon > 0)
{
    wantedKind = wanted.Substring(0, colon).Replace(" ", "").Trim();
    bare = wanted.Substring(colon + 1).Trim();
}

var answering = new List<View>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
{
    var v = element as View;
    if (v == null || v.IsTemplate) continue;
    if (v.ViewType != ViewType.Section && v.ViewType != ViewType.Elevation && v.ViewType != ViewType.Detail) continue;
    if (!string.Equals(v.Name, bare, StringComparison.OrdinalIgnoreCase)) continue;
    if (wantedKind != "" && !string.Equals(v.ViewType.ToString(), wantedKind, StringComparison.OrdinalIgnoreCase)) continue;
    answering.Add(v);
}

View view = null;
if (bare == "") refused = "No view was named, so NOTHING WAS MOVED.";
else if (answering.Count == 0) refused = "No section, elevation or detail view is named \"" + wanted + "\", so NOTHING WAS MOVED.";
else if (answering.Count > 1) refused = answering.Count + " views are named \"" + bare + "\" - write the kind first, \""
    + answering[0].ViewType + ": " + bare + "\". NOTHING WAS MOVED.";
else view = answering[0];

XYZ step = null;
if (refused == null)
{
    var looking = view.ViewDirection.Negate().Normalize();
    var through = (throughMm ?? "").Trim();
    if (through != "")
    {
        var parts = through.Split(',');
        double x, y, z;
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var numberStyle = System.Globalization.NumberStyles.Float;
        if (parts.Length != 3 || !double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
            || !double.TryParse(parts[1].Trim(), numberStyle, culture, out y)
            || !double.TryParse(parts[2].Trim(), numberStyle, culture, out z))
            refused = "\"" + through + "\" is not a point - three numbers in millimetres. NOTHING WAS MOVED.";
        else
        {
            var point = new XYZ(x / MmPerFoot, y / MmPerFoot, z / MmPerFoot);
            step = looking * (point - view.Origin).DotProduct(looking);
        }
    }
    else step = looking * (offsetMm / MmPerFoot);

    if (refused == null && step.GetLength() < 0.5 / MmPerFoot)
        refused = "The cut plane is already there - less than half a millimetre to move. NOTHING WAS MOVED.";
}

ElementId toMove = ElementId.InvalidElementId;
if (refused == null)
{
    if (view.ViewType == ViewType.Elevation)
    {
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ElevationMarker)))
        {
            var marker = element as ElevationMarker;
            if (marker == null) continue;
            var carries = new List<ElementId>();
            for (var i = 0; i < marker.MaximumViewCount; i++)
            {
                var id = marker.GetViewId(i);
                if (id != null && id != ElementId.InvalidElementId) carries.Add(id);
            }
            if (!carries.Contains(view.Id)) continue;
            if (carries.Count > 1)
                refused = "\"" + view.Name + "\" shares its elevation marker with "
                    + string.Join(", ", carries.Where(id => id != view.Id).Select(id => doc.GetElement(id) == null ? id.ToString() : doc.GetElement(id).Name))
                    + " - moving the marker would move them all. NOTHING WAS MOVED.";
            else toMove = marker.Id;
            break;
        }
    }
    else
    {
        foreach (var element in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Viewers))
        {
            if (element.Id != view.Id && string.Equals(element.Name, view.Name, StringComparison.Ordinal))
            {
                toMove = element.Id;
                break;
            }
        }
    }
    if (refused == null && toMove == ElementId.InvalidElementId)
        refused = "The section line or marker of \"" + view.Name + "\" could not be found, so NOTHING WAS MOVED.";
}

if (refused == null)
{
    var before = view.Origin;
    try
    {
        ElementTransformUtils.MoveElement(doc, toMove, step);
    }
    catch (Exception ex)
    {
        refused = "Revit refused the move: " + ex.Message + ". NOTHING WAS MOVED.";
    }
    if (refused == null)
    {
        doc.Regenerate();
        var after = view.Origin;
        var looking = view.ViewDirection.Negate().Normalize();
        movedMm = Math.Round((after - before).DotProduct(looking) * MmPerFoot, 1);
        Func<double, string> mm = v => Math.Round(v * MmPerFoot).ToString(System.Globalization.CultureInfo.InvariantCulture);
        newOrigin = mm(after.X) + "," + mm(after.Y) + "," + mm(after.Z);
        var asked = Math.Round(step.DotProduct(looking) * MmPerFoot, 1);
        if (Math.Abs(movedMm - asked) > 1.0)
            findings.Add("Asked to move " + asked + " mm, and the cut plane reads back moved " + movedMm
                + " mm - pinned, or held by something it is aligned to.");
        findings.Add("\"" + view.Name + "\" cut plane moved " + movedMm + " mm along the way it looks (read back); it "
            + "now cuts through " + newOrigin + " mm.");
    }
}
if (refused != null) findings.Add(refused);
