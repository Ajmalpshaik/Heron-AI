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
//
// VERSION 3: WHAT IS IN FRONT IS READ, NOT TAKEN FROM REVIT'S ANSWER (row
// 5b-1750). OpenAndActivateDocument threw inside the job's group on Revit
// 2024 while the family window opened all the same, so after a throw the
// document in front is read back. `opened` is true only when that document
// is the .rfa just written; `problem` is kept for a real failure.

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
                    var uiapp = uidoc.Application;
                    string refusal = null;
                    try
                    {
                        var openedUi = uiapp.OpenAndActivateDocument(path);
                        opened = openedUi != null;
                        if (openedUi != null && openedUi.Document != null)
                            activeAfter = openedUi.Document.Title;
                    }
                    catch (Exception error) { refusal = error.Message; }

                    // REVIT'S ANSWER IS NOT THE LAST WORD ON WHAT IS IN FRONT -
                    // row 5b-1750. On Revit 2024, 2026-11-16, called inside the
                    // job's group, OpenAndActivateDocument threw "An internal
                    // error has occurred." and the family window WAS open and in
                    // front: Revit's title bar named it, and the next call was
                    // pinned to it. So after a throw, or no window handed back,
                    // the document in front is READ, and it counts as opened
                    // only when it is a family whose file is the one just
                    // written - never a family of the same name from elsewhere.
                    if (!opened)
                    {
                        Func<string, string> full = p =>
                        {
                            try { return System.IO.Path.GetFullPath(p ?? "").TrimEnd('\\', '/'); }
                            catch (Exception) { return (p ?? "").Trim(); }
                        };
                        var target = full(path);

                        Document inFront = null;
                        try
                        {
                            var active = uiapp.ActiveUIDocument;
                            inFront = active == null ? null : active.Document;
                        }
                        catch (Exception) { inFront = null; }

                        if (inFront != null && inFront.IsFamilyDocument
                            && string.Equals(full(inFront.PathName), target,
                                             StringComparison.OrdinalIgnoreCase))
                        {
                            opened = true;
                            activeAfter = inFront.Title;
                            if (refusal != null)
                                findings.Add(string.Format("Revit answered \"{0}\" while opening "
                                    + "it, but the window in front IS {1} - read back after "
                                    + "the error, so it is reported as opened.", refusal, path));
                        }
                        else
                        {
                            // OPEN BUT NOT IN FRONT is said apart, because the
                            // next fragment writes to whatever IS in front.
                            var behind = false;
                            try
                            {
                                foreach (Document open in uiapp.Application.Documents)
                                {
                                    if (open != null && open.IsFamilyDocument
                                        && string.Equals(full(open.PathName), target,
                                                         StringComparison.OrdinalIgnoreCase))
                                    { behind = true; break; }
                                }
                            }
                            catch (Exception) { behind = false; }

                            var frontName = "";
                            try { frontName = inFront == null ? "" : inFront.Title; }
                            catch (Exception) { frontName = ""; }

                            var said = refusal ?? "Revit handed back no window";
                            problem = (problem.Length > 0 ? problem + " | " : "") + said;
                            findings.Add(string.Format("{0} was written but Revit would not open it: "
                                + "{1}", path, said));
                            if (behind)
                                findings.Add(string.Format("It IS open in Revit, but not in front - "
                                    + "the window in front is '{0}'. Click the family's window "
                                    + "before anything is written to it.",
                                    frontName.Length == 0 ? "<none>" : frontName));
                        }
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
