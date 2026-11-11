// NOT STANDALONE. Assumes `doc`, `elements`, `assemblyName` and
// `namingCategory` are in scope; leaves `assembly`, `memberIds`,
// `alreadyInOne`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// AN ASSEMBLY FROM THE ELEMENTS HANDED IN - Create Assembly in the ribbon - so
// CREATE_ASSEMBLY_VIEWS can then make its shop drawings. Named by the caller
// or by Revit.
//
// ELEMENTS ALREADY IN AN ASSEMBLY ARE LEFT OUT AND NAMED: Revit puts an element
// in one assembly only. The rest are checked with Revit's own test before
// anything is made, so a set Revit would refuse is refused here with the
// reason rather than half-made.
//
// THE NAMING CATEGORY decides the assembly's category and its default name.
// Given by name it must be one Revit accepts for this set; empty takes the
// first element's category when Revit accepts it.
//
// READ BACK: the members and the type name are read off the new assembly.
//
// ===========================================================================
// VERSION 2 (FRAGMENT-ISSUES 5b-1117).
// ===========================================================================
//
// "NOTHING WAS MADE" IS NOW TRUE WHEN IT IS SAID. Version 1 made the assembly,
// filled `memberIds` from it, and only then met Revit's refusal - on both
// measured runs "No valid type for the assembly instance", thrown when the
// new assembly's type name was read. Its catch set `refused` to "... NOTHING
// WAS MADE" and `assembly` to null, and took nothing back: the assembly stayed
// in the caller's transaction, which is kept unless something throws out of
// here, and `memberIds` stayed filled beside the refusal - a run that read as
// a pass, and was signed as one. Now the making sits in one SubTransaction.
// It is kept only after the members and the name have been read back, and
// `memberIds` is filled only then; any refusal rolls it back, so nothing of a
// refused assembly is left in the model and `memberIds` stays empty.
//
// THIS DOES NOT MAKE THE NAMING WORK (FRAGMENT-ISSUES 5b-1553). Revit's own
// remark on AssemblyInstance.Create, the same on all eight releases: "Assembly
// type is assigned after the transaction for creating assembly instance is
// complete." Until that row is repaired, expect the refusal above.

var findings = new List<string>();
string refused = null;
AssemblyInstance assembly = null;
var memberIds = new List<ElementId>();
var alreadyInOne = new List<ElementId>();

var ids = new List<ElementId>();
Element first = null;
foreach (var element in elements)
{
    if (element == null || !element.IsValidObject) continue;
    if (element.AssemblyInstanceId != null && element.AssemblyInstanceId != ElementId.InvalidElementId)
    {
        alreadyInOne.Add(element.Id);
        continue;
    }
    ids.Add(element.Id);
    if (first == null) first = element;
}

if (ids.Count == 0)
    refused = alreadyInOne.Count > 0
        ? "Every element handed in is already in an assembly, so NOTHING WAS MADE."
        : "No elements were handed in, so NOTHING WAS MADE.";

ElementId categoryId = null;
var categoryName = "";
if (refused == null)
{
    var wanted = (namingCategory ?? "").Trim();
    if (wanted == "")
    {
        categoryId = first.Category == null ? null : first.Category.Id;
        categoryName = first.Category == null ? "" : first.Category.Name;
    }
    else
    {
        foreach (Category category in doc.Settings.Categories)
            if (category != null && string.Equals(category.Name, wanted, StringComparison.OrdinalIgnoreCase))
            {
                categoryId = category.Id;
                categoryName = category.Name;
                break;
            }
        if (categoryId == null) refused = "No category is named \"" + wanted + "\", so NOTHING WAS MADE.";
    }
    if (refused == null && categoryId == null) refused = "The first element has no category to name the assembly by, so NOTHING WAS MADE.";
}

if (refused == null)
{
    bool valid = false, namingOk = false;
    try { valid = AssemblyInstance.AreElementsValidForAssembly(doc, ids, ElementId.InvalidElementId); } catch { }
    try { namingOk = AssemblyInstance.IsValidNamingCategory(doc, categoryId, ids); } catch { }
    if (!valid) refused = "Revit will not put these elements in one assembly - a view-specific element, a host "
        + "and its hosted element split apart, or an element type Revit does not assemble. NOTHING WAS MADE.";
    else if (!namingOk) refused = "Revit will not name an assembly of these elements by \"" + categoryName
        + "\". Name another category of the elements. NOTHING WAS MADE.";
}

if (refused == null)
{
    // IN A SUBTRANSACTION OF ITS OWN, so a refusal part way takes the assembly
    // back. See the header.
    using (var making = new SubTransaction(doc))
    {
        try
        {
            making.Start();
            assembly = AssemblyInstance.Create(doc, ids, categoryId);
            doc.Regenerate();
            var name = (assemblyName ?? "").Trim();
            string nameRefused = null;
            if (name != "")
            {
                try { assembly.AssemblyTypeName = name; }
                catch (Exception ex) { nameRefused = "The assembly was made, but Revit refused the name \"" + name + "\": " + ex.Message; }
            }
            doc.Regenerate();
            var readBack = new List<ElementId>(assembly.GetMemberIds());
            var made = "Made assembly \"" + assembly.AssemblyTypeName + "\" with " + readBack.Count + " member(s), read "
                + "back, from " + ids.Count + " offered" + (alreadyInOne.Count > 0 ? "; " + alreadyInOne.Count + " already in an assembly left out." : ".");

            // KEPT ONLY NOW, and only now is anything reported as made.
            making.Commit();
            memberIds.AddRange(readBack);
            if (nameRefused != null) findings.Add(nameRefused);
            findings.Add(made);
        }
        catch (Exception ex)
        {
            if (making.HasStarted() && !making.HasEnded()) making.RollBack();
            assembly = null;
            refused = "Revit refused the assembly: " + ex.Message.TrimEnd('.', ' ')
                + ". What it had started was taken back, so NOTHING WAS MADE.";
        }
    }
}
if (refused != null) findings.Add(refused);
