// NOT STANDALONE. Assumes `doc`, `view`, `datum`, `planes`, `spacingMm` and
// `count` are in scope, and leaves `created`, `skipped`, `report`, `refused`
// and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// WHAT IT DOES. Revit's Architecture > Work Plane > Ref Plane, in a PROJECT:
// reference planes set out from ONE datum the modeller names - a grid or a
// level, never guessed (D-33). With no datum, or one no grid or level has,
// nothing is made and the project's grids and levels are listed.
//
//   A GRID: vertical planes parallel to it, drawn in a PLAN. A plane at 0 lies
//   along the grid. A positive distance is to the east of a grid - the plan's
//   own +X - or to the north (+Y) of a grid running exactly east-west; a
//   skewed grid's positive side is whichever of its two sides points more to
//   the east. An arc grid is refused by name.
//   A LEVEL: horizontal planes that distance above it (below when negative),
//   drawn in a SECTION or ELEVATION - a horizontal plane cannot be drawn in
//   plan. Each runs 10 m either side of the view's origin, along the view.
//
// THE VIEW. Left out (null), the view in front. A template, or a kind of view
// the datum's planes cannot be drawn in - a sheet, a schedule, a 3D view, a
// plan for a level, a section for a grid - is refused by name before
// anything is made.
//
// `planes` is "name=mm" entries with ";" between - "Ceiling Datum=2700" - and
// an entry with no name and no "=" is an unnamed plane at that distance.
// `spacingMm` with `count` adds `count` unnamed planes at that spacing from
// the datum: 600 and 5 are planes at 600, 1200, 1800, 2400 and 3000.
//
// SKIPPED AND NAMED, NEVER MADE TWICE. A name a reference plane in the project
// already has, a name given twice in the call, and a plane where a reference
// plane already lies (same facing, within 0.01 mm) are each skipped and named
// in `skipped`; the rest are still made. Nothing that was there is moved,
// renamed or deleted.
//
// READ BACK. Each plane made is read again - which way it faces, how far it
// lies from the datum, its name off its own Name parameter - and one that
// does not read back as asked THROWS: the host rolls the whole call back and
// nothing is kept.
//
// A PLANE'S NAME IS READ FROM ITS NAME PARAMETER (DATUM_TEXT), as
// CREATE_REFERENCE_PLANES reads it: `ReferencePlane.Name` gives Revit's own
// label for a plane nobody named.
//
// MILLIMETRES IN, / 304.8 (D-20). An ElementId is never read as a number.
//
// RELEASES (tools/api-surface --members, 2026-11-02): ItemFactoryBase.
// NewReferencePlane(XYZ, XYZ, XYZ, View), ReferencePlane.Name / Normal /
// GetPlane, Grid.Curve / IsCurved, Level.ProjectElevation, View.ViewType /
// IsTemplate / GenLevel / RightDirection / ViewDirection / Origin,
// Document.Create / ActiveView / IsFamilyDocument and BuiltInParameter.
// DATUM_TEXT read the same 2020 through 2027.

var created = new List<ElementId>();
var skipped = new List<string>();
var report = "";
string refused = null;
var findings = new List<string>();

var culture = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString("0.#", culture);
var tolerance = 0.01 / 304.8;

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};
Func<string, string> label = name => string.IsNullOrEmpty(name) ? "an unnamed plane" : "\"" + name + "\"";

var grids = new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>().ToList();
var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
    .OrderBy(l => l.ProjectElevation).ToList();
Func<string> datumList = () =>
    " Grids in this project: " + (grids.Count == 0 ? "none" : string.Join(", ", grids.Select(g => g.Name).OrderBy(n => n)))
    + ". Levels: " + (levels.Count == 0 ? "none" : string.Join(", ", levels.Select(l => l.Name))) + ".";

// What was asked: (name, millimetres), in order.
var wanted = new List<KeyValuePair<string, double>>();
var problems = new List<string>();

foreach (var raw in (planes ?? "").Split(';'))
{
    var entry = raw.Trim();
    if (entry.Length == 0) continue;
    var cut = entry.LastIndexOf('=');
    var name = cut < 0 ? "" : entry.Substring(0, cut).Trim();
    var number = (cut < 0 ? entry : entry.Substring(cut + 1)).Trim();
    if (number.EndsWith("mm", StringComparison.OrdinalIgnoreCase)) number = number.Substring(0, number.Length - 2).Trim();
    double value;
    if (!double.TryParse(number, System.Globalization.NumberStyles.Float, culture, out value))
        problems.Add("\"" + entry + "\" is not a name=millimetres pair or a distance in millimetres.");
    else
        wanted.Add(new KeyValuePair<string, double>(name, value));
}

if (count > 0 || spacingMm != 0)
{
    if (count <= 0 || spacingMm <= 0)
        problems.Add("A spacing needs both a spacing in millimetres more than 0 and a count of planes more than 0 - "
            + "\"600 mm, 5 planes\".");
    else if (count > 500)
        problems.Add(count + " planes is more than the 500 one call makes.");
    else
        for (var i = 1; i <= count; i++) wanted.Add(new KeyValuePair<string, double>("", spacingMm * i));
}

var target = view ?? doc.ActiveView;
Grid grid = null;
Level level = null;

if (doc.IsFamilyDocument)
{
    refused = "The document in front is a family. Reference planes in a family are CREATE_REFERENCE_PLANES' "
        + "job - this one draws them in a project. NOTHING WAS MADE.";
}
else if (problems.Count > 0)
{
    refused = string.Join(" ", problems) + " NOTHING WAS MADE.";
}
else if (wanted.Count == 0)
{
    refused = "No planes were asked for - give name=millimetres pairs from the datum (\"Ceiling Datum=2700\"), "
        + "a distance (\"0\" for a plane along a grid), or a spacing and a count. NOTHING WAS MADE.";
}
else
{
    var said = (datum ?? "").Trim();
    var gridHits = grids.Where(g => string.Equals(g.Name, said, StringComparison.OrdinalIgnoreCase)).ToList();
    var levelHits = levels.Where(l => string.Equals(l.Name, said, StringComparison.OrdinalIgnoreCase)).ToList();

    if (said.Length == 0)
        refused = "No grid or level was named to set the planes out from, and it is never guessed. NOTHING WAS MADE."
            + datumList();
    else if (gridHits.Count + levelHits.Count == 0)
        refused = "No grid or level in this project is named \"" + said + "\". NOTHING WAS MADE." + datumList();
    else if (gridHits.Count > 0 && levelHits.Count > 0)
        refused = "\"" + said + "\" is the name of a grid and of a level - say which. NOTHING WAS MADE.";
    else if (gridHits.Count > 1 || levelHits.Count > 1)
        refused = "More than one " + (gridHits.Count > 1 ? "grid" : "level") + " is named \"" + said
            + "\" - say which. NOTHING WAS MADE.";
    else if (target == null)
        refused = "No view was named and none is in front. NOTHING WAS MADE.";
    else if (target.IsTemplate)
        refused = "\"" + target.Name + "\" is a view template, not a view a plane can be drawn in. NOTHING WAS MADE.";
    else if (gridHits.Count == 1)
    {
        grid = gridHits[0];
        var plan = target.ViewType == ViewType.FloorPlan || target.ViewType == ViewType.CeilingPlan
            || target.ViewType == ViewType.EngineeringPlan || target.ViewType == ViewType.AreaPlan;
        if (grid.IsCurved || !(grid.Curve is Line))
            refused = "Grid \"" + grid.Name + "\" is an arc. This draws planes along straight grids only. NOTHING WAS MADE.";
        else if (!plan)
            refused = "\"" + target.Name + "\" is a " + target.ViewType + " view. A reference plane along a grid is "
                + "drawn in a plan - name a floor, ceiling, structural or area plan. NOTHING WAS MADE.";
    }
    else
    {
        level = levelHits[0];
        var upright = target.ViewType == ViewType.Section || target.ViewType == ViewType.Elevation
            || target.ViewType == ViewType.Detail;
        if (!upright || Math.Abs(target.ViewDirection.Z) > 1e-6)
        {
            var some = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
                .Where(v => !v.IsTemplate && (v.ViewType == ViewType.Section || v.ViewType == ViewType.Elevation))
                .Select(v => v.Name).OrderBy(n => n).Take(10).ToList();
            refused = "\"" + target.Name + "\" is a " + target.ViewType + " view. A plane at a height above a level "
                + "is horizontal and is drawn in a section or elevation - name one"
                + (some.Count == 0 ? "; this project has none." : ": " + string.Join(", ", some) + ".")
                + " NOTHING WAS MADE.";
        }
    }
}

if (refused == null)
{
    // The datum's geometry: the plane every distance is measured from, its
    // facing, and how to draw a plane at a distance from it.
    XYZ facing, start = null, end = null, centre = null, along = null;
    double baseline;
    string datumWords;

    if (grid != null)
    {
        var line = (Line)grid.Curve;
        var z = target.GenLevel != null ? target.GenLevel.ProjectElevation : line.GetEndPoint(0).Z;
        start = new XYZ(line.GetEndPoint(0).X, line.GetEndPoint(0).Y, z);
        end = new XYZ(line.GetEndPoint(1).X, line.GetEndPoint(1).Y, z);
        facing = XYZ.BasisZ.CrossProduct((end - start).Normalize());
        if (facing.X < -1e-6 || (Math.Abs(facing.X) <= 1e-6 && facing.Y < 0)) facing = facing.Negate();
        baseline = start.DotProduct(facing);
        datumWords = "grid " + grid.Name;
    }
    else
    {
        facing = XYZ.BasisZ;
        baseline = level.ProjectElevation;
        centre = target.Origin;
        along = new XYZ(target.RightDirection.X, target.RightDirection.Y, 0).Normalize();
        datumWords = "level \"" + level.Name + "\"";
    }

    Func<double, string> sideWords = feet =>
    {
        if (level != null)
            return Math.Abs(feet) < tolerance ? "on " + datumWords
                : mm(Math.Abs(feet)) + " mm " + (feet > 0 ? "above " : "below ") + datumWords
                  + " (elevation " + mm(baseline + feet) + " mm)";
        if (Math.Abs(feet) < tolerance) return "along " + datumWords;
        var east = Math.Abs(facing.X) > 1e-6;
        return mm(Math.Abs(feet)) + " mm " + (east ? (feet > 0 ? "east" : "west") : (feet > 0 ? "north" : "south"))
            + " of " + datumWords;
    };

    var existing = new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>().ToList();
    var namesHere = new HashSet<string>(existing.Select(ownName).Where(n => n.Length > 0), StringComparer.OrdinalIgnoreCase);
    var placedAt = new List<double>();
    var rows = new List<string>();

    foreach (var pair in wanted)
    {
        var name = pair.Key;
        var offset = pair.Value / 304.8;
        var level0 = baseline + offset;

        if (name.Length > 0 && namesHere.Contains(name))
        {
            skipped.Add(label(name) + " - a reference plane in this project already has that name; not made");
            continue;
        }

        var lying = existing.FirstOrDefault(rp =>
            Math.Abs(Math.Abs(rp.Normal.DotProduct(facing)) - 1) < 1e-9
            && Math.Abs(rp.GetPlane().Origin.DotProduct(facing) - level0) < tolerance);
        if (lying != null || placedAt.Any(p => Math.Abs(p - level0) < tolerance))
        {
            skipped.Add(label(name) + " " + sideWords(offset) + " - " + (lying == null ? "asked for twice in this call"
                : "reference plane " + label(ownName(lying)) + " already lies there") + "; not made");
            continue;
        }

        XYZ bubble, free, cutVector;
        if (grid != null)
        {
            bubble = start + facing.Multiply(offset);
            free = end + facing.Multiply(offset);
            cutVector = XYZ.BasisZ;
        }
        else
        {
            var half = 10000 / 304.8;
            var middle = new XYZ(centre.X, centre.Y, level0);
            bubble = middle - along.Multiply(half);
            free = middle + along.Multiply(half);
            cutVector = target.ViewDirection;
        }

        ReferencePlane made;
        try
        {
            made = doc.Create.NewReferencePlane(bubble, free, cutVector, target);
            if (name.Length > 0) made.Name = name;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused the reference plane " + label(name) + " "
                + sideWords(offset) + ": " + ex.Message + " NOTHING from this call was kept.");
        }

        // READ BACK - facing, distance from the datum and name.
        var normal = made.Normal;
        var sits = made.GetPlane().Origin.DotProduct(facing) - baseline;
        var nameNow = ownName(made);
        if (Math.Abs(Math.Abs(normal.DotProduct(facing)) - 1) > 1e-6 || Math.Abs(sits - offset) > tolerance
            || !string.Equals(nameNow, name, StringComparison.Ordinal))
            throw new InvalidOperationException("The reference plane " + label(name) + " came out facing ("
                + Math.Round(normal.X, 3).ToString(culture) + ", " + Math.Round(normal.Y, 3).ToString(culture) + ", "
                + Math.Round(normal.Z, 3).ToString(culture) + ") " + sideWords(sits) + ", named " + label(nameNow)
                + " - not what was asked for. NOTHING from this call was kept.");

        created.Add(made.Id);
        placedAt.Add(level0);
        if (name.Length > 0) namesHere.Add(name);
        rows.Add(label(nameNow) + " " + sideWords(sits));
    }

    report = "Made " + created.Count + " of " + wanted.Count + " reference plane(s) in \"" + target.Name + "\" from "
        + datumWords + ", each read back: " + (rows.Count == 0 ? "none" : string.Join(" || ", rows))
        + (skipped.Count == 0 ? "" : " || SKIPPED: " + string.Join(" || ", skipped));
    findings.Add(report);
    if (created.Count > 0)
        findings.Add("Their lines run " + (grid != null ? "the length of the grid" : "10 m either side of the view's "
            + "middle") + "; SET_REFERENCE_PLANE_LENGTH sets them to one length.");
}

if (refused != null) findings.Add(refused);
