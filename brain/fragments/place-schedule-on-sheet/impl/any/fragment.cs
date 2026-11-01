// NOT STANDALONE. Assumes `doc`, `elements`, `sheetId`, `atXMm`, `atYMm`,
// `splitInto` and `segment` are in scope; leaves `placed`, `refused` and
// `placedAt` behind.
//
// VERSION 2 (2026-11-01, FRAGMENT-ISSUES row 5b-1363): SPLIT A SCHEDULE AND
// PLACE ITS SEGMENTS. Both new inputs are declared with the absent word
// "none", so a caller of version 1 is bound "none" twice and gets version 1 to
// the letter: the whole schedule placed by its top-left at the point.
//
//   splitInto  "3"          split into that many segments - ViewSchedule.
//                           Split(int). ASSUMPTION the proof settles: the
//                           number is the count of SEGMENTS, of equal height.
//              "200, 150"   split by heights in mm on paper, divided by 304.8
//                           - Split(IList<double>). ASSUMPTION: one height per
//                           segment, top first, in paper feet.
//              "none"       no split (version 1).
//              A schedule that is ALREADY split is refused a second split by
//              name, and nothing is placed: merging segments is done in Revit.
//   segment    "2"          place that segment, 1 the TOP one - the words a
//                           person uses; Revit counts from 0, and the read
//                           back says Revit's own index too.
//              "all"        every segment, side by side: the first at the
//                           point, each next one to the right of the one
//                           before, by that one's width on the sheet (its
//                           bounding box, after a regenerate) plus 10 mm.
//                           ASSUMPTION the proof settles: that the bounding
//                           box of a placed segment is its printed extent.
//              "none"       version 1's call, ScheduleSheetInstance.Create
//                           with no segment. ASSUMPTION: what that places for
//                           a schedule already split is NOT said by the
//                           reference - the read back names the segment each
//                           new instance shows, so the proof records it.
//
// SPLIT, IsSplit, GetSegmentCount AND THE FIVE-ARGUMENT Create ARE IN THE
// REVIT 2022 REFERENCE AND LATER, NOT 2020 OR 2021 - read off the shipped
// assemblies; Autodesk's own notes give 2022.1 as the update that added them.
// They are reached BY NAME at run time, not under `#if`, because the add-in
// compiles fragments with no release symbols (FRAGMENT-ISSUES 5b-181). On 2020
// and 2021 a split or a segment asked for refuses THAT SCHEDULE in plain words
// and places nothing for it - placing the whole schedule when a segment was
// asked is a different drawing from the one asked for. "all" or "1" on a
// schedule that is not split is the whole schedule, on any release, and said.
//
// A SPLIT THAT DOES NOT COME OUT AS PLANNED UNDOES THE WHOLE CALL. A segment
// asked for is checked against the planned number of segments BEFORE the
// split; if Revit then reports a different number, this throws and the
// add-in rolls everything back - a split nobody can place as asked is not
// kept.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// A SCHEDULE IS NOT PLACED THE WAY A VIEW IS. A view goes on a sheet as a
// Viewport; a schedule goes on as a ScheduleSheetInstance. PLACE_VIEW_ON_SHEET
// refuses schedules on purpose and names "a schedule that needs a different
// call" - this is that call.
//
// THE SAME SCHEDULE CAN GO ON MANY SHEETS, which is the OPPOSITE of a view. So
// nothing here refuses a repeat - and an accidental double placement is
// possible in a way it is not for a drawing, which is why what was placed is
// reported by sheet.
//
// A SCHEDULE HANGS FROM ITS TOP-LEFT. The point given is where the corner goes.
// The first guess is usually the centre, and a schedule placed by its centre
// sits half off the sheet.
//
// mm TO INTERNAL FEET BY /304.8, AND BACK BY *304.8. Plain arithmetic at the
// edge, never a units API - that is the call that breaks at Revit 2021. The
// read-back is reported in mm because that is the number somebody checks
// against the drawing.
//
// READ FIRST, WRITE, READ BACK. `placedAt` is the instance's own Point read
// after placing, not the point that was asked for.

var placed = new List<ElementId>();
var refused = new List<string>();
var placedAt = new List<string>();

var sheet = doc.GetElement(sheetId) as ViewSheet;

// ---- VERSION 2: the split and the segment, read once ---------------------
var invariant = System.Globalization.CultureInfo.InvariantCulture;
var splitText = (splitInto ?? "").Trim();
var segmentText = (segment ?? "").Trim().ToLowerInvariant();
var wantSplitCount = 0;
var wantSplitHeights = new List<double>();   // feet
var wantSegment = 0;                        // 1-based; 0 none; -1 all
string askProblem = null;

if (splitText.Length > 0 && !string.Equals(splitText, "none", StringComparison.OrdinalIgnoreCase))
{
    var parts = splitText.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(p => p.Trim().Replace("mm", "").Trim()).ToList();
    int count;
    if (parts.Count == 1 && int.TryParse(parts[0], System.Globalization.NumberStyles.None, invariant, out count))
    {
        if (count < 2) askProblem = "splitInto " + splitText + " - a split makes at least 2 segments";
        else wantSplitCount = count;
    }
    else
    {
        foreach (var part in parts)
        {
            double mm;
            if (!double.TryParse(part, System.Globalization.NumberStyles.Float, invariant, out mm) || mm <= 0)
            {
                askProblem = "splitInto '" + splitText + "' is not a number of segments ('3') or heights in mm "
                    + "with commas between ('200, 150')";
                break;
            }
            wantSplitHeights.Add(mm / 304.8);
        }
        if (askProblem == null && wantSplitHeights.Count < 2)
            askProblem = "splitInto '" + splitText + "' - give at least two heights, one per segment";
    }
}
if (segmentText.Length > 0 && segmentText != "none")
{
    int number;
    if (segmentText == "all" || segmentText == "every segment" || segmentText == "all segments") wantSegment = -1;
    else if (int.TryParse(segmentText, System.Globalization.NumberStyles.None, invariant, out number) && number >= 1)
        wantSegment = number;
    else askProblem = (askProblem == null ? "" : askProblem + "; ") + "segment '" + segment
        + "' is not a segment - give its number, 1 the top one, or 'all'";
}
var plannedSegments = wantSplitCount > 0 ? wantSplitCount : wantSplitHeights.Count;
if (askProblem == null && plannedSegments > 0 && wantSegment > plannedSegments)
    askProblem = "segment " + wantSegment + " was asked of a split into " + plannedSegments + " segments";

// Reached by name: 2022 and later only.
var splitByCount = typeof(ViewSchedule).GetMethod("Split", new[] { typeof(int) });
var splitByHeights = typeof(ViewSchedule).GetMethod("Split", new[] { typeof(IList<double>) });
var isSplitCall = typeof(ViewSchedule).GetMethod("IsSplit", Type.EmptyTypes);
var segmentCountCall = typeof(ViewSchedule).GetMethod("GetSegmentCount", Type.EmptyTypes);
var createSegment = typeof(ScheduleSheetInstance).GetMethod("Create",
    new[] { typeof(Document), typeof(ElementId), typeof(ElementId), typeof(XYZ), typeof(int) });
var segmentIndexOf = typeof(ScheduleSheetInstance).GetProperty("SegmentIndex");
var canSplit = splitByCount != null && splitByHeights != null && isSplitCall != null
    && segmentCountCall != null && createSegment != null;
Func<Exception, string> revitSaid = failure =>
    ((failure is System.Reflection.TargetInvocationException && failure.InnerException != null)
        ? failure.InnerException : failure).Message.TrimEnd('.');

if (sheet == null)
{
    refused.Add("What was given as the sheet is not a sheet in this project. Nothing was placed");
}
else if (askProblem != null)
{
    refused.Add(askProblem + ". Nothing was split or placed");
}
else
{
    var at = new XYZ(atXMm / 304.8, atYMm / 304.8, 0.0);

    foreach (var element in elements)
    {
        var schedule = element as ViewSchedule;
        if (schedule == null)
        {
            refused.Add(string.Format("'{0}' is not a schedule, so it cannot be placed this way. A "
                + "drawing view goes on a sheet through PLACE_VIEW_ON_SHEET",
                element == null ? "(nothing)" : element.Name));
            continue;
        }

        // A schedule template is not a schedule anybody puts on a drawing, and
        // Revit's refusal for one reads as a general failure.
        if (schedule.IsTemplate)
        {
            refused.Add(string.Format("'{0}' is a schedule TEMPLATE, not a schedule - there is "
                + "nothing to print", schedule.Name));
            continue;
        }

        // ---- VERSION 2: split first, when asked -------------------------
        var segmentWords = wantSegment == -1 ? "every segment" : "segment " + wantSegment;
        if ((plannedSegments > 0 || wantSegment != 0) && !canSplit)
        {
            refused.Add(string.Format("'{0}' - Revit 2020 and 2021 cannot split a schedule or place one "
                + "segment of it: Split arrived at Revit 2022 (the reference packages carry it from 2022; "
                + "Autodesk's notes give 2022.1). Nothing was placed for it - placing the whole schedule "
                + "would not be what was asked", schedule.Name));
            continue;
        }
        var segmentCount = 1;
        var isSplitNow = false;
        if (canSplit)
        {
            try
            {
                isSplitNow = (bool)isSplitCall.Invoke(schedule, null);
                if (isSplitNow) segmentCount = (int)segmentCountCall.Invoke(schedule, null);
            }
            catch (Exception failure)
            {
                refused.Add(string.Format("'{0}' - its segments could not be read: {1}. Nothing was placed",
                    schedule.Name, revitSaid(failure)));
                continue;
            }
        }
        var splitNote = "";
        if (plannedSegments > 0)
        {
            if (isSplitNow)
            {
                refused.Add(string.Format("'{0}' is already split into {1} segments, so it was not split "
                    + "again and nothing was placed. Merge its segments in Revit first, or leave splitInto "
                    + "out and name the segment to place", schedule.Name, segmentCount));
                continue;
            }
            try
            {
                if (wantSplitCount > 0) splitByCount.Invoke(schedule, new object[] { wantSplitCount });
                else splitByHeights.Invoke(schedule, new object[] { wantSplitHeights });
                segmentCount = (int)segmentCountCall.Invoke(schedule, null);
            }
            catch (Exception failure)
            {
                refused.Add(string.Format("'{0}' - Revit refused to split it: {1}. Nothing was placed for it",
                    schedule.Name, revitSaid(failure)));
                continue;
            }
            if (segmentCount != plannedSegments)
                throw new InvalidOperationException(string.Format("'{0}' was split for {1} segments and Revit "
                    + "reports {2}. Nothing is kept - the whole change is rolled back", schedule.Name,
                    plannedSegments, segmentCount));
            isSplitNow = true;
            splitNote = string.Format(" (split into {0} segments)", segmentCount);
        }
        if (wantSegment > 0 && wantSegment > segmentCount)
        {
            refused.Add(string.Format("'{0}' has {1} segment(s), so segment {2} does not exist. Nothing was "
                + "placed for it", schedule.Name, segmentCount, wantSegment));
            continue;
        }

        // Which segments go on the sheet: none named is version 1's call.
        var toPlace = new List<int>();                // Revit's 0-based index; -1 version 1
        if (wantSegment == 0 || !isSplitNow) toPlace.Add(-1);
        else if (wantSegment == -1) for (var i = 0; i < segmentCount; i++) toPlace.Add(i);
        else toPlace.Add(wantSegment - 1);
        if (wantSegment != 0 && !isSplitNow)
            splitNote = string.Format(" (it is not split, so {0} is the whole schedule)", segmentWords);

        var next = at;
        foreach (var index in toPlace)
        {
            ScheduleSheetInstance instance;
            try
            {
                instance = index < 0
                    ? ScheduleSheetInstance.Create(doc, sheet.Id, schedule.Id, next)
                    : createSegment.Invoke(null, new object[] { doc, sheet.Id, schedule.Id, next, index })
                        as ScheduleSheetInstance;
            }
            catch (Exception ex)
            {
                refused.Add(string.Format("'{0}'{1} on sheet '{2}' - Revit refused: {3}",
                    schedule.Name, index < 0 ? "" : " segment " + (index + 1), sheet.Name, revitSaid(ex)));
                continue;
            }

            if (instance == null)
            {
                refused.Add(string.Format("'{0}'{1} on sheet '{2}' - Revit reported no error and placed "
                    + "nothing", schedule.Name, index < 0 ? "" : " segment " + (index + 1), sheet.Name));
                continue;
            }

            placed.Add(instance.Id);

            // Read back the instance's own point, and the segment it shows.
            var shows = "";
            if (segmentIndexOf != null)
            {
                try
                {
                    var revitIndex = (int)segmentIndexOf.GetValue(instance, null);
                    if (isSplitNow) shows = string.Format(", showing segment {0} (Revit's index {1})",
                        revitIndex + 1, revitIndex);
                }
                catch (Exception) { shows = ", its segment could not be read back"; }
            }
            try
            {
                var where = instance.Point;
                placedAt.Add(string.Format("'{0}'{1} on sheet '{2}' at {3:0.#} mm, {4:0.#} mm from the "
                    + "sheet origin - top-left corner{5}", schedule.Name, splitNote, sheet.Name,
                    where.X * 304.8, where.Y * 304.8, shows));

                // The next segment to the right of this one, by its width on
                // the sheet - read after a regenerate, since a new instance
                // has no extent until then.
                if (toPlace.Count > 1)
                {
                    var width = 0.0;
                    try
                    {
                        doc.Regenerate();
                        var box = instance.get_BoundingBox(sheet);
                        if (box != null) width = box.Max.X - box.Min.X;
                    }
                    catch (Exception) { width = 0.0; }
                    if (width <= 0)
                    {
                        placedAt.Add(string.Format("'{0}' - the width of the segment just placed could not be "
                            + "read, so the next one is placed 10 mm to the right of its corner and the two "
                            + "OVERLAP - move them apart on the sheet", schedule.Name));
                    }
                    next = new XYZ(where.X + width + 10.0 / 304.8, where.Y, 0.0);
                }
            }
            catch (Exception)
            {
                placedAt.Add(string.Format("'{0}' on sheet '{1}' - placed, and its position could not "
                    + "be read back", schedule.Name, sheet.Name));
            }
        }
    }
}
