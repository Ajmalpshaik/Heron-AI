// NOT STANDALONE. Assumes `doc` is in scope; leaves `elements`, `findings` and
// `placeholders` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// A SHEET VIEW TEMPLATE IS NOT A SHEET. Revit lets a project hold view
// templates for sheets, and they come back from the same collector looking
// exactly like sheets - with a number, a name and no drawings on them. Listing
// them puts rows in the drawing list that can never be issued, so they are
// dropped on IsTemplate.
//
// A PLACEHOLDER SHEET IS A SHEET, AND IT IS ALSO NOT ONE. It exists so a
// drawing register can carry a number before anybody models what goes on it;
// it has no titleblock and cannot be printed. Reporting it as an ordinary
// sheet overstates what the project can issue, and hiding it loses a row the
// register deliberately contains. So it is listed AND flagged.
//
// SORTED BY SHEET NUMBER, BECAUSE THAT IS THE DRAWING LIST. A collector's
// order is element creation order, which is the order somebody happened to
// make them in and matches no register anybody has ever printed.

var elements = new List<Element>();
var findings = new List<string>();
var placeholders = new List<ElementId>();

var sheets = new List<ViewSheet>();

foreach (var sheet in new FilteredElementCollector(doc)
                          .OfClass(typeof(ViewSheet))
                          .Cast<ViewSheet>())
{
    if (sheet == null) continue;
    if (sheet.IsTemplate) continue;

    sheets.Add(sheet);
}

// String comparison, deliberately: sheet numbers are not numbers. "M-101",
// "A2.03" and "SK-07" are all normal, and anything that tried to parse them
// would have to decide what to do with the ones it could not.
sheets.Sort(delegate (ViewSheet a, ViewSheet b)
{
    return string.Compare(a.SheetNumber ?? "", b.SheetNumber ?? "",
                          StringComparison.OrdinalIgnoreCase);
});

foreach (var sheet in sheets)
{
    elements.Add(sheet);

    // GetAllPlacedViews returns the views placed on this sheet. A titleblock,
    // a legend component and a schedule are not views and are not counted -
    // which is right: "how many drawings are on this sheet" is the question,
    // and a titleblock is not a drawing.
    var placed = sheet.GetAllPlacedViews();
    var placedCount = placed == null ? 0 : placed.Count;

    var note = placedCount == 0 ? " - EMPTY" : "";
    if (sheet.IsPlaceholder)
    {
        placeholders.Add(sheet.Id);
        note = " - PLACEHOLDER, no titleblock and cannot be printed";
    }

    findings.Add(string.Format("{0}  {1}  ({2} view{3}){4}",
                               sheet.SheetNumber,
                               sheet.Name,
                               placedCount,
                               placedCount == 1 ? "" : "s",
                               note));
}
