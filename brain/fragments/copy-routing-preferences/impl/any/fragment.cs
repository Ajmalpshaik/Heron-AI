// NOT STANDALONE. Assumes `doc`, `elements`, `sourceTypeName` and `groups`
// are in scope; leaves `rulesCopied`, `groupsCopied`, `typesTouched`,
// `summary`, `skipped`, `notRoutable`, `unverified` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16), so
// copying every group is one undo entry.
//
// A GROUP HOLDS MORE THAN ONE RULE. A real copper type carries a full tee AND
// a reduced tee under Junctions, covering different sizes. That is why this
// rebuilds a whole group rather than replacing one rule.
//
// SEGMENTS IS EXCLUDED UNLESS NAMED. The segment carries the MATERIAL, so
// copying it silently replaces what the type is made of - and on a type
// somebody has just pointed at their own segment, that undoes the work
// without a word.
//
// EVERYTHING IS RESOLVED BEFORE ANYTHING IS CLEARED. A group is rebuilt by
// removing every rule and adding new ones, so failing halfway leaves it EMPTY
// - and an empty group inserts no fitting at all, with no error. A group
// whose parts cannot all be resolved is left exactly as it was.
//
// THE SIZE WINDOW TRAVELS WITH THE RULE. A rebuilt rule with no criterion
// covers every size, and a size outside every rule's range gets NO fitting.
// Criteria stay in internal feet the whole way - no units call is made.

var findings = new List<string>();
var skipped = new List<string>();
var notRoutable = new List<string>();
var unverified = new List<string>();
var summaryRows = new List<string>();
var rulesCopied = 0;
var groupsCopied = 0;
var typesTouched = 0;
var summary = "";

var everyGroup = new Dictionary<string, RoutingPreferenceRuleGroupType>(
    StringComparer.OrdinalIgnoreCase)
{
    { "Segments", RoutingPreferenceRuleGroupType.Segments },
    { "Elbows", RoutingPreferenceRuleGroupType.Elbows },
    { "Junctions", RoutingPreferenceRuleGroupType.Junctions },
    { "Crosses", RoutingPreferenceRuleGroupType.Crosses },
    { "Transitions", RoutingPreferenceRuleGroupType.Transitions },
    { "Unions", RoutingPreferenceRuleGroupType.Unions },
    { "MechanicalJoints", RoutingPreferenceRuleGroupType.MechanicalJoints },
    { "Caps", RoutingPreferenceRuleGroupType.Caps },
};

var proceed = true;

// WHICH GROUPS. Empty means every FITTING group - Segments is material and is
// only copied when somebody names it. See the header.
var wantedGroups = new List<RoutingPreferenceRuleGroupType>();
var wantedNames = new List<string>();
var asked = (groups ?? "").Trim();

if (asked.Length == 0)
{
    foreach (var pair in everyGroup)
    {
        if (pair.Value == RoutingPreferenceRuleGroupType.Segments) continue;
        wantedGroups.Add(pair.Value);
        wantedNames.Add(pair.Key);
    }
}
else
{
    foreach (var piece in asked.Split(','))
    {
        var one = piece.Trim();
        if (one.Length == 0) continue;
        RoutingPreferenceRuleGroupType found;
        if (!everyGroup.TryGetValue(one, out found))
        {
            findings.Add(string.Format("'{0}' is not a routing preference group. It is one of: {1}",
                one, string.Join(", ", everyGroup.Keys.ToArray())));
            proceed = false;
            continue;
        }
        wantedGroups.Add(found);
        wantedNames.Add(one);
    }
}

if (proceed && wantedGroups.Count == 0)
{
    findings.Add("No group was named and none could be worked out, so nothing was copied.");
    proceed = false;
}

// THE SOURCE TYPE, BY NAME.
MEPCurveType sourceType = null;
if (proceed)
{
    var wantedSource = (sourceTypeName ?? "").Trim();
    if (wantedSource.Length == 0)
    {
        findings.Add("No source type was named. This copies FROM a type that already has the "
            + "fittings wanted, so there is no sensible default.");
        proceed = false;
    }
    else
    {
        try
        {
            foreach (var found in new FilteredElementCollector(doc)
                .OfClass(typeof(MEPCurveType)).ToElements())
            {
                var candidate = found as MEPCurveType;
                if (candidate == null || candidate.Name == null) continue;
                if (string.Equals(candidate.Name, wantedSource, StringComparison.OrdinalIgnoreCase))
                {
                    sourceType = candidate;
                    break;
                }
            }
        }
        catch (Exception) { }

        if (sourceType == null)
        {
            findings.Add(string.Format("No pipe or duct type is called '{0}' to copy from.",
                wantedSource));
            proceed = false;
        }
    }
}

RoutingPreferenceManager sourceManager = null;
if (proceed)
{
    try { sourceManager = sourceType.RoutingPreferenceManager; }
    catch (Exception) { sourceManager = null; }

    if (sourceManager == null)
    {
        findings.Add(string.Format("'{0}' has no routing preference manager to copy from.",
            sourceType.Name));
        proceed = false;
    }
}

// The targets, deduped to types - two pipes of one type are ONE type.
var seenTypes = new HashSet<ElementId>();
var targets = new List<MEPCurveType>();
if (proceed)
{
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
        if (type.Id.Equals(sourceType.Id)) continue;   // copying onto itself is nothing
        if (seenTypes.Add(type.Id)) targets.Add(type);
    }
}

foreach (var target in targets)
{
    RoutingPreferenceManager manager = null;
    try { manager = target.RoutingPreferenceManager; }
    catch (Exception) { manager = null; }

    if (manager == null)
    {
        notRoutable.Add(string.Format("'{0}' has no routing preference manager", target.Name));
        continue;
    }

    typesTouched++;
    var groupParts = new List<string>();

    for (var g = 0; g < wantedGroups.Count; g++)
    {
        var group = wantedGroups[g];
        var groupName = wantedNames[g];

        var sourceCount = 0;
        try { sourceCount = sourceManager.GetNumberOfRules(group); }
        catch (Exception) { sourceCount = 0; }

        if (sourceCount == 0)
        {
            groupParts.Add(string.Format("{0}=source has none, left alone", groupName));
            continue;
        }

        // READ AND RESOLVE EVERYTHING FIRST. See the header - clearing before
        // knowing the rules can be rebuilt is how a group ends up empty.
        var partIds = new List<ElementId>();
        var descriptions = new List<string>();
        var criteriaPerRule = new List<List<PrimarySizeCriterion>>();
        var wholeGroupOk = true;

        for (var i = 0; i < sourceCount; i++)
        {
            RoutingPreferenceRule rule = null;
            try { rule = sourceManager.GetRule(group, i); }
            catch (Exception) { rule = null; }

            if (rule == null)
            {
                skipped.Add(string.Format("'{0}' {1} rule {2} could not be read", sourceType.Name,
                    groupName, i));
                wholeGroupOk = false;
                break;
            }

            // A RULE POINTING AT AN UNLOADED FAMILY. Copying the id writes a
            // rule the target cannot use either, so it is named, not carried.
            var part = doc.GetElement(rule.MEPPartId);
            if (part == null)
            {
                skipped.Add(string.Format("'{0}' {1} rule {2} points at a family that is NOT "
                    + "LOADED, so it was not copied - load the family and run this again",
                    sourceType.Name, groupName, i));
                continue;
            }

            var criteria = new List<PrimarySizeCriterion>();
            try
            {
                for (var c = 0; c < rule.NumberOfCriteria; c++)
                {
                    var size = rule.GetCriterion(c) as PrimarySizeCriterion;
                    if (size == null) continue;
                    criteria.Add(new PrimarySizeCriterion(size.MinimumSize, size.MaximumSize));
                }
            }
            catch (Exception) { }

            partIds.Add(rule.MEPPartId);
            descriptions.Add(rule.Description);
            criteriaPerRule.Add(criteria);
        }

        if (!wholeGroupOk)
        {
            groupParts.Add(string.Format("{0}=LEFT ALONE, source unreadable", groupName));
            continue;
        }

        if (partIds.Count == 0)
        {
            groupParts.Add(string.Format("{0}=LEFT ALONE, nothing copyable", groupName));
            continue;
        }

        // Now, and only now, clear and rebuild.
        try
        {
            var existing = manager.GetNumberOfRules(group);
            for (var i = existing - 1; i >= 0; i--) manager.RemoveRule(group, i);

            for (var i = 0; i < partIds.Count; i++)
            {
                var rebuilt = new RoutingPreferenceRule(partIds[i], descriptions[i]);
                foreach (var criterion in criteriaPerRule[i]) rebuilt.AddCriterion(criterion);
                manager.AddRule(group, rebuilt);
            }
        }
        catch (Exception error)
        {
            unverified.Add(string.Format("'{0}' {1} could not be rebuilt: {2}", target.Name,
                groupName, error.Message));
            groupParts.Add(string.Format("{0}=FAILED", groupName));
            continue;
        }

        // READ BACK. See the header.
        var after = 0;
        try { after = manager.GetNumberOfRules(group); }
        catch (Exception) { after = -1; }

        if (after == partIds.Count)
        {
            groupsCopied++;
            rulesCopied += partIds.Count;
            groupParts.Add(string.Format("{0}={1} rule(s)", groupName, partIds.Count));
        }
        else
        {
            unverified.Add(string.Format("'{0}' {1} asked for {2} rule(s) and reads back {3}",
                target.Name, groupName, partIds.Count, after));
            groupParts.Add(string.Format("{0}=UNVERIFIED", groupName));
        }
    }

    summaryRows.Add(string.Format("{0}: {1}", target.Name,
        string.Join(", ", groupParts.ToArray())));
}

summary = string.Join("  ||  ", summaryRows.ToArray());

findings.Insert(0, string.Format("{0} rule(s) copied across {1} group(s) onto {2} type(s); {3} "
    + "source rule(s) skipped, {4} group(s) unverified",
    rulesCopied, groupsCopied, typesTouched, skipped.Count, unverified.Count));

if (rulesCopied > 0)
{
    findings.Add("Editing a type changes EVERY element of that type. One Ctrl+Z puts all of "
        + "this back in a single step.");
}
