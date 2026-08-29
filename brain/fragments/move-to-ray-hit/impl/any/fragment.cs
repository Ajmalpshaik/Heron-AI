// NOT STANDALONE. Assumes `doc`, `view3d`, `elements`, `direction`,
// `maxDistance` and `offsetAlongRay` are in scope; leaves `snapped`, `noHit`
// and `notPointBased` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Distances are internal FEET.
//
// ===================================================================
// THE VIEW DECIDES WHAT THE RAY CAN SEE, AND THIS FRAGMENT MOVES THINGS.
// ===================================================================
//
// A ray only finds what is VISIBLE IN THE VIEW it is given - hidden categories,
// the section box, view filters and worksets all apply. Identical code found
// nothing in a view with walls hidden and found them in a view where they were
// visible.
//
// A view where the target is hidden ENTIRELY is the harmless case: every ray
// misses and nothing moves. The dangerous case is a PARTIALLY hidden model,
// where the ray passes through the real nearest surface and lands on whatever
// is visible behind it - and the elements are then moved there, confidently.
//
// This fragment cannot check that for the caller: "can this view see the thing
// we are snapping to" is not a question the API answers. Pass a view with full
// visibility. It is written in capitals because the failure is silent, plausible
// and moves real geometry.
//
// RAYS MUST SEE INTO LINKED MODELS. On a normal project the architecture - and
// therefore the ceilings and slabs almost everything snaps to - lives in a
// linked model. Without this flag every ray finds nothing and the fragment
// reads as broken when the model is simply arranged the usual way.
//
// NEVER `FindNearest` WHEN THE RAY STARTS INSIDE THE SOURCE ELEMENT. It returns
// the element's OWN face, every time, and a fragment built on it reports
// "nothing found" while sitting inside the answer. The pattern that works is
// find them all, drop the self-hits, then take the nearest of what is left.
//
// THE ELEMENT LANDS BY ITS INSERTION POINT, not by its top face. A family whose
// insertion point is its centre will look half-buried in the slab it just
// snapped to. That is what `offsetAlongRay` is for - negative pulls it back
// along the ray. Check ONE element by eye before running a batch.

var snapped = 0;
var noHit = new List<ElementId>();
var notPointBased = new List<ElementId>();

var unit = direction.Normalize();

var intersector = new ReferenceIntersector(view3d);
intersector.FindReferencesInRevitLinks = true;

foreach (var element in elements)
{
    // Only a point-based element can be "moved onto" a surface. A duct has a
    // curve, and moving it to a single point is not the job this fragment does.
    var placed = element.Location as LocationPoint;
    if (placed == null)
    {
        notPointBased.Add(element.Id);
        continue;
    }

    var from = placed.Point;

    var hits = intersector.Find(from, unit);
    if (hits == null || hits.Count == 0)
    {
        noHit.Add(element.Id);
        continue;
    }

    // Drop the self-hits and keep the nearest of the rest. Both halves matter:
    // without the drop, every element snaps to itself and moves nothing.
    var best = 0.0;
    var found = false;
    foreach (var hit in hits)
    {
        var reference = hit.GetReference();
        if (reference == null) continue;
        if (reference.ElementId == element.Id) continue;

        var away = hit.Proximity;
        if (away <= 0 || away > maxDistance) continue;
        if (!found || away < best) { best = away; found = true; }
    }

    if (!found)
    {
        noHit.Add(element.Id);
        continue;
    }

    var travel = best + offsetAlongRay;
    ElementTransformUtils.MoveElement(doc, element.Id, unit.Multiply(travel));
    snapped++;
}
