// NOT STANDALONE. Assumes `doc`, `category`, `projectionWeight`, `cutWeight`,
// `colour`, `pattern` and `material` are in scope; leaves `before`, `after`,
// `changed`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and never opens one. One call is
// one undo entry.
//
// MANAGE > OBJECT STYLES, ONE ROW. The row is a category ("Doors") or one of its
// subcategories ("Ducts > Hidden Lines"), named as Revit names it - LOCALISED,
// so on a French Revit "Portes", exactly as LIST_CATEGORIES prints the names
// this machine accepts. Capitals and runs of spaces are forgiven; nothing else
// is guessed. A name the model has not got is refused with the nearest names
// it has; a subcategory the category has not got, with that category's own.
//
// WHAT IS LEFT OUT IS KEPT. projectionWeight and cutWeight 0, and colour,
// pattern and material "keep" (each card need's `absent:`), leave that setting
// exactly as it is. Nothing asked at all is refused.
//
// EVERYTHING IS CHECKED BEFORE ANYTHING IS WRITTEN, so a refusal changes
// nothing: an internal category (Object Styles does not show it); the Lines
// category (a line style is CREATE_LINE_STYLE's); a weight outside 1 to 16; a
// cut weight on a category Revit does not cut; a colour that is neither R,G,B
// nor one of the ten names below; a line pattern or a material no element in
// this model is named; a material on a category that is not a model category.
//
// ONE LINE COLOUR PER ROW. Revit's Object Styles has one Line Color for a row,
// used for projection and cut alike - "the cut line colour of Walls" sets that
// one colour, and the answer says so. The line pattern is the row's one Line
// Pattern too, which Revit holds as the projection pattern.
//
// A WRITE REVIT REFUSES IS THROWN, so the caller's transaction rolls every
// setting back and no half-styled row is left (Golden Rule 16).
//
// READ BACK. Every setting is read off the category before and after one
// regeneration, and a value asked for that Revit does not hold is named NOT
// KEPT.

var findings = new List<string>();
string refused = null;
var before = "";
var after = "";
var changed = 0;

// THE COLOURS A NAME MAY STAND FOR - these ten, and nothing else. Anything else
// is R,G,B, each 0 to 255.
var namedColours = new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase)
{
    { "black", new[] { 0, 0, 0 } },
    { "white", new[] { 255, 255, 255 } },
    { "red", new[] { 255, 0, 0 } },
    { "green", new[] { 0, 128, 0 } },
    { "blue", new[] { 0, 0, 255 } },
    { "yellow", new[] { 255, 255, 0 } },
    { "cyan", new[] { 0, 255, 255 } },
    { "magenta", new[] { 255, 0, 255 } },
    { "orange", new[] { 255, 128, 0 } },
    { "grey", new[] { 128, 128, 128 } },
    { "gray", new[] { 128, 128, 128 } },
};

Func<string, string> plain = text => string.Join(" ",
    (text ?? "").Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
Func<string, bool> kept = text =>
{
    var t = (text ?? "").Trim();
    return t.Length == 0 || string.Equals(t, "keep", StringComparison.OrdinalIgnoreCase);
};

// Edit distance, for the names nearest to one the model has not got.
Func<string, string, int> distance = (a, b) =>
{
    var previous = new int[b.Length + 1];
    var current = new int[b.Length + 1];
    for (var j = 0; j <= b.Length; j++) previous[j] = j;
    for (var i = 1; i <= a.Length; i++)
    {
        current[0] = i;
        for (var j = 1; j <= b.Length; j++)
        {
            var cost = a[i - 1] == b[j - 1] ? 0 : 1;
            current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
        }
        var swap = previous; previous = current; current = swap;
    }
    return previous[b.Length];
};
Func<string, List<string>, int, string> nearest = (asked, names, count) =>
{
    var a = plain(asked);
    var ranked = names.Distinct().OrderBy(n => plain(n).Contains(a) || a.Contains(plain(n)) ? 0 : 1)
        .ThenBy(n => distance(a, plain(n))).ThenBy(n => n, StringComparer.OrdinalIgnoreCase).Take(count).ToList();
    return ranked.Count == 0 ? "(none)" : "\"" + string.Join("\", \"", ranked) + "\"";
};

// THE ROW: "Category" or "Category > Subcategory".
var askedRow = (category ?? "").Trim();
var arrow = askedRow.IndexOf('>');
var askedCategory = (arrow < 0 ? askedRow : askedRow.Substring(0, arrow)).Trim();
var askedSub = arrow < 0 ? "" : askedRow.Substring(arrow + 1).Trim();

Category parent = null;
Category row = null;
if (askedCategory.Length == 0)
    refused = "No category was named, so NOTHING WAS CHANGED. Name one as Object Styles shows it - \"Doors\", "
        + "or \"Ducts > Hidden Lines\" for a subcategory. LIST_CATEGORIES lists the names this model has.";
else if (arrow >= 0 && askedSub.Length == 0)
    refused = "\"" + askedRow + "\" names no subcategory after the \">\", so NOTHING WAS CHANGED.";

if (refused == null)
{
    var all = new List<Category>();
    foreach (Category c in doc.Settings.Categories)
    {
        if (c != null && !string.IsNullOrEmpty(c.Name)) all.Add(c);
    }
    parent = all.FirstOrDefault(c => plain(c.Name) == plain(askedCategory));
    if (parent == null)
        refused = "This model has no category named \"" + askedCategory + "\", so NOTHING WAS CHANGED. The nearest "
            + "it has: " + nearest(askedCategory, all.Select(c => c.Name).ToList(), 8) + ". Names are as this "
            + "Revit shows them in Object Styles - LIST_CATEGORIES lists them all.";
}

if (refused == null && parent.CategoryType == CategoryType.Internal)
    refused = "\"" + parent.Name + "\" is one of Revit's internal categories - Object Styles does not show it and "
        + "its style is not the modeller's to set - so NOTHING WAS CHANGED.";
Category lines = null;
try { lines = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines); }
catch (Exception) { }
if (refused == null && lines != null && parent.Id == lines.Id)
    refused = "\"" + parent.Name + "\" holds the line styles, and a line style is changed by CREATE_LINE_STYLE "
        + "(Manage > Additional Settings > Line Styles), so NOTHING WAS CHANGED here.";

if (refused == null)
{
    row = parent;
    if (askedSub.Length > 0)
    {
        var subs = new List<Category>();
        if (parent.SubCategories != null)
        {
            foreach (Category s in parent.SubCategories)
            {
                if (s != null && !string.IsNullOrEmpty(s.Name)) subs.Add(s);
            }
        }
        row = subs.FirstOrDefault(s => plain(s.Name) == plain(askedSub));
        if (row == null)
            refused = subs.Count == 0
                ? "\"" + parent.Name + "\" has no subcategories, so it has none named \"" + askedSub
                    + "\". NOTHING WAS CHANGED."
                : "\"" + parent.Name + "\" has no subcategory named \"" + askedSub + "\", so NOTHING WAS CHANGED. "
                    + "Its subcategories: \"" + string.Join("\", \"", subs.Select(s => s.Name)
                        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase).Take(40)) + "\""
                    + (subs.Count > 40 ? " and " + (subs.Count - 40) + " more" : "") + ".";
    }
}
var label = row == null ? askedRow : (row == parent ? parent.Name : parent.Name + " > " + row.Name);

// WHAT WAS ASKED, every value checked before anything is written.
var askedSomething = projectionWeight != 0 || cutWeight != 0 || !kept(colour) || !kept(pattern) || !kept(material);
if (refused == null && !askedSomething)
    refused = "Nothing to change was named for " + label + " - a projection or cut line weight, a line colour, a "
        + "line pattern or a material - so NOTHING WAS CHANGED.";
if (refused == null && (projectionWeight < 0 || projectionWeight > 16))
    refused = "A line weight is 1 to 16, and the projection weight " + projectionWeight + " is not. NOTHING WAS CHANGED.";
if (refused == null && (cutWeight < 0 || cutWeight > 16))
    refused = "A line weight is 1 to 16, and the cut weight " + cutWeight + " is not. NOTHING WAS CHANGED.";
if (refused == null && cutWeight != 0 && !row.IsCuttable)
    refused = "Revit does not cut " + label + ", so it has no cut line weight to set (Object Styles leaves that "
        + "cell empty). NOTHING WAS CHANGED.";

Color wantedColour = null;
var colourText = (colour ?? "").Trim();
if (refused == null && !kept(colour))
{
    int[] rgb;
    var parts = colourText.Split(',');
    int r, g, b;
    if (namedColours.TryGetValue(colourText, out rgb))
        wantedColour = new Color((byte)rgb[0], (byte)rgb[1], (byte)rgb[2]);
    else if (parts.Length == 3 && int.TryParse(parts[0].Trim(), out r) && int.TryParse(parts[1].Trim(), out g)
        && int.TryParse(parts[2].Trim(), out b) && r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255)
        wantedColour = new Color((byte)r, (byte)g, (byte)b);
    else
        refused = "\"" + colourText + "\" is not a colour this takes - three numbers 0 to 255 (\"255,0,0\"), or one "
            + "of: black, white, red, green, blue, yellow, cyan, magenta, orange, grey. NOTHING WAS CHANGED.";
}

ElementId wantedPattern = null;
var patternText = (pattern ?? "").Trim();
var solid = LinePatternElement.GetSolidPatternId();
if (refused == null && !kept(pattern))
{
    if (string.Equals(patternText, "Solid", StringComparison.OrdinalIgnoreCase))
        wantedPattern = solid;
    else
    {
        var exact = LinePatternElement.GetLinePatternElementByName(doc, patternText);
        if (exact != null) wantedPattern = exact.Id;
        else
        {
            var patterns = new FilteredElementCollector(doc).OfClass(typeof(LinePatternElement))
                .Cast<LinePatternElement>().ToList();
            var same = patterns.Where(p => plain(p.Name) == plain(patternText)).ToList();
            if (same.Count == 1) wantedPattern = same[0].Id;
            else
                refused = "This model has no line pattern named \"" + patternText + "\", so NOTHING WAS CHANGED. "
                    + "The nearest it has: " + nearest(patternText, patterns.Select(p => p.Name).ToList(), 8)
                    + ", or \"Solid\". CREATE_LINE_PATTERN makes a new one.";
        }
    }
}

Material wantedMaterial = null;
var materialText = (material ?? "").Trim();
if (refused == null && !kept(material))
{
    if (row.CategoryType != CategoryType.Model)
        refused = label + " is not a model category, and only a model category carries a material in Object "
            + "Styles. NOTHING WAS CHANGED.";
    else
    {
        var materials = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>().ToList();
        wantedMaterial = materials.FirstOrDefault(m => m.Name == materialText);
        if (wantedMaterial == null)
        {
            var same = materials.Where(m => plain(m.Name) == plain(materialText)).ToList();
            if (same.Count == 1) wantedMaterial = same[0];
            else
                refused = "This model has no material named \"" + materialText + "\", so NOTHING WAS CHANGED. "
                    + "The nearest it has: " + nearest(materialText, materials.Select(m => m.Name).ToList(), 8)
                    + ". LIST_MATERIALS lists them; CREATE_MATERIAL makes one.";
        }
    }
}

// ONE ROW, READ AS OBJECT STYLES SHOWS IT.
Func<Category, string> describe = c =>
{
    var lc = c.LineColor;
    var pw = c.GetLineWeight(GraphicsStyleType.Projection);
    var cw = c.IsCuttable ? c.GetLineWeight(GraphicsStyleType.Cut) : null;
    var pid = c.GetLinePatternId(GraphicsStyleType.Projection);
    string patternName;
    if (pid == null || pid == ElementId.InvalidElementId) patternName = "none";
    else if (pid == solid) patternName = "Solid";
    else
    {
        var element = doc.GetElement(pid);
        patternName = element == null ? "none" : element.Name;
    }
    var text = "projection weight " + (pw == null ? "none" : pw.Value.ToString())
        + ", cut weight " + (!c.IsCuttable ? "- (not cut)" : (cw == null ? "none" : cw.Value.ToString()))
        + ", line colour " + (lc == null || !lc.IsValid ? "none" : lc.Red + "," + lc.Green + "," + lc.Blue)
        + ", line pattern " + patternName;
    if (c.CategoryType == CategoryType.Model)
        text += ", material " + (c.Material == null ? "none" : c.Material.Name);
    return text;
};

if (refused == null)
{
    before = label + ": " + describe(row);

    var oldColour = row.LineColor;
    var oldProjection = row.GetLineWeight(GraphicsStyleType.Projection);
    var oldCut = row.IsCuttable ? row.GetLineWeight(GraphicsStyleType.Cut) : null;
    var oldPattern = row.GetLinePatternId(GraphicsStyleType.Projection);
    var oldMaterial = row.CategoryType == CategoryType.Model ? row.Material : null;
    var step = "";
    try
    {
        if (projectionWeight > 0 && oldProjection != projectionWeight)
        {
            step = "the projection line weight";
            row.SetLineWeight(projectionWeight, GraphicsStyleType.Projection);
            changed++;
        }
        if (cutWeight > 0 && oldCut != cutWeight)
        {
            step = "the cut line weight";
            row.SetLineWeight(cutWeight, GraphicsStyleType.Cut);
            changed++;
        }
        if (wantedColour != null && (oldColour == null || !oldColour.IsValid || oldColour.Red != wantedColour.Red
            || oldColour.Green != wantedColour.Green || oldColour.Blue != wantedColour.Blue))
        {
            step = "the line colour";
            row.LineColor = wantedColour;
            changed++;
        }
        if (wantedPattern != null && oldPattern != wantedPattern)
        {
            step = "the line pattern";
            row.SetLinePatternId(wantedPattern, GraphicsStyleType.Projection);
            changed++;
        }
        if (wantedMaterial != null && (oldMaterial == null || oldMaterial.Id != wantedMaterial.Id))
        {
            step = "the material";
            row.Material = wantedMaterial;
            changed++;
        }
    }
    catch (Exception ex)
    {
        // Thrown on, so the caller's transaction rolls back every setting.
        throw new InvalidOperationException("Revit refused " + step + " of " + label + " in Object Styles: "
            + ex.Message + " The whole change was rolled back, so NOTHING WAS CHANGED.", ex);
    }

    doc.Regenerate();
    after = label + ": " + describe(row);

    if (changed == 0)
        findings.Add(label + " already has every setting asked for, so nothing was changed: " + before + ".");
    else
        findings.Add("Object Styles changed for " + label + " (" + changed + " setting(s)), for the whole model - "
            + "every view that does not override it. Before: " + before + ". After, read back: " + after + ".");

    if (wantedColour != null && row.IsCuttable)
        findings.Add("Revit holds one line colour for " + label + ", used for its projection and cut lines alike.");

    // WHAT WAS ASKED AGAINST WHAT REVIT HOLDS - a value left out was not asked.
    var notKept = new List<string>();
    var c2 = row.LineColor;
    if (projectionWeight > 0 && row.GetLineWeight(GraphicsStyleType.Projection) != projectionWeight)
        notKept.Add("projection weight " + projectionWeight);
    if (cutWeight > 0 && row.GetLineWeight(GraphicsStyleType.Cut) != cutWeight)
        notKept.Add("cut weight " + cutWeight);
    if (wantedColour != null && (c2 == null || !c2.IsValid || c2.Red != wantedColour.Red
        || c2.Green != wantedColour.Green || c2.Blue != wantedColour.Blue))
        notKept.Add("line colour " + colourText);
    if (wantedPattern != null && row.GetLinePatternId(GraphicsStyleType.Projection) != wantedPattern)
        notKept.Add("line pattern " + patternText);
    if (wantedMaterial != null && (row.Material == null || row.Material.Id != wantedMaterial.Id))
        notKept.Add("material " + materialText);
    if (notKept.Count > 0)
        findings.Add("NOT KEPT: " + string.Join(", ", notKept) + " was asked, and Revit holds " + after + ".");
}
if (refused != null) findings.Add(refused);
