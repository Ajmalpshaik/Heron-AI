// NOT STANDALONE. Assumes `doc` and `numberOrNameContains` are in scope, and
// leaves `elements`, `placeholders`, `matchedOnName` and `findings` behind.
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

var wanted = (numberOrNameContains ?? "").Trim();

var elements = new List<Element>();
var placeholders = new List<ElementId>();
int matchedOnName = 0;
var findings = new List<string>();

foreach (var sheet in new FilteredElementCollector(doc)
                          .OfClass(typeof(ViewSheet))
                          .Cast<ViewSheet>()
                          .Where(s => s != null && !s.IsTemplate)
                          .OrderBy(s => s.SheetNumber ?? "", StringComparer.OrdinalIgnoreCase))
{
    var number = sheet.SheetNumber ?? "";
    var title = sheet.Name ?? "";

    if (wanted.Length > 0)
    {
        bool byNumber = number.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0;
        bool byName = title.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0;
        if (!byNumber && !byName) continue;
        // Counted only when the number would NOT have found it, so this reads
        // as "how many the number-only route would have missed".
        if (!byNumber) matchedOnName++;
    }

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

// Ordered by sheet number, as a plain string sort. "A-10" therefore sorts
// before "A-9". That is left alone deliberately: a natural-order sort is a
// presentation decision, and a filter that invented one would disagree with
// every other fragment about what "the first sheet" means.
