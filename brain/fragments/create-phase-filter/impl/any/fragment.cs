// NOT STANDALONE. Assumes `doc`, `filterName`, `newDisplay`, `existingDisplay`,
// `demolishedDisplay` and `temporaryDisplay` are in scope; leaves
// `phaseFilterId`, `created`, `statusReport` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// MAKES ONE NEW PHASE FILTER - what Manage > Phasing > Phase Filters > New
// does - and sets how each of its four phase statuses shows: New, Existing,
// Demolished, Temporary, each "By Category", "Overridden" or "Not Displayed",
// the three words that dialog uses.
//
// A NAME ALREADY IN USE REFUSES, and the filter holding it is named. It never
// edits the filter that is there: changing "Show Complete" because a request
// reused its name would change every view that uses it.
//
// EVERY VALUE IS CHECKED BEFORE ANYTHING IS MADE. A word that is not one of the
// three refuses the whole call, so a half-set filter is never left behind. A
// status left out is left as Revit makes it, and the read-back says what that
// was.
//
// READ BACK: the filter is fetched again by its id after it is made, and every
// one of the four statuses is read from it. The report says what each reads,
// not what was asked; any status that reads otherwise is named.
//
// "Overridden" draws that status with the Graphic Overrides tab of the
// Phasing dialog. That tab - lines, patterns, halftone, material per status -
// has no API on any release 2020 to 2027 (FRAGMENT-ISSUES row 5b-1350), so it
// is set by hand and never here.

ElementId phaseFilterId = ElementId.InvalidElementId;
var created = 0;
var findings = new List<string>();
var problems = new List<string>();
var lines = new List<string>();

// The three words of the Phase Filters tab, and the spellings a modeller
// types for each. Compared with case, spaces, hyphens, underscores and
// apostrophes taken out.
Func<string, string> squash = text => new string((text ?? "").ToLowerInvariant()
    .Where(c => c != ' ' && c != '-' && c != '_' && c != '\'' && c != '’').ToArray());
Func<PhaseStatusPresentation, string> word = p =>
    p == PhaseStatusPresentation.ShowByCategory ? "By Category"
    : p == PhaseStatusPresentation.ShowOverriden ? "Overridden"
    : p == PhaseStatusPresentation.DontShow ? "Not Displayed"
    : p.ToString();

// Each status: its name as the dialog shows it, the Revit status, the value
// asked (null = leave as Revit makes it), and whether the value was usable.
var statusNames = new[] { "New", "Existing", "Demolished", "Temporary" };
var statusEnums = new[] { ElementOnPhaseStatus.New, ElementOnPhaseStatus.Existing,
    ElementOnPhaseStatus.Demolished, ElementOnPhaseStatus.Temporary };
var asked = new[] { newDisplay, existingDisplay, demolishedDisplay, temporaryDisplay };
var wanted = new PhaseStatusPresentation?[4];

for (var i = 0; i < 4; i++)
{
    var raw = (asked[i] ?? "").Trim();
    var key = squash(raw);
    if (key == "" || key == "leave" || key == "unchanged" || key == "default")
    {
        wanted[i] = null;
        continue;
    }
    if (key == "bycategory" || key == "category" || key == "showbycategory" || key == "normal")
        wanted[i] = PhaseStatusPresentation.ShowByCategory;
    else if (key == "overridden" || key == "overriden" || key == "override" || key == "overrides"
             || key == "showoverridden" || key == "showoverriden")
        wanted[i] = PhaseStatusPresentation.ShowOverriden;
    else if (key == "notdisplayed" || key == "notshown" || key == "hidden" || key == "hide"
             || key == "dontshow" || key == "donotshow" || key == "none" || key == "off")
        wanted[i] = PhaseStatusPresentation.DontShow;
    else
        problems.Add(statusNames[i] + " was given \"" + raw + "\", which is not one of By Category, "
            + "Overridden or Not Displayed. Halftone, lines, patterns and material are the Graphic "
            + "Overrides tab of Manage > Phasing, which has no API - set those by hand.");
}

var name = (filterName ?? "").Trim();
if (doc.IsFamilyDocument)
    problems.Add("This is a family file. Phase filters live in a project - open the project and ask again.");
else if (name == "")
    problems.Add("No name was given for the phase filter - name it, \"As-Built\" for example. Nothing is "
        + "made with a name Revit would pick.");
else if (!NamingUtils.IsValidName(name))
    problems.Add("\"" + name + "\" cannot be a Revit name - it holds a character Revit refuses in names "
        + "( \\ : { } [ ] | ; < > ? ` ~ ). Choose a name without it.");

var existing = new FilteredElementCollector(doc).OfClass(typeof(PhaseFilter)).Cast<PhaseFilter>().ToList();
if (name != "" && !doc.IsFamilyDocument)
{
    var same = existing.FirstOrDefault(f => string.Equals((f.Name ?? "").Trim(), name,
        StringComparison.OrdinalIgnoreCase));
    if (same != null)
    {
        var shows = string.Join(", ", Enumerable.Range(0, 4).Select(i =>
            statusNames[i] + " " + word(same.GetPhaseStatusPresentation(statusEnums[i]))));
        problems.Add("A phase filter named \"" + same.Name + "\" is already in this project (id "
            + same.Id + "; " + shows + "). It is left as it is - every view using it would change. "
            + "Choose another name, or change that filter under Manage > Phasing > Phase Filters.");
    }
    else
    {
        var near = existing.Where(f => squash(f.Name) == squash(name)).Select(f => "\"" + f.Name + "\"").ToList();
        if (near.Count > 0)
            findings.Add("A filter with a near name is already here: " + string.Join(", ", near)
                + ". The new one is made as asked; delete one by hand if they are the same thing.");
    }
}

if (problems.Count == 0)
{
    var made = PhaseFilter.Create(doc, name);
    for (var i = 0; i < 4; i++)
        if (wanted[i].HasValue)
            made.SetPhaseStatusPresentation(statusEnums[i], wanted[i].Value);

    // Read back from the document, not from the object just written to.
    var back = doc.GetElement(made.Id) as PhaseFilter;
    if (back == null)
    {
        problems.Add("Revit made the filter but it does not read back by its id - nothing is reported as made.");
    }
    else
    {
        phaseFilterId = back.Id;
        created = 1;
        if (!string.Equals(back.Name, name, StringComparison.Ordinal))
            findings.Add("Asked for the name \"" + name + "\", and it reads back as \"" + back.Name + "\".");
        for (var i = 0; i < 4; i++)
        {
            var reads = back.GetPhaseStatusPresentation(statusEnums[i]);
            if (!wanted[i].HasValue)
                lines.Add(statusNames[i] + ": " + word(reads) + " (not asked - as Revit made it)");
            else if (reads == wanted[i].Value)
                lines.Add(statusNames[i] + ": " + word(reads));
            else
            {
                lines.Add(statusNames[i] + ": " + word(reads) + " - ASKED " + word(wanted[i].Value));
                findings.Add(statusNames[i] + " was set to " + word(wanted[i].Value) + " and reads back as "
                    + word(reads) + ". Check it under Manage > Phasing > Phase Filters.");
            }
        }
    }
}

var statusReport = string.Join("; ", lines);
if (problems.Count > 0)
{
    findings.Insert(0, "NOTHING WAS MADE: " + string.Join(" ", problems));
    if (existing.Count > 0)
        findings.Add("The phase filters in this project: "
            + string.Join(", ", existing.Select(f => "\"" + f.Name + "\"")) + ".");
}
else
{
    findings.Insert(0, "Phase filter \"" + name + "\" made and read back - " + statusReport + ". "
        + "Put it on a view with SET_VIEW_PHASE.");
}
