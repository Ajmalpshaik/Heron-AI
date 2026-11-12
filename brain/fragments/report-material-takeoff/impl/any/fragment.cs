// NOT STANDALONE. Assumes `elements`, `doc`, `factorParameter`, `groupBy` and
// `top` are in scope, and leaves `areas`, `volumes`, `paintedMaterials`,
// `elementsWithNoMaterial`, `materials`, `noMaterial`, `volumeM3`, `areaM2`,
// `carbon`, `totalKg`, `noFactor`, `ambiguous`, `noVolume` and `findings`
// behind.
//
// VERSION 3 (2026-11-01) - REPORT_EMBODIED_CARBON MERGED IN. Given
// `factorParameter` - a Number parameter on each MATERIAL holding kgCO2e per
// cubic metre - the same material volumes are multiplied by it: kg = m3 x
// factor, summed and grouped by material, category or type (`groupBy`),
// largest first, the top `top` listed (0 is 20) with each group's share of the
// total. THE FACTOR IS THE MODELLER'S: a material with no value is listed in
// `noFactor` and adds nothing, said aloud, so a total is never quietly low;
// Heron invents none. An element whose materials have no volume is counted in
// `noVolume`. Left out (`none`), no carbon is worked out and the takeoff is
// version 2's to the number. THE SAFER RULE KEPT: an element handed in twice
// is counted ONCE here too - REPORT_EMBODIED_CARBON counted it twice.
//
// VERSION 2 (2026-11-01) - READ_ELEMENT_MATERIAL MERGED IN. Its answers are
// here beside this card's, and no existing answer changed its unit:
//
//   materials    per element, the NAME of every material it carries - the
//                geometry set AND the painted set, so a painted finish is
//                named (READ_ELEMENT_MATERIAL read GetMaterialIds(false)
//                only, and missed every painted one its purpose promised)
//   noMaterial   elements carrying NO material at all. Not the same list as
//                elementsWithNoMaterial, which also holds an element whose
//                materials gave no area and no volume
//   volumeM3     `volumes` in cubic metres, beside the cubic feet - D-20's
//                arithmetic, 0.3048 cubed
//   areaM2       `areas` in square metres, beside the square feet
//
// AN ELEMENT HANDED IN TWICE IS TAKEN OFF ONCE (READ_ELEMENT_MATERIAL's rule):
// a selection merged from two finders would otherwise count its concrete
// twice.
//
// READ ONLY. Opens no transaction and needs none.
//
// TWO SEPARATE MATERIAL SETS, AND ASKING FOR ONE MISSES THE OTHER ENTIRELY.
//
//   GetMaterialIds(false)   the element's GEOMETRY and compound structure
//   GetMaterialIds(true)    materials applied face by face with PAINT
//
// They are different sets, and the area call must be given THE SAME FLAG the
// material was listed with: GetMaterialArea(id, true) for a painted face,
// GetMaterialArea(id, false) for a geometric one. Read only the geometry set
// and a painted finish reports as ZERO - which on a finishes schedule reads as
// "that material is not in this model" rather than "I did not ask for it".
//
// PAINT HAS AREA AND NEVER VOLUME.
//
// Paint is a surface, not a body, and there is no volume overload to call. A
// painted material gets an area and NO volume entry, and that absence is the
// right answer rather than missing data. It is also why nothing here sorts by
// volume: doing so buries every painted finish at the bottom of the one
// schedule they belong at the top of.
//
// THE NUMBERS ARE REVIT'S OWN.
//
// Nothing is derived from solids. A takeoff computed independently gives a
// defensible figure that DISAGREES with the model's own Material Takeoff
// schedule, and two numbers that both look right is worse than one that is
// plainly wrong.
//
// KEYED BY NAME, NOT BY ID. A takeoff is read and priced by name, and an id
// would have to be resolved again by every caller.

var areas = new Dictionary<string, double>();
var volumes = new Dictionary<string, double>();
var paintedMaterials = new List<string>();
var elementsWithNoMaterial = new List<ElementId>();
// Version 2, from READ_ELEMENT_MATERIAL.
var materials = new Dictionary<ElementId, ICollection<string>>();
var noMaterial = new List<ElementId>();
var volumeM3 = new Dictionary<string, double>();
var areaM2 = new Dictionary<string, double>();
var seen = new HashSet<ElementId>();
const double MetresPerFoot = 0.3048;

// Version 3, from REPORT_EMBODIED_CARBON.
const double CubicMetresPerCubicFoot = 0.028316846592;
var findings = new List<string>();
var noFactor = new List<string>();
// VERSION 4 (5b-203): materials carrying the factor's name twice - not read.
// `ambiguous` as every reader of that row names it; the names are for the
// finding.
var ambiguous = new List<ElementId>();
var ambiguousNames = new List<string>();
var noVolume = 0;
var totalKg = 0.0;
var carbon = "";
var factorName = (factorParameter ?? "").Trim();
var withCarbon = factorName.Length > 0 && !string.Equals(factorName, "none", StringComparison.OrdinalIgnoreCase);
var by = (groupBy ?? "").Trim().ToLowerInvariant();
if (by != "category" && by != "type") by = "material";
var shown = top <= 0 ? 20 : top;
var factors = new Dictionary<ElementId, double?>();
Func<ElementId, double?> factorOf = id =>
{
    double? known;
    if (factors.TryGetValue(id, out known)) return known;
    double? value = null;
    var material = doc.GetElement(id) as Material;
    if (material != null)
    {
        // A FACTOR NAME TWO PARAMETERS SHARE IS NOT READ (version 4; D-54 s3,
        // FRAGMENT-ISSUES 5b-203). LookupParameter hands back "the first one
        // encountered" when two parameters on one material share the name,
        // which Autodesk's reference says "is determined at random" - and a
        // carbon total multiplied by whichever came first is a number nobody
        // chose. Such a material adds nothing and is named apart from the
        // ones with no factor: the fix is different.
        var factorsNamed = material.GetParameters(factorName).Count;
        if (factorsNamed > 1)
        {
            if (!ambiguous.Contains(material.Id))
            {
                ambiguous.Add(material.Id);
                ambiguousNames.Add(material.Name);
            }
        }
        else
        {
            var p = material.LookupParameter(factorName);
            if (p != null && p.HasValue && (p.StorageType == StorageType.Double || p.StorageType == StorageType.Integer))
                value = p.StorageType == StorageType.Double ? p.AsDouble() : p.AsInteger();
            if (value == null && !noFactor.Contains(material.Name)) noFactor.Add(material.Name);
        }
    }
    factors[id] = value;
    return value;
};
var groups = new Dictionary<string, double[]>();   // key -> { kg, m3 }

Action<string, double, IDictionary<string, double>> addTo = (name, value, into) =>
{
    if (value <= 0.0) return;
    double running;
    into[name] = into.TryGetValue(name, out running) ? running + value : value;
};

foreach (var element in elements)
{
    if (element == null) continue;
    if (!seen.Add(element.Id)) continue;
    bool anything = false;
    var names = new List<string>();

    // Geometry and compound structure. Area AND volume both apply.
    ICollection<ElementId> geometric = null;
    try { geometric = element.GetMaterialIds(false); } catch { }
    var anyVolume = false;

    if (geometric != null)
    {
        foreach (var materialId in geometric)
        {
            var material = doc.GetElement(materialId) as Material;
            if (material == null) continue;
            if (!names.Contains(material.Name)) names.Add(material.Name);

            double area = 0.0, volume = 0.0;
            try { area = element.GetMaterialArea(materialId, false); } catch { }
            try { volume = element.GetMaterialVolume(materialId); } catch { }

            if (area > 0.0 || volume > 0.0) anything = true;
            addTo(material.Name, area, areas);
            addTo(material.Name, volume, volumes);

            // VERSION 3 - CARBON, from the same volume: kg = m3 x factor.
            if (withCarbon && volume > 0.0)
            {
                anyVolume = true;
                var cubicMetres = volume * CubicMetresPerCubicFoot;
                var factor = factorOf(materialId);
                var kg = factor.HasValue ? cubicMetres * factor.Value : 0;
                totalKg += kg;
                string key;
                if (by == "category") key = element.Category == null ? "(no category)" : element.Category.Name;
                else if (by == "type") { var t = doc.GetElement(element.GetTypeId()); key = t == null ? "(no type)" : t.Name; }
                else key = material.Name;
                double[] sums;
                if (!groups.TryGetValue(key, out sums)) { sums = new double[2]; groups[key] = sums; }
                sums[0] += kg;
                sums[1] += cubicMetres;
            }
        }
    }

    // Paint. The SAME flag must go to the area call, and there is no volume.
    ICollection<ElementId> painted = null;
    try { painted = element.GetMaterialIds(true); } catch { }

    if (painted != null)
    {
        foreach (var materialId in painted)
        {
            var material = doc.GetElement(materialId) as Material;
            if (material == null) continue;
            if (!names.Contains(material.Name)) names.Add(material.Name);

            double area = 0.0;
            try { area = element.GetMaterialArea(materialId, true); } catch { }
            if (area <= 0.0) continue;

            anything = true;
            addTo(material.Name, area, areas);
            if (!paintedMaterials.Contains(material.Name)) paintedMaterials.Add(material.Name);
        }
    }

    if (withCarbon && !anyVolume) noVolume++;
    if (!anything) elementsWithNoMaterial.Add(element.Id);
    if (names.Count == 0) noMaterial.Add(element.Id);
    else materials[element.Id] = names;
}

// Beside the internal units, never instead of them (D-20, arithmetic only).
foreach (var entry in volumes)
    volumeM3[entry.Key] = entry.Value * MetresPerFoot * MetresPerFoot * MetresPerFoot;
foreach (var entry in areas)
    areaM2[entry.Key] = entry.Value * MetresPerFoot * MetresPerFoot;

// VERSION 3 - THE CARBON, largest first, the hotspots leading.
var invariant = System.Globalization.CultureInfo.InvariantCulture;
if (withCarbon)
{
    var rows = groups.OrderByDescending(g => g.Value[0]).Take(shown).Select(g =>
        g.Key + " | " + Math.Round(g.Value[0]).ToString(invariant) + " kgCO2e | "
        + Math.Round(g.Value[1], 2).ToString(invariant) + " m3"
        + (totalKg > 0 ? " | " + Math.Round(100 * g.Value[0] / totalKg, 1).ToString(invariant) + "%" : "")).ToList();
    carbon = string.Join("\n", rows);
    findings.Add("Embodied carbon " + Math.Round(totalKg).ToString(invariant) + " kgCO2e over "
        + groups.Count + " " + by + "(s), the top " + Math.Min(shown, groups.Count) + " listed largest first; factors from the material "
        + "parameter \"" + factorName + "\".");
    if (noFactor.Count > 0)
        findings.Add("NO FACTOR on " + noFactor.Count + " material(s), so they add nothing to the total: " + string.Join(", ", noFactor.Take(15))
            + (noFactor.Count > 15 ? ", and more." : "."));
    if (ambiguous.Count > 0)
        findings.Add(ambiguous.Count + " material(s) carry TWO OR MORE parameters called '" + factorName
            + "' - NOT READ, because Revit hands back whichever of them it meets first: "
            + string.Join(", ", ambiguousNames.Take(15)) + (ambiguousNames.Count > 15 ? ", and more" : "")
            + ". They add nothing to the total.");
    if (noVolume > 0)
        findings.Add(noVolume + " element(s) have no material volume - a line, an annotation, a painted face - and add nothing.");
}
findings.Add(string.Format("{0} material(s) taken off {1} element(s), {2} painted; {3} element(s) carry no material",
    materials.Values.SelectMany(n => n).Distinct().Count(), seen.Count, paintedMaterials.Count, noMaterial.Count));
