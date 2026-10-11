// NOT STANDALONE. Assumes `doc`, `description`, `dateFormat`, `onlyDates`,
// `issuedBy`, `issuedTo`, `numbering` and `issueState` are in scope, and leaves
// `datesFound`, `revisionsCreated`, `revisionsReused`, `revisionsAddedToSheets`,
// `sheetsChanged`, `dateRevisions`, `sheetsWithNoDate`, `revisionsRemaining`,
// `refused` and `summary` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one. Every
// revision it makes and every sheet it changes sits inside the add-in's one
// TransactionGroup, so the whole job is ONE entry in Revit's Undo list - and a
// throw anywhere below rolls ALL of it back, never half a set.
//
// WHY ONE FRAGMENT AND NOT A CHAIN. On 2026-10-10 (Revit 2020, model
// 4355-BHVD-3D-50C10-BL001A) the same job took LIST_DATES_ON_SHEETS, seven
// CREATE_REVISION calls and twenty-two FIND_SHEETS / SET_SHEET_REVISIONS calls,
// each its own Undo entry, with revisions named by "Seq. N - description" typed
// from an earlier read - and when the owner deleted Seq. 1, Revit renumbered
// the rest and one of those names went stale mid-job. A chain cannot loop over
// dates, and every name it carries goes stale the same way. Here each revision
// is held by its ElementId from the moment it is found or made, so a sequence
// number is only ever READ, for the report, after the last write.
//
// THE DATES ARE READ EXACTLY AS LIST_DATES_ON_SHEETS READS THEM - only a text
// note whose owner view is a sheet, through the same parser, copied character
// for character (tests/test_sheet_date_parser.py holds the copies equal). A
// note inside a plan placed on the sheet belongs to the plan and is not read,
// and a numeric month is refused: 03-04-2026 is 3 April in Qatar and 4 March
// in America, and a revision dated the wrong month goes out on a signed sheet.
//
// AN EXISTING REVISION IS REUSED, NEVER DUPLICATED. One with the same
// description and that date - its date typed exactly as this call would write
// it, or reading as that one date through the same parser - is the revision for
// that date, and is left exactly as it was: EDIT_REVISION changes a revision's
// columns, not this. Two that both match is a refusal naming both, because
// picking one is a guess about which the drawing office meant.
//
// NEW REVISIONS ARE MADE OLDEST DATE FIRST, so the sequence numbers Revit hands
// out run in date order. A reused revision keeps its place; when that breaks
// the order, the summary says so and names REORDER_REVISION.
//
// A SHEET'S REVISIONS ARE ADDED TO, NEVER REPLACED - SET_SHEET_REVISIONS' rule.
// The sheet's own list (the "Revisions on Sheet" ticks) is read, the new ids
// appended, and the list written back once per sheet. A revision the sheet
// already carries - ticked, or put there by a cloud - is counted as already
// there and not written again.
//
// WRITING THAT LIST CAN MAKE REVIT PURGE AN UNUSED REVISION (SET_SHEET_REVISIONS'
// warning). So every revision the project had before the call, and every one
// made by it, is looked for again after the model is regenerated, and one that
// has gone throws - the whole call is rolled back rather than kept with a
// revision missing from the project.
//
// NOTHING IS ISSUED UNLESS issueState SAYS "issue", and then only the
// revisions this call made, LAST, after every sheet has taken them: issuing
// locks a revision's columns, and EDIT_REVISION version 3 met an issued
// revision refusing a change on Revit 2020 (5b-409).
//
// EVERYTHING IS CHECKED BEFORE ANYTHING IS MADE. An empty description, a
// date format it cannot spell, a numbering the project lacks, an onlyDates
// entry no sheet carries, or no date on any sheet at all refuses with nothing
// created and no sheet changed.
//
// THE ANSWER IS A STRING, ON PURPOSE - LIST_DATES_ON_SHEETS' reason. The add-in
// reports a list as its count and first three items, so `dateRevisions` holds
// every date, its revision and its sheets on one line.
//
// NO ElementId IS EVER READ AS A NUMBER. Ids are compared and used as keys,
// which reads the same 2020 to 2027.

var datesFound = 0;
var revisionsCreated = 0;
var revisionsReused = 0;
var revisionsAddedToSheets = 0;
var sheetsChanged = 0;
var dateRevisions = "";
var sheetsWithNoDate = "";
var revisionsRemaining = 0;
var refused = "";
var summary = "";

// THE PARSER - LIST_DATES_ON_SHEETS', CHARACTER FOR CHARACTER, from `var months`
// to the end of `datesIn`. Change it there and here together, or not at all.
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

Func<string, string> asked = given =>
{
    var typed = (given ?? "").Trim();
    if (typed.Length == 0) return null;
    if (string.Equals(typed, "<blank>", StringComparison.OrdinalIgnoreCase)) return "";
    return typed;
};
Func<string, string> quoted = value => string.IsNullOrEmpty(value) ? "(blank)" : "'" + value + "'";
Func<Exception, string> revitSaid = failure =>
    (failure.InnerException ?? failure).Message.TrimEnd('.');
Func<Revision, string> nameOf = revision =>
    "Seq. " + revision.SequenceNumber + " - " + (revision.Description ?? "");

var problems = new List<string>();

// ---- description: the line every revision schedule prints ------------------
var descriptionText = (description ?? "").Trim();
if (descriptionText.Length == 0 || string.Equals(descriptionText, "<blank>", StringComparison.OrdinalIgnoreCase))
{
    problems.Add("the revisions need a description - it is the line every sheet's revision "
               + "schedule prints, and it is how an existing revision for a date is recognised");
}

// ---- dateFormat: how each date is written in the revision ------------------
// Blank writes the parser's own spelling, DD-MMM-YYYY - the one
// LIST_DATES_ON_SHEETS prints, 07-APR-2025. A format the caller names is
// spelt here rather than handed to .NET, whose MMM follows the PC's language
// and never prints capitals.
string[] monthWords = { "january", "february", "march", "april", "may", "june", "july",
                        "august", "september", "october", "november", "december" };
var pattern = (dateFormat ?? "").Trim();
if (pattern.Length == 0) pattern = "DD-MMM-YYYY";

Func<int, int, int, List<string>, string> spell = (day, month, year, trouble) =>
{
    var printed = "";
    bool sawDay = false, sawMonth = false, sawYear = false;
    var at = 0;
    while (at < pattern.Length)
    {
        var c = pattern[at];
        var letter = char.ToLowerInvariant(c);
        if (letter != 'd' && letter != 'm' && letter != 'y')
        {
            if (char.IsLetter(c) && trouble != null)
                trouble.Add("has the letter '" + c + "', which is not a day, a month or a year");
            printed += c;
            at++;
            continue;
        }

        var run = 1;
        while (at + run < pattern.Length && char.ToLowerInvariant(pattern[at + run]) == letter) run++;
        var token = pattern.Substring(at, run);
        at += run;

        if (letter == 'd')
        {
            sawDay = true;
            if (run == 1) printed += day.ToString();
            else if (run == 2) printed += day.ToString("00");
            else if (trouble != null) trouble.Add("has '" + token + "' - a day is D or DD");
        }
        else if (letter == 'y')
        {
            sawYear = true;
            if (run == 2) printed += (year % 100).ToString("00");
            else if (run == 4) printed += year.ToString("0000");
            else if (trouble != null) trouble.Add("has '" + token + "' - a year is YY or YYYY");
        }
        else
        {
            sawMonth = true;
            if (run == 1) printed += month.ToString();
            else if (run == 2) printed += month.ToString("00");
            else if (run == 3 || run == 4)
            {
                var word = run == 3 ? monthWords[month - 1].Substring(0, 3) : monthWords[month - 1];
                var rest = token.Substring(1);
                if (token == token.ToUpperInvariant()) printed += word.ToUpperInvariant();
                else if (token == token.ToLowerInvariant()) printed += word;
                else if (char.IsUpper(token[0]) && rest == rest.ToLowerInvariant())
                    printed += char.ToUpperInvariant(word[0]) + word.Substring(1);
                else if (trouble != null)
                    trouble.Add("has '" + token + "' - a month's name is printed in capitals (MMM), "
                              + "in small letters (mmm) or with a capital first (Mmm)");
            }
            else if (trouble != null) trouble.Add("has '" + token + "' - a month is M, MM, MMM or MMMM");
        }
    }
    if (trouble != null && (!sawDay || !sawMonth || !sawYear))
        trouble.Add("needs a day, a month and a year - without all three two dates can print the "
                  + "same, and the revision schedule could not tell them apart");
    return printed;
};

var formatTrouble = new List<string>();
spell(7, 4, 2025, formatTrouble);
if (formatTrouble.Count > 0)
{
    problems.Add("dateFormat '" + pattern + "' " + string.Join("; ", formatTrouble.ToArray())
               + ". It takes D or DD for the day, M or MM for the month's number, MMM or MMMM for "
               + "its name, and YY or YYYY for the year; anything that is not a letter is written "
               + "as typed, so DD-MMM-YYYY writes 07-APR-2025, DD.MM.YYYY writes 07.04.2025 and "
               + "D Mmm YYYY writes 7 Apr 2025");
}

// ---- onlyDates: the dates to act on, when not every one ---------------------
// Blank is every date on the sheets. Otherwise each entry, between ; , or |,
// must read as exactly ONE date through the same parser, and must be on a
// sheet - a date nobody typed on a sheet is a mistake to say, not to skip.
var onlyWanted = new List<string>();
var onlyText = (onlyDates ?? "").Trim();
if (onlyText.Length > 0)
{
    foreach (var entry in onlyText.Split(new char[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries))
    {
        var piece = entry.Trim();
        if (piece.Length == 0) continue;
        var read = datesIn(piece);
        if (read.Count != 1)
            problems.Add("onlyDates '" + piece + "' is not one date with a written month - write each "
                       + "the way LIST_DATES_ON_SHEETS prints it, 07-APR-2025, with ; or , between them");
        else if (!onlyWanted.Contains(read[0])) onlyWanted.Add(read[0]);
    }
}

// ---- issueState: its own value, never implied ------------------------------
var issueWord = (issueState ?? "").Trim().ToLowerInvariant();
var issueIt = issueWord == "issue";
if (issueWord.Length > 0 && !issueIt)
    problems.Add("issueState takes 'issue' or nothing - a new revision is not issued unless "
               + "that is asked for in so many words - not '" + issueState + "'");

// ---- numbering: CREATE_REVISION's route, a sequence by name (2022 on) or a
// kind (2020, 2021), reached by name because the add-in compiles fragments
// with no release symbol defined ---------------------------------------------
var revisionClass = typeof(Revision);
var sequenceIdProperty = revisionClass.GetProperty("RevisionNumberingSequenceId");   // 2022 on
var numberTypeProperty = revisionClass.GetProperty("NumberType");                    // 2020 to 2022
var sequenceClass = revisionClass.Assembly.GetType(
    revisionClass.Namespace + ".RevisionNumberingSequence");                         // 2022 on

object numberingValue = null;       // an ElementId on 2022 on, a RevisionNumberType before
var numberingName = "";
var wantedNumbering = (numbering ?? "").Trim();
if (wantedNumbering.Length > 0)
{
    if (sequenceIdProperty != null && sequenceClass != null)
    {
        var sequenceNames = new List<string>();
        var getAll = sequenceClass.GetMethod("GetAllRevisionNumberingSequences", new[] { typeof(Document) });
        var sequenceIds = getAll.Invoke(null, new object[] { doc }) as IEnumerable<ElementId>;
        if (sequenceIds != null)
            foreach (var sequenceId in sequenceIds)
            {
                var candidate = doc.GetElement(sequenceId);
                if (candidate == null) continue;
                sequenceNames.Add(candidate.Name);
                if (string.Equals(candidate.Name, wantedNumbering, StringComparison.OrdinalIgnoreCase))
                {
                    numberingValue = candidate.Id;
                    numberingName = candidate.Name;
                }
            }
        if (numberingValue == null && string.Equals(wantedNumbering, "none", StringComparison.OrdinalIgnoreCase))
        {
            numberingValue = ElementId.InvalidElementId;
            numberingName = "None";
        }
        if (numberingValue == null)
            problems.Add("no numbering sequence called '" + wantedNumbering + "' in " + doc.Title
                       + " - it has " + (sequenceNames.Count == 0 ? "none" : string.Join(", ", sequenceNames.ToArray()))
                       + ". CREATE_REVISION_NUMBERING_SEQUENCE makes a new one");
    }
    else if (numberTypeProperty != null)
    {
        var kind = wantedNumbering.ToLowerInvariant();
        if (kind == "numeric") numberingValue = RevisionNumberType.Numeric;
        else if (kind == "alphanumeric") numberingValue = RevisionNumberType.Alphanumeric;
        else if (kind == "none") numberingValue = RevisionNumberType.None;
        else problems.Add("this Revit has no named numbering sequences - it numbers a revision "
                        + "'numeric', 'alphanumeric' or 'none', not '" + wantedNumbering + "'");
        if (numberingValue != null) numberingName = numberingValue.ToString();
    }
    else
    {
        problems.Add("this Revit exposes no way to choose a revision's numbering");
    }
}

// ---- THE SHEETS AND THEIR NOTES - LIST_DATES_ON_SHEETS' reading -------------
// Read whatever the values said, so a refusal still says what was looked at.
var sheetsById = new Dictionary<ElementId, ViewSheet>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)))
{
    var sheet = element as ViewSheet;
    if (sheet == null || sheet.IsTemplate) continue;
    sheetsById[sheet.Id] = sheet;
}
var sheetsRead = sheetsById.Count;

var sheetsOfDate = new Dictionary<string, List<ElementId>>();
var sheetsHoldingADate = new HashSet<ElementId>();
var notesOnSheets = 0;
var notesInViewsNotRead = 0;
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
        notesInViewsNotRead++;
        continue;
    }

    notesOnSheets++;

    string noteText;
    try { noteText = note.Text; }
    catch { notesUnreadable++; continue; }

    foreach (var found in datesIn(noteText))
    {
        List<ElementId> where;
        if (!sheetsOfDate.TryGetValue(found, out where))
        {
            where = new List<ElementId>();
            sheetsOfDate[found] = where;
        }
        if (!where.Contains(ownerId)) where.Add(ownerId);
        sheetsHoldingADate.Add(ownerId);
    }
}

// Sheet numbers compared as text, as LIST_SHEETS does: "M-101" and "A2.03" are
// not numbers.
Comparison<ElementId> bySheetNumber = (first, second) =>
    string.Compare(sheetsById[first].SheetNumber ?? "", sheetsById[second].SheetNumber ?? "",
                   StringComparison.OrdinalIgnoreCase);

// Oldest first - from the parser's own DD-MON-YYYY spelling, whose order as
// text means nothing.
Func<string, int> dayNumber = spelt =>
    int.Parse(spelt.Substring(7, 4)) * 10000
    + months[spelt.Substring(3, 3)] * 100
    + int.Parse(spelt.Substring(0, 2));

var allDates = new List<string>(sheetsOfDate.Keys);
allDates.Sort((first, second) => dayNumber(first).CompareTo(dayNumber(second)));
datesFound = allDates.Count;

var dates = new List<string>();
var leftOut = new List<string>();
if (onlyWanted.Count == 0) dates.AddRange(allDates);
else
{
    var onNoSheet = new List<string>();
    foreach (var wanted in onlyWanted) if (!sheetsOfDate.ContainsKey(wanted)) onNoSheet.Add(wanted);
    if (onNoSheet.Count > 0)
        problems.Add("onlyDates names " + string.Join(", ", onNoSheet.ToArray())
                   + ", which no text note typed on a sheet carries - the dates on the sheets are "
                   + (allDates.Count == 0 ? "none" : string.Join(", ", allDates.ToArray())));
    foreach (var onSheet in allDates)
    {
        if (onlyWanted.Contains(onSheet)) dates.Add(onSheet);
        else leftOut.Add(onSheet);
    }
}

var withNone = new List<ElementId>();
foreach (var sheetId in sheetsById.Keys)
    if (!sheetsHoldingADate.Contains(sheetId)) withNone.Add(sheetId);
withNone.Sort(bySheetNumber);
var noneLabels = new List<string>();
foreach (var sheetId in withNone)
{
    var sheet = sheetsById[sheetId];
    noneLabels.Add(sheet.IsPlaceholder ? sheet.SheetNumber + " (placeholder)" : sheet.SheetNumber);
}
sheetsWithNoDate = string.Join(", ", noneLabels.ToArray());

var notRead =
    " Not read: " + notesInViewsNotRead + " text note(s) typed inside views - a note in a plan "
    + "placed on a sheet belongs to that plan; title block labels and parameters such as Sheet "
    + "Issue Date, which are not text notes; and a date with a NUMERIC month such as 09-09-2026, "
    + "refused because 03-04-2026 is 3 April in one country and 4 March in another."
    + (notesUnreadable > 0 ? " " + notesUnreadable + " note(s) on sheets could not be read and were skipped." : "");

var nothingChanged = " Nothing was created and no sheet was changed.";

if (problems.Count > 0)
{
    refused = string.Join("; ", problems.ToArray()) + "." + nothingChanged;
}
else if (dates.Count == 0)
{
    refused = (sheetsRead == 0 ? "This model has no sheets, so there is no date on a sheet to make a revision from."
             : notesOnSheets == 0 ? sheetsRead + " sheet(s) read, and no text note is typed on any of them, so there is no date to make a revision from."
             : sheetsRead + " sheet(s) read and " + notesOnSheets + " text note(s) typed on them, and none carries a date with a written month, so there is no date to make a revision from.")
            + nothingChanged;
}

// ---- WHICH REVISION EACH DATE GETS: an existing one, or a new one ----------
var written = new Dictionary<string, string>();
var revisionOf = new Dictionary<string, Revision>();
var revisionIdsBefore = new List<ElementId>();
var nameBefore = new Dictionary<ElementId, string>();

if (refused.Length == 0)
{
    foreach (var date in dates)
        written[date] = spell(int.Parse(date.Substring(0, 2)), months[date.Substring(3, 3)],
                              int.Parse(date.Substring(7, 4)), null);

    revisionIdsBefore.AddRange(Revision.GetAllRevisionIds(doc));
    foreach (var revisionId in revisionIdsBefore)
    {
        var existing = doc.GetElement(revisionId) as Revision;
        if (existing != null) nameBefore[revisionId] = nameOf(existing);
    }

    var twice = new List<string>();
    foreach (var date in dates)
    {
        var same = new List<Revision>();
        foreach (var revisionId in revisionIdsBefore)
        {
            var existing = doc.GetElement(revisionId) as Revision;
            if (existing == null) continue;
            if (!string.Equals((existing.Description ?? "").Trim(), descriptionText, StringComparison.OrdinalIgnoreCase))
                continue;
            var held = (existing.RevisionDate ?? "").Trim();
            var heldDates = datesIn(held);
            if (string.Equals(held, written[date], StringComparison.OrdinalIgnoreCase)
                || (heldDates.Count == 1 && heldDates[0] == date))
                same.Add(existing);
        }
        if (same.Count == 1) revisionOf[date] = same[0];
        else if (same.Count > 1)
        {
            var named = new List<string>();
            foreach (var existing in same) named.Add(nameOf(existing) + " dated " + quoted(existing.RevisionDate));
            twice.Add(date + " is already " + string.Join(" and ", named.ToArray()));
        }
    }
    if (twice.Count > 0)
        refused = "more than one revision already stands for a date: " + string.Join("; ", twice.ToArray())
                + ". Which one the sheets should carry is the drawing office's choice, not a guess - "
                + "MERGE_REVISION or DELETE_REVISION leaves one." + nothingChanged;
}

// ---- MAKE, ATTACH, ISSUE - only now does anything change -------------------
var createdDates = new List<string>();
var addedTo = new Dictionary<string, List<string>>();
var alreadyOn = new Dictionary<string, List<string>>();

if (refused.Length == 0)
{
    Action<List<string>, string, string, Func<string>, Action<string>> write = (missed, label, wanted, read, put) =>
    {
        if (wanted == null) return;
        try { put(wanted); }
        catch (Exception failure) { missed.Add(label + " (" + revitSaid(failure) + ")"); return; }
        var now = "";
        try { now = read() ?? ""; } catch (Exception failure) { now = "unreadable: " + revitSaid(failure); }
        if (now != wanted) missed.Add(label + " reads back " + quoted(now) + ", not " + quoted(wanted));
    };

    // OLDEST DATE FIRST, so the sequence numbers Revit hands out run in date order.
    foreach (var date in dates)
    {
        addedTo[date] = new List<string>();
        alreadyOn[date] = new List<string>();
        if (revisionOf.ContainsKey(date)) continue;

        Revision made;
        try { made = Revision.Create(doc); }
        catch (Exception failure)
        {
            throw new InvalidOperationException("Revit would not create the revision for " + date + ": "
                + revitSaid(failure) + ". Nothing is kept - no revision made and no sheet changed.");
        }

        var missed = new List<string>();
        write(missed, "description", descriptionText, () => made.Description, v => made.Description = v);
        write(missed, "date", written[date], () => made.RevisionDate, v => made.RevisionDate = v);
        write(missed, "issued by", asked(issuedBy), () => made.IssuedBy, v => made.IssuedBy = v);
        write(missed, "issued to", asked(issuedTo), () => made.IssuedTo, v => made.IssuedTo = v);

        if (numberingValue != null)
        {
            try
            {
                if (sequenceIdProperty != null)
                {
                    sequenceIdProperty.SetValue(made, numberingValue, null);
                    var nowSequence = sequenceIdProperty.GetValue(made, null) as ElementId;
                    if (nowSequence == null || !nowSequence.Equals(numberingValue))
                        missed.Add("numbering reads back a different sequence");
                }
                else
                {
                    numberTypeProperty.SetValue(made, numberingValue, null);
                    var nowType = numberTypeProperty.GetValue(made, null);
                    if (nowType == null || !nowType.Equals(numberingValue))
                        missed.Add("numbering reads back " + nowType);
                }
            }
            catch (Exception failure) { missed.Add("numbering " + numberingName + " (" + revitSaid(failure) + ")"); }
        }

        if (missed.Count > 0)
            throw new InvalidOperationException("The revision for " + date + " would not take every value, "
                + "so nothing is kept: " + string.Join("; ", missed.ToArray()) + ".");

        revisionOf[date] = made;
        createdDates.Add(date);
    }
    revisionsCreated = createdDates.Count;
    revisionsReused = dates.Count - createdDates.Count;

    // EACH SHEET ONCE, in sheet-number order, with every date it carries.
    var sheetsToWrite = new List<ElementId>();
    foreach (var date in dates)
        foreach (var sheetId in sheetsOfDate[date])
            if (!sheetsToWrite.Contains(sheetId)) sheetsToWrite.Add(sheetId);
    sheetsToWrite.Sort(bySheetNumber);

    foreach (var sheetId in sheetsToWrite)
    {
        var sheet = sheetsById[sheetId];
        // CARRIED is everything in the sheet's revision schedule - clouded and
        // ticked; LISTED is only the ticked ones, the list this writes.
        var carried = new List<ElementId>(sheet.GetAllRevisionIds());
        var next = new List<ElementId>(sheet.GetAdditionalRevisionIds());
        var addedHere = new List<string>();

        foreach (var date in dates)
        {
            if (!sheetsOfDate[date].Contains(sheetId)) continue;
            var revisionId = revisionOf[date].Id;
            if (carried.Contains(revisionId) || next.Contains(revisionId))
            {
                alreadyOn[date].Add(sheet.SheetNumber);
                continue;
            }
            next.Add(revisionId);
            addedHere.Add(date);
        }
        if (addedHere.Count == 0) continue;

        try { sheet.SetAdditionalRevisionIds(next); }
        catch (Exception failure)
        {
            throw new InvalidOperationException("Sheet " + sheet.SheetNumber + " would not take the revision(s) for "
                + string.Join(", ", addedHere.ToArray()) + ": " + revitSaid(failure)
                + ". Nothing is kept - no revision made and no sheet changed.");
        }

        // Read back: what the sheet lists now, not what was sent.
        var after = new List<ElementId>(sheet.GetAdditionalRevisionIds());
        var lost = new List<string>();
        foreach (var revisionId in next)
            if (!after.Contains(revisionId))
            {
                var gone = doc.GetElement(revisionId) as Revision;
                lost.Add(gone == null ? "a revision that no longer exists" : nameOf(gone));
            }
        if (lost.Count > 0)
            throw new InvalidOperationException("Sheet " + sheet.SheetNumber + " does not list "
                + string.Join(", ", lost.ToArray()) + " after it was written, so nothing is kept.");

        sheetsChanged++;
        foreach (var date in addedHere)
        {
            addedTo[date].Add(sheet.SheetNumber);
            revisionsAddedToSheets++;
        }
    }

    // LAST, and only when asked: issuing locks the columns written above, and
    // only the revisions this call made - a reused one is left as it was.
    if (issueIt)
    {
        foreach (var date in createdDates)
        {
            var made = revisionOf[date];
            try { made.Issued = true; }
            catch (Exception failure)
            {
                throw new InvalidOperationException("Revit would not issue " + nameOf(made) + ": "
                    + revitSaid(failure) + ". Nothing is kept - no revision made and no sheet changed.");
            }
            if (!made.Issued)
                throw new InvalidOperationException(nameOf(made) + " reads back as not issued, so nothing is kept.");
        }
    }

    // DID A REVISION VANISH? Regenerated first, so a purge Revit makes when the
    // model updates has happened before the count - and then every revision
    // the project had, and every one made here, must still be there.
    doc.Regenerate();
    var vanished = new List<string>();
    foreach (var revisionId in revisionIdsBefore)
        if (!(doc.GetElement(revisionId) is Revision))
            vanished.Add(nameBefore.ContainsKey(revisionId) ? nameBefore[revisionId] : "a revision");
    foreach (var date in createdDates)
        if (!(doc.GetElement(revisionOf[date].Id) is Revision))
            vanished.Add("the new revision for " + date);
    if (vanished.Count > 0)
        throw new InvalidOperationException("Writing the sheets' revisions made Revit remove "
            + string.Join(", ", vanished.ToArray()) + " from the project, so nothing is kept - "
            + "no revision made and no sheet changed.");

    revisionsRemaining = Revision.GetAllRevisionIds(doc).Count;

    // THE REPORT, read from Revit after the last write - the sequence numbers
    // are final only now.
    var rows = new List<string>();
    foreach (var date in dates)
    {
        var revision = revisionOf[date];
        var made = createdDates.Contains(date);
        var row = date + " -> " + nameOf(revision) + ", dated " + quoted(revision.RevisionDate)
                + (made ? ", CREATED" + (revision.Issued ? " and issued" : "")
                        : ", REUSED as it was" + (revision.Issued ? " (issued)" : ""))
                + ": added to " + addedTo[date].Count + " sheet(s)"
                + (addedTo[date].Count > 0 ? " (" + string.Join(", ", addedTo[date].ToArray()) + ")" : "");
        if (alreadyOn[date].Count > 0)
            row += "; already on " + alreadyOn[date].Count + " (" + string.Join(", ", alreadyOn[date].ToArray()) + ")";
        rows.Add(row);
    }
    dateRevisions = string.Join("  ||  ", rows.ToArray());

    var outOfOrder = new List<string>();
    for (var k = 1; k < dates.Count; k++)
        if (revisionOf[dates[k]].SequenceNumber < revisionOf[dates[k - 1]].SequenceNumber)
            outOfOrder.Add(dates[k - 1] + " is " + nameOf(revisionOf[dates[k - 1]]) + " but " + dates[k]
                         + " is " + nameOf(revisionOf[dates[k]]));

    summary = (revisionsCreated == 0 && revisionsAddedToSheets == 0
                  ? "Nothing needed doing: every one of the " + dates.Count + " date(s) already has its revision, and every sheet carrying it already lists it."
                  : revisionsCreated + " revision(s) created and " + revisionsReused + " reused for "
                    + dates.Count + " date(s) typed on " + sheetsHoldingADate.Count + " of " + sheetsRead + " sheet(s); "
                    + revisionsAddedToSheets + " revision(s) added across " + sheetsChanged + " sheet(s), each added "
                    + "to what the sheet already listed - nothing was taken off a sheet.")
            + " Each date's revision and its sheets are in dateRevisions."
            + (revisionsReused > 0 ? " A reused revision is left exactly as it was - EDIT_REVISION changes its columns." : "")
            + (revisionsCreated > 0
                  ? (issueIt ? " The new revisions were issued last, after every sheet took them."
                             : " Nothing was issued - EDIT_REVISION issues them when the set goes out.")
                  : "")
            + (leftOut.Count > 0 ? " Left out because onlyDates did not name them: " + string.Join(", ", leftOut.ToArray()) + "." : "")
            + (outOfOrder.Count > 0 ? " The sequence does not follow the dates where a reused revision sits: "
                  + string.Join("; ", outOfOrder.ToArray()) + " - REORDER_REVISION moves one." : "")
            + " The project had " + revisionIdsBefore.Count + " revision(s) and has " + revisionsRemaining + " now."
            + notRead;
}
else
{
    summary = refused + " " + datesFound + " distinct date(s) on " + sheetsHoldingADate.Count + " of "
            + sheetsRead + " sheet(s), from " + notesOnSheets + " text note(s) typed on sheets." + notRead;
}
