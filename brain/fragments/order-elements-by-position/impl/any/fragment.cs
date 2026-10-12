// NOT STANDALONE. Assumes `doc`, `elements`, `order` and `rowMm` are in
// scope; leaves `elements` - the same ones, in order - `sequence` and
// `findings` behind.
//
// READ ONLY - opens no transaction.
//
// PUTS ELEMENTS IN AN ORDER BY WHERE THEY ARE, and hands them on in it - the
// order a numbering, a schedule or a sheet set should follow. RENUMBER_SEQUENTIAL
// numbers in the order it is handed and decides none: this is where the order
// is decided, said out loud.
//
//   order   "left to right", "right to left", "top to bottom", "bottom to top"
//           (top is north in a plan); "reading" - rows from the top, each left
//           to right; "columns" - from the left, each top to bottom;
//           "clockwise" or "anticlockwise" round the set's middle, starting
//           due north; "along <id>" - by where each falls along that line or
//           curve; "level" - lowest level first, reading order on each
//   rowMm   how far apart two rows are before they count as different rows,
//           for "reading", "columns" and "level"; 0 means 1000
//
// WHERE AN ELEMENT IS: a viewport by the middle of its box on the sheet, an
// element with a location point by that point, one with a location line by
// the line's middle, anything else by the middle of its box. One with none of
// those is left out and named.

const double MmPerFoot = 304.8;
var ordered = new List<Element>();
var findings = new List<string>();
string refusal = null;

var how = (order ?? "").Trim().ToLowerInvariant();
var band = (rowMm > 0 ? rowMm : 1000.0) / MmPerFoot;
Curve path = null;
if (how.StartsWith("along"))
{
    var idText = how.Substring(5).Trim();
    foreach (var candidate in new FilteredElementCollector(doc).WhereElementIsNotElementType())
        if (candidate.Id.ToString() == idText || candidate.UniqueId == idText)
        {
            var located = candidate.Location as LocationCurve;
            if (located != null) path = located.Curve;
            else { var curveElement = candidate as CurveElement; if (curveElement != null) path = curveElement.GeometryCurve; }
            break;
        }
    if (path == null) refusal = "\"" + idText + "\" is not a line or curve in this model to order along.";
}
else if (how != "left to right" && how != "right to left" && how != "top to bottom" && how != "bottom to top"
    && how != "reading" && how != "columns" && how != "clockwise" && how != "anticlockwise" && how != "level")
    refusal = "\"" + order + "\" is not an order this knows - left to right, right to left, top to bottom, bottom to top, "
        + "reading, columns, clockwise, anticlockwise, along <id> or level.";

// Each element with where it is and, for "level", how high its level is.
var placedAt = new List<Tuple<Element, XYZ, double>>();
var noPlace = new List<ElementId>();
if (refusal == null)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        XYZ at = null;
        var viewport = element as Viewport;
        if (viewport != null) at = viewport.GetBoxCenter();
        else
        {
            var point = element.Location as LocationPoint;
            var line = element.Location as LocationCurve;
            if (point != null) at = point.Point;
            else if (line != null) at = line.Curve.Evaluate(0.5, true);
            else
            {
                var box = element.get_BoundingBox(null);
                if (box != null) at = (box.Min + box.Max) / 2.0;
            }
        }
        if (at == null) { noPlace.Add(element.Id); continue; }
        var height = 0.0;
        if (how == "level")
        {
            var level = doc.GetElement(element.LevelId) as Level;
            height = level != null ? level.Elevation : at.Z;
        }
        placedAt.Add(Tuple.Create(element, at, height));
    }

    IEnumerable<Tuple<Element, XYZ, double>> sorted;
    Func<double, double> row = v => Math.Floor(v / band);
    if (how == "left to right") sorted = placedAt.OrderBy(t => t.Item2.X).ThenByDescending(t => t.Item2.Y);
    else if (how == "right to left") sorted = placedAt.OrderByDescending(t => t.Item2.X).ThenByDescending(t => t.Item2.Y);
    else if (how == "top to bottom") sorted = placedAt.OrderByDescending(t => t.Item2.Y).ThenBy(t => t.Item2.X);
    else if (how == "bottom to top") sorted = placedAt.OrderBy(t => t.Item2.Y).ThenBy(t => t.Item2.X);
    else if (how == "reading") sorted = placedAt.OrderByDescending(t => row(t.Item2.Y)).ThenBy(t => t.Item2.X);
    else if (how == "columns") sorted = placedAt.OrderBy(t => row(t.Item2.X)).ThenByDescending(t => t.Item2.Y);
    else if (how == "level") sorted = placedAt.OrderBy(t => t.Item3).ThenByDescending(t => row(t.Item2.Y)).ThenBy(t => t.Item2.X);
    else if (path != null)
        sorted = placedAt.OrderBy(t => { var hit = path.Project(t.Item2); return hit != null ? hit.Parameter : double.MaxValue; });
    else
    {
        // Round the middle of the set, measured from due north.
        var middle = new XYZ(placedAt.Count > 0 ? placedAt.Average(t => t.Item2.X) : 0, placedAt.Count > 0 ? placedAt.Average(t => t.Item2.Y) : 0, 0);
        Func<XYZ, double> bearing = p =>
        {
            var angle = Math.Atan2(p.X - middle.X, p.Y - middle.Y);
            if (angle < 0) angle += 2 * Math.PI;
            return how == "clockwise" ? angle : (2 * Math.PI - angle) % (2 * Math.PI);
        };
        sorted = placedAt.OrderBy(t => bearing(t.Item2));
    }
    foreach (var t in sorted) ordered.Add(t.Item1);
}

var sequence = string.Join(", ", ordered.Take(200).Select(e => e.Id.ToString()));
if (refusal != null) findings.Add(refusal + " Nothing was ordered.");
else
{
    findings.Add(ordered.Count + " element(s) put in order (" + how + ") and handed on in it"
        + (ordered.Count > 200 ? "; the sequence lists the first 200" : "") + ".");
    if (noPlace.Count > 0) findings.Add(noPlace.Count + " element(s) have no position to order by and were left out: "
        + string.Join(", ", noPlace.Take(20).Select(i => i.ToString())) + ".");
}

// Handed on as `elements`, the pipeline's currency, in the order asked.
elements = ordered;
