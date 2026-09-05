// NOT STANDALONE. Assumes `doc`, `template`, `parameterNames` and `include` are
// in scope; leaves `nowHeld`, `nowFree`, `notOnTemplate`, `viewsFollowing` and
// `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// THIS CHANGES THE TEMPLATE, SO IT CHANGES EVERY VIEW FOLLOWING IT. Freeing one
// setting to fix one plan frees it on all of them, and each keeps whatever
// value it happens to hold - which looks like nothing having happened. The
// count of views on the template is reported whether it was asked for or not.
//
// A VIEW THAT IS NOT A TEMPLATE IS REFUSED. Revit's documentation says these
// parameters are the ones "not marked as included when THIS VIEW is used as a
// template" - same wording on 2020 and 2027. Called on an ordinary view the API
// does not fail: it records a preference for the day that view becomes a
// template and changes nothing visible. A silent no-op is the worst possible
// answer here.
//
// NO "REPLACE THE WHOLE LIST" OPTION. Only the settings named are added or
// removed. A replacing list silently locks or frees everything the caller did
// not think to name, across every view on the template.
//
// A SETTING THE TEMPLATE CANNOT CONTROL IS REPORTED, NOT SWALLOWED. Revit
// ignores ids outside the template's controllable list without complaining,
// which turns a misspelt name into a successful call that did nothing.
//
// READ FIRST, WRITE, READ BACK. nowHeld and nowFree are a second read off the
// template, never the lists that were sent to it.

var nowHeld = new List<string>();
var nowFree = new List<string>();
var notOnTemplate = new List<string>();
var viewsFollowing = 0;
var refused = new List<string>();

if (template == null || !template.IsValidObject)
{
    refused.Add("no view template was given, so nothing was changed");
}
else if (!template.IsTemplate)
{
    refused.Add(string.Format("'{0}' is a view, not a view template. Which settings a template holds "
        + "belongs to the TEMPLATE - pass the template itself. Revit accepts this call on an ordinary "
        + "view and it changes nothing anybody can see, which is why it is refused here rather than "
        + "reported as done", template.Name));
}
else
{
    ICollection<ElementId> mayControl = null;
    ICollection<ElementId> notIncluded = null;

    try { mayControl = template.GetTemplateParameterIds(); }
    catch (Exception ex)
    {
        refused.Add(string.Format("'{0}' will not say which settings it can control: {1}",
            template.Name, ex.Message));
    }

    if (mayControl != null)
    {
        try { notIncluded = template.GetNonControlledTemplateParameterIds(); }
        catch (Exception) { notIncluded = new List<ElementId>(); }

        var controllable = new HashSet<ElementId>(mayControl);
        var excluded = new HashSet<ElementId>(notIncluded ?? new List<ElementId>());

        // Display name to id, off the template's own parameter set. Nothing
        // here reads an ElementId as a number.
        var wanted = new List<ElementId>();
        foreach (var asked in parameterNames)
        {
            if (string.IsNullOrEmpty(asked)) continue;

            var match = ElementId.InvalidElementId;
            try
            {
                foreach (Parameter parameter in template.Parameters)
                {
                    if (parameter == null || parameter.Definition == null) continue;
                    if (string.Equals(parameter.Definition.Name, asked.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                    {
                        match = parameter.Id;
                        break;
                    }
                }
            }
            catch (Exception) { }

            if (match == ElementId.InvalidElementId)
            {
                notOnTemplate.Add(string.Format("'{0}' - no setting of that name on '{1}'", asked,
                    template.Name));
                continue;
            }

            if (!controllable.Contains(match))
            {
                notOnTemplate.Add(string.Format("'{0}' - a real setting, and NOT one a view template "
                    + "can control. Revit would have ignored it without saying so", asked));
                continue;
            }

            wanted.Add(match);
        }

        if (wanted.Count == 0)
        {
            if (notOnTemplate.Count == 0)
            {
                refused.Add(string.Format("no settings were named, so nothing on '{0}' was changed",
                    template.Name));
            }
        }
        else
        {
            foreach (var id in wanted)
            {
                if (include) excluded.Remove(id);   // the template holds it
                else excluded.Add(id);              // each view decides it
            }

            try
            {
                template.SetNonControlledTemplateParameterIds(new List<ElementId>(excluded));
            }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}' - Revit refused the change: {1}", template.Name,
                    ex.Message));
                wanted.Clear();
            }
        }

        // Read back off the template. What was sent is not evidence.
        if (refused.Count == 0)
        {
            ICollection<ElementId> after = null;
            try { after = template.GetNonControlledTemplateParameterIds(); }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}' - changed, and could not be read back: {1}",
                    template.Name, ex.Message));
            }

            if (after != null)
            {
                var freeNow = new HashSet<ElementId>(after);
                try
                {
                    foreach (Parameter parameter in template.Parameters)
                    {
                        if (parameter == null || parameter.Definition == null) continue;
                        if (!controllable.Contains(parameter.Id)) continue;

                        if (freeNow.Contains(parameter.Id)) nowFree.Add(parameter.Definition.Name);
                        else nowHeld.Add(parameter.Definition.Name);
                    }
                }
                catch (Exception ex)
                {
                    refused.Add(string.Format("'{0}' - changed, and its settings could not be listed "
                        + "back: {1}", template.Name, ex.Message));
                }

                nowHeld.Sort();
                nowFree.Sort();
            }
        }
    }

    // How far this reaches. Reported whether it was asked for or not, because
    // it is the number that makes the change readable.
    try
    {
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
        {
            var following = element as View;
            if (following == null || following.IsTemplate) continue;
            if (following.ViewTemplateId == template.Id) viewsFollowing++;
        }
    }
    catch (Exception) { }
}
