// NOT STANDALONE. Assumes `doc`, `view`, `symbol`, `points` and
// `rotationDegrees` are in scope; leaves `placed`, `instanceId`,
// `instanceIds`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A VIEW-BASED FAMILY BELONGS TO ONE VIEW. Detail items and generic annotations
// are placed with the view overload of NewFamilyInstance - the call
// PLACE_FLOW_ARROWS already uses for its arrows. A family that is not
// view-based is refused by name: PLACE_FAMILY_INSTANCES places model families.
//
// IN A FAMILY TOO (version 2, 2026-11-11, row 5b-1499). In the Family Editor a
// detail item or a generic annotation loaded into the family - a diffuser's
// 600 mm flow arrow - is placed in ONE of the family's own views, the way
// Detail Component and Symbol place it by hand: `doc.FamilyCreate`'s view
// overload, the one ItemFactoryBase carries on every release. A project takes
// `doc.Create` exactly as version 1 did. PLACE_FAMILY_INSTANCES' nested branch
// places only work plane-based and level-based families; a view-based one is
// placed here.
//
// READ BACK, ALL OR NOTHING (version 2, both paths). Each one placed is read
// again: it exists, it belongs to the view asked for (its Owner View), its
// insertion point is where it was asked, within half a millimetre in the
// view's own plane, and its type is the one asked. One that does not read as
// asked fails the call, and the host rolls the whole call back. Its unique id
// is left behind - `instanceId`, the first, the same kind of id
// PLACE_FAMILY_INSTANCES' nested branch leaves for LOCK_NESTED_FAMILY_TO_PLANES.

var placed = new List<Element>();
var refused = new List<string>();
var findings = new List<string>();
var instanceId = "";
var instanceIds = "";

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;
var inFamily = doc.IsFamilyDocument;

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
        + "item or an annotation symbol - so NOTHING WAS PLACED. A model family goes on a level"
        + (inFamily ? ", or on a reference plane in a family" : "") + ": PLACE_FAMILY_INSTANCES.");
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
    // Each one placed, with the point it was asked at.
    var made = new List<Tuple<FamilyInstance, XYZ>>();
    var index = 0;
    foreach (var point in points)
    {
        index++;
        if (point == null) { refused.Add("Point " + index + " was empty - skipped."); continue; }
        try
        {
            var instance = inFamily
                ? doc.FamilyCreate.NewFamilyInstance(point, symbol, view)
                : doc.Create.NewFamilyInstance(point, symbol, view);
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
            made.Add(Tuple.Create(instance, point));
        }
        catch (Exception ex)
        {
            refused.Add("Point " + index + ": Revit refused - " + ex.Message);
        }
    }

    if (made.Count > 0) doc.Regenerate();

    // READ BACK - outside the per-point try, so a miss fails the whole call
    // rather than being counted as one refused point.
    var right = view.RightDirection;
    var up = view.UpDirection;
    foreach (var m in made)
    {
        var instance = m.Item1;
        var asked = m.Item2;
        var again = doc.GetElement(instance.Id) as FamilyInstance;
        if (again == null)
            throw new InvalidOperationException("A '" + symbol.FamilyName + ": " + symbol.Name + "' placed at "
                + mm(asked.DotProduct(right)) + ", " + mm(asked.DotProduct(up)) + " mm is not in the "
                + (inFamily ? "family" : "model") + " when read back. The call failed, and Heron rolls the whole call back.");
        if (again.OwnerViewId == null || !again.OwnerViewId.Equals(view.Id))
        {
            var owner = again.OwnerViewId == null || again.OwnerViewId.Equals(ElementId.InvalidElementId)
                ? null : doc.GetElement(again.OwnerViewId) as View;
            throw new InvalidOperationException("A '" + symbol.FamilyName + ": " + symbol.Name + "' reads its Owner View as "
                + (owner == null ? "none" : "'" + owner.Name + "'") + ", not '" + view.Name + "'. The call failed, and "
                + "Heron rolls the whole call back.");
        }
        if (!again.GetTypeId().Equals(symbol.Id))
            throw new InvalidOperationException("A placed instance reads another type than '" + symbol.FamilyName + ": "
                + symbol.Name + "'. The call failed, and Heron rolls the whole call back.");
        var at = again.Location as LocationPoint;
        if (at == null || at.Point == null)
            throw new InvalidOperationException("A '" + symbol.FamilyName + ": " + symbol.Name + "' has no insertion point to "
                + "read back. The call failed, and Heron rolls the whole call back.");
        var offRight = (at.Point - asked).DotProduct(right);
        var offUp = (at.Point - asked).DotProduct(up);
        if (Math.Abs(offRight) > halfMillimetre || Math.Abs(offUp) > halfMillimetre)
            throw new InvalidOperationException("A '" + symbol.FamilyName + ": " + symbol.Name + "' reads its insertion "
                + "point at " + mm(at.Point.DotProduct(right)) + ", " + mm(at.Point.DotProduct(up)) + " mm in '" + view.Name
                + "', not " + mm(asked.DotProduct(right)) + ", " + mm(asked.DotProduct(up)) + " mm as asked. The call "
                + "failed, and Heron rolls the whole call back.");
        placed.Add(again);
    }

    instanceId = placed.Count == 0 ? "" : placed[0].UniqueId;
    instanceIds = string.Join(",", placed.Select(p => p.UniqueId));

    findings.Insert(0, string.Format("Placed {0} of {1} '{2}: {3}' {4}'{5}'{6} - each read back in that view, at its "
        + "point and of that type.", placed.Count, points.Count, symbol.FamilyName, symbol.Name,
        inFamily ? "in the family's view " : "on ", view.Name,
        Math.Abs(angle) > 1e-9 ? string.Format(invariant, ", each turned {0:0.#} degrees", rotationDegrees) : ""));
    if (inFamily && placed.Count > 0)
        findings.Add("In a family the first one's id is " + instanceId + " - LOCK_NESTED_FAMILY_TO_PLANES locks it to "
            + "the family's planes in this view, ARRAY_FAMILY_FORMS arrays it here, and it shows in a project only in "
            + "views like this one.");
}

foreach (var reason in refused) findings.Add(reason);
