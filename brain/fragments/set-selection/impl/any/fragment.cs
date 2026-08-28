// NOT STANDALONE. Assumes `uidoc` and `elements` are already in scope, and
// leaves `selectedCount` for whatever reports the result.
//
// Reads nothing back from Revit's own selection on purpose: the caller asked
// for these elements to be selected, and re-reading to confirm would be
// testing Revit rather than this fragment.

var ids = elements.Select(e => e.Id).ToList();
uidoc.Selection.SetElementIds(ids);
var selectedCount = ids.Count;
