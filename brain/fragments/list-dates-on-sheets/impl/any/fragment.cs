// NOT STANDALONE. Assumes `doc` is in scope, and leaves `uniqueDateCount`,
// `dateList`, `sheetsWithDateCount`, `sheetsRead`, `notesOnSheets`,
// `notesInViewsNotRead`, `sheetsWithNoDate` and `summary` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// EVERY SHEET IN THE MODEL, AND NOTHING SELECTED.
//
// EXTRACT_DATES_FROM_TEXT reads the sheets it is HANDED, which is right when
// somebody has picked the sheets of one issue. The question this answers is
// "which dates are on our sheets at all", and selecting nineteen sheets in the
// project browser first is not part of that question. So this reads the
// document, and nothing else.
//
// A NOTE COUNTS WHEN ITS OWNER VIEW IS A SHEET - AND ONLY THEN.
//
// A text note typed while a sheet is open belongs to the sheet. A note typed
// in a plan belongs to the plan, even when the plan is placed on six sheets,
// and the modeller asked for the text ON the sheets (2026-10-10). So the test
// is the note's own OwnerViewId against the sheets in the model - not a
// view-scoped collector, which would leave the question "does a sheet's
// collector reach into its viewports" to Revit's behaviour rather than to this
// file. Every note that fails the test is still COUNTED, as
// notesInViewsNotRead, so an answer of no dates can be told from an answer
// that never looked.
//
// ONE COLLECTOR OVER THE WHOLE MODEL, NOT ONE PER SHEET - the reason
// FIND_DATES_IN_VIEWS gives: a model has hundreds of views and a handful of
// notes, and asking each sheet for its notes is the same answer at many times
// the cost.
//
// THE PARSER IS FIND_DATES_IN_VIEWS' AND EXTRACT_DATES_FROM_TEXT'S, CHARACTER
// FOR CHARACTER. Three fragments reading one note and reporting different
// dates would leave nobody able to say which to believe. A day, a written
// month of three or more letters, a 2 or 4 digit year; a calendar check; and
// a NUMERIC MONTH REFUSED, because 03-04-2026 is 3 April in Qatar and 4 March
// in America.
//
// THE ANSWER IS A STRING, ON PURPOSE. The add-in reports a list as its count
// and its first three items, and a dictionary as its count alone
// (RevitFragment.Describe). FIND_DATES_IN_VIEWS' dates are in a list, and on
// 2026-10-10 seven unique dates were found and five never reached the reply.
// A string is sent whole, so `dateList` carries every date.
//
// NO ElementId IS EVER READ AS A NUMBER. Ids are dictionary keys and compared
// to InvalidElementId, which reads the same 2020 to 2027.

var uniqueDateCount = 0;
var dateList = "";
var sheetsWithDateCount = 0;
var sheetsRead = 0;
var notesOnSheets = 0;
var notesInViewsNotRead = 0;
var sheetsWithNoDate = "";
var summary = "";

var months = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
string[] names = { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };
for (int i = 0; i < names.Length; i++) months[names[i]] = i + 1;

// Written out rather than expressed as one clever pattern, for the same reason
// it is in EXTRACT_DATES_FROM_TEXT: the separators differ per office and this
// has to be readable when one of them turns up unmatched.
Func<string, List<string>> datesIn = text =>
{
    var found = new List<string>();
    if (string.IsNullOrEmpty(text)) return found;

    var pieces = text.Split(new char[] { ' ', '\t', '\r', '\n', '-', '/', '.', ',', '(', ')', ':' },
                            StringSplitOptions.RemoveEmptyEntries);

    for (int i = 0; i + 2 < pieces.Length; i++)
    {
        int day, year;
        if (!int.TryParse(pieces[i], out day)) continue;

        string monthWord = pieces[i + 1];
        if (monthWord.Length < 3) continue;
        string key = monthWord.Substring(0, 3);
        if (!months.ContainsKey(key)) continue;
        int month = months[key];

        if (!int.TryParse(pieces[i + 2], out year)) continue;

        // A two-digit year is this century: a drawing dated 25 is 2025, and a
        // drawing dated 1925 does not exist in a Revit model.
        if (year < 100) year += 2000;
        if (year < 1900 || year > 2200) continue;

        // The calendar check. 31 SEP is not a date, and neither is 30 FEB.
        int lastDay = 31;
        if (month == 4 || month == 6 || month == 9 || month == 11) lastDay = 30;
        else if (month == 2) lastDay = (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? 29 : 28;
        if (day < 1 || day > lastDay) continue;

        // One canonical spelling, so MAY and May do not become two issues.
        string canonical = day.ToString("00") + "-" +
                           names[month - 1].ToUpperInvariant() + "-" + year.ToString("0000");
        if (!found.Contains(canonical)) found.Add(canonical);
    }

    return found;
};

// THE SHEETS FIRST, so a note's owner can be looked up rather than fetched.
// A sheet VIEW TEMPLATE comes back from the same collector looking like a
// sheet and is not one - LIST_SHEETS drops it on IsTemplate, and so does this.
var sheetsById = new Dictionary<ElementId, ViewSheet>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)))
{
    var sheet = element as ViewSheet;
    if (sheet == null || sheet.IsTemplate) continue;
    sheetsById[sheet.Id] = sheet;
}
sheetsRead = sheetsById.Count;

var sheetsOfDate = new Dictionary<string, List<ElementId>>();
var notesOfDate = new Dictionary<string, int>();
var sheetsHoldingADate = new HashSet<ElementId>();
var notesUnreadable = 0;

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(TextNote)))
{
    var note = element as TextNote;
    if (note == null) continue;

    var ownerId = note.OwnerViewId;
    ViewSheet owner;
    if (ownerId == null || ownerId == ElementId.InvalidElementId
        || !sheetsById.TryGetValue(ownerId, out owner))
    {
        // Typed in a plan, a section, a legend, a drafting view - not on a
        // sheet. Counted, never read.
        notesInViewsNotRead++;
        continue;
    }

    notesOnSheets++;

    string text;
    // A note whose text cannot be read costs that note, not the report - and
    // it is counted, so the summary says one was skipped rather than hiding it.
    try { text = note.Text; }
    catch { notesUnreadable++; continue; }

    foreach (var date in datesIn(text))
    {
        List<ElementId> where;
        if (!sheetsOfDate.TryGetValue(date, out where))
        {
            where = new List<ElementId>();
            sheetsOfDate[date] = where;
            notesOfDate[date] = 0;
        }
        if (!where.Contains(ownerId)) where.Add(ownerId);
        notesOfDate[date] = notesOfDate[date] + 1;
        sheetsHoldingADate.Add(ownerId);
    }
}

uniqueDateCount = sheetsOfDate.Count;
sheetsWithDateCount = sheetsHoldingADate.Count;

// SHEET NUMBERS ARE COMPARED AS TEXT, as LIST_SHEETS does: "M-101", "A2.03"
// and "SK-07" are all normal, and none of them is a number.
Comparison<ElementId> bySheetNumber = (a, b) =>
    string.Compare(sheetsById[a].SheetNumber ?? "", sheetsById[b].SheetNumber ?? "",
                   StringComparison.OrdinalIgnoreCase);

// OLDEST FIRST. The canonical spelling is DD-MON-YYYY, so its order as text is
// meaningless - 01-DEC comes before 31-JAN. It is turned back into a number
// for sorting only, from the parser's own spelling.
Func<string, int> dayNumber = canonical =>
    int.Parse(canonical.Substring(7, 4)) * 10000
    + months[canonical.Substring(3, 3)] * 100
    + int.Parse(canonical.Substring(0, 2));

var dates = new List<string>(sheetsOfDate.Keys);
dates.Sort((a, b) => dayNumber(a).CompareTo(dayNumber(b)));

var rows = new List<string>();
foreach (var date in dates)
{
    var where = sheetsOfDate[date];
    where.Sort(bySheetNumber);

    var numbers = new List<string>();
    foreach (var id in where) numbers.Add(sheetsById[id].SheetNumber);

    rows.Add(string.Format("{0} - {1} note(s) on {2} sheet(s): {3}",
        date, notesOfDate[date], where.Count, string.Join(", ", numbers.ToArray())));
}
// ONE LINE, "  ||  " between dates - LIST_MATERIALS' separator. Every date is
// in it; nothing is capped.
dateList = string.Join("  ||  ", rows.ToArray());

var withNone = new List<ElementId>();
foreach (var id in sheetsById.Keys)
    if (!sheetsHoldingADate.Contains(id)) withNone.Add(id);
withNone.Sort(bySheetNumber);

var noneLabels = new List<string>();
foreach (var id in withNone)
{
    // A PLACEHOLDER can hold no text at all - it has no view to type in - so
    // it is labelled, and nobody goes looking on it for a date it cannot carry.
    var sheet = sheetsById[id];
    noneLabels.Add(sheet.IsPlaceholder ? sheet.SheetNumber + " (placeholder)" : sheet.SheetNumber);
}
sheetsWithNoDate = string.Join(", ", noneLabels.ToArray());

// WHAT WAS READ AND WHAT WAS NOT, said every time, because "no date" is only
// an answer next to how much was looked at.
string notRead =
    " Not read: " + notesInViewsNotRead + " text note(s) typed inside views (plans, sections, legends, "
    + "drafting views) - a note in a view placed on a sheet belongs to that view;"
    + " title block labels and parameters such as Sheet Issue Date, which are not text notes"
    + " (READ_ELEMENT_PARAMETERS reads those);"
    + " and a date with a NUMERIC month such as 09-09-2026, refused because 03-04-2026 is"
    + " 3 April in one country and 4 March in another."
    + (notesUnreadable > 0 ? " " + notesUnreadable + " note(s) on sheets could not be read and were skipped." : "");

if (sheetsRead == 0)
{
    summary = "This model has no sheets, so there is no text on a sheet to read." + notRead;
}
else if (notesOnSheets == 0)
{
    summary = sheetsRead + " sheet(s) read, and no text note is typed on any of them." + notRead;
}
else if (uniqueDateCount == 0)
{
    summary = sheetsRead + " sheet(s) read, " + notesOnSheets + " text note(s) typed on them, "
        + "and none carries a date with a written month." + notRead;
}
else
{
    summary = uniqueDateCount + " distinct date(s) on " + sheetsWithDateCount + " of "
        + sheetsRead + " sheet(s), from " + notesOnSheets + " text note(s) typed on sheets."
        + " Each date is listed once, oldest first, with the sheets it is on (dateList)." + notRead;
}
