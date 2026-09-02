// NOT STANDALONE. Assumes `doc`, `elements` and `sheetNumber` are in scope, and
// leaves `placed`, `distinctPositions`, `notPlaced` and `sheetProblem` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16), so a whole sheet is one undo.
//
// WHY THIS COUNTS DISTINCT POSITIONS AND NOT JUST PLACEMENTS.
//
// A version of this job computed the sheet centre INSIDE the per-view loop with
// no per-view offset. Every viewport was therefore created at the same point.
// Three views produced three viewports stacked on one spot and it reported
// "placed 3" - a true count, an unusable sheet, and nothing in the answer that
// could tell the difference.
//
// A count of placements cannot distinguish a populated sheet from a pile. So
// the centre of each created viewport is READ BACK and the distinct ones are
// counted. placed 3 with distinctPositions 1 says out loud what otherwise only
// the drawing knows.
//
// WHY THE LAYOUT IS COMPUTED BEFORE THE LOOP.
//
// Only views that will ACTUALLY be placed get a grid slot. Working the position
// out inside the loop from a running index would leave a hole in the grid for
// every skipped view, and working it out from the sheet centre is the failure
// above. Both are avoided by deciding the slots first, for the survivors only.

int placed = 0;
int distinctPositions = 0;
var notPlaced = new List<ElementId>();
string sheetProblem = "";

var wantedNumber = (sheetNumber ?? "").Trim();

var sheet = new FilteredElementCollector(doc)
    .OfClass(typeof(ViewSheet))
    .Cast<ViewSheet>()
    .FirstOrDefault(s => string.Equals(s.SheetNumber, wantedNumber,
                                       StringComparison.OrdinalIgnoreCase));

if (sheet == null)
{
    sheetProblem = "no sheet numbered '" + wantedNumber + "'";
    foreach (var element in elements) notPlaced.Add(element.Id);
}
else if (sheet.IsPlaceholder)
{
    // FIND_SHEETS hands placeholders back on purpose rather than filtering them,
    // because renumbering needs them. The promise was that the ACTION would
    // refuse visibly. This is that promise.
    sheetProblem = "sheet '" + wantedNumber + "' is a placeholder and cannot hold a view";
    foreach (var element in elements) notPlaced.Add(element.Id);
}
else
{
    // Ask FIRST, for every view, and keep only the ones Revit will accept. A
    // normal view lives on exactly one sheet: one already placed elsewhere is
    // skipped by id and never moved off the sheet it is on.
    var placeable = new List<View>();
    foreach (var element in elements)
    {
        var view = element as View;
        if (view != null && Viewport.CanAddViewToSheet(doc, sheet.Id, view.Id))
            placeable.Add(view);
        else
            notPlaced.Add(element.Id);
    }

    if (placeable.Count > 0)
    {
        // A grid sized to the survivors, so no skipped view leaves a hole.
        int columns = (int)Math.Ceiling(Math.Sqrt(placeable.Count));
        int rows = (int)Math.Ceiling(placeable.Count / (double)columns);

        var outline = sheet.Outline;
        double spanU = outline.Max.U - outline.Min.U;
        double spanV = outline.Max.V - outline.Min.V;

        // Cell centres, so nothing sits on the sheet edge.
        double cellU = spanU / columns;
        double cellV = spanV / rows;

        var centres = new List<XYZ>();

        for (int i = 0; i < placeable.Count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            double u = outline.Min.U + cellU * (column + 0.5);
            double v = outline.Max.V - cellV * (row + 0.5);

            Viewport port;
            try
            {
                port = Viewport.Create(doc, sheet.Id, placeable[i].Id, new XYZ(u, v, 0));
            }
            catch (Exception)
            {
                // Accepted the question, refused the act. One view's problem,
                // and it must not cost the others - the transaction belongs to
                // the caller and letting this out would roll back their work.
                notPlaced.Add(placeable[i].Id);
                continue;
            }

            if (port == null)
            {
                notPlaced.Add(placeable[i].Id);
                continue;
            }

            placed++;
            centres.Add(port.GetBoxCenter());
        }

        // THE READ-BACK, and the reason this fragment exists. Rounded to a
        // millimetre before comparing: two viewports a thousandth of a foot
        // apart are stacked as far as anybody looking at the sheet is
        // concerned, and exact XYZ equality would call that two positions.
        var seen = new HashSet<string>();
        foreach (var centre in centres)
        {
            seen.Add(Math.Round(centre.X * 304.8).ToString() + ","
                   + Math.Round(centre.Y * 304.8).ToString());
        }
        distinctPositions = seen.Count;
    }
}
