// NOT STANDALONE. Assumes `doc`, `app`, `sourceDocumentTitle`, `labelIds`,
// `labelContains` and `placeAtMm` are in scope; leaves `copied`, `copiedIds`,
// `kept`, `readBack`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry, in
// the family in front. The seed family is only read: nothing is written to it.
//
// THE ONE ROUTE TO A NEW LABEL. Revit's API cannot make a label on any release
// 2020 to 2027 - 2027's AnnotationLabel.Create exists and is internal (docs/43
// section 8.3a). What every release can do is copy one: Copy to Clipboard in a
// seed family and Paste Aligned in this one, view to view, the way a modeller
// reuses a label already set up in Edit Label.
//
// ONE CATEGORY, OR NOTHING. Revit's own failure CannotPasteLabelsAcrossFamilies
// reads "Can't paste Labels across Families of different Categories", so a
// seed of another category is refused here, before anything is copied, in
// those words - a Pipe Tags seed for a Pipe Tags family.
//
//   sourceDocumentTitle  part of the seed family's title; it is open in this
//                        Revit, as COPY_ELEMENTS_BETWEEN_DOCUMENTS needs
//   labelIds             label ids in the SEED, comma separated, or "all"
//   labelContains        part of a label's text - or on 2027 a parameter's
//                        name - to pick by; "all" picks every label
//   placeAtMm            "x,y" in mm: where the first label picked (the top
//                        left one) lands; the rest keep their spacing.
//                        "keep" leaves them where they are in the seed
//
// READ BACK, LABEL BY LABEL. Each label is copied on its own so each copy is
// known to come from one seed label; the copy is then read as REPORT_FAMILY_
// LABELS reads it and compared with its seed - its text and type on every
// release, and on 2027 every parameter in order with its sample value, prefix,
// suffix, spaces, break and the unit format Revit returns. A copy that differs
// is named, never counted as kept. WHETHER REVIT KEEPS ALL OF THAT ACROSS A
// COPY IS WHAT NEEDS-CHECKING GROUP IJ MEASURES - this claims nothing in
// advance.
//
// A DUPLICATE TYPE NAME: when the seed's label type carries the name of a
// different type here, Revit asks in a dialog; Heron declines it during a job,
// the copy stops and nothing stays. The answer names the type beforehand.

const double MmPerFoot = 304.8;
var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;
var copied = 0;
var copiedIds = new List<ElementId>();
var kept = 0;
var readBack = "";
var findings = new List<string>();
var lines = new List<string>();
string refusal = null;

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

if (!doc.IsFamilyDocument)
    refusal = "\"" + doc.Title + "\" is a project, not a family. Labels are copied into a tag, annotation or title block "
        + "family open in the Family Editor. NOTHING WAS COPIED.";

// The seed - another family open in this Revit.
var wanted = (sourceDocumentTitle ?? "").Trim();
Document source = null;
var openTitles = new List<string>();
if (refusal == null)
{
    foreach (Document candidate in app.Documents)
    {
        if (candidate.Equals(doc)) continue;
        var title = "";
        try { title = candidate.Title ?? ""; } catch (Exception) { }
        openTitles.Add(title);
        if (source == null && wanted.Length > 0 && title.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0) source = candidate;
    }
    if (source == null)
        refusal = "No other open document matches \"" + sourceDocumentTitle + "\" - open the seed family in this Revit first; "
            + "open now: " + (openTitles.Count > 0 ? string.Join(", ", openTitles) : "nothing else") + ". NOTHING WAS COPIED.";
    else if (!source.IsFamilyDocument)
        refusal = "\"" + source.Title + "\" is a project, not a seed family. NOTHING WAS COPIED.";
}

// One category, or nothing - Revit's own rule.
if (refusal == null)
{
    Category here = null, there = null;
    try { here = doc.OwnerFamily.FamilyCategory; } catch (Exception) { }
    try { there = source.OwnerFamily.FamilyCategory; } catch (Exception) { }
    if (here == null || there == null)
        refusal = "The category of " + (here == null ? "this family" : "the seed") + " could not be read. NOTHING WAS COPIED.";
    else if (here.Id != there.Id)
        refusal = "Revit does not paste labels between categories - \"Can't paste Labels across Families of different "
            + "Categories\" - and the seed \"" + source.Title + "\" is " + there.Name + " while this family is " + here.Name
            + ". Use a seed of " + here.Name + ". NOTHING WAS COPIED.";
}

// The seed's labels to copy - by id, by what they hold, or all.
var picked = new List<TextElement>();
if (refusal == null)
{
    var everyLabel = new List<TextElement>();
    foreach (var element in new FilteredElementCollector(source).WhereElementIsNotElementType())
        if (isLabel(element)) everyLabel.Add((TextElement)element);
    var ids = (labelIds ?? "").Trim();
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
        picked.Add(label);
    }
    if (everyLabel.Count == 0) refusal = "The seed \"" + source.Title + "\" has no labels. NOTHING WAS COPIED.";
    else if (picked.Count == 0)
        refusal = "No label in \"" + source.Title + "\" matched - it has " + everyLabel.Count + " label(s); REPORT_FAMILY_LABELS "
            + "on the seed lists them. NOTHING WAS COPIED.";
    picked = picked.OrderByDescending(l => pointOf(l).Y).ThenBy(l => pointOf(l).X).ToList();
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
            var first = pointOf(picked[0]);
            shift = new XYZ(x / MmPerFoot - first.X, y / MmPerFoot - first.Y, 0);
        }
        else refusal = "placeAtMm is x,y in millimetres, such as 0,5, or keep. NOTHING WAS COPIED.";
    }
}

// The view each label goes into: the one in front when it is the seed view's
// kind, else this family's view of the same kind and name, else of that kind.
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
    var clash = picked.Select(l => nameOf(source.GetElement(l.GetTypeId()))).Where(n => typesHere.Contains(n)).Distinct().ToList();
    if (clash.Count > 0)
        lines.Add("Label type(s) named the same in both families: " + string.Join(", ", clash) + ". If they differ Revit asks "
            + "about duplicate types; Heron declines that box, the copy stops and nothing stays.");

    var after = new List<string>();
    foreach (var seedLabel in picked)
    {
        var arrived = new List<ElementId>();
        try
        {
            var seedView = source.GetElement(seedLabel.OwnerViewId) as View;
            var move = Transform.CreateTranslation(shift);
            ICollection<ElementId> made;
            if (seedView != null)
            {
                var target = viewHere(seedView);
                if (target == null)
                {
                    lines.Add("label " + seedLabel.Id + ": this family has no " + seedView.ViewType + " view to paste it into - left");
                    continue;
                }
                made = ElementTransformUtils.CopyElements(seedView, new List<ElementId> { seedLabel.Id }, target, move, new CopyPasteOptions());
            }
            else made = ElementTransformUtils.CopyElements(source, new List<ElementId> { seedLabel.Id }, doc, move, new CopyPasteOptions());
            arrived.AddRange(made);
        }
        catch (Exception ex)
        {
            lines.Add("label " + seedLabel.Id + ": Revit refused the copy - " + ex.Message);
            continue;
        }
        doc.Regenerate();

        var copies = arrived.Select(id => doc.GetElement(id)).Where(e => e != null && isLabel(e)).Cast<TextElement>().ToList();
        if (copies.Count == 0)
        {
            lines.Add("label " + seedLabel.Id + ": Revit made no label from it (" + arrived.Count + " element(s) arrived, none a label)");
            continue;
        }
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
                + " mm | " + (same ? "shows what the seed shows" : "DIFFERS - seed: " + seedContent + " / copy: " + copyContent)
                + " | " + copyContent);
        }
    }
    readBack = string.Join("  ||  ", after);
    lines.Insert(0, picked.Count + " label(s) picked in \"" + source.Title + "\"; " + copied + " arrived in \"" + doc.Title
        + "\", " + kept + " showing exactly what their seed shows.");
#if REVIT2027_OR_GREATER
    lines.Add("Compared parameter by parameter: name, sample value, prefix, suffix, spaces, break and unit format.");
#else
    lines.Add("Compared by text and label type only - Revit " + app.VersionNumber + " cannot read a label's parameters; "
        + "open Edit Label on a copy to see them.");
#endif
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.Add(copied + " label(s) copied from a seed family; " + kept + " read back the same as their seed.");
findings.AddRange(lines);
