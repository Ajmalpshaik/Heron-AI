// NOT STANDALONE. Assumes `doc`, `numbers`, `names` and `titleBlockName` are in
// scope, and leaves `created`, `numberedAs`, `namedAs`, `numberTaken`,
// `refused` and `titleBlockNotFound` behind.
//
// ASSUMES AN OPEN TRANSACTION. It does not start one - Golden Rule 16.
//
// THE NUMBER IS CHECKED BEFORE ANYTHING IS CREATED.
//
// ViewSheet.Create makes the sheet with a number REVIT picks; setting the
// wanted one is a second step that can be refused for a clash. So a clash
// leaves a REAL SHEET in the register under the wrong number, and a count
// reports the job as done. That is the same shape DUPLICATE_SHEETS was
// re-authored to remove, where the earlier version reported "Duplicated 0 of 1"
// while the sheet count went 2 to 3 and an orphan stayed behind.
//
// Checking first removes the failure instead of cleaning up after it, and it
// makes a RE-RUN create nothing rather than a second set of misnumbered sheets.
//
// THE TITLE BLOCK HAS NO DEFAULT. Revit will create a sheet with none at all,
// and a register full of blank-bordered sheets is discovered at issue. Which
// title block a project uses is a project decision.
//
// NUMBER AND NAME ARE READ BACK SEPARATELY because they fail independently: the
// number can be refused while the name is accepted, and the result looks
// correct in any list showing only one of them.

var created = new List<ElementId>();
var numberedAs = new Dictionary<ElementId, string>();
var namedAs = new Dictionary<ElementId, string>();
var numberTaken = new List<string>();
var refused = new List<ElementId>();
bool titleBlockNotFound = false;

// By NAME among the title block types - a project standard is written in names,
// and an id is not portable between models.
ElementId titleBlockId = ElementId.InvalidElementId;
foreach (var type in new FilteredElementCollector(doc)
             .OfCategory(BuiltInCategory.OST_TitleBlocks)
             .WhereElementIsElementType()
             .ToElements())
{
    string typeName = null;
    try { typeName = type.Name; } catch { }
    if (typeName == titleBlockName) { titleBlockId = type.Id; break; }
}

if (titleBlockId == ElementId.InvalidElementId) titleBlockNotFound = true;
else
{
    var taken = new HashSet<string>();
    foreach (var sheet in new FilteredElementCollector(doc)
                 .OfClass(typeof(ViewSheet))
                 .Cast<ViewSheet>())
    {
        try { taken.Add(sheet.SheetNumber); } catch { }
    }

    for (int i = 0; i < numbers.Count; i++)
    {
        string wantedNumber = numbers[i];

        // Checked BEFORE creating. Nothing is left behind to tidy up.
        if (taken.Contains(wantedNumber)) { numberTaken.Add(wantedNumber); continue; }

        ViewSheet sheet = null;
        try { sheet = ViewSheet.Create(doc, titleBlockId); } catch { }
        if (sheet == null) { numberTaken.Add(wantedNumber); continue; }

        created.Add(sheet.Id);
        taken.Add(wantedNumber);

        try { sheet.SheetNumber = wantedNumber; } catch { }
        if (names != null && i < names.Count && !string.IsNullOrWhiteSpace(names[i]))
        {
            try { sheet.Name = names[i]; } catch { }
        }

        string actualNumber = null, actualName = null;
        try { actualNumber = sheet.SheetNumber; } catch { }
        try { actualName = sheet.Name; } catch { }

        if (actualNumber != null) numberedAs[sheet.Id] = actualNumber;
        if (actualName != null) namedAs[sheet.Id] = actualName;

        if (actualNumber != wantedNumber) refused.Add(sheet.Id);
    }
}
