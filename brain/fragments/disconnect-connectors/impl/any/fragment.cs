// NOT STANDALONE. Assumes `doc`, `elements` and `withinSelectionOnly` are in
// scope; leaves `disconnected`, `pairs`, `untouched` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// IT DELETES NOTHING. Disconnecting an elbow leaves the elbow there, connected
// to nothing. That looks untidy and it is correct: deciding a fitting is rubbish
// is a judgement about the design, and DELETE_ELEMENTS is where that is made.
//
// A LOGICAL CONNECTION IS NOT A PHYSICAL ONE. Connector references include
// system membership as well as end-to-end joints. Breaking a system reference
// would quietly remove elements from their system with nothing on the drawing
// to show it, so only physical ends are touched.

var pairs = new List<string>();
var untouched = new List<string>();
var findings = new List<string>();
var disconnected = 0;

var selected = new HashSet<ElementId>();
foreach (var element in elements)
{
    if (element != null && element.IsValidObject) selected.Add(element.Id);
}

if (selected.Count == 0)
{
    findings.Add("No elements were handed in, so NOTHING WAS DISCONNECTED. "
        + "Run the filter or the selection first.");
}
else
{
    foreach (var element in elements)
    {
        if (element == null || !element.IsValidObject) continue;

        ConnectorManager manager = null;

        var curve = element as MEPCurve;
        if (curve != null) manager = curve.ConnectorManager;

        if (manager == null)
        {
            var instance = element as FamilyInstance;
            if (instance != null && instance.MEPModel != null) manager = instance.MEPModel.ConnectorManager;
        }

        if (manager == null || manager.Connectors == null) continue;

        // ConnectorSet is not generic, so the loop variables carry their type.
        foreach (Connector mine in manager.Connectors)
        {
            if (mine == null) continue;
            if (mine.ConnectorType != ConnectorType.End) continue;   // physical ends only
            if (!mine.IsConnected) continue;
            if (mine.AllRefs == null) continue;

            // Copied out before disconnecting: AllRefs describes a live
            // relationship, and breaking one while walking it is how a loop
            // ends up skipping the connection after the one it just undid.
            var partners = new List<Connector>();
            foreach (Connector other in mine.AllRefs)
            {
                if (other == null) continue;
                if (other.ConnectorType != ConnectorType.End) continue;
                if (other.Owner == null || !other.Owner.IsValidObject) continue;
                if (other.Owner.Id == element.Id) continue;          // itself
                partners.Add(other);
            }

            foreach (var other in partners)
            {
                var partnerId = other.Owner.Id;

                if (withinSelectionOnly && !selected.Contains(partnerId))
                {
                    // The far side was never selected. Breaking it is a much
                    // larger act than was asked for, so it is recorded rather
                    // than done.
                    untouched.Add(element.Id + " to " + partnerId
                        + " - the far side was not in the selection");
                    continue;
                }

                // Only break what is really joined. IsConnectedTo is the pair
                // question; IsConnected above only says "joined to something".
                if (!mine.IsConnectedTo(other)) continue;

                try
                {
                    mine.DisconnectFrom(other);
                    disconnected++;
                    pairs.Add(element.Id + " to " + partnerId);
                }
                catch (Exception ex)
                {
                    untouched.Add(element.Id + " to " + partnerId + " - Revit refused: " + ex.Message);
                }
            }
        }
    }

    findings.Add("Broke " + disconnected + " joint(s)"
        + (withinSelectionOnly ? " between the elements given" : " on the elements given, including to things outside the selection")
        + ". NOTHING WAS DELETED - every fitting is still there, now with an open end.");

    if (disconnected > 0)
    {
        findings.Add("Breaking a joint can split one system into two or orphan the far side. "
            + "That is what was asked for. TRACE_CONNECTIVITY shows the shape that resulted.");
    }
}

if (untouched.Count > 0)
{
    findings.Add(untouched.Count + " connection(s) were left alone: "
        + string.Join("; ", untouched.ToArray()));
}
