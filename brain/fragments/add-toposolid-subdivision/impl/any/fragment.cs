// NOT STANDALONE. Assumes `doc`, `elements` and `loopMm` are in scope; leaves
// `subdivisionIds`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A SUBDIVISION ON EACH TOPOSOLID HANDED IN - a car park, a path, a planted
// bed - its outline `loopMm` "x,y; x,y; ..." in millimetres, closed
// automatically, wholly inside the toposolid. The subdivision follows the
// terrain and takes its own material.
//
// REVIT 2024 brought toposolids, so the card declares 2024 on.
//
// READ BACK: each toposolid's subdivisions are listed again, and only a new
// one counts.

const double MmPerFoot = 304.8;
var subdivisionIds = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();

var outline = new List<XYZ>();
string refusal = null;
var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;
foreach (var chunk in (loopMm ?? "").Split(';'))
{
    if (chunk.Trim() == "") continue;
    var parts = chunk.Split(',');
    double x, y;
    if (parts.Length < 2 || !double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
        || !double.TryParse(parts[1].Trim(), numberStyle, culture, out y))
    { refusal = "\"" + chunk.Trim() + "\" is not a point - two numbers in millimetres. NOTHING WAS MADE."; break; }
    outline.Add(new XYZ(x / MmPerFoot, y / MmPerFoot, 0));
}
if (refusal == null && outline.Count < 3) refusal = "A subdivision outline needs three points or more. NOTHING WAS MADE.";

if (refusal == null)
{
    foreach (var element in elements)
    {
        var topo = element as Toposolid;
        if (topo == null) { if (element != null) lines.Add(element.Id + ": not a toposolid."); continue; }
        var before = new HashSet<ElementId>(topo.GetSubDivisionIds());
        try
        {
            var level = doc.GetElement(topo.LevelId) as Level;
            var z = level == null ? 0 : level.Elevation;
            var loop = new CurveLoop();
            for (var i = 0; i < outline.Count; i++)
            {
                var a = outline[i];
                var b = outline[(i + 1) % outline.Count];
                loop.Append(Line.CreateBound(new XYZ(a.X, a.Y, z), new XYZ(b.X, b.Y, z)));
            }
            topo.CreateSubDivision(doc, new List<CurveLoop> { loop });
        }
        catch (Exception ex) { lines.Add("toposolid " + topo.Id + ": Revit refused - " + ex.Message); continue; }
        doc.Regenerate();
        var added = topo.GetSubDivisionIds().Where(id => !before.Contains(id)).ToList();
        if (added.Count > 0) { subdivisionIds.AddRange(added); lines.Add("toposolid " + topo.Id + ": subdivision " + added[0] + "."); }
        else lines.Add("toposolid " + topo.Id + ": asked, and no new subdivision reads back.");
    }
    findings.Add(subdivisionIds.Count + " subdivision(s) made and read back.");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
