// NOT STANDALONE. Assumes `doc`, `elements`, `targetVelocity`, `roundSizesMm`,
// `rectSizesMm` and `holdWidth` are in scope; leaves `sized`, `findings`,
// `noFlow` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). `targetVelocity` is m/s.
//
// ===========================================================================
// THE UNIT ASSUMPTION IS PRINTED BEFORE ANYTHING IS WRITTEN.
// ===========================================================================
//
// Revit's internal airflow unit is TAKEN to be cubic feet per second and
// converted at 0.028316846592 m3 per ft3 - exact arithmetic if that assumption
// holds, since 1 ft = 0.3048 m by definition. Reflection over an assembly shows
// a property type, never a unit.
//
// So the first line of the report puts the RAW internal value beside Revit's
// own display string for the same duct. Wrong assumption, and those two
// disagree obviously on the first run, before a single size is written. That
// line is the check; read it.
//
// IT ALWAYS ROUNDS UP. Rounding down to the nearer standard size pushes the
// velocity ABOVE the design figure, and that is the direction that causes noise
// complaints and rework.
//
// ZERO FLOW IS NOT A SIZE. A duct connected to nothing carries zero, and zero
// sizes to nothing. Those are reported as NO FLOW, never sized to the smallest
// size in the list - a whole run with no flow is a connectivity problem and
// belongs to TRACE_CONNECTIVITY.
//
// SHAPE IS READ, NEVER GUESSED. Setting a diameter on a rectangular duct is
// refused by Revit. Which parameters the duct actually has is what decides.

var sized = 0;
var findings = new List<string>();
var noFlow = new List<ElementId>();
var refused = new List<string>();

var mmPerFoot = 304.8;
var cubicMetresPerCubicFoot = 0.028316846592;

// Ascending, so the first one big enough is the next size UP.
var round = roundSizesMm.OrderBy(v => v).ToList();
var rect = rectSizesMm.OrderBy(v => v).ToList();

Func<List<double>, double, double> nextSizeUp = (sizes, needMm) =>
{
    foreach (var size in sizes) if (size >= needMm) return size;
    return -1.0;
};

if (targetVelocity <= 0)
{
    findings.Add("A target velocity is needed and there is no default worth trusting - a main at 7 m/s "
        + "and a branch at 4 m/s give completely different ducts for the same air");
}
else
{
    // THE UNIT SANITY LINE. First readable duct only; this is a check, not a
    // report.
    foreach (var element in elements)
    {
        var flowParameter = element == null ? null : element.get_Parameter(BuiltInParameter.RBS_DUCT_FLOW_PARAM);
        if (flowParameter == null || !flowParameter.HasValue) continue;
        var raw = flowParameter.AsDouble();
        var shown = "";
        try { shown = flowParameter.AsValueString(); } catch { }
        findings.Add(string.Format(
            "UNIT CHECK on {0}: internal {1:0.000000} read as {2:0.0} L/s, and Revit itself shows '{3}'. "
            + "If those disagree, STOP - every size below is wrong by the same factor",
            element.Id, raw, raw * cubicMetresPerCubicFoot * 1000.0, shown));
        break;
    }

    foreach (var element in elements)
    {
        if (element == null) continue;

        var flowParameter = element.get_Parameter(BuiltInParameter.RBS_DUCT_FLOW_PARAM);
        if (flowParameter == null || !flowParameter.HasValue) { noFlow.Add(element.Id); continue; }

        var flow = flowParameter.AsDouble() * cubicMetresPerCubicFoot;
        if (flow <= 1e-9) { noFlow.Add(element.Id); continue; }

        var neededArea = flow / targetVelocity;

        var diameter = element.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
        var width = element.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
        var height = element.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);

        var isRound = diameter != null && diameter.HasValue && (width == null || !width.HasValue);

        try
        {
            if (isRound)
            {
                var neededMm = Math.Sqrt(4.0 * neededArea / Math.PI) * 1000.0;
                var pick = nextSizeUp(round, neededMm);
                if (pick < 0)
                {
                    refused.Add(string.Format("{0}: needs {1:0} mm diameter, past the largest size given",
                        element.Id, neededMm));
                    continue;
                }

                var was = diameter.AsDouble() * mmPerFoot;
                diameter.Set(pick / mmPerFoot);
                doc.Regenerate();

                // READ IT BACK. Revit snaps a size to what the type allows and
                // returns success while doing it - the whole reason SET_MEP_SIZE
                // exists as its own capability.
                var got = diameter.AsDouble() * mmPerFoot;
                var velocity = flow / (Math.PI * Math.Pow(got / 1000.0, 2) / 4.0);

                sized++;
                findings.Add(string.Format("{0}: {1:0} -> {2:0} mm round, {3:0.0} L/s at {4:0.0} m/s{5}",
                    element.Id, was, got, flow * 1000.0, velocity,
                    Math.Abs(got - pick) > 0.5
                        ? string.Format("  [asked {0:0}, the type gave {1:0}]", pick, got) : ""));
            }
            else if (width != null && height != null && width.HasValue && height.HasValue)
            {
                var wasW = width.AsDouble() * mmPerFoot;
                var wasH = height.AsDouble() * mmPerFoot;
                var neededAreaMm = neededArea * 1e6;

                var newW = wasW;
                var newH = wasH;

                if (holdWidth)
                {
                    var pick = nextSizeUp(rect, neededAreaMm / Math.Max(wasW, 1.0));
                    if (pick < 0)
                    {
                        refused.Add(string.Format("{0}: needs {1:0} mm high at {2:0} wide, past the "
                            + "largest size given", element.Id, neededAreaMm / Math.Max(wasW, 1.0), wasW));
                        continue;
                    }
                    newH = pick;
                    height.Set(newH / mmPerFoot);
                }
                else
                {
                    var pick = nextSizeUp(rect, neededAreaMm / Math.Max(wasH, 1.0));
                    if (pick < 0)
                    {
                        refused.Add(string.Format("{0}: needs {1:0} mm wide at {2:0} high, past the "
                            + "largest size given", element.Id, neededAreaMm / Math.Max(wasH, 1.0), wasH));
                        continue;
                    }
                    newW = pick;
                    width.Set(newW / mmPerFoot);
                }

                doc.Regenerate();

                var gotW = width.AsDouble() * mmPerFoot;
                var gotH = height.AsDouble() * mmPerFoot;
                var velocity = flow / ((gotW / 1000.0) * (gotH / 1000.0));
                var aspect = Math.Max(gotW, gotH) / Math.Max(1.0, Math.Min(gotW, gotH));

                sized++;
                findings.Add(string.Format(
                    "{0}: {1:0}x{2:0} -> {3:0}x{4:0} mm, {5:0.0} L/s at {6:0.0} m/s, aspect {7:0.0}:1{8}",
                    element.Id, wasW, wasH, gotW, gotH, flow * 1000.0, velocity, aspect,
                    aspect > 4.0 ? "  [FLAT - over 4:1, and hard to fit or fabricate]" : ""));
            }
            else
            {
                refused.Add(string.Format("{0}: its shape could not be read - it carries neither a "
                    + "usable diameter nor a width and height", element.Id));
            }
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("{0}: {1}", element.Id, ex.Message));
        }
    }

    findings.Add(string.Format("{0} duct(s) sized at {1:0.0} m/s. {2} carried NO FLOW and were left "
        + "alone - if that is most of them, the system is not connected and sizing is not the problem",
        sized, targetVelocity, noFlow.Count));
}
