// NOT STANDALONE. Assumes `doc`, `parameterNames`, `kind`, `instance` and
// `parameterGroup` are in scope; leaves `added`, `alreadyThere`, `notAFamily`,
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
    return spec.ToString();
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
        return value.ToString();
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

if (refused == null)
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

if (refused != null) findings.Add(refused);
