// NOT STANDALONE. Assumes `room` is in scope; leaves `boundary`, `holes`,
// `area`, `height`, `unbounded` and `heightKnown` behind.
//
// EVERYTHING IS IN REVIT'S INTERNAL FEET. Not millimetres. The conversion
// belongs at the edge where a person reads the number (D-20), and a fragment
// that converted would be a second place that knows the factor.
//
// AN UNBOUNDED ROOM IS THE CASE THIS FRAGMENT EXISTS TO MAKE VISIBLE.
//
// A Room or Space whose bounding elements do not close returns Area == 0 and no
// boundary segments. It does not throw and it does not warn. A layout composed
// on top of that divides by an area of zero, places nothing, and reports a
// clean success - which is D-30's "succeeded and did nothing" exactly.
//
// So `unbounded` is its own answer, and it is derived from the BOUNDARY rather
// than from the area: a room can be modelled so thin that its area rounds to
// nothing while still being bounded, and the two are different problems.
//
// HEIGHT IS REPORTED AS KNOWN OR NOT, NEVER AS ZERO MEANING UNKNOWN. Not every
// spatial element carries a height parameter, and 0.0 is a plausible-looking
// number that a spacing calculation will happily use.

var options = new SpatialElementBoundaryOptions();
options.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish;

// A ROOM'S BOUNDARY IS A LIST OF LOOPS, AND ONLY THE FIRST IS THE OUTLINE.
//
// Every loop after it is a HOLE - a core, a shaft, a column the room wraps
// around. Version 1 of this fragment flattened them all into one list, so a
// layout built on it treated the shaft's edge as part of the room's outline
// and laid terminals across the void. It went unnoticed because a room with no
// hole in it - most rooms - gives the same answer either way.

var boundary = new List<Curve>();
var holes = new List<Curve>();
var segments = room.GetBoundarySegments(options);

if (segments != null)
{
    for (int loopIndex = 0; loopIndex < segments.Count; loopIndex++)
    {
        var into = loopIndex == 0 ? boundary : holes;
        foreach (var segment in segments[loopIndex])
        {
            var curve = segment.GetCurve();
            if (curve != null) into.Add(curve);
        }
    }
}

var unbounded = boundary.Count == 0;
var area = room.Area;

// ROOM_HEIGHT is carried by both Room and Space. Asked for by BuiltInParameter
// rather than by display name, because the display name is localised and this
// must work on a French or Arabic Revit exactly as it does on an English one.
var height = 0.0;
var heightKnown = false;
var heightParameter = room.get_Parameter(BuiltInParameter.ROOM_HEIGHT);
if (heightParameter != null && heightParameter.HasValue &&
    heightParameter.StorageType == StorageType.Double)
{
    height = heightParameter.AsDouble();
    heightKnown = height > 0;
}
