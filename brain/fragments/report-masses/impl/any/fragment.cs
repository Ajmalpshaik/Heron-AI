// NOT STANDALONE. Assumes `doc`, `masses`, `siteArea` and `withOutlines` are
// in scope; leaves `elements`, `massCount`, `grossFloorAreaM2`, `report` and
// `findings` behind.
//
// READ ONLY - opens no transaction.
//
// A MASSING STUDY'S NUMBERS, read off the conceptual masses placed in the
// project - in-place or loaded:
//
//   each mass    family and type, in-place or loaded, its base and top and
//                height, Gross Volume, Gross Surface Area and Gross Floor
//                Area as Revit computes them, and what is built on its faces
//   each floor   its mass floors by level, low to high - floor area,
//                perimeter, exterior surface area, volume and Usage
//   the totals   gross floor area by level and by Usage, and with a site
//                area the plot ratio (FAR) and the site coverage
//
// masses     empty for every mass; otherwise ids, or words of a family or
//            type name, commas between
// siteArea   empty for none; a number of square metres; or "property lines"
//            for the summed Area of the project's property lines
// withOutlines  true adds each mass floor's outline in millimetres, written
//            as CREATE_FLOOR's boundary takes it - so a slab can be drawn on
//            a mass floor where Floor by Face has no API
//
// OVERLAP IS MEASURED, NOT ASSUMED: two masses whose solids share volume and
// are not joined both count that volume, and its floor area - Autodesk's
// help on joining mass instances says Join Geometry removes it. Each such
// pair is named with the shared volume.

const double MmPerFoot = 304.8;
const double M2PerFt2 = 0.09290304;
const double M3PerFt3 = 0.028316846592;
var culture = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> m2 = v => (v * M2PerFt2).ToString("0.00", culture);
Func<double, string> m3 = v => (v * M3PerFt3).ToString("0.00", culture);
Func<double, string> mm = v => Math.Round(v * MmPerFoot).ToString("0", culture);

var elements = new List<Element>();
var massCount = 0;
var grossFloorAreaM2 = 0.0;
var lines = new List<string>();
var findings = new List<string>();

// Which masses.
var asked = new List<string>();
foreach (var part in (masses ?? "").Split(','))
    if (part.Trim() != "") asked.Add(part.Trim());
var found = new List<FamilyInstance>();
foreach (var candidate in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Mass).WhereElementIsNotElementType())
{
    var instance = candidate as FamilyInstance;
    if (instance == null || instance.Symbol == null) continue;
    if (asked.Count > 0)
    {
        var label = instance.Symbol.FamilyName + " : " + instance.Symbol.Name;
        var hit = asked.Any(a => a == instance.Id.ToString() || a == instance.UniqueId
            || label.IndexOf(a, StringComparison.OrdinalIgnoreCase) >= 0);
        if (!hit) continue;
    }
    found.Add(instance);
}
if (found.Count == 0)
    findings.Add(asked.Count == 0
        ? "This project has no conceptual masses. In-place masses (Massing & Site > In-Place Mass) and loaded mass families both count."
        : "No mass matched \"" + string.Join(", ", asked) + "\" - an id, or words of its family or type name.");

// Every solid of a mass, in project coordinates.
var options = new Options();
options.DetailLevel = ViewDetailLevel.Fine;
Func<Element, List<Solid>> solidsOf = e =>
{
    var solids = new List<Solid>();
    Action<GeometryElement> walk = null;
    walk = g =>
    {
        if (g == null) return;
        foreach (var obj in g)
        {
            var solid = obj as Solid;
            if (solid != null && solid.Volume > 1e-9) solids.Add(solid);
            var nested = obj as GeometryInstance;
            if (nested != null) walk(nested.GetInstanceGeometry());
        }
    };
    walk(e.get_Geometry(options));
    return solids;
};

var levelElevation = new Dictionary<ElementId, double>();
var levelName = new Dictionary<ElementId, string>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Level)))
{
    var level = element as Level;
    if (level == null) continue;
    levelElevation[level.Id] = level.ProjectElevation;
    levelName[level.Id] = level.Name;
}

var byLevel = new Dictionary<string, double>();
var byLevelHeight = new Dictionary<string, double>();
var byUsage = new Dictionary<string, double>();
var coverage = 0.0;
var outlineCount = 0;
var solidsByMass = new Dictionary<ElementId, List<Solid>>();

foreach (var mass in found)
{
    elements.Add(mass);
    massCount++;
    var symbol = mass.Symbol;
    var inPlace = symbol.Family != null && symbol.Family.IsInPlace;
    var head = "mass " + mass.Id + " \"" + symbol.FamilyName + " : " + symbol.Name + "\" (" + (inPlace ? "in-place" : "loaded family") + ")";

    double volume = 0, surface = 0, gfa = 0;
    try
    {
        volume = MassInstanceUtils.GetGrossVolume(doc, mass.Id);
        surface = MassInstanceUtils.GetGrossSurfaceArea(doc, mass.Id);
        gfa = MassInstanceUtils.GetGrossFloorArea(doc, mass.Id);
    }
    catch (Exception ex) { lines.Add(head + ": Revit gave no gross values - " + ex.Message); continue; }
    grossFloorAreaM2 += gfa * M2PerFt2;

    var box = mass.get_BoundingBox(null);
    var text = head + ": Gross Volume " + m3(volume) + " m3, Gross Surface Area " + m2(surface) + " m2, Gross Floor Area " + m2(gfa) + " m2";
    if (box != null) text += ", base " + mm(box.Min.Z) + " mm, top " + mm(box.Max.Z) + " mm, height " + mm(box.Max.Z - box.Min.Z) + " mm";
    lines.Add(text + ".");

    var solids = solidsOf(mass);
    solidsByMass[mass.Id] = solids;

    // Its mass floors, low to high.
    var floors = new List<MassLevelData>();
    foreach (var id in MassInstanceUtils.GetMassLevelDataIds(doc, mass.Id))
    {
        var floor = doc.GetElement(id) as MassLevelData;
        if (floor != null) floors.Add(floor);
    }
    floors = floors.OrderBy(f => levelElevation.ContainsKey(f.LevelId) ? levelElevation[f.LevelId] : double.MaxValue).ToList();
    if (floors.Count == 0)
        lines.Add("  no mass floors - its Gross Floor Area stays 0 until CREATE_MASS_FLOORS adds them.");

    var sum = 0.0;
    var lowest = true;
    foreach (var floor in floors)
    {
        var name = levelName.ContainsKey(floor.LevelId) ? levelName[floor.LevelId] : "(level not read)";
        var usage = (floor.StrUsage ?? "").Trim();
        if (floor.IsEmpty())
        {
            lines.Add("  " + name + ": mass floor " + floor.Id + " is EMPTY - the level does not cut the mass.");
            continue;
        }
        var area = floor.NLevelFafArea;
        sum += area;
        if (lowest) { coverage += area; lowest = false; }
        byLevel[name] = (byLevel.ContainsKey(name) ? byLevel[name] : 0) + area;
        byLevelHeight[name] = levelElevation.ContainsKey(floor.LevelId) ? levelElevation[floor.LevelId] : double.MaxValue;
        var usageKey = usage == "" ? "(no Usage)" : usage;
        byUsage[usageKey] = (byUsage.ContainsKey(usageKey) ? byUsage[usageKey] : 0) + area;
        lines.Add("  " + name + ": mass floor " + floor.Id + " - area " + m2(area) + " m2, perimeter " + mm(floor.NLevelPerimeter)
            + " mm, exterior surface " + m2(floor.NExteriorSurfaceArea) + " m2, volume " + m3(floor.NVolume) + " m3, Usage "
            + (usage == "" ? "(none)" : "\"" + usage + "\"") + ".");

        // The outline at this level, as CREATE_FLOOR's boundary is written.
        if (withOutlines && levelElevation.ContainsKey(floor.LevelId) && outlineCount < 60)
        {
            var z = levelElevation[floor.LevelId];
            var plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, z));
            foreach (var solid in solids)
            {
                Solid above = null;
                try { above = BooleanOperationsUtils.CutWithHalfSpace(solid, plane); }
                catch (Exception) { above = null; }
                if (above == null) continue;
                foreach (Face face in above.Faces)
                {
                    var flat = face as PlanarFace;
                    if (flat == null || flat.FaceNormal.Z > -0.9999 || Math.Abs(flat.Origin.Z - z) > 1e-4) continue;
                    var loops = flat.GetEdgesAsCurveLoops();
                    CurveLoop outer = null;
                    var outerArea = -1.0;
                    foreach (var loop in loops)
                    {
                        var points = new List<XYZ>();
                        foreach (var curve in loop) points.AddRange(curve.Tessellate());
                        var shoelace = 0.0;
                        for (var i = 0; i < points.Count; i++)
                        {
                            var p = points[i];
                            var q = points[(i + 1) % points.Count];
                            shoelace += p.X * q.Y - q.X * p.Y;
                        }
                        if (Math.Abs(shoelace) > outerArea) { outerArea = Math.Abs(shoelace); outer = loop; }
                    }
                    if (outer == null) continue;
                    var curves = outer.ToList();
                    var parts = new List<string>();
                    var curvedOther = 0;
                    var start = curves[0].GetEndPoint(0);
                    parts.Add(mm(start.X) + "," + mm(start.Y) + ",0");
                    for (var i = 0; i < curves.Count; i++)
                    {
                        var curve = curves[i];
                        var last = i == curves.Count - 1;
                        var end = curve.GetEndPoint(1);
                        if (curve is Line)
                        {
                            if (!last) parts.Add(mm(end.X) + "," + mm(end.Y) + ",0");
                        }
                        else if (curve is Arc)
                        {
                            var middle = curve.Evaluate(0.5, true);
                            parts.Add("arc " + mm(middle.X) + "," + mm(middle.Y) + " " + mm(end.X) + "," + mm(end.Y));
                        }
                        else
                        {
                            curvedOther++;
                            var points = curve.Tessellate();
                            for (var k = 1; k < points.Count; k++)
                                if (!(last && k == points.Count - 1)) parts.Add(mm(points[k].X) + "," + mm(points[k].Y) + ",0");
                        }
                    }
                    outlineCount++;
                    var note = "";
                    if (loops.Count > 1) note += " It has " + (loops.Count - 1) + " hole(s) inside - a courtyard or an atrium; this outline is the outside edge only.";
                    if (curvedOther > 0) note += " " + curvedOther + " curved edge(s) that are not arcs were written as straight pieces.";
                    lines.Add("    outline at " + name + ": " + string.Join("; ", parts) + "." + note);
                }
            }
        }
    }
    if (floors.Count > 0 && Math.Abs(sum - gfa) * M2PerFt2 > 0.01)
        lines.Add("  its mass floors add up to " + m2(sum) + " m2 and its Gross Floor Area is " + m2(gfa) + " m2 - Revit adjusts a mass joined to another; both are reported as Revit gives them.");

    // What is built on its faces - walls, floors, roofs, curtain systems by face.
    try
    {
        var joined = MassInstanceUtils.GetJoinedElementIds(doc, mass.Id);
        if (joined.Count > 0)
        {
            var counts = new Dictionary<string, int>();
            foreach (var id in joined)
            {
                var other = doc.GetElement(id);
                var category = other == null || other.Category == null ? "(no category)" : other.Category.Name;
                counts[category] = (counts.ContainsKey(category) ? counts[category] : 0) + 1;
            }
            lines.Add("  joined to it: " + string.Join(", ", counts.Select(c => c.Value + " " + c.Key)) + ".");
        }
    }
    catch (Exception) { }
}

// Overlap between masses that are not joined - measured on the solids.
var overlapPairs = 0;
for (var a = 0; a < found.Count; a++)
{
    for (var b = a + 1; b < found.Count; b++)
    {
        if (!solidsByMass.ContainsKey(found[a].Id) || !solidsByMass.ContainsKey(found[b].Id)) continue;
        var boxA = found[a].get_BoundingBox(null);
        var boxB = found[b].get_BoundingBox(null);
        if (boxA == null || boxB == null) continue;
        if (boxA.Max.X < boxB.Min.X || boxB.Max.X < boxA.Min.X || boxA.Max.Y < boxB.Min.Y
            || boxB.Max.Y < boxA.Min.Y || boxA.Max.Z < boxB.Min.Z || boxB.Max.Z < boxA.Min.Z) continue;
        if (JoinGeometryUtils.AreElementsJoined(doc, found[a], found[b])) continue;
        var shared = 0.0;
        foreach (var sa in solidsByMass[found[a].Id])
            foreach (var sb in solidsByMass[found[b].Id])
            {
                try
                {
                    var common = BooleanOperationsUtils.ExecuteBooleanOperation(sa, sb, BooleanOperationsType.Intersect);
                    if (common != null) shared += common.Volume;
                }
                catch (Exception) { }
            }
        if (shared * M3PerFt3 < 0.001) continue;
        overlapPairs++;
        findings.Add("Masses " + found[a].Id + " and " + found[b].Id + " overlap by " + m3(shared)
            + " m3 and are NOT joined - both count it, in volume and in floor area. Join Geometry (JOIN_GEOMETRY) makes Revit count it once.");
    }
}

// The totals.
if (byLevel.Count > 0)
    lines.Add("by level: " + string.Join(", ", byLevel.OrderBy(l => byLevelHeight[l.Key]).Select(l => l.Key + " " + m2(l.Value) + " m2")) + ".");
if (byUsage.Count > 0)
    lines.Add("by Usage: " + string.Join(", ", byUsage.OrderByDescending(u => u.Value).Select(u => u.Key + " " + m2(u.Value) + " m2")) + ".");

// The site, when one is given.
var siteText = (siteArea ?? "").Trim();
double siteM2 = 0;
if (siteText != "")
{
    if (siteText.IndexOf("property", StringComparison.OrdinalIgnoreCase) >= 0)
    {
        var count = 0;
        foreach (var line in new FilteredElementCollector(doc).OfClass(typeof(PropertyLine)))
        {
            var parameter = line.get_Parameter(BuiltInParameter.PROPERTY_AREA);
            if (parameter != null && parameter.HasValue) { siteM2 += parameter.AsDouble() * M2PerFt2; count++; }
        }
        if (count == 0) findings.Add("No property line with an Area was found, so no plot ratio is given.");
        else findings.Add("Site area " + siteM2.ToString("0.00", culture) + " m2, from " + count + " property line(s).");
    }
    else
    {
        var number = new string(siteText.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        if (!double.TryParse(number, System.Globalization.NumberStyles.Float, culture, out siteM2) || siteM2 <= 0)
        {
            siteM2 = 0;
            findings.Add("\"" + siteText + "\" is not a site area - a number of square metres, or \"property lines\".");
        }
    }
}

var report = string.Join(" | ", lines);
findings.Insert(0, massCount + " mass(es); Gross Floor Area " + grossFloorAreaM2.ToString("0.00", culture) + " m2 in all"
    + (overlapPairs > 0 ? ", with " + overlapPairs + " unjoined overlap(s) counted twice" : "") + ".");
if (siteM2 > 0 && massCount > 0)
{
    findings.Insert(1, "Plot ratio (FAR) " + (grossFloorAreaM2 / siteM2).ToString("0.00", culture) + " on a site of " + siteM2.ToString("0.00", culture)
        + " m2; site coverage " + (100 * coverage * M2PerFt2 / siteM2).ToString("0.0", culture)
        + "% - taken from each mass's lowest mass floor, so a mass with none is not in it and an overlap counts twice.");
}
if (withOutlines) findings.Add(outlineCount + " outline(s) written" + (outlineCount >= 60 ? " - stopped at 60" : "") + "; each can be handed to CREATE_FLOOR as its boundary, in project coordinates.");
findings.AddRange(lines);
