// NOT STANDALONE. Assumes `doc`, `createdPhaseName`, `demolishedPhaseName` and
// `categories` are in scope; leaves `elements`, `withoutPhasing` and `findings`
// behind.
//
// MOST OF A MODEL HAS NO PHASING, AND IT IS COUNTED RATHER THAN DROPPED.
// Levels, grids, views, sheets and view-specific annotation carry no phase at
// all. Swallowing them in a try/catch makes a broken lookup and a correct small
// answer read identically - the same failure FILTER_ELEMENTS_BY_CATEGORY
// reports as `unresolvedLevel`, reported here as `withoutPhasing`.
//
// CreatedPhaseId AND DemolishedPhaseId ARE ON 2020. That was compiled at both
// ends of the range, not remembered; the plausible guess is that they are newer
// than that and the two phase BuiltInParameters are the compatible route.
//
// A PHASE NAME THAT DOES NOT EXIST IS REFUSED WITH THE REAL NAMES. Almost every
// office renames Revit's "Existing" and "New Construction", and a typo must not
// come back as an empty phase.

var elements = new List<Element>();
var withoutPhasing = 0;
var findings = new List<string>();

var wantCreated = string.IsNullOrEmpty(createdPhaseName) ? "" : createdPhaseName.Trim();
var wantDemolished = string.IsNullOrEmpty(demolishedPhaseName) ? "" : demolishedPhaseName.Trim();

if (wantCreated.Length == 0 && wantDemolished.Length == 0)
{
    findings.Add("No phase was named - give a Phase Created name, a Phase Demolished name, or both");
}
else
{
    var phases = new List<Phase>();
    foreach (var phase in new FilteredElementCollector(doc).OfClass(typeof(Phase)).Cast<Phase>())
        phases.Add(phase);

    var names = new List<string>();
    foreach (var phase in phases) names.Add(phase.Name);
    var known = names.Count == 0 ? "none" : string.Join(", ", names.ToArray());

    Phase created = null;
    Phase demolished = null;
    foreach (var phase in phases)
    {
        if (created == null && wantCreated.Length > 0
            && string.Equals(phase.Name, wantCreated, StringComparison.OrdinalIgnoreCase)) created = phase;
        if (demolished == null && wantDemolished.Length > 0
            && string.Equals(phase.Name, wantDemolished, StringComparison.OrdinalIgnoreCase)) demolished = phase;
    }

    if (wantCreated.Length > 0 && created == null)
        findings.Add(string.Format("No phase is called '{0}'. This model has: {1}", wantCreated, known));
    else if (wantDemolished.Length > 0 && demolished == null)
        findings.Add(string.Format("No phase is called '{0}'. This model has: {1}", wantDemolished, known));
    else
    {
        var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
        if (categories != null && categories.Count > 0)
            collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

        foreach (var element in collector)
        {
            var createdId = ElementId.InvalidElementId;
            var demolishedId = ElementId.InvalidElementId;
            var phased = false;

            try
            {
                createdId = element.CreatedPhaseId;
                demolishedId = element.DemolishedPhaseId;
                phased = createdId != ElementId.InvalidElementId
                    || demolishedId != ElementId.InvalidElementId;
            }
            catch
            {
                phased = false;
            }

            if (!phased)
            {
                withoutPhasing++;
                continue;
            }

            var createdOk = created == null || createdId == created.Id;
            var demolishedOk = demolished == null || demolishedId == demolished.Id;
            if (createdOk && demolishedOk) elements.Add(element);
        }

        var asked = new List<string>();
        if (created != null) asked.Add(string.Format("created in '{0}'", created.Name));
        if (demolished != null) asked.Add(string.Format("demolished in '{0}'", demolished.Name));

        findings.Add(string.Format("{0} element(s) {1}. {2} more carry no phasing at all and were "
            + "neither matched nor counted against the answer",
            elements.Count,
            string.Join(" and ", asked.ToArray()),
            withoutPhasing));

        if (demolished != null)
            findings.Add("This is 'demolished in that phase', not 'demolished' - an element that is "
                + "never demolished has no demolition phase to match");
    }
}
