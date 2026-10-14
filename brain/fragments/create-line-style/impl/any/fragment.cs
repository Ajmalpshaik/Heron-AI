// NOT STANDALONE. Assumes `doc`, `styleName`, `colour`, `weight`,
// `patternName`, `updateExisting` and `familyCategory` are in scope; leaves
// `style`, `madeNew`, `readBack`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A LINE STYLE IS A SUBCATEGORY OF LINES - Manage > Additional Settings > Line
// Styles is that list - so one is made with NewSubcategory under the Lines
// category and given its colour, projection line weight and line pattern.
//
// VERSION 2 - A SUBCATEGORY OF THE FAMILY'S OWN CATEGORY (2026-10-14, row
// 5b-463). familyCategory true, in a family open in the Family Editor, makes or
// changes a subcategory of the family's category instead - Object Styles there,
// on its Annotation Objects or Model Objects tab: Generic Annotations >
// Signature, the subcategory DRAW_FAMILY_DETAIL_LINES drew the lines on. The
// category's own name, or "none" as the family tools say it, is the category
// itself, which always exists. Colour, weight and pattern are set exactly as for
// a line style. Absent or false is a line style under Lines, version 1's code
// and sentences. It is a bool because the add-in binds an absent value only for an
// optional bool (HeronBindingNote.AbsentValue): a string would have been a new
// required input for every caller.
//
// EACH OF THE THREE IS OPTIONAL BY VALUE: a colour of "" keeps black (or the
// existing colour), a weight of 0 keeps 1 (or the existing weight), a pattern
// name of "" keeps Solid (or the existing pattern). A pattern is found by
// name and never invented; one that does not exist refuses the call.
//
// AN EXISTING STYLE IS REFUSED unless updateExisting is true, and then its
// graphics are changed in place - every line drawn in it changes with it. The
// same holds for a family's subcategory, so the lines a family already has on
// one are recoloured by naming it with updateExisting.
//
// READ BACK: colour, weight and pattern are read off the style after one
// regeneration and printed, so what Revit stored is what is seen; any value
// asked for that Revit does not hold is named in a NOT KEPT finding.

var findings = new List<string>();
string refused = null;
GraphicsStyle style = null;
var madeNew = false;
var readBack = "";

var name = (styleName ?? "").Trim();
Category lines = null;
try { lines = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines); } catch { }

// THE PARENT: Lines, as version 1, or the family's own category.
Category parent = lines;
string familyRefusal = null;
if (familyCategory)
{
    parent = null;
    if (!doc.IsFamilyDocument)
        familyRefusal = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family "
            + "Editor, so there is no family category to hold a subcategory. NOTHING WAS CHANGED. A project's line "
            + "style is this call without familyCategory.";
    else if (doc.OwnerFamily == null || doc.OwnerFamily.FamilyCategory == null)
        familyRefusal = "This family has no category to hold a subcategory, so NOTHING WAS CHANGED.";
    else
        parent = doc.OwnerFamily.FamilyCategory;
}

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

if (refused == null && name == "")
    refused = familyCategory ? "No subcategory name was given, so NOTHING WAS CHANGED."
        : "No style name was given, so NOTHING WAS CHANGED.";
if (refused == null && familyRefusal != null) refused = familyRefusal;
if (refused == null && parent == null) refused = "The Lines category could not be read, so NOTHING WAS CHANGED.";
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
var onCategory = false;
if (refused == null)
{
    // THE FAMILY'S CATEGORY ITSELF - by its own name, or "none" as
    // DRAW_FAMILY_DETAIL_LINES and SET_FAMILY_FORM_SUBCATEGORY say it.
    if (familyCategory && (string.Equals(name, "none", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, parent.Name, StringComparison.OrdinalIgnoreCase)))
    {
        sub = parent;
        onCategory = true;
    }
    else
    {
        foreach (Category existing in parent.SubCategories)
        {
            if (string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase)) { sub = existing; break; }
        }
    }
    if (sub != null && !updateExisting)
    {
        if (!familyCategory)
            refused = "A line style named \"" + sub.Name + "\" already exists, so NOTHING WAS CHANGED. Say "
                + "updateExisting to change its colour, weight and pattern in place.";
        else if (onCategory)
            refused = "\"" + sub.Name + "\" is the family's own category, which always exists, so NOTHING WAS "
                + "CHANGED. Say updateExisting to change its colour, weight and pattern in place.";
        else
            refused = "The family's category, " + parent.Name + ", already has a subcategory named \"" + sub.Name
                + "\", so NOTHING WAS CHANGED. Say updateExisting to change its colour, weight and pattern in "
                + "place - every line on it changes with it.";
        sub = null;
    }
    else if (sub == null)
    {
        if (familyCategory && !parent.CanAddSubcategory)
            refused = "The family's category, " + parent.Name + ", takes no subcategories, so \"" + name
                + "\" cannot be made in it. NOTHING WAS CHANGED.";
        else
        {
            try
            {
                sub = doc.Settings.Categories.NewSubcategory(parent, name);
                madeNew = true;
            }
            catch (Exception ex)
            {
                refused = (familyCategory ? "Revit refused the new subcategory of " + parent.Name + ": "
                    : "Revit refused the new line style: ") + ex.Message + ". NOTHING WAS CHANGED.";
            }
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
    var what = !familyCategory ? "line style \"" + sub.Name + "\""
        : onCategory ? "the family's own category \"" + sub.Name + "\""
        : "subcategory \"" + sub.Name + "\" of " + parent.Name;
    findings.Add((madeNew ? "Made" : "Updated") + " " + what + ": " + readBack + " (read back).");

    // WHAT WAS ASKED AGAINST WHAT REVIT HOLDS - a value left out was not asked.
    var notKept = new List<string>();
    if (wantedColour != null && (c == null || !c.IsValid || c.Red != wantedColour.Red
        || c.Green != wantedColour.Green || c.Blue != wantedColour.Blue))
        notKept.Add("colour " + colourText);
    if (weight > 0 && w != weight) notKept.Add("weight " + weight);
    if (patternId != null && pid != patternId) notKept.Add("pattern " + wantedPattern);
    if (notKept.Count > 0)
        findings.Add("NOT KEPT: " + string.Join(", ", notKept) + " was asked, and Revit holds " + readBack + ".");
}
if (refused != null) findings.Add(refused);
