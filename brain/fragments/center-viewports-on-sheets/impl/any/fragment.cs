// NOT STANDALONE. Assumes `doc` and `elements` are in scope, and leaves
// `centred`, `findings`, `alreadyCentred`, `ambiguous`, `noViewport`,
// `noTitleBlock`, `severalTitleBlocks` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). A drawing set, one undo.
//
// THE TARGET IS THE TITLE BLOCK, NOT THE SHEET.
//
// A sheet's own outline is not the paper: PLACE_VIEWS_ON_SHEET centres on
// `sheet.Outline`, and a plan placed that way and then rescaled sits to one
// side. The title block instance IS the paper, so the centre of its extents on
// the sheet is where the viewport's box goes.
//
// MEASURED BEFORE IT MOVES, READ BACK AFTER.
//
// Where the viewport was and where the title block's centre is are both in the
// report, in millimetres on the paper. A viewport already within half a
// millimetre of the centre is left alone and named, so a second run moves
// nothing. A move is counted only when GetBoxCenter reads back at the target.
//
// THE BOX IS WHAT IS CENTRED, AND THE BOX IS NOT ONLY THE PLAN.
//
// A viewport's box takes in the view's annotation - elevation markers, tags,
// the crop region - so a marker out to one side pulls the drawing off-centre
// by half its distance. Nothing is hidden or cropped to compensate. The box's
// size is reported beside the title block's, so a lopsided box is visible.
//
// SKIPPED AND NAMED, NEVER GUESSED.
//
// Two viewports on one sheet: which is "the plan" is not answerable here.
// No title block, or two: there is no single paper to centre on. Each such
// sheet is named in its own list and nothing on it moves.

int centred = 0;
var alreadyCentred = new List<ElementId>();
var ambiguous = new List<ElementId>();
var noViewport = new List<ElementId>();
var noTitleBlock = new List<ElementId>();
var severalTitleBlocks = new List<ElementId>();
var refused = new List<ElementId>();
var lines = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;

// Half a millimetre on the paper, in Revit's internal feet (D-20).
double tolerance = 0.5 / 304.8;

Func<double, string> mm = feet => Math.Round(feet * 304.8).ToString(invariant);
Func<XYZ, string> at = point => "(" + mm(point.X) + ", " + mm(point.Y) + ") mm";
Func<XYZ, XYZ, double> apartOnPaper = (a, b) =>
    Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

foreach (var element in elements)
{
    var sheet = element as ViewSheet;
    if (sheet == null)
    {
        if (element != null)
        {
            refused.Add(element.Id);
            lines.Add("'" + element.Name + "' is not a sheet - left alone");
        }
        continue;
    }

    string label = sheet.SheetNumber + " " + sheet.Name;

    var viewports = new List<Viewport>();
    try
    {
        foreach (var id in sheet.GetAllViewports())
        {
            var one = doc.GetElement(id) as Viewport;
            if (one != null) viewports.Add(one);
        }
    }
    catch { }

    if (viewports.Count == 0)
    {
        noViewport.Add(sheet.Id);
        lines.Add(label + ": no viewport on it - nothing to centre");
        continue;
    }
    if (viewports.Count > 1)
    {
        ambiguous.Add(sheet.Id);
        lines.Add(label + ": " + viewports.Count
                  + " viewports - which one is the plan is not answerable, nothing moved");
        continue;
    }

    var titleBlocks = new List<Element>();
    try
    {
        foreach (var one in new FilteredElementCollector(doc, sheet.Id)
                     .OfCategory(BuiltInCategory.OST_TitleBlocks)
                     .WhereElementIsNotElementType())
            titleBlocks.Add(one);
    }
    catch { }

    if (titleBlocks.Count == 0)
    {
        noTitleBlock.Add(sheet.Id);
        lines.Add(label + ": no title block - nothing to centre on, nothing moved");
        continue;
    }
    if (titleBlocks.Count > 1)
    {
        severalTitleBlocks.Add(sheet.Id);
        lines.Add(label + ": " + titleBlocks.Count
                  + " title blocks - which is the paper is not answerable, nothing moved");
        continue;
    }

    BoundingBoxXYZ paper = null;
    try { paper = titleBlocks[0].get_BoundingBox(sheet); } catch { }
    if (paper == null)
    {
        refused.Add(sheet.Id);
        lines.Add(label + ": its title block has no extents on the sheet - nothing moved");
        continue;
    }

    var viewport = viewports[0];
    XYZ before = null;
    Outline box = null;
    try { before = viewport.GetBoxCenter(); } catch { }
    try { box = viewport.GetBoxOutline(); } catch { }
    if (before == null)
    {
        refused.Add(sheet.Id);
        lines.Add(label + ": Revit gave no centre for its viewport - nothing moved");
        continue;
    }

    var target = new XYZ((paper.Min.X + paper.Max.X) / 2.0,
                         (paper.Min.Y + paper.Max.Y) / 2.0,
                         before.Z);

    string sizes = "";
    if (box != null)
    {
        double boxWide = box.MaximumPoint.X - box.MinimumPoint.X;
        double boxHigh = box.MaximumPoint.Y - box.MinimumPoint.Y;
        double paperWide = paper.Max.X - paper.Min.X;
        double paperHigh = paper.Max.Y - paper.Min.Y;
        sizes = "; viewport box " + mm(boxWide) + " x " + mm(boxHigh)
                + " mm in a title block " + mm(paperWide) + " x " + mm(paperHigh) + " mm";
        if (boxWide > paperWide || boxHigh > paperHigh)
            sizes += " - THE BOX IS BIGGER THAN THE TITLE BLOCK";
    }

    string view = "";
    try
    {
        var placed = doc.GetElement(viewport.ViewId) as View;
        if (placed != null) view = " (" + placed.ViewType + ": " + placed.Name + ")";
    }
    catch { }

    if (apartOnPaper(before, target) <= tolerance)
    {
        alreadyCentred.Add(sheet.Id);
        lines.Add(label + view + ": already centred at " + at(before) + sizes + " - nothing moved");
        continue;
    }

    try { viewport.SetBoxCenter(target); }
    catch (Exception failure)
    {
        // Where it was and where it was asked to go, even on a refusal: run
        // with no transaction open - Revit refusing every move - this is the
        // measurement, and the only one that cannot change the model.
        refused.Add(sheet.Id);
        lines.Add(label + view + ": was " + at(before) + ", title block centre " + at(target)
                  + sizes + " - Revit refused the move: " + failure.Message);
        continue;
    }

    // Read it back. A centre that did not take would leave the report saying
    // centred and the paper saying otherwise.
    XYZ after = null;
    try { after = viewport.GetBoxCenter(); } catch { }

    if (after != null && apartOnPaper(after, target) <= tolerance)
    {
        centred++;
        lines.Add(label + view + ": was " + at(before) + ", title block centre " + at(target)
                  + ", moved by " + at(target - before) + ", reads back " + at(after) + sizes);
    }
    else
    {
        // A MOVE WHOSE READ-BACK MISSED IS PUT BACK, never left in place and
        // called refused. The transaction keeps whatever the viewport holds,
        // so "refused" over a moved viewport is a report that lies - which is
        // what ALIGN_VIEWPORTS_ACROSS_SHEETS does (row 5b-237). And if it
        // cannot be put back and read back where it was, the run STOPS: an
        // exception rolls the whole group back, so nothing is kept at all.
        XYZ back = null;
        try
        {
            viewport.SetBoxCenter(before);
            back = viewport.GetBoxCenter();
        }
        catch { }

        if (back == null || apartOnPaper(back, before) > tolerance)
            throw new InvalidOperationException(
                label + ": its viewport was moved, did not read back at the title block centre, "
                + "and could not be put back where it was - nothing on any sheet has been kept");

        refused.Add(sheet.Id);
        lines.Add(label + view + ": was " + at(before) + ", asked for " + at(target)
                  + ", reads back " + (after == null ? "nothing" : at(after))
                  + " - NOT centred, and put back where it was");
    }
}

// PROSE, NOT A COUNT - so it is `findings`, the one name the proof reads as
// a note (NOTE_KEYS, D-51). Named anything else, a negative run's "no
// viewport on it" line would read as something found.
string findings = string.Join(" | ", lines);
