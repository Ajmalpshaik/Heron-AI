// NOT STANDALONE. Assumes `doc`, `uidoc`, `form` and `planes` are in scope;
// leaves `locked`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// ALIGN AND LOCK a form's flat faces to named reference planes and levels - the
// padlock Revit shows after Align - so the form follows the planes when a
// labelled dimension moves them. Any form: an extrusion of any shape, a blend,
// a revolve's flat ends, a sweep's flat ends.
//
// A FACE IS LOCKED ONLY WHERE IT ALREADY LIES. Revit's own remarks on the
// alignment call: the two references "must be already geometrically aligned
// (this function will not force them to become aligned)". So for each plane
// named, the form's flat faces lying ON it are found - parallel to it and within
// half a millimetre - and every one of them is locked. A plane with no face on
// it refuses the whole call, naming where the form's faces facing that way are.
//
// A FACE FACING LEFT-RIGHT OR FRONT-BACK IS LOCKED IN A FLOOR PLAN, one facing
// up or down in an elevation - the views in which each is seen edge on, the
// way it is done by hand. The face references come from the form's own
// geometry, asked for with references; a face Revit gives no reference for is
// named rather than skipped.
//
// ALL OR NOTHING, READ BACK. After the locks Revit regenerates, and the form's
// extent must not have moved - a lock that dragged the form somewhere is a lock
// on the wrong plane. Any refusal from Revit fails the call and nothing is kept.

var findings = new List<string>();
var locked = new List<string>();
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;

Func<XYZ, int> axisOf = n =>
    Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;
Func<XYZ, int, double> along = (point, index) => index == 0 ? point.X : index == 1 ? point.Y : point.Z;
var axisWords = new[] { "left-right", "front-back", "up-down" };
var axisLetters = new[] { "X", "Y", "Z" };

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};

// (element, reference, axis it faces, position in feet, name) for every named
// reference plane and every level.
var datums = new List<Tuple<Element, Reference, int, double, string>>();
if (doc.IsFamilyDocument)
{
    foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
    {
        var name = ownName(rp);
        if (name.Length == 0) continue;
        var at = axisOf(rp.Normal);
        datums.Add(Tuple.Create((Element)rp, rp.GetReference(), at, at < 0 ? 0.0 : along(rp.GetPlane().Origin, at), name));
    }
    foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
        datums.Add(Tuple.Create((Element)level, level.GetPlaneReference(), 2, level.Elevation, level.Name));
}

GenericForm target = null;
var wantedPlanes = new List<Tuple<Element, Reference, int, double, string>>();
var problems = new List<string>();
View plan = null, elevation = null;

// The form's flat faces that Revit gives a reference for, read fresh each time.
Func<List<PlanarFace>> flatFaces = () =>
{
    var faces = new List<PlanarFace>();
    var geometry = target.get_Geometry(new Options { ComputeReferences = true });
    if (geometry == null) return faces;
    foreach (GeometryObject piece in geometry)
    {
        var body = piece as Solid;
        if (body == null) continue;
        foreach (Face face in body.Faces)
        {
            var flat = face as PlanarFace;
            if (flat != null) faces.Add(flat);
        }
    }
    return faces;
};

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A form is locked to "
        + "its planes inside the family - open it first.";
}
else
{
    var said = (form ?? "").Trim();
    Element chosen = null;
    if (string.Equals(said, "selected", StringComparison.OrdinalIgnoreCase)
        || string.Equals(said, "selection", StringComparison.OrdinalIgnoreCase))
    {
        if (uidoc == null || uidoc.Document == null || !uidoc.Document.Equals(doc))
            problems.Add("\"selected\" means what is selected in this family's window, and this family is not the "
                + "window in front. Bring it to the front, or name the form by its id.");
        else
        {
            var ids = uidoc.Selection.GetElementIds().ToList();
            if (ids.Count != 1)
                problems.Add(ids.Count + " elements are selected. Select exactly one form.");
            else chosen = doc.GetElement(ids[0]);
        }
    }
    else if (said.Length == 0) problems.Add("No form was named.");
    else
    {
        chosen = doc.GetElement(said);
        if (chosen == null) problems.Add("No element in this family has the id \"" + said + "\".");
    }
    if (chosen != null)
    {
        target = chosen as GenericForm;
        if (target == null)
            problems.Add("\"" + (chosen.Name ?? said) + "\" is not a form - an extrusion, revolve, blend, sweep or "
                + "swept blend.");
    }

    var names = (planes ?? new List<string>()).Select(n => (n ?? "").Trim()).Where(n => n.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    if (names.Count == 0) problems.Add("No plane was named to lock the form to.");
    foreach (var name in names)
    {
        var found = datums.Where(d => string.Equals(d.Item5, name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (found.Count == 0)
            problems.Add("No reference plane or level is called \"" + name + "\". This family has "
                + (datums.Count == 0 ? "no named ones."
                    : string.Join(", ", datums.Select(d => d.Item5).Distinct().OrderBy(n => n).Take(20)) + "."));
        else if (found.Count > 1) problems.Add("\"" + name + "\" names " + found.Count + " planes - rename one first.");
        else if (found[0].Item3 < 0)
            problems.Add("\"" + name + "\" is at an angle; only a plane facing left-right, front-back or up is "
                + "locked here.");
        else wantedPlanes.Add(found[0]);
    }

    var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().Where(v => !v.IsTemplate).ToList();
    plan = views.FirstOrDefault(v => v.ViewType == ViewType.FloorPlan);
    elevation = views.Where(v => v.ViewType == ViewType.Elevation)
        .OrderBy(v => Math.Abs(v.ViewDirection.Y) > 0.9999 ? 0 : 1).FirstOrDefault();
    if (wantedPlanes.Any(p => p.Item3 != 2) && plan == null)
        problems.Add("This family has no floor plan, where a face facing left-right or front-back is locked.");
    if (wantedPlanes.Any(p => p.Item3 == 2) && elevation == null)
        problems.Add("This family has no elevation view, where a face facing up or down is locked.");

    // EVERY PLANE MUST HAVE A FACE ON IT, before anything is locked.
    if (problems.Count == 0)
    {
        var faces = flatFaces();
        foreach (var p in wantedPlanes)
        {
            var on = faces.Where(f => axisOf(f.FaceNormal) == p.Item3
                                   && Math.Abs(along(f.Origin, p.Item3) - p.Item4) < halfMillimetre).ToList();
            if (on.Count == 0)
            {
                var facing = faces.Where(f => axisOf(f.FaceNormal) == p.Item3)
                    .Select(f => mm(along(f.Origin, p.Item3))).Distinct().ToList();
                problems.Add("No flat face of the form lies on \"" + p.Item5 + "\" (" + axisLetters[p.Item3] + " "
                    + mm(p.Item4) + " mm). Its faces facing " + axisWords[p.Item3] + " lie at "
                    + (facing.Count == 0 ? "no position - it has none" : string.Join(", ", facing) + " mm")
                    + ". A face is locked only where it already lies.");
            }
            else if (on.Any(f => f.Reference == null))
                problems.Add("Revit gives no reference for " + on.Count(f => f.Reference == null) + " of the face(s) "
                    + "on \"" + p.Item5 + "\", so they cannot be locked - reported from an earlier family build for a "
                    + "form sketched on a vertical plane.");
        }
    }

    if (problems.Count > 0) refused = "Nothing was locked. " + string.Join(" ", problems);
}

if (refused == null)
{
    var start = target.get_BoundingBox(null);

    foreach (var p in wantedPlanes)
    {
        var view = p.Item3 == 2 ? elevation : plan;
        // Fresh faces for every plane: a lock regenerates the form, and a face
        // read before it is a face of the form as it was.
        var on = flatFaces().Where(f => axisOf(f.FaceNormal) == p.Item3
                                     && Math.Abs(along(f.Origin, p.Item3) - p.Item4) < halfMillimetre).ToList();
        foreach (var face in on)
        {
            try
            {
                var alignment = doc.FamilyCreate.NewAlignment(view, p.Item2, face.Reference);
                if (!alignment.IsLocked) alignment.IsLocked = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit would not lock the form's face at " + axisLetters[p.Item3]
                    + " " + mm(along(face.Origin, p.Item3)) + " mm to \"" + p.Item5 + "\": " + ex.Message
                    + " NOTHING from this call was kept.");
            }
            locked.Add("the face at " + axisLetters[p.Item3] + " " + mm(along(face.Origin, p.Item3)) + " mm to \""
                + p.Item5 + "\" (in " + view.Name + ")");
        }
        doc.Regenerate();
    }

    var end = target.get_BoundingBox(null);
    if (start == null || end == null)
        throw new InvalidOperationException("The form has no extent to read back. NOTHING from this call was kept.");
    for (var i = 0; i < 3; i++)
        if (Math.Abs(along(start.Min, i) - along(end.Min, i)) > halfMillimetre
            || Math.Abs(along(start.Max, i) - along(end.Max, i)) > halfMillimetre)
            throw new InvalidOperationException("Locking moved the form: " + axisLetters[i] + " " + mm(along(start.Min, i))
                + " to " + mm(along(start.Max, i)) + " mm before, " + mm(along(end.Min, i)) + " to "
                + mm(along(end.Max, i)) + " mm after. A lock never moves what it locks. NOTHING from this call was kept.");

    findings.Add("Locked " + locked.Count + " face(s): " + string.Join("; ", locked) + ".");
    findings.Add("Locked is not yet PROVED to follow the planes. FLEX_FAMILY changes the sizes, reads the form "
        + "back, and puts them back.");
}

if (refused != null) findings.Add(refused);
