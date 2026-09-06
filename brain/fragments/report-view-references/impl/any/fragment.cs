// NOT STANDALONE. Assumes `doc` and `elements` are in scope, and leaves
// `referencedViews`, `viewNames`, `notOnASheet` and `referencesNothing`
// behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE RELATIONSHIP IS NOT A PARAMETER, WHICH IS WHY THIS EXISTS.
//
// A section mark in a plan is one element; the section view it opens is a
// different one; and the numbers printed in the bubble come from the VIEW, not
// from the mark. Nothing on the mark says which view that is - reading its
// parameters finds text that was never written there. The utility below is the
// only public way to ask, and it takes the marker's id rather than the view's.
//
// AN EMPTY BUBBLE IS A VIEW THAT IS ON NO SHEET.
//
// The mark draws a detail number and a sheet number from its view; a view that
// has not been placed has neither, so the bubble prints blank. It looks like a
// broken mark and it is a view waiting to be placed - a different fix, and the
// reason the two are separate lists here.

var referencedViews = new Dictionary<ElementId, ElementId>();
var viewNames = new Dictionary<ElementId, string>();
var notOnASheet = new List<ElementId>();
var referencesNothing = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;

    ElementId referenced = ElementId.InvalidElementId;
    try { referenced = ReferenceableViewUtils.GetReferencedViewId(doc, element.Id); }
    catch { }

    // Most elements reference no view at all - a detail line, a dimension, a
    // tag. That is a fact about the element, not a failure, and the caller
    // usually handed in a whole selection.
    if (referenced == null || referenced == ElementId.InvalidElementId)
    {
        referencesNothing.Add(element.Id);
        continue;
    }

    var target = doc.GetElement(referenced) as View;
    if (target == null) { referencesNothing.Add(element.Id); continue; }

    referencedViews[element.Id] = referenced;
    try { viewNames[element.Id] = target.Name ?? ""; } catch { viewNames[element.Id] = ""; }

    // On a sheet or not. The view's own sheet number is blank until it is
    // placed, and blank is exactly what the bubble then prints.
    string sheetNumber = null;
    try
    {
        var parameter = target.get_Parameter(BuiltInParameter.VIEWPORT_SHEET_NUMBER);
        if (parameter != null && parameter.HasValue) sheetNumber = parameter.AsString();
    }
    catch { }

    if (string.IsNullOrEmpty(sheetNumber)) notOnASheet.Add(element.Id);
}
