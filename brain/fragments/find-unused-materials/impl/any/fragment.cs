// NOT STANDALONE. Assumes `doc` is in scope; leaves `unusedMaterials`,
// `unusedNames`, `scannedInstances`, `scannedTypes`, `unreadable` and
// `findings` behind.
//
// READ ONLY. Opens no transaction and needs none, and deletes nothing.
//
// A MATERIAL USED ONLY AS PAINT LOOKS UNUSED. GetMaterialIds answers two
// different questions depending on its flag:
//
//   false   geometry and compound structure
//   true    PAINT - the finishes applied face by face
//
// Read only the first and a painted finish has no user, is offered for
// deletion, and deleting it strips the paint off every face it was on. Both
// are read and unioned. This is the defect the fragment is built around.
//
// TYPES ARE WALKED AS WELL AS INSTANCES. A wall type's compound structure names
// its materials whether or not one wall of that type is placed, so scanning
// only placed elements reports every material of every loaded-but-unplaced type
// as unused - on a template-derived project, most of them.
//
// IT REPORTS AND DELETES NOTHING, the same decision FIND_UNUSED_FAMILIES and
// FIND_UNUSED_DEFINITIONS both took. Purging is one Revit command away and hard
// to reverse on a shared model.

var unusedMaterials = new List<ElementId>();
var unusedNames = new List<string>();
var findings = new List<string>();
var scannedInstances = 0;
var scannedTypes = 0;
var unreadable = 0;

var usedMaterialIds = new HashSet<ElementId>();

// Both reads, every time. Separated so one throwing does not lose the other -
// an element whose paint cannot be read still has geometry materials worth
// keeping, and treating the whole element as unreadable would under-report use,
// which is the dangerous direction.
Action<Element> harvest = element =>
{
    var readAnything = false;

    try
    {
        var geometryMaterials = element.GetMaterialIds(false);
        if (geometryMaterials != null)
        {
            foreach (var id in geometryMaterials) usedMaterialIds.Add(id);
        }
        readAnything = true;
    }
    catch { }

    try
    {
        var paintMaterials = element.GetMaterialIds(true);
        if (paintMaterials != null)
        {
            foreach (var id in paintMaterials) usedMaterialIds.Add(id);
        }
        readAnything = true;
    }
    catch { }

    if (!readAnything) unreadable++;
};

foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
{
    if (element == null) continue;
    scannedInstances++;
    harvest(element);
}

foreach (var element in new FilteredElementCollector(doc).WhereElementIsElementType())
{
    if (element == null) continue;
    scannedTypes++;
    harvest(element);
}

foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(Material)))
{
    var material = candidate as Material;
    if (material == null) continue;

    if (usedMaterialIds.Contains(material.Id)) continue;

    unusedMaterials.Add(material.Id);
    unusedNames.Add(string.IsNullOrEmpty(material.Name) ? "(no name)" : material.Name);
}

findings.Add(unusedMaterials.Count + " material(s) are referenced by nothing, out of "
    + scannedInstances + " placed element(s) and " + scannedTypes + " type(s) walked. "
    + "Both the geometry and the PAINT read were taken on every one - a material used only as "
    + "paint has no geometry user, and deleting it strips the paint off every face it was on.");

if (unreadable > 0)
{
    findings.Add(unreadable + " element(s) would not report their materials at all. Anything they "
        + "reference could be in the list above wrongly, so the list is a starting point rather "
        + "than a proof on this model.");
}

findings.Add("Nothing was deleted, deliberately. This says what is REFERENCED BY NOTHING, never "
    + "what should go - a project keeps materials for work not yet modelled and a template ships "
    + "them on purpose. Revit's own Purge Unused does the removing.");
