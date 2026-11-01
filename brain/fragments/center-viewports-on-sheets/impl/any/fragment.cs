// NOT STANDALONE. Assumes `doc`, `elements` and `masterSheetNumber` are in
// scope, and leaves `centred`, `aligned`, `findings`, `alreadyCentred`,
// `ambiguous`, `noViewport`, `noTitleBlock`, `severalTitleBlocks`,
// `scaleMismatch`, `titleBlockElsewhere` and `refused` behind.
//
// VERSION 2 (2026-11-01) - ALIGN_VIEWPORTS_ACROSS_SHEETS MERGED IN. TWO
// TARGETS, ONE MOVE. Left out, each sheet's viewport goes to the centre of
// its OWN title block, version 1 exactly. With `masterSheetNumber`, every
// sheet's viewport goes to the MASTER's viewport centre - the same place on
// every sheet, so the plan does not jump as the set is paged through - and
// the two rules that make that meaningful are checked before anything moves:
// THE SAME SCALE (`scaleMismatch` otherwise) and THE TITLE BLOCK AT THE SAME
// ORIGIN, because a viewport centre is measured in SHEET space, not from the
// paper (`titleBlockElsewhere` otherwise - skipped, and its title block never
// moved). The master must carry one viewport and one title block, or nothing
// moves. Every safer rule of version 1 holds in both: a sheet with several
// viewports, no title block or two is named and left; a move is counted only
// when it reads back; one that does not is put back.
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
int aligned = 0;
var alreadyCentred = new List<ElementId>();
var ambiguous = new List<ElementId>();
var noViewport = new List<ElementId>();
var noTitleBlock = new List<ElementId>();
var severalTitleBlocks = new List<ElementId>();
var scaleMismatch = new List<ElementId>();
var titleBlockElsewhere = new List<ElementId>();
var refused = new List<ElementId>();
var lines = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;

// Half a millimetre on the paper, in Revit's internal feet (D-20).
double tolerance = 0.5 / 304.8;

Func<double, string> mm = feet => Math.Round(feet * 304.8).ToString(invariant);
Func<XYZ, string> at = point => "(" + mm(point.X) + ", " + mm(point.Y) + ") mm";
Func<XYZ, XYZ, double> apartOnPaper = (a, b) =>
    Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

Func<ViewSheet, List<Viewport>> viewportsOf = sheet =>
{
    var found = new List<Viewport>();
    try
    {
        foreach (var id in sheet.GetAllViewports())
        {
            var one = doc.GetElement(id) as Viewport;
            if (one != null) found.Add(one);
        }
    }
    catch { }
    return found;
};
Func<ViewSheet, List<Element>> titleBlocksOf = sheet =>
{
    var found = new List<Element>();
    try
    {
        foreach (var one in new FilteredElementCollector(doc, sheet.Id)
                     .OfCategory(BuiltInCategory.OST_TitleBlocks)
                     .WhereElementIsNotElementType())
            found.Add(one);
    }
    catch { }
    return found;
};
Func<Viewport, int> scaleOf = viewport =>
{
    try
    {
        var placedView = doc.GetElement(viewport.ViewId) as View;
        return placedView == null ? -1 : placedView.Scale;
    }
    catch { return -1; }
};
Func<Element, XYZ> originOf = titleBlock =>
{
    var point = titleBlock == null ? null : titleBlock.Location as LocationPoint;
    return point == null ? null : point.Point;
};

// VERSION 2: THE MASTER, when one is named - the same place on every sheet,
// copied from it. Left out ("none"), every sheet is centred on its own title
// block as version 1.
string wantedMaster = (masterSheetNumber ?? "").Trim();
if (string.Equals(wantedMaster, "none", StringComparison.OrdinalIgnoreCase)) wantedMaster = "";
bool toMaster = wantedMaster.Length > 0;

ViewSheet master = null;
XYZ masterCentre = null;
int masterScale = -1;
XYZ masterOrigin = null;
string masterProblem = null;

if (toMaster)
{
    // Found across the whole document, so it need not be in the set: aligning
    // a set to a sheet outside it is the normal case when one is already right.
    foreach (var sheet in new FilteredElementCollector(doc)
                 .OfClass(typeof(ViewSheet))
                 .Cast<ViewSheet>())
    {
        if (sheet != null && !sheet.IsTemplate
            && string.Equals(sheet.SheetNumber, wantedMaster, StringComparison.OrdinalIgnoreCase))
        {
            master = sheet;
            break;
        }
    }

    if (master == null)
        masterProblem = "no sheet is numbered " + wantedMaster + ", so there is no master to line up to";
    else
    {
        var masterViewports = viewportsOf(master);
        var masterTitleBlocks = titleBlocksOf(master);
        if (masterViewports.Count != 1)
            masterProblem = "the master " + master.SheetNumber + " has " + masterViewports.Count
                + " viewports - which one is the plan is not answerable";
        else if (masterTitleBlocks.Count != 1)
            masterProblem = "the master " + master.SheetNumber + " has " + masterTitleBlocks.Count
                + " title blocks - its paper cannot be compared with another sheet's";
        else
        {
            try { masterCentre = masterViewports[0].GetBoxCenter(); } catch { }
            masterScale = scaleOf(masterViewports[0]);
            masterOrigin = originOf(masterTitleBlocks[0]);
            if (masterCentre == null)
                masterProblem = "Revit gave no centre for the master's viewport";
            else if (masterOrigin == null)
                masterProblem = "the master's title block has no origin to compare the others' with";
        }
    }
}

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

    // Nothing can be lined up to nothing. Every sheet refused by name rather
    // than silently left alone.
    if (masterProblem != null)
    {
        refused.Add(sheet.Id);
        lines.Add(label + ": not moved - " + masterProblem);
        continue;
    }

    if (toMaster && sheet.Id == master.Id)
    {
        lines.Add(label + ": the master - left as it is");
        continue;
    }

    var viewports = viewportsOf(sheet);

    if (viewports.Count == 0)
    {
        noViewport.Add(sheet.Id);
        lines.Add(label + ": no viewport on it - nothing to " + (toMaster ? "line up" : "centre"));
        continue;
    }
    if (viewports.Count > 1)
    {
        ambiguous.Add(sheet.Id);
        lines.Add(label + ": " + viewports.Count
                  + " viewports - which one is the plan is not answerable, nothing moved");
        continue;
    }

    var titleBlocks = titleBlocksOf(sheet);

    if (titleBlocks.Count == 0)
    {
        noTitleBlock.Add(sheet.Id);
        lines.Add(label + ": no title block - nothing to "
                  + (toMaster ? "compare with the master's paper" : "centre on") + ", nothing moved");
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
    if (paper == null && !toMaster)
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

    string view = "";
    try
    {
        var placed = doc.GetElement(viewport.ViewId) as View;
        if (placed != null) view = " (" + placed.ViewType + ": " + placed.Name + ")";
    }
    catch { }

    XYZ target;
    string targetWord;
    if (toMaster)
    {
        // VERSION 2, from ALIGN_VIEWPORTS_ACROSS_SHEETS. TWO SCALES CANNOT LINE
        // UP BY THEIR CENTRES: the same centre puts different amounts of
        // building in the same box. Skipped and named.
        int scale = scaleOf(viewport);
        if (scale != masterScale)
        {
            scaleMismatch.Add(sheet.Id);
            lines.Add(label + view + ": at 1:" + scale + " where the master is 1:" + masterScale
                      + " - two scales cannot line up by their centres, nothing moved");
            continue;
        }

        // A VIEWPORT CENTRE IS IN SHEET SPACE, NOT PAPER SPACE. Copying the
        // master's centre lands the plan at the right sheet coordinate - and,
        // on a sheet whose title block sits somewhere else, the wrong place on
        // the paper, with nothing looking wrong until it prints. Such a sheet
        // is SKIPPED AND NAMED, and its title block is never moved:
        // ALIGN_VIEWPORTS_ACROSS_SHEETS moved it to the master's origin, a
        // second change nobody asked for.
        var origin = originOf(titleBlocks[0]);
        if (origin == null || apartOnPaper(origin, masterOrigin) > tolerance)
        {
            titleBlockElsewhere.Add(sheet.Id);
            lines.Add(label + view + ": its title block sits "
                      + (origin == null ? "with no origin to compare" : "at " + at(origin))
                      + " where the master's sits at " + at(masterOrigin)
                      + " - copying the master's centre would land the plan in the wrong place on "
                      + "the paper, so nothing moved. The title block was left where it is");
            continue;
        }

        target = new XYZ(masterCentre.X, masterCentre.Y, before.Z);
        targetWord = "master " + master.SheetNumber + "'s viewport centre";
    }
    else
    {
        target = new XYZ((paper.Min.X + paper.Max.X) / 2.0,
                         (paper.Min.Y + paper.Max.Y) / 2.0,
                         before.Z);
        targetWord = "title block centre";
    }

    string sizes = "";
    if (box != null && paper != null)
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

    if (apartOnPaper(before, target) <= tolerance)
    {
        alreadyCentred.Add(sheet.Id);
        lines.Add(label + view + ": already at the " + targetWord + ", " + at(before) + sizes
                  + " - nothing moved");
        continue;
    }

    try { viewport.SetBoxCenter(target); }
    catch (Exception failure)
    {
        // Where it was and where it was asked to go, even on a refusal: run
        // with no transaction open - Revit refusing every move - this is the
        // measurement, and the only one that cannot change the model.
        refused.Add(sheet.Id);
        lines.Add(label + view + ": was " + at(before) + ", " + targetWord + " " + at(target)
                  + sizes + " - Revit refused the move: " + failure.Message);
        continue;
    }

    // Read it back. A centre that did not take would leave the report saying
    // moved and the paper saying otherwise.
    XYZ after = null;
    try { after = viewport.GetBoxCenter(); } catch { }

    if (after != null && apartOnPaper(after, target) <= tolerance)
    {
        if (toMaster) aligned++; else centred++;
        lines.Add(label + view + ": was " + at(before) + ", " + targetWord + " " + at(target)
                  + ", moved by " + at(target - before) + ", reads back " + at(after) + sizes);
    }
    else
    {
        // A MOVE WHOSE READ-BACK MISSED IS PUT BACK, never left in place and
        // called refused. The transaction keeps whatever the viewport holds,
        // so "refused" over a moved viewport is a report that lies - which is
        // what ALIGN_VIEWPORTS_ACROSS_SHEETS did (row 5b-237); its version
        // here puts it back too. And if it cannot be put back and read back
        // where it was, the run STOPS: an exception rolls the whole group
        // back, so nothing is kept at all.
        XYZ back = null;
        try
        {
            viewport.SetBoxCenter(before);
            back = viewport.GetBoxCenter();
        }
        catch { }

        if (back == null || apartOnPaper(back, before) > tolerance)
            throw new InvalidOperationException(
                label + ": its viewport was moved, did not read back at the " + targetWord + ", "
                + "and could not be put back where it was - nothing on any sheet has been kept");

        refused.Add(sheet.Id);
        lines.Add(label + view + ": was " + at(before) + ", asked for " + at(target)
                  + ", reads back " + (after == null ? "nothing" : at(after))
                  + " - NOT moved, and put back where it was");
    }
}

// PROSE, NOT A COUNT - so it is `findings`, the one name the proof reads as
// a note (NOTE_KEYS, D-51). Named anything else, a negative run's "no
// viewport on it" line would read as something found.
string findings = string.Join(" | ", lines);
