// NOT STANDALONE. Assumes `view`, `elements`, `marginMm`, `boundary` and
// `joinToleranceMm` are in scope; leaves `applied`, `enclosed`, `noGeometry`,
// `viewRefused`, `worstGap` and `refused` behind.
//
// VERSION 2 (2026-11-01) - SET_VIEW_CROP_TO_SHAPE MERGED IN, on the owner's
// instruction ("make sure there is no duplicate tools"). A crop is a crop: a
// rectangle round elements, or - with `boundary` - a SHAPED one from a closed
// loop of curves, an L round a wing, a stepped match line, a room's outline.
// The crop BOX can only ever be a rectangle; a shape goes through the view's
// crop-region shape manager, a different mechanism, and this does both:
//
//   REVIT SILENTLY REQUIRES THREE THINGS OF THE LOOP AND GIVES THE SAME
//   UNHELPFUL ERROR FOR ALL THREE: closed end to end, flat in the view's own
//   plane, and not crossing itself. The curves are CHAINED head to tail here,
//   because lines drawn by eye arrive in drawing order and almost never close;
//   a gap within `joinToleranceMm` is bridged and the WORST GAP BRIDGED IS
//   REPORTED, so a shape that was never really closed does not pass as one
//   that was. A gap past it is refused with the distance in millimetres.
//
//   THE CROP IS SWITCHED ON BEFORE CanHaveShape IS ASKED - it reads false on a
//   view whose crop is merely off, the ordinary state of most views - and its
//   visibility on afterwards, because a shaped crop on a view with cropping
//   off changes nothing anybody can see.
//
//   A SHEET IS REFUSED BEFORE THE CROP IS SWITCHED ON - forcing CropBoxActive
//   onto a sheet flips CanHaveShape true and SetCropShape then reports success
//   while nothing changes (measured 2026-09-17). A template is refused too.
//
//   ONE VIEW. A shape that fits one view rarely fits another.
//
// `refused` says, in words, why nothing was done on either road, and a
// repaired shape's bridged gap. With no `boundary` the rectangle road runs
// exactly as version 1, and a call that gives neither elements nor a loop is
// refused.
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
var worstGap = 0.0;
var refused = new List<string>();

const double MillimetresPerFoot = 304.8;

// VERSION 2: a loop of curves asks for a SHAPED crop. Left out it binds empty,
// and the rectangle road below is version 1's.
var shapeAsked = boundary != null && boundary.Count > 0;

// A schedule or a legend has no crop region to set. `CanBePrinted` is the
// property that separates a drawable view from one that is not, and a template
// is refused because cropping one would reach every view using it.
//
// A SHEET IS REFUSED FIRST, AND `CanBePrinted` IS WHY IT HAD TO BE.
//
// A sheet is the most printable thing in the model, so that test answers TRUE
// for one - it separates drawable from not-drawable, which is a different
// question from *does this have a crop region*. Measured 2026-09-13 on sheet
// `A101` of `Project1 work_ajmal.al`, handed 9 ducts that live in `1 - Mech`:
// **`applied true`, `enclosed 9`, `viewRefused false`**. `CropBoxActive` was
// set and read back as true on the sheet, so even the read-back agreed - it is
// the question that was wrong, not the answer. FRAGMENT-ISSUES rows 20 and 24.
//
// AND THIS ONE REFUSES THE WHOLE SHEET WHERE `hide-elements` AND
// `isolate-elements` REJECT ONLY THE ELEMENTS, WHICH IS A DIFFERENCE ON
// PURPOSE. Those two can be asked to hide or isolate something that genuinely
// lives on the sheet - a text note, a revision cloud, a title block - so
// refusing the view would block a real request, and they drop the off-sheet
// elements instead. A CROP REGION IS NOT LIKE THAT: a sheet has none at all,
// so there is no element, on the sheet or off it, that would make this call
// mean something. There is nothing narrower to refuse than the view.
if (view == null || view is ViewSheet || view.IsTemplate || !view.CanBePrinted)
{
    viewRefused = true;
    refused.Add(view == null ? "no view was given"
        : view is ViewSheet ? string.Format("'{0}' is a SHEET - crop the VIEW placed on it, not the sheet", view.Name)
        : view.IsTemplate ? string.Format("'{0}' is a view TEMPLATE - a crop is set on a real view, and cropping a "
            + "template would reach every view using it", view.Name)
        : string.Format("'{0}' ({1}) has no crop region to set", view.Name, view.ViewType));
}
else if (shapeAsked)
{
    // ---- VERSION 2: A SHAPED CROP (from SET_VIEW_CROP_TO_SHAPE) ----
    // MILLIMETRES IN, FEET INSIDE (D-71).
    double toleranceMm;
    if (!double.TryParse((joinToleranceMm ?? "").Trim(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out toleranceMm) || toleranceMm < 0)
        toleranceMm = -1;
    var joinTolerance = toleranceMm / MillimetresPerFoot;

    if (toleranceMm < 0)
    {
        viewRefused = true;
        refused.Add(string.Format("\"{0}\" is not a join tolerance - millimetres, such as 1", joinToleranceMm));
    }
    else if (boundary.Count < 3)
    {
        viewRefused = true;
        refused.Add("a crop shape needs at least three curves forming a closed loop");
    }
    else
    {
        // SWITCH THE CROP ON BEFORE ASKING WHETHER IT CAN BE SHAPED - see the
        // header. The lines further down set the same two parameters AFTER
        // SetCropShape has succeeded, so they can never rescue a view the
        // guard below has turned away.
        view.CropBoxActive = true;
        view.CropBoxVisible = true;

        var manager = view.GetCropRegionShapeManager();
        if (manager == null || !manager.CanHaveShape)
        {
            viewRefused = true;
            refused.Add(string.Format("'{0}' ({1}) cannot take a shaped crop at all", view.Name, view.ViewType));
        }
        else
        {
            // Chain the curves head to tail. The order they arrive in is
            // drawing order, which is not loop order.
            var remaining = new List<Curve>(boundary);
            var chain = new List<Curve>();
            chain.Add(remaining[0]);
            remaining.RemoveAt(0);
            var tail = chain[0].GetEndPoint(1);
            var broken = false;

            while (remaining.Count > 0)
            {
                var bestIndex = -1;
                var bestDistance = double.MaxValue;
                var bestReversed = false;
                for (var i = 0; i < remaining.Count; i++)
                {
                    var toStart = tail.DistanceTo(remaining[i].GetEndPoint(0));
                    var toEnd = tail.DistanceTo(remaining[i].GetEndPoint(1));
                    if (toStart < bestDistance) { bestDistance = toStart; bestIndex = i; bestReversed = false; }
                    if (toEnd < bestDistance) { bestDistance = toEnd; bestIndex = i; bestReversed = true; }
                }
                if (bestIndex < 0 || bestDistance > joinTolerance)
                {
                    refused.Add(string.Format("the boundary does not close - the nearest unused curve is {0:0.#} mm "
                        + "away, past the {1:0.#} mm allowed. Lines drawn by eye almost never meet",
                        bestDistance * MillimetresPerFoot, toleranceMm));
                    broken = true;
                    break;
                }
                if (bestDistance > worstGap) worstGap = bestDistance;
                var next = remaining[bestIndex];
                remaining.RemoveAt(bestIndex);
                var ordered = bestReversed ? next.CreateReversed() : next;
                chain.Add(ordered);
                tail = ordered.GetEndPoint(1);
            }

            if (broken) viewRefused = true;
            else
            {
                var closingGap = tail.DistanceTo(chain[0].GetEndPoint(0));
                if (closingGap > worstGap) worstGap = closingGap;
                if (closingGap > joinTolerance)
                {
                    viewRefused = true;
                    refused.Add(string.Format("the loop's two ends are {0:0.#} mm apart, past the {1:0.#} mm allowed - "
                        + "it is an open shape, not a crop boundary", closingGap * MillimetresPerFoot, toleranceMm));
                }
                else
                {
                    try
                    {
                        var loop = CurveLoop.Create(chain);
                        if (!manager.IsCropRegionShapeValid(loop))
                        {
                            viewRefused = true;
                            refused.Add("Revit rejects this loop as a crop shape. It is closed, so the cause is one "
                                + "of the other two: the curves are not all in the view's own plane, or the shape "
                                + "crosses itself");
                        }
                        else
                        {
                            manager.SetCropShape(loop);

                            // A shaped crop on a view with cropping off changes
                            // nothing anybody can see.
                            var cropOn = view.get_Parameter(BuiltInParameter.VIEWER_CROP_REGION);
                            var cropVisible = view.get_Parameter(BuiltInParameter.VIEWER_CROP_REGION_VISIBLE);
                            if (cropOn != null && !cropOn.IsReadOnly) cropOn.Set(1);
                            if (cropVisible != null && !cropVisible.IsReadOnly) cropVisible.Set(1);
                            applied = true;

                            if (worstGap > 0)
                                refused.Add(string.Format("the shape was repaired - the worst gap bridged was {0:0.##} "
                                    + "mm. Check the crop against what was drawn if that sounds larger than a "
                                    + "rounding error", worstGap * MillimetresPerFoot));
                        }
                    }
                    catch (Exception ex)
                    {
                        viewRefused = true;
                        refused.Add(string.Format("Revit refused the loop - {0}", ex.Message));
                    }
                }
            }
        }
    }
}
else if (elements == null || elements.Count == 0)
{
    // VERSION 2: neither road was asked for.
    viewRefused = true;
    refused.Add("nothing to crop to - hand in the elements to crop round, or a closed loop of curves in boundary");
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
        refused.Add("none of the elements has a box to crop round, so the crop was left as it was");
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
        // If a shaped crop is on this view - version 2's boundary road, or SET_VIEW_CROP_TO_SHAPE before it -
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
