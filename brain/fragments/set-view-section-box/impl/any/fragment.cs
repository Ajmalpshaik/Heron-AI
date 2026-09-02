// NOT STANDALONE. Assumes `view`, `elements` and `marginMm` are in scope;
// leaves `applied`, `enclosed`, `noGeometry` and `viewRefused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ONLY A 3D VIEW HAS A SECTION BOX. Refused by name on anything else rather
// than returning quietly - the user is watching for the view to change, and a
// silent no-op reads as a broken tool.
//
// THE BOX IS BUILT FROM MODEL-COORDINATE BOUNDING BOXES. `get_BoundingBox(null)`
// asks for the element's extent in model space rather than as cropped by some
// view, which is what a section box is set in. Passing a view here would ask
// "how big is it on that drawing" and box the wrong volume.
//
// AN ELEMENT WITH NO BOUNDING BOX IS NAMED, NOT TREATED AS THE ORIGIN. A box
// silently stretched to 0,0,0 encloses the whole building and looks like the
// tool ignored the request - which is what would happen if a null were folded
// into the min/max without being noticed.
//
// MILLIMETRES TO FEET IS ARITHMETIC (D-20): 1 ft = 304.8 mm exactly. Nothing
// for 2021's unit rewrite to reach.

var applied = false;
var enclosed = 0;
var noGeometry = new List<ElementId>();
var viewRefused = false;

const double MillimetresPerFoot = 304.8;

var view3D = view as View3D;
if (view3D == null || view3D.IsTemplate)
{
    viewRefused = true;
}
else
{
    double minX = 0, minY = 0, minZ = 0, maxX = 0, maxY = 0, maxZ = 0;
    var any = false;

    foreach (var element in elements)
    {
        if (element == null) continue;

        var box = element.get_BoundingBox(null);
        if (box == null)
        {
            noGeometry.Add(element.Id);
            continue;
        }

        if (!any)
        {
            minX = box.Min.X; minY = box.Min.Y; minZ = box.Min.Z;
            maxX = box.Max.X; maxY = box.Max.Y; maxZ = box.Max.Z;
            any = true;
        }
        else
        {
            if (box.Min.X < minX) minX = box.Min.X;
            if (box.Min.Y < minY) minY = box.Min.Y;
            if (box.Min.Z < minZ) minZ = box.Min.Z;
            if (box.Max.X > maxX) maxX = box.Max.X;
            if (box.Max.Y > maxY) maxY = box.Max.Y;
            if (box.Max.Z > maxZ) maxZ = box.Max.Z;
        }

        enclosed++;
    }

    if (!any)
    {
        // Nothing measurable. The existing section box is LEFT ALONE rather
        // than set to a degenerate volume - a zero-sized box shows an empty
        // view, which reads as "everything was deleted".
        viewRefused = true;
    }
    else
    {
        var margin = marginMm / MillimetresPerFoot;

        var box = new BoundingBoxXYZ();
        box.Min = new XYZ(minX - margin, minY - margin, minZ - margin);
        box.Max = new XYZ(maxX + margin, maxY + margin, maxZ + margin);

        view3D.SetSectionBox(box);

        // Setting the box does not switch it on. A view whose section box is
        // inactive looks exactly as it did, which would read as the call having
        // done nothing at all.
        view3D.IsSectionBoxActive = true;

        applied = view3D.IsSectionBoxActive;
    }
}
