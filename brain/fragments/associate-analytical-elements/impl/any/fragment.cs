// NOT STANDALONE. Assumes `doc`, `elements` and `replace` are in scope, and
// leaves `associated`, `associatedIds`, `report`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION and never opens one (Golden Rule 16).
//
// WHAT IT DOES. Analyze > Analytical Model > Associate: ties ONE analytical
// member or panel to ONE physical element - both selected - and reads the
// association back.
//
// AN ASSOCIATION ALREADY THERE IS NEVER BROKEN SILENTLY. Either element tied
// to something else is refused, naming what it is tied to; with `replace` true
// the old tie is removed first (RemoveAssociation - a tie, never an element)
// and named in the report. The same pair already tied is left as it is.
//
// NEVER DELETES OR MOVES AN ELEMENT, physical or analytical.
//
// RELEASES: 2023-2027, the card's claim (tools/api-surface --members,
// 2026-11-02). AnalyticalToPhysicalAssociationManager does not exist on
// 2020-2022.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var associated = 0;
var associatedIds = new List<ElementId>();
var report = "";
var refused = new List<string>();
var findings = new List<string>();

Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
Func<Element, string> describe = e => e == null ? "?" : (e.Category == null ? "" : e.Category.Name + " ") + quoted(e.Name) + " (" + e.Id + ")";
var ok = true;

var picked = (elements ?? new List<Element>()).Where(e => e != null).ToList();
Func<Element, bool> analytical = e => { try { return AnalyticalToPhysicalAssociationManager.IsAnalyticalElement(doc, e.Id); } catch { return false; } };
Func<Element, bool> physical = e => { try { return AnalyticalToPhysicalAssociationManager.IsPhysicalElement(doc, e.Id); } catch { return false; } };
var analyticals = picked.Where(e => analytical(e) && (e is AnalyticalMember || e is AnalyticalSurfaceBase)).ToList();
var physicals = picked.Where(e => !analytical(e) && physical(e)).ToList();

if (picked.Count != 2 || analyticals.Count != 1 || physicals.Count != 1)
{
    refused.Add("Select exactly two: one analytical member or panel and the beam, column, brace, floor or wall it stands "
        + "for. " + picked.Count + " element(s) were selected" + (picked.Count == 0 ? "" : " (" + string.Join(", ", picked.Select(describe)) + ")")
        + " - " + analyticals.Count + " analytical member or panel, " + physicals.Count + " physical element. Nothing was associated.");
    ok = false;
}

AnalyticalToPhysicalAssociationManager manager = null;
if (ok)
{
    try { manager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(doc); } catch { }
    if (manager == null)
    {
        refused.Add("Revit gave no analytical-to-physical association manager for this model. Nothing was associated.");
        ok = false;
    }
}

Func<ElementId, ElementId> tiedTo = id =>
{
    try
    {
        if (!manager.HasAssociation(id)) return ElementId.InvalidElementId;
        var other = manager.GetAssociatedElementId(id);
        return other ?? ElementId.InvalidElementId;
    }
    catch { return ElementId.InvalidElementId; }
};

if (ok)
{
    var a = analyticals[0];
    var p = physicals[0];
    var aWas = tiedTo(a.Id);
    var pWas = tiedTo(p.Id);
    if (aWas == p.Id)
    {
        findings.Add(describe(a) + " is already associated with " + describe(p) + " - left as it is.");
        ok = false;
    }
    else
    {
        var ties = new List<string>();
        if (aWas != ElementId.InvalidElementId) ties.Add(describe(a) + " is associated with " + describe(doc.GetElement(aWas)));
        if (pWas != ElementId.InvalidElementId) ties.Add(describe(p) + " is associated with " + describe(doc.GetElement(pWas)));
        if (ties.Count > 0 && !replace)
        {
            refused.Add(string.Join("; ", ties) + ". An association already there is never broken without asking - say "
                + "replace to remove it and make this one. Nothing was associated.");
            ok = false;
        }
        else
        {
            var removed = new List<string>();
            try
            {
                if (aWas != ElementId.InvalidElementId) { manager.RemoveAssociation(a.Id); removed.Add(describe(a) + " from " + describe(doc.GetElement(aWas))); }
                if (pWas != ElementId.InvalidElementId && tiedTo(p.Id) != ElementId.InvalidElementId)
                { manager.RemoveAssociation(p.Id); removed.Add(describe(p) + " from " + describe(doc.GetElement(pWas))); }
                manager.AddAssociation(a.Id, p.Id);
                doc.Regenerate();
            }
            catch (Exception ex)
            {
                // A tie removed and the new one refused would leave the model half
                // changed - throw, so the caller's transaction rolls it all back.
                if (removed.Count > 0)
                    throw new InvalidOperationException("Revit removed " + string.Join(" and ", removed)
                        + " but would not associate " + describe(a) + " with " + describe(p) + ": " + ex.Message
                        + " The whole change is rolled back.");
                refused.Add("Revit would not associate " + describe(a) + " with " + describe(p) + ": " + ex.Message + " Nothing was associated.");
                ok = false;
            }
            if (ok)
            {
                var now = tiedTo(a.Id);
                if (now == p.Id)
                {
                    associated = 1;
                    associatedIds.Add(a.Id);
                    report = describe(a) + " is now associated with " + describe(p)
                        + (removed.Count == 0 ? "" : "; removed first: " + string.Join("; ", removed)) + " - read back from the model";
                    findings.Add("Associated " + describe(a) + " with " + describe(p) + ". Neither element was moved.");
                }
                else
                    throw new InvalidOperationException("Revit took the association of " + describe(a) + " with " + describe(p)
                        + " but reads it back as " + (now == ElementId.InvalidElementId ? "tied to nothing" : "tied to " + describe(doc.GetElement(now)))
                        + ". The whole change is rolled back.");
            }
        }
    }
}
if (associated == 0) findings.Add("No association was made - the model is as it was.");
