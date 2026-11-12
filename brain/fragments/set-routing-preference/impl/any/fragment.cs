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
// READ BACK, NEVER TRUST THE CALL. AddRule returns nothing. This said "and
// throws nothing when the part is wrong for the group" until 2026-11-11; that
// was never measured, and Revit's own reference says the opposite on every
// release 2020 to 2027 - ArgumentException, "the rule is not valid for this
// group (e.g. an elbow may not be added to the junction group)". Which Revit
// really does is still not measured, so BOTH are handled the same way: a
// throw, or a rule that does not read back, takes the swap back. The part id
// AND the number of rules are read again afterwards - see version 2 below.
//
// A PART NAMED BUT NOT FOUND RETURNS THE NEAR MISSES. "Copper" and
// "Copper - K" are different segments, and choosing one for the caller is a
// confident wrong answer.
//
// ===========================================================================
// VERSION 2 (FRAGMENT-ISSUES 5b-1120 and 5b-1549).
// ===========================================================================
//
// A FITTING IS A FAMILY AND A TYPE, AND THE TYPE NAME ALONE DOES NOT SAY WHICH.
// "Standard" is the type name of a mitred elbow and of a balancing damper in
// one ordinary project, and version 1 took whichever of them the collector met
// first, wrote it into the rule and reported a change. A fitting is now named
// the way Revit's type selector writes it - "Family: Type", with or without a
// space round the colon, the spelling the binder takes for a type - or by its
// type name alone when ONE loaded family has a type of that name. A type name
// two families share is refused, and each is listed as "Family: Type".
//
// A REFUSED SWAP IS PUT BACK. RemoveRule comes before AddRule because there is
// no SetRule, and until version 2 a refusal after it - AddRule throwing, or
// the read-back not showing the part - left the type with its rule REMOVED:
// nothing here threw, so the caller's transaction was kept. The two calls and
// the read-back now sit in one SubTransaction, kept only when the rule reads
// back as asked and rolled back otherwise, so a refusal leaves that type's
// routing preferences exactly as they were (Constitution Article 8).
//
// "READS BACK AS ASKED" IS THE PART AND THE COUNT. If AddRule added nothing
// and the NEXT rule already pointed at the part asked for, the rule now at
// that index reads as asked while one rule has been deleted. So the group
// must also hold as many rules as it did before, or the swap is taken back.
//
// "PUT BACK" IS READ, NEVER SAID ON TRUST. After a swap is rolled back the
// rule is read again - how many rules, which part, how many criteria - and
// only when all three read as they did is the type named in `unverified` as
// put back. If they do not, this throws: the add-in then rolls the WHOLE job
// back and says so, which is the one thing left that can still restore it.
//
// SEVERAL TYPES ARE NOT ALL-OR-NOTHING, and that is said in the answer. Each
// type's swap is its own SubTransaction, as each column is in ATTACH_COLUMNS
// and each duct in CONVERT_TO_FLEX_DUCT: a type whose swap is refused is put
// back and named, and the types already changed STAY changed. It is still one
// Undo entry. Only a put-back that does not read as before stops everything.

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
//
// ---- PART NAMES: plain text only, no Revit type ----------------------------
// tests/test_binding_note.py compiles everything between this marker line and
// END PART NAMES into Heron.BindingNote.TestHost and asks it row 5b-1120's own
// names, beside the binder's rule - so the matching is tested with no Revit.
// The rule for keeping a swap sits here too, for the same reason (5b-1549).
// Keep both marker lines, and keep every Revit type out from between them.
//
// A name fits a part when it is the part's own name, or "Family: Type" for a
// part that has a family. Revit allows no colon in a family or type name, so
// the first colon is the divider and the spaces round it carry nothing.
// Capitals do not count, as they never did here.
Func<string, string, string, bool> namesPart = (family, name, wanted) =>
{
    if (name == null || wanted == null) return false;
    if (string.Equals(name, wanted, StringComparison.OrdinalIgnoreCase)) return true;
    var colon = wanted.IndexOf(':');
    if (colon < 0 || string.IsNullOrEmpty(family)) return false;
    return string.Equals(wanted.Substring(0, colon).Trim(), family, StringComparison.OrdinalIgnoreCase)
        && string.Equals(wanted.Substring(colon + 1).Trim(), name, StringComparison.OrdinalIgnoreCase);
};

// Every part the name fits, by position. ONE is the part; two or more is a
// name that does not say which; none is a name nothing has.
Func<IList<string>, IList<string>, string, List<int>> partsNamed = (families, names, wanted) =>
{
    var fits = new List<int>();
    for (var i = 0; i < names.Count; i++)
        if (namesPart(families[i], names[i], wanted)) fits.Add(i);
    return fits;
};

// A swap is kept only when the group holds as many rules as before AND the
// rule at the index points at the part asked for. The count is what catches a
// rule that was removed and never added again while its neighbour happened to
// point at the same part.
Func<int, int, bool, bool> swapHeld = (rulesBefore, rulesAfter, pointsAtWanted) =>
    rulesAfter == rulesBefore && pointsAtWanted;
// ---- END PART NAMES ---------------------------------------------------------

// A part the way Revit's type selector writes it: "Family: Type" for a
// fitting, the name alone for a segment.
Func<Element, string> partLabel = part =>
{
    if (part == null) return "<not loaded>";
    var symbol = part as FamilySymbol;
    var family = "";
    if (symbol != null)
    {
        try { family = symbol.FamilyName ?? ""; }
        catch (Exception) { family = ""; }
    }
    return family.Length == 0 ? part.Name : family + ": " + part.Name;
};

// A LIST A PERSON CAN READ. "Standard" is the type name of dozens of loaded
// families in an ordinary project, and every loaded family is looked through
// because Revit has no list of what a group will take. So what is listed is
// capped, duct and pipe fittings first - the ones a routing rule is likeliest
// to mean - then by name, and the answer says how many were left off.
var listLimit = 12;
var ductFittings = new ElementId(BuiltInCategory.OST_DuctFitting);
var pipeFittings = new ElementId(BuiltInCategory.OST_PipeFitting);
Func<Element, bool> isFitting = part =>
{
    try
    {
        var category = part.Category;
        return category != null && (category.Id == ductFittings || category.Id == pipeFittings);
    }
    catch (Exception) { return false; }
};
Func<List<Element>, int> listParts = which =>
{
    var ordered = which
        .OrderBy(part => isFitting(part) ? 0 : 1)
        .ThenBy(part => partLabel(part), StringComparer.OrdinalIgnoreCase)
        .ToList();
    foreach (var each in ordered.Take(listLimit)) nearMisses.Add(partLabel(each));
    return Math.Max(0, ordered.Count - listLimit);
};
Func<int, string> leftOff = more => more == 0
    ? "They are listed"
    : string.Format("The first {0} are listed, duct and pipe fittings first, and {1} more are not",
        listLimit, more);

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

    // Three lists, position for position: the part, its family ("" for a
    // segment) and its own name. Only the two text lists go to the matching.
    var parts = new List<Element>();
    var partFamilies = new List<string>();
    var partNames = new List<string>();
    foreach (var candidate in candidates)
    {
        if (candidate == null || candidate.Name == null) continue;
        var asSymbol = candidate as FamilySymbol;
        var familyName = "";
        if (asSymbol != null)
        {
            try { familyName = asSymbol.FamilyName ?? ""; }
            catch (Exception) { familyName = ""; }
        }
        parts.Add(candidate);
        partFamilies.Add(familyName);
        partNames.Add(candidate.Name);
    }

    var fitting = partsNamed(partFamilies, partNames, wantedName);
    if (fitting.Count == 1)
    {
        wantedPart = parts[fitting[0]];
    }
    else if (fitting.Count > 1)
    {
        // TWO PARTS OF ONE NAME ARE NEVER CHOSEN BETWEEN. Version 1 took the
        // first, and which came first was the collector's order.
        var several = new List<Element>();
        foreach (var at in fitting) several.Add(parts[at]);
        var unlisted = listParts(several);

        // Only a bare type name can fit twice - "Family: Type" is one type - so
        // for a fitting the parts that fit are types of different families,
        // and not all of them need be fittings: every loaded family is looked
        // through, because Revit has no list of what a group will take.
        findings.Add(wantedGroup == RoutingPreferenceRuleGroupType.Segments
            ? string.Format("{0} segments are called '{1}', so the name does not say which one is "
                + "meant, and nothing was changed. {2}.", fitting.Count, wantedName, leftOff(unlisted))
            : string.Format("'{0}' is the type name of {1} loaded families, so it does not say which "
                + "one is meant, and nothing was changed. {2} - name the one meant as 'Family: Type', "
                + "the way Revit's type selector writes it.", wantedName, fitting.Count,
                leftOff(unlisted)));
        proceed = false;
    }
    else
    {
        var close = new List<Element>();
        foreach (var candidate in parts)
        {
            if (partLabel(candidate).IndexOf(wantedName, StringComparison.OrdinalIgnoreCase) >= 0)
                close.Add(candidate);
        }
        var closeUnlisted = listParts(close);

        findings.Add(string.Format("No {0} part is called '{1}' in this project. {2}",
            wantedGroup, wantedName,
            close.Count == 0
                ? "Nothing came close either, so check the name in Manage > MEP Settings."
                : string.Format("{0} name(s) came close. {1} - say which one is meant.",
                    close.Count, leftOff(closeUnlisted))));
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
            type.Name, wantedGroup, ruleIndex, partLabel(wantedPart)));
        continue;
    }

    var oldPartId = old.MEPPartId;
    var oldName = partLabel(doc.GetElement(oldPartId));

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

    // THE SWAP, IN A SUBTRANSACTION OF ITS OWN. See the header - a refusal
    // after RemoveRule must not leave the rule removed.
    string notSwapped = null;
    using (var swap = new SubTransaction(doc))
    {
        try
        {
            swap.Start();
            manager.RemoveRule(wantedGroup, ruleIndex);
            manager.AddRule(wantedGroup, rebuilt, ruleIndex);

            // READ BACK, BEFORE IT IS KEPT: the part at the index AND how many
            // rules the group holds. See the header.
            RoutingPreferenceRule after = null;
            try { after = manager.GetRule(wantedGroup, ruleIndex); }
            catch (Exception) { after = null; }
            var rulesAfter = -1;
            try { rulesAfter = manager.GetNumberOfRules(wantedGroup); }
            catch (Exception) { rulesAfter = -1; }
            var pointsAtWanted = after != null && after.MEPPartId != null
                && after.MEPPartId.Equals(wantedPart.Id);

            if (swapHeld(ruleCount, rulesAfter, pointsAtWanted))
            {
                swap.Commit();
            }
            else
            {
                swap.RollBack();
                notSwapped = pointsAtWanted
                    ? string.Format("left the group with {0} rule(s) where it had {1}",
                        rulesAfter < 0 ? "an unreadable number of" : rulesAfter.ToString(), ruleCount)
                    : string.Format("did not read back as '{0}' - Revit took the call without "
                        + "complaint and the rule did not point there", partLabel(wantedPart));
            }
        }
        catch (Exception error)
        {
            if (swap.HasStarted() && !swap.HasEnded()) swap.RollBack();
            notSwapped = "could not be rebuilt: " + error.Message;
        }
    }

    if (notSwapped != null)
    {
        // PUT BACK IS READ, NOT SAID ON TRUST. See the header. Outside every
        // try on purpose: what is thrown here has to leave the fragment.
        var rulesNow = -1;
        var partNow = ElementId.InvalidElementId;
        var criteriaNow = -1;
        try
        {
            var managerNow = type.RoutingPreferenceManager;
            rulesNow = managerNow.GetNumberOfRules(wantedGroup);
            var ruleNow = managerNow.GetRule(wantedGroup, ruleIndex);
            partNow = ruleNow.MEPPartId;
            criteriaNow = ruleNow.NumberOfCriteria;
        }
        catch (Exception) { }

        var asBefore = rulesNow == ruleCount && partNow != null && partNow.Equals(oldPartId)
            && criteriaNow == criteriaSeen;
        if (!asBefore)
        {
            throw new InvalidOperationException(string.Format("'{0}' {1} rule {2} {3}, and after "
                + "that was taken back the rule does NOT read as it did before: {4} rule(s) where "
                + "there were {5}, {6} criterion(s) where there were {7}, and it {8} '{9}'. Heron "
                + "stopped here so that the whole job is rolled back - check the type's Routing "
                + "Preferences in Revit before anything else is done to it.",
                type.Name, wantedGroup, ruleIndex, notSwapped,
                rulesNow < 0 ? "an unreadable number of" : rulesNow.ToString(), ruleCount,
                criteriaNow < 0 ? "an unreadable number of" : criteriaNow.ToString(), criteriaSeen,
                partNow != null && partNow.Equals(oldPartId) ? "still points at" : "no longer points at",
                oldName));
        }

        unverified.Add(string.Format("'{0}' {1} rule {2} {3}. The rule was put back and read "
            + "again: {4} rule(s), {5} size window(s), still pointing at '{6}', as before.",
            type.Name, wantedGroup, ruleIndex, notSwapped, rulesNow, criteriaNow, oldName));
        continue;
    }

    rulesChanged++;
    findings.Add(string.Format("'{0}' {1} rule {2}: '{3}' -> '{4}', {5} size window(s) "
        + "carried across", type.Name, wantedGroup, ruleIndex, oldName, partLabel(wantedPart),
        criteria.Count));
}

findings.Insert(0, string.Format("{0} rule(s) changed across {1} type(s) examined; {2} already "
    + "correct, {3} unverified, {4} thing(s) with no routing preferences",
    rulesChanged, typesTouched, alreadyCorrect.Count, unverified.Count, notRoutable.Count));

if (rulesChanged > 0)
{
    findings.Add("Editing a type changes EVERY element of that type, not only the ones handed "
        + "in. One Ctrl+Z puts all of this back in a single step.");
}

// NOT ALL-OR-NOTHING, SAID. See the header.
if (rulesChanged > 0 && unverified.Count > 0)
{
    findings.Add(string.Format("{0} rule(s) were changed and are part of this job; {1} could not "
        + "be and were put back as they were. The job is not all-or-nothing: keeping it keeps the "
        + "{0} that changed, and one Undo takes them back.", rulesChanged, unverified.Count));
}
