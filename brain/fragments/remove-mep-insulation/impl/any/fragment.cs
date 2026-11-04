// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `removed`,
// `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry,
// and the caller owns it. Each host's delete runs inside a SubTransaction of
// that transaction, so a host whose delete would take more than its own
// insulation is put back on its own and the rest still go.
//
// REVIT'S REMOVE INSULATION. Ducts, pipes, their fittings, accessories and
// flex carry insulation as separate elements that name the host they wrap.
// This deletes those elements and only those. It never deletes the host, and
// it never touches LINING - internal lining is a different element doing a
// different job, and removing it is a separate request.
//
// THE DELETE IS ASKED IN REVIT FIRST (D-142, Article 7). Anything a kept change
// deletes from the model is put to the modeller in a box of its own before it
// stays, whatever Ask to Keep says - so this card takes no `confirm` of its
// own. DELETE_REVISION carries one because a find picks its revision; here the
// modeller picked the ducts.
//
// WHAT WENT IS WHAT Document.Delete RETURNED. Anything Revit would delete with
// the insulation is checked before it is kept: a tag on the insulation is
// view-specific and goes with it, as it does from the ribbon, and is counted.
// Anything else - above all the host itself - puts that host's delete back
// and names it. The insulation removed is read back afterwards, never assumed
// from the call.
//
// AN ELEMENT THAT CANNOT CARRY INSULATION IS NAMED, NOT SKIPPED. A wall, a
// terminal or a piece of equipment in the selection is a wrong selection, and
// it must not read the same as a duct that had nothing on it. Insulation
// picked directly (Tab-selected in Revit) is taken as its host's.

var removed = 0;
var report = "";
// ONE STRING, not a list: a list reaches the reply cut to sixty characters an
// item. Set only when NOTHING was removed.
string refused = null;
var findings = new List<string>();

var lines = new List<string>();
var bare = new List<string>();
var notInsulable = new List<string>();
var lining = new List<string>();
var problems = new List<string>();
var alsoWentTotal = 0;

// The eight categories Revit's Remove Insulation acts on. Compared as
// ElementIds built from the BuiltInCategory - never as numbers.
var insulable = new List<ElementId>
{
    new ElementId(BuiltInCategory.OST_DuctCurves),
    new ElementId(BuiltInCategory.OST_DuctFitting),
    new ElementId(BuiltInCategory.OST_DuctAccessory),
    new ElementId(BuiltInCategory.OST_FlexDuctCurves),
    new ElementId(BuiltInCategory.OST_PipeCurves),
    new ElementId(BuiltInCategory.OST_PipeFitting),
    new ElementId(BuiltInCategory.OST_PipeAccessory),
    new ElementId(BuiltInCategory.OST_FlexPipeCurves),
};

Func<Element, string> describe = element =>
{
    if (element == null) return "an element no longer in the model";
    var category = element.Category == null ? "element" : element.Category.Name;
    var name = "";
    try { name = element.Name ?? ""; } catch (Exception) { name = ""; }
    return name.Length > 0
        ? string.Format("{0} \"{1}\" (id {2})", category, name, element.Id)
        : string.Format("{0} (id {1})", category, element.Id);
};

Func<Element, bool> isInsulation = element => element is DuctInsulation || element is PipeInsulation;

// ---- which hosts ------------------------------------------------------------

var hosts = new List<ElementId>();
var pickedDirectly = 0;

if (elements != null)
{
    foreach (var element in elements)
    {
        if (element == null) continue;

        ElementId hostId = null;

        if (isInsulation(element))
        {
            try { hostId = ((InsulationLiningBase)element).HostElementId; }
            catch (Exception) { hostId = null; }
            if (hostId == null || hostId == ElementId.InvalidElementId)
            {
                problems.Add(describe(element) + ": names no host, so it was left alone");
                continue;
            }
            pickedDirectly++;
        }
        else if (element is DuctLining)
        {
            lining.Add(describe(element));
            continue;
        }
        else
        {
            var categoryId = element.Category == null ? null : element.Category.Id;
            var fits = false;
            if (categoryId != null)
            {
                foreach (var one in insulable)
                {
                    if (one == categoryId) { fits = true; break; }
                }
            }
            if (!fits)
            {
                notInsulable.Add(describe(element));
                continue;
            }
            hostId = element.Id;
        }

        var already = false;
        foreach (var known in hosts)
        {
            if (known == hostId) { already = true; break; }
        }
        if (!already) hosts.Add(hostId);
    }
}

// ---- remove, one host at a time ---------------------------------------------

foreach (var hostId in hosts)
{
    var host = doc.GetElement(hostId);
    var hostName = describe(host);

    // ONLY INSULATION. GetInsulationIds names insulation and not lining on
    // every release, and each id is checked again by type before it is
    // deleted - a lining handed back here would be a second job done unasked.
    var asked = new List<ElementId>();
    try
    {
        var found = InsulationLiningBase.GetInsulationIds(doc, hostId);
        if (found != null)
        {
            foreach (var id in found)
            {
                if (isInsulation(doc.GetElement(id))) asked.Add(id);
            }
        }
    }
    catch (Exception ex)
    {
        problems.Add(hostName + ": Revit would not list its insulation - " + ex.Message);
        continue;
    }

    if (asked.Count == 0)
    {
        bare.Add(hostName);
        continue;
    }

    // What Revit is allowed to take with the insulation: its own view-specific
    // dependents - a tag on the insulation. Read BEFORE the delete, because
    // afterwards there is nothing left to ask.
    var mayGoWith = new List<ElementId>();
    foreach (var id in asked)
    {
        try
        {
            // An id Revit no longer resolves is skipped, not dereferenced into the
            // catch-all below.
            var owner = doc.GetElement(id);
            if (owner == null) continue;
            var dependents = owner.GetDependentElements(null);
            if (dependents == null) continue;
            foreach (var dependentId in dependents)
            {
                if (asked.Contains(dependentId)) continue;
                var dependent = doc.GetElement(dependentId);
                if (dependent != null && dependent.ViewSpecific) mayGoWith.Add(dependentId);
            }
        }
        catch (Exception) { }
    }

    var alsoWent = 0;
    string putBack = null;

    using (var attempt = new SubTransaction(doc))
    {
        try
        {
            attempt.Start();
            var deleted = doc.Delete(asked);
            var unexpected = 0;
            if (deleted != null)
            {
                foreach (var id in deleted)
                {
                    if (asked.Contains(id)) continue;
                    if (id == hostId || hosts.Contains(id) || !mayGoWith.Contains(id)) unexpected++;
                    else alsoWent++;
                }
            }

            if (unexpected > 0)
            {
                attempt.RollBack();
                putBack = string.Format("{0}: NOTHING removed - Revit would also have deleted {1} "
                    + "other element(s) with its insulation, so it was put back", hostName, unexpected);
            }
            else
            {
                attempt.Commit();
            }
        }
        catch (Exception ex)
        {
            try { attempt.RollBack(); } catch (Exception) { }
            putBack = hostName + ": NOTHING removed - Revit refused: " + ex.Message;
        }
    }

    if (putBack != null)
    {
        problems.Add(putBack);
        continue;
    }

    // READ BACK: what is gone, and what is still on the host.
    var gone = 0;
    foreach (var id in asked)
    {
        var still = doc.GetElement(id);
        if (still == null || !still.IsValidObject) gone++;
    }
    var left = 0;
    try
    {
        var after = InsulationLiningBase.GetInsulationIds(doc, hostId);
        left = after == null ? 0 : after.Count;
    }
    catch (Exception) { }

    removed += gone;
    alsoWentTotal += alsoWent;

    var line = string.Format("{0}: {1} insulation element(s) removed", hostName, gone);
    if (alsoWent > 0) line += string.Format(", and {0} tag(s) on it went with it", alsoWent);
    if (left > 0) line += string.Format(" - {0} STILL on it afterwards", left);
    lines.Add(line);
}

// ---- the answer -------------------------------------------------------------

var all = new List<string>(lines);
foreach (var one in bare) all.Add(one + ": had no insulation - nothing removed");
report = string.Join("\n", all.ToArray());

findings.Add(string.Format("{0} insulation element(s) removed from {1} of {2} duct, pipe, fitting, "
    + "accessory or flex element(s); {3} had none", removed, lines.Count, hosts.Count, bare.Count));

if (pickedDirectly > 0)
    findings.Add(string.Format("{0} of the selected item(s) were insulation picked directly, and are "
        + "reported under the element they wrapped", pickedDirectly));

if (alsoWentTotal > 0)
    findings.Add(string.Format("{0} tag(s) on the removed insulation went with it, as they do when "
        + "Remove Insulation is pressed in Revit", alsoWentTotal));

foreach (var one in problems) findings.Add(one);

foreach (var one in notInsulable)
    findings.Add("Not a duct, pipe, fitting, accessory or flex - cannot carry insulation, left alone: " + one);

foreach (var one in lining)
    findings.Add("Lining, not insulation - left alone, removing lining is a separate job: " + one);

if (removed > 0)
    findings.Add("The ducts and pipes themselves are untouched, and so is any lining. A run that was "
        + "clash-checked with insulation on now needs less space than it was checked with");

if (removed == 0)
{
    if (elements == null || elements.Count == 0)
        refused = "Nothing was selected - select the ducts, pipes or fittings to take the insulation off";
    else if (hosts.Count == 0)
        refused = string.Format("None of the {0} selected element(s) is a duct, pipe, fitting, accessory "
            + "or flex, so there was no insulation to remove", elements.Count);
    else if (problems.Count == 0)
        refused = string.Format("None of the {0} duct, pipe, fitting, accessory or flex element(s) "
            + "selected had insulation on it - nothing was removed", hosts.Count);
    else
        refused = string.Format("Nothing was removed: {0} element(s) could not be done - the findings "
            + "name each and why", problems.Count);
}
