// NOT STANDALONE. Assumes `doc`, `elements`, `group`, `partName` and
// `ruleIndex` are in scope; leaves `rulesChanged`, `typesTouched`,
// `alreadyCorrect`, `nearMisses`, `notRoutable`, `unverified` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16), so
// changing several types is one undo entry.
//
// THERE IS NO SetRule ON ANY RELEASE, AND MEPPartId IS READ-ONLY. Changing
// which part a rule points at is RemoveRule then AddRule at the same index, so
// the rule is REBUILT rather than edited - and whatever is not copied across
// is silently gone.
//
// THE SIZE WINDOW IS WHAT GETS LOST. A rule carries its criteria separately
// from its part, so a rebuilt rule with no criterion covers EVERY size. Where
// the old rule covered 15 to 50 mm that is not a smaller mistake than naming
// the wrong segment: a size outside every rule's range gets NO fitting at all,
// with no error and no warning. Every criterion is read off the old rule
// before it is removed, put back on the new one, and the count is reported.
//
// SIZES ARE NEVER CONVERTED. A criterion is read in Revit's internal feet and
// handed straight back in internal feet, so no units call is made - which is
// what keeps this clear of the 2021 units move.
//
// READ BACK, NEVER TRUST THE CALL. AddRule returns nothing and throws nothing
// when the part is wrong for the group, so the part id is read off the rule
// again afterwards and compared. That comparison is the fragment; the swap
// itself is two lines.
//
// A PART NAMED BUT NOT FOUND RETURNS THE NEAR MISSES. "Copper" and
// "Copper - K" are different segments, and choosing one for the caller is a
// confident wrong answer.

var findings = new List<string>();
var rulesChanged = 0;
var typesTouched = 0;
var alreadyCorrect = new List<string>();
var nearMisses = new List<string>();
var notRoutable = new List<string>();
var unverified = new List<string>();

// WHICH GROUP. The caller says the word. Undefined is Revit's own placeholder
// and is deliberately not offered - it names no rule anybody can mean.
var groupsByName = new Dictionary<string, RoutingPreferenceRuleGroupType>(
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

// EMPTY MEANS SEGMENTS, because the material question is what this is for and
// a caller who names no group means the one the type is made of.
var wantedGroupName = (group ?? "").Trim();
if (wantedGroupName.Length == 0) wantedGroupName = "Segments";

var wantedGroup = RoutingPreferenceRuleGroupType.Segments;
if (!groupsByName.TryGetValue(wantedGroupName, out wantedGroup))
{
    findings.Add(string.Format("'{0}' is not a routing preference group. It is one of: {1}",
        wantedGroupName, string.Join(", ", groupsByName.Keys.ToArray())));
    proceed = false;
}

var wantedName = (partName ?? "").Trim();
if (proceed && wantedName.Length == 0)
{
    findings.Add("No part was named. This needs the segment or fitting to point the rule at - "
        + "'Copper', 'Copper - K', an elbow family type - because there is no sensible default "
        + "for what a type should be made of.");
    proceed = false;
}

// THE PART, BY NAME. Segments are Segment elements; every other group points
// at a loaded family symbol, and looking in the wrong collection is how a
// perfectly real name comes back missing.
Element wantedPart = null;
if (proceed)
{
    var candidates = new List<Element>();
    try
    {
        if (wantedGroup == RoutingPreferenceRuleGroupType.Segments)
        {
            foreach (var found in new FilteredElementCollector(doc)
                .OfClass(typeof(Segment)).ToElements())
                candidates.Add(found);
        }
        else
        {
            foreach (var found in new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol)).ToElements())
                candidates.Add(found);
        }
    }
    catch (Exception) { }

    foreach (var candidate in candidates)
    {
        if (candidate == null || candidate.Name == null) continue;
        if (string.Equals(candidate.Name, wantedName, StringComparison.OrdinalIgnoreCase))
        {
            wantedPart = candidate;
            break;
        }
    }

    if (wantedPart == null)
    {
        foreach (var candidate in candidates)
        {
            if (candidate == null || candidate.Name == null) continue;
            if (candidate.Name.IndexOf(wantedName, StringComparison.OrdinalIgnoreCase) >= 0)
                nearMisses.Add(candidate.Name);
        }

        findings.Add(string.Format("No {0} part is called '{1}' in this project. {2}",
            wantedGroup, wantedName,
            nearMisses.Count == 0
                ? "Nothing came close either, so check the name in Manage > MEP Settings."
                : string.Format("{0} name(s) came close and are listed - say which one is meant.",
                    nearMisses.Count)));
        proceed = false;
    }
}

// Dedupe to types: an instance resolves to its type, a type is used as-is.
// Same reduction as REPORT_ROUTING_PREFERENCES, because two pipes of one type
// are ONE type and changing it twice is the same change.
var seenTypes = new HashSet<ElementId>();
var typesToChange = new List<MEPCurveType>();

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

        if (seenTypes.Add(type.Id)) typesToChange.Add(type);
    }
}

foreach (var type in typesToChange)
{
    RoutingPreferenceManager manager = null;
    try { manager = type.RoutingPreferenceManager; }
    catch (Exception) { manager = null; }

    if (manager == null)
    {
        notRoutable.Add(string.Format("'{0}' has no routing preference manager", type.Name));
        continue;
    }

    var ruleCount = 0;
    try { ruleCount = manager.GetNumberOfRules(wantedGroup); }
    catch (Exception) { ruleCount = 0; }

    // AN EMPTY GROUP IS NOT A RULE TO CHANGE. Adding one here would be
    // inventing a rule nobody asked for, with a size window this fragment
    // would have to guess - and guessing that window is the failure in the
    // header.
    if (ruleCount == 0)
    {
        findings.Add(string.Format("'{0}' has NO {1} rule, so there is nothing to point "
            + "elsewhere. A rule has to exist before it can be changed.", type.Name, wantedGroup));
        continue;
    }

    if (ruleIndex < 0 || ruleIndex >= ruleCount)
    {
        findings.Add(string.Format("'{0}' has {1} {2} rule(s), so rule {3} is not one of them",
            type.Name, ruleCount, wantedGroup, ruleIndex));
        continue;
    }

    RoutingPreferenceRule old = null;
    try { old = manager.GetRule(wantedGroup, ruleIndex); }
    catch (Exception) { old = null; }

    if (old == null)
    {
        unverified.Add(string.Format("'{0}' {1} rule {2} could not be read at all",
            type.Name, wantedGroup, ruleIndex));
        continue;
    }

    typesTouched++;

    // ALREADY POINTING THERE IS NOT A CHANGE, and not a failure either (D-52).
    if (old.MEPPartId != null && old.MEPPartId.Equals(wantedPart.Id))
    {
        alreadyCorrect.Add(string.Format("'{0}' {1} rule {2} already uses '{3}'",
            type.Name, wantedGroup, ruleIndex, wantedPart.Name));
        continue;
    }

    var oldPart = doc.GetElement(old.MEPPartId);
    var oldName = oldPart == null ? "<not loaded>" : oldPart.Name;

    // THE CRITERIA COME OFF FIRST. See the header - this is the whole reason
    // the rule is rebuilt rather than replaced outright.
    var criteria = new List<PrimarySizeCriterion>();
    var criteriaSeen = 0;
    try
    {
        criteriaSeen = old.NumberOfCriteria;
        for (var c = 0; c < criteriaSeen; c++)
        {
            var size = old.GetCriterion(c) as PrimarySizeCriterion;
            if (size == null) continue;
            criteria.Add(new PrimarySizeCriterion(size.MinimumSize, size.MaximumSize));
        }
    }
    catch (Exception) { }

    // A CRITERION THAT IS NOT A SIZE CANNOT BE COPIED, and saying so is the
    // difference between a carried rule and a quietly narrowed one.
    if (criteriaSeen > criteria.Count)
    {
        findings.Add(string.Format("'{0}' {1} rule {2} had {3} criterion(s) that are not size "
            + "windows and could NOT be carried across - check the rule in Revit",
            type.Name, wantedGroup, ruleIndex, criteriaSeen - criteria.Count));
    }

    var rebuilt = new RoutingPreferenceRule(wantedPart.Id, old.Description);
    foreach (var criterion in criteria) rebuilt.AddCriterion(criterion);

    try
    {
        manager.RemoveRule(wantedGroup, ruleIndex);
        manager.AddRule(wantedGroup, rebuilt, ruleIndex);
    }
    catch (Exception error)
    {
        unverified.Add(string.Format("'{0}' {1} rule {2} could not be rebuilt: {3}",
            type.Name, wantedGroup, ruleIndex, error.Message));
        continue;
    }

    // READ BACK. See the header.
    RoutingPreferenceRule after = null;
    try { after = manager.GetRule(wantedGroup, ruleIndex); }
    catch (Exception) { after = null; }

    if (after != null && after.MEPPartId != null && after.MEPPartId.Equals(wantedPart.Id))
    {
        rulesChanged++;
        findings.Add(string.Format("'{0}' {1} rule {2}: '{3}' -> '{4}', {5} size window(s) "
            + "carried across", type.Name, wantedGroup, ruleIndex, oldName, wantedPart.Name,
            criteria.Count));
    }
    else
    {
        unverified.Add(string.Format("'{0}' {1} rule {2} did not read back as '{3}' - the call "
            + "returned without complaint and the rule still does not point there",
            type.Name, wantedGroup, ruleIndex, wantedPart.Name));
    }
}

findings.Insert(0, string.Format("{0} rule(s) changed across {1} type(s) examined; {2} already "
    + "correct, {3} unverified, {4} thing(s) with no routing preferences",
    rulesChanged, typesTouched, alreadyCorrect.Count, unverified.Count, notRoutable.Count));

if (rulesChanged > 0)
{
    findings.Add("Editing a type changes EVERY element of that type, not only the ones handed "
        + "in. One Ctrl+Z puts all of this back in a single step.");
}
