// NOT STANDALONE. Assumes `doc` and `labelContains` are in scope; leaves
// `labelCount`, `labelIds`, `labelList`, `parameterCount`, `parameterList`,
// `labelProperties`, `labelTypes`, `canReadParameters`, `familyCategory`,
// `textElements`, `textNotes`, `notAFamily` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// EVERY LABEL IN THE FAMILY OPEN IN THE FAMILY EDITOR - a tag, a generic
// annotation, a title block, any category. Where each one is, its view, its
// label type, its width, its alignment, its angle, Keep Readable, what its
// Visible is linked to, and the text Revit gives for it.
//
// WHAT A LABEL SHOWS IS READABLE ON REVIT 2027 ONLY. Revit 2020 to 2026 have
// no member that says which parameters a label shows; 2027's AnnotationLabel
// gives them in order, each with its sample value, prefix, suffix, spaces and
// break. Read 2026-10-21 off every release's shipped assemblies - docs/43
// section 8.3a has the table. So one #if: on 2027 each parameter is listed,
// before it the answer says plainly that this Revit cannot.
//
// THE UNIT FORMAT IS ASKED, NOT CLAIMED. Element.GetParameterFormatOptions is
// on every release; given the parameter's id, which only 2027 can give, it is
// asked on the label. Whether Revit answers with the label's own "Edit
// parameter's unit format" setting is NEEDS-CHECKING IJ3 - the answer says
// what Revit returned, never more.
//
// A LABEL IS FOUND BY WHAT IT IS, NOT BY OfClass. On 2020 to 2026 a label has
// no class of its own: TextElement's one public subclass is TextNote, so a
// text element that is not a text note is taken as a label - and every text
// note is counted and left out, so a family with notes and no labels says so.
// On 2027 it is an AnnotationLabel.
//
// EVERY LIST COMES BACK AS ONE STRING. A list reaches a chat as its first three.

const double MmPerFoot = 304.8;
var culture = System.Globalization.CultureInfo.InvariantCulture;
var findings = new List<string>();
var labelCount = 0;
var labelIds = new List<ElementId>();
var labelList = "";
var parameterCount = 0;
var parameterList = "";
var labelProperties = "";
var labelTypes = "";
var canReadParameters = false;
var familyCategory = "";
var textElements = 0;
var textNotes = 0;
var notAFamily = false;

var release = "";
try { release = doc.Application.VersionNumber; } catch (Exception) { }

// "all", or nothing, is no narrowing. Several parts with commas or semicolons
// between: a label whose text - or, on 2027, a parameter's name, sample,
// prefix or suffix - holds any one of them, ignoring case.
var said = (labelContains ?? "").Trim();
var parts = said.Length == 0 || string.Equals(said, "all", StringComparison.OrdinalIgnoreCase)
    ? new List<string>()
    : said.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
Func<string, bool> holds = text =>
    parts.Count == 0 || parts.Any(p => (text ?? "").IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);

Func<double, string> mm = feet => Math.Round(feet * MmPerFoot, 1).ToString("0.#", culture);
Func<Element, string> nameOf = element =>
{
    if (element == null) return "(none)";
    try { return element.Name; } catch (Exception) { return "(no name)"; }
};
Func<TextElement, string> textOf = label =>
{
    try { return (label.Text ?? "").Replace("\r", " ").Replace("\n", " ").Trim(); }
    catch (Exception ex) { return "(Revit gave no text - " + ex.Message + ")"; }
};

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    findings.Add("\"" + doc.Title + "\" is a project, not a family. Labels live in a tag, annotation or title block "
        + "family open in the Family Editor - open one and ask again. NOTHING WAS READ.");
}
else
{
    try
    {
        var owner = doc.OwnerFamily;
        if (owner != null && owner.FamilyCategory != null) familyCategory = owner.FamilyCategory.Name;
    }
    catch (Exception) { }

    var labels = new List<TextElement>();
    foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
    {
        var text = element as TextElement;
        if (text == null) continue;
        textElements++;
        if (text is TextNote) { textNotes++; continue; }
        labels.Add(text);
    }
    labels = labels
        .OrderBy(l => nameOf(doc.GetElement(l.OwnerViewId)))
        .ThenByDescending(l => { try { return l.Coord.Y; } catch (Exception) { return 0.0; } })
        .ThenBy(l => { try { return l.Coord.X; } catch (Exception) { return 0.0; } })
        .ToList();

    var lines = new List<string>();
    var paramLines = new List<string>();
    var propertyLines = new List<string>();
    var typeNames = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

#if REVIT2027_OR_GREATER
    canReadParameters = true;
    var familyParameterIds = new HashSet<string>();
    try { foreach (FamilyParameter fp in doc.FamilyManager.Parameters) familyParameterIds.Add(fp.Id.ToString()); }
    catch (Exception) { }
    Func<InternalDefinition, string> kindOf = definition =>
    {
        if (definition == null) return "unknown";
        if (definition.BuiltInParameter != BuiltInParameter.INVALID) return "built-in";
        var shared = doc.GetElement(definition.Id) as SharedParameterElement;
        if (shared != null) return "shared, GUID " + shared.GuidValue;
        if (familyParameterIds.Contains(definition.Id.ToString())) return "a parameter of this family";
        return "a parameter Revit holds for this category";
    };
    // What GetParameterFormatOptions returns on the label for that parameter,
    // in the words of the "Edit parameter's unit format" box.
    Func<Element, InternalDefinition, string> formatOf = (label, definition) =>
    {
        if (definition == null) return "not read";
        FormatOptions options;
        try { options = label.GetParameterFormatOptions(definition.Id); }
        catch (Exception ex) { return "not read - " + ex.Message; }
        if (options == null) return "not read - Revit gave none";
        if (options.UseDefault) return "Use project settings";
        var said2 = new List<string>();
        try { said2.Add(LabelUtils.GetLabelForUnit(options.GetUnitTypeId())); } catch (Exception) { said2.Add("unit not named"); }
        said2.Add("rounding " + options.Accuracy.ToString("0.########", culture));
        try
        {
            var symbol = options.GetSymbolTypeId();
            said2.Add(symbol == null || symbol.Empty() ? "no unit symbol" : "symbol " + LabelUtils.GetLabelForSymbol(symbol));
        }
        catch (Exception) { said2.Add("symbol not read"); }
        said2.Add("+ sign " + (options.UsePlusPrefix ? "on" : "off"));
        said2.Add("digit grouping " + (options.UseDigitGrouping ? "on" : "off"));
        said2.Add("suppress trailing 0s " + (options.SuppressTrailingZeros ? "on" : "off"));
        said2.Add("suppress leading 0s " + (options.SuppressLeadingZeros ? "on" : "off"));
        said2.Add("suppress spaces " + (options.SuppressSpaces ? "on" : "off"));
        return string.Join(", ", said2);
    };
#endif

    foreach (var label in labels)
    {
        var text = textOf(label);
        var own = new List<string>();
        var matched = holds(text);

#if REVIT2027_OR_GREATER
        var annotation = label as AnnotationLabel;
        if (annotation != null)
        {
            IList<AnnotationLabelParameterFormatting> formatting = null;
            try { formatting = annotation.GetParameterFormatting(); }
            catch (Exception ex) { own.Add("label " + label.Id + ": Revit refused its parameters - " + ex.Message); }
            var position = 0;
            foreach (var each in formatting ?? new List<AnnotationLabelParameterFormatting>())
            {
                position++;
                InternalDefinition definition = null;
                try { definition = each.GetParameterDefinition(doc); } catch (Exception) { }
                var name = definition != null ? definition.Name : "(Revit named no parameter)";
                if (holds(name) || holds(each.SampleText) || holds(each.Prefix) || holds(each.Suffix)) matched = true;
                own.Add("label " + label.Id + " #" + position + ": " + name + " (" + kindOf(definition) + ")"
                    + " | sample \"" + each.SampleText + "\" | prefix \"" + each.Prefix + "\" | suffix \"" + each.Suffix + "\""
                    + " | spaces " + each.LeadingSpaces + " | break " + (each.LeadingLineBreak ? "yes - a new line before it" : "no")
                    + " | unit format: " + formatOf(label, definition));
            }
            if (formatting != null && formatting.Count > 0)
                own.Add("label " + label.Id + ": Wrap between parameters only " + (annotation.UseNonBreakingSpaces ? "ON" : "off"));
        }
        else own.Add("label " + label.Id + ": a text element Revit 2027 does not call a label - its parameters are not readable");
#endif

        if (!matched) continue;

        labelCount++;
        labelIds.Add(label.Id);
        parameterCount += own.Count(l => l.Contains(" #"));
        paramLines.AddRange(own);

        var type = doc.GetElement(label.GetTypeId());
        typeNames.Add(nameOf(type));
        try { foreach (var valid in label.GetValidTypes()) typeNames.Add(nameOf(doc.GetElement(valid))); } catch (Exception) { }

        var at = "";
        try { at = mm(label.Coord.X) + "," + mm(label.Coord.Y); } catch (Exception) { at = "not read"; }
        var width = "";
        try { width = mm(label.Width); } catch (Exception) { width = "not read"; }
        var angle = "";
        try { angle = Math.Round(Math.Atan2(label.BaseDirection.Y, label.BaseDirection.X) * 180.0 / Math.PI, 1).ToString("0.#", culture); }
        catch (Exception) { angle = "not read"; }
        var align = "";
        try { align = label.HorizontalAlignment + "/" + label.VerticalAlignment; } catch (Exception) { align = "not read"; }
        var readable = "";
        try { readable = label.KeepRotatedTextReadable ? "yes" : "no"; } catch (Exception) { readable = "not read"; }
        var visible = "always";
        try
        {
            var shown = label.get_Parameter(BuiltInParameter.IS_VISIBLE_PARAM);
            if (shown != null)
            {
                var link = doc.FamilyManager.GetAssociatedFamilyParameter(shown);
                if (link != null) visible = "linked to " + link.Definition.Name;
                else if (shown.AsInteger() == 0) visible = "off";
            }
        }
        catch (Exception) { visible = "not read"; }

        lines.Add("label " + label.Id + " | view " + nameOf(doc.GetElement(label.OwnerViewId)) + " | type " + nameOf(type)
            + " | at " + at + " mm | width " + width + " mm | " + align + " | angle " + angle + " deg | keep readable " + readable
            + " | visible " + visible + " | text \"" + text + "\"");

        var properties = new List<string>();
        foreach (Parameter p in label.Parameters)
        {
            string value;
            try { value = p.AsValueString() ?? p.AsString() ?? ""; } catch (Exception) { value = ""; }
            properties.Add(p.Definition.Name + "=" + value);
        }
        properties.Sort(StringComparer.OrdinalIgnoreCase);
        propertyLines.Add("label " + label.Id + ": " + string.Join(", ", properties));
    }

    labelList = string.Join("  ||  ", lines);
    parameterList = string.Join("  ||  ", paramLines);
    labelProperties = string.Join("  ||  ", propertyLines);
    labelTypes = string.Join(", ", typeNames);

    findings.Add(labelCount + " label(s) in \"" + doc.Title + "\" (" + (familyCategory == "" ? "category not read" : familyCategory)
        + ")" + (parts.Count > 0 ? " holding \"" + said + "\"" : "") + "; " + textElements + " text element(s) looked at, "
        + textNotes + " of them text notes, which are not labels.");
    if (labels.Count > 0 && labelCount == 0)
        findings.Add("No label holds \"" + said + "\" - " + labels.Count + " label(s) are in the family; ask with \"all\" to see them.");
    if (labels.Count == 0)
        findings.Add("This family has no labels. A label is added by hand (Create > Label) or copied from a seed family of the "
            + "same category - Revit's API cannot make one.");
    if (canReadParameters)
        findings.Add(parameterCount + " parameter(s) read from the labels listed, in each label's order. The unit format is what "
            + "Revit returns for that parameter on the label - check one against Edit Label before relying on it.");
    else if (labelCount > 0)
        findings.Add("Revit " + (release == "" ? "2026 or older" : release) + " cannot say which parameters a label shows, their "
            + "order, sample values, prefix, suffix, spaces, break or unit format - Revit 2027 is the first that can. Edit Label "
            + "shows them. The text listed is what Revit gives for each label.");
}
