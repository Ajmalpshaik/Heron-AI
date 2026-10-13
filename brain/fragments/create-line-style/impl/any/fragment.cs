// NOT STANDALONE. Assumes `doc`, `styleName`, `colour`, `weight`,
// `patternName` and `updateExisting` are in scope; leaves `style`, `madeNew`,
// `readBack`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A LINE STYLE IS A SUBCATEGORY OF LINES - Manage > Additional Settings > Line
// Styles is that list - so one is made with NewSubcategory under the Lines
// category and given its colour, projection line weight and line pattern.
//
// EACH OF THE THREE IS OPTIONAL BY VALUE: a colour of "" keeps black (or the
// existing colour), a weight of 0 keeps 1 (or the existing weight), a pattern
// name of "" keeps Solid (or the existing pattern). A pattern is found by
// name and never invented; one that does not exist refuses the call.
//
// AN EXISTING STYLE IS REFUSED unless updateExisting is true, and then its
// graphics are changed in place - every line drawn in it changes with it.
//
// READ BACK: colour, weight and pattern are read off the style afterwards and
// printed, so what Revit stored is what is seen.

var findings = new List<string>();
string refused = null;
GraphicsStyle style = null;
var madeNew = false;
var readBack = "";

var name = (styleName ?? "").Trim();
Category lines = null;
try { lines = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines); } catch { }

// THE COLOUR, "R,G,B" each 0 to 255, or empty.
Color wantedColour = null;
var colourText = (colour ?? "").Trim();
if (colourText != "")
{
    var parts = colourText.Split(',');
    int r, g, b;
    if (parts.Length == 3 && int.TryParse(parts[0].Trim(), out r) && int.TryParse(parts[1].Trim(), out g)
        && int.TryParse(parts[2].Trim(), out b) && r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255)
        wantedColour = new Color((byte)r, (byte)g, (byte)b);
    else
        refused = "\"" + colourText + "\" is not a colour - three numbers 0 to 255, \"255,0,0\" for red. "
            + "NOTHING WAS CHANGED.";
}

if (refused == null && name == "") refused = "No style name was given, so NOTHING WAS CHANGED.";
if (refused == null && lines == null) refused = "The Lines category could not be read, so NOTHING WAS CHANGED.";
if (refused == null && (weight < 0 || weight > 16))
    refused = "A line weight is 1 to 16 (0 keeps it as it is), and " + weight + " is not. NOTHING WAS CHANGED.";

ElementId patternId = null;
var wantedPattern = (patternName ?? "").Trim();
if (refused == null && wantedPattern != "")
{
    if (string.Equals(wantedPattern, "Solid", StringComparison.OrdinalIgnoreCase))
        patternId = LinePatternElement.GetSolidPatternId();
    else
    {
        var found = LinePatternElement.GetLinePatternElementByName(doc, wantedPattern);
        if (found == null) refused = "No line pattern is named \"" + wantedPattern + "\", so NOTHING WAS CHANGED. "
            + "CREATE_LINE_PATTERN makes one.";
        else patternId = found.Id;
    }
}

Category sub = null;
if (refused == null)
{
    foreach (Category existing in lines.SubCategories)
    {
        if (string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase)) { sub = existing; break; }
    }
    if (sub != null && !updateExisting)
    {
        refused = "A line style named \"" + sub.Name + "\" already exists, so NOTHING WAS CHANGED. Say "
            + "updateExisting to change its colour, weight and pattern in place.";
        sub = null;
    }
    else if (sub == null)
    {
        try
        {
            sub = doc.Settings.Categories.NewSubcategory(lines, name);
            madeNew = true;
        }
        catch (Exception ex)
        {
            refused = "Revit refused the new line style: " + ex.Message + ". NOTHING WAS CHANGED.";
        }
    }
}

if (refused == null && sub != null)
{
    try
    {
        if (wantedColour != null) sub.LineColor = wantedColour;
        if (weight > 0) sub.SetLineWeight(weight, GraphicsStyleType.Projection);
        if (patternId != null) sub.SetLinePatternId(patternId, GraphicsStyleType.Projection);
    }
    catch (Exception ex)
    {
        findings.Add("Revit refused part of the graphics: " + ex.Message + ".");
    }

    doc.Regenerate();
    style = sub.GetGraphicsStyle(GraphicsStyleType.Projection);

    var c = sub.LineColor;
    var w = sub.GetLineWeight(GraphicsStyleType.Projection);
    var pid = sub.GetLinePatternId(GraphicsStyleType.Projection);
    var p = pid == LinePatternElement.GetSolidPatternId() ? "Solid"
        : (doc.GetElement(pid) == null ? "(none)" : doc.GetElement(pid).Name);
    readBack = "colour " + (c == null || !c.IsValid ? "(none)" : c.Red + "," + c.Green + "," + c.Blue)
        + ", weight " + (w == null ? "(none)" : w.ToString()) + ", pattern " + p;
    findings.Add((madeNew ? "Made" : "Updated") + " line style \"" + sub.Name + "\": " + readBack + " (read back).");
}
if (refused != null) findings.Add(refused);
