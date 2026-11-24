// NOT STANDALONE. Assumes `doc`, `elements` and `toleranceMm` are in scope;
// leaves `measured`, `worstMm`, `overTolerance`, `flatness`, `notMeasured` and
// `findings` behind.
//
// READ ONLY. Nothing here writes to the model, so no transaction is needed.
// Revit keeps lengths in feet; every distance said is in MILLIMETRES (D-71).
//
// ===========================================================================
// HOW FAR A PANEL'S CORNERS ARE FROM ONE FLAT PLANE.
// ===========================================================================
//
// A panel on a twisted or doubly curved facade - an adaptive component placed
// on the nodes of a divided surface, or between points - has corners that need
// not lie on one plane. A sheet of glass does. So each panel's PLACEMENT
// POINTS are read (AdaptiveComponentInstanceUtils, then each ReferencePoint's
// Position), the plane that fits them best is found by Newell's method - the
// sum of each edge's cross terms, which holds for any number of corners in
// order round the panel - through their centre, and the panel's out-of-plane
// value is the furthest any corner sits from that plane. For a four-cornered
// panel all four are that far, two in front and two behind.
//
// A PANEL WHOSE CORNERS ARE NOT ADAPTIVE POINTS IS NOT MEASURED, AND SAID.
// A curtain panel in a curtain wall is flat by construction; a pattern-based
// panel's corners may or may not be adaptive points the API returns - an
// ASSUMPTION the proof settles. Such a panel is named in `notMeasured`, with
// why, and never given a number. Three points always lie on one plane, so a
// panel with fewer than four is named too.
//
// THE LIMIT IS THE MODELLER'S. `toleranceMm` left out is "none": every panel
// is measured and listed worst first, and none is called good or bad - how
// much twist a sheet takes is the glazier's and the architect's number, not
// Heron's.

const double MillimetresPerFoot = 304.8;

// The most panel lines the answer spells out; every panel is still measured.
const int MostSaid = 200;

var measured = 0;
var worstMm = 0.0;
var overTolerance = new List<ElementId>();
var flatness = new List<string>();
var notMeasured = new List<string>();
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> sayMm = value => value.ToString("0.##", invariant);

Func<Element, string> nameOf = element =>
{
    string named = "";
    try { named = element.Name; } catch (Exception) { named = ""; }
    return string.Format("'{0}' (id {1})", named, element.Id);
};

// THE LIMIT, if one was given.
var limit = -1.0;
var limitWords = (toleranceMm ?? "").Trim();
var badLimit = "";
if (limitWords.Length > 0 && !string.Equals(limitWords, "none", StringComparison.OrdinalIgnoreCase))
{
    double value;
    if (!double.TryParse(limitWords, System.Globalization.NumberStyles.Float, invariant, out value)
        || double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        badLimit = string.Format("'{0}' is not a limit in millimetres - digits only, more than 0, such as 5. "
            + "Every panel is measured and listed without one", limitWords);
    else limit = value;
}

var rows = new List<KeyValuePair<double, string>>();
foreach (var element in elements ?? new List<Element>())
{
    if (element == null) continue;
    var instance = element as FamilyInstance;
    var adaptive = false;
    if (instance != null)
    {
        try { adaptive = AdaptiveComponentInstanceUtils.IsAdaptiveComponentInstance(instance); }
        catch (Exception) { adaptive = false; }
    }
    if (!adaptive)
    {
        notMeasured.Add(nameOf(element) + (instance == null
            ? " is not a placed family - left alone"
            : " is not an adaptive component, so its corners are not points Heron can read - not measured"));
        continue;
    }

    IList<ElementId> pointIds = null;
    try { pointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(instance); }
    catch (Exception) { pointIds = null; }
    var corners = new List<XYZ>();
    if (pointIds != null)
    {
        foreach (var id in pointIds)
        {
            var point = doc.GetElement(id) as ReferencePoint;
            if (point == null) continue;
            try { corners.Add(point.Position); } catch (Exception) { }
        }
    }
    if (corners.Count < 4)
    {
        notMeasured.Add(string.Format("{0} has {1} placement point(s) Revit gives - three or fewer always lie on "
            + "one plane, so there is nothing to measure", nameOf(element), corners.Count));
        continue;
    }

    // Newell's method: the plane's normal from the corners in order.
    double nx = 0, ny = 0, nz = 0;
    var centre = XYZ.Zero;
    for (var i = 0; i < corners.Count; i++)
    {
        var a = corners[i];
        var b = corners[(i + 1) % corners.Count];
        nx += (a.Y - b.Y) * (a.Z + b.Z);
        ny += (a.Z - b.Z) * (a.X + b.X);
        nz += (a.X - b.X) * (a.Y + b.Y);
        centre = centre + a;
    }
    centre = centre / corners.Count;
    var normal = new XYZ(nx, ny, nz);
    if (normal.GetLength() <= 1e-12)
    {
        notMeasured.Add(nameOf(element) + " has corners that lie on one line, or fold back on themselves - no "
            + "plane fits them, so it is not measured");
        continue;
    }
    normal = normal.Normalize();
    var furthest = 0.0;
    foreach (var corner in corners)
        furthest = Math.Max(furthest, Math.Abs((corner - centre).DotProduct(normal)));
    var mm = furthest * MillimetresPerFoot;
    measured++;
    worstMm = Math.Max(worstMm, mm);
    var over = limit > 0 && mm > limit;
    if (over) overTolerance.Add(element.Id);
    rows.Add(new KeyValuePair<double, string>(mm, string.Format("{0}: {1} corners, {2} mm out of plane{3}",
        nameOf(element), corners.Count, sayMm(mm), limit > 0 ? (over ? " - OVER " + sayMm(limit) + " mm"
            : " - within " + sayMm(limit) + " mm") : "")));
}

flatness = rows.OrderByDescending(r => r.Key).Take(MostSaid).Select(r => r.Value).ToList();

if (badLimit.Length > 0) findings.Add(badLimit);
if (measured == 0)
{
    findings.Add(notMeasured.Count == 0
        ? "No panel was given. Select the adaptive panels to measure, and ask again"
        : string.Format("No panel could be measured - {0}. Select adaptive components placed as panels",
            string.Join("; ", notMeasured.Take(3))));
}
else
{
    findings.Add(string.Format("Measured {0} panel(s): the furthest corner of the worst sits {1} mm off the plane "
        + "that fits it best", measured, sayMm(worstMm)));
    findings.Add(limit > 0
        ? string.Format("{0} of {1} are over {2} mm - see overTolerance to select them", overTolerance.Count,
            measured, sayMm(limit))
        : "No limit was given, so none is called flat enough or not - how much twist a sheet takes is the "
            + "glazier's and the architect's number. Worst first in flatness");
    if (rows.Count > MostSaid)
        findings.Add(string.Format("flatness lists the worst {0}; all {1} were measured", MostSaid, rows.Count));
}
if (notMeasured.Count > 0)
    findings.Add(string.Format("{0} element(s) not measured - see notMeasured", notMeasured.Count));
