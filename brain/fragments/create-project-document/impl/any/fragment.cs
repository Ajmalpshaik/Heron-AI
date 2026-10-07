// NOT STANDALONE. Assumes `app`, `uidoc`, `templatePath` and `savePath` are in
// scope; leaves `created`, `title`, `savedTo`, `templateUsed`, `openNext`,
// `refused` and `findings`.
//
// OPENS NO TRANSACTION. A new document cannot be inside the current one's.
//
// NOTHING IN HERON COULD START A PROJECT. Measured 2026-10-06:
// `NewProjectDocument` appeared in no fragment, so a new model began with
// File > New > Project by hand. This makes the project from the template the
// caller names, saves it to exactly the path the caller names, closes the
// windowless copy the API makes, and opens the file in a real window.
//
// THE BODY BELOW THE SHARED LINE IS CREATE_FAMILY_DOCUMENT'S, word for word, and
// tests/test_new_document_twins.py fails the moment the two differ. The five
// lines above that line are the whole difference between a family and a
// project here.
//
// THE TEMPLATE IS ASKED FOR AND NEVER GUESSED. The office template decides the
// units, the views, the families and the standards the model starts with, and
// picking one is a decision about the job (D-33). A bare name is looked up in
// the folder of THIS Revit's default project template, by exact file name.
//
// THE FILE NAME IS THE CALLER'S AND NEVER INVENTED. A path that already exists
// is refused - this never overwrites a model - and so is a folder that does
// not exist, rather than created.

var kind = "project";
var templateExtension = ".rte";
var fileExtension = ".rvt";
var exampleTemplate = "Mechanical-Default_Metric";
Func<string, Document> makeNew = path => app.NewProjectDocument(path);
Func<string> templateFolder = () =>
    string.IsNullOrEmpty(app.DefaultProjectTemplate) ? ""
        : (System.IO.Path.GetDirectoryName(app.DefaultProjectTemplate) ?? "");

// ---- SHARED with create-family-document from here down ----

Document created = null;
string title = null;
string savedTo = null;
string templateUsed = null;
string openNext = null;
string refused = null;
var findings = new List<string>();

var template = (templatePath ?? "").Trim().Trim('"');
var target = (savePath ?? "").Trim().Trim('"');
var article = kind == "family" ? "a family" : "a project";

// WHERE THIS REVIT KEEPS ITS TEMPLATES - read from Revit, never typed. It
// differs by version, by language and by installation.
var folder = "";
try { folder = app == null ? "" : templateFolder(); }
catch (Exception) { folder = ""; }

var targetExtension = "";
try { targetExtension = System.IO.Path.GetExtension(target).ToLowerInvariant(); }
catch (Exception) { targetExtension = ""; }

if (app == null || uidoc == null)
{
    refused = "No Revit application was given";
}
else if (template.Length == 0)
{
    refused = string.Format("No template was given. The template decides almost "
        + "everything about how {0} behaves, so it is asked for, never guessed. Give a "
        + "full {1} path, or a template's name such as \"{2}\"", article,
        templateExtension, exampleTemplate);
}
else if (target.Length == 0)
{
    refused = string.Format("No savePath was given. The new {0} is saved to a file YOU "
        + "name - the full path ending {1}. Nothing here invents a file name, because an "
        + "invented name is how another file gets overwritten", kind, fileExtension);
}
else if (!System.IO.Path.IsPathRooted(target))
{
    refused = string.Format("savePath '{0}' is not a full path. Give the whole path, "
        + "drive and folder included, so there is no doubt where the file goes", target);
}
else if (targetExtension != fileExtension)
{
    refused = string.Format("savePath '{0}' does not end {1}, and {2} is saved as {1}",
        target, fileExtension, article);
}
else if (System.IO.File.Exists(target) || System.IO.Directory.Exists(target))
{
    refused = string.Format("'{0}' already exists. Nothing was created and the file was "
        + "not touched - this never overwrites. Name a new file", target);
}
else if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(target) ?? ""))
{
    refused = string.Format("The folder '{0}' does not exist. Nothing was created. Make "
        + "the folder first, or name one that exists - this does not create folders",
        System.IO.Path.GetDirectoryName(target));
}
else
{
    // THE TEMPLATE: a full path as given, or a name looked up by exact file name.
    var looksLikePath = template.IndexOf('\\') >= 0 || template.IndexOf('/') >= 0
        || System.IO.Path.IsPathRooted(template);
    if (looksLikePath)
    {
        if (System.IO.File.Exists(template)) templateUsed = template;
        else refused = string.Format("No template file at '{0}'. Template folders differ "
            + "by Revit version, by language and by installation", template);
    }
    else
    {
        var wantedName = template.EndsWith(templateExtension, StringComparison.OrdinalIgnoreCase)
            ? template : template + templateExtension;
        var matches = new List<string>();
        if (folder.Length > 0 && System.IO.Directory.Exists(folder))
        {
            try
            {
                foreach (var file in System.IO.Directory.GetFiles(folder, "*" + templateExtension,
                             System.IO.SearchOption.AllDirectories))
                {
                    if (string.Equals(System.IO.Path.GetFileName(file), wantedName,
                                      StringComparison.OrdinalIgnoreCase))
                        matches.Add(file);
                }
            }
            catch (Exception ex)
            {
                refused = string.Format("Could not read the template folder '{0}': {1}",
                    folder, ex.Message);
            }
        }

        if (refused == null)
        {
            if (matches.Count == 1)
                templateUsed = matches[0];
            else if (matches.Count == 0)
                refused = string.Format("No template called '{0}' in {1}. Give the full "
                    + "path instead - nothing here picks a near match, because a near match "
                    + "makes the wrong kind of {2}", wantedName,
                    folder.Length == 0
                        ? "any folder this Revit reports (it reports none)"
                        : "'" + folder + "', the folder this Revit uses", kind);
            else
                refused = string.Format("{0} templates are called '{1}': {2}. Give the full "
                    + "path of the one you mean", matches.Count, wantedName,
                    string.Join("; ", matches));
        }
    }

    // A TEMPLATE OF THE OTHER KIND is refused by name, not handed to Revit to fail.
    if (refused == null
        && !templateUsed.EndsWith(templateExtension, StringComparison.OrdinalIgnoreCase))
    {
        refused = string.Format("'{0}' is not {1} template - those end {2}. A family "
            + "template is CREATE_FAMILY_DOCUMENT's and a project template is "
            + "CREATE_PROJECT_DOCUMENT's", templateUsed, article, templateExtension);
    }

    Document made = null;
    if (refused == null)
    {
        try
        {
            made = makeNew(templateUsed);
            if (made == null)
                refused = string.Format("Revit returned no document from '{0}'. The usual "
                    + "cause is a template made by a newer Revit than this one", templateUsed);
        }
        catch (Exception ex)
        {
            refused = string.Format("Revit refused the new {0}: {1}", kind, ex.Message);
        }
    }

    // SAVED TO EXACTLY THE CALLER'S PATH, never over anything: OverwriteExistingFile
    // stays false, so a file that appeared since the check above is still safe.
    if (refused == null)
    {
        try
        {
            made.SaveAs(target, new SaveAsOptions { OverwriteExistingFile = false });
            savedTo = made.PathName;
        }
        catch (Exception ex)
        {
            refused = string.Format("Revit made the new {0} but would not save it to '{1}': "
                + "{2}. The unsaved copy was closed, so nothing is left open", kind, target,
                ex.Message);
            try { made.Close(false); } catch (Exception) { }
            made = null;
        }
    }

    // THE HIDDEN COPY IS CLOSED AND THE FILE OPENED IN A WINDOW. A document made by
    // the API has no window and cannot be given one; opening the saved file is the
    // only way the modeller sees it. Closed first, so Revit opens the file rather
    // than finding the windowless copy already in memory.
    if (refused == null)
    {
        try { made.Close(false); }
        catch (Exception ex)
        {
            findings.Add(string.Format("The windowless copy could not be closed ({0}); it "
                + "stays in memory until Revit closes", ex.Message));
        }

        // REVIT OPENS NO WINDOW WHILE THE ACTIVE DOCUMENT IS MODIFIABLE - and it is
        // whenever Heron's own transaction is open in it, which is every run aimed
        // at the window in front. MEASURED 2026-10-06, Revit 2024, every route:
        //   transaction open in the active document - refused, "The active
        //     document is currently modifiable";
        //   only Heron's group open there - the window opened AND Revit threw "An
        //     internal error has occurred", which is not a route to build on;
        //   deferred to Revit's Idling event - it never fired, in two runs.
        // So in that case the file is saved and NOT opened here, and the reply
        // names the call that opens it: ACTIVATE_DOCUMENT runs with no
        // transaction at all, and opened this exact file in a window on the same
        // day. Aimed at a document that is not in front, nothing is open in the
        // active one, and it opens here and now.
        var uiapp = uidoc.Application;
        Document inFront = null;
        try { inFront = uiapp.ActiveUIDocument == null ? null : uiapp.ActiveUIDocument.Document; }
        catch (Exception) { inFront = null; }

        if (inFront != null && inFront.IsModifiable)
        {
            title = System.IO.Path.GetFileNameWithoutExtension(savedTo);
            openNext = savedTo;
            findings.Add(string.Format("'{0}' IS SAVED, NOT YET IN A WINDOW. Revit opens no "
                + "window while Heron is working in the document in front, which this run "
                + "was. Open it with ACTIVATE_DOCUMENT, documentPath={1}", title, savedTo));
        }
        else
        {
            try
            {
                var opened = uiapp.OpenAndActivateDocument(savedTo);
                if (opened != null && opened.Document != null)
                {
                    created = opened.Document;
                    title = created.Title;
                }
            }
            catch (Exception ex)
            {
                refused = string.Format("The new {0} IS SAVED at '{1}', but Revit would not "
                    + "open it in a window: {2}. Open it with File > Open, or ACTIVATE_DOCUMENT "
                    + "with that path", kind, savedTo, ex.Message);
            }

            if (refused == null && title == null)
                refused = string.Format("The new {0} is saved at '{1}', but Revit did not say "
                    + "it opened. Open it with File > Open", kind, savedTo);
        }
    }

    if (refused == null)
    {
        findings.Add(string.Format("New {0} '{1}' made from '{2}' and saved to '{3}'. It "
            + "holds only what the template holds. Target it next by its title: \"{1}\"",
            kind, title, templateUsed, savedTo));
    }
}

if (refused != null) findings.Add(refused);
