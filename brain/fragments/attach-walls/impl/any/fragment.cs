// NOT STANDALONE. Assumes `doc`, `elements`, `where` and `detach` are in
// scope; leaves `attached`, `attachedIds`, `noTarget`, `report` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// WALLS ATTACHED TO WHAT IS ABOVE OR BELOW THEM - Attach Top/Base in the
// ribbon - or detached again. Each wall finds its own target: the nearest
// roof, floor or ceiling whose plan extent overlaps the wall's, above the
// wall's top (for "top") or below its base (for "base"). Nothing is guessed
// across the building: a wall with nothing over it is named and left.
//
// REVIT 2025 opened wall attachment to code; the card declares 2025 on.
//
// detach true removes every attachment at that end instead.
//
// READ BACK: the wall's attachments at that end are read again after a
// regeneration, and only a wall now attached to its target counts.

var attached = 0;
var attachedIds = new List<ElementId>();
var noTarget = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();

var end = (where ?? "").Trim().ToLowerInvariant();
var location = end == "base" ? AttachmentLocation.Base : AttachmentLocation.Top;
string refusal = null;
if (end != "" && end != "top" && end != "base") refusal = "\"" + where + "\" is not top or base. NOTHING WAS CHANGED.";

// Every roof, floor and ceiling once, with its box.
var hosts = new List<Tuple<Element, BoundingBoxXYZ>>();
if (refusal == null && !detach)
{
    var categories = new[] { BuiltInCategory.OST_Roofs, BuiltInCategory.OST_Floors, BuiltInCategory.OST_Ceilings };
    foreach (var category in categories)
        foreach (var element in new FilteredElementCollector(doc).OfCategory(category).WhereElementIsNotElementType())
        {
            var box = element.get_BoundingBox(null);
            if (box != null) hosts.Add(Tuple.Create(element, box));
        }
}

var tried = new List<Tuple<Wall, ElementId>>();
if (refusal == null)
{
    foreach (var element in elements)
    {
        var wall = element as Wall;
        if (wall == null) { if (element != null) lines.Add(element.Id + ": not a wall."); continue; }

        if (detach)
        {
            try
            {
                foreach (var id in wall.GetAttachmentIds(location).ToList()) wall.RemoveAttachment(id);
                tried.Add(Tuple.Create(wall, ElementId.InvalidElementId));
            }
            catch (Exception ex) { lines.Add("wall " + wall.Id + ": Revit refused - " + ex.Message); }
            continue;
        }

        var box = wall.get_BoundingBox(null);
        if (box == null) { noTarget.Add(wall.Id); continue; }
        Element best = null;
        var bestGap = double.MaxValue;
        foreach (var host in hosts)
        {
            var hb = host.Item2;
            var overlaps = hb.Min.X < box.Max.X && hb.Max.X > box.Min.X && hb.Min.Y < box.Max.Y && hb.Max.Y > box.Min.Y;
            if (!overlaps) continue;
            var gap = location == AttachmentLocation.Top ? hb.Min.Z - box.Max.Z : box.Min.Z - hb.Max.Z;
            // A small negative gap is a host the wall already runs into.
            if (gap < -1.0) continue;
            if (gap < bestGap) { bestGap = gap; best = host.Item1; }
        }
        if (best == null) { noTarget.Add(wall.Id); continue; }
        try
        {
            wall.AddAttachment(best.Id, location);
            tried.Add(Tuple.Create(wall, best.Id));
        }
        catch (Exception ex) { lines.Add("wall " + wall.Id + " to " + best.Category.Name + " " + best.Id + ": Revit refused - " + ex.Message); }
    }

    doc.Regenerate();
    foreach (var pair in tried)
    {
        var now = pair.Item1.GetAttachmentIds(location);
        var ok = detach ? now.Count == 0 : now.Contains(pair.Item2);
        if (ok)
        {
            attached++;
            attachedIds.Add(pair.Item1.Id);
            if (!detach) lines.Add("wall " + pair.Item1.Id + ": " + (location == AttachmentLocation.Top ? "top" : "base")
                + " attached to " + doc.GetElement(pair.Item2).Category.Name + " " + pair.Item2 + ".");
        }
        else lines.Add("wall " + pair.Item1.Id + ": asked, and the attachment does not read back.");
    }
    if (noTarget.Count > 0)
        lines.Add("Nothing to attach to " + (location == AttachmentLocation.Top ? "above" : "below") + ": "
            + string.Join(", ", noTarget.Select(i => i.ToString())));
    findings.Add(attached + " wall(s) " + (detach ? "detached" : "attached") + " at the "
        + (location == AttachmentLocation.Top ? "top" : "base") + " and read back; " + noTarget.Count + " with nothing to attach to.");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
