// NOT STANDALONE. Assumes `doc`, `uidoc` and `spaces` are in scope, and
// leaves `layoutJson`, `spaceCount` and `findings` behind.
//
// READS ONLY. No transaction is opened and nothing is set.
//
// THE ROOMS A SPRINKLER LAYOUT IS MADE FOR (docs/47 section 3). For each Space
// or Room asked: its outline and holes in plan, its level and the level's two
// elevations, the ceilings over it, every sprinkler on its level inside its
// plan extent, and - once - every sprinkler type loaded. The brain
// (heron_sprinkler_layout.py) decides which heads are IN a room, lays it out
// and checks it; nothing here calculates.
//
// WHICH ROOMS. `spaces` = `*` is the Spaces and Rooms selected now - only when
// the model read is the one in front. Otherwise UniqueIds separated by `;` -
// how Apply reads the same rooms again whatever is selected by then. One that
// matches nothing is a finding, never a guess.
//
// THE HEADS ARE FOUND IN PLAN, NOT BY REVIT'S VOLUME TEST. IsPointInSpace and
// IsPointInRoom test a point against the room's height, and a head at the
// ceiling is often above it (NEEDS-CHECKING AJ5, FRAGMENT-ISSUES 5b-175). So
// every sprinkler on the room's level inside the room's plan extent crosses,
// and the brain tests it against the very outline the layout uses.
//
// BOTH ELEVATIONS CROSS. Level.Elevation and Level.ProjectElevation are
// described differently by two fragments here (create-level, measure-ceiling-
// height); which one a placed point's z is measured from is not yet measured.
// Each existing head's z crosses with its own Elevation from Level, so the
// read itself shows which one the model uses.
//
// NOTHING IS DEFAULTED. A value Revit does not give is null, never 0.
//
// METRES, in the model's own coordinates, to the millimetre (D-20): 1 ft =
// 0.3048 m exactly. Format 1 is defined in ONE place:
// docs/work-notes/plans/sprinkler-layout-2026-10-06.md.
//
// THE BACKSLASH AND THE QUOTE ARE BUILT FROM THEIR CHARACTER CODES, as in
// read-element-table: the tools that write these files have eaten typed
// backslashes before.

var findings = new List<string>();
var spaceCount = 0;
var layoutJson = "";

var FeetToMetres = 0.3048;
var backslash = ((char)92).ToString();
var quote = ((char)34).ToString();
var invariant = System.Globalization.CultureInfo.InvariantCulture;

Func<string, string> esc = text =>
{
    if (text == null) return "null";
    var sb = new System.Text.StringBuilder(quote);
    foreach (var c in text)
    {
        if (c == (char)92) sb.Append(backslash + backslash);
        else if (c == (char)34) sb.Append(backslash + quote);
        else if (c < (char)32) sb.Append(backslash + "u" + ((int)c).ToString("x4"));
        else sb.Append(c);
    }
    return sb.Append(quote).ToString();
};
Func<double, string> metres = d =>
    (double.IsNaN(d) || double.IsInfinity(d)) ? "null" : Math.Round(d * FeetToMetres, 3).ToString("R", invariant);
Func<double?, string> metresOrNull = d => d.HasValue ? metres(d.Value) : "null";
Func<double?, string> numOrNull = d =>
    (!d.HasValue || double.IsNaN(d.Value) || double.IsInfinity(d.Value)) ? "null" : Math.Round(d.Value, 6).ToString("R", invariant);

Func<Element, BuiltInCategory, bool> isCategory = (element, bic) =>
{
    try { return element.Category != null && element.Category.Id.Equals(new ElementId(bic)); }
    catch { return false; }
};
Func<Element, BuiltInParameter, double?> doubleOf = (element, which) =>
{
    try
    {
        var p = element.get_Parameter(which);
        if (p != null && p.HasValue && p.StorageType == StorageType.Double) return p.AsDouble();
    }
    catch { }
    return null;
};
Func<Element, ElementId> levelIdOf = element =>
{
    try
    {
        var id = element.LevelId;
        if (id != null && id != ElementId.InvalidElementId) return id;
        foreach (var which in new[] { BuiltInParameter.FAMILY_LEVEL_PARAM, BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM })
        {
            var p = element.get_Parameter(which);
            if (p != null && p.HasValue && p.StorageType == StorageType.ElementId)
            {
                var got = p.AsElementId();
                if (got != null && got != ElementId.InvalidElementId) return got;
            }
        }
    }
    catch { }
    return null;
};
Func<Element, string> typeName = element =>
{
    try
    {
        var symbol = doc.GetElement(element.GetTypeId()) as FamilySymbol;
        if (symbol != null) return symbol.Family.Name + ": " + symbol.Name;
    }
    catch { }
    return null;
};
Func<Element, string> kParameter = element =>
{
    foreach (Parameter p in element.Parameters)
    {
        try
        {
            var name = p.Definition == null ? "" : (p.Definition.Name ?? "");
            var low = name.ToLowerInvariant();
            if (!low.Contains("factor") || !(low.StartsWith("k") || low.Contains(" k"))) continue;
            if (!p.HasValue) continue;
            var shown = p.AsValueString();
            if (string.IsNullOrEmpty(shown) && p.StorageType == StorageType.String) shown = p.AsString();
            if (!string.IsNullOrEmpty(shown)) return name + " = " + shown;
        }
        catch { }
    }
    return null;
};

// ---- which rooms -------------------------------------------------------------

var rooms = new List<SpatialElement>();
var taken = new HashSet<string>();
Action<Element> takeRoom = element =>
{
    var s = element as SpatialElement;
    if (s == null || !(s is Space || s is Room)) return;
    if (taken.Add(s.UniqueId)) rooms.Add(s);
};
var asked = (spaces ?? "").Trim();
if (asked.Length == 0 || asked == "*")
{
    // THE SELECTION ONLY FOR THE MODEL IN FRONT. A model read from behind has
    // no selection of its own; another window's would be the wrong answer.
    if (uidoc != null && uidoc.Document != null && uidoc.Document.Equals(doc))
        foreach (var id in uidoc.Selection.GetElementIds()) takeRoom(doc.GetElement(id));
    if (rooms.Count == 0)
        findings.Add("No Space or Room is selected - select the rooms to lay out, then ask again.");
}
else
{
    foreach (var part in asked.Split(';'))
    {
        var key = part.Trim();
        if (key.Length == 0) continue;
        Element found = null;
        try { found = doc.GetElement(key); } catch { }
        if (found == null || !(found is Space || found is Room))
            findings.Add("No Space or Room in this model has the id " + quote + key + quote + ".");
        else takeRoom(found);
    }
}

// ---- what every room is read against, collected once ------------------------

var ceilings = new List<Element>();
try { foreach (Element e in new FilteredElementCollector(doc).OfClass(typeof(Ceiling))) ceilings.Add(e); }
catch { findings.Add("The ceilings could not be read."); }
var heads = new List<FamilyInstance>();
try
{
    foreach (Element e in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Sprinklers).WhereElementIsNotElementType())
    {
        var fi = e as FamilyInstance;
        if (fi != null && fi.Location is LocationPoint) heads.Add(fi);
    }
}
catch { findings.Add("The sprinklers already in the model could not be read."); }

// ---- the rooms ---------------------------------------------------------------

var json = new System.Text.StringBuilder("{" + esc("format") + ":1");
json.Append("," + esc("document") + ":").Append(esc(doc.Title));
json.Append("," + esc("asked") + ":").Append(esc(asked.Length == 0 ? "*" : asked));
json.Append("," + esc("spaces") + ":[");
var firstRoom = true;
foreach (var s in rooms)
{
    var label = ((s.Number ?? "") + " " + (s.Name ?? "")).Trim();
    double area = 0.0;
    try { area = s.Area; } catch { }
    if (s.Location == null || area <= 0.0)
    {
        findings.Add("The room " + label + " is not placed or not enclosed - it has no area, so it is not laid out.");
        continue;
    }
    // THE OUTER LOOP IS THE LARGEST ONE; the others are columns and shafts.
    // An edge bounded by a separation line is counted: the layout holds every
    // edge as a wall, and a separation line is not one.
    var loopsText = new List<string>();
    var bestText = (string)null;
    double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
    var separationEdges = 0;
    try
    {
        var loops = s.GetBoundarySegments(new SpatialElementBoundaryOptions());
        if (loops != null && loops.Count > 0)
        {
            var bestIndex = -1;
            var bestArea = -1.0;
            var allPts = new List<List<XYZ>>();
            for (var li = 0; li < loops.Count; li++)
            {
                var pts = new List<XYZ>();
                foreach (BoundarySegment segment in loops[li])
                {
                    var curve = segment.GetCurve();
                    if (curve == null) continue;
                    foreach (XYZ p in curve.Tessellate())
                        if (pts.Count == 0 || p.DistanceTo(pts[pts.Count - 1]) >= 1e-6) pts.Add(p);
                }
                allPts.Add(pts);
                var twice = 0.0;
                for (var i = 0; i < pts.Count; i++)
                {
                    var a = pts[i];
                    var b = pts[(i + 1) % pts.Count];
                    twice += a.X * b.Y - b.X * a.Y;
                }
                if (Math.Abs(twice) > bestArea) { bestArea = Math.Abs(twice); bestIndex = li; }
            }
            for (var li = 0; li < allPts.Count; li++)
            {
                var pts = allPts[li];
                if (pts.Count < 3) continue;
                var text = new System.Text.StringBuilder("[");
                for (var i = 0; i < pts.Count; i++)
                {
                    if (i > 0) text.Append(",");
                    text.Append("[").Append(metres(pts[i].X)).Append(",").Append(metres(pts[i].Y)).Append("]");
                    if (li == bestIndex)
                    {
                        minX = Math.Min(minX, pts[i].X); maxX = Math.Max(maxX, pts[i].X);
                        minY = Math.Min(minY, pts[i].Y); maxY = Math.Max(maxY, pts[i].Y);
                    }
                }
                text.Append("]");
                if (li == bestIndex) bestText = text.ToString();
                else loopsText.Add(text.ToString());
            }
            if (bestIndex >= 0)
                foreach (BoundarySegment segment in loops[bestIndex])
                {
                    try
                    {
                        var by = doc.GetElement(segment.ElementId);
                        if (by != null && (isCategory(by, BuiltInCategory.OST_MEPSpaceSeparationLines)
                                           || isCategory(by, BuiltInCategory.OST_RoomSeparationLines)))
                            separationEdges++;
                    }
                    catch { }
                }
        }
    }
    catch { bestText = null; }
    if (bestText == null)
        findings.Add("The outline of " + label + " could not be read - it is not laid out.");

    XYZ at = null;
    try { var lp = s.Location as LocationPoint; if (lp != null) at = lp.Point; } catch { }
    Level level = null;
    try { level = s.Level; } catch { }
    double? elevation = null, projectElevation = null;
    try { if (level != null) { elevation = level.Elevation; projectElevation = level.ProjectElevation; } } catch { }

    if (!firstRoom) json.Append(",");
    firstRoom = false;
    spaceCount++;
    json.Append("{" + esc("id") + ":").Append(esc(s.UniqueId))
        .Append("," + esc("element_id") + ":").Append(esc(s.Id.ToString()))
        .Append("," + esc("kind") + ":").Append(esc(s is Space ? "space" : "room"))
        .Append("," + esc("number") + ":").Append(esc(s.Number))
        .Append("," + esc("name") + ":").Append(esc(s.Name))
        .Append("," + esc("level") + ":").Append(esc(level == null ? null : level.Name))
        .Append("," + esc("level_elevation_m") + ":").Append(metresOrNull(elevation))
        .Append("," + esc("level_project_elevation_m") + ":").Append(metresOrNull(projectElevation))
        .Append("," + esc("area_m2") + ":").Append(numOrNull(area * 0.09290304))
        .Append("," + esc("location") + ":").Append(at == null ? "null" : "[" + metres(at.X) + "," + metres(at.Y) + "]")
        .Append("," + esc("outline") + ":").Append(bestText ?? "null")
        .Append("," + esc("holes") + ":[").Append(string.Join(",", loopsText)).Append("]")
        .Append("," + esc("separation_edges") + ":").Append(separationEdges.ToString(invariant));

    // The ceilings over it: on its level, their plan extent holding its point.
    json.Append("," + esc("ceilings") + ":[");
    var firstCeiling = true;
    if (at != null && level != null)
        foreach (var c in ceilings)
        {
            try
            {
                var cl = levelIdOf(c);
                if (cl == null || !cl.Equals(level.Id)) continue;
                var box = c.get_BoundingBox(null);
                if (box == null || at.X < box.Min.X || at.X > box.Max.X || at.Y < box.Min.Y || at.Y > box.Max.Y) continue;
                if (!firstCeiling) json.Append(",");
                firstCeiling = false;
                json.Append("{" + esc("id") + ":").Append(esc(c.Id.ToString()))
                    .Append("," + esc("type") + ":").Append(esc(typeName(c) ?? (doc.GetElement(c.GetTypeId()) == null ? null : doc.GetElement(c.GetTypeId()).Name)))
                    .Append("," + esc("height_m") + ":").Append(metresOrNull(doubleOf(c, BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM)))
                    .Append("}");
            }
            catch { }
        }
    json.Append("]");

    // Every sprinkler on its level inside its plan extent - which are IN it is the brain's.
    json.Append("," + esc("sprinklers") + ":[");
    var firstHead = true;
    if (level != null && bestText != null)
        foreach (var fi in heads)
        {
            try
            {
                var hl = levelIdOf(fi);
                if (hl == null || !hl.Equals(level.Id)) continue;
                var p = ((LocationPoint)fi.Location).Point;
                if (p.X < minX - 0.01 || p.X > maxX + 0.01 || p.Y < minY - 0.01 || p.Y > maxY + 0.01) continue;
                if (!firstHead) json.Append(",");
                firstHead = false;
                json.Append("{" + esc("id") + ":").Append(esc(fi.Id.ToString()))
                    .Append("," + esc("type") + ":").Append(esc(typeName(fi)))
                    .Append("," + esc("x") + ":").Append(metres(p.X))
                    .Append("," + esc("y") + ":").Append(metres(p.Y))
                    .Append("," + esc("z") + ":").Append(metres(p.Z))
                    .Append("," + esc("offset_m") + ":").Append(metresOrNull(doubleOf(fi, BuiltInParameter.INSTANCE_ELEVATION_PARAM)))
                    .Append("}");
            }
            catch { }
        }
    json.Append("]}");
}
json.Append("]");

// ---- the sprinkler types loaded ---------------------------------------------

json.Append("," + esc("sprinkler_types") + ":[");
var firstType = true;
try
{
    foreach (Element e in new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_Sprinklers))
    {
        var symbol = e as FamilySymbol;
        if (symbol == null) continue;
        string placement = null;
        try { placement = symbol.Family.FamilyPlacementType.ToString(); } catch { }
        if (!firstType) json.Append(",");
        firstType = false;
        json.Append("{" + esc("name") + ":").Append(esc(symbol.Family.Name + ": " + symbol.Name))
            .Append("," + esc("family") + ":").Append(esc(symbol.Family.Name))
            .Append("," + esc("type") + ":").Append(esc(symbol.Name))
            .Append("," + esc("placement") + ":").Append(esc(placement))
            .Append("," + esc("k") + ":").Append(esc(kParameter(symbol)))
            .Append("}");
    }
}
catch { findings.Add("The sprinkler types could not be read."); }
json.Append("]");
if (firstType) findings.Add("No sprinkler family is loaded in this model - load one before laying out.");

json.Append("," + esc("findings") + ":[");
for (var i = 0; i < findings.Count; i++)
{
    if (i > 0) json.Append(",");
    json.Append(esc(findings[i]));
}
json.Append("]}");
layoutJson = json.ToString();
