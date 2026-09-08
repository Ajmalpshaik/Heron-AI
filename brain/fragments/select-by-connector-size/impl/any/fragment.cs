// NOT STANDALONE. Assumes `elements`, `shape`, `diameterMm`, `widthMm`,
// `heightMm`, `toleranceMm` and `eitherOrientation` are in scope; leaves
// `matched`, `matchedIds`, `noConnectors`, `unknownShape` and `findings` behind.
//
// NO TRANSACTION, and it needs none. Nothing changes.
//
// A PARAMETER FILTER CANNOT ASK THIS. Size parameters describe the duct; a
// connector is where it JOINS, and on equipment those are different numbers.
//
// MILLIMETRES IN, FEET INSIDE. Revit stores connector dimensions in decimal
// feet, so every wanted size is converted once here. A tolerance is required,
// not optional: 230 mm is 0.75459... feet and no stored value equals a typed one.

var matched = new List<Element>();
var matchedIds = new List<ElementId>();
var noConnectors = new List<ElementId>();
var unknownShape = new List<ElementId>();
var findings = new List<string>();

var mmPerFoot = 304.8;

var wantRound = string.Equals(shape, "round", StringComparison.OrdinalIgnoreCase);
var wantRect = string.Equals(shape, "rectangular", StringComparison.OrdinalIgnoreCase);
var wantAny = string.Equals(shape, "any", StringComparison.OrdinalIgnoreCase)
    || string.IsNullOrEmpty(shape);

var tolFt = toleranceMm / mmPerFoot;
var diaFt = diameterMm / mmPerFoot;
var widthFt = widthMm / mmPerFoot;
var heightFt = heightMm / mmPerFoot;

if (!wantRound && !wantRect && !wantAny)
{
    findings.Add("\"" + shape + "\" is not a shape this understands. Use \"round\", "
        + "\"rectangular\" or \"any\". NOTHING WAS FILTERED and nothing was selected.");
}
else if (toleranceMm <= 0)
{
    findings.Add("A tolerance greater than zero is required, so NOTHING WAS FILTERED. "
        + "Revit stores these in feet: 230 mm is 0.75459... feet, and an exact comparison "
        + "against a typed number matches nothing at all.");
}
else if (diameterMm <= 0 && widthMm <= 0 && heightMm <= 0)
{
    // REFUSED RATHER THAN RUN. With no size asked for, every comparison below is
    // skipped and the filter returns an empty list - which is indistinguishable
    // from "nothing in this model is that size". A filter that answers zero to a
    // question nobody asked is the trap this fragment warns about elsewhere, and
    // it would be dishonest to guard the tolerance against it and not this.
    findings.Add("No size was asked for - diameter, width and height are all zero, so NOTHING "
        + "WAS FILTERED. Give at least one of them. Left to run, this would return an empty "
        + "list that looks exactly like \"nothing in the model is that size\".");
}
else
{
    // A local rather than a lambda, because a fragment's scope is generated and
    // keeping the shape flat is what keeps it readable at the machine.
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

        if (manager == null || manager.Connectors == null || manager.Connectors.Size == 0)
        {
            // NOT the same as "did not match". There was nothing to measure -
            // a wall or a generic model would land here, and counting it as a
            // miss would hide a wrong selection behind a plausible number.
            noConnectors.Add(element.Id);
            continue;
        }

        var hit = false;
        var sawMeasurable = false;

        // ConnectorSet is not generic, so the loop variable carries its type.
        foreach (Connector connector in manager.Connectors)
        {
            if (connector == null) continue;

            var round = connector.Shape == ConnectorProfileType.Round;
            var boxy = connector.Shape == ConnectorProfileType.Rectangular
                || connector.Shape == ConnectorProfileType.Oval;

            if (!round && !boxy) continue;          // shape this cannot measure
            sawMeasurable = true;

            if (round && (wantRound || wantAny))
            {
                if (diameterMm <= 0) continue;      // nothing asked about diameter
                var actual = connector.Radius * 2.0;
                if (Math.Abs(actual - diaFt) <= tolFt) { hit = true; break; }
            }
            else if (boxy && (wantRect || wantAny))
            {
                var w = connector.Width;
                var h = connector.Height;

                var straight = (widthMm <= 0 || Math.Abs(w - widthFt) <= tolFt)
                    && (heightMm <= 0 || Math.Abs(h - heightFt) <= tolFt);

                // A 400 x 230 duct and a 230 x 400 duct are the same duct
                // rotated. Whether that counts is the caller's policy.
                var swapped = eitherOrientation
                    && (widthMm <= 0 || Math.Abs(h - widthFt) <= tolFt)
                    && (heightMm <= 0 || Math.Abs(w - heightFt) <= tolFt);

                if (widthMm <= 0 && heightMm <= 0) continue;   // nothing asked
                if (straight || swapped) { hit = true; break; }
            }
        }

        if (hit)
        {
            // ANY connector matching is enough. A fitting has several and they
            // differ - requiring all of them returns nothing on every reducer,
            // which is the fitting most worth finding.
            matched.Add(element);
            matchedIds.Add(element.Id);
        }
        else if (!sawMeasurable)
        {
            unknownShape.Add(element.Id);
        }
    }

    findings.Add(matched.Count + " of " + elements.Count + " element(s) have a connector matching "
        + (wantRound ? "round" : wantRect ? "rectangular" : "any shape")
        + " at " + (diameterMm > 0 ? diameterMm + " mm dia" : "")
        + (widthMm > 0 || heightMm > 0 ? (diameterMm > 0 ? ", " : "") + widthMm + " x " + heightMm + " mm" : "")
        + ", within " + toleranceMm + " mm"
        + (eitherOrientation ? ", either way round." : ", that way round only."));
}

if (noConnectors.Count > 0)
{
    findings.Add(noConnectors.Count + " element(s) have NO connectors at all, so there was nothing "
        + "to measure on them. They are not a failed match - the filter does not apply to them.");
}

if (unknownShape.Count > 0)
{
    findings.Add(unknownShape.Count + " element(s) have only connectors whose shape this cannot "
        + "measure. Reported apart from a plain miss, because a size filter that quietly drops "
        + "what it could not read is a filter you cannot trust.");
}
