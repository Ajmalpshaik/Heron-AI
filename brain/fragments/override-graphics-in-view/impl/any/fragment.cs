// NOT STANDALONE. Assumes `view`, `elements` and `overrides` are in scope, and
// leaves `overridden`, `skipped` and `wrapsFollowed` behind.
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
//
// ===========================================================================
// INSULATION AND LINING TAKE THE LOOK OF THE RUN THEY WRAP.
// ===========================================================================
//
// The owner's standing rule, and HIGHLIGHT_VS_REST has followed it since it was
// written: make a duct red and leave its insulation alone, and the red sits
// inside a jacket of the old colour. Until 2026-09-23 this fragment overrode
// the run and not the wrap. Now every element it overrides passes the SAME
// settings to its own insulation and lining, where the view shows them.
//
// ONE DIRECTION ONLY: RUN TO WRAP, and that is deliberate. A look handed to
// the WRAPS alone - a grayout's quiet grey insulation layer is exactly that -
// must not drag the run along, so a wrap handed in never pulls in its run.
//
// SO THE ORDER NOW MATTERS IN A GRAYOUT. Bringing the services forward through
// this fragment brings their insulation forward with them, and an element
// override beats a category one. A job that wants the insulation to keep a
// look of its own gives the insulation that look AFTER the services, through
// this fragment - not before, and not by category.
//
// GetInsulationIds and GetLiningIds THROW for an element that cannot be
// wrapped, so catching per element IS the test, as in HIGHLIGHT_VS_REST. A wrap
// the view will not take is passed over rather than allowed to lose the rest.

var overridden = 0;
var skipped = new List<ElementId>();
var wrapsFollowed = 0;

if (view == null || !view.AreGraphicsOverridesAllowed())
{
    // Every element, so the caller's count still adds up and "nothing happened"
    // is reported as a fact rather than inferred from a zero.
    foreach (var element in elements) skipped.Add(element.Id);
}
else
{
    var document = view.Document;

    // What was handed in. A wrap that is here on its own takes the look as one
    // of the elements, and is counted there rather than as a follower.
    var handedIds = new HashSet<ElementId>();
    foreach (var element in elements) if (element != null) handedIds.Add(element.Id);

    // What the view shows, asked for only once a wrap turns up. A wrap outside
    // the view has nothing to override, and counting it would pad the report.
    HashSet<ElementId> shownInView = null;
    var wrapsDone = new HashSet<ElementId>();

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

        // The run's insulation and lining take the same look. See the header.
        var wraps = new List<ElementId>();
        try { foreach (var wrapId in InsulationLiningBase.GetInsulationIds(document, element.Id)) wraps.Add(wrapId); }
        catch { }
        try { foreach (var wrapId in InsulationLiningBase.GetLiningIds(document, element.Id)) wraps.Add(wrapId); }
        catch { }

        foreach (var wrapId in wraps)
        {
            if (handedIds.Contains(wrapId) || !wrapsDone.Add(wrapId)) continue;

            if (shownInView == null)
            {
                shownInView = new HashSet<ElementId>();
                foreach (var shown in new FilteredElementCollector(document, view.Id).WhereElementIsNotElementType())
                    shownInView.Add(shown.Id);
            }
            if (!shownInView.Contains(wrapId)) continue;

            try
            {
                view.SetElementOverrides(wrapId, overrides);
                wrapsFollowed++;
            }
            catch { }
        }
    }
}
