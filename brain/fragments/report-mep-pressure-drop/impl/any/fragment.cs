// NOT STANDALONE. Assumes `doc`, `systemNameContains` and `includeLinks` are in
// scope; leaves `findings`, `systemsCalculated`, `uncalculated`,
// `criticalPathTotals`, `linksSearched` and `linkedMatches` behind.
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
//
// LINKS ARE READ ONLY WHEN ASKED FOR, AND ONLY AS TEXT - D-59. On a job split
// by trade the plumbing or the mechanical model is a link. With `includeLinks`
// set, each loaded link's systems are read the same way - calculated or not,
// and the critical-path total - and reported in `linkedMatches`, one line per
// link and one per calculated system. `systemsCalculated`, `uncalculated` and
// `criticalPathTotals` stay this model's own, so the host answer reads exactly
// as before. NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM. Only model
// elements are read from a link, never its views or sheets.

var findings = new List<string>();
var systemsCalculated = 0;
var uncalculated = new List<string>();
var criticalPathTotals = new List<string>();

// ---- D-59: which links, only when asked for --------------------------------

var linksSearched = 0;
var linkedMatches = new List<string>();
var linkedTotal = 0;
var nestedLinks = 0;

// One entry per link FILE, keyed by link type - a file placed twice is one
// model placed twice, and counting placements would report a job with four
// links as having nine. LIST_LINKED_MODELS' rule, as REPORT_AREAS applies it.
var linkTypes = new List<ElementId>();
var linkDocs = new List<Document>();
var linkPlacements = new List<List<RevitLinkInstance>>();

if (includeLinks)
{
    foreach (var instance in new FilteredElementCollector(doc)
        .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
    {
        if (instance == null) continue;

        var typeId = instance.GetTypeId();
        if (typeId == null || typeId == ElementId.InvalidElementId) continue;

        var known = linkTypes.IndexOf(typeId);
        if (known >= 0) { linkPlacements[known].Add(instance); continue; }

        // LOADED IS ESTABLISHED BY ASKING FOR THE DOCUMENT, never by a status.
        Document linked = null;
        try { linked = instance.GetLinkDocument(); }
        catch (Exception) { linked = null; }
        if (linked == null) continue;

        linkTypes.Add(typeId);
        linkDocs.Add(linked);
        linkPlacements.Add(new List<RevitLinkInstance> { instance });

        try
        {
            nestedLinks += new FilteredElementCollector(linked)
                .OfClass(typeof(RevitLinkInstance)).GetElementCount();
        }
        catch (Exception) { }
    }
}

var linkBlocked = "";

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

// ---- D-59: the same read, in each link -------------------------------------

for (var i = 0; i < linkDocs.Count; i++)
{
    var linked = linkDocs[i];
    var linkSystems = new List<MEPSystem>();
    try
    {
        foreach (var element in new FilteredElementCollector(linked)
            .OfClass(typeof(MechanicalSystem)).WhereElementIsNotElementType())
        {
            var system = element as MEPSystem;
            if (system != null) linkSystems.Add(system);
        }
        foreach (var element in new FilteredElementCollector(linked)
            .OfClass(typeof(PipingSystem)).WhereElementIsNotElementType())
        {
            var system = element as MEPSystem;
            if (system != null) linkSystems.Add(system);
        }
    }
    catch (Exception) { }

    var linkCalculated = 0;
    var linkUncalculated = 0;
    var rows = new List<string>();
    foreach (var system in linkSystems)
    {
        var name = system.Name ?? "";
        if (!string.IsNullOrEmpty(systemNameContains)
            && name.IndexOf(systemNameContains, StringComparison.OrdinalIgnoreCase) < 0)
            continue;

        var mech = system as MechanicalSystem;
        var pipe = system as PipingSystem;
        var sectionCount = 0;
        try { sectionCount = mech != null ? mech.SectionsCount : (pipe != null ? pipe.SectionsCount : 0); }
        catch (Exception) { sectionCount = 0; }
        if (sectionCount == 0) { linkUncalculated++; continue; }

        var criticalNumbers = new HashSet<int>();
        try
        {
            var path = mech != null ? mech.GetCriticalPathSectionNumbers() : pipe.GetCriticalPathSectionNumbers();
            if (path != null) foreach (var n in path) criticalNumbers.Add(n);
        }
        catch (Exception) { }

        // BY INDEX, as above.
        var criticalLoss = 0.0;
        for (var index = 0; index < sectionCount; index++)
        {
            try
            {
                var section = mech != null ? mech.GetSectionByIndex(index) : pipe.GetSectionByIndex(index);
                if (section != null && criticalNumbers.Contains(section.Number))
                    criticalLoss += section.TotalPressureLoss;
            }
            catch (Exception) { }
        }

        linkCalculated++;
        rows.Add(string.Format("  {0} - '{1}' critical path {2:0.####} (internal units){3}",
            linked.Title, name, criticalLoss,
            criticalNumbers.Count == 0 ? " - no critical path reported" : ""));
    }

    linksSearched++;
    linkedTotal += linkCalculated;
    linkedMatches.Add(string.Format("{0}: {1} system(s) calculated, {2} UNCALCULATED",
        linked.Title, linkCalculated, linkUncalculated));
    linkedMatches.AddRange(rows);
}

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} calculated system(s), NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("What a link holds is reported here as text only - nothing from a link "
        + "is carried to the next step, which would look it up in this model");
