// NOT STANDALONE. Assumes `doc`, `elements` and `capTypeId` are in scope;
// leaves `capped`, `endsCapped`, `stillOpen`, `alreadyClosed`, `notPipes` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE SET IT IS GIVEN IS THE DECISION. An open end is very often deliberate - a
// riser waiting for the level above, a leg left for the contractor - and
// capping every one makes a model LOOK finished while hiding the gaps that are
// real. FIND_DEAD_ENDS does the sorting; this does the writing and nothing
// else.
//
// REVIT PLACES THE CAP ITSELF. PlaceCapOnOpenEnds finds the fitting, sizes it,
// works out which way the connector faces and joins it. By hand that is a
// family lookup, a direction calculation and an instance per end.
//
// ENDS ARE COUNTED, NOT PIPES. A pipe open at both ends takes two caps, so a
// count of pipes understates the work by half on exactly the runs worth
// looking at. Counting the open End connectors before and after is the only
// way to get that number, and it doubles as the read-back.
//
// A PIPE STILL OPEN AFTER THE CALL IS NAMED. The call can return without
// raising and leave the end as it was - a routing preference with no cap in it
// does exactly that - and a count taken from "we called it" reports that pipe
// as terminated.

var capped = new List<ElementId>();
var stillOpen = new List<ElementId>();
var alreadyClosed = new List<ElementId>();
var notPipes = new List<ElementId>();
var findings = new List<string>();
var endsCapped = 0;

// Open END connectors only. A pipe also carries connectors that are not ends,
// and counting those would report work that was never possible.
Func<Pipe, int> openEndsOf = pipe =>
{
    var open = 0;
    try
    {
        var manager = pipe.ConnectorManager;
        if (manager == null) return -1;

        foreach (Connector connector in manager.Connectors)
        {
            if (connector == null) continue;
            if (connector.ConnectorType != ConnectorType.End) continue;
            if (connector.IsConnected) continue;
            open++;
        }
    }
    catch
    {
        // Unreadable connectivity is not zero open ends. Reported as unknown
        // rather than counted as closed.
        return -1;
    }

    return open;
};

foreach (var element in elements)
{
    if (element == null) continue;

    var pipe = element as Pipe;
    if (pipe == null)
    {
        // Ducts land here, and there is no duct equivalent of this call. Named
        // rather than dropped, so a set that was mostly duct is visible as such.
        notPipes.Add(element.Id);
        continue;
    }

    var openBefore = openEndsOf(pipe);

    if (openBefore < 0)
    {
        stillOpen.Add(pipe.Id);
        findings.Add(pipe.Id + ": its connectors could not be read, so nothing was attempted. "
            + "Unreadable is not the same as closed.");
        continue;
    }

    if (openBefore == 0)
    {
        alreadyClosed.Add(pipe.Id);
        continue;
    }

    try
    {
        PlumbingUtils.PlaceCapOnOpenEnds(doc, pipe.Id, capTypeId);
    }
    catch (Exception failure)
    {
        stillOpen.Add(pipe.Id);
        findings.Add(pipe.Id + ": capping failed - " + failure.Message
            + ". A pipe type whose routing preferences name no cap fails here; "
            + "REPORT_ROUTING_PREFERENCES is where the missing entry shows.");
        continue;
    }

    // Read back. The call returns without raising and leaves the end exactly as
    // it was often enough that the count has to come from the model.
    var openAfter = openEndsOf(pipe);

    if (openAfter < 0)
    {
        stillOpen.Add(pipe.Id);
        findings.Add(pipe.Id + ": the cap was placed but the connectors could not be re-read, so "
            + "whether it took is unknown. Not counted as capped.");
        continue;
    }

    var closedHere = openBefore - openAfter;

    if (closedHere > 0)
    {
        endsCapped += closedHere;
        capped.Add(pipe.Id);
    }

    if (openAfter > 0)
    {
        stillOpen.Add(pipe.Id);
    }
}

findings.Add("Capped " + endsCapped + " end(s) on " + capped.Count + " pipe(s). The number that "
    + "matters is ends: a pipe open at both ends takes two caps.");

if (stillOpen.Count > 0)
{
    findings.Add(stillOpen.Count + " pipe(s) still read open. That is the read-back, not the "
        + "call - a cap that was not placed leaves no error behind it.");
}

if (notPipes.Count > 0)
{
    findings.Add(notPipes.Count + " element(s) in the set are not pipes and were not touched. "
        + "There is no equivalent call that caps a duct; a duct cap is drawn as part of the run.");
}
