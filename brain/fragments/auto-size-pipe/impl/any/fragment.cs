// NOT STANDALONE. Assumes `doc`, `elements`, `targetVelocity`,
// `nominalSizesMm` and `boresMm` are in scope; leaves `sized`, `findings`,
// `noFlow`, `refused` and `velocityIsApproximate` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// UNITS ARE PLAIN ARITHMETIC AND NEVER A UNITS API (D-20). Revit's internal
// length is feet and its internal flow is cubic feet per second. 304.8 mm per
// foot and 0.028316846592 m3 per ft3 are both exact. Asking Revit to convert
// would mean DisplayUnitType before 2021 and ForgeTypeId from 2022 - the break
// this project refuses to write.
//
// NOMINAL SIZE IS NOT BORE, AND ON PIPE THAT DECIDES THE ANSWER. A pipe
// labelled 50 has a bore near 53 in steel and near 44 in thick-walled plastic.
// Velocity goes as the square of the bore, so using the label is wrong by a
// fifth exactly where noise and erosion bite. Bores are asked for separately
// and paired with the labels by position.
//
// WITHOUT THE BORES IT STILL SIZES AND SAYS SO ON EVERY ROW, with the DIRECTION
// of the error: a true bore larger than the label means the real velocity is
// lower than reported, which is safe; a bore smaller than the label means it is
// higher, which is the one that causes the complaint. An approximate number
// nobody flagged is the number that gets signed off.
//
// ALWAYS UP, NEVER TO NEAREST. Rounding down pushes velocity above the design
// figure - the direction that causes noise and rework.
//
// EVERY SIZE IS READ BACK. A pipe type whose segment offers only certain sizes
// refuses anything else, and refuses quietly. The velocity reported is computed
// on the size that came back.

var mmPerFoot = 304.8;
var cubicMetresPerCubicFoot = 0.028316846592;

var sized = 0;
var findings = new List<string>();
var noFlow = new List<ElementId>();
var refused = new List<string>();

var haveBores = boresMm != null && nominalSizesMm != null
    && boresMm.Count == nominalSizesMm.Count && boresMm.Count > 0;

var velocityIsApproximate = !haveBores;

if (targetVelocity <= 0.0)
{
    findings.Add("A target velocity is needed and there is no default worth trusting - a chilled "
        + "water main at 2.5 m/s and a branch at 1.0 m/s are both normal and give completely "
        + "different pipes for the same flow. Nothing was sized.");
}
else if (nominalSizesMm == null || nominalSizesMm.Count == 0)
{
    findings.Add("No size table was given. A size table is a project standard, not a constant, "
        + "so there is nothing here to snap up to. Nothing was sized.");
}
else
{
    if (boresMm != null && boresMm.Count > 0 && boresMm.Count != nominalSizesMm.Count)
    {
        findings.Add("The bore list and the size list are different lengths, so they cannot be "
            + "paired. The bores are ignored and every velocity below is approximate - pairing "
            + "them by guesswork would put a wrong bore against a right label.");
    }

    if (velocityIsApproximate)
    {
        findings.Add("VELOCITIES BELOW ARE APPROXIMATE - they are computed on the nominal label, "
            + "not on the true bore, because no bore list was given. If the real bore is LARGER "
            + "than the label (steel, copper) the true velocity is LOWER than reported, which is "
            + "safe. If it is SMALLER (thick-walled plastic) the true velocity is HIGHER, which "
            + "is the direction that causes noise and erosion.");
    }

    // Ascending, so the first entry big enough is the smallest one that is.
    var sizes = new List<double>();
    foreach (var size in nominalSizesMm) sizes.Add(size);
    sizes.Sort();

    foreach (var element in elements)
    {
        if (element == null) continue;

        var flowParameter = element.get_Parameter(BuiltInParameter.RBS_PIPE_FLOW_PARAM);
        if (flowParameter == null || !flowParameter.HasValue)
        {
            // Not a pipe, or a pipe with no flow parameter at all. Either way
            // there is no flow to size from.
            noFlow.Add(element.Id);
            continue;
        }

        var flow = flowParameter.AsDouble() * cubicMetresPerCubicFoot;

        if (flow <= 1e-12)
        {
            // Zero flow sizes to nothing. Sizing it to the smallest pipe in the
            // list would report a whole disconnected run as successfully sized.
            noFlow.Add(element.Id);
            continue;
        }

        var diameter = element.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
        if (diameter == null || diameter.IsReadOnly)
        {
            refused.Add(element.Id + ": no diameter that can be written. A pipe whose size is "
                + "driven by its type or by a locked segment is changed on the type, not here.");
            continue;
        }

        var wasMm = diameter.AsDouble() * mmPerFoot;

        // Area needed for this flow at the design velocity, then the bore that
        // gives it. Both in metres, then out to millimetres.
        var areaNeeded = flow / targetVelocity;
        var boreNeededMm = Math.Sqrt(4.0 * areaNeeded / Math.PI) * 1000.0;

        double pick = -1.0;
        var pickIndex = -1;
        for (int i = 0; i < sizes.Count; i++)
        {
            // Up, never to nearest. The tolerance only stops a size that IS the
            // answer being rejected by floating point.
            if (sizes[i] >= boreNeededMm - 1e-6) { pick = sizes[i]; pickIndex = i; break; }
        }

        if (pick < 0.0)
        {
            refused.Add(element.Id + ": needs " + boreNeededMm.ToString("F1") + " mm bore, which "
                + "is larger than every size in the table. Sizing it to the biggest available "
                + "would leave it over velocity with nothing saying so.");
            continue;
        }

        diameter.Set(pick / mmPerFoot);

        // Read back. The pipe type's segment table can refuse a size without
        // raising anything, and the velocity has to be computed on what the
        // pipe actually became.
        var gotMm = diameter.AsDouble() * mmPerFoot;

        // The bore to compute velocity on: the paired one when the size that
        // came back is the size that was asked for, otherwise the size itself.
        var boreMm = gotMm;
        if (haveBores && Math.Abs(gotMm - pick) < 1e-6 && pickIndex >= 0)
        {
            boreMm = boresMm[pickIndex];
        }
        else if (haveBores)
        {
            // The segment snapped it elsewhere. Find that size in the table so
            // its bore is used rather than its label.
            for (int i = 0; i < nominalSizesMm.Count; i++)
            {
                if (Math.Abs(nominalSizesMm[i] - gotMm) < 0.5) { boreMm = boresMm[i]; break; }
            }
        }

        var velocity = flow / (Math.PI * Math.Pow(boreMm / 1000.0, 2) / 4.0);

        if (Math.Abs(gotMm - pick) > 0.5)
        {
            refused.Add(element.Id + ": asked for " + pick.ToString("F0") + " mm and the type gave "
                + gotMm.ToString("F0") + " mm - its segment does not offer that size. The velocity "
                + "below is the one it actually has.");
        }
        else
        {
            sized++;
        }

        findings.Add(element.Id + ": flow " + (flow * 1000.0).ToString("F2") + " L/s, was "
            + wasMm.ToString("F0") + " mm, needs " + boreNeededMm.ToString("F1") + " mm bore, now "
            + gotMm.ToString("F0") + " mm, velocity " + velocity.ToString("F2") + " m/s"
            + (velocityIsApproximate ? " (approximate - computed on the label, not the bore)" : ""));
    }

    if (noFlow.Count > 0)
    {
        findings.Add(noFlow.Count + " pipe(s) carry no flow and were NOT sized. That is not a "
            + "pass. A pipe reads zero when nothing is connected to it, so a whole run with no "
            + "flow is a connectivity problem - TRACE_CONNECTIVITY, not a sizing job.");
    }
}
