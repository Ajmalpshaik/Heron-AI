// NOT STANDALONE. Assumes `doc`, `elements`, `reference`, `alignX`, `alignY`,
// `alignZ`, `edge` and `targetZ` are in scope; leaves `aligned`, `blocked`,
// `unverified`, `noSize`, `notCurveBased` and `refused`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Coordinates are internal FEET.
//
// TWO WAYS TO LINE THINGS UP, ONE TOOL (version 2, 2026-11-01).
// ALIGN_MEP_ELEVATION, which levelled MEP runs by their top, bottom or centre
// to a typed height, is DEPRECATED and merged into this card:
//
//   `reference` and the axes   each element's position moved onto the
//                              reference's, axis by axis - version 1, unchanged
//   `edge` and `targetZ`       each run's TOP, BOTTOM or CENTRE put at one
//                              typed height in millimetres, read from the
//                              run's own size - ALIGN_MEP_ELEVATION's job
//
// One or the other. Both said at once is refused rather than one picked: a
// reference and a typed height disagree about where things go.
//
// WHAT "ITS POSITION" MEANS, AND WHY IT MATTERS MOST FOR A DUCT.
//
// A placed family has a point. A duct or pipe has a curve, and the point used
// here is its MIDPOINT - so two runs of different lengths aligned in X share a
// centre and their ENDS DO NOT LINE UP. That is right for equipment and wrong
// for somebody who meant "make the ends meet", and the difference does not show
// until it is looked at.
//
// FOR MEP LEVELLED BY TOP OR BOTTOM, USE `edge`. Aligning a 600 duct and a 100
// pipe by the reference in Z shares their centrelines and leaves the soffits
// 250 apart. With `edge` each run's own half-height is read and subtracted:
//
//   rectangular duct   RBS_CURVE_HEIGHT_PARAM
//   round duct         RBS_CURVE_DIAMETER_PARAM
//   pipe               RBS_PIPE_DIAMETER_PARAM
//   cable tray         RBS_CABLETRAY_HEIGHT_PARAM
//
// A run carrying none of them goes to `noSize` and is NOT moved - never
// treated as flat. Something not curve-based goes to `notCurveBased`.
// INSULATION IS NOT INCLUDED: these give the bare service size. A sloped run's
// MIDPOINT lands on the height and its slope is kept.
//
// `targetZ` ARRIVES IN MILLIMETRES and is divided by 304.8 here (D-71).
//
// THE OPPOSITE RULE, AND THE SAFER ONE KEPT. ALIGN_MEP_ELEVATION read any edge
// word that was not "top" or "bottom" as the centre, and moved without reading
// back. Here an edge word it does not know is REFUSED, naming the three it
// takes, and every move is verified as version 1's are - a run Revit would not
// move (a group member) is in `blocked`, not counted as aligned.
//
// THE MOVE IS VERIFIED, for the reason every move here is: Revit returns
// normally and moves nothing for a group member.

Func<Element, XYZ> positionOf = e =>
{
    if (e == null) return null;

    var point = e.Location as LocationPoint;
    if (point != null) return point.Point;

    var curve = e.Location as LocationCurve;
    if (curve != null && curve.Curve != null) return curve.Curve.Evaluate(0.5, true);

    try
    {
        var box = e.get_BoundingBox(null);
        if (box != null) return (box.Min + box.Max) * 0.5;
    }
    catch
    {
        // No geometry in the current view. Reported, never assumed.
    }
    return null;
};

// VERSION 2 - ALIGN_MEP_ELEVATION's half-height, word for word.
Func<Element, double> halfHeightOf = e =>
{
    var order = new[]
    {
        BuiltInParameter.RBS_CURVE_HEIGHT_PARAM,       // rectangular duct, tray
        BuiltInParameter.RBS_CURVE_DIAMETER_PARAM,     // round duct
        BuiltInParameter.RBS_PIPE_DIAMETER_PARAM,      // pipe
        BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM,   // cable tray
    };

    foreach (var which in order)
    {
        var parameter = e.get_Parameter(which);
        if (parameter == null || !parameter.HasValue) continue;
        if (parameter.StorageType != StorageType.Double) continue;
        var size = parameter.AsDouble();
        if (size > 0) return size / 2.0;
    }
    return -1.0;      // negative means "no size found", not "zero height"
};

var aligned = 0;
var blocked = new List<ElementId>();
var unverified = new List<ElementId>();
var noSize = new List<ElementId>();
var notCurveBased = new List<ElementId>();
string refused = null;

var tolerance = 1.0 / 304.8;

var wanted = (edge ?? "").Trim().ToLowerInvariant();
var byEdge = wanted.Length > 0 && wanted != "none";

if (byEdge)
{
    if (wanted == "center") wanted = "centre";

    if (wanted != "top" && wanted != "bottom" && wanted != "centre")
    {
        refused = "\"" + edge + "\" is not an edge this lines up by, so NOTHING WAS MOVED. Say top, bottom "
            + "or centre - the underside, the soffit a ceiling clears, is bottom.";
    }
    else if (double.IsNaN(targetZ) || double.IsInfinity(targetZ))
    {
        refused = "An edge was given and no height to put it at, so NOTHING WAS MOVED. Give targetZ in "
            + "millimetres - the height the " + wanted + " of every run should be at.";
    }
    else if (reference != null || alignX || alignY || alignZ)
    {
        refused = "Both a typed height (edge and targetZ) and a reference or an axis were given, so "
            + "NOTHING WAS MOVED. They disagree about where things go - say one: an edge and a height, or "
            + "a reference and the axes.";
    }
    else
    {
        const double MillimetresPerFoot = 304.8;
        var height = targetZ / MillimetresPerFoot;

        foreach (var element in elements)
        {
            if (element == null) continue;

            var line = element.Location as LocationCurve;
            if (line == null || line.Curve == null)
            {
                notCurveBased.Add(element.Id);
                continue;
            }

            var half = halfHeightOf(element);
            if (half < 0)
            {
                // Reported rather than treated as flat - a run whose size could
                // not be read would align its centreline while every other run
                // aligned its soffit, which looks almost right.
                noSize.Add(element.Id);
                continue;
            }

            var wantCentre = height;
            if (wanted == "top") wantCentre = height - half;
            else if (wanted == "bottom") wantCentre = height + half;

            var at = line.Curve.Evaluate(0.5, true);
            var shift = wantCentre - at.Z;

            if (Math.Abs(shift) < tolerance) { aligned++; continue; }

            ElementTransformUtils.MoveElement(doc, element.Id, new XYZ(0, 0, shift));

            var now = positionOf(element);
            if (now == null || Math.Abs(now.Z - at.Z) < tolerance) blocked.Add(element.Id);
            else aligned++;
        }
    }
}
else if (reference == null)
{
    refused = "Nothing to line up TO, so NOTHING WAS MOVED. Name a reference element - select it and "
        + "say \"selected\" - with the axes, or an edge (top, bottom or centre) and a height.";
}
else
{
    var target = positionOf(reference);

    // With no reference position there is nothing to align TO, and moving
    // everything to an invented coordinate would be worse than doing nothing.
    if (target == null)
    {
        refused = "The reference's position could not be read, so NOTHING WAS MOVED - there is "
            + "nothing to align to, and an invented coordinate would be worse than doing nothing.";
    }
    else
    {
        foreach (var element in elements)
        {
            if (element == null) continue;
            if (element.Id == reference.Id) continue;      // the reference stays put

            var at = positionOf(element);
            if (at == null) { unverified.Add(element.Id); continue; }

            var shift = new XYZ(alignX ? target.X - at.X : 0,
                                alignY ? target.Y - at.Y : 0,
                                alignZ ? target.Z - at.Z : 0);

            // Already in line. Counted as aligned, because it is where it should be
            // - and a zero move would otherwise read as blocked below.
            if (shift.GetLength() < tolerance) { aligned++; continue; }

            ElementTransformUtils.MoveElement(doc, element.Id, shift);

            var now = positionOf(element);
            if (now == null || now.DistanceTo(at) < tolerance) blocked.Add(element.Id);
            else aligned++;
        }
    }
}
