// NOT STANDALONE. Assumes `doc`, `elements`, `phaseId` and `phaseFilterId` are
// in scope; leaves `phaseSet`, `phaseFilterSet`, `alreadySet`,
// `templateControlled`, `unsupported` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THIS CHANGES THE DRAWING, NOT THE BUILDING. A view phase decides what the
// view looks FROM; an element phase decides when the thing exists.
// SET_ELEMENT_PHASE is the other one, and reaching for it by mistake rewrites
// the construction sequence in order to fix a drawing.
//
// THE TWO PROPERTIES ARE SET SEPARATELY BECAUSE THEY FAIL SEPARATELY. A
// schedule has a phase and no phase filter; a drafting view and a legend have
// neither. Setting them as one block leaves the other unset on a refusal, with
// nothing saying which.
//
// EVERY VALUE IS READ BACK. A view parameter can refuse without raising, so a
// count of Set calls reports views changed that were not.
//
// AN INVALID ID MEANS LEAVE THAT PROPERTY ALONE - not "write a default". A
// default written into whichever property was omitted re-phases a drawing set.

var phaseSet = new List<ElementId>();
var phaseFilterSet = new List<ElementId>();
var alreadySet = new List<ElementId>();
var templateControlled = new List<ElementId>();
var unsupported = new List<ElementId>();
var findings = new List<string>();

// Both ids are resolved and checked BEFORE anything is written. An element id
// parameter set to an id of the wrong kind leaves a view pointing at nonsense,
// and it is accepted quietly often enough to matter.
var wantPhase = phaseId != null && phaseId != ElementId.InvalidElementId;
var wantPhaseFilter = phaseFilterId != null && phaseFilterId != ElementId.InvalidElementId;

if (wantPhase)
{
    var resolvedPhase = doc.GetElement(phaseId) as Phase;
    if (resolvedPhase == null)
    {
        findings.Add("The id given for the phase is not a Phase in this model. No phase is "
            + "written - a view pointing at something that is not a phase is worse than a "
            + "view left alone.");
        wantPhase = false;
    }
}

if (wantPhaseFilter)
{
    var resolvedFilter = doc.GetElement(phaseFilterId) as PhaseFilter;
    if (resolvedFilter == null)
    {
        findings.Add("The id given for the phase filter is not a Phase Filter in this model. "
            + "No phase filter is written.");
        wantPhaseFilter = false;
    }
}

if (!wantPhase && !wantPhaseFilter)
{
    findings.Add("Neither a phase nor a phase filter was given, so nothing was changed.");
}
else
{
    foreach (var element in elements)
    {
        var view = element as View;
        if (view == null)
        {
            unsupported.Add(element == null ? ElementId.InvalidElementId : element.Id);
            continue;
        }

        var touched = false;
        var refused = false;
        var carriedNeither = true;

        if (wantPhase)
        {
            var parameter = view.get_Parameter(BuiltInParameter.VIEW_PHASE);
            if (parameter != null)
            {
                carriedNeither = false;

                if (parameter.IsReadOnly)
                {
                    // The commonest refusal here, and it is a reason rather
                    // than a failure: a template is holding the property.
                    refused = true;
                }
                else if (parameter.AsElementId() == phaseId)
                {
                    // Already what was asked for. Counting this as "set" would
                    // report work that did not happen.
                    alreadySet.Add(view.Id);
                }
                else
                {
                    parameter.Set(phaseId);

                    // Read back. Set can return without raising and leave the
                    // old value in place.
                    if (parameter.AsElementId() == phaseId)
                    {
                        phaseSet.Add(view.Id);
                        touched = true;
                    }
                    else
                    {
                        refused = true;
                    }
                }
            }
        }

        if (wantPhaseFilter)
        {
            var parameter = view.get_Parameter(BuiltInParameter.VIEW_PHASE_FILTER);
            if (parameter != null)
            {
                carriedNeither = false;

                if (parameter.IsReadOnly)
                {
                    refused = true;
                }
                else if (parameter.AsElementId() == phaseFilterId)
                {
                    if (!alreadySet.Contains(view.Id)) alreadySet.Add(view.Id);
                }
                else
                {
                    parameter.Set(phaseFilterId);

                    if (parameter.AsElementId() == phaseFilterId)
                    {
                        phaseFilterSet.Add(view.Id);
                        touched = true;
                    }
                    else
                    {
                        refused = true;
                    }
                }
            }
        }

        // A view carrying neither parameter is a drafting view, a legend or a
        // sheet. That is not a refusal and not a template - it is a view type
        // the question does not apply to.
        if (carriedNeither)
        {
            unsupported.Add(view.Id);
        }
        else if (refused)
        {
            // `&& !touched` USED TO BE HERE AND IT SWALLOWED HALF THE ANSWER.
            // `touched` goes true the moment the PHASE is written, so a view
            // whose phase set and whose phase FILTER was refused in the same
            // call satisfied `!touched == false` and was added to nothing -
            // reported as a clean success with phaseFilterSet silently one
            // short.
            //
            // MEASURED 2026-09-13 on `test projject`, view '1 - Mech', asked
            // for phase New Construction and filter Show All: phaseSet 1,
            // phaseFilterSet 0, alreadySet 0, templateControlled 0,
            // unsupported 0, findings 0. Nothing anywhere said the filter had
            // not taken. `alreadySet` being empty is what rules out "it was
            // already Show All" - that path adds to it at line 129.
            //
            // A refusal is a refusal whether or not the OTHER property went
            // through. The read-back already catches it; only the reporting
            // was throwing it away.
            templateControlled.Add(view.Id);
        }
    }

    if (templateControlled.Count > 0)
    {
        findings.Add(templateControlled.Count + " view(s) refused the phase or the phase filter - "
            + "the value was written and read back unchanged. A VIEW TEMPLATE HOLDING THE PROPERTY "
            + "is much the commonest reason: REPORT_VIEW_TEMPLATE_CONTROL says which template and "
            + "what it holds, and the fix is to change the template or release the property rather "
            + "than to try again. It is not the ONLY reason, so the sentence no longer claims it is; "
            + "a view listed here had SOMETHING refuse, and the template is where to look first.");
    }

    if (unsupported.Count > 0)
    {
        findings.Add(unsupported.Count + " element(s) do not carry a view phase at all - a "
            + "drafting view, a legend, a sheet, or something that is not a view. Not a "
            + "refusal; the question does not apply to them.");
    }
}
