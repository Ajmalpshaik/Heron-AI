// NOT STANDALONE. Assumes `doc`, `elements`, `view`, `pointsMm`, `wireType`,
// `wiringStyle` and `snapMm` are in scope; leaves `made`, `wireIds`,
// `looseEnds`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// TWO WAYS, ONE JOB - wires drawn in a plan:
//
//   points given    ONE wire through them. Each end is put on the electrical
//                   connector of a device handed in when that connector lies
//                   within snapMm of the end point, in plan; otherwise that
//                   end is left free and counted.
//   no points       a wire from each device to the next - a daisy chain. The
//                   order is walked nearest-first from the FIRST device handed
//                   in, and is printed in the report, so nobody has to guess
//                   which device was joined to which.
//
// REVIT'S RULE: a wire lives in ONE floor plan or reflected ceiling plan view.
// Anything else is refused before a wire is attempted.
//
// AN ARC WIRE NEEDS A THIRD POINT. Between two connectors alone Revit draws a
// straight wire, so an arc between devices is given one point off the middle,
// a fifth of the span to the side - the shape the ribbon's arc wire draws.
//
// EVERY WIRE IS READ BACK: each end counted connected only if the wire's own
// connector says it is.

const double MmPerFoot = 304.8;
var made = 0;
var wireIds = new List<ElementId>();
var looseEnds = 0;
var lines = new List<string>();
var findings = new List<string>();

Func<Element, Connector> electrical = e =>
{
    var instance = e as FamilyInstance;
    if (instance == null || instance.MEPModel == null || instance.MEPModel.ConnectorManager == null)
        return null;
    foreach (Connector c in instance.MEPModel.ConnectorManager.Connectors)
        if (c != null && c.Domain == Domain.DomainElectrical) return c;
    return null;
};

Func<XYZ, XYZ, double> planDistance = (a, b) =>
    Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

// THE POINTS, AS TEXT IN MILLIMETRES - "x,y,z; x,y,z" - and EMPTY MEANS NONE.
// A list-of-points need cannot be left out (the binder refuses an absent one),
// and "no points, chain the devices" is half of what this does, so the points
// are read here. A point that is not three numbers stops the run rather than
// being skipped, because a wire with one vertex missing is a different wire.
List<XYZ> points = null;
string pointProblem = null;
var pointText = (pointsMm ?? "").Trim();
if (pointText != "")
{
    points = new List<XYZ>();
    foreach (var chunk in pointText.Split(';'))
    {
        if (chunk.Trim() == "") continue;
        var parts = chunk.Split(',');
        double x, y, z;
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var numberStyle = System.Globalization.NumberStyles.Float;
        if (parts.Length != 3
            || !double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
            || !double.TryParse(parts[1].Trim(), numberStyle, culture, out y)
            || !double.TryParse(parts[2].Trim(), numberStyle, culture, out z))
        {
            pointProblem = "\"" + chunk.Trim() + "\" is not a point - three numbers in millimetres, "
                + "separated by commas, points separated by semicolons. NO WIRE WAS DRAWN.";
            break;
        }
        points.Add(new XYZ(x / MmPerFoot, y / MmPerFoot, z / MmPerFoot));
    }
}

var devices = new List<Element>();
var noConnector = new List<string>();
foreach (var element in elements)
{
    if (element == null || !element.IsValidObject) continue;
    if (electrical(element) == null) noConnector.Add(element.Id.ToString());
    else devices.Add(element);
}

var style = (wiringStyle ?? "").Trim().ToLowerInvariant();
var shape = style == "chamfer" || style == "straight" ? WiringType.Chamfer : WiringType.Arc;
var snap = (snapMm <= 0 ? 300.0 : snapMm) / MmPerFoot;

// THE WIRE TYPE, by name; empty takes the first in the project.
Element typeElement = null;
var wantedType = (wireType ?? "").Trim();
foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(WireType)))
{
    if (wantedType == "" || string.Equals(candidate.Name, wantedType, StringComparison.OrdinalIgnoreCase))
    {
        typeElement = candidate;
        break;
    }
}

string refusal = pointProblem;
if (refusal != null) { }
else if (view == null || (view.ViewType != ViewType.FloorPlan && view.ViewType != ViewType.CeilingPlan))
    refusal = "A wire is drawn in one floor plan or reflected ceiling plan, and "
        + (view == null ? "no view was given" : "\"" + view.Name + "\" is neither")
        + ", so NO WIRE WAS DRAWN.";
else if (typeElement == null)
    refusal = wantedType == ""
        ? "The project has no wire type at all, so NO WIRE WAS DRAWN."
        : "No wire type is named \"" + wantedType + "\", so NO WIRE WAS DRAWN. Name one as it reads "
          + "under Electrical Settings > Wiring Types.";
else if ((points == null || points.Count < 2) && devices.Count < 2)
    refusal = "With no points, wires are drawn from device to device, and fewer than two devices "
        + "with an electrical connector were handed in - so NO WIRE WAS DRAWN.";

// One wire, read back: how many of its ends are on a connector.
Action<IList<XYZ>, Connector, Connector, string> draw = (vertices, start, end, what) =>
{
    try
    {
        var wire = Wire.Create(doc, typeElement.Id, view.Id, shape, vertices, start, end);
        if (wire == null) { lines.Add(what + ": Revit returned no wire."); return; }
        doc.Regenerate();
        var connectedEnds = 0;
        try
        {
            foreach (Connector c in wire.ConnectorManager.Connectors)
                if (c != null && c.IsConnected) connectedEnds++;
        }
        catch { }
        made++;
        wireIds.Add(wire.Id);
        looseEnds += Math.Max(0, 2 - connectedEnds);
        lines.Add(what + ": wire " + wire.Id + ", " + connectedEnds + " of 2 ends on a device.");
    }
    catch (Exception ex)
    {
        lines.Add(what + ": Revit refused - " + ex.Message);
    }
};

if (refusal == null && points != null && points.Count >= 2)
{
    // ONE WIRE THROUGH THE POINTS.
    // The nearest device connector to each end, within snapMm in plan. The
    // same device is never both ends.
    Connector start = null, end = null;
    double bestStart = snap, bestEnd = snap;
    foreach (var device in devices)
    {
        var c = electrical(device);
        var toFirst = planDistance(c.Origin, points[0]);
        if (toFirst <= bestStart) { bestStart = toFirst; start = c; }
    }
    foreach (var device in devices)
    {
        var c = electrical(device);
        if (start != null && device.Id == start.Owner.Id) continue;
        var toLast = planDistance(c.Origin, points[points.Count - 1]);
        if (toLast <= bestEnd) { bestEnd = toLast; end = c; }
    }

    var vertices = new List<XYZ>(points);
    if (start != null) vertices.RemoveAt(0);
    if (end != null && vertices.Count > 0) vertices.RemoveAt(vertices.Count - 1);
    draw(vertices, start, end, "through " + points.Count + " point(s)"
        + (start == null ? ", start free" : ", start on " + start.Owner.Id)
        + (end == null ? ", end free" : ", end on " + end.Owner.Id));
}
else if (refusal == null)
{
    // A DAISY CHAIN, nearest-first from the first device handed in.
    var order = new List<Element> { devices[0] };
    var left = new List<Element>(devices.Skip(1));
    while (left.Count > 0)
    {
        var from = electrical(order[order.Count - 1]).Origin;
        var next = left.OrderBy(d => planDistance(electrical(d).Origin, from)).First();
        order.Add(next);
        left.Remove(next);
    }
    lines.Add("Order walked nearest-first from the first device: "
        + string.Join(" > ", order.Select(d => d.Id.ToString())));

    for (var i = 0; i + 1 < order.Count; i++)
    {
        var a = electrical(order[i]);
        var b = electrical(order[i + 1]);
        var vertices = new List<XYZ>();
        if (shape == WiringType.Arc)
        {
            var span = b.Origin - a.Origin;
            var flat = new XYZ(span.X, span.Y, 0);
            var middle = (a.Origin + b.Origin) / 2.0;
            if (flat.GetLength() > 1e-6)
            {
                var side = new XYZ(-flat.Y, flat.X, 0).Normalize() * (flat.GetLength() / 5.0);
                vertices.Add(middle + side);
            }
        }
        draw(vertices, a, b, order[i].Id + " to " + order[i + 1].Id);
    }
}

if (refusal != null) lines.Add(refusal);
if (noConnector.Count > 0)
    lines.Add("No electrical connector, so left out: " + string.Join(", ", noConnector));

var report = string.Join(" | ", lines);
findings.Add(made + " wire(s) drawn" + (view == null ? "" : " in \"" + view.Name + "\"")
    + ", " + looseEnds + " end(s) left on no device.");
findings.AddRange(lines);
