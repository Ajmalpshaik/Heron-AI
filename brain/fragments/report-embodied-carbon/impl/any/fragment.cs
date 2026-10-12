// NOT STANDALONE. Assumes `doc`, `elements`, `factorParameter`, `groupBy` and
// `top` are in scope; leaves `carbon`, `totalKg`, `noFactor`, `noVolume` and
// `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// EMBODIED CARBON FROM THE MODEL: for every element handed in, the volume of
// each of its materials, times that material's carbon factor, summed and
// grouped - by material, by category or by type - largest first, so the
// hotspots lead the list.
//
// THE FACTOR IS THE MODELLER'S, ON THE MATERIAL: a Number parameter named
// `factorParameter` holding kgCO2e per cubic metre. Heron invents no factor; a
// material with no value is listed in `noFactor` and contributes nothing,
// said aloud, so a total is never quietly low. Writing the factors is
// WRITE_ELEMENT_PARAMETERS on the materials.
//
// VOLUME is Revit's own material volume per element (cubic feet inside,
// converted here: 1 ft3 = 0.028316846592 m3). An element whose materials have
// no volume - a line, an annotation - is counted in `noVolume`.
//
// To compare two constructions, run this on each set and read the totals.

const double CubicMetresPerCubicFoot = 0.028316846592;
var findings = new List<string>();
var noFactor = new List<string>();
var noVolume = 0;
var totalKg = 0.0;

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
        var p = material.LookupParameter((factorParameter ?? "").Trim());
        if (p != null && p.HasValue && (p.StorageType == StorageType.Double || p.StorageType == StorageType.Integer))
            value = p.StorageType == StorageType.Double ? p.AsDouble() : p.AsInteger();
        if (value == null && !noFactor.Contains(material.Name)) noFactor.Add(material.Name);
    }
    factors[id] = value;
    return value;
};

var groups = new Dictionary<string, double[]>();   // key -> { kg, m3 }
foreach (var element in elements)
{
    if (element == null) continue;
    ICollection<ElementId> materials = null;
    try { materials = element.GetMaterialIds(false); } catch { }
    var any = false;
    if (materials != null)
    {
        foreach (var id in materials)
        {
            double volume = 0;
            try { volume = element.GetMaterialVolume(id) * CubicMetresPerCubicFoot; } catch { }
            if (volume <= 0) continue;
            any = true;
            var factor = factorOf(id);
            var kg = factor.HasValue ? volume * factor.Value : 0;
            totalKg += kg;
            string key;
            if (by == "category") key = element.Category == null ? "(no category)" : element.Category.Name;
            else if (by == "type") { var t = doc.GetElement(element.GetTypeId()); key = t == null ? "(no type)" : t.Name; }
            else { var m = doc.GetElement(id); key = m == null ? id.ToString() : m.Name; }
            double[] sums;
            if (!groups.TryGetValue(key, out sums)) { sums = new double[2]; groups[key] = sums; }
            sums[0] += kg;
            sums[1] += volume;
        }
    }
    if (!any) noVolume++;
}

var lines = groups.OrderByDescending(g => g.Value[0]).Take(shown).Select(g =>
    g.Key + " | " + Math.Round(g.Value[0]).ToString(System.Globalization.CultureInfo.InvariantCulture) + " kgCO2e | "
    + Math.Round(g.Value[1], 2).ToString(System.Globalization.CultureInfo.InvariantCulture) + " m3"
    + (totalKg > 0 ? " | " + Math.Round(100 * g.Value[0] / totalKg, 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%" : "")).ToList();
var carbon = string.Join("\n", lines);

findings.Add("Embodied carbon " + Math.Round(totalKg).ToString(System.Globalization.CultureInfo.InvariantCulture) + " kgCO2e over "
    + groups.Count + " " + by + "(s), the top " + Math.Min(shown, groups.Count) + " listed largest first; factors from the material "
    + "parameter \"" + factorParameter + "\".");
if (noFactor.Count > 0)
    findings.Add("NO FACTOR on " + noFactor.Count + " material(s), so they add nothing to the total: " + string.Join(", ", noFactor.Take(15))
        + (noFactor.Count > 15 ? ", and more." : "."));
