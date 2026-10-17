// NOT STANDALONE. Assumes `doc`, `sheetId`, `views`, `at` and `oldViewId` are
// in scope; leaves `placed`, `refused`, `swapped` and `notDone` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// TWO JOBS, ONE QUESTION - WHERE DOES THE VIEW GO. With no `oldViewId` this
// is version 1, unchanged: each view at the point `at`, on the sheet
// `sheetId`. With an `oldViewId` it is version 2: the one view in `views` goes
// where the old view sits, on every sheet the old view is on - or only on
// `sheetId` when one is named - and the old view comes off those sheets. The
// old view stays in the project; only its place on the sheet is taken.
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
// EVERY VIEWPORT AT THE SAME POINT, deliberately, when placing. Laying out a
// sheet - what goes where, at what scale, aligned with what - is drafting
// judgement and its own job. Stacking them is honest and predictable: a
// viewport that exists can be dragged, and one that was never placed cannot.

var placed = new List<ElementId>();
var refused = new List<ElementId>();
var swapped = new List<string>();
var notDone = new List<string>();

bool replacing = oldViewId != null && oldViewId != ElementId.InvalidElementId;

if (!replacing)
{
    // VERSION 1. `sheetId` may be left out since version 2, because "every
    // sheet the old view is on" needs no sheet named - but placing still does.
    if (sheetId == null || sheetId == ElementId.InvalidElementId)
    {
        foreach (var view in views)
            if (view != null) refused.Add(view.Id);
        notDone.Add("No sheet was named, so nothing was placed. Name the sheet by its "
                  + "number - A101.");
    }
    else
    {
        foreach (var view in views)
        {
            if (view == null) continue;

            if (!Viewport.CanAddViewToSheet(doc, sheetId, view.Id))
            {
                refused.Add(view.Id);
                continue;
            }

            try
            {
                var viewport = Viewport.Create(doc, sheetId, view.Id, at);

                // A null back without an exception is the silent no-op shape this
                // repository has met on the move and parameter paths. Never counted.
                if (viewport == null) refused.Add(view.Id);
                else placed.Add(viewport.Id);
            }
            catch (Exception)
            {
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
    var named = new List<View>();
    foreach (var view in views)
        if (view != null) named.Add(view);
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
                // A plan, section or drafting view is on a sheet now and can go
                // on no other - the next sheet's refusal names this one.
                if (newFirstSheet == null) newFirstSheet = sheet;
            }
        }
    }
}
