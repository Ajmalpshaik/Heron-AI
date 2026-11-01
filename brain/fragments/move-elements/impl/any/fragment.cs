// NOT STANDALONE. Assumes `doc`, `elements`, `offset`, `toPoint` and
// `stackOnOnePoint` are in scope, and leaves `moved`, `partly`, `blocked`,
// `unverified`, `movedIds`, `notAtPoint`, `noLocationPoint` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). `offset` is in internal FEET,
// converted by the add-in; `toPoint` is MILLIMETRES, converted below (D-71).
//
// VERSION 2 (2026-11-01) - MOVE_ELEMENTS_TO_POINT MERGED IN. As well as by an
// offset, an element can now be put AT A POINT: `toPoint` "x,y,z" in
// millimetres, and each element's offset is worked out from ITS OWN location
// point, here, where the element is already in hand - so the subtraction
// cannot go stale between two calls. What that card had, and its two rules
// made safer:
//
//   AN ELEMENT POSITIONED BY A CURVE IS COUNTED, NOT MOVED. A wall, a duct, a
//   line has no single point - its start, end and middle are three answers -
//   so it is named in `noLocationPoint` rather than moved by a number nobody
//   chose.
//
//   SEVERAL ELEMENTS TO ONE POINT IS REFUSED UNLESS THE CALLER SAYS SO.
//   Putting two elements at one coordinate stacks them - a duplicate that
//   looks right on screen and schedules twice. More than one element with a
//   location point and `stackOnOnePoint` not true refuses the call before
//   anything moves. MOVE_ELEMENTS_TO_POINT moved them all and said nothing.
//
//   REGENERATE BEFORE READING BACK. MOVE_ELEMENTS_TO_POINT read each position
//   straight after the move; this card's own rule - positions do not update
//   until the document regenerates - is the safer one, and holds here too.
//   One that is not within a millimetre of the point is in `notAtPoint`.
//
// An offset and a point together are refused: two answers to "where to".
//
// WHY THIS FRAGMENT IS MOSTLY A COMPARISON.
//
// Revit's move call RETURNS NORMALLY AND MOVES NOTHING when an element cannot
// be moved. No exception, no return value, no warning. The case that actually
// reaches here is a member of a GROUP: it is not pinned, so every filter that
// looks for pinning lets it through, and then it silently stays put.
//
// Counting "it did not throw" as "it moved" reports a clean success for
// elements that have not shifted a millimetre. That was found by running the
// code against a real model, not by reading it - which is the point, because
// reading it looks fine.
//
// So the only evidence used here is the position itself, read before and read
// again after. FOUR outcomes, kept apart because a user needs a different
// answer to each:
//
//   moved       it is where it was asked to be
//   partly      it moved, but not the whole way - held by a host or an
//               attachment. Legitimate, and NOT a failure
//   blocked     it did not move at all, and Revit reported no error
//   unverified  its position could not be read either side, so this fragment
//               will not claim either way
//
// `blocked` and `unverified` carry IDS rather than counts. "Three did not move"
// is a fact nobody can act on; the ids are what a person selects in Revit to go
// and look at.
//
// NOTHING IS ROLLED BACK HERE. A blocked element is a fact to report, and
// undoing the ones that DID move because one did not would be its own surprise.
// Whether the whole operation should fail is the caller's decision, made with
// these four numbers in front of it.

Func<Element, XYZ> probe = e =>
{
    if (e == null) return null;

    var point = e.Location as LocationPoint;
    if (point != null) return point.Point;

    // A duct or pipe keeps its position as a curve. The midpoint is stable
    // under a translation, which is all this is used for.
    var curve = e.Location as LocationCurve;
    if (curve != null && curve.Curve != null) return curve.Curve.Evaluate(0.5, true);

    try
    {
        var box = e.get_BoundingBox(null);
        if (box != null) return (box.Min + box.Max) * 0.5;
    }
    catch
    {
        // An element with no geometry in the current view. Not knowing where
        // something is is an ordinary outcome, and it is reported as one.
    }
    return null;
};

// One millimetre in feet: smaller than any move worth asking for, larger than
// coordinate noise.
var tolerance = 1.0 / 304.8;

var moved = 0;
var partly = 0;
var blocked = new List<ElementId>();
var unverified = new List<ElementId>();
// VERSION 2, from MOVE_ELEMENTS_TO_POINT.
var movedIds = new List<ElementId>();
var notAtPoint = new List<ElementId>();
var noLocationPoint = new List<ElementId>();
var findings = new List<string>();

// THE POINT, IF ONE WAS ASKED FOR - three real numbers in millimetres inside
// 100 km, the add-in's own bound for a point, to feet.
var pointWord = (toPoint ?? "").Trim();
var toAPoint = pointWord.Length > 0 && !string.Equals(pointWord, "none", StringComparison.OrdinalIgnoreCase);
XYZ target = null;
string stop = null;
if (toAPoint)
{
    var parts = pointWord.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    var values = new double[3];
    var ok = parts.Length == 3;
    for (var i = 0; ok && i < 3; i++)
    {
        ok = double.TryParse(parts[i].Trim(), System.Globalization.NumberStyles.Float,
                             System.Globalization.CultureInfo.InvariantCulture, out values[i])
             && !double.IsNaN(values[i]) && !double.IsInfinity(values[i]) && Math.Abs(values[i]) <= 100000000.0;
    }
    if (!ok)
        stop = "\"" + pointWord + "\" is not a point - three numbers in MILLIMETRES, \"12400, 8600, 3000\". "
            + "Nothing was moved";
    else if (offset != null && offset.GetLength() >= tolerance)
        stop = "An offset AND a point were both given - two answers to where. Give the point to put the "
            + "elements AT it, or the offset to shift them BY it. Nothing was moved";
    else
        target = new XYZ(values[0] / 304.8, values[1] / 304.8, values[2] / 304.8);
}

if (stop != null || toAPoint)
{
    // ------------------------------------------------------------------
    // VERSION 2: TO A POINT.
    // ------------------------------------------------------------------
    var placed = new List<Element>();
    if (stop == null)
    {
        foreach (var element in elements)
        {
            if (element == null) continue;
            if (element.Location is LocationPoint) placed.Add(element);
            else noLocationPoint.Add(element.Id);
        }
        if (placed.Count > 1 && !stackOnOnePoint)
            stop = string.Format("{0} elements would all be put at ONE point, stacked on each other - a "
                + "duplicate that looks right on screen and schedules twice. Say stackOnOnePoint true if "
                + "that is meant, or move them by an offset instead. Nothing was moved", placed.Count);
    }

    if (stop != null)
    {
        findings.Add(stop);
        noLocationPoint.Clear();
    }
    else
    {
        var shifted = new List<Element>();
        foreach (var element in placed)
        {
            var from = ((LocationPoint)element.Location).Point;
            var shift = target - from;
            if (shift.GetLength() < tolerance) continue;   // already there - read back below
            try
            {
                ElementTransformUtils.MoveElement(doc, element.Id, shift);
                shifted.Add(element);
            }
            catch
            {
                // Revit refused the move outright - it did not move at all.
                blocked.Add(element.Id);
            }
        }

        // Positions do not update until the document regenerates.
        doc.Regenerate();

        foreach (var element in placed)
        {
            var after = element.Location as LocationPoint;
            if (after != null && after.Point.DistanceTo(target) < tolerance)
            {
                moved++;
                movedIds.Add(element.Id);
            }
            else notAtPoint.Add(element.Id);
        }

        findings.Add(string.Format(
            "{0} are now at the point, {1} did not arrive ({2} of them Revit would not move at all - a "
            + "constraint, a group or a pin holds them), {3} are positioned by a curve rather than a point "
            + "and have no single answer to this",
            moved, notAtPoint.Count, blocked.Count, noLocationPoint.Count));
    }
}
else
{
var before = new Dictionary<ElementId, XYZ>();
foreach (var element in elements)
{
    var at = probe(element);
    if (at != null) before[element.Id] = at;
}

var attempted = new List<Element>();
foreach (var element in elements)
{
    try
    {
        ElementTransformUtils.MoveElement(doc, element.Id, offset);
        attempted.Add(element);
    }
    catch
    {
        // The element type refuses to be moved at all - some hosted and system
        // elements do. That IS an exception, unlike the silent case above, and
        // it is counted as blocked rather than lost.
        blocked.Add(element.Id);
    }
}

// Positions do not update until the document regenerates, so a comparison
// before this would measure each element against itself and pass every time.
doc.Regenerate();

// A move of nothing is a move nobody can measure: asked for zero, every element
// is already where it should be, and comparing would report them all blocked.
var askedForNothing = offset.GetLength() < tolerance;

foreach (var element in attempted)
{
    if (askedForNothing) { moved++; continue; }

    XYZ was;
    var now = probe(element);
    if (now == null || !before.TryGetValue(element.Id, out was))
    {
        unverified.Add(element.Id);
        continue;
    }

    if (now.DistanceTo(was) < tolerance) blocked.Add(element.Id);
    else if (now.DistanceTo(was + offset) > tolerance) partly++;
    else { moved++; movedIds.Add(element.Id); }
}

findings.Add(string.Format("{0} moved the whole offset, {1} part of it, {2} did not move, {3} could not "
    + "be read back", moved, partly, blocked.Count, unverified.Count));
}
