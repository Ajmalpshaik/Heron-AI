// NOT STANDALONE. Assumes `doc`, `errorsOnly` and `descriptionContains` are in
// scope; leaves `elements`, `findings`, `warningCount`, `notInThisModel`,
// `matched` and `unresolved` behind.
//
// VERSION 2 (2026-11-01) - SELECT_WITH_WARNINGS MERGED IN, AND WHAT IT HAD
// THAT VERSION 1 LACKED:
//
//   `errorsOnly` - only the warnings Revit raises at Error severity, so "isolate
//   the error level warnings" is one call. Left out it is false: every warning.
//
//   `descriptionContains` - only the warnings whose text holds that phrase,
//   capitals ignored, so the elements of one kind of warning can be handed on.
//   Left out it binds "none", which is no phrase.
//
//   `matched` - how many warnings passed those filters; `warningCount` stays
//   the model's whole total, so the two can be told apart. `unresolved` - how
//   many ids in the matched warnings resolve to nothing here, the count beside
//   `notInThisModel`'s list. A large figure means the warning list is stale.
//
//   ITS FINDINGS: how many elements the matched warnings name, each once; that
//   no warning matched a filter - which is NOT a clean model; and that these
//   are Revit's own warnings, so a clash, a wrong size, a missing tag or a
//   naming breach never appears here. An empty answer is not a clean model.
//
// READ ONLY. Opens no transaction and needs none.
//
// GROUPED BY MESSAGE TEXT, WHICH IS THE ONLY THING THAT GROUPS THEM USEFULLY.
// A failure definition id would be tidier and is worse: several distinct
// messages share one definition, so grouping on it merges problems a modeller
// treats separately. The text is what the Review Warnings dialog shows and
// what somebody would group by if they were doing it on paper.
//
// AN ELEMENT APPEARS ONCE, HOWEVER MANY WARNINGS NAME IT. A duct that is both
// duplicated and disconnected is one thing to go and look at, and handing the
// same element twice to ISOLATE_ELEMENTS would say nothing extra.
//
// A FAILING ELEMENT CAN BE ABSENT FROM THIS DOCUMENT. Warnings survive the
// element they were raised against in two ways: an id can be emptied by a
// delete or an undo, and a warning about a clash with a link carries an id
// from the LINK'S id space, which resolves to a different element here or to
// nothing at all. Either way doc.GetElement() returns null, and those ids are
// recorded in `notInThisModel` rather than dropped - an audit that quietly
// reports fewer elements than warnings is an audit nobody can reconcile.

var warnings = doc.GetWarnings();

var warningCount = warnings == null ? 0 : warnings.Count;
var findings = new List<string>();
var elements = new List<Element>();
var notInThisModel = new List<ElementId>();

// THE FILTERS (version 2, SELECT_WITH_WARNINGS). "none" is the card's
// `absent:` - no phrase.
var phrase = (descriptionContains ?? "").Trim();
if (string.Equals(phrase, "none", StringComparison.OrdinalIgnoreCase)) phrase = "";
var matched = 0;

var seen = new HashSet<ElementId>();
var counts = new Dictionary<string, int>();
var order = new List<string>();
var worst = new Dictionary<string, FailureSeverity>();

if (warnings != null)
{
    foreach (var warning in warnings)
    {
        if (warning == null) continue;

        var text = warning.GetDescriptionText();
        if (string.IsNullOrEmpty(text)) text = "(the warning carries no description)";

        if (errorsOnly && warning.GetSeverity() != FailureSeverity.Error) continue;
        if (phrase.Length > 0 && text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) < 0)
            continue;
        matched++;

        if (!counts.ContainsKey(text))
        {
            counts[text] = 0;
            worst[text] = warning.GetSeverity();
            order.Add(text);
        }
        counts[text] = counts[text] + 1;

        // An Error among Warnings changes what the row means, so the harshest
        // severity in the group is the one reported. Averaging or taking the
        // first would let one Error hide inside 600 warnings.
        if (warning.GetSeverity() == FailureSeverity.Error) worst[text] = FailureSeverity.Error;

        var failing = warning.GetFailingElements();
        if (failing == null) continue;

        foreach (var id in failing)
        {
            if (id == null || id == ElementId.InvalidElementId) continue;
            if (!seen.Add(id)) continue;

            var element = doc.GetElement(id);
            if (element == null) { notInThisModel.Add(id); continue; }

            elements.Add(element);
        }
    }
}

// Worst first, then commonest. An Error buried under a long list of warnings is
// the one thing this report exists to surface.
order.Sort(delegate (string a, string b)
{
    var aError = worst[a] == FailureSeverity.Error;
    var bError = worst[b] == FailureSeverity.Error;
    if (aError != bError) return aError ? -1 : 1;
    return counts[b].CompareTo(counts[a]);
});

foreach (var text in order)
{
    findings.Add(string.Format("{0} x{1}  {2}",
                               worst[text] == FailureSeverity.Error ? "ERROR  " : "warning",
                               counts[text],
                               text));
}

// ---- version 2: SELECT_WITH_WARNINGS' sentences ----------------------------

var unresolved = notInThisModel.Count;
var filtered = errorsOnly || phrase.Length > 0;

findings.Insert(0, string.Format("{0} element(s) named by {1} of the model's {2} warning(s){3}{4}. "
    + "Each element appears once however many warnings name it",
    elements.Count, matched, warningCount,
    errorsOnly ? ", error severity only" : "",
    phrase.Length == 0 ? "" : string.Format(", description containing '{0}'", phrase)));

if (unresolved > 0)
    findings.Add(string.Format("{0} id(s) in those warnings resolve to no element in this model - "
        + "deleted since, or a link's id. They are in notInThisModel. A large figure means "
        + "Revit's warning list is behind the model; reopen the warning dialog before acting on "
        + "this set", unresolved));

if (matched == 0)
    findings.Add(!filtered
        ? "This model has no warnings at all"
        : "No warning matched that filter - the model may still have others");

findings.Add("These are Revit's own warnings, of THIS document only. A clash, a wrong size, a "
    + "missing tag or a naming breach is not one of them and will never appear here, and a "
    + "link's warnings are its own - an empty answer is not a clean model");
