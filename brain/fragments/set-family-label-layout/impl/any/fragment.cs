// NOT STANDALONE. Assumes `doc`, `labelIds`, `positionMm`, `moveByMm`,
// `widthMm`, `horizontalAlign`, `verticalAlign` and `keepReadable` are in
// scope; leaves `changed`, `changedIds`, `readBack`, `report` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// WHERE A LABEL SITS AND HOW IT IS LAID OUT - the part of a label Revit's API
// lets a program change, on every release 2020 to 2027: its point, its width,
// its horizontal and vertical alignment, and Keep Readable. Read off all eight
// releases' shipped assemblies on 2026-10-21 (docs/43 section 8.3a).
//
// WHAT IT SHOWS IS NOT CHANGED, ON ANY RELEASE, AND IT SAYS SO. Which
// parameters a label shows, their order, sample value, prefix, suffix,
// spaces, break and unit format have no public setter on any release - 2027's
// SetParameterFormatting is internal - so they are the Edit Label dialog's.
// The label's Text is never set: setting a text element's Text replaces its
// content, and on a label that is not a change anybody asked for.
//
// THE LABEL TYPE IS CHANGE_ELEMENT_TYPE'S, already built for every element.
//
//   labelIds         ids, comma separated, or "all" - every label in the family
//   positionMm       "x,y" in mm: where ONE label's point goes
//   moveByMm         "dx,dy" in mm: moves every label named by that much
//   widthMm          the label box's width in mm, inside what Revit allows
//   horizontalAlign  left, center or right
//   verticalAlign    top, middle or bottom
//   keepReadable     yes or no
//
// "keep", or nothing, leaves that setting as it is.
//
// A LABEL IS FOUND BY WHAT IT IS - a text element that is not a text note, or
// on 2027 an AnnotationLabel - the rule REPORT_FAMILY_LABELS reads by. A text
// note's id is refused by name: it is not a label.
//
// READ BACK: after Revit regenerates, each label's point, width, alignment and
// Keep Readable are read again, and only a label that reads as asked counts.

const double MmPerFoot = 304.8;
var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;
var changed = 0;
var changedIds = new List<ElementId>();
var readBack = "";
var findings = new List<string>();
var lines = new List<string>();
string refusal = null;

Func<string, string> asked = text =>
{
    var t = (text ?? "").Trim();
    return string.Equals(t, "keep", StringComparison.OrdinalIgnoreCase) ? "" : t;
};
Func<string, double[]> pairOf = text =>
{
    var bits = text.Split(',');
    double a, b;
    if (bits.Length == 2 && double.TryParse(bits[0].Trim(), numberStyle, culture, out a)
        && double.TryParse(bits[1].Trim(), numberStyle, culture, out b)) return new[] { a, b };
    return null;
};
Func<double, string> mm = feet => Math.Round(feet * MmPerFoot, 1).ToString("0.#", culture);
Func<Element, bool> isLabel = element => element is TextElement && !(element is TextNote);

var place = asked(positionMm);
var move = asked(moveByMm);
var width = asked(widthMm);
var horizontal = asked(horizontalAlign).ToLowerInvariant();
var vertical = asked(verticalAlign).ToLowerInvariant();
var readable = asked(keepReadable).ToLowerInvariant();

double[] placeAt = null, moveBy = null;
double widthWanted = 0;
HorizontalTextAlignment? horizontalWanted = null;
VerticalTextAlignment? verticalWanted = null;
bool? readableWanted = null;

if (!doc.IsFamilyDocument)
    refusal = "\"" + doc.Title + "\" is a project, not a family. Labels live in a tag, annotation or title block family "
        + "open in the Family Editor. NOTHING WAS CHANGED.";
else if (place == "" && move == "" && width == "" && horizontal == "" && vertical == "" && readable == "")
    refusal = "Nothing to change was given - a position, a move, a width, an alignment or Keep Readable. Which "
        + "parameters a label shows, its sample value, prefix, suffix, spaces, break and unit format are changed in "
        + "Edit Label by hand: Revit's API cannot change them on any release. NOTHING WAS CHANGED.";
else if (place != "" && move != "")
    refusal = "Give a position or a move, not both. NOTHING WAS CHANGED.";
else if (place != "" && (placeAt = pairOf(place)) == null)
    refusal = "A position is x,y in millimetres, such as 0,5. NOTHING WAS CHANGED.";
else if (move != "" && (moveBy = pairOf(move)) == null)
    refusal = "A move is dx,dy in millimetres, such as 0,2.5. NOTHING WAS CHANGED.";
else if (width != "" && (!double.TryParse(width, numberStyle, culture, out widthWanted) || widthWanted <= 0))
    refusal = "A width is a number of millimetres above 0. NOTHING WAS CHANGED.";
else if (horizontal != "" && horizontal != "left" && horizontal != "center" && horizontal != "centre" && horizontal != "right")
    refusal = "Horizontal alignment is left, center or right. NOTHING WAS CHANGED.";
else if (vertical != "" && vertical != "top" && vertical != "middle" && vertical != "bottom")
    refusal = "Vertical alignment is top, middle or bottom. NOTHING WAS CHANGED.";
else if (readable != "" && readable != "yes" && readable != "no" && readable != "true" && readable != "false")
    refusal = "Keep Readable is yes or no. NOTHING WAS CHANGED.";

if (refusal == null)
{
    if (horizontal != "") horizontalWanted = horizontal == "left" ? HorizontalTextAlignment.Left
        : horizontal == "right" ? HorizontalTextAlignment.Right : HorizontalTextAlignment.Center;
    if (vertical != "") verticalWanted = vertical == "top" ? VerticalTextAlignment.Top
        : vertical == "bottom" ? VerticalTextAlignment.Bottom : VerticalTextAlignment.Middle;
    if (readable != "") readableWanted = readable == "yes" || readable == "true";
}

// The labels named - "all", or ids. A text note, a missing id or anything else
// is refused by name, before anything changes.
var picked = new List<TextElement>();
if (refusal == null)
{
    var idText = (labelIds ?? "").Trim();
    if (idText == "") refusal = "No label was named - give label ids (REPORT_FAMILY_LABELS lists them) or \"all\". NOTHING WAS CHANGED.";
    else if (string.Equals(idText, "all", StringComparison.OrdinalIgnoreCase))
    {
        foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
            if (isLabel(element)) picked.Add((TextElement)element);
        if (picked.Count == 0) refusal = "This family has no labels. NOTHING WAS CHANGED.";
    }
    else
    {
        // Matched as text against each element's id or UniqueId - an ElementId
        // is never built from a number (D-20); a family holds few elements.
        var byText = new Dictionary<string, Element>();
        foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
        {
            byText[element.Id.ToString()] = element;
            byText[element.UniqueId] = element;
        }
        var wrong = new List<string>();
        foreach (var part in idText.Split(',').Select(t => t.Trim()).Where(t => t != ""))
        {
            Element element;
            byText.TryGetValue(part, out element);
            if (element == null) wrong.Add(part + " is not in this family");
            else if (element is TextNote) wrong.Add(part + " is a text note, not a label");
            else if (!isLabel(element)) wrong.Add(part + " is " + (element.Category != null ? element.Category.Name : element.GetType().Name) + ", not a label");
            else if (!picked.Any(p => p.Id == element.Id)) picked.Add((TextElement)element);
        }
        if (wrong.Count > 0) refusal = string.Join("; ", wrong) + ". NOTHING WAS CHANGED.";
        else if (picked.Count == 0) refusal = "No label was named. NOTHING WAS CHANGED.";
    }
}
if (refusal == null && placeAt != null && picked.Count != 1)
    refusal = "A position puts ONE label at a point, and " + picked.Count + " were named - name one, or move them all by "
        + "moveByMm. NOTHING WAS CHANGED.";
if (refusal == null && width != "")
{
    var wantFeet = widthWanted / MmPerFoot;
    foreach (var label in picked)
    {
        double low = 0, high = double.MaxValue;
        try { low = label.GetMinimumAllowedWidth(); high = label.GetMaximumAllowedWidth(); } catch (Exception) { }
        if (wantFeet < low - 1e-9 || wantFeet > high + 1e-9)
        {
            refusal = "Label " + label.Id + " takes a width from " + mm(low) + " to " + mm(high) + " mm, and " + width
                + " mm was asked. NOTHING WAS CHANGED.";
            break;
        }
    }
}

if (refusal == null)
{
    foreach (var label in picked)
    {
        var problems = new List<string>();
        try
        {
            var from = label.Coord;
            XYZ shift = null;
            if (placeAt != null) shift = new XYZ(placeAt[0] / MmPerFoot - from.X, placeAt[1] / MmPerFoot - from.Y, 0);
            else if (moveBy != null) shift = new XYZ(moveBy[0] / MmPerFoot, moveBy[1] / MmPerFoot, 0);
            if (shift != null && !shift.IsZeroLength()) ElementTransformUtils.MoveElement(doc, label.Id, shift);
        }
        catch (Exception ex) { problems.Add("Revit refused the move - " + ex.Message); }
        if (width != "")
        {
            try { label.Width = widthWanted / MmPerFoot; } catch (Exception ex) { problems.Add("Revit refused the width - " + ex.Message); }
        }
        if (horizontalWanted.HasValue)
        {
            try { label.HorizontalAlignment = horizontalWanted.Value; } catch (Exception ex) { problems.Add("Revit refused the alignment - " + ex.Message); }
        }
        if (verticalWanted.HasValue)
        {
            try { label.VerticalAlignment = verticalWanted.Value; } catch (Exception ex) { problems.Add("Revit refused the vertical alignment - " + ex.Message); }
        }
        if (readableWanted.HasValue)
        {
            try { label.KeepRotatedTextReadable = readableWanted.Value; } catch (Exception ex) { problems.Add("Revit refused Keep Readable - " + ex.Message); }
        }
        if (problems.Count > 0) lines.Add("label " + label.Id + ": " + string.Join("; ", problems));
    }
    doc.Regenerate();

    // READ BACK - only a label that reads as asked is counted.
    var after = new List<string>();
    foreach (var label in picked)
    {
        var fresh = doc.GetElement(label.Id) as TextElement;
        if (fresh == null) { lines.Add("label " + label.Id + " is gone after the change"); continue; }
        var ok = true;
        string line;
        try
        {
            var at = fresh.Coord;
            if (placeAt != null && (Math.Abs(at.X * MmPerFoot - placeAt[0]) > 0.05 || Math.Abs(at.Y * MmPerFoot - placeAt[1]) > 0.05)) ok = false;
            if (width != "" && Math.Abs(fresh.Width * MmPerFoot - widthWanted) > 0.05) ok = false;
            if (horizontalWanted.HasValue && fresh.HorizontalAlignment != horizontalWanted.Value) ok = false;
            if (verticalWanted.HasValue && fresh.VerticalAlignment != verticalWanted.Value) ok = false;
            if (readableWanted.HasValue && fresh.KeepRotatedTextReadable != readableWanted.Value) ok = false;
            line = "label " + label.Id + " | at " + mm(at.X) + "," + mm(at.Y) + " mm | width " + mm(fresh.Width) + " mm | "
                + fresh.HorizontalAlignment + "/" + fresh.VerticalAlignment + " | keep readable " + (fresh.KeepRotatedTextReadable ? "yes" : "no");
        }
        catch (Exception ex) { ok = false; line = "label " + label.Id + " | could not be read back - " + ex.Message; }
        after.Add(line + (ok ? "" : " | NOT AS ASKED"));
        if (ok) { changed++; changedIds.Add(label.Id); }
        else lines.Add("label " + label.Id + " does not read back as asked - Revit kept something else");
    }
    readBack = string.Join("  ||  ", after);
    lines.Insert(0, changed + " of " + picked.Count + " label(s) in \"" + doc.Title + "\" read back as asked.");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.Add(changed + " label(s) laid out as asked.");
findings.AddRange(lines);
if (refusal == null)
    findings.Add("What each label SHOWS - its parameters, sample value, prefix, suffix, spaces, break and unit format - was not "
        + "touched: Edit Label changes those, by hand. Its type is CHANGE_ELEMENT_TYPE's.");
