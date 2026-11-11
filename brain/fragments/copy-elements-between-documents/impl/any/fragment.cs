// NOT STANDALONE. Assumes `doc`, `app`, `sourceDocumentTitle`, `categories`,
// `elementIds`, `offsetMm`, `fromLink`, `labels`, `labelContains` and
// `placeAtMm` are in scope; leaves `copied`, `copiedIds`, `kept`, `readBack`,
// `report` and `findings` behind.
//
// VERSION 4 (2026-11-11, row 5b-1123) - A TITLE TWO OPEN DOCUMENTS CARRY IS
// REFUSED, NEVER GUESSED.
//
//   Versions 1 to 3 took the FIRST open document whose title matched and said
//   nothing - right only while every document of that title is the same file.
//   Now, when the document the words find shares its title with another open
//   document, nothing is copied: the answer names each one's path, or says it
//   is not saved yet, and asks for the ones not meant to be closed - as a link
//   placed twice is refused on the other route. The document in front and
//   linked documents are not counted: neither is ever a source here. Label
//   mode finds its seed the same way, so the same refusal holds there.
//
// VERSION 3 (2026-11-01) - COPY_LABELS_FROM_FAMILY MERGED IN, AS `labels`.
//
//   VIEW-SPECIFIC ELEMENTS STAY OUT BY DEFAULT - this card's rule, kept. LABEL
//   MODE IS EXPLICIT: `labels` true copies LABELS from a seed family open in
//   this Revit into the tag, annotation or title block family in front, VIEW
//   TO VIEW - Copy to Clipboard and Paste Aligned, the one route to a new
//   label, because Revit's API makes none on any release 2020 to 2027.
//   Picked in the seed by `elementIds` (label ids, or empty for all) and by
//   `labelContains` (part of a label's text, or on 2027 of a parameter's name,
//   sample, prefix or suffix); the first picked - the top left one - lands at
//   `placeAtMm` x,y, the rest keeping their spacing, or "keep" where the seed
//   has them.
//
//   ONE CATEGORY, OR NOTHING - Revit's own failure reads "Can't paste Labels
//   across Families of different Categories", so a seed of another category is
//   refused before anything is copied. Both sides must be families, and the
//   source never a link.
//
//   READ BACK LABEL BY LABEL against its seed: text and label type on every
//   release, and on Revit 2027 every parameter in order with its sample value,
//   prefix, suffix, spaces, break and unit format. A copy that differs is
//   named, never counted in `kept`. A label type of the same name here is
//   named before the copy.
//
//   THE SAFER RULE WHERE THEY DIFFERED: COPY_LABELS_FROM_FAMILY skipped a label
//   Revit refused, or one with no view of its kind here, and kept the rest;
//   here, as for every copy this card makes, nothing is copied then - the
//   call THROWS and is rolled back whole.
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
var openPaths = new List<string>();
string titleRefusal = null;
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
        var path = "";
        try { path = candidate.PathName ?? ""; } catch { }
        openPaths.Add(path != "" ? path : "one not saved yet");
        if (wanted.Length > 0 && title.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0 && source == null) source = candidate;
    }
    // VERSION 4: a title two open documents carry cannot say which is meant.
    if (source != null)
    {
        var sourceTitle = "";
        try { sourceTitle = source.Title ?? ""; } catch { }
        var sameTitle = new List<string>();
        for (var i = 0; i < openTitles.Count; i++)
            if (string.Equals(openTitles[i], sourceTitle, StringComparison.OrdinalIgnoreCase)) sameTitle.Add(openPaths[i]);
        if (sameTitle.Count > 1)
        {
            titleRefusal = sameTitle.Count + " open documents are titled \"" + sourceTitle + "\" - " + string.Join("; ", sameTitle)
                + ". The title cannot say which one to copy from: close the ones you do not mean and ask again. NOTHING WAS COPIED.";
            source = null;
        }
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
if (refusal == null && titleRefusal != null) refusal = titleRefusal;
if (refusal == null && source == null && linkedTitleMatched)
    refusal = "\"" + sourceDocumentTitle + "\" is a LINKED model here, not another open document. Ask again with "
        + "fromLink=true so the copies land where the link puts them. NOTHING WAS COPIED.";
if (refusal == null && source == null)
    refusal = "No other open document matches \"" + sourceDocumentTitle + "\" - both have to be open in this Revit; open now: "
        + (openTitles.Count > 0 ? string.Join(", ", openTitles) : "nothing else") + ". NOTHING WAS COPIED.";

// VERSION 3, from COPY_LABELS_FROM_FAMILY: what label mode leaves.
var kept = 0;
var readBack = "";
if (refusal == null && labels)
{
    if (fromLink)
        refusal = "Labels are copied from a seed FAMILY open in this Revit, never out of a link. Ask with fromLink "
            + "false. NOTHING WAS COPIED.";
    else if (!doc.IsFamilyDocument)
        refusal = "\"" + doc.Title + "\" is a project, not a family. Labels are copied into a tag, annotation or title "
            + "block family open in the Family Editor. NOTHING WAS COPIED.";
    else if (!source.IsFamilyDocument)
        refusal = "\"" + source.Title + "\" is a project, not a seed family. NOTHING WAS COPIED.";
    else if (offsetText != "")
        refusal = "A label is placed by placeAtMm - x,y in millimetres, or keep - not by offsetMm. NOTHING WAS COPIED.";
    else
    {
        var categoryWords = (categories ?? "").Trim().ToLowerInvariant();
        if (categoryWords != "" && categoryWords != "all" && categoryWords != "labels" && categoryWords != "label")
            refusal = "With labels true the labels are picked by elementIds (label ids in the seed) and labelContains, "
                + "and categories is left empty, \"all\" or \"labels\" - it reads \"" + categories + "\". NOTHING WAS "
                + "COPIED.";
    }
}

var picked = new List<ElementId>();
var viewOwned = 0;
if (refusal == null && labels)
{
    // ===========================================================================
    // VERSION 3 - LABEL MODE, COPY_LABELS_FROM_FAMILY's, moved whole except
    // where the safer rule is this card's. See the header.
    // ===========================================================================
    Func<Element, bool> isLabel = element => element is TextElement && !(element is TextNote);
    Func<Element, string> nameOf = element =>
    {
        if (element == null) return "(none)";
        try { return element.Name; } catch (Exception) { return "(no name)"; }
    };
    Func<TextElement, string> textOf = label =>
    {
        try { return (label.Text ?? "").Replace("\r", " ").Replace("\n", " ").Trim(); } catch (Exception) { return ""; }
    };
    Func<double, string> mm = feet => Math.Round(feet * MmPerFoot, 1).ToString("0.#", culture);
    Func<TextElement, XYZ> pointOf = label =>
    {
        try { return label.Coord; } catch (Exception) { return XYZ.Zero; }
    };

    // What a label SHOWS, as one comparable line - the same reading on the seed
    // and on the copy.
    Func<Document, TextElement, string> contentOf = (owner, label) =>
    {
        var bits = new List<string>();
        bits.Add("text \"" + textOf(label) + "\"");
        bits.Add("type " + nameOf(owner.GetElement(label.GetTypeId())));
#if REVIT2027_OR_GREATER
        var annotation = label as AnnotationLabel;
        if (annotation != null)
        {
            try
            {
                foreach (var each in annotation.GetParameterFormatting())
                {
                    InternalDefinition definition = null;
                    try { definition = each.GetParameterDefinition(owner); } catch (Exception) { }
                    var format = "format not read";
                    if (definition != null)
                    {
                        try
                        {
                            var options = label.GetParameterFormatOptions(definition.Id);
                            format = options == null ? "format none" : options.UseDefault ? "project units"
                                : options.GetUnitTypeId().TypeId + " " + options.Accuracy.ToString("0.########", culture)
                                  + (options.UsePlusPrefix ? " +" : "") + (options.UseDigitGrouping ? " grouped" : "")
                                  + " " + options.GetSymbolTypeId().TypeId;
                        }
                        catch (Exception) { }
                    }
                    bits.Add((definition != null ? definition.Name : "?") + " [sample \"" + each.SampleText + "\", prefix \""
                        + each.Prefix + "\", suffix \"" + each.Suffix + "\", spaces " + each.LeadingSpaces + ", break "
                        + (each.LeadingLineBreak ? "yes" : "no") + ", " + format + "]");
                }
                bits.Add("wrap between parameters only " + (annotation.UseNonBreakingSpaces ? "on" : "off"));
            }
            catch (Exception ex) { bits.Add("parameters not read - " + ex.Message); }
        }
#endif
        return string.Join(" ; ", bits);
    };

    // One category, or nothing - Revit's own rule.
    Category here = null, there = null;
    try { here = doc.OwnerFamily.FamilyCategory; } catch (Exception) { }
    try { there = source.OwnerFamily.FamilyCategory; } catch (Exception) { }
    if (here == null || there == null)
        refusal = "The category of " + (here == null ? "this family" : "the seed") + " could not be read. NOTHING WAS COPIED.";
    else if (here.Id != there.Id)
        refusal = "Revit does not paste labels between categories - \"Can't paste Labels across Families of different "
            + "Categories\" - and the seed \"" + source.Title + "\" is " + there.Name + " while this family is " + here.Name
            + ". Use a seed of " + here.Name + ". NOTHING WAS COPIED.";

    // The seed's labels to copy - by id, by what they hold, or all.
    var chosen = new List<TextElement>();
    if (refusal == null)
    {
        var everyLabel = new List<TextElement>();
        foreach (var element in new FilteredElementCollector(source).WhereElementIsNotElementType())
            if (isLabel(element)) everyLabel.Add((TextElement)element);
        var ids = (elementIds ?? "").Trim();
        var byIds = ids.Length > 0 && !string.Equals(ids, "all", StringComparison.OrdinalIgnoreCase);
        var idSet = new HashSet<string>(ids.Split(',').Select(t => t.Trim()).Where(t => t != ""), StringComparer.OrdinalIgnoreCase);
        var said = (labelContains ?? "").Trim();
        var parts = said.Length == 0 || string.Equals(said, "all", StringComparison.OrdinalIgnoreCase)
            ? new List<string>()
            : said.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
        foreach (var label in everyLabel)
        {
            if (byIds && !idSet.Contains(label.Id.ToString()) && !idSet.Contains(label.UniqueId)) continue;
            if (parts.Count > 0)
            {
                var content = contentOf(source, label);
                if (!parts.Any(p => content.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0)) continue;
            }
            chosen.Add(label);
        }
        if (everyLabel.Count == 0) refusal = "The seed \"" + source.Title + "\" has no labels. NOTHING WAS COPIED.";
        else if (chosen.Count == 0)
            refusal = "No label in \"" + source.Title + "\" matched - it has " + everyLabel.Count + " label(s); "
                + "REPORT_FAMILY_LABELS on the seed lists them. NOTHING WAS COPIED.";
        chosen = chosen.OrderByDescending(l => pointOf(l).Y).ThenBy(l => pointOf(l).X).ToList();
    }

    // Where they land.
    var shift = XYZ.Zero;
    if (refusal == null)
    {
        var placeText = (placeAtMm ?? "").Trim();
        if (placeText != "" && !string.Equals(placeText, "keep", StringComparison.OrdinalIgnoreCase))
        {
            var bits = placeText.Split(',');
            double x, y;
            if (bits.Length == 2 && double.TryParse(bits[0].Trim(), numberStyle, culture, out x)
                && double.TryParse(bits[1].Trim(), numberStyle, culture, out y))
            {
                var first = pointOf(chosen[0]);
                shift = new XYZ(x / MmPerFoot - first.X, y / MmPerFoot - first.Y, 0);
            }
            else refusal = "placeAtMm is x,y in millimetres, such as 0,5, or keep. NOTHING WAS COPIED.";
        }
    }

    // The view each label goes into: the one in front when it is the seed
    // view's kind, else this family's view of the same kind and name, else of
    // that kind.
    Func<View, View> viewHere = seedView =>
    {
        var front = doc.ActiveView;
        if (front != null && !front.IsTemplate && front.ViewType == seedView.ViewType) return front;
        View sameKind = null;
        foreach (var view in new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>())
        {
            if (view.IsTemplate || view.ViewType != seedView.ViewType) continue;
            if (string.Equals(view.Name, seedView.Name, StringComparison.OrdinalIgnoreCase)) return view;
            if (sameKind == null) sameKind = view;
        }
        return sameKind;
    };

    if (refusal == null)
    {
        // A label type named the same here may make Revit ask - say so first.
        var typesHere = new HashSet<string>(new FilteredElementCollector(doc).WhereElementIsElementType()
            .Select(t => nameOf(t)), StringComparer.OrdinalIgnoreCase);
        var clash = chosen.Select(l => nameOf(source.GetElement(l.GetTypeId()))).Where(n => typesHere.Contains(n))
            .Distinct().ToList();
        if (clash.Count > 0)
            lines.Add("Label type(s) named the same in both families: " + string.Join(", ", clash) + ". If they differ "
                + "Revit asks about duplicate types; Heron declines that box, the copy stops and nothing stays.");

        // EACH LABEL ON ITS OWN, so each copy is known to come from one seed
        // label. ONE THAT CANNOT BE COPIED UNDOES THE CALL - this card's rule,
        // the safer one: COPY_LABELS_FROM_FAMILY skipped it and kept the rest.
        var after = new List<string>();
        foreach (var seedLabel in chosen)
        {
            var arrived = new List<ElementId>();
            var seedView = source.GetElement(seedLabel.OwnerViewId) as View;
            var move = Transform.CreateTranslation(shift);
            ICollection<ElementId> made;
            View target = null;
            if (seedView != null)
            {
                target = viewHere(seedView);
                if (target == null)
                    throw new InvalidOperationException("Label " + seedLabel.Id + ": this family has no " + seedView.ViewType
                        + " view to paste it into. NOTHING WAS COPIED - the whole call is rolled back.");
            }
            try
            {
                made = seedView != null
                    ? ElementTransformUtils.CopyElements(seedView, new List<ElementId> { seedLabel.Id }, target, move,
                        new CopyPasteOptions())
                    : ElementTransformUtils.CopyElements(source, new List<ElementId> { seedLabel.Id }, doc, move,
                        new CopyPasteOptions());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Label " + seedLabel.Id + ": Revit refused the copy - " + ex.Message
                    + " NOTHING WAS COPIED - the whole call is rolled back.");
            }
            arrived.AddRange(made);
            doc.Regenerate();

            var copies = arrived.Select(id => doc.GetElement(id)).Where(e => e != null && isLabel(e)).Cast<TextElement>()
                .ToList();
            if (copies.Count == 0)
                throw new InvalidOperationException("Label " + seedLabel.Id + ": Revit made no label from it ("
                    + arrived.Count + " element(s) arrived, none a label). NOTHING WAS COPIED - the whole call is rolled "
                    + "back.");
            var seedContent = contentOf(source, seedLabel);
            foreach (var copy in copies)
            {
                copied++;
                copiedIds.Add(copy.Id);
                var copyContent = contentOf(doc, copy);
                var same = copyContent == seedContent;
                if (same) kept++;
                var landed = pointOf(copy);
                after.Add("label " + copy.Id + " from seed " + seedLabel.Id + " | at " + mm(landed.X) + "," + mm(landed.Y)
                    + " mm | " + (same ? "shows what the seed shows" : "DIFFERS - seed: " + seedContent + " / copy: "
                        + copyContent) + " | " + copyContent);
            }
        }
        readBack = string.Join("  ||  ", after);
        lines.Insert(0, chosen.Count + " label(s) picked in \"" + source.Title + "\"; " + copied + " arrived in \""
            + doc.Title + "\", " + kept + " showing exactly what their seed shows.");
#if REVIT2027_OR_GREATER
        lines.Add("Compared parameter by parameter: name, sample value, prefix, suffix, spaces, break and unit format.");
#else
        lines.Add("Compared by text and label type only - Revit " + app.VersionNumber + " cannot read a label's "
            + "parameters; open Edit Label on a copy to see them.");
#endif
    }
}
else if (refusal == null)
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

if (refusal == null && !labels)
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
findings.Add(labels
    ? copied + " label(s) copied from a seed family; " + kept + " read back the same as their seed."
    : copied + " element(s) copied from " + (fromLink ? "a linked model." : "another open document."));
findings.AddRange(lines);
