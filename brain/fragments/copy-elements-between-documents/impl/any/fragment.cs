// NOT STANDALONE. Assumes `doc`, `app`, `sourceDocumentTitle`, `categories`,
// `elementIds`, `offsetMm` and `fromLink` are in scope; leaves `copied`,
// `copiedIds`, `report` and `findings` behind.
//
// VERSION 2 (2026-11-01) - COPY_FROM_LINK MERGED IN, AS `fromLink`.
//
//   THE SOURCE IS A LINK, AND ITS TRANSFORM IS APPLIED. With `fromLink` true,
//   `sourceDocumentTitle` names a LINK in this model - part of its name as
//   Manage Links shows it, or of its file's title - and the copies land where
//   the link puts them: the placement's total transform, then the offset. A
//   link's coordinates are its own world, not this one's; copied without the
//   transform everything lands somewhere plausible and wrong, which is worse
//   than obviously wrong. The ids are ids INSIDE the link.
//
//   ONE PLACEMENT, NEVER A GUESSED ONE. A link placed twice has two
//   transforms; words that match both are refused with each placement named.
//   A link that is not loaded is refused naming RELOAD_LINKS.
//
//   THE COPIES ARE OURS AND STOP TRACKING THE LINK - said in the answer.
//
//   AND THE GAP IT CLOSES ON THE OTHER ROUTE. A loaded link's document is in
//   Application.Documents, so version 1 could match one by title and copy out
//   of it with NO transform. Without `fromLink` a linked document is now
//   passed over, and words that match only a link are refused pointing at
//   `fromLink`.
//
//   FEWER ARRIVED THAN WERE PICKED is said: Revit refuses some kinds of
//   element across documents quietly, by returning a shorter list.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// COPIES ELEMENTS FROM ANOTHER OPEN DOCUMENT INTO THE ONE IN FRONT - Copy to
// Clipboard in one window and Paste Aligned in the other. Either side can be
// a project or a family: forms and lines from one family into another, model
// elements from one project into this one.
//
//   sourceDocumentTitle   part of the other document's title
//   categories            category names, comma separated, or "all" for
//                         every model element; EMPTY when ids are given
//   elementIds            ids in the OTHER document, comma separated
//   offsetMm              "x,y,z" moved on the way; EMPTY keeps them aligned
//
// View-specific elements (detail lines, text, dimensions) belong to a view
// and are left, counted: Revit copies those view to view. Types and
// materials the copies need come with them, as Revit's own paste brings them.
//
// READ BACK: every id Revit returns is looked up in this document.

const double MmPerFoot = 304.8;
var copied = 0;
var copiedIds = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();
string refusal = null;

var wanted = (sourceDocumentTitle ?? "").Trim();
Document source = null;
var openTitles = new List<string>();
var linkedTitleMatched = false;
var placed = Transform.Identity;
string linkRefusal = null;
if (!fromLink)
{
    foreach (Document candidate in app.Documents)
    {
        if (candidate.Equals(doc)) continue;
        var title = "";
        try { title = candidate.Title ?? ""; } catch { }
        // A LINKED document is never a source here: it has no transform on
        // this route (version 2). Said below when it is the only match.
        var linkedHere = false;
        try { linkedHere = candidate.IsLinked; } catch { }
        if (linkedHere)
        {
            if (wanted.Length > 0 && title.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0) linkedTitleMatched = true;
            continue;
        }
        openTitles.Add(title);
        if (wanted.Length > 0 && title.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0 && source == null) source = candidate;
    }
}
else
{
    // COPY_FROM_LINK's route: the link placement carries both the document and
    // the transform, so neither is guessed.
    var placements = new List<RevitLinkInstance>();
    var placementNames = new List<string>();
    foreach (var instance in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
    {
        var name = "";
        try { name = instance.Name ?? ""; } catch { }
        Document linkedDoc = null;
        try { linkedDoc = instance.GetLinkDocument(); } catch { }
        var linkTitle = "";
        try { linkTitle = linkedDoc == null ? "" : (linkedDoc.Title ?? ""); } catch { }
        openTitles.Add(name);
        if (wanted.Length == 0) continue;
        if (name.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) < 0
            && linkTitle.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) < 0) continue;
        placements.Add(instance);
        placementNames.Add(name);
    }
    if (placements.Count == 0)
        linkRefusal = "No link in \"" + doc.Title + "\" matches \"" + sourceDocumentTitle + "\"; links here: "
            + (openTitles.Count > 0 ? string.Join(", ", openTitles) : "none") + ". NOTHING WAS COPIED.";
    else if (placements.Count > 1)
        linkRefusal = placements.Count + " link placements match \"" + sourceDocumentTitle + "\" - "
            + string.Join("; ", placementNames) + ". Each has its own position, so name the one you mean. NOTHING WAS COPIED.";
    else
    {
        Document linkedDoc = null;
        try { linkedDoc = placements[0].GetLinkDocument(); } catch { }
        if (linkedDoc == null)
            linkRefusal = "'" + placementNames[0] + "' is not loaded, so there is nothing to copy out of it - "
                + "RELOAD_LINKS loads it. NOTHING WAS COPIED.";
        else
        {
            try { placed = placements[0].GetTotalTransform(); }
            catch (Exception ex) { linkRefusal = "The link's position could not be read - " + ex.Message + ". NOTHING WAS COPIED."; }
            if (linkRefusal == null) source = linkedDoc;
        }
    }
}

var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;
var offset = XYZ.Zero;
var offsetText = (offsetMm ?? "").Trim();
if (offsetText != "")
{
    var parts = offsetText.Split(',');
    double x, y, z;
    if (parts.Length == 3 && double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
        && double.TryParse(parts[1].Trim(), numberStyle, culture, out y)
        && double.TryParse(parts[2].Trim(), numberStyle, culture, out z)) offset = new XYZ(x / MmPerFoot, y / MmPerFoot, z / MmPerFoot);
    else refusal = "The offset is x,y,z in millimetres. NOTHING WAS COPIED.";
}

if (refusal == null && linkRefusal != null) refusal = linkRefusal;
if (refusal == null && source == null && linkedTitleMatched)
    refusal = "\"" + sourceDocumentTitle + "\" is a LINKED model here, not another open document. Ask again with "
        + "fromLink=true so the copies land where the link puts them. NOTHING WAS COPIED.";
if (refusal == null && source == null)
    refusal = "No other open document matches \"" + sourceDocumentTitle + "\" - both have to be open in this Revit; open now: "
        + (openTitles.Count > 0 ? string.Join(", ", openTitles) : "nothing else") + ". NOTHING WAS COPIED.";

var picked = new List<ElementId>();
var viewOwned = 0;
if (refusal == null)
{
    var idTexts = new HashSet<string>((elementIds ?? "").Split(',').Select(t => t.Trim()).Where(t => t != ""));
    var names = new HashSet<string>((categories ?? "").Split(',').Select(t => t.Trim().ToLowerInvariant()).Where(t => t != ""));
    var everything = names.Contains("all");
    if (idTexts.Count == 0 && names.Count == 0) refusal = "Give category names, \"all\", or element ids from the other document. NOTHING WAS COPIED.";
    else
    {
        foreach (var element in new FilteredElementCollector(source).WhereElementIsNotElementType())
        {
            var byId = idTexts.Contains(element.Id.ToString()) || idTexts.Contains(element.UniqueId);
            var category = element.Category;
            var byCategory = false;
            if (!byId && category != null && names.Count > 0)
            {
                var own = category.Name.ToLowerInvariant();
                var parent = category.Parent != null ? category.Parent.Name.ToLowerInvariant() : "";
                // "all" means model elements with an extent - a material,
                // a sketch plane or a type has none and is not pasted.
                byCategory = everything ? category.CategoryType == CategoryType.Model && element.get_BoundingBox(null) != null
                    : names.Contains(own) || names.Contains(parent);
            }
            if (!byId && !byCategory) continue;
            if (element.OwnerViewId != ElementId.InvalidElementId) { viewOwned++; continue; }
            picked.Add(element.Id);
        }
        if (picked.Count == 0) refusal = "Nothing in \"" + source.Title + "\" matched" + (viewOwned > 0 ? " outside a view (" + viewOwned + " view-specific left)" : "") + ". NOTHING WAS COPIED.";
    }
}

if (refusal == null)
{
    try
    {
        // The link's own placement first, then the offset, in this model's space.
        var moveBy = Transform.CreateTranslation(offset).Multiply(placed);
        var arrived = ElementTransformUtils.CopyElements(source, picked, doc, moveBy, new CopyPasteOptions());
        doc.Regenerate();
        foreach (var id in arrived)
            if (doc.GetElement(id) != null) { copied++; copiedIds.Add(id); }
        var byCategory = copiedIds.Select(id => doc.GetElement(id)).Where(e => e.Category != null)
            .GroupBy(e => e.Category.Name).Select(g => g.Key + " " + g.Count());
        lines.Add(picked.Count + " element(s) picked in \"" + source.Title + "\"; " + copied + " arrived in \"" + doc.Title
            + "\" and read back: " + string.Join(", ", byCategory) + ".");
        if (viewOwned > 0) lines.Add(viewOwned + " view-specific element(s) left - Revit copies those view to view.");
        if (copied < picked.Count)
            lines.Add((picked.Count - copied) + " picked did not arrive - Revit refuses some kinds of element across "
                + "documents, and does so quietly by returning fewer ids.");
        if (fromLink)
            lines.Add("Placed through the link's own transform. They are OURS now and do NOT track the link - "
                + "reload it after the other team moves something and these stay where they are.");
    }
    catch (Exception ex) { refusal = "Revit refused the copy - " + ex.Message + ". NOTHING WAS COPIED."; }
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.Add(copied + " element(s) copied from " + (fromLink ? "a linked model." : "another open document."));
findings.AddRange(lines);
