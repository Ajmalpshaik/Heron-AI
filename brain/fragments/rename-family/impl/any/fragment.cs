// NOT STANDALONE. Assumes `doc`, `elements` and `newName` are in scope; leaves
// `renamed`, `findings` and `notFamilies` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// RENAMING A FAMILY CHANGES IT EVERYWHERE. Every type in it and every instance
// of those types follow, across the whole model, whether or not they were in
// the selection. That is what was asked for and it still surprises people, so
// the report says how many went with it.
//
// MORE THAN ONE SOURCE FAMILY IS A REFUSAL, not a loop. Family names must be
// unique, so one literal name over three families takes on the first and
// quietly does not on the rest.
//
// AN IN-PLACE FAMILY IS REFUSED. It is tied to the element that made it and
// renaming it does not mean what people expect.
//
// THE FILE ON DISK IS UNTOUCHED. The family in the project is renamed; the .rfa
// keeps its own name, and reloading from it brings the old name back.

var renamed = new List<string>();
var findings = new List<string>();
var notFamilies = new List<ElementId>();

var families = new List<Family>();

foreach (var element in elements)
{
    if (element == null) continue;

    Family family = null;

    // Handed the family itself, one of its types, or an instance of one.
    family = element as Family;

    if (family == null)
    {
        var symbol = element as FamilySymbol;
        if (symbol != null) family = symbol.Family;
    }

    if (family == null)
    {
        var instance = element as FamilyInstance;
        if (instance != null && instance.Symbol != null) family = instance.Symbol.Family;
    }

    if (family == null) { notFamilies.Add(element.Id); continue; }

    var seen = false;
    foreach (var known in families) if (known.Id == family.Id) { seen = true; break; }
    if (!seen) families.Add(family);
}

if (string.IsNullOrEmpty(newName) || newName.Trim().Length == 0)
{
    findings.Add("A new name is needed");
}
else if (families.Count == 0)
{
    findings.Add("Nothing handed in belongs to a loaded family - system families such as walls, ducts "
        + "and pipes have no family to rename, and their TYPES are RENAME_ELEMENTS' job");
}
else if (families.Count > 1)
{
    var names = new List<string>();
    foreach (var family in families) names.Add("'" + family.Name + "'");
    findings.Add(string.Format(
        "The selection covers {0} different families, and one name cannot serve them all - Revit needs "
        + "family names to be unique. NOTHING was renamed. They are: {1}",
        families.Count, string.Join(", ", names)));
}
else
{
    var family = families[0];
    var wasCalled = family.Name;

    if (family.IsInPlace)
    {
        findings.Add(string.Format("'{0}' is an in-place family - it is tied to the element that made "
            + "it, and renaming it does not mean what it looks like it means", wasCalled));
    }
    else
    {
        var typeCount = 0;
        try { typeCount = family.GetFamilySymbolIds().Count; }
        catch { }

        try
        {
            family.Name = newName.Trim();

            // READ IT BACK. The name Revit settled on is the answer.
            var nowCalled = (doc.GetElement(family.Id) as Family).Name;
            renamed.Add(nowCalled);

            findings.Add(string.Format(
                "'{0}' is now '{1}'. Its {2} type(s) and EVERY instance of them changed with it, across "
                + "the whole model and not only what was selected. The .rfa file on disk keeps its own "
                + "name - reloading from it will bring '{0}' back",
                wasCalled, nowCalled, typeCount));
        }
        catch (Exception ex)
        {
            // A name already in use is the usual cause, and it is a question for
            // the user rather than something to work around by inventing one.
            findings.Add(string.Format("'{0}' could not be renamed to '{1}': {2}",
                wasCalled, newName.Trim(), ex.Message));
        }
    }
}

if (notFamilies.Count > 0)
    findings.Add(string.Format("{0} element(s) belong to no loadable family", notFamilies.Count));
