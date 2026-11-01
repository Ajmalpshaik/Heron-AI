// NOT STANDALONE. Assumes `doc`, `elements`, `sheetsOnly` and
// `onlySheetsGiven` are in scope, and leaves `datesFound`, `viewNames`,
// `viewsWithDates`, `findings`, `notesRead`, `viewsScanned`, `uniqueDateCount`,
// `dateList`, `sheetsWithDateCount`, `sheetsRead`, `notesOnSheets`,
// `notesInViewsNotRead`, `sheetsWithNoDate`, `summary` and `notASheet` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// EVERY DATE SAYS WHERE IT WAS FOUND, AND THAT IS THE WHOLE POINT.
//
// EXTRACT_DATES_FROM_TEXT reads the notes ON a sheet and attributes each date
// TO that sheet, which is right for revision work and wrong for finding a date
// somebody typed in a plan. A plan can sit on five sheets; a date typed inside
// it belongs to the plan, and attributing it to all five would invent four
// dates that were never written. So this one reports the OWNING VIEW with every
// date and never rolls anything up to a sheet.
//
// ONE COLLECTOR OVER THE WHOLE MODEL, NOT ONE PER VIEW.
//
// A model has hundreds of views and a handful of text notes. Walking the views
// and asking each for its notes is the same answer at many times the cost, and
// on a real project the difference is a question that answers and one that is
// abandoned.
//
// THE PARSER IS THE SAME ONE EXTRACT_DATES_FROM_TEXT USES, DELIBERATELY.
//
// Two fragments reading the same note and reporting different dates would be
// worse than either of them being wrong alone, because there would be no way to
// tell which to believe. The rules are identical: a day, a written month of 3
// or more letters, a 2 or 4 digit year. A NUMERIC MONTH IS REFUSED - 03-04-2026
// is 3 April in one country and 4 March in another, and a fragment that guessed
// would be wrong for half the world.
//
// A SHEET IS A VIEW AND IS INCLUDED, said plainly because it looks like overlap
// and is not. This fragment answers "where in this model is a date typed"; the
// other answers "what date does this sheet carry". The first needs the sheet's
// own notes in the list to be a complete answer.
//
// VERSION 2 (2026-11-01) - LIST_DATES_ON_SHEETS MERGED IN, AND WHAT IT HAD
// THAT THIS LACKED.
//
// THE SHEETS ONLY, WHEN A SWITCH SAYS SO. `sheetsOnly` on reads only the text
// typed ON a sheet - a note whose OWN owner view is a sheet - every sheet, and
// nothing selected. A note typed in a plan belongs to the plan, even when the
// plan is placed on six sheets, and the modeller asked for the text ON the
// sheets (2026-10-10). The test is the note's OwnerViewId against the sheets
// in the model - not a view-scoped collector, which would leave "does a
// sheet's collector reach into its viewports" to Revit rather than to this
// file. Every note that fails it is still COUNTED, as notesInViewsNotRead.
//
// THE SHEETS HANDED IN, BY A SECOND SWITCH, NEVER GUESSED. `onlySheetsGiven`
// on reads only the sheets in `elements` - the sheets of one issue, selected
// - and implies sheetsOnly. It is NEVER inferred from an empty `elements`:
// the binder hands an absent list in EMPTY (HeronBindingNote.EmptyWhenAbsent),
// so "nothing handed in" and "read them all" would be one value, and a job
// meant for one issue's sheets would quietly read the whole set. With the
// switch on and nothing handed in, nothing is read and the summary says so.
// Something handed in that is not a sheet - a view, a duct, a sheet view
// template - is named in `notASheet`, never read. `elements` is not read at
// all with the switch off.
//
// A SHEET VIEW TEMPLATE IS NOT A SHEET, and is left out of the sheets read.
//
// EVERY DATE IN ONE STRING, UNCAPPED - `dateList`. The add-in reports a list
// as its count and its first three items, and a dictionary as its count
// alone (RevitFragment.Describe): on 2026-10-10 this card found seven unique
// dates and five never reached the reply. A string is sent whole. Oldest
// first; with the sheets switch, each date's sheet numbers; without, each
// date's views. `datesFound` is ordered the same way, and with the sheets
// switch its ids are the SHEETS - each date's sheets, the ones a revision
// for that date is put on (SET_SHEET_REVISIONS, handed them as elements).
//
// THE COUNTS, said every time, because "no date" is only an answer next to
// how much was looked at: uniqueDateCount, sheetsWithDateCount, sheetsRead,
// notesOnSheets, notesInViewsNotRead, sheetsWithNoDate and `summary`. A note
// whose text cannot be read is counted in the summary, never hidden.
//
// THE PARSER BELOW IS UNCHANGED, character for character - it is the block
// tests/test_sheet_date_parser.py holds the same in every sheet-date card.

var datesFound = new Dictionary<string, IList<ElementId>>();
var viewNames = new Dictionary<ElementId, string>();
var findings = new List<string>();
var notesRead = 0;
var viewsScanned = 0;
var uniqueDateCount = 0;
var dateList = "";
var sheetsWithDateCount = 0;
var sheetsRead = 0;
var notesOnSheets = 0;
var notesInViewsNotRead = 0;
var sheetsWithNoDate = "";
var summary = "";
var notASheet = new List<ElementId>();

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
// sheet and is not one - dropped on IsTemplate (VERSION 2).
var sheetsById = new Dictionary<ElementId, ViewSheet>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)))
{
    var sheet = element as ViewSheet;
    if (sheet == null || sheet.IsTemplate) continue;
    sheetsById[sheet.Id] = sheet;
}
var sheetsInModel = new HashSet<ElementId>(sheetsById.Keys);

// VERSION 2: WHICH NOTES ARE READ. Neither switch: every note in the model,
// as version 1. sheetsOnly: only notes typed on a sheet. onlySheetsGiven: only
// notes typed on the sheets handed in - it implies sheetsOnly.
bool sheetsScope = sheetsOnly || onlySheetsGiven;
if (onlySheetsGiven)
{
    var handedIn = new Dictionary<ElementId, ViewSheet>();
    foreach (var element in elements ?? new List<Element>())
    {
        if (element == null) continue;
        var sheet = element as ViewSheet;
        if (sheet == null || sheet.IsTemplate || !sheetsInModel.Contains(sheet.Id))
        {
            if (!notASheet.Contains(element.Id)) notASheet.Add(element.Id);
            continue;
        }
        handedIn[sheet.Id] = sheet;
    }
    sheetsById = handedIn;
}
sheetsRead = sheetsById.Count;

var notesOnOtherSheets = 0;
var notesUnreadable = 0;
var notesOfDate = new Dictionary<string, int>();
var sheetsHoldingADate = new HashSet<ElementId>();
var seenViews = new HashSet<ElementId>();

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(TextNote)))
{
    var note = element as TextNote;
    if (note == null) continue;

    var ownerId = note.OwnerViewId;
    bool onASheet = ownerId != null && sheetsById.ContainsKey(ownerId);

    if (sheetsScope)
    {
        // VERSION 2. On a sheet that was not handed in - counted apart.
        if (ownerId != null && sheetsInModel.Contains(ownerId) && !onASheet)
        {
            notesOnOtherSheets++;
            continue;
        }
        // Typed in a plan, a section, a legend, a drafting view - not on a
        // sheet. Counted, never read.
        if (!onASheet)
        {
            notesInViewsNotRead++;
            continue;
        }
    }

    notesRead++;

    if (ownerId == null || ownerId == ElementId.InvalidElementId) continue;

    if (seenViews.Add(ownerId)) viewsScanned++;
    if (onASheet) notesOnSheets++;

    string text = "";
    // A note whose text cannot be read costs that note, not the report - and
    // since version 2 it is counted, so the summary says one was skipped. One
    // corrupt annotation must not hide every date in the model.
    try { text = note.Text; }
    catch { notesUnreadable++; continue; }

    var dates = datesIn(text);
    if (dates.Count == 0) continue;

    // The view is named ONCE, and only when it turns out to hold a date. A
    // dictionary of every view in the model would bury the answer.
    if (!viewNames.ContainsKey(ownerId))
    {
        var owner = doc.GetElement(ownerId) as View;
        string label;
        if (owner == null)
        {
            label = "(the view this note belongs to could not be read)";
        }
        else
        {
            // A SHEET IS LABELLED AS ONE. Somebody reading "Level 1" and
            // "M102 - Plan HVAC L2" in the same list has to be able to tell
            // which date is on a drawing and which is inside it.
            var asSheet = owner as ViewSheet;
            label = asSheet == null
                ? owner.Name + "  (" + owner.ViewType + ")"
                : asSheet.SheetNumber + " - " + asSheet.Name + "  (SHEET)";
        }
        viewNames[ownerId] = label;
    }

    foreach (var date in dates)
    {
        IList<ElementId> where;
        if (!datesFound.TryGetValue(date, out where))
        {
            where = new List<ElementId>();
            datesFound[date] = where;
            notesOfDate[date] = 0;
        }
        if (!where.Contains(ownerId)) where.Add(ownerId);
        notesOfDate[date] = notesOfDate[date] + 1;
        if (onASheet) sheetsHoldingADate.Add(ownerId);
    }
}

var viewsWithDates = viewNames.Count;
uniqueDateCount = datesFound.Count;
sheetsWithDateCount = sheetsHoldingADate.Count;

// SHEET NUMBERS ARE COMPARED AS TEXT: "M-101", "A2.03" and "SK-07" are all
// normal, and none of them is a number.
Comparison<ElementId> bySheetNumber = (a, b) =>
    string.Compare(sheetsById[a].SheetNumber ?? "", sheetsById[b].SheetNumber ?? "",
                   StringComparison.OrdinalIgnoreCase);

// OLDEST FIRST (VERSION 2). The canonical spelling is DD-MON-YYYY, so its
// order as text is meaningless - 01-DEC comes before 31-JAN. It is turned
// back into a number for sorting only, from the parser's own spelling.
Func<string, int> dayNumber = canonical =>
    int.Parse(canonical.Substring(7, 4)) * 10000
    + months[canonical.Substring(3, 3)] * 100
    + int.Parse(canonical.Substring(0, 2));

var datesInOrder = new List<string>(datesFound.Keys);
datesInOrder.Sort((a, b) => dayNumber(a).CompareTo(dayNumber(b)));

var ordered = new Dictionary<string, IList<ElementId>>();
var rows = new List<string>();
foreach (var date in datesInOrder)
{
    var where = new List<ElementId>(datesFound[date]);
    if (sheetsScope)
    {
        // Every owner here is a sheet read; in sheet-number order.
        where.Sort(bySheetNumber);
        var numbers = new List<string>();
        foreach (var id in where) numbers.Add(sheetsById[id].SheetNumber);
        rows.Add(string.Format("{0} - {1} note(s) on {2} sheet(s): {3}",
            date, notesOfDate[date], where.Count, string.Join(", ", numbers.ToArray())));
    }
    else
    {
        var places = new List<string>();
        foreach (var id in where)
        {
            string label;
            places.Add(viewNames.TryGetValue(id, out label) ? label : id.ToString());
        }
        rows.Add(string.Format("{0} - {1} note(s) in {2} view(s): {3}",
            date, notesOfDate[date], where.Count, string.Join("; ", places.ToArray())));
    }
    ordered[date] = where;
}
datesFound = ordered;
// ONE LINE, "  ||  " between dates. Every date is in it; nothing is capped.
dateList = string.Join("  ||  ", rows.ToArray());

// The sheets read that carry no date typed ON them. A PLACEHOLDER can hold no
// text at all, so it is labelled, and nobody goes looking on it for a date.
var withNone = new List<ElementId>();
foreach (var id in sheetsById.Keys)
    if (!sheetsHoldingADate.Contains(id)) withNone.Add(id);
withNone.Sort(bySheetNumber);
var noneLabels = new List<string>();
foreach (var id in withNone)
{
    var sheet = sheetsById[id];
    noneLabels.Add(sheet.IsPlaceholder ? sheet.SheetNumber + " (placeholder)" : sheet.SheetNumber);
}
sheetsWithNoDate = string.Join(", ", noneLabels.ToArray());

if (notesRead == 0 && !sheetsScope)
{
    findings.Add("No text notes in this model at all, so no typed date could be found. "
        + "A date held in a title block PARAMETER is not typed text - "
        + "READ_ELEMENT_PARAMETERS reads those.");
}
else if (datesFound.Count == 0)
{
    findings.Add(string.Format(
        "{0} text note(s) read across {1} view(s), and none carries a date. A date written "
        + "with a NUMERIC month - 09-09-2026 - is deliberately not read: it is 9 September "
        + "in one country and 9 September in another only by luck, and 03-04 is not. Write "
        + "the month: 09 SEP 2026.", notesRead, viewsScanned));
}
else
{
    findings.Add(string.Format("{0} date(s) typed in {1} view(s), from {2} text note(s) read.",
        datesFound.Count, viewsWithDates, notesRead));

    foreach (var pair in datesFound)
    {
        var places = new List<string>();
        foreach (var id in pair.Value)
        {
            string label;
            places.Add(viewNames.TryGetValue(id, out label) ? label : id.ToString());
        }
        findings.Add(string.Format("{0}  - in {1}: {2}",
            pair.Key, pair.Value.Count, string.Join("; ", places.ToArray())));
    }

    findings.Add("Each date is reported against the view it was TYPED IN. A view placed on "
        + "several sheets carries its date once here, not once per sheet - "
        + (sheetsScope
              ? "and with the sheets switch on, only a note typed ON a sheet is read."
              : "sheetsOnly reads the notes typed on the sheets alone."));
}

// WHAT WAS READ AND WHAT WAS NOT (VERSION 2), said every time.
string notRead =
    " Not read: title block labels and parameters such as Sheet Issue Date, which are not"
    + " text notes (READ_ELEMENT_PARAMETERS reads those);"
    + " and a date with a NUMERIC month such as 09-09-2026, refused because 03-04-2026 is"
    + " 3 April in one country and 4 March in another."
    + (sheetsScope
          ? " " + notesInViewsNotRead + " text note(s) typed inside views (plans, sections, "
            + "legends, drafting views) were not read - a note in a view placed on a sheet "
            + "belongs to that view."
          : "")
    + (notesUnreadable > 0 ? " " + notesUnreadable + " note(s) could not be read and were skipped." : "")
    + (onlySheetsGiven
          ? " Only the " + sheetsRead + " sheet(s) handed in were read"
            + (notesOnOtherSheets > 0 ? ", and " + notesOnOtherSheets + " note(s) on other sheets were not" : "")
            + (notASheet.Count > 0 ? "; " + notASheet.Count + " thing(s) handed in are not sheets (notASheet)" : "")
            + "."
          : "");

if (onlySheetsGiven && sheetsRead == 0)
{
    summary = "Only the sheets handed in were to be read, and "
        + (notASheet.Count > 0 ? "nothing handed in is a sheet" : "no sheet was handed in")
        + " - so nothing was read. Select the sheets, or leave onlySheetsGiven off to read "
        + "every sheet." + notRead;
}
else if (sheetsScope && sheetsRead == 0)
{
    summary = "This model has no sheets, so there is no text on a sheet to read." + notRead;
}
else if (sheetsScope && notesOnSheets == 0)
{
    summary = sheetsRead + " sheet(s) read, and no text note is typed on any of them." + notRead;
}
else if (sheetsScope && uniqueDateCount == 0)
{
    summary = sheetsRead + " sheet(s) read, " + notesOnSheets + " text note(s) typed on them, "
        + "and none carries a date with a written month." + notRead;
}
else if (sheetsScope)
{
    summary = uniqueDateCount + " distinct date(s) on " + sheetsWithDateCount + " of "
        + sheetsRead + " sheet(s), from " + notesOnSheets + " text note(s) typed on sheets."
        + " Each date is listed once, oldest first, with the sheets it is on (dateList)." + notRead;
}
else
{
    summary = uniqueDateCount + " distinct date(s) typed in " + viewsWithDates + " view(s), from "
        + notesRead + " text note(s) read across " + viewsScanned + " view(s); "
        + sheetsWithDateCount + " of the " + sheetsRead + " sheet(s) carry a date typed ON the "
        + "sheet. Each date is listed once, oldest first, with the views it is in (dateList)."
        + notRead;
}
