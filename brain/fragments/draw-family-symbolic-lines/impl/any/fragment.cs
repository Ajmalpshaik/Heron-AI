// NOT STANDALONE. Assumes `doc`, `workPlane`, `shapes`, `detailLevels` and
// `subcategory` are in scope; leaves `lineIds`, `drawn`, `notAFamily`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// SYMBOLIC LINES - the 2D lines a family draws in plan or elevation in place of
// its 3D forms: the bow tie of a valve, the circle and cross of a fan, the arc
// of a door's swing. The Family Editor's Symbolic Line. They show only in
// views parallel to the plane they are drawn on - a plan, for lines on a level
// - and never in 3D, which is why a family hides a form in a coarse plan with
// SET_FAMILY_FORM_VISIBILITY and draws these there instead.
//
// THE SAME SHAPE LANGUAGE AS THE FORM TOOLS, in millimetres from the family
// origin, in the plane's own two model coordinates - X,Y on a level or a
// horizontal plane, X,Z on a plane facing front or back, Y,Z on one facing
// left or right. Closed shapes are "circle CX,CY R", "rect X1,Y1 X2,Y2",
// "polygon CX,CY R N", or corners "X,Y; X,Y; arc MX,MY X,Y" closed back to the
// first. OPEN LINES are "path X,Y; X,Y; arc MX,MY X,Y" and are not closed.
// Pieces have `|` between them.
//
// THE DETAIL LEVELS ARE NAMED IN FULL - "coarse, medium" or "all" - so a
// symbol drawn for coarse plans can stand in for a form hidden there. Which
// kind of visibility Revit takes for a symbolic line is not in its remarks,
// beyond "not valid" being refused: the one for things shown only where they
// are drawn is tried first, then the one forms take, and the setting is READ
// BACK either way.
//
// ON A SUBCATEGORY of the family's own category, made when it is new - "none"
// leaves the lines on the category itself.
//
// SKETCHED ON THE PLANE ITSELF, from its element id, so the lines move with
// it. READ BACK, ALL OR NOTHING: every line's ends, its subcategory and its
// detail levels are read again, and one that does not read as asked fails the
// call; the host rolls the whole call back.

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

// Which world axis a direction lies along: 0 X, 1 Y, 2 Z, -1 none of them.
Func<XYZ, int> axisOf = n =>
    Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;
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

// ---- the plane, the shapes, the levels, the subcategory ---------------------

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
// Each piece: its segments, and whether it is closed.
var pieces = new List<Tuple<List<double[]>, bool>>();
bool coarse = false, medium = false, fine = false;
Category parent = null;
Category wanted = null;
var wantedName = (subcategory ?? "").Trim();
var onCategory = squash(wantedName) == "none";

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A family's symbolic "
        + "lines are drawn inside the family - open it first.";
}
else
{
    var said = (workPlane ?? "").Trim();
    var found = datums.Where(d => string.Equals(d.Item4, said, StringComparison.OrdinalIgnoreCase)).ToList();
    if (said.Length == 0) problems.Add("No work plane was named.");
    else if (found.Count == 0)
        problems.Add("No reference plane or level is called \"" + said + "\". This family has "
            + (datums.Count == 0 ? "no named ones."
                : string.Join(", ", datums.Select(d => d.Item4).Distinct().OrderBy(n => n).Take(20)) + "."));
    else if (found.Count > 1) problems.Add("\"" + said + "\" names " + found.Count + " planes - rename one first.");
    else if (found[0].Item2 < 0)
        problems.Add("\"" + said + "\" is at an angle. A shape here is written in the model's own coordinates, so "
            + "the plane must face left-right, front-back or up.");
    else plane = found[0];

    var texts = (shapes ?? "").Split('|').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
    if (texts.Count == 0) problems.Add("No shapes were given.");
    for (var i = 0; i < texts.Count; i++)
    {
        var where = texts.Count == 1 ? "The shape" : "Shape " + (i + 1);
        var words = texts[i].Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 0 && words[0].ToLowerInvariant() == "path")
        {
            var open = openOf(words.Length > 1 ? words[1] : "", where);
            if (open != null) pieces.Add(Tuple.Create(open, false));
        }
        else
        {
            var closed = loopOf(texts[i], where);
            if (closed != null) pieces.Add(Tuple.Create(closed, true));
        }
    }

    foreach (var word in (detailLevels ?? "").Split(',').Select(squash).Where(w => w.Length > 0))
    {
        if (word == "all") { coarse = true; medium = true; fine = true; }
        else if (word == "coarse") coarse = true;
        else if (word == "medium") medium = true;
        else if (word == "fine") fine = true;
        else problems.Add("\"" + word + "\" is not a detail level. Name coarse, medium and fine, or all.");
    }
    if (!coarse && !medium && !fine)
        problems.Add("No detail level was named. Lines shown at none would never be seen - name coarse, medium or "
            + "fine, or all.");

    parent = doc.OwnerFamily == null ? null : doc.OwnerFamily.FamilyCategory;
    if (parent == null) problems.Add("This family has no category to draw lines in - SET_FAMILY_CATEGORY first.");
    else if (wantedName.Length == 0)
        problems.Add("No subcategory was named. Name one, or \"none\" for the family's own category, " + parent.Name
            + ".");
    else if (onCategory) wanted = parent;
    else
    {
        foreach (Category sub in parent.SubCategories)
            if (string.Equals(sub.Name, wantedName, StringComparison.OrdinalIgnoreCase)) { wanted = sub; break; }
        if (wanted == null && !parent.CanAddSubcategory)
            problems.Add("The family's category, " + parent.Name + ", takes no subcategories, so \"" + wantedName
                + "\" cannot be made in it.");
    }

    if (problems.Count > 0) refused = "Nothing was drawn. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// DRAW, THEN READ EVERY LINE BACK
// ---------------------------------------------------------------------------

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

    var madeSubcategory = false;
    if (wanted == null)
    {
        try
        {
            wanted = doc.Settings.Categories.NewSubcategory(parent, wantedName);
            madeSubcategory = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not make the subcategory \"" + wantedName + "\" under "
                + parent.Name + ": " + ex.Message + " The call failed, and Heron rolls the whole call back.");
        }
    }

    GraphicsStyle style = null;
    try { style = wanted.GetGraphicsStyle(GraphicsStyleType.Projection); }
    catch (Exception) { style = null; }
    if (style == null)
        throw new InvalidOperationException("\"" + wanted.Name + "\" has no projection line style for symbolic lines to "
            + "take. The call failed, and Heron rolls the whole call back.");

    SketchPlane sketch;
    try
    {
        sketch = SketchPlane.Create(doc, plane.Item1.Id);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit would not make a sketch plane on \"" + plane.Item4 + "\": "
            + ex.Message + " The call failed, and Heron rolls the whole call back.");
    }
    var surface = sketch.GetPlane();
    if (axisOf(surface.Normal) != axis || Math.Abs(along(surface.Origin, axis) - at) > halfMillimetre)
        throw new InvalidOperationException("The sketch plane made on \"" + plane.Item4 + "\" does not lie on it. "
            + "The call failed, and Heron rolls the whole call back.");

    // Each line drawn, with the two ends it was asked for.
    var made = new List<Tuple<SymbolicCurve, XYZ, XYZ>>();
    var kindTaken = "";
    foreach (var piece in pieces)
        foreach (var s in piece.Item1)
        {
            var a = onPlane(s[1], s[2]);
            var b = onPlane(s[5], s[6]);
            SymbolicCurve line;
            try
            {
                Curve curve = s[0] == 0.0 ? (Curve)Line.CreateBound(a, b) : Arc.Create(a, b, onPlane(s[3], s[4]));
                line = doc.FamilyCreate.NewSymbolicCurve(curve, sketch);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit refused a symbolic line from " + plain(s[1]) + "," + plain(s[2])
                    + " to " + plain(s[5]) + "," + plain(s[6]) + " on \"" + plane.Item4 + "\": " + ex.Message
                    + " The call failed, and Heron rolls the whole call back.");
            }
            if (line == null)
                throw new InvalidOperationException("Revit made no symbolic line from " + plain(s[1]) + "," + plain(s[2])
                    + " to " + plain(s[5]) + "," + plain(s[6]) + ". The call failed, and Heron rolls the whole call back.");

            // A SYMBOLIC LINE'S SUBCATEGORY IS A LINE STYLE - the subcategory's
            // projection style - and is set even for the category itself, so
            // what it reads is what was asked rather than Revit's default.
            try
            {
                line.Subcategory = style;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit would not put a symbolic line on \"" + wanted.Name + "\": "
                    + ex.Message + " The call failed, and Heron rolls the whole call back.");
            }

            // THE KIND OF VISIBILITY A SYMBOLIC LINE TAKES is tried, not assumed:
            // the kind for what shows only where it is drawn, then the forms' kind.
            var set = false;
            var refusals = new List<string>();
            foreach (var kind in new[] { FamilyElementVisibilityType.ViewSpecific, FamilyElementVisibilityType.Model })
            {
                if (kindTaken.Length > 0 && kindTaken != kind.ToString()) continue;
                try
                {
                    var setting = new FamilyElementVisibility(kind);
                    setting.IsShownInCoarse = coarse;
                    setting.IsShownInMedium = medium;
                    setting.IsShownInFine = fine;
                    line.SetVisibility(setting);
                    kindTaken = kind.ToString();
                    set = true;
                    break;
                }
                catch (Exception ex)
                {
                    refusals.Add(kind + ": " + ex.Message);
                }
            }
            if (!set)
                throw new InvalidOperationException("Revit would not set the detail levels of a symbolic line: "
                    + string.Join("; ", refusals) + " The call failed, and Heron rolls the whole call back.");
            made.Add(Tuple.Create(line, a, b));
        }

    doc.Regenerate();

    // READ BACK: ends, subcategory, levels.
    var ids = new List<string>();
    var length = 0.0;
    foreach (var m in made)
    {
        var curve = m.Item1.GeometryCurve;
        if (curve == null)
            throw new InvalidOperationException("A symbolic line " + m.Item1.UniqueId + " has no curve after it was "
                + "drawn. The call failed, and Heron rolls the whole call back.");
        var p0 = curve.GetEndPoint(0);
        var p1 = curve.GetEndPoint(1);
        var asked = (p0.DistanceTo(m.Item2) <= halfMillimetre && p1.DistanceTo(m.Item3) <= halfMillimetre)
            || (p0.DistanceTo(m.Item3) <= halfMillimetre && p1.DistanceTo(m.Item2) <= halfMillimetre);
        if (!asked)
            throw new InvalidOperationException("A symbolic line reads from " + mm(along(p0, u)) + "," + mm(along(p0, v))
                + " to " + mm(along(p1, u)) + "," + mm(along(p1, v)) + " mm, not where it was drawn. The call failed, "
                + "and Heron rolls the whole call back.");
        var takenStyle = m.Item1.Subcategory;
        var category = takenStyle == null ? null : takenStyle.GraphicsStyleCategory;
        if (category == null || category.Id != wanted.Id)
            throw new InvalidOperationException("A symbolic line reads the subcategory \""
                + (category == null ? "none" : category.Name) + "\", not \"" + wanted.Name + "\". The call failed, and "
                + "Heron rolls the whole call back.");
        var shown = m.Item1.GetVisibility();
        if (shown == null || shown.IsShownInCoarse != coarse || shown.IsShownInMedium != medium
            || shown.IsShownInFine != fine)
            throw new InvalidOperationException("A symbolic line does not read back at the detail levels asked. The "
                + "call failed, and Heron rolls the whole call back.");
        ids.Add(m.Item1.UniqueId);
        length += curve.Length;
    }

    lineIds = string.Join(",", ids);
    var levels = string.Join(", ", new[] { coarse ? "coarse" : null, medium ? "medium" : null, fine ? "fine" : null }
        .Where(x => x != null));
    drawn = made.Count + " symbolic line(s) in " + pieces.Count + " shape(s) on \"" + plane.Item4 + "\", "
        + mm(length) + " mm in all, on " + (onCategory ? "the family's category, " + parent.Name : "\"" + wanted.Name + "\"")
        + ", shown at " + levels + " - every end read back.";
    findings.Add(drawn);
    if (madeSubcategory)
        findings.Add("Made the subcategory \"" + wanted.Name + "\" under " + parent.Name + "; its line weight and "
            + "colour are Revit's defaults until set in Object Styles.");
    findings.Add("Revit took the " + kindTaken + " kind of visibility for these lines (NEEDS-CHECKING BQ2).");
    findings.Add("They show only in views parallel to \"" + plane.Item4 + "\", never in 3D. To show them in place "
        + "of a form, hide the form at the same levels with SET_FAMILY_FORM_VISIBILITY.");
}

if (refused != null) findings.Add(refused);
