// NOT STANDALONE. Assumes `elements` and `doc` are in scope, and leaves
// `areas`, `volumes`, `paintedMaterials`, `elementsWithNoMaterial`,
// `materials`, `noMaterial`, `volumeM3` and `areaM2` behind.
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

    if (!anything) elementsWithNoMaterial.Add(element.Id);
    if (names.Count == 0) noMaterial.Add(element.Id);
    else materials[element.Id] = names;
}

// Beside the internal units, never instead of them (D-20, arithmetic only).
foreach (var entry in volumes)
    volumeM3[entry.Key] = entry.Value * MetresPerFoot * MetresPerFoot * MetresPerFoot;
foreach (var entry in areas)
    areaM2[entry.Key] = entry.Value * MetresPerFoot * MetresPerFoot;
