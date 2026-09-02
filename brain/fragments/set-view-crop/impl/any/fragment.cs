// NOT STANDALONE. Assumes `view`, `elements` and `marginMm` are in scope;
// leaves `applied`, `enclosed`, `noGeometry` and `viewRefused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE BOUNDING BOXES ARE ASKED FOR IN THIS VIEW, not in model space, and that
// is the difference between this fragment and SET_VIEW_SECTION_BOX. A crop
// region lives in the VIEW's coordinates: passing null here would give model
// coordinates and crop the wrong rectangle, on a view whose plane is not the
// model's.
//
// SET AND SWITCH ON, BOTH. A crop box that is set and inactive leaves the view
// looking exactly as it did, which reads as the call having done nothing at
// all. `CropBoxVisible` is left alone deliberately - whether the crop boundary
// is drawn is a drafting preference and not part of this instruction.
//
// AN ELEMENT WITH NO BOUNDING BOX IN THIS VIEW IS NAMED. It may be hidden,
// outside the view range, or on a different level - all normal, and all
// meaning it cannot contribute to the region. Folding a null in would stretch
// the crop to the project origin and enclose the whole drawing, which looks
// exactly like the request being ignored.
//
// A CROP CHANGES WHAT PRINTS. If this view is on a sheet, whatever falls
// outside is gone from the issued drawing and nobody reviewing it can tell it
// was ever there.

var applied = false;
var enclosed = 0;
var noGeometry = new List<ElementId>();
var viewRefused = false;

const double MillimetresPerFoot = 304.8;

// A schedule or a legend has no crop region to set. `CanBePrinted` is the
// property that separates a drawable view from one that is not, and a template
// is refused because cropping one would reach every view using it.
if (view == null || view.IsTemplate || !view.CanBePrinted)
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

        var box = element.get_BoundingBox(view);
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
        // Nothing measurable. The existing crop is LEFT ALONE rather than
        // replaced with a degenerate region, which would show an empty drawing.
        viewRefused = true;
    }
    else
    {
        var margin = marginMm / MillimetresPerFoot;

        var region = new BoundingBoxXYZ();
        region.Min = new XYZ(minX - margin, minY - margin, minZ - margin);
        region.Max = new XYZ(maxX + margin, maxY + margin, maxZ + margin);

        view.CropBox = region;
        view.CropBoxActive = true;

        applied = view.CropBoxActive;
    }
}
