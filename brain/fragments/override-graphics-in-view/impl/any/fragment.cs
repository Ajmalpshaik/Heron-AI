// NOT STANDALONE. Assumes `view`, `elements` and `overrides` are in scope, and
// leaves `overridden` and `skipped` behind.
//
// ASSUMES AN OPEN TRANSACTION. Golden Rule 16 - the TransactionGroup belongs to
// the operation, so a grayout of six categories is ONE undo entry.
//
// THE VIEW IS CHECKED ONCE, NOT PER ELEMENT, AND IT IS CHECKED FIRST.
//
// SetElementOverrides throws on a view that cannot carry overrides - a
// schedule, a sheet, or a view whose template controls V/G. Catching that
// per element would turn one answerable refusal into one exception per element
// and a half-applied view, which is the state hardest to explain and hardest
// to undo.
//
// So the view is asked once. If it cannot take overrides, NOTHING is attempted
// and every element is reported skipped - a clean refusal the caller can turn
// into a sentence about the view template, rather than a partial result.
//
// AreGraphicsOverridesAllowed() is the call that knows about all three cases at
// once, including the template one, which is the case that actually happens in
// real projects: the user asks for the grayout, the view has a template, and
// nothing appears to happen.
//
// THIS REPLACES THE ELEMENT'S WHOLE OVERRIDE. IT DOES NOT MERGE.
//
// `SetElementOverrides` writes the settings object wholesale, so anything the
// element already had that this object does not carry is GONE - a colour set
// last week disappears when somebody sets halftone today. Nothing warns.
//
// It is the CALLER that has to know this, because the settings arrive already
// built (see the compatibility note for why). To change one aspect and keep the
// rest, read the element's current overrides with `view.GetElementOverrides(id)`,
// modify that object, and pass it here. Building a fresh
// OverrideGraphicSettings and setting one property on it is the mistake, and it
// looks completely reasonable in the code that makes it.

var overridden = 0;
var skipped = new List<ElementId>();

if (view == null || !view.AreGraphicsOverridesAllowed())
{
    // Every element, so the caller's count still adds up and "nothing happened"
    // is reported as a fact rather than inferred from a zero.
    foreach (var element in elements) skipped.Add(element.Id);
}
else
{
    foreach (var element in elements)
    {
        // An element from another document, or one already deleted, cannot be
        // overridden here. Reported rather than thrown: one bad id in a list of
        // four hundred must not lose the other three hundred and ninety-nine.
        if (element == null || !element.IsValidObject ||
            element.Document == null || !element.Document.Equals(view.Document))
        {
            if (element != null) skipped.Add(element.Id);
            continue;
        }

        view.SetElementOverrides(element.Id, overrides);
        overridden++;
    }
}
