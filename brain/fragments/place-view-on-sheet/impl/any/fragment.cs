// NOT STANDALONE. Assumes `doc`, `sheetId`, `views`, `elements`, `at` and
// `oldViewId` are in scope; leaves `placed`, `refused`, `swapped`, `notDone`,
// `placedCount`, `distinctPositions`, `sheetProblem`, `layout` and `placedAt`
// behind.
//
// VERSION 4 (2026-11-01) - PLACE_SCHEDULE_ON_SHEET MERGED IN. THE PLACING JOB
// PLACES SCHEDULES TOO. A view goes on a sheet as a Viewport; a schedule goes
// on as a ScheduleSheetInstance, a different call - this card used to refuse
// schedules correctly with nothing to hand them to. Now a schedule in `views`
// or handed on in `elements` is placed by ScheduleSheetInstance.Create:
//
//   THE POINT IS A VIEWPORT'S CENTRE BUT A SCHEDULE'S TOP-LEFT CORNER. A
//   schedule hangs from its corner and grows down and to the right, so a
//   schedule placed where a viewport would be centred sits half off the
//   sheet if the point was meant as its centre. `layout` says which applied,
//   and `placedAt` reads each one back in millimetres - the corner for a
//   schedule, the centre for a viewport - from the instance, not the request.
//
//   ONE SHEET PER VIEW, MANY SHEETS PER SCHEDULE - REVIT'S RULE. A plan or a
//   section can be on one sheet; a schedule can be placed again and again, so
//   a schedule already on another sheet is NOT refused, where a view is. An
//   accidental double placement is possible for a schedule in a way it is not
//   for a drawing, which is why each placement is named by sheet.
//
//   A SCHEDULE TEMPLATE is refused by name - nothing to print - and Revit's
//   own refusal or a placement that made nothing is said in `notDone`.
//
// VERSION 3, REVIEW FIX OF 2026-11-01 - THE VIEWS MAY BE HANDED ON, as
// PLACE_VIEWS_ON_SHEET took them: `elements`, from FIND_UNPLACED_VIEWS or a
// selection, is used ONLY WHEN `views` NAMES NONE. An explicit `views` list
// always wins and `elements` is then not read at all, so a selection can
// never add a view the caller did not name. Of what is handed on, each
// element that is not a view is refused BY ID in `refused` and counted in
// `notDone`, and never placed. Both left empty is refused in `notDone`.
// `views` binds an empty list when left out - HeronBindingNote.AbsentValue's
// IList<View> case, added for this - and `elements` binds empty when nothing
// fills it (EmptyWhenAbsent).
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// TWO JOBS, ONE QUESTION - WHERE DOES THE VIEW GO. With no `oldViewId` this
// is the placing job, on the sheet `sheetId`: one view at the point `at`, or
// several laid out on a grid (version 3, below). With an `oldViewId` it is
// version 2's swap, unchanged but for the read-back of where each one landed:
// the one view in `views` goes where the old view sits, on every sheet the
// old view is on - or only on `sheetId` when one is named - and the old view
// comes off those sheets. The old view stays in the project; only its place
// on the sheet is taken.
//
// ASKED BEFORE ATTEMPTED. `CanAddViewToSheet` is Revit's own answer and it
// covers what would otherwise be exceptions - a view already on another sheet,
// a template, a schedule that needs a different call entirely. Asking turns
// each into a reported outcome and lets the other twenty-nine views place.
//
// A VIEW CAN BE ON ONLY ONE SHEET. That is the commonest refusal here and the
// fix is not in this fragment: duplicate the view and place the copy. Worth
// knowing before reading a batch that placed eleven of thirty. A legend and a
// schedule are the two that can be on many sheets.
//
// VERSION 3 (2026-11-01) - PLACE_VIEWS_ON_SHEET MERGED IN.
//
// SEVERAL VIEWS ARE LAID OUT ON A GRID, NEVER STACKED. Versions 1 and 2 put
// every viewport at the one point `at`, and PLACE_VIEWS_ON_SHEET was written
// because a job that did that reported "placed 3" over three viewports on one
// spot - a true count and an unusable sheet. The two rules were opposite and
// the safer is kept (the owner's answer, 2026-11-01): several views go on a
// grid across the sheet's outline, sized to the views that WILL place - each
// is asked first, so no refused view leaves a hole. A point given is used
// only when ONE view is placed; with several it is not used, and `layout`
// says so. Left out (0,0,0), one view goes to the sheet's centre.
//
// THE READ-BACK. `distinctPositions` counts the viewports this run made that
// sit in DIFFERENT places, read back from the model and rounded to the
// millimetre - placed 3 with distinctPositions 1 says out loud what otherwise
// only the drawing knows. Counted for the swap job as well, per sheet.
//
// THE SHEET. The binder finds `sheetId` by its number, capitals ignored, and
// refuses a number no sheet has before this runs. What this card still has to
// refuse is said in `sheetProblem`: no sheet named, a PLACEHOLDER sheet -
// FIND_SHEETS hands placeholders back on purpose, on the promise that the
// action refuses them by name - and an id that is not a sheet.

var placed = new List<ElementId>();
var refused = new List<ElementId>();
var swapped = new List<string>();
var notDone = new List<string>();
int placedCount = 0;
int distinctPositions = 0;
string sheetProblem = "";
string layout = "";
var placedAt = new List<string>();

// Where each viewport this run made now sits - sheet, then millimetres -
// filled from the model after each one is made.
var landedAt = new HashSet<string>();
Action<ElementId, XYZ> noteLanding = (onSheet, point) =>
{
    if (point == null) return;
    landedAt.Add(onSheet.ToString() + ":" + Math.Round(point.X * 304.8).ToString() + ","
                 + Math.Round(point.Y * 304.8).ToString());
};

// THE VIEWS TO WORK ON. Named ones win; only when none is named are the
// elements handed on read, and only their views are kept.
var chosen = new List<View>();
var namedAny = false;
if (views != null)
    foreach (var view in views)
        if (view != null) { chosen.Add(view); namedAny = true; }

if (!namedAny && elements != null)
{
    var notViews = new List<ElementId>();
    var seenIds = new HashSet<ElementId>();
    foreach (var element in elements)
    {
        if (element == null || !seenIds.Add(element.Id)) continue;
        var asView = element as View;
        if (asView != null) chosen.Add(asView);
        else notViews.Add(element.Id);
    }
    if (notViews.Count > 0)
    {
        refused.AddRange(notViews);
        notDone.Add(notViews.Count + " of the elements handed on are not views and were not placed - "
            + "ids " + string.Join(", ", notViews.Select(id => id.ToString()).ToArray())
            + ". Only views go on a sheet; name them in views, or hand on what FIND_UNPLACED_VIEWS found.");
    }
}

bool noViews = chosen.Count == 0;
if (noViews)
    notDone.Add("No view was named and none was handed on, so nothing was done. Name the views, "
        + "or select them, or run FIND_UNPLACED_VIEWS first.");

bool replacing = oldViewId != null && oldViewId != ElementId.InvalidElementId;

if (noViews)
{
    // Nothing to place or swap in; said above.
}
else if (!replacing)
{
    // THE PLACING JOB. `sheetId` may be left out since version 2, because
    // "every sheet the old view is on" needs no sheet named - but placing
    // still does.
    var target = sheetId == null || sheetId == ElementId.InvalidElementId
        ? null : doc.GetElement(sheetId) as ViewSheet;

    if (sheetId == null || sheetId == ElementId.InvalidElementId)
        sheetProblem = "No sheet was named, so nothing was placed. Name the sheet by its "
                     + "number - A101.";
    else if (target == null)
        sheetProblem = "What was named as the sheet is not a sheet in " + doc.Title
                     + ", so nothing was placed.";
    else if (target.IsPlaceholder)
        // VERSION 3, from PLACE_VIEWS_ON_SHEET: refused BY NAME, never failed
        // obscurely. A placeholder has no title block and cannot hold a view.
        sheetProblem = "Sheet " + target.SheetNumber + " is a placeholder and cannot hold a "
                     + "view, so nothing was placed.";

    if (sheetProblem.Length > 0)
    {
        foreach (var view in chosen)
            refused.Add(view.Id);
        notDone.Add(sheetProblem);
    }
    else
    {
        // ASK FIRST, for every view, and keep only the ones Revit will accept,
        // so the grid is sized to the views that will place and a refused one
        // leaves no hole (VERSION 3).
        var placeable = new List<View>();
        foreach (var view in chosen)
        {
            // VERSION 4: A SCHEDULE GOES ON AS A ScheduleSheetInstance, not a
            // viewport, so CanAddViewToSheet - which answers no for every
            // schedule - is not its question. A schedule TEMPLATE has nothing
            // to print and is refused by name; any other schedule is placed,
            // even one already on another sheet or on this one - a schedule may
            // be on many sheets, which is Revit's rule, not this card's.
            var asSchedule = view as ViewSchedule;
            if (asSchedule != null)
            {
                if (asSchedule.IsTemplate)
                {
                    refused.Add(view.Id);
                    notDone.Add("'" + view.Name + "' is a schedule TEMPLATE, not a schedule - there "
                        + "is nothing to print, so it was not placed.");
                    continue;
                }
                placeable.Add(view);
                continue;
            }

            if (!Viewport.CanAddViewToSheet(doc, sheetId, view.Id))
            {
                refused.Add(view.Id);
                continue;
            }
            placeable.Add(view);
        }

        // WHERE EACH ONE GOES, decided before any is made. One view and a
        // point: the point. Otherwise a grid over the sheet's outline, cell
        // centres so nothing sits on the edge - one view and no point is the
        // one cell's centre, the middle of the sheet.
        bool pointGiven = at != null && !at.IsZeroLength();
        var slots = new List<XYZ>();
        if (placeable.Count == 1 && pointGiven)
        {
            slots.Add(at);
            layout = "1 view, at the point given.";
        }
        else if (placeable.Count > 0)
        {
            int columns = (int)Math.Ceiling(Math.Sqrt(placeable.Count));
            int rows = (int)Math.Ceiling(placeable.Count / (double)columns);

            var outline = target.Outline;
            double spanU = outline.Max.U - outline.Min.U;
            double spanV = outline.Max.V - outline.Min.V;
            double cellU = spanU / columns;
            double cellV = spanV / rows;

            for (int i = 0; i < placeable.Count; i++)
            {
                int column = i % columns;
                int row = i / columns;
                slots.Add(new XYZ(outline.Min.U + cellU * (column + 0.5),
                                  outline.Max.V - cellV * (row + 0.5), 0));
            }

            layout = placeable.Count == 1
                ? "1 view, at the centre of sheet " + target.SheetNumber + " - no point was given."
                : placeable.Count + " views on a grid of " + columns + " x " + rows
                  + " across sheet " + target.SheetNumber + "."
                  + (pointGiven ? " The point given was not used - a point places one view, "
                                  + "and several on one point would sit on top of each other."
                                : "");
        }

        // VERSION 4. Which point is which, said whenever a schedule is placed.
        if (placeable.Any(v => v is ViewSchedule))
            layout += " A schedule hangs from its TOP-LEFT corner, so its corner is at the "
                    + (placeable.Count == 1 && pointGiven ? "point" : "cell's centre")
                    + " and it grows down and to the right; a view's viewport is CENTRED on it.";

        for (int i = 0; i < placeable.Count; i++)
        {
            var view = placeable[i];

            // VERSION 4, from PLACE_SCHEDULE_ON_SHEET. A schedule's point is its
            // instance's TOP-LEFT corner, read back from the instance itself -
            // the first guess is usually the centre, and a schedule placed by
            // its centre sits half off the sheet.
            if (view is ViewSchedule)
            {
                try
                {
                    var instance = ScheduleSheetInstance.Create(doc, sheetId, view.Id, slots[i]);
                    if (instance == null)
                    {
                        refused.Add(view.Id);
                        notDone.Add("'" + view.Name + "' on sheet " + target.SheetNumber + " - Revit "
                            + "reported no error and placed nothing.");
                        continue;
                    }
                    placed.Add(instance.Id);
                    XYZ corner = null;
                    try { corner = instance.Point; } catch (Exception) { }
                    noteLanding(sheetId, corner);
                    placedAt.Add(corner == null
                        ? "'" + view.Name + "' on sheet " + target.SheetNumber + " - placed, and its "
                          + "position could not be read back"
                        : string.Format("'{0}' on sheet {1} at {2:0.#} mm, {3:0.#} mm from the sheet "
                          + "origin - its top-left corner", view.Name, target.SheetNumber,
                          corner.X * 304.8, corner.Y * 304.8));
                }
                catch (Exception ex)
                {
                    // One schedule's problem, said in Revit's own words; let out,
                    // it would roll back the caller's work.
                    refused.Add(view.Id);
                    notDone.Add("'" + view.Name + "' on sheet " + target.SheetNumber
                        + " - Revit refused: " + ex.Message);
                }
                continue;
            }

            try
            {
                var viewport = Viewport.Create(doc, sheetId, view.Id, slots[i]);

                // A null back without an exception is the silent no-op shape this
                // repository has met on the move and parameter paths. Never counted.
                if (viewport == null) refused.Add(view.Id);
                else
                {
                    placed.Add(viewport.Id);
                    var centre = viewport.GetBoxCenter();
                    noteLanding(sheetId, centre);
                    // VERSION 4: where each one landed, read back, beside a
                    // schedule's corner - a viewport is placed by its CENTRE.
                    if (centre != null)
                        placedAt.Add(string.Format("'{0}' on sheet {1} at {2:0.#} mm, {3:0.#} mm "
                            + "from the sheet origin - its viewport's centre", view.Name,
                            target.SheetNumber, centre.X * 304.8, centre.Y * 304.8));
                }
            }
            catch (Exception)
            {
                // Accepted the question, refused the act. One view's problem,
                // and it must not cost the others - the transaction belongs to
                // the caller and letting this out would roll back their work.
                refused.Add(view.Id);
            }
        }
    }
}
else
{
    // VERSION 2: PUT THIS VIEW WHERE THAT ONE IS (FRAGMENT-ISSUES row 5b-540).
    //
    // A VIEWPORT'S VIEW IS NOT CHANGED, IT IS REPLACED. On 2020 to 2022 the
    // API has no setter for Viewport.ViewId; 2023 added one. One path for
    // every release - a new viewport at the old box centre, the old one
    // deleted - so a run on 2020 and a run on 2027 are the same run and are
    // read back the same way.
    //
    // A SCHEDULE IS NOT A VIEWPORT. It sits on a sheet as a
    // ScheduleSheetInstance and hangs from its top-left corner, so the old
    // instance's Point is where the new one goes - its box centre would move
    // the corner whenever the two schedules differ in size.
    //
    // ONE SHEET AT A TIME, ALL OR NOTHING. Each sheet is a SubTransaction: a
    // sheet where any step fails or the read-back disagrees is rolled back
    // whole, the old view stays exactly where it was, and the sheet is named
    // in notDone with the reason. Never silently skipped. The caller's one
    // transaction around the whole run is still one undo.
    //
    // `at` IS AN OFFSET HERE, in millimetres, from where the old view sat.
    // 0,0,0 - the job - is exactly there.
    var oldView = doc.GetElement(oldViewId) as View;
    var named = new List<View>(chosen);
    var newView = named.Count == 1 ? named[0] : null;

    string stop = null;
    if (oldView == null)
        stop = "What was named as the old view is not a view in " + doc.Title
             + ". Nothing was changed.";
    else if (named.Count != 1)
        stop = named.Count == 0
            ? "No new view was named to put where '" + oldView.Name + "' is. Nothing was changed."
            : named.Count + " new views were named, and one place holds one view. Name the one "
              + "to put where '" + oldView.Name + "' is. Nothing was changed.";
    else if (newView.Id == oldView.Id)
        stop = "The old and the new view are both '" + oldView.Name + "', so there is nothing "
             + "to replace. Nothing was changed.";
    else if (newView.IsTemplate)
        stop = "'" + newView.Name + "' is a view TEMPLATE, and a template never goes on a "
             + "sheet. Nothing was changed.";
    else if (newView is ViewSheet)
        stop = "'" + newView.Name + "' is a sheet, and a sheet cannot go on a sheet. Nothing "
             + "was changed.";
    else if ((oldView is ViewSchedule) != (newView is ViewSchedule))
        stop = "'" + oldView.Name + "' and '" + newView.Name + "' are not the same kind - one "
             + "is a schedule and the other is not, and a schedule sits on a sheet by its "
             + "corner where a view sits by its centre. Nothing was changed.";

    var isSchedule = oldView is ViewSchedule;
    if (stop == null && isSchedule)
    {
        var oldSchedule = (ViewSchedule)oldView;
        var newSchedule = (ViewSchedule)newView;
        if (oldSchedule.IsTitleblockRevisionSchedule || newSchedule.IsTitleblockRevisionSchedule)
            stop = "A title block's revision schedule belongs to the title block and moves "
                 + "with it - it is not swapped here. Nothing was changed.";
#if REVIT2020 || REVIT2021
#else
        // SPLIT SCHEDULES ARE NOT REBUILT. From 2022 a schedule can be split
        // into pieces, each its own instance with the same ScheduleId, and
        // putting the whole new schedule at every piece would show it twice.
        else if (oldSchedule.IsSplit())
            stop = "'" + oldView.Name + "' is split into " + oldSchedule.GetSegmentCount()
                 + " pieces on its sheet, and Heron does not rebuild the pieces. Merge it "
                 + "back into one, then run this again. Nothing was changed.";
        else if (newSchedule.IsSplit())
            stop = "'" + newView.Name + "' is split into " + newSchedule.GetSegmentCount()
                 + " pieces, and Heron places a schedule whole. Merge it back into one, then "
                 + "run this again. Nothing was changed.";
#endif
    }

    if (stop != null)
    {
        notDone.Add(stop);
    }
    else
    {
        // Where the OLD view sits, sheet by sheet, and where the NEW one sat
        // before this run began - read once, before anything changes.
        var oldOn = new Dictionary<ElementId, List<Element>>();
        var newWasOn = new HashSet<ElementId>();
        ViewSheet newFirstSheet = null;

        if (isSchedule)
        {
            foreach (var instance in new FilteredElementCollector(doc)
                                         .OfClass(typeof(ScheduleSheetInstance))
                                         .Cast<ScheduleSheetInstance>())
            {
                if (instance == null || instance.IsTitleblockRevisionSchedule) continue;
                if (instance.ScheduleId == oldView.Id)
                {
                    if (!oldOn.ContainsKey(instance.OwnerViewId))
                        oldOn[instance.OwnerViewId] = new List<Element>();
                    oldOn[instance.OwnerViewId].Add(instance);
                }
                else if (instance.ScheduleId == newView.Id)
                {
                    newWasOn.Add(instance.OwnerViewId);
                }
            }
        }
        else
        {
            foreach (var port in new FilteredElementCollector(doc)
                                     .OfClass(typeof(Viewport))
                                     .Cast<Viewport>())
            {
                if (port == null) continue;
                if (port.ViewId == oldView.Id)
                {
                    if (!oldOn.ContainsKey(port.SheetId))
                        oldOn[port.SheetId] = new List<Element>();
                    oldOn[port.SheetId].Add(port);
                }
                else if (port.ViewId == newView.Id)
                {
                    newWasOn.Add(port.SheetId);
                    if (newFirstSheet == null) newFirstSheet = doc.GetElement(port.SheetId) as ViewSheet;
                }
            }
        }

        var sheets = new List<ViewSheet>();
        foreach (var id in oldOn.Keys)
        {
            var sheet = doc.GetElement(id) as ViewSheet;
            if (sheet != null) sheets.Add(sheet);
        }
        sheets.Sort((a, b) => string.Compare(a.SheetNumber, b.SheetNumber,
                                             StringComparison.OrdinalIgnoreCase));

        bool oneSheet = sheetId != null && sheetId != ElementId.InvalidElementId;
        if (oneSheet)
        {
            var asked = doc.GetElement(sheetId) as ViewSheet;
            sheets = sheets.Where(s => s.Id == sheetId).ToList();
            if (sheets.Count == 0)
            {
                if (asked != null) refused.Add(asked.Id);
                notDone.Add("'" + oldView.Name + "' is not on sheet "
                          + (asked == null ? "(not a sheet)" : asked.SheetNumber)
                          + ", so there is nothing there to replace. Nothing was changed.");
            }
        }
        else if (sheets.Count == 0)
        {
            notDone.Add("'" + oldView.Name + "' is not on any sheet, so there is nowhere to put '"
                      + newView.Name + "'. Nothing was changed.");
        }

        var offset = at == null ? XYZ.Zero : new XYZ(at.X, at.Y, 0.0);

        foreach (var sheet in sheets)
        {
            string why = null;
            var lines = new List<string>();
            var made = new List<ElementId>();
            var madeAt = new List<XYZ>();

            if (newWasOn.Contains(sheet.Id))
                why = "'" + newView.Name + "' is already on this sheet, and putting it here again "
                    + "would show it twice. '" + oldView.Name + "' was left where it is - take one "
                    + "off by hand if the old one is no longer wanted";

            if (why == null)
            {
                using (var attempt = new SubTransaction(doc))
                {
                    attempt.Start();
                    try
                    {
                        foreach (var old in oldOn[sheet.Id])
                        {
                            var oldId = old.Id;
                            var typeId = old.GetTypeId();
                            bool wasPinned = old.Pinned;
                            Element fresh;
                            XYZ target;
                            string title = "";

                            // THE DETAIL NUMBER, when the old viewport has one. A
                            // callout or section head elsewhere points at "3 on
                            // A101", so a plan swapped in keeps the old number. It
                            // is unique on the sheet, so it is set once the old
                            // viewport is gone.
                            string detail = null;
                            if (!isSchedule)
                            {
                                var oldDetail = old.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER);
                                if (oldDetail != null && oldDetail.HasValue) detail = oldDetail.AsString();
                            }

                            if (isSchedule)
                            {
                                var oldInstance = (ScheduleSheetInstance)old;
                                target = oldInstance.Point + offset;
                                var rotation = oldInstance.Rotation;

                                var instance = ScheduleSheetInstance.Create(doc, sheet.Id,
                                                                            newView.Id, target);
                                if (instance == null)
                                {
                                    why = "Revit reported no error and placed nothing";
                                    break;
                                }
                                if (instance.Rotation != rotation) instance.Rotation = rotation;
                                fresh = instance;
                            }
                            else
                            {
                                // ASKED PER PLACEMENT, NOT ONCE PER SHEET. An old legend
                                // can sit on one sheet twice, and the second place is
                                // where a view that may go on a sheet once is refused.
                                if (!Viewport.CanAddViewToSheet(doc, sheet.Id, newView.Id))
                                {
                                    var elsewhere = newFirstSheet != null && newFirstSheet.Id != sheet.Id
                                        ? " - it is on sheet " + newFirstSheet.SheetNumber
                                        : "";
                                    why = "Revit will not put '" + newView.Name + "' here" + elsewhere
                                        + ". Only a legend or a schedule can be on more than one "
                                        + "sheet; duplicate the view and name the copy";
                                    break;
                                }

                                var oldPort = (Viewport)old;
                                target = oldPort.GetBoxCenter() + offset;
                                var rotation = oldPort.Rotation;
#if REVIT2020 || REVIT2021
#else
                                var labelOffset = oldPort.LabelOffset;
#endif
                                var port = Viewport.Create(doc, sheet.Id, newView.Id, target);
                                if (port == null)
                                {
                                    why = "Revit reported no error and placed nothing";
                                    break;
                                }

                                // THE OLD VIEWPORT'S TYPE - title shown or not, its line.
                                if (typeId != null && typeId != ElementId.InvalidElementId
                                    && port.GetTypeId() != typeId)
                                    port.ChangeTypeId(typeId);
                                if (port.Rotation != rotation) port.Rotation = rotation;
#if REVIT2020 || REVIT2021
                                // The title's position cannot be reached before 2022:
                                // Revit draws it where it draws a new viewport's.
                                title = ", title where Revit draws a new one - 2020 and 2021 "
                                      + "cannot move it";
#else
                                // THE TITLE'S POSITION, where 2022 and later reach it.
                                // Its line LENGTH is left as Revit draws it for the new
                                // view, because the old one's was sized to the old name.
                                try
                                {
                                    port.LabelOffset = labelOffset;
                                    title = ", title position copied";
                                }
                                catch (Exception)
                                {
                                    title = ", title position NOT copied - Revit refused it";
                                }
#endif
                                // A type or a rotation can move the box. Put it back.
                                port.SetBoxCenter(target);
                                fresh = port;
                            }

                            doc.Delete(oldId);
                            doc.Regenerate();

                            // READ BACK. What the model holds now, not what was asked.
                            if (doc.GetElement(oldId) != null)
                            {
                                why = "the old one is still on the sheet after it was deleted";
                                break;
                            }

                            XYZ landed;
                            string typeWord;
                            if (isSchedule)
                            {
                                var instance = (ScheduleSheetInstance)fresh;
                                if (instance.ScheduleId != newView.Id || instance.OwnerViewId != sheet.Id)
                                {
                                    why = "the new schedule did not read back on this sheet";
                                    break;
                                }
                                landed = instance.Point;
                            }
                            else
                            {
                                var port = (Viewport)fresh;
                                if (port.ViewId != newView.Id || port.SheetId != sheet.Id)
                                {
                                    why = "the new viewport did not read back on this sheet";
                                    break;
                                }
                                landed = port.GetBoxCenter();
                            }

                            var nowType = fresh.GetTypeId();
                            var typeElement = nowType == null || nowType == ElementId.InvalidElementId
                                ? null : doc.GetElement(nowType);
                            if (!isSchedule && typeId != null && typeId != ElementId.InvalidElementId
                                && nowType != typeId)
                            {
                                why = "the new one did not keep the old viewport type";
                                break;
                            }
                            typeWord = isSchedule
                                ? "a schedule has no viewport type - its look is its own"
                                : typeElement == null ? "no viewport type"
                                : "viewport type '" + typeElement.Name + "' kept";

                            if (!string.IsNullOrEmpty(detail))
                            {
                                var newDetail = fresh.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER);
                                if (newDetail != null && !newDetail.IsReadOnly && newDetail.AsString() != detail)
                                {
                                    try { newDetail.Set(detail); }
                                    catch (Exception) { }
                                }
                                var nowDetail = newDetail == null ? null : newDetail.AsString();
                                typeWord += nowDetail == detail
                                    ? ", detail number " + detail + " kept"
                                    : ", detail number " + (nowDetail ?? "(none)") + " - the old "
                                      + detail + " could not be kept";
                            }

                            // PINNED AS THE OLD ONE WAS, last, once it sits in place.
                            if (wasPinned && !fresh.Pinned)
                            {
                                try { fresh.Pinned = true; }
                                catch (Exception) { }
                            }
                            if (wasPinned)
                                typeWord += fresh.Pinned ? ", pinned as before" : ", NOT pinned - the old one was";

                            // In millimetres, on the sheet's plane. 0.1 mm is the
                            // tolerance: closer than that is the same place on paper.
                            double dx = (landed.X - target.X) * 304.8;
                            double dy = (landed.Y - target.Y) * 304.8;
                            double off = Math.Sqrt(dx * dx + dy * dy);
                            if (off > 0.1)
                            {
                                why = string.Format("the new one landed {0:0.0} mm from where the old "
                                    + "one was, and 0.1 mm is the most allowed", off);
                                break;
                            }

                            double movedX = (landed.X - (target.X - offset.X)) * 304.8;
                            double movedY = (landed.Y - (target.Y - offset.Y)) * 304.8;
                            double moved = Math.Sqrt(movedX * movedX + movedY * movedY);

                            made.Add(fresh.Id);
                            madeAt.Add(landed);
                            lines.Add(string.Format("Sheet {0}: '{1}' gone, '{2}' in its place - "
                                + "{3} {4:0.0} mm from the old one, {5}{6}",
                                sheet.SheetNumber, oldView.Name, newView.Name,
                                isSchedule ? "corner" : "centre", moved, typeWord, title));
                        }

                        // The old view must be off this sheet entirely now.
                        if (why == null)
                        {
                            int left = isSchedule
                                ? new FilteredElementCollector(doc).OfClass(typeof(ScheduleSheetInstance))
                                      .Cast<ScheduleSheetInstance>()
                                      .Count(i => i.ScheduleId == oldView.Id && i.OwnerViewId == sheet.Id
                                                  && !i.IsTitleblockRevisionSchedule)
                                : new FilteredElementCollector(doc).OfClass(typeof(Viewport))
                                      .Cast<Viewport>()
                                      .Count(p => p.ViewId == oldView.Id && p.SheetId == sheet.Id);
                            if (left > 0)
                                why = "'" + oldView.Name + "' is still on the sheet " + left
                                    + " time(s) after the swap";
                        }
                    }
                    catch (Exception failure)
                    {
                        // REVIT'S OWN WORDS. The sheet is rolled back below.
                        why = "Revit refused: " + failure.Message;
                    }

                    if (why == null) attempt.Commit();
                    else attempt.RollBack();
                }
            }

            if (why != null)
            {
                refused.Add(sheet.Id);
                notDone.Add("Sheet " + sheet.SheetNumber + ": " + why
                          + ". Nothing on this sheet was changed.");
            }
            else
            {
                placed.AddRange(made);
                swapped.AddRange(lines);
                foreach (var point in madeAt) noteLanding(sheet.Id, point);
                // A plan, section or drafting view is on a sheet now and can go
                // on no other - the next sheet's refusal names this one.
                if (newFirstSheet == null) newFirstSheet = sheet;
            }
        }
    }
}

// VERSION 3. The counts, from what the model holds - never from what was asked.
placedCount = placed.Count;
distinctPositions = landedAt.Count;
