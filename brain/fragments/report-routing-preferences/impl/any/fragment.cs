// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `findings`,
// `summary`, `typesReported`, `emptyGroups`, `missingFamilies` and
// `notRoutable` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// A SIZE OUTSIDE EVERY RULE'S RANGE GETS NO FITTING AT ALL, with no error and no
// warning - the run just stays unconnected at that junction. That gap is what
// this fragment is really for, so the COVERED SIZE RANGE per group is reported
// explicitly rather than left to be worked out from a table of numbers.
//
// A RULE POINTING AT AN UNLOADED FAMILY LOOKS LIKE A BUG AND IS NOT. The rule is
// fine; the family is gone. Same symptom as an empty group, different fix, so
// the two are reported separately.
//
// AN EMPTY GROUP IS A REAL ANSWER. A type with no cap rule cannot cap a run.
// Reporting only the groups that have rules hides exactly the group somebody is
// asking about when a run will not close.
//
// THE SIZE WINDOW COMES FROM GetCriterion(i), NOT FROM A GetCriteria()
// COLLECTION - that method exists on no release.
//
// SIZES ARE INTERNAL FEET, MULTIPLIED BY 304.8. Plain arithmetic at the edge,
// never a units API - that is the call that breaks at Revit 2021.

var findings = new List<string>();
// EVERY RULE ON ONE LINE, AS A STRING. `findings` is a list, and a list is
// abbreviated to its first three entries before a reader sees it - so a type
// with eight rule groups showed three and read as though it had three. A
// string is not abbreviated. This is the line somebody actually reads when
// deciding whether one type's fittings can be copied onto another.
var summary = "";
var summaryRows = new List<string>();
var typesReported = 0;
var emptyGroups = new List<string>();
var missingFamilies = new List<string>();
var notRoutable = new List<string>();

// The groups worth reporting. Undefined is Revit's own placeholder.
var groups = new List<RoutingPreferenceRuleGroupType>
{
    RoutingPreferenceRuleGroupType.Segments,
    RoutingPreferenceRuleGroupType.Elbows,
    RoutingPreferenceRuleGroupType.Junctions,
    RoutingPreferenceRuleGroupType.Crosses,
    RoutingPreferenceRuleGroupType.Transitions,
    RoutingPreferenceRuleGroupType.Unions,
    RoutingPreferenceRuleGroupType.MechanicalJoints,
    RoutingPreferenceRuleGroupType.Caps,
};

// Dedupe to types: an instance resolves to its type, a type is used as-is.
var seenTypes = new HashSet<ElementId>();
var typesToReport = new List<MEPCurveType>();

foreach (var element in elements)
{
    if (element == null) continue;
    var asType = element as MEPCurveType;
    var type = asType ?? doc.GetElement(element.GetTypeId()) as MEPCurveType;

    if (type == null)
    {
        notRoutable.Add(string.Format("'{0}' is not a pipe or duct type and has no routing "
            + "preferences", element.Name));
        continue;
    }

    if (seenTypes.Add(type.Id)) typesToReport.Add(type);
}

foreach (var type in typesToReport)
{
    RoutingPreferenceManager manager = null;
    try { manager = type.RoutingPreferenceManager; }
    catch (Exception) { manager = null; }

    if (manager == null)
    {
        notRoutable.Add(string.Format("'{0}' has no routing preference manager", type.Name));
        continue;
    }

    typesReported++;
    findings.Add(string.Format("TYPE '{0}':", type.Name));
    var groupParts = new List<string>();

    foreach (var group in groups)
    {
        var ruleCount = 0;
        try { ruleCount = manager.GetNumberOfRules(group); }
        catch (Exception) { ruleCount = 0; }

        if (ruleCount == 0)
        {
            emptyGroups.Add(string.Format("'{0}' has NO {1} rule", type.Name, group));
            findings.Add(string.Format("    {0,-22} NO RULE - this type cannot place a {1} at all",
                group, group));
            continue;
        }

        var lowestMm = double.MaxValue;
        var highestMm = double.MinValue;
        var sawSize = false;

        findings.Add(string.Format("    {0,-22} {1} rule(s):", group, ruleCount));

        for (var i = 0; i < ruleCount; i++)
        {
            RoutingPreferenceRule rule = null;
            try { rule = manager.GetRule(group, i); }
            catch (Exception) { rule = null; }
            if (rule == null) continue;

            // The family the rule points at. Missing means UNLOADED, not broken.
            var part = doc.GetElement(rule.MEPPartId);
            string partName;
            if (part == null)
            {
                partName = "<FAMILY NOT LOADED - the rule is fine, the family is gone>";
                missingFamilies.Add(string.Format("'{0}' / {1} rule {2} points at a family that is "
                    + "not in this project", type.Name, group, i + 1));
            }
            else
            {
                var partType = part as ElementType;
                partName = partType == null ? part.Name
                    : string.Format("{0}: {1}", partType.FamilyName, partType.Name);
            }

            // The size window, through GetCriterion. See the header.
            var window = "any size";
            try
            {
                for (var c = 0; c < rule.NumberOfCriteria; c++)
                {
                    var size = rule.GetCriterion(c) as PrimarySizeCriterion;
                    if (size == null) continue;

                    var minMm = size.MinimumSize * 304.8;
                    var maxMm = size.MaximumSize * 304.8;
                    window = string.Format("{0:0.#} to {1:0.#} mm", minMm, maxMm);

                    if (minMm < lowestMm) lowestMm = minMm;
                    if (maxMm > highestMm) highestMm = maxMm;
                    sawSize = true;
                }
            }
            catch (Exception) { }

            findings.Add(string.Format("        {0,-28} {1}", window, partName));
            groupParts.Add(string.Format("{0}={1}", group, partName));
        }

        if (ruleCount == 0) groupParts.Add(string.Format("{0}=NONE", group));

        if (sawSize)
        {
            findings.Add(string.Format("        covered {0:0.#} to {1:0.#} mm - OUTSIDE this range "
                + "Revit inserts NOTHING, with no warning", lowestMm, highestMm));
        }
    }

    // One line per type. See the note beside `summary`.
    summaryRows.Add(string.Format("{0}: {1}", type.Name,
        string.Join(", ", groupParts.ToArray())));
}

if (emptyGroups.Count > 0)
{
    findings.Add(string.Format("{0} empty rule group(s) - a type with no rule for a group cannot "
        + "place that fitting, which is one cause of a run that will not close",
        emptyGroups.Count));
}

if (missingFamilies.Count > 0)
{
    findings.Add(string.Format("{0} rule(s) point at a family NOT LOADED in this project. The rules "
        + "are fine - load the families", missingFamilies.Count));
}

summary = string.Join("  ||  ", summaryRows.ToArray());

findings.Insert(0, string.Format("{0} pipe/duct type(s) reported; {1} empty group(s), {2} rule(s) "
    + "with a missing family, {3} thing(s) handed in that have no routing preferences",
    typesReported, emptyGroups.Count, missingFamilies.Count, notRoutable.Count));
