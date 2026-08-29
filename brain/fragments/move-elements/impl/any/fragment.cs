// NOT STANDALONE. Assumes `doc`, `elements` and `offset` are in scope, and
// leaves `moved`, `partly`, `blocked` and `unverified` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). `offset` is in internal FEET.
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
    else moved++;
}
