// NOT STANDALONE. Assumes `doc`, `typeName` and `values` are in scope; leaves
// `typeUsed`, `typeCreated`, `written`, `drives`, `notAFamily`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE PARAMETER'S KIND DECIDES THE UNIT, and the kind is read from the family.
// A length is millimetres, an angle degrees, a yes/no true or false. The kind
// moved across the version range - `Definition.ParameterType` up to 2022,
// `Definition.GetDataType()` from 2022 - so it is read by reflection and
// compared with each kind's SpecTypeId property, or enum name, rather than
// either spelling being written down.
//
// EVERYTHING IS CHECKED BEFORE THE FIRST WRITE. A missing parameter, a value
// that is not a number, a formula-driven or reporting parameter, a type name
// Revit would refuse - any one of them refuses the whole call with nothing
// written. After that, a refusal from Revit THROWS so the host rolls the
// whole call back: a Revit exception caught inside a transaction can leave it
// marked failed while later writes appear to succeed - reported from an
// earlier family build.
//
// THE ANSWER IS READ BACK from the type after the write, never taken from what
// was asked for.

var findings = new List<string>();
var typeUsed = "";
var typeCreated = false;
var written = new List<string>();
var drives = 0;
var notAFamily = false;
string refused = null;

var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;
var invariant = System.Globalization.CultureInfo.InvariantCulture;

Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);

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

// A spec by its SpecTypeId class and property - null on a release without it.
Func<string, string, object> specNamed = (owner, property) =>
{
    var type = revitAssembly.GetType(dbNamespace + "." + owner);
    var found = type == null ? null : type.GetProperty(property, flags);
    return found == null ? null : found.GetValue(null);
};

var kindsNew = new List<KeyValuePair<string, object>>
{
    new KeyValuePair<string, object>("length",  specNamed("SpecTypeId", "Length")),
    new KeyValuePair<string, object>("angle",   specNamed("SpecTypeId", "Angle")),
    new KeyValuePair<string, object>("number",  specNamed("SpecTypeId", "Number")),
    new KeyValuePair<string, object>("integer", specNamed("SpecTypeId+Int", "Integer")),
    new KeyValuePair<string, object>("yesno",   specNamed("SpecTypeId+Boolean", "YesNo")),
    new KeyValuePair<string, object>("text",    specNamed("SpecTypeId+String", "Text")),
};

var kindsOld = new Dictionary<string, string>
{
    { "Length", "length" }, { "Angle", "angle" }, { "Number", "number" },
    { "Integer", "integer" }, { "YesNo", "yesno" }, { "Text", "text" },
};

// What kind a parameter holds, as one of the six words this writes - or the
// kind's own name, which is then refused.
Func<FamilyParameter, string> kindOf = p =>
{
    try
    {
        var definition = p.Definition;
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null)
        {
            var spec = getDataType.Invoke(definition, null);
            if (spec == null) return "unknown";
            foreach (var entry in kindsNew)
                if (sameKind(entry.Value, spec)) return entry.Key;
            return spec.ToString();
        }
        var property = definition.GetType().GetProperty("ParameterType");
        var old = property == null ? null : property.GetValue(definition);
        if (old == null) return "unknown";
        string word;
        return kindsOld.TryGetValue(old.ToString(), out word) ? word : old.ToString();
    }
    catch (Exception)
    {
        return "unknown";
    }
};

// A value as the type holds it, in the unit it was typed in.
Func<FamilyType, FamilyParameter, string, string> shown = (type, p, kindWord) =>
{
    if (type == null || !type.HasValue(p)) return "(no value)";
    switch (kindWord)
    {
        case "length":
            var length = type.AsDouble(p);
            return length.HasValue ? mm(length.Value) + " mm" : "(no value)";
        case "angle":
            var angle = type.AsDouble(p);
            return angle.HasValue
                ? Math.Round(angle.Value * 180.0 / Math.PI, 4).ToString(invariant) + " degrees"
                : "(no value)";
        case "number":
            var number = type.AsDouble(p);
            return number.HasValue ? number.Value.ToString(invariant) : "(no value)";
        case "integer":
            var whole = type.AsInteger(p);
            return whole.HasValue ? whole.Value.ToString(invariant) : "(no value)";
        case "yesno":
            var flag = type.AsInteger(p);
            return flag.HasValue ? (flag.Value != 0 ? "yes" : "no") : "(no value)";
        default:
            var text = type.AsString(p);
            return text == null ? "(no value)" : "\"" + text + "\"";
    }
};

// ---------------------------------------------------------------------------
// CHECK EVERYTHING BEFORE THE FIRST WRITE
// ---------------------------------------------------------------------------

var wantedType = typeName == null ? "" : typeName.Trim();
var forbidden = "\\:{}[]|;<>?`~";
var badCharacters = wantedType.Where(c => forbidden.IndexOf(c) >= 0).Distinct().ToList();

// Each planned write: the parameter, its kind, the value in Revit's own unit.
var planned = new List<Tuple<FamilyParameter, string, object>>();
var problems = new List<string>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor, so it "
        + "has no family types to write. Values on elements in a project are "
        + "WRITE_ELEMENT_PARAMETERS' job.";
}
else if (wantedType.Length == 0)
{
    refused = "No type name was given. The values go into one named type - \"600 x 600\" - and a "
        + "new family has none until one is made.";
}
else if (badCharacters.Count > 0)
{
    refused = "Revit does not allow " + string.Join(" ", badCharacters.Select(c => c.ToString()))
        + " in a type name, so nothing was written. Change the name and ask again.";
}
else if (values == null || values.Count == 0)
{
    refused = "No values were given - name=value pairs with semicolons between, "
        + "\"Width=600; Depth=600\".";
}
else
{
    var fm = doc.FamilyManager;

    foreach (var pair in values)
    {
        var name = pair.Key == null ? "" : pair.Key.Trim();
        var text = pair.Value == null ? "" : pair.Value.Trim();
        var p = fm.get_Parameter(name);

        if (p == null)
        {
            // The same name in another case is offered, never taken.
            var near = new List<string>();
            foreach (FamilyParameter candidate in fm.Parameters)
                if (string.Equals(candidate.Definition.Name, name, StringComparison.OrdinalIgnoreCase))
                    near.Add(candidate.Definition.Name);
            problems.Add("\"" + name + "\" is not a parameter of this family"
                + (near.Count > 0 ? " - did you mean \"" + near[0] + "\"?" : " - ADD_FAMILY_PARAMETERS makes one."));
            continue;
        }

        if (p.IsDeterminedByFormula)
        {
            problems.Add("\"" + name + "\" is driven by the formula \"" + p.Formula + "\", so a typed "
                + "value cannot hold. Clearing the formula is SET_FAMILY_FORMULA with an empty formula.");
            continue;
        }

        if (p.IsReporting)
        {
            problems.Add("\"" + name + "\" is a REPORTING parameter - its value is read off the "
                + "geometry and cannot be typed in.");
            continue;
        }

        var kindWord = kindOf(p);
        double number;
        var isNumber = double.TryParse(text, System.Globalization.NumberStyles.Float, invariant, out number);

        switch (kindWord)
        {
            case "length":
                // NOT REFUSED FOR BEING ZERO OR BELOW. An offset can be either, and a
                // size Revit cannot take is refused by Revit, in its own words.
                if (!isNumber) { problems.Add("\"" + text + "\" is not a number for the length \"" + name + "\" - millimetres, digits only: 600, not 600mm."); break; }
                planned.Add(Tuple.Create(p, kindWord, (object)(number / 304.8)));
                break;
            case "angle":
                if (!isNumber) { problems.Add("\"" + text + "\" is not a number for the angle \"" + name + "\" - degrees, digits only."); break; }
                planned.Add(Tuple.Create(p, kindWord, (object)(number * Math.PI / 180.0)));
                break;
            case "number":
                if (!isNumber) { problems.Add("\"" + text + "\" is not a number for \"" + name + "\"."); break; }
                planned.Add(Tuple.Create(p, kindWord, (object)number));
                break;
            case "integer":
                int whole;
                if (!int.TryParse(text, System.Globalization.NumberStyles.Integer, invariant, out whole)) { problems.Add("\"" + text + "\" is not a whole number for \"" + name + "\"."); break; }
                planned.Add(Tuple.Create(p, kindWord, (object)whole));
                break;
            case "yesno":
                var lowered = text.ToLowerInvariant();
                if (lowered == "true" || lowered == "yes" || lowered == "1") planned.Add(Tuple.Create(p, kindWord, (object)1));
                else if (lowered == "false" || lowered == "no" || lowered == "0") planned.Add(Tuple.Create(p, kindWord, (object)0));
                else problems.Add("\"" + text + "\" is not true or false for the yes/no \"" + name + "\".");
                break;
            case "text":
                planned.Add(Tuple.Create(p, kindWord, (object)text));
                break;
            default:
                problems.Add("\"" + name + "\" holds a kind this does not write (" + kindWord + ") - a "
                    + "material, an area or a flow is set in the Family Types dialog for now.");
                break;
        }
    }

    if (problems.Count > 0)
        refused = "Nothing was written. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// WRITE
// ---------------------------------------------------------------------------

if (refused == null)
{
    var fm = doc.FamilyManager;

    FamilyType target = null;
    foreach (FamilyType existing in fm.Types)
        if (existing != null && string.Equals(existing.Name, wantedType, StringComparison.OrdinalIgnoreCase))
        { target = existing; break; }

    // What each parameter held before, in the type about to be written.
    var before = new Dictionary<string, string>();
    foreach (var plan in planned)
        before[plan.Item1.Definition.Name] = target == null ? "(new type)" : shown(target, plan.Item1, plan.Item2);

    try
    {
        if (target == null)
        {
            target = fm.NewType(wantedType);
            typeCreated = true;
        }

        if (fm.CurrentType == null || fm.CurrentType.Name != target.Name)
            fm.CurrentType = target;

        foreach (var plan in planned)
        {
            if (plan.Item3 is double) fm.Set(plan.Item1, (double)plan.Item3);
            else if (plan.Item3 is int) fm.Set(plan.Item1, (int)plan.Item3);
            else fm.Set(plan.Item1, (string)plan.Item3);
        }
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the values for type \"" + wantedType + "\": "
            + ex.Message + " NOTHING from this call was kept.");
    }

    doc.Regenerate();

    // READ BACK from the type the family now has current.
    var current = fm.CurrentType;
    typeUsed = current == null ? "" : current.Name;

    var labels = new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>()
        .Where(d => { try { return d.FamilyLabel != null; } catch (Exception) { return false; } })
        .ToList();

    var mismatched = new List<string>();
    foreach (var plan in planned)
    {
        var p = plan.Item1;
        var name = p.Definition.Name;
        var now = shown(current, p, plan.Item2);
        written.Add(name + " = " + now + " (was " + before[name] + ")");

        // Held what was asked? Compared in Revit's own unit.
        var held = true;
        if (plan.Item3 is double)
        {
            var value = current.AsDouble(p);
            held = value.HasValue && Math.Abs(value.Value - (double)plan.Item3) < 1e-6;
        }
        else if (plan.Item3 is int)
        {
            var value = current.AsInteger(p);
            held = value.HasValue && value.Value == (int)plan.Item3;
        }
        else
        {
            held = string.Equals(current.AsString(p) ?? "", (string)plan.Item3, StringComparison.Ordinal);
        }
        if (!held) mismatched.Add(name + " reads " + now);

        drives += labels.Count(d => d.FamilyLabel.Id == p.Id);
        try { drives += p.AssociatedParameters.Size; } catch (Exception) { }
    }

    findings.Add((typeCreated ? "Made the type \"" : "Wrote into the type \"") + typeUsed + "\", now the "
        + "family's current type. Read back: " + string.Join("; ", written) + ".");

    if (mismatched.Count > 0)
        findings.Add("NOT EVERY VALUE HELD what was asked for: " + string.Join("; ", mismatched)
            + ". Revit keeps the value its constraints allow - check the dimensions labelled with "
            + "these parameters.");

    findings.Add(drives == 0
        ? "Nothing in the family is labelled with or associated to these parameters yet, so no "
          + "geometry moved."
        : "These parameters drive " + drives + " labelled dimension(s) or associated element "
          + "parameter(s), and the geometry locked to them moved with the values.");
}

if (refused != null) findings.Add(refused);
