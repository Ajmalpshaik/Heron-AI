// NOT STANDALONE. Assumes `doc`, `parameterNames`, `instance` and
// `parameterGroup` are in scope; leaves `added`, `alreadyThere`, `sharedFile`,
// `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// SHARED PARAMETERS IN A FAMILY - Family Types' "New Parameter" with Shared
// parameter chosen, several at a time. A project schedules and tags a family's
// own value only when it is a SHARED parameter, the same one by its GUID in the
// family and in the project; ADD_FAMILY_PARAMETERS makes family parameters,
// which a tag cannot read.
//
// THE OFFICE'S SHARED PARAMETER FILE IS THE ONLY SOURCE, AND IT IS ONLY READ.
// The file Revit is set to (Manage > Shared Parameters) is opened and each name
// looked for in it; nothing is written to it and the setting is never changed.
// A GUID made here would be a different parameter from the office's one with
// the same name, and every tag and schedule would miss it. No file set, or a
// name not in it, is refused - with the names that are close.
//
// THE KIND COMES FROM THE FILE - what the definition holds. Type or instance,
// and the Properties palette heading, are asked for; the group is matched by
// the name the palette shows, as ADD_FAMILY_PARAMETERS matches it.
//
// THE CALL MOVED ACROSS THE RANGE: AddParameter(definition, GROUP, isInstance)
// took BuiltInParameterGroup up to 2024 and a ForgeTypeId from 2022, so the
// overload is picked at run time by its own second parameter, newest first.
//
// A NAME ALREADY IN THE FAMILY is left alone when it is this very shared
// parameter - the same GUID - and refused when it is anything else, which would
// be a second parameter under the office's name.
//
// READ BACK, ALL OR NOTHING: each one is found again by name, shared, with the
// file's GUID and the scope asked; anything else fails the call, and the host
// rolls the whole call back.

var findings = new List<string>();
var added = new List<string>();
var alreadyThere = new List<string>();
var sharedFile = "";
var notAFamily = false;
string refused = null;

var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;

Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

// AddParameter(ExternalDefinition, GROUP, isInstance), picked by its own
// signature - the ForgeTypeId group first.
var addParameter = typeof(FamilyManager).GetMethods()
    .Where(m => m.Name == "AddParameter")
    .Where(m =>
    {
        var p = m.GetParameters();
        return p.Length == 3 && p[0].ParameterType == typeof(ExternalDefinition) && p[2].ParameterType == typeof(bool);
    })
    .OrderBy(m => m.GetParameters()[1].ParameterType.Name == "ForgeTypeId" ? 0 : 1)
    .FirstOrDefault();
var groupType = addParameter == null ? null : addParameter.GetParameters()[1].ParameterType;

// EVERY GROUP THIS REVIT OFFERS, under the label the Properties palette shows.
var groups = new List<KeyValuePair<string, object>>();
if (groupType != null)
{
    if (groupType.IsEnum)
    {
        var label = typeof(LabelUtils).GetMethod("GetLabelFor", new[] { groupType });
        foreach (var value in Enum.GetValues(groupType))
        {
            string text = null;
            try { text = label == null ? null : (string)label.Invoke(null, new[] { value }); }
            catch (Exception) { }
            if (text == null && value.ToString() == "INVALID") text = "Other";
            groups.Add(new KeyValuePair<string, object>(text ?? value.ToString(), value));
        }
    }
    else
    {
        var owner = revitAssembly.GetType(dbNamespace + ".GroupTypeId");
        var label = typeof(LabelUtils).GetMethod("GetLabelForGroup", new[] { groupType });
        if (owner != null)
            foreach (var property in owner.GetProperties(flags))
            {
                if (property.PropertyType != groupType) continue;
                var value = property.GetValue(null);
                if (value == null) continue;
                string text = null;
                try { text = label == null ? null : (string)label.Invoke(null, new[] { value }); }
                catch (Exception) { }
                groups.Add(new KeyValuePair<string, object>(text ?? property.Name, value));
            }
    }
}

// WHAT A DEFINITION HOLDS, in Revit's words - read by reflection across 2022.
Func<Definition, string> kindLabel = definition =>
{
    try
    {
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        object spec = null;
        System.Reflection.MethodInfo label = null;
        if (getDataType != null)
        {
            spec = getDataType.Invoke(definition, null);
            label = spec == null ? null : typeof(LabelUtils).GetMethod("GetLabelForSpec", new[] { spec.GetType() });
        }
        else
        {
            var property = definition.GetType().GetProperty("ParameterType");
            spec = property == null ? null : property.GetValue(definition);
            label = spec == null ? null : typeof(LabelUtils).GetMethod("GetLabelFor", new[] { spec.GetType() });
        }
        if (spec == null) return "an unknown kind";
        if (label != null) return (string)label.Invoke(null, new[] { spec });
        return spec.ToString();
    }
    catch (Exception)
    {
        return "an unknown kind";
    }
};

var names = new List<string>();
if (parameterNames != null)
    foreach (var raw in parameterNames)
    {
        var trimmed = raw == null ? "" : raw.Trim();
        if (trimmed.Length > 0 && !names.Any(n => string.Equals(n, trimmed, StringComparison.OrdinalIgnoreCase)))
            names.Add(trimmed);
    }

// Each one to add: its name as typed, and the file's definition of it.
var plans = new List<Tuple<string, ExternalDefinition>>();
object groupValue = null;
var problems = new List<string>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A shared parameter is "
        + "added to a project with ADD_PROJECT_PARAMETER; to add one to a family, open it first.";
}
else if (addParameter == null)
    refused = "This Revit has no AddParameter(shared definition, group, instance) that this recognises, so nothing "
        + "was added.";
else
{
    // ---- the office's file, read and never written --------------------------
    DefinitionFile file = null;
    try { sharedFile = doc.Application.SharedParametersFilename ?? ""; } catch (Exception) { sharedFile = ""; }
    if (sharedFile.Trim().Length == 0)
        problems.Add("Revit has no shared parameter file set. Set the office's file in Manage > Shared Parameters, "
            + "then ask again - a GUID made here would not be the office's parameter.");
    else
    {
        try { file = doc.Application.OpenSharedParameterFile(); } catch (Exception) { file = null; }
        if (file == null)
            problems.Add("Revit could not open the shared parameter file \"" + sharedFile + "\" - it is missing or "
                + "not a shared parameter file. Check Manage > Shared Parameters.");
    }

    if (names.Count == 0) problems.Add("No parameter names were given - commas between.");

    if (file != null)
    {
        // Every definition in the file, with the group it is filed under there.
        var everything = new List<Tuple<string, ExternalDefinition>>();
        foreach (DefinitionGroup group in file.Groups)
            foreach (Definition definition in group.Definitions)
            {
                var external = definition as ExternalDefinition;
                if (external != null) everything.Add(Tuple.Create(group.Name, external));
            }

        foreach (var name in names)
        {
            var exact = everything.Where(e => e.Item2.Name == name).ToList();
            if (exact.Count == 0)
                exact = everything.Where(e => string.Equals(e.Item2.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
            var guids = exact.Select(e => e.Item2.GUID).Distinct().ToList();
            if (exact.Count == 0)
            {
                var close = everything.Where(e => squash(e.Item2.Name).Contains(squash(name))
                        || (squash(name).Length > 0 && squash(name).Contains(squash(e.Item2.Name)) && squash(e.Item2.Name).Length > 3))
                    .Select(e => e.Item2.Name + " (" + e.Item1 + ")").Distinct().Take(10).ToList();
                problems.Add("\"" + name + "\" is not in the shared parameter file \"" + sharedFile + "\"."
                    + (close.Count > 0 ? " Close: " + string.Join(", ", close) + "." : " Add it there first, or name one "
                        + "the file has."));
                continue;
            }
            if (guids.Count > 1)
            {
                problems.Add("\"" + name + "\" is in the file " + guids.Count + " times with different GUIDs (in "
                    + string.Join(", ", exact.Select(e => e.Item1)) + ") - which one the office means cannot be told.");
                continue;
            }
            plans.Add(Tuple.Create(name, exact[0].Item2));
        }
    }

    // ---- the group --------------------------------------------------------------
    var wantedGroup = (parameterGroup ?? "").Trim();
    if (wantedGroup.Length == 0)
        problems.Add("No group was named - the Properties palette heading, such as \"Mechanical\" or \"Identity Data\".");
    else
    {
        foreach (var entry in groups)
            if (string.Equals(entry.Key.Trim(), wantedGroup, StringComparison.OrdinalIgnoreCase))
            { groupValue = entry.Value; break; }
        if (groupValue == null)
            problems.Add("No group called \"" + wantedGroup + "\" exists in this Revit. Groups are the headings of the "
                + "Properties palette - for example " + string.Join(", ", groups.Select(g => g.Key)
                    .Where(k => k.Length > 0).Distinct().OrderBy(k => k).Take(12)) + ".");
    }

    // ---- a name the family already has ------------------------------------------
    if (problems.Count == 0)
    {
        var fm = doc.FamilyManager;
        foreach (var plan in plans.ToList())
        {
            var existing = fm.get_Parameter(plan.Item2.Name);
            if (existing == null) continue;
            Guid? existingGuid = null;
            try { if (existing.IsShared) existingGuid = existing.GUID; } catch (Exception) { existingGuid = null; }
            if (existingGuid.HasValue && existingGuid.Value == plan.Item2.GUID)
            {
                alreadyThere.Add(plan.Item2.Name + " (" + (existing.IsInstance ? "instance" : "type") + ")");
                plans.Remove(plan);
                if (existing.IsInstance != instance)
                    findings.Add("\"" + plan.Item2.Name + "\" is already in this family as this shared parameter, but as "
                        + (existing.IsInstance ? "an instance" : "a type") + " parameter, not the "
                        + (instance ? "instance" : "type") + " one asked for. It was left as it is.");
                continue;
            }
            problems.Add("This family already has a parameter called \"" + plan.Item2.Name + "\" that is "
                + (existingGuid.HasValue ? "another shared parameter (GUID " + existingGuid.Value + ")" : "not shared")
                + " - adding the office's one would put two under one name. Rename or remove that one first.");
        }
    }

    if (problems.Count > 0) refused = "Nothing was added. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// ADD, THEN READ EVERY ONE BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var fm = doc.FamilyManager;
    foreach (var plan in plans)
    {
        try { addParameter.Invoke(fm, new object[] { plan.Item2, groupValue, instance }); }
        catch (Exception ex)
        {
            var reason = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            throw new InvalidOperationException("Revit refused the shared parameter \"" + plan.Item2.Name + "\": "
                + reason + " The call failed, and Heron rolls the whole call back.");
        }

        var made = fm.get_Parameter(plan.Item2.Name);
        Guid? guid = null;
        try { if (made != null && made.IsShared) guid = made.GUID; } catch (Exception) { guid = null; }
        if (made == null || !guid.HasValue || guid.Value != plan.Item2.GUID || made.IsInstance != instance)
            throw new InvalidOperationException("\"" + plan.Item2.Name + "\" does not read back as the office's shared "
                + (instance ? "instance" : "type") + " parameter with GUID " + plan.Item2.GUID + ". The call failed, and "
                + "Heron rolls the whole call back.");

        added.Add(plan.Item2.Name + " (" + kindLabel(plan.Item2) + ", " + (instance ? "instance" : "type") + ", GUID "
            + plan.Item2.GUID + ")");
    }

    if (added.Count > 0)
    {
        findings.Add("Added " + added.Count + " shared parameter(s) from \"" + sharedFile + "\", read back by name and "
            + "GUID: " + string.Join("; ", added) + ".");
        findings.Add("They hold no value yet - SET_FAMILY_TYPE_VALUES writes it. A project's tags and schedules read "
            + "them once the family is loaded there, by the same GUID.");
    }
    if (alreadyThere.Count > 0)
        findings.Add(alreadyThere.Count + " were already in this family as the office's shared parameter, and were "
            + "left alone: " + string.Join("; ", alreadyThere) + ".");
}

if (refused != null) findings.Add(refused);
