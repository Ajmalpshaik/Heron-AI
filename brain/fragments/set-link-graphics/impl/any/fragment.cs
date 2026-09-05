// NOT STANDALONE. Assumes `doc`, `view`, `elements` and `overrides` are in
// scope; leaves `overridden`, `notALink`, `refused` and `appliedTo` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// A CATEGORY OVERRIDE IN THE HOST DOES NOT REACH INSIDE A LINK, AND SAYS
// NOTHING WHEN IT DOESN'T. That is why this exists: greying "walls" in a host
// whose walls are all in the ARCH link succeeds, changes nothing, and reports
// no error, because nothing was wrong.
//
// ONE OVERRIDE ON THE LINK INSTANCE COVERS EVERYTHING INSIDE IT - one call per
// link however many thousand elements are in the file.
//
// NOTHING HERE NAMES FilteredElementCollector(doc, viewId, linkId) OR
// RevitLinkGraphicsSettings. The first is absent on Revit 2020, the second is
// absent from the 2020 assembly entirely and has no halftone or transparency on
// the releases that do have it. Naming either would stop this compiling on
// 2020, and a try/catch does not help - the code never gets to run.
//
// THE OVERRIDE ARRIVES ALREADY BUILT, so an empty OverrideGraphicSettings IS
// the cleared state and no separate reset call is needed.
//
// READ FIRST, WRITE, READ BACK. appliedTo is a second read off the view.

var overridden = 0;
var notALink = 0;
var refused = new List<string>();
var appliedTo = new List<string>();

if (view == null || !view.IsValidObject)
{
    refused.Add("no view was given, so no link graphics were changed");
}
else if (view.IsTemplate)
{
    refused.Add(string.Format("'{0}' is a view template. An element override is set on a real view - "
        + "set it there, then capture the view as a template", view.Name));
}
else if (!view.AreGraphicsOverridesAllowed())
{
    refused.Add(string.Format("'{0}' ({1}) does not accept graphic overrides at all, so nothing was "
        + "changed", view.Name, view.ViewType));
}
else
{
    foreach (var element in elements)
    {
        var link = element as RevitLinkInstance;
        if (link == null || !link.IsValidObject) { notALink++; continue; }

        // Named by the link TYPE - the file name a person recognises. Two
        // placements of one file share the name and are reported separately,
        // because each carries its own override.
        var linkName = "(a link whose file cannot be named)";
        try
        {
            var linkType = doc.GetElement(link.GetTypeId()) as RevitLinkType;
            if (linkType != null) linkName = linkType.Name;
        }
        catch (Exception) { }

        try
        {
            view.SetElementOverrides(link.Id, overrides);
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("'{0}' in '{1}' - Revit refused the override: {2}", linkName,
                view.Name, ex.Message));
            continue;
        }

        // Read back off the view. What was sent is not evidence that the view
        // took it - a view template controlling V/G overrides wins over this
        // and leaves the picture unchanged.
        try
        {
            var after = view.GetElementOverrides(link.Id);
            if (after == null)
            {
                refused.Add(string.Format("'{0}' in '{1}' - the override was accepted and a read "
                    + "back finds none on the link", linkName, view.Name));
                continue;
            }

            appliedTo.Add(string.Format("'{0}' in '{1}': halftone {2}, transparency {3}%", linkName,
                view.Name, after.Halftone, after.Transparency));
            overridden++;
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("'{0}' in '{1}' - overridden and could not be read back: {2}",
                linkName, view.Name, ex.Message));
        }
    }

    if (overridden > 0 && view.ViewTemplateId != ElementId.InvalidElementId)
    {
        refused.Add(string.Format("'{0}' follows a view template. If the link still looks the same, "
            + "that template controls V/G overrides and wins over what was just set here - "
            + "REPORT_VIEW_TEMPLATE_CONTROL says whether it does", view.Name));
    }
}
