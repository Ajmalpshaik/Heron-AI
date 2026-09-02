// NOT STANDALONE. Assumes `doc` and `uidoc` are in scope; leaves `elements`,
// `selectionCount` and `vanished` behind.
//
// READ ONLY. Opens no transaction, needs none, and does not touch the
// selection it is reading. That separation is the whole reason this fragment
// is not part of SET_SELECTION.
//
// THE IDS ARE A SNAPSHOT, NOT A LIVE VIEW. GetElementIds() hands back a copy of
// what was picked at the instant it was called. The user can click somewhere
// else a moment later and this list will not follow - which is correct, and is
// why anything acting on the result should act on THIS list rather than asking
// again halfway through.
//
// AN ID IN THE SELECTION CAN POINT AT NOTHING. Revit keeps a deleted element
// selected until the selection is refreshed, and an undo can empty an id out
// from under it. doc.GetElement() then returns null. Those are collected into
// `vanished` rather than dropped in silence: acting on four elements when the
// user picked five is exactly the kind of quiet shortfall that gets noticed
// after the drawing is issued.
//
// AN EMPTY SELECTION STAYS EMPTY. It does NOT fall back to the whole model, to
// the active view, or to anything else. "Do it to what I selected" with nothing
// selected must do nothing at all - the alternative is a fragment that moves
// every duct in the building because a click missed.

var picked = uidoc.Selection.GetElementIds();

var selectionCount = picked.Count;
var vanished = new List<ElementId>();
var elements = new List<Element>();

foreach (var id in picked)
{
    if (id == null || id == ElementId.InvalidElementId) continue;

    var element = doc.GetElement(id);

    // Null means the id no longer resolves in this document - deleted, undone,
    // or belonging to a link. Recorded, never guessed at.
    if (element == null) { vanished.Add(id); continue; }

    elements.Add(element);
}
