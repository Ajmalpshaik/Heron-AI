// NOT STANDALONE. Assumes `doc`, `numberOrNameContains`, `revisionSequence`,
// `revisionDescription`, `revisionDate` and `sheetNumbers` are in scope, and
// leaves `elements`, `placeholders`, `matchedOnName`, `findings`,
// `revisionsMatched`, `notFound`, `listedWithoutRevision`, `notRead` and
// `refused` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHY THE MATCH READS TWO FIELDS.
//
// A sheet has a NUMBER and a NAME, and people ask by whichever one they think
// in. "The M-series" is a number. "The ground floor sheets" is a name. Reading
// only the number answers half the requests and returns a confident zero for
// the rest, which is indistinguishable from there being no such sheets.
//
// `matchedOnName` counts the ones the NUMBER would have missed. It is not
// decoration: it is how a caller can tell "you found these by title" from "you
// found these by number", and the two mean different things when the answer
// looks wrong.
//
// WHY PLACEHOLDERS ARE HANDED BACK RATHER THAN REMOVED.
//
// A placeholder sheet is a real ViewSheet - a number and a name reserved in the
// drawing register - with no titleblock, that cannot hold a viewport. The jobs
// that act on sheets split cleanly on it:
//
//   renumbering, revisions   MUST include them. They are sheets in the register
//                            and skipping them renumbers part of a set
//   viewports, PDF, DWG      cannot use them at all
//
// Of the two ways to be wrong, silently removing them is much the worse: a
// renumber then reports success over an incomplete set and nobody finds out
// until the drawing list is printed. Failing visibly on an export is
// recoverable in a way that is not.
//
// So they are returned in `elements` AND their ids are handed over separately.
// Ids rather than a count, for the same reason MOVE_ELEMENTS reports blocked
// ids: "three of them are placeholders" is not something a person can act on,
// and a list of ids is what they select in Revit to go and look.
//
// VERSION 2 (2026-11-01) - LIST_SHEETS MERGED IN, AND ITS READING WITH IT.
//
// A SHEET VIEW TEMPLATE IS NOT A SHEET. Revit lets a project hold view
// templates for sheets, and they come back from the same collector looking
// exactly like sheets - a number, a name, no drawings. Version 1 returned them,
// so a renumber or an export was handed a row that can never be issued. They
// are dropped on IsTemplate - a placeholder is still kept, because it IS a row
// of the register.
//
// ONE `findings` ROW PER SHEET - "number  name  (n views)" - the drawing list
// as it is printed, with EMPTY on a sheet that holds no view and PLACEHOLDER
// on one that cannot hold any. The count is GetAllPlacedViews: a title block,
// a legend component and a schedule are not views and are not counted, which
// is right for "how many drawings are on this sheet". An empty sheet is
// usually one made for a drawing nobody has produced yet - exactly what
// somebody scans for before an issue.

// VERSION 3 (2026-11-01) - FIND_SHEETS_BY_REVISION_OR_NUMBER MERGED IN, AND
// WHAT IT HAD THAT THIS LACKED. Each new input narrows the sheets FURTHER;
// left out, it does not narrow, and the card reads as version 2 did.
//
// A REVISION, ANY ONE ON THE SHEET - NOT ONLY THE LATEST. A sheet's Current
// Revision parameters hold the LATEST revision and nothing else. On
// 2026-10-14 a sheet set of the 8 sheets carrying Seq. 15 was made by
// matching Current Revision Description - right that day only because Seq. 15
// was the latest on all 8. Add Seq. 16 to one of them and that sheet silently
// leaves the set. GetAllRevisionIds() is Revit's own list of every revision
// the sheet carries, ticked by hand or clouded in a view on it. The revision
// is chosen by its sequence (the Seq. column of Sheet Issues/Revisions), its
// description or its date, and must fit every one given.
//
// A DESCRIPTION THAT FITS TWO REVISIONS IS REFUSED. "as-built" is in "IAB-
// ISSUED AS-BUILT" and in "DRAFT AS-BUILT", and the sheets of both are an
// issue set nobody asked for, with a count that looks plausible. Whole first,
// ignoring case, and only then a part; the refusal names each with its
// sequence. A date may choose several - two issues on one day are both that
// day's issue - and each is named in `revisionsMatched`.
//
// A TYPED LIST OF SHEET NUMBERS MATCHES WHOLE - a separate input from the
// part-match above, on purpose: `numberOrNameContains` "17" finds 17, 170 and
// 179, and a list a person typed is a list of whole numbers. A number that
// names no sheet is handed back in `notFound`, a listed sheet without the
// revision in `listedWithoutRevision`, a sheet whose revisions Revit would
// not hand over in `notRead` - counted, never dropped in silence.
//
// NOT CARRIED: that card refused when NOTHING was given to pick by. Here
// nothing given is every sheet - this card's own job ("list the sheets"),
// proved in version 1 - and the danger the refusal guarded is still closed:
// a revision or a list GIVEN that fits nothing is refused or comes back
// empty, never widened to every sheet.

// Runs of spaces become one, and the ends go, so "IAB-ISSUED  AS-BUILT"
// typed with two spaces is the description it plainly means.
Func<string, string> squash = s =>
    string.Join(" ", (s ?? "").Split(new[] { ' ', '\t', '\r', '\n' },
                                     StringSplitOptions.RemoveEmptyEntries));
Func<string, string, bool> sameWord = (a, b) =>
    string.Equals(squash(a), squash(b), StringComparison.OrdinalIgnoreCase);
// The card's `absent:` word, or nothing typed, means the input is not used.
Func<string, string, bool> given = (text, absentWord) =>
    squash(text).Length > 0 && !sameWord(text, absentWord);

// VERSION 3: `numberOrNameContains` may be left out - bound as "all", which
// narrows nothing. Typed as exactly "all" it means every sheet, as it reads;
// any other text, "wall" included, is the part-match it always was.
var wanted = given(numberOrNameContains, "all") ? (numberOrNameContains ?? "").Trim() : "";

var elements = new List<Element>();
var placeholders = new List<ElementId>();
int matchedOnName = 0;
var findings = new List<string>();
string revisionsMatched = "";
string notFound = "";
string listedWithoutRevision = "";
string notRead = "";
string refused = "";

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

// Every revision in the project, in sequence order, as Revit keeps them -
// read only when a revision was asked for.
var allRevisions = new List<Revision>();
if (byRevision)
{
    foreach (var id in Revision.GetAllRevisionIds(doc))
    {
        var r = doc.GetElement(id) as Revision;
        if (r != null) allRevisions.Add(r);
    }
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
        var part = squash(revisionDescription);
        left = whole.Count > 0
            ? whole
            : left.Where(r => squash(r.Description)
                                  .IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0)
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
if (refused.Length == 0 && byList)
{
    foreach (var piece in sheetNumbers.Split(new[] { ',', ';', '\r', '\n', '\t' },
                                             StringSplitOptions.RemoveEmptyEntries))
    {
        var number = squash(piece);
        if (number.Length > 0 && !listed.Any(n => sameWord(n, number))) listed.Add(number);
    }
    if (listed.Count == 0)
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
                              .Where(s => s != null && !s.IsTemplate)
                              .OrderBy(s => s.SheetNumber ?? "", StringComparer.OrdinalIgnoreCase))
    {
        var number = sheet.SheetNumber ?? "";
        var title = sheet.Name ?? "";

        // The typed list first, WHOLE. A listed number is "seen" whatever the
        // other inputs say, so `notFound` names only numbers no sheet has.
        bool isListed = !byList || listed.Any(n => sameWord(n, number));
        if (byList && isListed) seen.Add(number);
        if (!isListed) continue;

        bool nameOnly = false;
        if (wanted.Length > 0)
        {
            bool byNumber = number.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0;
            bool byName = title.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0;
            if (!byNumber && !byName) continue;
            // Counted only when the number would NOT have found it, so this
            // reads as "how many the number-only route would have missed".
            nameOnly = !byNumber;
        }

        if (byRevision)
        {
            IList<ElementId> onSheet;
            try { onSheet = sheet.GetAllRevisionIds(); }
            catch (Exception ex)
            {
                unread.Add(number + " (" + ex.Message + ")");
                continue;
            }
            var hits = onSheet.Where(id => chosenIds.Contains(id)).ToList();
            foreach (var id in hits) carriedBy[id] = carriedBy[id] + 1;
            if (hits.Count == 0)
            {
                if (byList) without.Add(number);
                continue;
            }
        }

        if (nameOnly) matchedOnName++;
        elements.Add(sheet);

        var placedViews = sheet.GetAllPlacedViews();
        var placedCount = placedViews == null ? 0 : placedViews.Count;

        var note = placedCount == 0 ? " - EMPTY" : "";
        if (sheet.IsPlaceholder)
        {
            placeholders.Add(sheet.Id);
            note = " - PLACEHOLDER, no titleblock and cannot be printed";
        }

        findings.Add(string.Format("{0}  {1}  ({2} view{3}){4}",
                                   number,
                                   title,
                                   placedCount,
                                   placedCount == 1 ? "" : "s",
                                   note));
    }

    revisionsMatched = string.Join(" || ", chosen.Select(r =>
        describe(r) + " - on " + carriedBy[r.Id] + " sheet(s)"
        + (byList || wanted.Length > 0 ? " of those asked for" : "")));
    notFound = string.Join(", ", listed.Where(n => !seen.Any(s => sameWord(s, n))));
    listedWithoutRevision = string.Join(", ", without);
    notRead = string.Join(", ", unread);
}

// Ordered by sheet number, as a plain string sort. "A-10" therefore sorts
// before "A-9". That is left alone deliberately: a natural-order sort is a
// presentation decision, and a filter that invented one would disagree with
// every other fragment about what "the first sheet" means.
