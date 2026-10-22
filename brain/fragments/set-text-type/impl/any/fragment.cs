// NOT STANDALONE. Assumes `doc`, `textTypeName`, `changes`, `duplicateAs`
// and `switchTo` are in scope; leaves `changedTypeId`, `typeAfter`,
// `switched`, `typeBefore`, `usedBy`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// CHANGES A TEXT TYPE - the type of a LABEL in a tag, symbol or title block
// family, or of a TEXT NOTE in a project or a family - as Type Properties
// does:
//
//   textTypeName  the type, by name - "2.5mm Arial" - or "Family: Type" where
//                 a label type and a text type share a name. Left out,
//                 "labels": the ONE type this family's labels use; labels on
//                 two or more types are refused, naming them
//   changes       "Text Size=5; Bold=yes; Text Font=Arial Narrow" - each
//                 setting as Type Properties names it, a value after "=",
//                 ";" or a new line between. Sizes are PAPER millimetres
//   duplicateAs   a new name: the type is copied first and the COPY changed
//                 - what "make a 2.5 mm text type" means. Left out, the type
//                 itself is changed
//   switchTo      with duplicateAs: "labels", "text notes" or "all" - those
//                 on the original type move onto the copy. Left out, "none"
//
// The settings: Text Size, Text Font, Width Factor, Bold, Italic, Underline,
// Background (opaque / transparent), Color (R,G,B), Line Weight (1-16), Show
// Border, Leader/Border Offset, Tab Size and Leader Arrowhead (an arrowhead
// type by name, or none) - where the type has them. A label type has no Leader
// Arrowhead, and asking for one is refused naming what it has.
//
// WHAT A LABEL IS, read off the shipped RevitAPI.dll 2020 to 2027: a
// TextElement that is not a TextNote - AnnotationLabel from 2027 - whose type
// is a TextElementType reached by GetTypeId(), the way a text note's is.
// REPORT_TEXT_TYPES' comment has the rest.
//
// EVERYTHING IS CHECKED BEFORE ANYTHING IS WRITTEN: the type found, every
// setting known to it and writable, every value in range, the arrowhead found,
// the new name free. Then one regeneration and every value READ BACK; a value
// that reads back other than asked, a label or note that did not take the
// copy, or a refusal from Revit throws - so the copy, the changes and the
// switch are taken back together. Nothing half-done is ever kept.
//
// LENGTHS ARE PAPER MILLIMETRES, into Revit's internal feet by 304.8 (D-20).
// No units API is called, so neither the 2021 units change nor the 2022 spec
// change reaches this code, and there is no #if.
//
// A TYPE CHANGE REACHES EVERYTHING OF THAT TYPE. `usedBy` counts what changed
// with it; in a family, the tags in a project change only when the family is
// loaded back into it.

const double MmPerFoot = 304.8;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
var findings = new List<string>();
var changedTypeId = ElementId.InvalidElementId;
var typeBefore = "";
var typeAfter = "";
var usedBy = "";
var switched = 0;
string refused = null;

// Name, the parameters that hold it (the first the type carries), the kind
// of value, and the other words a modeller says for it.
var settings = new List<Tuple<string, BuiltInParameter[], string, string[]>>
{
    Tuple.Create("Color", new[] { BuiltInParameter.LINE_COLOR, BuiltInParameter.TEXT_COLOR }, "color", new[] { "colour", "text color", "text colour" }),
    Tuple.Create("Line Weight", new[] { BuiltInParameter.LINE_PEN }, "integer", new[] { "lineweight", "pen" }),
    Tuple.Create("Background", new[] { BuiltInParameter.TEXT_BACKGROUND }, "background", new string[0]),
    Tuple.Create("Show Border", new[] { BuiltInParameter.TEXT_BOX_VISIBILITY }, "yesno", new[] { "border", "box" }),
    Tuple.Create("Leader/Border Offset", new[] { BuiltInParameter.LEADER_OFFSET_SHEET }, "mm", new[] { "leader offset", "border offset", "offset" }),
    Tuple.Create("Leader Arrowhead", new[] { BuiltInParameter.LEADER_ARROWHEAD }, "element", new[] { "arrowhead", "arrow" }),
    Tuple.Create("Text Font", new[] { BuiltInParameter.TEXT_FONT }, "text", new[] { "font" }),
    Tuple.Create("Text Size", new[] { BuiltInParameter.TEXT_SIZE }, "mm", new[] { "size", "height", "text height" }),
    Tuple.Create("Tab Size", new[] { BuiltInParameter.TEXT_TAB_SIZE }, "mm", new[] { "tab" }),
    Tuple.Create("Bold", new[] { BuiltInParameter.TEXT_STYLE_BOLD }, "yesno", new string[0]),
    Tuple.Create("Italic", new[] { BuiltInParameter.TEXT_STYLE_ITALIC }, "yesno", new string[0]),
    Tuple.Create("Underline", new[] { BuiltInParameter.TEXT_STYLE_UNDERLINE }, "yesno", new string[0]),
    Tuple.Create("Width Factor", new[] { BuiltInParameter.TEXT_WIDTH_SCALE }, "factor", new[] { "width scale", "width" }),
};

Func<string, string> squash = s => new string((s ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

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

Func<ElementType, string> fullName = t =>
    string.IsNullOrWhiteSpace(t.FamilyName) ? t.Name : t.FamilyName + ": " + t.Name;

// The storage each kind is written in. A type holding a setting another way
// is refused for that setting rather than written a way Revit did not mean.
Func<string, StorageType> storageFor = kind =>
    kind == "mm" || kind == "factor" ? StorageType.Double
    : kind == "text" ? StorageType.String
    : kind == "element" ? StorageType.ElementId
    : StorageType.Integer;

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

Func<ElementType, string> describe = t =>
{
    var parts = new List<string> { fullName(t) };
    foreach (var s in settings)
    {
        var p = parameterOf(t, s.Item2);
        if (p != null) parts.Add(s.Item1 + " " + shown(p, s.Item3));
    }
    return string.Join(" | ", parts);
};

// ---- What there is ---------------------------------------------------------

var textElementTypes = new FilteredElementCollector(doc).WhereElementIsElementType()
    .OfType<TextElementType>().OrderBy(t => t.FamilyName ?? "").ThenBy(t => t.Name ?? "").ToList();
var notes = new FilteredElementCollector(doc).OfClass(typeof(TextNote)).ToElements();
var labels = doc.IsFamilyDocument
    ? new FilteredElementCollector(doc).WhereElementIsNotElementType()
        .OfType<TextElement>().Where(e => !(e is TextNote)).Cast<Element>().ToList()
    : new List<Element>();
var typeList = textElementTypes.Count == 0 ? "none" : string.Join(", ", textElementTypes.Select(fullName));

// ---- The type --------------------------------------------------------------

TextElementType start = null;
var asked = (textTypeName ?? "").Trim();
if (asked.Length == 0 || string.Equals(asked, "labels", StringComparison.OrdinalIgnoreCase)
    || string.Equals(asked, "label", StringComparison.OrdinalIgnoreCase))
{
    if (!doc.IsFamilyDocument)
        refused = "This is a project, and labels live in tag, symbol and title block families - open the family "
            + "(Edit Family) and ask there, or name the text type to change. The text types here are: " + typeList + ".";
    else if (labels.Count == 0)
        refused = "This family has no labels - name the text type to change. Its text types are: " + typeList + ".";
    else
    {
        var used = labels.Select(e => e.GetTypeId()).Distinct().ToList();
        var usedTypes = textElementTypes.Where(t => used.Contains(t.Id)).ToList();
        if (usedTypes.Count == 1) start = usedTypes[0];
        else if (usedTypes.Count == 0)
            refused = "The labels in this family are on a type Revit does not hand back as a text type, so it cannot "
                + "be changed here - name a text type to change instead. The text types here are: " + typeList + ".";
        else
            refused = "The " + labels.Count + " labels in this family use " + used.Count + " types: "
                + string.Join(", ", usedTypes.Select(t => "\"" + fullName(t) + "\" ("
                    + labels.Count(e => e.GetTypeId() == t.Id) + ")"))
                + ". Name the one to change.";
    }
}
else
{
    var matches = textElementTypes.Where(t =>
        string.Equals(t.Name, asked, StringComparison.OrdinalIgnoreCase)
        || string.Equals(fullName(t), asked, StringComparison.OrdinalIgnoreCase)).ToList();
    if (matches.Count == 1) start = matches[0];
    else if (matches.Count > 1)
        refused = "\"" + asked + "\" names " + matches.Count + " types: "
            + string.Join(", ", matches.Select(t => "\"" + fullName(t) + "\""))
            + ". Name one as \"Family: Type\".";
    else
    {
        var why = "";
        foreach (var other in new FilteredElementCollector(doc).WhereElementIsElementType().ToElements())
        {
            var type = other as ElementType;
            if (type == null || type is TextElementType) continue;
            if (!string.Equals(type.Name, asked, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fullName(type), asked, StringComparison.OrdinalIgnoreCase)) continue;
            if (type is ModelTextType)
                why = " \"" + fullName(type) + "\" is a MODEL TEXT type - 3D text in the model, not a label or a "
                    + "text note; ADD_FAMILY_MODEL_TEXT makes and sizes model text.";
            else if (why == "" && parameterOf(type, new[] { BuiltInParameter.TEXT_SIZE }) != null)
                why = " \"" + fullName(type) + "\" is a " + (string.IsNullOrWhiteSpace(type.FamilyName) ? "type" : type.FamilyName)
                    + ", not a label or text type.";
        }
        refused = "No label or text type is named \"" + asked + "\"." + why + " The label and text types here are: "
            + typeList + ".";
    }
}

// ---- What to change --------------------------------------------------------

// setting, value to write, the words given
var wanted = new List<Tuple<Tuple<string, BuiltInParameter[], string, string[]>, object, string>>();
var said = (changes ?? "").Trim();
if (refused == null && said.Length > 0 && !string.Equals(said, "none", StringComparison.OrdinalIgnoreCase))
{
    foreach (var raw in said.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var piece = raw.Trim();
        if (piece.Length == 0) continue;
        var eq = piece.IndexOf('=');
        if (eq <= 0) { refused = "\"" + piece + "\" is not a setting=value pair - e.g. Text Size=5; Bold=yes."; break; }
        var name = piece.Substring(0, eq).Trim();
        var value = piece.Substring(eq + 1).Trim();
        var setting = settings.FirstOrDefault(s => squash(s.Item1) == squash(name)
            || s.Item4.Any(a => squash(a) == squash(name)));
        if (setting == null)
        {
            refused = "\"" + name + "\" is not a text type setting. They are: "
                + string.Join(", ", settings.Select(s => s.Item1)) + ".";
            break;
        }
        if (wanted.Any(w => w.Item1 == setting)) { refused = setting.Item1 + " is asked twice."; break; }

        object parsed = null;
        string problem = null;
        var kind = setting.Item3;
        var lower = value.ToLowerInvariant();
        if (kind == "mm" || kind == "factor")
        {
            var number = lower.EndsWith("mm") && kind == "mm" ? lower.Substring(0, lower.Length - 2).Trim() : lower;
            double d;
            if (value.Contains("\"") || value.Contains("'") || lower.EndsWith("in") || lower.EndsWith("ft"))
                problem = "give it in millimetres - 5 for 5 mm";
            else if (!double.TryParse(number, System.Globalization.NumberStyles.Float, invariant, out d))
                problem = "is not a number";
            else if (setting.Item1 == "Leader/Border Offset" ? d < 0 : d <= 0)
                problem = setting.Item1 == "Leader/Border Offset" ? "cannot be below zero" : "must be above zero";
            else parsed = kind == "mm" ? d / MmPerFoot : d;
        }
        else if (kind == "yesno")
        {
            if (new[] { "yes", "true", "on", "1" }.Contains(lower)) parsed = 1;
            else if (new[] { "no", "false", "off", "0" }.Contains(lower)) parsed = 0;
            else problem = "is not yes or no";
        }
        else if (kind == "integer")
        {
            int n;
            if (!int.TryParse(lower, System.Globalization.NumberStyles.Integer, invariant, out n) || n < 1 || n > 16)
                problem = "is not a line weight from 1 to 16";
            else parsed = n;
        }
        else if (kind == "background")
        {
            if (lower == "opaque") parsed = 0;
            else if (lower == "transparent") parsed = 1;
            else problem = "is not opaque or transparent";
        }
        else if (kind == "color")
        {
            int[] rgb = null;
            if (lower == "black") rgb = new[] { 0, 0, 0 };
            else if (lower == "white") rgb = new[] { 255, 255, 255 };
            else if (lower.StartsWith("#") && lower.Length == 7)
            {
                try { rgb = new[] { Convert.ToInt32(lower.Substring(1, 2), 16), Convert.ToInt32(lower.Substring(3, 2), 16), Convert.ToInt32(lower.Substring(5, 2), 16) }; }
                catch { rgb = null; }
            }
            else
            {
                var bits = lower.Split(new[] { ',', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
                int r, g, b;
                if (bits.Length == 3 && int.TryParse(bits[0], out r) && int.TryParse(bits[1], out g) && int.TryParse(bits[2], out b))
                    rgb = new[] { r, g, b };
            }
            if (rgb == null || rgb.Any(c => c < 0 || c > 255)) problem = "is not a colour - give R,G,B from 0 to 255, #RRGGBB, black or white";
            else parsed = rgb[0] + (rgb[1] << 8) + (rgb[2] << 16);
        }
        else if (kind == "text")
        {
            if (value.Length == 0) problem = "is empty";
            else parsed = value;
        }
        else if (kind == "element")
        {
            var arrowheads = new FilteredElementCollector(doc).WhereElementIsElementType().ToElements()
                .Where(e => parameterOf(e, new[] { BuiltInParameter.ARROW_TYPE }) != null).ToList();
            if (lower == "none") parsed = ElementId.InvalidElementId;
            else
            {
                var arrow = arrowheads.FirstOrDefault(e => string.Equals(e.Name, value, StringComparison.OrdinalIgnoreCase));
                if (arrow == null)
                    problem = "is not an arrowhead here - they are: "
                        + (arrowheads.Count == 0 ? "none" : string.Join(", ", arrowheads.Select(e => e.Name))) + ", or none";
                else parsed = arrow.Id;
            }
        }
        if (problem != null) { refused = setting.Item1 + " \"" + value + "\" " + problem + "."; break; }
        wanted.Add(Tuple.Create(setting, parsed, value));
    }
}

// ---- The copy and the switch -----------------------------------------------

var newName = (duplicateAs ?? "").Trim();
if (string.Equals(newName, "none", StringComparison.OrdinalIgnoreCase)) newName = "";
var move = (switchTo ?? "").Trim().ToLowerInvariant();
var moveLabels = move == "labels" || move == "label" || move == "all" || move == "both";
var moveNotes = move == "text notes" || move == "text note" || move == "notes" || move == "textnotes"
    || move == "all" || move == "both";

if (refused == null && move != "" && move != "none" && !moveLabels && !moveNotes)
    refused = "switchTo \"" + switchTo + "\" is not labels, text notes, all or none.";
if (refused == null && (moveLabels || moveNotes) && newName == "")
    refused = "switchTo moves labels or text notes onto a COPY, and no duplicateAs was given - they are already on \""
        + fullName(start) + "\". To put some on another type that exists, select them and change their type "
        + "(CHANGE_ELEMENT_TYPE).";
if (refused == null && moveLabels && !moveNotes && !doc.IsFamilyDocument)
    refused = "A project has no labels to move - they live in tag, symbol and title block families.";
if (refused == null && newName != "" && textElementTypes.Any(t =>
        string.Equals(t.Name, newName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(t.FamilyName ?? "", start.FamilyName ?? "", StringComparison.OrdinalIgnoreCase)))
    refused = "A " + (string.IsNullOrWhiteSpace(start.FamilyName) ? "text" : start.FamilyName) + " type is already named \""
        + newName + "\" - give another name, or change that type by its name.";
if (refused == null && wanted.Count == 0 && newName == "")
    refused = "Nothing was asked - give a setting to change (Text Size=5; Bold=yes) or a duplicateAs name.";

// Every setting asked must be one this type has, writable, held as expected.
if (refused == null)
{
    var missing = new List<string>();
    foreach (var w in wanted)
    {
        var p = parameterOf(start, w.Item1.Item2);
        if (p == null) missing.Add(w.Item1.Item1 + " - this type has none");
        else if (p.IsReadOnly) missing.Add(w.Item1.Item1 + " - read-only on this type");
        else if (p.StorageType != storageFor(w.Item1.Item3)) missing.Add(w.Item1.Item1 + " - held another way here");
    }
    if (missing.Count > 0)
        refused = "\"" + fullName(start) + "\" cannot take: " + string.Join("; ", missing) + ". Its settings are: "
            + string.Join(", ", settings.Where(s => parameterOf(start, s.Item2) != null).Select(s => s.Item1)) + ".";
}

// ---- The change ------------------------------------------------------------

if (refused == null)
{
    typeBefore = describe(start);
    var onStartLabels = labels.Where(e => e.GetTypeId() == start.Id).ToList();
    var onStartNotes = notes.Where(e => e.GetTypeId() == start.Id).ToList();

    TextElementType target = start;
    if (newName != "")
    {
        try { target = start.Duplicate(newName) as TextElementType; }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Nothing was kept. Revit would not copy \"" + fullName(start) + "\" as \""
                + newName + "\": " + ex.Message);
        }
        if (target == null)
            throw new InvalidOperationException("Nothing was kept. Copying \"" + fullName(start) + "\" gave no text type.");
    }

    var notTaken = new List<string>();
    foreach (var w in wanted)
    {
        var p = parameterOf(target, w.Item1.Item2);
        bool ok;
        try
        {
            if (w.Item2 is double) ok = p.Set((double)w.Item2);
            else if (w.Item2 is int) ok = p.Set((int)w.Item2);
            else if (w.Item2 is ElementId) ok = p.Set((ElementId)w.Item2);
            else ok = p.Set((string)w.Item2);
        }
        catch (Exception ex) { ok = false; notTaken.Add(w.Item1.Item1 + " \"" + w.Item3 + "\": " + ex.Message); continue; }
        if (!ok) notTaken.Add(w.Item1.Item1 + " \"" + w.Item3 + "\": Revit did not take it");
    }
    if (notTaken.Count > 0)
        throw new InvalidOperationException("Nothing was kept. Revit refused " + string.Join("; ", notTaken) + ".");

    doc.Regenerate();

    // READ BACK - the type as Revit now holds it, against what was asked.
    var wrong = new List<string>();
    foreach (var w in wanted)
    {
        var p = parameterOf(target, w.Item1.Item2);
        var kind = w.Item1.Item3;
        bool same;
        if (w.Item2 is double) same = Math.Abs(p.AsDouble() - (double)w.Item2) < (kind == "mm" ? 0.001 / MmPerFoot : 1e-6);
        else if (w.Item2 is int) same = p.AsInteger() == (int)w.Item2;
        else if (w.Item2 is ElementId) same = p.AsElementId() == (ElementId)w.Item2;
        else same = string.Equals(p.AsString() ?? "", (string)w.Item2, StringComparison.Ordinal);
        // Background is also asked of Revit's own word for it, so a wrong
        // reading of which number is which cannot pass as kept.
        if (same && kind == "background")
        {
            var word = (p.AsValueString() ?? "").ToLowerInvariant();
            var other = (int)w.Item2 == 1 ? "opaque" : "transparent";
            if (word.Contains(other)) same = false;
        }
        if (!same) wrong.Add(w.Item1.Item1 + " reads " + shown(p, kind) + ", asked " + w.Item3);
    }
    if (wrong.Count > 0)
        throw new InvalidOperationException("Nothing was kept. NOT AS ASKED after the change: " + string.Join("; ", wrong) + ".");

    // The switch: what stood on the original moves onto the copy, all or none.
    if (target.Id != start.Id && (moveLabels || moveNotes))
    {
        var toMove = new List<Element>();
        if (moveLabels) toMove.AddRange(onStartLabels);
        if (moveNotes) toMove.AddRange(onStartNotes);
        var stuck = new List<string>();
        foreach (var e in toMove)
        {
            try
            {
                if (!e.IsValidType(target.Id)) { stuck.Add(e.Id + " (Revit says that type is not valid for it)"); continue; }
                e.ChangeTypeId(target.Id);
            }
            catch (Exception ex) { stuck.Add(e.Id + " (" + ex.Message + ")"); }
        }
        doc.Regenerate();
        foreach (var e in toMove)
        {
            var back = doc.GetElement(e.Id);
            if (back != null && back.GetTypeId() == target.Id) switched++;
            else if (!stuck.Any(s => s.StartsWith(e.Id + " "))) stuck.Add(e.Id + " (reads back on its old type)");
        }
        if (stuck.Count > 0)
            throw new InvalidOperationException("Nothing was kept. " + stuck.Count + " of " + toMove.Count
                + " did not move onto \"" + fullName(target) + "\": " + string.Join(", ", stuck.Take(10)) + ".");
        if (toMove.Count == 0)
            findings.Add("Nothing " + (moveLabels && moveNotes ? "" : moveLabels ? "among the labels " : "among the text notes ")
                + "stood on \"" + fullName(start) + "\", so nothing moved onto the copy.");
    }

    changedTypeId = target.Id;
    typeAfter = describe(target);
    var labelsOn = labels.Count(e => doc.GetElement(e.Id) != null && doc.GetElement(e.Id).GetTypeId() == target.Id);
    var notesOn = notes.Count(e => doc.GetElement(e.Id) != null && doc.GetElement(e.Id).GetTypeId() == target.Id);
    usedBy = labelsOn + " label(s) and " + notesOn + " text note(s) are on \"" + fullName(target) + "\"";

    findings.Add((target.Id != start.Id ? "Copied \"" + fullName(start) + "\" as \"" + fullName(target) + "\"" : "Changed \"" + fullName(target) + "\"")
        + (wanted.Count > 0 ? ": " + string.Join(", ", wanted.Select(w => w.Item1.Item1 + " " + w.Item3)) : " with nothing changed on it")
        + " - read back as asked.");
    if (switched > 0) findings.Add(switched + " moved onto the copy.");
    findings.Add(usedBy + (target.Id == start.Id ? " and changed with it." : "."));
    if (wanted.Any(w => w.Item1.Item1 == "Text Font"))
        findings.Add("The font must be installed on every PC that opens this file - Heron does not check that.");
    if (doc.IsFamilyDocument)
        findings.Add("This is the family. Tags already in a project change only when it is loaded back into that project.");
}

if (refused != null)
{
    refused = refused.TrimEnd() + " NOTHING WAS CHANGED.";
    findings.Add(refused);
}
