// NOT STANDALONE. Assumes `doc` and `numberOrNameContains` are in scope, and
// leaves `elements`, `placeholders` and `matchedOnName` behind.
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

var wanted = (numberOrNameContains ?? "").Trim();

var elements = new List<Element>();
var placeholders = new List<ElementId>();
int matchedOnName = 0;

foreach (var sheet in new FilteredElementCollector(doc)
                          .OfClass(typeof(ViewSheet))
                          .Cast<ViewSheet>()
                          .OrderBy(s => s.SheetNumber, StringComparer.OrdinalIgnoreCase))
{
    if (wanted.Length > 0)
    {
        bool byNumber = sheet.SheetNumber.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0;
        bool byName = sheet.Name.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0;
        if (!byNumber && !byName) continue;
        // Counted only when the number would NOT have found it, so this reads
        // as "how many the number-only route would have missed".
        if (!byNumber) matchedOnName++;
    }

    elements.Add(sheet);
    if (sheet.IsPlaceholder) placeholders.Add(sheet.Id);
}

// Ordered by sheet number, as a plain string sort. "A-10" therefore sorts
// before "A-9". That is left alone deliberately: a natural-order sort is a
// presentation decision, and a filter that invented one would disagree with
// every other fragment about what "the first sheet" means.
