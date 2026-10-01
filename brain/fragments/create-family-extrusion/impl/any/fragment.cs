// NOT STANDALONE. Assumes `doc`, `workPlane`, `profile`, `startMm`, `endMm` and
// `solid` are in scope; leaves `formId`, `built`, `notAFamily`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// ANY CLOSED SHAPE, NOT ONLY A BOX. A profile is one or more loops, `|`
// between them: "circle CX,CY R", "rect X1,Y1 X2,Y2", "polygon CX,CY R N", or
// corners "X,Y; X,Y; arc MX,MY X,Y; X,Y" - a straight line to each corner, an
// arc through MX,MY to X,Y, and the loop closed back to its first corner. A
// loop inside another is a hole; a loop beside the others is a second outline,
// a second body - Revit decides by where the loops lie, not by their order, and
// so does the report (5b-275).
//
// THE PLANE'S OWN TWO MODEL COORDINATES, IN MILLIMETRES FROM THE FAMILY ORIGIN.
// On a level or a horizontal plane they are X,Y as seen in plan; on a plane
// facing front or back, X,Z; on a plane facing left or right, Y,Z. The plane
// fixes the third. Never relative to the plane, never mirrored by which way
// the plane happens to face - so the same numbers mean the same place whichever
// plane is named.
//
// START AND END RUN ALONG THE MODEL AXIS THE PLANE FACES, POSITIVE UP (+Z),
// BACK (+Y) OR RIGHT (+X). A plane's own normal can point the other way, and
// Revit measures an extrusion along that normal - so both are turned onto the
// sketch plane's normal here, and the extent is READ BACK along the model axis
// to prove they were turned the right way.
//
// SKETCHED ON THE PLANE ITSELF, from its element id, so the shape moves with the
// plane. A sketch plane made from a copy of the plane's geometry is hosted on
// nothing and cannot be re-hosted - reported from an earlier family build.
//
// A VOID CUTS NOTHING BY BEING MADE. Through the API a void form in an ordinary
// family cuts no solid until it is combined with it - Revit's own Cut Geometry.
// The forum record and the API's own remarks agree: the solid-cut utilities
// refuse an ordinary family document, and the geometry combination is the
// family editor's join and cut. So a void is built, read back and reported as
// cutting nothing yet - never as a hole.
//
// ANY REFUSAL FROM REVIT AFTER THE CHECKS THROWS, and the host rolls the whole
// call back. Half a shape is worse than none.

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

    if (Math.Abs(endMm - startMm) * 1.0 < shortest)
        problems.Add("The extrusion starts at " + plain(startMm) + " mm and ends at " + plain(endMm) + " mm - it "
            + "needs a depth.");

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

    Extrusion form;
    var curves = new List<Curve>();
    // Each loop as points in the plane's two coordinates, to say which lies inside which.
    var rings = new List<List<double[]>>();
    double sign;

    try
    {
        var sketch = SketchPlane.Create(doc, plane.Item1.Id);
        var surface = sketch.GetPlane();
        if (axisOf(surface.Normal) != axis || Math.Abs(along(surface.Origin, axis) - at) > halfMillimetre)
            throw new InvalidOperationException("the sketch plane made on \"" + plane.Item4 + "\" does not lie on it");
        sign = along(surface.Normal, axis) > 0 ? 1.0 : -1.0;

        var outline = new CurveArrArray();
        foreach (var loop in loops)
        {
            var array = new CurveArray();
            var ring = new List<double[]>();
            foreach (var s in loop)
            {
                var a = onPlane(s[1], s[2]);
                var b = onPlane(s[5], s[6]);
                Curve piece = s[0] == 0.0 ? (Curve)Line.CreateBound(a, b) : Arc.Create(a, b, onPlane(s[3], s[4]));
                array.Append(piece);
                curves.Add(piece);
                for (var k = 0; k < 16; k++)
                {
                    var p = piece.Evaluate(k / 16.0, true);
                    ring.Add(new[] { along(p, u), along(p, v) });
                }
            }
            outline.Append(array);
            rings.Add(ring);
        }

        // Along the sketch plane's own normal: a start and an end along the model
        // axis, turned by which way that normal points. Revit makes it from the
        // plane to one end, so it is made to whichever end is not ON the plane,
        // and the other end is set afterwards - an extrusion made to zero is
        // refused, and "from 500 down to 0" is a fair thing to ask for.
        var farMm = Math.Abs(endMm) >= Math.Abs(startMm) ? endMm : startMm;
        var nearMm = Math.Abs(endMm) >= Math.Abs(startMm) ? startMm : endMm;
        form = doc.FamilyCreate.NewExtrusion(solid, outline, sketch, farMm / 304.8 * sign);
        if (Math.Abs(nearMm) > 1e-9) form.StartOffset = nearMm / 304.8 * sign;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the extrusion on \"" + plane.Item4 + "\": " + ex.Message
            + " NOTHING from this call was kept.");
    }

    doc.Regenerate();

    // READ BACK, from the form as Revit now holds it.
    var bounds = form.get_BoundingBox(null);
    if (bounds == null)
        throw new InvalidOperationException("The extrusion was built and has no extent to read back. NOTHING from "
            + "this call was kept.");

    var low = Math.Min(startMm, endMm) / 304.8 + at;
    var high = Math.Max(startMm, endMm) / 304.8 + at;
    if (Math.Abs(along(bounds.Min, axis) - low) > halfMillimetre
        || Math.Abs(along(bounds.Max, axis) - high) > halfMillimetre)
        throw new InvalidOperationException("The extrusion reads " + axisLetters[axis] + " " + mm(along(bounds.Min, axis))
            + " to " + mm(along(bounds.Max, axis)) + " mm and was asked for " + mm(low) + " to " + mm(high)
            + " mm. NOTHING from this call was kept.");

    // Across the plane it must cover what the profile covers - its outline,
    // sampled finely enough that an arc's furthest point is not missed.
    var spanMin = new[] { double.MaxValue, double.MaxValue, double.MaxValue };
    var spanMax = new[] { double.MinValue, double.MinValue, double.MinValue };
    foreach (var c in curves)
        for (var k = 0; k <= 256; k++)
        {
            var p = c.Evaluate(k / 256.0, true);
            foreach (var index in new[] { u, v })
            {
                spanMin[index] = Math.Min(spanMin[index], along(p, index));
                spanMax[index] = Math.Max(spanMax[index], along(p, index));
            }
        }
    foreach (var index in new[] { u, v })
    {
        // A CURVED FACE'S EXTENT MAY BE READ FROM ITS TESSELLATION, a little inside
        // the true arc, so the allowance grows with the size: half a percent, plus a
        // millimetre. Enough for that, far too little for a profile on the wrong axes.
        var slack = 1.0 / 304.8 + 0.005 * (spanMax[index] - spanMin[index]);
        if (Math.Abs(along(bounds.Min, index) - spanMin[index]) > slack
            || Math.Abs(along(bounds.Max, index) - spanMax[index]) > slack)
            throw new InvalidOperationException("The extrusion reads " + axisLetters[index] + " "
                + mm(along(bounds.Min, index)) + " to " + mm(along(bounds.Max, index)) + " mm and its profile "
                + "covers " + mm(spanMin[index]) + " to " + mm(spanMax[index]) + " mm. NOTHING from this call was kept.");
    }

    var volume = 0.0;
    var geometry = form.get_Geometry(new Options());
    if (geometry != null)
        foreach (GeometryObject piece in geometry)
        {
            var body = piece as Solid;
            if (body != null && body.Volume > 0) volume += body.Volume;
        }
    if (solid && volume <= 0)
        throw new InvalidOperationException("The extrusion was built and holds no solid to measure. NOTHING from "
            + "this call was kept.");

    // WHICH LOOP IS A HOLE, by where it lies: inside an odd number of the others
    // it is a hole, inside none or an even number it is an outline of its own.
    // Every point of it is tested, so a loop half in and half out is named as
    // crossing rather than guessed at. Never by its place in the list (5b-275).
    Func<double[], List<double[]>, bool> inside = (point, ring) =>
    {
        var odd = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            if ((ring[i][1] > point[1]) != (ring[j][1] > point[1])
                && point[0] < (ring[j][0] - ring[i][0]) * (point[1] - ring[i][1]) / (ring[j][1] - ring[i][1]) + ring[i][0])
                odd = !odd;
        return odd;
    };
    var outlines = 0;
    var holes = 0;
    var crossing = 0;
    for (var i = 0; i < rings.Count; i++)
    {
        var within = 0;
        var crosses = false;
        for (var j = 0; j < rings.Count; j++)
        {
            if (j == i) continue;
            var count = rings[i].Count(point => inside(point, rings[j]));
            if (count == rings[i].Count) within++;
            else if (count > 0) crosses = true;
        }
        if (crosses) crossing++;
        else if (within % 2 == 1) holes++;
        else outlines++;
    }
    var parts = new List<string>();
    if (outlines > 0) parts.Add(outlines + (outlines == 1 ? " outline" : holes == 0 ? " separate outlines" : " outlines"));
    if (holes > 0) parts.Add(holes + (holes == 1 ? " hole" : " holes") + " inside " + (outlines == 1 ? "it" : "them"));
    if (crossing > 0) parts.Add(crossing + " crossing another loop");

    formId = form.UniqueId;
    built = (solid ? "Solid" : "Void") + " extrusion on \"" + plane.Item4 + "\" (" + facing[axis] + ", "
        + axisLetters[axis] + " " + mm(at) + " mm): " + loops.Count + " loop(s)"
        + (loops.Count > 1 ? " (" + string.Join(", ", parts) + ")" : "") + ", " + axisLetters[axis] + " " + mm(low) + " to " + mm(high) + " mm. Reads "
        + string.Join(", ", new[] { 0, 1, 2 }.Select(i => axisLetters[i] + " " + mm(along(bounds.Min, i)) + " to "
            + mm(along(bounds.Max, i)))) + " mm"
        + (solid ? "; volume " + Math.Round(volume * 28.316846592, 3).ToString(invariant) + " L." : ".");

    findings.Add("Built: " + built + " Its id is " + formId + ".");
    findings.Add("Its sketch is hosted on \"" + plane.Item4 + "\" and moves with it. Nothing else ties it to the "
        + "family's planes or parameters yet - move a plane one of its edges lines up with and it stays where it is. "
        + "LOCK_FORM_TO_PLANES locks its flat faces to planes, and LABEL_FAMILY_RADIUS labels a round size.");
    if (!solid)
        findings.Add("A void CUTS NOTHING by being made. In a family built through the API it cuts a solid only "
            + "once the two are combined - Revit's Cut Geometry, which COMBINE_FAMILY_FORMS does with this "
            + "id and the solid's.");
}

if (refused != null) findings.Add(refused);
