// NOT STANDALONE. Assumes `doc`, `elements`, `exportFolder` and
// `fieldDelimiter` are in scope; leaves `written`, `missingAfterExport`,
// `refused` and `rowsWritten` behind.
//
// NO TRANSACTION, AND NONE IS NEEDED - export is document I/O, not a model edit.
//
// RISK IS PUBLISH. Nothing in the model changes; a file is written that gets
// emailed to somebody, and one Ctrl+Z does not un-send it.
//
// THE FILE IS CHECKED ON DISK AFTERWARDS. Revit's Export returns nothing and
// throws inconsistently, so "it did not throw" is not evidence a file exists -
// the same read-back the PDF and DWG exports use, for the same reason.
//
// IT WRITES WHAT THE SCHEDULE PRINTS, FILTERS INCLUDED. Somebody exporting "the
// duct schedule" and getting a third of the ducts has a FILTERED schedule, not
// a broken export - so the row count is reported rather than left to be
// discovered in Excel.
//
// THE DELIMITER IS A STATED INPUT. A comma is wrong in every region whose
// decimal separator is a comma - the file opens as one column. An unrecognised
// word falls back to TAB, which is the safe one, and says so.
//
// A SET HOLDING NO SCHEDULE AT ALL IS REFUSED, not reported as `written 0`.
// An empty export folder and a selection with no schedule in it used to look
// the same from outside - both "nothing came out". Section 3h.1 of
// docs/FRAGMENT-ISSUES.md, 2026-09-09.
//
// System.IO IS FULLY QUALIFIED because the wrapper does not import it.

// A SCHEDULE ON A SHEET IS A ScheduleSheetInstance, NOT A ViewSchedule, AND
// CLICKING IT IS THE ONLY WAY A PERSON CAN POINT AT ONE. The same fix and the
// same reason as REPORT_SCHEDULE_DEFINITION, where it was found on 2026-09-08:
// a schedule is a VIEW, a view cannot be selected as an element, opening one
// selects its ROWS, and nothing in this library provides a ViewSchedule to
// chain from. So a bare cast refuses the only input that could ever arrive.
//
// The placement carries the id of the schedule it draws, so it is resolved
// here rather than passed over.
Func<Element, ViewSchedule> scheduleBehind = candidate =>
{
    var direct = candidate as ViewSchedule;
    if (direct != null) return direct;

    var placed = candidate as ScheduleSheetInstance;
    if (placed == null) return null;

    // Guarded like everything else: a placement whose schedule was deleted
    // under it should cost one element, not the whole run.
    try { return doc.GetElement(placed.ScheduleId) as ViewSchedule; }
    catch { return null; }
};

var written = new List<string>();
var missingAfterExport = new List<string>();
var refused = new List<string>();
var rowsWritten = 0;

var handed = 0;
var schedulesSeen = 0;

var folder = (exportFolder ?? "").Trim();

if (string.IsNullOrEmpty(folder))
{
    refused.Add("No export folder was given. Nothing was written - a default folder here would put "
        + "somebody's quantities somewhere they will not look for them");
}
else
{
    try
    {
        if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);
    }
    catch (Exception ex)
    {
        refused.Add(string.Format("'{0}' could not be created or reached: {1}", folder, ex.Message));
        folder = "";
    }
}

// The delimiter, resolved once. Unrecognised falls back to tab and says so.
var wanted = (fieldDelimiter ?? "").Trim().ToLowerInvariant();
var delimiter = "\t";

if (wanted == "," || wanted == "comma") delimiter = ",";
else if (wanted == ";" || wanted == "semicolon") delimiter = ";";
else if (wanted == "\t" || wanted == "tab" || wanted == "") delimiter = "\t";
else
{
    delimiter = "\t";
    refused.Add(string.Format("'{0}' is not a delimiter this fragment writes - a TAB was used "
        + "instead, which is the one that survives a region whose decimal separator is a comma",
        fieldDelimiter));
}

foreach (var element in elements)
{
    if (string.IsNullOrEmpty(folder)) break;
    if (element == null) continue;
    handed++;

    var schedule = scheduleBehind(element);
    if (schedule == null) continue;
    schedulesSeen++;

    // A schedule name can carry characters a file name cannot.
    var stem = schedule.Name;
    foreach (var bad in System.IO.Path.GetInvalidFileNameChars())
    {
        stem = stem.Replace(bad, '_');
    }

    var expected = System.IO.Path.Combine(folder, stem + ".csv");

    var options = new ViewScheduleExportOptions();
    options.FieldDelimiter = delimiter;
    options.TextQualifier = ExportTextQualifier.DoubleQuote;
    options.Title = true;
    options.ColumnHeaders = ExportColumnHeaders.OneRow;
    options.HeadersFootersBlanks = true;

    var threw = false;
    try
    {
        schedule.Export(folder, stem + ".csv", options);
    }
    catch (Exception ex)
    {
        threw = true;
        refused.Add(string.Format("'{0}' - Revit refused the export: {1}",
            schedule.Name, ex.Message));
    }

    // Checked on disk. Not throwing is not evidence.
    if (System.IO.File.Exists(expected))
    {
        written.Add(expected);
        try
        {
            var body = schedule.GetTableData().GetSectionData(SectionType.Body);
            rowsWritten += body.NumberOfRows;
        }
        catch { }
    }
    else if (!threw)
    {
        missingAfterExport.Add(string.Format("'{0}' - Revit reported no error and there is no file "
            + "at {1}", schedule.Name, expected));
    }
}

// Decided after the loop, and safe there: an element that is not a schedule
// `continue`s before Export is reached, so no file was written on this path.
if (!string.IsNullOrEmpty(folder) && handed > 0 && schedulesSeen == 0)
{
    refused.Add(string.Format(
        "not one of the {0} element(s) handed in is a schedule, so NOTHING WAS EXPORTED. "
        + "A schedule is a VIEW and cannot be selected in the model; what CAN be "
        + "selected is a schedule PLACED ON A SHEET, which this now reads through to "
        + "the schedule behind it. Click the schedule on the sheet, or select the "
        + "category 'Schedule Graphics'",
        handed));
}
