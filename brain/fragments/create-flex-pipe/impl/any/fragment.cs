// NOT STANDALONE. Assumes `doc`, `systemType`, `flexPipeType`, `level`, `points`
// and `maxLengthMm` are in scope; leaves `created`, `lengthMm`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// FLEX DUCT AND FLEX PIPE, ONE TOOL. Version 1 made flexible pipe only, and
// CREATE_FLEX_DUCT made flexible duct with the same three rules: it guesses no
// route, the length limit is enforced before anything is made, and it makes
// flex, not a connection.
//
// VERSION 2 (2026-11-01) - CREATE_FLEX_DUCT MERGED IN. THE TYPE'S CLASS DECIDES
// WHICH IS MADE:
//
//   a FLEX DUCT type  -> FlexDuct.Create, and the system must be a duct system
//   a FLEX PIPE type  -> FlexPipe.Create, and the system must be a piping system
//   anything else     -> refused by name before Revit is asked
//
// `systemType` arrives as an MEPSystemType, the class the binder resolves for
// "duct or pipe system type", and `flexPipeType` as an MEPCurveType - so a duct
// system with a pipe type, or the other way round, can arrive together. That
// PAIRING IS CHECKED HERE, by name, before Revit is asked. CREATE_FLEX_DUCT
// left it to an exception from Create and caught the message; a refusal that
// names both and says which goes with which is the one a modeller can act on.
//
// THE TYPE IS CHECKED BEFORE REVIT IS ASKED. FlexPipe.IsFlexPipeTypeId is
// Revit's own answer to "is this a flex pipe type"; a flex duct type is one of
// class FlexDuctType. A rigid pipe or duct type is neither, and is refused by
// name rather than left to an exception from Create.

var refused = new List<string>();
var findings = new List<string>();
MEPCurve created = null;
var lengthMm = 0.0;

var mmPerFoot = 304.8;

var isFlexDuct = flexPipeType is FlexDuctType;
var isFlexPipe = !isFlexDuct && flexPipeType != null
    && FlexPipe.IsFlexPipeTypeId(doc, flexPipeType.Id);
var what = isFlexDuct ? "duct" : "pipe";

if (points == null || points.Count < 2)
{
    refused.Add("Fewer than two points were given, so NOTHING WAS CREATED. Two points are the "
        + "minimum, and the order they are given in IS the route.");
}
else if (systemType == null || flexPipeType == null || level == null)
{
    refused.Add("A system type, a flex duct or flex pipe type and a level are all required, and "
        + "at least one was missing. NOTHING WAS CREATED.");
}
else if (!isFlexDuct && !isFlexPipe)
{
    refused.Add("'" + flexPipeType.Name + "' is neither a FLEX duct type nor a FLEX pipe type, so "
        + "NOTHING WAS CREATED. Name a type from Flex Ducts or Flex Pipes in the Project Browser - "
        + "rigid pipe is CREATE_PIPE's job, and rigid duct is CREATE_DUCT_RUNS'.");
}
else if (isFlexDuct && !(systemType is MechanicalSystemType))
{
    // VERSION 2: the pairing, checked rather than left to Create's exception.
    refused.Add("'" + flexPipeType.Name + "' is a flex DUCT type and '" + systemType.Name
        + "' is not a duct system, so NOTHING WAS CREATED. A flex duct goes on a duct system - "
        + "Supply Air, Return Air, Exhaust Air - and a piping system takes a flex PIPE type.");
}
else if (isFlexPipe && !(systemType is PipingSystemType))
{
    refused.Add("'" + flexPipeType.Name + "' is a flex PIPE type and '" + systemType.Name
        + "' is not a piping system, so NOTHING WAS CREATED. A flex pipe goes on a piping "
        + "system - a sprinkler, domestic water or hydronic system - and a duct system takes a "
        + "flex DUCT type.");
}
else if (maxLengthMm <= 0)
{
    refused.Add("A maximum length greater than zero is required, so NOTHING WAS CREATED. A "
        + "flexible " + what + " is listed for a maximum length in most specifications, and it is "
        + "asked for rather than assumed.");
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
            // Create takes ids. The resolved objects are asked for theirs here,
            // rather than an integer being turned into an ElementId at the call
            // site - that conversion is what breaks in Revit 2024 (D-05).
            if (isFlexDuct)
                created = FlexDuct.Create(doc, systemType.Id, flexPipeType.Id, level.Id, points);
            else
                created = FlexPipe.Create(doc, systemType.Id, flexPipeType.Id, level.Id, points);

            if (created == null)
            {
                refused.Add("Revit returned no flex " + what + " and raised no error.");
            }
            else
            {
                lengthMm = routeMm;
                findings.Add("Created one flexible " + what + " of " + Math.Round(lengthMm)
                    + " mm along " + points.Count + " point(s), on '" + systemType.Name
                    + "', within the " + Math.Round(maxLengthMm) + " mm limit.");

                findings.Add("IT IS NOT CONNECTED TO ANYTHING. The " + what + " was made along the "
                    + "points given; sharing an endpoint with "
                    + (isFlexDuct ? "a diffuser or a branch" : "a sprinkler or a tee")
                    + " is not a connection in Revit. CONNECT_OPEN_ENDS joins it, "
                    + "TRACE_CONNECTIVITY proves it.");
            }
        }
        catch (Exception ex)
        {
            refused.Add("Revit refused to create the flex " + what + ": " + ex.Message
                + ". Nothing was created.");
        }
    }
}

foreach (var reason in refused) findings.Add(reason);
