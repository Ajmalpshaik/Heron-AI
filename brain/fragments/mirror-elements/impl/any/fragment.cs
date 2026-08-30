// NOT STANDALONE. Assumes `doc`, `elements`, `planeFrom`, `planeTo` and
// `asCopy` are in scope; leaves `copies`, `mirrored` and `degenerate` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Points are internal FEET.
//
// MIRRORING CONNECTED MEP IN PLACE DOES NOT DO WHAT IT LOOKS LIKE.
//
// Mirror a duct run in place WITHOUT its fittings and Revit keeps the
// connections and re-fits the geometry around them. What comes back is a
// constrained shape, not a reflection - and it looks like a bug in the mirror
// rather than what it is, which is Revit protecting the system. Include the
// fittings in the selection, or mirror as a COPY, where nothing is connected
// to anything yet.
//
// THE PLANE IS VERTICAL, THROUGH TWO POINTS IN PLAN. That is the "draw an axis
// and mirror about it" the interactive tool does, and it is what somebody means
// by "mirror it to the other room". A mirror about a horizontal plane is a
// different request and is not this fragment.
//
// TWO POINTS AT THE SAME PLACE DEFINE NO PLANE. Revit would throw; this reports
// `degenerate` and changes nothing, because "your axis has no length" is an
// answer somebody can act on and an exception is not.

var copies = new List<ElementId>();
var mirrored = 0;
var degenerate = false;

var along = planeTo - planeFrom;

// Only the plan direction matters - the plane is vertical whatever height the
// two points were picked at.
var flat = new XYZ(along.X, along.Y, 0);

if (flat.GetLength() < (1.0 / 304.8))
{
    degenerate = true;
}
else
{
    // The plane's normal is the plan direction turned through a right angle.
    var normal = new XYZ(-flat.Y, flat.X, 0).Normalize();
    var plane = Plane.CreateByNormalAndOrigin(normal, planeFrom);

    var ids = new List<ElementId>();
    foreach (var element in elements)
    {
        if (element != null) ids.Add(element.Id);
    }

    if (ids.Count > 0)
    {
        if (asCopy)
        {
            // The copies come back, so this mode is self-evidencing in the way
            // COPY_ELEMENTS is - no position probe needed or possible, since
            // the originals are meant to be exactly where they were.
            var made = ElementTransformUtils.MirrorElements(doc, ids, plane, true);
            if (made != null) copies.AddRange(made);
            mirrored = copies.Count;
        }
        else
        {
            ElementTransformUtils.MirrorElements(doc, ids, plane, false);
            mirrored = ids.Count;
        }
    }
}
