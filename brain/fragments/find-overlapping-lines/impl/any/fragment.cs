// NOT STANDALONE. Assumes `doc`, `elements` and `toleranceMm` are in scope;
// leaves `findings`, `overlapping` and `notStraight` behind.
//
// READ ONLY. Opens no transaction and needs none. NOTHING IS DELETED.
//
// ===========================================================================
// "ARE THESE TWO LINES THE SAME" IS NOT A DIRECT COMPARISON.
// ===========================================================================
//
// Two lines that look identical almost never have equal endpoints - drawn in
// opposite directions, overlapping only partly, or a millionth of a foot apart.
// Comparing endpoints finds nearly nothing.
//
// What works: give each line the identity of the INFINITE LINE IT SITS ON, and
// group by that. Three things make the key work and each is a defect if
// skipped:
//
//   1  NORMALISE THE DIRECTION. Left-to-right and right-to-left give opposite
//      vectors. Flip every direction into one half-plane or the two never meet.
//      This is the commonest reason such a check "finds nothing".
//   2  ROUND the direction AND the offset before they become the key.
//      Unrounded floats put genuinely collinear lines in different groups.
//   3  ONLY THEN compare along the line. Inside a group everything is
//      collinear, so each line is a 1-D interval and overlap is arithmetic.
//
// LINE STYLE IS PART OF THE IDENTITY. Two lines on one alignment in different
// styles are usually deliberate - a grid line and a hidden line - and calling
// them duplicates would have somebody delete a drawing convention.
//
// ONLY STRAIGHT LINES. Collinearity means nothing for an arc, and pretending
// otherwise puts real geometry on a delete list.

var findings = new List<string>();
var overlapping = new List<ElementId>();
var notStraight = new List<ElementId>();

var tolerance = toleranceMm / 304.8;
var round = Math.Max(tolerance, 1e-9);

// key -> the lines on that infinite line, as (id, start, end, height)
var groups = new Dictionary<string, List<ElementId>>();
var starts = new Dictionary<ElementId, double>();
var ends = new Dictionary<ElementId, double>();
var heights = new Dictionary<ElementId, double>();

foreach (var element in elements)
{
    if (element == null) continue;

    var location = element.Location as LocationCurve;
    var line = location == null ? null : location.Curve as Line;
    if (line == null) { notStraight.Add(element.Id); continue; }

    var direction = line.Direction;

    // 1. NORMALISE. Flip into one half-plane so both drawing directions agree.
    var dx = direction.X;
    var dy = direction.Y;
    if (dx < -1e-9 || (Math.Abs(dx) <= 1e-9 && dy < 0)) { dx = -dx; dy = -dy; }

    var length = Math.Sqrt(dx * dx + dy * dy);
    if (length <= 1e-9) { notStraight.Add(element.Id); continue; }
    dx = dx / length;
    dy = dy / length;

    var origin = line.GetEndPoint(0);

    // Perpendicular distance from the origin, in plan.
    var offset = origin.X * dy - origin.Y * dx;

    // 2. ROUND both parts of the key.
    // The line style is on CurveElement.LineStyle, not on the element's type -
    // a line's type id is not a graphics style, and reaching for it there gets
    // nothing on any release.
    var styleName = "";
    try
    {
        var curve = element as CurveElement;
        if (curve != null && curve.LineStyle != null) styleName = curve.LineStyle.Name ?? "";
    }
    catch { }

    var key = string.Format("{0:0.000}|{1:0.000}|{2:0.###}|{3}",
        Math.Round(dx / round) * round, Math.Round(dy / round) * round,
        Math.Round(offset / round) * round, styleName);

    if (!groups.ContainsKey(key)) groups[key] = new List<ElementId>();
    groups[key].Add(element.Id);

    // 3. Position ALONG the line, so overlap becomes interval arithmetic.
    var a = line.GetEndPoint(0);
    var b = line.GetEndPoint(1);
    var at0 = a.X * dx + a.Y * dy;
    var at1 = b.X * dx + b.Y * dy;

    starts[element.Id] = Math.Min(at0, at1);
    ends[element.Id] = Math.Max(at0, at1);
    heights[element.Id] = a.Z;
}

var pairs = 0;
var flagged = new HashSet<ElementId>();

foreach (var group in groups)
{
    var ids = group.Value;
    if (ids.Count < 2) continue;

    for (var i = 0; i < ids.Count; i++)
    {
        for (var j = i + 1; j < ids.Count; j++)
        {
            var shared = Math.Min(ends[ids[i]], ends[ids[j]]) - Math.Max(starts[ids[i]], starts[ids[j]]);
            if (shared <= tolerance) continue;

            pairs++;
            flagged.Add(ids[i]);
            flagged.Add(ids[j]);

            var sameHeight = Math.Abs(heights[ids[i]] - heights[ids[j]]) <= tolerance;

            findings.Add(string.Format("{0} and {1} share {2:0} mm of the same line{3}",
                ids[i], ids[j], shared * 304.8,
                sameHeight ? ""
                    : string.Format(" - but they are {0:0} mm APART IN HEIGHT, so on a plan they stack "
                        + "and in the model they do not", Math.Abs(heights[ids[i]] - heights[ids[j]]) * 304.8)));
        }
    }
}

foreach (var id in flagged) overlapping.Add(id);

findings.Insert(0, string.Format(
    "{0} overlapping pair(s) across {1} line(s), {2} of them involved. {3} were not straight and were "
    + "skipped. NOTHING has been deleted - read the heights before removing a model line",
    pairs, elements.Count, flagged.Count, notStraight.Count));
