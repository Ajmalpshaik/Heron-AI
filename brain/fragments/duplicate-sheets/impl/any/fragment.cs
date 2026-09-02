// NOT STANDALONE. Assumes `doc`, `elements`, `numberSuffix` and `namePrefix`
// are in scope, and leaves `newSheetIds`, `notDuplicated` and `collidedNumbers`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16), so a set of new sheets is one
// undo entry.
//
// WHY THE NUMBER IS CHECKED BEFORE ANYTHING IS CREATED.
//
// Revit requires sheet numbers to be unique, and a sheet has to EXIST before
// its number can be set. So the natural order - create, number, catch the
// collision - leaves a sheet already committed, carrying Revit's own
// auto-number, while the code reports a failure.
//
// That is the worst shape this failure takes. Elsewhere the danger is claiming
// something happened when it did not; here the report says NOTHING HAPPENED
// while a stray sheet sits in the drawing register under a number nobody chose.
// Measured in the library this was re-authored from: "Duplicated 0 of 1
// sheet(s), 1 failed" while the sheet count went 2 to 3.
//
// Checking first removes the failure instead of handling it. Nothing is created
// that then needs cleaning up - which is also why this fragment never deletes
// anything. The original's fix was to delete its own half-built sheet, and a
// delete is a permission and a risk this design does not need at all.

var suffix = (numberSuffix ?? "").Trim();
var prefix = (namePrefix ?? "").Trim();

var newSheetIds = new List<ElementId>();
var notDuplicated = new List<ElementId>();
var collidedNumbers = new List<string>();

// Every sheet number already in the document, gathered once. New numbers are
// added as they are created so two source sheets in one batch cannot both
// claim the same new number - a collision the document alone could not predict.
var takenNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var existing in new FilteredElementCollector(doc)
                             .OfClass(typeof(ViewSheet))
                             .Cast<ViewSheet>())
{
    takenNumbers.Add(existing.SheetNumber);
}

foreach (var element in elements)
{
    var source = element as ViewSheet;
    if (source == null)
    {
        notDuplicated.Add(element.Id);
        continue;
    }

    var wantedNumber = source.SheetNumber + suffix;

    // THE CHECK THAT MAKES THE ORPHAN IMPOSSIBLE. Before anything exists.
    if (takenNumbers.Contains(wantedNumber))
    {
        collidedNumbers.Add(wantedNumber);
        notDuplicated.Add(source.Id);
        continue;
    }

    // The new sheet carries the source's title block, so it looks like its
    // parent rather than like Revit's default. A placeholder has none, and a
    // placeholder is not something to copy - it is a reservation in the
    // register, not a drawing.
    var titleBlockTypeId = ElementId.InvalidElementId;
    if (!source.IsPlaceholder)
    {
        foreach (var tb in new FilteredElementCollector(doc, source.Id)
                               .OfCategory(BuiltInCategory.OST_TitleBlocks)
                               .WhereElementIsNotElementType())
        {
            titleBlockTypeId = tb.GetTypeId();
            break;
        }
    }

    if (titleBlockTypeId == ElementId.InvalidElementId)
    {
        // No title block to copy - a placeholder, or a sheet somebody stripped.
        // Reported rather than guessed at: picking an arbitrary title block
        // would produce a sheet that does not match the set it belongs to.
        notDuplicated.Add(source.Id);
        continue;
    }

    ViewSheet made;
    try
    {
        made = ViewSheet.Create(doc, titleBlockTypeId);
        made.SheetNumber = wantedNumber;
        made.Name = prefix + source.Name;
    }
    catch (Exception)
    {
        notDuplicated.Add(source.Id);
        continue;
    }

    // THE READ-BACK. A created sheet whose number did not take is not the
    // sheet that was asked for, and must not be reported as one.
    if (made != null
        && doc.GetElement(made.Id) is ViewSheet
        && string.Equals(made.SheetNumber, wantedNumber, StringComparison.OrdinalIgnoreCase))
    {
        newSheetIds.Add(made.Id);
        takenNumbers.Add(wantedNumber);
    }
    else
    {
        notDuplicated.Add(source.Id);
    }
}
