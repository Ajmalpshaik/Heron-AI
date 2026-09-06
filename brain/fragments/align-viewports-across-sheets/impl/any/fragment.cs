// NOT STANDALONE. Assumes `doc`, `elements` and `masterSheetNumber` are in
// scope, and leaves `aligned`, `scaleMismatch`, `titleBlockMoved`, `ambiguous`
// and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). A drawing set, one undo.
//
// A VIEWPORT CENTRE IS IN SHEET SPACE, NOT PAPER SPACE.
//
// It is measured from the sheet's origin, not from the title block. So if one
// sheet's title block sits somewhere else, copying the centre puts the plan at
// the right sheet coordinate and the wrong place on the paper - and nothing
// looks wrong until it prints. Title blocks whose origin differs from the
// master's are reported, and moved only when the caller has asked for it by
// passing them in as part of the set.
//
// SCALE MUST MATCH OR ALIGNMENT MEANS NOTHING.
//
// Two viewports at different scales cannot line up by their centres: the same
// centre puts different amounts of building in the same box. Those sheets are
// skipped and named rather than moved into a different kind of wrong.
//
// SEVERAL VIEWPORTS ON A SHEET IS AMBIGUOUS.
//
// Which of three views is "the plan" is not answerable here, and moving the
// wrong one is worse than moving none.

int aligned = 0;
var scaleMismatch = new List<ElementId>();
var titleBlockMoved = new List<ElementId>();
var ambiguous = new List<ElementId>();
var refused = new List<ElementId>();

string wantedMaster = (masterSheetNumber ?? "").Trim();

Func<ViewSheet, Viewport> onlyViewportOf = sheet =>
{
    var viewports = new List<Viewport>();
    try
    {
        foreach (var id in sheet.GetAllViewports())
        {
            var viewport = doc.GetElement(id) as Viewport;
            if (viewport != null) viewports.Add(viewport);
        }
    }
    catch { }
    return viewports.Count == 1 ? viewports[0] : null;
};

Func<ViewSheet, Element> titleBlockOf = sheet =>
{
    try
    {
        foreach (var element in new FilteredElementCollector(doc, sheet.Id)
                     .OfCategory(BuiltInCategory.OST_TitleBlocks)
                     .WhereElementIsNotElementType())
            return element;
    }
    catch { }
    return null;
};

// The master. Found across the whole document, so it does not have to be in
// the set being aligned - aligning a set to a sheet outside it is the normal
// case when one sheet is already right.
ViewSheet master = null;
foreach (var sheet in new FilteredElementCollector(doc)
             .OfClass(typeof(ViewSheet))
             .Cast<ViewSheet>())
{
    if (string.Equals(sheet.SheetNumber, wantedMaster, StringComparison.OrdinalIgnoreCase))
    {
        master = sheet;
        break;
    }
}

Viewport masterViewport = master != null ? onlyViewportOf(master) : null;

if (master == null || masterViewport == null)
{
    // Nothing can be aligned to nothing. Refused by name rather than silently
    // doing nothing to a whole set.
    foreach (var element in elements) if (element != null) refused.Add(element.Id);
}
else
{
    XYZ masterCentre = null;
    try { masterCentre = masterViewport.GetBoxCenter(); } catch { }

    int masterScale = -1;
    try
    {
        var view = doc.GetElement(masterViewport.ViewId) as View;
        if (view != null) masterScale = view.Scale;
    }
    catch { }

    XYZ masterTitleBlockOrigin = null;
    var masterTitleBlock = titleBlockOf(master);
    if (masterTitleBlock != null)
    {
        var point = masterTitleBlock.Location as LocationPoint;
        if (point != null) masterTitleBlockOrigin = point.Point;
    }

    if (masterCentre == null)
    {
        foreach (var element in elements) if (element != null) refused.Add(element.Id);
    }
    else
    {
        foreach (var element in elements)
        {
            var sheet = element as ViewSheet;
            if (sheet == null) { if (element != null) refused.Add(element.Id); continue; }
            if (sheet.Id == master.Id) continue;

            var viewport = onlyViewportOf(sheet);
            if (viewport == null) { ambiguous.Add(sheet.Id); continue; }

            int scale = -1;
            try
            {
                var view = doc.GetElement(viewport.ViewId) as View;
                if (view != null) scale = view.Scale;
            }
            catch { }

            if (scale != masterScale) { scaleMismatch.Add(sheet.Id); continue; }

            // The title block origin, which is what makes the centre mean the
            // same thing on this sheet as on the master.
            var titleBlock = titleBlockOf(sheet);
            if (masterTitleBlockOrigin != null && titleBlock != null)
            {
                var point = titleBlock.Location as LocationPoint;
                if (point != null && !point.Point.IsAlmostEqualTo(masterTitleBlockOrigin))
                {
                    try
                    {
                        point.Point = masterTitleBlockOrigin;
                        titleBlockMoved.Add(sheet.Id);
                    }
                    catch { }
                }
            }

            try { viewport.SetBoxCenter(masterCentre); }
            catch { refused.Add(sheet.Id); continue; }

            // Read it back. A centre that did not take leaves the sheet
            // looking aligned in the report and not on the paper.
            XYZ now = null;
            try { now = viewport.GetBoxCenter(); } catch { }

            if (now != null && now.IsAlmostEqualTo(masterCentre)) aligned++;
            else refused.Add(sheet.Id);
        }
    }
}
