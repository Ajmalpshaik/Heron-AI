// NOT STANDALONE. Assumes `doc`, `circuitKind`, `circuitName` and
// `includeLinks` are in scope; leaves `elements`, `panels`, `findings`,
// `linksSearched` and `linkedMatches` behind.
//
// A CIRCUIT'S Elements ARE ITS LOADS. THE PANEL IS NOT AMONG THEM. Measured: a
// circuit plainly involving a receptacle and a panelboard reported ONE element.
// Revit is right - everything downstream of the panel is on the circuit, the
// panel FEEDS it - and it is very easy to read as a broken filter. So the
// panels are reported separately rather than left as an apparent omission.
//
// SEPARATE FROM SELECT_BY_MEP_SYSTEM ON PURPOSE. A duct or pipe system type is
// a document element with a name a project renames; a circuit's type is a fixed
// enumeration in the API. Same words from a modeller, genuinely different reads.
//
// THE KIND IS MATCHED AS TEXT against the enumeration's own name, so "power"
// finds PowerCircuit. The alternative is asking a modeller to know the API's
// spelling.
//
// LINKS ARE READ ONLY WHEN ASKED FOR, AND THEY ARE COUNTED, NEVER SELECTED -
// D-59. The electrical model is often a link in somebody else's model. With
// `includeLinks` set, each loaded link's circuits are matched the same way and
// reported as TEXT in `linkedMatches`, one line per link: circuits, loads and
// the panels that feed them. Nothing enters `elements` or `panels`: those feed
// the next fragment, and the chain revives ids against the HOST document, so a
// linked id carried there binds an unrelated element silently
// (FRAGMENT-ISSUES row 75). NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS
// THEM. Only model elements are read from a link, never its views or sheets.

var elements = new List<Element>();
var panels = new List<string>();
var findings = new List<string>();

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

var wantKind = string.IsNullOrEmpty(circuitKind) ? "" : circuitKind.Trim();
var wantName = string.IsNullOrEmpty(circuitName) ? "" : circuitName.Trim();

if (wantKind.Length == 0 && wantName.Length == 0)
{
    findings.Add("Name a circuit kind - power, lighting, data - or a circuit name, or both. Every "
        + "circuit in the model is a legitimate answer to neither, and almost never the question");
}
else
{
    var matched = 0;
    var seen = new List<ElementId>();

    foreach (var circuit in new FilteredElementCollector(doc)
        .OfClass(typeof(ElectricalSystem)).Cast<ElectricalSystem>())
    {
        var kindText = "";
        try { kindText = circuit.SystemType.ToString(); }
        catch { kindText = ""; }

        if (wantKind.Length > 0 && kindText.IndexOf(wantKind, StringComparison.OrdinalIgnoreCase) < 0)
            continue;

        var name = circuit.Name ?? "";
        if (wantName.Length > 0 && name.IndexOf(wantName, StringComparison.OrdinalIgnoreCase) < 0)
            continue;

        matched++;

        try
        {
            var panelName = circuit.PanelName;
            if (!string.IsNullOrEmpty(panelName) && !panels.Contains(panelName)) panels.Add(panelName);
        }
        catch
        {
            // An unassigned circuit has no panel. That is a real state, not a failure.
        }

        foreach (Element load in circuit.Elements)
        {
            if (load == null) continue;

            var already = false;
            foreach (var known in seen)
            {
                if (known == load.Id) { already = true; break; }
            }
            if (already) continue;

            seen.Add(load.Id);
            elements.Add(load);
        }
    }

    var asked = new List<string>();
    if (wantKind.Length > 0) asked.Add(string.Format("kind containing '{0}'", wantKind));
    if (wantName.Length > 0) asked.Add(string.Format("name containing '{0}'", wantName));

    findings.Add(string.Format("{0} element(s) across {1} circuit(s) with {2}",
        elements.Count, matched, string.Join(" and ", asked.ToArray())));

    if (matched > 0)
        findings.Add(string.Format("These are the LOADS. The panel feeding a circuit is NOT one of "
            + "its elements, so a count that looks one short usually is not: the panel(s) here are {0}",
            panels.Count == 0 ? "none - these circuits have no panel assigned" : string.Join(", ", panels.ToArray())));

    if (matched == 0)
        findings.Add("No circuit matched. A model with no electrical work at all gives the same "
            + "answer, so check that circuits exist before reading this as a filter problem");

    // THE SAME MATCH IN EACH LINK. Counted, never selected.
    for (var i = 0; i < linkDocs.Count; i++)
    {
        var linked = linkDocs[i];
        var linkCircuits = 0;
        var linkLoads = new HashSet<ElementId>();
        var linkPanels = new List<string>();
        try
        {
            foreach (var circuit in new FilteredElementCollector(linked)
                .OfClass(typeof(ElectricalSystem)).Cast<ElectricalSystem>())
            {
                var kindText = "";
                try { kindText = circuit.SystemType.ToString(); } catch { kindText = ""; }
                if (wantKind.Length > 0 && kindText.IndexOf(wantKind, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                var name = circuit.Name ?? "";
                if (wantName.Length > 0 && name.IndexOf(wantName, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                linkCircuits++;
                try
                {
                    var panelName = circuit.PanelName;
                    if (!string.IsNullOrEmpty(panelName) && !linkPanels.Contains(panelName))
                        linkPanels.Add(panelName);
                }
                catch { }
                foreach (Element load in circuit.Elements)
                    if (load != null) linkLoads.Add(load.Id);
            }
        }
        catch (Exception) { }

        linksSearched++;
        linkedTotal += linkLoads.Count;
        linkedMatches.Add(string.Format("{0}: {1} load(s) across {2} circuit(s); panel(s) {3}",
            linked.Title, linkLoads.Count, linkCircuits,
            linkPanels.Count == 0 ? "none" : string.Join(", ", linkPanels.ToArray())));
    }
}

if (wantKind.Length == 0 && wantName.Length == 0 && linkDocs.Count > 0)
    linkBlocked = "Links NOT read: no circuit kind or name was given to look for";

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} load(s) on matching circuits, NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("Linked elements are counted here, never selected - the next step would "
        + "look them up in this model and could bind the wrong element");
