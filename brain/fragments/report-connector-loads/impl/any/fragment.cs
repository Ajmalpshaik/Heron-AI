// NOT STANDALONE. Assumes `elements` is in scope, and leaves `loads`, `unset`,
// `noConnectors` and `expectedEmpty` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE INPUT SIDE OF A CALCULATION EVERYBODY READS THE OUTPUT OF.
//
// Revit sizes systems and reports pressure loss from what is SET ON THE
// CONNECTORS. A fixture with no demand, a sprinkler with no K-factor, an air
// terminal whose flow was never assigned - each contributes zero, and the
// system report comes back clean, fast and completely wrong. The numbers look
// right because they are real numbers. Nothing downstream can tell that an
// input was never filled in.
//
// READING A PROPERTY THE DOMAIN DOES NOT HAVE THROWS.
//
// A duct connector has no K-factor. An electrical connector has no flow. Revit
// RAISES rather than returning zero, so each value is guarded on its own. Guard
// the whole connector in one block instead and a single inapplicable property
// discards every readable value beside it - reporting a properly filled-in
// fixture as empty, which is worse than not reporting it.
//
// A ZERO ON A CURVE IS NORMAL; A ZERO ON A FIXTURE IS THE FINDING.
//
// A duct or pipe's own connectors carry no demand - the demand belongs to the
// fixture at the end of the run. `expectedEmpty` holds the curves so they are
// not counted as holes, and `unset` holds the family instances that genuinely
// have nothing assigned. Counting every zero as a problem produces a report
// nobody reads, which lands in the same place as writing none.

var loads = new Dictionary<ElementId, IDictionary<string, double>>();
var unset = new List<ElementId>();
var noConnectors = new List<ElementId>();
var expectedEmpty = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;

    ConnectorManager manager = null;
    // MEPCurve and FamilyInstance reach their connectors by different routes,
    // and neither is a base of the other. Both are asked so one code path
    // serves ducts, pipes, terminals and equipment.
    var curve = element as MEPCurve;
    if (curve != null)
    {
        try { manager = curve.ConnectorManager; } catch { }
    }
    else
    {
        var instance = element as FamilyInstance;
        if (instance != null && instance.MEPModel != null)
        {
            try { manager = instance.MEPModel.ConnectorManager; } catch { }
        }
    }

    if (manager == null) { noConnectors.Add(element.Id); continue; }

    ConnectorSet connectors = null;
    try { connectors = manager.Connectors; } catch { }
    if (connectors == null || connectors.Size == 0) { noConnectors.Add(element.Id); continue; }

    var readings = new Dictionary<string, double>();
    int index = 0;

    foreach (Connector connector in connectors)
    {
        if (connector == null) continue;
        string prefix = index.ToString() + ".";
        index++;

        // Each read stands alone. A throw here means the domain does not have
        // that property - which is a different fact from a value of zero, and
        // is recorded by the key being ABSENT rather than by a zero being
        // written.
        try { readings[prefix + "Flow"] = connector.Flow; } catch { }
        try { readings[prefix + "AssignedFlow"] = connector.AssignedFlow; } catch { }
        try { readings[prefix + "Demand"] = connector.Demand; } catch { }
        try { readings[prefix + "KCoefficient"] = connector.AssignedKCoefficient; } catch { }
        try { readings[prefix + "FixtureUnits"] = connector.AssignedFixtureUnits; } catch { }
        try { readings[prefix + "LossCoefficient"] = connector.AssignedLossCoefficient; } catch { }
        try { readings[prefix + "PressureDrop"] = connector.AssignedPressureDrop; } catch { }
        try { readings[prefix + "Coefficient"] = connector.Coefficient; } catch { }
    }

    loads[element.Id] = readings;

    bool anythingAssigned = false;
    foreach (var reading in readings)
    {
        if (Math.Abs(reading.Value) > 0.0) { anythingAssigned = true; break; }
    }

    if (anythingAssigned) continue;

    // Nothing assigned anywhere on this element. Whether that is a hole depends
    // entirely on what the element IS.
    if (curve != null) expectedEmpty.Add(element.Id);
    else unset.Add(element.Id);
}
