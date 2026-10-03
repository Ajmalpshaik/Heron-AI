// NOT STANDALONE. Assumes `doc` and `shape` are in scope; leaves `openingId`,
// `built`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// AN OPENING IN THE HOST of a wall-based or ceiling-based family - the Family
// Editor's Opening tool, which only those templates offer. The template carries
// a sample wall or ceiling; an opening cut in it is cut in every wall or
// ceiling the family is placed on in a project: a wall-based grille's hole, a
// recessed light's cut in the ceiling. Revit's call takes a wall or a ceiling
// and nothing else, so a floor- or roof-based family is refused by name.
//
// THE SHAPE IS ONE CLOSED LOOP in the host's own two coordinates, millimetres
// from the family origin: along the wall and up it for a wall (X,Z for a wall
// running along X, Y,Z for one along Y); X,Y in plan for a ceiling. "rect
// X1,Y1 X2,Y2", "circle CX,CY R", "polygon CX,CY R N" or corners.
//
// ITS SIZE IS FIXED AS DRAWN. An opening's sketch cannot be locked to planes
// through Revit's API, so this opening does not follow a parameter; an opening
// that must resize is a VOID form locked to planes instead - whether a void
// cuts the host of these templates by itself is NEEDS-CHECKING BV12. Said in
// the findings every time.
//
// READ BACK, ALL OR NOTHING: the opening's host and its outline's extent are
// read again; one that does not read as asked fails the call.

var findings = new List<string>();
var openingId = "";
var built = "";
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(invariant);
Func<double, string> plain = value => Math.Round(value, 2).ToString(invariant);
Func<XYZ, int> axisOf = n =>
    n == null ? -1 : Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;
Func<XYZ, int, double> along = (point, index) => index == 0 ? point.X : index == 1 ? point.Y : point.Z;
var letters = new[] { "X", "Y", "Z" };

Func<string, double?> number = text =>
{
    double value;
    if (!double.TryParse((text ?? "").Trim(), System.Globalization.NumberStyles.Float, invariant, out value))
        return null;
    if (double.IsNaN(value) || double.IsInfinity(value)) return null;
    return value;
};

Func<string, double[]> pair = text =>
{
    var parts = (text ?? "").Split(',');
    if (parts.Length != 2) return null;
    var first = number(parts[0]);
    var second = number(parts[1]);
    return first.HasValue && second.HasValue ? new[] { first.Value, second.Value } : null;
};

var problems = new List<string>();
// Revit's own shortest curve, in millimetres - a segment shorter is refused by
// Revit with a message nobody can place, so it is refused here by name.
var shortest = doc.Application.ShortCurveTolerance * 304.8;

// ONE LOOP, as segments {kind, u0, v0, um, vm, u1, v1} in millimetres - kind 0
// a straight line, 1 an arc through (um, vm). Null, with the reason added to
// `problems`, when the text is not a closed loop.
Func<string, string, List<double[]>> loopOf = (text, where) =>
{
    var said = System.Text.RegularExpressions.Regex.Replace((text ?? "").Trim(), @"\s*,\s*", ",");
    var words = said.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    var head = words.Length == 0 ? "" : words[0].ToLowerInvariant();
    var segments = new List<double[]>();

    if (head == "circle")
    {
        var centre = words.Length == 3 ? pair(words[1]) : null;
        var radius = words.Length == 3 ? number(words[2]) : null;
        if (centre == null || !radius.HasValue || radius.Value <= 0)
        {
            problems.Add(where + " is not a circle written \"circle CX,CY R\" - its centre, then its radius in "
                + "millimetres, as in \"circle 0,0 150\". It reads \"" + said + "\".");
            return null;
        }
        var r = radius.Value;
        // TWO HALVES, NEVER ONE CLOSED CURVE. A blend and a swept blend refuse a
        // loop of one curve outright, and an extrusion splits it in two anyway.
        segments.Add(new[] { 1.0, centre[0] + r, centre[1], centre[0], centre[1] + r, centre[0] - r, centre[1] });
        segments.Add(new[] { 1.0, centre[0] - r, centre[1], centre[0], centre[1] - r, centre[0] + r, centre[1] });
    }
    else if (head == "rect")
    {
        var a = words.Length == 3 ? pair(words[1]) : null;
        var b = words.Length == 3 ? pair(words[2]) : null;
        if (a == null || b == null)
        {
            problems.Add(where + " is not a rectangle written \"rect X1,Y1 X2,Y2\" - two opposite corners, as in "
                + "\"rect -300,-200 300,200\". It reads \"" + said + "\".");
            return null;
        }
        segments.Add(new[] { 0.0, a[0], a[1], 0, 0, b[0], a[1] });
        segments.Add(new[] { 0.0, b[0], a[1], 0, 0, b[0], b[1] });
        segments.Add(new[] { 0.0, b[0], b[1], 0, 0, a[0], b[1] });
        segments.Add(new[] { 0.0, a[0], b[1], 0, 0, a[0], a[1] });
    }
    else if (head == "polygon")
    {
        var centre = words.Length == 4 ? pair(words[1]) : null;
        var radius = words.Length == 4 ? number(words[2]) : null;
        var sides = words.Length == 4 ? number(words[3]) : null;
        if (centre == null || !radius.HasValue || radius.Value <= 0 || !sides.HasValue
            || sides.Value != Math.Floor(sides.Value) || sides.Value < 3 || sides.Value > 64)
        {
            problems.Add(where + " is not a regular polygon written \"polygon CX,CY R N\" - its centre, the radius "
                + "to a corner and 3 to 64 sides, as in \"polygon 0,0 100 6\". It reads \"" + said + "\".");
            return null;
        }
        var n = (int)sides.Value;
        for (var k = 0; k < n; k++)
        {
            var a0 = 2 * Math.PI * k / n;
            var a1 = 2 * Math.PI * (k + 1) / n;
            segments.Add(new[] { 0.0,
                centre[0] + radius.Value * Math.Cos(a0), centre[1] + radius.Value * Math.Sin(a0), 0, 0,
                centre[0] + radius.Value * Math.Cos(a1), centre[1] + radius.Value * Math.Sin(a1) });
        }
    }
    else
    {
        var items = said.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        var first = items.Count == 0 ? null : pair(items[0]);
        if (first == null)
        {
            problems.Add(where + " does not start with a corner \"X,Y\". A loop is \"circle ...\", \"rect ...\", "
                + "\"polygon ...\" or corners with semicolons between, as in \"0,0; 600,0; 600,300; 0,300\". It "
                + "reads \"" + said + "\".");
            return null;
        }
        var at = first;
        foreach (var item in items.Skip(1))
        {
            var parts = item.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && parts[0].ToLowerInvariant() == "arc")
            {
                var through = parts.Length == 3 ? pair(parts[1]) : null;
                var end = parts.Length == 3 ? pair(parts[2]) : null;
                if (through == null || end == null)
                {
                    problems.Add(where + ": \"" + item + "\" is not an arc written \"arc MX,MY X,Y\" - a point "
                        + "the arc passes through, then where it ends.");
                    return null;
                }
                segments.Add(new[] { 1.0, at[0], at[1], through[0], through[1], end[0], end[1] });
                at = end;
            }
            else
            {
                var next = parts.Length == 1 ? pair(parts[0]) : null;
                if (next == null)
                {
                    problems.Add(where + ": \"" + item + "\" is neither a corner \"X,Y\" nor an arc \"arc MX,MY X,Y\".");
                    return null;
                }
                segments.Add(new[] { 0.0, at[0], at[1], 0, 0, next[0], next[1] });
                at = next;
            }
        }
        // CLOSED BACK TO THE FIRST CORNER - by a straight line when the last
        // corner is not already there, by snapping it when it is all but there.
        var gap = Math.Sqrt(Math.Pow(at[0] - first[0], 2) + Math.Pow(at[1] - first[1], 2));
        if (gap >= shortest) segments.Add(new[] { 0.0, at[0], at[1], 0, 0, first[0], first[1] });
        else if (segments.Count > 0)
        {
            var last = segments[segments.Count - 1];
            last[5] = first[0];
            last[6] = first[1];
        }
    }

    if (segments.Count < 2 || (segments.Count < 3 && segments.All(s => s[0] == 0.0)))
    {
        problems.Add(where + " has " + segments.Count + " side(s) - a closed loop needs at least three straight "
            + "sides, or an arc and one more side.");
        return null;
    }

    foreach (var s in segments)
    {
        var chord = Math.Sqrt(Math.Pow(s[5] - s[1], 2) + Math.Pow(s[6] - s[2], 2));
        if (s[0] == 0.0 && chord < shortest)
        {
            problems.Add(where + " has a side from " + plain(s[1]) + "," + plain(s[2]) + " to " + plain(s[5]) + ","
                + plain(s[6]) + " shorter than Revit's shortest line, " + plain(shortest) + " mm.");
            return null;
        }
        if (s[0] == 1.0)
        {
            var ux = s[3] - s[1]; var uy = s[4] - s[2];
            var vx = s[5] - s[1]; var vy = s[6] - s[2];
            var lu = Math.Sqrt(ux * ux + uy * uy);
            var lv = Math.Sqrt(vx * vx + vy * vy);
            var lw = Math.Sqrt(Math.Pow(s[5] - s[3], 2) + Math.Pow(s[6] - s[4], 2));
            if (lu < shortest || lv < shortest || lw < shortest
                || Math.Abs(ux * vy - uy * vx) < 1e-6 * lu * lv)
            {
                problems.Add(where + " has an arc from " + plain(s[1]) + "," + plain(s[2]) + " through "
                    + plain(s[3]) + "," + plain(s[4]) + " to " + plain(s[5]) + "," + plain(s[6]) + " that is not "
                    + "an arc - its three points are in a line, or two are in the same place.");
                return null;
            }
        }
    }

    // A LOOP WITH NO AREA - every corner on one line - is refused here rather
    // than by Revit. The corners and each arc's middle point are enough to tell.
    var ring = new List<double[]>();
    foreach (var s in segments)
    {
        ring.Add(new[] { s[1], s[2] });
        if (s[0] == 1.0) ring.Add(new[] { s[3], s[4] });
    }
    var area = 0.0;
    for (var i = 0; i < ring.Count; i++)
    {
        var p = ring[i];
        var q = ring[(i + 1) % ring.Count];
        area += p[0] * q[1] - q[0] * p[1];
    }
    if (Math.Abs(area) / 2 < shortest * shortest)
    {
        problems.Add(where + " encloses no area - its corners lie on one line.");
        return null;
    }
    return segments;
};


// ---- the host, the shape ------------------------------------------------------

List<double[]> shapeLoop = null;

Element host = null;
var hostKind = "";
// The axis the host's plane faces, and where along it the shape is drawn.
var facing = -1;
var planeAt = 0.0;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "An opening in a project's wall or floor is CREATE_OPENING; this one is cut in a hosted family's template "
        + "host - open the family first.";
}
else
{
    var walls = new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().ToList();
    var ceilings = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Ceilings)
        .WhereElementIsNotElementType().ToList();
    var floorsOrRoofs = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Floors)
        .WhereElementIsNotElementType().GetElementCount()
        + new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Roofs).WhereElementIsNotElementType()
        .GetElementCount();

    if (walls.Count + ceilings.Count == 0)
        problems.Add(floorsOrRoofs > 0
            ? "This family's template carries a floor or a roof, and Revit's opening call takes only a wall or a "
              + "ceiling. Cut a floor- or roof-based family's host with a void form instead."
            : "This family's template carries no wall or ceiling to cut - the Opening tool belongs to wall-based and "
              + "ceiling-based families (REPORT_FAMILY_TEMPLATE says which this is).");
    else if (walls.Count + ceilings.Count > 1)
        problems.Add("This family holds " + (walls.Count + ceilings.Count) + " walls and ceilings, so which one to cut "
            + "is not clear. A hosted template carries one.");
    else if (walls.Count == 1)
    {
        host = walls[0];
        hostKind = "wall";
        var location = walls[0].Location as LocationCurve;
        var line = location == null ? null : location.Curve as Line;
        var runs = line == null ? -1 : axisOf(line.Direction);
        if (runs != 0 && runs != 1)
            problems.Add("The template's wall does not run straight along X or Y, so its face has no two model "
                + "coordinates to write the shape in.");
        else
        {
            // The shape is written along the wall and up it, on the wall's
            // centre plane.
            facing = runs == 0 ? 1 : 0;
            planeAt = along(line.GetEndPoint(0), facing);
        }
    }
    else
    {
        host = ceilings[0];
        hostKind = "ceiling";
        facing = 2;
        var box = host.get_BoundingBox(null);
        planeAt = box == null ? 0 : box.Min.Z;
    }

    List<double[]> loop = null;
    var texts = (shape ?? "").Split('|').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
    if (texts.Count != 1)
        problems.Add("An opening takes ONE closed loop - " + texts.Count + " were given.");
    else if (texts[0].ToLowerInvariant().StartsWith("path"))
        problems.Add("The shape is an open path; an opening is one closed loop.");
    else loop = loopOf(texts[0], "The shape");
    shapeLoop = loop;

    if (problems.Count > 0) refused = "Nothing was cut. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// CUT, THEN READ THE OPENING BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var inPlane = new[] { new[] { 1, 2 }, new[] { 0, 2 }, new[] { 0, 1 } };
    var u = inPlane[facing][0];
    var v = inPlane[facing][1];
    Func<double, double, XYZ> onPlane = (first, second) =>
    {
        var xyz = new double[3];
        xyz[facing] = planeAt;
        xyz[u] = first / 304.8;
        xyz[v] = second / 304.8;
        return new XYZ(xyz[0], xyz[1], xyz[2]);
    };

    var profile = new CurveArray();
    // The outline's true extent - every curve tessellated, so an arc's bulge
    // counts - in the host's two coordinates, to read the opening back against.
    var drawnPoints = new List<XYZ>();
    foreach (var s in shapeLoop)
    {
        var a = onPlane(s[1], s[2]);
        var b = onPlane(s[5], s[6]);
        Curve piece = s[0] == 0.0 ? (Curve)Line.CreateBound(a, b) : Arc.Create(a, b, onPlane(s[3], s[4]));
        profile.Append(piece);
        drawnPoints.AddRange(piece.Tessellate());
    }
    var uMin = drawnPoints.Min(p => along(p, u)); var uMax = drawnPoints.Max(p => along(p, u));
    var vMin = drawnPoints.Min(p => along(p, v)); var vMax = drawnPoints.Max(p => along(p, v));

    Opening opening;
    try { opening = doc.FamilyCreate.NewOpening(host, profile); }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the opening in the template's " + hostKind + ": " + ex.Message
            + " The call failed, and Heron rolls the whole call back.");
    }
    if (opening == null)
        throw new InvalidOperationException("Revit made no opening. The call failed, and Heron rolls the whole call back.");

    doc.Regenerate();

    if (opening.Host == null || opening.Host.Id != host.Id)
        throw new InvalidOperationException("The opening reads hosted by " + (opening.Host == null ? "nothing"
            : "\"" + opening.Host.Name + "\"") + ", not the template's " + hostKind + ". The call failed, and Heron rolls "
            + "the whole call back.");
    var read = new List<XYZ>();
    foreach (Curve c in opening.BoundaryCurves) read.AddRange(c.Tessellate());
    if (read.Count == 0)
        throw new InvalidOperationException("The opening has no boundary to read back. The call failed, and Heron rolls "
            + "the whole call back.");
    var ru0 = read.Min(p => along(p, u)); var ru1 = read.Max(p => along(p, u));
    var rv0 = read.Min(p => along(p, v)); var rv1 = read.Max(p => along(p, v));
    // THE OUTLINE READ BACK AGAINST THE OUTLINE DRAWN, both tessellated, to a
    // millimetre - the tolerance a tessellated arc stays inside.
    var oneMillimetre = 1.0 / 304.8;
    if (Math.Abs(ru0 - uMin) > oneMillimetre || Math.Abs(ru1 - uMax) > oneMillimetre
        || Math.Abs(rv0 - vMin) > oneMillimetre || Math.Abs(rv1 - vMax) > oneMillimetre)
        throw new InvalidOperationException("The opening reads " + letters[u] + " " + mm(ru0) + " to " + mm(ru1) + ", "
            + letters[v] + " " + mm(rv0) + " to " + mm(rv1) + " mm, not where it was drawn - " + letters[u] + " "
            + mm(uMin) + " to " + mm(uMax) + ", " + letters[v] + " " + mm(vMin) + " to " + mm(vMax) + " mm. The call "
            + "failed, and Heron rolls the whole call back.");

    openingId = opening.UniqueId;
    built = "An opening in the template's " + hostKind + ", " + letters[u] + " " + mm(ru0) + " to " + mm(ru1) + ", "
        + letters[v] + " " + mm(rv0) + " to " + mm(rv1) + " mm - read back.";
    findings.Add(built);
    findings.Add("Its size is FIXED as drawn: an opening's sketch cannot be locked to planes through Revit's API. An "
        + "opening that must follow a parameter is a void form locked to planes instead - whether that void cuts the "
        + "template's host by itself is NEEDS-CHECKING BV12.");
    findings.Add("Placed in a project, the family cuts this opening in the " + hostKind + " it is placed on.");
}

if (refused != null) findings.Add(refused);
