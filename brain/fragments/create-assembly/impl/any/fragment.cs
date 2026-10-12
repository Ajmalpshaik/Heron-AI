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
    try
    {
        assembly = AssemblyInstance.Create(doc, ids, categoryId);
        doc.Regenerate();
        var name = (assemblyName ?? "").Trim();
        if (name != "")
        {
            try { assembly.AssemblyTypeName = name; }
            catch (Exception ex) { findings.Add("The assembly was made, but Revit refused the name \"" + name + "\": " + ex.Message); }
        }
        doc.Regenerate();
        memberIds.AddRange(assembly.GetMemberIds());
        findings.Add("Made assembly \"" + assembly.AssemblyTypeName + "\" with " + memberIds.Count + " member(s), read "
            + "back, from " + ids.Count + " offered" + (alreadyInOne.Count > 0 ? "; " + alreadyInOne.Count + " already in an assembly left out." : "."));
    }
    catch (Exception ex)
    {
        refused = "Revit refused the assembly: " + ex.Message + ". NOTHING WAS MADE.";
        assembly = null;
    }
}
if (refused != null) findings.Add(refused);
