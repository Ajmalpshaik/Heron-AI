// NOT STANDALONE. Assumes `doc`, `schemeNameContains`, `listEachArea` and
// `includeLinks` are in scope; leaves `findings`, `schemeTotalsM2`,
// `placedCount`, `unplaced`, `unbounded` and `linksSearched` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE SCHEMES OVERLAP ON PURPOSE, AND THAT IS WHY THERE IS NO GRAND TOTAL. The
// same floor is measured twice - once under Gross Building, once under Rentable
// - and both sets of Area elements sit in OST_Areas at the same time, in the
// same place. Collect the category, sum it, and the total is roughly double the
// building and belongs to no scheme at all. Every number below is PER SCHEME and
// no cross-scheme total is offered, because such a total would be believed.
//
// UNPLACED AND UNBOUNDED BOTH READ AS ZERO AND HAVE DIFFERENT FIXES:
//   unplaced   - the Area exists in the schedule and sits nowhere. Location null
//   unbounded  - it IS placed, but its boundary does not close, so Revit gives 0
// Both occupy a row in every area schedule and contribute nothing. Reporting
// them as "0" beside real areas hides the difference, and the difference is the
// whole fix.
//
// AREA IS INTERNAL SQUARE FEET, MULTIPLIED BY 0.09290304 FOR SQUARE METRES.
// Plain arithmetic at the edge, never a units API - that is the call that breaks
// at Revit 2021.
//
// LINKS ARE READ ONLY WHEN ASKED FOR - D-59, and this fragment is the case that
// makes the decision matter. In a federated job the AREAS ARE IN THE
// ARCHITECTURAL MODEL, which for an MEP coordinator is a link, so "report the
// areas" against the host alone comes back confidently EMPTY on a job where
// every area exists. Absent means host only, which is what this measured before.
//
// LOADED IS ESTABLISHED BY ASKING FOR THE DOCUMENT, never by the recorded
// status. GetLinkDocument() returns null exactly when there is no geometry to
// read - after a workset-level unload, or when the file has moved - and a status
// saying "loaded" over a document that is not there would put a zero in a total.
// LIST_LINKED_MODELS' rule, applied rather than restated.
//
// `linksSearched` COUNTS DOCUMENTS, NOT PLACEMENTS. A wing linked in twice is
// one model placed twice; counting instances reports a job with four links as
// having nine, and would read each of its areas twice into the totals.
//
// THE SCHEME KEY IS QUALIFIED BY MODEL, and this is the trap. The block above
// exists because two SCHEMES measure one floor twice and adding them is double
// the building. Two MODELS each holding a scheme called "Gross Building" is the
// same fault one level up: merged under one key, a link's floor plate would be
// added to the host's and the total would belong to no building at all. So when
// links are read the key carries the model name, and the schemes stay apart.

var findings = new List<string>();
var schemeTotalsM2 = new List<string>();
var placedCount = 0;
var unplaced = new List<string>();
var unbounded = new List<string>();

var linksSearched = 0;

// The host first, always. `sources` is the documents actually read, each with
// the name a finding will carry.
var sources = new List<KeyValuePair<string, Document>>();
sources.Add(new KeyValuePair<string, Document>(doc.Title, doc));

if (includeLinks)
{
    // DE-DUPLICATED BY LINK TYPE, WHICH IS THE FILE - not by Document
    // identity. LIST_LINKED_MODELS groups by GetTypeId() for exactly this
    // reason and has been proved against a real model doing so; whether two
    // placements of one file hand back the same Document OBJECT is an API
    // detail nothing here has put in front of Revit, and the type id needs no
    // such assumption.
    var seenTypes = new List<ElementId>();
    foreach (var instance in new FilteredElementCollector(doc)
        .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
    {
        if (instance == null) continue;

        var typeId = instance.GetTypeId();
        if (typeId == null || typeId == ElementId.InvalidElementId) continue;
        if (seenTypes.Contains(typeId)) continue;   // placed twice, one model

        Document linked = null;
        try { linked = instance.GetLinkDocument(); }
        catch (Exception) { linked = null; }
        if (linked == null) continue;               // nothing readable in it

        seenTypes.Add(typeId);
        sources.Add(new KeyValuePair<string, Document>(linked.Title, linked));
        linksSearched++;
    }
}

// EACH AREA CARRIES THE MODEL IT CAME FROM, rather than a lookup keyed by
// its id. ELEMENT IDS ARE UNIQUE WITHIN A DOCUMENT AND NOT ACROSS THEM, and
// they are handed out low and sequential - so a host Area and a linked Area
// sharing an id is ordinary, not a corner case. A dictionary keyed by id
// would hold ONE entry for the two of them and report one of the areas under
// the other's model name. That is this fragment's own fault in a new place:
// a number that looks like a floor area, attributed to the wrong building.
var areas = new List<KeyValuePair<string, Area>>();
foreach (var source in sources)
{
    foreach (var element in new FilteredElementCollector(source.Value)
        .OfCategory(BuiltInCategory.OST_Areas).WhereElementIsNotElementType())
    {
        var area = element as Area;
        if (area == null) continue;
        areas.Add(new KeyValuePair<string, Area>(source.Key, area));
    }
}

if (areas.Count == 0)
{
    findings.Add("This model has no Area elements at all. Note AREAS are not ROOMS - a model full of "
        + "rooms has none of these, and the room fragments are what answers a room question");
}
else
{
    // Group by scheme. The scheme is the only level at which a total means
    // anything, so it is the only level one is produced at.
    var byScheme = new Dictionary<string, List<Area>>();
    foreach (var entry in areas)
    {
        var area = entry.Value;
        var schemeName = "<no scheme>";
        try
        {
            var scheme = area.AreaScheme;
            if (scheme != null) schemeName = scheme.Name;
        }
        catch (Exception) { }

        // Only when links were read - the host-only key is what every existing
        // proof measured and it is left exactly as it was.
        if (linksSearched > 0)
        {
            schemeName = string.Format("{0} :: {1}", entry.Key, schemeName);
        }

        if (!string.IsNullOrEmpty(schemeNameContains)
            && schemeName.IndexOf(schemeNameContains, StringComparison.OrdinalIgnoreCase) < 0)
        {
            continue;
        }

        if (!byScheme.ContainsKey(schemeName)) byScheme[schemeName] = new List<Area>();
        byScheme[schemeName].Add(area);
    }

    if (byScheme.Count == 0)
    {
        findings.Add(string.Format("{0} Area element(s) in the model, but none in a scheme whose "
            + "name contains '{1}'", areas.Count, schemeNameContains));
    }

    foreach (var scheme in byScheme)
    {
        var schemeTotal = 0.0;
        var schemePlaced = 0;
        var byLevel = new Dictionary<string, double>();
        var levelCounts = new Dictionary<string, int>();

        findings.Add(string.Format("SCHEME '{0}' - {1} area element(s):", scheme.Key,
            scheme.Value.Count));

        foreach (var area in scheme.Value)
        {
            var label = string.Format("'{0}' no. {1}", area.Name, area.Number);

            // Unplaced and unbounded both read as zero and are different faults.
            if (area.Location == null)
            {
                unplaced.Add(string.Format("{0} in scheme '{1}'", label, scheme.Key));
                continue;
            }

            var sqFt = 0.0;
            try { sqFt = area.Area; }
            catch (Exception) { sqFt = 0.0; }

            if (sqFt <= 0)
            {
                unbounded.Add(string.Format("{0} in scheme '{1}' - placed, but its boundary does "
                    + "not close", label, scheme.Key));
                continue;
            }

            var m2 = sqFt * 0.09290304;
            schemeTotal += m2;
            schemePlaced++;
            placedCount++;

            var levelName = "<no level>";
            try
            {
                var level = area.Level;
                if (level != null) levelName = level.Name;
            }
            catch (Exception) { }

            if (byLevel.ContainsKey(levelName)) { byLevel[levelName] += m2; levelCounts[levelName]++; }
            else { byLevel[levelName] = m2; levelCounts[levelName] = 1; }

            if (listEachArea)
            {
                findings.Add(string.Format("    {0}  {1}  {2:0.##} m2", label, levelName, m2));
            }
        }

        foreach (var level in byLevel)
        {
            findings.Add(string.Format("    {0}: {1} area(s), {2:0.##} m2",
                level.Key, levelCounts[level.Key], level.Value));
        }

        findings.Add(string.Format("    SCHEME TOTAL: {0:0.##} m2 across {1} placed area(s)",
            schemeTotal, schemePlaced));

        schemeTotalsM2.Add(string.Format("{0} = {1:0.##} m2", scheme.Key, schemeTotal));
    }

    if (unplaced.Count > 0)
    {
        findings.Add(string.Format("{0} UNPLACED area(s) - they hold a row in every area schedule "
            + "and sit nowhere in the model. Place them or delete the row", unplaced.Count));
    }

    if (unbounded.Count > 0)
    {
        findings.Add(string.Format("{0} UNBOUNDED area(s) - placed, but the boundary lines do not "
            + "close, so Revit gives them nothing. Close the boundary", unbounded.Count));
    }

    findings.Add("NO TOTAL ACROSS SCHEMES IS GIVEN, on purpose - schemes measure the same floor more "
        + "than once, so adding them together is roughly double the building and belongs to no "
        + "scheme at all");
}

// D-59: THE ANSWER SAYS WHAT IT READ. The three cases read differently on
// purpose - asked-and-found, asked-and-none-loaded, and not asked - because a
// federated job where the links are all unloaded returns the host's number and
// nothing else would say so.
if (!includeLinks)
{
    findings.Insert(0, "Read THIS MODEL ONLY. Areas in a linked model were not counted - "
        + "in a federated job the areas are usually in the architectural link, so ask for "
        + "links if that is the number you want");
}
else if (linksSearched == 0)
{
    findings.Insert(0, "Links were ASKED FOR AND NONE ARE LOADED, so this is the host model's "
        + "own number. That is not the same as there being no links - reload them in Manage "
        + "Links and ask again");
}
else
{
    findings.Insert(0, string.Format("Read this model AND {0} linked model(s). Each scheme is "
        + "reported under the model it belongs to - two models with a scheme of the same name "
        + "are two schemes, and adding them would be two buildings", linksSearched));
}

findings.Insert(0, string.Format("{0} placed area(s) reported; {1} unplaced, {2} unbounded",
    placedCount, unplaced.Count, unbounded.Count));
