// NOT STANDALONE. Assumes `doc` is in scope; leaves `elements`, `findings` and
// `onNoSheets` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE SHEETS ARE COLLECTED ONCE AND ASKED PER REVISION, not the other way
// round. A project with 40 revisions and 300 sheets would otherwise mean 40
// collector passes over the whole model, and the cost of this check is what
// decides whether anybody runs it before an issue.
//
// GetAllRevisionIds ON A SHEET RETURNS BOTH WAYS A REVISION GETS THERE - a
// cloud drawn on the sheet or on a view placed on it, AND a revision ticked on
// the sheet by hand with no cloud anywhere. Both print. A sheet showing a
// revision is issued under it however it got there, so both are counted, and
// separating them would answer a question nobody asked while getting the
// printed set wrong.
//
// A REVISION ON NO SHEETS IS THE ONE WORTH FINDING. It is either not yet
// clouded, or clouded on a view that is on no sheet - and the second is
// invisible in Revit until somebody notices the drawing went out unmarked.

var elements = new List<Element>();
var findings = new List<string>();
var onNoSheets = new List<ElementId>();

// Every sheet, once.
var sheets = new List<ViewSheet>();
foreach (var sheet in new FilteredElementCollector(doc)
                          .OfClass(typeof(ViewSheet))
                          .Cast<ViewSheet>())
{
    if (sheet != null && !sheet.IsTemplate) sheets.Add(sheet);
}

// Which revisions each sheet shows, worked out once per sheet rather than once
// per revision per sheet.
var shownOn = new Dictionary<ElementId, List<ViewSheet>>();

foreach (var sheet in sheets)
{
    var shown = sheet.GetAllRevisionIds();
    if (shown == null) continue;

    foreach (var revisionId in shown)
    {
        if (revisionId == null || revisionId == ElementId.InvalidElementId) continue;
        if (!shownOn.ContainsKey(revisionId)) shownOn[revisionId] = new List<ViewSheet>();
        shownOn[revisionId].Add(sheet);
    }
}

// In sequence, because that is the order the revision schedule prints in and
// the order somebody reads a revision history.
foreach (var revisionId in Revision.GetAllRevisionIds(doc))
{
    var revision = doc.GetElement(revisionId) as Revision;
    if (revision == null) continue;

    elements.Add(revision);

    var carrying = shownOn.ContainsKey(revisionId) ? shownOn[revisionId] : new List<ViewSheet>();

    if (carrying.Count == 0)
    {
        onNoSheets.Add(revisionId);
        findings.Add(string.Format(
            "{0}  {1}  - ON NO SHEETS. Either not clouded yet, or clouded on a view "
            + "that is on no sheet",
            revision.RevisionDate, revision.Description));
        continue;
    }

    // Sorted by sheet number so the list reads as a drawing register. Numbers
    // are compared as text because sheet numbers are not numbers - "M-101"
    // and "A2.03" are both normal.
    carrying.Sort(delegate (ViewSheet a, ViewSheet b)
    {
        return string.Compare(a.SheetNumber ?? "", b.SheetNumber ?? "",
                              StringComparison.OrdinalIgnoreCase);
    });

    var numbers = new List<string>();
    foreach (var sheet in carrying) numbers.Add(sheet.SheetNumber);

    findings.Add(string.Format("{0}  {1}  - {2} sheet{3}: {4}",
                               revision.RevisionDate,
                               revision.Description,
                               carrying.Count,
                               carrying.Count == 1 ? "" : "s",
                               string.Join(", ", numbers.ToArray())));
}
