// NOT STANDALONE. Assumes `doc`, `profiles`, `depthMm` and `solid` are in
// scope; leaves `formId`, `built`, `notAFamily`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A FORM ON LINES - in an adaptive component, a pattern-based panel or a mass,
// the Family Editor's Create Form. Built on curves that run through the
// family's points, the form follows the points: the flat glass of a panel that
// fits every cell, an adaptive fin, a lofted canopy. The classic forms -
// extrusion, blend, revolve, sweep - are the other Family Editor's, and are not
// made in these families; this is the one these families have.
//
// WHAT IS MADE FOLLOWS FROM WHAT IS GIVEN, so no kind is asked:
//   ONE closed profile and no depth  -> a flat SURFACE filling it (a cap);
//   ONE closed profile and a depth   -> an EXTRUSION that far along its normal;
//   TWO OR MORE profiles             -> a LOFT through them in the order given.
// A profile is curve ids, commas between - the ids DRAW_FAMILY_POINT_CURVES
// gives back - and profiles have `|` between them.
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
// Each profile: its curves and its references.
var groups = new List<List<CurveElement>>();
double depth = 0;
var hasDepth = false;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "A form on lines is made inside an adaptive or pattern-based family - open it first.";
}
else
{
    var said = (depthMm ?? "").Trim();
    if (said.Length > 0)
    {
        double value;
        if (!double.TryParse(said, System.Globalization.NumberStyles.Float, invariant, out value)
            || double.IsNaN(value) || double.IsInfinity(value))
            problems.Add("\"" + said + "\" is not a depth in millimetres - a number, or empty for a surface or a loft.");
        else if (Math.Abs(value) >= 0.5) { depth = value / 304.8; hasDepth = true; }
    }

    var texts = (profiles ?? "").Split('|').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
    if (texts.Count == 0)
        problems.Add("No profiles were given - curve ids, commas between, `|` between profiles.");
    foreach (var text in texts)
    {
        var group = new List<CurveElement>();
        foreach (var token in text.Split(new[] { ',', ' ', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var curve = doc.GetElement(token) as CurveElement;
            if (curve == null || curve.GeometryCurve == null || curve.GeometryCurve.Reference == null)
            {
                problems.Add("\"" + token + "\" is not the id of a line in this family that a form can be built on.");
                continue;
            }
            group.Add(curve);
        }
        if (group.Count > 0) groups.Add(group);
    }

    // A PROFILE THAT IS TO BE FILLED OR PUSHED MUST CLOSE: every end meets
    // another end.
    Func<List<CurveElement>, bool> closes = group =>
    {
        var ends = new List<XYZ>();
        foreach (var c in group)
        {
            ends.Add(c.GeometryCurve.GetEndPoint(0));
            ends.Add(c.GeometryCurve.GetEndPoint(1));
        }
        if (group.Count == 1) return ends[0].DistanceTo(ends[1]) < halfMillimetre || !group[0].GeometryCurve.IsBound;
        return ends.All(e => ends.Count(o => o.DistanceTo(e) < halfMillimetre) >= 2);
    };
    if (groups.Count == 1 && !closes(groups[0]))
        problems.Add("The profile does not close - a surface or an extrusion needs every end to meet another. Draw "
            + "the missing side, or give two profiles for a loft.");
    if (groups.Count > 1 && hasDepth)
        problems.Add("A depth was given with " + groups.Count + " profiles. Two or more profiles make a loft through "
            + "them; a depth goes with one profile.");
    if (groups.Count == 1 && !hasDepth && !solid)
        problems.Add("A void needs a volume - a flat surface cannot cut. Give a depth to extrude it.");

    if (problems.Count > 0) refused = "Nothing was made. " + string.Join(" ", problems);
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

    var kind = groups.Count > 1 ? "loft" : hasDepth ? "extrusion" : "surface";
    Form form;
    try
    {
        if (kind == "loft")
        {
            var all = new ReferenceArrayArray();
            foreach (var g in groups) all.Append(referencesOf(g));
            form = doc.FamilyCreate.NewLoftForm(solid, all);
        }
        else if (kind == "surface")
            form = doc.FamilyCreate.NewFormByCap(solid, referencesOf(groups[0]));
        else
        {
            // THE NORMAL OF THE PROFILE'S PLANE, by Newell's method over its
            // tessellated points; a profile that is not flat cannot be pushed
            // straight and is refused by name.
            var ring = new List<XYZ>();
            foreach (var c in groups[0]) ring.AddRange(c.GeometryCurve.Tessellate());
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
                throw new InvalidOperationException("The profile encloses no area, so it has no direction to be pushed "
                    + "in. The call failed, and Heron rolls the whole call back.");
            var normal = new XYZ(nx / size, ny / size, nz / size);
            var origin = ring[0];
            if (ring.Any(p => Math.Abs((p - origin).DotProduct(normal)) > halfMillimetre))
                throw new InvalidOperationException("The profile is not flat - its points leave its own plane by more than "
                    + "half a millimetre - so it cannot be pushed straight. Make a surface or a loft instead. The call "
                    + "failed, and Heron rolls the whole call back.");
            form = doc.FamilyCreate.NewExtrusionForm(solid, referencesOf(groups[0]), normal.Multiply(depth));
        }
    }
    catch (InvalidOperationException) { throw; }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit would not make the " + kind + ": " + ex.Message + " The call failed, "
            + "and Heron rolls the whole call back.");
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
        + groups.Sum(g => g.Count) + " curve(s)" + (kind == "extrusion" ? ", " + mm(Math.Abs(depth)) + " mm deep" : "")
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
