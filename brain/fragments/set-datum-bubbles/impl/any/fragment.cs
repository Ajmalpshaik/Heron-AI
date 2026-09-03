// NOT STANDALONE. Assumes `doc`, `view`, `elements` and `mode` are in scope;
// leaves `changed`, `findings` and `notDatums` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// BUBBLES ARE PER END AND PER VIEW, and this changes ONE view. That is correct
// for bubbles and is NOT how datum extents behave, where the model type is
// shared across views - confusing the two is how somebody "fixes" one plan and
// finds they have changed twenty.
//
// FLIP IS NOT ONE CALL. Revit offers show, hide and ask, per end. So a flip is
// read both ends, then hide one and show the other. Two cases have no obvious
// flip and are decided here rather than silently doing nothing:
//
//   NEITHER end visible -> show End0. Something has to appear, or the flip
//                          looks broken.
//   BOTH ends visible   -> hide End1, leaving one, which is what flipped means.
//
// END0 AND END1 ARE THE DATUM'S OWN ENDS, NOT LEFT AND RIGHT. Which looks left
// depends on the direction the grid was drawn in and on the view. A batch that
// flips half of them the way you wanted means those grids were drawn in
// opposite directions - the model, not this fragment - and the fix is to name
// the end instead.
//
// EVERY CHANGE IS READ BACK. Ask the view again; a call that returned is not a
// bubble that moved.

var changed = 0;
var findings = new List<string>();
var notDatums = new List<ElementId>();

var how = (mode ?? "").Trim().ToLower();
var known = how == "flip" || how == "end0" || how == "end1" || how == "both" || how == "none";

// Bubbles mean nothing outside a plan, section or elevation.
var viewTakesBubbles = view.ViewType == ViewType.FloorPlan
    || view.ViewType == ViewType.CeilingPlan
    || view.ViewType == ViewType.EngineeringPlan
    || view.ViewType == ViewType.AreaPlan
    || view.ViewType == ViewType.Section
    || view.ViewType == ViewType.Elevation
    || view.ViewType == ViewType.Detail;

if (!known)
{
    findings.Add(string.Format("'{0}' is not a mode this understands - ask for \"flip\", \"end0\", "
        + "\"end1\", \"both\" or \"none\". Showing and hiding are opposite instructions and neither is "
        + "a safe default", mode));
}
else if (!viewTakesBubbles)
{
    findings.Add(string.Format("'{0}' is a {1}, where a bubble means nothing. Bubbles live in plans, "
        + "sections and elevations", view.Name, view.ViewType));
}
else
{
    foreach (var element in elements)
    {
        if (element == null) continue;

        var datum = element as DatumPlane;
        if (datum == null) { notDatums.Add(element.Id); continue; }

        try
        {
            var at0 = datum.IsBubbleVisibleInView(DatumEnds.End0, view);
            var at1 = datum.IsBubbleVisibleInView(DatumEnds.End1, view);

            bool want0, want1;

            if (how == "both") { want0 = true; want1 = true; }
            else if (how == "none") { want0 = false; want1 = false; }
            else if (how == "end0") { want0 = true; want1 = false; }
            else if (how == "end1") { want0 = false; want1 = true; }
            else
            {
                // flip - and the two ambiguous cases are decided, not skipped.
                if (!at0 && !at1) { want0 = true; want1 = false; }
                else if (at0 && at1) { want0 = true; want1 = false; }
                else { want0 = at1; want1 = at0; }
            }

            if (want0 == at0 && want1 == at1) continue;

            if (want0) datum.ShowBubbleInView(DatumEnds.End0, view);
            else datum.HideBubbleInView(DatumEnds.End0, view);

            if (want1) datum.ShowBubbleInView(DatumEnds.End1, view);
            else datum.HideBubbleInView(DatumEnds.End1, view);

            // READ IT BACK off the view.
            var now0 = datum.IsBubbleVisibleInView(DatumEnds.End0, view);
            var now1 = datum.IsBubbleVisibleInView(DatumEnds.End1, view);

            if (now0 == want0 && now1 == want1) changed++;
            else findings.Add(string.Format("{0} '{1}': the calls were accepted and the bubbles did not "
                + "move - a view template usually controls this", element.Id, element.Name));
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("{0}: {1}", element.Id, ex.Message));
        }
    }

    findings.Add(string.Format(
        "{0} datum(s) changed in '{1}' only - bubbles are per view, so no other drawing moved. If half "
        + "of a flipped batch went the way you wanted and half did not, those grids were drawn in "
        + "opposite directions: name the end instead of flipping", changed, view.Name));
}
