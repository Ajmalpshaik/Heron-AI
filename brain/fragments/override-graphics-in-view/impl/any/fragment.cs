// NOT STANDALONE. Assumes `view`, `elements`, `overrides` and `linksOnly` are
// in scope, and leaves `overridden`, `skipped`, `wrapsFollowed`, `notALink`,
// `refused` and `appliedTo` behind.
//
// VERSION 2 (2026-11-01) - SET_LINK_GRAPHICS MERGED IN, on the owner's
// instruction ("make sure there is no duplicate tools"). A linked model is ONE
// element in the host, so one override on its instance covers everything
// inside it - this card's job, handed a link. What that card had that this
// lacked:
//
//   `linksOnly` - asked for a LINK, anything handed in that is not a link
//   instance is counted in `notALink` and NOT overridden. A model element
//   handed in by mistake must not be quietly greyed as though it were a link.
//   Left out it is false, and every element is overridden as version 1 did.
//
//   A VIEW TEMPLATE IS REFUSED BY NAME. An element override is set on a real
//   view; set on a template it would look as if it worked. A view that takes
//   no override at all is refused with its type named, and no view given is
//   said - in `refused`, beside every element in `skipped` as before.
//
//   A REVIT REFUSAL IS CAUGHT PER ELEMENT. Version 1 let SetElementOverrides
//   throw out of the fragment, which aborts the caller's transaction - work
//   this fragment never saw. Now the element is skipped, Revit's words are in
//   `refused`, and the rest go on.
//
//   A LINK'S OVERRIDE IS READ BACK, NAMED BY ITS FILE - the link TYPE's name,
//   the one a person recognises; two placements of one file are reported
//   separately - with the halftone and transparency the view now holds, in
//   `appliedTo`. What was sent is not evidence the view took it.
//
//   THE TEMPLATE WINS, AND THAT IS SAID. A view following a template that
//   controls V/G overrides keeps the template's look whatever is set here, so
//   after any override on such a view a sentence in `refused` names the view
//   and REPORT_VIEW_TEMPLATE_CONTROL.
//
//   NOT CARRIED, AND WHY: nothing. SET_LINK_GRAPHICS' warning that a CATEGORY
//   override in the host never reaches inside a link stays true, and is the
//   reason a link is greyed here, by its instance. Overriding categories
//   INSIDE a link is still not done - the collector it needs is absent on
//   Revit 2020.
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
var notALink = 0;
var refused = new List<string>();
var appliedTo = new List<string>();

var viewProblem = "";
if (view == null || !view.IsValidObject)
    viewProblem = "no view was given, so no graphics were changed";
else if (view.IsTemplate)
    viewProblem = string.Format("'{0}' is a view template. An element override is set on a real view - set it "
        + "there, then capture the view as a template", view.Name);
else if (!view.AreGraphicsOverridesAllowed())
    viewProblem = string.Format("'{0}' ({1}) does not accept graphic overrides at all, so nothing was changed",
        view.Name, view.ViewType);

if (viewProblem.Length > 0)
{
    // Every element, so the caller's count still adds up and "nothing happened"
    // is reported as a fact rather than inferred from a zero.
    refused.Add(viewProblem);
    foreach (var element in elements) if (element != null) skipped.Add(element.Id);
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

        // ASKED FOR A LINK (version 2, from SET_LINK_GRAPHICS), anything else
        // is counted and left alone.
        var link = element as RevitLinkInstance;
        if (linksOnly && link == null)
        {
            notALink++;
            skipped.Add(element.Id);
            continue;
        }

        // Named by the link TYPE - the file name a person recognises.
        var linkName = "";
        if (link != null)
        {
            linkName = "(a link whose file cannot be named)";
            try
            {
                var linkType = document.GetElement(link.GetTypeId()) as RevitLinkType;
                if (linkType != null) linkName = linkType.Name;
            }
            catch (Exception) { }
        }

        // A REVIT REFUSAL IS CAUGHT HERE (version 2): one element's problem,
        // never the caller's transaction.
        try
        {
            view.SetElementOverrides(element.Id, overrides);
        }
        catch (Exception ex)
        {
            skipped.Add(element.Id);
            refused.Add(string.Format("{0} in '{1}' - Revit refused the override: {2}",
                link != null ? "'" + linkName + "'" : "element " + element.Id, view.Name, ex.Message));
            continue;
        }
        overridden++;

        // A LINK'S OVERRIDE, READ BACK OFF THE VIEW (version 2).
        if (link != null)
        {
            try
            {
                var after = view.GetElementOverrides(link.Id);
                if (after == null)
                    refused.Add(string.Format("'{0}' in '{1}' - the override was accepted and a read back finds "
                        + "none on the link", linkName, view.Name));
                else
                    appliedTo.Add(string.Format("'{0}' in '{1}': halftone {2}, transparency {3}%", linkName,
                        view.Name, after.Halftone, after.Transparency));
            }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}' in '{1}' - overridden and could not be read back: {2}",
                    linkName, view.Name, ex.Message));
            }
        }

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

// THE TEMPLATE WINS, AND IT IS SAID (version 2, from SET_LINK_GRAPHICS). The
// write succeeds and the picture does not move, which is the hardest of these
// to diagnose from a result that only counts successes.
if (viewProblem.Length == 0 && overridden > 0 && view.ViewTemplateId != ElementId.InvalidElementId)
{
    refused.Add(string.Format("'{0}' follows a view template. If it still looks the same, that template controls "
        + "V/G overrides and wins over what was just set here - REPORT_VIEW_TEMPLATE_CONTROL says whether it does",
        view.Name));
}
