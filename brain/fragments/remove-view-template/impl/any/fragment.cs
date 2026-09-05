// NOT STANDALONE. Assumes `doc` and `views` are in scope; leaves `detached`,
// `hadNoTemplate`, `refused` and `detachedFrom` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// DETACHING DOES NOT RESTORE ANYTHING. The view keeps every setting the
// template last gave it - the same scale, the same visibility, the same
// overrides - they just stop being locked. So the view looks IDENTICAL the
// instant this runs, and somebody who asked to "undo the template" will read
// that as nothing having happened. That is why detachedFrom names the template
// that was on it and says so in words: the read-back has to report what
// happened, not what was asked for.
//
// IT DOES NOT DELETE THE TEMPLATE. Every other view using it is untouched.
// Deleting the template element is DELETE_ELEMENTS and reaches the whole
// project.
//
// A VIEW TEMPLATE IS ITSELF A VIEW, so one can be passed in by mistake. It is
// refused by name, never skipped - a silent skip reads exactly like success.
//
// THE TEMPLATE NAME IS READ BEFORE THE DETACH, because after it the view no
// longer knows which template it had.

var detached = 0;
var hadNoTemplate = new List<string>();
var refused = new List<string>();
var detachedFrom = new List<string>();

foreach (var view in views)
{
    if (view == null || !view.IsValidObject) continue;

    if (view.IsTemplate)
    {
        refused.Add(string.Format("'{0}' is itself a view template - a template has no template of "
            + "its own, so there is nothing here to detach", view.Name));
        continue;
    }

    var templateId = view.ViewTemplateId;
    if (templateId == ElementId.InvalidElementId)
    {
        hadNoTemplate.Add(view.Name);
        continue;
    }

    // Read the name FIRST. After the detach the view cannot say what it had.
    var templateName = "(a template that cannot be named)";
    try
    {
        var template = doc.GetElement(templateId) as View;
        if (template != null) templateName = template.Name;
    }
    catch (Exception) { }

    try
    {
        view.ViewTemplateId = ElementId.InvalidElementId;
    }
    catch (Exception ex)
    {
        refused.Add(string.Format("'{0}' - Revit refused to detach '{1}': {2}",
            view.Name, templateName, ex.Message));
        continue;
    }

    // Read back. A write that reported success and left the template on is the
    // defect this whole project is built around.
    try
    {
        if (view.ViewTemplateId != ElementId.InvalidElementId)
        {
            refused.Add(string.Format("'{0}' - the detach was accepted and a read back still finds "
                + "a template on this view", view.Name));
            continue;
        }

        detachedFrom.Add(string.Format("'{0}' - was following '{1}'. It LOOKS THE SAME and those "
            + "settings are now its own to edit", view.Name, templateName));
        detached++;
    }
    catch (Exception ex)
    {
        refused.Add(string.Format("'{0}' - detached from '{1}' and could not be read back: {2}",
            view.Name, templateName, ex.Message));
    }
}
