// NOT STANDALONE. Assumes `doc`, `styleName`, `colour`, `weight`,
// `patternName`, `updateExisting`, `familyCategory` and `parentCategory` are
// in scope; leaves
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
// VERSION 3 - A SUBCATEGORY OF ANY CATEGORY THAT TAKES ONE (2026-11-17).
// parentCategory names the category the subcategory goes under - "Reference
// Planes" above all, so a family's planes can be put on a coloured
// subcategory of their own by SET_FAMILY_PLANE_REFERENCE - as Object Styles
// shows the name, or Revit's own OST_ name. It works in a project or a family,
// and only on a category Revit lets take subcategories (CanAddSubcategory).
// Left out it is "none", and the call is version 2's to the letter. Naming it
// together with familyCategory true is refused: the two say where the
// subcategory goes, and only one may. The parent's own name is the category
// itself, as for familyCategory, so "Reference Planes" with parentCategory
// "Reference Planes" recolours the category every plain plane is drawn in.
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

// THE PARENT: Lines, as version 1, the family's own category, or (version
// 3) the category parentCategory names.
Category parent = lines;
string familyRefusal = null;
var askedParent = (parentCategory ?? "").Trim();
if (string.Equals(askedParent, "none", StringComparison.OrdinalIgnoreCase)) askedParent = "";
var namedParent = askedParent.Length > 0;
// From here a family's category and a named one are read alike: a
// subcategory of a category, not a line style.
var ownCategory = familyCategory || namedParent;
if (familyCategory && namedParent)
{
    parent = null;
    familyRefusal = "Both familyCategory and parentCategory \"" + askedParent + "\" say where the subcategory goes, "
        + "and only one may, so NOTHING WAS CHANGED. Leave familyCategory out to use " + askedParent + ".";
}
else if (namedParent)
{
    parent = null;
    var squashed = new string(askedParent.ToLowerInvariant().Where(ch => char.IsLetterOrDigit(ch)).ToArray());
    var all = new List<Category>();
    foreach (Category c in doc.Settings.Categories)
    {
        if (c != null && !string.IsNullOrEmpty(c.Name)) all.Add(c);
    }
    var matches = all.Where(c => string.Equals(c.Name.Trim(), askedParent, StringComparison.OrdinalIgnoreCase)).ToList();
    if (matches.Count == 1) parent = matches[0];
    // Revit's own name for it - "OST_CLines" - or Reference Planes in any
    // spelling, which is asked for by its BuiltInCategory so it never hangs on
    // whether the category list offers it.
    BuiltInCategory builtIn;
    if (parent == null && askedParent.StartsWith("OST_", StringComparison.OrdinalIgnoreCase)
        && Enum.TryParse(askedParent, true, out builtIn))
    {
        try { parent = doc.Settings.Categories.get_Item(builtIn); } catch (Exception) { }
    }
    if (parent == null && (squashed == "referenceplanes" || squashed == "referenceplane"))
    {
        try { parent = doc.Settings.Categories.get_Item(BuiltInCategory.OST_CLines); } catch (Exception) { }
    }
    if (parent == null && matches.Count > 1)
        familyRefusal = "\"" + askedParent + "\" names " + matches.Count + " categories, so NOTHING WAS CHANGED. Name "
            + "it by Revit's own OST_ name instead.";
    else if (parent == null)
        familyRefusal = "This " + (doc.IsFamilyDocument ? "family" : "model") + " has no category named \""
            + askedParent + "\", so NOTHING WAS CHANGED. Name it as Object Styles shows it - \"Reference Planes\" - "
            + "or by Revit's own OST_ name. LIST_CATEGORIES lists them.";
}
else if (familyCategory)
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
        : namedParent ? "No subcategory name was given, so NOTHING WAS CHANGED."
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
    foreach (Category existing in parent.SubCategories)
    {
        if (string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase)) { sub = existing; break; }
    }
    // THE FAMILY'S CATEGORY ITSELF - by its own name, or "none" as
    // DRAW_FAMILY_DETAIL_LINES and SET_FAMILY_FORM_SUBCATEGORY say it - and
    // only when no real subcategory has that name, so one called "None" is
    // still reached.
    if (sub == null && ownCategory && (string.Equals(name, "none", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, parent.Name, StringComparison.OrdinalIgnoreCase)))
    {
        sub = parent;
        onCategory = true;
    }
    if (sub != null && !updateExisting)
    {
        if (!ownCategory)
            refused = "A line style named \"" + sub.Name + "\" already exists, so NOTHING WAS CHANGED. Say "
                + "updateExisting to change its colour, weight and pattern in place.";
        else if (onCategory)
            refused = "\"" + sub.Name + "\" is " + (namedParent ? "the category " + parent.Name + " itself"
                : "the family's own category") + ", which always exists, so NOTHING WAS "
                + "CHANGED. Say updateExisting to change its colour, weight and pattern in place.";
        else
            refused = (namedParent ? "The category " : "The family's category, ") + parent.Name
                + (namedParent ? "" : ",") + " already has a subcategory named \"" + sub.Name
                + "\", so NOTHING WAS CHANGED. Say updateExisting to change its colour, weight and pattern in "
                + "place - every line on it changes with it.";
        sub = null;
    }
    else if (sub == null)
    {
        if (ownCategory && !parent.CanAddSubcategory)
            refused = (namedParent ? "The category " + parent.Name + " takes" : "The family's category, " + parent.Name
                + ", takes") + " no subcategories, so \"" + name
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
                refused = (ownCategory ? "Revit refused the new subcategory of " + parent.Name + ": "
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
    var what = !ownCategory ? "line style \"" + sub.Name + "\""
        : onCategory ? (namedParent ? "the category" : "the family's own category") + " \"" + sub.Name + "\""
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
