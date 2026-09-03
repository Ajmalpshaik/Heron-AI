// NOT STANDALONE. Assumes `doc`, `view`, `visibleWorksetNames` and
// `hideAllOthers` are in scope; leaves `shown`, `hidden`, `notFound` and
// `refused` behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THERE IS NO SINGLE "WHICH WORKSETS DOES THIS VIEW SHOW" PROPERTY. A phase is
// one settable property on the view; workset visibility is one setting per
// workset per view. That is why this loops rather than assigns, and it is why
// somebody looking for one call concludes it cannot be done.
//
// A WORKSET HAS THREE STATES IN A VIEW, NOT TWO: Visible, Hidden, and Use
// Global Setting - which defers to the workset's own default and is usually
// visible. Turning the wanted ones on WITHOUT pushing the rest to Hidden leaves
// them on global, the view still shows everything, and it reads as the call
// having done nothing. hideAllOthers is what closes that.
//
// ON A MODEL THAT IS NOT WORKSHARED there are no user worksets to set. Reported,
// not thrown - the question does not apply rather than the view being wrong.

var shown = 0;
var hidden = 0;
var notFound = new List<string>();
var refused = new List<string>();

if (!doc.IsWorkshared)
{
    refused.Add("this model is not workshared - it has no user worksets, so there is nothing to "
        + "turn on or off. Nothing was changed");
}
else if (view.IsTemplate)
{
    refused.Add(string.Format("'{0}' is a view TEMPLATE. Set the worksets on a real view, then save "
        + "that view as the template", view.Name));
}
else
{
    var wanted = new List<string>();
    foreach (var name in visibleWorksetNames)
    {
        if (!string.IsNullOrEmpty(name)) wanted.Add(name);
    }

    var worksets = new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset).ToWorksets();
    var matched = new List<string>();

    foreach (var workset in worksets)
    {
        var isWanted = false;
        foreach (var name in wanted)
        {
            if (string.Equals(workset.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                isWanted = true;
                matched.Add(name);
                break;
            }
        }

        try
        {
            if (isWanted)
            {
                view.SetWorksetVisibility(workset.Id, WorksetVisibility.Visible);
                shown++;
            }
            else if (hideAllOthers)
            {
                view.SetWorksetVisibility(workset.Id, WorksetVisibility.Hidden);
                hidden++;
            }
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("workset '{0}': {1}", workset.Name, ex.Message));
        }
    }

    // A name that matched nothing is the difference between a view showing one
    // workset and a view showing none, so it is named rather than counted.
    foreach (var name in wanted)
    {
        var found = false;
        foreach (var hit in matched)
        {
            if (string.Equals(hit, name, StringComparison.OrdinalIgnoreCase)) { found = true; break; }
        }
        if (!found) notFound.Add(name);
    }

    if (notFound.Count > 0)
    {
        var available = new List<string>();
        foreach (var workset in worksets) available.Add(workset.Name);
        refused.Add(string.Format("no workset named {0}. This model has: {1}",
            string.Join(", ", notFound), string.Join(", ", available)));
    }

    if (!hideAllOthers && shown > 0)
    {
        refused.Add("the other worksets were left on Use Global Setting, which is usually VISIBLE - "
            + "the view may look unchanged. Set hideAllOthers to close that");
    }
}
