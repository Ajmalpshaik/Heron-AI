// NOT STANDALONE. Assumes `doc`, `view`, `boundary` and `joinTolerance` are in
// scope; leaves `applied`, `worstGap` and `refused` behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Tolerances are internal FEET.
//
// SET_VIEW_CROP CANNOT DO THIS. That one sets the crop BOX - two corners and a
// transform, so it can only ever be a rectangle. A shaped crop goes through the
// view's crop-region shape manager, which is a different mechanism.
//
// REVIT REQUIRES THREE THINGS OF THE LOOP AND GIVES THE SAME UNHELPFUL ERROR
// FOR ALL THREE: closed end to end, flat and in the view's plane, and not
// self-intersecting. The curves are CHAINED here rather than trusted in the
// order they arrived, because lines drawn by eye come back in drawing order and
// almost never close. The worst gap bridged is REPORTED so a shape that was
// never really closed does not pass as one that was.
//
// A CROP THAT IS OFF LOOKS EXACTLY LIKE A CROP THAT DID NOT APPLY. Both the
// crop and its visibility are switched on here.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
joinTolerance = joinTolerance / MillimetresPerFoot;

var applied = false;
var worstGap = 0.0;
var refused = new List<string>();

if (view.IsTemplate)
{
    refused.Add(string.Format("'{0}' is a view TEMPLATE - a crop shape is set on a real view",
        view.Name));
}
else if (view is ViewSheet)
{
    // A SHEET IS THE PAPER, NOT THE DRAWING, and it has no crop region at all.
    // Cropping "the sheet" is not a thing anybody wants - what they want is the
    // VIEW placed on it cropped, which then shows cropped on the sheet. So this
    // is a request that cannot be honoured rather than one that failed, and
    // saying which is the difference between sending somebody to look and
    // sending them away satisfied.
    //
    // REFUSED EXPLICITLY, BEFORE THE CROP IS SWITCHED ON, and that order is the
    // whole reason this guard exists. Forcing CropBoxActive onto a ViewSheet
    // flips CanHaveShape to TRUE, and SetCropShape then reports success while
    // nothing anybody can see has changed - measured on `test projject.rvt`,
    // 2026-09-17, against 'PLAIN SHEET'. A false success is worse than the
    // false refusal it replaced: a refusal makes somebody go and look.
    refused.Add(string.Format("'{0}' is a SHEET - crop the VIEW placed on it, not the sheet",
        view.Name));
}
else if (boundary == null || boundary.Count < 3)
{
    refused.Add("a crop shape needs at least three curves forming a closed loop");
}
else
{
    // SWITCH THE CROP ON BEFORE ASKING WHETHER IT CAN BE SHAPED.
    //
    // CanHaveShape is FALSE on a view whose crop is merely switched off, which
    // is the ordinary state of most views. Asking first and switching on
    // afterwards refused every one of them with "cannot take a shaped crop at
    // all" - which reads as the VIEW TYPE being incapable when it is nothing
    // of the kind, and sends somebody hunting for a limitation that is not
    // there. Found 2026-09-17 on an ordinary FloorPlan with a closed
    // four-curve boundary and worstGap 0.
    //
    // The lines further down set the same two parameters, and they are not
    // redundant with this: they run AFTER SetCropShape has succeeded, so they
    // can never rescue a view the guard below has already turned away.
    view.CropBoxActive = true;
    view.CropBoxVisible = true;

    var manager = view.GetCropRegionShapeManager();

    if (manager == null || !manager.CanHaveShape)
    {
        refused.Add(string.Format("'{0}' ({1}) cannot take a shaped crop at all", view.Name, view.ViewType));
    }
    else
    {
        // Chain the curves head to tail. The order they arrive in is drawing
        // order, which is not loop order, and CurveLoop.Create demands loop order.
        var remaining = new List<Curve>(boundary);
        var chain = new List<Curve>();

        chain.Add(remaining[0]);
        remaining.RemoveAt(0);
        var tail = chain[0].GetEndPoint(1);

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
                refused.Add(string.Format("the boundary does not close - the nearest unused curve is "
                    + "{0:0.#} mm away, past the {1:0.#} mm allowed. Lines drawn by eye almost never "
                    + "meet", bestDistance * 304.8, joinTolerance * 304.8));
                break;
            }

            if (bestDistance > worstGap) worstGap = bestDistance;

            var next = remaining[bestIndex];
            remaining.RemoveAt(bestIndex);
            var ordered = bestReversed ? next.CreateReversed() : next;
            chain.Add(ordered);
            tail = ordered.GetEndPoint(1);
        }

        if (remaining.Count == 0)
        {
            var closingGap = tail.DistanceTo(chain[0].GetEndPoint(0));
            if (closingGap > worstGap) worstGap = closingGap;

            if (closingGap > joinTolerance)
            {
                refused.Add(string.Format("the loop's two ends are {0:0.#} mm apart, past the {1:0.#} "
                    + "mm allowed - it is an open shape, not a crop boundary",
                    closingGap * 304.8, joinTolerance * 304.8));
            }
            else
            {
                try
                {
                    var loop = CurveLoop.Create(chain);

                    if (!manager.IsCropRegionShapeValid(loop))
                    {
                        refused.Add("Revit rejects this loop as a crop shape. It is closed, so the "
                            + "cause is one of the other two: the curves are not all in the view's own "
                            + "plane, or the shape crosses itself");
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
                        {
                            refused.Add(string.Format("the shape was repaired - the worst gap bridged "
                                + "was {0:0.##} mm. Check the crop against what was drawn if that "
                                + "sounds larger than a rounding error", worstGap * 304.8));
                        }
                    }
                }
                catch (Exception ex)
                {
                    refused.Add(string.Format("Revit refused the loop - {0}", ex.Message));
                }
            }
        }
    }
}
