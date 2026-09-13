// NOT STANDALONE. Assumes `view3d`, `points`, `direction` and `maxDistance` are
// in scope; leaves `blocked`, `clear` and `notChecked` behind.
//
// DISTANCES ARE IN INTERNAL FEET, and no conversion happens here (D-20).
//
// THE THING THAT MAKES THIS FRAGMENT LIE, IF NOBODY SAYS IT OUT LOUD:
//
// ReferenceIntersector only finds what is VISIBLE IN THE VIEW IT IS GIVEN. A
// 3D view with structural framing switched off reports every point clear, and
// it does so confidently - no error, no warning, a tidy list of clear points
// and a sprinkler layout straight through a beam.
//
// So the view is not a detail of the API here, it is the whole assumption, and
// the caller has to supply one that can see what is being checked for. This
// fragment cannot verify that for them - "can this view see beams" is not a
// question the API answers - which is why it is written in capitals here and
// stated in the proof cases rather than left to be discovered.
//
// A VIEW TEMPLATE CANNOT BE USED. ReferenceIntersector throws on one, and a
// template is a perfectly ordinary thing to have a handle on, so it is checked
// once rather than caught forty times.
//
// `notChecked` exists so the three outcomes stay three. A point that was never
// tested is not a clear point, and adding it to `clear` would be the most
// expensive kind of wrong answer this fragment could give.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
maxDistance = maxDistance / MillimetresPerFoot;

var blocked = new List<XYZ>();
var clear = new List<XYZ>();
var notChecked = 0;

if (view3d == null || view3d.IsTemplate)
{
    // Nothing is attempted, and nothing is called clear. A refusal the caller
    // can turn into a sentence, rather than a list that looks like an answer.
    notChecked = points.Count;
}
else
{
    var intersector = new ReferenceIntersector(view3d);
    intersector.FindReferencesInRevitLinks = true;

    foreach (var point in points)
    {
        if (point == null || direction == null)
        {
            notChecked++;
            continue;
        }

        var hit = intersector.FindNearest(point, direction);

        if (hit == null)
        {
            clear.Add(point);
        }
        else if (hit.Proximity <= maxDistance)
        {
            blocked.Add(point);
        }
        else
        {
            // Something is up there, but further away than the clearance asked
            // for. That is clear FOR THIS QUESTION, and the distance is what
            // decides it - not the existence of a hit.
            clear.Add(point);
        }
    }
}
