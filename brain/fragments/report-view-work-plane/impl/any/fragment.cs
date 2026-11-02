// NOT STANDALONE. Assumes `doc` and `views` are in scope; leaves
// `workPlanes`, `withPlane`, `noPlane` and `findings` behind.
//
// READS ONLY. No transaction is opened and nothing is changed. The write half
// is SET_VIEW_WORK_PLANE; a question about which work plane a view is on must
// never reach it (D-86).
//
// WHY A CARD OF ITS OWN (2026-11-02, FRAGMENT-ISSUES row 5b-1392). This read
// was first written into REPORT_VIEW_SETTINGS as its `workPlanes`, on a branch
// where that card was DRAFT. The same day #790 proved REPORT_VIEW_SETTINGS on
// Revit 2020 against code without it, so the read was moved here, line for
// line, rather than make that proof stale.
//
// THE WORK PLANE EACH VIEW IS ON. Revit's Architecture > Work Plane > Set
// dialog, its "Current" line, read from View.SketchPlane: the level,
// reference plane or grid the work plane was taken from -
// SketchPlane.GetPlaneReference, the element it names - in the dialog's own
// "Level : Level 2" form, and where the plane lies in millimetres. When Revit
// names no such element, the work plane's own name. A view with none - a
// sheet, a schedule - is named in `findings` as having none, never given one.
// With NO view named, the view in front: "which work plane is this view on"
// names no view. Whether the work plane is SHOWN is not read - Revit gives no
// member for it on any release.
//
// ONE STRING, ROWS JOINED BY "  ||  ", because a list reaches the chat cut to
// three entries and the rows are the answer.
//
// RELEASES (tools/api-surface --members, 2026-11-02): View.SketchPlane,
// SketchPlane.GetPlane and GetPlaneReference, Document.GetElement(Reference),
// Document.ActiveView and BuiltInParameter.DATUM_TEXT, 2020 through 2027.

var findings = new List<string>();
var workPlanes = "";
var withPlane = 0;
var noPlane = 0;
{
    var culture = System.Globalization.CultureInfo.InvariantCulture;
    Func<double, string> mmOf = feet => Math.Round(feet * 304.8, 1).ToString("0.#", culture);
    Func<double, string> unit = v => Math.Round(v, 3).ToString(culture);
    var rows = new List<string>();

    var named = views ?? new List<View>();
    var asked = named.Where(v => v != null).ToList();
    var inFront = asked.Count == 0;
    if (inFront && doc.ActiveView != null) asked.Add(doc.ActiveView);

    var planeSeen = new HashSet<ElementId>();
    foreach (var view in asked)
    {
        if (view == null || !view.IsValidObject) continue;
        if (!planeSeen.Add(view.Id)) continue;

        string name;
        try { name = view.Name; }
        catch (Exception) { name = "(a view whose name cannot be read)"; }
        var label = "'" + name + "' (" + view.ViewType + ")";

        SketchPlane sketch = null;
        try { sketch = view.SketchPlane; }
        catch (Exception) { }
        if (sketch == null)
        {
            noPlane++;
            findings.Add(label + " no work plane");
            continue;
        }

        string whose = null;
        try
        {
            var reference = sketch.GetPlaneReference();
            var host = reference == null ? null : doc.GetElement(reference);
            if (host is Level) whose = "Level : " + host.Name;
            else if (host is Grid) whose = "Grid : " + host.Name;
            else if (host is ReferencePlane)
            {
                var own = host.get_Parameter(BuiltInParameter.DATUM_TEXT);
                var text = own == null ? null : own.AsString();
                whose = "Reference Plane : " + (string.IsNullOrEmpty(text) ? "(unnamed)" : text);
            }
        }
        catch (Exception) { }
        if (whose == null) whose = "\"" + sketch.Name + "\"";

        var lies = "";
        try
        {
            var plane = sketch.GetPlane();
            var n = plane.Normal;
            if (Math.Abs(Math.Abs(n.Z) - 1) < 1e-6) lies = "horizontal at elevation " + mmOf(plane.Origin.Z) + " mm";
            else if (Math.Abs(n.Z) < 1e-6)
                lies = "vertical, facing " + unit(n.X) + "," + unit(n.Y) + ", through " + mmOf(plane.Origin.X) + ","
                    + mmOf(plane.Origin.Y) + " mm";
            else lies = "sloped, facing " + unit(n.X) + "," + unit(n.Y) + "," + unit(n.Z);
        }
        catch (Exception) { }

        withPlane++;
        rows.Add(label + " work plane " + whose + (lies == "" ? "" : " - " + lies));
    }

    workPlanes = string.Join("  ||  ", rows.ToArray());
    if (inFront)
        findings.Insert(0, withPlane + noPlane == 0
            ? "No view was named and none is in front, so no work plane was read."
            : "No view was named, so the work plane was read for the view in front.");
    findings.Insert(0, "Read " + (withPlane + noPlane) + " view(s) for their work plane: " + withPlane
        + " with one, " + noPlane + " with none. Nothing was changed.");
}
