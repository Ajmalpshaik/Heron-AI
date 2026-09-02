// NOT STANDALONE. Assumes `doc` and `familyPath` are in scope; leaves
// `created`, `refused` and `typeNames` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// IT DOES NOT OVERWRITE, AND THAT IS A LIMIT OF THE FRAGMENT FORMAT ITSELF -
// worth writing down rather than leaving as an apparent oversight. Revit
// overwrites a loaded family only when handed an IFamilyLoadOptions object
// deciding what to do with parameter values that differ. That object needs a
// CLASS, and a fragment is a body of statements: C# has no local classes, so
// there is nowhere here to define one.
//
// The overload without it returns FALSE for a family already loaded rather than
// replacing it. So the behaviour lands on the safe side by accident and is kept
// on purpose: silently overwriting is how a colleague's edited type is replaced
// by the office standard mid-job, taking its parameter values with it.
//
// >> IF RELOADING IS EVER WANTED, it belongs in the executor (D-28) as a
// >> supplied helper, not in a fragment - the same way `doc` and `uidoc` are
// >> supplied. Do not solve it by making fragments able to declare types; that
// >> changes what a fragment IS.
//
// THE FILE IS CHECKED BEFORE THE CALL, because LoadFamily's own failure for a
// missing path is indistinguishable from its failure for an already-loaded
// family - both are a bare `false` - and those two need completely different
// things done about them.

ElementId created = null;
string refused = null;
var typeNames = new List<string>();

var path = (familyPath ?? "").Trim();

if (path.Length == 0)
{
    refused = "no family file was given";
}
else if (!path.ToLowerInvariant().EndsWith(".rfa"))
{
    refused = string.Format(
        "\"{0}\" is not a .rfa file. A project (.rvt) is linked, not loaded, and a "
        + "template (.rft) is what a family is built FROM", path);
}
else if (!System.IO.File.Exists(path))
{
    // Checked separately because LoadFamily returns a bare `false` for both a
    // missing file and an already-loaded family, and those need opposite
    // responses.
    refused = string.Format("there is no file at \"{0}\"", path);
}
else
{
    Family family;
    var loaded = doc.LoadFamily(path, out family);

    if (!loaded || family == null)
    {
        refused = string.Format(
            "Revit did not load \"{0}\". The usual reason is that a family of that name "
            + "is ALREADY in the project - this cannot overwrite one, deliberately, "
            + "because replacing a colleague's edited type takes its parameter values "
            + "with it. Use Revit's own Load Family to reload. The other reason is a "
            + "family saved by a NEWER Revit than this one, which Revit itself refuses",
            path);
    }
    else
    {
        created = family.Id;

        // The types are what somebody actually needs next - a family with one
        // type and a family with forty are placed very differently, and the
        // names are what PLACE_FAMILY_INSTANCES gets asked for.
        foreach (var typeId in family.GetFamilySymbolIds())
        {
            var symbol = doc.GetElement(typeId);
            if (symbol != null) typeNames.Add(symbol.Name);
        }

        typeNames.Sort(delegate (string a, string b)
        {
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        });
    }
}
