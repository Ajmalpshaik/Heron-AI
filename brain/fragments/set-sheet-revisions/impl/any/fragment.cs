// NOT STANDALONE. Assumes `doc`, `elements`, `revisionIds` and `attach` are in
// scope, and leaves `sheetsChanged`, `cloudDriven`, `notASheet` and
// `revisionsRemaining` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A SHEET CARRIES REVISIONS TWO WAYS.
//
// A cloud drawn on the sheet puts its revision in the schedule by itself. A
// revision can also be LISTED on a sheet with no cloud anywhere - a
// whole-drawing issue - and that second list is the one written here. The
// cloud-driven ones are not in it and cannot be removed through it, which is
// why a removal that finds nothing names the sheet instead of reporting
// success.
//
// ADD OR REMOVE, NEVER REPLACE.
//
// Writing the list wholesale drops every earlier issue off the title block,
// and nobody reads a revision block to check that last month's entries are
// still there.
//
// WRITING THIS LIST CAN PURGE AN UNUSED REVISION.
//
// A revision on no sheet and in no cloud can vanish project-wide the next time
// sheet revisions are written. That is Revit's behaviour, not this fragment's -
// and the count of revisions still in the project afterwards is how anybody
// would notice.

int sheetsChanged = 0;
var cloudDriven = new List<ElementId>();
var notASheet = new List<ElementId>();
int revisionsRemaining = 0;

var wanted = new List<ElementId>();
if (revisionIds != null) foreach (var id in revisionIds) if (id != null) wanted.Add(id);

foreach (var element in elements)
{
    var sheet = element as ViewSheet;
    if (sheet == null)
    {
        if (element != null) notASheet.Add(element.Id);
        continue;
    }

    ICollection<ElementId> current = null;
    try { current = sheet.GetAdditionalRevisionIds(); }
    catch { continue; }

    var next = new List<ElementId>();
    if (current != null) next.AddRange(current);

    bool touched = false;

    foreach (var id in wanted)
    {
        if (attach)
        {
            if (next.Contains(id)) continue;   // already listed: not a change
            next.Add(id);
            touched = true;
        }
        else
        {
            if (!next.Contains(id))
            {
                // Either it was never on this sheet, or a CLOUD put it there -
                // and a cloud's revision is not in this list and cannot be
                // taken out of it.
                if (!cloudDriven.Contains(sheet.Id)) cloudDriven.Add(sheet.Id);
                continue;
            }
            next.Remove(id);
            touched = true;
        }
    }

    if (!touched) continue;

    try { sheet.SetAdditionalRevisionIds(next); }
    catch { continue; }

    // Read back: what the sheet lists now, not what was sent.
    ICollection<ElementId> after = null;
    try { after = sheet.GetAdditionalRevisionIds(); } catch { }
    if (after != null && after.Count == next.Count) sheetsChanged++;
}

// How many revisions the project still has. Writing sheet revisions can purge
// one that is on no sheet and in no cloud, and this number is where that shows.
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Revision)))
    if (element != null) revisionsRemaining++;
