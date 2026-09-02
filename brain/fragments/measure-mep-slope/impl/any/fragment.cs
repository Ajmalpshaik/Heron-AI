// NOT STANDALONE. Assumes `elements` and `minimumSlopePercent` are in scope;
// leaves `slopePercent`, `belowMinimum`, `vertical` and `noGeometry` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// MEASURED FROM THE ENDS, NOT READ FROM THE SLOPE PARAMETER.
//
// This repository's standing rule is that Revit's own data describes intent and
// is not always the physical fact - it is how the real refrigerant pairing was
// found when both the tag names and the connector flags said otherwise. A pipe
// whose slope parameter reads 1% and whose two endpoints sit at the same
// elevation is FLAT. The geometry is the half that drains.
//
// NO UNITS ARE INVOLVED, WHICH IS WHY THIS IS SAFE ACROSS EVERY RELEASE. Slope
// is rise divided by run - two internal lengths - so the ratio is identical in
// feet, millimetres or anything else, and 2021's unit API rewrite has nothing
// here to break. It is the one measurement in this library that needs no
// conversion and therefore cannot carry a unit error.
//
// A VERTICAL PIPE HAS NO SLOPE, and calling it infinitely sloped or dividing by
// a horizontal run of zero would put an infinity into a report. A riser is
// separated out by name instead: it is not a drainage defect, it is a different
// kind of pipe, and listing it as "below minimum" would send somebody to fix
// something that is already correct.
//
// SIGN IS KEPT, BUT IT IS NOT A VERDICT, AND CONFUSING THE TWO WOULD MAKE THIS
// FRAGMENT WRONG ABOUT HALF THE PIPES IT LOOKS AT.
//
// The sign is measured from the curve's own start to its own end. REVIT'S CURVE
// DIRECTION IS NOT FLOW DIRECTION - it is whichever way the modeller happened
// to draw, and two identical drains drawn from opposite ends report +1% and
// -1%. So a signed comparison against a positive minimum would pass one and
// fail the other while both are correct on site.
//
// Therefore: the signed value is REPORTED, because it is the fact and the
// caller may know which end is downstream. The verdict is taken on the
// MAGNITUDE, which answers the question this fragment can actually answer -
// is there enough fall here at all. Whether it falls the RIGHT WAY needs to
// know which end is downstream, and that is TRACE_CONNECTIVITY's job, not
// this one. Answering it here would mean inventing a flow direction.

var slopePercent = new Dictionary<ElementId, double>();
var belowMinimum = new List<ElementId>();
var vertical = new List<ElementId>();
var noGeometry = new List<ElementId>();

// Below this much horizontal run the element is a riser rather than a sloped
// pipe. 1/16 of a foot is about 1.6 mm - far below any real horizontal run,
// and far above the floating-point noise on two subtracted coordinates.
const double MinimumHorizontalRunFeet = 0.0625;

foreach (var element in elements)
{
    if (element == null) continue;

    var placement = element.Location as LocationCurve;
    if (placement == null || placement.Curve == null)
    {
        noGeometry.Add(element.Id);
        continue;
    }

    var start = placement.Curve.GetEndPoint(0);
    var end = placement.Curve.GetEndPoint(1);

    var dx = end.X - start.X;
    var dy = end.Y - start.Y;
    var run = Math.Sqrt((dx * dx) + (dy * dy));

    if (run < MinimumHorizontalRunFeet)
    {
        vertical.Add(element.Id);
        continue;
    }

    var rise = end.Z - start.Z;
    var percent = (rise / run) * 100.0;

    if (slopePercent.ContainsKey(element.Id)) continue;
    slopePercent[element.Id] = percent;

    // MAGNITUDE, for the reason set out above: the same drain drawn from the
    // other end reports the opposite sign, and a signed test would fail it for
    // the way somebody dragged the mouse. A flat pipe fails this whichever way
    // it was drawn, which is the defect that matters and the one this can see.
    if (Math.Abs(percent) < minimumSlopePercent) belowMinimum.Add(element.Id);
}
