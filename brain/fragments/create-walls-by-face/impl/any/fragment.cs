// NOT STANDALONE. Assumes `doc`, `elements`, `typeName`, `faces` and
// `locationLine` are in scope; leaves `created`, `createdIds`, `skipped`,
// `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// MODEL BY FACE on conceptual masses - Massing & Site > Model by Face:
//
//   a WALL TYPE            Wall by Face, one wall on each face chosen
//   a CURTAIN SYSTEM TYPE  Curtain System by Face, one system per mass over
//                          every face chosen
//
// What is made follows from the type named, so no kind is asked. A floor or
// a roof type is refused by name: Floor by Face and Roof by Face have no API
// on any release, and REPORT_MASSES' outlines with CREATE_FLOOR are the
// route for a slab.
//
//   faces          sides (empty) - every face that does not face straight
//                  up or down; vertical - upright faces only; up - every
//                  face that faces upward, flat or sloped, a glazed roof;
//                  all
//   locationLine   walls only: finish face exterior (empty), finish face
//                  interior, wall centerline, core centerline, core
//                  exterior, core interior. Empty puts the wall's OUTSIDE
//                  face on the mass face - Heron's default, said in the
//                  answer, not Revit's.
//
// A MASS ALREADY CARRYING WALLS BY FACE, or a curtain system for a curtain
// system, is skipped by name rather than given a second skin on top.
//
// READ BACK: every element made is read again - its type, and a wall's
// area or a system's grid count; one that does not read back fails the call
// and nothing is kept.

var created = 0;
var createdIds = new List<ElementId>();
var skipped = 0;
var lines = new List<string>();
var findings = new List<string>();
var problems = new List<string>();

// The type, by "Family : Type" or by its type name.
var wantedName = (typeName ?? "").Trim();
Func<ElementType, bool> named = t =>
    string.Equals(t.Name, wantedName, StringComparison.OrdinalIgnoreCase)
    || string.Equals(t.FamilyName + " : " + t.Name, wantedName, StringComparison.OrdinalIgnoreCase)
    || string.Equals(t.FamilyName + ": " + t.Name, wantedName, StringComparison.OrdinalIgnoreCase);
var wallTypes = new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().Where(t => named(t)).ToList();
var systemTypes = new FilteredElementCollector(doc).OfClass(typeof(CurtainSystemType)).Cast<CurtainSystemType>().Where(t => named(t)).ToList();
var floorOrRoof = new FilteredElementCollector(doc).WhereElementIsElementType().Cast<Element>()
    .Where(t => (t is FloorType || t is RoofType) && named((ElementType)t)).ToList();
WallType wallType = null;
CurtainSystemType systemType = null;
if (wantedName == "") problems.Add("No type was given - a wall type for Wall by Face, or a curtain system type for Curtain System by Face.");
else if (wallTypes.Count + systemTypes.Count == 0 && floorOrRoof.Count > 0)
    problems.Add("\"" + wantedName + "\" is a floor or roof type. Floor by Face and Roof by Face have no API on any release - for a slab on a mass floor, REPORT_MASSES with withOutlines gives the outline CREATE_FLOOR takes; Roof by Face is done by hand.");
else if (wallTypes.Count + systemTypes.Count == 0)
    problems.Add("No wall type or curtain system type is named \"" + wantedName + "\". Curtain system types: "
        + string.Join(", ", new FilteredElementCollector(doc).OfClass(typeof(CurtainSystemType)).Cast<ElementType>().Select(t => t.Name).Take(15)) + ".");
else if (wallTypes.Count + systemTypes.Count > 1)
    problems.Add("\"" + wantedName + "\" names " + (wallTypes.Count + systemTypes.Count) + " types - write it as \"Family : Type\".");
else if (wallTypes.Count == 1)
{
    wallType = wallTypes[0];
    if (!FaceWall.IsWallTypeValidForFaceWall(doc, wallType.Id))
        problems.Add("\"" + wallType.Name + "\" (" + wallType.Kind + ") cannot be a wall by face - Revit's own check refuses it. A curtain wall on a mass is a curtain system by face.");
}
else systemType = systemTypes[0];

// Which faces.
var kind = (faces ?? "").Trim().ToLowerInvariant();
if (kind == "") kind = "sides";
if (kind != "sides" && kind != "vertical" && kind != "up" && kind != "all")
    problems.Add("faces is sides, vertical, up or all - not \"" + faces + "\".");
Func<XYZ, bool> faceWanted = n =>
    kind == "all" || (kind == "sides" && Math.Abs(n.Z) < 0.999) || (kind == "vertical" && Math.Abs(n.Z) < 0.0175) || (kind == "up" && n.Z > 0.0175);

// Where the wall sits on the face.
var line = WallLocationLine.FinishFaceExterior;
var lineText = (locationLine ?? "").Trim().ToLowerInvariant().Replace(":", "").Replace("-", " ");
if (lineText == "" || lineText == "finish face exterior") line = WallLocationLine.FinishFaceExterior;
else if (lineText == "finish face interior") line = WallLocationLine.FinishFaceInterior;
else if (lineText == "wall centerline" || lineText == "wall centreline") line = WallLocationLine.WallCenterline;
else if (lineText == "core centerline" || lineText == "core centreline") line = WallLocationLine.CoreCenterline;
else if (lineText == "core exterior" || lineText == "core face exterior") line = WallLocationLine.CoreExterior;
else if (lineText == "core interior" || lineText == "core face interior") line = WallLocationLine.CoreInterior;
else if (wallType != null) problems.Add("locationLine \"" + locationLine + "\" is not one of finish face exterior, finish face interior, wall centerline, core centerline, core exterior, core interior.");

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

// A mass's faces with their references, by two routes: the instance's own
// geometry, and the family's geometry carried by the instance. Which route
// Revit accepts is said in the answer.
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
Func<FamilyInstance, bool, List<Tuple<Reference, XYZ>>> facesOf = (mass, symbolRoute) =>
{
    var found = new List<Tuple<Reference, XYZ>>();
    Action<GeometryElement, Transform, bool> walk = null;
    walk = (g, t, inside) =>
    {
        if (g == null) return;
        foreach (var obj in g)
        {
            var solid = obj as Solid;
            if (solid != null && solid.Volume > 1e-9 && (inside || !symbolRoute))
                foreach (Face face in solid.Faces)
                    if (face.Reference != null) found.Add(Tuple.Create(face.Reference, normalOf(face, t)));
            var nested = obj as GeometryInstance;
            if (nested == null) continue;
            if (symbolRoute) walk(nested.GetSymbolGeometry(), t == null ? nested.Transform : t.Multiply(nested.Transform), true);
            else walk(nested.GetInstanceGeometry(), null, true);
        }
    };
    walk(mass.get_Geometry(options), null, false);
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
            {
                var other = doc.GetElement(id);
                if ((wallType != null && other is FaceWall) || (systemType != null && other is CurtainSystem)) already++;
            }
        }
        catch (Exception) { }
        if (already > 0)
        {
            skipped++;
            lines.Add("mass " + mass.Id + ": skipped - it already carries " + already + (wallType != null ? " wall(s)" : " curtain system(s)")
                + " by face. Remove them first, or use Update to Face after changing the mass.");
            continue;
        }

        var made = 0;
        var route = "";
        foreach (var symbolRoute in new[] { false, true })
        {
            var candidates = facesOf(mass, symbolRoute).Where(c => faceWanted(c.Item2)).ToList();
            if (candidates.Count == 0) continue;
            if (wallType != null)
            {
                var valid = candidates.Where(c => FaceWall.IsValidFaceReferenceForFaceWall(doc, c.Item1)).ToList();
                if (valid.Count == 0) continue;
                route = symbolRoute ? "the family's geometry" : "the instance's geometry";
                foreach (var c in valid)
                {
                    try
                    {
                        var wall = FaceWall.Create(doc, wallType.Id, line, c.Item1);
                        if (wall != null) { createdIds.Add(wall.Id); made++; }
                    }
                    catch (Exception ex) { problems.Add("mass " + mass.Id + ": Revit refused a wall on a face it had accepted - " + ex.Message); }
                }
                var refused = candidates.Count - valid.Count;
                lines.Add("mass " + mass.Id + ": " + made + " wall(s) by face, " + wallType.Name + ", location line " + line
                    + (refused > 0 ? "; " + refused + " face(s) Revit's own check refused for a wall" : "") + " - faces read from " + route + ".");
            }
            else
            {
                var references = new ReferenceArray();
                foreach (var c in candidates) references.Append(c.Item1);
                ICollection<ElementId> systems = null;
                try { systems = doc.Create.NewCurtainSystem2(references, systemType); }
                catch (Exception ex) { lines.Add("mass " + mass.Id + ": " + (symbolRoute ? "the family's" : "the instance's") + " faces were refused - " + ex.Message); continue; }
                if (systems == null || systems.Count == 0) continue;
                route = symbolRoute ? "the family's geometry" : "the instance's geometry";
                foreach (var id in systems) { createdIds.Add(id); made++; }
                lines.Add("mass " + mass.Id + ": " + made + " curtain system(s), " + systemType.Name + ", over " + candidates.Count + " face(s) - faces read from " + route + ".");
            }
            break;
        }
        if (made == 0)
        {
            skipped++;
            lines.Add("mass " + mass.Id + ": nothing made - no face facing " + kind + " that Revit accepts.");
        }
    }

    // Read back, all or nothing.
    doc.Regenerate();
    foreach (var id in createdIds)
    {
        var element = doc.GetElement(id);
        if (element == null) { problems.Add("element " + id + " is not in the model after it was made."); continue; }
        if (wallType != null)
        {
            var area = element.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
            if (element.GetTypeId() != wallType.Id) problems.Add("wall " + id + " reads another type.");
            else if (area == null || area.AsDouble() <= 0) problems.Add("wall " + id + " has no area.");
        }
        else
        {
            var system = element as CurtainSystem;
            if (system == null || system.GetTypeId() != systemType.Id) problems.Add("curtain system " + id + " does not read back as " + systemType.Name + ".");
            else if (system.CurtainGrids == null || system.CurtainGrids.Size == 0) problems.Add("curtain system " + id + " has no curtain grid on any face.");
        }
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
    findings.Add(created + (wallType != null ? " wall(s)" : " curtain system(s)") + " by face made and read back; " + skipped + " mass(es) skipped."
        + (wallType != null && (locationLine ?? "").Trim() == "" ? " Location line Finish Face: Exterior - the wall's outside face is on the mass face; that is Heron's default." : ""));
    findings.AddRange(lines);
    findings.Add("They are tied to the mass's faces: after the mass changes, select them and use Update to Face. The mass itself stays in the model.");
}
