// NOT STANDALONE. Assumes `doc`, `views`, `cropActive`, `cropVisible` and
// `annotationCrop` are in scope; leaves `changed` and `refused` behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE ANNOTATION CROP IS THE HALF NOTHING ELSE COVERS AND THE ONE THAT SPOILS
// SHEETS. The model crop trims geometry; tags, dimensions and text keep printing
// outside it until the annotation crop is on too. That is why a neatly cropped
// plan still arrives with a tag over the title block.
//
// IT NEVER RESIZES. SET_VIEW_CROP sizes a crop round elements in one view; this
// flips flags across a drawing set. They compose.
//
// EACH FLAG IS OPTIONAL AND UNSET MEANS LEAVE ALONE. A batch that forces three
// settings when one was asked about is how a drawing set loses its crop
// boundaries overnight.
//
// THE ANNOTATION CROP DOES NOTHING WHILE CROPPING IS OFF - Revit ignores it. It
// is still set if asked, and the report says so, because silence there reads as
// the call having failed.

var changed = 0;
var refused = new List<string>();

Func<string, int> wanted = flag =>
{
    if (string.IsNullOrWhiteSpace(flag)) return -1;                 // leave alone
    var text = flag.Trim().ToLowerInvariant();
    if (text == "on" || text == "true" || text == "yes") return 1;
    if (text == "off" || text == "false" || text == "no") return 0;
    return -2;                                                       // not understood
};

var wantCrop = wanted(cropActive);
var wantLine = wanted(cropVisible);
var wantAnnotation = wanted(annotationCrop);

if (wantCrop == -2 || wantLine == -2 || wantAnnotation == -2)
{
    refused.Add("a flag was neither \"on\", \"off\" nor empty. Empty means LEAVE ALONE, which is not "
        + "the same as off, and guessing between them would change drawings nobody asked about");
}
else if (wantCrop == -1 && wantLine == -1 && wantAnnotation == -1)
{
    refused.Add("all three flags were left empty - nothing to change");
}
else
{
    foreach (var view in views)
    {
        if (view == null || !view.IsValidObject) continue;

        if (view.IsTemplate)
        {
            refused.Add(string.Format("'{0}' is a view TEMPLATE - set these on a real view, then save "
                + "the template", view.Name));
            continue;
        }

        var touched = false;

        if (wantCrop >= 0)
        {
            try { view.CropBoxActive = wantCrop == 1; touched = true; }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}': cropping could not be set - {1}", view.Name, ex.Message));
            }
        }

        if (wantLine >= 0)
        {
            try { view.CropBoxVisible = wantLine == 1; touched = true; }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}': the crop line could not be set - {1}",
                    view.Name, ex.Message));
            }
        }

        if (wantAnnotation >= 0)
        {
            var parameter = view.get_Parameter(BuiltInParameter.VIEWER_ANNOTATION_CROP_ACTIVE);
            if (parameter == null || parameter.IsReadOnly)
            {
                refused.Add(string.Format("'{0}' ({1}) has no settable annotation crop",
                    view.Name, view.ViewType));
            }
            else
            {
                try
                {
                    parameter.Set(wantAnnotation);
                    touched = true;

                    // Revit ignores the annotation crop while cropping is off.
                    var croppingOn = wantCrop == 1 || (wantCrop == -1 && view.CropBoxActive);
                    if (wantAnnotation == 1 && !croppingOn)
                    {
                        refused.Add(string.Format("'{0}': the annotation crop was set, but cropping is "
                            + "OFF on this view so Revit ignores it. Turn cropping on for it to bite",
                            view.Name));
                    }
                }
                catch (Exception ex)
                {
                    refused.Add(string.Format("'{0}': the annotation crop could not be set - {1}",
                        view.Name, ex.Message));
                }
            }
        }

        if (touched) changed++;
    }
}
