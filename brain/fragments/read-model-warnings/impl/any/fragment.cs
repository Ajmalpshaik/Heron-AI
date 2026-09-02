// NOT STANDALONE. Assumes `doc` is in scope; leaves `elements`, `findings`,
// `warningCount` and `notInThisModel` behind.
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
