// NOT STANDALONE. Assumes `doc`, `view` and `room` are in scope, and leaves
// `pathCount`, `pathIds`, `report`, `refused` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHAT IT READS. The path of travel lines in one floor plan - Revit's Analyze
// > Route Analysis > Path of Travel - each one's id, its measured length in
// METRES (the sum of its own curves, read back, never a straight line between
// its ends), where it starts and ends in millimetres, how many waypoints it
// has, the room each end is in (in the plan's phase), and any door within
// 1500 mm of its end, named by its Mark. With `room`, only the paths that
// start or end in that room, by number or name.
//
// THE PLAN. Left out (null), the view in front. Paths of travel are drawn in
// floor plans only, so any other view - a ceiling plan, a section, a 3D view,
// a sheet, a schedule - and any template is refused by name.
//
// A MEASURED LENGTH, NOT A VERDICT. Whether a travel distance is acceptable is
// the engineer's or the authority's decision under their code; this says how
// long each line is and never calls one compliant or not.
//
// ONE STRING, ROWS JOINED BY "  ||  ", because a reply cuts a list to three
// entries and this is the answer. Nothing found is said in words.
//
// RELEASES (tools/api-surface --members, 2026-11-02). PathOfTravel, its
// GetCurves, GetWaypoints, PathStart and PathEnd are the same 2020 to 2027 -
// the 2020 assembly checked here already has it. Curve.Length, View.GenLevel,
// FamilyInstance and Document.GetRoomAtPoint likewise.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var pathCount = 0;
var pathIds = new List<ElementId>();
var report = "";
string refused = null;
var findings = new List<string>();

const double MillimetresPerFoot = 304.8;
const double MetresPerFoot = 0.3048;
var culture = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * MillimetresPerFoot).ToString("0", culture);
Func<XYZ, string> at = p => p == null ? "an unknown point" : mm(p.X) + "," + mm(p.Y) + " mm";

Func<View, string> viewWords = v =>
{
    switch (v.ViewType)
    {
        case ViewType.CeilingPlan: return "a ceiling plan";
        case ViewType.AreaPlan: return "an area plan";
        case ViewType.EngineeringPlan: return "a structural plan";
        case ViewType.ThreeD: return "a 3D view";
        case ViewType.Section: return "a section";
        case ViewType.Elevation: return "an elevation";
        case ViewType.DrawingSheet: return "a sheet";
        case ViewType.Schedule: return "a schedule";
        case ViewType.DraftingView: return "a drafting view";
        case ViewType.Legend: return "a legend";
        default: return "a " + v.ViewType + " view";
    }
};

// ---- the plan
var target = view ?? doc.ActiveView;
if (target == null)
    refused = "No plan was named and Revit has no view in front. Name a floor plan. NOTHING WAS READ.";
else if (target.IsTemplate)
    refused = "\"" + target.Name + "\" is a view template, not a plan. Name a floor plan. NOTHING WAS READ.";
else if (target.ViewType != ViewType.FloorPlan)
    refused = "\"" + target.Name + "\" is " + viewWords(target) + ". Paths of travel are drawn in floor plans "
        + "only - name a floor plan. NOTHING WAS READ.";

if (refused == null)
{
    // The plan's phase decides which room a point is in.
    Phase phase = null;
    try
    {
        var phaseParameter = target.get_Parameter(BuiltInParameter.VIEW_PHASE);
        if (phaseParameter != null) phase = doc.GetElement(phaseParameter.AsElementId()) as Phase;
    }
    catch (Exception) { phase = null; }

    var level = target.GenLevel;
    // A path's points can sit exactly on the level, where a room's volume
    // starts; the room is looked up a foot above it.
    Func<XYZ, Room> roomAt = p =>
    {
        if (p == null) return null;
        var probe = new XYZ(p.X, p.Y, (level != null ? level.ProjectElevation : p.Z) + 1.0);
        try { return phase != null ? doc.GetRoomAtPoint(probe, phase) : doc.GetRoomAtPoint(probe); }
        catch (Exception) { return null; }
    };
    Func<Room, string> roomWords = r => r == null ? "no room"
        : "room " + (r.Number ?? "") + " \"" + (r.Name ?? "") + "\"";

    // Doors on the plan's level, to name the one a path ends at.
    var doors = new List<FamilyInstance>();
    foreach (var element in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors)
                 .WhereElementIsNotElementType())
    {
        var door = element as FamilyInstance;
        if (door == null || !(door.Location is LocationPoint)) continue;
        if (level != null && door.LevelId != level.Id) continue;
        doors.Add(door);
    }
    Func<XYZ, string> doorNear = p =>
    {
        if (p == null) return null;
        FamilyInstance best = null;
        var bestDistance = 1500.0 / MillimetresPerFoot;
        foreach (var door in doors)
        {
            var q = ((LocationPoint)door.Location).Point;
            var d = new XYZ(q.X - p.X, q.Y - p.Y, 0).GetLength();
            if (d <= bestDistance) { bestDistance = d; best = door; }
        }
        if (best == null) return null;
        var markParameter = best.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
        var mark = markParameter == null ? "" : (markParameter.AsString() ?? "");
        return "door " + (mark == "" ? "with no Mark" : "\"" + mark + "\"") + " (" + best.Name + ", "
            + mm(bestDistance) + " mm away)";
    };

    var wanted = (room ?? "").Trim();
    Func<Room, bool> isWanted = r => r != null
        && (string.Equals((r.Number ?? "").Trim(), wanted, StringComparison.OrdinalIgnoreCase)
            || string.Equals((r.Name ?? "").Trim(), wanted, StringComparison.OrdinalIgnoreCase));

    var rows = new List<string>();
    var all = 0;
    double longest = 0;
    foreach (var element in new FilteredElementCollector(doc, target.Id).OfClass(typeof(PathOfTravel)))
    {
        var path = element as PathOfTravel;
        if (path == null) continue;
        all++;

        XYZ start = null, end = null;
        try { start = path.PathStart; end = path.PathEnd; } catch (Exception) { }
        var fromRoom = roomAt(start);
        var toRoom = roomAt(end);
        if (wanted != "" && !isWanted(fromRoom) && !isWanted(toRoom)) continue;

        double feet = 0;
        var measured = true;
        try
        {
            foreach (var curve in path.GetCurves())
                if (curve != null) feet += curve.Length;
        }
        catch (Exception) { measured = false; }

        var waypoints = 0;
        try { waypoints = path.GetWaypoints().Count; } catch (Exception) { }

        pathCount++;
        pathIds.Add(path.Id);
        if (measured && feet > longest) longest = feet;

        var door = doorNear(end);
        rows.Add("path " + path.Id + ": "
            + (measured ? (feet * MetresPerFoot).ToString("0.00", culture) + " m" : "length could not be read")
            + ", from " + at(start) + " in " + roomWords(fromRoom)
            + " to " + at(end) + " in " + roomWords(toRoom)
            + (door != null ? ", at " + door : "")
            + (waypoints > 0 ? ", " + waypoints + " waypoint(s)" : ""));
    }

    if (pathCount == 0)
    {
        if (all == 0)
            report = "\"" + target.Name + "\" has no path of travel lines. CREATE_PATHS_OF_TRAVEL draws them, from "
                + "rooms or points to a named exit door or point.";
        else
            report = "\"" + target.Name + "\" has " + all + " path(s) of travel, and none starts or ends in a room "
                + "numbered or named \"" + wanted + "\".";
    }
    else
    {
        report = pathCount + " path(s) of travel in \"" + target.Name + "\""
            + (wanted != "" ? " starting or ending in \"" + wanted + "\" (of " + all + ")" : "")
            + ", the longest " + (longest * MetresPerFoot).ToString("0.00", culture) + " m: "
            + string.Join("  ||  ", rows);
    }

    findings.Add("Lengths are measured along each path's own lines, in metres. A measured length is not a "
        + "verdict - whether a travel distance is acceptable is for the engineer or the authority to decide.");
    if (phase == null)
        findings.Add("The plan's phase could not be read, so rooms were looked up in the last phase.");
}
