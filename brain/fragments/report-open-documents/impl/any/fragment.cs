// NOT STANDALONE. Assumes `app` and `doc` are in scope; leaves `documents`,
// `paths`, `titles`, `activeTitle`, `count` and `findings` behind.
//
// NO TRANSACTION, and it needs none. Nothing changes.
//
// A TITLE IS NOT AN IDENTIFIER. Two projects called "Project1" can be open at
// once, so the path is reported beside every title and is what a later switch
// matches on first.
//
// LINKS AND FAMILIES ARE OPEN DOCUMENTS AND ARE NOT PROJECTS. Application.Documents
// returns every loaded Document - each linked model, each family being edited.
// They have no tab to switch to, so they are counted rather than listed.

var documents = new List<string>();
var paths = new List<string>();
var titles = new List<string>();
var findings = new List<string>();
var activeTitle = "";
var count = 0;

if (doc != null && doc.IsValidObject) activeTitle = doc.Title;

var links = 0;
var families = 0;

// DocumentSet is not generic, so the loop variable carries its type.
foreach (Document candidate in app.Documents)
{
    if (candidate == null || !candidate.IsValidObject) continue;
    if (candidate.IsLinked) { links++; continue; }
    if (candidate.IsFamilyDocument) { families++; continue; }

    var title = candidate.Title ?? "";
    var path = candidate.PathName ?? "";
    var isActive = !string.IsNullOrEmpty(activeTitle)
        && string.Equals(title, activeTitle, StringComparison.OrdinalIgnoreCase)
        && ReferenceEquals(candidate, doc);

    titles.Add(title);
    paths.Add(path);

    documents.Add(title
        + (isActive ? "  [ACTIVE]" : "")
        + (candidate.IsModified ? "  [unsaved changes]" : "")
        + "  -  "
        + (string.IsNullOrEmpty(path) ? "never saved, so it has no path and can only be named by title" : path));

    count++;
}

// The whole reason this fragment reports paths. Said once, with the titles that
// collide, rather than left for somebody to notice in the list.
var duplicated = titles
    .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
    .Where(g => g.Count() > 1)
    .Select(g => g.Key)
    .ToList();

if (duplicated.Count > 0)
{
    findings.Add("MORE THAN ONE OPEN PROJECT SHARES A TITLE: "
        + string.Join(", ", duplicated.Select(t => "\"" + t + "\"").ToArray())
        + ". A title cannot pick between them - use the full path.");
}

if (count == 0)
{
    findings.Add("No project is open in this Revit"
        + (links + families > 0
            ? " - only " + (links + families) + " linked or family document(s), which have no tab of their own."
            : ".")
        + " Nothing was read from a model.");
}
else
{
    findings.Add(count + " project(s) open"
        + (string.IsNullOrEmpty(activeTitle) ? "" : ", active is \"" + activeTitle + "\"")
        + ".");
}

if (links > 0)
{
    findings.Add(links + " linked model(s) are loaded and are NOT listed above - a link has no tab "
        + "to switch to. LIST_LINKED_MODELS describes them.");
}

if (families > 0)
{
    findings.Add(families + " family document(s) are open and are NOT listed above - a family is "
        + "edited in its own editor rather than being a project.");
}
