// NOT STANDALONE. Assumes `doc`, `pointsMm`, `table` and `startMm` are in
// scope; leaves `propertyLine`, `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and never opens one (Golden Rule 16). One call
// is one undo entry.
//
// A PROPERTY LINE - Massing & Site > Property Line - drawn one of two ways,
// exactly as Revit's own dialog offers:
//
//   pointsMm  "x,y; x,y; ..." millimetres from the internal origin, in order.
//             Kept as given: OPEN, unless the last point repeats the first,
//             when it closes. Revit's "create by sketching"
//   table     rows "distance, bearing" separated by semicolons - the distance
//             in millimetres, the bearing a quadrant bearing (N45-30-00E,
//             N 45d30'00" E, S12.5W) or degrees clockwise from north (45.5).
//             An arc row is "distance, bearing, arc, radius, left|right", the
//             radius in millimetres. Revit's "create by entering distances
//             and bearings". `startMm` "x,y" moves the table's start point;
//             left out, Revit's own start
//
// One of the two, never both. THE BEARING IS PASSED AS AN ANGLE IN 0 TO 2*PI
// - what RevitAPI.xml says of PropertyTableEntry - and which way and from which
// north Revit measures it is NOT MEASURED - NEEDS REAL REVIT. The read-back
// reports the bearings Revit holds, beside what was typed, so a wrong turn is
// seen, never hidden; the area is the same either way.
//
// RELEASES. PropertyLine.Create(Document, IList<CurveLoop>) and
// (Document, IList<PropertyTableEntry>), PropertyTableEntry, IsValidBoundary,
// IsValidPropertyTable, IsSketchBased, IsClosedLoop, Area, GetStart, SetStart,
// GetBoundary and GetPropertyTable are present in 2027 only (tools/api-surface
// --members, 2026-11-02) - the card claims 2027.
//
// `absent: none` arrives as the text "none" when left out (FRAGMENT-ISSUES row
// 5b-1432). AN ElementId IS NEVER READ AS A NUMBER.

const double MmPerFoot = 304.8;
const double SquareMetresPerSquareFoot = 0.09290304;
PropertyLine propertyLine = null;
var report = new List<string>();
var findings = new List<string>();
string refused = null;

var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;
Func<double, string> num = v => Math.Round(v, 1).ToString("0.#", culture);
Func<double, string> deg = v => Math.Round(v * 180.0 / Math.PI, 4).ToString("0.####", culture);
Func<string, string> given = s =>
{
    var t = (s ?? "").Trim();
    return string.Equals(t, "none", StringComparison.OrdinalIgnoreCase) ? "" : t;
};
Func<string, double?> number = s =>
{
    double v;
    return double.TryParse((s ?? "").Trim(), numberStyle, culture, out v) ? (double?)v : null;
};
// "x,y" in millimetres to feet, at the internal origin's height.
Func<string, XYZ> point = s =>
{
    var parts = (s ?? "").Split(',');
    if (parts.Length != 2) return null;
    var x = number(parts[0]);
    var y = number(parts[1]);
    return x == null || y == null ? null : new XYZ(x.Value / MmPerFoot, y.Value / MmPerFoot, 0);
};
// A bearing to radians clockwise from north, 0 to 2*PI; null when unreadable.
var quadrant = new System.Text.RegularExpressions.Regex(
    "^([NS])\\s*(\\d+(?:\\.\\d+)?)(?:\\s*[d\\u00B0\\-:\\s]\\s*(\\d+(?:\\.\\d+)?))?(?:\\s*['m\\-:\\s]\\s*(\\d+(?:\\.\\d+)?))?\\s*[\"s]?\\s*([EW])$",
    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
Func<string, double?> bearing = s =>
{
    var text = (s ?? "").Trim();
    var plain = number(text);
    double degrees;
    if (plain != null)
    {
        if (plain.Value < 0 || plain.Value > 360) return null;
        degrees = plain.Value;
    }
    else
    {
        var m = quadrant.Match(text);
        if (!m.Success) return null;
        var a = double.Parse(m.Groups[2].Value, culture)
            + (m.Groups[3].Success ? double.Parse(m.Groups[3].Value, culture) / 60.0 : 0)
            + (m.Groups[4].Success ? double.Parse(m.Groups[4].Value, culture) / 3600.0 : 0);
        if (a > 90) return null;
        var north = m.Groups[1].Value.ToUpperInvariant() == "N";
        var east = m.Groups[5].Value.ToUpperInvariant() == "E";
        degrees = north ? (east ? a : 360 - a) : (east ? 180 - a : 180 + a);
    }
    if (degrees >= 360) degrees -= 360;
    return degrees * Math.PI / 180.0;
};

var pointsText = given(pointsMm);
var tableText = given(table);
var startText = given(startMm);

if (pointsText == "" && tableText == "")
    refused = "Give the property line as points, \"x,y; x,y; ...\" in millimetres, or as a table of \"distance, bearing\" rows. NOTHING WAS MADE.";
else if (pointsText != "" && tableText != "")
    refused = "Give points or a table, not both - Revit draws a property line one way or the other. NOTHING WAS MADE.";

// ---- by points (a sketch) ----------------------------------------------------
if (refused == null && pointsText != "")
{
    var points = new List<XYZ>();
    foreach (var chunk in pointsText.Split(';'))
    {
        if (chunk.Trim() == "") continue;
        var p = point(chunk);
        if (p == null) { refused = "Each point is two numbers in millimetres, x,y - " + "\"" + chunk.Trim() + "\" is not. NOTHING WAS MADE."; break; }
        points.Add(p);
    }
    if (refused == null && points.Count < 2) refused = "A property line needs two points or more. NOTHING WAS MADE.";
    if (refused == null)
        for (var i = 0; i + 1 < points.Count && refused == null; i++)
            if (points[i].DistanceTo(points[i + 1]) < 1.0 / MmPerFoot)
                refused = "Points " + (i + 1) + " and " + (i + 2) + " are at the same place. NOTHING WAS MADE.";
    if (refused == null)
    {
        var closes = points.Count > 3 && points[0].DistanceTo(points[points.Count - 1]) < 1.0 / MmPerFoot;
        if (closes) points[points.Count - 1] = points[0];
        var loop = new CurveLoop();
        for (var i = 0; i + 1 < points.Count; i++) loop.Append(Line.CreateBound(points[i], points[i + 1]));
        var loops = new List<CurveLoop> { loop };
        if (!PropertyLine.IsValidBoundary(loops)) refused = "Revit says these points are not a property line - the line may cross itself. NOTHING WAS MADE.";
        else
        {
            try { propertyLine = PropertyLine.Create(doc, loops); }
            catch (Exception ex) { refused = "Revit refused the property line: " + ex.Message + " NOTHING WAS MADE."; }
            if (startText != "") findings.Add("startMm is for a table; points place themselves, so it was not used.");
        }
    }
}

// ---- by distances and bearings (a table) -------------------------------------
var typed = new List<string>();
if (refused == null && tableText != "")
{
    var entries = new List<PropertyTableEntry>();
    var rows = tableText.Split(';').Select(r => r.Trim()).Where(r => r != "").ToList();
    for (var i = 0; i < rows.Count && refused == null; i++)
    {
        var f = rows[i].Split(',').Select(t => t.Trim()).ToList();
        var name = "Row " + (i + 1) + " \"" + rows[i] + "\"";
        if (f.Count != 2 && f.Count != 5) { refused = name + " is not \"distance, bearing\" nor \"distance, bearing, arc, radius, left|right\". NOTHING WAS MADE."; break; }
        var distance = number(f[0]);
        var angle = bearing(f[1]);
        if (distance == null || distance.Value <= 0) { refused = name + ": the distance is not a length in millimetres. NOTHING WAS MADE."; break; }
        if (angle == null) { refused = name + ": the bearing is not one Heron reads - N45-30-00E, S12.5W or degrees from north. NOTHING WAS MADE."; break; }
        var curveType = PropertyTableEntryCurveType.Line;
        var radius = 0.0;
        var direction = PropertyTableEntryArcDirection.Left;
        if (f.Count == 5)
        {
            var r = number(f[3]);
            var side = f[4].ToLowerInvariant();
            if (f[2].ToLowerInvariant() != "arc" || r == null || (side != "left" && side != "right"))
            { refused = name + ": an arc row is \"distance, bearing, arc, radius, left|right\". NOTHING WAS MADE."; break; }
            curveType = PropertyTableEntryCurveType.Arc;
            radius = r.Value / MmPerFoot;
            direction = side == "right" ? PropertyTableEntryArcDirection.Right : PropertyTableEntryArcDirection.Left;
        }
        if (!PropertyTableEntry.IsValidPropertyTableEntry(distance.Value / MmPerFoot, angle.Value, curveType, radius, direction))
        { refused = name + ": Revit says this row is not valid - too short, or an arc radius under half its distance. NOTHING WAS MADE."; break; }
        entries.Add(PropertyTableEntry.Create(distance.Value / MmPerFoot, angle.Value, curveType, radius, direction, i));
        typed.Add(f[1] + " (" + deg(angle.Value) + " deg)");
    }
    XYZ start = null;
    if (refused == null && startText != "")
    {
        start = point(startText);
        if (start == null) refused = "startMm is two numbers in millimetres, x,y. NOTHING WAS MADE.";
    }
    if (refused == null && !PropertyLine.IsValidPropertyTable(doc, entries))
        refused = "Revit says this table is not a property line - too few rows, or a row too short. NOTHING WAS MADE.";
    if (refused == null)
    {
        try
        {
            propertyLine = PropertyLine.Create(doc, entries);
            if (propertyLine != null && start != null) propertyLine.SetStart(start);
        }
        catch (Exception ex) { refused = "Revit refused the property line: " + ex.Message + " NOTHING WAS MADE."; propertyLine = null; }
    }
}

// ---- read back ---------------------------------------------------------------
if (refused == null)
{
    doc.Regenerate();
    var back = propertyLine == null ? null : doc.GetElement(propertyLine.Id) as PropertyLine;
    if (back == null) { refused = "Revit returned no property line. NOTHING WAS MADE."; propertyLine = null; }
    else
    {
        propertyLine = back;
        var start = back.GetStart();
        var area = back.Area;
        report.Add("property line " + back.Id.ToString() + ", " + (!back.IsSketchBased() ? "from a table" : "sketched") + ", "
            + (back.IsClosedLoop() ? "closed, area " + num(area * SquareMetresPerSquareFoot) + " m2" : "open - no area")
            + ", starting at " + num(start.X * MmPerFoot) + "," + num(start.Y * MmPerFoot) + " mm.");
        if (!back.IsSketchBased())
        {
            var held = back.GetPropertyTable();
            for (var i = 0; i < held.Count; i++)
                report.Add("row " + (i + 1) + ": " + num(held[i].Distance * MmPerFoot) + " mm at " + deg(held[i].Bearing) + " deg held"
                    + (i < typed.Count ? ", typed " + typed[i] : "")
                    + (held[i].CurveType == PropertyTableEntryCurveType.Arc ? ", arc of radius " + num(held[i].Radius * MmPerFoot) + " mm " + held[i].ArcDirection.ToString().ToLowerInvariant() : "") + ".");
        }
        else
        {
            var curves = back.GetBoundary().SelectMany(l => l).ToList();
            report.Add(curves.Count + " segment(s), " + num(curves.Sum(c => c.Length) * MmPerFoot) + " mm long in all.");
        }
        findings.Add("Made a property line, read back: " + report[0]);
    }
}
if (refused != null) findings.Add(refused);
