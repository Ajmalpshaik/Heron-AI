// NOT STANDALONE. Assumes `doc`, `view`, `shapes`, `subcategory` and
// `lockToPlanes` are in scope; leaves `lineIds`, `drawn`, `notAFamily`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE LINES OF A 2D FAMILY - a profile, a detail item, an annotation symbol, a
// title block - drawn in its view with the Family Editor's Line tool, which in
// these families makes DETAIL lines. In a PROFILE family every line drawn is
// part of the profile, so the loops given are the profile: one or more closed
// loops that do not cross, a loop inside another a hole.
//
// THE SAME SHAPE LANGUAGE AS THE FORM TOOLS, millimetres in the view's own two
// model coordinates - X,Y in the plan these families draw in. Closed shapes
// are "circle CX,CY R", "rect X1,Y1 X2,Y2", "polygon CX,CY R N", or corners
// "X,Y; X,Y; arc MX,MY X,Y" closed back to the first; OPEN lines are "path X,Y;
// X,Y; ...". Pieces have `|` between them.
//
// LOCKED TO THE PLANES THEY LIE ON, when asked: every straight line that lies
// along a NAMED reference plane is aligned and locked to it, so a profile
// resizes when a labelled dimension moves the plane - the step that makes a
// profile parametric. A line is locked only where it already lies; one lying
// on no named plane is left free and counted.
//
// A 3D MODEL FAMILY IS REFUSED - its 2D symbol is DRAW_FAMILY_SYMBOLIC_LINES.
//
// READ BACK, ALL OR NOTHING: every line's ends, its style and every lock are
// read again; one that does not read as asked fails the call, and the host
// rolls the whole call back.

var findings = new List<string>();
var lineIds = "";
var drawn = "";
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
Func<double, string> plain = value => Math.Round(value, 2).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
Func<XYZ, int> axisOf = n =>
    n == null ? -1 : Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;
Func<XYZ, int, double> along = (point, index) => index == 0 ? point.X : index == 1 ? point.Y : point.Z;
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

// AN OPEN LINE, "path X,Y; X,Y; arc MX,MY X,Y", as segments in the same shape as
// a loop's - never closed back to its start.
Func<string, string, List<double[]>> openOf = (text, where) =>
{
    var said = System.Text.RegularExpressions.Regex.Replace((text ?? "").Trim(), @"\s*,\s*", ",");
    var items = said.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
    var first = items.Count == 0 ? null : pair(items[0]);
    if (first == null)
    {
        problems.Add(where + " does not start with a point \"X,Y\" after \"path\" - \"path 0,0; 600,0; arc "
            + "900,300 600,600\". It reads \"" + said + "\".");
        return null;
    }
    var segments = new List<double[]>();
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
                problems.Add(where + ": \"" + item + "\" is not an arc written \"arc MX,MY X,Y\".");
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
                problems.Add(where + ": \"" + item + "\" is neither a point \"X,Y\" nor an arc \"arc MX,MY X,Y\".");
                return null;
            }
            segments.Add(new[] { 0.0, at[0], at[1], 0, 0, next[0], next[1] });
            at = next;
        }
    }
    if (segments.Count == 0)
    {
        problems.Add(where + " has one point and no length - it needs at least a second point.");
        return null;
    }
    foreach (var s in segments)
    {
        var chord = Math.Sqrt(Math.Pow(s[5] - s[1], 2) + Math.Pow(s[6] - s[2], 2));
        if (s[0] == 0.0 && chord < shortest)
        {
            problems.Add(where + " has a piece from " + plain(s[1]) + "," + plain(s[2]) + " to " + plain(s[5]) + ","
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
                problems.Add(where + " has an arc from " + plain(s[1]) + "," + plain(s[2]) + " through "
                    + plain(s[3]) + "," + plain(s[4]) + " to " + plain(s[5]) + "," + plain(s[6]) + " that is not an "
                    + "arc - its three points are in a line, or two are in the same place.");
                return null;
            }
        }
    }
    return segments;
};


// ---- the family, the view, the shapes, the line style ------------------------

var family = doc.IsFamilyDocument ? doc.OwnerFamily : null;
var category = family == null ? null : family.FamilyCategory;
Func<BuiltInCategory, bool> isCategory = bic => category != null && category.Id == new ElementId(bic);
var twoD = false;
try
{
    twoD = category != null && (category.CategoryType == CategoryType.Annotation
        || isCategory(BuiltInCategory.OST_DetailComponents) || isCategory(BuiltInCategory.OST_ProfileFamilies)
        || isCategory(BuiltInCategory.OST_TitleBlocks));
}
catch (Exception) { twoD = false; }
var profile = isCategory(BuiltInCategory.OST_ProfileFamilies);

View target = null;
var pieces = new List<Tuple<List<double[]>, bool>>();
Category wanted = null;
var wantedName = (subcategory ?? "").Trim();
var onCategory = squash(wantedName) == "none";

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "Lines in a project's view are CREATE_LINE; these are drawn inside a 2D family - open it first.";
}
else if (!twoD)
{
    refused = "This is a 3D model family (" + (category == null ? "no category" : category.Name) + "). Its 2D symbol "
        + "is drawn with DRAW_FAMILY_SYMBOLIC_LINES; detail lines belong to a profile, a detail item, an annotation "
        + "or a title block. Nothing was drawn.";
}
else
{
    var said = (view ?? "").Trim();
    var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate).ToList();
    var found = views.Where(v => string.Equals(v.Name, said, StringComparison.OrdinalIgnoreCase)).ToList();
    if (said.Length == 0 || found.Count != 1)
        problems.Add((said.Length == 0 ? "No view was named." : "No single view is called \"" + said + "\".")
            + " This family's views: " + string.Join(", ", views.Select(v => v.Name).Take(10)) + ".");
    else if (axisOf(found[0].ViewDirection) < 0)
        problems.Add("The view \"" + said + "\" looks at an angle; a shape here is written in model coordinates.");
    else target = found[0];

    var texts = (shapes ?? "").Split('|').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
    if (texts.Count == 0) problems.Add("No shapes were given.");
    for (var i = 0; i < texts.Count; i++)
    {
        var where = texts.Count == 1 ? "The shape" : "Shape " + (i + 1);
        var words = texts[i].Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 0 && words[0].ToLowerInvariant() == "path")
        {
            if (profile)
            {
                problems.Add(where + " is an open path. Every line in a profile family is part of the profile, and a "
                    + "profile is closed loops only.");
                continue;
            }
            var open = openOf(words.Length > 1 ? words[1] : "", where);
            if (open != null) pieces.Add(Tuple.Create(open, false));
        }
        else
        {
            var closed = loopOf(texts[i], where);
            if (closed != null) pieces.Add(Tuple.Create(closed, true));
        }
    }

    // A PROFILE'S LOOPS MAY NEST - a loop inside another is a hole - BUT NEVER
    // CROSS OR TOUCH, or the profile fails in a sweep or a project however well
    // each line was drawn. Every edge of one loop is tested against every edge
    // of the others, in the shape's own millimetres, an arc as short chords.
    if (profile && pieces.Count > 0 && problems.Count == 0)
    {
        Func<double[], List<double[]>> chords = s =>
        {
            var points = new List<double[]>();
            if (s[0] == 0.0) { points.Add(new[] { s[1], s[2] }); points.Add(new[] { s[5], s[6] }); return points; }
            double ax = s[1], ay = s[2], bx = s[3], by = s[4], cx = s[5], cy = s[6];
            var d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
            if (Math.Abs(d) < 1e-9) { points.Add(new[] { ax, ay }); points.Add(new[] { cx, cy }); return points; }
            var ox = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / d;
            var oy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / d;
            var r = Math.Sqrt((ax - ox) * (ax - ox) + (ay - oy) * (ay - oy));
            Func<double, double> turn = x => { while (x < 0) x += 2 * Math.PI; while (x >= 2 * Math.PI) x -= 2 * Math.PI; return x; };
            var a0 = Math.Atan2(ay - oy, ax - ox);
            var toEnd = turn(Math.Atan2(cy - oy, cx - ox) - a0);
            var toMid = turn(Math.Atan2(by - oy, bx - ox) - a0);
            var sweep = toMid <= toEnd ? toEnd : toEnd - 2 * Math.PI;
            for (var k = 0; k <= 64; k++)
            {
                var t = a0 + sweep * k / 64;
                points.Add(new[] { ox + r * Math.Cos(t), oy + r * Math.Sin(t) });
            }
            return points;
        };
        Func<double[], double[], double[], double> side = (a, b, c) => (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]);
        Func<double[], double[], double[], bool> within = (a, b, c) =>
            Math.Min(a[0], b[0]) - 1e-6 <= c[0] && c[0] <= Math.Max(a[0], b[0]) + 1e-6
            && Math.Min(a[1], b[1]) - 1e-6 <= c[1] && c[1] <= Math.Max(a[1], b[1]) + 1e-6;
        Func<double[], double[], double[], double[], bool> meet = (p, q, m, n) =>
        {
            var d1 = side(m, n, p);
            var d2 = side(m, n, q);
            var d3 = side(p, q, m);
            var d4 = side(p, q, n);
            if (((d1 > 1e-9 && d2 < -1e-9) || (d1 < -1e-9 && d2 > 1e-9))
                && ((d3 > 1e-9 && d4 < -1e-9) || (d3 < -1e-9 && d4 > 1e-9))) return true;
            return (Math.Abs(d1) <= 1e-9 && within(m, n, p)) || (Math.Abs(d2) <= 1e-9 && within(m, n, q))
                || (Math.Abs(d3) <= 1e-9 && within(p, q, m)) || (Math.Abs(d4) <= 1e-9 && within(p, q, n));
        };
        var traced = pieces.Select(pc => pc.Item1.Select(chords).ToList()).ToList();
        // A LOOP MAY NOT CROSS ITSELF EITHER: every pair of its edges that do
        // not meet at a corner - not neighbours, nor the last and the first.
        for (var i = 0; i < traced.Count; i++)
        {
            var edges = traced[i];
            var crossed = false;
            for (var e = 0; e < edges.Count && !crossed; e++)
                for (var f = e + 2; f < edges.Count && !crossed; f++)
                {
                    if (e == 0 && f == edges.Count - 1) continue;
                    var ca = edges[e];
                    var cb = edges[f];
                    crossed = Enumerable.Range(0, ca.Count - 1).Any(x => Enumerable.Range(0, cb.Count - 1)
                        .Any(y => meet(ca[x], ca[x + 1], cb[y], cb[y + 1])));
                }
            if (crossed)
                problems.Add((traced.Count == 1 ? "The shape" : "Shape " + (i + 1)) + " crosses itself. A profile's "
                    + "loop goes round once, never over its own edges.");
        }
        for (var i = 0; i < traced.Count; i++)
            for (var j = i + 1; j < traced.Count; j++)
            {
                var hit = traced[i].Any(ca => traced[j].Any(cb =>
                    Enumerable.Range(0, ca.Count - 1).Any(x => Enumerable.Range(0, cb.Count - 1)
                        .Any(y => meet(ca[x], ca[x + 1], cb[y], cb[y + 1])))));
                if (hit)
                    problems.Add("Shapes " + (i + 1) + " and " + (j + 1) + " cross or touch. A profile's loops may sit "
                        + "one inside another - a hole - but never cross or touch.");
            }
    }

    if (wantedName.Length == 0)
        problems.Add("No subcategory was named. Name one for the lines' weight, or \"none\" for the family's own "
            + "category, " + (category == null ? "" : category.Name) + ".");
    else if (onCategory) wanted = category;
    else if (category != null)
    {
        foreach (Category sub in category.SubCategories)
            if (string.Equals(sub.Name, wantedName, StringComparison.OrdinalIgnoreCase)) { wanted = sub; break; }
        if (wanted == null && !category.CanAddSubcategory)
            problems.Add("The family's category, " + category.Name + ", takes no subcategories, so \"" + wantedName
                + "\" cannot be made in it - name \"none\".");
    }

    if (problems.Count > 0) refused = "Nothing was drawn. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// DRAW, LOCK, THEN READ EVERY LINE BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var axis = axisOf(target.ViewDirection);
    var at = along(target.Origin, axis);
    var u = inPlane[axis][0];
    var v = inPlane[axis][1];
    Func<double, double, XYZ> onPlane = (first, second) =>
    {
        var xyz = new double[3];
        xyz[axis] = at;
        xyz[u] = first / 304.8;
        xyz[v] = second / 304.8;
        return new XYZ(xyz[0], xyz[1], xyz[2]);
    };

    var madeSubcategory = false;
    if (wanted == null)
    {
        try
        {
            wanted = doc.Settings.Categories.NewSubcategory(category, wantedName);
            madeSubcategory = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not make the subcategory \"" + wantedName + "\" under "
                + category.Name + ": " + ex.Message + " The call failed, and Heron rolls the whole call back.");
        }
    }
    // A NAMED SUBCATEGORY'S STYLE IS ITS PROJECTION STYLE, offered to Revit only
    // where Revit lists it as one this line may take (CurveElement.LineStyle's
    // remark ties line styles to the Lines category, so the line's own list is
    // the test), then READ BACK. "none" leaves Revit's default, which is read
    // back and reported by name rather than assumed.
    GraphicsStyle style = null;
    if (!onCategory)
    {
        try { style = wanted.GetGraphicsStyle(GraphicsStyleType.Projection); } catch (Exception) { style = null; }
        if (style == null)
            throw new InvalidOperationException("\"" + wanted.Name + "\" has no projection line style for detail lines "
                + "to take. The call failed, and Heron rolls the whole call back.");
    }

    var made = new List<Tuple<DetailCurve, XYZ, XYZ>>();
    foreach (var piece in pieces)
        foreach (var s in piece.Item1)
        {
            var a = onPlane(s[1], s[2]);
            var b = onPlane(s[5], s[6]);
            DetailCurve line;
            try
            {
                Curve curve = s[0] == 0.0 ? (Curve)Line.CreateBound(a, b) : Arc.Create(a, b, onPlane(s[3], s[4]));
                line = doc.FamilyCreate.NewDetailCurve(target, curve);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit refused a line from " + plain(s[1]) + "," + plain(s[2]) + " to "
                    + plain(s[5]) + "," + plain(s[6]) + " in \"" + target.Name + "\": " + ex.Message + " The call failed, "
                    + "and Heron rolls the whole call back.");
            }
            if (line == null)
                throw new InvalidOperationException("Revit made no line from " + plain(s[1]) + "," + plain(s[2]) + " to "
                    + plain(s[5]) + "," + plain(s[6]) + ". The call failed, and Heron rolls the whole call back.");
            if (style != null)
            {
                var allowed = line.GetLineStyleIds();
                if (allowed != null && allowed.Count > 0 && !allowed.Contains(style.Id))
                    throw new InvalidOperationException("Revit does not offer \"" + wanted.Name + "\" as a style for a "
                        + "detail line here - it offers " + string.Join(", ", allowed.Select(id => doc.GetElement(id))
                            .Where(e => e != null).Select(e => e.Name).Take(15)) + ". The call failed, and Heron rolls the "
                        + "whole call back.");
                try { line.LineStyle = style; }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Revit would not put a line on \"" + wanted.Name + "\": "
                        + ex.Message + " The call failed, and Heron rolls the whole call back.");
                }
            }
            made.Add(Tuple.Create(line, a, b));
        }

    doc.Regenerate();

    // LOCK every straight line to the named plane it lies along.
    var locks = new List<string>();
    var free = 0;
    if (lockToPlanes)
    {
        var planes = new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>()
            .Where(rp => ownName(rp).Length > 0).ToList();
        foreach (var m in made)
        {
            var straight = m.Item1.GeometryCurve as Line;
            if (straight == null) continue;
            var direction = straight.Direction;
            var onIt = planes.Where(rp =>
            {
                var plane = rp.GetPlane();
                return Math.Abs(plane.Normal.DotProduct(direction)) < 1e-6
                    && Math.Abs(plane.Normal.DotProduct(straight.GetEndPoint(0) - plane.Origin)) < halfMillimetre
                    && Math.Abs(plane.Normal.DotProduct(straight.GetEndPoint(1) - plane.Origin)) < halfMillimetre
                    && Math.Abs(plane.Normal.DotProduct(target.ViewDirection)) < 1e-6;
            }).ToList();
            if (onIt.Count == 0) { free++; continue; }
            // ONE PLANE PER LINE. Where named planes coincide - a template's
            // centre plane under an edge plane - the one that does not define
            // the origin is the one a size moves, so the line follows it alone.
            // Two such planes are ambiguous and refused, never both locked: a
            // label moving one apart would leave the line held by both.
            if (onIt.Count > 1)
            {
                var movable = onIt.Where(rp =>
                {
                    var origin = rp.get_Parameter(BuiltInParameter.DATUM_PLANE_DEFINES_ORIGIN);
                    return origin == null || origin.StorageType != StorageType.Integer || origin.AsInteger() != 1;
                }).ToList();
                if (movable.Count != 1)
                    throw new InvalidOperationException("A line lies along " + string.Join(" and ",
                        onIt.Select(rp => "\"" + ownName(rp) + "\"")) + ", which coincide, and lockToPlanes cannot tell "
                        + "which it should follow. Move one plane apart, or draw without locks and lock the line by hand. "
                        + "The call failed, and Heron rolls the whole call back.");
                findings.Add("A line along \"" + ownName(movable[0]) + "\" also lies on " + string.Join(", ",
                    onIt.Where(rp => rp.Id != movable[0].Id).Select(rp => "\"" + ownName(rp) + "\""))
                    + ", which defines the origin - it is locked to \"" + ownName(movable[0]) + "\" only.");
                onIt = movable;
            }
            foreach (var rp in onIt)
            {
                Dimension alignment;
                try
                {
                    alignment = doc.FamilyCreate.NewAlignment(target, rp.GetReference(), straight.Reference);
                    if (alignment != null && !alignment.IsLocked) alignment.IsLocked = true;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Revit would not lock the line along \"" + ownName(rp) + "\": "
                        + ex.Message + " The call failed, and Heron rolls the whole call back.");
                }
                if (alignment == null || !alignment.IsLocked)
                    throw new InvalidOperationException("The line along \"" + ownName(rp) + "\" does not read as locked "
                        + "to it. The call failed, and Heron rolls the whole call back.");
                locks.Add(ownName(rp));
            }
        }
        doc.Regenerate();
    }

    // READ BACK: ends, then style.
    var ids = new List<string>();
    var styleNames = new List<string>();
    var length = 0.0;
    foreach (var m in made)
    {
        var curve = m.Item1.GeometryCurve;
        if (curve == null)
            throw new InvalidOperationException("A line " + m.Item1.UniqueId + " has no curve after it was drawn. The call "
                + "failed, and Heron rolls the whole call back.");
        var p0 = curve.GetEndPoint(0);
        var p1 = curve.GetEndPoint(1);
        var asked = (p0.DistanceTo(m.Item2) <= halfMillimetre && p1.DistanceTo(m.Item3) <= halfMillimetre)
            || (p0.DistanceTo(m.Item3) <= halfMillimetre && p1.DistanceTo(m.Item2) <= halfMillimetre);
        if (!asked)
            throw new InvalidOperationException("A line reads from " + mm(along(p0, u)) + "," + mm(along(p0, v)) + " to "
                + mm(along(p1, u)) + "," + mm(along(p1, v)) + " mm, not where it was drawn"
                + (lockToPlanes ? " - locking moved it" : "") + ". The call failed, and Heron rolls the whole call back.");
        if (style != null && (m.Item1.LineStyle == null || m.Item1.LineStyle.Id != style.Id))
            throw new InvalidOperationException("A line reads the line style \""
                + (m.Item1.LineStyle == null ? "none" : m.Item1.LineStyle.Name) + "\", not \"" + wanted.Name + "\". The call "
                + "failed, and Heron rolls the whole call back.");
        styleNames.Add(m.Item1.LineStyle == null ? "none" : m.Item1.LineStyle.Name);
        ids.Add(m.Item1.UniqueId);
        length += curve.Length;
    }

    lineIds = string.Join(",", ids);
    drawn = made.Count + " detail line(s) in " + pieces.Count + " shape(s) in \"" + target.Name + "\", " + mm(length)
        + " mm in all, on " + (style != null ? "\"" + wanted.Name + "\""
            : "Revit's default style, \"" + string.Join("\", \"", styleNames.Distinct()) + "\"")
        + (lockToPlanes ? ", " + locks.Count + " lock(s) to " + string.Join(", ", locks.Distinct()) + " and " + free
            + " straight line(s) on no named plane" : "") + " - read back.";
    findings.Add(drawn);
    if (madeSubcategory)
        findings.Add("Made the subcategory \"" + wanted.Name + "\"; its weight and colour are Revit's defaults until set "
            + "in Object Styles.");
    if (profile)
        findings.Add("In a profile family every line is the profile. Its Profile Usage - Wall Sweep, Mullion, Railing - "
            + "is SET_FAMILY_SETTINGS; a size is LABEL_FAMILY_DIMENSION between the planes the lines are locked to.");
}

if (refused != null) findings.Add(refused);
