// NOT STANDALONE. Assumes `doc`, `kind`, `profiles`, `path` and `solid` are in
// scope; leaves `formId`, `built`, `notAFamily`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// ONE SMOOTH FORM THE CLASSIC FORMS CANNOT MAKE, built as a solid and kept in
// the family as a FREEFORM form (FreeFormElement.Create): a LOFT through two or
// more sections placed anywhere and facing any way - a body, a hull, a horn -
// or a SWEEP of a profile along a path that leaves the plane - a coil spring, a
// hose, a rail bent in three directions.
//
// NOT PARAMETRIC. The form is a fixed shape: it has no sketch, nothing ties it
// to a plane or a parameter, and a flex cannot move it. Changing it is making
// it again.
//
// EVERYTHING IS IN MILLIMETRES IN THE FAMILY'S OWN X, Y, Z FROM ITS ORIGIN,
// except a loop, written in its own two coordinates: the first runs LEVEL
// across the loop, the second UP it, and a level loop takes X and Y. So a
// section facing X is written in Y,Z and one facing Y in X,Z - the way the
// other form tools write a plane facing left-right or front-back.
//
// MEASURED ON REVIT 2024 (2026-10-07, rolled back, a scratch Generic Model
// family), and the reason for each check below:
//   - a loft whose sections have different numbers of pieces is built
//     distorted - 2/2/4/2/2 pieces gave 201.1 L where 2 each gave 221.4 L - so
//     it is refused;
//   - a sweep whose path crosses itself, or whose coils overlap, is BUILT by
//     Revit without complaint, as a solid that counts the overlap twice, so the
//     path is checked here before Revit sees it;
//   - Revit refuses its own helix for a sweep ("a helical curve ... not
//     supported"), so a helix here is a smooth curve through 16 points a turn,
//     0.036 mm from a true helix at a 55 mm radius (8 points a turn: 0.51 mm);
//   - a closed smooth loop is two open splines whose end directions match; a
//     closed spline cut in two is refused by Revit's loft.
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
var problems = new List<string>();
// Revit's own shortest curve, in millimetres - anything shorter is refused here
// by name rather than by Revit with a message nobody can place.
var shortest = doc.Application.ShortCurveTolerance * 304.8;

Func<string, double?> number = text =>
{
    double value;
    if (!double.TryParse((text ?? "").Trim(), System.Globalization.NumberStyles.Float, invariant, out value))
        return null;
    if (double.IsNaN(value) || double.IsInfinity(value)) return null;
    return value;
};

Func<string, int, double[]> numbers = (text, count) =>
{
    var parts = (text ?? "").Split(',');
    if (parts.Length != count) return null;
    var values = new double[count];
    for (var i = 0; i < count; i++)
    {
        var value = number(parts[i]);
        if (!value.HasValue) return null;
        values[i] = value.Value;
    }
    return values;
};

Func<string, string> tidy = text => System.Text.RegularExpressions.Regex.Replace((text ?? "").Trim(), @"\s*,\s*", ",");
Func<double[], XYZ> feet = p => new XYZ(p[0] / 304.8, p[1] / 304.8, p[2] / 304.8);
Func<XYZ, string> spot = p => mm(p.X) + "," + mm(p.Y) + "," + mm(p.Z);
Func<double[], double[], double> apart = (p, q) =>
    Math.Sqrt(Enumerable.Range(0, p.Length).Sum(i => (p[i] - q[i]) * (p[i] - q[i])));

// A LOOP'S OWN TWO DIRECTIONS for a loop facing `n`: the first LEVEL across it,
// pointing +X - or +Y when the loop faces more along X than along Y - and the
// second UP it. A level loop takes X and Y.
Func<XYZ, XYZ[]> axesFacing = n =>
{
    n = n.Normalize();
    if (Math.Sqrt(n.X * n.X + n.Y * n.Y) < 1e-6) return new[] { XYZ.BasisX, XYZ.BasisY };
    var up = (XYZ.BasisZ - n.Multiply(n.Z)).Normalize();
    var across = up.CrossProduct(n).Normalize();
    if ((Math.Abs(n.X) >= Math.Abs(n.Y) ? across.Y : across.X) < 0) across = across.Negate();
    return new[] { across, up };
};

// A point in a loop's own coordinates, laid in 3D on the loop's origin and its
// two directions.
Func<XYZ, XYZ, XYZ, double, double, XYZ> lay = (o, u, v, a, b) => o + u.Multiply(a / 304.8) + v.Multiply(b / 304.8);

// WHERE EACH ROUND LOOP STARTS, held so a loft can turn it: the angle it
// starts at, then its centre and its two half-widths in its own coordinates.
var roundStarts = new Dictionary<object, Tuple<double[], double, double, double, double>>();

// ONE LOOP, as pieces each laid in 3D from the loop's origin and directions.
// Null, with the reason added to `problems`, when the text is not a closed loop.
Func<string, string, List<Func<XYZ, XYZ, XYZ, Curve>>> loopOf = (text, label) =>
{
    var said = tidy(text);
    var words = said.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    var head = words.Length == 0 ? "" : words[0].ToLowerInvariant();
    var pieces = new List<Func<XYZ, XYZ, XYZ, Curve>>();
    // {kind, u0, v0, um, vm, u1, v1} - kind 0 a straight side, 1 an arc through (um, vm).
    var segments = new List<double[]>();

    if (head == "circle" || head == "ellipse")
    {
        var round = head == "circle";
        var need = round ? 3 : 4;
        var centre = words.Length >= need ? numbers(words[1], 2) : null;
        var across = words.Length >= need ? number(words[2]) : null;
        var up = round ? across : (words.Length >= need ? number(words[3]) : null);
        var count = words.Length == need + 1 ? number(words[need]) : (words.Length == need ? 2.0 : (double?)null);
        if (centre == null || !across.HasValue || !up.HasValue || across.Value <= 0 || up.Value <= 0
            || !count.HasValue || count.Value != Math.Floor(count.Value) || count.Value < 2 || count.Value > 16)
        {
            problems.Add(label + " is not " + (round ? "a circle written \"circle CX,CY R\" - its centre, then its radius"
                    : "an ellipse written \"ellipse CX,CY RX RY\" - its centre, then its half-widths along the first and "
                    + "the second coordinate")
                + " in millimetres, and if wanted how many pieces it is cut into, 2 to 16, 2 when left out - as in \""
                + (round ? "circle 0,0 150" : "ellipse 0,0 300 200 4") + "\". It reads \"" + said + "\".");
            return null;
        }
        var cx = centre[0];
        var cy = centre[1];
        var a = across.Value;
        var b = up.Value;
        var n = (int)count.Value;
        if (Math.Min(a, b) * 2 * Math.PI / n < shortest)
        {
            problems.Add(label + " is too small to cut into " + n + " pieces - each would be shorter than Revit's "
                + "shortest line, " + plain(shortest) + " mm.");
            return null;
        }
        // CUT INTO PIECES, NEVER ONE CLOSED CURVE - Revit refuses a loop of one
        // curve - the first starting on the first coordinate's + side, or where
        // a loft turns it to, and each going on toward the second coordinate.
        var turn = new double[1];
        for (var k = 0; k < n; k++)
        {
            var from = 2 * Math.PI * k / n;
            var to = 2 * Math.PI * (k + 1) / n;
            if (Math.Abs(a - b) < 1e-9)
                pieces.Add((o, u, v) => Arc.Create(lay(o, u, v, cx, cy), a / 304.8, turn[0] + from, turn[0] + to, u, v));
            else
                pieces.Add((o, u, v) => Ellipse.CreateCurve(lay(o, u, v, cx, cy), a / 304.8, b / 304.8, u, v,
                    turn[0] + from, turn[0] + to));
        }
        roundStarts[pieces] = Tuple.Create(turn, cx, cy, a, b);
        return pieces;
    }

    if (head == "spline")
    {
        var points = said.Substring(6).Split(';').Select(s => s.Trim()).Where(s => s.Length > 0)
            .Select(s => numbers(s, 2)).ToList();
        if (points.Count > 1 && points.All(p => p != null) && apart(points[points.Count - 1], points[0]) < shortest)
            points.RemoveAt(points.Count - 1);
        if (points.Count < 4 || points.Count > 2000 || points.Any(p => p == null))
        {
            problems.Add(label + " is not a smooth loop written \"spline X,Y; X,Y; X,Y; X,Y\" - four to 2000 points "
                + "it passes through, in order round it, semicolons between. It reads \"" + said + "\".");
            return null;
        }
        var count = points.Count;
        for (var k = 0; k < count; k++)
            if (apart(points[k], points[(k + 1) % count]) < shortest)
            {
                problems.Add(label + " has two points closer together than Revit's shortest line, " + plain(shortest)
                    + " mm, at " + plain(points[k][0]) + "," + plain(points[k][1]) + ".");
                return null;
            }
        // THE DIRECTION THROUGH EACH POINT is from the point before it to the
        // point after it, so the two halves meet smoothly at both joins.
        Func<int, double[]> heading = k =>
        {
            var next = points[(k + 1) % count];
            var last = points[(k - 1 + count) % count];
            return new[] { next[0] - last[0], next[1] - last[1] };
        };
        var half = count / 2;
        foreach (var k in new[] { 0, half })
            if (Math.Sqrt(Math.Pow(heading(k)[0], 2) + Math.Pow(heading(k)[1], 2)) < shortest)
            {
                problems.Add(label + " turns straight back on itself at " + plain(points[k][0]) + "," + plain(points[k][1])
                    + " - the points either side of it are in the same place.");
                return null;
            }
        var halves = new[]
        {
            Tuple.Create(Enumerable.Range(0, half + 1).Select(k => points[k]).ToList(), heading(0), heading(half)),
            Tuple.Create(Enumerable.Range(half, count - half).Select(k => points[k]).Concat(new[] { points[0] }).ToList(),
                heading(half), heading(0)),
        };
        foreach (var piece in halves)
        {
            var through = piece.Item1;
            var leaving = piece.Item2;
            var arriving = piece.Item3;
            pieces.Add((o, u, v) =>
            {
                var tangents = new HermiteSplineTangents();
                tangents.StartTangent = (u.Multiply(leaving[0]) + v.Multiply(leaving[1])).Normalize();
                tangents.EndTangent = (u.Multiply(arriving[0]) + v.Multiply(arriving[1])).Normalize();
                return HermiteSpline.Create(through.Select(p => lay(o, u, v, p[0], p[1])).ToList(), false, tangents);
            });
        }
        return pieces;
    }

    if (head == "rect")
    {
        var p = words.Length == 3 ? numbers(words[1], 2) : null;
        var q = words.Length == 3 ? numbers(words[2], 2) : null;
        if (p == null || q == null)
        {
            problems.Add(label + " is not a rectangle written \"rect X1,Y1 X2,Y2\" - two opposite corners, as in "
                + "\"rect -300,-200 300,200\". It reads \"" + said + "\".");
            return null;
        }
        segments.Add(new[] { 0.0, p[0], p[1], 0, 0, q[0], p[1] });
        segments.Add(new[] { 0.0, q[0], p[1], 0, 0, q[0], q[1] });
        segments.Add(new[] { 0.0, q[0], q[1], 0, 0, p[0], q[1] });
        segments.Add(new[] { 0.0, p[0], q[1], 0, 0, p[0], p[1] });
    }
    else if (head == "polygon")
    {
        var centre = words.Length == 4 ? numbers(words[1], 2) : null;
        var radius = words.Length == 4 ? number(words[2]) : null;
        var sides = words.Length == 4 ? number(words[3]) : null;
        if (centre == null || !radius.HasValue || radius.Value <= 0 || !sides.HasValue
            || sides.Value != Math.Floor(sides.Value) || sides.Value < 3 || sides.Value > 64)
        {
            problems.Add(label + " is not a regular polygon written \"polygon CX,CY R N\" - its centre, the radius to "
                + "a corner and 3 to 64 sides, as in \"polygon 0,0 100 6\". It reads \"" + said + "\".");
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
        var first = items.Count == 0 ? null : numbers(items[0], 2);
        if (first == null)
        {
            problems.Add(label + " does not start with a corner \"X,Y\". A loop is \"circle ...\", \"ellipse ...\", "
                + "\"rect ...\", \"polygon ...\", \"spline ...\" or corners with semicolons between, as in "
                + "\"0,0; 600,0; arc 700,100 600,200; 0,200\". It reads \"" + said + "\".");
            return null;
        }
        var at = first;
        foreach (var item in items.Skip(1))
        {
            var parts = item.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && parts[0].ToLowerInvariant() == "arc")
            {
                var through = parts.Length == 3 ? numbers(parts[1], 2) : null;
                var end = parts.Length == 3 ? numbers(parts[2], 2) : null;
                if (through == null || end == null)
                {
                    problems.Add(label + ": \"" + item + "\" is not an arc written \"arc MX,MY X,Y\" - a point the arc "
                        + "passes through, then where it ends.");
                    return null;
                }
                segments.Add(new[] { 1.0, at[0], at[1], through[0], through[1], end[0], end[1] });
                at = end;
            }
            else
            {
                var next = parts.Length == 1 ? numbers(parts[0], 2) : null;
                if (next == null)
                {
                    problems.Add(label + ": \"" + item + "\" is neither a corner \"X,Y\" nor an arc \"arc MX,MY X,Y\".");
                    return null;
                }
                segments.Add(new[] { 0.0, at[0], at[1], 0, 0, next[0], next[1] });
                at = next;
            }
        }
        // CLOSED BACK TO THE FIRST CORNER - by a straight side when the last
        // corner is not already there, by snapping it when it is all but there.
        if (apart(at, first) >= shortest) segments.Add(new[] { 0.0, at[0], at[1], 0, 0, first[0], first[1] });
        else if (segments.Count > 0)
        {
            segments[segments.Count - 1][5] = first[0];
            segments[segments.Count - 1][6] = first[1];
        }
    }

    if (segments.Count < 2 || (segments.Count < 3 && segments.All(s => s[0] == 0.0)))
    {
        problems.Add(label + " has " + segments.Count + " side(s) - a closed loop needs at least three straight sides, "
            + "or an arc and one more side.");
        return null;
    }
    foreach (var s in segments)
    {
        var chord = Math.Sqrt(Math.Pow(s[5] - s[1], 2) + Math.Pow(s[6] - s[2], 2));
        if (s[0] == 0.0 && chord < shortest)
        {
            problems.Add(label + " has a side from " + plain(s[1]) + "," + plain(s[2]) + " to " + plain(s[5]) + ","
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
            if (lu < shortest || lv < shortest || lw < shortest || Math.Abs(ux * vy - uy * vx) < 1e-6 * lu * lv)
            {
                problems.Add(label + " has an arc from " + plain(s[1]) + "," + plain(s[2]) + " through " + plain(s[3])
                    + "," + plain(s[4]) + " to " + plain(s[5]) + "," + plain(s[6]) + " that is not an arc - its three "
                    + "points are in a line, or two are in the same place.");
                return null;
            }
        }
    }
    foreach (var segment in segments)
    {
        var s = segment;
        if (s[0] == 0.0) pieces.Add((o, u, v) => Line.CreateBound(lay(o, u, v, s[1], s[2]), lay(o, u, v, s[5], s[6])));
        else pieces.Add((o, u, v) => Arc.Create(lay(o, u, v, s[1], s[2]), lay(o, u, v, s[5], s[6]), lay(o, u, v, s[3], s[4])));
    }
    return pieces;
};

// A LOOP LAID IN 3D, with the area it encloses, how far it reaches from its
// origin and the angle its first point sits at round its middle, all in its own
// coordinates and read off Revit's own curves at 64 points a piece - Revit's
// own tessellation is too coarse for a small circle's area (127.31 mm2 for
// 153.94).
Func<List<Func<XYZ, XYZ, XYZ, Curve>>, XYZ, XYZ, XYZ, Tuple<CurveLoop, double, double, double>> laid = (pieces, o, u, v) =>
{
    var loop = new CurveLoop();
    var ring = new List<double[]>();
    foreach (var make in pieces)
    {
        var curve = make(o, u, v);
        loop.Append(curve);
        for (var k = 0; k < 64; k++)
        {
            var d = curve.Evaluate(k / 64.0, true) - o;
            ring.Add(new[] { d.DotProduct(u) * 304.8, d.DotProduct(v) * 304.8 });
        }
    }
    var area = 0.0;
    for (var i = 0; i < ring.Count; i++)
    {
        var p = ring[i];
        var q = ring[(i + 1) % ring.Count];
        area += p[0] * q[1] - q[0] * p[1];
    }
    var middleAcross = ring.Average(p => p[0]);
    var middleUp = ring.Average(p => p[1]);
    return Tuple.Create(loop, Math.Abs(area) / 2, ring.Max(p => Math.Sqrt(p[0] * p[0] + p[1] * p[1])),
        Math.Atan2(ring[0][1] - middleUp, ring[0][0] - middleAcross));
};


// ---- what was asked -------------------------------------------------------

var how = (kind ?? "").Trim().ToLowerInvariant();
var lofting = how == "loft";
// Loft: (origin, facing, pieces, written) per section. Sweep: the profile's loops.
var sections = new List<Tuple<XYZ, XYZ, List<Func<XYZ, XYZ, XYZ, Curve>>, string>>();
var outlines = new List<List<Func<XYZ, XYZ, XYZ, Curve>>>();
var route = new List<Curve>();
var routeSaid = "";
var coiled = false;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A family's forms are "
        + "built inside the family - open it first.";
}
else
{
    var parts = (profiles ?? "").Split('|').Select(s => s.Trim()).ToList();
    if (how != "loft" && how != "sweep")
        problems.Add("The kind is \"loft\" or \"sweep\" - it reads \"" + (kind ?? "") + "\". A revolve, an extrusion or "
            + "a blend between two parallel shapes is CREATE_FAMILY_REVOLUTION, CREATE_FAMILY_EXTRUSION or "
            + "CREATE_FAMILY_BLEND, which stay parametric.");
    else if (parts.All(s => s.Length == 0))
        problems.Add(lofting ? "No sections were given." : "No profile was given.");
    else if (parts.Any(s => s.Length == 0))
        problems.Add("An empty " + (lofting ? "section" : "loop") + " sits between two | marks.");
    else if (lofting)
    {
        if (parts.Count < 2 || parts.Count > 50)
            problems.Add("A loft runs through 2 to 50 sections, with | between them - " + parts.Count + " given.");
        else if ((path ?? "").Trim().Length > 0)
            problems.Add("A loft has no path - it runs through its sections in the order written. Leave the path empty.");
        else
            for (var i = 0; i < parts.Count; i++)
            {
                var label = "Section " + (i + 1);
                var said = tidy(parts[i]);
                var match = System.Text.RegularExpressions.Regex.Match(said, @"^at\s+(\S+)\s+facing\s+([^:]+?)\s*:\s*(.+)$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var origin = match.Success ? numbers(match.Groups[1].Value, 3) : null;
                var towards = match.Success ? match.Groups[2].Value.Trim().ToUpperInvariant() : "";
                double[] facing = null;
                var sign = towards.StartsWith("-") ? -1.0 : 1.0;
                var letter = towards.TrimStart('+', '-');
                if (letter == "X") facing = new[] { sign, 0, 0 };
                else if (letter == "Y") facing = new[] { 0, sign, 0 };
                else if (letter == "Z") facing = new[] { 0, 0, sign };
                else if (towards.Length > 0) facing = numbers(towards, 3);
                if (origin == null || facing == null || Math.Sqrt(facing.Sum(f => f * f)) < 1e-9)
                {
                    problems.Add(label + " is not written \"at X,Y,Z facing X: <loop>\" - where its own 0,0 sits in the "
                        + "family, the way it faces (X, Y, Z or a direction NX,NY,NZ), then its loop, as in \"at "
                        + "300,0,850 facing X: ellipse 0,0 220 250\". It reads \"" + said + "\".");
                    continue;
                }
                var loop = loopOf(match.Groups[3].Value, label + "'s loop");
                if (loop != null)
                    sections.Add(Tuple.Create(feet(origin), new XYZ(facing[0], facing[1], facing[2]).Normalize(), loop,
                        said));
            }
    }
    else
    {
        for (var i = 0; i < parts.Count; i++)
        {
            var label = parts.Count == 1 ? "The profile" : "The profile's loop " + (i + 1);
            if (parts[i].TrimStart().StartsWith("at ", StringComparison.OrdinalIgnoreCase))
            {
                problems.Add(label + " is placed by the path - written in its own coordinates, 0,0 where the path "
                    + "starts, with no \"at ... facing\".");
                continue;
            }
            var loop = loopOf(parts[i], label);
            if (loop != null) outlines.Add(loop);
        }

        // THE PATH, in the family's X,Y,Z: a helix, a smooth spline through
        // points, or straight pieces and arcs end to end.
        routeSaid = tidy(path);
        try
        {
            var words = routeSaid.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var head = words.Length == 0 ? "" : words[0].ToLowerInvariant();
            if (routeSaid.Length == 0) problems.Add("A sweep needs a path.");
            else if (head == "helix")
            {
                var centre = words.Length >= 5 ? numbers(words[1], 3) : null;
                var radius = words.Length >= 5 ? number(words[2]) : null;
                var pitch = words.Length >= 5 ? number(words[3]) : null;
                var turns = words.Length >= 5 ? number(words[4]) : null;
                var axis = "Z";
                var left = false;
                var extra = words.Skip(5).Select(w => w.ToUpperInvariant()).ToList();
                var understood = extra.Count <= 2;
                foreach (var w in extra)
                    if (w == "X" || w == "Y" || w == "Z") axis = w;
                    else if (w == "LEFT") left = true;
                    else if (w != "RIGHT") understood = false;
                if (centre == null || !radius.HasValue || radius.Value <= 0 || !pitch.HasValue || pitch.Value <= 0
                    || !turns.HasValue || turns.Value <= 0 || turns.Value > 100 || !understood)
                    problems.Add("The path is not a helix written \"helix CX,CY,CZ R PITCH TURNS\" - the centre of its "
                        + "base, its radius, how far it rises in one turn, and 0 to 100 turns, then if wanted the axis it "
                        + "winds round (X, Y or Z, Z when left out) and \"left\" for a left-hand coil, as in \"helix "
                        + "0,0,0 55 25 6\". It reads \"" + routeSaid + "\".");
                else
                {
                    // AROUND Z it starts on the +X side; around X on the +Y side;
                    // around Y on the +Z side - and rises along the axis.
                    var rise = axis == "X" ? XYZ.BasisX : axis == "Y" ? XYZ.BasisY : XYZ.BasisZ;
                    var first = axis == "X" ? XYZ.BasisY : axis == "Y" ? XYZ.BasisZ : XYZ.BasisX;
                    var second = rise.CrossProduct(first);
                    if (left) second = second.Negate();
                    var steps = (int)Math.Ceiling(16 * turns.Value);
                    var through = new List<XYZ>();
                    for (var k = 0; k <= steps; k++)
                    {
                        var angle = 2 * Math.PI * turns.Value * k / steps;
                        through.Add(feet(centre) + first.Multiply(radius.Value * Math.Cos(angle) / 304.8)
                            + second.Multiply(radius.Value * Math.Sin(angle) / 304.8)
                            + rise.Multiply(pitch.Value * turns.Value * k / steps / 304.8));
                    }
                    route.Add(HermiteSpline.Create(through, false));
                    coiled = true;
                    routeSaid = "a " + (left ? "left" : "right") + "-hand helix of " + plain(turns.Value) + " turn(s) round "
                        + axis + ", radius " + plain(radius.Value) + " mm, pitch " + plain(pitch.Value) + " mm, from "
                        + spot(through[0]) + " - a smooth curve through " + through.Count + " points";
                }
            }
            else
            {
                var smooth = head == "spline";
                var items = (smooth ? routeSaid.Substring(6) : routeSaid).Split(';').Select(s => s.Trim())
                    .Where(s => s.Length > 0).ToList();
                if (smooth)
                {
                    var points = items.Select(s => numbers(s, 3)).ToList();
                    if (points.Count < 3 || points.Count > 2000 || points.Any(p => p == null))
                        problems.Add("The path is not a smooth curve written \"spline X,Y,Z; X,Y,Z; X,Y,Z\" - three to 2000 "
                            + "points it passes through, in order. It reads \"" + routeSaid + "\".");
                    else if (Enumerable.Range(1, points.Count - 1).Any(k => apart(points[k], points[k - 1]) < shortest))
                        problems.Add("The path has two points in a row closer than Revit's shortest line, " + plain(shortest)
                            + " mm.");
                    else
                    {
                        route.Add(HermiteSpline.Create(points.Select(feet).ToList(), false));
                        routeSaid = "a smooth path through " + points.Count + " points from " + spot(feet(points[0]));
                    }
                }
                else
                {
                    var at = items.Count == 0 ? null : numbers(items[0], 3);
                    if (at == null || items.Count < 2)
                        problems.Add("The path is not written \"helix ...\", \"spline X,Y,Z; ...\" or corners \"X,Y,Z; X,Y,Z; "
                            + "arc MX,MY,MZ X,Y,Z\" with semicolons between. It reads \"" + routeSaid + "\".");
                    else
                    {
                        var straight = 0;
                        var bent = 0;
                        foreach (var item in items.Skip(1))
                        {
                            var bits = item.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            var arc = bits.Length > 0 && bits[0].ToLowerInvariant() == "arc";
                            var through = arc && bits.Length == 3 ? numbers(bits[1], 3) : null;
                            var end = arc ? (bits.Length == 3 ? numbers(bits[2], 3) : null)
                                          : (bits.Length == 1 ? numbers(bits[0], 3) : null);
                            if (end == null || (arc && through == null))
                            {
                                problems.Add("The path's \"" + item + "\" is neither a corner \"X,Y,Z\" nor an arc \"arc "
                                    + "MX,MY,MZ X,Y,Z\".");
                                break;
                            }
                            if (apart(at, end) < shortest || (arc && (apart(at, through) < shortest
                                || apart(through, end) < shortest)))
                            {
                                problems.Add("The path's piece from " + spot(feet(at)) + " to " + spot(feet(end)) + " is "
                                    + "shorter than Revit's shortest line, " + plain(shortest) + " mm.");
                                break;
                            }
                            if (arc)
                            {
                                var one = feet(through) - feet(at);
                                var two = feet(end) - feet(at);
                                if (one.CrossProduct(two).GetLength() < 1e-6 * one.GetLength() * two.GetLength())
                                {
                                    problems.Add("The path's arc from " + spot(feet(at)) + " through " + spot(feet(through))
                                        + " to " + spot(feet(end)) + " is not an arc - its three points are in a line.");
                                    break;
                                }
                                route.Add(Arc.Create(feet(at), feet(end), feet(through)));
                                bent++;
                            }
                            else
                            {
                                route.Add(Line.CreateBound(feet(at), feet(end)));
                                straight++;
                            }
                            at = end;
                        }
                        routeSaid = "a path of " + (straight + bent) + " piece(s) - " + straight + " straight, " + bent
                            + " arc(s) - from " + (route.Count > 0 ? spot(route[0].GetEndPoint(0)) : "");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            problems.Add("Revit could not make the path from \"" + routeSaid + "\": " + ex.Message);
        }
    }

    // A LOFT'S SECTIONS PAIR PIECE BY PIECE: equal counts, or the body comes
    // out distorted (measured).
    if (lofting && problems.Count == 0 && sections.Count == parts.Count)
    {
        var counts = sections.Select(s => s.Item3.Count).ToList();
        if (counts.Distinct().Count() > 1)
            problems.Add("Every section of a loft must be cut into the same number of pieces - these are "
                + string.Join("/", counts) + ". Revit pairs them piece by piece and builds unequal ones distorted "
                + "(measured: 2/2/4/2/2 pieces gave 201.1 L where 2 each gave 221.4 L). A circle or an ellipse takes "
                + "its count last - \"ellipse 0,0 300 200 4\" - and a rectangle is 4.");
        for (var i = 1; i < sections.Count; i++)
            if (sections[i].Item1.DistanceTo(sections[i - 1].Item1) * 304.8 < shortest)
                problems.Add("Sections " + i + " and " + (i + 1) + " sit at the same point, " + spot(sections[i].Item1)
                    + ".");
    }

    if (problems.Count > 0) refused = "Nothing was built. " + string.Join(" ", problems);
}


// ---- laid in 3D and checked -----------------------------------------------

var loops = new List<CurveLoop>();
var areas = new List<double>();
var reach = 0.0;
var length = 0.0;
CurveLoop pathLoop = null;
var startParameter = 0.0;
XYZ[] square = null;

if (refused == null)
{
    try
    {
        if (lofting)
        {
            // A ROUND SECTION STARTS LEVEL WITH THE SECTION BEFORE IT, so a
            // rectangle lofted into a circle does not twist - started on its +
            // side instead, it measured 34.337 L, twisted.
            var previousStart = double.NaN;
            foreach (var s in sections)
            {
                Tuple<double[], double, double, double, double> round;
                if (!double.IsNaN(previousStart) && roundStarts.TryGetValue(s.Item3, out round))
                    round.Item1[0] = Math.Atan2(round.Item4 * Math.Sin(previousStart), round.Item5 * Math.Cos(previousStart));
                var axes = axesFacing(s.Item2);
                var made = laid(s.Item3, s.Item1, axes[0], axes[1]);
                loops.Add(made.Item1);
                areas.Add(made.Item2);
                previousStart = made.Item4;
            }
        }
        else
        {
            pathLoop = new CurveLoop();
            foreach (var curve in route) pathLoop.Append(curve);
            length = route.Sum(c => c.Length) * 304.8;
            startParameter = route[0].GetEndParameter(0);
            var start = route[0].GetEndPoint(0);
            var tangent = route[0].ComputeDerivatives(startParameter, false).BasisX.Normalize();
            square = axesFacing(tangent);
            foreach (var outline in outlines)
            {
                var made = laid(outline, start, square[0], square[1]);
                loops.Add(made.Item1);
                areas.Add(made.Item2);
                reach = Math.Max(reach, made.Item3);
            }
        }
    }
    catch (Exception ex)
    {
        problems.Add("Revit could not lay the " + (lofting ? "sections" : "profile and path") + " out: " + ex.Message);
    }

    for (var i = 0; i < areas.Count && problems.Count == 0; i++)
        if (areas[i] < shortest * shortest)
            problems.Add((lofting ? "Section " + (i + 1) : "The profile's loop " + (i + 1)) + " encloses no area - its "
                + "points lie on one line.");

    // A SWEEP MUST NOT RUN INTO ITSELF. Revit builds one that does without
    // complaint, as a solid that is not whole (measured), so two things are
    // checked on the path first, both against how far the profile reaches from
    // it: no bend tighter than that reach, and no two places further apart
    // ALONG the path than half a turn round the profile coming closer than
    // twice that reach. Two straight pieces meeting at a corner of 90 degrees
    // or wider are mitred by Revit and left alone.
    if (!lofting && problems.Count == 0)
    {
        var spacing = Math.Max(reach / 2, length / 8000);
        if (spacing > reach)
            problems.Add("The path is " + plain(length) + " mm long - too long beside a profile reaching only "
                + plain(reach) + " mm for it to be checked against running into itself. Sweep it in parts.");
        else
        {
            var at = new List<XYZ>();
            var along = new List<double>();
            var piece = new List<int>();
            var straightPiece = route.Select(c => c is Line).ToList();
            var tightest = double.MaxValue;
            XYZ tightAt = null;
            var travelled = 0.0;
            for (var c = 0; c < route.Count; c++)
            {
                var curve = route[c];
                var n = Math.Max(8, (int)Math.Ceiling(curve.Length * 304.8 / spacing));
                var p0 = curve.GetEndParameter(0);
                var p1 = curve.GetEndParameter(1);
                for (var k = (c == 0 ? 0 : 1); k <= n; k++)
                {
                    var t = p0 + (p1 - p0) * k / n;
                    var point = curve.Evaluate(t, false);
                    if (at.Count > 0) travelled += point.DistanceTo(at[at.Count - 1]) * 304.8;
                    at.Add(point);
                    along.Add(travelled);
                    piece.Add(c);
                    if (!straightPiece[c])
                    {
                        var d = curve.ComputeDerivatives(t, false);
                        var speed = d.BasisX.GetLength();
                        var bend = d.BasisX.CrossProduct(d.BasisY).GetLength();
                        if (speed > 1e-9 && bend > 1e-12)
                        {
                            var radius = Math.Pow(speed, 3) / bend * 304.8;
                            if (radius < tightest) { tightest = radius; tightAt = point; }
                        }
                    }
                }
            }
            if (tightest < reach)
                problems.Add("The path bends tighter than its profile near " + spot(tightAt) + " - a radius of "
                    + plain(tightest) + " mm, where the profile reaches " + plain(reach) + " mm from the path - so the "
                    + "form would fold into itself. Ease the bend, or make the profile smaller.");
            else
            {
                var closed = at[0].DistanceTo(at[at.Count - 1]) * 304.8 < shortest;
                var limit = 2 * reach / 304.8;
                var limitSquared = limit * limit;
                var nearest = double.MaxValue;
                int nearA = -1, nearB = -1;
                for (var i = 0; i < at.Count; i++)
                {
                    var pi = at[i];
                    for (var j = i + 1; j < at.Count; j++)
                    {
                        var gap = along[j] - along[i];
                        if (closed) gap = Math.Min(gap, travelled - gap);
                        if (gap <= Math.PI * reach) continue;
                        var dx = at[j].X - pi.X;
                        var dy = at[j].Y - pi.Y;
                        var dz = at[j].Z - pi.Z;
                        var squared = dx * dx + dy * dy + dz * dz;
                        if (squared >= limitSquared || squared >= nearest) continue;
                        if (Math.Abs(piece[j] - piece[i]) == 1 && straightPiece[piece[i]] && straightPiece[piece[j]])
                        {
                            var a = route[Math.Min(piece[i], piece[j])];
                            var b = route[Math.Max(piece[i], piece[j])];
                            var inward = (a.GetEndPoint(0) - a.GetEndPoint(1)).Normalize();
                            var outward = (b.GetEndPoint(1) - b.GetEndPoint(0)).Normalize();
                            if (inward.DotProduct(outward) <= 1e-9) continue;
                        }
                        nearest = squared;
                        nearA = i;
                        nearB = j;
                    }
                }
                if (nearA >= 0)
                    problems.Add("The path comes within " + plain(Math.Sqrt(nearest) * 304.8) + " mm of itself, near "
                        + spot(at[nearA]) + " and again near " + spot(at[nearB]) + ", " + plain(along[nearB] - along[nearA])
                        + " mm further along it - less than twice the " + plain(reach) + " mm the profile reaches from "
                        + "it, so the form would run into itself. Revit builds such a sweep without complaint, as a solid "
                        + "that is not whole (measured), so it is refused here. Space the path out or make the profile "
                        + "smaller" + (coiled ? " - a coil's pitch must be more than its wire's thickness." : "."));
            }
        }
    }

    if (problems.Count > 0) refused = "Nothing was built. " + string.Join(" ", problems);
}


// ---- built, kept as a freeform form, read back ----------------------------

if (refused == null)
{
    var what = lofting ? "loft" : "sweep";
    FreeFormElement form;
    try
    {
        var shape = lofting
            ? GeometryCreationUtilities.CreateLoftGeometry(loops,
                new SolidOptions(ElementId.InvalidElementId, ElementId.InvalidElementId))
            : GeometryCreationUtilities.CreateSweptGeometry(pathLoop, 0, startParameter, loops);
        if (shape == null || shape.Volume <= 0)
            throw new InvalidOperationException("it made no solid from what was given");
        form = FreeFormElement.Create(doc, shape);
        if (!solid)
        {
            // A VOID IS THE SAME FORM WITH ITS Solid/Void FIELD SET - measured:
            // IsSolid then reads false, and a combine cuts a solid with it.
            var cutting = form.get_Parameter(BuiltInParameter.ELEMENT_IS_CUTTING);
            if (cutting == null || cutting.IsReadOnly)
                throw new InvalidOperationException("the form has no Solid/Void field to make it a void");
            cutting.Set(1);
        }
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the freeform " + what + ": " + ex.Message
            + (lofting ? " A loft runs through its sections in the order written - sections out of order, or one "
                + "crossing another, fold the body into itself." : "")
            + " NOTHING from this call was kept.");
    }

    doc.Regenerate();

    if (form.IsSolid != solid)
        throw new InvalidOperationException("The freeform " + what + " was built and reads as a "
            + (form.IsSolid ? "solid" : "void") + ", not the " + (solid ? "solid" : "void") + " asked for. NOTHING from "
            + "this call was kept.");
    var bounds = form.get_BoundingBox(null);
    if (bounds == null)
        throw new InvalidOperationException("The freeform " + what + " was built and has no extent to read back. "
            + "NOTHING from this call was kept.");
    var volume = 0.0;
    var geometry = form.get_Geometry(new Options());
    if (geometry != null)
        foreach (GeometryObject part in geometry)
        {
            var body = part as Solid;
            if (body != null && body.Volume > 0) volume += body.Volume;
        }
    if (solid && volume <= 0)
        throw new InvalidOperationException("The freeform " + what + " was built and holds no solid to measure. "
            + "NOTHING from this call was kept.");

    formId = form.UniqueId;
    built = (solid ? "Solid" : "Void") + " freeform " + what + " "
        + (lofting
            ? "through " + sections.Count + " sections of " + sections[0].Item3.Count + " piece(s) each, from "
                + spot(sections[0].Item1) + " to " + spot(sections[sections.Count - 1].Item1)
            : "of a " + loops.Count + "-loop profile (" + string.Join(" and ", areas.Select(plain)) + " mm2, reaching "
                + plain(reach)
                + " mm from the path) along " + routeSaid + ", " + plain(length) + " mm long")
        + ". Reads X " + mm(bounds.Min.X) + " to " + mm(bounds.Max.X) + ", Y " + mm(bounds.Min.Y) + " to "
        + mm(bounds.Max.Y) + ", Z " + mm(bounds.Min.Z) + " to " + mm(bounds.Max.Z) + " mm"
        + (volume > 0 ? "; volume " + Math.Round(volume * 28.316846592, 3).ToString(invariant) + " L." : ".");

    findings.Add("Built: " + built + " Its id is " + formId + ".");
    findings.Add("NOT PARAMETRIC. A freeform form is a fixed shape, kept as it was made: it has no sketch, no "
        + "parameter drives it, it moves with no reference plane, and a flex cannot change it. To change its size, "
        + "delete it and make it again.");
    if (lofting)
        findings.Add("Each section's pieces were joined to the next section's in order. A circle or an ellipse was "
            + "started level with the section before it - the first section on its first coordinate's + side - a "
            + "spline starts at its first point and corners at the first corner. Sections started apart twist the "
            + "body: LOOK at it.");
    else
        findings.Add("The profile was laid square to the path's start, its first coordinate level across it (toward +X, "
            + "or +Y when the path starts along X) and its second up it. A profile not round about its 0,0 is turned "
            + "by Revit as it goes: LOOK at it.");
    if (!solid)
        findings.Add("A void CUTS NOTHING by being made. It cuts a solid once the two are combined - Revit's Cut "
            + "Geometry, which COMBINE_FAMILY_FORMS does with this id and the solid's.");
    else
        findings.Add("Its material is SET_FAMILY_FORM_MATERIAL with this id, and showing it at Fine only is "
            + "SET_FAMILY_FORM_VISIBILITY - both take any family form.");
}

if (refused != null) findings.Add(refused);
