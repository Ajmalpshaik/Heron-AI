// NOT STANDALONE. Assumes `doc` is in scope, and leaves `datesFound`,
// `viewNames`, `viewsWithDates`, `notesRead` and `viewsScanned` behind.
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

var datesFound = new Dictionary<string, IList<ElementId>>();
var viewNames = new Dictionary<ElementId, string>();
var findings = new List<string>();
var notesRead = 0;
var viewsScanned = 0;

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

var seenViews = new HashSet<ElementId>();

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(TextNote)))
{
    var note = element as TextNote;
    if (note == null) continue;

    notesRead++;

    var ownerId = note.OwnerViewId;
    if (ownerId == null || ownerId == ElementId.InvalidElementId) continue;

    if (seenViews.Add(ownerId)) viewsScanned++;

    string text = "";
    // A note whose text cannot be read costs that note, not the report. One
    // corrupt annotation must not hide every date in the model.
    try { text = note.Text; }
    catch { continue; }

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
        }
        if (!where.Contains(ownerId)) where.Add(ownerId);
    }
}

var viewsWithDates = viewNames.Count;

if (notesRead == 0)
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
        + "EXTRACT_DATES_FROM_TEXT is the one that answers per sheet.");
}
