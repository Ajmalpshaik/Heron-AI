// NOT STANDALONE. Assumes `doc`, `view3d`, `elements`, `directions` and
// `maxDistance` are in scope, and leaves `hits`, `nothingHit` and `noOrigin`
// behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE BIGGEST TRAP: A RAY SEES ONLY WHAT THE 3D VIEW SHOWS.
//
// Ray casting runs inside a 3D view and obeys it completely - hidden
// categories, section boxes, view filters, closed worksets. A view with walls
// switched off reports a clear path with a wall standing in it, silently.
// Proven in a real model: same element, same direction, 0 neighbours in one
// view and 4 in another. That is why the view is a request input and the first
// thing to check when a result looks impossibly empty.
//
// THE DISTANCE IS FROM THE ELEMENT'S POINT, NOT ITS FACE.
//
// The ray starts at the insertion point, inside the element, so a 600 mm deep
// unit reports about 300 mm less clearance than a tape would. Reading it as
// face to face is how a clearance passes on paper and fails on site.
//
// ONE RAY PER DIRECTION MISSES WHAT IS BESIDE A LARGE ELEMENT.
//
// It sees only what is directly in line with the centre: a pipe passing the
// corner of an air handling unit is invisible to it.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
maxDistance = maxDistance / MillimetresPerFoot;

var hits = new Dictionary<ElementId, IList<string>>();
var nothingHit = new List<ElementId>();
var noOrigin = new List<ElementId>();

var intersector = new ReferenceIntersector(view3d);
intersector.FindReferencesInRevitLinks = false;
intersector.TargetType = FindReferenceTarget.Element;

foreach (var element in elements)
{
    if (element == null) continue;

    // Where the ray starts: the element's own point, or the middle of its box
    // when it has no point. Both are inside the element, which is why its own
    // faces have to be dropped below.
    XYZ origin = null;
    var point = element.Location as LocationPoint;
    if (point != null) { try { origin = point.Point; } catch { } }

    if (origin == null)
    {
        try
        {
            var box = element.get_BoundingBox(null);
            if (box != null) origin = (box.Min + box.Max) / 2.0;
        }
        catch { }
    }

    if (origin == null) { noOrigin.Add(element.Id); continue; }

    var found = new List<string>();

    foreach (var direction in directions)
    {
        if (direction == null || direction.GetLength() < 1e-9) continue;
        var unit = direction.Normalize();

        ReferenceWithContext nearest = null;
        try { nearest = intersector.FindNearest(origin, unit); }
        catch { continue; }

        if (nearest == null) continue;

        double distance = nearest.Proximity;
        if (distance > maxDistance) continue;

        ElementId hitId = ElementId.InvalidElementId;
        try
        {
            var reference = nearest.GetReference();
            if (reference != null) hitId = reference.ElementId;
        }
        catch { }

        // Its own geometry is the first thing in the way of a ray that starts
        // inside it.
        if (hitId == ElementId.InvalidElementId || hitId == element.Id) continue;

        var hitElement = doc.GetElement(hitId);
        string what = "(unknown)";
        if (hitElement != null)
        {
            string name = "";
            string category = "";
            try { name = hitElement.Name ?? ""; } catch { }
            try { if (hitElement.Category != null) category = hitElement.Category.Name ?? ""; } catch { }
            what = (category.Length > 0 ? category + " " : "") + name + " (" + hitId.ToString() + ")";
        }

        found.Add("(" + unit.X.ToString("0.##") + ", " + unit.Y.ToString("0.##") + ", " +
                  unit.Z.ToString("0.##") + ") hits " + what +
                  " at " + distance.ToString("0.###") + " ft from the element's own point");
    }

    if (found.Count > 0) hits[element.Id] = found;
    else nothingHit.Add(element.Id);
}
