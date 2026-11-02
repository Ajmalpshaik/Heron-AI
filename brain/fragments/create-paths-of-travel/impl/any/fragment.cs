// NOT STANDALONE. Assumes `doc`, `view`, `rooms`, `fromPoints`, `toDoor` and
// `toPoint` are in scope, and leaves `created`, `notFound`, `report`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// WHAT IT DOES. Revit's Analyze > Route Analysis > Path of Travel: one path
// of travel line in a floor plan from each start to ONE exit, found by
// Revit around the walls and furniture the plan shows. The starts are rooms
// - by number or name, separated by ";", or "all" for every placed room on
// the plan's level, each from its room location point - and/or points
// "x,y; x,y" in millimetres. The exit is a door named by its Mark, ended at
// the door's location point, or a point "x,y" in millimetres.
//
// THE EXIT IS NEVER GUESSED (D-33). With no door and no point, or both, or a
// Mark no door on the level has, or one two doors share, nothing is drawn and
// the doors on the plan's level are listed with their Marks so the modeller
// can name one. "The nearest exit" is the modeller's to name.
//
// THE PLAN. Left out (null), the view in front. Revit draws paths of travel
// in floor plans only, so any other view and any template is refused by name
// before anything is drawn.
//
// NEVER DELETES. A path already in the plan from the same start to the same
// end (within 10 mm) is named and not drawn twice; nothing that was there is
// moved or removed.
//
// READ BACK, AFTER A REGENERATE: each path looked up again by id, its length
// in METRES as the sum of its own curves, its start and end in mm. A start
// Revit found no path from is named in `notFound` with Revit's own status -
// NoPathOfTravel, PointOutsideActiveCrop, StartAndEndPointsTooClose and the
// rest - and the others are still drawn.
//
// A MEASURED LENGTH, NOT A VERDICT. Whether a travel distance is acceptable is
// the engineer's or the authority's decision; this never calls one compliant.
//
// MILLIMETRES IN, / 304.8 (D-20); metres out, * 0.3048.
//
// RELEASES (tools/api-surface --members, 2026-11-02). PathOfTravel.Create(
// View, XYZ, XYZ, out PathOfTravelCalculationStatus), GetCurves, PathStart
// and PathEnd are the same 2020 to 2027 - the 2020 assembly checked here
// already has them. PathOfTravelCalculationStatus gained two values in 2021;
// the status is reported by its name, so neither is named in the code.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var created = new List<ElementId>();
var notFound = new List<string>();
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
Level level = null;
if (target == null)
    refused = "No plan was named and Revit has no view in front. Name a floor plan. NOTHING WAS DRAWN.";
else if (target.IsTemplate)
    refused = "\"" + target.Name + "\" is a view template, not a plan. Name a floor plan. NOTHING WAS DRAWN.";
else if (target.ViewType != ViewType.FloorPlan)
    refused = "\"" + target.Name + "\" is " + viewWords(target) + ". Revit draws paths of travel in floor plans "
        + "only - name a floor plan. NOTHING WAS DRAWN.";
else
{
    level = target.GenLevel;
    if (level == null)
        refused = "\"" + target.Name + "\" has no level, so its rooms and doors cannot be found. NOTHING WAS DRAWN.";
}
var z = level != null ? level.ProjectElevation : 0.0;

// "x,y; x,y" in millimetres -> points on the plan's level, in feet.
Func<string, List<XYZ>> parsePoints = text =>
{
    var list = new List<XYZ>();
    foreach (var piece in (text ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var parts = piece.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        double x, y;
        if (parts.Length != 2
            || !double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, culture, out x)
            || !double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, culture, out y))
            return null;
        list.Add(new XYZ(x / MillimetresPerFoot, y / MillimetresPerFoot, z));
    }
    return list;
};

// ---- the doors on the plan's level, listed whenever the exit is not clear
var doors = new List<FamilyInstance>();
Func<FamilyInstance, string> markOf = d =>
{
    var p = d.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
    return p == null ? "" : (p.AsString() ?? "").Trim();
};
if (refused == null)
{
    foreach (var element in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors)
                 .WhereElementIsNotElementType())
    {
        var door = element as FamilyInstance;
        if (door == null || !(door.Location is LocationPoint) || door.LevelId != level.Id) continue;
        doors.Add(door);
    }
}
Func<string> doorList = () =>
{
    if (doors.Count == 0) return "No door is on level \"" + level.Name + "\".";
    var names = doors.Take(40).Select(d => (markOf(d) == "" ? "(no Mark)" : "\"" + markOf(d) + "\"") + " "
        + d.Name + " at " + at(((LocationPoint)d.Location).Point)).ToList();
    return "Doors on level \"" + level.Name + "\": " + string.Join("; ", names)
        + (doors.Count > 40 ? " and " + (doors.Count - 40) + " more." : ".");
};

// ---- the exit: one door by Mark, or one point - never guessed
XYZ exit = null;
var exitWords = "";
var doorName = (toDoor ?? "").Trim();
var exitPointText = (toPoint ?? "").Trim();
if (refused == null)
{
    if (doorName == "" && exitPointText == "")
        refused = "No exit was named. Name the exit door by its Mark, or give the exit as a point x,y in "
            + "millimetres - the exit is never guessed. NOTHING WAS DRAWN. " + doorList();
    else if (doorName != "" && exitPointText != "")
        refused = "Both an exit door and an exit point were given; name one. NOTHING WAS DRAWN.";
    else if (doorName != "")
    {
        var matches = doors.Where(d => string.Equals(markOf(d), doorName, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 0)
            refused = "No door on level \"" + level.Name + "\" has the Mark \"" + doorName + "\". NOTHING WAS DRAWN. "
                + doorList();
        else if (matches.Count > 1)
            refused = matches.Count + " doors on level \"" + level.Name + "\" have the Mark \"" + doorName
                + "\", so which is the exit is not clear. NOTHING WAS DRAWN. " + doorList();
        else
        {
            var q = ((LocationPoint)matches[0].Location).Point;
            exit = new XYZ(q.X, q.Y, z);
            exitWords = "door \"" + doorName + "\" (" + matches[0].Name + ") at " + at(exit);
        }
    }
    else
    {
        var one = parsePoints(exitPointText);
        if (one == null || one.Count != 1)
            refused = "\"" + toPoint + "\" is not one point. Give the exit as x,y in millimetres - 12000,4500. "
                + "NOTHING WAS DRAWN.";
        else
        {
            exit = one[0];
            exitWords = "the point " + at(exit);
        }
    }
}

// ---- the starts: rooms, then points
var starts = new List<XYZ>();
var startWords = new List<string>();
if (refused == null)
{
    var roomText = (rooms ?? "").Trim();
    if (roomText != "")
    {
        // Rooms in the plan's own phase only: a room of another phase is not
        // what the plan shows, and a path from it would start in thin air.
        ElementId viewPhase = ElementId.InvalidElementId;
        try
        {
            var phaseParameter = target.get_Parameter(BuiltInParameter.VIEW_PHASE);
            if (phaseParameter != null) viewPhase = phaseParameter.AsElementId();
        }
        catch (Exception) { viewPhase = ElementId.InvalidElementId; }
        Func<Room, bool> inPhase = r =>
        {
            if (viewPhase == ElementId.InvalidElementId) return true;
            var p = r.get_Parameter(BuiltInParameter.ROOM_PHASE_ID);
            return p == null || p.AsElementId() == viewPhase;
        };

        var placed = new List<Room>();
        foreach (var element in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms)
                     .WhereElementIsNotElementType())
        {
            var r = element as Room;
            if (r != null && r.LevelId == level.Id && r.Area > 0 && r.Location is LocationPoint && inPhase(r)) placed.Add(r);
        }
        var chosen = new List<Room>();
        if (string.Equals(roomText, "all", StringComparison.OrdinalIgnoreCase))
            chosen.AddRange(placed);
        else
        {
            var missing = new List<string>();
            foreach (var word in roomText.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(w => w.Trim()).Where(w => w != ""))
            {
                var hits = placed.Where(r => string.Equals((r.Number ?? "").Trim(), word, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (hits.Count == 0)
                    hits = placed.Where(r => string.Equals((r.Name ?? "").Trim(), word, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                if (hits.Count == 0) missing.Add("\"" + word + "\"");
                foreach (var hit in hits) if (!chosen.Contains(hit)) chosen.Add(hit);
            }
            if (missing.Count > 0)
                refused = "No placed room on level \"" + level.Name + "\" is numbered or named " + string.Join(", ", missing)
                    + ". NOTHING WAS DRAWN. Rooms there: "
                    + (placed.Count == 0 ? "none."
                        : string.Join(", ", placed.Take(40).Select(r => (r.Number ?? "") + " \"" + (r.Name ?? "") + "\""))
                          + (placed.Count > 40 ? " and " + (placed.Count - 40) + " more." : "."));
        }
        if (refused == null && chosen.Count == 0)
            refused = "Level \"" + level.Name + "\" has no placed room to start from. NOTHING WAS DRAWN.";
        if (refused == null)
            foreach (var r in chosen)
            {
                var p = ((LocationPoint)r.Location).Point;
                starts.Add(new XYZ(p.X, p.Y, z));
                startWords.Add("room " + (r.Number ?? "") + " \"" + (r.Name ?? "") + "\"");
            }
    }

    var pointText = (fromPoints ?? "").Trim();
    if (refused == null && pointText != "")
    {
        var points = parsePoints(pointText);
        if (points == null || points.Count == 0)
            refused = "\"" + fromPoints + "\" is not a list of points. Give them as x,y in millimetres, separated by "
                + "\";\" - 1000,2000; 5000,2000. NOTHING WAS DRAWN.";
        else
            foreach (var p in points) { starts.Add(p); startWords.Add("the point " + at(p)); }
    }

    if (refused == null && starts.Count == 0)
        refused = "No start was named. Name the rooms by number or name (\"all\" for every room on the level), or "
            + "give points x,y in millimetres. NOTHING WAS DRAWN.";
}

// ---- draw
var rows = new List<string>();
if (refused == null)
{
    // Paths already in this plan, so none is drawn twice and none is touched.
    var tolerance = 10.0 / MillimetresPerFoot;
    var existing = new List<PathOfTravel>();
    foreach (var element in new FilteredElementCollector(doc, target.Id).OfClass(typeof(PathOfTravel)))
    {
        var path = element as PathOfTravel;
        if (path != null) existing.Add(path);
    }
    Func<XYZ, XYZ, bool> same = (a, b) => a != null && b != null
        && new XYZ(a.X - b.X, a.Y - b.Y, 0).GetLength() <= tolerance;

    for (var i = 0; i < starts.Count; i++)
    {
        var start = starts[i];
        var twin = existing.FirstOrDefault(e =>
        {
            try { return same(e.PathStart, start) && same(e.PathEnd, exit); }
            catch (Exception) { return false; }
        });
        if (twin != null)
        {
            rows.Add(startWords[i] + ": already a path of travel (" + twin.Id + ") to there - not drawn twice");
            continue;
        }

        PathOfTravel made = null;
        var status = PathOfTravelCalculationStatus.Success;
        string thrown = null;
        try { made = PathOfTravel.Create(target, start, exit, out status); }
        catch (Exception ex) { thrown = ex.Message; }

        if (made == null)
            notFound.Add(startWords[i] + ": Revit found no path - "
                + (thrown != null ? "it said \"" + thrown + "\"" : "status " + status));
        else
        {
            created.Add(made.Id);
            if (status != PathOfTravelCalculationStatus.Success)
                findings.Add(startWords[i] + ": drawn, and Revit's status was " + status + ".");
        }
    }

    if (created.Count > 0) doc.Regenerate();

    // Read back by id.
    double longest = 0;
    foreach (var id in created)
    {
        var path = doc.GetElement(id) as PathOfTravel;
        if (path == null) { rows.Add("path " + id + ": not found again after drawing"); continue; }
        double feet = 0;
        var measured = true;
        try { foreach (var curve in path.GetCurves()) if (curve != null) feet += curve.Length; }
        catch (Exception) { measured = false; }
        if (measured && feet > longest) longest = feet;
        XYZ s = null, e = null;
        try { s = path.PathStart; e = path.PathEnd; } catch (Exception) { }
        var who = "";
        for (var i = 0; i < starts.Count; i++)
            if (same(starts[i], s)) { who = startWords[i] + ": "; break; }
        rows.Add(who + "path " + id + ", "
            + (measured ? (feet * MetresPerFoot).ToString("0.00", culture) + " m" : "length could not be read")
            + ", from " + at(s) + " to " + at(e));
    }

    report = "Drew " + created.Count + " of " + starts.Count + " path(s) of travel in \"" + target.Name + "\" to "
        + exitWords
        + (created.Count > 0 ? ", the longest " + (longest * MetresPerFoot).ToString("0.00", culture) + " m" : "")
        + (rows.Count > 0 ? ": " + string.Join("  ||  ", rows) : ".")
        + (notFound.Count > 0 ? "  ||  NO PATH: " + string.Join("  ||  ", notFound) : "");

    findings.Add("Lengths are measured along each path's own lines, in metres. A measured length is not a "
        + "verdict - whether a travel distance is acceptable is for the engineer or the authority to decide.");
    if (doorName != "")
        findings.Add("Each path ends at the door's location point, on the wall's line inside the opening.");
}
