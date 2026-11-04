// NOT STANDALONE. Assumes `elements`, `targets`, `defaultClearance`, `rules`,
// `includeInsulation`, `assumeThicknessMm`, `ignoreConnected`, `maxRows`, `doc`
// and `includeLinks` are in scope, and leaves `tooClose`, `gapsMm`, `judgedBy`,
// `pairsChecked`, `linksSearched`, `linkedMatches`, `tooCloseIds`, `violations`,
// `insulatedCount`, `bareCount` and `findings` behind.
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

// Boxes and jackets once per element, not once per pair.
var targetInfo = new List<KeyValuePair<Element, BoundingBoxXYZ>>();
var targetJacket = new Dictionary<ElementId, double>();
var withoutBox = new HashSet<ElementId>();
var measurable = new HashSet<ElementId>();
foreach (var target in targets)
{
    if (target == null) continue;
    BoundingBoxXYZ box = null;
    try { box = target.get_BoundingBox(null); } catch { }
    if (box == null) { withoutBox.Add(target.Id); continue; }
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

foreach (var element in elements)
{
    if (element == null) continue;
    BoundingBoxXYZ sourceBox = null;
    try { sourceBox = element.get_BoundingBox(null); } catch { }
    if (sourceBox == null) { withoutBox.Add(element.Id); continue; }
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
        pairsChecked++;

        double dx = axisGap(sourceBox.Min.X, sourceBox.Max.X, box.Min.X, box.Max.X);
        double dy = axisGap(sourceBox.Min.Y, sourceBox.Max.Y, box.Min.Y, box.Max.Y);
        double dz = axisGap(sourceBox.Min.Z, sourceBox.Max.Z, box.Min.Z, box.Max.Z);
        double gap = Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));

        // Both jackets come off the gap. A pair whose insulation already
        // overlaps is reported at 0, not at a negative number, because "they
        // are touching" is the finding and a negative gap reads as a depth.
        double jacket;
        if (!targetJacket.TryGetValue(target.Id, out jacket)) jacket = 0.0;
        double bareGap = gap;
        gap = gap - sourceJacket - jacket;
        if (gap < 0.0) gap = 0.0;

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

        if (gap >= required) continue;

        string key = element.Id.ToString() + " -> " + target.Id.ToString();
        tooClose.Add(key);
        gapsMm[key] = gap * MillimetresPerFoot;
        judgedBy[key] = rule;

        violations++;
        if (flagged.Add(element.Id)) tooCloseIds.Add(element.Id);
        if (flagged.Add(target.Id)) tooCloseIds.Add(target.Id);

        if (rows.Count < rowLimit)
        {
            var assumedHere = assumedJacket.Contains(element.Id) || assumedJacket.Contains(target.Id);
            rows.Add(string.Format("  {0} (id {1}) to {2} (id {3}): bare {4:F0} mm, jackets {5:F0} + "
                + "{6:F0} mm, REAL GAP {7:F0} mm against {8:F0} required ({9}){10}",
                element.Name, element.Id, target.Name, target.Id,
                bareGap * MillimetresPerFoot, sourceJacket * MillimetresPerFoot,
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

var findings = new List<string>();
findings.Add(string.Format("{0} pair(s) too close of {1} checked{2}. Distances are bounding-box "
    + "based - an engineering figure, not a defensible millimetre", violations, pairsChecked,
    insulationOn ? ", measured between the OUTSIDE faces of the insulation" : ", bare geometry"));
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
    findings.Add(string.Format("{0} element(s) had NO readable extent and were skipped. They were "
        + "not checked, which is not the same as passing", withoutBox.Count));
if (measurable.Count < 2)
    findings.Add("Fewer than two measurable elements, so no pair could be compared. That is NOT "
        + "every pair passing");
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

if (includeLinks)
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

var linkBlocked = "";

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

        foreach (var element in elements)
        {
            if (element == null) continue;
            BoundingBoxXYZ sourceBox = null;
            try { sourceBox = element.get_BoundingBox(null); } catch { }
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

                linkPairs++;
                var dx = axisGap(sourceBox.Min.X, sourceBox.Max.X, oLow.X, oHigh.X);
                var dy = axisGap(sourceBox.Min.Y, sourceBox.Max.Y, oLow.Y, oHigh.Y);
                var dz = axisGap(sourceBox.Min.Z, sourceBox.Max.Z, oLow.Z, oHigh.Z);
                var gap = Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz)) - sourceJacket;
                if (gap < 0.0) gap = 0.0;

                var categoryName = other.Category == null ? "" : other.Category.Name;
                var required = defaultClearance;
                var rule = "default";
                if (categoryName.Length > 0 && clearances.ContainsKey(categoryName))
                {
                    required = clearances[categoryName];
                    rule = categoryName;
                }
                if (gap >= required) continue;

                linkClose++;
                if (linkedMatches.Count - summaryAt < 50)
                    linkedMatches.Add(string.Format("  {0} -> {1} - {2}: {3:0} mm, needs {4:0} mm ({5})",
                        element.Id, linked.Title, describeLinked(linked, other), gap * MillimetresPerFoot,
                        required * MillimetresPerFoot, rule));
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
