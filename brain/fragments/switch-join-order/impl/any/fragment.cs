// NOT STANDALONE. Assumes `doc`, `first` and `second` are in scope; leaves
// `switched` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16), so a batch of pairs is one undo.
//
// THIS IS THE FIX WHEN THE JOIN IS RIGHT AND ITS DIRECTION IS WRONG, which is
// most of the time. Unjoining to make the picture look right loses the join and
// leaves both volumes overlapping in the takeoff; turning it round keeps the
// join and moves the overlap to the side that should carry it.
//
// A PAIR THAT IS NOT JOINED IS REFUSED, NOT JOINED THEN SWITCHED. Joining
// because somebody asked to turn a join round is inventing an instruction, and
// the two may have been left apart on purpose.
//
// THE DIRECTION IS READ BEFORE AND AFTER. The call returning is not the
// direction having changed, and a silent no-op leaves a takeoff everybody now
// believes is right.

var switched = 0;
var findings = new List<string>();

if (first == null || second == null || first.Count == 0)
{
    findings.Add("No pairs were given - hand in two matching lists, one element of each pair in each");
}
else if (first.Count != second.Count)
{
    findings.Add(string.Format("A join is a pair, so the two lists have to be the same length - {0} "
        + "and {1} were given. Nothing changed", first.Count, second.Count));
}
else
{
    var notJoined = 0;
    var refused = new List<string>();

    for (var i = 0; i < first.Count; i++)
    {
        var a = first[i];
        var b = second[i];

        if (a == null || b == null)
        {
            refused.Add(string.Format("pair {0}: one of the two is missing", i + 1));
            continue;
        }

        try
        {
            if (!JoinGeometryUtils.AreElementsJoined(doc, a, b))
            {
                notJoined++;
                continue;
            }

            var before = JoinGeometryUtils.IsCuttingElementInJoin(doc, a, b);
            JoinGeometryUtils.SwitchJoinOrder(doc, a, b);
            var after = JoinGeometryUtils.IsCuttingElementInJoin(doc, a, b);

            if (before == after)
            {
                refused.Add(string.Format("ids {0} and {1}: the call returned and the direction did "
                    + "not change", a.Id, b.Id));
                continue;
            }

            switched++;
            findings.Add(string.Format("Ids {0} and {1}: {2} now cuts, where {3} did before",
                a.Id, b.Id, after ? a.Id : b.Id, before ? a.Id : b.Id));
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("ids {0} and {1}: {2}", a.Id, b.Id, ex.Message));
        }
    }

    findings.Add(string.Format("{0} of {1} join(s) turned round. {2} pair(s) are not joined at all, "
        + "and were refused rather than joined first",
        switched, first.Count, notJoined));

    foreach (var failure in refused) findings.Add("Refused: " + failure);

    if (switched > 0)
        findings.Add("THE OVERLAP HAS MOVED from one takeoff to the other. That is the operation, "
            + "not a side effect - any schedule issued before this run reports different numbers now");
}
