// NOT STANDALONE. Assumes `doc`, `elements`, `angleRadians`, `pivot` and
// `axisLine` are in scope; leaves `rotated`, `blocked`, `unverified`,
// `refused`, `rotatedIds` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). The angle is in RADIANS;
// `pivot` and `axisLine` are MILLIMETRES, converted below (D-71).
//
// VERSION 3 (2026-11-01, the review of the merges) - THE PIVOT MAY BE LEFT OUT,
// AS THE PURPOSE ALWAYS SAID. `pivot` was a point the add-in converted, and an
// absent point is refused before this runs - so "each about its own point",
// the "spin that damper round" job version 1's code handled with a null pivot,
// could never be asked. It is text now, "x,y,z" in millimetres as before, and
// left out it binds "none": each element turns about a vertical axis through
// its own location point. "itself" is read the same way. A pivot that is not
// three numbers is refused and nothing is turned.
//
// VERSION 2 (2026-11-01) - ROTATE_ELEMENTS_ABOUT_AXIS MERGED IN:
//
//   AN AXIS IN ANY DIRECTION, so it can TILT. `axisLine` "x,y,z; x,y,z" in
//   millimetres is the line every element turns about - a sloped panel, a
//   raked support. Left out, the axis is vertical, through `pivot` or each
//   element's own point, as version 1. "Rotate about X" means nothing until
//   somebody says WHERE that line is, so the caller gives the line.
//
//   REFUSED IS KEPT APART FROM DID-NOT-MOVE. Revit refusing the call outright
//   - a pinned element - is `refused`; accepted and still where it was - a
//   group member, a constraint - is `blocked`. Version 1 put both in
//   `blocked`.
//
//   A ZERO ANGLE IS REFUSED, AND SO IS A WHOLE TURN. Nothing would move, and
//   nothing is attempted - which is not the same as nothing being turnable.
//   Version 1 counted both as rotated; the safer rule is kept.
//
//   NOTHING TO COMPARE IS UNVERIFIED, NEVER TURNED. ROTATE_ELEMENTS_ABOUT_AXIS
//   counted an element with nothing to compare as turned; this card never did.
//
//   WHAT IS COMPARED, FIXED IN THE MOVE. ROTATE_ELEMENTS_ABOUT_AXIS compared a
//   placed element's POSITION only, so one turned about a line through its
//   own point - which does not move it - was reported as not moving. The
//   signature below is everything that changes when a thing turns, in three
//   dimensions: a family instance's whole placement (origin and its three
//   axes), a curve's two ends, a placed point and its rotation, the box.
//
// THE SAME SILENT NO-OP AS MOVING, MEASURED THE SAME WAY. Revit's rotate call
// returns normally and rotates NOTHING for a member of a group. Five grouped
// air terminals stayed at 0.00 degrees while code that trusted the call
// reported "rotated 5, skipped 0". Only the model can confirm a rotation.

var rotated = 0;
var blocked = new List<ElementId>();
var unverified = new List<ElementId>();
var refused = new List<ElementId>();
var rotatedIds = new List<ElementId>();
var findings = new List<string>();

var culture = System.Globalization.CultureInfo.InvariantCulture;
Func<XYZ, string> at = p => p.X.ToString("F6", culture) + "," + p.Y.ToString("F6", culture) + ","
    + p.Z.ToString("F6", culture);

// WHAT CHANGES WHEN A THING TURNS, about any axis. Every reading the element
// has is joined, so a change in any one of them is a turn: a family's own
// axes swing even when its origin is on the axis, and a curve's ends swing
// even when its midpoint does not move.
Func<Element, string> orientationOf = e =>
{
    if (e == null) return null;
    var parts = new List<string>();

    var instance = e as FamilyInstance;
    if (instance != null)
    {
        try
        {
            var t = instance.GetTransform();
            parts.Add("T" + at(t.Origin) + ";" + at(t.BasisX) + ";" + at(t.BasisY) + ";" + at(t.BasisZ));
        }
        catch { }
    }

    var placed = e.Location as LocationPoint;
    if (placed != null)
    {
        try { parts.Add("P" + at(placed.Point) + ";R" + placed.Rotation.ToString("F6", culture)); }
        catch { parts.Add("P" + at(placed.Point)); }
    }

    var line = e.Location as LocationCurve;
    if (line != null && line.Curve != null)
        parts.Add("C" + at(line.Curve.GetEndPoint(0)) + ";" + at(line.Curve.GetEndPoint(1)));

    try
    {
        var box = e.get_BoundingBox(null);
        if (box != null) parts.Add("B" + at(box.Min) + ";" + at(box.Max));
    }
    catch
    {
        // No geometry to read. Reported as unverified, never as turned.
    }
    return parts.Count == 0 ? null : string.Join("|", parts);
};

// A whole turn changes nothing, and neither does no turn at all.
var full = 2.0 * Math.PI;
var normalised = Math.Abs(angleRadians % full);
var askedForNothing = normalised < 1e-9 || Math.Abs(normalised - full) < 1e-9;

// A POINT IN MILLIMETRES, to feet - the axis's ends and, since version 3, the
// pivot. Null for anything that is not three finite numbers within 100 km.
Func<string, XYZ> pointMm = text =>
{
    var bits = (text ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    if (bits.Length != 3) return null;
    var v = new double[3];
    for (var i = 0; i < 3; i++)
    {
        if (!double.TryParse(bits[i].Trim(), System.Globalization.NumberStyles.Float, culture, out v[i])
            || double.IsNaN(v[i]) || double.IsInfinity(v[i]) || Math.Abs(v[i]) > 100000000.0) return null;
    }
    return new XYZ(v[0] / 304.8, v[1] / 304.8, v[2] / 304.8);
};

// THE PIVOT (version 3): "none" - the card's `absent:` - or "itself" is each
// element about its own point; anything else must be a point.
string stop = null;
var pivotText = (pivot ?? "").Trim();
var eachAboutItself = pivotText.Length == 0
    || string.Equals(pivotText, "none", StringComparison.OrdinalIgnoreCase)
    || string.Equals(pivotText, "itself", StringComparison.OrdinalIgnoreCase);
XYZ pivotPoint = eachAboutItself ? null : pointMm(pivotText);
if (!eachAboutItself && pivotPoint == null)
    stop = "\"" + pivotText + "\" is not a pivot. A pivot is a point - three numbers in millimetres, "
        + "\"24000,3000,0\" - or leave it out (\"none\") to turn each element about its own point. "
        + "Nothing was turned";

// THE AXIS, IF ONE WAS GIVEN - two points in millimetres, to feet.
var axisText = (axisLine ?? "").Trim();
var aboutALine = axisText.Length > 0 && !string.Equals(axisText, "none", StringComparison.OrdinalIgnoreCase);
Line given = null;

if (stop == null && aboutALine)
{
    var ends = axisText.Split(';').Select(each => each.Trim()).Where(each => each.Length > 0).ToList();
    var a = ends.Count == 2 ? pointMm(ends[0]) : null;
    var b = ends.Count == 2 ? pointMm(ends[1]) : null;
    if (a == null || b == null)
        stop = "\"" + axisText + "\" is not an axis. An axis is a LINE in space - two points in "
            + "millimetres, \"0,0,3000; 5000,0,3000\" - because naming X or Y does not say where that line "
            + "is. Nothing was turned";
    else if (a.DistanceTo(b) < 1.0 / 304.8)
        stop = "The two points of the axis are the same - a line needs two. Nothing was turned";
    else
        given = Line.CreateBound(a, b);
}

if (stop == null && askedForNothing)
{
    // VERSION 2: REFUSED, NOT COUNTED (the safer of the two cards' rules).
    stop = "The angle is zero, or a whole turn, so nothing would move. Nothing was attempted, which is "
        + "not the same as nothing being turnable";
}
if (stop == null && (elements == null || elements.Count == 0))
{
    stop = "No elements were given, so there is nothing to turn";
}

if (stop != null)
{
    findings.Add(stop);
}
else
{
    var before = new Dictionary<ElementId, string>();
    foreach (var element in elements)
    {
        if (element == null) continue;
        var was = orientationOf(element);
        if (was != null) before[element.Id] = was;
    }

    var attempted = new List<Element>();
    foreach (var element in elements)
    {
        if (element == null) continue;

        var axis = given;
        if (axis == null)
        {
            // NO PIVOT (left out, version 3): each element then turns about
            // its own position - "spin that damper round" against "swing this
            // row about the corner".
            var about = pivotPoint;
            if (about == null)
            {
                var placed = element.Location as LocationPoint;
                if (placed == null)
                {
                    // Nothing to turn it about, and this fragment will not invent one.
                    unverified.Add(element.Id);
                    continue;
                }
                about = placed.Point;
            }
            // A vertical axis through the pivot. Revit needs a bounded line,
            // and its length is irrelevant to the rotation.
            axis = Line.CreateBound(about, about + XYZ.BasisZ);
        }

        try
        {
            ElementTransformUtils.RotateElement(doc, element.Id, axis, angleRadians);
            attempted.Add(element);
        }
        catch
        {
            // Revit would not turn it at all - KEPT APART from one that was
            // accepted and did not move (version 2).
            refused.Add(element.Id);
        }
    }

    doc.Regenerate();

    foreach (var element in attempted)
    {
        string was;
        var now = orientationOf(element);
        if (now == null || !before.TryGetValue(element.Id, out was))
        {
            unverified.Add(element.Id);
            continue;
        }

        if (now == was) blocked.Add(element.Id);
        else
        {
            rotated++;
            rotatedIds.Add(element.Id);
        }
    }

    findings.Add(string.Format(
        "{0} turned about {1}, {2} accepted the call and did not move (a group member, or held by a "
        + "constraint), {3} Revit would not turn at all (pinned), {4} could not be checked{5}",
        rotated,
        given != null ? "the axis given" : pivotPoint != null ? "a vertical axis through the pivot"
            : "a vertical axis through each one's own point",
        blocked.Count, refused.Count, unverified.Count,
        given != null && pivotPoint != null ? ". The pivot is not used when an axis is given" : ""));
}
