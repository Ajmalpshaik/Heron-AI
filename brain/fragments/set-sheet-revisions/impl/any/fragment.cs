// NOT STANDALONE. Assumes `doc`, `elements`, `revisionIds`, `attach` and
// `replaceWith` are in scope, and leaves `sheetsChanged`, `cloudDriven`,
// `cloudsHolding`, `notOnSheet`, `notASheet`, `findings` and
// `revisionsRemaining` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A SHEET CARRIES REVISIONS TWO WAYS.
//
// A cloud drawn on the sheet - or on a view placed on it - puts its revision
// in the schedule by itself. A revision can also be TICKED on a sheet with no
// cloud anywhere - a whole-drawing issue - and that second list is the one
// written here. The cloud-driven ones cannot be removed through it.
//
// AND ONE REVISION CAN BE ON A SHEET BOTH WAYS AT ONCE. That is version 1's
// defect, FRAGMENT-ISSUES row 5b-1463: ticked AND clouded, the code took the
// tick off, saw the ticked list one shorter, and counted the sheet changed -
// while the sheet still printed the revision. So the answer is now judged on
// what the SHEET PRINTS after the write, `GetAllRevisionIds()`, never on the
// ticked list alone.
//
// ADD OR REMOVE, NEVER OVERWRITE THE LIST.
//
// Writing the list wholesale drops every earlier issue off the title block.
// REPLACE (`replaceWith`) is still not that: it takes ONE revision off and
// puts ONE on, sheet by sheet, and every other ticked revision stays.
//
// WRITING THIS LIST CAN PURGE AN UNUSED REVISION.
//
// A revision on no sheet and in no cloud can vanish project-wide the next time
// sheet revisions are written. That is Revit's behaviour, not this fragment's,
// and the count of revisions still in the project afterwards is how anybody
// would notice. A replace that takes A off its last sheet can do exactly that
// to A.

int sheetsChanged = 0;
var cloudDriven = new List<ElementId>();
var cloudsHolding = "";
var notOnSheet = new List<ElementId>();
var notASheet = new List<ElementId>();
var findings = new List<string>();
int revisionsRemaining = 0;

var wanted = new List<ElementId>();
if (revisionIds != null)
    foreach (var id in revisionIds)
        if (id != null && id != ElementId.InvalidElementId && !wanted.Contains(id)) wanted.Add(id);

// ---- which revision `replaceWith` names, if any ---------------------------
//
// Text, because only `revisionId`/`revisionIds` are resolved to a revision by
// the add-in. Read the way the Sheet Issues/Revisions dialog shows a revision:
// "Seq. 21", "21" (its sequence), its name, its description, its date, or its
// element id written out. More than one match is refused with the matches
// named, because descriptions and dates repeat across a job.

Func<Revision, string> label = r =>
    "Seq. " + r.SequenceNumber + " '" + (r.Description ?? "") + "' " + (r.RevisionDate ?? "");

var said = (replaceWith ?? "").Trim();
var replacing = said.Length > 0 && !string.Equals(said, "none", StringComparison.OrdinalIgnoreCase);
Revision replacement = null;
string refusal = null;

if (replacing)
{
    var text = said;
    if (text.StartsWith("Seq.", StringComparison.OrdinalIgnoreCase)) text = text.Substring(4).Trim();
    else if (text.StartsWith("Seq", StringComparison.OrdinalIgnoreCase)) text = text.Substring(3).Trim();

    var hits = new List<Revision>();
    foreach (var revisionId in Revision.GetAllRevisionIds(doc))
    {
        var r = doc.GetElement(revisionId) as Revision;
        if (r == null) continue;
        if (string.Equals(r.SequenceNumber.ToString(), text, StringComparison.Ordinal)
            || string.Equals(r.Id.ToString(), said, StringComparison.Ordinal)
            || string.Equals(r.Name ?? "", said, StringComparison.OrdinalIgnoreCase)
            || string.Equals(r.Description ?? "", said, StringComparison.OrdinalIgnoreCase)
            || string.Equals(r.RevisionDate ?? "", said, StringComparison.OrdinalIgnoreCase))
            hits.Add(r);
    }

    if (hits.Count == 1) replacement = hits[0];
    else if (hits.Count == 0)
        refusal = "replaceWith='" + said + "' names no revision in this project. Give its "
                + "sequence as the Sheet Issues/Revisions dialog shows it, like Seq. 21. "
                + "NOTHING WAS CHANGED.";
    else
    {
        var names = new List<string>();
        foreach (var r in hits) names.Add(label(r));
        refusal = "replaceWith='" + said + "' matches " + hits.Count + " revisions ("
                + string.Join("; ", names.ToArray()) + "), so it does not say which. Give "
                + "its sequence, like Seq. " + hits[0].SequenceNumber + ". NOTHING WAS CHANGED.";
    }

    if (replacement != null && wanted.Contains(replacement.Id))
        refusal = "replaceWith names " + label(replacement) + ", which is also the revision "
                + "being taken off - a swap needs two different revisions. NOTHING WAS CHANGED.";
    if (refusal == null && wanted.Count == 0)
        refusal = "replaceWith was given but no revision to take off. Say which revision "
                + "it replaces. NOTHING WAS CHANGED.";
    if (refusal == null && attach)
        findings.Add("replaceWith was given, so this is a swap: " + label(replacement)
                   + " goes on where the other comes off, and attach=true was not used.");
}

// ---- the clouds, read once and only if a sheet needs explaining -----------

List<RevisionCloud> allClouds = null;
Func<ViewSheet, ElementId, List<ElementId>> cloudsOn = (sheet, revisionId) =>
{
    if (allClouds == null)
    {
        allClouds = new List<RevisionCloud>();
        foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(RevisionCloud)))
        {
            var c = e as RevisionCloud;
            if (c != null) allClouds.Add(c);
        }
    }
    var held = new List<ElementId>();
    foreach (var c in allClouds)
    {
        if (c.RevisionId != revisionId) continue;
        ISet<ElementId> on = null;
        try { on = c.GetSheetIds(); } catch { on = null; }
        if ((on != null && on.Contains(sheet.Id)) || c.OwnerViewId == sheet.Id) held.Add(c.Id);
    }
    return held;
};

var holdingLines = new List<string>();

if (refusal != null)
{
    findings.Add(refusal);
}
else
{
    foreach (var element in elements)
    {
        var sheet = element as ViewSheet;
        if (sheet == null)
        {
            if (element != null) notASheet.Add(element.Id);
            continue;
        }

        ICollection<ElementId> current = null;
        ICollection<ElementId> printedBefore = null;
        try
        {
            current = sheet.GetAdditionalRevisionIds();
            printedBefore = sheet.GetAllRevisionIds();
        }
        catch (Exception failure)
        {
            findings.Add("sheet " + sheet.SheetNumber + " could not be read: " + failure.Message);
            continue;
        }

        var next = new List<ElementId>();
        if (current != null) next.AddRange(current);
        var printed = new List<ElementId>();
        if (printedBefore != null) printed.AddRange(printedBefore);

        bool touched = false;
        bool carriesA = false;

        foreach (var id in wanted)
        {
            var on = printed.Contains(id) || next.Contains(id);
            if (attach && !replacing)
            {
                if (next.Contains(id)) continue;   // already ticked: not a change
                next.Add(id);
                touched = true;
            }
            else
            {
                if (!on)
                {
                    // NEVER ON THIS SHEET. Version 1 called this cloud-driven,
                    // which sent the modeller looking for clouds that are not there.
                    if (!notOnSheet.Contains(sheet.Id)) notOnSheet.Add(sheet.Id);
                    continue;
                }
                carriesA = true;
                if (next.Remove(id)) touched = true;
            }
        }

        // THE SWAP: B goes on only where A was. A sheet that never carried A is
        // named in notOnSheet and left alone.
        if (replacing && carriesA && !printed.Contains(replacement.Id) && !next.Contains(replacement.Id))
        {
            next.Add(replacement.Id);
            touched = true;
        }

        if (touched)
        {
            try { sheet.SetAdditionalRevisionIds(next); }
            catch (Exception failure)
            {
                findings.Add("sheet " + sheet.SheetNumber + " refused the change: " + failure.Message);
                continue;
            }
        }

        // READ BACK WHAT THE SHEET PRINTS, NOT WHAT WAS SENT (row 5b-1463).
        ICollection<ElementId> printedAfter = null;
        try { printedAfter = sheet.GetAllRevisionIds(); } catch { printedAfter = null; }
        var after = new List<ElementId>();
        if (printedAfter != null) after.AddRange(printedAfter);

        bool asked = touched;
        foreach (var id in wanted)
        {
            if (attach && !replacing)
            {
                if (touched && !after.Contains(id)) asked = false;
                continue;
            }
            if (!after.Contains(id)) continue;

            // STILL PRINTED. A cloud holds it, whatever happened to the tick.
            asked = false;
            if (!cloudDriven.Contains(sheet.Id)) cloudDriven.Add(sheet.Id);
            var held = cloudsOn(sheet, id);
            var rev = doc.GetElement(id) as Revision;
            var ids = new List<string>();
            foreach (var c in held) ids.Add(c.ToString());
            holdingLines.Add("sheet " + sheet.SheetNumber + ": "
                + (rev == null ? "revision " + id : label(rev))
                + " still printed, held by " + (held.Count == 0
                    ? "a cloud this read could not find"
                    : held.Count + " cloud(s) " + string.Join(", ", ids.ToArray()))
                + ". EDIT_REVISION_CLOUDS moves or deletes them");
        }
        if (replacing && carriesA && !after.Contains(replacement.Id)) asked = false;

        if (asked) sheetsChanged++;
    }
}

cloudsHolding = string.Join(" || ", holdingLines.ToArray());

// How many revisions the project still has. Writing sheet revisions can purge
// one that is on no sheet and in no cloud, and this number is where that shows.
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Revision)))
    if (element != null) revisionsRemaining++;
