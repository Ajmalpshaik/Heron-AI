// NOT STANDALONE. Assumes `elements` and `distance` are in scope; leaves
// `offset`, `notLinear` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). `distance` is internal FEET.
//
// THIS IS A PLAN OFFSET. The offset direction is fixed to the world vertical,
// which means the run moves sideways within a horizontal plane - the normal
// case, a parallel run beside another. A VERTICAL RISER OR A SLOPED RUN WILL
// NOT DO WHAT YOU EXPECT: there is no meaningful "sideways in plan" for a pipe
// going straight up. Not a defect, but not this fragment's job either.
//
// THE SIGN PICKS THE SIDE, AND IT IS NOT "LEFT" OR "RIGHT". Which side a
// positive distance lands on follows the run's own direction, so two parallel
// ducts drawn in opposite directions offset to opposite sides for the same
// number. If it lands on the wrong side, flip the sign - there is no naming
// that would make this predictable without knowing how each run was drawn.
//
// OFFSETTING CONNECTED MEP BREAKS THE CONNECTIONS. The geometry moves and the
// fittings do not follow, so the run comes apart at every joint. Revit's
// behaviour, not this fragment's, and the same caveat the plain move carries.
// Reconnect afterwards, or offset the fittings with it.
//
// IN PLACE ONLY. A parallel COPY is COPY_ELEMENTS and then this, which is the
// composition doing its job rather than a mode this fragment has to grow.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
distance = distance / MillimetresPerFoot;

var offset = 0;
var notLinear = new List<ElementId>();
var refused = new List<ElementId>();

foreach (var element in elements)
{
    var line = element.Location as LocationCurve;
    if (line == null || line.Curve == null)
    {
        if (element != null) notLinear.Add(element.Id);
        continue;
    }

    Curve moved = null;
    try
    {
        moved = line.Curve.CreateOffset(distance, XYZ.BasisZ);
    }
    catch
    {
        // An offset Revit cannot construct - an arc offset inward past its own
        // centre is the usual one, where the result would have a negative
        // radius. A reported refusal, not a thrown batch.
        refused.Add(element.Id);
        continue;
    }

    if (moved == null) { refused.Add(element.Id); continue; }

    try
    {
        line.Curve = moved;
        offset++;
    }
    catch
    {
        // The element would not accept the new curve - constrained by a host,
        // pinned, or joined to something that will not give. Counted, so the
        // rest of the batch still runs.
        refused.Add(element.Id);
    }
}
