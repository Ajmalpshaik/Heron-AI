// NOT STANDALONE. Assumes `app` and `templatePath` are in scope; leaves
// `created`, `refused` and `findings`.
//
// OPENS NO TRANSACTION. A new document cannot be inside the current one's.
//
// EVERY OTHER FAMILY FRAGMENT ACTS ON ONE THAT EXISTS - load, open, rename,
// upgrade, export. Nothing created one, so the first step of making a new
// component had no route at all.
//
// THE TEMPLATE IS AN INPUT AND IS NEVER GUESSED. Template files live in a
// per-installation, per-language folder; a guessed path either fails or quietly
// makes the wrong KIND of family, which is worse.
//
// THE DOCUMENT IS LEFT OPEN and belongs to the caller. It holds memory and a
// file lock until somebody closes it.

Document created = null;
string refused = null;
var findings = new List<string>();

if (app == null)
{
    refused = "No application was given";
}
else if (string.IsNullOrEmpty(templatePath))
{
    refused = "No template was given. A family template decides the category and "
        + "almost everything about how the family behaves - a generic model and a "
        + "face-based one are not interchangeable - and nothing here guesses a "
        + "path, because template folders differ by version, language and "
        + "installation";
}
else if (!System.IO.File.Exists(templatePath))
{
    refused = string.Format(
        "No template file at '{0}'. Family templates are .rft files and their "
        + "folder differs by Revit version, by language and by installation",
        templatePath);
}
else
{
    try
    {
        created = app.NewFamilyDocument(templatePath);

        if (created == null)
        {
            refused = string.Format(
                "Revit returned no document from '{0}'. The usual cause is a "
                + "template made by a newer Revit than this one", templatePath);
        }
        else
        {
            findings.Add(string.Format(
                "New family document created from '{0}'. It is EMPTY - no "
                + "geometry, no parameters, no types - and it is NOT SAVED. It "
                + "is also still open, and holds memory and a file lock until "
                + "somebody closes it", templatePath));
        }
    }
    catch (Exception ex)
    {
        refused = string.Format("Revit refused the new family: {0}", ex.Message);
    }
}

if (refused != null) findings.Add(refused);
