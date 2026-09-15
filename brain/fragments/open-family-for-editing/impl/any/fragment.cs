// NOT STANDALONE. Assumes `doc`, `uidoc`, `familyName` and `saveFolder` are
// in scope; leaves `opened`, `savedTo`, `activeAfter`, `problem` and
// `findings` behind.
//
// IT CANNOT RUN INSIDE A TRANSACTION. EditFamily and OpenAndActivateDocument
// both refuse while the document is modifiable - "The document is currently
// modifiable! Close the transaction before calling EditFamily." That is why
// this is EXECUTE and not MODIFY.
//
// IT GOES THROUGH A FILE, DELIBERATELY. A document from EditFamily is real,
// editable, and INVISIBLE: it never becomes the window in front, and it
// cannot be loaded back without an answer to Revit's overwrite question that
// a fragment may not give. Edit one and close it and the work goes with it -
// measured 2026-09-16, six families imported and every import discarded. So
// the family is written to a .rfa and THAT is opened, because a document
// opened from a file is a window a person can see and load from.
//
// THE FAMILY IN FRONT AFTERWARDS IS NOT THE PROJECT'S COPY. It is the file
// just written, which starts identical. Until somebody loads it back, the
// project still holds the original - so `activeAfter` is reported rather than
// assumed, and nothing here claims the model changed.

var findings = new List<string>();
var opened = false;
var savedTo = "";
var activeAfter = "";
var problem = "";

var wanted = (familyName ?? "").Trim();
var folder = (saveFolder ?? "").Trim();

if (wanted.Length == 0)
{
    findings.Add("No family was named.");
}
else if (folder.Length == 0)
{
    findings.Add("No folder was given to write the .rfa into. The file is how this reaches "
        + "Revit's own window, so there is nothing to default to.");
}
else
{
    Family family = null;
    try
    {
        foreach (var found in new FilteredElementCollector(doc).OfClass(typeof(Family)).ToElements())
        {
            var candidate = found as Family;
            if (candidate == null || candidate.Name == null) continue;
            if (string.Equals(candidate.Name, wanted, StringComparison.OrdinalIgnoreCase))
            {
                family = candidate;
                break;
            }
        }
    }
    catch (Exception) { }

    if (family == null)
    {
        findings.Add(string.Format("No loaded family is called '{0}'.", wanted));
    }
    else
    {
        var editable = true;
        try { editable = family.IsEditable; }
        catch (Exception) { editable = true; }

        if (!editable)
        {
            findings.Add(string.Format("'{0}' is not editable - an in-place or system family "
                + "cannot be opened this way.", wanted));
        }
        else
        {
            Document familyDocument = null;
            try { familyDocument = doc.EditFamily(family); }
            catch (Exception error)
            {
                problem = error.Message;
                findings.Add(string.Format("'{0}' could not be opened for editing: {1}",
                    wanted, error.Message));
            }

            if (familyDocument != null)
            {
                var path = System.IO.Path.Combine(folder, wanted + ".rfa");
                var written = false;
                try
                {
                    var options = new SaveAsOptions();
                    options.OverwriteExistingFile = true;
                    familyDocument.SaveAs(path, options);
                    savedTo = path;
                    written = true;
                }
                catch (Exception error)
                {
                    problem = error.Message;
                    findings.Add(string.Format("'{0}' could not be written to {1}: {2}",
                        wanted, path, error.Message));
                }

                // CLOSED BEFORE OPENING IT AGAIN. The same family cannot be
                // open twice, and the invisible copy is of no use to anybody.
                try { familyDocument.Close(false); }
                catch (Exception) { }

                if (written)
                {
                    try
                    {
                        var uiapp = uidoc.Application;
                        var openedUi = uiapp.OpenAndActivateDocument(path);
                        opened = openedUi != null;
                        if (openedUi != null && openedUi.Document != null)
                            activeAfter = openedUi.Document.Title;
                    }
                    catch (Exception error)
                    {
                        problem = (problem.Length > 0 ? problem + " | " : "") + error.Message;
                        findings.Add(string.Format("{0} was written but Revit would not open it: "
                            + "{1}", path, error.Message));
                    }
                }
            }
        }
    }
}

if (opened)
{
    findings.Add(string.Format("'{0}' is now the document in front, opened from {1}.",
        activeAfter, savedTo));
    findings.Add("THE PROJECT HAS NOT CHANGED. This is a copy on disk that starts identical to "
        + "the project's family. Whatever is done in that window reaches the model only when "
        + "somebody clicks Load into Project.");
}

findings.Insert(0, string.Format("{0}; in front now: {1}",
    opened ? "opened in Revit" : "NOT opened",
    activeAfter.Length == 0 ? "<unchanged>" : activeAfter));
