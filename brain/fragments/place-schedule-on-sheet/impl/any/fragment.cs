// NOT STANDALONE. Assumes `doc`, `elements`, `sheetId`, `atXMm` and `atYMm` are
// in scope; leaves `placed`, `refused` and `placedAt` behind.
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

if (sheet == null)
{
    refused.Add("What was given as the sheet is not a sheet in this project. Nothing was placed");
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

        ScheduleSheetInstance instance;
        try
        {
            instance = ScheduleSheetInstance.Create(doc, sheet.Id, schedule.Id, at);
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("'{0}' on sheet '{1}' - Revit refused: {2}",
                schedule.Name, sheet.Name, ex.Message));
            continue;
        }

        if (instance == null)
        {
            refused.Add(string.Format("'{0}' on sheet '{1}' - Revit reported no error and placed "
                + "nothing", schedule.Name, sheet.Name));
            continue;
        }

        placed.Add(instance.Id);

        // Read back the instance's own point. Not the point that was asked for.
        try
        {
            var where = instance.Point;
            placedAt.Add(string.Format("'{0}' on sheet '{1}' at {2:0.#} mm, {3:0.#} mm from the "
                + "sheet origin - top-left corner", schedule.Name, sheet.Name,
                where.X * 304.8, where.Y * 304.8));
        }
        catch (Exception)
        {
            placedAt.Add(string.Format("'{0}' on sheet '{1}' - placed, and its position could not "
                + "be read back", schedule.Name, sheet.Name));
        }
    }
}
