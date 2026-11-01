// NOT STANDALONE. Assumes `doc`, `elements`, `target` and `join` are in scope;
// leaves `changed`, `alreadyRight`, `wouldNot`, `refused` and `findings` behind.
//
// VERSION 2 (2026-11-01) - UNJOIN_GEOMETRY MERGED IN. Unjoining was already
// here (join false) with the same read-back; what that card had and this one
// lacked is added:
//
//   THE SAME ELEMENT ON BOTH SIDES IS REFUSED BY ID, NOT SKIPPED. The target
//   handed in among the elements, and a missing element, are named in
//   `refused` - version 1 stepped over both without a word. The safer of the
//   two cards' rules.
//
//   QUANTITIES HAVE MOVED, said whenever a pair was joined or unjoined. Joined
//   elements share their overlap and one of them loses that volume from its
//   takeoff, so a schedule issued before the run now disagrees with the model.
//
// NOT CARRIED: UNJOIN_GEOMETRY's two matched lists (`first`, `second`) and its
// different-lengths refusal. Heron's binder fills one element list from the
// chain or the selection, never two - the add-in refuses two unbound element
// lists as one selection that cannot say which is which - and that card never
// ran for that reason. Each element here is paired with `target`, the one
// input that can say which element the other side is.
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
// THE TARGET IS NEVER JOINED TO ITSELF. Since version 2 it is REFUSED BY ID in
// `refused` when it appears in the set, rather than silently skipped.

var changed = 0;
var alreadyRight = new List<ElementId>();
var wouldNot = new List<string>();
// VERSION 2, from UNJOIN_GEOMETRY: pairs refused before Revit was asked.
var refused = new List<string>();
var findings = new List<string>();

if (target == null)
{
    findings.Add("No target element was given - joining needs the one thing everything else joins to");
}
else
{
    var position = 0;
    foreach (var element in elements)
    {
        position++;
        if (element == null)
        {
            refused.Add(string.Format("element {0} of the set is missing, so it has no pair", position));
            continue;
        }

        // ElementId to ElementId. Never as a number. Refused by id, never
        // skipped (version 2).
        if (element.Id == target.Id)
        {
            refused.Add(string.Format("{0}: the same element as the target - an element is never "
                + "joined to or unjoined from itself", element.Id));
            continue;
        }

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

    foreach (var reason in refused) findings.Add("Refused: " + reason);

    // VERSION 2, from UNJOIN_GEOMETRY.
    if (changed > 0)
        findings.Add("QUANTITIES HAVE MOVED. Joined elements share their overlap and one of them "
            + "loses that volume from its takeoff, so any schedule issued before this run is now "
            + "reporting different numbers from the model");
}
