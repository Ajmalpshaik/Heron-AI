// NOT STANDALONE. Assumes `elements` and `doc` are in scope, and leaves
// `areas`, `volumes`, `paintedMaterials` and `elementsWithNoMaterial` behind.
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

Action<string, double, IDictionary<string, double>> addTo = (name, value, into) =>
{
    if (value <= 0.0) return;
    double running;
    into[name] = into.TryGetValue(name, out running) ? running + value : value;
};

foreach (var element in elements)
{
    if (element == null) continue;
    bool anything = false;

    // Geometry and compound structure. Area AND volume both apply.
    ICollection<ElementId> geometric = null;
    try { geometric = element.GetMaterialIds(false); } catch { }

    if (geometric != null)
    {
        foreach (var materialId in geometric)
        {
            var material = doc.GetElement(materialId) as Material;
            if (material == null) continue;

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

            double area = 0.0;
            try { area = element.GetMaterialArea(materialId, true); } catch { }
            if (area <= 0.0) continue;

            anything = true;
            addTo(material.Name, area, areas);
            if (!paintedMaterials.Contains(material.Name)) paintedMaterials.Add(material.Name);
        }
    }

    if (!anything) elementsWithNoMaterial.Add(element.Id);
}
