// NOT STANDALONE. Assumes `doc`, `view`, `symbol`, `points` and
// `rotationDegrees` are in scope; leaves `placed`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A VIEW-BASED FAMILY BELONGS TO ONE VIEW. Detail items and generic annotations
// are placed with the view overload of NewFamilyInstance - the call
// PLACE_FLOW_ARROWS already uses for its arrows. A family that is not
// view-based is refused by name: PLACE_FAMILY_INSTANCES places model families.

var placed = new List<Element>();
var refused = new List<string>();
var findings = new List<string>();

var go = true;
if (view == null || symbol == null)
{
    refused.Add("A view and a family type are both required, so NOTHING WAS PLACED.");
    go = false;
}
else if (view.IsTemplate)
{
    refused.Add("'" + view.Name + "' is a view TEMPLATE, which holds no elements - NOTHING WAS PLACED.");
    go = false;
}
else if (view is View3D)
{
    refused.Add("'" + view.Name + "' is a 3D view, and Revit draws detail components and symbols only in "
        + "2D views - plans, sections, elevations, drafting views. NOTHING WAS PLACED.");
    go = false;
}
else if (symbol.Family == null || symbol.Family.FamilyPlacementType != FamilyPlacementType.ViewBased)
{
    refused.Add("'" + symbol.FamilyName + ": " + symbol.Name + "' is not a view-based family - not a detail "
        + "item or an annotation symbol - so NOTHING WAS PLACED. A model family goes on a level: "
        + "PLACE_FAMILY_INSTANCES.");
    go = false;
}
else if (points == null || points.Count == 0)
{
    refused.Add("No points were given, so NOTHING WAS PLACED.");
    go = false;
}

if (go)
{
    if (!symbol.IsActive) symbol.Activate();

    var angle = rotationDegrees * Math.PI / 180.0;
    var index = 0;
    foreach (var point in points)
    {
        index++;
        if (point == null) { refused.Add("Point " + index + " was empty - skipped."); continue; }
        try
        {
            var instance = doc.Create.NewFamilyInstance(point, symbol, view);
            if (instance == null)
            {
                refused.Add("Point " + index + ": Revit returned nothing and raised no error.");
                continue;
            }
            if (Math.Abs(angle) > 1e-9)
            {
                var axis = Line.CreateBound(point, point + view.ViewDirection);
                ElementTransformUtils.RotateElement(doc, instance.Id, axis, angle);
            }
            placed.Add(instance);
        }
        catch (Exception ex)
        {
            refused.Add("Point " + index + ": Revit refused - " + ex.Message);
        }
    }

    findings.Insert(0, string.Format("Placed {0} of {1} '{2}: {3}' on '{4}'{5}.", placed.Count, points.Count,
        symbol.FamilyName, symbol.Name, view.Name,
        Math.Abs(angle) > 1e-9 ? string.Format(", each turned {0:0.#} degrees", rotationDegrees) : ""));
}

foreach (var reason in refused) findings.Add(reason);
