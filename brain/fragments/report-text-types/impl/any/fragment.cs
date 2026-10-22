// NOT STANDALONE. Assumes `doc` and `textTypeName` are in scope; leaves
// `textTypes`, `typeCount`, `labelCount`, `textNoteCount`, `notTextTypes`
// and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// EVERY TEXT TYPE - the type of a LABEL in a tag, symbol or title block
// family, and the type of a TEXT NOTE in a project or a family - with the
// settings its Type Properties shows: Text Size, Text Font, Width Factor,
// Bold, Italic, Underline, Background, Color, Line Weight, Show Border,
// Leader/Border Offset, Tab Size and Leader Arrowhead, where the type has
// them. A label type has no Leader Arrowhead; a setting the type has not got
// is left out of its line, never printed as a guess.
//
// WHAT A LABEL IS, READ OFF THE SHIPPED ASSEMBLIES 2020 TO 2027
// (2026-10-21): a label is a TextElement that is not a TextNote - a plain
// TextElement 2020 to 2026, an AnnotationLabel (a TextElement too) from 2027.
// Its type is a TextElementType, reached the same way as a text note's -
// GetTypeId() - on every release. TextNoteType is the only class derived from
// TextElementType, so `is TextNoteType` is not used to tell a label type from
// a text type: Revit's own family name for the type (ElementType.FamilyName,
// as Type Properties prints it) and what uses it say which it is. Nothing
// here depends on the class Revit gives a label type.
//
// LENGTHS ARE PAPER MILLIMETRES, from Revit's internal feet by 304.8 (D-20).
// Text Size, Leader/Border Offset and Tab Size are sizes on the printed
// sheet, so no units API is called - nothing here moved at the 2021 units
// change or the 2022 spec change, and there is no #if.
//
// MODEL TEXT IS NOT TEXT HERE. A model text type (ModelTextType) is 3D text,
// ADD_FAMILY_MODEL_TEXT's; a name that is one is said so, never listed.
//
// EVERY LIST COMES BACK AS ONE STRING. A list is cut to three entries before
// a reader sees it, and a family with eight text types would read as three.

var findings = new List<string>();
var invariant = System.Globalization.CultureInfo.InvariantCulture;
const double MmPerFoot = 304.8;
var notTextTypes = new List<string>();

// The settings, in the order Type Properties lists them. Each is found by
// the first of its parameters the type carries; LINE_COLOR is what Revit's
// text types hold their Color in, TEXT_COLOR is asked only when it is absent.
var settings = new List<Tuple<string, BuiltInParameter[], string>>
{
    Tuple.Create("Color", new[] { BuiltInParameter.LINE_COLOR, BuiltInParameter.TEXT_COLOR }, "color"),
    Tuple.Create("Line Weight", new[] { BuiltInParameter.LINE_PEN }, "integer"),
    Tuple.Create("Background", new[] { BuiltInParameter.TEXT_BACKGROUND }, "background"),
    Tuple.Create("Show Border", new[] { BuiltInParameter.TEXT_BOX_VISIBILITY }, "yesno"),
    Tuple.Create("Leader/Border Offset", new[] { BuiltInParameter.LEADER_OFFSET_SHEET }, "mm"),
    Tuple.Create("Leader Arrowhead", new[] { BuiltInParameter.LEADER_ARROWHEAD }, "element"),
    Tuple.Create("Text Font", new[] { BuiltInParameter.TEXT_FONT }, "text"),
    Tuple.Create("Text Size", new[] { BuiltInParameter.TEXT_SIZE }, "mm"),
    Tuple.Create("Tab Size", new[] { BuiltInParameter.TEXT_TAB_SIZE }, "mm"),
    Tuple.Create("Bold", new[] { BuiltInParameter.TEXT_STYLE_BOLD }, "yesno"),
    Tuple.Create("Italic", new[] { BuiltInParameter.TEXT_STYLE_ITALIC }, "yesno"),
    Tuple.Create("Underline", new[] { BuiltInParameter.TEXT_STYLE_UNDERLINE }, "yesno"),
    Tuple.Create("Width Factor", new[] { BuiltInParameter.TEXT_WIDTH_SCALE }, "factor"),
};

Func<Element, BuiltInParameter[], Parameter> parameterOf = (e, which) =>
{
    foreach (var bip in which)
    {
        Parameter p = null;
        try { p = e.get_Parameter(bip); } catch { p = null; }
        if (p != null) return p;
    }
    return null;
};

// One setting as Type Properties would show it. A value Revit holds in a
// storage this does not expect is printed as Revit's own text.
Func<Parameter, string, string> shown = (p, kind) =>
{
    try
    {
        if (kind == "mm" && p.StorageType == StorageType.Double)
            return Math.Round(p.AsDouble() * MmPerFoot, 3).ToString(invariant) + " mm";
        if (kind == "factor" && p.StorageType == StorageType.Double)
            return Math.Round(p.AsDouble(), 4).ToString(invariant);
        if (kind == "yesno" && p.StorageType == StorageType.Integer)
            return p.AsInteger() != 0 ? "yes" : "no";
        if (kind == "integer" && p.StorageType == StorageType.Integer)
            return p.AsInteger().ToString(invariant);
        if (kind == "background" && p.StorageType == StorageType.Integer)
        {
            var revitSays = p.AsValueString();
            return !string.IsNullOrWhiteSpace(revitSays) ? revitSays
                : (p.AsInteger() == 1 ? "Transparent" : "Opaque");
        }
        if (kind == "color" && p.StorageType == StorageType.Integer)
        {
            var c = p.AsInteger();
            return string.Format(invariant, "{0},{1},{2}", c & 0xFF, (c >> 8) & 0xFF, (c >> 16) & 0xFF);
        }
        if (kind == "text" && p.StorageType == StorageType.String)
            return p.AsString() ?? "";
        if (kind == "element" && p.StorageType == StorageType.ElementId)
        {
            var target = doc.GetElement(p.AsElementId());
            return target == null ? "None" : target.Name;
        }
        return p.AsValueString() ?? "";
    }
    catch { return ""; }
};

// Every text and label type, and what uses each. Text notes are found in
// the whole document; labels exist only inside a family.
var textElementTypes = new FilteredElementCollector(doc).WhereElementIsElementType()
    .OfType<TextElementType>().OrderBy(t => t.FamilyName ?? "").ThenBy(t => t.Name ?? "").ToList();

var notes = new FilteredElementCollector(doc).OfClass(typeof(TextNote)).ToElements();
var labels = doc.IsFamilyDocument
    ? new FilteredElementCollector(doc).WhereElementIsNotElementType()
        .OfType<TextElement>().Where(e => !(e is TextNote)).Cast<Element>().ToList()
    : new List<Element>();
var labelCount = labels.Count;
var textNoteCount = notes.Count;

Func<ElementType, string> fullName = t =>
    string.IsNullOrWhiteSpace(t.FamilyName) ? t.Name : t.FamilyName + ": " + t.Name;

// "all" or nothing - every type. "labels" - the types the labels in this
// family use. Anything else - a type by its name, or "Family: Type" where a
// label type and a text type share a name.
var asked = (textTypeName ?? "").Trim();
var wantAll = asked.Length == 0 || string.Equals(asked, "all", StringComparison.OrdinalIgnoreCase);
var wantLabels = string.Equals(asked, "labels", StringComparison.OrdinalIgnoreCase)
    || string.Equals(asked, "label", StringComparison.OrdinalIgnoreCase);

var chosen = new List<TextElementType>();
if (wantAll) chosen.AddRange(textElementTypes);
else if (wantLabels)
{
    if (!doc.IsFamilyDocument)
        findings.Add("This is a project, and labels live in tag, symbol and title block families - open the "
            + "family (Edit Family) and ask there. The text types here are listed instead.");
    var used = new HashSet<ElementId>(labels.Select(e => e.GetTypeId()));
    chosen.AddRange(textElementTypes.Where(t => used.Contains(t.Id)));
    if (doc.IsFamilyDocument && labels.Count == 0)
        findings.Add("This family has no labels. Its text types are listed instead.");
    if (chosen.Count == 0) chosen.AddRange(textElementTypes);
}
else
{
    chosen.AddRange(textElementTypes.Where(t =>
        string.Equals(t.Name, asked, StringComparison.OrdinalIgnoreCase)
        || string.Equals(fullName(t), asked, StringComparison.OrdinalIgnoreCase)));
    if (chosen.Count == 0)
    {
        // Is it a type of something else that also carries a text size?
        foreach (var other in new FilteredElementCollector(doc).WhereElementIsElementType().ToElements())
        {
            var type = other as ElementType;
            if (type == null || type is TextElementType) continue;
            if (!string.Equals(type.Name, asked, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fullName(type), asked, StringComparison.OrdinalIgnoreCase)) continue;
            if (type is ModelTextType)
                notTextTypes.Add("\"" + fullName(type) + "\" is a MODEL TEXT type - 3D text in the model, "
                    + "not a label or a text note. ADD_FAMILY_MODEL_TEXT makes and sizes model text");
            else if (parameterOf(type, new[] { BuiltInParameter.TEXT_SIZE }) != null)
                notTextTypes.Add("\"" + fullName(type) + "\" is not a label or text type - it is a "
                    + (string.IsNullOrWhiteSpace(type.FamilyName) ? "type" : type.FamilyName)
                    + " whose text settings are its own");
        }
        findings.Add("No label or text type is named \"" + asked + "\". "
            + (notTextTypes.Count > 0 ? string.Join(". ", notTextTypes) + ". " : "")
            + "The label and text types here are: "
            + (textElementTypes.Count == 0 ? "none" : string.Join(", ", textElementTypes.Select(fullName))) + ".");
    }
}

var lines = new List<string>();
foreach (var type in chosen)
{
    var parts = new List<string> { fullName(type) };
    foreach (var setting in settings)
    {
        var p = parameterOf(type, setting.Item2);
        if (p == null) continue;
        parts.Add(setting.Item1 + " " + shown(p, setting.Item3));
    }
    var byLabels = labels.Count(e => e.GetTypeId() == type.Id);
    var byNotes = notes.Count(e => e.GetTypeId() == type.Id);
    parts.Add("used by " + byLabels + " label(s) and " + byNotes + " text note(s)");
    lines.Add(string.Join(" | ", parts));
}

var typeCount = chosen.Count;
var textTypes = string.Join("  ||  ", lines);
findings.Add(typeCount + " text type(s) listed of " + textElementTypes.Count + " in "
    + (doc.IsFamilyDocument ? "this family" : "this project") + "; " + labelCount + " label(s) and "
    + textNoteCount + " text note(s) in it. Lengths are paper millimetres, as printed.");
