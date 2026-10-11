// NOT STANDALONE. Assumes `doc` and `sheetNumber` are in scope, and leaves
// `viewsOnSheet`, `viewCount`, `schedulesOnSheet`, `scheduleCount`,
// `sheetRead`, `revisionSchedules`, `sheetProblem` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHY THIS EXISTS (FRAGMENT-ISSUES row 146, 2026-10-09). "What views are on
// this sheet" reached PLACE_VIEWS_ON_SHEET - a write - because no read named a
// sheet's views. LIST_SHEETS counts them and stops there.
//
// THE SHEET IS FOUND THE WAY PLACE_VIEWS_ON_SHEET FINDS IT: the whole sheet
// number, ignoring case. Never a part of a number, and never the sheet in
// front when the number is blank - a read that falls back to something it was
// not given answers a question nobody asked, and looks right doing it. A sheet
// view template comes back from the same collector carrying a number and is
// not a sheet (LIST_SHEETS drops it for the same reason), so it is skipped.
//
// VIEWS AND SCHEDULES ARRIVE BY DIFFERENT ROUTES, AND BOTH ARE READ.
// GetAllViewports() returns every viewport - plans, sections, 3D views,
// drafting views, legends. RevitAPI.xml says on every release 2020-2027 that
// schedules are NOT among them, and that ScheduleSheetInstance.OwnerViewId is
// how to find the sheet a schedule sits on. So the schedules are collected
// from the whole model and kept when their owner view is this sheet.
//
// A SCHEDULE SPLIT INTO PIECES IS ONE SCHEDULE. From 2022 a schedule can be
// split across a sheet, and each piece is its own ScheduleSheetInstance with
// the same ScheduleId. Grouping by that id counts it once and says how many
// pieces - with no 2022-only call, so 2020 and 2021 read the same way.
//
// A REVISION SCHEDULE INSIDE THE TITLE BLOCK IS NOT A SCHEDULE ANYBODY PLACED.
// It belongs to the title block family and is on every sheet that title block
// is on. Counted apart, never listed.
//
// LENGTHS ON THE SHEET ARE MILLIMETRES. Revit hands back sheet positions in
// its internal feet; * 304.8, rounded to the millimetre (D-20).

var findings = new List<string>();
var viewsOnSheet = "";
var viewCount = 0;
var schedulesOnSheet = "";
var scheduleCount = 0;
var sheetRead = "";
var revisionSchedules = 0;
var sheetProblem = "";

var wantedNumber = (sheetNumber ?? "").Trim();

if (wantedNumber.Length == 0)
{
    sheetProblem = "no sheet number was given";
    findings.Add("No sheet number was given, so no sheet was read. Name the sheet by its number "
        + "- for example M-101. LIST_SHEETS lists every sheet's number.");
}
else
{
    ViewSheet target = null;
    foreach (var candidate in new FilteredElementCollector(doc)
                                  .OfClass(typeof(ViewSheet))
                                  .Cast<ViewSheet>())
    {
        if (candidate == null || candidate.IsTemplate) continue;
        if (string.Equals(candidate.SheetNumber, wantedNumber, StringComparison.OrdinalIgnoreCase))
        {
            target = candidate;
            break;
        }
    }

    if (target == null)
    {
        sheetProblem = "no sheet numbered '" + wantedNumber + "'";
        findings.Add("There is no sheet numbered '" + wantedNumber + "' in " + doc.Title
            + ", so nothing was read. LIST_SHEETS lists every sheet's number; FIND_SHEETS "
            + "finds one by part of its number or its name.");
    }
    else if (target.IsPlaceholder)
    {
        sheetRead = target.SheetNumber + " - " + target.Name;
        sheetProblem = "sheet '" + target.SheetNumber + "' is a placeholder and cannot hold a view";
        findings.Add("Sheet " + sheetRead + " is a PLACEHOLDER - a row in the drawing list with no "
            + "title block. Revit puts no view or schedule on one, so nothing is on it.");
    }
    else
    {
        sheetRead = target.SheetNumber + " - " + target.Name;

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

        // ------------------------------------------------------------------
        // THE VIEWPORTS - views and legends
        // ------------------------------------------------------------------
        var rows = new List<KeyValuePair<string, string>>();
        foreach (var portId in target.GetAllViewports())
        {
            var port = doc.GetElement(portId) as Viewport;
            if (port == null) continue;

            var detail = "";
            try
            {
                var number = port.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER);
                if (number != null && number.HasValue) detail = number.AsString() ?? "";
            }
            catch (Exception) { }

            var where = "";
            try { where = "  centre (" + millimetres(port.GetBoxCenter()) + ") mm"; }
            catch (Exception) { where = "  centre not read"; }

            var view = doc.GetElement(port.ViewId) as View;
            var row = view == null
                ? "a viewport whose view Revit did not hand back"
                : view.Name + "  (" + typeWords(view) + ", " + scaleOf(view) + ")";

            rows.Add(new KeyValuePair<string, string>(detail,
                (detail.Length > 0 ? detail : "-") + "  " + row + where));
        }
        rows.Sort(byDetail);
        viewCount = rows.Count;
        viewsOnSheet = string.Join(" || ", rows.Select(r => r.Value).ToArray());

        // ------------------------------------------------------------------
        // THE SCHEDULES - by their owner view, grouped by schedule
        // ------------------------------------------------------------------
        var pieces = new Dictionary<ElementId, int>();
        var placedAt = new Dictionary<ElementId, XYZ>();
        var nameOf = new Dictionary<ElementId, string>();
        foreach (var instance in new FilteredElementCollector(doc)
                                     .OfClass(typeof(ScheduleSheetInstance))
                                     .Cast<ScheduleSheetInstance>())
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
            }
            pieces[scheduleId]++;
        }

        var scheduleRows = new List<string>();
        foreach (var scheduleId in pieces.Keys)
        {
            var point = placedAt[scheduleId];
            scheduleRows.Add(nameOf[scheduleId]
                + (point != null ? "  placed at (" + millimetres(point) + ") mm" : "  position not read")
                + (pieces[scheduleId] > 1 ? "  - split into " + pieces[scheduleId] + " pieces" : ""));
        }
        scheduleRows.Sort(StringComparer.OrdinalIgnoreCase);
        scheduleCount = scheduleRows.Count;
        schedulesOnSheet = string.Join(" || ", scheduleRows.ToArray());

        findings.Add("Sheet " + sheetRead + ": " + viewCount + " view" + (viewCount == 1 ? "" : "s")
            + " and " + scheduleCount + " schedule" + (scheduleCount == 1 ? "" : "s") + ".");
        if (viewCount == 0 && scheduleCount == 0)
            findings.Add("Nothing is placed on it - no view, no legend and no schedule.");
        if (revisionSchedules > 0)
            findings.Add("Its title block carries " + revisionSchedules + " revision schedule"
                + (revisionSchedules == 1 ? "" : "s") + ", which belong to the title block family "
                + "and are not counted.");
    }
}
