// NOT STANDALONE. Assumes `doc`, `elements`, `thicknessMm` and `liningType`
// are in scope; leaves `added`, `changed`, `removed`, `unchanged`, `report`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry, and
// the caller owns it. Each host runs inside a SubTransaction of that
// transaction, so a host Revit refuses - or whose remove would take more than
// its own lining - is put back on its own and the rest still go.
//
// REVIT'S ADD / EDIT / REMOVE LINING, AS ONE JOB. Lining is the duct's own
// internal layer, and its thickness is the one value all three buttons set:
//   thicknessMm > 0, host unlined  -> Add Lining at that thickness
//   thicknessMm > 0, host lined    -> Edit Lining: the thickness (and the type,
//                                     when one is named) changed IN PLACE -
//                                     never deleted and made again
//   thicknessMm = 0                -> Remove Lining: the lining elements only
//
// NEVER INSULATION. Insulation is a different element on the outside of the
// duct; this neither reads nor writes it. Insulation handed in - Tab-selected -
// is named and left. Lining is found with GetLiningIds and every id is checked
// again by type before anything is done to it.
//
// PIPES HAVE NO LINING IN REVIT. A pipe, pipe fitting, pipe accessory or flex
// pipe is refused BY NAME, so a pipe selection does not read the same as a
// duct that needed nothing. Flex duct is named too: Revit's Add Lining is for
// ducts, duct fittings and duct accessories.
//
// THE LINING TYPE. `liningType` names a Duct Lining type, by its name or as
// "Family: Type". Left out it arrives as "keep": a lined host keeps its own
// type, and an unlined one takes the model's lining type ONLY when the model
// has exactly one - with several, that host is refused and the types named,
// rather than one chosen for the modeller.
//
// MILLIMETRES TO FEET IS ARITHMETIC (D-20): 1 ft = 304.8 mm exactly.
//
// WHAT IS REPORTED IS READ BACK. After each host, its lining is listed again
// and each layer's thickness and type read off the element - never assumed
// from the call that set it.

var added = 0;
var changed = 0;
var removed = 0;
var unchanged = 0;
var report = "";
// ONE STRING, not a list: a list reaches the reply cut to sixty characters an
// item. Set only when NOTHING was added, changed or removed.
string refused = null;
var findings = new List<string>();

const double MillimetresPerFoot = 304.8;
// Half a hundredth of a millimetre: closer than that is the thickness asked.
const double SameMm = 0.005;

var lines = new List<string>();
var skipped = new List<string>();
var pipes = new List<string>();
var flex = new List<string>();
var notDuct = new List<string>();
var insulation = new List<string>();
var problems = new List<string>();
var alsoWentTotal = 0;
var pickedDirectly = 0;

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

Func<double, string> mm = feet => (feet * MillimetresPerFoot).ToString("0.#") + " mm";

Func<ElementId, string> typeName = id =>
{
    var type = id == null ? null : doc.GetElement(id);
    if (type == null) return "no type";
    try { return type.Name ?? "a lining type"; } catch (Exception) { return "a lining type"; }
};

// The three categories Revit's Add Lining acts on, and the ones named as
// unable to carry it. Compared as ElementIds built from the BuiltInCategory -
// never as numbers.
var lineable = new List<ElementId>
{
    new ElementId(BuiltInCategory.OST_DuctCurves),
    new ElementId(BuiltInCategory.OST_DuctFitting),
    new ElementId(BuiltInCategory.OST_DuctAccessory),
};
var pipeCategories = new List<ElementId>
{
    new ElementId(BuiltInCategory.OST_PipeCurves),
    new ElementId(BuiltInCategory.OST_PipeFitting),
    new ElementId(BuiltInCategory.OST_PipeAccessory),
    new ElementId(BuiltInCategory.OST_FlexPipeCurves),
};
var flexDuct = new ElementId(BuiltInCategory.OST_FlexDuctCurves);

Func<List<ElementId>, ElementId, bool> holds = (list, id) =>
{
    if (id == null) return false;
    foreach (var one in list) if (one == id) return true;
    return false;
};

// ---- what was asked ---------------------------------------------------------

var removing = false;
double thickness = 0;
string askedProblem = null;

if (double.IsNaN(thicknessMm) || double.IsInfinity(thicknessMm) || thicknessMm < 0)
{
    askedProblem = string.Format("A lining thickness of {0} mm is not one Revit can take - give a "
        + "thickness in millimetres, or 0 to remove the lining", thicknessMm);
}
else if (thicknessMm == 0)
{
    removing = true;
}
else
{
    thickness = thicknessMm / MillimetresPerFoot;
    if (!InsulationLiningBase.IsValidThickness(thickness))
        askedProblem = string.Format("Revit will not take a lining {0} mm thick", thicknessMm);
}

// The lining type: named, or "keep".
var said = (liningType ?? "").Trim();
var keepType = said.Length == 0 || string.Equals(said, "keep", StringComparison.OrdinalIgnoreCase);
ElementId namedType = null;
var allTypes = new List<Element>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(DuctLiningType)))
{
    if (element != null) allTypes.Add(element);
}
var typeNames = new List<string>();
foreach (var one in allTypes)
{
    try { typeNames.Add("\"" + one.Name + "\""); } catch (Exception) { }
}
var typeChoices = typeNames.Count == 0
    ? "this model has no Duct Lining type"
    : "the model's lining types are " + string.Join(", ", typeNames.ToArray());

if (askedProblem == null && !removing && !keepType)
{
    var matches = new List<Element>();
    foreach (var one in allTypes)
    {
        string name = "", family = "";
        try { name = one.Name ?? ""; } catch (Exception) { }
        try { family = ((ElementType)one).FamilyName ?? ""; } catch (Exception) { }
        if (string.Equals(name, said, StringComparison.OrdinalIgnoreCase)
            || string.Equals(family + ": " + name, said, StringComparison.OrdinalIgnoreCase)
            || string.Equals(family + " : " + name, said, StringComparison.OrdinalIgnoreCase))
            matches.Add(one);
    }
    if (matches.Count == 1) namedType = matches[0].Id;
    else if (matches.Count == 0)
        askedProblem = string.Format("No Duct Lining type called \"{0}\" - {1}", said, typeChoices);
    else
        askedProblem = string.Format("{0} Duct Lining types are called \"{1}\" - name it as "
            + "\"Family: Type\"", matches.Count, said);
}

// The type an unlined host takes when none is named: the model's only one.
ElementId onlyType = allTypes.Count == 1 ? allTypes[0].Id : null;

// ---- which hosts ------------------------------------------------------------

var hosts = new List<ElementId>();

if (askedProblem == null && elements != null)
{
    foreach (var element in elements)
    {
        if (element == null) continue;

        ElementId hostId = null;
        var categoryId = element.Category == null ? null : element.Category.Id;

        if (element is DuctLining)
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
        else if (element is DuctInsulation || element is PipeInsulation)
        {
            insulation.Add(describe(element));
            continue;
        }
        else if (holds(pipeCategories, categoryId))
        {
            pipes.Add(describe(element));
            continue;
        }
        else if (categoryId != null && categoryId == flexDuct)
        {
            flex.Add(describe(element));
            continue;
        }
        else if (!holds(lineable, categoryId))
        {
            notDuct.Add(describe(element));
            continue;
        }
        else
        {
            hostId = element.Id;
        }

        if (!holds(hosts, hostId)) hosts.Add(hostId);
    }
}

// ---- one host at a time -----------------------------------------------------

foreach (var hostId in hosts)
{
    var host = doc.GetElement(hostId);
    var hostName = describe(host);

    // ONLY LINING. GetLiningIds names lining and not insulation; each id is
    // checked by type again, because touching insulation here would be a
    // second job done unasked.
    var lining = new List<ElementId>();
    try
    {
        var found = InsulationLiningBase.GetLiningIds(doc, hostId);
        if (found != null)
        {
            foreach (var id in found)
            {
                if (doc.GetElement(id) is DuctLining) lining.Add(id);
            }
        }
    }
    catch (Exception ex)
    {
        problems.Add(hostName + ": Revit would not list its lining - " + ex.Message);
        continue;
    }

    // ---- remove -------------------------------------------------------------
    if (removing)
    {
        if (lining.Count == 0)
        {
            skipped.Add(hostName + ": had no lining - nothing removed");
            continue;
        }

        // What Revit may take with the lining: its own view-specific
        // dependents - a tag on it. Read BEFORE the delete.
        var mayGoWith = new List<ElementId>();
        foreach (var id in lining)
        {
            try
            {
                var dependents = doc.GetElement(id).GetDependentElements(null);
                if (dependents == null) continue;
                foreach (var dependentId in dependents)
                {
                    if (lining.Contains(dependentId)) continue;
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
                var deleted = doc.Delete(lining);
                var unexpected = 0;
                if (deleted != null)
                {
                    foreach (var id in deleted)
                    {
                        if (lining.Contains(id)) continue;
                        if (id == hostId || hosts.Contains(id) || !mayGoWith.Contains(id)) unexpected++;
                        else alsoWent++;
                    }
                }
                if (unexpected > 0)
                {
                    attempt.RollBack();
                    putBack = string.Format("{0}: NOTHING removed - Revit would also have deleted "
                        + "{1} other element(s) with its lining, so it was put back", hostName, unexpected);
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

        // READ BACK.
        var gone = 0;
        foreach (var id in lining)
        {
            var still = doc.GetElement(id);
            if (still == null || !still.IsValidObject) gone++;
        }
        var left = 0;
        try
        {
            var after = InsulationLiningBase.GetLiningIds(doc, hostId);
            left = after == null ? 0 : after.Count;
        }
        catch (Exception) { }

        removed += gone;
        alsoWentTotal += alsoWent;
        var removedLine = string.Format("{0}: lining removed ({1} lining element(s))", hostName, gone);
        if (alsoWent > 0) removedLine += string.Format(", and {0} tag(s) on it went with it", alsoWent);
        if (left > 0) removedLine += string.Format(" - {0} STILL on it afterwards", left);
        lines.Add(removedLine);
        continue;
    }

    // ---- add or change ------------------------------------------------------
    ElementId useType = namedType;
    if (lining.Count == 0 && useType == null)
    {
        if (onlyType == null)
        {
            problems.Add(string.Format("{0}: NOT lined - no lining type was named and {1}; name one",
                hostName, typeChoices));
            continue;
        }
        useType = onlyType;
    }

    // Already as asked? Then it is left exactly as it was.
    if (lining.Count > 0)
    {
        var allSame = true;
        foreach (var id in lining)
        {
            var layer = doc.GetElement(id) as DuctLining;
            if (layer == null) { allSame = false; break; }
            if (Math.Abs(layer.Thickness * MillimetresPerFoot - thicknessMm) > SameMm) allSame = false;
            if (useType != null && layer.GetTypeId() != useType) allSame = false;
        }
        if (allSame)
        {
            unchanged++;
            var first = doc.GetElement(lining[0]) as DuctLining;
            skipped.Add(string.Format("{0}: already {1} of \"{2}\" - left as it was", hostName,
                mm(first.Thickness), typeName(first.GetTypeId())));
            continue;
        }
    }

    string before = null;
    string failed = null;
    using (var attempt = new SubTransaction(doc))
    {
        try
        {
            attempt.Start();
            if (lining.Count == 0)
            {
                DuctLining.Create(doc, hostId, useType, thickness);
            }
            else
            {
                var was = doc.GetElement(lining[0]) as DuctLining;
                before = string.Format("{0} of \"{1}\"", mm(was.Thickness), typeName(was.GetTypeId()));
                foreach (var id in lining)
                {
                    var layer = doc.GetElement(id) as DuctLining;
                    if (useType != null && layer.GetTypeId() != useType) layer.ChangeTypeId(useType);
                    layer.Thickness = thickness;
                }
            }
            attempt.Commit();
        }
        catch (Exception ex)
        {
            try { attempt.RollBack(); } catch (Exception) { }
            failed = string.Format("{0}: NOT {1} - Revit refused: {2}", hostName,
                lining.Count == 0 ? "lined" : "changed", ex.Message);
        }
    }
    if (failed != null)
    {
        problems.Add(failed);
        continue;
    }

    // READ BACK from the lining now on the host, never from the call.
    var now = new List<DuctLining>();
    try
    {
        var after = InsulationLiningBase.GetLiningIds(doc, hostId);
        if (after != null)
        {
            foreach (var id in after)
            {
                var layer = doc.GetElement(id) as DuctLining;
                if (layer != null) now.Add(layer);
            }
        }
    }
    catch (Exception) { }

    var asAsked = now.Count > 0;
    foreach (var layer in now)
    {
        if (Math.Abs(layer.Thickness * MillimetresPerFoot - thicknessMm) > SameMm) asAsked = false;
        if (useType != null && layer.GetTypeId() != useType) asAsked = false;
    }
    if (!asAsked)
    {
        problems.Add(now.Count == 0
            ? hostName + ": Revit took the call but NO lining reads back on it"
            : string.Format("{0}: lining reads back as {1} of \"{2}\", not as asked", hostName,
                mm(now[0].Thickness), typeName(now[0].GetTypeId())));
        continue;
    }

    var nowSays = string.Format("{0} of \"{1}\"", mm(now[0].Thickness), typeName(now[0].GetTypeId()));
    if (lining.Count == 0)
    {
        added++;
        lines.Add(string.Format("{0}: lining added, {1}", hostName, nowSays));
    }
    else
    {
        changed++;
        lines.Add(string.Format("{0}: lining changed from {1} to {2}", hostName, before, nowSays));
    }
}

// ---- the answer -------------------------------------------------------------

var all = new List<string>(lines);
all.AddRange(skipped);
report = string.Join("\n", all.ToArray());

if (removing)
    findings.Add(string.Format("Lining removed from {0} of {1} duct, fitting or accessory element(s) "
        + "({2} lining element(s))", lines.Count, hosts.Count, removed));
else if (askedProblem == null)
    findings.Add(string.Format("{0} lined, {1} changed and {2} already as asked, of {3} duct, fitting "
        + "or accessory element(s), at {4} mm", added, changed, unchanged, hosts.Count, thicknessMm));

if (pickedDirectly > 0)
    findings.Add(string.Format("{0} of the selected item(s) were lining picked directly, and are "
        + "reported under the element it lines", pickedDirectly));

if (alsoWentTotal > 0)
    findings.Add(string.Format("{0} tag(s) on the removed lining went with it, as they do when "
        + "Remove Lining is pressed in Revit", alsoWentTotal));

foreach (var one in problems) findings.Add(one);

foreach (var one in pipes)
    findings.Add("A pipe has no lining in Revit - left alone (its outer layer is insulation): " + one);

foreach (var one in flex)
    findings.Add("Flex duct - not lined here; Revit's Add Lining is for ducts, duct fittings and "
        + "duct accessories: " + one);

foreach (var one in notDuct)
    findings.Add("Not a duct, duct fitting or duct accessory - cannot carry lining, left alone: " + one);

foreach (var one in insulation)
    findings.Add("Insulation, not lining - left alone, insulation is a separate job: " + one);

if (added + changed + removed > 0)
    findings.Add("Insulation on these elements is untouched. Lining sits inside the duct: it does not "
        + "change the outside size a clash check sees, but it does narrow the free area the air has");

if (added + changed + removed == 0)
{
    if (askedProblem != null)
        refused = askedProblem;
    else if (elements == null || elements.Count == 0)
        refused = "Nothing was selected - select the ducts, duct fittings or duct accessories to line";
    else if (hosts.Count == 0 && pipes.Count > 0)
        refused = string.Format("Nothing was done: {0} of the {1} selected element(s) are pipes, pipe "
            + "fittings or accessories, and pipes have no lining in Revit - insulate them instead",
            pipes.Count, elements.Count);
    else if (hosts.Count == 0)
        refused = string.Format("None of the {0} selected element(s) is a duct, duct fitting or duct "
            + "accessory, so there was nothing to line", elements.Count);
    else if (problems.Count == 0 && removing)
        refused = string.Format("None of the {0} duct, fitting or accessory element(s) selected had "
            + "lining on it - nothing was removed", hosts.Count);
    else if (problems.Count == 0)
        refused = string.Format("All {0} duct, fitting or accessory element(s) selected already had "
            + "{1} mm of lining as asked - nothing was changed", hosts.Count, thicknessMm);
    else
        refused = string.Format("Nothing was done: {0} element(s) could not be done - the findings "
            + "name each and why", problems.Count);
}
