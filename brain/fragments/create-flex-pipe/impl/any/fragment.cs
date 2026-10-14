// NOT STANDALONE. Assumes `doc`, `systemType`, `flexPipeType`, `level`, `points`
// and `maxLengthMm` are in scope; leaves `created`, `lengthMm`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE PIPE TWIN OF CREATE_FLEX_DUCT, and the same three rules hold: it guesses
// no route, the length limit is enforced before anything is made, and it
// makes flex, not a connection.
//
// THE TYPE IS CHECKED BEFORE REVIT IS ASKED. `flexPipeType` arrives as an
// MEPCurveType, because that is the class the binder resolves by name - so a
// RIGID pipe type, or a duct type, can arrive under it. FlexPipe.IsFlexPipeTypeId
// is Revit's own answer to "is this a flex pipe type", and a no is refused by
// name rather than left to an exception from Create.

var refused = new List<string>();
var findings = new List<string>();
FlexPipe created = null;
var lengthMm = 0.0;

var mmPerFoot = 304.8;

if (points == null || points.Count < 2)
{
    refused.Add("Fewer than two points were given, so NOTHING WAS CREATED. Two points are the "
        + "minimum, and the order they are given in IS the route.");
}
else if (systemType == null || flexPipeType == null || level == null)
{
    refused.Add("A piping system type, a flex pipe type and a level are all required, and at "
        + "least one was missing. NOTHING WAS CREATED.");
}
else if (!FlexPipe.IsFlexPipeTypeId(doc, flexPipeType.Id))
{
    refused.Add("'" + flexPipeType.Name + "' is not a FLEX pipe type, so NOTHING WAS CREATED. "
        + "Name a type from Flex Pipes in the Project Browser - a rigid pipe is CREATE_PIPE's job, "
        + "and flexible duct is CREATE_FLEX_DUCT's.");
}
else if (maxLengthMm <= 0)
{
    refused.Add("A maximum length greater than zero is required, so NOTHING WAS CREATED. A "
        + "flexible drop is listed for a maximum length, and it is asked for rather than assumed.");
}
else
{
    // Measured along the route as given, before anything is made.
    var totalFt = 0.0;
    var badPoint = false;

    for (var i = 1; i < points.Count; i++)
    {
        if (points[i] == null || points[i - 1] == null) { badPoint = true; break; }
        totalFt += points[i].DistanceTo(points[i - 1]);
    }

    var routeMm = totalFt * mmPerFoot;

    if (badPoint)
    {
        refused.Add("One of the points was empty, so NOTHING WAS CREATED.");
    }
    else if (routeMm > maxLengthMm)
    {
        // lengthMm stays 0: nothing came out. The route's length is in the
        // sentence, where a measurement of what was ASKED FOR belongs -
        // CREATE_FLEX_DUCT reported the asked-for length beside a null once,
        // and it read as a flex that existed.
        refused.Add("The route measures " + Math.Round(routeMm) + " mm, over the "
            + Math.Round(maxLengthMm) + " mm limit given, so NOTHING WAS CREATED. "
            + "Shorten the route or raise the limit deliberately.");
    }
    else
    {
        try
        {
            created = FlexPipe.Create(doc, systemType.Id, flexPipeType.Id, level.Id, points);

            if (created == null)
            {
                refused.Add("Revit returned no flex pipe and raised no error.");
            }
            else
            {
                lengthMm = routeMm;
                findings.Add("Created one flexible pipe of " + Math.Round(lengthMm) + " mm along "
                    + points.Count + " point(s), on '" + systemType.Name + "', within the "
                    + Math.Round(maxLengthMm) + " mm limit.");

                findings.Add("IT IS NOT CONNECTED TO ANYTHING. The pipe was made along the points "
                    + "given; sharing an endpoint with a sprinkler or a tee is not a connection in "
                    + "Revit. CONNECT_OPEN_ENDS joins it, TRACE_CONNECTIVITY proves it.");
            }
        }
        catch (Exception ex)
        {
            refused.Add("Revit refused to create the flex pipe: " + ex.Message
                + ". Nothing was created.");
        }
    }
}

foreach (var reason in refused) findings.Add(reason);
