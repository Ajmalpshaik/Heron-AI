// NOT STANDALONE. Assumes `doc`, `copyFromName`, `newName` and `abbreviation`
// are in scope; leaves `created`, `classification` and `refused` behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THERE IS NO CREATE CALL FOR A SYSTEM TYPE ON ANY RELEASE - checked against the
// reference assemblies at both ends. One is made by DUPLICATING an existing one,
// which makes choosing the parent the real decision: the new type inherits its
// CLASSIFICATION - supply, return, sanitary - and that cannot be changed
// afterwards. Copy a Return to make a Supply and it behaves as a Return forever
// while reading correctly on every drawing.
//
// THIS IS NOT DUPLICATE_TYPE. That duplicates the type behind ELEMENTS - a
// duct's size and shape. A system type decides colour, abbreviation and which
// system a run belongs to.
//
// THE ABBREVIATION IS WHAT APPEARS ON THE DRAWING, and Revit accepts a duplicate
// without complaint - two systems then tag identically. Checked here.

Element created = null;
var classification = "";
var refused = new List<string>();

MEPSystemType parent = null;
var available = new List<string>();
var takenNames = new List<string>();
var takenAbbreviations = new List<string>();

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(MEPSystemType)))
{
    var systemType = element as MEPSystemType;
    if (systemType == null) continue;

    takenNames.Add(systemType.Name);
    try
    {
        var existing = systemType.Abbreviation;
        if (!string.IsNullOrEmpty(existing)) takenAbbreviations.Add(existing);
    }
    catch { }

    var label = systemType.Name;
    try { label = string.Format("{0} [{1}]", systemType.Name, systemType.SystemClassification); }
    catch { }
    available.Add(label);

    if (string.Equals(systemType.Name, copyFromName, StringComparison.OrdinalIgnoreCase))
    {
        parent = systemType;
    }
}

if (parent == null)
{
    available.Sort();
    refused.Add(string.Format("no system type called '{0}'. There is NO create call on any release - a "
        + "system type is made by duplicating one - so the parent decides the classification and it "
        + "cannot be changed afterwards. This project has: {1}",
        copyFromName, string.Join(", ", available)));
}
else if (string.IsNullOrWhiteSpace(newName))
{
    refused.Add("no new name given");
}
else
{
    var nameTaken = false;
    foreach (var existing in takenNames)
    {
        if (string.Equals(existing, newName, StringComparison.OrdinalIgnoreCase)) { nameTaken = true; break; }
    }

    if (nameTaken)
    {
        refused.Add(string.Format("'{0}' is already a system type here. Revit refuses a duplicate name "
            + "by throwing, and a throw partway leaves a type behind carrying whatever it was called",
            newName));
    }
    else
    {
        try
        {
            var copy = parent.Duplicate(newName) as MEPSystemType;
            created = copy;

            try { classification = parent.SystemClassification.ToString(); }
            catch { classification = "(unreadable)"; }

            if (copy != null && !string.IsNullOrWhiteSpace(abbreviation))
            {
                var clash = false;
                foreach (var existing in takenAbbreviations)
                {
                    if (string.Equals(existing, abbreviation, StringComparison.OrdinalIgnoreCase))
                    {
                        clash = true;
                        break;
                    }
                }

                try { copy.Abbreviation = abbreviation; }
                catch (Exception ex)
                {
                    refused.Add(string.Format("the abbreviation could not be set - {0}", ex.Message));
                }

                if (clash)
                {
                    refused.Add(string.Format("'{0}' is ALREADY the abbreviation of another system type. "
                        + "Revit accepts that without complaint and the two then tag identically on "
                        + "every drawing", abbreviation));
                }
            }

            refused.Add(string.Format("'{0}' was copied from '{1}' and is classified {2}. That "
                + "classification came from the parent and CANNOT be changed - if it is wrong, delete "
                + "this and copy from a type of the right kind", newName, parent.Name, classification));
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("'{0}' could not be created - {1}", newName, ex.Message));
        }
    }
}
