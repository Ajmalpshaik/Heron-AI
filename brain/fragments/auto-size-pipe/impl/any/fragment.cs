// NOT STANDALONE. Assumes `doc`, `elements`, `targetVelocity`,
// `nominalSizesMm` and `boresMm` are in scope; leaves `sized`, `findings`,
// `noFlow`, `refused`, `velocityIsApproximate`, `openConnectorsBefore`,
// `openConnectorsAfter` and `openedByResize` behind.
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
// on the size that came back - and it is read AFTER A REGENERATION, because
// the snap happens there and not at the set. Until 2026-09-23 this read back
// straight after the set, which returns the size that was ASKED FOR (the rule
// SET_MEP_SIZE's header records, and the reason AUTO_SIZE_MEP regenerates), so
// the snapped-size refusal below could never fire.
//
// ===========================================================================
// RESIZING A CONNECTED RUN CAN LEAVE ITS FITTINGS BEHIND.
// ===========================================================================
//
// OBSERVED ELSEWHERE, mostly on Revit 2020, and NOT YET PROVEN HERE: resizing
// a connected run left fittings at the old size and added transitions - 22
// pipes became 41 elements, and unions became two transitions back to back
// with connectors left OPEN. Every pipe still reads its new size, so `sized`
// cannot show it.
//
// So the open connectors around the pipes are COUNTED BEFORE ANY SIZE IS SET
// and COUNTED AGAIN AFTER: the pipes handed in, and everything a walk through
// pipe FITTINGS reaches from them - a union replaced by two transitions is two
// hops out, not one. Whatever the walk stops at (another pipe, a valve,
// equipment) is counted too and not walked through, so the answer stays about
// these pipes and not the whole system. An element with more open connectors
// after than before - a new one had none before, because it did not exist - is
// named in `openedByResize`.
//
// ONLY PIPING ENDS ARE COUNTED. An unconnected electrical connector on a pump
// is not what resizing breaks, and counting it would bury the one that is.

var mmPerFoot = 304.8;
var cubicMetresPerCubicFoot = 0.028316846592;

var sized = 0;
var findings = new List<string>();
var noFlow = new List<ElementId>();
var refused = new List<string>();
var openConnectorsBefore = 0;
var openConnectorsAfter = 0;
var openedByResize = new List<ElementId>();

// The connectors of anything that has them. Written once, asked before and
// after.
Func<Element, ConnectorSet> connectorsOf = e =>
{
    try
    {
        var curve = e as MEPCurve;
        if (curve != null) return curve.ConnectorManager.Connectors;
        var instance = e as FamilyInstance;
        if (instance != null && instance.MEPModel != null && instance.MEPModel.ConnectorManager != null)
            return instance.MEPModel.ConnectorManager.Connectors;
    }
    catch { }
    return null;
};

// Open piping ENDS on one element. A connector that cannot be read is counted
// neither way.
Func<Element, int> openEnds = e =>
{
    var open = 0;
    var set = connectorsOf(e);
    if (set == null) return 0;
    foreach (Connector connector in set)
    {
        try
        {
            if (connector.ConnectorType != ConnectorType.End) continue;
            if (connector.Domain != Domain.DomainPiping) continue;
            if (!connector.IsConnected) open++;
        }
        catch { }
    }
    return open;
};

var pipeFittingCategory = new ElementId(BuiltInCategory.OST_PipeFitting);
Func<Element, bool> isFitting = e =>
{
    try { return e != null && e.Category != null && e.Category.Id == pipeFittingCategory; }
    catch { return false; }
};

// Open ends per element, over the pipes handed in and what a walk through
// fittings reaches from them. See the header for where the walk stops.
Func<Dictionary<ElementId, int>> openAround = () =>
{
    var open = new Dictionary<ElementId, int>();
    var pending = new Stack<Element>();
    foreach (var element in elements)
    {
        if (element == null || open.ContainsKey(element.Id)) continue;
        open[element.Id] = openEnds(element);
        pending.Push(element);
    }
    while (pending.Count > 0)
    {
        var here = pending.Pop();
        var set = connectorsOf(here);
        if (set == null) continue;
        foreach (Connector connector in set)
        {
            ConnectorSet joined = null;
            try { joined = connector.AllRefs; } catch { }
            if (joined == null) continue;
            foreach (Connector other in joined)
            {
                Element neighbour = null;
                try { neighbour = other.Owner; } catch { }
                // A pipe's link to its own SYSTEM is in AllRefs too, and a
                // system is not a joint.
                if (neighbour == null || neighbour is MEPSystem) continue;
                if (open.ContainsKey(neighbour.Id)) continue;
                open[neighbour.Id] = openEnds(neighbour);
                if (isFitting(neighbour)) pending.Push(neighbour);
            }
        }
    }
    return open;
};

Func<Dictionary<ElementId, int>, int> fittingsIn = open =>
{
    var fittings = 0;
    foreach (var id in open.Keys) if (isFitting(doc.GetElement(id))) fittings++;
    return fittings;
};

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

    // BEFORE ANY SIZE IS SET. See the header.
    var openBefore = openAround();
    foreach (var count in openBefore.Values) openConnectorsBefore += count;
    var fittingsBefore = fittingsIn(openBefore);
    var setCalls = 0;

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
        setCalls++;

        // Regenerate, THEN read back. The pipe type's segment table can refuse
        // a size without raising anything, and the snap happens at the
        // regeneration - before it, the parameter still says what was asked
        // for. The velocity has to be computed on what the pipe became.
        doc.Regenerate();
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

    // AFTER. Only when a size was actually set - with none, nothing can have
    // opened, and the count stands as it was.
    if (setCalls == 0)
    {
        openConnectorsAfter = openConnectorsBefore;
    }
    else
    {
        var openAfter = openAround();

        // An element the walk no longer reaches may simply have been cut loose
        // - a fitting left behind at the old size is exactly that - so every
        // one seen before is counted again if it still exists. Only the ones
        // Revit deleted drop out, and they are counted as gone.
        var goneSince = 0;
        foreach (var id in openBefore.Keys)
        {
            if (openAfter.ContainsKey(id)) continue;
            var still = doc.GetElement(id);
            if (still == null || !still.IsValidObject) { goneSince++; continue; }
            openAfter[id] = openEnds(still);
        }

        foreach (var count in openAfter.Values) openConnectorsAfter += count;
        var fittingsAfter = fittingsIn(openAfter);

        foreach (var entry in openAfter)
        {
            var had = 0;
            openBefore.TryGetValue(entry.Key, out had);
            if (entry.Value > had) openedByResize.Add(entry.Key);
        }

        findings.Add(string.Format("Around these pipes: {0} open piping connector(s) and {1} fitting(s) "
            + "before any size was set; {2} and {3} after. {4} element(s) that were there before are gone",
            openConnectorsBefore, fittingsBefore, openConnectorsAfter, fittingsAfter, goneSince));

        if (openedByResize.Count > 0)
        {
            findings.Add(string.Format("RESIZING LEFT {0} ELEMENT(S) WITH A CONNECTOR OPEN THAT WAS NOT OPEN "
                + "BEFORE - listed in `openedByResize`. Revit has been seen elsewhere to leave fittings at "
                + "the old size and add transitions between them when a connected run is resized. Look at "
                + "every joint named before keeping this", openedByResize.Count));
        }
    }
}
