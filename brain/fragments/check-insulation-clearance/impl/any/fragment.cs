// NOT STANDALONE. Assumes `doc`, `elements`, `requiredClearanceMm`,
// `assumeThicknessMm`, `ignoreConnected` and `maxRows` are in scope; leaves
// `findings`, `tooClose`, `violations`, `insulatedCount` and `bareCount` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE BARE CHECK IS OPTIMISTIC. Revit models insulation as SEPARATE elements
// wrapped round the host, so the host's own box stops at the sheet metal. Every
// distance off the host is too generous by the jacket at each end. 50 mm each
// side eats 100 mm of the gap.
//
// GetInsulationIds THROWS for anything that cannot carry a wrap, and THE CATCH
// IS THE CATEGORY FILTER. Pre-filtering by category would miss whatever the API
// accepts and nobody predicted.
//
// LINING IS NOT COUNTED. It sits INSIDE the duct and takes air away, not outside
// space. Counting it would inflate every gap and invent failures.
//
// THE THICKEST WRAP GOVERNS, NOT THE SUM. A host may carry more than one; adding
// them would invent a jacket nobody specified.
//
// NO INSULATION MODELLED IS THE SILENT PROBLEM, so the split is said BEFORE any
// result. Where nothing is lagged this reads exactly like the bare check and
// passes things that will not fit.
//
// ElementId IS NEVER READ AS A NUMBER. The earlier library reflected `Value` /
// `IntegerValue` to key connected elements; ElementId hashes as itself, so the
// 2024 widening to 64-bit never reaches this fragment.
//
// mm -> internal feet by / 304.8, plain arithmetic. No units API.

var findings = new List<string>();
var tooClose = new List<ElementId>();
var violations = 0;
var insulatedCount = 0;
var bareCount = 0;

var requiredFeet = requiredClearanceMm / 304.8;
var assumedFeet = assumeThicknessMm / 304.8;

// Thickness is declared on the DERIVED wrap classes, not on the base the API
// hands back, so it is read by name. The built-in parameter is the fallback.
// Neither is assumed to be there.
Func<Element, double> thicknessOf = wrap =>
{
    if (wrap == null) return 0.0;
    try
    {
        var property = wrap.GetType().GetProperty("Thickness");
        if (property != null)
        {
            var value = property.GetValue(wrap, null);
            if (value is double) return (double)value;
        }
    }
    catch (Exception) { }

    try
    {
        var parameter = wrap.get_Parameter(BuiltInParameter.RBS_INSULATION_THICKNESS);
        if (parameter != null && parameter.HasValue) return parameter.AsDouble();
    }
    catch (Exception) { }

    return 0.0;
};

Func<Element, double> insulationFeetOf = element =>
{
    ICollection<ElementId> wrapIds = null;
    try { wrapIds = InsulationLiningBase.GetInsulationIds(doc, element.Id); }
    catch (Exception) { return 0.0; }
    if (wrapIds == null || wrapIds.Count == 0) return 0.0;

    var thickest = 0.0;
    foreach (var wrapId in wrapIds)
    {
        var thickness = thicknessOf(doc.GetElement(wrapId));
        if (thickness > thickest) thickest = thickness;
    }
    return thickest;
};

var subjects = new List<Element>();
var boxes = new List<BoundingBoxXYZ>();
var jackets = new List<double>();
var assumedFlags = new List<bool>();
var withoutBox = 0;
var thickestJacket = 0.0;

foreach (var element in elements)
{
    if (element == null || !element.IsValidObject) continue;

    BoundingBoxXYZ box = null;
    try { box = element.get_BoundingBox(null); } catch (Exception) { }
    if (box == null) { withoutBox++; continue; }

    var jacket = insulationFeetOf(element);
    var assumed = false;
    if (jacket <= 0.0 && assumedFeet > 0.0) { jacket = assumedFeet; assumed = true; }

    if (jacket > 0.0 && !assumed) insulatedCount++; else bareCount++;
    if (jacket > thickestJacket) thickestJacket = jacket;

    subjects.Add(element);
    boxes.Add(box);
    jackets.Add(jacket);
    assumedFlags.Add(assumed);
}

Func<Element, HashSet<ElementId>> connectedTo = element =>
{
    var connected = new HashSet<ElementId>();
    if (!ignoreConnected) return connected;

    ConnectorManager manager = null;
    var curve = element as MEPCurve;
    if (curve != null) manager = curve.ConnectorManager;
    var instance = element as FamilyInstance;
    if (manager == null && instance != null && instance.MEPModel != null)
        manager = instance.MEPModel.ConnectorManager;
    if (manager == null) return connected;

    try
    {
        foreach (Connector connector in manager.Connectors)
        {
            foreach (Connector reference in connector.AllRefs)
            {
                if (reference != null && reference.Owner != null) connected.Add(reference.Owner.Id);
            }
        }
    }
    catch (Exception) { }

    return connected;
};

// Gap between two boxes, and zero where they already overlap.
Func<BoundingBoxXYZ, BoundingBoxXYZ, double> gapBetween = (a, b) =>
{
    var dx = Math.Max(0.0, Math.Max(a.Min.X - b.Max.X, b.Min.X - a.Max.X));
    var dy = Math.Max(0.0, Math.Max(a.Min.Y - b.Max.Y, b.Min.Y - a.Max.Y));
    var dz = Math.Max(0.0, Math.Max(a.Min.Z - b.Max.Z, b.Min.Z - a.Max.Z));
    return Math.Sqrt(dx * dx + dy * dy + dz * dz);
};

if (subjects.Count < 2)
{
    findings.Add("Fewer than two measurable elements, so no pair could be compared. That is NOT every "
        + "pair passing");
}
else
{
    var rows = new List<string>();
    var flagged = new HashSet<ElementId>();

    for (var i = 0; i < subjects.Count; i++)
    {
        var connected = connectedTo(subjects[i]);

        for (var j = i + 1; j < subjects.Count; j++)
        {
            if (ignoreConnected && connected.Contains(subjects[j].Id)) continue;

            var bareGap = gapBetween(boxes[i], boxes[j]);

            // The search envelope has to allow for the fattest possible pair of jackets, or a
            // violation caused ENTIRELY by insulation is never even looked at.
            if (bareGap > requiredFeet + 2.0 * thickestJacket) continue;

            var realGap = bareGap - jackets[i] - jackets[j];
            if (realGap >= requiredFeet) continue;

            violations++;
            if (!flagged.Contains(subjects[i].Id)) { flagged.Add(subjects[i].Id); tooClose.Add(subjects[i].Id); }
            if (!flagged.Contains(subjects[j].Id)) { flagged.Add(subjects[j].Id); tooClose.Add(subjects[j].Id); }

            if (rows.Count < maxRows)
            {
                var note = "";
                if (assumedFlags[i] || assumedFlags[j])
                {
                    note = "  [ASSUMED lagging on "
                        + (assumedFlags[i] && assumedFlags[j] ? "both" : "one")
                        + " - not modelled]";
                }

                rows.Add(string.Format(
                    "  {0} (id {1}) to {2} (id {3}): bare {4:F0} mm, jackets {5:F0} + {6:F0} mm, "
                    + "REAL GAP {7:F0} mm against {8:F0} required{9}",
                    subjects[i].Name, subjects[i].Id, subjects[j].Name, subjects[j].Id,
                    bareGap * 304.8, jackets[i] * 304.8, jackets[j] * 304.8,
                    realGap * 304.8, requiredClearanceMm, note));
            }
        }
    }

    foreach (var row in rows) findings.Add(row);
    if (violations > rows.Count)
    {
        findings.Add(string.Format("  ... {0} further violation(s) not listed - the count above is "
            + "complete", violations - rows.Count));
    }
    if (violations == 0)
    {
        findings.Add("  no pair is closer than the rule, measured to the outside of the lagging");
    }
}

if (withoutBox > 0)
{
    findings.Insert(0, string.Format("{0} element(s) had NO readable extent and were skipped. They "
        + "were not checked, which is not the same as passing", withoutBox));
}

findings.Insert(0, "Lining is deliberately NOT counted - it sits inside the duct and takes no outside "
    + "space, so counting it would inflate every gap");

if (bareCount > 0 && assumedFeet <= 0.0)
{
    findings.Insert(0, string.Format("WARNING: {0} element(s) carry NO insulation in the model, so for "
        + "those this gives EXACTLY the bare-geometry answer. If they are meant to be lagged, this "
        + "check is passing pairs that will not fit - set assumeThicknessMm, or model the insulation",
        bareCount));
}

findings.Insert(0, string.Format("{0} element(s) measurable: {1} insulated in the model, {2} with none"
    + "{3}. Read this line first - it says whether the answer below is about clearance or about the "
    + "model",
    subjects.Count, insulatedCount, bareCount,
    assumedFeet > 0.0
        ? string.Format(" (uninsulated treated as {0:F0} mm)", assumeThicknessMm)
        : ""));

findings.Insert(0, string.Format("{0} violation(s) at {1:F0} mm between the OUTSIDE faces of the "
    + "insulation. Distances are bounding-box based and are an engineering figure, not a defensible "
    + "millimetre", violations, requiredClearanceMm));
