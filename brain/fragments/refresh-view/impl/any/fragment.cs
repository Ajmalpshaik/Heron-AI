// NOT STANDALONE. Assumes `doc` and `uidoc` are in scope; leaves `regenerated`,
// `redrawn` and `findings`.
//
// REGENERATING AND REDRAWING ARE TWO THINGS. Document.Regenerate recomputes the
// model; RefreshActiveView puts the result on screen. Doing only the first
// leaves the screen stale, which is the complaint this exists to answer.
//
// Regenerate is already called INSIDE several write fragments where their own
// next step needs it. That is correct and is not this.

var regenerated = false;
var redrawn = false;
var findings = new List<string>();

if (doc == null)
{
    findings.Add("No document was given");
}
else
{
    try
    {
        doc.Regenerate();
        regenerated = true;
    }
    catch (Exception ex)
    {
        findings.Add(string.Format(
            "Revit would not regenerate the model: {0}. That usually means a "
            + "change is half-made - a transaction still open, or a failure "
            + "waiting to be answered", ex.Message));
    }

    if (uidoc == null)
    {
        findings.Add("The model was recomputed and there is no UI document, so "
            + "there is no screen to redraw. That is expected in a headless run "
            + "and would be a defect in front of somebody");
    }
    else
    {
        try
        {
            uidoc.RefreshActiveView();
            redrawn = true;
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("The active view would not redraw: {0}",
                ex.Message));
        }
    }

    if (regenerated && redrawn)
    {
        findings.Add("Model recomputed and the active view redrawn - what is on "
            + "screen now matches the model");
    }
}
