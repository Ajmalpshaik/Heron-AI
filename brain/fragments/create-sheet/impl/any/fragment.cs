// NOT STANDALONE. Assumes `doc`, `sheetNumber`, `sheetName` and
// `titleblockTypeId` are in scope; leaves `created`, `refused` and
// `noTitleblock` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE NUMBER IS CHECKED BEFORE THE SHEET IS MADE, and the order is the whole
// point. ViewSheet.Create succeeds and the number is assigned AFTERWARDS, so a
// clash throws with the sheet already in the model - unnumbered, or carrying
// whatever default Revit picked. That is a blank row in the drawing register
// which nobody notices until an issue. Looking first costs one collector pass.
//
// COMPARED WITHOUT CASE, because "M-101" and "m-101" are the same drawing to
// everybody except a string comparison. Revit itself refuses the pair.
//
// A SHEET WITH NO TITLEBLOCK IS LEGAL AND IS REPORTED. Passing
// InvalidElementId is how the API is told "no titleblock", and it produces a
// sheet that cannot be printed with a border. That is occasionally what
// somebody wants and is usually a mistake, so it is flagged rather than
// refused - refusing would block a legitimate job, and silence would hand back
// a sheet that looks finished and prints bare.

ElementId created = null;
string refused = null;
var noTitleblock = false;

var wanted = (sheetNumber ?? "").Trim();

if (wanted.Length == 0)
{
    refused = "a sheet needs a number - Revit has no unnumbered sheets";
}
else
{
    // Every existing number, including sheets that are only placeholders: a
    // placeholder holds its number precisely so nothing else takes it, and
    // ignoring them would collide with the register on purpose.
    string clash = null;

    foreach (var sheet in new FilteredElementCollector(doc)
                              .OfClass(typeof(ViewSheet))
                              .Cast<ViewSheet>())
    {
        if (sheet == null) continue;
        if (sheet.IsTemplate) continue;

        if (string.Equals(sheet.SheetNumber ?? "", wanted,
                          StringComparison.OrdinalIgnoreCase))
        {
            clash = sheet.Name;
            break;
        }
    }

    if (clash != null)
    {
        refused = string.Format(
            "sheet number {0} is already used by \"{1}\" - Revit refuses a duplicate, "
            + "and creating it first and numbering it after leaves an unnumbered sheet "
            + "in the model", wanted, clash);
    }
    else
    {
        var titleblock = titleblockTypeId;
        if (titleblock == null) titleblock = ElementId.InvalidElementId;
        if (titleblock == ElementId.InvalidElementId) noTitleblock = true;

        var sheet = ViewSheet.Create(doc, titleblock);

        if (sheet == null)
        {
            refused = "Revit declined to create the sheet";
        }
        else
        {
            sheet.SheetNumber = wanted;

            // An empty name is allowed by the API and gives a sheet whose title
            // line is blank. Left as Revit's default rather than written blank,
            // so the register shows something rather than nothing.
            var name = (sheetName ?? "").Trim();
            if (name.Length > 0) sheet.Name = name;

            created = sheet.Id;
        }
    }
}
