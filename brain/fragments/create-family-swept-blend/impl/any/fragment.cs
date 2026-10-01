// NOT STANDALONE. Assumes `doc`, `workPlane`, `path`, `bottomProfile`,
// `topProfile` and `solid` are in scope; leaves `formId`, `built`,
// `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// ONE SHAPE CHANGING INTO ANOTHER ALONG ONE PIECE OF PATH - a reducing bend, a
// rectangular-to-round elbow, a horn. The path is drawn on the named plane;
// the bottom profile sits square to its start and the top profile square to
// its end.
//
// THE PATH IS ONE PIECE - Revit's own rule for a swept blend - written in the
// plane's own two model coordinates, millimetres from the family origin: a
// straight "X,Y; X,Y" or an arc "X,Y; arc MX,MY X,Y". X,Y on a level or a
// horizontal plane, X,Z on a plane facing front or back, Y,Z on a plane facing
// left or right.
//
// EACH PROFILE IS ONE LOOP IN ITS OWN TWO COORDINATES, millimetres, 0,0 where
// the path passes through it - "circle 0,0 100", "rect -150,-100 150,100".
// Revit's remarks: each lies in its XY plane and is moved onto the path by
// Revit, and a circle must be at least two pieces for its corners to pair - so
// every circle is split before Revit sees it. A profile symmetric about 0,0
// comes out the same whichever way Revit turns it; an asymmetric one must be
// LOOKED AT.
//
// SKETCHED ON THE PLANE ITSELF, from its element id, so the path moves with it.
//
// A VOID CUTS NOTHING BY BEING MADE - not until it is combined with a solid,
// Revit's Cut Geometry.
//
// ANY REFUSAL FROM REVIT AFTER THE CHECKS THROWS, and the host rolls the whole
// call back.

var findings = new List<string>();
var formId = "";
var built = "";
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
Func<double, string> plain = value => Math.Round(value, 2).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;

// Which world axis a direction lies along: 0 X, 1 Y, 2 Z, -1 none of them.
Func<XYZ, int> axisOf = n =>
    Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;
Func<XYZ, int, double> along = (point, index) => index == 0 ? point.X : index == 1 ? point.Y : point.Z;
var axisLetters = new[] { "X", "Y", "Z" };
var facing = new[] { "facing left or right", "facing front or back", "horizontal" };
// The two model coordinates a profile is written in, per axis the plane faces.
var inPlane = new[] { new[] { 1, 2 }, new[] { 0, 2 }, new[] { 0, 1 } };

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};

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


// ---- the plane ------------------------------------------------------------

// (element, axis it faces, position along that axis in feet, name) for every
// named reference plane and every level.
var datums = new List<Tuple<Element, int, double, string>>();
if (doc.IsFamilyDocument)
{
    foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
    {
        var name = ownName(rp);
        if (name.Length == 0) continue;
        var at = axisOf(rp.Normal);
        datums.Add(Tuple.Create((Element)rp, at, at < 0 ? 0.0 : along(rp.GetPlane().Origin, at), name));
    }
    foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
        datums.Add(Tuple.Create((Element)level, 2, level.Elevation, level.Name));
}

Tuple<Element, int, double, string> plane = null;
double[] piece = null;
List<double[]> bottomLoop = null, topLoop = null;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A family's forms are "
        + "built inside the family - open it first.";
}
else
{
    var wanted = (workPlane ?? "").Trim();
    var found = datums.Where(d => string.Equals(d.Item4, wanted, StringComparison.OrdinalIgnoreCase)).ToList();
    if (wanted.Length == 0) problems.Add("No work plane was named.");
    else if (found.Count == 0)
        problems.Add("No reference plane or level is called \"" + wanted + "\". This family has "
            + (datums.Count == 0 ? "no named ones."
                : string.Join(", ", datums.Select(d => d.Item4).Distinct().OrderBy(n => n).Take(20)) + "."));
    else if (found.Count > 1) problems.Add("\"" + wanted + "\" names " + found.Count + " planes - rename one first.");
    else if (found[0].Item2 < 0)
        problems.Add("\"" + wanted + "\" is at an angle. A path here is written in the model's own coordinates, "
            + "so the plane must face left-right, front-back or up.");
    else plane = found[0];

    // THE PATH: ONE STRAIGHT PIECE OR ONE ARC, and nothing after it.
    var said = System.Text.RegularExpressions.Regex.Replace((path ?? "").Trim(), @"\s*,\s*", ",");
    var items = said.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
    var start = items.Count == 2 ? pair(items[0]) : null;
    var parts = items.Count == 2 ? items[1].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                 : new string[0];
    if (start != null && parts.Length == 1 && pair(parts[0]) != null)
    {
        var end = pair(parts[0]);
        piece = new[] { 0.0, start[0], start[1], 0, 0, end[0], end[1] };
        if (Math.Sqrt(Math.Pow(end[0] - start[0], 2) + Math.Pow(end[1] - start[1], 2)) < shortest)
        {
            problems.Add("The path is shorter than Revit's shortest line, " + plain(shortest) + " mm.");
            piece = null;
        }
    }
    else if (start != null && parts.Length == 3 && parts[0].ToLowerInvariant() == "arc"
             && pair(parts[1]) != null && pair(parts[2]) != null)
    {
        var through = pair(parts[1]);
        var end = pair(parts[2]);
        var ux = through[0] - start[0]; var uy = through[1] - start[1];
        var vx = end[0] - start[0]; var vy = end[1] - start[1];
        var lu = Math.Sqrt(ux * ux + uy * uy);
        var lv = Math.Sqrt(vx * vx + vy * vy);
        var lw = Math.Sqrt(Math.Pow(end[0] - through[0], 2) + Math.Pow(end[1] - through[1], 2));
        if (lu < shortest || lv < shortest || lw < shortest || Math.Abs(ux * vy - uy * vx) < 1e-6 * lu * lv)
            problems.Add("The path's arc is not an arc - its three points are in a line, or two are in the same "
                + "place.");
        else piece = new[] { 1.0, start[0], start[1], through[0], through[1], end[0], end[1] };
    }
    else
        problems.Add("A swept blend's path is ONE piece: a straight \"X,Y; X,Y\" or an arc \"X,Y; arc MX,MY X,Y\". "
            + "It reads \"" + said + "\". A longer path is a sweep, which carries one profile.");

    if ((bottomProfile ?? "").Contains("|"))
        problems.Add("The bottom profile has more than one loop. A swept blend's profiles are one loop each.");
    else if ((bottomProfile ?? "").Trim().Length == 0) problems.Add("No bottom profile was given.");
    else bottomLoop = loopOf(bottomProfile, "The bottom profile");

    if ((topProfile ?? "").Contains("|"))
        problems.Add("The top profile has more than one loop. A swept blend's profiles are one loop each.");
    else if ((topProfile ?? "").Trim().Length == 0) problems.Add("No top profile was given.");
    else topLoop = loopOf(topProfile, "The top profile");

    if (problems.Count > 0) refused = "Nothing was built. " + string.Join(" ", problems);
}

if (refused == null)
{
    var axis = plane.Item2;
    var u = inPlane[axis][0];
    var v = inPlane[axis][1];
    var at = plane.Item3;

    Func<double, double, XYZ> onPlane = (first, second) =>
    {
        var xyz = new double[3];
        xyz[axis] = at;
        xyz[u] = first / 304.8;
        xyz[v] = second / 304.8;
        return new XYZ(xyz[0], xyz[1], xyz[2]);
    };
    Func<double, double, XYZ> flat = (first, second) => new XYZ(first / 304.8, second / 304.8, 0);
    Func<List<double[]>, CurveArrArray> loopArray = loop =>
    {
        var array = new CurveArray();
        foreach (var s in loop)
        {
            var a = flat(s[1], s[2]);
            var b = flat(s[5], s[6]);
            array.Append(s[0] == 0.0 ? (Curve)Line.CreateBound(a, b) : Arc.Create(a, b, flat(s[3], s[4])));
        }
        var one = new CurveArrArray();
        one.Append(array);
        return one;
    };

    SweptBlend form;
    Curve route;

    try
    {
        var sketch = SketchPlane.Create(doc, plane.Item1.Id);
        var surface = sketch.GetPlane();
        if (axisOf(surface.Normal) != axis || Math.Abs(along(surface.Origin, axis) - at) > halfMillimetre)
            throw new InvalidOperationException("the sketch plane made on \"" + plane.Item4 + "\" does not lie on it");

        var a = onPlane(piece[1], piece[2]);
        var b = onPlane(piece[5], piece[6]);
        route = piece[0] == 0.0 ? (Curve)Line.CreateBound(a, b) : Arc.Create(a, b, onPlane(piece[3], piece[4]));

        var bottom = doc.Application.Create.NewCurveLoopsProfile(loopArray(bottomLoop));
        var top = doc.Application.Create.NewCurveLoopsProfile(loopArray(topLoop));
        form = doc.FamilyCreate.NewSweptBlend(solid, route, sketch, bottom, top);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the swept blend on \"" + plane.Item4 + "\": " + ex.Message
            + " NOTHING from this call was kept.");
    }

    doc.Regenerate();

    var bounds = form.get_BoundingBox(null);
    if (bounds == null)
        throw new InvalidOperationException("The swept blend was built and has no extent to read back. NOTHING "
            + "from this call was kept.");

    var volume = 0.0;
    var geometry = form.get_Geometry(new Options());
    if (geometry != null)
        foreach (GeometryObject part in geometry)
        {
            var body = part as Solid;
            if (body != null && body.Volume > 0) volume += body.Volume;
        }
    if (solid && volume <= 0)
        throw new InvalidOperationException("The swept blend was built and holds no solid to measure. NOTHING from "
            + "this call was kept.");

    formId = form.UniqueId;
    built = (solid ? "Solid" : "Void") + " swept blend on \"" + plane.Item4 + "\" (" + facing[axis] + ", "
        + axisLetters[axis] + " " + mm(at) + " mm): " + (piece[0] == 0.0 ? "a straight path" : "an arc path") + " "
        + mm(route.Length) + " mm long. Reads "
        + string.Join(", ", new[] { 0, 1, 2 }.Select(i => axisLetters[i] + " " + mm(along(bounds.Min, i)) + " to "
            + mm(along(bounds.Max, i)))) + " mm"
        + (solid ? "; volume " + Math.Round(volume * 28.316846592, 3).ToString(invariant) + " L." : ".");

    findings.Add("Built: " + built + " Its id is " + formId + ".");
    findings.Add("The bottom profile was laid square to the path's start and the top square to its end, turned by "
        + "Revit's own rule, with their corners paired by Revit - LOOK at an asymmetric profile. The path's sketch "
        + "is hosted on \"" + plane.Item4 + "\" and moves with it.");
    if (!solid)
        findings.Add("A void CUTS NOTHING by being made. In a family built through the API it cuts a solid only "
            + "once the two are combined - Revit's Cut Geometry, which COMBINE_FAMILY_FORMS does with this "
            + "id and the solid's.");
}

if (refused != null) findings.Add(refused);
