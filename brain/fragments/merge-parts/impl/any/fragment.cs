// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `merged`,
// `mergedPart`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// ADJACENT PARTS MERGED BACK INTO ONE - Merge Parts. Revit's own test decides
// whether the set can merge: the parts must touch and share a material and
// source; a set that cannot is refused with the reason, nothing changed.
//
// READ BACK: the merge's part maker is looked up again after a regeneration;
// its id is what `mergedPart` returns - Revit's handle on the merged part.

var findings = new List<string>();
string refused = null;
var merged = 0;
ElementId mergedPart = ElementId.InvalidElementId;

var parts = new List<ElementId>();
foreach (var element in elements) if (element is Part) parts.Add(element.Id);

if (parts.Count < 2) refused = "Two parts or more are needed to merge. NOTHING WAS MERGED.";
else
{
    bool ok = false;
    try { ok = PartUtils.ArePartsValidForMerge(doc, parts); } catch { }
    if (!ok) refused = "Revit will not merge these parts - they must touch and share a material and source. NOTHING WAS MERGED.";
}
if (refused == null)
{
    try
    {
        var maker = PartUtils.CreateMergedPart(doc, parts);
        doc.Regenerate();
        if (maker != null && doc.GetElement(maker.Id) != null)
        {
            merged = parts.Count;
            mergedPart = maker.Id;
            findings.Add(parts.Count + " part(s) merged into one, read back.");
        }
    }
    catch (Exception ex) { refused = "Revit refused the merge: " + ex.Message + ". NOTHING WAS MERGED."; }
}
if (refused != null) findings.Add(refused);
