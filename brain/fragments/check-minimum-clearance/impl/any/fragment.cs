// NOT STANDALONE. Assumes `elements`, `targets`, `defaultClearance`, `rules`,
// `includeInsulation` and `doc` are in scope, and leaves `tooClose`, `gapsMm`,
// `judgedBy` and `pairsChecked` behind.
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

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
defaultClearance = defaultClearance / MillimetresPerFoot;

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
Func<Element, double> jacketOf = element =>
{
    if (!includeInsulation) return 0.0;
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
                if (wrap.Thickness > thickest) thickest = wrap.Thickness;
            }
        }
    }
    catch { }
    return thickest;
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
foreach (var target in targets)
{
    if (target == null) continue;
    BoundingBoxXYZ box = null;
    try { box = target.get_BoundingBox(null); } catch { }
    if (box == null) continue;
    targetInfo.Add(new KeyValuePair<Element, BoundingBoxXYZ>(target, box));
    targetJacket[target.Id] = jacketOf(target);
}

foreach (var element in elements)
{
    if (element == null) continue;
    BoundingBoxXYZ sourceBox = null;
    try { sourceBox = element.get_BoundingBox(null); } catch { }
    if (sourceBox == null) continue;

    double sourceJacket = jacketOf(element);

    foreach (var pair in targetInfo)
    {
        var target = pair.Key;
        if (target.Id == element.Id) continue;

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
    }
}
