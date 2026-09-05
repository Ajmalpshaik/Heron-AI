// NOT STANDALONE. Assumes `doc`, `views`, `folder`, `filePrefix` and
// `pixelWidth` are in scope; leaves `exported` and `findings` behind.
//
// PUBLISH, NOT MODIFY. This leaves the model alone and puts files where other
// people can find them, which is a higher bar than changing a duct.
//
// NO TRANSACTION, AND THAT IS NOT AN OVERSIGHT. An export reads the model and
// writes a file; there is nothing to undo.
//
// REVIT NAMES THE FILES ITSELF. The prefix is a stem and Revit appends the view
// type and name, so the folder and prefix are reported and the exact filenames
// are NOT invented - a predicted list that turned out wrong is worse than none.
//
// A VIEW THAT CANNOT BE PRINTED IS NAMED, not dropped. A folder with fewer
// files than views and nothing saying which are missing is the failure.

var exported = 0;
var findings = new List<string>();

if (views == null || views.Count == 0)
{
    findings.Add("No views were given to export");
}
else if (string.IsNullOrEmpty(folder))
{
    findings.Add("No folder was given - an export has to be told where to put the files");
}
else
{
    var printable = new List<ElementId>();
    var refusedNames = new List<string>();

    foreach (var view in views)
    {
        if (view == null) continue;

        if (view.IsTemplate || !view.CanBePrinted)
        {
            refusedNames.Add(string.Format("'{0}'{1}", view.Name, view.IsTemplate ? " (a template)" : ""));
            continue;
        }

        printable.Add(view.Id);
    }

    if (printable.Count == 0)
    {
        findings.Add(string.Format("None of the {0} view(s) given can be exported as an image. "
            + "Templates cannot, and neither can some view types: {1}",
            views.Count, refusedNames.Count == 0 ? "" : string.Join(", ", refusedNames.ToArray())));
    }
    else
    {
        var prefix = string.IsNullOrEmpty(filePrefix) ? "view" : filePrefix;
        var width = pixelWidth > 0 ? pixelWidth : 1920;

        try
        {
            System.IO.Directory.CreateDirectory(folder);

            var options = new ImageExportOptions();
            options.FilePath = System.IO.Path.Combine(folder, prefix);
            options.ExportRange = ExportRange.SetOfViews;
            options.HLRandWFViewsFileType = ImageFileType.PNG;
            options.ShadowViewsFileType = ImageFileType.PNG;
            options.ZoomType = ZoomFitType.FitToPage;
            options.PixelSize = width;
            options.FitDirection = FitDirectionType.Horizontal;
            options.SetViewsAndSheets(printable);

            doc.ExportImage(options);
            exported = printable.Count;

            findings.Add(string.Format("{0} view(s) written as PNG at {1} px wide into '{2}', with the "
                + "prefix '{3}'. Revit names each file itself by adding the view type and name to that "
                + "prefix, so the exact filenames are not predicted here - look in the folder",
                exported, width, folder, prefix));
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("The export failed: {0}. Nothing was written, or only part of "
                + "it was - look in '{1}' before running it again", ex.Message, folder));
        }
    }

    if (refusedNames.Count > 0)
        findings.Add(string.Format("{0} view(s) could NOT be exported and are named so the file count "
            + "can be reconciled: {1}", refusedNames.Count, string.Join(", ", refusedNames.ToArray())));
}
