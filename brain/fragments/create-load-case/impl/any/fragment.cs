// NOT STANDALONE. Assumes `doc`, `name`, `category`, `nature` and `number` are
// in scope, and leaves `added`, `before`, `after`, `skipped`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and never opens one (Golden Rule 16).
//
// WHAT IT DOES. Analyze > Load Cases > Add: makes ONE load case with the
// modeller's name, a load case category (Dead, Live, Wind, Snow, Roof Live,
// Accidental, Temperature, Seismic - or a category the project added under
// Structural Load Cases) and a load nature the project already has. Nothing
// existing is changed, renamed or removed: a load case name already present, in
// any capitals, is SKIPPED and named. The load cases are read before and after,
// and both are handed back.
//
// NEVER GUESSED (D-33). No name - refused. No category - refused, with the
// categories listed: "Wind X" is the modeller's name, not a category, and it
// is never read as one. A nature left out is the nature called the same as the
// category (Revit's templates name one per category - "Wind" for Wind); with
// none of that name, refused with the natures listed. A nature named is
// matched exactly; one differing only in capitals is named, never taken. A
// case number already used is refused, never moved.
//
// RELEASES. LoadCase.Create(Document, string, ElementId, LoadCaseCategory) and
// (Document, string, ElementId, ElementId), LoadCase.Number, NatureId,
// SubcategoryId and IsNumberUnique, LoadNature, the LoadCaseCategory values
// and BuiltInCategory.OST_LoadCases carry the same signatures 2020 to 2027
// (tools/api-surface --members, 2026-11-01). One implementation for all eight.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER.

var added = 0;
var before = new List<string>();
var after = new List<string>();
var skipped = new List<string>();
var refused = new List<string>();
var findings = new List<string>();

Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
Func<string, string> squash = s => (s ?? "").Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
// "Wind Loads", "wind load", "Wind" all read as "wind".
Func<string, string> bare = s =>
{
    var w = squash(s);
    if (w.EndsWith("loads")) w = w.Substring(0, w.Length - 5);
    else if (w.EndsWith("load")) w = w.Substring(0, w.Length - 4);
    return w;
};

// ---- What the project has ----------------------------------------------------
Func<List<LoadCase>> readCases = () => new FilteredElementCollector(doc).OfClass(typeof(LoadCase)).Cast<LoadCase>()
    .OrderBy(c => c.Number).ThenBy(c => c.Name).ToList();
Func<LoadCase, string> caseLine = c =>
{
    string natureName = "?", categoryName = "?";
    try { var n = doc.GetElement(c.NatureId); natureName = n == null ? "none" : n.Name; } catch { }
    try
    {
        var p = c.get_Parameter(BuiltInParameter.LOAD_CASE_SUBCATEGORY);
        categoryName = p == null ? "?" : (p.AsValueString() ?? "?");
    }
    catch { }
    return c.Number + " " + quoted(c.Name) + " - nature " + natureName + ", category " + categoryName;
};

var cases = readCases();
foreach (var c in cases) before.Add(caseLine(c));
var natures = new FilteredElementCollector(doc).OfClass(typeof(LoadNature)).Cast<LoadNature>()
    .OrderBy(n => n.Name).ToList();
var categories = new List<Category>();
try
{
    var parent = doc.Settings.Categories.get_Item(BuiltInCategory.OST_LoadCases);
    if (parent != null)
        foreach (Category sub in parent.SubCategories) categories.Add(sub);
}
catch { }
categories = categories.OrderBy(c => c.Name).ToList();
var categoryList = categories.Count == 0
    ? "Dead, Live, Wind, Snow, Roof Live, Accidental, Temperature, Seismic"
    : string.Join(", ", categories.Select(c => c.Name));
var natureList = natures.Count == 0 ? "none" : string.Join(", ", natures.Select(n => quoted(n.Name)));

var wantName = (name ?? "").Trim();
var wantCategory = (category ?? "").Trim();
var wantNature = (nature ?? "").Trim();
// `absent: none` arrives as the text "none" when left out (FRAGMENT-ISSUES row 5b-1432).
if (string.Equals(wantCategory, "none", StringComparison.OrdinalIgnoreCase)) wantCategory = "";
if (string.Equals(wantNature, "none", StringComparison.OrdinalIgnoreCase)) wantNature = "";
var ok = true;

if (wantName.Length == 0)
{
    refused.Add("No load case name was given. A name is never made up - say what the new load case is called.");
    ok = false;
}

var clash = ok ? cases.FirstOrDefault(c => string.Equals(c.Name, wantName, StringComparison.OrdinalIgnoreCase)) : null;
if (clash != null)
{
    skipped.Add(quoted(wantName) + " - the load case " + caseLine(clash) + " already exists; left as it is, nothing "
        + "renamed or changed.");
    ok = false;
}

// ---- The category ------------------------------------------------------------
LoadCaseCategory? builtIn = null;
Category custom = null;
var categoryWord = bare(wantCategory);
if (ok && wantCategory.Length == 0)
{
    refused.Add("No load case category was given for " + quoted(wantName) + ", and it is never guessed from the name. "
        + "The categories this project has: " + categoryList + ".");
    ok = false;
}
else if (ok)
{
    switch (categoryWord)
    {
        case "dead": builtIn = LoadCaseCategory.Dead; break;
        case "live": builtIn = LoadCaseCategory.Live; break;
        case "wind": builtIn = LoadCaseCategory.Wind; break;
        case "snow": builtIn = LoadCaseCategory.Snow; break;
        case "rooflive": builtIn = LoadCaseCategory.RoofLive; break;
        case "accidental": builtIn = LoadCaseCategory.Accidental; break;
        case "temperature": builtIn = LoadCaseCategory.Temperature; break;
        case "seismic": builtIn = LoadCaseCategory.Seismic; break;
    }
    if (builtIn == null)
    {
        custom = categories.FirstOrDefault(c => bare(c.Name) == categoryWord);
        if (custom == null)
        {
            refused.Add(quoted(wantCategory) + " is not a load case category this project has: " + categoryList
                + ". Nothing was guessed for it.");
            ok = false;
        }
    }
}

// ---- The nature --------------------------------------------------------------
LoadNature chosen = null;
if (ok)
{
    if (wantNature.Length > 0)
    {
        chosen = natures.FirstOrDefault(n => n.Name == wantNature);
        if (chosen == null)
        {
            var near = natures.FirstOrDefault(n => string.Equals(n.Name, wantNature, StringComparison.OrdinalIgnoreCase));
            refused.Add(near != null
                ? "No load nature is called exactly " + quoted(wantNature) + "; " + quoted(near.Name)
                  + " differs only in capitals and was NOT taken."
                : quoted(wantNature) + " is not a load nature this project has: " + natureList + ".");
            ok = false;
        }
    }
    else
    {
        var same = natures.Where(n => bare(n.Name) == categoryWord).ToList();
        if (same.Count == 1)
        {
            chosen = same[0];
            findings.Add("No nature was named, so the nature called the same as the category was used: "
                + quoted(chosen.Name) + ".");
        }
        else
        {
            refused.Add("No nature was named and " + (same.Count == 0 ? "no" : "more than one") + " load nature is "
                + "called the same as the category " + quoted(wantCategory) + ". Name one of: " + natureList + ".");
            ok = false;
        }
    }
}

// ---- The number --------------------------------------------------------------
if (ok && number > 0 && !LoadCase.IsNumberUnique(doc, number))
{
    var holder = cases.FirstOrDefault(c => c.Number == number);
    refused.Add("Case number " + number + " is already used" + (holder != null ? " by " + quoted(holder.Name) : "")
        + "; no existing case is renumbered. Leave the number out for Revit's next one, or give a free one.");
    ok = false;
}

// ---- Make it -----------------------------------------------------------------
if (ok)
{
    LoadCase made = null;
    try
    {
        made = builtIn != null
            ? LoadCase.Create(doc, wantName, chosen.Id, builtIn.Value)
            : LoadCase.Create(doc, wantName, chosen.Id, custom.Id);
    }
    catch (Exception ex)
    {
        refused.Add("Revit would not make the load case " + quoted(wantName) + ": " + ex.Message);
    }
    if (made != null)
    {
        if (number > 0)
        {
            try { made.Number = number; }
            catch (Exception ex) { findings.Add("Made, but Revit would not give it number " + number + ": " + ex.Message); }
        }
        added = 1;
        findings.Add("Made the load case " + caseLine(made) + ", read back from the model.");
        if (made.NatureId != chosen.Id)
            findings.Add("Read back, its nature is not " + quoted(chosen.Name) + " - look at it in Analyze > Load Cases.");
    }
}

foreach (var c in readCases()) after.Add(caseLine(c));
if (added == 0) findings.Add("Nothing was added - the load cases are as they were.");
