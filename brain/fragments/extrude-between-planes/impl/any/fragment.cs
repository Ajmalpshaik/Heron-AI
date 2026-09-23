// NOT STANDALONE. Assumes `doc`, `sidePlanes`, `basePlane` and `topPlane` are
// in scope; leaves `size`, `locked`, `notAFamily`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A BOX AT THE SAME COORDINATES AS THE PLANES IS NOT TIED TO THEM. Move the
// planes and it stays put - reported from an earlier family build. So each of
// the four sides and the top is ALIGNED AND LOCKED to its plane, and the base
// is sketched on the base plane itself, from its element id, so it follows it.
// A sketch plane made from a copy of the plane's geometry is hosted on nothing
// and cannot be re-hosted - reported from the same builds.
//
// THE PROFILE IS FLAT, ON THE BASE. Face references come back from a sketch on
// a horizontal plane; a sketch stood on a vertical plane gave one face of six
// (reported). So the box always grows upward from its base, and a neck stands
// on the body by taking the body's top plane as its base.
//
// ANY LOCK THAT CANNOT BE MADE THROWS, and the host rolls the whole call back -
// the box included. A box with four faces locked and one loose looks finished
// and is not, which is worse than no box.
//
// THE SIZE IS READ BACK from the box's own bounding box after the locks, and
// must match the planes, or nothing is kept.

var findings = new List<string>();
var size = "";
var locked = new List<string>();
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;

Func<XYZ, int> axisOf = n =>
    Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;
Func<XYZ, int, double> along = (point, index) => index == 0 ? point.X : index == 1 ? point.Y : point.Z;

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};

// (element, reference, axis, position in feet, name) for every plane and level.
var datums = new List<Tuple<Element, Reference, int, double, string>>();
if (doc.IsFamilyDocument)
{
    foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
    {
        var at = axisOf(rp.Normal);
        if (at < 0) continue;
        datums.Add(Tuple.Create((Element)rp, rp.GetReference(), at, along(rp.GetPlane().Origin, at), ownName(rp)));
    }
    foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
        datums.Add(Tuple.Create((Element)level, new Reference(level), 2, level.Elevation, level.Name));
}

var problems = new List<string>();

Func<string, string, Tuple<Element, Reference, int, double, string>> one = (wanted, role) =>
{
    var name = (wanted ?? "").Trim();
    if (name.Length == 0) { problems.Add("No " + role + " was named."); return null; }
    var found = datums.Where(d => d.Item5.Length > 0
                               && string.Equals(d.Item5, name, StringComparison.OrdinalIgnoreCase)).ToList();
    if (found.Count == 0)
    {
        var offered = datums.Where(d => d.Item5.Length > 0).Select(d => d.Item5).Distinct()
            .OrderBy(n => n).Take(15).ToList();
        problems.Add("No reference plane or level is called \"" + name + "\" (the " + role + "). This family "
            + "has " + (offered.Count == 0 ? "no named ones." : string.Join(", ", offered) + "."));
        return null;
    }
    if (found.Count > 1) { problems.Add("\"" + name + "\" names " + found.Count + " planes - rename one first."); return null; }
    return found[0];
};

var sides = new List<Tuple<Element, Reference, int, double, string>>();
Tuple<Element, Reference, int, double, string> bottom = null, top = null;
View plan = null, elevation = null;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A family's "
        + "geometry is built inside the family - open it first.";
}
else
{
    var names = new List<string>();
    if (sidePlanes != null)
        foreach (var raw in sidePlanes)
        {
            var trimmed = raw == null ? "" : raw.Trim();
            if (trimmed.Length > 0) names.Add(trimmed);
        }

    if (names.Count != 4 || names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 4)
    {
        problems.Add("Four DIFFERENT side planes are needed - two controlling left-right and two "
            + "front-back - and " + names.Count + " were given: " + string.Join(", ", names) + ".");
    }
    else
    {
        foreach (var name in names)
        {
            var found = one(name, "side plane");
            if (found == null) continue;
            if (!(found.Item1 is ReferencePlane) || found.Item3 == 2)
                problems.Add("\"" + found.Item5 + "\" is horizontal - a side plane controls left-right or "
                    + "front-back.");
            else sides.Add(found);
        }

        if (problems.Count == 0)
        {
            var xs = sides.Where(s => s.Item3 == 0).ToList();
            var ys = sides.Where(s => s.Item3 == 1).ToList();
            if (xs.Count != 2 || ys.Count != 2)
                problems.Add("Of the four side planes, " + xs.Count + " control left-right and " + ys.Count
                    + " front-back. A box needs two of each.");
            else if (Math.Abs(xs[0].Item4 - xs[1].Item4) < halfMillimetre)
                problems.Add("\"" + xs[0].Item5 + "\" and \"" + xs[1].Item5 + "\" are in the same place.");
            else if (Math.Abs(ys[0].Item4 - ys[1].Item4) < halfMillimetre)
                problems.Add("\"" + ys[0].Item5 + "\" and \"" + ys[1].Item5 + "\" are in the same place.");
        }
    }

    bottom = one(basePlane, "base");
    top = one(topPlane, "top plane");

    if (bottom != null && bottom.Item3 != 2)
        problems.Add("The base \"" + bottom.Item5 + "\" is not horizontal - a box stands on a level or a "
            + "horizontal plane.");
    if (top != null && top.Item3 != 2)
        problems.Add("The top \"" + top.Item5 + "\" is not horizontal.");
    if (bottom != null && top != null && bottom.Item3 == 2 && top.Item3 == 2
        && top.Item4 - bottom.Item4 < halfMillimetre)
        problems.Add("\"" + top.Item5 + "\" (at " + mm(top.Item4) + " mm) is not above \"" + bottom.Item5
            + "\" (at " + mm(bottom.Item4) + " mm), so there is no height to extrude.");

    var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
        .Where(v => !v.IsTemplate).ToList();
    plan = views.FirstOrDefault(v => v.ViewType == ViewType.FloorPlan);
    elevation = views.Where(v => v.ViewType == ViewType.Elevation)
        .OrderBy(v => Math.Abs(v.ViewDirection.Y) > 0.9999 ? 0 : 1).FirstOrDefault();
    if (plan == null) problems.Add("This family has no floor plan, where the side faces are locked.");
    if (elevation == null) problems.Add("This family has no elevation view, where the top face is locked.");

    if (problems.Count > 0) refused = "Nothing was built. " + string.Join(" ", problems);
}

if (refused == null)
{
    var xs = sides.Where(s => s.Item3 == 0).OrderBy(s => s.Item4).ToList();
    var ys = sides.Where(s => s.Item3 == 1).OrderBy(s => s.Item4).ToList();
    var x1 = xs[0].Item4; var x2 = xs[1].Item4;
    var y1 = ys[0].Item4; var y2 = ys[1].Item4;

    Extrusion box;
    double zBase;

    try
    {
        var sketch = SketchPlane.Create(doc, bottom.Item1.Id);
        var surface = sketch.GetPlane();
        zBase = surface.Origin.Z;
        var up = surface.Normal.Z;
        if (Math.Abs(Math.Abs(up) - 1.0) > 1e-6)
            throw new InvalidOperationException("the sketch plane on \"" + bottom.Item5 + "\" is not horizontal");

        var loop = new CurveArray();
        loop.Append(Line.CreateBound(new XYZ(x1, y1, zBase), new XYZ(x2, y1, zBase)));
        loop.Append(Line.CreateBound(new XYZ(x2, y1, zBase), new XYZ(x2, y2, zBase)));
        loop.Append(Line.CreateBound(new XYZ(x2, y2, zBase), new XYZ(x1, y2, zBase)));
        loop.Append(Line.CreateBound(new XYZ(x1, y2, zBase), new XYZ(x1, y1, zBase)));
        var profile = new CurveArrArray();
        profile.Append(loop);

        // Measured along the sketch plane's own normal, which may point down.
        box = doc.FamilyCreate.NewExtrusion(true, profile, sketch, (top.Item4 - zBase) / up);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the extrusion between \"" + xs[0].Item5 + "\", \""
            + xs[1].Item5 + "\", \"" + ys[0].Item5 + "\" and \"" + ys[1].Item5 + "\": " + ex.Message
            + " NOTHING from this call was kept.");
    }

    doc.Regenerate();

    // THE FACES, with references, off the box itself.
    var faces = new List<PlanarFace>();
    var geometry = box.get_Geometry(new Options { ComputeReferences = true });
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

    Action<Tuple<Element, Reference, int, double, string>, View, string> lockTo = (datum, inView, which) =>
    {
        var match = faces.Where(f => axisOf(f.FaceNormal) == datum.Item3
                                  && Math.Abs(along(f.Origin, datum.Item3) - datum.Item4) < halfMillimetre)
                         .ToList();
        if (match.Count != 1)
            throw new InvalidOperationException("The box's " + which + " face could not be found lying on \""
                + datum.Item5 + "\" (" + match.Count + " candidate faces), so it could not be locked. NOTHING "
                + "from this call was kept.");
        try
        {
            var alignment = doc.FamilyCreate.NewAlignment(inView, datum.Item2, match[0].Reference);
            if (!alignment.IsLocked) alignment.IsLocked = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not lock the box's " + which + " face to \""
                + datum.Item5 + "\": " + ex.Message + " NOTHING from this call was kept.");
        }
        locked.Add(which + " face to \"" + datum.Item5 + "\"");
    };

    lockTo(xs[0], plan, "left");
    lockTo(xs[1], plan, "right");
    lockTo(ys[0], plan, "front");
    lockTo(ys[1], plan, "back");
    lockTo(top, elevation, "top");

    doc.Regenerate();

    // READ BACK the box as Revit now holds it.
    var bounds = box.get_BoundingBox(null);
    if (bounds == null)
        throw new InvalidOperationException("The box was built and has no extent to read back. NOTHING from "
            + "this call was kept.");

    var width = bounds.Max.X - bounds.Min.X;
    var depth = bounds.Max.Y - bounds.Min.Y;
    var height = bounds.Max.Z - bounds.Min.Z;

    if (Math.Abs(width - (x2 - x1)) > halfMillimetre || Math.Abs(depth - (y2 - y1)) > halfMillimetre
        || Math.Abs(height - (top.Item4 - zBase)) > halfMillimetre)
        throw new InvalidOperationException("The box reads " + mm(width) + " x " + mm(depth) + " x "
            + mm(height) + " mm and the planes are " + mm(x2 - x1) + " x " + mm(y2 - y1) + " x "
            + mm(top.Item4 - zBase) + " mm apart - it did not land between them. NOTHING from this call "
            + "was kept.");

    size = mm(width) + " x " + mm(depth) + " x " + mm(height) + " mm (width x depth x height), centred at X "
        + mm((bounds.Max.X + bounds.Min.X) / 2) + ", Y " + mm((bounds.Max.Y + bounds.Min.Y) / 2)
        + ", from Z " + mm(bounds.Min.Z) + " to " + mm(bounds.Max.Z);

    findings.Add("Built a solid extrusion " + size + ", read back from the box. Locked: "
        + string.Join("; ", locked) + ". Its base is sketched on \"" + bottom.Item5 + "\" and moves with it.");

    findings.Add("Locked to planes is not yet PROVED to follow them. FLEX_FAMILY changes the sizes, reads "
        + "the box back, and puts them back.");
}

if (refused != null) findings.Add(refused);
