// NOT STANDALONE. Assumes `doc`, `elements` and `targetViewId` are in scope,
// and leaves `repointed`, `alreadyThere`, `referencesNothing` and `refused`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). A sheet's marks, one undo.
//
// THE TEXT IN A BUBBLE IS NOT ON THE MARK.
//
// A mark prints the detail and sheet number of the VIEW it references, so
// changing what it says means changing which view that is. There is nothing on
// the mark to retype, which is why "the bubble is wrong" so often turns into
// an afternoon spent looking at the wrong element.
//
// THE TARGET IS CHECKED ONCE, NOT PER MARK.
//
// A view that cannot be referenced fails every mark identically, and finding
// that out forty times over is forty identical refusals in the report. It is
// asked once, before anything is attempted.
//
// A MARK ALREADY POINTING AT THE TARGET IS NOT A CHANGE.
//
// Counting it would report forty re-pointed when four moved, which is the
// number somebody checks the drawing against.

int repointed = 0;
var alreadyThere = new List<ElementId>();
var referencesNothing = new List<ElementId>();
var refused = new List<ElementId>();

var target = doc.GetElement(targetViewId) as View;

if (target == null)
{
    // Nothing is attempted, and every element is refused BY NAME rather than
    // the batch silently doing nothing.
    foreach (var element in elements) if (element != null) refused.Add(element.Id);
}
else
{
    foreach (var element in elements)
    {
        if (element == null) continue;

        ElementId before = ElementId.InvalidElementId;
        try { before = ReferenceableViewUtils.GetReferencedViewId(doc, element.Id); }
        catch { }

        // An element that references no view is not a marker at all - a
        // dimension, a tag, a detail line. It is a fact about the element and
        // the rest of the selection still has to be done.
        if (before == null || before == ElementId.InvalidElementId)
        {
            referencesNothing.Add(element.Id);
            continue;
        }

        if (before == targetViewId) { alreadyThere.Add(element.Id); continue; }

        try { ReferenceableViewUtils.ChangeReferencedView(doc, element.Id, targetViewId); }
        catch { refused.Add(element.Id); continue; }

        // What it points at NOW. The call can leave a mark where it was, and
        // a count of what was asked for is not evidence of anything.
        ElementId after = ElementId.InvalidElementId;
        try { after = ReferenceableViewUtils.GetReferencedViewId(doc, element.Id); }
        catch { }

        if (after == targetViewId) repointed++;
        else refused.Add(element.Id);
    }
}
