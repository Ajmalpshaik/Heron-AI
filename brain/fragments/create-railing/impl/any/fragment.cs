// NOT STANDALONE. Assumes `doc`, `elements`, `railingTypeName`, `placement`,
// `pathMm` and `level` are in scope; leaves `made`, `railingIds`, `report`
// and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// RAILINGS, TWO WAYS:
//
//   on stairs or ramps   every stair or ramp handed in gets Revit's own
//                        railings on all its sides - on the treads or on the
//                        stringer, as `placement` says - as Railing > Place on
//                        Stair/Ramp does
//   along a path         `pathMm` "x,y; x,y; ..." in millimetres, an open run
//                        drawn on `level` - a balcony edge, a guard rail
//
// The type is found by name among the project's railing types; a name not
// there refuses, naming those that are.
//
// READ BACK: every railing Revit hands back is looked up again by its id.

const double MmPerFoot = 304.8;
var made = 0;
var railingIds = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();

var types = new FilteredElementCollector(doc).OfClass(typeof(RailingType)).ToElements();
Element type = null;
foreach (var candidate in types)
    if (string.Equals(candidate.Name, (railingTypeName ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) { type = candidate; break; }
var where = (placement ?? "").Trim().ToLowerInvariant();

string refusal = null;
if (type == null)
    refusal = "No railing type is named \"" + railingTypeName + "\". The project has: "
        + (types.Count == 0 ? "none" : string.Join(", ", types.Select(t => t.Name))) + ". NOTHING WAS MADE.";
else if (where != "" && where != "treads" && where != "stringer")
    refusal = "\"" + placement + "\" is not treads or stringer. NOTHING WAS MADE.";

var hosts = new List<Element>();
if (elements != null)
    foreach (var element in elements)
    {
        if (element == null || element.Category == null) continue;
        var c = element.Category.Id;
        if (c == new ElementId(BuiltInCategory.OST_Stairs) || c == new ElementId(BuiltInCategory.OST_Ramps)) hosts.Add(element);
        else lines.Add(element.Id + ": not a stair or ramp - left out.");
    }

var path = new List<XYZ>();
if (refusal == null && (pathMm ?? "").Trim() != "")
{
    var culture = System.Globalization.CultureInfo.InvariantCulture;
    var numberStyle = System.Globalization.NumberStyles.Float;
    foreach (var chunk in pathMm.Split(';'))
    {
        if (chunk.Trim() == "") continue;
        var parts = chunk.Split(',');
        double x, y;
        if (parts.Length < 2 || !double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
            || !double.TryParse(parts[1].Trim(), numberStyle, culture, out y))
        { refusal = "\"" + chunk.Trim() + "\" is not a point - two numbers in millimetres. NOTHING WAS MADE."; break; }
        path.Add(new XYZ(x / MmPerFoot, y / MmPerFoot, level == null ? 0 : level.Elevation));
    }
    if (refusal == null && path.Count < 2) refusal = "A railing path needs two points or more. NOTHING WAS MADE.";
    if (refusal == null && level == null) refusal = "A railing along a path needs a level. NOTHING WAS MADE.";
}
if (refusal == null && hosts.Count == 0 && path.Count == 0)
    refusal = "Neither a stair or ramp nor a path was given. NOTHING WAS MADE.";

if (refusal == null)
{
    var position = where == "stringer" ? RailingPlacementPosition.Stringer : RailingPlacementPosition.Treads;
    foreach (var host in hosts)
    {
        try
        {
            var ids = Railing.Create(doc, host.Id, type.Id, position);
            if (ids != null) railingIds.AddRange(ids);
            lines.Add(host.Category.Name + " " + host.Id + ": " + (ids == null ? 0 : ids.Count) + " railing(s).");
        }
        catch (Exception ex) { lines.Add(host.Id + ": Revit refused - " + ex.Message); }
    }
    if (path.Count >= 2)
    {
        try
        {
            var loop = new CurveLoop();
            for (var i = 0; i + 1 < path.Count; i++) loop.Append(Line.CreateBound(path[i], path[i + 1]));
            var railing = Railing.Create(doc, loop, type.Id, level.Id);
            if (railing != null) { railingIds.Add(railing.Id); lines.Add("along the path: 1 railing."); }
        }
        catch (Exception ex) { lines.Add("along the path: Revit refused - " + ex.Message); }
    }
    doc.Regenerate();
    made = railingIds.Count(id => doc.GetElement(id) != null);
    findings.Add(made + " railing(s) of \"" + type.Name + "\" made and read back.");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
