// NOT STANDALONE. Assumes `doc`, `centreMm`, `radiiMm`, `spokeAnglesDegrees`,
// `startDegrees` and `endDegrees` are in scope; leaves `made`, `gridIds`,
// `gridNames`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A RADIAL GRID: arcs about one centre at the radii given, between a start
// and end angle, and spokes - straight grids out from the centre - at the
// angles given. Angles are degrees anticlockwise from east, as Revit measures
// them; radii and the centre are millimetres.
//
//   arcs     one grid per radius, from startDegrees to endDegrees
//   spokes   one grid per angle, from the centre to the largest radius
//
// A spoke angle outside the arcs' sweep is still drawn, and named, because a
// grid the caller asked for is not silently dropped.
//
// Revit names each grid as it numbers them; the names are read back and
// returned. CREATE_GRIDS sets out straight grids from bay spacings.

const double MmPerFoot = 304.8;
var made = 0;
var gridIds = new List<ElementId>();
var gridNames = new List<string>();
var findings = new List<string>();
string refused = null;

var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;
Func<string, List<double>> numbers = text =>
{
    var list = new List<double>();
    foreach (var part in (text ?? "").Split(','))
    {
        if (part.Trim() == "") continue;
        double value;
        if (!double.TryParse(part.Trim(), numberStyle, culture, out value)) return null;
        list.Add(value);
    }
    return list;
};

var centreParts = numbers(centreMm);
var radii = numbers(radiiMm);
var spokes = numbers(spokeAnglesDegrees);

if (centreParts == null || (centreParts.Count != 2 && centreParts.Count != 3))
    refused = "\"" + centreMm + "\" is not a centre - two or three numbers in millimetres. NOTHING WAS MADE.";
else if (radii == null || spokes == null)
    refused = "The radii and the spoke angles are numbers separated by commas. NOTHING WAS MADE.";
else if (radii.Count == 0 && spokes.Count == 0)
    refused = "Neither a radius nor a spoke angle was given. NOTHING WAS MADE.";
else if (radii.Any(r => r <= 0))
    refused = "Every radius must be more than 0 mm. NOTHING WAS MADE.";
else if (spokes.Count > 0 && radii.Count == 0)
    refused = "Spokes run from the centre to the largest radius, and no radius was given. NOTHING WAS MADE.";

if (refused == null)
{
    var centre = new XYZ(centreParts[0] / MmPerFoot, centreParts[1] / MmPerFoot,
                         centreParts.Count == 3 ? centreParts[2] / MmPerFoot : 0);
    var start = startDegrees * Math.PI / 180.0;
    var finish = endDegrees * Math.PI / 180.0;
    if (finish <= start) finish += 2 * Math.PI;
    if (finish - start >= 2 * Math.PI - 1e-6) finish = start + 2 * Math.PI - 1e-3;   // a grid cannot be a closed circle

    Action<Curve, string> grid = (curve, what) =>
    {
        try
        {
            Grid g = curve is Arc ? Grid.Create(doc, (Arc)curve) : Grid.Create(doc, (Line)curve);
            made++;
            gridIds.Add(g.Id);
        }
        catch (Exception ex) { findings.Add(what + ": Revit refused - " + ex.Message); }
    };

    foreach (var radius in radii)
    {
        var arc = Arc.Create(centre, radius / MmPerFoot, start, finish, XYZ.BasisX, XYZ.BasisY);
        grid(arc, "the arc at " + radius + " mm");
    }
    var outer = radii.Max() / MmPerFoot;
    var sweepStart = startDegrees;
    var sweepEnd = endDegrees <= startDegrees ? endDegrees + 360 : endDegrees;
    foreach (var angle in spokes)
    {
        var a = angle * Math.PI / 180.0;
        var tip = centre + new XYZ(Math.Cos(a), Math.Sin(a), 0) * outer;
        grid(Line.CreateBound(centre, tip), "the spoke at " + angle + " degrees");
        var normal = ((angle - sweepStart) % 360 + 360) % 360;
        if (radii.Count > 0 && normal > (sweepEnd - sweepStart) + 1e-9)
            findings.Add("The spoke at " + angle + " degrees lies outside the arcs' sweep - drawn as asked.");
    }

    doc.Regenerate();
    foreach (var id in gridIds)
    {
        var g = doc.GetElement(id);
        if (g != null) gridNames.Add(g.Name);
    }
    findings.Insert(0, "Made " + made + " grid(s) and read back their names: " + string.Join(", ", gridNames) + ".");
}
if (refused != null) findings.Add(refused);
