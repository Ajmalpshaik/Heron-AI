// NOT STANDALONE. Assumes `doc`, `elements`, `target` and `join` are in scope;
// leaves `changed`, `alreadyRight`, `wouldNot`, `refused` and `findings` behind.
//
// VERSION 3 (2026-11-01) - SWITCH_JOIN_ORDER MERGED IN, on the owner's
// instruction ("make sure there is no duplicate tools"). A third mode beside
// join and unjoin, on the same target-and-elements shape:
//
//   `join` IS NOW A WORD - true or join, false or unjoin, or switch. true and
//   false read exactly as version 2's bool did. Anything else is refused
//   before anything changes: they are opposite instructions and a guess at
//   one is a change nobody asked for.
//
//   SWITCH TURNS THE JOIN BETWEEN THE TARGET AND EACH ELEMENT ROUND, so the
//   one that was giving up its overlap keeps it - the fix when the join is
//   right and its direction is wrong ("the wall has eaten the column").
//
//   A PAIR THAT IS NOT JOINED IS REFUSED, NOT JOINED THEN SWITCHED - named in
//   `refused`. Joining because somebody asked to turn a join round invents an
//   instruction, and the two may have been left apart on purpose.
//
//   THE DIRECTION IS READ BEFORE AND AFTER. The call returning is not the
//   direction having changed: a switch that left it as it was is named in
//   `wouldNot`, never counted in `changed`. Each one turned says which now
//   cuts, and THE OVERLAP HAS MOVED from one takeoff to the other is said.
//
// NOT CARRIED: SWITCH_JOIN_ORDER's two matched lists (`first`, `second`) and
// their different-lengths refusal. Both were element lists binding the chain's
// one `elements`, so from the chain each pair was an element with ITSELF -
// never joined, every pair "not joined", nothing switched - and from the
// selection the add-in refused them as two lists one selection cannot tell
// apart. That card, DRAFT, could never have switched a pair. Here each element
// is paired with `target`, as join and unjoin are.
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

// THE MODE (version 3). true / join, false / unjoin, or switch - as the word
// was typed, capitals ignored. A bool's own spellings read as version 2's.
var mode = (join ?? "").Trim().ToLowerInvariant();
var switching = mode == "switch" || mode == "switch order" || mode == "switch join order";
var joinWanted = mode == "true" || mode == "join" || mode == "yes";
var unjoinWanted = mode == "false" || mode == "unjoin" || mode == "no";
var notJoined = 0;

if (!switching && !joinWanted && !unjoinWanted)
{
    findings.Add(string.Format("join was '{0}' - say true (join), false (unjoin) or switch (turn an "
        + "existing join round). They are opposite instructions, so NOTHING WAS CHANGED", join));
}
else if (target == null)
{
    findings.Add("No target element was given - joining needs the one thing everything else joins to");
}
else if (switching)
{
    // VERSION 3, SWITCH_JOIN_ORDER's job: the join between the target and
    // each element turned round, read before and after.
    var position = 0;
    var turned = new List<string>();
    foreach (var element in elements)
    {
        position++;
        if (element == null)
        {
            refused.Add(string.Format("element {0} of the set is missing, so it has no pair", position));
            continue;
        }
        if (element.Id == target.Id)
        {
            refused.Add(string.Format("{0}: the same element as the target - an element has no join with "
                + "itself to turn round", element.Id));
            continue;
        }

        try
        {
            // NOT JOINED IS REFUSED, NOT JOINED FIRST.
            if (!JoinGeometryUtils.AreElementsJoined(doc, element, target))
            {
                notJoined++;
                refused.Add(string.Format("{0}: not joined to the target - refused, NOT joined first. JOIN "
                    + "them (join true) if they should be", element.Id));
                continue;
            }

            var before = JoinGeometryUtils.IsCuttingElementInJoin(doc, element, target);
            JoinGeometryUtils.SwitchJoinOrder(doc, element, target);
            var after = JoinGeometryUtils.IsCuttingElementInJoin(doc, element, target);

            // READ IT BACK. A call that returned is not a switch.
            if (before == after)
            {
                wouldNot.Add(string.Format("{0}: the call returned and the direction did not change",
                    element.Id));
                continue;
            }

            changed++;
            if (turned.Count < 50)
                turned.Add(string.Format("{0} now cuts, where {1} did before",
                    after ? element.Id : target.Id, before ? element.Id : target.Id));
        }
        catch (Exception ex)
        {
            wouldNot.Add(string.Format("{0}: {1}", element.Id, ex.Message));
        }
    }

    findings.Add(string.Format("{0} join(s) with '{1}' turned round. {2} pair(s) are not joined at all and "
        + "were refused rather than joined first{3}", changed, target.Name ?? target.Id.ToString(), notJoined,
        wouldNot.Count == 0 ? "" : string.Format(", and {0} would not turn", wouldNot.Count)));
    foreach (var line in turned) findings.Add("Turned: " + line);
    foreach (var reason in refused) findings.Add("Refused: " + reason);

    if (changed > 0)
        findings.Add("THE OVERLAP HAS MOVED from one takeoff to the other. That is the operation, not a "
            + "side effect - any schedule issued before this run reports different numbers now");
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

            if (joined == joinWanted)
            {
                alreadyRight.Add(element.Id);
                continue;
            }

            if (joinWanted) JoinGeometryUtils.JoinGeometry(doc, element, target);
            else JoinGeometryUtils.UnjoinGeometry(doc, element, target);

            // READ IT BACK. A call that returned is not a join.
            if (JoinGeometryUtils.AreElementsJoined(doc, element, target) == joinWanted) changed++;
            else wouldNot.Add(string.Format("{0}: the call was accepted and the pair is still {1}",
                element.Id, joinWanted ? "unjoined" : "joined"));
        }
        catch (Exception ex)
        {
            wouldNot.Add(string.Format("{0}: {1}", element.Id, ex.Message));
        }
    }

    findings.Add(string.Format("{0} {1} with '{2}'. {3} were already {1} and were left alone{4}",
        changed, joinWanted ? "joined" : "unjoined", target.Name ?? target.Id.ToString(),
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
