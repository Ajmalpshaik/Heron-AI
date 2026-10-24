// NOT STANDALONE. Assumes `doc`, `parameterName`, `sides`, `basePlane` and
// `topPlane` are in scope; leaves `size`, `acrossFlatsMm`, `planesMade`,
// `locked`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A POLYGON'S SLANTED SIDES ARE TIED TO NOTHING when it is drawn as a plain
// extrusion, and the planes Heron otherwise makes run left-right, front-back
// or level - so a hexagon could not follow a size. Measured 2026-10-24 on
// Revit 2020 (session 83568, a rolled-back probe in a scratch family) before
// this was written: a vertical reference plane per side, each pair of
// opposite sides with an EQ dimension about a plane through the origin and a
// second dimension labelled with the parameter, and each side face of the
// extrusion aligned and locked to its plane - Across Flats 18, 46 and 10 read
// back 18, 46 and 10 across the flats, 20.78, 53.12 and 11.55 across the
// corners. That is what this builds.
//
// ONE PAIR OF FLATS FACES FRONT AND BACK. Side k's outward normal is
// 90 + k * 360 / sides degrees in plan, so the first pair is measured along
// Y and its EQ uses the family's own Center (Front/Back); a pair facing
// left and right uses Center (Left/Right). Any other pair gets a centre
// plane made through the origin and PINNED, so the EQs keep the prism on
// the origin. The polygon is centred on the family's origin, X 0, Y 0.
//
// THE PARAMETER MUST ALREADY HOLD THE SIZE. A label moves its planes to the
// parameter's value, and a new length reads 0.
//
// ANY LABEL OR LOCK THAT CANNOT BE MADE THROWS, and the host rolls the whole
// call back - planes and prism included. A prism with five sides locked and
// one loose looks finished and is not.

var findings = new List<string>();
var size = "";
var acrossFlatsMm = 0.0;
var planesMade = new List<string>();
var locked = new List<string>();
var notAFamily = false;
string refused = null;

var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;

Func<XYZ, int> axisOf = n =>
    Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};

// Two Revit values of one kind - a ForgeTypeId is compared by NameEquals,
// an enum and plain equality is exact.
Func<object, object, bool> sameKind = (first, second) =>
{
    if (first == null || second == null) return false;
    var nameEquals = first.GetType().GetMethod("NameEquals", new[] { second.GetType() });
    if (nameEquals != null)
    {
        try { return (bool)nameEquals.Invoke(first, new[] { second }); }
        catch (Exception) { }
    }
    return first.Equals(second);
};

// Is this parameter a length? Read the way its own release allows.
Func<FamilyParameter, bool> isLength = p =>
{
    try
    {
        var definition = p.Definition;
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null)
        {
            var spec = getDataType.Invoke(definition, null);
            var owner = revitAssembly.GetType(dbNamespace + ".SpecTypeId");
            var property = owner == null ? null : owner.GetProperty("Length", flags);
            var length = property == null ? null : property.GetValue(null);
            return sameKind(spec, length);
        }
        var old = definition.GetType().GetProperty("ParameterType");
        var value = old == null ? null : old.GetValue(definition);
        return value != null && value.ToString() == "Length";
    }
    catch (Exception)
    {
        return false;
    }
};

// (element, reference, height in feet, name) for every horizontal plane and level.
var heights = new List<Tuple<Element, Reference, double, string>>();
var allPlanes = new List<ReferencePlane>();
if (doc.IsFamilyDocument)
{
    foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
    {
        allPlanes.Add(rp);
        if (axisOf(rp.Normal) == 2)
            heights.Add(Tuple.Create((Element)rp, rp.GetReference(), rp.GetPlane().Origin.Z, ownName(rp)));
    }
    foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
        heights.Add(Tuple.Create((Element)level, level.GetPlaneReference(), level.Elevation, level.Name));
}

var problems = new List<string>();

Func<string, string, Tuple<Element, Reference, double, string>> oneHeight = (wanted, role) =>
{
    var name = (wanted ?? "").Trim();
    if (name.Length == 0) { problems.Add("No " + role + " was named."); return null; }
    var found = heights.Where(d => d.Item4.Length > 0
                                && string.Equals(d.Item4, name, StringComparison.OrdinalIgnoreCase)).ToList();
    if (found.Count == 0)
    {
        var offered = heights.Where(d => d.Item4.Length > 0).Select(d => d.Item4).Distinct()
            .OrderBy(n => n).Take(15).ToList();
        problems.Add("No level or horizontal reference plane is called \"" + name + "\" (the " + role + "). This "
            + "family has " + (offered.Count == 0 ? "none named." : string.Join(", ", offered) + "."));
        return null;
    }
    if (found.Count > 1) { problems.Add("\"" + name + "\" names " + found.Count + " planes - rename one first."); return null; }
    return found[0];
};

FamilyParameter parameter = null;
double across = 0;
Tuple<Element, Reference, double, string> bottom = null, top = null;
View plan = null, elevation = null;
var wantedParameter = (parameterName ?? "").Trim();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A family's "
        + "geometry is built inside the family - open it first.";
}
else
{
    var fm = doc.FamilyManager;
    if (sides < 4 || sides > 12 || sides % 2 != 0)
        problems.Add("A prism here has an EVEN number of sides, 4 to 12 - 6 for a hexagon - and " + sides
            + " was asked for. Its size is measured across opposite flats, which an odd polygon has not got.");

    if (wantedParameter.Length == 0) problems.Add("No parameter was named for the size across the flats.");
    else
    {
        parameter = fm.get_Parameter(wantedParameter);
        if (parameter == null)
            problems.Add("The family has no parameter called \"" + wantedParameter + "\". Make it a Length with "
                + "ADD_FAMILY_PARAMETERS and give it the size with SET_FAMILY_TYPE_VALUES first.");
        else if (!isLength(parameter))
            problems.Add("\"" + wantedParameter + "\" is not a length, and the size across the flats is one.");
        else if (fm.CurrentType == null)
            problems.Add("The family has no type yet, so \"" + wantedParameter + "\" holds no size - "
                + "SET_FAMILY_TYPE_VALUES makes the first type.");
        else
        {
            var holds = fm.CurrentType.AsDouble(parameter);
            across = holds.HasValue ? holds.Value : 0;
            if (across < 1 / 304.8)
                problems.Add("\"" + wantedParameter + "\" holds " + mm(across) + " mm in type \"" + fm.CurrentType.Name
                    + "\" - no size yet. Labelling now would pull the planes onto each other: give it the size "
                    + "across the flats with SET_FAMILY_TYPE_VALUES first.");
        }
    }

    bottom = oneHeight(basePlane, "base");
    top = oneHeight(topPlane, "top plane");
    if (bottom != null && top != null && top.Item3 - bottom.Item3 < halfMillimetre)
        problems.Add("\"" + top.Item4 + "\" (at " + mm(top.Item3) + " mm) is not above \"" + bottom.Item4
            + "\" (at " + mm(bottom.Item3) + " mm), so there is no height to extrude.");

    if (wantedParameter.Length > 0)
    {
        var prefix = wantedParameter + " Side ";
        var taken = allPlanes.Select(ownName)
            .Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                     || n.StartsWith(wantedParameter + " Centre ", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (taken.Count > 0)
            problems.Add("The family already has " + string.Join(", ", taken.Select(n => "\"" + n + "\"")) + " - a "
                + "prism for \"" + wantedParameter + "\" is built once. Delete those planes and the prism first, "
                + "or name another parameter.");
    }

    var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
        .Where(v => !v.IsTemplate).ToList();
    plan = views.FirstOrDefault(v => v.ViewType == ViewType.FloorPlan);
    elevation = views.Where(v => v.ViewType == ViewType.Elevation)
        .OrderBy(v => Math.Abs(v.ViewDirection.Y) > 0.9999 ? 0 : 1).FirstOrDefault();
    if (plan == null) problems.Add("This family has no floor plan, where the sides are labelled and locked.");
    if (elevation == null) problems.Add("This family has no elevation view, where the top face is locked.");

    if (problems.Count > 0) refused = "Nothing was built. " + string.Join(" ", problems);
}

if (refused == null)
{
    var origin = XYZ.Zero;
    var step = 360.0 / sides;
    Func<double, XYZ> direction = degrees =>
        new XYZ(Math.Cos(degrees * Math.PI / 180), Math.Sin(degrees * Math.PI / 180), 0);
    Func<XYZ, XYZ> sideways = n => new XYZ(-n.Y, n.X, 0);
    var reach = Math.Max(across, 20 / 304.8);

    // ONE PLANE PER SIDE, on the parameter's present size, so nothing moves
    // when the labels go on.
    var sidePlanesMade = new ReferencePlane[sides];
    var normals = new XYZ[sides];
    for (int k = 0; k < sides; k++)
    {
        normals[k] = direction(90 + k * step);
        var at = origin + normals[k].Multiply(across / 2);
        var run = sideways(normals[k]).Multiply(reach);
        try
        {
            sidePlanesMade[k] = doc.FamilyCreate.NewReferencePlane(at + run, at - run, XYZ.BasisZ, plan);
            sidePlanesMade[k].Name = wantedParameter + " Side " + (k + 1);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not make the plane for side " + (k + 1) + ": " + ex.Message
                + " NOTHING from this call was kept.");
        }
        planesMade.Add("\"" + sidePlanesMade[k].Name + "\", facing " + Math.Round(90 + k * step, 1) + " degrees in plan, "
            + mm(across / 2) + " mm from the origin");
    }

    // THE CENTRE OF EACH PAIR: the family's own Center plane where the pair
    // faces that way, otherwise a plane made through the origin and pinned.
    Func<XYZ, ReferencePlane> existingCentre = n => allPlanes.FirstOrDefault(rp =>
    {
        var pl = rp.GetPlane();
        return Math.Abs(Math.Abs(pl.Normal.DotProduct(n)) - 1) < 1e-9
            && Math.Abs((pl.Origin - origin).DotProduct(pl.Normal)) < halfMillimetre / 10;
    });

    // EVERY PLANE IS MADE BEFORE ANY DIMENSION, and Revit regenerates in
    // between: a centre plane made and dimensioned in one breath gave "The
    // references are not geometric references" on Revit 2020 (2026-10-24,
    // Group IZ's first run) - the plane has no geometry until a regeneration.
    var pairs = sides / 2;
    var centresMade = 0;
    var centres = new ReferencePlane[pairs];
    for (int k = 0; k < pairs; k++)
    {
        var n = normals[k];
        var centre = existingCentre(n);
        if (centre == null)
        {
            var run = sideways(n).Multiply(reach * 1.2);
            try
            {
                centre = doc.FamilyCreate.NewReferencePlane(origin + run, origin - run, XYZ.BasisZ, plan);
                centre.Name = wantedParameter + " Centre " + (k + 1);
                centre.Pinned = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit would not make the centre plane for sides " + (k + 1) + " and "
                    + (k + pairs + 1) + ": " + ex.Message + " NOTHING from this call was kept.");
            }
            centresMade++;
            planesMade.Add("\"" + centre.Name + "\", through the origin, pinned - the centre of sides " + (k + 1)
                + " and " + (k + pairs + 1));
        }
        centres[k] = centre;
    }
    doc.Regenerate();

    for (int k = 0; k < pairs; k++)
    {
        var a = sidePlanesMade[k];
        var b = sidePlanesMade[k + pairs];
        var n = normals[k];
        var centre = centres[k];
        var pa = origin + n.Multiply(across / 2);
        var pb = origin - n.Multiply(across / 2);
        var offset = sideways(n).Multiply(across * 0.8 + 10 / 304.8);
        try
        {
            var equal = new ReferenceArray();
            equal.Append(a.GetReference());
            equal.Append(centre.GetReference());
            equal.Append(b.GetReference());
            var eq = doc.FamilyCreate.NewDimension(plan, Line.CreateBound(pa + offset, pb + offset), equal);
            eq.AreSegmentsEqual = true;

            var ends = new ReferenceArray();
            ends.Append(a.GetReference());
            ends.Append(b.GetReference());
            var outer = offset.Multiply(1.4);
            var label = doc.FamilyCreate.NewDimension(plan, Line.CreateBound(pa + outer, pb + outer), ends);
            label.FamilyLabel = parameter;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not label sides " + (k + 1) + " and " + (k + pairs + 1)
                + " with \"" + wantedParameter + "\" about \"" + ownName(centre) + "\": " + ex.Message
                + " NOTHING from this call was kept.");
        }
    }
    doc.Regenerate();

    // THE PRISM, its corners where the sides meet, sketched ON the base so it
    // follows it.
    var circumradius = (across / 2) / Math.Cos(Math.PI / sides);
    Extrusion prism;
    double zBase;
    try
    {
        var sketch = SketchPlane.Create(doc, bottom.Item1.Id);
        var surface = sketch.GetPlane();
        zBase = surface.Origin.Z;
        var up = surface.Normal.Z;
        if (Math.Abs(Math.Abs(up) - 1.0) > 1e-6)
            throw new InvalidOperationException("the sketch plane on \"" + bottom.Item4 + "\" is not horizontal");
        var loop = new CurveArray();
        for (int k = 0; k < sides; k++)
        {
            var c1 = direction(90 + k * step - step / 2).Multiply(circumradius);
            var c2 = direction(90 + k * step + step / 2).Multiply(circumradius);
            loop.Append(Line.CreateBound(new XYZ(c1.X, c1.Y, zBase), new XYZ(c2.X, c2.Y, zBase)));
        }
        var profile = new CurveArrArray();
        profile.Append(loop);
        prism = doc.FamilyCreate.NewExtrusion(true, profile, sketch, (top.Item3 - zBase) / up);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the " + sides + "-sided prism on \"" + bottom.Item4 + "\": "
            + ex.Message + " NOTHING from this call was kept.");
    }
    doc.Regenerate();

    var faces = new List<PlanarFace>();
    var geometry = prism.get_Geometry(new Options { ComputeReferences = true });
    if (geometry != null)
        foreach (GeometryObject piece in geometry)
        {
            var solid = piece as Solid;
            if (solid == null) continue;
            foreach (Face face in solid.Faces)
            {
                var flat = face as PlanarFace;
                if (flat != null && flat.Reference != null) faces.Add(flat);
            }
        }

    Action<Reference, Plane, string, View, string> lockTo = (planeReference, plane, planeName, inView, which) =>
    {
        var match = faces.Where(f => f.FaceNormal.DotProduct(plane.Normal) > 0.9999
                                  || f.FaceNormal.DotProduct(plane.Normal) < -0.9999)
                         .Where(f => Math.Abs((f.Origin - plane.Origin).DotProduct(plane.Normal)) < halfMillimetre)
                         .ToList();
        if (match.Count != 1)
            throw new InvalidOperationException("The prism's " + which + " face could not be found lying on \""
                + planeName + "\" (" + match.Count + " candidate faces), so it could not be locked. NOTHING from "
                + "this call was kept.");
        try
        {
            var alignment = doc.FamilyCreate.NewAlignment(inView, planeReference, match[0].Reference);
            if (!alignment.IsLocked) alignment.IsLocked = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not lock the prism's " + which + " face to \""
                + planeName + "\": " + ex.Message + " NOTHING from this call was kept.");
        }
        locked.Add(which + " face to \"" + planeName + "\"");
    };

    for (int k = 0; k < sides; k++)
        lockTo(sidePlanesMade[k].GetReference(), sidePlanesMade[k].GetPlane(), sidePlanesMade[k].Name, plan,
            "side " + (k + 1));
    lockTo(top.Item2, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, top.Item3)), top.Item4, elevation, "top");
    doc.Regenerate();

    // READ BACK the prism as Revit now holds it.
    var bounds = prism.get_BoundingBox(null);
    if (bounds == null)
        throw new InvalidOperationException("The prism was built and has no extent to read back. NOTHING from "
            + "this call was kept.");
    var frontToBack = bounds.Max.Y - bounds.Min.Y;
    var leftToRight = bounds.Max.X - bounds.Min.X;
    var height = bounds.Max.Z - bounds.Min.Z;
    var wideWay = (sides % 4 == 0) ? across : 2 * circumradius;
    if (Math.Abs(frontToBack - across) > halfMillimetre || Math.Abs(leftToRight - wideWay) > halfMillimetre
        || Math.Abs(height - (top.Item3 - zBase)) > halfMillimetre)
        throw new InvalidOperationException("The prism reads " + mm(leftToRight) + " x " + mm(frontToBack) + " x "
            + mm(height) + " mm and should read " + mm(wideWay) + " x " + mm(across) + " x " + mm(top.Item3 - zBase)
            + " mm - it did not land on its planes. NOTHING from this call was kept.");

    acrossFlatsMm = Math.Round(frontToBack * 304.8, 2);
    size = sides + " sides, " + mm(frontToBack) + " mm across the flats and " + mm(2 * circumradius) + " mm across "
        + "the corners, " + mm(height) + " mm high, centred at X " + mm((bounds.Max.X + bounds.Min.X) / 2) + ", Y "
        + mm((bounds.Max.Y + bounds.Min.Y) / 2) + ", from Z " + mm(bounds.Min.Z) + " to " + mm(bounds.Max.Z);

    findings.Add("Built a solid prism of " + size + ", read back from the prism. \"" + wantedParameter + "\" labels "
        + pairs + " pair(s) of opposite sides, each kept even about its centre; " + locked.Count + " faces locked. "
        + "Its base is sketched on \"" + bottom.Item4 + "\" and moves with it.");
    if (centresMade > 0)
        findings.Add(centresMade + " centre plane(s) were made through the origin and PINNED, so the EQs hold the "
            + "prism on the origin when the size changes.");
    findings.Add("Locked is not yet PROVED to follow the planes. FLEX_FAMILY changes \"" + wantedParameter
        + "\", reads the prism back, and puts it back.");
}

if (refused != null) findings.Add(refused);
