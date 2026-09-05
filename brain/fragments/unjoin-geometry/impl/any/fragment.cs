// NOT STANDALONE. Assumes `doc`, `first` and `second` are in scope; leaves
// `unjoined` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16), so a batch of pairs is one undo.
//
// UNTIL THIS FRAGMENT EXISTED, ASKING TO UNJOIN REACHED JOIN_GEOMETRY. Measured.
// It is the worst shape a routing miss can take - the opposite operation
// arrives, succeeds, and the model is more joined than before somebody asked
// for less.
//
// A JOIN IS A PAIR, SO THIS TAKES PAIRS. Unjoining everything from everything
// is a different and far more destructive operation, and it is not this one.
//
// A PAIR THAT WAS NEVER JOINED IS SAID SO, NOT COUNTED AS DONE. "12 unjoined"
// when 5 of them were never joined is exactly the defect this project is built
// around.

var unjoined = 0;
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
    var alreadyApart = 0;
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

        if (a.Id == b.Id)
        {
            refused.Add(string.Format("pair {0}: both sides are the same element (id {1})", i + 1, a.Id));
            continue;
        }

        try
        {
            if (!JoinGeometryUtils.AreElementsJoined(doc, a, b))
            {
                alreadyApart++;
                continue;
            }

            JoinGeometryUtils.UnjoinGeometry(doc, a, b);

            // READ IT BACK. Revit returning is not Revit having done it.
            if (JoinGeometryUtils.AreElementsJoined(doc, a, b))
                refused.Add(string.Format("ids {0} and {1}: still joined after the call", a.Id, b.Id));
            else
                unjoined++;
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("ids {0} and {1}: {2}", a.Id, b.Id, ex.Message));
        }
    }

    findings.Add(string.Format("{0} of {1} pair(s) were separated. {2} were not joined to begin with",
        unjoined, first.Count, alreadyApart));

    foreach (var failure in refused) findings.Add("Refused: " + failure);

    if (unjoined > 0)
        findings.Add("QUANTITIES HAVE MOVED. Joined elements share their overlap and one of them "
            + "loses that volume from its takeoff, so any schedule issued before this run is now "
            + "reporting different numbers from the model");
}
