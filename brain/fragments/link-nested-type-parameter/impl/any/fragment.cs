// NOT STANDALONE. Assumes `doc`, `family`, `type` and `links` are in scope;
// leaves `linkReport`, `changed`, `alreadyLinked`, `notAFamily`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A NESTED FAMILY'S TYPE PARAMETERS, DRIVEN BY THIS FAMILY'S - the Associate
// Family Parameter button in the nested type's Type Properties: the motor's
// Width following the air handler's Motor Width. LINK_FAMILY_PARAMETER links a
// nested family's INSTANCE parameters, on the placed family; a TYPE parameter
// lives on the nested type, which this names by family and type.
//
// A LINK REVIT WOULD REFUSE IS REFUSED FIRST, BY NAME - a family parameter that
// does not exist, holds another kind of value (Revit's own remark: the two
// parameters must be the same type) or only reports; a parameter the nested
// type lacks, or that Revit will not let be linked - with the ones that could.
// The kind moved at 2022, Definition.ParameterType before and GetDataType()
// from, so both are reached by reflection. A nested TYPE is shared by every
// placed copy of it, so an INSTANCE parameter of this family may be refused by
// Revit as its driver - Revit's own words are passed on when it is.
//
// EVERY LINK IS READ BACK, with what the parameter reads after it. One that
// does not read as asked THROWS: the call failed, and the host rolls the whole
// call back. One already as asked is reported and not counted.

var findings = new List<string>();
var linkReport = "";
var changed = 0;
var alreadyLinked = 0;
var notAFamily = false;
string refused = null;

var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;

Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

// "YesNo" read as "Yes No", the way Revit's own list writes it.
Func<string, string> spaced = name =>
{
    var text = new System.Text.StringBuilder();
    for (var i = 0; i < name.Length; i++)
    {
        if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) text.Append(' ');
        text.Append(name[i]);
    }
    return text.ToString();
};

// TWO KINDS ARE THE SAME KIND WHATEVER VERSION THEIR IDS CARRY - from 2021 a
// kind is a ForgeTypeId compared by name; before that an enum.
Func<object, object, bool> sameKind = (first, second) =>
{
    if (first == null || second == null) return false;
    var nameEquals = first.GetType().GetMethod("NameEquals", new[] { second.GetType() });
    if (nameEquals != null)
    {
        try { return (bool)nameEquals.Invoke(first, new[] { second }); }
        catch (Exception) { }
    }
    return first.Equals(second);
};

// WHAT KIND OF VALUE A PARAMETER HOLDS, and the words Revit shows for it -
// "Yes/No", "Angle", "Length". Item1 is null when the kind cannot be read, and
// then Revit judges the link rather than this.
Func<Definition, Tuple<object, string>> kindOf = definition =>
{
    var unknown = Tuple.Create<object, string>(null, "a kind that could not be read");
    if (definition == null) return unknown;
    try
    {
        var labels = revitAssembly.GetType(dbNamespace + ".LabelUtils");
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null)
        {
            var spec = getDataType.Invoke(definition, null);
            if (spec == null) return unknown;
            var typeIdProperty = spec.GetType().GetProperty("TypeId");
            var typeId = typeIdProperty == null ? null : typeIdProperty.GetValue(spec, null) as string;
            if (string.IsNullOrEmpty(typeId)) return unknown;
            string words = null;
            var forSpec = labels == null ? null
                : labels.GetMethod("GetLabelForSpec", flags, null, new[] { spec.GetType() }, null);
            if (forSpec != null)
            {
                try { words = forSpec.Invoke(null, new[] { spec }) as string; }
                catch (Exception) { words = null; }
            }
            return Tuple.Create<object, string>(spec, string.IsNullOrEmpty(words) ? typeId : words);
        }

        var property = definition.GetType().GetProperty("ParameterType");
        var old = property == null ? null : property.GetValue(definition, null);
        if (old == null || old.ToString() == "Invalid") return unknown;
        string oldWords = null;
        var forType = labels == null ? null
            : labels.GetMethod("GetLabelFor", flags, null, new[] { old.GetType() }, null);
        if (forType != null)
        {
            try { oldWords = forType.Invoke(null, new[] { old }) as string; }
            catch (Exception) { oldWords = null; }
        }
        return Tuple.Create<object, string>(old, string.IsNullOrEmpty(oldWords) ? spaced(old.ToString()) : oldWords);
    }
    catch (Exception)
    {
        return unknown;
    }
};

Func<FamilyParameter, string> nameOf = fp =>
{
    if (fp == null) return "not linked";
    try { return "\"" + fp.Definition.Name + "\""; }
    catch (Exception) { return "?"; }
};

// WHAT A FIELD READS, in Revit's own words - never a number re-derived here,
// so no unit is named in this file.
Func<Parameter, string> reads = p =>
{
    try
    {
        var text = p.AsValueString() ?? "";
        if (text.Length == 0 && p.StorageType == StorageType.String) text = p.AsString() ?? "";
        return text;
    }
    catch (Exception)
    {
        return "";
    }
};


FamilySymbol nestedType = null;
var problems = new List<string>();
// Each link asked for: the nested type's parameter, the family parameter as
// typed, and whether it is an unlink.
var asked = new List<Tuple<string, string, bool>>();
// Each link to make: its parameter, the family parameter (null to unlink), the
// name as typed, and what it is linked to now.
var plans = new List<Tuple<Parameter, FamilyParameter, string, FamilyParameter>>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "A nested type's parameters are linked inside the family that holds it - open it first. Nothing was changed.";
}
else
{
    var fm = doc.FamilyManager;

    // ---- the nested family and its type ------------------------------------
    var own = doc.OwnerFamily == null ? ElementId.InvalidElementId : doc.OwnerFamily.Id;
    var loaded = new FilteredElementCollector(doc).OfClass(typeof(Family)).Cast<Family>().Where(f => f.Id != own).ToList();
    var saidFamily = (family ?? "").Trim();
    var nestedFamily = loaded.FirstOrDefault(f => string.Equals(f.Name, saidFamily, StringComparison.OrdinalIgnoreCase));
    if (nestedFamily == null)
        problems.Add("No family called \"" + saidFamily + "\" is loaded into this family. Loaded: "
            + (loaded.Count == 0 ? "none" : string.Join(", ", loaded.Select(f => f.Name).OrderBy(n => n).Take(25))) + ".");
    else
    {
        var types = nestedFamily.GetFamilySymbolIds().Select(id => doc.GetElement(id) as FamilySymbol)
            .Where(s => s != null).ToList();
        var saidType = (type ?? "").Trim();
        nestedType = types.FirstOrDefault(s => string.Equals(s.Name, saidType, StringComparison.OrdinalIgnoreCase));
        if (nestedType == null)
            problems.Add("\"" + nestedFamily.Name + "\" has no type called \"" + saidType + "\". Its types: "
                + string.Join(", ", types.Select(s => s.Name).OrderBy(n => n)) + ".");
    }

    // ---- the links ----------------------------------------------------------
    var familyParameters = new List<FamilyParameter>();
    foreach (FamilyParameter fp in fm.Parameters) if (fp != null) familyParameters.Add(fp);

    Func<string, FamilyParameter> familyParameterNamed = name =>
    {
        FamilyParameter exact = null;
        try { exact = fm.get_Parameter(name); } catch (Exception) { exact = null; }
        if (exact != null) return exact;
        var loose = familyParameters.Where(fp =>
        {
            try { return string.Equals(fp.Definition.Name, name, StringComparison.OrdinalIgnoreCase); }
            catch (Exception) { return false; }
        }).ToList();
        return loose.Count == 1 ? loose[0] : null;
    };

    Func<Parameter, bool> canLink = p =>
    {
        try { return p != null && fm.CanElementParameterBeAssociated(p); }
        catch (Exception) { return false; }
    };

    Func<Element, string, List<Parameter>> parametersNamed = (e, name) =>
    {
        var found = new List<Parameter>();
        foreach (Parameter p in e.Parameters)
        {
            var ownName = "";
            try { ownName = p.Definition == null ? "" : p.Definition.Name; } catch (Exception) { ownName = ""; }
            if (string.Equals(ownName, name, StringComparison.OrdinalIgnoreCase)) found.Add(p);
        }
        return found;
    };

    Func<Element, string> linkableNames = e =>
    {
        var names = new List<string>();
        foreach (Parameter p in e.Parameters)
        {
            if (!canLink(p)) continue;
            try { if (p.Definition != null) names.Add(p.Definition.Name); } catch (Exception) { }
        }
        names = names.Distinct().OrderBy(n => n).ToList();
        return names.Count == 0 ? "none" : string.Join(", ", names);
    };

    Func<Parameter, string> couldDrive = target =>
    {
        var targetKind = kindOf(target.Definition);
        var names = familyParameters
            .Where(fp =>
            {
                try
                {
                    if (fp.IsReporting || fp.StorageType != target.StorageType) return false;
                    var theirs = kindOf(fp.Definition);
                    return targetKind.Item1 == null || theirs.Item1 == null || sameKind(targetKind.Item1, theirs.Item1);
                }
                catch (Exception) { return false; }
            })
            .Select(fp => fp.Definition.Name + (fp.IsInstance ? " (instance)" : "")).OrderBy(n => n).ToList();
        return names.Count == 0
            ? "none in this family - ADD_FAMILY_PARAMETERS makes one (" + targetKind.Item2 + ")"
            : string.Join(", ", names);
    };

    if (links == null || links.Count == 0)
        problems.Add("No links were given - \"Width=Motor Width\", semicolons between, or \"Width=none\" to unlink.");
    else
        foreach (var pair in links)
        {
            var left = (pair.Key ?? "").Trim();
            var wanted = (pair.Value ?? "").Trim();
            if (left.Length == 0)
            {
                problems.Add("\"=" + wanted + "\" names no parameter of the nested type - its name goes on the left.");
                continue;
            }
            if (asked.Any(a => string.Equals(a.Item1, left, StringComparison.OrdinalIgnoreCase)))
            {
                problems.Add("\"" + left + "\" is named twice in the links - say it once.");
                continue;
            }
            if (wanted.Length == 0)
            {
                problems.Add("\"" + left + "=\" names no family parameter - write \"" + left + "=none\" to unlink it.");
                continue;
            }
            var unlink = string.Equals(wanted, "none", StringComparison.OrdinalIgnoreCase);
            if (unlink && familyParameterNamed(wanted) != null)
            {
                problems.Add("This family has a parameter called \"" + wanted + "\", so \"" + left + "=" + wanted
                    + "\" could mean unlink it or link it to that parameter - rename the parameter first.");
                continue;
            }
            asked.Add(Tuple.Create(left, unlink ? "none" : wanted, unlink));
        }

    // ---- every link checked before the first change -------------------------
    if (problems.Count == 0)
        foreach (var link in asked)
        {
            var found = parametersNamed(nestedType, link.Item1);
            var linkable = found.Where(canLink).ToList();
            if (linkable.Count == 0)
            {
                problems.Add(found.Count == 0
                    ? "The nested type \"" + nestedType.Name + "\" has no parameter called \"" + link.Item1 + "\". What "
                      + "can be linked on it: " + linkableNames(nestedType) + ". An INSTANCE parameter of the nested "
                      + "family is on the placed family - LINK_FAMILY_PARAMETER links those."
                    : "Revit does not let \"" + link.Item1 + "\" of the nested type be linked to a family parameter.");
                continue;
            }
            if (linkable.Count > 1)
            {
                problems.Add(linkable.Count + " parameters called \"" + link.Item1 + "\" can be linked on the nested "
                    + "type, and which is meant cannot be told from the name.");
                continue;
            }
            var target = linkable[0];
            FamilyParameter now = null;
            try { now = fm.GetAssociatedFamilyParameter(target); }
            catch (Exception ex)
            {
                problems.Add("Revit could not say what \"" + link.Item1 + "\" is linked to: " + ex.Message);
                continue;
            }
            FamilyParameter driver = null;
            if (!link.Item3)
            {
                driver = familyParameterNamed(link.Item2);
                if (driver == null)
                {
                    problems.Add("No family parameter is called \"" + link.Item2 + "\". The ones that could drive \""
                        + link.Item1 + "\" (" + kindOf(target.Definition).Item2 + "): " + couldDrive(target) + ".");
                    continue;
                }
                if (driver.IsReporting)
                {
                    problems.Add("\"" + driver.Definition.Name + "\" is a reporting parameter and cannot drive \""
                        + link.Item1 + "\". The ones that could: " + couldDrive(target) + ".");
                    continue;
                }
                var targetKind = kindOf(target.Definition);
                var driverKind = kindOf(driver.Definition);
                var kindsDiffer = targetKind.Item1 != null && driverKind.Item1 != null
                    && !sameKind(targetKind.Item1, driverKind.Item1);
                var storesDiffer = false;
                try { storesDiffer = target.StorageType != driver.StorageType; } catch (Exception) { storesDiffer = false; }
                if (kindsDiffer || storesDiffer)
                {
                    problems.Add("\"" + driver.Definition.Name + "\" (" + driverKind.Item2 + ") cannot drive \"" + link.Item1
                        + "\" (" + targetKind.Item2 + ") - Revit links two parameters only when they hold the same "
                        + "kind of value. The ones that could: " + couldDrive(target) + ".");
                    continue;
                }
            }
            plans.Add(Tuple.Create(target, driver, link.Item1, now));
        }

    if (problems.Count > 0)
    {
        var shown = problems.Take(12).ToList();
        refused = "Nothing was changed. " + string.Join(" ", shown)
            + (problems.Count > shown.Count ? " And " + (problems.Count - shown.Count) + " more like these." : "");
    }
}

// ---------------------------------------------------------------------------
// LINK, THEN READ EVERY ONE BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var fm = doc.FamilyManager;
    Func<FamilyParameter, FamilyParameter, bool> same = (first, second) =>
        first == null ? second == null : second != null && first.Id == second.Id;
    var asItWas = string.Join("; ", plans.Select(p => p.Item3 + " " + nameOf(p.Item4)));
    var named = "\"" + nestedType.FamilyName + " : " + nestedType.Name + "\"";

    var any = false;
    foreach (var plan in plans)
    {
        if (same(plan.Item4, plan.Item2)) continue;
        try
        {
            fm.AssociateElementParameterToFamilyParameter(plan.Item1, plan.Item2);
            any = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused to " + (plan.Item2 == null
                    ? "unlink \"" + plan.Item3 + "\" of " + named
                    : "link \"" + plan.Item3 + "\" of " + named + " to " + nameOf(plan.Item2)
                      + (plan.Item2.IsInstance ? ", an INSTANCE parameter - a type is shared by every placed copy" : ""))
                + ": " + ex.Message + " The call failed there, and Heron rolls the whole call back. As the links "
                + "stood before the call: " + asItWas + ".");
        }
    }

    if (any) doc.Regenerate();

    var parts = new List<string>();
    foreach (var plan in plans)
    {
        FamilyParameter after = null;
        try { after = fm.GetAssociatedFamilyParameter(plan.Item1); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("\"" + plan.Item3 + "\" of " + named + " could not be read back after "
                + "the call: " + ex.Message + " The call failed, and Heron rolls the whole call back. As the links "
                + "stood before the call: " + asItWas + ".");
        }
        if (!same(after, plan.Item2))
            throw new InvalidOperationException("\"" + plan.Item3 + "\" of " + named + " reads " + nameOf(after) + " after "
                + "the call, not " + (plan.Item2 == null ? "unlinked" : nameOf(plan.Item2)) + ". The call failed, and "
                + "Heron rolls the whole call back. As the links stood before the call: " + asItWas + ".");

        var value = reads(plan.Item1);
        var tail = value.Length > 0 ? " (reads " + value + ")" : "";
        if (same(plan.Item4, plan.Item2))
        {
            alreadyLinked++;
            parts.Add(plan.Item3 + (after == null ? " already not linked" : " already linked to " + nameOf(after)) + tail);
        }
        else
        {
            changed++;
            parts.Add(plan.Item3 + " " + nameOf(plan.Item4) + " -> " + nameOf(after) + tail);
        }
    }
    linkReport = named + ": " + string.Join("; ", parts);

    findings.Add(changed + " link(s) changed and " + alreadyLinked + " already as asked, on the nested type " + named
        + " - read back from the family.");
    if (changed > 0)
        findings.Add("Every copy of " + named + " placed in this family follows these links; another type of that "
            + "family does not, until it is linked too.");
}

if (refused != null) findings.Add(refused);
