// NOT STANDALONE. Assumes `doc`, `hostType`, `layerIndex` and `widthMm` are in
// scope; leaves `applied`, `wasMm`, `refused` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A WALL'S THICKNESS IS NOT A PARAMETER. It is the sum of its layers, which is
// why WRITE_ELEMENT_PARAMETERS cannot reach it and why REPORT_COMPOUND_STRUCTURE
// could only ever read it.
//
// REVIT HANDS OUT A COPY. GetCompoundStructure returns a copy of the structure,
// so changing it in place does NOTHING until SetCompoundStructure puts it back.
// That silent no-op is the whole reason this file exists rather than three lines
// at a call site.
//
// IT IS A TYPE CHANGE. Every instance of this type moves. Said out loud in the
// findings rather than discovered afterwards.
//
// 304.8 appears twice and only at the edges - once converting in, once out.

const double MillimetresPerFoot = 304.8;

var applied = false;
var wasMm = 0.0;
string refused = null;
var findings = new List<string>();

if (hostType == null)
{
    refused = "No type was given. A build-up belongs to a TYPE - a wall type, a "
        + "floor type, a roof type - never to one placed element";
}
else if (widthMm <= 0)
{
    refused = string.Format(
        "A layer has to have a thickness and {0} mm was asked for", widthMm);
}
else
{
    CompoundStructure structure = null;
    try
    {
        structure = hostType.GetCompoundStructure();
    }
    catch
    {
    }

    if (structure == null)
    {
        refused = string.Format(
            "'{0}' has no compound structure. A curtain wall is the usual case - "
            + "it is made of panels and mullions rather than layers, which is "
            + "REPORT_CURTAIN_ELEMENTS", hostType.Name);
    }
    else if (layerIndex < 0 || layerIndex >= structure.LayerCount)
    {
        refused = string.Format(
            "Layer {0} does not exist - '{1}' has {2} layer(s), numbered 0 to {3}. "
            + "REPORT_COMPOUND_STRUCTURE is what turns a layer's name into its "
            + "number", layerIndex, hostType.Name, structure.LayerCount,
            structure.LayerCount - 1);
    }
    else
    {
        try
        {
            wasMm = structure.GetLayerWidth(layerIndex) * MillimetresPerFoot;
            structure.SetLayerWidth(layerIndex, widthMm / MillimetresPerFoot);

            // WITHOUT THIS LINE NOTHING HAPPENS - see the header.
            hostType.SetCompoundStructure(structure);

            // READ IT BACK off the type, not off the copy.
            var after = hostType.GetCompoundStructure();
            if (after != null && layerIndex < after.LayerCount)
            {
                var nowMm = after.GetLayerWidth(layerIndex) * MillimetresPerFoot;
                applied = Math.Abs(nowMm - widthMm) < 0.5;
                if (!applied)
                {
                    refused = string.Format(
                        "Revit accepted the change and the layer is {0:F1} mm "
                        + "rather than the {1:F1} mm asked for - a layer function "
                        + "or a variable-thickness setting is holding it",
                        nowMm, widthMm);
                }
            }
        }
        catch (Exception ex)
        {
            refused = string.Format("Revit refused the change: {0}", ex.Message);
        }
    }
}

if (applied)
{
    findings.Add(string.Format(
        "Layer {0} of '{1}' changed from {2:F1} mm to {3:F1} mm. THIS IS A TYPE "
        + "CHANGE - every instance of this type in the model has moved. If that "
        + "was not wanted, DUPLICATE_TYPE first and edit the copy",
        layerIndex, hostType.Name, wasMm, widthMm));
}
else if (refused != null)
{
    findings.Add(refused);
}
