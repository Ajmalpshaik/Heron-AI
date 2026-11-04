// NOT STANDALONE. Assumes `doc`, `mode`, `pointsMm`, `level`, `typeName` and
// `host` are in scope; leaves `resultIds`, `report`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION and never opens one (Golden Rule 16). One call
// is one undo entry.
//
// THE SITE REVIT HAD BEFORE TOPOSOLIDS - Massing & Site on Revit 2020 to 2023.
// One run makes one thing, chosen by `mode`:
//
//   toposurface  a TopographySurface through `pointsMm` "x,y,z; x,y,z; ..." -
//                three or more points with heights, millimetres above the
//                project's internal origin, as typed from a survey sheet
//                already reduced to the project (CREATE_TOPOSOLID's rule)
//   pad          a BuildingPad of the type `typeName` at the level `level`,
//                inside the corners `pointsMm` "x,y; x,y; ...", closed
//                automatically. Revit cuts it into the toposurface under it
//   subregion    a SiteSubRegion on the toposurface `host` (an id), inside
//                the corners `pointsMm` "x,y; x,y; ...", closed automatically
//                - a car park, a path, a planted bed
//
// NEVER GUESSED (D-33). A level, a pad type or a host that is not named is
// taken only when the model has exactly one; otherwise the run is refused with
// the list to choose from. Nothing present is changed.
//
// RELEASES. TopographySurface.Create(Document, IList<XYZ>), SiteSubRegion
// .Create(Document, IList<CurveLoop>, ElementId) and BuildingPad.Create
// (Document, ElementId, ElementId, IList<CurveLoop>) are present 2020 to 2027,
// and the first two are marked obsolete from 2024, when toposolids replaced the
// toposurface (tools/api-surface --members, 2026-11-02). The card claims 2020
// to 2023; on a later release it refuses and names the toposolid's words.
//
// READ BACK: the new element looked up again - a toposurface's point count,
// heights and areas, a pad's level, host toposurface, corners and area, a
// subregion found among its host's subregions and its area.
//
// `absent: none` arrives as the text "none" when left out (FRAGMENT-ISSUES row
// 5b-1432). AN ElementId IS COMPARED WITH == OR AS TEXT, NEVER READ AS A NUMBER.

const double MmPerFoot = 304.8;
const double SquareMetresPerSquareFoot = 0.09290304;
var resultIds = new List<ElementId>();
var report = new List<string>();
var findings = new List<string>();
string refused = null;

var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;
Func<double, string> num = v => Math.Round(v, 1).ToString("0.#", culture);
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
Func<string, string> given = s =>
{
    var t = (s ?? "").Trim();
    return string.Equals(t, "none", StringComparison.OrdinalIgnoreCase) ? "" : t;
};
// "x,y,z; ..." (three numbers each) or "x,y; ..." (two), millimetres to feet.
Func<string, int, List<XYZ>> read = (text, count) =>
{
    var list = new List<XYZ>();
    foreach (var chunk in (text ?? "").Split(';'))
    {
        if (chunk.Trim() == "") continue;
        var parts = chunk.Split(',');
        if (parts.Length != count) return null;
        var values = new double[3];
        for (var i = 0; i < count; i++)
            if (!double.TryParse(parts[i].Trim(), numberStyle, culture, out values[i])) return null;
        list.Add(new XYZ(values[0] / MmPerFoot, values[1] / MmPerFoot, values[2] / MmPerFoot));
    }
    return list;
};
Func<Element, BuiltInParameter, string> areaOf = (e, which) =>
{
    var p = e == null ? null : e.get_Parameter(which);
    return p == null || !p.HasValue ? "not read" : num(p.AsDouble() * SquareMetresPerSquareFoot) + " m2";
};
// The outline closed automatically, flat at one height.
Func<List<XYZ>, double, CurveLoop> outline = (corners, z) =>
{
    var loop = new CurveLoop();
    for (var i = 0; i < corners.Count; i++)
    {
        var a = corners[i];
        var b = corners[(i + 1) % corners.Count];
        loop.Append(Line.CreateBound(new XYZ(a.X, a.Y, z), new XYZ(b.X, b.Y, z)));
    }
    return loop;
};

var how = given(mode).ToLowerInvariant().Replace(" ", "").Replace("-", "");
if (how == "topography" || how == "topo" || how == "surface") how = "toposurface";
if (how == "buildingpad") how = "pad";

var release = 0;
int.TryParse(doc.Application.VersionNumber, out release);
var surfaces = new FilteredElementCollector(doc).OfClass(typeof(TopographySurface)).Cast<TopographySurface>()
    .Where(t => !t.IsSiteSubRegion).OrderBy(t => t.Id.ToString()).ToList();
Func<TopographySurface, string> surfaceLine = t => "toposurface " + t.Id.ToString() + " (" + t.GetPoints().Count + " points)";

if (release >= 2024)
    refused = "Revit " + release + " makes terrain as toposolids, not toposurfaces: ask to \"make a toposolid from these survey points\", "
        + "or to \"add a car park area to the terrain\" for a subdivision, or to \"cut the terrain with the building pad\". NOTHING WAS MADE.";
else if (how != "toposurface" && how != "pad" && how != "subregion")
    refused = "Say what to make: toposurface, pad or subregion - " + quoted(given(mode)) + " is none of them. NOTHING WAS MADE.";

// ---- toposurface -------------------------------------------------------------
if (refused == null && how == "toposurface")
{
    var points = read(pointsMm, 3);
    if (points == null) refused = "Each toposurface point is three numbers in millimetres, x,y,z, separated by semicolons. NOTHING WAS MADE.";
    else if (points.Count < 3) refused = "A toposurface needs three points or more; " + points.Count + " given. NOTHING WAS MADE.";
    else if (!TopographySurface.ArePointsDistinct(points)) refused = "Two of the points are at the same x,y - a toposurface takes one height at each place. NOTHING WAS MADE.";
    else
    {
        try
        {
            var made = TopographySurface.Create(doc, points);
            doc.Regenerate();
            var back = made == null ? null : doc.GetElement(made.Id) as TopographySurface;
            if (back == null) refused = "Revit returned no toposurface. NOTHING WAS MADE.";
            else
            {
                resultIds.Add(back.Id);
                var surfacePoints = back.GetPoints();
                report.Add("toposurface " + back.Id.ToString() + ": " + surfacePoints.Count + " point(s), " + back.GetBoundaryPoints().Count
                    + " on its edge, heights " + num(surfacePoints.Min(p => p.Z) * MmPerFoot) + " to " + num(surfacePoints.Max(p => p.Z) * MmPerFoot)
                    + " mm, projected area " + areaOf(back, BuiltInParameter.PROJECTED_SURFACE_AREA)
                    + ", surface area " + areaOf(back, BuiltInParameter.SURFACE_AREA) + ".");
                findings.Add("Made a toposurface from " + points.Count + " point(s), read back with " + surfacePoints.Count + ".");
            }
        }
        catch (Exception ex) { refused = "Revit refused the toposurface: " + ex.Message + " NOTHING WAS MADE."; }
    }
}

// ---- pad and subregion: the outline first ------------------------------------
List<XYZ> corners = null;
if (refused == null && (how == "pad" || how == "subregion"))
{
    corners = read(pointsMm, 2);
    if (corners == null) refused = "Each corner is two numbers in millimetres, x,y, separated by semicolons. NOTHING WAS MADE.";
    else if (corners.Count < 3) refused = "An outline needs three corners or more; " + corners.Count + " given. NOTHING WAS MADE.";
    else
        for (var i = 0; i < corners.Count && refused == null; i++)
            if (corners[i].DistanceTo(corners[(i + 1) % corners.Count]) < 1.0 / MmPerFoot)
                refused = "Corners " + (i + 1) + " and " + ((i + 1) % corners.Count + 1) + " are at the same place. NOTHING WAS MADE.";
    if (refused == null && surfaces.Count == 0)
        refused = "This model has no toposurface for the " + (how == "pad" ? "building pad to cut into" : "subregion to sit on")
            + " - make one first, \"make a toposurface from these points\". NOTHING WAS MADE.";
}

// ---- pad ---------------------------------------------------------------------
if (refused == null && how == "pad")
{
    var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.ProjectElevation).ToList();
    var levelList = string.Join(", ", levels.Select(l => quoted(l.Name) + " at " + num(l.ProjectElevation * MmPerFoot) + " mm"));
    var wantLevel = given(level);
    Level onLevel = null;
    if (wantLevel == "" && levels.Count == 1) onLevel = levels[0];
    else onLevel = levels.FirstOrDefault(l => string.Equals(l.Name, wantLevel, StringComparison.OrdinalIgnoreCase));

    var types = new FilteredElementCollector(doc).OfClass(typeof(BuildingPadType)).Cast<BuildingPadType>().OrderBy(t => t.Name).ToList();
    var typeList = string.Join(", ", types.Select(t => quoted(t.Name)));
    var wantType = given(typeName);
    BuildingPadType padType = null;
    if (wantType == "" && types.Count == 1) padType = types[0];
    else padType = types.FirstOrDefault(t => string.Equals(t.Name, wantType, StringComparison.OrdinalIgnoreCase));

    if (onLevel == null)
        refused = (wantLevel == "" ? "Name the level for the building pad" : "No level is named " + quoted(wantLevel))
            + " - the levels here are " + levelList + ". NOTHING WAS MADE.";
    else if (types.Count == 0)
        refused = "This model has no building pad type. NOTHING WAS MADE.";
    else if (padType == null)
        refused = (wantType == "" ? "Name the building pad type" : "No building pad type is named " + quoted(wantType))
            + " - the types here are " + typeList + ". NOTHING WAS MADE.";
    else
    {
        try
        {
            // The outline lies at the level's height above the internal origin.
            var loop = outline(corners, onLevel.ProjectElevation);
            var made = BuildingPad.Create(doc, padType.Id, onLevel.Id, new List<CurveLoop> { loop });
            doc.Regenerate();
            var back = made == null ? null : doc.GetElement(made.Id) as BuildingPad;
            if (back == null) refused = "Revit returned no building pad. NOTHING WAS MADE.";
            else
            {
                resultIds.Add(back.Id);
                var hostText = back.HostId == ElementId.InvalidElementId ? "in no toposurface" : "cut into toposurface " + back.HostId.ToString();
                var offset = back.get_Parameter(BuiltInParameter.BUILDINGPAD_HEIGHTABOVELEVEL_PARAM);
                var vertices = back.GetBoundary().Sum(l => l.Count());
                report.Add("building pad " + back.Id.ToString() + " of type " + quoted(padType.Name) + " on " + quoted(onLevel.Name)
                    + (offset != null && offset.HasValue ? ", " + num(offset.AsDouble() * MmPerFoot) + " mm above it" : "")
                    + ", " + hostText + ", " + back.GetBoundary().Count + " loop(s) of " + vertices + " edge(s), area "
                    + areaOf(back, BuiltInParameter.HOST_AREA_COMPUTED) + ".");
                findings.Add("Made a building pad from " + corners.Count + " corner(s), read back.");
                if (back.HostId == ElementId.InvalidElementId)
                    findings.Add("Revit put the pad in no toposurface - its outline may lie outside every toposurface ("
                        + string.Join(", ", surfaces.Select(surfaceLine)) + ").");
            }
        }
        catch (Exception ex) { refused = "Revit refused the building pad: " + ex.Message + " NOTHING WAS MADE."; }
    }
}

// ---- subregion ---------------------------------------------------------------
if (refused == null && how == "subregion")
{
    var wantHost = given(host);
    TopographySurface onSurface = null;
    if (wantHost == "" && surfaces.Count == 1) onSurface = surfaces[0];
    else onSurface = surfaces.FirstOrDefault(t => t.Id.ToString() == wantHost || t.UniqueId == wantHost);
    var surfaceList = string.Join(", ", surfaces.Select(surfaceLine));
    var loops = new List<CurveLoop> { outline(corners, 0.0) };

    if (onSurface == null)
        refused = (wantHost == "" ? "Name the toposurface for the subregion" : "No toposurface has the id " + quoted(wantHost))
            + " - the toposurfaces here are " + surfaceList + ". NOTHING WAS MADE.";
    else if (!SiteSubRegion.IsValidBoundary(loops))
        refused = "Revit says this outline is not a subregion boundary - it may cross itself. NOTHING WAS MADE.";
    else
    {
        try
        {
            var before = new HashSet<string>(onSurface.GetHostedSubRegionIds().Select(i => i.ToString()));
            var made = SiteSubRegion.Create(doc, loops, onSurface.Id);
            doc.Regenerate();
            var element = made == null ? null : made.TopographySurface;
            var back = element == null ? null : doc.GetElement(element.Id) as TopographySurface;
            var hosted = onSurface.GetHostedSubRegionIds().Select(i => i.ToString()).ToList();
            if (back == null) refused = "Revit returned no subregion. NOTHING WAS MADE.";
            else
            {
                resultIds.Add(back.Id);
                var listed = hosted.Contains(back.Id.ToString()) && !before.Contains(back.Id.ToString());
                report.Add("subregion " + back.Id.ToString() + " on toposurface " + onSurface.Id.ToString() + ", "
                    + made.GetBoundary().Sum(l => l.Count()) + " edge(s), projected area "
                    + areaOf(back, BuiltInParameter.PROJECTED_SURFACE_AREA) + ", "
                    + (listed ? "listed among the toposurface's subregions" : "NOT found among the toposurface's subregions") + ".");
                findings.Add("Made a subregion from " + corners.Count + " corner(s), read back; the toposurface has "
                    + hosted.Count + " subregion(s) now, " + before.Count + " before.");
            }
        }
        catch (Exception ex) { refused = "Revit refused the subregion: " + ex.Message + " NOTHING WAS MADE."; }
    }
}

if (refused != null) { findings.Add(refused); resultIds.Clear(); }
