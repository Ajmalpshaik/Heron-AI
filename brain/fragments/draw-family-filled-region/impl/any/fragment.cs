// NOT STANDALONE. Assumes `doc`, `workPlane`, `shapes` and `regionType` are in
// scope; leaves `regionId`, `drawn`, `notAFamily`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A FILLED OR MASKING REGION INSIDE A FAMILY - the solid patch of a detail
// item's cut steel, the hatched insulation of a line-based detail, the white
// mask a symbol hides what is under it with, the solid fill of a plan symbol in
// a model family. The Family Editor's Filled Region and Masking Region.
//
// TWO KINDS OF FAMILY, TWO CALLS, AND THE RELEASE DECIDES WHAT EXISTS:
//   a 2D family - a detail item, an annotation, a profile, a title block - draws
//   in its VIEW, by the view's name, on every release 2020 to 2027;
//   a 3D model family draws on a level or a named reference plane, and Revit
//   has a call for that only from 2023;
//   a MASKING region has a call of its own only from 2024, both kinds.
// Each is found on the running Revit by its own signature; a release without
// it is refused by name, never imitated.
//
// THE SAME SHAPE LANGUAGE AS THE FORM TOOLS, millimetres in the plane's own
// two model coordinates - X,Y on a level, a horizontal plane or a plan view.
// Closed loops only, `|` between them: "circle CX,CY R", "rect X1,Y1 X2,Y2",
// "polygon CX,CY R N", or corners "X,Y; X,Y; arc MX,MY X,Y" closed back to the
// first. A loop inside another is a hole; Revit decides by where they lie.
//
// THE TYPE IS NAMED - "Solid Black", or the family's own name for a hatch - or
// "masking". A name the family does not hold is refused with the ones it does.
//
// READ BACK, ALL OR NOTHING: the region, its loops, its type and whether it is
// masking are read again; one that does not read as asked fails the call, and
// the host rolls the whole call back.

var findings = new List<string>();
var regionId = "";
var drawn = "";
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
Func<double, string> plain = value => Math.Round(value, 2).ToString(invariant);
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

// Which world axis a direction lies along: 0 X, 1 Y, 2 Z, -1 none of them.
Func<XYZ, int> axisOf = n =>
    n == null ? -1 : Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;
Func<XYZ, int, double> along = (point, index) => index == 0 ? point.X : index == 1 ? point.Y : point.Z;
// The two model coordinates a shape is written in, per axis the plane faces.
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


// ---- the family, the place, the shapes, the type ----------------------------

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

var masking = squash(regionType) == "masking" || squash(regionType) == "maskingregion";
View view = null;
Tuple<Element, int, double, string> plane = null;
FilledRegionType fillType = null;
var loops = new List<List<double[]>>();
System.Reflection.MethodInfo call = null;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "A region in a project's view is CREATE_FILLED_REGION; this one draws inside a family - open it first.";
}
else
{
    var said = (workPlane ?? "").Trim();
    if (twoD)
    {
        // A 2D FAMILY DRAWS IN ITS VIEW, named as the Project Browser shows it.
        var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate).ToList();
        var found = views.Where(v => string.Equals(v.Name, said, StringComparison.OrdinalIgnoreCase)).ToList();
        if (said.Length == 0) problems.Add("No view was named. This is a 2D family - name the view to draw in: "
            + string.Join(", ", views.Select(v => v.Name).Take(10)) + ".");
        else if (found.Count != 1)
            problems.Add("No single view is called \"" + said + "\". This family's views: "
                + (views.Count == 0 ? "none" : string.Join(", ", views.Select(v => v.Name).Take(10))) + ".");
        else if (axisOf(found[0].ViewDirection) < 0)
            problems.Add("The view \"" + said + "\" looks at an angle; a shape here is written in model coordinates.");
        else view = found[0];
    }
    else
    {
        // A 3D FAMILY DRAWS ON A LEVEL OR A NAMED REFERENCE PLANE.
        var datums = new List<Tuple<Element, int, double, string>>();
        foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
        {
            var name = ownName(rp);
            if (name.Length == 0) continue;
            var at = axisOf(rp.Normal);
            datums.Add(Tuple.Create((Element)rp, at, at < 0 ? 0.0 : along(rp.GetPlane().Origin, at), name));
        }
        foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
            datums.Add(Tuple.Create((Element)level, 2, level.Elevation, level.Name));
        var found = datums.Where(d => string.Equals(d.Item4, said, StringComparison.OrdinalIgnoreCase)).ToList();
        if (said.Length == 0) problems.Add("No work plane was named - a level or a named reference plane.");
        else if (found.Count == 0)
            problems.Add("No reference plane or level is called \"" + said + "\". This family has "
                + (datums.Count == 0 ? "no named ones."
                    : string.Join(", ", datums.Select(d => d.Item4).Distinct().OrderBy(n => n).Take(20)) + "."));
        else if (found.Count > 1) problems.Add("\"" + said + "\" names " + found.Count + " planes - rename one first.");
        else if (found[0].Item2 < 0)
            problems.Add("\"" + said + "\" is at an angle. Revit draws a region only on a plane facing left-right, "
                + "front-back or up.");
        else plane = found[0];
    }

    var texts = (shapes ?? "").Split('|').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
    if (texts.Count == 0) problems.Add("No shapes were given - closed loops, \"rect -300,-50 300,50\".");
    for (var i = 0; i < texts.Count; i++)
    {
        if (texts[i].ToLowerInvariant().StartsWith("path"))
        {
            problems.Add("Loop " + (i + 1) + " is an open path. A region is closed loops only.");
            continue;
        }
        var loop = loopOf(texts[i], texts.Count == 1 ? "The shape" : "Loop " + (i + 1));
        if (loop != null) loops.Add(loop);
    }

    // THE CALL THIS RELEASE HAS, by its own signature.
    var curveLoops = typeof(IList<CurveLoop>);
    if (masking)
        call = typeof(FilledRegion).GetMethod("CreateMaskingRegion",
            new[] { typeof(Document), twoD ? typeof(ElementId) : typeof(SketchPlane), curveLoops });
    else
    {
        var name = (regionType ?? "").Trim();
        var kinds = new FilteredElementCollector(doc).OfClass(typeof(FilledRegionType)).Cast<FilledRegionType>().ToList();
        fillType = kinds.FirstOrDefault(k => string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase));
        if (name.Length == 0 || fillType == null)
            problems.Add((name.Length == 0 ? "No region type was named." : "No filled region type is called \"" + name
                + "\" in this family.") + " It holds: " + (kinds.Count == 0 ? "none"
                : string.Join(", ", kinds.Select(k => k.Name).OrderBy(n => n).Take(20))) + " - or name \"masking\".");
        call = typeof(FilledRegion).GetMethod("Create",
            new[] { typeof(Document), typeof(ElementId), twoD ? typeof(ElementId) : typeof(SketchPlane), curveLoops });
    }
    // A filled region in a 3D family needs the 2023 call; a masking region the
    // 2024 one. Which is missing decides what to offer instead.
    var filledIn3D = twoD || typeof(FilledRegion).GetMethod("Create",
        new[] { typeof(Document), typeof(ElementId), typeof(SketchPlane), curveLoops }) != null;
    if (call == null && masking && filledIn3D)
        problems.Add("This Revit has no call to draw a masking region - it arrived in Revit 2024. Name a filled region "
            + "type whose Masking box is ticked instead, which hides what is behind it the same way, or draw it by "
            + "hand with Masking Region.");
    else if (call == null)
        problems.Add("This Revit has no call to draw a " + (masking ? "masking or a " : "") + "filled region in a 3D "
            + "model family - the filled region arrived in Revit 2023, the masking region in 2024. Draw the symbol's "
            + "outline with DRAW_FAMILY_SYMBOLIC_LINES, or nest a detail item.");

    if (problems.Count > 0) refused = "Nothing was drawn. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// DRAW, THEN READ THE REGION BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var axis = twoD ? axisOf(view.ViewDirection) : plane.Item2;
    var at = twoD ? along(view.Origin, axis) : plane.Item3;
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

    var boundaries = new List<CurveLoop>();
    foreach (var loop in loops)
    {
        var ring = new CurveLoop();
        foreach (var s in loop)
        {
            var a = onPlane(s[1], s[2]);
            var b = onPlane(s[5], s[6]);
            ring.Append(s[0] == 0.0 ? (Curve)Line.CreateBound(a, b) : Arc.Create(a, b, onPlane(s[3], s[4])));
        }
        boundaries.Add(ring);
    }

    object where;
    var placeName = twoD ? "the view \"" + view.Name + "\"" : "\"" + plane.Item4 + "\"";
    if (twoD) where = view.Id;
    else
    {
        try { where = SketchPlane.Create(doc, plane.Item1.Id); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not make a sketch plane on \"" + plane.Item4 + "\": "
                + ex.Message + " The call failed, and Heron rolls the whole call back.");
        }
    }

    FilledRegion region;
    try
    {
        var arguments = masking
            ? new object[] { doc, where, (IList<CurveLoop>)boundaries }
            : new object[] { doc, fillType.Id, where, (IList<CurveLoop>)boundaries };
        region = call.Invoke(null, arguments) as FilledRegion;
    }
    catch (System.Reflection.TargetInvocationException ex)
    {
        throw new InvalidOperationException("Revit refused the " + (masking ? "masking" : "filled") + " region on "
            + placeName + ": " + (ex.InnerException == null ? ex.Message : ex.InnerException.Message)
            + " The call failed, and Heron rolls the whole call back.");
    }
    if (region == null)
        throw new InvalidOperationException("Revit made no region on " + placeName + ". The call failed, and Heron "
            + "rolls the whole call back.");

    doc.Regenerate();

    // READ BACK: the loops, masking or not, and the type.
    var readLoops = region.GetBoundaries();
    if (readLoops == null || readLoops.Count != boundaries.Count)
        throw new InvalidOperationException("The region reads " + (readLoops == null ? 0 : readLoops.Count)
            + " loop(s), not the " + boundaries.Count + " drawn. The call failed, and Heron rolls the whole call back.");
    if (region.IsMasking != masking && (masking || !fillType.IsMasking))
        throw new InvalidOperationException("The region reads " + (region.IsMasking ? "masking" : "filled")
            + ", not what was asked. The call failed, and Heron rolls the whole call back.");
    if (!masking && region.GetTypeId() != fillType.Id)
        throw new InvalidOperationException("The region reads a different type from \"" + fillType.Name + "\". The "
            + "call failed, and Heron rolls the whole call back.");

    var length = readLoops.Sum(l => l.GetExactLength());
    regionId = region.UniqueId;
    drawn = (region.IsMasking ? "A masking" : "A filled") + " region" + (masking ? "" : " of type \"" + fillType.Name
        + "\"") + " on " + placeName + ": " + readLoops.Count + " loop(s), " + mm(length) + " mm of boundary - read "
        + "back.";
    findings.Add(drawn);
    if (!twoD)
        findings.Add("In a 3D family the region shows in views parallel to \"" + plane.Item4 + "\" - a plan, for one "
            + "on a level - and never in 3D, like a symbolic line.");
    findings.Add("Its boundary lines take Revit's thin lines. A region only some types show is linked to a Yes/No "
        + "parameter by the button beside Visible in its Properties.");
}

if (refused != null) findings.Add(refused);
