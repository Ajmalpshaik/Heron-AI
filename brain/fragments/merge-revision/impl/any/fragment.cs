// NOT STANDALONE. Assumes `doc`, `elements`, `direction` and `confirm` are in
// scope, and leaves `merged`, `cloudsMoved`, `mergedInto`, `sheetsTouched`,
// `order`, `revisionName` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One undo puts back the
// revision, its clouds and the numbering.
//
// THE MERGE UP / MERGE DOWN BUTTONS OF SHEET ISSUES/REVISIONS. The revision
// handed in is REMOVED: its clouds and tags go to the revision above it
// ("up") or below it ("down"), and it is deleted. Revit's own
// Revision.CombineWithPrevious / CombineWithNext do the work and return the
// clouds they moved.
//
// IT REMOVES A REVISION, SO IT IS ARTICLE 7 AND NEEDS THE OWNER'S YES FOR
// THAT REVISION. `confirm` must name the revision being merged away - its
// sequence number ("2") or Revit's "Seq. 2 - description". A bare "yes" or a
// number belonging to another revision is refused, and nothing changes.
//
// ONE REVISION, HANDED IN BY A FIND, never the selection -
// SELECT_BY_PARAMETER_VALUE on the Revisions category.
//
// BOTH MUST BE UNISSUED - Revit's rule, stated in its API documentation - and
// the refusal names whichever is issued before Revit is asked.
//
// READ BACK: the removed revision is looked for and must be gone, and every
// cloud Revit says it moved must now carry the revision it was merged into.
// Anything else throws, and the add-in rolls the whole merge back. The sheets
// that showed the removed revision are listed before, with what each shows
// after.

var merged = false;
var cloudsMoved = new List<ElementId>();
var mergedInto = "";
var sheetsTouched = "";
var order = "";
var revisionName = "";
var refused = "";

{
    Func<string, string> quoted = value => string.IsNullOrEmpty(value) ? "(blank)" : "'" + value + "'";
    Func<Exception, string> revitSaid = failure => (failure.InnerException ?? failure).Message.TrimEnd('.');
    Func<Revision, string> named = r => "Seq. " + r.SequenceNumber + " " + quoted(r.Description);
    Func<string> listed = () =>
    {
        var names = new List<string>();
        foreach (var id in Revision.GetAllRevisionIds(doc))
        {
            var revision = doc.GetElement(id) as Revision;
            if (revision != null) names.Add(named(revision) + (revision.Issued ? " (ISSUED)" : ""));
        }
        return string.Join(", ", names.ToArray());
    };

    var handedIn = new List<Revision>();
    var notRevisions = 0;
    if (elements != null)
        foreach (var element in elements)
        {
            var candidate = element as Revision;
            if (candidate != null) handedIn.Add(candidate);
            else if (element != null) notRevisions++;
        }

    var way = (direction ?? "").Trim().ToLowerInvariant();

    if (way != "up" && way != "down")
        refused = "direction takes 'up' (merge into the revision above - the dialog's Merge Up) "
                + "or 'down' (into the one below - Merge Down), not '" + direction + "'. Nothing changed.";
    else if (handedIn.Count == 0)
        refused = "No revision was handed in" + (notRevisions > 0
            ? " - " + notRevisions + " element(s) came in and none of them is a revision" : "")
            + ". Find it first with SELECT_BY_PARAMETER_VALUE, categories=Revisions, "
            + "parameterName=Revision Sequence. Nothing changed.";
    else if (handedIn.Count > 1)
    {
        var labels = new List<string>();
        foreach (var candidate in handedIn) labels.Add(named(candidate));
        refused = handedIn.Count + " revisions were handed in (" + string.Join(", ", labels.ToArray())
                + ") and this merges ONE away. Find it by Revision Sequence. Nothing changed.";
    }
    else
    {
        var target = handedIn[0];
        var targetId = target.Id;
        revisionName = named(target);
        order = "Before: " + listed() + ".";

        var all = new List<ElementId>(Revision.GetAllRevisionIds(doc));
        var at = all.IndexOf(targetId);
        var neighbourAt = way == "up" ? at - 1 : at + 1;
        var neighbour = neighbourAt >= 0 && neighbourAt < all.Count
            ? doc.GetElement(all[neighbourAt]) as Revision : null;

        // WHAT IT CARRIES, before anything moves.
        var clouds = new List<ElementId>();
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(RevisionCloud)))
        {
            var cloud = element as RevisionCloud;
            if (cloud != null && cloud.RevisionId == targetId) clouds.Add(cloud.Id);
        }
        var sheets = new List<ViewSheet>();
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)))
        {
            var sheet = element as ViewSheet;
            if (sheet == null || sheet.IsTemplate) continue;
            var shown = sheet.GetAllRevisionIds();
            if (shown != null && shown.Contains(targetId)) sheets.Add(sheet);
        }
        var carries = clouds.Count + " revision cloud(s), and it shows on " + sheets.Count + " sheet(s)";

        var said = (confirm ?? "").Trim();
        var confirmed = said.Length > 0
            && (said == target.SequenceNumber.ToString()
                || string.Equals(said, target.Name, StringComparison.OrdinalIgnoreCase));

        if (neighbour == null)
            refused = revisionName + " is the " + (way == "up" ? "first" : "last")
                    + " revision, so there is nothing " + (way == "up" ? "above" : "below")
                    + " it to merge into. Nothing changed.";
        else if (!confirmed)
            refused = "Merging " + revisionName + " " + way + " into " + named(neighbour)
                    + " REMOVES " + revisionName + ", so it needs the owner's yes for it: pass confirm="
                    + target.SequenceNumber + " (or confirm=" + target.Name + ") only after he has "
                    + "agreed. " + (said.Length == 0 ? "No confirm was given"
                        : "confirm='" + said + "' does not name it")
                    + ". Nothing changed. It carries " + carries + ".";
        else if (target.Issued || neighbour.Issued)
            refused = "Revit merges only two UNISSUED revisions, and "
                    + (target.Issued && neighbour.Issued ? revisionName + " and " + named(neighbour) + " are"
                       : (target.Issued ? revisionName : named(neighbour)) + " is")
                    + " ISSUED. Nothing changed.";
        else
        {
            var neighbourId = neighbour.Id;
            mergedInto = named(neighbour);

            ISet<ElementId> returned = null;
            var revitReason = "";
            try
            {
                returned = way == "up"
                    ? Revision.CombineWithPrevious(doc, targetId)
                    : Revision.CombineWithNext(doc, targetId);
            }
            catch (Exception failure) { revitReason = revitSaid(failure); }

            var gone = doc.GetElement(targetId) == null;

            if (!gone && revitReason.Length > 0)
                refused = "Revit would not merge " + revisionName + " " + way + " into " + mergedInto
                        + ": " + revitReason + ". Nothing changed. It carries " + carries + ".";
            else if (!gone)
                throw new InvalidOperationException(
                    "MERGE_REVISION: Revit returned from the merge and " + revisionName
                    + " is still there, so nothing is kept.");
            else
            {
                if (returned != null) cloudsMoved.AddRange(returned);

                // EVERY CLOUD REVIT SAYS IT MOVED NOW CARRIES THE NEIGHBOUR.
                var astray = new List<string>();
                foreach (var id in cloudsMoved)
                {
                    var cloud = doc.GetElement(id) as RevisionCloud;
                    if (cloud == null || cloud.RevisionId != neighbourId) astray.Add(id.ToString());
                }
                if (astray.Count > 0)
                    throw new InvalidOperationException(
                        "MERGE_REVISION: cloud(s) " + string.Join(", ", astray.ToArray())
                        + " do not read back on " + mergedInto + ", so nothing is kept.");

                merged = true;

                var sheetLines = new List<string>();
                foreach (var sheet in sheets)
                {
                    var shown = sheet.GetAllRevisionIds();
                    sheetLines.Add(sheet.SheetNumber + (shown != null && shown.Contains(neighbourId)
                        ? " now shows " + mergedInto : " no longer shows either"));
                }
                sheetsTouched = sheets.Count == 0
                    ? "it was on no sheet"
                    : string.Join(", ", sheetLines.ToArray());

                var after = doc.GetElement(neighbourId) as Revision;
                if (after != null) mergedInto = named(after) + " (was " + mergedInto + ")";
                order += " After: " + listed() + ".";
            }
        }
    }
}
