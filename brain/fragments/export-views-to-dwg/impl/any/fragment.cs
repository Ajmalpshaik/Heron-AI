// NOT STANDALONE. Assumes `doc`, `views`, `folder` and `setupName` are in
// scope; leaves `created`, `refused` and `wouldOverwrite` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). It does not need one to write
// files, and it does not open one.
//
// THE ONLY FRAGMENT HERE THAT WRITES OUTSIDE THE MODEL, AND THE RISK IS A
// DIFFERENT SHAPE. Everything else in this library can be undone in Revit.
// Export cannot: a file with a matching name is OVERWRITTEN, with no undo, no
// warning and no record of what was there. So the names are worked out first
// and anything already on disk is reported.
//
// IT STILL EXPORTS THE REST. A run that refuses everything because one file
// exists means somebody deletes the folder and re-runs, which is worse: the
// point is to say WHICH ones collided, and let that be a decision.
//
// THE EXPORT SETUP IS THE JOB. Layer mapping, line weights, text and how solids
// are written all come from a named setup the office agreed. Exporting with
// Revit's defaults gives a file that OPENS PERFECTLY and fails the recipient's
// CAD standard - the worst kind of wrong, because nothing looks broken. So a
// missing setup is a refusal, never a fallback to the defaults.
//
// ONE VIEW PER FILE. Revit will merge a set into one DWG, and nobody wants
// that: a drawing is a drawing.

var created = new List<string>();
var wouldOverwrite = new List<string>();
string refused = null;

var target = (folder ?? "").Trim();
var setup = (setupName ?? "").Trim();

if (views == null || views.Count == 0)
{
    refused = "no views were given to export";
}
else if (target.Length == 0)
{
    refused = "no folder was given to export into";
}
else if (!System.IO.Directory.Exists(target))
{
    // Not created here. Making a folder somebody mistyped scatters drawings
    // into a path nobody looks in, and the mistyped path is far more likely
    // than a genuinely missing one.
    refused = string.Format(
        "there is no folder at \"{0}\". It is not created here on purpose - a mistyped "
        + "path is more likely than a missing one, and inventing it scatters drawings "
        + "somewhere nobody looks", target);
}
else if (setup.Length == 0)
{
    refused = "name the DWG export setup. Revit's defaults produce a file that opens "
            + "perfectly and fails the recipient's CAD standard, which is the worst "
            + "kind of wrong because nothing looks broken";
}
else
{
    var settings = ExportDWGSettings.FindByName(doc, setup);

    if (settings == null)
    {
        refused = string.Format(
            "this project has no DWG export setup called \"{0}\". That is a refusal "
            + "rather than a fall back to the defaults, deliberately", setup);
    }
    else
    {
        var options = settings.GetDWGExportOptions();

        foreach (var view in views)
        {
            if (view == null) continue;

            // Revit builds the file name from the view; this mirrors the
            // commonest form so the overwrite check is looking at the right
            // names. It is a prediction, not a guarantee - which is why the
            // report says "would overwrite" rather than "overwrote".
            var stem = view.Name ?? "view";

            foreach (var bad in System.IO.Path.GetInvalidFileNameChars())
                stem = stem.Replace(bad, '-');

            var full = System.IO.Path.Combine(target, stem + ".dwg");
            if (System.IO.File.Exists(full)) wouldOverwrite.Add(full);

            // One view per call, so one file per drawing. Handing Revit the
            // whole set merges them into a single DWG, which nobody wants.
            var one = new List<ElementId>();
            one.Add(view.Id);

            if (doc.Export(target, stem, one, options)) created.Add(full);
        }

        if (created.Count == 0)
        {
            refused = "Revit exported nothing. A schedule and a template cannot go to "
                    + "DWG, and neither can a view that is not on a sheet in some setups";
        }
    }
}
