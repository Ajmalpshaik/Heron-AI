// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `arrowFamilyName`,
// `arrowTypeName` and `spacingMm` are in scope, and leaves `placed`,
// `noDirection`, `tooVertical` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). A view of arrows, one undo.
//
// THE CURVE DOES NOT KNOW WHICH WAY THE AIR GOES.
//
// A duct's location curve runs whichever way somebody drew it. The connectors
// know: one is an inlet and one an outlet, and the arrow runs from the first
// to the second. Using the curve puts half a drawing's arrows backwards with
// nothing to warn anybody - and once printed, an arrow is read as fact.
//
// A RUN THAT WILL NOT SAY GETS NOTHING.
//
// Ductwork modelled without a system carries no direction on its connectors.
// Naming those runs is the honest answer; an arrow chosen from the geometry
// looks exactly like one that was known.
//
// ROTATE ABOUT THE VIEW'S OWN AXIS.
//
// The symbol is placed in the view and turned to line up with the run. The
// axis is the view's direction, not world Z - about Z it is right in a plain
// plan and wrong in every section and elevation, which is the kind of error
// that only shows up on one sheet.
//
// AN INACTIVE SYMBOL THROWS ON THE FIRST PLACEMENT.
//
// The classic first-use-in-a-session failure, and it looks like the family is
// wrong rather than unactivated.

const double MmToFeet = 1.0 / 304.8;

int placed = 0;
var noDirection = new List<ElementId>();
var tooVertical = new List<ElementId>();
string refused = "";

double spacing = spacingMm * MmToFeet;

FamilySymbol arrow = null;
foreach (var element in new FilteredElementCollector(doc)
             .OfClass(typeof(FamilySymbol)))
{
    var symbol = element as FamilySymbol;
    if (symbol == null) continue;
    string family = "";
    string type = "";
    try { family = symbol.FamilyName ?? ""; } catch { }
    try { type = symbol.Name ?? ""; } catch { }

    if (string.Equals(family, (arrowFamilyName ?? "").Trim(), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(type, (arrowTypeName ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
    {
        arrow = symbol;
        break;
    }
}

if (arrow == null)
{
    refused = "No symbol '" + arrowFamilyName + " : " + arrowTypeName + "' is loaded. This fragment " +
              "places an arrow family that is already in the project and loads nothing - the arrow is a " +
              "drawing-office standard, not something to invent here.";
}
else if (spacing <= 0)
{
    refused = "A spacing of zero or less would place arrows for ever, or none at all.";
}
else
{
    if (!arrow.IsActive)
    {
        try { arrow.Activate(); doc.Regenerate(); }
        catch { refused = "The symbol could not be activated, so nothing can be placed."; }
    }
}

if (refused.Length == 0)
{
    foreach (var element in elements)
    {
        if (element == null) continue;

        var location = element.Location as LocationCurve;
        Line run = location != null ? location.Curve as Line : null;
        if (run == null) { noDirection.Add(element.Id); continue; }

        // Which way the flow goes, from the connectors.
        XYZ from = null, to = null;
        ConnectorManager manager = null;
        var curve = element as MEPCurve;
        if (curve != null) { try { manager = curve.ConnectorManager; } catch { } }

        if (manager != null)
        {
            ConnectorSet set = null;
            try { set = manager.Connectors; } catch { }
            if (set != null)
            {
                foreach (Connector connector in set)
                {
                    if (connector == null) continue;
                    try
                    {
                        if (connector.Direction == FlowDirectionType.In) from = connector.Origin;
                        else if (connector.Direction == FlowDirectionType.Out) to = connector.Origin;
                    }
                    catch { }
                }
            }
        }

        if (from == null || to == null) { noDirection.Add(element.Id); continue; }

        var flow = to - from;
        if (flow.GetLength() < 1e-9) { noDirection.Add(element.Id); continue; }
        flow = flow.Normalize();

        // Flat in the view's plane. A run pointing at the reader has almost
        // nothing left after this, and an arrow drawn from it is nonsense.
        var flat = flow - view.ViewDirection.Multiply(flow.DotProduct(view.ViewDirection));
        if (flat.GetLength() < 0.2) { tooVertical.Add(element.Id); continue; }
        flat = flat.Normalize();

        double length = run.Length;
        double angle = Math.Atan2(flat.DotProduct(view.UpDirection), flat.DotProduct(view.RightDirection));

        // Start half a spacing in, so an arrow does not sit on a fitting.
        for (double along = spacing / 2.0; along < length; along += spacing)
        {
            XYZ point;
            try { point = run.Evaluate(along / length, true); }
            catch { break; }

            FamilyInstance instance = null;
            try { instance = doc.Create.NewFamilyInstance(point, arrow, view); }
            catch { continue; }
            if (instance == null) continue;

            if (Math.Abs(angle) > 1e-9)
            {
                try
                {
                    var axis = Line.CreateBound(point, point + view.ViewDirection);
                    ElementTransformUtils.RotateElement(doc, instance.Id, axis, angle);
                }
                catch { }
            }

            placed++;
        }
    }
}
