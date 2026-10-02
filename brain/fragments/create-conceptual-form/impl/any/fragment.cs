// NOT STANDALONE. Assumes `doc`, `profiles`, `depthMm`, `path` and `solid` are
// in scope; leaves `formId`, `built`, `notAFamily`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A FORM ON LINES - in an adaptive component, a pattern-based panel or a mass,
// the Family Editor's Create Form. Built on curves that run through the
// family's points, the form follows the points: the flat glass of a panel that
// fits every cell, an adaptive fin, a lofted canopy, a two-point beam. The
// classic forms - extrusion, blend, revolve, sweep - are the other Family
// Editor's, and are not made in these families; this is the one these have.
//
// WHAT IS MADE FOLLOWS FROM WHAT IS GIVEN, so no kind is asked:
//   ONE closed profile, no depth, no path  -> a flat SURFACE filling it (a cap);
//   ONE closed profile and a depth         -> an EXTRUSION that far along its normal;
//   TWO OR MORE profiles, no path          -> a LOFT through them in the order given;
//   a PATH and one profile                 -> a SWEEP of the profile along it;
//   a PATH and two or more profiles        -> a SWEPT BLEND along it.
// A profile is curve ids, commas between - the ids DRAW_FAMILY_POINT_CURVES
// gives back - and profiles have `|` between them; a path is curve ids too.
//
// WHICH SIDE A DEPTH GOES IS A RULE, NOT THE ORDER THE LINES WERE DRAWN IN: the
// profile's normal is taken from its curves chained end to end, then turned so
// that the axis it faces most is positive - up for a profile lying flat, +Y or
// +X for an upright one. A negative depth goes the other way.
//
// READ BACK, ALL OR NOTHING: the form, its profile count and what it encloses
// - a solid's volume or a surface's area - are read again; a form that holds no
// geometry fails the call, and the host rolls the whole call back.

var findings = new List<string>();
var formId = "";
var built = "";
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;

var problems = new List<string>();
// Each profile's curves, and the path's.
var groups = new List<List<CurveElement>>();
var route = new List<CurveElement>();
double depth = 0;
var hasDepth = false;
XYZ normal = null;

Func<string, List<CurveElement>> curvesOf = text =>
{
    var found = new List<CurveElement>();
    foreach (var token in text.Split(new[] { ',', ' ', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var curve = doc.GetElement(token) as CurveElement;
        if (curve == null || curve.GeometryCurve == null || curve.GeometryCurve.Reference == null)
        {
            problems.Add("\"" + token + "\" is not the id of a line in this family that a form can be built on.");
            continue;
        }
        found.Add(curve);
    }
    return found;
};

// A PROFILE CLOSES when every end meets another end - or when it is one closed
// circle or ellipse, which has no ends at all.
Func<List<CurveElement>, bool> closes = group =>
{
    if (group.Count == 1 && !group[0].GeometryCurve.IsBound) return true;
    if (group.Any(c => !c.GeometryCurve.IsBound)) return false;
    var ends = new List<XYZ>();
    foreach (var c in group)
    {
        ends.Add(c.GeometryCurve.GetEndPoint(0));
        ends.Add(c.GeometryCurve.GetEndPoint(1));
    }
    if (group.Count == 1) return ends[0].DistanceTo(ends[1]) < halfMillimetre;
    return ends.All(e => ends.Count(o => o.DistanceTo(e) < halfMillimetre) >= 2);
};

// THE PROFILE'S POINTS IN ORDER ROUND IT: each curve's tessellation, the next
// curve the one whose end meets the last, turned round where it was drawn the
// other way. Null when the curves do not chain.
Func<List<CurveElement>, List<XYZ>> ringOf = group =>
{
    var left = group.Select(c => c.GeometryCurve).ToList();
    var ring = new List<XYZ>(left[0].Tessellate());
    left.RemoveAt(0);
    while (left.Count > 0)
    {
        var end = ring[ring.Count - 1];
        var next = -1;
        var turned = false;
        for (var i = 0; i < left.Count && next < 0; i++)
        {
            if (left[i].GetEndPoint(0).DistanceTo(end) < halfMillimetre) next = i;
            else if (left[i].GetEndPoint(1).DistanceTo(end) < halfMillimetre) { next = i; turned = true; }
        }
        if (next < 0) return null;
        var points = new List<XYZ>(left[next].Tessellate());
        if (turned) points.Reverse();
        ring.AddRange(points.Skip(1));
        left.RemoveAt(next);
    }
    return ring;
};

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "A form on lines is made inside an adaptive or pattern-based family - open it first.";
}
else
{
    // ONLY THE CONCEPTUAL FAMILIES MAKE THESE FORMS - the same three flags
    // PLACE_ADAPTIVE_POINTS reads.
    var family = doc.OwnerFamily;
    var conceptual = false;
    var adaptive = false;
    var panel = false;
    try { conceptual = family != null && family.IsConceptualMassFamily; } catch (Exception) { }
    try { adaptive = family != null && AdaptiveComponentFamilyUtils.IsAdaptiveComponentFamily(family); } catch (Exception) { }
    try { panel = family != null && family.IsCurtainPanelFamily; } catch (Exception) { }
    if (!conceptual && !adaptive && !panel)
        problems.Add("This family is not an adaptive component, a pattern-based panel or a mass, which are the families "
            + "that make forms on lines. A classic family's forms are CREATE_FAMILY_EXTRUSION, CREATE_FAMILY_SWEEP and "
            + "their siblings (REPORT_FAMILY_TEMPLATE says which this is).");

    var said = (depthMm ?? "").Trim();
    if (said.Length > 0)
    {
        double value;
        if (!double.TryParse(said, System.Globalization.NumberStyles.Float, invariant, out value)
            || double.IsNaN(value) || double.IsInfinity(value))
            problems.Add("\"" + said + "\" is not a depth in millimetres - a number, or empty for a surface, a loft or a "
                + "sweep.");
        else if (Math.Abs(value) >= 0.5) { depth = value / 304.8; hasDepth = true; }
    }

    var texts = (profiles ?? "").Split('|').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
    if (texts.Count == 0)
        problems.Add("No profiles were given - curve ids, commas between, `|` between profiles.");
    foreach (var text in texts)
    {
        var group = curvesOf(text);
        if (group.Count > 0) groups.Add(group);
    }
    if ((path ?? "").Trim().Length > 0) route = curvesOf(path);
    var sweeping = route.Count > 0;

    if (sweeping && hasDepth)
        problems.Add("A depth and a path were both given. A path makes a sweep along it; a depth pushes one profile "
            + "straight - give one.");
    if (sweeping && groups.Count > 1 && route.Count > 1)
        problems.Add("A swept blend of " + groups.Count + " profiles needs a path of ONE curve - Revit's own rule for "
            + "it; this path has " + route.Count + ".");
    if (sweeping && route.Any(c => groups.Any(g => g.Any(p => p.Id == c.Id))))
        problems.Add("A curve is in the path and in a profile - a sweep's path and its profile are different lines.");
    if (!sweeping && groups.Count == 1 && !closes(groups[0]))
        problems.Add("The profile does not close - a surface or an extrusion needs every end to meet another. Draw "
            + "the missing side, or give two profiles for a loft.");
    if (!sweeping && groups.Count > 1 && hasDepth)
        problems.Add("A depth was given with " + groups.Count + " profiles. Two or more profiles make a loft through "
            + "them; a depth goes with one profile.");
    if (!sweeping && groups.Count == 1 && !hasDepth && !solid)
        problems.Add("A void needs a volume - a flat surface cannot cut. Give a depth to extrude it, or a path to "
            + "sweep it.");

    // THE NORMAL OF A PROFILE TO BE PUSHED - from its curves in order, by
    // Newell's method, or a closed circle's or ellipse's own. Refused by name
    // when it encloses nothing or is not flat.
    if (problems.Count == 0 && !sweeping && groups.Count == 1 && hasDepth)
    {
        var only = groups[0];
        List<XYZ> ring = null;
        if (only.Count == 1 && !only[0].GeometryCurve.IsBound)
        {
            var arc = only[0].GeometryCurve as Arc;
            var ellipse = only[0].GeometryCurve as Ellipse;
            normal = arc != null ? arc.Normal : ellipse != null ? ellipse.Normal : null;
            if (normal == null)
                problems.Add("The profile is a closed curve of a kind whose plane is not read here - draw it as two "
                    + "arcs or as lines.");
        }
        else
        {
            ring = ringOf(only);
            if (ring == null) problems.Add("The profile's curves do not run end to end.");
            else
            {
                double nx = 0, ny = 0, nz = 0;
                for (var i = 0; i < ring.Count; i++)
                {
                    var p = ring[i];
                    var q = ring[(i + 1) % ring.Count];
                    nx += (p.Y - q.Y) * (p.Z + q.Z);
                    ny += (p.Z - q.Z) * (p.X + q.X);
                    nz += (p.X - q.X) * (p.Y + q.Y);
                }
                var size = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (size < 1e-9)
                    problems.Add("The profile encloses no area, so it has no direction to be pushed in.");
                else
                {
                    normal = new XYZ(nx / size, ny / size, nz / size);
                    var origin = ring[0];
                    if (ring.Any(p => Math.Abs((p - origin).DotProduct(normal)) > halfMillimetre))
                    {
                        problems.Add("The profile is not flat - its points leave its own plane by more than half a "
                            + "millimetre - so it cannot be pushed straight. Make a surface or a loft instead.");
                        normal = null;
                    }
                }
            }
        }
        // The axis it faces most is positive: up for a profile lying flat.
        if (normal != null)
        {
            var biggest = Math.Abs(normal.X) >= Math.Abs(normal.Y) && Math.Abs(normal.X) >= Math.Abs(normal.Z) ? normal.X
                : Math.Abs(normal.Y) >= Math.Abs(normal.Z) ? normal.Y : normal.Z;
            if (biggest < 0) normal = normal.Negate();
        }
    }

    if (problems.Count > 0) refused = "Nothing was made. " + string.Join(" ", problems.Distinct());
}

// ---------------------------------------------------------------------------
// MAKE THE FORM, THEN READ IT BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    Func<List<CurveElement>, ReferenceArray> referencesOf = group =>
    {
        var array = new ReferenceArray();
        foreach (var c in group) array.Append(c.GeometryCurve.Reference);
        return array;
    };

    var kind = route.Count > 0 ? (groups.Count > 1 ? "swept blend" : "sweep")
        : groups.Count > 1 ? "loft" : hasDepth ? "extrusion" : "surface";
    Form form;
    try
    {
        if (route.Count > 0)
        {
            var all = new ReferenceArrayArray();
            foreach (var g in groups) all.Append(referencesOf(g));
            form = doc.FamilyCreate.NewSweptBlendForm(solid, referencesOf(route), all);
        }
        else if (kind == "loft")
        {
            var all = new ReferenceArrayArray();
            foreach (var g in groups) all.Append(referencesOf(g));
            form = doc.FamilyCreate.NewLoftForm(solid, all);
        }
        else if (kind == "surface")
            form = doc.FamilyCreate.NewFormByCap(solid, referencesOf(groups[0]));
        else
            form = doc.FamilyCreate.NewExtrusionForm(solid, referencesOf(groups[0]), normal.Multiply(depth));
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit would not make the " + kind + ": " + ex.Message
            + (route.Count > 0 ? " Revit's rule for a sweep: the path lies in one plane, and each profile's plane meets "
                + "the path square to it." : "") + " The call failed, and Heron rolls the whole call back.");
    }
    if (form == null)
        throw new InvalidOperationException("Revit made no " + kind + ". The call failed, and Heron rolls the whole call "
            + "back.");

    doc.Regenerate();

    var volume = 0.0;
    var area = 0.0;
    var geometry = form.get_Geometry(new Options());
    if (geometry != null)
        foreach (GeometryObject piece in geometry)
        {
            var body = piece as Solid;
            if (body == null) continue;
            if (body.Volume > 0) volume += body.Volume;
            foreach (Face face in body.Faces) area += face.Area;
        }
    if (area <= 0)
        throw new InvalidOperationException("The " + kind + " was made and holds no geometry to measure. The call failed, "
            + "and Heron rolls the whole call back.");

    var box = form.get_BoundingBox(null);
    formId = form.UniqueId;
    built = (solid ? "Solid" : "Void") + " " + kind + " " + form.UniqueId + " on " + groups.Count + " profile(s) of "
        + groups.Sum(g => g.Count) + " curve(s)"
        + (route.Count > 0 ? " along a path of " + route.Count + " curve(s)" : "")
        + (kind == "extrusion" ? ", " + mm(Math.Abs(depth)) + " mm deep toward (" + Math.Round(normal.X * Math.Sign(depth), 3)
            .ToString(invariant) + ", " + Math.Round(normal.Y * Math.Sign(depth), 3).ToString(invariant) + ", "
            + Math.Round(normal.Z * Math.Sign(depth), 3).ToString(invariant) + ")" : "")
        + (box == null ? "" : ", X " + mm(box.Min.X) + " to " + mm(box.Max.X) + ", Y " + mm(box.Min.Y) + " to "
            + mm(box.Max.Y) + ", Z " + mm(box.Min.Z) + " to " + mm(box.Max.Z) + " mm")
        + (volume > 0 ? "; volume " + Math.Round(volume * 28.316846592, 3).ToString(invariant) + " L"
            : "; surface area " + Math.Round(area * 0.09290304, 4).ToString(invariant) + " m2")
        + ". Read back.";
    findings.Add(built);
    findings.Add("Built on curves that run through the family's points, the form follows the points when the family "
        + "is placed - flex it by moving a point in the Family Editor before trusting it.");
}

if (refused != null) findings.Add(refused);
