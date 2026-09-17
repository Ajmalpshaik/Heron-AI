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

        // MODEL SPACE, AND THAT IS THE WHOLE POINT.
        //
        // get_BoundingBox(VIEW) returns a box whose Min/Max are expressed in
        // that box's OWN Transform, which this fragment then read as if they
        // were model coordinates - and combined across elements that need not
        // share a transform at all. Passing null asks for the model box with an
        // identity transform, which is the only space in which two elements'
        // extents can honestly be merged.
        //
        // Found 2026-09-17 by the owner LOOKING AT THE SCREEN: the crop landed
        // "only one side and very less", and the selected wall was outside it.
        // Nothing in the reply could have said so - see the note at the bottom.
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
        // Nothing measurable. The existing crop is LEFT ALONE rather than
        // replaced with a degenerate region, which would show an empty drawing.
        viewRefused = true;
    }
    else
    {
        var margin = marginMm / MillimetresPerFoot;

        // A CROP BOX IS READ IN ITS OWN TRANSFORM, NEVER IN MODEL COORDINATES.
        //
        // `new BoundingBoxXYZ()` carries an IDENTITY transform. Handing model
        // X/Y/Z to a box like that and assigning it to view.CropBox puts the
        // crop wherever the view's real transform happens to send those
        // numbers - which on a plan view is somewhere else entirely. The view
        // reports CropBoxActive true afterwards either way, so the fragment
        // said `applied true, enclosed 7` while cropping the wrong place.
        //
        // The view's OWN box is taken here and only its Min/Max are moved, so
        // whatever transform the view carries is preserved rather than
        // replaced.
        var region = view.CropBox;
        var toCrop = region.Transform.Inverse;

        // ALL EIGHT CORNERS, because the transform can rotate. Sending only the
        // two opposite corners through it gives the box's diagonal in crop
        // space rather than its extent, and a rotated view would crop to
        // something smaller than what was asked for - narrowing on one side,
        // which is exactly how this was spotted.
        var corners = new List<XYZ>
        {
            new XYZ(minX, minY, minZ), new XYZ(maxX, minY, minZ),
            new XYZ(minX, maxY, minZ), new XYZ(maxX, maxY, minZ),
            new XYZ(minX, minY, maxZ), new XYZ(maxX, minY, maxZ),
            new XYZ(minX, maxY, maxZ), new XYZ(maxX, maxY, maxZ)
        };

        var first = toCrop.OfPoint(corners[0]);
        double cropMinX = first.X, cropMinY = first.Y;
        double cropMaxX = first.X, cropMaxY = first.Y;

        foreach (var corner in corners)
        {
            var local = toCrop.OfPoint(corner);
            if (local.X < cropMinX) cropMinX = local.X;
            if (local.Y < cropMinY) cropMinY = local.Y;
            if (local.X > cropMaxX) cropMaxX = local.X;
            if (local.Y > cropMaxY) cropMaxY = local.Y;
        }

        // Z IS LEFT EXACTLY AS THE VIEW HAD IT, and that is deliberate. On a
        // plan view the crop box's Z is the view DEPTH - the front and back
        // clipping planes - not the height of anything being enclosed. Writing
        // the elements' own Z into it clips the view to the thickness of what
        // was selected, which is a second way to make the very thing you asked
        // to see disappear.
        region.Min = new XYZ(cropMinX - margin, cropMinY - margin, region.Min.Z);
        region.Max = new XYZ(cropMaxX + margin, cropMaxY + margin, region.Max.Z);

        // A SHAPED CROP OUTRANKS THE BOX, AND SETTING THE BOX UNDER ONE DOES
        // NOTHING AT ALL.
        //
        // If SET_VIEW_CROP_TO_SHAPE has given this view a non-rectangular crop,
        // Revit follows the SHAPE and the rectangle written below is simply
        // ignored - silently. The view keeps the old outline at the old size, so
        // every margin asked for lands nowhere, while CropBoxActive still reads
        // back true and this fragment still reports `applied true`.
        //
        // FOUND 2026-09-17, AND ONLY BY LOOKING. Six runs at 500 mm and 3000 mm
        // all reported success on a view whose crop never moved off 8800 x 6800
        // - the outline of the room a shaped crop had been set to earlier. The
        // owner saw one wall at the left edge and 27 missing, and said so; no
        // number in any reply could have.
        //
        // The shape is removed rather than refused, because the caller asking
        // for a rectangle with a margin has said plainly which of the two they
        // want.
        var shapeManager = view.GetCropRegionShapeManager();
        if (shapeManager != null && shapeManager.ShapeSet)
        {
            shapeManager.RemoveCropRegionShape();
        }

        view.CropBox = region;
        view.CropBoxActive = true;
        view.CropBoxVisible = true;

        // NOT EVIDENCE THAT THE CROP IS IN THE RIGHT PLACE, and it never was.
        // CropBoxActive reads back true whatever region was written, so this
        // says the crop is ON and nothing more. `enclosed` counts what was
        // MEASURED, not what ended up inside. Neither could see the defect
        // above, and a person looking at the view found it in seconds.
        applied = view.CropBoxActive;
    }
}
