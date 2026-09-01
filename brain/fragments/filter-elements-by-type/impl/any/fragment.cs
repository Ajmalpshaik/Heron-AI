// NOT STANDALONE. Assumes `doc`, `exemplar` and `inViewOnly` are in scope;
// leaves `elements` and `found` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// `inViewOnly` IS A VIEW OR NULL, AND THAT IS THE INSTRUCTION.
//
//   a view   only what that view contains - "the ones I can see"
//   null     the whole model - "all of them"
//
// It is an input rather than a default because the two answers differ by an
// order of magnitude on a real project, and choosing silently is how a change
// meant for one floor reaches nine. A bool would have been the same decision
// with less information; passing the view means the scope is the thing itself.
//
// SAME TYPE, NOT SAME FAMILY AND NOT SAME CATEGORY. Two 600x600 diffusers of
// different types are indistinguishable on a plan and are different products.
// Category sweeps up everything; family mixes the sizes.
//
// THE EXEMPLAR IS INCLUDED in the result, because "everything like this one"
// includes this one - and a caller passing the result to a change expects it
// to change too.

var elements = new List<Element>();

var typeId = exemplar == null ? ElementId.InvalidElementId : exemplar.GetTypeId();

if (typeId != null && typeId != ElementId.InvalidElementId)
{
    var collector = inViewOnly != null
        ? new FilteredElementCollector(doc, inViewOnly.Id)
        : new FilteredElementCollector(doc);

    foreach (var candidate in collector.WhereElementIsNotElementType())
    {
        if (candidate == null) continue;
        if (candidate.GetTypeId() != typeId) continue;
        elements.Add(candidate);
    }
}

var found = elements.Count;
