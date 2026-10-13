// NOT STANDALONE. Assumes `doc`, `elements`, `snapDegrees` and `maxOffDegrees`
// are in scope; leaves `straightened`, `straightenedIds`, `alreadyOnAxis`,
// `tooFarOff`, `notStraightened`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// "SLIGHTLY OFF AXIS" IS ONE JOB, WHATEVER CARRIES THE LINE. A wall, a grid, a
// model or detail line, a reference plane, a room separation line and an area
// boundary line all raise the same Revit warning for the same reason - a
// straight line a fraction of a degree off an axis - and all are fixed the
// same way: turned in plan about one of their own ends until the line lies on
// the axis. So they are one fragment, not seven.
//
// WHAT IS "AN AXIS": a multiple of snapDegrees from the project's X axis -
// 90 by default, so east-west and north-south; 45 adds the diagonals. A line
// further off than maxOffDegrees (half a degree by default) is not "slightly"
// off - it was drawn at that angle on purpose - and is left alone and counted.
//
// TURNED ABOUT ITS START, so that end does not move at all and the other moves
// by the smallest amount that puts it on the axis - a hair, which is the point.
// A joined wall's neighbour may follow its end; that is Revit keeping the
// join, and the answer says so.
//
// ONLY THE MODEL SAYS IT IS STRAIGHT. Revit's rotate call returns normally and
// turns nothing for a pinned element or a group member, so every element's
// angle is read again after a regeneration and only one now on the axis
// counts.
//
// NOT HANDLED, AND NAMED: an arc or any curve that is not a line; a line that
// is not level (one end higher than the other); a grid made of several
// segments; a line inside a floor, ceiling or roof sketch - those can only be
// changed in sketch mode.

var straightened = 0;
var straightenedIds = new List<ElementId>();
var alreadyOnAxis = 0;
var tooFarOff = new List<ElementId>();
var notStraightened = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();

var snap = snapDegrees <= 0 ? 90.0 : snapDegrees;
var limit = maxOffDegrees <= 0 ? 0.5 : maxOffDegrees;
const double Exact = 1e-9;   // degrees; below this the line is already on the axis

Func<Element, string> label = e => (e.Category == null ? "element" : e.Category.Name) + " " + e.Id;

// The plan line of an element, start first, or null with the reason why not.
Func<Element, Tuple<XYZ, XYZ, string>> planLine = e =>
{
    try
    {
        var grid = e as Grid;
        if (grid != null)
        {
            if (grid.IsCurved) return Tuple.Create<XYZ, XYZ, string>(null, null, "a curved grid");
            var c = grid.Curve as Line;
            if (c == null) return Tuple.Create<XYZ, XYZ, string>(null, null, "a grid of several segments");
            return Tuple.Create(c.GetEndPoint(0), c.GetEndPoint(1), (string)null);
        }

        var plane = e as ReferencePlane;
        if (plane != null)
        {
            if (Math.Abs(plane.BubbleEnd.Z - plane.FreeEnd.Z) > 1e-6)
                return Tuple.Create<XYZ, XYZ, string>(null, null, "a reference plane that is not vertical in plan");
            return Tuple.Create(plane.BubbleEnd, plane.FreeEnd, (string)null);
        }

        var curveElement = e as CurveElement;
        if (curveElement != null)
        {
            if (curveElement.Category != null
                && curveElement.Category.Id == new ElementId(BuiltInCategory.OST_SketchLines))
                return Tuple.Create<XYZ, XYZ, string>(null, null, "a line inside a sketch - change it in sketch mode");
            var c = curveElement.GeometryCurve as Line;
            if (c == null) return Tuple.Create<XYZ, XYZ, string>(null, null, "not a straight line");
            return Tuple.Create(c.GetEndPoint(0), c.GetEndPoint(1), (string)null);
        }

        var located = e.Location as LocationCurve;
        if (located != null)
        {
            var c = located.Curve as Line;
            if (c == null) return Tuple.Create<XYZ, XYZ, string>(null, null, "not a straight line");
            return Tuple.Create(c.GetEndPoint(0), c.GetEndPoint(1), (string)null);
        }
    }
    catch (Exception ex)
    {
        return Tuple.Create<XYZ, XYZ, string>(null, null, "its line could not be read - " + ex.Message);
    }
    return Tuple.Create<XYZ, XYZ, string>(null, null, "it carries no straight line");
};

// Degrees off the nearest axis, signed: positive means turn clockwise to fix.
Func<XYZ, XYZ, double> offAxis = (a, b) =>
{
    var angle = Math.Atan2(b.Y - a.Y, b.X - a.X) * 180.0 / Math.PI;
    var nearest = Math.Round(angle / snap) * snap;
    return angle - nearest;
};

var attempted = new List<Element>();

foreach (var element in elements)
{
    if (element == null || !element.IsValidObject) continue;

    var line = planLine(element);
    if (line.Item3 != null)
    {
        notStraightened.Add(element.Id);
        lines.Add(label(element) + ": left alone - " + line.Item3 + ".");
        continue;
    }
    var start = line.Item1;
    var end = line.Item2;
    if (Math.Abs(start.Z - end.Z) > 1e-6)
    {
        notStraightened.Add(element.Id);
        lines.Add(label(element) + ": left alone - one end is higher than the other.");
        continue;
    }
    if (new XYZ(end.X - start.X, end.Y - start.Y, 0).GetLength() < 1e-6)
    {
        notStraightened.Add(element.Id);
        lines.Add(label(element) + ": left alone - it has no length in plan.");
        continue;
    }

    var off = offAxis(start, end);
    if (Math.Abs(off) < Exact) { alreadyOnAxis++; continue; }
    if (Math.Abs(off) > limit)
    {
        tooFarOff.Add(element.Id);
        continue;
    }

    try
    {
        var axis = Line.CreateBound(start, start + XYZ.BasisZ);
        ElementTransformUtils.RotateElement(doc, element.Id, axis, -off * Math.PI / 180.0);
        attempted.Add(element);
        lines.Add(label(element) + ": was " + off.ToString("0.000000") + " degrees off.");
    }
    catch (Exception ex)
    {
        notStraightened.Add(element.Id);
        lines.Add(label(element) + ": Revit refused - " + ex.Message);
    }
}

doc.Regenerate();

// READ BACK - only an element whose line now lies on the axis counts.
foreach (var element in attempted)
{
    var line = planLine(element);
    if (line.Item3 == null && Math.Abs(offAxis(line.Item1, line.Item2)) < 1e-6)
    {
        straightened++;
        straightenedIds.Add(element.Id);
    }
    else
    {
        notStraightened.Add(element.Id);
        lines.Add(label(element) + ": still off the axis after the turn - pinned, in a group, "
            + "or held by what it is joined to.");
    }
}

findings.Add(straightened + " straightened onto a " + snap + "-degree axis and read back, "
    + alreadyOnAxis + " already on an axis, " + tooFarOff.Count + " more than " + limit
    + " degrees off and left as drawn, " + notStraightened.Count + " not straightened.");
if (straightened > 0)
    findings.Add("Each was turned about its start; a wall's joined neighbour may have followed its "
        + "other end by the same hair. Check the warnings list - the off-axis ones should be gone.");

var report = string.Join(" | ", lines);
findings.AddRange(lines);
