// NOT STANDALONE. Assumes `doc`, `uidoc`, `forms` and `links` are in scope;
// leaves `linkReport`, `changed`, `alreadyLinked`, `notAFamily`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A FORM'S OWN FIELDS, DRIVEN BY THE FAMILY'S PARAMETERS - the "Associate Family
// Parameter" button at the end of a row in Properties, for the forms named and
// no others: a part shown only while a Yes/No parameter is ticked (Visible), a
// revolve whose sweep follows an Angle parameter (Start Angle, End Angle), an
// extrusion whose depth follows a Length one (Extrusion Start, Extrusion End).
// Anything else Revit lets be linked on a form is linked the same way; Revit
// decides what can be (CanElementParameterBeAssociated), never a list typed
// here.
//
// ONE FORM AT A TIME IS THE POINT. LINK_FAMILY_PARAMETER links a parameter on
// every element of a category at once - every extrusion together - and a
// handle that may be left out is one extrusion among several.
//
// MATERIAL IS NOT HERE. SET_FAMILY_FORM_MATERIAL links a form's Material, and
// also sets one or puts it back to <By Category>; two tools for one field
// would answer the same words two ways, so it is refused here by name.
//
// A LINK REVIT WOULD REFUSE IS REFUSED FIRST, BY NAME - a family parameter that
// does not exist, holds another kind of value (Revit's own remark: the two
// parameters must be the same type) or only reports; a field a form does not
// have, or that Revit will not let be linked - with the ones that could. The
// kind moved at 2022, Definition.ParameterType before and GetDataType() from,
// so both are reached by reflection; a kind that cannot be read on either side
// is left to Revit to judge rather than refused on a guess. Nothing is changed
// until every link has been checked.
//
// EVERY LINK IS READ BACK, with what the field reads after it. One that does
// not read as asked THROWS: the call failed, and the host rolls the whole call
// back. One already as asked is reported and not counted.

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

Func<GenericForm, string> shapeOf = f => f is Extrusion ? "extrusion" : f is Revolution ? "revolve"
    : f is Blend ? "blend" : f is SweptBlend ? "swept blend" : f is Sweep ? "sweep" : "form";
Func<GenericForm, string> describe = f =>
{
    var solid = true;
    try { solid = f.IsSolid; } catch (Exception) { solid = true; }
    return (solid ? "solid " : "void ") + shapeOf(f) + " " + f.UniqueId;
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

var targets = new List<GenericForm>();
var problems = new List<string>();
// Each link asked for: the field's name, the family parameter's name as typed,
// and whether it is an unlink.
var asked = new List<Tuple<string, string, bool>>();
// Each link to make: the form, its field, the family parameter (null to
// unlink), the field's name as typed, and what the field is linked to now.
var plans = new List<Tuple<GenericForm, Parameter, FamilyParameter, string, FamilyParameter>>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "A form's fields are linked inside the family - open it for editing first (OPEN_FAMILY_FOR_EDITING). "
        + "Nothing was changed.";
}
else
{
    var fm = doc.FamilyManager;

    // ---- the forms --------------------------------------------------------
    var named = new List<Element>();
    var which = (forms ?? "").Trim();
    if (string.Equals(which, "selected", StringComparison.OrdinalIgnoreCase)
        || string.Equals(which, "selection", StringComparison.OrdinalIgnoreCase))
    {
        if (uidoc == null || uidoc.Document == null || !uidoc.Document.Equals(doc))
            problems.Add("\"selected\" means what is selected in this family's window, and this family is not the "
                + "window in front. Bring it to the front, or name the forms by their ids.");
        else
            foreach (var id in uidoc.Selection.GetElementIds())
            {
                var element = doc.GetElement(id);
                if (element != null) named.Add(element);
            }
    }
    else
        foreach (var part in which.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0))
        {
            var element = doc.GetElement(part);
            if (element == null) problems.Add("No element in this family has the id \"" + part + "\".");
            else named.Add(element);
        }

    foreach (var element in named)
    {
        var f = element as GenericForm;
        if (f == null)
            problems.Add("\"" + (element.Name ?? element.UniqueId) + "\" (" + element.UniqueId + ") is not a form - "
                + "an extrusion, revolve, blend, sweep or swept blend.");
        else if (!targets.Any(t => t.Id == f.Id)) targets.Add(f);
    }
    if (problems.Count == 0 && targets.Count == 0)
        problems.Add("No form was named. Name them by the ids the form tools gave back, commas between, or select "
            + "them and say \"selected\".");

    // ---- the links --------------------------------------------------------
    var familyParameters = new List<FamilyParameter>();
    foreach (FamilyParameter fp in fm.Parameters) if (fp != null) familyParameters.Add(fp);

    // A FAMILY PARAMETER BY NAME - exactly as typed first, then the one whose
    // name differs only in case, and only when exactly one does.
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

    // A form's fields by name, case ignored - the name in Properties.
    Func<Element, string, List<Parameter>> fieldsNamed = (e, name) =>
    {
        var found = new List<Parameter>();
        foreach (Parameter p in e.Parameters)
        {
            var own = "";
            try { own = p.Definition == null ? "" : p.Definition.Name; } catch (Exception) { own = ""; }
            if (string.Equals(own, name, StringComparison.OrdinalIgnoreCase)) found.Add(p);
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

    // THE FAMILY PARAMETERS THAT COULD DRIVE ONE FIELD: the same kind of value,
    // stored the same way, and not reporting.
    Func<Parameter, string> couldDrive = own =>
    {
        var ownKind = kindOf(own.Definition);
        var names = familyParameters
            .Where(fp =>
            {
                try
                {
                    if (fp.IsReporting || fp.StorageType != own.StorageType) return false;
                    var theirs = kindOf(fp.Definition);
                    return ownKind.Item1 == null || theirs.Item1 == null || sameKind(ownKind.Item1, theirs.Item1);
                }
                catch (Exception) { return false; }
            })
            .Select(fp => fp.Definition.Name).OrderBy(n => n).ToList();
        return names.Count == 0
            ? "none in this family - ADD_FAMILY_PARAMETERS makes one (" + ownKind.Item2 + ")"
            : string.Join(", ", names);
    };

    if (links == null || links.Count == 0)
        problems.Add("No links were given - \"Visible=Show_Handle\", semicolons between, or \"Visible=none\" to "
            + "unlink.");
    else
        foreach (var pair in links)
        {
            var own = (pair.Key ?? "").Trim();
            var wanted = (pair.Value ?? "").Trim();
            if (own.Length == 0)
            {
                problems.Add("\"=" + wanted + "\" names no field of the form - the name in Properties goes on the left.");
                continue;
            }
            if (asked.Any(a => string.Equals(a.Item1, own, StringComparison.OrdinalIgnoreCase)))
            {
                problems.Add("\"" + own + "\" is named twice in the links - say it once.");
                continue;
            }
            if (squash(own) == "material")
            {
                problems.Add("A form's Material is linked by SET_FAMILY_FORM_MATERIAL, which also sets one or puts it "
                    + "back to <By Category> - it is left to that tool.");
                continue;
            }
            if (wanted.Length == 0)
            {
                problems.Add("\"" + own + "=\" names no family parameter - write \"" + own + "=none\" to unlink it.");
                continue;
            }
            var unlink = string.Equals(wanted, "none", StringComparison.OrdinalIgnoreCase);
            if (unlink && familyParameterNamed(wanted) != null)
            {
                problems.Add("This family has a parameter called \"" + wanted + "\", so \"" + own + "=" + wanted
                    + "\" could mean unlink it or link it to that parameter - rename the parameter first.");
                continue;
            }
            asked.Add(Tuple.Create(own, unlink ? "none" : wanted, unlink));
        }

    // ---- every form, every link, checked before the first change ----------
    if (problems.Count == 0)
        foreach (var f in targets)
            foreach (var link in asked)
            {
                var found = fieldsNamed(f, link.Item1);
                var linkable = found.Where(canLink).ToList();
                if (linkable.Count == 0)
                {
                    problems.Add(found.Count == 0
                        ? "The " + describe(f) + " has no field called \"" + link.Item1 + "\". What can be linked on it: "
                          + linkableNames(f) + "."
                        : "Revit does not let \"" + link.Item1 + "\" be linked to a family parameter on the "
                          + describe(f) + ".");
                    continue;
                }
                if (linkable.Count > 1)
                {
                    problems.Add(linkable.Count + " fields called \"" + link.Item1 + "\" can be linked on the "
                        + describe(f) + ", and which is meant cannot be told from the name.");
                    continue;
                }
                var field = linkable[0];

                // A READ THAT FAILED IS NOT "NOT LINKED" - null is what an
                // unlinked field answers too.
                FamilyParameter now = null;
                try { now = fm.GetAssociatedFamilyParameter(field); }
                catch (Exception ex)
                {
                    problems.Add("Revit could not say what \"" + link.Item1 + "\" on the " + describe(f) + " is linked "
                        + "to: " + ex.Message);
                    continue;
                }

                FamilyParameter target = null;
                if (!link.Item3)
                {
                    target = familyParameterNamed(link.Item2);
                    if (target == null)
                    {
                        problems.Add("No family parameter is called \"" + link.Item2 + "\". The ones that could drive \""
                            + link.Item1 + "\" (" + kindOf(field.Definition).Item2 + ") on the " + describe(f) + ": "
                            + couldDrive(field) + ".");
                        continue;
                    }
                    if (target.IsReporting)
                    {
                        problems.Add("\"" + target.Definition.Name + "\" is a reporting parameter - it reads a size "
                            + "off the family and cannot drive one. The ones that could drive \"" + link.Item1
                            + "\": " + couldDrive(field) + ".");
                        continue;
                    }
                    var ownKind = kindOf(field.Definition);
                    var targetKind = kindOf(target.Definition);
                    var kindsDiffer = ownKind.Item1 != null && targetKind.Item1 != null
                        && !sameKind(ownKind.Item1, targetKind.Item1);
                    var storesDiffer = false;
                    try { storesDiffer = field.StorageType != target.StorageType; } catch (Exception) { storesDiffer = false; }
                    if (kindsDiffer || storesDiffer)
                    {
                        problems.Add("\"" + target.Definition.Name + "\" (" + targetKind.Item2 + ") cannot drive \""
                            + link.Item1 + "\" (" + ownKind.Item2 + ") on the " + describe(f) + " - Revit links two "
                            + "parameters only when they hold the same kind of value. The ones that could: "
                            + couldDrive(field) + ".");
                        continue;
                    }
                }
                plans.Add(Tuple.Create(f, field, target, link.Item1, now));
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
    var asItWas = string.Join("; ", plans.Select(p => describe(p.Item1) + " " + p.Item4 + " " + nameOf(p.Item5)));

    var any = false;
    foreach (var plan in plans)
    {
        if (same(plan.Item5, plan.Item3)) continue;
        try
        {
            fm.AssociateElementParameterToFamilyParameter(plan.Item2, plan.Item3);
            any = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused to " + (plan.Item3 == null
                    ? "unlink \"" + plan.Item4 + "\" on the " + describe(plan.Item1)
                    : "link \"" + plan.Item4 + "\" on the " + describe(plan.Item1) + " to " + nameOf(plan.Item3))
                + ": " + ex.Message + " The call failed there, and Heron rolls the whole call back. As the links "
                + "stood before the call: " + asItWas + ".");
        }
    }

    if (any) doc.Regenerate();

    var rows = new List<string>();
    foreach (var f in targets)
    {
        var parts = new List<string>();
        foreach (var plan in plans.Where(p => p.Item1.Id == f.Id))
        {
            FamilyParameter after = null;
            try { after = fm.GetAssociatedFamilyParameter(plan.Item2); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("\"" + plan.Item4 + "\" on the " + describe(f) + " could not be "
                    + "read back after the call: " + ex.Message + " The call failed, and Heron rolls the whole call "
                    + "back. As the links stood before the call: " + asItWas + ".");
            }
            if (!same(after, plan.Item3))
                throw new InvalidOperationException("\"" + plan.Item4 + "\" on the " + describe(f) + " reads "
                    + nameOf(after) + " after the call, not " + (plan.Item3 == null ? "unlinked" : nameOf(plan.Item3))
                    + ". The call failed, and Heron rolls the whole call back. As the links stood before the call: "
                    + asItWas + ".");

            var value = reads(plan.Item2);
            var tail = value.Length > 0 ? " (reads " + value + ")" : "";
            if (same(plan.Item5, plan.Item3))
            {
                alreadyLinked++;
                parts.Add(plan.Item4 + (after == null ? " already not linked" : " already linked to " + nameOf(after)) + tail);
            }
            else
            {
                changed++;
                parts.Add(plan.Item4 + " " + nameOf(plan.Item5) + " -> " + nameOf(after) + tail);
            }
        }
        rows.Add(describe(f) + ": " + string.Join("; ", parts));
    }
    linkReport = string.Join("  ||  ", rows);

    findings.Add(changed + " link(s) changed and " + alreadyLinked + " already as asked, on " + targets.Count
        + " form(s) - read back from the family.");
    if (plans.Any(p => p.Item3 == null && p.Item5 != null))
        findings.Add("An unlinked field keeps the value it had and can be typed into again in Properties.");
    if (plans.Any(p => p.Item3 != null && p.Item2.Id == new ElementId(BuiltInParameter.IS_VISIBLE_PARAM)))
        findings.Add("A form linked to a Yes/No parameter still shows in the Family Editor; in a project it shows "
            + "only in the types where that parameter is ticked.");
    if (changed > 0)
        findings.Add("A field linked here follows the parameter's value in each type, picked in Family Types. A "
            + "face that LOCK_FORM_TO_PLANES also locked to a plane is held by both, and FLEX_FAMILY is where a "
            + "conflict between them shows.");
}

if (refused != null) findings.Add(refused);
