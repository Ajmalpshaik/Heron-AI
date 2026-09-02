// NOT STANDALONE. Assumes `elements` is in scope; leaves `materials`,
// `volumeM3` and `noMaterial` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// `GetMaterialIds(false)` MEANS "NOT ONLY THE PAINTED ONES". Passing true
// returns just the materials applied as paint, which on most elements is
// nothing at all - and an empty answer for a wall plainly made of something
// reads as a broken tool rather than a wrong argument.
//
// EVERY MATERIAL, NOT THE FIRST. A duct has its own and its lining; a wall has
// every layer of its build-up. Reporting one would be right about a diffuser
// and wrong about everything compound, which is most of what gets taken off.
//
// A MATERIAL WITH NO VOLUME IS STILL REPORTED. Paint and surface finishes have
// no thickness and return zero, and dropping them loses precisely the ones a
// finishes schedule is about. The volume total simply does not grow.
//
// VOLUMES ARE TOTALLED BY MATERIAL NAME, which is what a takeoff wants - and
// names are what a person reads. Two materials sharing a name would total
// together; that is a modelling problem the answer cannot hide and should not
// silently split.

var materials = new Dictionary<ElementId, ICollection<string>>();
var volumeM3 = new Dictionary<string, double>();
var noMaterial = new List<ElementId>();

const double MetresPerFoot = 0.3048;
var cubic = MetresPerFoot * MetresPerFoot * MetresPerFoot;

foreach (var element in elements)
{
    if (element == null) continue;
    if (materials.ContainsKey(element.Id) || noMaterial.Contains(element.Id)) continue;

    ICollection<ElementId> ids;
    try
    {
        ids = element.GetMaterialIds(false);
    }
    catch (Exception)
    {
        noMaterial.Add(element.Id);
        continue;
    }

    if (ids == null || ids.Count == 0)
    {
        noMaterial.Add(element.Id);
        continue;
    }

    var names = new List<string>();

    foreach (var id in ids)
    {
        var material = element.Document.GetElement(id) as Material;
        if (material == null) continue;

        names.Add(material.Name);

        double feet3;
        try
        {
            feet3 = element.GetMaterialVolume(id);
        }
        catch (Exception)
        {
            // No volume for this material on this element - a painted finish,
            // or a category Revit does not compute. The material is still
            // named; only the total does not grow.
            continue;
        }

        if (feet3 <= 0.0) continue;

        if (!volumeM3.ContainsKey(material.Name)) volumeM3[material.Name] = 0.0;
        volumeM3[material.Name] += feet3 * cubic;
    }

    if (names.Count == 0) noMaterial.Add(element.Id);
    else materials[element.Id] = names;
}
