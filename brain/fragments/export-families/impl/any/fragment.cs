// NOT STANDALONE. Assumes `doc`, `folder` and `nameContains` are in scope, and
// leaves `saved`, `savedAs`, `nameCollisions` and `refused` behind.
//
// OPENS NO TRANSACTION, AND MUST NOT.
//
// Each family is a SEPARATE document. A transaction in the project would not
// cover them, and the project itself is not being changed at all - files
// appear outside it, which is why this is a publish rather than a modify.
//
// THE MECHANISM IS A DOCUMENT WITH NO WINDOW.
//
// The project somebody is looking at cannot be saved from here. A family
// opened for editing is a different thing: a background document with no
// window, and one of those saves normally. That is the whole trick, and it is
// the same one that lets a family be authored end to end.
//
// EVERY DOCUMENT IS CLOSED, INCLUDING ON THE WAY OUT OF A FAILURE.
//
// Leaving them open is what turns a run over four hundred families into a
// Revit that has to be restarted - and the failure path is exactly where a
// close gets forgotten.
//
// A LEGAL FAMILY NAME IS NOT ALWAYS A LEGAL FILE NAME.
//
// "300 x 150 / 45deg" is fine in Revit and impossible on a disk, and the save
// throws. Names are cleaned, and both names are reported so that two families
// cleaning to the same file name show up as a collision rather than one
// silently overwriting the other.

int saved = 0;
var savedAs = new Dictionary<string, string>();
var nameCollisions = new List<string>();
var refused = new List<string>();

string wanted = (nameContains ?? "").Trim();
string target = (folder ?? "").Trim();

Func<string, string> cleanName = name =>
{
    var kept = new System.Text.StringBuilder();
    foreach (var character in name ?? "")
    {
        // The characters Windows refuses in a file name, plus the two path
        // separators - a family name with a slash in it is common.
        if (character == '\\' || character == '/' || character == ':' || character == '*' ||
            character == '?' || character == '"' || character == '<' || character == '>' ||
            character == '|') continue;
        kept.Append(character);
    }
    return kept.ToString().Trim();
};

if (target.Length == 0)
{
    refused.Add("No folder was given, so there is nowhere to save to.");
}
else
{
    var families = new List<Family>();
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Family)))
    {
        var family = element as Family;
        if (family == null) continue;

        // Only a loadable family has a file behind it. A system family - a
        // wall type, a duct type - cannot be opened for editing at all, and
        // asking is refused rather than thrown.
        bool editable = false;
        try { editable = family.IsEditable; } catch { }
        if (!editable) continue;

        string name = "";
        try { name = family.Name ?? ""; } catch { }
        if (wanted.Length > 0 && name.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) < 0) continue;

        families.Add(family);
    }

    var usedFileNames = new Dictionary<string, string>();

    foreach (var family in families)
    {
        string name = "";
        try { name = family.Name ?? ""; } catch { }

        string fileName = cleanName(name);
        if (fileName.Length == 0)
        {
            refused.Add("A family's name cleans to nothing usable as a file name.");
            continue;
        }

        if (usedFileNames.ContainsKey(fileName.ToLowerInvariant()))
        {
            // Two families, one file name. Saving both would leave one file
            // and no sign that the other ever existed.
            nameCollisions.Add(name + " and " + usedFileNames[fileName.ToLowerInvariant()] +
                               " both clean to '" + fileName + "'");
            continue;
        }

        Document familyDocument = null;
        try { familyDocument = doc.EditFamily(family); }
        catch { refused.Add(name + ": Revit refused to open it for editing."); continue; }

        if (familyDocument == null) { refused.Add(name + ": no family document came back."); continue; }

        try
        {
            // Belt and braces: only ever save something that IS a family
            // document. Saving anything else here would write over a project.
            if (!familyDocument.IsFamilyDocument)
            {
                refused.Add(name + ": what came back was not a family document.");
                continue;
            }

            var options = new SaveAsOptions();
            options.OverwriteExistingFile = true;
            familyDocument.SaveAs(System.IO.Path.Combine(target, fileName + ".rfa"), options);

            usedFileNames[fileName.ToLowerInvariant()] = name;
            savedAs[name] = fileName + ".rfa";
            saved++;
        }
        catch { refused.Add(name + ": the save failed."); }
        finally
        {
            // Closed WITHOUT saving again - the save above is the save. This
            // runs on the failure path too, which is where it gets forgotten.
            try { familyDocument.Close(false); } catch { }
        }
    }
}
