// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `joins`,
// `withoutJoins` and `findings` behind.
//
// A JOIN IS INVISIBLE UNTIL IT IS WRONG. Nothing on screen says two elements are
// joined - what shows is a wall stopping short, a beam missing its end, or a
// takeoff quietly short by the overlap.
//
// WHICH ONE CUTS IS THE HALF NOBODY ASKS. A join has a direction: one element
// gives its overlap to the other. Two walls joined the wrong way round look
// almost the same in plan and give different volumes and a different section.
//
// AN ELEMENT JOINED TO NOTHING IS NORMAL. Most are. The count is reported so an
// empty list cannot be read as a failed lookup.

var joins = new List<string>();
var withoutJoins = 0;
var findings = new List<string>();

if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were given - hand in the ones whose joins are in question");
}
else
{
    var pairs = 0;
    var unreadable = 0;

    foreach (var element in elements)
    {
        if (element == null) continue;

        ICollection<ElementId> joinedIds = null;
        try { joinedIds = JoinGeometryUtils.GetJoinedElements(doc, element); }
        catch { unreadable++; continue; }

        if (joinedIds == null || joinedIds.Count == 0) { withoutJoins++; continue; }

        foreach (var otherId in joinedIds)
        {
            var other = doc.GetElement(otherId);
            if (other == null) continue;

            pairs++;

            var mine = string.Format("{0} (id {1})", element.Name, element.Id);
            var theirs = string.Format("{0} (id {1})", other.Name, other.Id);

            try
            {
                var cuts = JoinGeometryUtils.IsCuttingElementInJoin(doc, element, other);
                joins.Add(cuts
                    ? mine + " CUTS " + theirs
                    : mine + " is CUT BY " + theirs);
            }
            catch
            {
                joins.Add(mine + " is joined to " + theirs
                    + ", and which side cuts could not be read");
            }
        }
    }

    findings.Add(string.Format("{0} join(s) across {1} element(s). {2} of them are joined to nothing "
        + "at all, which is the ordinary case", pairs, elements.Count, withoutJoins));

    if (unreadable > 0)
        findings.Add(string.Format("{0} element(s) could not be asked about joins at all", unreadable));

    if (pairs > 0)
        findings.Add("The side that CUTS gives its overlap away, so it is the one whose volume and "
            + "material takeoff are reduced. Nothing on screen says which way round a join is");
}
