// NOT STANDALONE. Assumes `doc`, `level`, `roofType`, `plane`, `profile`,
// `extrusionStartMm` and `extrusionEndMm` are in scope, and leaves `roof`,
// `planeDrawn`, `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// WHAT IT DOES. Revit's Architecture > Roof > Roof by Extrusion: a roof
// profile drawn as an OPEN line on a VERTICAL plane, extruded between a start
// and an end measured from that plane. A barrel, a gable, a mono-pitch or any
// chain of straight and curved edges - the profile is the roof's shape seen
// end on. CREATE_FLOOR makes the other roof, a footprint roof from a closed
// outline on plan; SLOPE_ROOF_EDGES slopes one of those.
//
// NOTHING IS GUESSED (D-33). The level, the roof type and the plane are each
// named by the modeller; with one left out, or a name nothing has, nothing is
// made and the project's levels, roof types or named reference planes and
// grids are listed.
//
// THE PLANE. A named reference plane, which must be vertical, or a straight
// grid - then a reference plane is drawn along the grid, in a floor plan of
// the roof's level, to sketch the profile on; it is handed back as
// `planeDrawn` and named in the report.
//
// THE PROFILE, in MILLIMETRES, in the plane's own two directions: "U,V" with
// ";" between - U ALONG the plane from its start (a reference plane's bubble
// end; a grid's first end), V UP from the level's PROJECT elevation - and
// "arc MU,MV U,V" after a point makes the next edge an arc through MU,MV
// ending at U,V, the writing CREATE_FLOOR's boundary takes. A barrel 12 m
// across rising 2 m is "0,0; arc 6000,2000 12000,0". The line is OPEN: Revit
// refuses a closed one, so a last point back on the first is refused here,
// by name, before Revit is asked. Two points in one place, and an arc whose
// three points are in a line, are refused the same way.
//
// THE EXTENTS, in MILLIMETRES from the plane: positive to the EAST of it, or
// to the NORTH when the plane faces exactly north-south - the side
// CREATE_PROJECT_REFERENCE_PLANES calls positive. Revit's own Extrusion Start
// and Extrusion End are measured along the plane's normal, whichever way it
// points; the sign is turned here so a positive number is always east (or
// north), and the report says which side it went.
//
// REVIT'S OWN REFUSAL IS REPORTED, NEVER SWALLOWED. A profile Revit will not
// extrude - one that crosses itself - throws with Revit's own words, the host
// rolls the whole call back and nothing is kept, the plane drawn along a grid
// included.
//
// READ BACK. The new roof's type, level, Extrusion Start and End, its
// profile's edge count and the height of its top above the level are read off
// Revit, never recomputed from the input. A type, level, extents or edge
// count that does not read back as asked THROWS: nothing is kept.
//
// MILLIMETRES IN, / 304.8 (D-20). An ElementId is never read as a number.
//
// RELEASES (tools/api-surface --members, 2026-11-02): the creation
// Document's NewExtrusionRoof(CurveArray, ReferencePlane, Level, RoofType,
// double, double), ItemFactoryBase.NewReferencePlane(XYZ, XYZ, XYZ, View),
// ExtrusionRoof.GetProfile, ReferencePlane.Normal / Direction / BubbleEnd,
// Grid.Curve / IsCurved, Level.ProjectElevation, ElementType.FamilyName,
// Element.LevelId, Arc.Create(XYZ, XYZ, XYZ), Line.CreateBound,
// CurveArray.Append and BuiltInParameter EXTRUSION_START_PARAM /
// EXTRUSION_END_PARAM / DATUM_TEXT read the same 2020 through 2027.

var roof = ElementId.InvalidElementId;
var planeDrawn = ElementId.InvalidElementId;
var report = "";
string refused = null;
var findings = new List<string>();

const double MillimetresPerFoot = 304.8;
var culture = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * MillimetresPerFoot, 1).ToString("0.#", culture);
var shortest = doc.Application.ShortCurveTolerance;
var halfMillimetre = 0.5 / MillimetresPerFoot;

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};
Func<RoofType, string> typeLabel = t => (t.FamilyName ?? "") + ": " + t.Name;
Func<IEnumerable<string>, string> listOf = names =>
{
    var all = names.Distinct().OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
    if (all.Count == 0) return "none";
    return string.Join(", ", all.Take(25)) + (all.Count > 25 ? " and " + (all.Count - 25) + " more" : "");
};

var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
    .OrderBy(l => l.ProjectElevation).ToList();
var roofTypes = new FilteredElementCollector(doc).OfClass(typeof(RoofType)).Cast<RoofType>().ToList();
var namedPlanes = new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>()
    .Where(rp => ownName(rp).Length > 0).ToList();
var grids = new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>().ToList();

Func<string> levelList = () => " Levels in this project: " + string.Join(", ", levels.Select(l => l.Name)) + ".";
Func<string> typeList = () => " Roof types in this project: " + listOf(roofTypes.Select(typeLabel)) + ".";
Func<string> planeList = () => " Named reference planes: " + listOf(namedPlanes.Select(ownName))
    + ". Grids: " + listOf(grids.Select(g => g.Name)) + ".";

// ---- the profile: points (U, V) in feet, and the arc point before each ----

string problem = null;
var points = new List<double[]>();
var through = new List<double[]>();
var arcs = 0;

Func<string, double[]> pairOf = text =>
{
    var parts = text.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
    if (parts.Count != 2)
    {
        problem = "A profile point is two numbers in MILLIMETRES - along the plane, then up from the level - "
            + "separated by a comma: \"6000,2000\". \"" + text + "\" has " + parts.Count + ".";
        return null;
    }
    var pair = new double[2];
    for (var i = 0; i < 2; i++)
    {
        double value;
        if (!double.TryParse(parts[i], System.Globalization.NumberStyles.Float, culture, out value)
            || double.IsNaN(value) || double.IsInfinity(value))
        {
            problem = "\"" + parts[i] + "\" is not a number of millimetres. Type digits only - 2000 or 2000.5.";
            return null;
        }
        if (Math.Abs(value) > 100.0 * 1000.0 * 1000.0)
        {
            problem = "\"" + parts[i] + "\" is more than 100 km, which is no roof. The profile is in MILLIMETRES.";
            return null;
        }
        pair[i] = value / MillimetresPerFoot;
    }
    return pair;
};
Func<double[], double[], double> apart = (a, b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
Func<double[], string> said = p => mm(p[0]) + "," + mm(p[1]);

foreach (var piece in (profile ?? "").Split(';'))
{
    var item = piece.Trim();
    if (item.Length == 0) continue;
    var words = System.Text.RegularExpressions.Regex.Replace(item, @"\s*,\s*", ",")
        .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    if (words.Length > 0 && words[0].ToLowerInvariant() == "arc")
    {
        arcs++;
        if (points.Count == 0)
        {
            problem = "A profile starts at a point - \"" + item + "\" is an arc with nothing before it to start from.";
            break;
        }
        if (words.Length != 3)
        {
            problem = "\"" + item + "\" is not an arc written \"arc MU,MV U,V\" - a point the edge passes "
                + "through, then where it ends, in millimetres.";
            break;
        }
        var mid = pairOf(words[1]);
        var end = mid == null ? null : pairOf(words[2]);
        if (end == null) break;
        through.Add(mid);
        points.Add(end);
    }
    else
    {
        var point = pairOf(item);
        if (point == null) break;
        if (points.Count > 0) through.Add(null);
        points.Add(point);
    }
}

if (problem == null && points.Count < 2)
    problem = "A roof profile is a line of at least two points in millimetres, \"U,V\" with \";\" between - "
        + "\"0,0; 6000,2000; 12000,0\" for a gable 12 m across rising 2 m. "
        + (points.Count == 0 ? "None was given." : "Only one was given.");
for (var i = 0; problem == null && i + 1 < points.Count; i++)
{
    var a = points[i];
    var b = points[i + 1];
    if (apart(a, b) <= shortest)
        problem = "Two points of the profile are in one place, " + said(a) + " - an edge needs two ends.";
    else if (through[i] != null)
    {
        var m = through[i];
        var cross = (m[0] - a[0]) * (b[1] - a[1]) - (m[1] - a[1]) * (b[0] - a[0]);
        if (apart(a, m) <= shortest || apart(m, b) <= shortest || Math.Abs(cross) < 1e-6 * apart(a, m) * apart(a, b))
            problem = "The arc from " + said(a) + " through " + said(m) + " to " + said(b) + " is not an arc - its "
                + "three points are in a line, or two are in the same place.";
    }
}
if (problem == null && apart(points[0], points[points.Count - 1]) <= shortest)
    problem = "The profile ends where it starts, at " + said(points[0]) + ". A roof by extrusion is sketched as an "
        + "OPEN line - Revit refuses a closed one. Leave off the last point.";

// ---- what was named ---------------------------------------------------------

Level host = null;
RoofType type = null;
ReferencePlane sketchOn = null;
Grid grid = null;

if (doc.IsFamilyDocument)
    refused = "The document in front is a family. A roof by extrusion is made in a project; a family's "
        + "extrusions are CREATE_FAMILY_FORMS' job. NOTHING WAS MADE.";
else if (problem != null)
    refused = problem + " NOTHING WAS MADE.";
else if (double.IsNaN(extrusionStartMm) || double.IsNaN(extrusionEndMm))
    refused = "Where the roof starts and ends is never guessed - give both, in millimetres from the plane, positive "
        + "to the east (north when the plane faces north-south): \"from 0 to 6000\". NOTHING WAS MADE.";
else if (double.IsInfinity(extrusionStartMm) || double.IsInfinity(extrusionEndMm)
    || Math.Abs(extrusionStartMm) > 1e8 || Math.Abs(extrusionEndMm) > 1e8)
    refused = "An extrusion start or end beyond 100 km is no roof - they are in MILLIMETRES. NOTHING WAS MADE.";
else if (Math.Abs(extrusionEndMm - extrusionStartMm) / MillimetresPerFoot <= shortest)
    refused = "The roof would start and end in one place (" + extrusionStartMm.ToString(culture) + " mm) - it "
        + "would have no length. NOTHING WAS MADE.";
else
{
    var levelName = (level ?? "").Trim();
    var levelHits = levels.Where(l => string.Equals(l.Name, levelName, StringComparison.OrdinalIgnoreCase)).ToList();
    var typeName = (roofType ?? "").Trim();
    var typeHits = roofTypes.Where(t => string.Equals(typeLabel(t), typeName, StringComparison.OrdinalIgnoreCase)).ToList();
    if (typeHits.Count == 0)
        typeHits = roofTypes.Where(t => string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase)).ToList();
    var planeName = (plane ?? "").Trim();
    var planeHits = namedPlanes.Where(rp => string.Equals(ownName(rp), planeName, StringComparison.OrdinalIgnoreCase)).ToList();
    var gridHits = grids.Where(g => string.Equals(g.Name, planeName, StringComparison.OrdinalIgnoreCase)).ToList();

    if (levelName.Length == 0)
        refused = "No level was named for the roof, and it is never guessed. NOTHING WAS MADE." + levelList();
    else if (levelHits.Count == 0)
        refused = "No level in this project is named \"" + levelName + "\". NOTHING WAS MADE." + levelList();
    else if (levelHits.Count > 1)
        refused = "More than one level is named \"" + levelName + "\" - say which. NOTHING WAS MADE.";
    else if (typeName.Length == 0)
        refused = "No roof type was named, and it is never guessed. NOTHING WAS MADE." + typeList();
    else if (typeHits.Count == 0)
        refused = "No roof type in this project is named \"" + typeName + "\". NOTHING WAS MADE." + typeList();
    else if (typeHits.Count > 1)
        refused = "\"" + typeName + "\" names more than one roof type - " + listOf(typeHits.Select(typeLabel))
            + " - say which, \"Family: Type\". NOTHING WAS MADE.";
    else if (planeName.Length == 0)
        refused = "No plane was named to sketch the profile on - a named reference plane or a straight grid - and "
            + "it is never guessed. NOTHING WAS MADE." + planeList();
    else if (planeHits.Count + gridHits.Count == 0)
        refused = "No named reference plane or grid in this project is called \"" + planeName + "\". NOTHING WAS MADE."
            + planeList();
    else if (planeHits.Count > 0 && gridHits.Count > 0)
        refused = "\"" + planeName + "\" is the name of a reference plane and of a grid - say which. NOTHING WAS MADE.";
    else if (planeHits.Count > 1 || gridHits.Count > 1)
        refused = "More than one " + (gridHits.Count > 1 ? "grid" : "reference plane") + " is named \"" + planeName
            + "\" - say which. NOTHING WAS MADE.";
    else
    {
        host = levelHits[0];
        type = typeHits[0];
        if (planeHits.Count == 1)
        {
            sketchOn = planeHits[0];
            if (Math.Abs(sketchOn.Normal.Z) > 1e-6)
                refused = "Reference plane \"" + planeName + "\" is not vertical. A roof by extrusion is sketched on a "
                    + "vertical plane - one drawn in a plan, or upright in a section. NOTHING WAS MADE.";
        }
        else
        {
            grid = gridHits[0];
            if (grid.IsCurved || !(grid.Curve is Line))
                refused = "Grid \"" + grid.Name + "\" is an arc. A roof by extrusion is sketched on a flat plane - "
                    + "name a straight grid or a reference plane. NOTHING WAS MADE.";
        }
    }
}

// ---- the plane: along a grid, one is drawn ---------------------------------

XYZ origin = null, along = null;
string planeWords = null;

if (refused == null && grid != null)
{
    var plans = new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>()
        .Where(v => !v.IsTemplate && v.ViewType == ViewType.FloorPlan && v.GenLevel != null && v.GenLevel.Id == host.Id)
        .OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ToList();
    var active = doc.ActiveView as ViewPlan;
    var drawIn = active != null && plans.Any(v => v.Id == active.Id) ? active : plans.FirstOrDefault();
    if (drawIn == null)
        refused = "Level \"" + host.Name + "\" has no floor plan to draw a reference plane along grid " + grid.Name
            + " in. Make one, or name a reference plane already drawn. NOTHING WAS MADE.";
    else
    {
        var line = (Line)grid.Curve;
        var start = new XYZ(line.GetEndPoint(0).X, line.GetEndPoint(0).Y, host.ProjectElevation);
        var end = new XYZ(line.GetEndPoint(1).X, line.GetEndPoint(1).Y, host.ProjectElevation);
        ReferencePlane drawn;
        try
        {
            drawn = doc.Create.NewReferencePlane(start, end, XYZ.BasisZ, drawIn);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused a reference plane along grid " + grid.Name + " in \""
                + drawIn.Name + "\": " + ex.Message + " NOTHING from this call was kept.");
        }
        if (drawn == null || Math.Abs(drawn.Normal.Z) > 1e-6
            || Math.Abs(drawn.Normal.DotProduct((end - start).Normalize())) > 1e-6)
            throw new InvalidOperationException("The reference plane drawn along grid " + grid.Name + " did not read "
                + "back as a vertical plane along it. NOTHING from this call was kept.");
        sketchOn = drawn;
        planeDrawn = drawn.Id;
        origin = start;
        along = (end - start).Normalize();
        planeWords = "a reference plane drawn along grid " + grid.Name + " in \"" + drawIn.Name + "\" (id "
            + drawn.Id.ToString() + ")";
    }
}
else if (refused == null)
{
    var direction = sketchOn.Direction;
    along = Math.Abs(direction.Z) < 1e-6
        ? new XYZ(direction.X, direction.Y, 0).Normalize()
        : XYZ.BasisZ.CrossProduct(sketchOn.Normal).Normalize();
    origin = sketchOn.BubbleEnd;
    planeWords = "reference plane \"" + ownName(sketchOn) + "\"";
}

// ---- the roof ----------------------------------------------------------------

if (refused == null)
{
    var normal = sketchOn.Normal;
    var eastward = normal.X > 1e-6 || (Math.Abs(normal.X) <= 1e-6 && normal.Y > 0);
    var sign = eastward ? 1.0 : -1.0;
    var sideWords = Math.Abs(normal.X) > 1e-6 ? "east" : "north";
    var startFeet = sign * extrusionStartMm / MillimetresPerFoot;
    var endFeet = sign * extrusionEndMm / MillimetresPerFoot;

    Func<double[], XYZ> at = p => new XYZ(origin.X + p[0] * along.X, origin.Y + p[0] * along.Y,
        host.ProjectElevation + p[1]);
    var curves = new CurveArray();
    for (var i = 0; i + 1 < points.Count; i++)
        curves.Append(through[i] == null
            ? (Curve)Line.CreateBound(at(points[i]), at(points[i + 1]))
            : Arc.Create(at(points[i]), at(points[i + 1]), at(through[i])));

    ExtrusionRoof made;
    try
    {
        made = doc.Create.NewExtrusionRoof(curves, sketchOn, host, type, startFeet, endFeet);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the roof profile on " + planeWords + ": " + ex.Message
            + " NOTHING from this call was kept.");
    }
    if (made == null)
        throw new InvalidOperationException("Revit made no roof from the profile on " + planeWords + " and gave no "
            + "reason - the usual cause is a profile that crosses itself. NOTHING from this call was kept.");
    doc.Regenerate();

    // READ BACK, off Revit.
    var startParameter = made.get_Parameter(BuiltInParameter.EXTRUSION_START_PARAM);
    var endParameter = made.get_Parameter(BuiltInParameter.EXTRUSION_END_PARAM);
    if (startParameter == null || endParameter == null)
        throw new InvalidOperationException("The new roof has no Extrusion Start or End to read back. NOTHING from "
            + "this call was kept.");
    var gotStart = startParameter.AsDouble();
    var gotEnd = endParameter.AsDouble();
    var extentsHold = (Math.Abs(gotStart - startFeet) <= halfMillimetre && Math.Abs(gotEnd - endFeet) <= halfMillimetre)
        || (Math.Abs(gotStart - endFeet) <= halfMillimetre && Math.Abs(gotEnd - startFeet) <= halfMillimetre);
    var gotType = doc.GetElement(made.GetTypeId()) as RoofType;
    var profileBack = made.GetProfile();
    var edgesBack = profileBack == null ? 0 : profileBack.Size;
    if (!extentsHold || gotType == null || gotType.Id != type.Id || made.LevelId != host.Id
        || edgesBack != points.Count - 1)
        throw new InvalidOperationException("The roof did not read back as asked: Extrusion Start " + mm(gotStart)
            + " and End " + mm(gotEnd) + " mm (asked " + mm(startFeet) + " and " + mm(endFeet) + " along the plane's "
            + "normal), type " + (gotType == null ? "none" : "\"" + typeLabel(gotType) + "\"") + ", "
            + edgesBack + " profile edge(s) of " + (points.Count - 1) + (made.LevelId != host.Id ? ", another level" : "")
            + ". NOTHING from this call was kept.");

    roof = made.Id;
    var box = made.get_BoundingBox(null);
    var top = box == null ? "its top NOT read back - Revit gave no bounding box"
        : "its top " + mm(box.Max.Z - host.ProjectElevation) + " mm above \"" + host.Name + "\"";

    report = "Roof by extrusion made (id " + made.Id.ToString() + "), type \"" + typeLabel(gotType) + "\", level \""
        + host.Name + "\", sketched on " + planeWords + ": " + edgesBack + " edge(s)"
        + (arcs > 0 ? ", " + arcs + " of them arc(s)" : "") + ", extruded from "
        + mm(Math.Min(sign * gotStart, sign * gotEnd)) + " to " + mm(Math.Max(sign * gotStart, sign * gotEnd)) + " mm "
        + sideWords
        + " of the plane - read back as Extrusion Start " + mm(gotStart) + " and End " + mm(gotEnd)
        + " mm in Properties - " + top + ".";
    findings.Add(report);
    if (planeDrawn != ElementId.InvalidElementId)
        findings.Add("The reference plane drawn along grid " + grid.Name + " is the one the roof's profile is "
            + "sketched on, and it stays in the model with the roof.");
}

if (refused != null) findings.Add(refused);
