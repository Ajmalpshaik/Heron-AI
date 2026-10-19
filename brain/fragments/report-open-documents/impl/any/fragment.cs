// NOT STANDALONE. Assumes `app` and `doc` are in scope; leaves `documents`,
// `paths`, `titles`, `activeTitle`, `count`, `familyList`, `familyCount`,
// `fileTimes`, `staleFileCount` and `findings` behind.
//
// NO TRANSACTION, and it needs none. Nothing changes.
//
// A TITLE IS NOT AN IDENTIFIER. Two projects called "Project1" can be open at
// once, so the path is reported beside every title and is what a later switch
// matches on first.
//
// LINKS ARE OPEN DOCUMENTS AND HAVE NO WINDOW. Application.Documents returns
// every loaded Document, each linked model among them. A link cannot be
// switched to, so it is counted rather than listed - LIST_LINKED_MODELS
// describes links.
//
// A FAMILY OPEN IN THE FAMILY EDITOR IS LISTED APART FROM THE PROJECTS
// (version 2, row 5b-435). It has a window of its own, and closing a family
// that was never saved loses it - so each one is named with its category and
// its file, or said out loud to have none. Version 1 only counted them, and
// "is the family saved" had no answer. The projects' lists and `count` are
// version 1's, unchanged: SWITCH_ACTIVE_PROJECT works from them, and a family
// is not a project to switch to.
//
// THE FAMILIES ARE ONE LINE, NOT A LIST. A list reaches the reply cut to three
// entries of sixty characters, and a family's path alone is longer than that.
//
// WHEN THE FILE ON DISK WAS LAST WRITTEN (version 3, row 5b-1216). "[unsaved
// changes]" says something is not saved, never how much. On 2026-10-19 a
// 40-floor tower's .rvt stayed at 10:42 while 124 changes went in after 10:45:
// Ctrl+S was pressed twice, and the keys went to another window. Only the
// file's own time on disk showed it. So every project and family with a file
// is given that time and its age in `fileTimes`, one line for the reason
// `familyList` is, and one with unsaved changes whose file is older than
// STALE_MINUTES is said out loud - the check a save is verified with, rather
// than trusting that a key was pressed.

var documents = new List<string>();
var paths = new List<string>();
var titles = new List<string>();
var findings = new List<string>();
var activeTitle = "";
var count = 0;
var familyList = "";
var familyCount = 0;
var fileTimes = "";
var staleFileCount = 0;

if (doc != null && doc.IsValidObject) activeTitle = doc.Title;

var links = 0;
var familyRows = new List<string>();
var familiesNeverSaved = new List<string>();
var projectsNeverSaved = 0;
var fileRows = new List<string>();
var staleRows = new List<string>();

// Unsaved work this old, against a file this old, is said out loud. Fifteen
// minutes: long enough that a save made a moment before the question is not
// called stale, short enough that the tower's 25 minutes would have been.
const int STALE_MINUTES = 15;
var now = DateTime.Now;

// The time the file was last written, or none: a cloud model's PathName is not
// a file on this PC, and a file moved or deleted since it was opened has no
// time to read. File.GetLastWriteTime answers 1601 for a missing file, so the
// file is asked for first.
Func<string, DateTime?> writtenAt = delegate (string p)
{
    if (string.IsNullOrEmpty(p)) return null;
    try
    {
        if (!System.IO.File.Exists(p)) return null;
        return System.IO.File.GetLastWriteTime(p);
    }
    catch (Exception) { return null; }
};

// The clock time alone when it is today, the date as well when it is not.
Func<DateTime, string> clock = delegate (DateTime t)
{
    return t.ToString(t.Date == now.Date ? "HH:mm" : "yyyy-MM-dd HH:mm",
        System.Globalization.CultureInfo.InvariantCulture);
};

Func<DateTime, string> age = delegate (DateTime t)
{
    var span = now - t;
    if (span.TotalMinutes < -1) return "a time ahead of this PC's clock";
    if (span.TotalMinutes < 1) return "under a minute ago";
    if (span.TotalMinutes < 60) return (int)span.TotalMinutes + " min ago";
    if (span.TotalHours < 48) return (int)span.TotalHours + " h " + span.Minutes + " min ago";
    return (int)span.TotalDays + " days ago";
};

// DocumentSet is not generic, so the loop variable carries its type.
foreach (Document candidate in app.Documents)
{
    if (candidate == null || !candidate.IsValidObject) continue;
    if (candidate.IsLinked) { links++; continue; }

    var title = candidate.Title ?? "";
    var path = candidate.PathName ?? "";
    // Document.Equals, as LOAD_FAMILY compares an open document with doc.
    var isActive = doc != null && candidate.Equals(doc);

    // Version 3: when its file was last written, projects and families alike.
    // A document never saved has no file, and the NEVER SAVED findings below
    // already speak for it.
    if (!string.IsNullOrEmpty(path))
    {
        var written = writtenAt(path);
        fileRows.Add(title + "  -  "
            + (written.HasValue
                ? "file written " + clock(written.Value) + ", " + age(written.Value)
                : "no file on this PC to read the time of - a cloud model, or a file moved since it was opened")
            + (candidate.IsModified ? "  [unsaved changes]" : ""));

        if (candidate.IsModified && written.HasValue && (now - written.Value).TotalMinutes >= STALE_MINUTES)
        {
            staleRows.Add("\"" + title + "\": the file on disk is from " + clock(written.Value)
                + " (" + age(written.Value) + ") - everything since is not saved");
        }
    }

    if (candidate.IsFamilyDocument)
    {
        // The category the family belongs to - what Family Category and
        // Parameters shows. A family whose category cannot be read says so
        // rather than leaving a gap that reads as a fault in the list.
        var category = "category not read";
        var owner = candidate.OwnerFamily;
        if (owner != null && owner.FamilyCategory != null) category = owner.FamilyCategory.Name;

        familyRows.Add(title
            + (isActive ? "  [ACTIVE]" : "")
            + (candidate.IsModified ? "  [unsaved changes]" : "")
            + "  -  " + category
            + "  -  "
            + (string.IsNullOrEmpty(path) ? "NEVER SAVED, no file of its own" : path));

        if (string.IsNullOrEmpty(path)) familiesNeverSaved.Add(title);
        continue;
    }

    titles.Add(title);
    paths.Add(path);

    documents.Add(title
        + (isActive ? "  [ACTIVE]" : "")
        + (candidate.IsModified ? "  [unsaved changes]" : "")
        + "  -  "
        + (string.IsNullOrEmpty(path) ? "never saved, so it has no path and can only be named by title" : path));

    if (string.IsNullOrEmpty(path)) projectsNeverSaved++;
    count++;
}

familyCount = familyRows.Count;
familyList = string.Join("  ||  ", familyRows.ToArray());
fileTimes = string.Join("  ||  ", fileRows.ToArray());
staleFileCount = staleRows.Count;

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
        + (familyCount > 0 ? " - only " + familyCount + " family document(s), listed in familyList." : ".")
        + " Nothing was read from a project.");
}
else
{
    findings.Add(count + " project(s) open"
        + (string.IsNullOrEmpty(activeTitle) ? "" : ", active is \"" + activeTitle + "\"")
        + ".");
}

if (familyCount > 0)
{
    findings.Add(familyCount + " family document(s) open in the Family Editor, each in familyList "
        + "with its category and its file - a family is not a project to switch to.");
}

// WORK NOT YET ON DISK (version 3). A file older than the unsaved changes in
// it is the tower of 2026-10-19: the keys went to another window, and only the
// file's time showed it. Said with the time, and with where to click first.
if (staleFileCount > 0)
{
    findings.Add(staleFileCount + " open document(s) have unsaved changes and a file on disk more than "
        + STALE_MINUTES + " minutes old - " + string.Join("; ", staleRows.ToArray())
        + ". Ctrl+S saves it: click inside the Revit window first, because the keys go to whichever "
        + "window is in front, then read this again - the file's time should be now.");
}

// LOST WORK. A window with no file behind it is gone when it closes, and Heron
// cannot prevent that: SAVE_DOCUMENT saves only a document that already has a
// file, and never the one in front. So it is said, with the names.
if (familiesNeverSaved.Count > 0)
{
    findings.Add(familiesNeverSaved.Count + " family document(s) have NEVER BEEN SAVED: "
        + string.Join(", ", familiesNeverSaved.Select(t => "\"" + t + "\"").ToArray())
        + ". Closing the window loses the family - the modeller saves it with Ctrl+S; "
        + "Heron cannot save a document that has no file.");
}

if (projectsNeverSaved > 0)
{
    findings.Add(projectsNeverSaved + " project(s) have NEVER BEEN SAVED. Closing one loses it - "
        + "the modeller saves it with Ctrl+S; Heron cannot save a document that has no file.");
}

if (links > 0)
{
    findings.Add(links + " linked model(s) are loaded and are NOT listed above - a link has no window "
        + "to switch to. LIST_LINKED_MODELS describes them.");
}
