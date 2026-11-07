// NOT STANDALONE. Assumes `doc`, `elements`, `action`, `revisionId`,
// `fromRevision` and `confirm` are in scope; leaves `changed`, `deleted`,
// `alsoDeleted`, `sheetEffect`, `skipped` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one. One
// call is one entry in Revit's undo list.
//
// THE CLOUDS COME IN HANDED, NOT SEARCHED FOR. LIST_REVISION_CLOUDS finds them
// by sheet, view and revision - or the modeller selects them - and this acts on
// exactly those. That is what keeps ANOTHER SHEET'S clouds on the old revision
// untouched, which is the whole difference from MERGE_REVISION: merge moves
// every cloud of a revision on every sheet and deletes the revision.
//
// `fromRevision` IS A GUARD, NOT A SEARCH. Given, a handed cloud that is not on
// it is skipped and named - so a stale or too-wide hand-off cannot move a
// cloud nobody meant.
//
// MOVE (`action=reassign`): every handed cloud on `revisionId`. A cloud
// already on it is named in `skipped`, not counted.
//
// DELETE (`action=delete`) IS DESTRUCTIVE (Article 7), so `confirm` must be
// the NUMBER of clouds that will go - the count the modeller was shown and
// agreed to. Anything else deletes nothing. Its tags go with it, and are named.
//
// WHAT THE SHEETS PRINT, BEFORE AND AFTER. A cloud is why a revision is on a
// sheet; moving or deleting the last one can take that revision off the
// sheet's revision block, and putting one on adds its revision. Every sheet
// the clouds print on is read before and after, and every revision that came
// or went is named in `sheetEffect` - the line the modeller checks a title
// block against.

var changed = new List<ElementId>();
var deleted = new List<ElementId>();
var alsoDeleted = new List<ElementId>();
var sheetEffect = "";
var skipped = "";
var refused = "";

Func<string, string> quoted = text => string.IsNullOrEmpty(text) ? "(blank)" : "'" + text + "'";
Func<Revision, string> label = r =>
    "Seq. " + r.SequenceNumber + " " + quoted(r.Description) + " " + quoted(r.RevisionDate);

var verb = (action ?? "").Trim().ToLowerInvariant();
if (verb == "move" || verb == "reassign" || verb == "change" || verb == "set") verb = "reassign";
if (verb == "remove" || verb == "erase") verb = "delete";

var skippedLines = new List<string>();
var problems = new List<string>();

// ---- the clouds handed in ---------------------------------------------------
var clouds = new List<RevisionCloud>();
var notACloud = 0;
if (elements != null)
    foreach (var e in elements)
    {
        if (e == null) continue;
        var c = e as RevisionCloud;
        if (c == null) { notACloud++; continue; }
        if (!clouds.Exists(x => x.Id == c.Id)) clouds.Add(c);
    }
if (notACloud > 0) skippedLines.Add(notACloud + " element(s) handed in are not revision clouds and were left alone");

// ---- the guard: fromRevision ------------------------------------------------
var fromSaid = (fromRevision ?? "").Trim();
if (fromSaid.Length > 0 && !string.Equals(fromSaid, "none", StringComparison.OrdinalIgnoreCase))
{
    var bare = fromSaid;
    if (bare.StartsWith("Seq.", StringComparison.OrdinalIgnoreCase)) bare = bare.Substring(4).Trim();
    else if (bare.StartsWith("Seq", StringComparison.OrdinalIgnoreCase)) bare = bare.Substring(3).Trim();
    var hits = new List<Revision>();
    foreach (var id in Revision.GetAllRevisionIds(doc))
    {
        var r = doc.GetElement(id) as Revision;
        if (r == null) continue;

        // THE NUMBER IT PRINTS COUNTS TOO. "revision 21" may mean the sequence or
        // the printed number; where they point at two revisions the match below
        // finds both and is refused with both named. "Seq. 21" is the sequence
        // alone. RevisionNumber throws under Per Sheet numbering.
        string printedNumber = null;
        if (!fromSaid.StartsWith("Seq", StringComparison.OrdinalIgnoreCase))
        {
            try { printedNumber = r.RevisionNumber; } catch { printedNumber = null; }
        }
        if ((printedNumber != null && string.Equals(printedNumber, fromSaid, StringComparison.OrdinalIgnoreCase))
            || string.Equals(r.SequenceNumber.ToString(), bare, StringComparison.Ordinal)
            || string.Equals(r.Id.ToString(), fromSaid, StringComparison.Ordinal)
            || string.Equals(r.Name ?? "", fromSaid, StringComparison.OrdinalIgnoreCase)
            || string.Equals(r.Description ?? "", fromSaid, StringComparison.OrdinalIgnoreCase)
            || string.Equals(r.RevisionDate ?? "", fromSaid, StringComparison.OrdinalIgnoreCase))
            hits.Add(r);
    }
    if (hits.Count != 1)
    {
        var names = new List<string>();
        foreach (var r in hits) names.Add(label(r));
        problems.Add("fromRevision='" + fromSaid + "' " + (hits.Count == 0
            ? "names no revision in this project"
            : "matches " + hits.Count + " revisions (" + string.Join("; ", names.ToArray()) + ")")
            + ". Give its sequence, like Seq. 20");
    }
    else
    {
        var from = hits[0];
        var kept = new List<RevisionCloud>();
        var off = 0;
        foreach (var c in clouds) { if (c.RevisionId == from.Id) kept.Add(c); else off++; }
        if (off > 0) skippedLines.Add(off + " cloud(s) handed in are not on " + label(from) + " and were left alone");
        clouds = kept;
    }
}

// ---- the request -------------------------------------------------------------
var target = verb == "reassign" ? doc.GetElement(revisionId) as Revision : null;

if (verb != "reassign" && verb != "delete")
    problems.Add("action='" + (action ?? "") + "' is not one this does. Say reassign (move the clouds "
               + "to another revision) or delete");
if (verb == "reassign" && target == null)
    problems.Add("no revision to move the clouds to - revisionId names none. CREATE_REVISION makes one; "
               + "this never invents one");
if (problems.Count == 0 && clouds.Count == 0)
    problems.Add("no revision cloud to act on. LIST_REVISION_CLOUDS finds them by sheet, view and revision, "
               + "or select them in Revit");
if (problems.Count == 0 && verb == "delete")
{
    var said = (confirm ?? "").Trim();
    if (said != clouds.Count.ToString())
        problems.Add("delete removes " + clouds.Count + " revision cloud(s) and their tags. Run it with "
                   + "confirm=" + clouds.Count + " only after the modeller has agreed. " + (said.Length == 0
                       || string.Equals(said, "none", StringComparison.OrdinalIgnoreCase)
                       ? "No confirm was given" : "confirm='" + said + "' is not that count"));
}

if (problems.Count > 0)
{
    refused = string.Join(" || ", problems.ToArray()) + ". NOTHING WAS CHANGED.";
}
else
{
    // ---- the sheets, read BEFORE --------------------------------------------
    var sheetIds = new HashSet<ElementId>();
    foreach (var c in clouds)
    {
        try { foreach (var id in c.GetSheetIds()) sheetIds.Add(id); } catch { }
        if (doc.GetElement(c.OwnerViewId) is ViewSheet) sheetIds.Add(c.OwnerViewId);
    }
    var before = new Dictionary<ElementId, HashSet<ElementId>>();
    foreach (var id in sheetIds)
    {
        var s = doc.GetElement(id) as ViewSheet;
        if (s == null) continue;
        try { before[id] = new HashSet<ElementId>(s.GetAllRevisionIds()); } catch { }
    }

    if (verb == "reassign")
    {
        var failures = new List<string>();
        foreach (var c in clouds)
        {
            if (c.RevisionId == target.Id) { skippedLines.Add("cloud " + c.Id + " is already on " + label(target)); continue; }
            var was = doc.GetElement(c.RevisionId) as Revision;
            try { c.RevisionId = target.Id; }
            catch (Exception failure)
            {
                failures.Add("cloud " + c.Id + " (" + (was == null ? "no revision" : label(was)
                    + (was.Issued ? ", ISSUED" : "")) + "): Revit refused - " + failure.Message);
                continue;
            }
            if (c.RevisionId == target.Id) changed.Add(c.Id);
            else failures.Add("cloud " + c.Id + ": the revision did not read back as " + label(target));
        }
        // ALL OR NOTHING: a part-moved set is the one outcome nobody can check
        // by eye, so one failure throws and the whole call is rolled back.
        if (failures.Count > 0)
            throw new InvalidOperationException("Not every cloud moved, so nothing is kept: "
                + string.Join(" || ", failures.ToArray())
                + (target.Issued ? ". " + label(target) + " is ISSUED - EDIT_REVISION issueState=unissue first" : ""));
    }
    else
    {
        var ids = new List<ElementId>();
        foreach (var c in clouds) ids.Add(c.Id);
        var gone = doc.Delete(ids);
        if (gone != null)
            foreach (var id in gone)
            {
                if (ids.Contains(id)) deleted.Add(id);
                else alsoDeleted.Add(id);
            }
        if (deleted.Count != ids.Count)
            throw new InvalidOperationException("Revit deleted " + deleted.Count + " of the " + ids.Count
                + " clouds, so nothing is kept");
    }

    // ---- the sheets, read AFTER ---------------------------------------------
    var effects = new List<string>();
    var ordered = new List<ViewSheet>();
    foreach (var id in before.Keys) { var s = doc.GetElement(id) as ViewSheet; if (s != null) ordered.Add(s); }
    ordered.Sort((a, b) => string.Compare(a.SheetNumber ?? "", b.SheetNumber ?? "", StringComparison.OrdinalIgnoreCase));
    foreach (var s in ordered)
    {
        HashSet<ElementId> now;
        try { now = new HashSet<ElementId>(s.GetAllRevisionIds()); } catch { continue; }
        var came = new List<string>();
        var went = new List<string>();
        foreach (var id in now) if (!before[s.Id].Contains(id)) { var r = doc.GetElement(id) as Revision; came.Add(r == null ? id.ToString() : label(r)); }
        foreach (var id in before[s.Id]) if (!now.Contains(id)) { var r = doc.GetElement(id) as Revision; went.Add(r == null ? id.ToString() : label(r)); }
        var current = doc.GetElement(s.GetCurrentRevision()) as Revision;
        effects.Add("sheet " + s.SheetNumber + ": "
            + (came.Count == 0 && went.Count == 0 ? "revision block unchanged"
               : (came.Count > 0 ? "now prints " + string.Join(", ", came.ToArray()) : "")
                 + (came.Count > 0 && went.Count > 0 ? "; " : "")
                 + (went.Count > 0 ? "no longer prints " + string.Join(", ", went.ToArray()) : ""))
            + "; current " + (current == null ? "none" : label(current)));
    }
    sheetEffect = string.Join(" || ", effects.ToArray());
}

skipped = string.Join(" || ", skippedLines.ToArray());
