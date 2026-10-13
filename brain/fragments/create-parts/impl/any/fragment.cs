// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `madeFrom`,
// `partIds`, `alreadyParts`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// PARTS MADE FROM WALLS, FLOORS, ROOFS AND CEILINGS - Create Parts - one part
// per layer of each, so layers can be divided and scheduled on their own.
// Revit's own test decides which elements can have parts; the rest are named.
// An element that already has parts is counted, not made twice.
//
// READ BACK: each element's parts are looked up afterwards.

var madeFrom = 0;
var partIds = new List<ElementId>();
var alreadyParts = 0;
var lines = new List<string>();
var findings = new List<string>();

var candidates = new List<ElementId>();
foreach (var element in elements)
{
    if (element == null) continue;
    ICollection<ElementId> existing = null;
    try { existing = PartUtils.GetAssociatedParts(doc, element.Id, false, true); } catch { }
    if (existing != null && existing.Count > 0) { alreadyParts++; continue; }
    candidates.Add(element.Id);
}

if (candidates.Count > 0)
{
    var valid = new List<ElementId>();
    foreach (var id in candidates)
    {
        bool ok = false;
        try { ok = PartUtils.AreElementsValidForCreateParts(doc, new List<ElementId> { id }); } catch { }
        if (ok) valid.Add(id);
        else lines.Add(id + ": Revit will not make parts of it.");
    }
    if (valid.Count > 0)
    {
        try { PartUtils.CreateParts(doc, valid); }
        catch (Exception ex) { lines.Add("Revit refused the parts: " + ex.Message); }
        doc.Regenerate();
        foreach (var id in valid)
        {
            ICollection<ElementId> parts = null;
            try { parts = PartUtils.GetAssociatedParts(doc, id, false, true); } catch { }
            if (parts != null && parts.Count > 0) { madeFrom++; partIds.AddRange(parts); }
            else lines.Add(id + ": asked, and no part reads back.");
        }
    }
}

var report = string.Join(" | ", lines);
findings.Add(partIds.Count + " part(s) made from " + madeFrom + " element(s), read back; " + alreadyParts + " already had parts.");
findings.AddRange(lines);
