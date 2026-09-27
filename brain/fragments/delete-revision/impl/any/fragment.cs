// NOT STANDALONE. Assumes `doc`, `elements` and `confirm` are in scope, and
// leaves `deleted`, `renumbered`, `revisionName`, `cloudsTouched`,
// `sheetsTouched`, `alsoDeleted` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One undo puts back both the
// revision and the numbering it disturbed.
//
// DELETING IS ARTICLE 7, SO IT NEEDS THE OWNER'S YES FOR THIS REVISION.
//
// `confirm` must name the revision being deleted - its sequence number as the
// dialog shows it ("2"), or Revit's own name for it ("Seq. 2 - Issued for
// Review"). A bare "yes" is refused, and so is a number that belongs to a
// different revision: a yes given for one revision must never delete another,
// and deleting renumbers everything after it, so yesterday's "2" is not
// today's. The caller passes it only after the owner has agreed to delete THAT
// revision; nothing here can ask him.
//
// ONE REVISION, HANDED IN BY A FIND, never the selection -
// SELECT_BY_PARAMETER_VALUE on the Revisions category, by Revision Sequence or
// by description.
//
// AN ISSUED REVISION IS REFUSED BY NAME. It records an issue that has gone
// out; removing it rewrites the history printed on those sheets. Un-issuing it
// first (EDIT_REVISION, issueState=unissue) is a separate decision.
//
// DELETING ONE RENUMBERS EVERY LATER REVISION.
//
// Delete 3 of 5 and the old 4 and 5 become 3 and 4. Every email and every
// printed drawing that said "revision 4" now names a different issue, and
// nothing in the model records the shift. So the numbering AFTER the delete is
// read back and every revision that moved is named.
//
// WHAT IT TOUCHED IS COUNTED BEFORE AND LOOKED AT AFTER. The clouds carrying
// it and the sheets showing it are listed first; afterwards each cloud is
// looked for again, and whatever Revit removed along with the revision is
// handed back. REVIT'S OWN REASON is reported when it refuses - the last
// revision in a project, or one still in use - never a reason guessed here.
// Some releases refuse without throwing, so the outcome is established by
// looking for the revision afterwards rather than by trusting the call.

var deleted = false;
var renumbered = "";
var revisionName = "";
var cloudsTouched = "";
var sheetsTouched = "";
var alsoDeleted = new List<ElementId>();
var refused = "";

{
    Func<string, string> quoted = value => string.IsNullOrEmpty(value) ? "(blank)" : "'" + value + "'";
    Func<Exception, string> revitSaid = failure => (failure.InnerException ?? failure).Message.TrimEnd('.');

    var handedIn = new List<Revision>();
    var notRevisions = 0;
    if (elements != null)
        foreach (var element in elements)
        {
            var candidate = element as Revision;
            if (candidate != null) handedIn.Add(candidate);
            else if (element != null) notRevisions++;
        }

    if (handedIn.Count == 0)
        refused = "No revision was handed in" + (notRevisions > 0
            ? " - " + notRevisions + " element(s) came in and none of them is a revision" : "")
            + ". Find it first with SELECT_BY_PARAMETER_VALUE, categories=Revisions, "
            + "parameterName=Revision Sequence. Nothing was deleted.";
    else if (handedIn.Count > 1)
    {
        var labels = new List<string>();
        foreach (var candidate in handedIn)
            labels.Add("Seq. " + candidate.SequenceNumber + " " + quoted(candidate.Description));
        refused = handedIn.Count + " revisions were handed in (" + string.Join(", ", labels.ToArray())
                + ") and this deletes ONE. Find it by Revision Sequence. Nothing was deleted.";
    }
    else
    {
        var target = handedIn[0];
        var targetId = target.Id;
        revisionName = "Seq. " + target.SequenceNumber + " " + quoted(target.Description);

        var said = (confirm ?? "").Trim();
        var confirmed = said.Length > 0
            && (said == target.SequenceNumber.ToString()
                || string.Equals(said, target.Name, StringComparison.OrdinalIgnoreCase));

        // WHAT IT CARRIES, counted before anything moves.
        var clouds = new List<RevisionCloud>();
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(RevisionCloud)))
        {
            var cloud = element as RevisionCloud;
            if (cloud != null && cloud.RevisionId == targetId) clouds.Add(cloud);
        }
        var sheetLines = new List<string>();
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)))
        {
            var sheet = element as ViewSheet;
            if (sheet == null || sheet.IsTemplate) continue;
            var all = sheet.GetAllRevisionIds();
            if (all == null || !all.Contains(targetId)) continue;
            var byHand = sheet.GetAdditionalRevisionIds();
            sheetLines.Add(sheet.SheetNumber + (byHand != null && byHand.Contains(targetId)
                ? " (ticked on the sheet by hand)" : " (from a cloud)"));
        }
        sheetsTouched = sheetLines.Count == 0
            ? "on no sheet"
            : "on " + sheetLines.Count + " sheet(s): " + string.Join(", ", sheetLines.ToArray());

        var cloudIds = new List<ElementId>();
        foreach (var cloud in clouds) cloudIds.Add(cloud.Id);
        cloudsTouched = clouds.Count == 0
            ? "no revision cloud"
            : clouds.Count + " revision cloud(s), ids "
              + string.Join(", ", cloudIds.Select(id => id.ToString()).ToArray());

        var before = new Dictionary<ElementId, int>();
        foreach (var id in Revision.GetAllRevisionIds(doc))
        {
            var revision = doc.GetElement(id) as Revision;
            if (revision == null) continue;
            before[id] = revision.SequenceNumber;
        }

        if (!confirmed)
            refused = "Deleting " + revisionName + " needs the owner's yes for THIS revision: pass "
                    + "confirm=" + target.SequenceNumber + " (or confirm=" + target.Name + ") only "
                    + "after he has agreed. " + (said.Length == 0 ? "No confirm was given"
                        : "confirm='" + said + "' does not name it")
                    + ". Nothing was deleted. It carries " + cloudsTouched + " and is "
                    + sheetsTouched + ".";
        else if (target.Issued)
            refused = revisionName + " is ISSUED - it records an issue that has gone out, and "
                    + "Heron does not delete one. Nothing was deleted. Un-issuing it first "
                    + "(EDIT_REVISION, issueState=unissue) is the owner's separate decision.";
        else
        {
            ICollection<ElementId> removed = null;
            var revitReason = "";
            try { removed = doc.Delete(targetId); }
            catch (Exception failure) { revitReason = revitSaid(failure); }

            // Established by looking, not by the call returning: some releases
            // refuse without throwing at all.
            deleted = doc.GetElement(targetId) == null;

            if (!deleted)
            {
                var count = Revision.GetAllRevisionIds(doc).Count;
                refused = "Revit did not delete " + revisionName + ". "
                        + (revitReason.Length > 0 ? "Revit said: " + revitReason
                           : "Revit kept it without giving a reason"
                             + (count == 1 ? " - it is the project's only revision" : ""))
                        + ". Nothing was deleted. It carries " + cloudsTouched + " and is "
                        + sheetsTouched + ".";
            }
            else
            {
                if (removed != null)
                    foreach (var id in removed)
                        if (id != targetId) alsoDeleted.Add(id);

                if (clouds.Count > 0)
                {
                    var gone = 0;
                    var moved = new List<string>();
                    foreach (var id in cloudIds)
                    {
                        var still = doc.GetElement(id) as RevisionCloud;
                        if (still == null) { gone++; continue; }
                        var now = doc.GetElement(still.RevisionId) as Revision;
                        moved.Add(id + " now on " + (now == null ? "no revision"
                            : "Seq. " + now.SequenceNumber + " " + quoted(now.Description)));
                    }
                    cloudsTouched += ". After the delete: " + gone + " deleted with it"
                                   + (moved.Count > 0 ? ", " + string.Join(", ", moved.ToArray()) : "");
                }

                // WHAT MOVED. Every revision whose number changed, old to new.
                var shifted = new List<string>();
                var nowList = new List<string>();
                foreach (var id in Revision.GetAllRevisionIds(doc))
                {
                    var revision = doc.GetElement(id) as Revision;
                    if (revision == null) continue;
                    nowList.Add("Seq. " + revision.SequenceNumber + " " + quoted(revision.Description));
                    int was;
                    if (before.TryGetValue(id, out was) && was != revision.SequenceNumber)
                        shifted.Add(quoted(revision.Description) + " was Seq. " + was
                                    + ", now Seq. " + revision.SequenceNumber);
                }
                renumbered = (shifted.Count == 0 ? "No other revision was renumbered. "
                              : "RENUMBERED: " + string.Join("; ", shifted.ToArray()) + ". ")
                           + "Now: " + string.Join(", ", nowList.ToArray());
            }
        }
    }
}
