// NOT STANDALONE. Assumes `doc`, `elements` and `direction` are in scope, and
// leaves `moved`, `order`, `revisionName` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One undo puts the order back.
//
// THE MOVE UP / MOVE DOWN BUTTONS OF SHEET ISSUES/REVISIONS. `direction` is
// "up" or "down" - one row, as the buttons do - or "first" or "last".
//
// ONE REVISION, HANDED IN BY A FIND, never the selection -
// SELECT_BY_PARAMETER_VALUE on the Revisions category, by Revision Sequence or
// by description. Two found is refused with both named.
//
// MOVING A REVISION RENUMBERS IT AND EVERY REVISION IT PASSES. Revit's own API
// documentation says so, and says it happens to issued revisions too. An
// issued revision's number is printed on drawings that have already gone out,
// so a move that would change the number of ANY issued revision - the one
// moved or one it passes - is refused, naming each. Revit would allow it;
// Heron treats an issued revision as locked, and un-issuing is a separate,
// explicit decision (EDIT_REVISION, issueState=unissue).
//
// THE ORDER IS READ BACK. Revit takes the whole new sequence at once; the
// order it holds afterwards is read and compared, and the before and after are
// both handed back. A move that does not read back where it was asked throws,
// and the add-in rolls it back.

var moved = false;
var order = "";
var revisionName = "";
var refused = "";

{
    Func<string, string> quoted = value => string.IsNullOrEmpty(value) ? "(blank)" : "'" + value + "'";
    Func<Exception, string> revitSaid = failure => (failure.InnerException ?? failure).Message.TrimEnd('.');
    Func<IList<ElementId>, string> listed = ids =>
    {
        var names = new List<string>();
        foreach (var id in ids)
        {
            var revision = doc.GetElement(id) as Revision;
            if (revision != null)
                names.Add("Seq. " + revision.SequenceNumber + " " + quoted(revision.Description)
                          + (revision.Issued ? " (ISSUED)" : ""));
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

    if (way != "up" && way != "down" && way != "first" && way != "last")
        refused = "direction takes 'up' or 'down' - the dialog's Move Up and Move Down, one row - "
                + "or 'first' or 'last', not '" + direction + "'. Nothing moved.";
    else if (handedIn.Count == 0)
        refused = "No revision was handed in" + (notRevisions > 0
            ? " - " + notRevisions + " element(s) came in and none of them is a revision" : "")
            + ". Find it first with SELECT_BY_PARAMETER_VALUE, categories=Revisions, "
            + "parameterName=Revision Sequence. Nothing moved.";
    else if (handedIn.Count > 1)
    {
        var labels = new List<string>();
        foreach (var candidate in handedIn)
            labels.Add("Seq. " + candidate.SequenceNumber + " " + quoted(candidate.Description));
        refused = handedIn.Count + " revisions were handed in (" + string.Join(", ", labels.ToArray())
                + ") and this moves ONE. Find it by Revision Sequence. Nothing moved.";
    }
    else
    {
        var target = handedIn[0];
        revisionName = "Seq. " + target.SequenceNumber + " " + quoted(target.Description);

        var current = new List<ElementId>(Revision.GetAllRevisionIds(doc));
        var from = current.IndexOf(target.Id);
        var to = way == "up" ? from - 1 : way == "down" ? from + 1 : way == "first" ? 0 : current.Count - 1;

        var before = listed(current);

        if (from < 0)
            refused = revisionName + " is not in the project's revision list. Nothing moved.";
        else if (to < 0 || to >= current.Count)
            refused = revisionName + " is already " + (to < 0 ? "the first" : "the last")
                    + " revision, so it cannot move " + way + ". Nothing moved. Order: " + before + ".";
        else if (to == from)
            order = revisionName + " is already " + way + "; nothing moved. Order: " + before + ".";
        else
        {
            // EVERY REVISION WHOSE NUMBER WOULD CHANGE: the one moved and each
            // one it passes.
            var low = Math.Min(from, to);
            var high = Math.Max(from, to);
            var issued = new List<string>();
            for (var i = low; i <= high; i++)
            {
                var revision = doc.GetElement(current[i]) as Revision;
                if (revision != null && revision.Issued)
                    issued.Add("Seq. " + revision.SequenceNumber + " " + quoted(revision.Description));
            }

            var wanted = new List<ElementId>(current);
            wanted.RemoveAt(from);
            wanted.Insert(to, target.Id);

            if (issued.Count > 0)
                refused = "Moving " + revisionName + " " + way + " would renumber "
                        + string.Join(", ", issued.ToArray()) + ", which "
                        + (issued.Count == 1 ? "is" : "are") + " ISSUED - that number is printed on "
                        + "drawings already sent out. Nothing moved. Order: " + before + ".";
            else
            {
                var revitReason = "";
                try { Revision.ReorderRevisionSequence(doc, wanted); }
                catch (Exception failure) { revitReason = revitSaid(failure); }

                var after = new List<ElementId>(Revision.GetAllRevisionIds(doc));
                var landed = after.IndexOf(target.Id);

                if (revitReason.Length > 0 && landed == from)
                {
                    // THE ORDER IT WOULD HAVE MADE, by position - the sequence
                    // numbers Revit would have given them, not the ones they hold.
                    var would = new List<string>();
                    for (var i = 0; i < wanted.Count; i++)
                    {
                        var revision = doc.GetElement(wanted[i]) as Revision;
                        if (revision != null) would.Add((i + 1) + ". " + quoted(revision.Description));
                    }
                    refused = "Revit would not move " + revisionName + " " + way + ": " + revitReason
                            + ". Nothing moved. It would have gone from position " + (from + 1)
                            + " to " + (to + 1) + ", giving " + string.Join(", ", would.ToArray()) + ".";
                }
                else if (landed != to)
                    throw new InvalidOperationException(
                        "REORDER_REVISION asked Revit to put " + revisionName + " at position "
                        + (to + 1) + " and it reads back at " + (landed + 1)
                        + ", so nothing is kept." + (revitReason.Length > 0 ? " Revit said: " + revitReason : ""));
                else
                {
                    moved = true;
                    order = "Before: " + before + ". After: " + listed(after) + ".";
                }
            }
        }
    }
}
