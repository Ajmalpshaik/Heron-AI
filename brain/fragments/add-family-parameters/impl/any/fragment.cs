// NOT STANDALONE. Assumes `doc`, `parameterNames`, `kind`, `instance`,
// `parameterGroup` and `switchExisting` are in scope; leaves `added`,
// `alreadyThere`, `switched`, `scopeReadBack`, `keptLinks`, `notAFamily`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// ===========================================================================
// THE CREATING CALL MOVED ACROSS THE VERSION RANGE, SO IT IS NEVER WRITTEN DOWN
// ===========================================================================
//
//   FamilyManager.AddParameter(name, GROUP, KIND, isInstance)
//       took BuiltInParameterGroup and ParameterType up to Revit 2022, and two
//       ForgeTypeIds from 2022. Neither spelling compiles on 2020 AND 2027, so
//       the overload is picked at run time by its OWN third parameter's type,
//       newest first - never by asking whether ForgeTypeId exists, because a
//       release can ship the type while a given call still takes the old enum.
//       The kind and the group are then looked up BY NAME in whichever world
//       that overload lives in.
//
// A NAME THE FAMILY ALREADY HAS IS LEFT ALONE, and named. Re-running a request
// must not add a second one, and a different kind under a familiar name is
// said out loud rather than built on.
//
// SWITCHING, NOT ADDING, when `switchExisting` is true: the names are
// parameters ALREADY in the family - or `all` - and `instance` is the scope
// they are moved to. The SWITCH section at the end has its rules.
//
// ALL OR NOTHING. Names, kind and group are checked before the first add. A
// refusal from Revit after that THROWS, so the host rolls the whole call back:
// a Revit exception caught inside a transaction can leave it marked failed
// while later writes appear to succeed - reported from an earlier family
// build - and a list half-added is harder to see than one not added at all.

var findings = new List<string>();
var added = new List<string>();
var alreadyThere = new List<string>();
var notAFamily = false;
string refused = null;
var switched = new List<string>();
var scopeReadBack = "";
var keptLinks = "";

var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;

// The namespace is taken from a type already in scope rather than written out:
// the structure check refuses the API namespace spelled inside a fragment.
var dbNamespace = typeof(Document).Namespace;

// TWO KINDS ARE THE SAME KIND WHATEVER VERSION THEIR IDS CARRY. A ForgeTypeId
// names its version - "...:length-2.0.0" - and plain equality may compare it,
// so a length a template made under an older version would read as some other
// kind. NameEquals compares the name alone, from 2021; before that a kind is
// an enum and plain equality is exact.
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

// A ForgeTypeId prints as its CLASS name, so a kind or a group this cannot
// label is shown by its id string instead - empty for the "Other" group.
Func<object, string> idText = value =>
{
    if (value == null) return "";
    var typeId = value.GetType().GetProperty("TypeId");
    if (typeId == null) return value.ToString();
    try { return (typeId.GetValue(value) as string) ?? ""; }
    catch (Exception) { return ""; }
};

// THE KINDS. Each under its old enum name, and the SpecTypeId class and
// property that replaced it. "Text" and "YesNo" are filed under nested classes
// in the newer world.
var kinds = new Dictionary<string, string[]>
{
    { "length",   new[] { "Length",      "SpecTypeId",           "Length" } },
    { "number",   new[] { "Number",      "SpecTypeId",           "Number" } },
    { "integer",  new[] { "Integer",     "SpecTypeId+Int",       "Integer" } },
    { "angle",    new[] { "Angle",       "SpecTypeId",           "Angle" } },
    { "area",     new[] { "Area",        "SpecTypeId",           "Area" } },
    { "volume",   new[] { "Volume",      "SpecTypeId",           "Volume" } },
    { "yesno",    new[] { "YesNo",       "SpecTypeId+Boolean",   "YesNo" } },
    { "text",     new[] { "Text",        "SpecTypeId+String",    "Text" } },
    { "material", new[] { "Material",    "SpecTypeId+Reference", "Material" } },
    { "airflow",  new[] { "HVACAirflow", "SpecTypeId",           "AirFlow" } },
};

// "Yes/No", "yes no" and "YesNo" are one kind; "Air Flow" and "airflow" too.
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

// AddParameter(name, GROUP, KIND, isInstance), picked by its own signature.
// The (name, group, Category, isInstance) overload makes a family-TYPE
// parameter, which is a different job, so it is excluded by its third type.
var addParameter = typeof(FamilyManager).GetMethods()
    .Where(m => m.Name == "AddParameter")
    .Where(m =>
    {
        var p = m.GetParameters();
        return p.Length == 4
            && p[0].ParameterType == typeof(string)
            && p[2].ParameterType != typeof(Category)
            && p[3].ParameterType == typeof(bool);
    })
    .OrderBy(m => m.GetParameters()[2].ParameterType.Name == "ForgeTypeId" ? 0 : 1)
    .FirstOrDefault();

var groupType = addParameter == null ? null : addParameter.GetParameters()[1].ParameterType;
var specType = addParameter == null ? null : addParameter.GetParameters()[2].ParameterType;

Func<string[], object> specFor = names =>
{
    try
    {
        if (specType.Name != "ForgeTypeId") return Enum.Parse(specType, names[0]);
        var owner = revitAssembly.GetType(dbNamespace + "." + names[1]);
        var property = owner == null ? null : owner.GetProperty(names[2], flags);
        return property == null ? null : property.GetValue(null);
    }
    catch (Exception)
    {
        return null;
    }
};

// EVERY GROUP THIS REVIT OFFERS, under the label the Properties palette shows.
// Matched by that label because that is what a modeller reads and types.
var groups = new List<KeyValuePair<string, object>>();
var groupInternalNames = new Dictionary<object, string>();
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
            // INVALID is the palette's "Other", and has no label of its own.
            if (text == null && value.ToString() == "INVALID") text = "Other";
            groups.Add(new KeyValuePair<string, object>(text ?? value.ToString(), value));
            groupInternalNames[value] = value.ToString();
        }
    }
    else
    {
        var owner = revitAssembly.GetType(dbNamespace + ".GroupTypeId");
        var label = typeof(LabelUtils).GetMethod("GetLabelForGroup", new[] { groupType });
        if (owner != null)
        {
            foreach (var property in owner.GetProperties(flags))
            {
                if (property.PropertyType != groupType) continue;
                var value = property.GetValue(null);
                if (value == null) continue;
                string text = null;
                try { text = label == null ? null : (string)label.Invoke(null, new[] { value }); }
                catch (Exception) { }
                groups.Add(new KeyValuePair<string, object>(text ?? property.Name, value));
                groupInternalNames[value] = property.Name;
            }
        }

        // "OTHER" HAS NO GroupTypeId PROPERTY - it is the EMPTY id, where the
        // older releases had INVALID. Offered so the same word works on every
        // release; whether AddParameter takes an empty group is unproven, and a
        // refusal from Revit rolls the call back with its own words.
        var none = Activator.CreateInstance(groupType);
        groups.Add(new KeyValuePair<string, object>("Other", none));
        groupInternalNames[none] = "Other";
    }
}

// THE DEFAULT GROUP, by its internal name in each world: PG_GEOMETRY is
// GroupTypeId.Geometry, which the palette calls Dimensions.
Func<string, string, object> groupNamed = (enumName, propertyName) =>
{
    foreach (var entry in groupInternalNames)
        if (entry.Value == enumName || entry.Value == propertyName) return entry.Key;
    return null;
};

// What a parameter already in the family IS - its kind's label, its scope and
// its group - read the way its own release allows.
Func<FamilyParameter, object> specOf = p =>
{
    try
    {
        var definition = p.Definition;
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null) return getDataType.Invoke(definition, null);
        var property = definition.GetType().GetProperty("ParameterType");
        return property == null ? null : property.GetValue(definition);
    }
    catch (Exception)
    {
        return null;
    }
};

Func<object, string> kindLabel = spec =>
{
    if (spec == null) return "an unknown kind";
    try
    {
        var label = spec.GetType().Name == "ForgeTypeId"
            ? typeof(LabelUtils).GetMethod("GetLabelForSpec", new[] { spec.GetType() })
            : typeof(LabelUtils).GetMethod("GetLabelFor", new[] { spec.GetType() });
        if (label != null) return (string)label.Invoke(null, new[] { spec });
    }
    catch (Exception) { }
    var shown = idText(spec);
    return shown.Length > 0 ? shown : "an unknown kind";
};

Func<FamilyParameter, string> groupLabel = p =>
{
    try
    {
        var definition = p.Definition;
        object value = null;
        var getGroup = definition.GetType().GetMethod("GetGroupTypeId", System.Type.EmptyTypes);
        if (getGroup != null) value = getGroup.Invoke(definition, null);
        else
        {
            var property = definition.GetType().GetProperty("ParameterGroup");
            if (property != null) value = property.GetValue(definition);
        }
        if (value == null) return "no group";
        foreach (var entry in groups)
            if (sameKind(entry.Value, value)) return entry.Key;
        var shown = idText(value);
        return shown.Length > 0 ? shown : "Other";
    }
    catch (Exception)
    {
        return "a group that could not be read";
    }
};

Func<FamilyParameter, string> describe = p =>
    kindLabel(specOf(p)) + ", " + (p.IsInstance ? "instance" : "type") + ", under " + groupLabel(p);

// ---------------------------------------------------------------------------
// CHECK EVERYTHING BEFORE THE FIRST WRITE
// ---------------------------------------------------------------------------

var names = new List<string>();
if (parameterNames != null)
    foreach (var raw in parameterNames)
    {
        var trimmed = raw == null ? "" : raw.Trim();
        if (trimmed.Length > 0) names.Add(trimmed);
    }

var kindKey = squash(kind);
string[] kindNames = null;
if (kindKey.Length > 0 && kinds.ContainsKey(kindKey)) kindNames = kinds[kindKey];

var repeated = names.GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
    .Where(g => g.Count() > 1)
    .Select(g => g.Key)
    .ToList();

object specValue = null;
object groupValue = null;
var wantedGroup = parameterGroup == null ? "" : parameterGroup.Trim();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor, so it "
        + "takes no family parameters. A project parameter is ADD_PROJECT_PARAMETER's job; to work on "
        + "a family, open it first.";
}
else if (switchExisting)
{
    // Checked in the SWITCH section below: the kind and the group are not
    // used, and the names are parameters the family already holds.
}
else if (addParameter == null)
{
    refused = "This Revit has no AddParameter(name, group, kind, instance) that this recognises, so "
        + "nothing was added.";
}
else if (names.Count == 0)
{
    refused = "No parameter names were given - comma separated, \"Width, Depth, Height\".";
}
else if (repeated.Count > 0)
{
    refused = "Named twice: " + string.Join(", ", repeated) + ". A family cannot hold two parameters "
        + "of one name, so nothing was added.";
}
else if (kindNames == null)
{
    refused = "\"" + (kind ?? "") + "\" is not a kind this adds. It takes Length, Number, Integer, "
        + "Angle, Area, Volume, YesNo, Text, Material or Air Flow - what the parameter HOLDS.";
}
else
{
    specValue = specFor(kindNames);

    if (wantedGroup.Length == 0)
    {
        groupValue = (kindKey == "length" || kindKey == "angle")
            ? groupNamed("PG_GEOMETRY", "Geometry")
            : groupNamed("PG_DATA", "Data");
    }
    else
    {
        foreach (var entry in groups)
            if (string.Equals(entry.Key.Trim(), wantedGroup, StringComparison.OrdinalIgnoreCase))
            { groupValue = entry.Value; break; }

        if (groupValue == null)
            foreach (var entry in groupInternalNames)
                if (string.Equals(entry.Value, wantedGroup, StringComparison.OrdinalIgnoreCase))
                { groupValue = entry.Key; break; }
    }

    if (specValue == null)
    {
        refused = "This Revit has no \"" + kindNames[0] + "\" kind for a family parameter, so nothing "
            + "was added.";
    }
    else if (groupValue == null)
    {
        var offered = groups.Select(g => g.Key).Where(k => k.Length > 0).Distinct().OrderBy(k => k)
            .Take(12).ToList();
        refused = (wantedGroup.Length == 0
                ? "The default group could not be found in this Revit"
                : "No group called \"" + wantedGroup + "\" exists in this Revit")
            + ", so nothing was added. Groups are the headings of the Properties palette - for "
            + "example " + string.Join(", ", offered) + ".";
    }
}

// ---------------------------------------------------------------------------
// ADD
// ---------------------------------------------------------------------------

if (refused == null && !switchExisting)
{
    var fm = doc.FamilyManager;
    var toAdd = new List<string>();

    foreach (var name in names)
    {
        var existing = fm.get_Parameter(name);
        if (existing == null) { toAdd.Add(name); continue; }

        alreadyThere.Add(name + " (" + describe(existing) + ")");

        // A DIFFERENT PARAMETER UNDER A FAMILIAR NAME is the case worth a line
        // of its own. The next step would otherwise be built on it.
        var asked = sameKind(specOf(existing), specValue);
        if (!asked || existing.IsInstance != instance)
        {
            findings.Add("\"" + name + "\" is ALREADY in this family as " + describe(existing)
                + " - not the " + kindLabel(specValue) + ", " + (instance ? "instance" : "type")
                + " parameter asked for. It was left as it is. Use another name, or change that one "
                + "by hand in Family Types.");
        }
    }

    foreach (var name in toAdd)
    {
        try
        {
            addParameter.Invoke(fm, new object[] { name, groupValue, specValue, instance });
        }
        catch (Exception ex)
        {
            var reason = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            throw new InvalidOperationException("Revit refused the parameter \"" + name + "\": " + reason
                + " NOTHING from this call was kept - the parameters in it are added together or "
                + "not at all.");
        }

        // READ BACK BY NAME. The call returning is not the family holding it.
        var madeNow = fm.get_Parameter(name);
        if (madeNow == null)
            throw new InvalidOperationException("\"" + name + "\" was added and could not be found by "
                + "name afterwards, so nothing from this call was kept.");

        added.Add(name + " (" + describe(madeNow) + ")");
    }

    if (added.Count > 0)
    {
        findings.Add("Added " + added.Count + " parameter(s), read back from the family: "
            + string.Join("; ", added) + ".");
        findings.Add("They hold NO value yet - a new length reads 0 in every type. Write the sizes "
            + "with SET_FAMILY_TYPE_VALUES before labelling any dimension with them: a label on a "
            + "parameter that reads 0 drives its planes to 0.");
    }

    if (alreadyThere.Count > 0)
        findings.Add(alreadyThere.Count + " name(s) were already in the family and were left alone: "
            + string.Join("; ", alreadyThere) + ".");
}

// ===========================================================================
// SWITCH - parameters already in the family, between TYPE and INSTANCE
// ===========================================================================
//
// FamilyManager.MakeInstance and MakeType, unchanged from 2020 to 2027.
//
// A TYPE FORMULA CANNOT READ AN INSTANCE PARAMETER. So the state the family is
// left in is checked as a whole before the first switch, and the switches are
// then made in the one order that never passes through a forbidden state:
// going to instance, a parameter moves BEFORE what its formula reads (C, then
// B, then A, where C reads B and B reads A); going to type, AFTER it (A, B,
// C). The order comes from the formulas, never from the order names arrive in.
//
// REFUSED BY NAME, BEFORE ANYTHING CHANGES:
//   - a switch that would leave a type formula reading an instance parameter;
//   - to instance, a parameter a nested family's TYPE parameter is linked to -
//     Revit links a nested type parameter to a host type parameter only;
//   - to instance, a column of the family's type catalogue (the .txt beside
//     the saved .rfa) - a catalogue writes types, and an instance value is not
//     a type's;
//   - a built-in parameter, whose scope is the category's, not the family's.
// Anything else Revit refuses part-way THROWS with Revit's own words first,
// and the host rolls the whole call back.
//
// READ BACK, ALL OR NOTHING. Every parameter named is read again by name
// afterwards - its scope, its formula, what is associated with it and how many
// dimensions carry it as their label - and anything not as intended throws.

if (refused == null && switchExisting)
{
    var fm = doc.FamilyManager;
    var wantInstance = instance;
    Func<bool, string> scopeOf = isInstance => isInstance ? "instance" : "type";
    var scopeWord = scopeOf(wantInstance);

    Func<FamilyParameter, bool> isBuiltIn = p =>
    {
        var internalDefinition = p.Definition as InternalDefinition;
        return internalDefinition != null
            && internalDefinition.BuiltInParameter != BuiltInParameter.INVALID;
    };

    var everyParameter = new List<FamilyParameter>();
    foreach (FamilyParameter p in fm.Parameters)
        if (p.Definition != null) everyParameter.Add(p);

    // The family's own parameters win a name over a built-in of the same name.
    var byName = new Dictionary<string, FamilyParameter>(StringComparer.Ordinal);
    foreach (var p in everyParameter.OrderBy(x => isBuiltIn(x) ? 1 : 0))
        if (!byName.ContainsKey(p.Definition.Name)) byName[p.Definition.Name] = p;

    // WHAT EACH FORMULA READS. Names are written bare in a Revit formula, spaces
    // and all, so each is looked for whole - longest first, and blanked once
    // found, so "Width" is not found a second time inside "Neck Width". Text in
    // quotes is blanked before anything: a word in quotes is a value.
    var namesLongestFirst = byName.Keys.OrderByDescending(n => n.Length).ToList();
    Func<string, List<string>> namesRead = formula =>
    {
        var found = new List<string>();
        if (string.IsNullOrEmpty(formula)) return found;
        var letters = formula.ToCharArray();
        var inQuotes = false;
        for (var i = 0; i < letters.Length; i++)
        {
            if (letters[i] == '"') { inQuotes = !inQuotes; letters[i] = ' '; continue; }
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
                if (whole)
                {
                    if (!found.Contains(name)) found.Add(name);
                    work = work.Substring(0, at) + new string(' ', name.Length) + work.Substring(end);
                }
                at = end;
            }
        }
        return found;
    };

    var formulaBefore = new Dictionary<string, string>(StringComparer.Ordinal);
    var readsOf = new Dictionary<FamilyParameter, List<FamilyParameter>>();
    foreach (var p in everyParameter)
    {
        string formula = null;
        try { formula = p.Formula; } catch (Exception) { }
        formula = formula ?? "";
        // KEYED BY NAME FROM THE PARAMETER THE NAME SELECTS, so a built-in that
        // shares a family parameter's name never stands in for its formula.
        if (byName[p.Definition.Name] == p) formulaBefore[p.Definition.Name] = formula;
        readsOf[p] = namesRead(formula).Select(n => byName[n]).Where(r => r != p).ToList();
    }

    // WHAT IS TIED TO A PARAMETER: everything associated with it - a connector's
    // size, a nested family's parameter - and the dimensions labelled with it.
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

    Func<FamilyParameter, List<string>> tiedTo = p =>
    {
        var list = new List<string>();
        try
        {
            foreach (Parameter tied in p.AssociatedParameters) list.Add(describeTied(tied));
        }
        catch (Exception) { }
        list.Sort(StringComparer.Ordinal);
        return list;
    };

    Func<FamilyParameter, Dictionary<string, int>, string> linkLine = (p, labels) =>
    {
        var name = p.Definition.Name;
        var parts = tiedTo(p);
        var labelCount = labels.ContainsKey(name) ? labels[name] : 0;
        if (labelCount > 0) parts.Add(labelCount + " labelled dimension(s)");
        return parts.Count == 0 ? "" : string.Join(", ", parts);
    };

    // THE TYPE CATALOGUE, when the family is saved and has one beside it.
    var catalogueColumns = new List<string>();
    try
    {
        if (!string.IsNullOrEmpty(doc.PathName))
        {
            var cataloguePath = System.IO.Path.ChangeExtension(doc.PathName, ".txt");
            if (System.IO.File.Exists(cataloguePath))
            {
                var header = System.IO.File.ReadLines(cataloguePath).FirstOrDefault() ?? "";
                foreach (var column in header.Split(','))
                {
                    var cut = column.IndexOf("##", StringComparison.Ordinal);
                    var columnName = (cut >= 0 ? column.Substring(0, cut) : column).Trim().Trim('"').Trim();
                    if (columnName.Length > 0) catalogueColumns.Add(columnName);
                }
            }
        }
    }
    catch (Exception) { }

    // ----- WHICH PARAMETERS -----
    var asked = new List<FamilyParameter>();
    var unknown = new List<string>();
    var builtIn = new List<string>();
    var everything = names.Count == 1 && string.Equals(names[0], "all", StringComparison.OrdinalIgnoreCase);

    // "all" IS A REAL NAME TOO. A family that holds a parameter called All
    // cannot tell the one from every one, so that spelling is refused rather
    // than read the wider way.
    var allIsAName = everything
        && byName.Keys.Any(n => string.Equals(n, "all", StringComparison.OrdinalIgnoreCase));
    if (allIsAName) everything = false;
    if (everything)
    {
        foreach (var p in everyParameter) if (!isBuiltIn(p) && !asked.Contains(p)) asked.Add(p);
    }
    else
    {
        foreach (var name in names)
        {
            FamilyParameter p;
            if (!byName.TryGetValue(name, out p)) { if (!unknown.Contains(name)) unknown.Add(name); continue; }
            if (isBuiltIn(p)) { if (!builtIn.Contains(name)) builtIn.Add(name); continue; }
            if (!asked.Contains(p)) asked.Add(p);
        }
    }

    var toSwitch = asked.Where(p => p.IsInstance != wantInstance).ToList();
    var leftAlone = asked.Where(p => p.IsInstance == wantInstance).ToList();
    Func<FamilyParameter, bool> endsInstance = p => toSwitch.Contains(p) ? wantInstance : p.IsInstance;

    // ----- REFUSE BY NAME, BEFORE ANYTHING CHANGES -----
    var reasons = new List<string>();
    if (names.Count == 0)
        reasons.Add("No parameter names were given - name them, comma separated, or give all for every "
            + "parameter the family made itself.");
    if (unknown.Count > 0)
        reasons.Add("Not in this family: " + string.Join(", ", unknown) + ". Names are matched exactly, "
            + "as Family Types shows them.");
    if (builtIn.Count > 0)
        reasons.Add("Built in to the category, so their scope is not the family's to change: "
            + string.Join(", ", builtIn) + ".");

    foreach (var p in everyParameter)
    {
        if (endsInstance(p)) continue;
        foreach (var r in readsOf[p])
        {
            if (!endsInstance(r)) continue;
            reasons.Add("'" + p.Definition.Name + "' would be a TYPE parameter whose formula '"
                + formulaBefore[p.Definition.Name] + "' reads '" + r.Definition.Name + "', which "
                + (toSwitch.Contains(r) ? "this would make instance" : "stays instance")
                + " - a type formula cannot read an instance parameter. "
                + (wantInstance
                    ? "Switch '" + p.Definition.Name + "' too, or leave '" + r.Definition.Name + "' as type."
                    : "Switch '" + r.Definition.Name + "' too, or leave '" + p.Definition.Name + "' as instance."));
        }
    }

    if (allIsAName)
        reasons.Add("This family has a parameter called '" + names[0] + "', so all could mean that one "
            + "or every one. Name the parameters instead.");

    // AN IMAGE PARAMETER IS TYPE ONLY - Revit's MakeInstance throws on one - so
    // it is refused here by name rather than part-way through.
    Func<FamilyParameter, bool> isImage = p =>
    {
        var spec = specOf(p);
        if (spec == null) return false;
        var text = spec.GetType().Name == "ForgeTypeId" ? idText(spec) : spec.ToString();
        return text == "Image" || text.ToLowerInvariant().Contains(":image-");
    };

    if (wantInstance)
    {
        foreach (var p in toSwitch)
        {
            if (isImage(p))
                reasons.Add("'" + p.Definition.Name + "' is an Image parameter, and Revit keeps an image "
                    + "parameter as type only.");
            var typeLinks = tiedTo(p).Where(t => t.Contains("(TYPE)")).ToList();
            if (typeLinks.Count > 0)
                reasons.Add("'" + p.Definition.Name + "' drives " + string.Join(", ", typeLinks)
                    + " - a nested family's TYPE parameter can be linked to a host TYPE parameter only, "
                    + "so it must stay type while that link stands.");
            if (catalogueColumns.Contains(p.Definition.Name))
                reasons.Add("'" + p.Definition.Name + "' is a column of the type catalogue beside this "
                    + "family - the catalogue writes it per type, so it must stay type.");
        }
    }

    if (reasons.Count > 0)
    {
        refused = "Nothing was switched. " + string.Join(" ", reasons);
    }
    else
    {
        // ----- THE ORDER, FROM THE FORMULAS -----
        var ordered = new List<FamilyParameter>();
        var pending = new List<FamilyParameter>(toSwitch);
        while (pending.Count > 0)
        {
            FamilyParameter next = null;
            foreach (var p in pending)
            {
                // To instance, wait for every pending parameter whose formula
                // reads this one; to type, for every pending one this one reads.
                var waits = wantInstance
                    ? pending.Any(o => o != p && readsOf[o].Contains(p))
                    : readsOf[p].Any(r => pending.Contains(r));
                if (!waits) { next = p; break; }
            }
            // A circle of formulas is something Revit itself refuses to hold;
            // if one is met anyway, Revit has the last word on the next switch.
            if (next == null) next = pending[0];
            ordered.Add(next);
            pending.Remove(next);
        }

        var labelsBefore = countLabels();
        var linksBefore = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var p in asked) linksBefore[p.Definition.Name] = linkLine(p, labelsBefore);

        // ----- SWITCH -----
        foreach (var p in ordered)
        {
            var name = p.Definition.Name;
            try
            {
                if (wantInstance) fm.MakeInstance(p);
                else fm.MakeType(p);
            }
            catch (Exception ex)
            {
                var reason = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                throw new InvalidOperationException(reason + " - Revit, making '" + name + "' " + scopeWord
                    + ". NOTHING from this call was kept: the parameters in it are switched together or "
                    + "not at all.");
            }
        }

        // ----- READ BACK, ALL OR NOTHING -----
        var now = new Dictionary<string, FamilyParameter>(StringComparer.Ordinal);
        foreach (FamilyParameter p in fm.Parameters)
            if (p.Definition != null && !(isBuiltIn(p) && now.ContainsKey(p.Definition.Name)))
                now[p.Definition.Name] = p;
        var labelsAfter = countLabels();

        var readBack = new List<string>();
        var wrong = new List<string>();
        var kept = new List<string>();
        foreach (var p in asked)
        {
            var name = p.Definition.Name;
            FamilyParameter after;
            if (!now.TryGetValue(name, out after)) { wrong.Add("'" + name + "' could not be found by name"); continue; }
            readBack.Add(name + " " + scopeOf(after.IsInstance));
            if (after.IsInstance != wantInstance) wrong.Add("'" + name + "' reads " + scopeOf(after.IsInstance));

            string formulaNow = null;
            try { formulaNow = after.Formula; } catch (Exception) { }
            if ((formulaNow ?? "") != formulaBefore[name])
                wrong.Add("'" + name + "' formula changed from '" + formulaBefore[name] + "' to '" + (formulaNow ?? "") + "'");

            var linksNow = linkLine(after, labelsAfter);
            if (linksNow != linksBefore[name])
                wrong.Add("'" + name + "' was tied to [" + linksBefore[name] + "] and is now tied to [" + linksNow + "]");
            else if (linksNow.Length > 0) kept.Add(name + ": " + linksNow);
        }

        if (wrong.Count > 0)
            throw new InvalidOperationException("Read back after switching: " + string.Join("; ", wrong)
                + ". NOTHING from this call was kept.");

        foreach (var p in ordered) switched.Add(p.Definition.Name + " (now " + scopeWord + ")");
        scopeReadBack = string.Join("; ", readBack);
        keptLinks = kept.Count == 0 ? "none of them is associated with anything or labels a dimension"
            : string.Join("; ", kept);

        findings.Add(ordered.Count == 0
            ? "Nothing to switch - every parameter named is already " + scopeWord + ". Read back: " + scopeReadBack + "."
            : "Switched " + ordered.Count + " parameter(s) to " + scopeWord + ", in the order the formulas allow: "
                + string.Join(", ", ordered.Select(p => p.Definition.Name)) + ". Read back from the family: "
                + scopeReadBack + ".");
        findings.Add("Kept as they were: " + keptLinks + ".");
        if (ordered.Count > 0 && leftAlone.Count > 0)
            findings.Add("Already " + scopeWord + ", left as they were: "
                + string.Join(", ", leftAlone.Select(p => p.Definition.Name)) + ".");
    }
}

if (refused != null) findings.Add(refused);
