// NOT STANDALONE. Assumes `doc` and `nameContains` are in scope, and leaves
// `elements`, `skippedTemplates`, `skippedInternal` and `matchedButExcluded`
// behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// WHY THIS EXISTS SEPARATELY FROM FIND_VIEWS.
//
// A schedule IS a View, and FIND_VIEWS excludes it on purpose - returning one
// among the drawings would let an action that duplicates or applies a template
// reach a schedule. That was the right call and it left nothing able to hand a
// schedule to anything. This is the answer, rather than loosening the other
// fragment and reintroducing the trap it was written to prevent.
//
// TWO KINDS COME BACK FROM A PLAIN COLLECTOR AND ARE NOT WHAT ANYBODY MEANS.
//
//   IsTemplate      a schedule template - the same trap view templates are
//   angle-bracketed Revit's own bookkeeping schedules, a revision schedule
//                   being the one seen most often
//
// The second is recognised by NAME, and that is a fact about the API rather
// than a shortcut: Revit exposes no property separating its internal schedules
// from real ones. Because the test is by name, the count is reported - so a
// user schedule wrongly excluded for being named with a bracket is VISIBLE
// rather than silently missing.

var wanted = (nameContains ?? "").Trim();

int skippedTemplates = 0;
int skippedInternal = 0;
int matchedButExcluded = 0;

var found = new List<Element>();

foreach (var schedule in new FilteredElementCollector(doc)
                             .OfClass(typeof(ViewSchedule))
                             .Cast<ViewSchedule>())
{
    // Name match first, so an excluded schedule can still be recognised as
    // having matched what was asked for. After the exclusions it would be
    // impossible to tell "no such schedule" from "found it and refused it".
    bool nameMatches = wanted.Length == 0
        || schedule.Name.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0;

    if (schedule.IsTemplate)
    {
        skippedTemplates++;
        if (nameMatches) matchedButExcluded++;
        continue;
    }

    if (schedule.Name.StartsWith("<"))
    {
        skippedInternal++;
        if (nameMatches) matchedButExcluded++;
        continue;
    }

    if (!nameMatches) continue;
    found.Add(schedule);
}

var elements = found.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                    .Cast<Element>().ToList();
