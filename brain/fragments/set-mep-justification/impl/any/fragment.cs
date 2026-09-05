// NOT STANDALONE. Assumes `doc`, `elements`, `horizontalOffsetMm` and
// `verticalOffsetMm` are in scope; leaves `set`, `withoutOffsets` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THIS IS WHAT "ALIGN THE TOPS" MEANS ON MEP. A 400 deep duct reducing to a 250
// keeps its CENTRELINE by default, so the top steps up and the bottom steps
// down at the transition. Above a ceiling only the top matters.
//
// THE OFFSETS ARE WRITTEN, NOT THE DROPDOWN. Revit shows justification as a
// choice and stores the result as a pair of offsets from the curve's reference.
// What this does NOT do is work out the offset that means "top" for a given
// size - that depends on the run's own height, and handing the number in keeps
// the arithmetic somewhere it can be seen.
//
// AN ELEMENT WITHOUT THE OFFSETS IS COUNTED, NOT SKIPPED. Fittings, accessories
// and flex do not carry them, and "twelve set" when nine could take the setting
// leaves three stepping with nothing saying which.
//
// EACH VALUE IS READ BACK. A parameter that refuses returns without complaint
// on some element types.

const double MillimetresPerFoot = 304.8;

var set = 0;
var withoutOffsets = 0;
var findings = new List<string>();

if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were given - hand in the ducts or pipes to set out");
}
else
{
    var wantedHorizontal = horizontalOffsetMm / MillimetresPerFoot;
    var wantedVertical = verticalOffsetMm / MillimetresPerFoot;

    var refused = new List<string>();
    var notCurves = 0;

    foreach (var element in elements)
    {
        if (element == null) continue;

        var curve = element as MEPCurve;
        if (curve == null) { notCurves++; continue; }

        var horizontal = curve.get_Parameter(BuiltInParameter.RBS_CURVE_HOR_OFFSET_PARAM);
        var vertical = curve.get_Parameter(BuiltInParameter.RBS_CURVE_VERT_OFFSET_PARAM);

        if (horizontal == null && vertical == null) { withoutOffsets++; continue; }

        var wrote = 0;
        var wanted = 0;

        try
        {
            if (horizontal != null && !horizontal.IsReadOnly)
            {
                wanted++;
                horizontal.Set(wantedHorizontal);
                if (Math.Abs(horizontal.AsDouble() - wantedHorizontal) < 1e-6) wrote++;
            }

            if (vertical != null && !vertical.IsReadOnly)
            {
                wanted++;
                vertical.Set(wantedVertical);
                if (Math.Abs(vertical.AsDouble() - wantedVertical) < 1e-6) wrote++;
            }
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("id {0}: {1}", element.Id, ex.Message));
            continue;
        }

        if (wanted == 0) { withoutOffsets++; continue; }

        if (wrote == wanted) set++;
        else refused.Add(string.Format("id {0}: {1} of {2} offset(s) did not take the value asked for",
            element.Id, wanted - wrote, wanted));
    }

    findings.Add(string.Format("{0} run(s) set to {1:0.#} mm horizontal and {2:0.#} mm vertical "
        + "offset. {3} carry no offsets at all, and {4} of what was handed in are not duct or pipe "
        + "runs",
        set, horizontalOffsetMm, verticalOffsetMm, withoutOffsets, notCurves));

    foreach (var failure in refused) findings.Add("Refused: " + failure);

    findings.Add("This writes the OFFSETS. Whether they place the run where Revit's own "
        + "justification dropdown would has not been watched on a model - look at a size change in "
        + "section before trusting a batch of this");
}
