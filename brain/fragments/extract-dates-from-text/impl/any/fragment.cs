// NOT STANDALONE. Assumes `doc` and `elements` are in scope, and leaves
// `datesFound`, `sheetsWithNoDate` and `notASheet` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE DATE ON A DRAWING IS OFTEN TYPED TEXT.
//
// Title blocks get filled in by hand, and a date typed into a text note is
// invisible to every schedule and every filter in the model. Turning that text
// into data is the whole job.
//
// A THREE-LETTER WORD BETWEEN TWO NUMBERS IS NOT A DATE.
//
// Every candidate is checked as a real calendar date: 31 SEP is not one, and a
// note reading "2 OFF 300" must not become one. Without that check the result
// is a list somebody has to re-read rather than act on.
//
// TEXT NOTES ON THE SHEET, NOT IN THE VIEWS ON IT.
//
// A date typed into a plan belongs to the plan. Counting it would attribute
// that date to every sheet the view is placed on.

var datesFound = new Dictionary<string, IList<ElementId>>();
var sheetsWithNoDate = new List<ElementId>();
var notASheet = new List<ElementId>();

var months = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
string[] names = { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };
for (int i = 0; i < names.Length; i++) months[names[i]] = i + 1;

// day, a 3+ letter month, a 2 or 4 digit year, separated by space, dash or
// slash. Written out rather than expressed as one clever pattern, because the
// separators differ per office and this has to be readable when one of them
// turns up unmatched.
Func<string, List<string>> datesIn = text =>
{
    var found = new List<string>();
    if (string.IsNullOrEmpty(text)) return found;

    var pieces = text.Split(new char[] { ' ', '\t', '\r', '\n', '-', '/', '.', ',', '(', ')', ':' },
                            StringSplitOptions.RemoveEmptyEntries);

    for (int i = 0; i + 2 < pieces.Length + 0; i++)
    {
        int day, year;
        if (!int.TryParse(pieces[i], out day)) continue;
        if (i + 2 >= pieces.Length) break;

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

foreach (var element in elements)
{
    var sheet = element as ViewSheet;
    if (sheet == null)
    {
        if (element != null) notASheet.Add(element.Id);
        continue;
    }

    bool anyOnThisSheet = false;

    // Scoped to the SHEET's own view, so a note inside a placed plan belongs
    // to the plan and not to every sheet that plan appears on.
    foreach (var element2 in new FilteredElementCollector(doc, sheet.Id)
                 .OfClass(typeof(TextNote))
                 .WhereElementIsNotElementType())
    {
        var note = element2 as TextNote;
        if (note == null) continue;

        string text = "";
        try { text = note.Text ?? ""; } catch { }

        foreach (var date in datesIn(text))
        {
            if (!datesFound.ContainsKey(date)) datesFound[date] = new List<ElementId>();
            if (!datesFound[date].Contains(sheet.Id)) datesFound[date].Add(sheet.Id);
            anyOnThisSheet = true;
        }
    }

    if (!anyOnThisSheet) sheetsWithNoDate.Add(sheet.Id);
}
