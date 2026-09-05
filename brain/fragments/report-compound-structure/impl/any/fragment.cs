// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `findings`,
// `typesReported`, `withoutStructure` and `totalThicknessMm` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// IT REPORTS TYPES, NOT INSTANCES, AND DEDUPES. Nine walls of one type are one
// build-up, and listing it nine times buries the thing being asked. An instance
// is resolved to its type; a type handed in directly is used as it is, so this
// works downstream of a category filter or a type filter equally.
//
// THE CORE IS MARKED. Revit's core boundary decides where the location line can
// sit, what a room boundary follows, and what a dimension to "wall face" hits.
// A layer list without it reads as a plain stack and hides the one line that
// changes measurements.
//
// A CURTAIN WALL OR IN-PLACE ELEMENT HAS NO COMPOUND STRUCTURE, AND THAT IS AN
// ANSWER. Named, with REPORT_CURTAIN_ELEMENTS as the question that fits, rather
// than returned as an empty result that reads as a failure.
//
// WIDTHS ARE INTERNAL FEET, MULTIPLIED BY 304.8. Plain arithmetic at the edge,
// never a units API - that is the call that breaks at Revit 2021.

var findings = new List<string>();
var typesReported = 0;
var withoutStructure = new List<string>();
var totalThicknessMm = new List<string>();

// Dedupe to types. An instance resolves to its type; a type is used as-is.
var seenTypes = new HashSet<ElementId>();
var typesToReport = new List<ElementType>();

foreach (var element in elements)
{
    if (element == null) continue;
    var asType = element as ElementType;
    var type = asType ?? doc.GetElement(element.GetTypeId()) as ElementType;
    if (type == null) continue;
    if (seenTypes.Add(type.Id)) typesToReport.Add(type);
}

foreach (var type in typesToReport)
{
    var label = string.Format("'{0}: {1}'", type.FamilyName, type.Name);

    CompoundStructure structure = null;
    var host = type as HostObjAttributes;
    if (host != null)
    {
        try { structure = host.GetCompoundStructure(); }
        catch (Exception) { structure = null; }
    }

    if (structure == null)
    {
        withoutStructure.Add(label);
        findings.Add(string.Format("{0} - NO layer build-up. A curtain wall, an in-place element or a "
            + "non-host type. For a curtain wall the panels and mullions are what it is made of",
            label));
        continue;
    }

    IList<CompoundStructureLayer> layers;
    try { layers = structure.GetLayers(); }
    catch (Exception ex)
    {
        findings.Add(string.Format("{0} - its layers could not be read: {1}", label, ex.Message));
        continue;
    }

    var firstCore = -1;
    var lastCore = -1;
    try
    {
        firstCore = structure.GetFirstCoreLayerIndex();
        lastCore = structure.GetLastCoreLayerIndex();
    }
    catch (Exception) { }

    findings.Add(string.Format("{0} - {1} layer(s), outside face first:", label, layers.Count));

    var total = 0.0;
    for (var i = 0; i < layers.Count; i++)
    {
        var layer = layers[i];
        var widthMm = layer.Width * 304.8;
        total += widthMm;

        var material = doc.GetElement(layer.MaterialId) as Material;
        var materialName = material == null ? "<by category / none>" : material.Name;

        var coreMark = "";
        if (i == firstCore && i == lastCore) coreMark = "   <-- THE CORE";
        else if (i == firstCore) coreMark = "   <-- core starts";
        else if (i == lastCore) coreMark = "   <-- core ends";

        findings.Add(string.Format("    {0}. {1,-16} {2,8:0.#} mm   {3}{4}",
            i + 1, layer.Function, widthMm, materialName, coreMark));
    }

    findings.Add(string.Format("    total {0:0.#} mm", total));
    totalThicknessMm.Add(string.Format("{0} = {1:0.#} mm", label, total));
    typesReported++;
}

findings.Insert(0, string.Format("{0} type(s) with a layer build-up, from {1} element(s); {2} had none",
    typesReported, elements.Count, withoutStructure.Count));
