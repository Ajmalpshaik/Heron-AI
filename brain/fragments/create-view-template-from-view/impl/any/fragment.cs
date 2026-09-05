// NOT STANDALONE. Assumes `doc`, `sourceView`, `newTemplateName` and
// `applyToSource` are in scope; leaves `templateId`, `templateName`,
// `appliedToSource` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// CreateViewTemplate() RETURNS A View, NOT AN ElementId. It reads like an
// id-returning call and is not; code written on that assumption does not
// compile. The returned View is used directly and its Id taken from it.
//
// IT DOES NOT APPLY THE TEMPLATE BACK TO THE SOURCE VIEW UNLESS ASKED.
// Capturing a view's settings and being governed by them are two different
// requests, and only the second LOCKS every setting on that view. Revit's own
// Create Template from Current View does not apply it back either.
//
// A DUPLICATE NAME DOES NOT LOSE THE TEMPLATE. Revit refuses a name something
// else already holds. The template is still created and keeps the name Revit
// gave it - throwing it away over a naming clash would destroy the captured
// settings, which are the expensive part.
//
// READ BACK. templateName is read off the template AFTER the rename attempt,
// never assumed from what was asked for.

var templateId = ElementId.InvalidElementId;
var templateName = "";
var appliedToSource = false;
var refused = new List<string>();

if (sourceView == null || !sourceView.IsValidObject)
{
    refused.Add("no source view was given, so nothing was captured");
}
else if (sourceView.IsTemplate)
{
    refused.Add(string.Format("'{0}' is already a view template, not a real view. A template has no "
        + "settings of its own to capture - pick a view that is set up the way the standard should "
        + "look", sourceView.Name));
}
else
{
    View created = null;
    try
    {
        created = sourceView.CreateViewTemplate();
    }
    catch (Exception ex)
    {
        refused.Add(string.Format("'{0}' - Revit would not make a template from this view: {1}",
            sourceView.Name, ex.Message));
    }

    if (created == null)
    {
        if (refused.Count == 0)
        {
            refused.Add(string.Format("'{0}' - Revit returned no template and did not say why",
                sourceView.Name));
        }
    }
    else
    {
        templateId = created.Id;

        // The rename is allowed to fail. The template is already made and its
        // settings are what matter; a clashing name is a naming problem, not a
        // reason to lose the capture.
        if (!string.IsNullOrEmpty(newTemplateName))
        {
            try
            {
                created.Name = newTemplateName;
            }
            catch (Exception ex)
            {
                refused.Add(string.Format("the template was created and could NOT be called '{0}': "
                    + "{1}. It is in the project under the name reported below", newTemplateName,
                    ex.Message));
            }
        }

        // Read the name back off the template rather than reporting what was
        // asked for. When the rename was refused these are different, and the
        // reported one is the only one that will find it in Revit.
        try { templateName = created.Name; }
        catch (Exception) { templateName = "(created, and its name cannot be read)"; }

        if (applyToSource)
        {
            try
            {
                sourceView.ViewTemplateId = created.Id;

                if (sourceView.ViewTemplateId == created.Id)
                {
                    appliedToSource = true;
                }
                else
                {
                    refused.Add(string.Format("'{0}' - the template was applied back to it and a "
                        + "read back does not find it there", sourceView.Name));
                }
            }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}' - the template was created and could not be "
                    + "applied back to this view: {1}", sourceView.Name, ex.Message));
            }
        }
    }
}
