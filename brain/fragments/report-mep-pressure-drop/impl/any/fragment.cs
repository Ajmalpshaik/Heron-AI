// NOT STANDALONE. Assumes `doc` and `systemNameContains` are in scope; leaves
// `findings`, `systemsCalculated`, `uncalculated` and `criticalPathTotals`
// behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// SECTION INDEX AND SECTION NUMBER ARE NOT THE SAME THING. `SectionsCount`
// bounds the INDEX; the number is a property of the section and is NOT
// guaranteed to run 1..N without gaps. Looping over numbers reads some sections
// twice and misses others - and being a report, it looks plausible either way.
// So this iterates by INDEX and reads `.Number` off each section.
//
// A SYSTEM THAT IS NOT FULLY CONNECTED REPORTS ZERO SECTIONS, NOT AN ERROR.
// Revit only computes hydraulics for a complete, sized, connected system. Zero
// sections means NOT CALCULABLE, not "no pressure loss" - so those systems are
// listed separately and named, never folded into the table as zeros.
//
// THE CRITICAL PATH IS THE POINT. Total loss along it is what the fan or pump
// must overcome; the whole-system total over-specifies the plant.
//
// PRESSURE AND FRICTION STAY IN REVIT'S INTERNAL UNITS AND ARE SAID TO BE.
// Converting needs the units API, the call that breaks at Revit 2021.
//
// MEPSection IS IN THE Mechanical NAMESPACE FOR PIPE SYSTEMS TOO, which its use
// on a PipingSystem does not suggest.

var findings = new List<string>();
var systemsCalculated = 0;
var uncalculated = new List<string>();
var criticalPathTotals = new List<string>();

// Both system kinds, walked the same way.
var systems = new List<MEPSystem>();
foreach (var element in new FilteredElementCollector(doc)
    .OfClass(typeof(MechanicalSystem)).WhereElementIsNotElementType())
{
    var system = element as MEPSystem;
    if (system != null) systems.Add(system);
}
foreach (var element in new FilteredElementCollector(doc)
    .OfClass(typeof(PipingSystem)).WhereElementIsNotElementType())
{
    var system = element as MEPSystem;
    if (system != null) systems.Add(system);
}

foreach (var system in systems)
{
    var name = system.Name;
    if (!string.IsNullOrEmpty(systemNameContains)
        && name.IndexOf(systemNameContains, StringComparison.OrdinalIgnoreCase) < 0)
    {
        continue;
    }

    var mech = system as MechanicalSystem;
    var pipe = system as PipingSystem;

    var sectionCount = 0;
    try { sectionCount = mech != null ? mech.SectionsCount : (pipe != null ? pipe.SectionsCount : 0); }
    catch (Exception) { sectionCount = 0; }

    if (sectionCount == 0)
    {
        uncalculated.Add(string.Format("'{0}' - UNCALCULATED. Revit computes hydraulics only for a "
            + "complete, sized, connected system. An open end, an unconnected terminal or an "
            + "unsized segment gives nothing - which is NOT the same as no pressure loss",
            name));
        continue;
    }

    // The critical path, as section NUMBERS.
    var criticalNumbers = new HashSet<int>();
    try
    {
        var path = mech != null
            ? mech.GetCriticalPathSectionNumbers()
            : pipe.GetCriticalPathSectionNumbers();
        if (path != null) foreach (var n in path) criticalNumbers.Add(n);
    }
    catch (Exception) { }

    findings.Add(string.Format("'{0}' - {1} section(s):", name, sectionCount));

    var criticalLoss = 0.0;
    var criticalSeen = 0;
    var rows = new List<string>();

    // BY INDEX. See the header.
    for (var i = 0; i < sectionCount; i++)
    {
        MEPSection section = null;
        try
        {
            section = mech != null ? mech.GetSectionByIndex(i) : pipe.GetSectionByIndex(i);
        }
        catch (Exception) { section = null; }

        if (section == null) continue;

        var number = 0;
        var flow = 0.0;
        var velocity = 0.0;
        var loss = 0.0;
        var friction = 0.0;
        try
        {
            number = section.Number;
            flow = section.Flow;
            velocity = section.Velocity;
            loss = section.TotalPressureLoss;
            friction = section.Friction;
        }
        catch (Exception) { }

        var onCritical = criticalNumbers.Contains(number);
        if (onCritical) { criticalLoss += loss; criticalSeen++; }

        rows.Add(string.Format("    section {0,-5} flow {1,10:0.###}  velocity {2,8:0.##}  "
            + "loss {3,10:0.####}  friction {4,10:0.######}{5}",
            number, flow, velocity, loss, friction,
            onCritical ? "   <-- CRITICAL PATH" : ""));
    }

    // Critical path first - it is what sizes the plant.
    findings.Add(string.Format("    CRITICAL PATH: {0} section(s), total pressure loss {1:0.####} "
        + "(Revit internal units, NOT converted). This is what the fan or pump has to overcome",
        criticalSeen, criticalLoss));

    if (criticalSeen == 0)
    {
        findings.Add("    no critical path reported for this system - it is sectioned but Revit "
            + "names no index run, which usually means it is not fully connected end to end");
    }

    foreach (var row in rows) findings.Add(row);

    criticalPathTotals.Add(string.Format("{0} = {1:0.####} (internal units)", name, criticalLoss));
    systemsCalculated++;
}

if (uncalculated.Count > 0)
{
    findings.Add(string.Format("{0} system(s) UNCALCULATED - listed above. Use FIND_DEAD_ENDS to "
        + "find the break and CONNECT_OPEN_ENDS to close it", uncalculated.Count));
}

findings.Insert(0, string.Format("{0} system(s) with hydraulics calculated, {1} uncalculated. "
    + "Pressure and friction are in REVIT'S INTERNAL UNITS and are not converted - compare against "
    + "the system's own properties in Revit, where the same figures appear formatted",
    systemsCalculated, uncalculated.Count));
