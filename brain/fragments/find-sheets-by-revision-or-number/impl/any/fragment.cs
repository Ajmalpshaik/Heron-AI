// NOT STANDALONE. Assumes `doc`, `revisionSequence`, `revisionDescription`,
// `revisionDate` and `sheetNumbers` are in scope, and leaves `elements`,
// `placeholders`, `revisionsMatched`, `notFound`, `listedWithoutRevision`,
// `notRead` and `refused` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHY EVERY REVISION ON THE SHEET AND NOT "CURRENT REVISION".
//
// A sheet's Current Revision parameters hold the LATEST revision on it and
// nothing else. On 2026-10-14 a sheet set of the 8 sheets carrying Seq. 15 was
// made by matching Current Revision Description - which was right that day
// only because Seq. 15 happened to be the latest on all 8. Add Seq. 16 to one
// of them and that sheet silently leaves the set. GetAllRevisionIds() is
// Revit's own list of every revision the sheet carries, ticked by hand or
// clouded in a view on it, so the match does not depend on what came after.
//
// WHY A DESCRIPTION THAT FITS TWO REVISIONS IS REFUSED.
//
// A description is a name, and a part of a name fits more than one: "as-built"
// is in "IAB-ISSUED AS-BUILT" and in "DRAFT AS-BUILT". Picking the sheets of
// both makes an issue set nobody asked for, and the count looks plausible.
// The refusal names each with its sequence; the next ask gives the one. A
// date is different - two revisions issued on one day are both "that day's
// issue" - so a date may choose several, and each is named.
//
// WHY SHEET NUMBERS MATCH WHOLE.
//
// "17" as a part would pick 17, 170 and 179. A list typed by a person is a
// list of whole numbers, so each is compared whole, ignoring case and outer
// spaces, and one that names no sheet is handed back rather than forgotten.

var elements = new List<Element>();
var placeholders = new List<ElementId>();
string revisionsMatched = "";
string notFound = "";
string listedWithoutRevision = "";
string notRead = "";
string refused = "";

// Runs of spaces become one, and the ends go, so "IAB-ISSUED  AS-BUILT"
// typed with two spaces is the description it plainly means.
Func<string, string> squash = s =>
    string.Join(" ", (s ?? "").Split(new[] { ' ', '\t', '\r', '\n' },
                                     StringSplitOptions.RemoveEmptyEntries));
Func<string, string, bool> sameWord = (a, b) =>
    string.Equals(squash(a), squash(b), StringComparison.OrdinalIgnoreCase);
// The card's `absent:` text, or nothing typed, means the input is not used.
Func<string, string, bool> given = (text, absentWord) =>
    squash(text).Length > 0 && !sameWord(text, absentWord);

bool bySequence = given(revisionSequence, "any");
bool byDescription = given(revisionDescription, "any");
bool byDate = given(revisionDate, "any");
bool byRevision = bySequence || byDescription || byDate;
bool byList = given(sheetNumbers, "all");

// THE SEQUENCE, as one whole number. "Seq. 15" and "15" are the same ask;
// "15 16" or "fifteen" are not one number, and are refused rather than read
// as the first digits found.
int sequenceWanted = -1;
if (bySequence)
{
    var runs = new List<string>();
    var run = "";
    foreach (char c in revisionSequence)
    {
        if (c >= '0' && c <= '9') { run += c; continue; }
        if (run.Length > 0) { runs.Add(run); run = ""; }
    }
    if (run.Length > 0) runs.Add(run);
    int parsed;
    if (runs.Count == 1 && int.TryParse(runs[0], out parsed)) sequenceWanted = parsed;
    else refused = "'" + squash(revisionSequence) + "' is not one sequence number. Give the "
                 + "number in the Seq. column of Sheet Issues/Revisions - for example 15.";
}

if (refused.Length == 0 && !byRevision && !byList)
    refused = "Nothing to pick the sheets by. Give a revision - its sequence, description "
            + "or date - or a list of sheet numbers. Every sheet is FIND_SHEETS' answer.";

// Every revision in the project, in sequence order, as Revit keeps them.
var allRevisions = new List<Revision>();
foreach (var id in Revision.GetAllRevisionIds(doc))
{
    var r = doc.GetElement(id) as Revision;
    if (r != null) allRevisions.Add(r);
}

// Per Sheet numbering has no one number for a revision, and asking for it
// throws (LIST_REVISIONS meets the same) - so it is said, not guessed.
Func<Revision, string> describe = r =>
{
    string number;
    try { number = r.RevisionNumber ?? ""; }
    catch (Exception) { number = "per sheet"; }
    return "Seq " + r.SequenceNumber + ", number " + number
         + ", date " + (r.RevisionDate ?? "") + ", " + (r.Description ?? "");
};
Func<string> everyRevision = () =>
{
    if (allRevisions.Count == 0) return "This project has no revisions.";
    var shown = allRevisions.Take(60).Select(describe).ToList();
    var text = "The project's revisions: " + string.Join(" || ", shown);
    if (allRevisions.Count > shown.Count)
        text += " || and " + (allRevisions.Count - shown.Count) + " more";
    return text;
};

// CHOOSE THE REVISIONS. Sequence and date narrow first; the description then
// matches WHOLE inside what is left, and only when none matches whole does it
// match a PART. Narrowing first is what lets "as-built" with Seq. 15 find
// "IAB-ISSUED AS-BUILT" even when another revision is called just "AS-BUILT".
var chosen = new List<Revision>();
if (refused.Length == 0 && byRevision)
{
    var left = allRevisions
        .Where(r => !bySequence || r.SequenceNumber == sequenceWanted)
        .Where(r => !byDate || sameWord(r.RevisionDate, revisionDate))
        .ToList();
    if (byDescription)
    {
        var whole = left.Where(r => sameWord(r.Description, revisionDescription)).ToList();
        var wanted = squash(revisionDescription);
        left = whole.Count > 0
            ? whole
            : left.Where(r => squash(r.Description)
                                  .IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0)
                  .ToList();
    }
    chosen = left;

    var asked = new List<string>();
    if (bySequence) asked.Add("sequence " + sequenceWanted);
    if (byDescription) asked.Add("description '" + squash(revisionDescription) + "'");
    if (byDate) asked.Add("date '" + squash(revisionDate) + "'");

    if (chosen.Count == 0)
        refused = "No revision has " + string.Join(" and ", asked) + ". " + everyRevision();
    else if (byDescription && chosen.Count > 1)
        refused = chosen.Count + " revisions fit " + string.Join(" and ", asked)
                + ", and a set made from the sheets of all of them is not one issue: "
                + string.Join(" || ", chosen.Select(describe))
                + ". Give the sequence of the one meant.";
}

// THE TYPED LIST, whole numbers, each once, in the order typed.
var listed = new List<string>();
if (byList)
{
    foreach (var part in sheetNumbers.Split(new[] { ',', ';', '\r', '\n', '\t' },
                                            StringSplitOptions.RemoveEmptyEntries))
    {
        var number = squash(part);
        if (number.Length > 0 && !listed.Any(n => sameWord(n, number))) listed.Add(number);
    }
    if (listed.Count == 0 && refused.Length == 0)
        refused = "'" + squash(sheetNumbers) + "' holds no sheet number. Give them with "
                + "commas between - for example 179, 180, 181.";
}

if (refused.Length == 0)
{
    var chosenIds = new HashSet<ElementId>(chosen.Select(r => r.Id));
    var carriedBy = chosen.ToDictionary(r => r.Id, r => 0);
    var seen = new List<string>();
    var without = new List<string>();
    var unread = new List<string>();

    foreach (var sheet in new FilteredElementCollector(doc)
                              .OfClass(typeof(ViewSheet))
                              .Cast<ViewSheet>()
                              .OrderBy(s => s.SheetNumber, StringComparer.OrdinalIgnoreCase))
    {
        bool isListed = !byList || listed.Any(n => sameWord(n, sheet.SheetNumber));
        if (byList && isListed) seen.Add(sheet.SheetNumber);
        if (!isListed) continue;

        if (byRevision)
        {
            IList<ElementId> onSheet;
            try { onSheet = sheet.GetAllRevisionIds(); }
            catch (Exception ex)
            {
                unread.Add(sheet.SheetNumber + " (" + ex.Message + ")");
                continue;
            }
            var hits = onSheet.Where(id => chosenIds.Contains(id)).ToList();
            foreach (var id in hits) carriedBy[id] = carriedBy[id] + 1;
            if (hits.Count == 0)
            {
                if (byList) without.Add(sheet.SheetNumber);
                continue;
            }
        }

        elements.Add(sheet);
        if (sheet.IsPlaceholder) placeholders.Add(sheet.Id);
    }

    revisionsMatched = string.Join(" || ", chosen.Select(r =>
        describe(r) + " - on " + carriedBy[r.Id] + " sheet(s)"
        + (byList ? " of those listed" : "")));
    notFound = string.Join(", ", listed.Where(n => !seen.Any(s => sameWord(s, n))));
    listedWithoutRevision = string.Join(", ", without);
    notRead = string.Join(", ", unread);
}

// Ordered by sheet number as a plain string sort, as FIND_SHEETS orders them,
// so "the first sheet" means the same sheet to both cards.
