// NOT STANDALONE. Assumes `doc`, `sheetNumber` and `viewName` are in scope,
// and leaves `viewsOnSheet`, `viewCount`, `schedulesOnSheet`, `scheduleCount`,
// `offTitleBlock`, `offTitleBlockCount`, `overlaps`, `overlapCount`,
// `sheetRead`, `sheetsRead`, `revisionSchedules`, `notChecked`,
// `sheetProblem`, `viewProblem` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHY THIS EXISTS (FRAGMENT-ISSUES row 146, 2026-10-09). "What views are on
// this sheet" reached PLACE_VIEWS_ON_SHEET - a write - because no read named a
// sheet's views. LIST_SHEETS counts them and stops there.
//
// WHY VERSION 2 (FRAGMENT-ISSUES row 5b-560, 2026-10-17). Asked to put a
// second note beside a legend, a chat had to write its own probe to learn that
// the legend was on four sheets, where its box sat on each, and that a wider
// legend would run off all four. Nothing said which sheets a view is on, where
// its box sits on the paper, or that a box runs past the title block or over
// another. So each box is given corner to corner, and the two problems are
// measured and said with how far, in millimetres.
//
// THE SHEET IS FOUND THE WAY PLACE_VIEWS_ON_SHEET FINDS IT: the whole sheet
// number, ignoring case. Never a part of a number, and never the sheet in
// front when the number is blank - a read that falls back to something it was
// not given answers a question nobody asked, and looks right doing it. "all"
// is every sheet, said back as every sheet; it is also what the card reads a
// number LEFT OUT as, so "does any view run off the sheet" needs no number. A
// sheet view template comes back from the same collector carrying a number and
// is not a sheet (LIST_SHEETS drops it for the same reason), so it is skipped.
//
// ONE VIEW BY NAME, ON EVERY SHEET IT IS ON. A legend or a schedule can sit on
// many sheets; a plan on one. viewName, matched whole and ignoring case, keeps
// only that view's rows - and the checks still measure it against EVERYTHING
// on each sheet, so an overlap with a neighbour is found, but only a problem
// that involves the view asked about is said. Left out, or "all", is every view.
//
// VIEWS AND SCHEDULES ARRIVE BY DIFFERENT ROUTES, AND BOTH ARE READ.
// GetAllViewports() returns every viewport - plans, sections, 3D views,
// drafting views, legends. RevitAPI.xml says on every release 2020-2027 that
// schedules are NOT among them, and that ScheduleSheetInstance.OwnerViewId is
// how to find the sheet a schedule sits on. So the schedules are collected
// from the whole model, once, and kept on a sheet when their owner view is it.
//
// A SCHEDULE SPLIT INTO PIECES IS ONE SCHEDULE. From 2022 a schedule can be
// split across a sheet, and each piece is its own ScheduleSheetInstance with
// the same ScheduleId. Grouping by that id counts it once and says how many
// pieces - with no 2022-only call, so 2020 and 2021 read the same way. Each
// piece keeps its own box, because each piece is its own rectangle on paper.
//
// A REVISION SCHEDULE INSIDE THE TITLE BLOCK IS NOT A SCHEDULE ANYBODY PLACED.
// It belongs to the title block family and is on every sheet that title block
// is on. Counted apart, never listed, and never checked as a box of its own.
//
// THE BOXES. A viewport's is GetBoxOutline() - RevitAPI.xml: the outline on
// the sheet EXCLUDING the viewport label - so it is the view and its
// annotation, never the title under it. A schedule's is its bounding box in
// the sheet. The paper is the bounding box of the title block on the sheet,
// as CENTER_VIEWPORTS_ON_SHEETS takes it; two title blocks on one sheet are
// taken as the one box around both. A sheet with no title block cannot be
// checked against one, and is named in `notChecked` rather than passed.
//
// HALF A MILLIMETRE IS NOT A PROBLEM. A box edge within 0.5 mm of the title
// block's, or two boxes sharing an edge, is a layout snapped into place - not
// a view off the sheet, and not two drawings on top of each other.
//
// LENGTHS ON THE SHEET ARE MILLIMETRES. Revit hands back sheet positions in
// its internal feet; * 304.8, rounded to the millimetre (D-20).
//
// THE ANSWER IS CUT AT 60000 CHARACTERS A LIST, AND SAYS SO. "all" on a
// project of six hundred sheets is thousands of rows; a reply that size buries
// the chat it lands in. The counts are never cut, and the last row says how
// many were not written and how to ask for them.

var findings = new List<string>();
var viewsOnSheet = "";
var viewCount = 0;
var schedulesOnSheet = "";
var scheduleCount = 0;
var offTitleBlock = "";
var offTitleBlockCount = 0;
var overlaps = "";
var overlapCount = 0;
var sheetRead = "";
var sheetsRead = 0;
var revisionSchedules = 0;
var notChecked = "";
var sheetProblem = "";
var viewProblem = "";

var wantedNumber = (sheetNumber ?? "").Trim();
var wantedView = (viewName ?? "").Trim();
var everySheet = string.Equals(wantedNumber, "all", StringComparison.OrdinalIgnoreCase);
var everyView = wantedView.Length == 0
    || string.Equals(wantedView, "all", StringComparison.OrdinalIgnoreCase);

if (wantedNumber.Length == 0)
{
    sheetProblem = "no sheet number was given";
    findings.Add("No sheet number was given, so no sheet was read. Name the sheet by its number "
        + "- for example M-101 - or say all for every sheet. LIST_SHEETS lists every sheet's number.");
}
else
{
    var targets = new List<ViewSheet>();
    foreach (var candidate in new FilteredElementCollector(doc)
                                  .OfClass(typeof(ViewSheet))
                                  .Cast<ViewSheet>())
    {
        if (candidate == null || candidate.IsTemplate) continue;
        if (everySheet
            || string.Equals(candidate.SheetNumber, wantedNumber, StringComparison.OrdinalIgnoreCase))
            targets.Add(candidate);
    }
    targets.Sort((a, b) => string.Compare(a.SheetNumber, b.SheetNumber,
                                          StringComparison.OrdinalIgnoreCase));

    // A PLACEHOLDER is a number and a name with no title block. It can hold no
    // view, so it is never read: asked for by its number it is refused with
    // that reason, and among every sheet it is counted and left out.
    var readable = new List<ViewSheet>();
    var placeholders = new List<string>();
    foreach (var sheet in targets)
    {
        if (sheet.IsPlaceholder) placeholders.Add(sheet.SheetNumber + " - " + sheet.Name);
        else readable.Add(sheet);
    }

    if (targets.Count == 0)
    {
        sheetProblem = everySheet
            ? "the model has no sheets"
            : "no sheet numbered '" + wantedNumber + "'";
        findings.Add(everySheet
            ? doc.Title + " has no sheets, so nothing was read."
            : "There is no sheet numbered '" + wantedNumber + "' in " + doc.Title
              + ", so nothing was read. LIST_SHEETS lists every sheet's number; FIND_SHEETS "
              + "finds one by part of its number or its name.");
    }
    else if (readable.Count == 0)
    {
        sheetRead = string.Join(", ", placeholders.ToArray());
        sheetProblem = everySheet
            ? "every sheet is a placeholder and cannot hold a view"
            : "sheet '" + targets[0].SheetNumber + "' is a placeholder and cannot hold a view";
        findings.Add((everySheet ? "Every sheet in " + doc.Title + " is" : "Sheet " + sheetRead + " is")
            + " a PLACEHOLDER - a row in the drawing list with no title block. Revit puts no "
            + "view or schedule on one, so nothing is on it.");
    }
    else
    {
        // One sheet asked for by its number answers without repeating it on
        // every row - sheetRead names it. Several sheets name theirs per row.
        var several = everySheet || readable.Count > 1;
        var slack = 0.5;
        var mostCharacters = 60000;

        // The view type in the words the Project Browser uses. Anything not
        // named here is said as Revit names it, never dropped.
        Func<View, string> typeWords = view =>
        {
            switch (view.ViewType)
            {
                case ViewType.FloorPlan: return "Floor Plan";
                case ViewType.CeilingPlan: return "Reflected Ceiling Plan";
                case ViewType.EngineeringPlan: return "Structural Plan";
                case ViewType.AreaPlan: return "Area Plan";
                case ViewType.Elevation: return "Elevation";
                case ViewType.Section: return "Section";
                case ViewType.Detail: return "Detail View";
                case ViewType.ThreeD: return "3D View";
                case ViewType.DraftingView: return "Drafting View";
                case ViewType.Legend: return "Legend";
                case ViewType.Rendering: return "Rendering";
                default: return view.ViewType.ToString();
            }
        };

        // The scale only where the view has one. RevitAPI.xml calls View.Scale
        // meaningless for a perspective view, so a perspective says so rather
        // than reporting a number. Otherwise the View Scale parameter decides:
        // a view Revit shows no scale for has none here either.
        Func<View, string> scaleOf = view =>
        {
            var view3d = view as View3D;
            if (view3d != null && view3d.IsPerspective) return "perspective, no scale";
            try
            {
                var scale = view.get_Parameter(BuiltInParameter.VIEW_SCALE);
                if (scale != null && scale.HasValue && scale.AsInteger() > 0)
                    return "1 : " + scale.AsInteger();
            }
            catch (Exception) { }
            return "no scale";
        };

        Func<XYZ, string> millimetres = point =>
            Math.Round(point.X * 304.8).ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ", "
            + Math.Round(point.Y * 304.8).ToString(System.Globalization.CultureInfo.InvariantCulture);

        // A box corner to corner - lower left, then upper right.
        Func<XYZ, XYZ, string> corners = (low, high) =>
            "(" + millimetres(low) + ") to (" + millimetres(high) + ")";

        // A distance already in millimetres, as a whole number.
        Func<double, string> wholeMm = amount =>
            Math.Round(amount).ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Detail numbers are text ("1", "2", "A3"). Numbers come first, in
        // number order so 10 comes after 9; text comes after every number, in
        // text order. ONE ORDER FOR A MIXED SHEET: comparing two numbers as
        // numbers and a number with text as text was not an order at all -
        // 2 < 10 as numbers, 10 < 1a and 1a < 2 as text - and a sort handed
        // that leaves the rows in no defined order (row 146 review, 2026-10-09).
        Comparison<KeyValuePair<string, string>> byDetail = (a, b) =>
        {
            int left, right;
            var leftIsNumber = int.TryParse(a.Key, out left);
            var rightIsNumber = int.TryParse(b.Key, out right);
            if (leftIsNumber && rightIsNumber) return left.CompareTo(right);
            if (leftIsNumber != rightIsNumber) return leftIsNumber ? -1 : 1;
            return string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase);
        };

        // Rows joined with " || " until the next would pass the limit; then
        // one last row naming how many were left out. The count is never cut.
        Func<List<string>, string> joined = list =>
        {
            var kept = new List<string>();
            var length = 0;
            foreach (var one in list)
            {
                if (length + one.Length + 4 > mostCharacters) break;
                kept.Add(one);
                length += one.Length + 4;
            }
            if (kept.Count < list.Count)
                kept.Add("... and " + (list.Count - kept.Count) + " more not written here - "
                    + "ask by one sheet number or one view name for the rest");
            return string.Join(" || ", kept.ToArray());
        };

        var schedulesInModel = new List<ScheduleSheetInstance>();
        foreach (var instance in new FilteredElementCollector(doc)
                                     .OfClass(typeof(ScheduleSheetInstance))
                                     .Cast<ScheduleSheetInstance>())
            if (instance != null) schedulesInModel.Add(instance);

        var viewRows = new List<string>();
        var scheduleRows = new List<string>();
        var offRows = new List<string>();
        var overlapRows = new List<string>();
        var withoutPaper = new List<string>();
        var sheetsWithIt = new List<string>();
        var nearNames = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var target in readable)
        {
            var lead = several ? target.SheetNumber + ": " : "";

            // Every box on this sheet: what it is, lower left, upper right, and
            // whether it is a view this answer was asked about.
            var boxes = new List<Tuple<string, XYZ, XYZ, bool>>();

            // ------------------------------------------------------------------
            // THE VIEWPORTS - views and legends
            // ------------------------------------------------------------------
            var rows = new List<KeyValuePair<string, string>>();
            foreach (var portId in target.GetAllViewports())
            {
                var port = doc.GetElement(portId) as Viewport;
                if (port == null) continue;

                var view = doc.GetElement(port.ViewId) as View;
                var shown = view == null ? "" : view.Name;
                var asked = everyView
                    || string.Equals(shown, wantedView, StringComparison.OrdinalIgnoreCase);
                if (!asked && shown.IndexOf(wantedView, StringComparison.OrdinalIgnoreCase) >= 0)
                    nearNames.Add(shown);

                Outline box = null;
                try { box = port.GetBoxOutline(); } catch (Exception) { }
                var what = view == null
                    ? "a viewport whose view Revit did not hand back"
                    : "'" + view.Name + "' (" + typeWords(view) + ")";
                if (box != null)
                    boxes.Add(Tuple.Create(what, box.MinimumPoint, box.MaximumPoint, asked));
                if (!asked) continue;

                var detail = "";
                try
                {
                    var number = port.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER);
                    if (number != null && number.HasValue) detail = number.AsString() ?? "";
                }
                catch (Exception) { }

                var portType = "";
                try
                {
                    var type = doc.GetElement(port.GetTypeId());
                    if (type != null) portType = type.Name;
                }
                catch (Exception) { }

                var where = box != null
                    ? "  box " + corners(box.MinimumPoint, box.MaximumPoint) + " mm"
                    : "  box not read";
                try { where += "  centre (" + millimetres(port.GetBoxCenter()) + ") mm"; }
                catch (Exception) { where += "  centre not read"; }

                var row = view == null
                    ? "a viewport whose view Revit did not hand back"
                    : view.Name + "  (" + typeWords(view) + ", " + scaleOf(view) + ")";

                rows.Add(new KeyValuePair<string, string>(detail,
                    lead + (detail.Length > 0 ? detail : "-") + "  " + row
                    + "  viewport type " + (portType.Length > 0 ? "'" + portType + "'" : "not read")
                    + where));
            }
            rows.Sort(byDetail);
            viewRows.AddRange(rows.Select(r => r.Value));

            // ------------------------------------------------------------------
            // THE SCHEDULES - by their owner view, grouped by schedule
            // ------------------------------------------------------------------
            var pieces = new Dictionary<ElementId, int>();
            var placedAt = new Dictionary<ElementId, XYZ>();
            var nameOf = new Dictionary<ElementId, string>();
            var boxesOf = new Dictionary<ElementId, List<string>>();
            foreach (var instance in schedulesInModel)
            {
                if (instance == null || instance.OwnerViewId != target.Id) continue;
                if (instance.IsTitleblockRevisionSchedule)
                {
                    revisionSchedules++;
                    continue;
                }

                var scheduleId = instance.ScheduleId;
                if (!pieces.ContainsKey(scheduleId))
                {
                    pieces[scheduleId] = 0;
                    XYZ point = null;
                    try { point = instance.Point; } catch (Exception) { }
                    placedAt[scheduleId] = point;
                    var schedule = doc.GetElement(scheduleId) as View;
                    nameOf[scheduleId] = schedule != null ? schedule.Name : instance.Name;
                    boxesOf[scheduleId] = new List<string>();
                }
                pieces[scheduleId]++;

                var asked = everyView
                    || string.Equals(nameOf[scheduleId], wantedView, StringComparison.OrdinalIgnoreCase);
                BoundingBoxXYZ extent = null;
                try { extent = instance.get_BoundingBox(target); } catch (Exception) { }
                if (extent == null) continue;
                boxes.Add(Tuple.Create("schedule '" + nameOf[scheduleId] + "'", extent.Min, extent.Max, asked));
                boxesOf[scheduleId].Add(corners(extent.Min, extent.Max));
            }

            var sheetSchedules = new List<string>();
            foreach (var scheduleId in pieces.Keys)
            {
                if (!everyView
                    && !string.Equals(nameOf[scheduleId], wantedView, StringComparison.OrdinalIgnoreCase))
                {
                    if (nameOf[scheduleId].IndexOf(wantedView, StringComparison.OrdinalIgnoreCase) >= 0)
                        nearNames.Add(nameOf[scheduleId]);
                    continue;
                }
                var point = placedAt[scheduleId];
                sheetSchedules.Add(lead + nameOf[scheduleId]
                    + (boxesOf[scheduleId].Count > 0
                        ? "  box " + string.Join("; ", boxesOf[scheduleId].ToArray()) + " mm"
                        : "  box not read")
                    + (point != null ? "  placed at (" + millimetres(point) + ") mm" : "  position not read")
                    + (pieces[scheduleId] > 1 ? "  - split into " + pieces[scheduleId] + " pieces" : ""));
            }
            sheetSchedules.Sort(StringComparer.OrdinalIgnoreCase);
            scheduleRows.AddRange(sheetSchedules);

            if (rows.Count > 0 || sheetSchedules.Count > 0)
                sheetsWithIt.Add(target.SheetNumber + " - " + target.Name);

            // ------------------------------------------------------------------
            // THE PAPER - every title block on the sheet, as one box
            // ------------------------------------------------------------------
            XYZ paperLow = null;
            XYZ paperHigh = null;
            try
            {
                foreach (var block in new FilteredElementCollector(doc, target.Id)
                                          .OfCategory(BuiltInCategory.OST_TitleBlocks)
                                          .WhereElementIsNotElementType())
                {
                    BoundingBoxXYZ extent = null;
                    try { extent = block.get_BoundingBox(target); } catch (Exception) { }
                    if (extent == null) continue;
                    paperLow = paperLow == null ? extent.Min
                        : new XYZ(Math.Min(paperLow.X, extent.Min.X), Math.Min(paperLow.Y, extent.Min.Y), 0);
                    paperHigh = paperHigh == null ? extent.Max
                        : new XYZ(Math.Max(paperHigh.X, extent.Max.X), Math.Max(paperHigh.Y, extent.Max.Y), 0);
                }
            }
            catch (Exception) { }

            // ------------------------------------------------------------------
            // A BOX PAST THE TITLE BLOCK - each side it runs past, and how far
            // ------------------------------------------------------------------
            if (paperLow == null || paperHigh == null)
            {
                if (boxes.Any(b => b.Item4)) withoutPaper.Add(target.SheetNumber + " - " + target.Name);
            }
            else
            {
                foreach (var b in boxes)
                {
                    if (!b.Item4) continue;
                    var past = new List<string>();
                    var leftBy = (paperLow.X - b.Item2.X) * 304.8;
                    var rightBy = (b.Item3.X - paperHigh.X) * 304.8;
                    var belowBy = (paperLow.Y - b.Item2.Y) * 304.8;
                    var aboveBy = (b.Item3.Y - paperHigh.Y) * 304.8;
                    if (leftBy > slack) past.Add(wholeMm(leftBy) + " mm on the left");
                    if (rightBy > slack) past.Add(wholeMm(rightBy) + " mm on the right");
                    if (belowBy > slack) past.Add(wholeMm(belowBy) + " mm at the bottom");
                    if (aboveBy > slack) past.Add(wholeMm(aboveBy) + " mm at the top");
                    if (past.Count == 0) continue;
                    offRows.Add(lead + b.Item1 + " runs past the title block by "
                        + string.Join(", ", past.ToArray()) + " - its box " + corners(b.Item2, b.Item3)
                        + " mm, the title block " + corners(paperLow, paperHigh) + " mm");
                }
            }

            // ------------------------------------------------------------------
            // TWO BOXES OVER EACH OTHER - how wide and how high the overlap is
            // ------------------------------------------------------------------
            for (var i = 0; i < boxes.Count; i++)
            {
                for (var j = i + 1; j < boxes.Count; j++)
                {
                    var one = boxes[i];
                    var other = boxes[j];
                    if (!one.Item4 && !other.Item4) continue;
                    var wide = (Math.Min(one.Item3.X, other.Item3.X) - Math.Max(one.Item2.X, other.Item2.X)) * 304.8;
                    var high = (Math.Min(one.Item3.Y, other.Item3.Y) - Math.Max(one.Item2.Y, other.Item2.Y)) * 304.8;
                    if (wide <= slack || high <= slack) continue;
                    overlapRows.Add(lead + one.Item1 + " and " + other.Item1 + " overlap by "
                        + wholeMm(wide) + " x " + wholeMm(high) + " mm");
                }
            }
        }

        sheetsRead = readable.Count;
        viewCount = viewRows.Count;
        scheduleCount = scheduleRows.Count;
        offTitleBlockCount = offRows.Count;
        overlapCount = overlapRows.Count;
        viewsOnSheet = joined(viewRows);
        schedulesOnSheet = joined(scheduleRows);
        offTitleBlock = joined(offRows);
        overlaps = joined(overlapRows);
        notChecked = string.Join(" || ", withoutPaper.ToArray());

        // Two sheets answer one number only if Revit let two numbers differ by
        // case alone; both are read, and said as that, never as every sheet.
        var scope = everySheet
            ? "every sheet - " + readable.Count + " read"
            : readable.Count + " sheets numbered '" + wantedNumber + "'";
        if (!several)
            sheetRead = readable[0].SheetNumber + " - " + readable[0].Name;
        else if (everyView)
            sheetRead = scope;
        else
            sheetRead = scope + ", '" + wantedView + "' on " + sheetsWithIt.Count + ": "
                + string.Join(", ", sheetsWithIt.ToArray());

        if (!everyView && viewCount == 0 && scheduleCount == 0)
        {
            var inModel = new FilteredElementCollector(doc)
                              .OfClass(typeof(View))
                              .Cast<View>()
                              .Any(v => v != null && !v.IsTemplate
                                        && string.Equals(v.Name, wantedView, StringComparison.OrdinalIgnoreCase));
            viewProblem = (inModel
                    ? "'" + wantedView + "' is in the model but on none of the sheets read"
                    : "no view or schedule named '" + wantedView + "'")
                + (nearNames.Count > 0
                    ? " - names on those sheets that hold it: " + string.Join(", ", nearNames.Take(10).ToArray())
                    : "");
            findings.Add("Nothing named '" + wantedView + "' is on "
                + (several ? "any sheet read" : "sheet " + sheetRead) + ": " + viewProblem + ".");
        }
        else if (!everyView)
        {
            findings.Add("'" + wantedView + "' is on " + sheetsWithIt.Count + " sheet"
                + (sheetsWithIt.Count == 1 ? "" : "s") + ": "
                + string.Join(", ", sheetsWithIt.ToArray()) + ".");
        }
        else
        {
            findings.Add((several ? readable.Count + " sheets" : "Sheet " + sheetRead) + ": "
                + viewCount + " view" + (viewCount == 1 ? "" : "s")
                + " and " + scheduleCount + " schedule" + (scheduleCount == 1 ? "" : "s") + ".");
            if (viewCount == 0 && scheduleCount == 0)
                findings.Add("Nothing is placed on " + (several ? "any of them" : "it")
                    + " - no view, no legend and no schedule.");
        }

        findings.Add(offTitleBlockCount == 0 && overlapCount == 0
            ? "No box runs past its title block and no two boxes overlap."
            : offTitleBlockCount + " box" + (offTitleBlockCount == 1 ? " runs" : "es run")
              + " past the title block, and " + overlapCount + " pair"
              + (overlapCount == 1 ? "" : "s") + " of boxes overlap - offTitleBlock and overlaps say "
              + "which, and by how many millimetres.");
        if (withoutPaper.Count > 0)
            findings.Add(withoutPaper.Count + " sheet" + (withoutPaper.Count == 1 ? " has" : "s have")
                + " no title block to measure against, so nothing on "
                + (withoutPaper.Count == 1 ? "it" : "them") + " was checked for running off: notChecked names "
                + (withoutPaper.Count == 1 ? "it" : "them") + ".");
        if (placeholders.Count > 0)
            findings.Add(placeholders.Count + " placeholder sheet" + (placeholders.Count == 1 ? "" : "s")
                + " hold nothing and " + (placeholders.Count == 1 ? "was" : "were") + " not read.");
        if (revisionSchedules > 0)
            findings.Add("The title blocks carry " + revisionSchedules + " revision schedule"
                + (revisionSchedules == 1 ? "" : "s") + ", which belong to the title block family "
                + "and are not counted.");
    }
}
