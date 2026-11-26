// NOT STANDALONE. Assumes `elements`, `targets`, `defaultClearance`, `rules`,
// `includeInsulation`, `assumeThicknessMm`, `ignoreConnected`, `maxRows`, `doc`,
// `includeLinks`, `stackedOnly` and `overlapAs` are in scope, and leaves
// `tooClose`, `gapsMm`, `judgedBy`, `pairsChecked`, `linksSearched`,
// `linkedMatches`, `tooCloseIds`, `violations`, `insulatedCount`, `bareCount`,
// `clashing`, `overlapsMm`, `slopedOrVertical` and `findings` behind.
//
// VERSION 5 (2026-11-26) - CHECK_VERTICAL_CLEARANCE MERGED IN (row 5b-1878 (A),
// issue #1088, the owner's "Yes, one tool"), AND WHAT IT HAD THAT VERSION 4
// LACKED:
//
//   `stackedOnly` - ONLY PAIRS ONE ABOVE THE OTHER, AND ONLY THE VERTICAL GAP.
//   A pair whose plan boxes (each grown by its own jacket) do not overlap is
//   passed over and counted, and for the rest the gap is the bottom of the
//   upper box to the top of the lower one. Each row says WHICH IS ON TOP.
//   For a pair one above the other the box gap already was the vertical gap;
//   what this adds is every pair side by side LEFT OUT, and the upper one named.
//   In plan, boxes that only touch count as overlapping, and so does a
//   crossing at an angle over its box corner - at a crossing, the box corner
//   is where the hanger goes (CHECK_VERTICAL_CLEARANCE's rule).
//
//   `slopedOrVertical` - with `stackedOnly`, an element whose box is taller
//   than a quarter of its longest side and taller than the clearance is
//   counted and said: its box is the whole rise, so a vertical gap to it
//   comes out SMALLER than the truth. It over-reports and never misses.
//
//   `overlapAs` - HOW A PAIR THAT OVERLAPS IS REPORTED, the owner's choice:
//   "depending on the user it will be ask depth or just 0, both option maybe
//   needed" (D-208). "clash", the default: CLASH with the depth of the
//   overlap - boxes, jackets included - in `overlapsMm` and on its row, and
//   too close whatever clearance was asked, because an overlap is never a
//   pass. "zero": the gap reads 0 mm and is judged against the clearance,
//   exactly as version 4 did. `clashing` counts the pairs that overlap either
//   way - an overlap is a fact about the model, not about the clearance asked
//   - and `gapsMm` never holds a negative number in either: a minus sign in a
//   clearance column reads as tight. Any other word measures nothing and
//   says so, never a guess at which was meant.
//
// VERSION 3 (2026-11-01) - CHECK_INSULATION_CLEARANCE MERGED IN, AND WHAT IT
// HAD THAT VERSION 2 LACKED:
//
//   NO INSULATION MODELLED IS THE SILENT PROBLEM, SO IT IS COUNTED AND SAID
//   FIRST. `insulatedCount` and `bareCount` split the measurable elements - both
//   sets, each element once - and with insulation counted and some bare, the
//   first findings line is a WARNING that for those this is exactly the bare
//   answer. `assumeThicknessMm` treats every element with no wrap as lagged
//   that thick - how a run is checked against what it is GOING to be lagged
//   with - and every row it touches says ASSUMED. Given above 0 it counts the
//   insulation even with includeInsulation off, and says so: a thickness asked
//   for and then ignored would pass pairs that will not fit.
//
//   `ignoreConnected` - a pair joined by a connector is passed over. A duct and
//   the fitting on its end are at zero clearance by construction.
//
//   ONE SET AGAINST ITSELF. With `targets` the same set as `elements` - which is
//   what `binds: elements` gives - each pair was measured twice, A to B and B to
//   A, and reported twice. A pair is now measured once, whichever way round it
//   comes.
//
//   `findings`, CAPPED AT `maxRows` ROWS - one row per pair too close, with the
//   bare gap, both jackets and the real gap in millimetres - and the count
//   beside it is complete. `tooCloseIds` - every element in a pair too close,
//   once, so the next step can select or isolate them. `violations` - how many
//   pairs. An element with no readable extent is counted and said, never a pass,
//   and so is a run with fewer than two measurable elements.
//
//   The wrap's thickness falls back to RBS_INSULATION_THICKNESS where Thickness
//   cannot be read. LINING IS STILL NOT COUNTED - it is inside the duct.
//
// READ ONLY. Opens no transaction, moves nothing.
//
// EVERY OFFENDING PAIR, NOT THE NEAREST ONE.
//
// FIND_NEAREST_ELEMENTS answers "which is closest to each" and returns one per
// element. A duct with four services inside its clearance zone has FOUR
// problems, and a report naming one of them closes none.
//
// INSULATION IS A FLAG, NOT A SECOND FRAGMENT.
//
// A 50 mm jacket on both services eats 100 mm of the gap: a pair reading 120 mm
// clear on the bare geometry has 20 mm in reality and cannot be built. It is
// the same sweep with each box grown by its own insulation, and two fragments
// differing by one measurement drift apart the moment one of them is fixed.
//
// THE BOX IS BIGGER THAN THE ELEMENT, so a rotated pair reads CLOSER than it
// is. For a clearance check that is the safe direction - it over-reports rather
// than missing a clash - but it is not a certified figure.
//
// THE WALLS AND STRUCTURE ARE USUALLY A LINK, AND THEY ARE READ ONLY WHEN ASKED
// FOR - D-59. With `includeLinks` set, every element is also measured against
// the linked walls, floors, roofs, ceilings, columns, beams, foundations and
// stairs near it, with the same box gap in THIS model's space and the same
// rules by the linked element's category name. Each pair too close is TEXT in
// `linkedMatches` with the gap, the clearance asked for and the linked
// element's type, id IN THE LINK and Fire Rating. Only the element's own
// insulation comes off a linked gap - a link's elements carry none worth
// reading here. `tooClose`, `gapsMm`, `judgedBy` and `pairsChecked` stay this
// model's own, keyed by ids the next step looks up here (FRAGMENT-ISSUES row
// 75). NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM. Only model
// elements are read from a link, never its views or sheets.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
defaultClearance = defaultClearance / MillimetresPerFoot;

// AND THE ASSUMED JACKET (version 3, CHECK_INSULATION_CLEARANCE): millimetres
// in, the same 304.8. Above 0 it counts the insulation whatever
// includeInsulation says - see the header.
var assumedFeet = assumeThicknessMm > 0.0 ? assumeThicknessMm / MillimetresPerFoot : 0.0;
var insulationOn = includeInsulation || assumedFeet > 0.0;
var rowLimit = maxRows > 0 ? maxRows : 50;

// VERSION 5: how an overlap is reported (D-208). Left out it binds "clash".
var overlapWord = (overlapAs ?? "").Trim().ToLowerInvariant();
var overlapAsClash = overlapWord.Length == 0 || overlapWord == "clash" || overlapWord == "depth";
var overlapAsZero = overlapWord == "zero" || overlapWord == "0";
var refused = !overlapAsClash && !overlapAsZero;
IList<Element> hostElements = refused ? new List<Element>() : elements;
IList<Element> hostTargets = refused ? new List<Element>() : targets;

// AND `rules` IS THE SECOND LENGTH IN THIS FILE, which the D-71 sweep walked
// straight past. FRAGMENT-ISSUES row 51 records that sweep: it looked for a
// `double` request input with no conversion and found two. `rules` is an
// `IDictionary<string, double>`, so it was never a `double` and was never
// looked at - and every number in it is a clearance in exactly the same
// millimetres `defaultClearance` is in.
//
// UNCONVERTED IT IS THE WORST KIND OF WRONG: `rules="Walls=150"` asked for
// 150 mm and demanded 150 FEET, so every pair judged by a rule - and ONLY the
// pairs judged by a rule, while the default-judged ones stayed correct -
// reported as too close. A clearance report where some rows are 304.8x out
// reads as a model full of clashes, and the rule column beside it looks like
// the explanation rather than the fault.
//
// Copied into a new dictionary rather than written back: `rules` is the
// caller's, a fragment re-run on the same values must not halve them twice,
// and the read-back still shows what was typed.
var clearances = new Dictionary<string, double>();
if (rules != null)
{
    foreach (var rule in rules)
        clearances[rule.Key] = rule.Value / MillimetresPerFoot;
}

var tooClose = new List<string>();

// MILLIMETRES OUT, the same 304.8 the other way. The caller types this
// fragment's thresholds in millimetres; handing the measured gap back in feet
// made ONE fragment answer in two units, and `gaps: 0.29` against a
// `defaultClearance: 150` is unreadable. Comparisons stay in feet - that is
// what a bounding box is in - and only the number that leaves is converted.
var gapsMm = new Dictionary<string, double>();
var judgedBy = new Dictionary<string, string>();
int pairsChecked = 0;

// How far this element's own insulation stands off its surface. Zero when it
// has none, which is most elements.
// THE MODELLED JACKET ALONE - the thickest wrap, never the sum. GetInsulationIds
// throws for anything that cannot carry one, and the catch is what reads that as
// none (CHECK_INSULATION_CLEARANCE's rule).
Func<Element, double> modelledJacketOf = element =>
{
    double thickest = 0.0;
    try
    {
        var wraps = InsulationLiningBase.GetInsulationIds(doc, element.Id);
        if (wraps != null)
        {
            foreach (var wrapId in wraps)
            {
                var wrap = doc.GetElement(wrapId) as InsulationLiningBase;
                if (wrap == null) continue;
                double thickness = 0.0;
                try { thickness = wrap.Thickness; } catch { thickness = 0.0; }
                if (thickness <= 0.0)
                {
                    try
                    {
                        var parameter = wrap.get_Parameter(BuiltInParameter.RBS_INSULATION_THICKNESS);
                        if (parameter != null && parameter.HasValue) thickness = parameter.AsDouble();
                    }
                    catch { }
                }
                if (thickness > thickest) thickest = thickness;
            }
        }
    }
    catch { }
    return thickest;
};

// Each element once: its jacket, and whether it was ASSUMED rather than modelled.
var jacketCache = new Dictionary<ElementId, double>();
var assumedJacket = new HashSet<ElementId>();
var modelledInsulated = new HashSet<ElementId>();
Func<Element, double> jacketOf = element =>
{
    if (!insulationOn) return 0.0;
    double cached;
    if (jacketCache.TryGetValue(element.Id, out cached)) return cached;
    var jacket = modelledJacketOf(element);
    if (jacket > 0.0) modelledInsulated.Add(element.Id);
    else if (assumedFeet > 0.0) { jacket = assumedFeet; assumedJacket.Add(element.Id); }
    jacketCache[element.Id] = jacket;
    return jacket;
};

// IGNORE CONNECTED (version 3): the owners of every connector joined to this one.
Func<Element, HashSet<ElementId>> connectedTo = element =>
{
    var connected = new HashSet<ElementId>();
    if (!ignoreConnected || element == null) return connected;
    ConnectorManager manager = null;
    try
    {
        var curve = element as MEPCurve;
        if (curve != null) manager = curve.ConnectorManager;
        var instance = element as FamilyInstance;
        if (manager == null && instance != null && instance.MEPModel != null)
            manager = instance.MEPModel.ConnectorManager;
        if (manager == null) return connected;
        foreach (Connector connector in manager.Connectors)
            foreach (Connector reference in connector.AllRefs)
                if (reference != null && reference.Owner != null) connected.Add(reference.Owner.Id);
    }
    catch { }
    return connected;
};

Func<double, double, double, double, double> axisGap = (aMin, aMax, bMin, bMax) =>
{
    if (bMin > aMax) return bMin - aMax;
    if (aMin > bMax) return aMin - bMax;
    return 0.0;
};

// VERSION 5: ONE MEASUREMENT FOR BOTH SWEEPS, host and link. Boxes a and b by
// their corners, and the two jackets added together. Null when the pair is not
// measured - `stackedOnly` and neither above the other. Otherwise
// { the BARE gap, 1 when a is the upper one }. A bare gap below zero is a box
// overlap of that depth: the smallest of the three axis overlaps, or with
// `stackedOnly` the Z overlap alone.
Func<XYZ, XYZ, XYZ, XYZ, double, double[]> measure = (aMin, aMax, bMin, bMax, jackets) =>
{
    var dx = axisGap(aMin.X, aMax.X, bMin.X, bMax.X);
    var dy = axisGap(aMin.Y, aMax.Y, bMin.Y, bMax.Y);
    if (stackedOnly)
    {
        // Plan boxes, each grown by its own jacket, must overlap, or this is
        // not a stacking question.
        if (dx > jackets || dy > jackets) return null;
        if (aMin.Z >= bMin.Z) return new[] { aMin.Z - bMax.Z, 1.0 };
        return new[] { bMin.Z - aMax.Z, 0.0 };
    }
    var dz = axisGap(aMin.Z, aMax.Z, bMin.Z, bMax.Z);
    if (dx > 0.0 || dy > 0.0 || dz > 0.0)
        return new[] { Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz)), 0.0 };
    var ox = Math.Min(aMax.X, bMax.X) - Math.Max(aMin.X, bMin.X);
    var oy = Math.Min(aMax.Y, bMax.Y) - Math.Max(aMin.Y, bMin.Y);
    var oz = Math.Min(aMax.Z, bMax.Z) - Math.Max(aMin.Z, bMin.Z);
    return new[] { -Math.Min(ox, Math.Min(oy, oz)), 0.0 };
};

// VERSION 5, from CHECK_VERTICAL_CLEARANCE: with `stackedOnly`, an element whose
// box is mostly rise is sloped or vertical - each one once.
var sloped = new HashSet<ElementId>();
Action<Element, BoundingBoxXYZ> noteSlope = (element, box) =>
{
    if (!stackedOnly || element == null || box == null) return;
    var height = box.Max.Z - box.Min.Z;
    var run = Math.Max(box.Max.X - box.Min.X, box.Max.Y - box.Min.Y);
    if (height > run * 0.25 && height > defaultClearance) sloped.Add(element.Id);
};

// VERSION 4 (row 5b-1400): A MODEL BOX ONLY. A sheet, a view, a type or an
// annotation element can answer a box with no model extent behind it, and
// version 3 measured seven sheets as 21 pairs too close on Revit 2020. Such an
// element has no box here: it is counted with the ones Revit gives none, and
// said - not checked, which is not passing.
var noModelGeometry = new HashSet<ElementId>();
Func<Element, BoundingBoxXYZ> modelBox = e =>
{
    if (e == null) return null;
    if (e is View || e is ElementType || e.Category == null || e.Category.CategoryType != CategoryType.Model)
    {
        noModelGeometry.Add(e.Id);
        return null;
    }
    try { return e.get_BoundingBox(null); } catch { return null; }
};

// Boxes and jackets once per element, not once per pair.
var targetInfo = new List<KeyValuePair<Element, BoundingBoxXYZ>>();
var targetJacket = new Dictionary<ElementId, double>();
var withoutBox = new HashSet<ElementId>();
var measurable = new HashSet<ElementId>();
foreach (var target in hostTargets)
{
    if (target == null) continue;
    var box = modelBox(target);
    if (box == null) { withoutBox.Add(target.Id); continue; }
    noteSlope(target, box);
    targetInfo.Add(new KeyValuePair<Element, BoundingBoxXYZ>(target, box));
    targetJacket[target.Id] = jacketOf(target);
    measurable.Add(target.Id);
}

// Version 3: each pair once, whichever way round it comes - one set against
// itself would otherwise be measured and reported twice.
var pairsSeen = new HashSet<string>();
var tooCloseIds = new List<ElementId>();
var flagged = new HashSet<ElementId>();
var violations = 0;
var rows = new List<string>();
// VERSION 5: overlapping pairs, their depth, and the pairs `stackedOnly` passed over.
var clashing = 0;
var overlapsMm = new Dictionary<string, double>();
var notStacked = 0;

foreach (var element in hostElements)
{
    if (element == null) continue;
    var sourceBox = modelBox(element);
    if (sourceBox == null) { withoutBox.Add(element.Id); continue; }
    noteSlope(element, sourceBox);
    measurable.Add(element.Id);

    double sourceJacket = jacketOf(element);
    var connected = connectedTo(element);

    foreach (var pair in targetInfo)
    {
        var target = pair.Key;
        if (target.Id == element.Id) continue;

        var idA = element.Id.ToString();
        var idB = target.Id.ToString();
        if (!pairsSeen.Add(string.CompareOrdinal(idA, idB) < 0 ? idA + " & " + idB : idB + " & " + idA))
            continue;
        if (ignoreConnected && connected.Contains(target.Id)) continue;

        var box = pair.Value;

        double jacket;
        if (!targetJacket.TryGetValue(target.Id, out jacket)) jacket = 0.0;

        // Version 5: one measurement, straight-line or - with `stackedOnly` -
        // vertical, and a pair not one above the other is passed over, counted.
        var measured = measure(sourceBox.Min, sourceBox.Max, box.Min, box.Max, sourceJacket + jacket);
        if (measured == null) { notStacked++; continue; }
        pairsChecked++;

        // Both jackets come off the gap. Below zero the two OVERLAP, boxes and
        // jackets together, and `overlapAs` says how that reads (D-208): CLASH
        // with the depth, or 0 mm as version 4 reported it. `gap` itself is
        // never negative - a minus sign in a clearance column reads as tight.
        double bareGap = measured[0];
        double real = bareGap - sourceJacket - jacket;
        double gap = real < 0.0 ? 0.0 : real;
        bool overlaps = real < 0.0;
        if (overlaps) clashing++;
        bool asClash = overlaps && overlapAsClash;

        // Which rule judged this pair. Reported so a wrong threshold is visible
        // rather than buried in a pass.
        string categoryName = "";
        try { if (target.Category != null) categoryName = target.Category.Name; } catch { }

        double required = defaultClearance;
        string rule = "default";
        if (categoryName.Length > 0 && clearances.ContainsKey(categoryName))
        {
            required = clearances[categoryName];
            rule = categoryName;
        }

        // An overlap reported as a CLASH is too close whatever was asked.
        if (gap >= required && !asClash) continue;

        string key = element.Id.ToString() + " -> " + target.Id.ToString();
        tooClose.Add(key);
        gapsMm[key] = gap * MillimetresPerFoot;
        judgedBy[key] = rule;
        if (asClash) overlapsMm[key] = -real * MillimetresPerFoot;

        violations++;
        if (flagged.Add(element.Id)) tooCloseIds.Add(element.Id);
        if (flagged.Add(target.Id)) tooCloseIds.Add(target.Id);

        if (rows.Count < rowLimit)
        {
            var assumedHere = assumedJacket.Contains(element.Id) || assumedJacket.Contains(target.Id);
            // With `stackedOnly` the row names the upper one first and says so.
            var aIsUpper = measured[1] > 0.5;
            var first = stackedOnly && !aIsUpper ? target : element;
            var second = stackedOnly && !aIsUpper ? element : target;
            var firstJacket = stackedOnly && !aIsUpper ? jacket : sourceJacket;
            var secondJacket = stackedOnly && !aIsUpper ? sourceJacket : jacket;
            var bareShown = bareGap < 0.0 ? 0.0 : bareGap;
            if (asClash)
                rows.Add(string.Format("  CLASH: {0} (id {1}){2} {3} (id {4}) - they overlap by {5:F0} mm{6} "
                    + "(boxes {7:F0} mm, jackets {8:F0} + {9:F0} mm), against {10:F0} required ({11}){12}",
                    first.Name, first.Id, stackedOnly ? " is ABOVE" : " and", second.Name, second.Id,
                    -real * MillimetresPerFoot, stackedOnly ? " in height" : "",
                    bareGap < 0.0 ? -bareGap * MillimetresPerFoot : 0.0,
                    firstJacket * MillimetresPerFoot, secondJacket * MillimetresPerFoot,
                    required * MillimetresPerFoot, rule,
                    assumedHere ? "  [ASSUMED lagging - not modelled]" : ""));
            else if (stackedOnly)
                rows.Add(string.Format("  {0} (id {1}) is ABOVE {2} (id {3}): bare vertical gap {4:F0} mm, "
                    + "jackets {5:F0} + {6:F0} mm, REAL GAP {7:F0} mm against {8:F0} required ({9}) - "
                    + "{10:F0} mm short{11}",
                    first.Name, first.Id, second.Name, second.Id, bareShown * MillimetresPerFoot,
                    firstJacket * MillimetresPerFoot, secondJacket * MillimetresPerFoot,
                    gap * MillimetresPerFoot, required * MillimetresPerFoot, rule,
                    (required - gap) * MillimetresPerFoot,
                    assumedHere ? "  [ASSUMED lagging - not modelled]" : ""));
            else
                rows.Add(string.Format("  {0} (id {1}) to {2} (id {3}): bare {4:F0} mm, jackets {5:F0} + "
                    + "{6:F0} mm, REAL GAP {7:F0} mm against {8:F0} required ({9}){10}",
                    element.Name, element.Id, target.Name, target.Id,
                    bareShown * MillimetresPerFoot, sourceJacket * MillimetresPerFoot,
                    jacket * MillimetresPerFoot, gap * MillimetresPerFoot,
                    required * MillimetresPerFoot, rule,
                    assumedHere ? "  [ASSUMED lagging - not modelled]" : ""));
        }
    }
}

// ---- version 3: CHECK_INSULATION_CLEARANCE's findings ----------------------

var insulatedCount = 0;
var bareCount = 0;
foreach (var id in measurable)
{
    if (modelledInsulated.Contains(id)) insulatedCount++;
    else bareCount++;
}

var slopedOrVertical = sloped.Count;

var findings = new List<string>();
findings.Add(string.Format("{0} pair(s) too close of {1} checked{2}{3}. Distances are bounding-box "
    + "based - an engineering figure, not a defensible millimetre", violations, pairsChecked,
    insulationOn ? ", measured between the OUTSIDE faces of the insulation" : ", bare geometry",
    stackedOnly ? ", STACKED PAIRS ONLY - the vertical gap from the bottom of the upper one to the "
        + "top of the lower one" : ""));
// Version 5: the overlaps, and how they were reported (D-208).
if (clashing > 0)
    findings.Add(overlapAsClash
        ? string.Format("{0} pair(s) OVERLAP - reported as CLASH with the depth of the overlap, "
            + "too close whatever clearance was asked (overlapAs clash)", clashing)
        : string.Format("{0} pair(s) OVERLAP - reported as a 0 mm gap, as asked (overlapAs zero); "
            + "ask with overlapAs clash for how deep each overlap is", clashing));
if (stackedOnly && notStacked > 0)
    findings.Add(string.Format("{0} pair(s) passed over: not one above the other in plan, so not a "
        + "stacking question", notStacked));
if (slopedOrVertical > 0)
    findings.Add(string.Format("{0} element(s) are sloped or vertical. Their bounding box is the whole "
        + "rise, so any vertical gap reported for them is SMALLER than the truth - take those to a "
        + "section", slopedOrVertical));
if (insulationOn)
{
    findings.Add(string.Format("{0} element(s) measurable: {1} insulated in the model, {2} with "
        + "none{3}. Read this line first - it says whether the answer below is about clearance "
        + "or about the model", measurable.Count, insulatedCount, bareCount,
        assumedFeet > 0.0
            ? string.Format(" (uninsulated treated as {0:F0} mm)", assumeThicknessMm) : ""));
    if (bareCount > 0 && assumedFeet <= 0.0)
        findings.Insert(0, string.Format("WARNING: {0} element(s) carry NO insulation in the model, "
            + "so for those this gives EXACTLY the bare-geometry answer. If they are meant to be "
            + "lagged, this check is passing pairs that will not fit - set assumeThicknessMm, or "
            + "model the insulation", bareCount));
    if (!includeInsulation)
        findings.Add("assumeThicknessMm was given, so insulation was counted although "
            + "includeInsulation was off - a thickness asked for is never ignored");
    findings.Add("Lining is deliberately NOT counted - it sits inside the duct and takes no "
        + "outside space, so counting it would inflate every gap");
}
if (withoutBox.Count > 0)
    findings.Add(string.Format("{0} element(s) had NO readable extent and were skipped{1}. They were "
        + "not checked, which is not the same as passing", withoutBox.Count,
        noModelGeometry.Count == 0 ? "" : string.Format(" - {0} of them have no model geometry at all (a sheet, "
            + "a view, a type or annotation)", noModelGeometry.Count)));
if (measurable.Count < 2 && !refused)
    findings.Add("Fewer than two measurable elements, so no pair could be compared. That is NOT "
        + "every pair passing");
if (refused)
    findings.Insert(0, string.Format("NOTHING WAS MEASURED: overlapAs '{0}' is not a word this reads. "
        + "Say clash - an overlap reported as CLASH with its depth, the default - or zero - an "
        + "overlap reported as a 0 mm gap", overlapAs));
foreach (var row in rows) findings.Add(row);
if (violations > rows.Count)
    findings.Add(string.Format("  ... {0} further pair(s) not listed - the count above is complete",
        violations - rows.Count));

// ---- D-59: the same gap, against each link's building ---------------------

// ---- D-59: which links, only when asked for --------------------------------

var linksSearched = 0;
var linkedMatches = new List<string>();
var linkedTotal = 0;
var nestedLinks = 0;

// One entry per link FILE, keyed by link type - a file placed twice is one
// model placed twice, and counting placements would report a job with four
// links as having nine. LIST_LINKED_MODELS' rule, as REPORT_AREAS applies it.
var linkTypes = new List<ElementId>();
var linkDocs = new List<Document>();
var linkPlacements = new List<List<RevitLinkInstance>>();

if (includeLinks && !refused)
{
    foreach (var instance in new FilteredElementCollector(doc)
        .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
    {
        if (instance == null) continue;

        var typeId = instance.GetTypeId();
        if (typeId == null || typeId == ElementId.InvalidElementId) continue;

        var known = linkTypes.IndexOf(typeId);
        if (known >= 0) { linkPlacements[known].Add(instance); continue; }

        // LOADED IS ESTABLISHED BY ASKING FOR THE DOCUMENT, never by a status.
        Document linked = null;
        try { linked = instance.GetLinkDocument(); }
        catch (Exception) { linked = null; }
        if (linked == null) continue;

        linkTypes.Add(typeId);
        linkDocs.Add(linked);
        linkPlacements.Add(new List<RevitLinkInstance> { instance });

        try
        {
            nestedLinks += new FilteredElementCollector(linked)
                .OfClass(typeof(RevitLinkInstance)).GetElementCount();
        }
        catch (Exception) { }
    }
}


// Fire Rating by the name Revit's own walls, floors and doors carry it under,
// on the element first and then its type. Read as the palette shows it. A
// family that calls it something else reads "not set" - said, never guessed.
// TWO PARAMETERS BY THAT NAME where the lookup lands - a shared or project
// "Fire Rating" bound beside the built-in one - read "NOT READ" with the
// count, never either value: asked by name, Revit returns "the first one
// encountered", which its own reference says "is determined at random"
// (D-54 s3, FRAGMENT-ISSUES 5b-203). The guard check-sleeve-size carries.
Func<Element, string> fireRatingOf = element =>
{
    Element type = null;
    try { type = element.Document.GetElement(element.GetTypeId()); } catch (Exception) { }
    foreach (var source in new[] { element, type })
    {
        if (source == null) continue;
        // Two by this name where the lookup lands: said, never read (5b-203).
        var sharing = 0;
        try { sharing = source.GetParameters("Fire Rating").Count; } catch (Exception) { }
        if (sharing > 1) return "NOT READ - " + sharing + " parameters share that name";
        Parameter parameter = null;
        try { parameter = source.LookupParameter("Fire Rating"); } catch (Exception) { }
        if (parameter == null || !parameter.HasValue) continue;
        string text = null;
        try
        {
            text = parameter.StorageType == StorageType.String
                ? parameter.AsString() : parameter.AsValueString();
        }
        catch (Exception) { }
        if (!string.IsNullOrEmpty(text)) return text;
    }
    return "not set";
};

// A linked element as text: the category, 'Family: Type', its id IN THE LINK
// (never this model's), and its Fire Rating.
Func<Document, Element, string> describeLinked = (linked, element) =>
{
    var typeName = "";
    try
    {
        var type = linked.GetElement(element.GetTypeId()) as ElementType;
        if (type != null)
            typeName = string.IsNullOrEmpty(type.FamilyName)
                ? type.Name : type.FamilyName + ": " + type.Name;
    }
    catch (Exception) { }
    return string.Format("{0} '{1}' (id {2} in the link), Fire Rating {3}",
        element.Category == null ? "element" : element.Category.Name,
        typeName, element.Id, fireRatingOf(element));
};

var linkBlocked = refused ? "Links NOT read: nothing was measured - see findings" : "";

var buildingCategories = new List<BuiltInCategory> { BuiltInCategory.OST_Walls,
    BuiltInCategory.OST_Floors, BuiltInCategory.OST_Roofs, BuiltInCategory.OST_Ceilings,
    BuiltInCategory.OST_Columns, BuiltInCategory.OST_StructuralColumns,
    BuiltInCategory.OST_StructuralFraming, BuiltInCategory.OST_StructuralFoundation,
    BuiltInCategory.OST_Stairs };

// The widest clearance anything could ask for - how far round each element to look.
var reach = defaultClearance;
foreach (var value in clearances.Values) if (value > reach) reach = value;

for (var i = 0; i < linkDocs.Count; i++)
{
    var linked = linkDocs[i];
    var summaryAt = linkedMatches.Count;
    var linkPairs = 0;
    var linkClose = 0;

    foreach (var placement in linkPlacements[i])
    {
        Transform placed = null;
        try { placed = placement.GetTotalTransform(); } catch (Exception) { }
        if (placed == null) continue;
        var inverse = placed.Inverse;

        foreach (var element in hostElements)
        {
            if (element == null) continue;
            var sourceBox = modelBox(element);
            if (sourceBox == null) continue;
            var sourceJacket = jacketOf(element);
            var grow = reach + sourceJacket;

            // The search box, grown by the reach, carried into the link.
            var low = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
            var high = new XYZ(double.MinValue, double.MinValue, double.MinValue);
            foreach (var x in new[] { sourceBox.Min.X - grow, sourceBox.Max.X + grow })
                foreach (var y in new[] { sourceBox.Min.Y - grow, sourceBox.Max.Y + grow })
                    foreach (var z in new[] { sourceBox.Min.Z - grow, sourceBox.Max.Z + grow })
                    {
                        var corner = inverse.OfPoint(new XYZ(x, y, z));
                        low = new XYZ(Math.Min(low.X, corner.X), Math.Min(low.Y, corner.Y), Math.Min(low.Z, corner.Z));
                        high = new XYZ(Math.Max(high.X, corner.X), Math.Max(high.Y, corner.Y), Math.Max(high.Z, corner.Z));
                    }

            IList<Element> near = new List<Element>();
            try
            {
                near = new FilteredElementCollector(linked)
                    .WhereElementIsNotElementType()
                    .WherePasses(new ElementMulticategoryFilter(buildingCategories))
                    .WherePasses(new BoundingBoxIntersectsFilter(new Outline(low, high)))
                    .ToElements();
            }
            catch (Exception) { }

            foreach (var other in near)
            {
                BoundingBoxXYZ otherBox = null;
                try { otherBox = other.get_BoundingBox(null); } catch { }
                if (otherBox == null) continue;

                // The linked box, in THIS model's space.
                var oLow = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
                var oHigh = new XYZ(double.MinValue, double.MinValue, double.MinValue);
                foreach (var x in new[] { otherBox.Min.X, otherBox.Max.X })
                    foreach (var y in new[] { otherBox.Min.Y, otherBox.Max.Y })
                        foreach (var z in new[] { otherBox.Min.Z, otherBox.Max.Z })
                        {
                            var corner = placed.OfPoint(new XYZ(x, y, z));
                            oLow = new XYZ(Math.Min(oLow.X, corner.X), Math.Min(oLow.Y, corner.Y), Math.Min(oLow.Z, corner.Z));
                            oHigh = new XYZ(Math.Max(oHigh.X, corner.X), Math.Max(oHigh.Y, corner.Y), Math.Max(oHigh.Z, corner.Z));
                        }

                // Version 5: the same measurement as the host sweep, and the
                // same rule for an overlap (D-208) - only this element's own
                // jacket comes off.
                var measured = measure(sourceBox.Min, sourceBox.Max, oLow, oHigh, sourceJacket);
                if (measured == null) continue;
                linkPairs++;
                var real = measured[0] - sourceJacket;
                var gap = real < 0.0 ? 0.0 : real;
                var asClash = real < 0.0 && overlapAsClash;

                var categoryName = other.Category == null ? "" : other.Category.Name;
                var required = defaultClearance;
                var rule = "default";
                if (categoryName.Length > 0 && clearances.ContainsKey(categoryName))
                {
                    required = clearances[categoryName];
                    rule = categoryName;
                }
                if (gap >= required && !asClash) continue;

                linkClose++;
                if (linkedMatches.Count - summaryAt < 50)
                    linkedMatches.Add(string.Format("  {0} -> {1} - {2}: {3}, needs {4:0} mm ({5}){6}",
                        element.Id, linked.Title, describeLinked(linked, other),
                        asClash ? string.Format("CLASH, they overlap by {0:0} mm", -real * MillimetresPerFoot)
                            : string.Format("{0:0} mm", gap * MillimetresPerFoot),
                        required * MillimetresPerFoot, rule,
                        stackedOnly ? (measured[1] > 0.5 ? ", this one ABOVE it" : ", this one BELOW it") : ""));
            }
        }
    }

    linksSearched++;
    linkedTotal += linkClose;
    linkedMatches.Insert(summaryAt, string.Format("{0}: {1} pair(s) too close of {2} checked against its "
        + "walls, floors, roofs, ceilings, columns, beams, foundations and stairs{3}", linked.Title,
        linkClose, linkPairs, linkClose > 50 ? "; the first 50 are named" : ""));
}

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} pair(s) too close to a linked element, NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("What a link holds is reported here as text only - nothing from a link "
        + "is carried to the next step, which would look it up in this model");
