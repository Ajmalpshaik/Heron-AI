// NOT STANDALONE. Assumes `doc`, `parameterNames`, `instance`,
// `parameterGroup` and `replaceExisting` are in scope; leaves `added`,
// `alreadyThere`, `replaced`, `formulasReadBack`, `keptLinks`, `sharedFile`,
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
//
// REPLACING, not adding, when `replaceExisting` is true: each name is a pair,
// Old=SharedName, and a FAMILY parameter already in the family becomes the
// office's shared one in place - Family Types > Modify > Shared parameter >
// Select. The REPLACE section at the end has its rules.

var findings = new List<string>();
var added = new List<string>();
var alreadyThere = new List<string>();
var replaced = new List<string>();
var formulasReadBack = "";
var keptLinks = "";
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

// ReplaceParameter(FamilyParameter, ExternalDefinition, GROUP, isInstance) -
// the same move across the range as AddParameter (BuiltInParameterGroup 2020
// to 2024, ForgeTypeId from 2022), picked the same way.
var replaceParameter = typeof(FamilyManager).GetMethods()
    .Where(m => m.Name == "ReplaceParameter")
    .Where(m =>
    {
        var p = m.GetParameters();
        return p.Length == 4 && p[0].ParameterType == typeof(FamilyParameter)
            && p[1].ParameterType == typeof(ExternalDefinition) && p[3].ParameterType == typeof(bool);
    })
    .OrderBy(m => m.GetParameters()[2].ParameterType.Name == "ForgeTypeId" ? 0 : 1)
    .FirstOrDefault();

// The group's type is the one the call this request makes takes.
var groupType = replaceExisting
    ? (replaceParameter == null ? null : replaceParameter.GetParameters()[2].ParameterType)
    : (addParameter == null ? null : addParameter.GetParameters()[1].ParameterType);

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
// Each one to replace: the old name as typed, and the file's definition.
var swaps = new List<Tuple<string, ExternalDefinition>>();
object groupValue = null;
var problems = new List<string>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A shared parameter is "
        + "added to a project with ADD_PROJECT_PARAMETER; to add one to a family, open it first.";
}
else if (!replaceExisting && addParameter == null)
    refused = "This Revit has no AddParameter(shared definition, group, instance) that this recognises, so nothing "
        + "was added.";
else if (replaceExisting && replaceParameter == null)
    refused = "This Revit has no ReplaceParameter(family parameter, shared definition, group, instance) that this "
        + "recognises, so nothing was replaced.";
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

    if (names.Count == 0)
        problems.Add(replaceExisting
            ? "No replacements were given - each one as Old=SharedName, separated by semicolons: "
                + "\"Width=Unit_Width; Depth=Unit_Thickness\"."
            : "No parameter names were given - commas between.");

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

        // ONE NAME, LOOKED FOR IN THE FILE: the definition, or null with the
        // reason added to the problems - close names, or two GUIDs.
        Func<string, ExternalDefinition> findInFile = name =>
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
                return null;
            }
            if (guids.Count > 1)
            {
                problems.Add("\"" + name + "\" is in the file " + guids.Count + " times with different GUIDs (in "
                    + string.Join(", ", exact.Select(e => e.Item1)) + ") - which one the office means cannot be told.");
                return null;
            }
            return exact[0].Item2;
        };

        if (!replaceExisting)
        {
            foreach (var name in names)
            {
                var definition = findInFile(name);
                if (definition != null) plans.Add(Tuple.Create(name, definition));
            }
        }
        else
        {
            // THE PAIRS. Old=SharedName, separated by semicolons or commas. A
            // semicolon is safe to split on because Revit refuses one in a name.
            foreach (var entry in names)
                foreach (var piece in entry.Split(';'))
                {
                    var text = piece.Trim();
                    if (text.Length == 0) continue;
                    var halves = text.Split('=');
                    if (halves.Length != 2 || halves[0].Trim().Length == 0 || halves[1].Trim().Length == 0)
                    {
                        problems.Add("Not one Old=SharedName: '" + text + "'.");
                        continue;
                    }
                    var definition = findInFile(halves[1].Trim());
                    if (definition != null) swaps.Add(Tuple.Create(halves[0].Trim(), definition));
                }
        }
    }

    // ---- the group --------------------------------------------------------------
    // Replacing, an empty group keeps each parameter under its own heading.
    var wantedGroup = (parameterGroup ?? "").Trim();
    if (wantedGroup.Length == 0)
    {
        if (!replaceExisting)
            problems.Add("No group was named - the Properties palette heading, such as \"Mechanical\" or \"Identity Data\".");
    }
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
    if (problems.Count == 0 && !replaceExisting)
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

    if (problems.Count > 0)
        refused = (replaceExisting ? "Nothing was replaced. " : "Nothing was added. ") + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// ADD, THEN READ EVERY ONE BACK
// ---------------------------------------------------------------------------

if (refused == null && !replaceExisting)
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

// ===========================================================================
// REPLACE - a family parameter already in the family becomes the office's
// shared one, each given as Old=SharedName
// ===========================================================================
//
// FamilyManager.ReplaceParameter(FamilyParameter, ExternalDefinition, GROUP,
// isInstance). Revit keeps every dimension label, every formula and every
// association - a connector's size, a nested family's parameter - that pointed
// at the old parameter, and they point at the shared one after. All of that is
// READ BACK here rather than trusted.
//
// EACH KEEPS ITS OWN SCOPE: `instance` is not used here, because moving a
// parameter between type and instance in the same step could leave a type
// formula reading an instance one - that is ADD_FAMILY_PARAMETERS'
// switchExisting, a call of its own. An empty parameterGroup keeps each one
// under its own heading; a named one moves them all under it.
//
// REFUSED BY NAME, BEFORE ANYTHING CHANGES:
//   - a piece that is not one Old=SharedName, an old name given twice, or one
//     shared name asked for two old ones;
//   - a shared name not in the office's file (with the close ones);
//   - an old name the family does not have, matched exactly as Family Types
//     shows it, or a built-in one, whose name is the category's;
//   - an old parameter that is ALREADY shared - it keeps its GUID;
//   - a shared definition whose kind (Length, Number, Text ...) is not the old
//     parameter's - its values and every formula would read differently;
//   - a shared name already in the family, by name (capitals ignored) or by
//     the file's GUID - the earlier way round left Unit_Width beside Width;
//   - a FORMULA THAT NAMES THE SHARED NAME: Revit throws on the replace -
//     clear that formula first (SET_FAMILY_FORMULA).
// Anything else Revit refuses part-way THROWS with Revit's own words first,
// and the host rolls the whole call back.
//
// READ BACK, ALL OR NOTHING. Each shared parameter is found again by its name:
// shared, the file's GUID, the old scope, the heading asked. No family
// parameter is still called by the old name. Every formula that read an old
// name reads the shared one and no longer the old, every other formula is
// exactly what it was, and the replaced parameter's own formula is kept.
// Labelled dimensions, associations and every type's value are compared
// per parameter, before and after.

if (refused == null && replaceExisting)
{
    var fm = doc.FamilyManager;

    // TWO KINDS ARE THE SAME KIND WHATEVER VERSION THEIR IDS CARRY - the
    // comparison ADD_FAMILY_PARAMETERS uses: NameEquals on a ForgeTypeId, plain
    // equality on the enum before 2021.
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

    // What a definition holds: GetDataType from 2022, ParameterType before it.
    Func<Definition, object> specOf = definition =>
    {
        try
        {
            var getDataType = typeof(Definition).GetMethod("GetDataType", System.Type.EmptyTypes);
            if (getDataType != null) return getDataType.Invoke(definition, null);
            var property = typeof(Definition).GetProperty("ParameterType");
            return property == null ? null : property.GetValue(definition);
        }
        catch (Exception) { return null; }
    };

    // The heading a definition sits under, in the world the replace call takes:
    // GetGroupTypeId for a ForgeTypeId, ParameterGroup for the enum.
    Func<Definition, object> groupOf = definition =>
    {
        try
        {
            if (groupType != null && !groupType.IsEnum)
            {
                var getGroup = typeof(Definition).GetMethod("GetGroupTypeId", System.Type.EmptyTypes);
                return getGroup == null ? null : getGroup.Invoke(definition, null);
            }
            var property = typeof(Definition).GetProperty("ParameterGroup");
            return property == null ? null : property.GetValue(definition);
        }
        catch (Exception) { return null; }
    };
    Func<object, string> groupName = value =>
    {
        if (value == null) return "an unknown heading";
        foreach (var entry in groups) if (sameKind(entry.Value, value)) return entry.Key;
        return value.ToString();
    };

    Func<FamilyParameter, bool> isBuiltIn = p =>
    {
        var internalDefinition = p.Definition as InternalDefinition;
        return internalDefinition != null
            && internalDefinition.BuiltInParameter != BuiltInParameter.INVALID;
    };

    // WHAT EACH FORMULA READS - the reader ADD_FAMILY_PARAMETERS uses. Names are
    // written bare in a Revit formula, spaces and all, so each is looked for
    // whole - longest first, and blanked once found, so "Width" is not found a
    // second time inside "Unit_Width". Text in quotes is blanked first.
    Func<string, List<string>, List<string>> namesReadFrom = (formula, namesLongestFirst) =>
    {
        var found = new List<string>();
        if (string.IsNullOrEmpty(formula)) return found;
        var letters = formula.ToCharArray();
        var inQuotes = false;
        for (var i = 0; i < letters.Length; i++)
        {
            if (letters[i] == '"')
            {
                // AN INCH MARK IS NOT A QUOTE: 6" + Width.
                var back = i - 1;
                while (back >= 0 && letters[back] == ' ') back--;
                var inchMark = !inQuotes && back >= 0 && char.IsDigit(letters[back]);
                if (!inchMark) inQuotes = !inQuotes;
                letters[i] = ' ';
                continue;
            }
            if (inQuotes) letters[i] = ' ';
        }
        var work = new string(letters);
        foreach (var name in namesLongestFirst)
        {
            var at = 0;
            while (at < work.Length && (at = work.IndexOf(name, at, StringComparison.Ordinal)) >= 0)
            {
                var end = at + name.Length;
                var before = at == 0 ? ' ' : work[at - 1];
                var after = end >= work.Length ? ' ' : work[end];
                var whole = !char.IsLetterOrDigit(before) && before != '_'
                    && !char.IsLetterOrDigit(after) && after != '_';
                // A UNIT IS NOT A NAME: in 100 mm the mm follows a number.
                var look = at - 1;
                while (look >= 0 && work[look] == ' ') look--;
                var unitAfterNumber = look >= 0 && (char.IsDigit(work[look]) || work[look] == '.');
                if (whole)
                {
                    if (!unitAfterNumber && !found.Contains(name)) found.Add(name);
                    work = work.Substring(0, at) + new string(' ', name.Length) + work.Substring(end);
                }
                at = end;
            }
        }
        return found;
    };

    // WHAT IS TIED TO A PARAMETER: everything associated with it, and the
    // dimensions labelled with it.
    Func<Parameter, string> describeTied = tied =>
    {
        var what = tied.Definition == null ? "a parameter" : tied.Definition.Name;
        var element = tied.Element;
        var connector = element as ConnectorElement;
        if (connector != null) return connector.Domain.ToString().Replace("Domain", "") + " connector's " + what;
        var nested = element as FamilyInstance;
        if (nested != null)
            return "nested " + (nested.Symbol != null ? nested.Symbol.Family.Name : nested.Name) + " (instance) " + what;
        var nestedType = element as FamilySymbol;
        if (nestedType != null) return "nested " + nestedType.Family.Name + " (TYPE) " + what;
        return (element == null ? "an element" : element.Name) + "'s " + what;
    };
    Func<FamilyParameter, string> tiedTo = p =>
    {
        var list = new List<string>();
        try
        {
            foreach (Parameter tied in p.AssociatedParameters) list.Add(describeTied(tied));
        }
        catch (Exception) { }
        list.Sort(StringComparer.Ordinal);
        return string.Join(", ", list);
    };
    Func<Dictionary<string, int>> countLabels = () =>
    {
        var counted = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Dimension dimension in new FilteredElementCollector(doc).OfClass(typeof(Dimension)))
        {
            FamilyParameter label = null;
            try { label = dimension.FamilyLabel; } catch (Exception) { }
            if (label == null || label.Definition == null) continue;
            var name = label.Definition.Name;
            counted[name] = counted.ContainsKey(name) ? counted[name] + 1 : 1;
        }
        return counted;
    };

    // EVERY TYPE'S VALUE, as Family Types shows it - read from each FamilyType
    // without making it current, so nothing changes to read it.
    Func<FamilyParameter, string> valuesOf = p =>
    {
        var parts = new List<string>();
        foreach (FamilyType familyType in fm.Types)
        {
            string value;
            try
            {
                if (!familyType.HasValue(p)) value = "(no value)";
                else
                {
                    value = familyType.AsValueString(p) ?? familyType.AsString(p);
                    if (value == null)
                    {
                        var number = familyType.AsDouble(p);
                        var whole = familyType.AsInteger(p);
                        var id = familyType.AsElementId(p);
                        var element = id == null ? null : doc.GetElement(id);
                        value = number.HasValue ? number.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
                            : whole.HasValue ? whole.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                            : element != null ? element.Name : "(set)";
                    }
                }
            }
            catch (Exception) { value = "(unreadable)"; }
            parts.Add((familyType.Name ?? "") + " " + value);
        }
        return string.Join(", ", parts);
    };

    var everyParameter = new List<FamilyParameter>();
    foreach (FamilyParameter p in fm.Parameters)
        if (p.Definition != null) everyParameter.Add(p);

    // The family's own parameters win a name over a built-in of the same name.
    var byName = new Dictionary<string, FamilyParameter>(StringComparer.Ordinal);
    foreach (var p in everyParameter.OrderBy(x => isBuiltIn(x) ? 1 : 0))
        if (!byName.ContainsKey(p.Definition.Name)) byName[p.Definition.Name] = p;

    var sharedNames = swaps.Select(s => s.Item2.Name).ToList();
    var namesBefore = byName.Keys.Concat(sharedNames).Distinct(StringComparer.Ordinal)
        .OrderByDescending(n => n.Length).ToList();

    // ----- REFUSE BY NAME, BEFORE ANYTHING CHANGES -----
    var reasons = new List<string>();
    foreach (var g in swaps.GroupBy(x => x.Item1, StringComparer.Ordinal).Where(g => g.Count() > 1))
        reasons.Add("'" + g.Key + "' is replaced twice in one call.");
    foreach (var g in swaps.GroupBy(x => x.Item2.GUID).Where(g => g.Count() > 1))
        reasons.Add("The shared parameter '" + g.First().Item2.Name + "' is asked for " + g.Count() + " parameters - "
            + "a family holds one parameter of a GUID.");

    var plan = new List<Tuple<FamilyParameter, string, ExternalDefinition>>();
    foreach (var swap in swaps)
    {
        var oldName = swap.Item1;
        var definition = swap.Item2;
        FamilyParameter p;
        if (!byName.TryGetValue(oldName, out p))
        {
            reasons.Add("'" + oldName + "' is not in this family. Names are matched exactly, as Family Types shows them.");
            continue;
        }
        if (isBuiltIn(p))
        {
            reasons.Add("'" + oldName + "' is built in to the category, so it is not the family's to replace.");
            continue;
        }
        if (p.IsShared)
        {
            Guid? guid = null;
            try { guid = p.GUID; } catch (Exception) { }
            reasons.Add("'" + oldName + "' is already a SHARED parameter" + (guid.HasValue ? " (GUID " + guid.Value + ")" : "")
                + " - only a family parameter is replaced here.");
            continue;
        }

        var oldKind = specOf(p.Definition);
        var newKind = specOf(definition);
        if (oldKind == null || newKind == null || !sameKind(oldKind, newKind))
        {
            reasons.Add("'" + oldName + "' holds " + kindLabel(p.Definition) + " and the shared '" + definition.Name
                + "' holds " + kindLabel(definition) + " - the kinds must be the same, or every value and formula "
                + "reading it would mean something else.");
            continue;
        }

        var takenByName = everyParameter.FirstOrDefault(o => o.Id != p.Id
            && string.Equals(o.Definition.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
        FamilyParameter takenByGuid = null;
        try { takenByGuid = fm.get_Parameter(definition.GUID); } catch (Exception) { }
        if (takenByName != null || takenByGuid != null)
        {
            var taken = takenByName ?? takenByGuid;
            reasons.Add("'" + definition.Name + "' is already in this family"
                + (taken.Definition.Name == definition.Name ? "" : " (as '" + taken.Definition.Name + "')")
                + " - replacing '" + oldName + "' with it would put two under one name. Clear any formula that reads it "
                + "and delete it first (SET_FAMILY_FORMULA, then DELETE_FAMILY_PARAMETERS), then replace.");
        }

        // A FORMULA THAT NAMES THE SHARED NAME: Revit throws on the replace.
        foreach (var other in everyParameter)
        {
            string formula = null;
            try { formula = other.Formula; } catch (Exception) { }
            if (string.IsNullOrEmpty(formula)) continue;
            if (namesReadFrom(formula, namesBefore).Contains(definition.Name))
                reasons.Add("'" + other.Definition.Name + "' has the formula '" + formula + "', which names '"
                    + definition.Name + "' - Revit refuses the replace while a formula names the shared parameter. "
                    + "Clear that formula first (SET_FAMILY_FORMULA).");
        }

        plan.Add(Tuple.Create(p, oldName, definition));
    }

    object wanted = null;
    if (groupValue != null) wanted = groupValue;
    foreach (var step in plan)
        if (groupValue == null && groupOf(step.Item1.Definition) == null)
            reasons.Add("The Properties palette heading of '" + step.Item2 + "' could not be read - name one in "
                + "parameterGroup.");

    if (reasons.Count > 0)
    {
        refused = "Nothing was replaced. " + string.Join(" ", reasons);
    }
    else
    {
        // ----- BEFORE -----
        var oldNames = plan.Select(x => x.Item2).ToList();
        var newFor = plan.ToDictionary(x => x.Item2, x => x.Item3.Name, StringComparer.Ordinal);
        var formulaBefore = new Dictionary<string, string>(StringComparer.Ordinal);
        var readsBefore = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var entry in byName)
        {
            if (isBuiltIn(entry.Value)) continue;
            string formula = null;
            try { formula = entry.Value.Formula; } catch (Exception) { }
            formulaBefore[entry.Key] = formula ?? "";
            readsBefore[entry.Key] = namesReadFrom(formula ?? "", namesBefore);
        }
        var labelsBefore = countLabels();
        var scopeBefore = new Dictionary<string, bool>(StringComparer.Ordinal);
        var groupWanted = new Dictionary<string, object>(StringComparer.Ordinal);
        var tiedBefore = new Dictionary<string, string>(StringComparer.Ordinal);
        var valuesBefore = new Dictionary<string, string>(StringComparer.Ordinal);
        var reportingBefore = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var step in plan)
        {
            scopeBefore[step.Item2] = step.Item1.IsInstance;
            groupWanted[step.Item2] = wanted ?? groupOf(step.Item1.Definition);
            tiedBefore[step.Item2] = tiedTo(step.Item1);
            valuesBefore[step.Item2] = valuesOf(step.Item1);
            reportingBefore[step.Item2] = step.Item1.IsReporting;
        }

        // ----- REPLACE -----
        foreach (var step in plan)
        {
            try
            {
                replaceParameter.Invoke(fm, new object[] { step.Item1, step.Item3, groupWanted[step.Item2],
                    scopeBefore[step.Item2] });
            }
            catch (Exception ex)
            {
                var reason = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                throw new InvalidOperationException(reason + " - Revit, replacing '" + step.Item2 + "' with the shared '"
                    + step.Item3.Name + "'. NOTHING from this call was kept: the parameters in it are replaced together "
                    + "or not at all.");
            }
        }

        // ----- READ BACK, ALL OR NOTHING -----
        var now = new Dictionary<string, FamilyParameter>(StringComparer.Ordinal);
        foreach (FamilyParameter p in fm.Parameters)
            if (p.Definition != null && !isBuiltIn(p) && !now.ContainsKey(p.Definition.Name)) now[p.Definition.Name] = p;
        var labelsAfter = countLabels();

        // The old names are looked for too, longest first with the rest, so an
        // old name inside a shared one - Width inside Unit_Width - is not found
        // there.
        var namesAfter = now.Keys.Concat(oldNames).Distinct(StringComparer.Ordinal)
            .OrderByDescending(n => n.Length).ToList();

        var wrong = new List<string>();
        var kept = new List<string>();
        foreach (var step in plan)
        {
            var oldName = step.Item2;
            var newName = step.Item3.Name;
            FamilyParameter after;
            if (!now.TryGetValue(newName, out after))
            {
                wrong.Add("'" + newName + "' could not be found in the family afterwards");
                continue;
            }
            Guid? guid = null;
            try { if (after.IsShared) guid = after.GUID; } catch (Exception) { }
            if (!guid.HasValue || guid.Value != step.Item3.GUID)
                wrong.Add("'" + newName + "' does not read back as the office's shared parameter with GUID " + step.Item3.GUID);
            if (oldName != newName && now.ContainsKey(oldName))
                wrong.Add("a family parameter is still called '" + oldName + "'");
            if (after.IsInstance != scopeBefore[oldName])
                wrong.Add("'" + newName + "' is " + (after.IsInstance ? "instance" : "type") + " where '" + oldName + "' was "
                    + (scopeBefore[oldName] ? "instance" : "type"));
            var groupNow = groupOf(after.Definition);
            if (!sameKind(groupNow, groupWanted[oldName]))
                wrong.Add("'" + newName + "' sits under " + groupName(groupNow) + ", not " + groupName(groupWanted[oldName]));
            if (after.IsReporting != reportingBefore[oldName])
                wrong.Add("'" + newName + "' is " + (after.IsReporting ? "" : "not ") + "a reporting parameter where '"
                    + oldName + "' was " + (reportingBefore[oldName] ? "" : "not "));

            var labelsWere = labelsBefore.ContainsKey(oldName) ? labelsBefore[oldName] : 0;
            var labelsAre = labelsAfter.ContainsKey(newName) ? labelsAfter[newName] : 0;
            if (labelsAre != labelsWere)
                wrong.Add("'" + newName + "' labels " + labelsAre + " dimension(s), where '" + oldName + "' labelled " + labelsWere);

            var tiedNow = tiedTo(after);
            if (tiedNow != tiedBefore[oldName])
                wrong.Add("'" + oldName + "' was tied to [" + tiedBefore[oldName] + "] and '" + newName + "' is tied to ["
                    + tiedNow + "]");

            var valuesNow = valuesOf(after);
            if (valuesNow != valuesBefore[oldName])
                wrong.Add("'" + oldName + "' held [" + valuesBefore[oldName] + "] and '" + newName + "' holds [" + valuesNow + "]");

            var keptParts = new List<string>();
            if (labelsAre > 0) keptParts.Add(labelsAre + " labelled dimension(s)");
            if (tiedNow.Length > 0) keptParts.Add(tiedNow);
            if (valuesNow.Length > 0) keptParts.Add("values " + valuesNow);
            if (keptParts.Count > 0) kept.Add(newName + ": " + string.Join(", ", keptParts));
            replaced.Add(oldName + " -> " + newName + " (" + kindLabel(step.Item3) + ", "
                + (after.IsInstance ? "instance" : "type") + ", under " + groupName(groupNow) + ", GUID " + step.Item3.GUID + ")");
        }

        var formulaLines = new List<string>();
        foreach (var entry in formulaBefore)
        {
            // A replaced parameter is found again under its shared name.
            var nameNow = newFor.ContainsKey(entry.Key) ? newFor[entry.Key] : entry.Key;
            FamilyParameter after;
            if (!now.TryGetValue(nameNow, out after)) continue;
            string formulaNow = null;
            try { formulaNow = after.Formula; } catch (Exception) { }
            formulaNow = formulaNow ?? "";
            var oldRead = readsBefore[entry.Key].Where(n => newFor.ContainsKey(n)).ToList();
            if (oldRead.Count == 0)
            {
                if (formulaNow != entry.Value)
                    wrong.Add("'" + nameNow + "' formula changed from '" + entry.Value + "' to '" + formulaNow
                        + "', though it read none of the parameters replaced");
                else if (newFor.ContainsKey(entry.Key) && formulaNow.Length > 0)
                    formulaLines.Add(nameNow + " = " + formulaNow);
                continue;
            }
            var readNow = namesReadFrom(formulaNow, namesAfter);
            foreach (var oldName in oldRead)
            {
                if (oldName != newFor[oldName] && readNow.Contains(oldName))
                    wrong.Add("'" + nameNow + "' formula '" + formulaNow + "' still reads '" + oldName + "'");
                if (!readNow.Contains(newFor[oldName]))
                    wrong.Add("'" + nameNow + "' formula '" + formulaNow + "' does not read '" + newFor[oldName] + "'");
            }
            formulaLines.Add(nameNow + " = " + formulaNow);
        }

        if (wrong.Count > 0)
            throw new InvalidOperationException("Read back after replacing: " + string.Join("; ", wrong)
                + ". NOTHING from this call was kept.");

        formulasReadBack = formulaLines.Count == 0
            ? "no formula in the family reads the parameters replaced, and none of them has one"
            : string.Join("; ", formulaLines);
        keptLinks = kept.Count == 0 ? "none of them is associated with anything, labels a dimension or holds a value"
            : string.Join("; ", kept);

        findings.Add("Replaced " + replaced.Count + " family parameter(s) with the office's shared ones from \""
            + sharedFile + "\", read back by name and GUID: " + string.Join("; ", replaced) + ".");
        findings.Add("Formulas read back with the shared names: " + formulasReadBack + ".");
        findings.Add("Kept as they were: " + keptLinks + ".");
        if (instance)
            findings.Add("instance was not used: each kept the scope it had. Switching type and instance is "
                + "ADD_FAMILY_PARAMETERS with switchExisting, a call of its own.");

        // THE TYPE CATALOGUE, when the family is saved and has one beside it -
        // its first line still names the old parameters.
        try
        {
            if (!string.IsNullOrEmpty(doc.PathName))
            {
                var cataloguePath = System.IO.Path.ChangeExtension(doc.PathName, ".txt");
                if (System.IO.File.Exists(cataloguePath))
                {
                    var header = System.IO.File.ReadLines(cataloguePath).FirstOrDefault() ?? "";
                    var delimiter = header.Length > 0 && !char.IsLetterOrDigit(header[0]) && header[0] != '"'
                        ? header[0] : ',';
                    var columns = new List<string>();
                    foreach (var column in header.Split(delimiter))
                    {
                        var cut = column.IndexOf("##", StringComparison.Ordinal);
                        var columnName = (cut >= 0 ? column.Substring(0, cut) : column).Trim().Trim('"').Trim();
                        if (columnName.Length > 0) columns.Add(columnName);
                    }
                    var inCatalogue = oldNames.Where(n => columns.Contains(n) && n != newFor[n]).ToList();
                    if (inCatalogue.Count > 0)
                        findings.Add("The type catalogue beside this family still names " + string.Join(", ", inCatalogue)
                            + " in its first line - rename those columns in the .txt by hand to the shared names, or the "
                            + "catalogue no longer fills them.");
                }
            }
        }
        catch (Exception) { }
    }
}

if (refused != null) findings.Add(refused);
