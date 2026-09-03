// NOT STANDALONE. Assumes `doc`, `view`, `filter`, `overrides` and `visible`
// are in scope; leaves `applied` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// EITHER KIND OF FILTER WORKS. A rule-based view filter and a saved selection
// filter share a base class, and a view does not care which it was given.
//
// ALREADY ON THE VIEW IS AN UPDATE, NOT A FAILURE. Adding it twice throws;
// somebody adjusting a colour wants the second call to take.
//
// PROJECTION AND CUT ARE BOTH THE CALLER'S JOB, and the note in the manifest
// says why: Revit derives neither from the other, so an override carrying only
// a projection colour looks right in plan and shows the wrong colour the moment
// anything is cut. This fragment applies what it is given and REPORTS what came
// back, so a half-built override is visible here rather than on the sheet.
//
// SETTING A FILTER INVISIBLE HIDES WHAT IT MATCHES. That is the ordinary way to
// take a discipline out of a drawing without deleting anything, and it is the
// same operation rather than a different one.

var applied = false;
var findings = new List<string>();

if (filter == null)
{
    findings.Add("No filter was given - make one first with CREATE_VIEW_FILTER for a rule, or "
        + "CREATE_SELECTION_FILTER for a hand-picked list");
}
else
{
    try
    {
        var alreadyOn = false;
        foreach (var id in view.GetFilters())
        {
            // ElementId to ElementId. Never as a number.
            if (id == filter.Id) { alreadyOn = true; break; }
        }

        if (!alreadyOn) view.AddFilter(filter.Id);

        view.SetFilterOverrides(filter.Id, overrides);
        view.SetFilterVisibility(filter.Id, visible);

        // READ IT BACK OFF THE VIEW. The view is what decides, not the call.
        var stuck = false;
        foreach (var id in view.GetFilters()) if (id == filter.Id) { stuck = true; break; }

        applied = stuck;

        if (!stuck)
        {
            findings.Add(string.Format("'{0}' was accepted by '{1}' and is not on it - a view template "
                + "usually controls the filters", filter.Name, view.Name));
        }
        else
        {
            var back = view.GetFilterOverrides(filter.Id);
            var hasProjection = back != null && back.ProjectionLineColor != null
                && back.ProjectionLineColor.IsValid;
            var hasCut = back != null && back.CutLineColor != null && back.CutLineColor.IsValid;

            findings.Add(string.Format("'{0}' {1} on '{2}', and what it matches is {3}",
                filter.Name, alreadyOn ? "was already there and was updated" : "is now",
                view.Name, visible ? "visible" : "HIDDEN in this view"));

            if (hasProjection && !hasCut)
                findings.Add("WARNING: the override sets a projection colour and NO cut colour. Revit "
                    + "derives neither from the other, so this will look right in plan and show the "
                    + "wrong colour the moment anything is cut");
        }
    }
    catch (Exception ex)
    {
        findings.Add(string.Format("'{0}' could not be applied to '{1}': {2}",
            filter.Name, view.Name, ex.Message));
    }
}
