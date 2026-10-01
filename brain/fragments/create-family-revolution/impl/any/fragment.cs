// NOT STANDALONE. Assumes `doc`, `workPlane`, `profile`, `axisLine`,
// `startDegrees`, `endDegrees` and `solid` are in scope; leaves `formId`,
// `built`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A SHAPE TURNED ABOUT AN AXIS - a cone, a dome, a knob, a flange, a pipe bend.
// The profile is the half-section, drawn wholly on one side of the axis, on a
// plane the axis lies in: a vertical axis on "Center (Front/Back)" turns into
// a round body standing up.
//
// THE PROFILE AND THE AXIS ARE WRITTEN THE SAME WAY, in the plane's own two
// model coordinates, millimetres from the family origin: X,Y on a level or a
// horizontal plane, X,Z on a plane facing front or back, Y,Z on a plane facing
// left or right. Loops `|` between: "circle CX,CY R", "rect X1,Y1 X2,Y2",
// "polygon CX,CY R N", or corners "X,Y; X,Y; arc MX,MY X,Y". The axis is two
// points, "X1,Y1 X2,Y2".
//
// A PROFILE CROSSING ITS AXIS IS REFUSED HERE, by name, rather than by Revit
// with a message nobody can place: every corner and every arc's middle point
// must lie on one side of the axis line or on it.
//
// THE ANGLES ARE REVIT'S OWN START AND END ANGLE, in degrees. A full turn is
// 0 to 360. Which way a part-turn sweeps is Revit's rule for the plane's
// normal, and it is READ BACK in the extent rather than predicted here.
//
// SKETCHED ON THE PLANE ITSELF, from its element id, so it moves with it.
//
// A VOID CUTS NOTHING BY BEING MADE. Through the API a void in an ordinary
// family cuts a solid only once the two are combined - Revit's Cut Geometry.
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
var loops = new List<List<double[]>>();
double[] axisFrom = null, axisTo = null;

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
        problems.Add("\"" + wanted + "\" is at an angle. A profile here is written in the model's own "
            + "coordinates, so the plane must face left-right, front-back or up.");
    else plane = found[0];

    var pieces = (profile ?? "").Split('|').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
    if (pieces.Count == 0) problems.Add("No profile was given.");
    for (var i = 0; i < pieces.Count; i++)
    {
        var loop = loopOf(pieces[i], pieces.Count == 1 ? "The profile" : "Loop " + (i + 1) + " of the profile");
        if (loop != null) loops.Add(loop);
    }

    var ends = System.Text.RegularExpressions.Regex.Replace((axisLine ?? "").Trim(), @"\s*,\s*", ",")
        .Split(new[] { ' ', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries);
    axisFrom = ends.Length == 2 ? pair(ends[0]) : null;
    axisTo = ends.Length == 2 ? pair(ends[1]) : null;
    if (axisFrom == null || axisTo == null)
        problems.Add("The axis is not two points written \"X1,Y1 X2,Y2\" - \"0,0 0,1000\" is a vertical axis "
            + "through the origin on a plane facing front or back. It reads \"" + (axisLine ?? "").Trim() + "\".");
    else if (Math.Sqrt(Math.Pow(axisTo[0] - axisFrom[0], 2) + Math.Pow(axisTo[1] - axisFrom[1], 2)) < shortest)
        problems.Add("The axis's two points are in the same place, so it has no direction.");

    if (endDegrees <= startDegrees)
        problems.Add("The end angle, " + plain(endDegrees) + " degrees, is not past the start angle, "
            + plain(startDegrees) + ". A full turn is 0 to 360.");
    else if (endDegrees - startDegrees > 360.0 + 1e-9)
        problems.Add("From " + plain(startDegrees) + " to " + plain(endDegrees) + " degrees is more than one "
            + "full turn.");

    // EVERY CORNER AND EVERY ARC'S MIDDLE POINT ON ONE SIDE OF THE AXIS LINE.
    if (problems.Count == 0)
    {
        var dx = axisTo[0] - axisFrom[0];
        var dy = axisTo[1] - axisFrom[1];
        var length = Math.Sqrt(dx * dx + dy * dy);
        var left = false;
        var right = false;
        foreach (var loop in loops)
            foreach (var s in loop)
                foreach (var p in s[0] == 1.0
                    ? new[] { new[] { s[1], s[2] }, new[] { s[3], s[4] } } : new[] { new[] { s[1], s[2] } })
                {
                    var side = (dx * (p[1] - axisFrom[1]) - dy * (p[0] - axisFrom[0])) / length;
                    if (side > shortest) left = true;
                    if (side < -shortest) right = true;
                }
        if (left && right)
            problems.Add("The profile crosses its axis. A revolve's profile lies wholly on one side of the axis - "
                + "draw half the section, from the axis outwards.");
        if (!left && !right)
            problems.Add("The profile lies on its axis, so turning it encloses nothing.");
    }

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

    Revolution form;
    var curves = new List<Curve>();
    Line turnAbout;

    try
    {
        var sketch = SketchPlane.Create(doc, plane.Item1.Id);
        var surface = sketch.GetPlane();
        if (axisOf(surface.Normal) != axis || Math.Abs(along(surface.Origin, axis) - at) > halfMillimetre)
            throw new InvalidOperationException("the sketch plane made on \"" + plane.Item4 + "\" does not lie on it");

        var outline = new CurveArrArray();
        foreach (var loop in loops)
        {
            var array = new CurveArray();
            foreach (var s in loop)
            {
                var a = onPlane(s[1], s[2]);
                var b = onPlane(s[5], s[6]);
                Curve piece = s[0] == 0.0 ? (Curve)Line.CreateBound(a, b) : Arc.Create(a, b, onPlane(s[3], s[4]));
                array.Append(piece);
                curves.Add(piece);
            }
            outline.Append(array);
        }

        turnAbout = Line.CreateBound(onPlane(axisFrom[0], axisFrom[1]), onPlane(axisTo[0], axisTo[1]));
        form = doc.FamilyCreate.NewRevolution(solid, outline, sketch, turnAbout,
            startDegrees * Math.PI / 180.0, endDegrees * Math.PI / 180.0);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the revolve on \"" + plane.Item4 + "\": " + ex.Message
            + " NOTHING from this call was kept.");
    }

    doc.Regenerate();

    // THE EXTENT IS MEASURED ON THE SOLID ITSELF, never taken from the form's
    // bounding box. That box comes from Revit's tessellation: a 32 mm sphere
    // read 1.57 mm short on each side and was refused (5b-277). Every edge is
    // walked, and every face is sampled on its own surface inside its trim, so
    // the extent sits within a sampling step of the true curved surface.
    var volume = 0.0;
    var low = new[] { double.MaxValue, double.MaxValue, double.MaxValue };
    var high = new[] { double.MinValue, double.MinValue, double.MinValue };
    var measured = false;
    Action<XYZ> take = p =>
    {
        for (var i = 0; i < 3; i++)
        {
            low[i] = Math.Min(low[i], along(p, i));
            high[i] = Math.Max(high[i], along(p, i));
        }
        measured = true;
    };
    var geometry = form.get_Geometry(new Options());
    if (geometry != null)
        foreach (GeometryObject piece in geometry)
        {
            var body = piece as Solid;
            if (body == null || body.Faces.Size == 0) continue;
            if (body.Volume > 0) volume += body.Volume;
            foreach (Edge edge in body.Edges)
                for (var k = 0; k <= 256; k++)
                    take(edge.Evaluate(k / 256.0));
            foreach (Face face in body.Faces)
            {
                var box = face.GetBoundingBox();
                for (var i = 0; i <= 128; i++)
                    for (var j = 0; j <= 64; j++)
                    {
                        var uv = new UV(box.Min.U + (box.Max.U - box.Min.U) * i / 128.0,
                            box.Min.V + (box.Max.V - box.Min.V) * j / 64.0);
                        if (face.IsInside(uv)) take(face.Evaluate(uv));
                    }
            }
        }

    // A FORM WITH NO FACES TO MEASURE - a void can come back with none - falls
    // back to its bounding box, and to the allowance a tessellated box needs.
    var bounds = form.get_BoundingBox(null);
    if (!measured && bounds == null)
        throw new InvalidOperationException("The revolve was built and has no extent to read back. NOTHING from "
            + "this call was kept.");
    Func<int, double> readLow = i => measured ? low[i] : along(bounds.Min, i);
    Func<int, double> readHigh = i => measured ? high[i] : along(bounds.Max, i);

    // A FULL TURN ABOUT AN AXIS LYING ALONG A MODEL AXIS HAS AN EXTENT THAT CAN
    // BE WORKED OUT: along the axis, what the profile covers; across it, the
    // axis plus and minus the profile's furthest reach. Read back against that.
    var direction = turnAbout.Direction;
    var spin = axisOf(direction);
    if (endDegrees - startDegrees >= 360.0 - 1e-9 && spin >= 0)
    {
        var origin = turnAbout.GetEndPoint(0);
        double lo = double.MaxValue, hi = double.MinValue, reach = 0.0;
        foreach (var c in curves)
            for (var k = 0; k <= 256; k++)
            {
                var p = c.Evaluate(k / 256.0, true);
                lo = Math.Min(lo, along(p, spin));
                hi = Math.Max(hi, along(p, spin));
                var off = p - origin;
                reach = Math.Max(reach, (off - direction.Multiply(off.DotProduct(direction))).GetLength());
            }
        // MEASURED ON THE SURFACE, the extent is short of the true one by no more
        // than a sampling step - under 0.06 percent of the size at 128 steps round
        // and 64 along - so the allowance is half a millimetre plus a tenth of a
        // percent: under 0.6 mm on a 32 mm sphere, still far too little for a
        // profile on the wrong axes. Only a tessellated box keeps the old, wider
        // one: a millimetre plus half a percent.
        var size = Math.Max(hi - lo, 2 * reach);
        var slack = measured ? halfMillimetre + 0.001 * size : 1.0 / 304.8 + 0.005 * size;
        var expected = new List<Tuple<int, double, double>> { Tuple.Create(spin, lo, hi) };
        foreach (var index in new[] { 0, 1, 2 }.Where(i => i != spin))
            expected.Add(Tuple.Create(index, along(origin, index) - reach, along(origin, index) + reach));
        foreach (var e in expected)
            if (Math.Abs(readLow(e.Item1) - e.Item2) > slack
                || Math.Abs(readHigh(e.Item1) - e.Item3) > slack)
                throw new InvalidOperationException("The revolve reads " + axisLetters[e.Item1] + " "
                    + mm(readLow(e.Item1)) + " to " + mm(readHigh(e.Item1)) + " mm, and a full "
                    + "turn of this profile about this axis covers " + mm(e.Item2) + " to " + mm(e.Item3) + " mm. "
                    + "NOTHING from this call was kept.");
    }

    if (solid && volume <= 0)
        throw new InvalidOperationException("The revolve was built and holds no solid to measure. NOTHING from "
            + "this call was kept.");

    formId = form.UniqueId;
    built = (solid ? "Solid" : "Void") + " revolve on \"" + plane.Item4 + "\" (" + facing[axis] + ", "
        + axisLetters[axis] + " " + mm(at) + " mm): " + loops.Count + " loop(s) turned from " + plain(startDegrees)
        + " to " + plain(endDegrees) + " degrees about the axis " + plain(axisFrom[0]) + "," + plain(axisFrom[1])
        + " to " + plain(axisTo[0]) + "," + plain(axisTo[1]) + ". Reads "
        + string.Join(", ", new[] { 0, 1, 2 }.Select(i => axisLetters[i] + " " + mm(readLow(i)) + " to "
            + mm(readHigh(i)))) + " mm"
        + (solid ? "; volume " + Math.Round(volume * 28.316846592, 3).ToString(invariant) + " L." : ".");

    findings.Add("Built: " + built + " Its id is " + formId + ".");
    if (endDegrees - startDegrees < 360.0 - 1e-9)
        findings.Add("A part-turn: which way it swept is Revit's rule for the plane's normal - LOOK at it, and if it "
            + "went the wrong way, run it again with the angles moved by 180 degrees.");
    findings.Add("Its sketch is hosted on \"" + plane.Item4 + "\" and moves with it. Nothing else ties it to the "
        + "family's planes or parameters yet - LOCK_FORM_TO_PLANES locks its flat ends, and LABEL_FAMILY_RADIUS "
        + "labels a round size.");
    if (!solid)
        findings.Add("A void CUTS NOTHING by being made. In a family built through the API it cuts a solid only "
            + "once the two are combined - Revit's Cut Geometry, which COMBINE_FAMILY_FORMS does with this "
            + "id and the solid's.");
}

if (refused != null) findings.Add(refused);
