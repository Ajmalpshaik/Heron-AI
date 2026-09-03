// NOT STANDALONE. Assumes `doc`, `equipment` and `sizeTolerance` are in scope;
// leaves `findings`, `mismatched` and `unconnectedSpigots` behind.
//
// READ ONLY. Opens no transaction and needs none. Sizes are internal FEET.
//
// THE UNCONNECTED SPIGOTS ARE USUALLY THE BIGGER FINDING. An AHU with four
// connectors and two joined to nothing is a coordination gap no clash test and
// no connectivity walk flags - the system that IS connected traces perfectly.
// Every connector is listed with its state, so a missing service shows up as an
// absence.
//
// SIZE COMPARISON IS SHAPE-AWARE, and this is where such a check produces
// confident nonsense if it is not. Round reports a diameter, rectangular
// reports width and height; comparing them as one number is wrong. A
// round-to-rectangular joint is reported as a SHAPE CHANGE rather than
// measured - the transition may be right, but it should be a decision.
//
// DOMAIN MISMATCH IS ITS OWN CLASS. A pipe joined to a duct connector is not a
// tolerance problem; it usually means the wrong family was placed.
//
// IT READS WHAT THE FAMILY DECLARES. Connectors drawn at the wrong size agree
// with each other and are wrong together.

var findings = new List<string>();
var mismatched = new List<ElementId>();
var unconnectedSpigots = 0;

Func<Connector, string> describe = connector =>
{
    try
    {
        if (connector.Shape == ConnectorProfileType.Round)
        {
            return string.Format("round {0:0} mm dia", connector.Radius * 2 * 304.8);
        }
        if (connector.Shape == ConnectorProfileType.Rectangular
            || connector.Shape == ConnectorProfileType.Oval)
        {
            return string.Format("{0} {1:0} x {2:0} mm",
                connector.Shape == ConnectorProfileType.Oval ? "oval" : "rect",
                connector.Width * 304.8, connector.Height * 304.8);
        }
    }
    catch { }
    return "(size unreadable)";
};

foreach (var unit in equipment)
{
    var instance = unit as FamilyInstance;
    if (instance == null)
    {
        findings.Add(string.Format("{0} (id {1}): not a placed family instance - it has no connectors",
            unit.Name, unit.Id));
        continue;
    }

    ConnectorManager manager = null;
    try { manager = instance.MEPModel == null ? null : instance.MEPModel.ConnectorManager; }
    catch { }

    if (manager == null)
    {
        findings.Add(string.Format("{0} (id {1}): no MEP model at all. A Generic Model used as "
            + "equipment, or a placeholder - it can never join a system", unit.Name, unit.Id));
        continue;
    }

    var index = 0;
    foreach (Connector connector in manager.Connectors)
    {
        index++;
        var mine = describe(connector);

        var joined = false;
        try { joined = connector.IsConnected; }
        catch { }

        if (!joined)
        {
            unconnectedSpigots++;
            findings.Add(string.Format("{0} (id {1}) connector {2} [{3}]: NOTHING PLUGGED IN. No clash "
                + "test and no connectivity walk will flag this - the services that ARE connected trace "
                + "perfectly", unit.Name, unit.Id, index, mine));
            continue;
        }

        // What is actually on the other side. A joint through a transition is
        // compared against the FITTING, which is what is physically bolted on.
        Connector other = null;
        try
        {
            foreach (Connector reference in connector.AllRefs)
            {
                if (reference.Owner == null) continue;
                if (reference.Owner.Id == unit.Id) continue;
                other = reference;
                break;
            }
        }
        catch { }

        if (other == null)
        {
            findings.Add(string.Format("{0} (id {1}) connector {2} [{3}]: reads as connected but "
                + "nothing could be read on the other side", unit.Name, unit.Id, index, mine));
            continue;
        }

        var theirs = describe(other);
        var ownerName = other.Owner.Category == null ? "element" : other.Owner.Category.Name;

        // ---- domain first: a different kind of wrong
        var domainClash = false;
        try { domainClash = connector.Domain != other.Domain; }
        catch { }

        if (domainClash)
        {
            mismatched.Add(unit.Id);
            findings.Add(string.Format("{0} (id {1}) connector {2}: DOMAIN MISMATCH - {3} joined to a "
                + "{4}. Not a tolerance problem; usually the wrong family was placed",
                unit.Name, unit.Id, index, connector.Domain, other.Domain));
            continue;
        }

        // ---- shape next: never compared as one number
        var sameShape = false;
        try { sameShape = connector.Shape == other.Shape; }
        catch { }

        if (!sameShape)
        {
            findings.Add(string.Format("{0} (id {1}) connector {2}: SHAPE CHANGE - {3} onto {4} ({5}). "
                + "May be a correct transition; it should be a decision rather than an accident",
                unit.Name, unit.Id, index, mine, theirs, ownerName));
            continue;
        }

        // ---- size, per shape
        var differs = false;
        var detail = "";
        try
        {
            if (connector.Shape == ConnectorProfileType.Round)
            {
                var delta = Math.Abs(connector.Radius - other.Radius) * 2;
                differs = delta > sizeTolerance;
                detail = string.Format("{0:0.#} mm on diameter", delta * 304.8);
            }
            else
            {
                var dw = Math.Abs(connector.Width - other.Width);
                var dh = Math.Abs(connector.Height - other.Height);
                differs = dw > sizeTolerance || dh > sizeTolerance;
                detail = string.Format("{0:0.#} mm wide, {1:0.#} mm high", dw * 304.8, dh * 304.8);
            }
        }
        catch { }

        if (differs)
        {
            mismatched.Add(unit.Id);
            findings.Add(string.Format("{0} (id {1}) connector {2}: SIZE MISMATCH - spigot {3}, {4} is "
                + "{5}. Out by {6}. Revit builds it with a transition and nobody sees it until the "
                + "airflow does not arrive", unit.Name, unit.Id, index, mine, ownerName, theirs, detail));
        }
    }
}

findings.Insert(0, string.Format("{0} unit(s) checked: {1} with a mismatched joint, {2} spigot(s) with "
    + "nothing plugged in. Sizes are what the FAMILY declares - check one unit of each type against its "
    + "datasheet once, or every result agrees with itself and is wrong together",
    equipment.Count, mismatched.Count, unconnectedSpigots));
