// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `findings`,
// `withTitleBlock`, `withoutTitleBlock` and `typesInUse` behind.
//
// READS ONLY. No transaction, nothing changed.
//
// THE CALL TO MAKE BEFORE SET_SHEET_TITLE_BLOCK, because that fragment behaves
// in two entirely different ways depending on this answer: a same-family target
// is a safe swap, a different family deletes the old title block and everything
// held on it.
//
// A SHEET WITH NO TITLE BLOCK IS A FINDING, NOT A BLANK ROW. It prints with
// nothing round the drawing and looks completely normal in the Project Browser.
//
// A SHEET WITH MORE THAN ONE IS ALSO A FINDING. Two borders stacked on top of
// each other look like one and print like one, and a later swap changes
// whichever the API reaches first while the other stays.
//
// ONE COLLECTOR, NOT ONE PER SHEET. A collector inside the loop is the same
// query run once per sheet.

var findings = new List<string>();
var withTitleBlock = 0;
var withoutTitleBlock = new List<string>();
var typesInUse = new List<string>();

var allBlocks = new List<FamilyInstance>();
foreach (var element in new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_TitleBlocks)
    .WhereElementIsNotElementType())
{
    var instance = element as FamilyInstance;
    if (instance != null && instance.IsValidObject) allBlocks.Add(instance);
}

foreach (var element in elements)
{
    var sheet = element as ViewSheet;
    if (sheet == null || !sheet.IsValidObject) continue;

    var onThisSheet = new List<FamilyInstance>();
    foreach (var candidate in allBlocks)
    {
        if (candidate.OwnerViewId == sheet.Id) onThisSheet.Add(candidate);
    }

    if (onThisSheet.Count == 0)
    {
        withoutTitleBlock.Add(string.Format("{0} - {1}", sheet.SheetNumber, sheet.Name));
        findings.Add(string.Format("{0} - {1}: NO TITLE BLOCK. It will print with nothing round the "
            + "drawing and looks normal in the browser", sheet.SheetNumber, sheet.Name));
        continue;
    }

    var names = new List<string>();
    foreach (var block in onThisSheet)
    {
        var name = "(a title block whose type cannot be read)";
        try { name = block.Symbol.Family.Name + " : " + block.Symbol.Name; }
        catch (Exception) { }

        names.Add(name);
        if (!typesInUse.Contains(name)) typesInUse.Add(name);
    }

    if (onThisSheet.Count == 1)
    {
        findings.Add(string.Format("{0} - {1}: {2}", sheet.SheetNumber, sheet.Name, names[0]));
    }
    else
    {
        findings.Add(string.Format("{0} - {1}: {2} TITLE BLOCKS ON ONE SHEET - {3}. Stacked they "
            + "look like one and print like one, and a swap changes only whichever is reached first",
            sheet.SheetNumber, sheet.Name, onThisSheet.Count, string.Join(", ", names.ToArray())));
    }

    withTitleBlock++;
}

typesInUse.Sort();

if (typesInUse.Count > 1)
{
    findings.Add(string.Format("{0} different title blocks across this set: {1}. A set on one border "
        + "reports one", typesInUse.Count, string.Join(", ", typesInUse.ToArray())));
}
