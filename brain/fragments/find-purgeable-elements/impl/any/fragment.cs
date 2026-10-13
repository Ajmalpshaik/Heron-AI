// NOT STANDALONE. Assumes `doc` and `categories` are in scope; leaves
// `elements`, `purgeableNames`, `byCategory` and `findings` behind.
//
// READ ONLY - opens no transaction. Nothing is deleted here.
//
// WHAT PURGE UNUSED WOULD OFFER - in a project, or in a family in front -
// found by Revit's own list of unused elements, so nothing is decided here
// about what counts as used:
//
//   categories   category names, comma separated, to look in; EMPTY looks
//                in every category, as Purge Unused does
//
// The elements are handed on: DELETE_ELEMENTS removes them, after the
// modeller has seen the list. Purge is a ladder - removing a type can leave
// its material unused - so a second run after the delete finds the next rung.
//
// REVIT 2024 opened the unused-element list to code; the card declares 2024 on.

var elements = new List<Element>();
var purgeableNames = new List<string>();
var findings = new List<string>();
string refusal = null;

var wanted = new HashSet<string>((categories ?? "").Split(',').Select(t => t.Trim().ToLowerInvariant()).Where(t => t != ""));
var scope = new HashSet<ElementId>();
if (wanted.Count > 0)
{
    foreach (Category category in doc.Settings.Categories)
        if (wanted.Contains(category.Name.ToLowerInvariant())) scope.Add(category.Id);
    if (scope.Count == 0) refusal = "No category here is named " + string.Join(", ", wanted) + ".";
}

var counts = new Dictionary<string, int>();
if (refusal == null)
{
    foreach (var id in doc.GetUnusedElements(scope))
    {
        var element = doc.GetElement(id);
        if (element == null) continue;
        elements.Add(element);
        var group = element.Category != null ? element.Category.Name : element.GetType().Name;
        counts[group] = counts.ContainsKey(group) ? counts[group] + 1 : 1;
        if (purgeableNames.Count < 300) purgeableNames.Add(group + ": " + element.Name);
    }
}

var byCategory = string.Join(", ", counts.OrderByDescending(p => p.Value).Select(p => p.Key + " " + p.Value));
if (refusal != null) findings.Add(refusal);
else findings.Add(elements.Count + " element(s) Purge Unused would offer in \"" + doc.Title + "\""
    + (elements.Count > 0 ? ": " + byCategory + ". Nothing deleted - DELETE_ELEMENTS removes them, then run this again for the next rung." : "."));
if (elements.Count > purgeableNames.Count) findings.Add("The names list stops at " + purgeableNames.Count + "; every element is handed on.");
