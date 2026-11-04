// NOT STANDALONE. Assumes `doc` and `path` are in scope; leaves `imported`,
// `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and never opens one. One call is
// one undo entry.
//
// INSERT > IMPORT GBXML. Document.Import(string, GBXMLImportOptions) - one
// signature 2020 to 2027, and GBXMLImportOptions has nothing to set but its
// constructor (tools/api-surface --members, 2026-11-02).
//
// INSIDE THE CALLER'S TRANSACTION. RevitAPI.dll's own refusals for this method
// were read on 2020, 2024 and 2027 (the strings the assembly carries): an empty
// file name, "The GBXml file does not contains what it should!" and "The GBXml
// file is not valid for importing!" - and no "close the transaction" refusal of
// the kind Document.EditFamily carries in the same file. So nothing in the API
// says it must run outside one. NOT SEEN IN REVIT: whether the import runs
// inside the host's open transaction. If Revit refuses that, its words come back
// and nothing is kept.
//
// WHAT IT BRINGS IN IS READ BACK, NOT ASSUMED. Every element in the model is
// listed before and after; what is new is handed back as `imported` and counted
// by category in the answer. Which elements a gbXML import makes is Revit's to
// show - this card names none in advance.
//
// REFUSED BEFORE ANYTHING IS IMPORTED: a family; no file named; no file at that
// path; a file that is not .xml or .gbxml. Revit's own refusal, or Revit
// answering false, throws so the caller's transaction rolls back and nothing
// is kept.
//
// An ElementId is never read as a number.

var findings = new List<string>();
string refused = null;
string report = null;
var imported = new List<ElementId>();

var file = (path ?? "").Trim().Trim('"').Trim();
if (doc.IsFamilyDocument)
    refused = "The document in front is a family. A gbXML file is imported into a project, so NOTHING WAS IMPORTED.";
else if (file.Length == 0)
    refused = "No gbXML file was named. Give the full path of the .xml file, so NOTHING WAS IMPORTED.";
else if (!System.IO.File.Exists(file))
    refused = "There is no file at " + file + ", so NOTHING WAS IMPORTED.";
else
{
    var ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
    if (ext != ".xml" && ext != ".gbxml")
        refused = System.IO.Path.GetFileName(file) + " is not a gbXML file (.xml or .gbxml), so NOTHING WAS IMPORTED.";
}

if (refused == null)
{
    Func<HashSet<ElementId>> everything = () =>
    {
        var ids = new HashSet<ElementId>(new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElementIds());
        foreach (var id in new FilteredElementCollector(doc).WhereElementIsElementType().ToElementIds()) ids.Add(id);
        return ids;
    };
    var beforeIds = everything();

    bool ok;
    try { ok = doc.Import(file, new GBXMLImportOptions()); }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused to import " + System.IO.Path.GetFileName(file) + ": " + ex.Message
            + " - Revit's own words. NOTHING from this call was kept.");
    }
    if (!ok)
        throw new InvalidOperationException("Revit answered that " + System.IO.Path.GetFileName(file) + " was not imported, "
            + "and gave no reason. NOTHING from this call was kept.");

    foreach (var id in everything())
        if (!beforeIds.Contains(id)) imported.Add(id);

    var byCategory = imported
        .Select(id => doc.GetElement(id))
        .Where(e => e != null)
        .GroupBy(e => e.Category != null ? e.Category.Name : (e is ElementType ? "types with no category" : "elements with no category"))
        .OrderByDescending(g => g.Count())
        .Select(g => g.Count() + " " + g.Key)
        .ToList();

    if (imported.Count == 0)
        findings.Add("Revit says the import succeeded, but no element is new in the model. Look at the model before "
            + "keeping this - there may be nothing to keep.");
    report = System.IO.Path.GetFileName(file) + " imported: " + imported.Count + " new element(s)"
        + (byCategory.Count > 0 ? " - " + string.Join(", ", byCategory.Take(12)) + (byCategory.Count > 12 ? ", ..." : "") : "")
        + ". Read back by listing the model before and after.";
    findings.Add(report);
}
if (refused != null) findings.Add(refused);
