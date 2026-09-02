// NOT STANDALONE. Assumes `doc`, `elements`, `target` and `join` are in scope;
// leaves `changed`, `alreadyRight`, `wouldNot` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// JOINING IS NOT TOUCHING AND IT IS NOT A CLASH. Two elements can occupy the
// same space and be unjoined; Revit draws both and the section shows a line
// through the middle. FIND_CLASHES reports the overlap. This makes the two
// agree about which one cuts the other.
//
// ALREADY IN THE ASKED-FOR STATE IS NOT WORK. Joining a joined pair does
// nothing, and counting it would inflate the report of a job that did less than
// it says. Those pairs are listed separately.
//
// A PAIR REVIT WILL NOT JOIN IS A NORMAL OUTCOME. Elements that do not really
// touch, and categories Revit refuses, are counted with the reason - never
// thrown, because a batch must not lose the eleven that worked over the twelfth
// that would not.
//
// THE TARGET IS NEVER JOINED TO ITSELF. It is silently skipped if it appears in
// the set, since a filter that produced it is not a mistake worth a message.

var changed = 0;
var alreadyRight = new List<ElementId>();
var wouldNot = new List<string>();
var findings = new List<string>();

if (target == null)
{
    findings.Add("No target element was given - joining needs the one thing everything else joins to");
}
else
{
    foreach (var element in elements)
    {
        if (element == null) continue;

        // ElementId to ElementId. Never as a number.
        if (element.Id == target.Id) continue;

        try
        {
            var joined = JoinGeometryUtils.AreElementsJoined(doc, element, target);

            if (joined == join)
            {
                alreadyRight.Add(element.Id);
                continue;
            }

            if (join) JoinGeometryUtils.JoinGeometry(doc, element, target);
            else JoinGeometryUtils.UnjoinGeometry(doc, element, target);

            // READ IT BACK. A call that returned is not a join.
            if (JoinGeometryUtils.AreElementsJoined(doc, element, target) == join) changed++;
            else wouldNot.Add(string.Format("{0}: the call was accepted and the pair is still {1}",
                element.Id, join ? "unjoined" : "joined"));
        }
        catch (Exception ex)
        {
            wouldNot.Add(string.Format("{0}: {1}", element.Id, ex.Message));
        }
    }

    findings.Add(string.Format("{0} {1} with '{2}'. {3} were already {1} and were left alone{4}",
        changed, join ? "joined" : "unjoined", target.Name ?? target.Id.ToString(),
        alreadyRight.Count,
        wouldNot.Count == 0 ? "" : string.Format(", and {0} would not - usually because the two do not "
            + "really touch, or Revit does not join those categories", wouldNot.Count)));
}
