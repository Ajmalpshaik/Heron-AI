// NOT STANDALONE. Assumes `doc`, `elements`, `wallType`, `faces` and
// `locationLine` are in scope; leaves `created`, `createdIds`, `skipped`,
// `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// WALL BY FACE on conceptual masses - Massing & Site > Wall by Face - one
// wall on each face chosen, of the wall type named.
//
// A curtain system type is refused by name - CREATE_CURTAIN_SYSTEM makes
// Curtain System by Face - and so is a floor or roof type: Floor by Face and
// Roof by Face have no API on any release, and REPORT_MASSES' outlines with
// CREATE_FLOOR are the route for a slab.
//
//   faces          sides (empty) - every face that does not face straight
//                  up or down; vertical - upright faces only, to within one
//                  degree
//   locationLine   finish face exterior (empty), finish face interior, wall
//                  centerline, core centerline, core exterior, core
//                  interior. Empty puts the wall's OUTSIDE face on the mass
//                  face - Heron's default, said in the answer, not Revit's.
//
// THE FACE REFERENCES COME FROM THE FAMILY'S OWN GEOMETRY -
// GeometryInstance.GetSymbolGeometry() with no transform, the only geometry
// RevitAPI.xml's remarks call "suitable for creating new Revit elements
// referencing the original element". The instance's transform turns the
// normals into the model's directions and touches nothing else. Solids the
// mass returns directly, not inside an instance, are used as they come.
//
// A MASS ALREADY CARRYING WALLS BY FACE is skipped by name rather than given
// a second skin on top.
//
// READ BACK: every wall made is read again - its type and its area; one
// that does not read back fails the call and nothing is kept.

var created = 0;
var createdIds = new List<ElementId>();
var skipped = 0;
var lines = new List<string>();
var findings = new List<string>();
var problems = new List<string>();

// The type, by "Family : Type" or by its type name.
var wantedName = (wallType ?? "").Trim();
Func<ElementType, bool> named = t =>
    string.Equals(t.Name, wantedName, StringComparison.OrdinalIgnoreCase)
    || string.Equals(t.FamilyName + " : " + t.Name, wantedName, StringComparison.OrdinalIgnoreCase)
    || string.Equals(t.FamilyName + ": " + t.Name, wantedName, StringComparison.OrdinalIgnoreCase);
var wallTypes = new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().Where(t => named(t)).ToList();
var otherTypes = new FilteredElementCollector(doc).WhereElementIsElementType().Cast<Element>()
    .Where(t => (t is FloorType || t is RoofType || t is CurtainSystemType) && named((ElementType)t)).ToList();
WallType type = null;
if (wantedName == "") problems.Add("No wall type was given.");
else if (wallTypes.Count == 0 && otherTypes.Any(t => t is CurtainSystemType))
    problems.Add("\"" + wantedName + "\" is a curtain system type - CREATE_CURTAIN_SYSTEM makes Curtain System by Face.");
else if (wallTypes.Count == 0 && otherTypes.Count > 0)
    problems.Add("\"" + wantedName + "\" is a floor or roof type. Floor by Face and Roof by Face have no API on any release - for a slab on a mass floor, REPORT_MASSES with withOutlines gives the outline CREATE_FLOOR takes; Roof by Face is done by hand.");
else if (wallTypes.Count == 0)
    problems.Add("No wall type is named \"" + wantedName + "\". Wall types: "
        + string.Join(", ", new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<ElementType>().Select(t => t.Name).Take(15)) + ".");
else if (wallTypes.Count > 1)
    problems.Add("\"" + wantedName + "\" names " + wallTypes.Count + " wall types - write it as \"Family : Type\".");
else
{
    type = wallTypes[0];
    if (!FaceWall.IsWallTypeValidForFaceWall(doc, type.Id))
        problems.Add("\"" + type.Name + "\" (" + type.Kind + ") cannot be a wall by face - Revit's own check refuses it. A curtain wall on a mass is a curtain system - CREATE_CURTAIN_SYSTEM.");
}

// Which faces.
var kind = (faces ?? "").Trim().ToLowerInvariant();
if (kind == "") kind = "sides";
if (kind != "sides" && kind != "vertical")
    problems.Add("faces is sides or vertical - not \"" + faces + "\".");
Func<XYZ, bool> faceWanted = n =>
    (kind == "sides" && Math.Abs(n.Z) < 0.999) || (kind == "vertical" && Math.Abs(n.Z) < 0.0175);

// Where the wall sits on the face.
var line = WallLocationLine.FinishFaceExterior;
var lineText = (locationLine ?? "").Trim().ToLowerInvariant().Replace(":", "").Replace("-", " ");
if (lineText == "" || lineText == "finish face exterior") line = WallLocationLine.FinishFaceExterior;
else if (lineText == "finish face interior") line = WallLocationLine.FinishFaceInterior;
else if (lineText == "wall centerline" || lineText == "wall centreline") line = WallLocationLine.WallCenterline;
else if (lineText == "core centerline" || lineText == "core centreline") line = WallLocationLine.CoreCenterline;
else if (lineText == "core exterior" || lineText == "core face exterior") line = WallLocationLine.CoreExterior;
else if (lineText == "core interior" || lineText == "core face interior") line = WallLocationLine.CoreInterior;
else problems.Add("locationLine \"" + locationLine + "\" is not one of finish face exterior, finish face interior, wall centerline, core centerline, core exterior, core interior.");

var massCategory = new ElementId(BuiltInCategory.OST_Mass);
var massList = new List<FamilyInstance>();
foreach (var element in elements ?? new List<Element>())
{
    if (element == null) continue;
    var instance = element as FamilyInstance;
    if (instance == null || element.Category == null || element.Category.Id != massCategory) problems.Add(element.Id + " is not a mass.");
    else massList.Add(instance);
}
if (massList.Count == 0 && problems.Count == 0) problems.Add("No mass was given - select the masses, or hand on REPORT_MASSES' elements.");

// A mass's faces with their references, from the family's own geometry.
var options = new Options();
options.ComputeReferences = true;
options.DetailLevel = ViewDetailLevel.Fine;
Func<Face, Transform, XYZ> normalOf = (f, t) =>
{
    XYZ n;
    var flat = f as PlanarFace;
    if (flat != null) n = flat.FaceNormal;
    else
    {
        var box = f.GetBoundingBox();
        n = f.ComputeNormal((box.Min + box.Max) / 2);
    }
    return t == null ? n : t.OfVector(n);
};
Func<FamilyInstance, List<Tuple<Reference, XYZ>>> facesOf = mass =>
{
    var found = new List<Tuple<Reference, XYZ>>();
    Action<GeometryElement, Transform> take = (g, t) =>
    {
        if (g == null) return;
        foreach (var obj in g)
        {
            var solid = obj as Solid;
            if (solid == null || solid.Volume <= 1e-9) continue;
            foreach (Face face in solid.Faces)
                if (face.Reference != null) found.Add(Tuple.Create(face.Reference, normalOf(face, t)));
        }
    };
    var top = mass.get_Geometry(options);
    take(top, null);
    if (top != null)
        foreach (var obj in top)
        {
            var nested = obj as GeometryInstance;
            if (nested != null) take(nested.GetSymbolGeometry(), nested.Transform);
        }
    return found;
};

if (problems.Count == 0)
{
    foreach (var mass in massList)
    {
        // What is on it already.
        var already = 0;
        try
        {
            foreach (var id in MassInstanceUtils.GetJoinedElementIds(doc, mass.Id))
                if (doc.GetElement(id) is FaceWall) already++;
        }
        catch (Exception) { }
        if (already > 0)
        {
            skipped++;
            lines.Add("mass " + mass.Id + ": skipped - it already carries " + already
                + " wall(s) by face. Remove them first, or use Update to Face after changing the mass.");
            continue;
        }

        var candidates = facesOf(mass).Where(c => faceWanted(c.Item2)).ToList();
        var valid = candidates.Where(c => FaceWall.IsValidFaceReferenceForFaceWall(doc, c.Item1)).ToList();
        var made = 0;
        foreach (var c in valid)
        {
            try
            {
                var wall = FaceWall.Create(doc, type.Id, line, c.Item1);
                if (wall != null) { createdIds.Add(wall.Id); made++; }
            }
            catch (Exception ex) { problems.Add("mass " + mass.Id + ": Revit refused a wall on a face it had accepted - " + ex.Message); }
        }
        var refused = candidates.Count - valid.Count;
        if (made == 0)
        {
            skipped++;
            lines.Add("mass " + mass.Id + ": nothing made - " + candidates.Count + " face(s) facing " + kind
                + ", none that Revit's own check takes for a wall.");
        }
        else
            lines.Add("mass " + mass.Id + ": " + made + " wall(s) by face, " + type.Name + ", location line " + line
                + (refused > 0 ? "; " + refused + " face(s) Revit's own check refused for a wall" : "") + ".");
    }

    // Read back, all or nothing.
    doc.Regenerate();
    foreach (var id in createdIds)
    {
        var element = doc.GetElement(id);
        if (element == null) { problems.Add("wall " + id + " is not in the model after it was made."); continue; }
        var area = element.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
        if (element.GetTypeId() != type.Id) problems.Add("wall " + id + " reads another type.");
        else if (area == null || area.AsDouble() <= 0) problems.Add("wall " + id + " has no area.");
    }
    if (problems.Count > 0)
    {
        foreach (var id in createdIds) { try { if (doc.GetElement(id) != null) doc.Delete(id); } catch (Exception) { } }
        createdIds.Clear();
    }
    created = createdIds.Count;
}

var report = string.Join(" | ", lines);
if (problems.Count > 0)
{
    findings.Add("NOTHING WAS MADE: " + string.Join(" ", problems));
    findings.AddRange(lines);
}
else
{
    findings.Add(created + " wall(s) by face made and read back; " + skipped + " mass(es) skipped."
        + ((locationLine ?? "").Trim() == "" ? " Location line Finish Face: Exterior - the wall's outside face is on the mass face; that is Heron's default." : ""));
    findings.AddRange(lines);
    findings.Add("They are tied to the mass's faces: after the mass changes, select them and use Update to Face. The mass itself stays in the model.");
}
