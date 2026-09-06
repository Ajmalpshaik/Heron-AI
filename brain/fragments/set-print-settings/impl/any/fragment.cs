// NOT STANDALONE. Assumes `doc`, `paperSizeName`, `orientation`, `zoomToFit`
// and `saveAsName` are in scope, and leaves `availableSizes`, `applied`,
// `savedAs` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) for the document side.
//
// THE PAPER SIZES BELONG TO THE DRIVER.
//
// Revit offers what the CURRENT printer offers. A size on the PDF writer may
// not exist on the plotter down the corridor, and asking for a missing one
// fails in a way that reads as a Revit fault. What the driver actually has is
// always reported, so a refusal can be read against the list.
//
// AND THE PRINT MANAGER IS NOT TRANSACTIONAL.
//
// These settings are application state as much as document state: an undo does
// not reliably put the previous paper size back.
//
// EVERY ASSIGNMENT IS GUARDED ON ITS OWN.
//
// Some drivers throw on a paper size rather than refusing it, and one that
// throws must not cost the orientation and the fit as well.

var availableSizes = new List<string>();
var applied = new List<string>();
string savedAs = "";
string refused = "";

PrintManager printManager = null;
try { printManager = doc.PrintManager; }
catch { refused = "The print manager could not be opened."; }

if (printManager != null)
{
    PaperSize wantedSize = null;
    try
    {
        foreach (PaperSize size in printManager.PaperSizes)
        {
            if (size == null) continue;
            string name = "";
            try { name = size.Name ?? ""; } catch { }
            availableSizes.Add(name);
            if (name.Length > 0 &&
                string.Equals(name, (paperSizeName ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                wantedSize = size;
        }
    }
    catch { }

    if (!string.IsNullOrEmpty((paperSizeName ?? "").Trim()) && wantedSize == null)
        refused = "This print driver has no paper size called '" + paperSizeName +
                  "'. What it does have is in availableSizes - the sizes come from the driver, " +
                  "not from Revit.";

    if (refused.Length == 0)
    {
        PrintSetup setup = null;
        try { setup = printManager.PrintSetup; } catch { }

        IPrintSetting setting = null;
        try { setting = setup.CurrentPrintSetting; } catch { }

        PrintParameters parameters = null;
        try { parameters = setting.PrintParameters; } catch { }

        if (parameters == null)
            refused = "The current print setting has no parameters to change.";
        else
        {
            if (wantedSize != null)
            {
                try { parameters.PaperSize = wantedSize; applied.Add("paper size"); } catch { }
            }

            string wantedOrientation = (orientation ?? "").Trim().ToLowerInvariant();
            if (wantedOrientation == "portrait" || wantedOrientation == "landscape")
            {
                try
                {
                    parameters.PageOrientation = wantedOrientation == "portrait"
                        ? PageOrientationType.Portrait
                        : PageOrientationType.Landscape;
                    applied.Add("orientation");
                }
                catch { }
            }

            try
            {
                parameters.ZoomType = zoomToFit ? ZoomType.FitToPage : ZoomType.Zoom;
                applied.Add("fit");
            }
            catch { }

            string name = (saveAsName ?? "").Trim();
            if (name.Length > 0)
            {
                // A name already in use is refused rather than overwritten:
                // quietly replacing the setting another drawing set prints
                // with is worse than saying no.
                bool taken = false;
                try
                {
                    foreach (var element in new FilteredElementCollector(doc)
                                 .OfClass(typeof(PrintSetting)))
                    {
                        if (element == null) continue;
                        string existing = "";
                        try { existing = element.Name ?? ""; } catch { }
                        if (string.Equals(existing, name, StringComparison.OrdinalIgnoreCase))
                        {
                            taken = true;
                            break;
                        }
                    }
                }
                catch { }

                if (taken)
                    refused = "A print setting called '" + name + "' already exists, and overwriting " +
                              "one another drawing set prints with is not what 'save as' means.";
                else
                {
                    try { setup.SaveAs(name); savedAs = name; }
                    catch { refused = "Revit refused to save the print setting '" + name + "'."; }
                }
            }
        }
    }
}
