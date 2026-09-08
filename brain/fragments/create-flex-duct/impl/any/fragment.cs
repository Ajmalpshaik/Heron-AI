// NOT STANDALONE. Assumes `doc`, `systemType`, `flexDuctType`, `level`, `points`
// and `maxLengthMm` are in scope; leaves `created`, `lengthMm`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// IT GUESSES NO ROUTE. Every point is given. Invented flex looks plausible,
// passes a clash check, and is not what anybody would have drawn - and nothing
// afterwards distinguishes the asked-for parts from the imagined ones.
//
// IT MAKES FLEX, NOT A CONNECTION. Two things sharing an endpoint are not
// connected in Revit. CONNECT_OPEN_ENDS joins them; TRACE_CONNECTIVITY proves it.

var refused = new List<string>();
var findings = new List<string>();
FlexDuct created = null;
var lengthMm = 0.0;

var mmPerFoot = 304.8;

if (points == null || points.Count < 2)
{
    refused.Add("Fewer than two points were given, so NOTHING WAS CREATED. Two points are the "
        + "minimum, and the order they are given in IS the route.");
}
else if (systemType == null || flexDuctType == null || level == null)
{
    refused.Add("A system type, a flex duct type and a level are all required, and at least one "
        + "was missing. NOTHING WAS CREATED.");
}
else if (maxLengthMm <= 0)
{
    refused.Add("A maximum length greater than zero is required, so NOTHING WAS CREATED. "
        + "Flexible duct has a limit in most specifications and it is exceeded by accident, "
        + "so the limit is asked for rather than assumed.");
}
else
{
    // Measured along the route as given, before anything is made. A length found
    // after creation is one somebody has to go back and unpick.
    var totalFt = 0.0;
    var badPoint = false;

    for (var i = 1; i < points.Count; i++)
    {
        if (points[i] == null || points[i - 1] == null) { badPoint = true; break; }
        totalFt += points[i].DistanceTo(points[i - 1]);
    }

    lengthMm = totalFt * mmPerFoot;

    if (badPoint)
    {
        refused.Add("One of the points was empty, so NOTHING WAS CREATED.");
        lengthMm = 0.0;
    }
    else if (lengthMm > maxLengthMm)
    {
        refused.Add("The route measures " + Math.Round(lengthMm) + " mm, over the "
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
            created = FlexDuct.Create(doc, systemType.Id, flexDuctType.Id, level.Id, points);

            if (created == null)
            {
                refused.Add("Revit returned no flex duct and raised no error.");
            }
            else
            {
                findings.Add("Created one flexible duct of " + Math.Round(lengthMm) + " mm along "
                    + points.Count + " point(s), within the " + Math.Round(maxLengthMm) + " mm limit.");

                findings.Add("IT IS NOT CONNECTED TO ANYTHING. The duct was made along the points "
                    + "given; sharing an endpoint with a diffuser is not a connection in Revit. "
                    + "CONNECT_OPEN_ENDS joins it, TRACE_CONNECTIVITY proves it.");
            }
        }
        catch (Exception ex)
        {
            refused.Add("Revit refused to create the flex duct: " + ex.Message
                + ". Nothing was created. The system type and the flex duct type have to be "
                + "compatible - a duct type cannot serve a piping system.");
        }
    }
}

foreach (var reason in refused) findings.Add(reason);
