// NOT STANDALONE. Assumes `doc`, `typeName`, `values` and `deleteType` are in
// scope; leaves `typeUsed`, `typeCreated`, `written`, `drives`, `typesNow`,
// `notAFamily`, `refused` and `findings` behind.
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
//
// A MATERIAL IS WRITTEN BY NAME, and only a material the family already holds.
// "Nut Material=Steel - Galvanised" finds that material exactly, or in another
// case when only one matches; absent, the call is refused naming the closest
// names, and none is ever made. "<By Category>" or "none" clears it. A URL, and
// any other kind Revit stores as text, is written as text.
//
// THE TYPE'S OWN NAME IS A ROW IN `values`, "Type Name=HexNut - ISO 4032", the
// way the Family Types dialog's Rename sits beside its rows (version 3). It is
// not a separate need because the binder refuses an absent text need
// (HeronBindingNote.AbsentValue), and a separate `renameTo` would have stopped
// every existing call. A rename never makes a type: the type named `typeName`
// must exist, and the new name may not be one another type already has - in
// any case, because Revit compares type names without case. A family with its
// OWN parameter called "Type Name" is refused rather than guessed at.
//
// `deleteType` REMOVES the type named `typeName` (FamilyManager.DeleteCurrentType)
// and does nothing else, so it is refused beside any value or a rename. Absent
// is false (`optional: true`). The last type is never deleted: a family with no
// type cannot hold a formula, and making one again is a different call.
//
// MANY TYPES IN ONE CALL (version 4, 2026-11-11). `typeName` may be "all" (or
// "every type") or a comma list - "600 x 600, 625 x 625" - and the same values
// go into every type it names, in ONE change: one entry in Revit's undo list,
// one Keep or Discard question. A name that is exactly one type's whole name is
// still that one type, as before. A list only writes types that EXIST: one name
// the family does not have refuses the whole call, nothing written, and a list
// never makes, renames or deletes a type. Every type is read back.
//
// REVIT WRITES A FAMILY TYPE ONLY WHILE IT IS THE CURRENT TYPE: FamilyType has
// readers and no setter, and FamilyManager.Set writes the current type (the
// 2020 RevitAPI.dll, read 2026-11-11). Making a type current is a regeneration,
// and on Revit 2020 that can delete an automatic sketch dimension - one
// "delete 1 element" question per type when each type was its own call
// (2026-11-11, three air terminal families of seven types). So:
//   - a type that already holds every value asked for is NOT made current;
//   - TEXT going into EVERY type of the family, where Revit lets the parameter
//     take a formula (CanAssignFormula) and the text holds no double quote, is
//     first tried with NO type made current: the text set as a formula on the
//     parameter - which Revit evaluates for every type at once - and the
//     formula cleared again, inside a sub-transaction, kept only if every type
//     then reads the text and no formula is left. Anything else rolls the trial
//     back and the types are made current in turn;
//   - every other kind - a length, an angle, a number, an integer, a yes/no, a
//     material - and text into only SOME types, makes each type current in
//     turn, as the Family Types dialog does.
// The type that was current before the call is made current again at the end,
// so the family is left showing the type it showed.

var findings = new List<string>();
var typeUsed = "";
var typeCreated = false;
var written = new List<string>();
var drives = 0;
var typesNow = "";
var notAFamily = false;
string refused = null;

// Version 4: the types a list or "all" named, those written, those that
// already held every value, and how they were written - "no type made current"
// or "each type made current in turn".
var askedMany = false;
var manyTypes = new List<string>();
var typesWritten = new List<string>();
var alreadyHeld = new List<string>();
var route = "";

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
    new KeyValuePair<string, object>("text",    specNamed("SpecTypeId+String", "Url")),
    new KeyValuePair<string, object>("text",    specNamed("SpecTypeId+String", "MultilineText")),
    new KeyValuePair<string, object>("material", specNamed("SpecTypeId+Reference", "Material")),
};

var kindsOld = new Dictionary<string, string>
{
    { "Length", "length" }, { "Angle", "angle" }, { "Number", "number" },
    { "Integer", "integer" }, { "YesNo", "yesno" }, { "Text", "text" },
    { "URL", "text" }, { "MultilineText", "text" }, { "Material", "material" },
};

// The material a material parameter holds, by name - "<By Category>" when none.
Func<FamilyType, FamilyParameter, string> materialShown = (type, p) =>
{
    ElementId id = null;
    try { id = type == null ? null : type.AsElementId(p); } catch (Exception) { }
    if (id == null || id == ElementId.InvalidElementId) return "<By Category>";
    var material = doc.GetElement(id);
    return material == null ? "(a material that is not in the family)" : "\"" + material.Name + "\"";
};

// How far apart two names are, in single-letter edits - to name the closest
// materials when the one asked for is not in the family.
Func<string, string, int> editsApart = (first, second) =>
{
    var a = first.ToLowerInvariant();
    var b = second.ToLowerInvariant();
    var row = Enumerable.Range(0, b.Length + 1).ToArray();
    for (var i = 1; i <= a.Length; i++)
    {
        var previous = row[0];
        row[0] = i;
        for (var j = 1; j <= b.Length; j++)
        {
            var kept = row[j];
            row[j] = Math.Min(Math.Min(row[j] + 1, row[j - 1] + 1), previous + (a[i - 1] == b[j - 1] ? 0 : 1));
            previous = kept;
        }
    }
    return row[b.Length];
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
            // A ForgeTypeId prints as its CLASS name, so the id string is shown.
            var typeId = spec.GetType().GetProperty("TypeId");
            var id = typeId == null ? null : typeId.GetValue(spec) as string;
            return string.IsNullOrEmpty(id) ? "an unnamed kind" : id;
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
    if (kindWord == "material") return materialShown(type, p);
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

// "Type Name" is lifted out of `values`; what is left are parameters.
const string typeNameRow = "Type Name";
string renameTo = null;
var parameterValues = new Dictionary<string, string>();
if (values != null)
    foreach (var pair in values)
    {
        if (string.Equals((pair.Key ?? "").Trim(), typeNameRow, StringComparison.OrdinalIgnoreCase))
            renameTo = (pair.Value ?? "").Trim();
        else
            parameterValues[pair.Key] = pair.Value;
    }

// Every type the family has, as one line - the answer a missing name is given.
Func<string> typeList = () =>
{
    var names = doc.FamilyManager.Types.Cast<FamilyType>().Where(t => t != null)
        .Select(t => "\"" + t.Name + "\"").ToList();
    return names.Count == 0 ? "none" : string.Join(", ", names);
};

// Each planned write: the parameter, its kind, the value in Revit's own unit.
var planned = new List<Tuple<FamilyParameter, string, object>>();
var problems = new List<string>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor, so it "
        + "has no family types to write. Values on elements in a project are "
        + "WRITE_ELEMENT_PARAMETERS' job; the same values on many of a project's TYPES are "
        + "SET_PARAMETER_VALUES_BY_ID's, a key with a * and types=yes.";
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
else if (deleteType && (renameTo != null || parameterValues.Count > 0))
{
    refused = "Deleting a type is done alone - values or a new name beside deleteType would be "
        + "written into a type that is then removed. Nothing was changed.";
}
else if (!deleteType && renameTo == null && parameterValues.Count == 0)
{
    refused = "No values were given - name=value pairs with semicolons between, "
        + "\"Width=600; Depth=600\", or \"Type Name=New name\" to rename the type.";
}
else
{
    var fm = doc.FamilyManager;
    List<Material> materials = null;

    FamilyType named = null;
    foreach (FamilyType existing in fm.Types)
        if (existing != null && string.Equals(existing.Name, wantedType, StringComparison.OrdinalIgnoreCase))
        { named = existing; break; }

    // VERSION 4 - "all" or a comma list names MANY types. A name that is one
    // type's whole name is that type, comma or not, exactly as before.
    var typeWord = wantedType.ToLowerInvariant();
    var everyWord = typeWord == "all" || typeWord == "all types" || typeWord == "every type" || typeWord == "*";
    if (everyWord && named != null)
    {
        problems.Add("The family has a type called \"" + named.Name + "\", so \"" + wantedType + "\" could "
            + "mean that type or every type - say \"every type\" for every type.");
    }
    else if (everyWord)
    {
        askedMany = true;
        foreach (FamilyType existing in fm.Types)
            if (existing != null) manyTypes.Add(existing.Name);
        if (manyTypes.Count == 0)
            problems.Add("The family has no type yet, so there is no type to write - name one, and it is made.");
    }
    else if (named == null && wantedType.IndexOf(',') >= 0)
    {
        askedMany = true;
        var unknown = new List<string>();
        foreach (var part in wantedType.Split(','))
        {
            var one = part.Trim();
            if (one.Length == 0) continue;
            FamilyType hit = null;
            foreach (FamilyType existing in fm.Types)
                if (existing != null && string.Equals(existing.Name, one, StringComparison.OrdinalIgnoreCase))
                { hit = existing; break; }
            if (hit == null) { if (!unknown.Contains(one)) unknown.Add(one); }
            else if (!manyTypes.Contains(hit.Name)) manyTypes.Add(hit.Name);
        }
        if (unknown.Count > 0)
            problems.Add("This family has no type " + string.Join(", ", unknown.Select(u => "\"" + u + "\""))
                + ", so NO type in the list was written - a list writes only types that exist and never "
                + "makes one. The family's types are " + typeList() + ".");
        else if (manyTypes.Count == 0)
            problems.Add("The list \"" + wantedType + "\" names no type.");
    }

    if (askedMany && deleteType)
        problems.Add("Types are deleted one at a time - a list or \"all\" deletes nothing. Name the one type.");
    if (askedMany && renameTo != null)
        problems.Add("A rename names one type - \"Type Name=\" cannot go into a list or \"all\".");

    if (deleteType && !askedMany)
    {
        if (named == null)
            problems.Add("There is no type \"" + wantedType + "\" to delete - the family's types are "
                + typeList() + ".");
        else if (fm.Types.Size <= 1)
            problems.Add("\"" + named.Name + "\" is the family's only type, and a family with no type "
                + "cannot hold a formula or a value - make another type first, then delete this one.");
    }

    if (renameTo != null && !askedMany)
    {
        var badNew = renameTo.Where(c => forbidden.IndexOf(c) >= 0).Distinct().ToList();
        // Another type with the new name, in any case - never the type being renamed.
        var clash = fm.Types.Cast<FamilyType>().FirstOrDefault(t => t != null
            && (named == null || t.Name != named.Name)
            && string.Equals(t.Name, renameTo, StringComparison.OrdinalIgnoreCase));
        var ownRow = fm.Parameters.Cast<FamilyParameter>().FirstOrDefault(fp =>
            string.Equals(fp.Definition.Name, typeNameRow, StringComparison.OrdinalIgnoreCase));

        if (ownRow != null)
            problems.Add("This family has its own parameter called \"" + ownRow.Definition.Name + "\", "
                + "so \"Type Name=\" could mean that parameter or the type's name - rename the "
                + "parameter first.");
        else if (renameTo.Length == 0)
            problems.Add("\"Type Name=\" was given with no new name.");
        else if (badNew.Count > 0)
            problems.Add("Revit does not allow " + string.Join(" ", badNew.Select(c => c.ToString()))
                + " in a type name, so \"" + renameTo + "\" cannot be the new name.");
        else if (named == null)
            problems.Add("There is no type \"" + wantedType + "\" to rename, and a rename never makes "
                + "one - the family's types are " + typeList() + ".");
        else if (clash != null)
            problems.Add("The family already has a type \"" + clash.Name + "\", and Revit does not "
                + "tell type names apart by case - choose another name for \"" + named.Name + "\".");
        else if (named.Name == renameTo)
            problems.Add("The type is already called \"" + renameTo + "\".");
    }

    foreach (var pair in parameterValues)
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
        var isNumber = double.TryParse(text, System.Globalization.NumberStyles.Float, invariant, out number)
            && !double.IsNaN(number) && !double.IsInfinity(number);

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
            case "material":
                var clearing = text.ToLowerInvariant();
                if (clearing == "<by category>" || clearing == "by category" || clearing == "none")
                {
                    planned.Add(Tuple.Create(p, kindWord, (object)ElementId.InvalidElementId));
                    break;
                }
                if (materials == null)
                    materials = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>().ToList();
                // EXACT FIRST; another case only when it names one material alone.
                var exact = materials.Where(m => string.Equals(m.Name, text, StringComparison.Ordinal)).ToList();
                if (exact.Count == 0)
                    exact = materials.Where(m => string.Equals(m.Name, text, StringComparison.OrdinalIgnoreCase)).ToList();
                if (exact.Count == 1)
                {
                    planned.Add(Tuple.Create(p, kindWord, (object)exact[0].Id));
                    break;
                }
                if (exact.Count > 1)
                {
                    problems.Add("\"" + text + "\" matches " + exact.Count + " materials that differ only in "
                        + "case (" + string.Join(", ", exact.Select(m => "\"" + m.Name + "\"")) + ") - type the one "
                        + "meant for \"" + name + "\" exactly.");
                    break;
                }
                var closest = materials.OrderBy(m => editsApart(m.Name, text)).ThenBy(m => m.Name)
                    .Take(3).Select(m => "\"" + m.Name + "\"").ToList();
                problems.Add("There is no material \"" + text + "\" in this family for \"" + name + "\", and "
                    + "none is made here"
                    + (closest.Count > 0 ? " - the closest are " + string.Join(", ", closest) + "." : " - the family holds no materials yet.")
                    + " Make or load it in Manage > Materials first, or \"<By Category>\" clears the parameter.");
                break;
            default:
                // ANY OTHER KIND REVIT STORES AS TEXT is written as text.
                if (p.StorageType == StorageType.String)
                {
                    planned.Add(Tuple.Create(p, "text", (object)text));
                    break;
                }
                problems.Add("\"" + name + "\" holds a kind this does not write (" + kindWord + ") - an "
                    + "area or a flow is set in the Family Types dialog for now.");
                break;
        }
    }

    if (problems.Count > 0)
        refused = "Nothing was written. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// WRITE
// ---------------------------------------------------------------------------

if (refused == null && deleteType)
{
    var fm = doc.FamilyManager;
    FamilyType doomed = null;
    foreach (FamilyType existing in fm.Types)
        if (existing != null && string.Equals(existing.Name, wantedType, StringComparison.OrdinalIgnoreCase))
        { doomed = existing; break; }
    var gone = doomed.Name;

    try
    {
        if (fm.CurrentType == null || fm.CurrentType.Name != gone)
            fm.CurrentType = doomed;
        fm.DeleteCurrentType();
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused to delete the type \"" + gone + "\": "
            + ex.Message + " NOTHING from this call was kept.");
    }

    doc.Regenerate();

    // READ BACK: the type is gone, and the family still has a current type.
    if (fm.Types.Cast<FamilyType>().Any(t => t != null && t.Name == gone))
        throw new InvalidOperationException("The type \"" + gone + "\" is still in the family after "
            + "the delete. NOTHING from this call was kept.");
    typeUsed = fm.CurrentType == null ? "" : fm.CurrentType.Name;
    findings.Add("Deleted the type \"" + gone + "\". The family's current type is now \"" + typeUsed
        + "\"; its types are " + typeList() + ".");
}

// ---------------------------------------------------------------------------
// VERSION 4 - WRITE MANY TYPES IN ONE CHANGE
// ---------------------------------------------------------------------------

if (refused == null && askedMany)
{
    var fm = doc.FamilyManager;
    var startName = fm.CurrentType == null ? null : fm.CurrentType.Name;

    // A type by name, fetched afresh - a FamilyType held across a rolled-back
    // sub-transaction or a switch is not trusted.
    Func<string, FamilyType> typeNamed = name =>
        fm.Types.Cast<FamilyType>().FirstOrDefault(t => t != null && t.Name == name);

    // Does this type hold this planned value now? Read from the type itself,
    // which needs no switch, compared in Revit's own unit.
    Func<FamilyType, Tuple<FamilyParameter, string, object>, bool> holdsPlan = (t, plan) =>
    {
        var p = plan.Item1;
        if (plan.Item3 is double)
        {
            var value = t.HasValue(p) ? t.AsDouble(p) : null;
            return value.HasValue && Math.Abs(value.Value - (double)plan.Item3) < 1e-6;
        }
        if (plan.Item3 is int)
        {
            var value = t.HasValue(p) ? t.AsInteger(p) : null;
            return value.HasValue && value.Value == (int)plan.Item3;
        }
        if (plan.Item3 is ElementId)
        {
            ElementId value = null;
            try { value = t.AsElementId(p); } catch (Exception) { }
            return (value ?? ElementId.InvalidElementId).Equals((ElementId)plan.Item3);
        }
        // A blank asked and nothing held read the same - Revit shows both empty.
        return string.Equals(t.AsString(p) ?? "", (string)plan.Item3, StringComparison.Ordinal);
    };

    // WHAT EACH TYPE HELD BEFORE, and which types need writing at all.
    var beforeMany = new Dictionary<string, string>();
    var toWrite = new List<string>();
    foreach (var name in manyTypes)
    {
        var t = typeNamed(name);
        foreach (var plan in planned)
            beforeMany[name + "|" + plan.Item1.Definition.Name] = shown(t, plan.Item1, plan.Item2);
        if (planned.All(plan => holdsPlan(t, plan))) alreadyHeld.Add(name);
        else toWrite.Add(name);
    }

    var everyType = manyTypes.Count == fm.Types.Cast<FamilyType>().Count(t => t != null);
    var movesGeometry = planned.Any(plan => plan.Item2 != "text" && plan.Item2 != "material");

    // THE WAY WITH NO TYPE MADE CURRENT - text into EVERY type: a formula that
    // is the text itself, evaluated by Revit for every type at once, then the
    // formula cleared. Tried inside a sub-transaction and kept only if every
    // type reads the text afterwards and no formula is left behind.
    var formulaTried = false;
    var formulaReady = toWrite.Count > 0 && everyType && planned.All(plan =>
    {
        if (plan.Item2 != "text" || !(plan.Item3 is string)) return false;
        var text = (string)plan.Item3;
        if (text.IndexOf('"') >= 0 || text.IndexOf('\n') >= 0 || text.IndexOf('\r') >= 0) return false;
        try { return plan.Item1.CanAssignFormula; } catch (Exception) { return false; }
    });
    if (formulaReady)
    {
        formulaTried = true;
        using (var trial = new SubTransaction(doc))
        {
            trial.Start();
            var took = false;
            try
            {
                foreach (var plan in planned) fm.SetFormula(plan.Item1, "\"" + (string)plan.Item3 + "\"");
                doc.Regenerate();
                foreach (var plan in planned) fm.SetFormula(plan.Item1, null);
                doc.Regenerate();
                took = planned.All(plan => !plan.Item1.IsDeterminedByFormula)
                    && toWrite.All(name => planned.All(plan => holdsPlan(typeNamed(name), plan)));
            }
            catch (Exception)
            {
                // Revit refused the formula: the trial is rolled back whole and
                // the types are made current in turn instead.
                took = false;
            }
            if (took) { trial.Commit(); route = "no type made current"; }
            else trial.RollBack();
        }
    }

    // THE WAY THE FAMILY TYPES DIALOG WRITES - each type made current in turn.
    // Revit posts a size its constraints cannot take as a WARNING and keeps the
    // value while the planes stay put, so where a value can move geometry every
    // labelled dimension that agreed with its parameter in that type before
    // the write must agree after it.
    var broken = new List<string>();
    if (route.Length == 0 && toWrite.Count > 0)
    {
        route = "each type made current in turn";
        Func<List<Dimension>> labelledNow = () => new FilteredElementCollector(doc).OfClass(typeof(Dimension))
            .Cast<Dimension>()
            .Where(d => { try { return d.FamilyLabel != null; } catch (Exception) { return false; } })
            .ToList();
        Func<Dimension, bool> agrees = d =>
        {
            var holds = fm.CurrentType == null ? null : fm.CurrentType.AsDouble(d.FamilyLabel);
            var reads = d.Value;
            return holds.HasValue && reads.HasValue && Math.Abs(holds.Value - reads.Value) < 0.01 / 304.8;
        };

        foreach (var name in toWrite)
        {
            var t = typeNamed(name);
            HashSet<string> agreedHere = null;
            try
            {
                if (fm.CurrentType == null || fm.CurrentType.Name != name) fm.CurrentType = t;
                if (movesGeometry)
                {
                    doc.Regenerate();
                    agreedHere = new HashSet<string>(labelledNow().Where(d => agrees(d)).Select(d => d.UniqueId));
                }
                foreach (var plan in planned)
                {
                    if (plan.Item3 is double) fm.Set(plan.Item1, (double)plan.Item3);
                    else if (plan.Item3 is int) fm.Set(plan.Item1, (int)plan.Item3);
                    else if (plan.Item3 is ElementId) fm.Set(plan.Item1, (ElementId)plan.Item3);
                    else fm.Set(plan.Item1, (string)plan.Item3);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit refused the values for type \"" + name + "\": "
                    + ex.Message + " NOTHING from this call was kept - no type was written.");
            }
            if (movesGeometry)
            {
                doc.Regenerate();
                var current = fm.CurrentType;
                broken.AddRange(labelledNow()
                    .Where(d => agreedHere.Contains(d.UniqueId))
                    .Where(d => !agrees(d))
                    .Select(d => "in \"" + name + "\" the dimension labelled " + d.FamilyLabel.Definition.Name
                        + " reads " + (d.Value.HasValue ? mm(d.Value.Value) + " mm" : "nothing")
                        + " where the parameter holds "
                        + (current.AsDouble(d.FamilyLabel).HasValue ? mm(current.AsDouble(d.FamilyLabel).Value) + " mm" : "nothing")));
            }
        }

        // THE TYPE THAT WAS CURRENT BEFORE IS CURRENT AGAIN.
        var start = startName == null ? null : typeNamed(startName);
        if (start != null && (fm.CurrentType == null || fm.CurrentType.Name != startName))
        {
            try { fm.CurrentType = start; }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit would not make \"" + startName + "\" current again: "
                    + ex.Message + " NOTHING from this call was kept.");
            }
        }
    }

    doc.Regenerate();

    // READ BACK, every type asked for, from the type itself.
    var mismatchedMany = new List<string>();
    foreach (var name in manyTypes)
    {
        var t = typeNamed(name);
        foreach (var plan in planned)
        {
            var p = plan.Item1;
            var now = shown(t, p, plan.Item2);
            if (toWrite.Contains(name))
                written.Add("\"" + name + "\": " + p.Definition.Name + " = " + now
                    + " (was " + beforeMany[name + "|" + p.Definition.Name] + ")");
            if (!holdsPlan(t, plan)) mismatchedMany.Add("\"" + name + "\": " + p.Definition.Name + " reads " + now);
        }
    }
    typesWritten.AddRange(toWrite);
    typeUsed = fm.CurrentType == null ? "" : fm.CurrentType.Name;

    var labelsMany = new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>()
        .Where(d => { try { return d.FamilyLabel != null; } catch (Exception) { return false; } })
        .ToList();
    foreach (var plan in planned)
    {
        drives += labelsMany.Count(d => d.FamilyLabel.Id == plan.Item1.Id);
        try { drives += plan.Item1.AssociatedParameters.Size; } catch (Exception) { }
    }

    // ALL OR NOTHING, READ BACK - a value that did not hold in any type, or a
    // dimension that stopped agreeing with its parameter, THROWS.
    if (mismatchedMany.Count > 0)
        throw new InvalidOperationException("Not every type held what was asked for: "
            + string.Join("; ", mismatchedMany) + ". NOTHING from this call was kept.");
    if (broken.Count > 0)
        throw new InvalidOperationException("Revit could not move the family to these values: "
            + string.Join("; ", broken) + ". Its constraints cannot take them, so NOTHING from this call "
            + "was kept.");

    if (toWrite.Count == 0)
        findings.Add("Every type asked for already held these values, so nothing was written and no type "
            + "was made current: " + string.Join(", ", manyTypes.Select(n => "\"" + n + "\"")) + ".");
    else
        findings.Add("Wrote into " + toWrite.Count + " of the " + manyTypes.Count + " type(s) asked for, in ONE "
            + "change - one entry in Revit's undo list: " + string.Join(", ", toWrite.Select(n => "\"" + n + "\""))
            + ". Read back: " + string.Join("; ", written) + ".");
    if (toWrite.Count > 0 && alreadyHeld.Count > 0)
        findings.Add(alreadyHeld.Count + " type(s) already held every value and were not touched or made "
            + "current: " + string.Join(", ", alreadyHeld.Select(n => "\"" + n + "\"")) + ".");
    if (route == "no type made current")
        findings.Add("No type was made current: Revit's API writes a family type only while it is current, so "
            + "this text was set as a formula on every type at once and the formula cleared again - every type "
            + "keeps the text, no formula is left, and nothing a type switch deletes was deleted.");
    else if (route == "each type made current in turn")
        findings.Add((formulaTried ? "The way with no type made current was tried and did not hold, so it was "
                + "taken back. " : "")
            + "Each type was made current in turn to write it, as the Family Types dialog does - Revit's API "
            + "writes a family type only while it is current. On Revit 2020 a switch can delete an automatic "
            + "sketch dimension, and anything deleted is counted once, in this call's one question. The family's "
            + "current type is \"" + typeUsed + "\", as it was before the call.");
    if (toWrite.Count > 0)
        findings.Add(drives == 0
            ? "Nothing in the family is labelled with or associated to these parameters, so no geometry moved."
            : "These parameters drive " + drives + " labelled dimension(s) or associated element parameter(s)"
              + (movesGeometry ? ", and every labelled dimension reads its parameter in every type written."
                               : "; text and materials move no geometry."));
}

if (refused == null && !deleteType && !askedMany)
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

    // THE LABELLED DIMENSIONS THAT AGREE WITH THEIR PARAMETERS NOW. Revit posts
    // a constraint it cannot satisfy as a WARNING, which the host dismisses and
    // keeps - so the parameter would read what was asked while the planes stay
    // where they were. The only witness is the dimension, so every one that
    // agrees before the write must still agree after it, or nothing is kept.
    Func<List<Dimension>> labelledNow = () => new FilteredElementCollector(doc).OfClass(typeof(Dimension))
        .Cast<Dimension>()
        .Where(d => { try { return d.FamilyLabel != null; } catch (Exception) { return false; } })
        .ToList();
    Func<Dimension, bool> agrees = d =>
    {
        var holds = fm.CurrentType == null ? null : fm.CurrentType.AsDouble(d.FamilyLabel);
        var reads = d.Value;
        return holds.HasValue && reads.HasValue && Math.Abs(holds.Value - reads.Value) < 0.01 / 304.8;
    };
    // Those that disagree ALREADY are the family's own business and are left out;
    // a family with no type yet has nothing labelled to agree.
    var agreedIds = new HashSet<string>(labelledNow().Where(d => agrees(d)).Select(d => d.UniqueId));

    try
    {
        if (target == null)
        {
            target = fm.NewType(wantedType);
            typeCreated = true;
        }

        if (fm.CurrentType == null || fm.CurrentType.Name != target.Name)
            fm.CurrentType = target;

        // THE RENAME GOES FIRST, so the values below are written into the type
        // by its new name, and both are read back from the same current type.
        if (renameTo != null)
            fm.RenameCurrentType(renameTo);

        foreach (var plan in planned)
        {
            if (plan.Item3 is double) fm.Set(plan.Item1, (double)plan.Item3);
            else if (plan.Item3 is int) fm.Set(plan.Item1, (int)plan.Item3);
            else if (plan.Item3 is ElementId) fm.Set(plan.Item1, (ElementId)plan.Item3);
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
        else if (plan.Item3 is ElementId)
        {
            ElementId value = null;
            try { value = current.AsElementId(p); } catch (Exception) { }
            held = (value ?? ElementId.InvalidElementId).Equals((ElementId)plan.Item3);
        }
        else
        {
            held = string.Equals(current.AsString(p) ?? "", (string)plan.Item3, StringComparison.Ordinal);
        }
        if (!held) mismatched.Add(name + " reads " + now);

        drives += labels.Count(d => d.FamilyLabel.Id == p.Id);
        try { drives += p.AssociatedParameters.Size; } catch (Exception) { }
    }

    if (renameTo != null)
    {
        // READ BACK: the current type carries the new name, and no type the old
        // one unless the rename only changed its case.
        var oldStays = !string.Equals(wantedType, renameTo, StringComparison.OrdinalIgnoreCase)
            && fm.Types.Cast<FamilyType>().Any(t => t != null
                && string.Equals(t.Name, wantedType, StringComparison.OrdinalIgnoreCase));
        if (typeUsed != renameTo || oldStays)
            throw new InvalidOperationException("The type did not take the name \"" + renameTo + "\" - "
                + "the family's types read " + typeList() + ". NOTHING from this call was kept.");
        findings.Add("Renamed the type \"" + wantedType + "\" to \"" + typeUsed + "\", now the family's "
            + "current type. The family's types are " + typeList() + ".");
    }

    if (planned.Count > 0)
        findings.Add((typeCreated ? "Made the type \"" : "Wrote into the type \"") + typeUsed + "\", now the "
            + "family's current type. Read back: " + string.Join("; ", written) + ".");

    // ALL OR NOTHING, READ BACK: a value that did not hold, or a dimension that
    // stopped agreeing with its parameter, THROWS - the host rolls the whole
    // call back rather than keeping a family whose numbers and planes disagree.
    if (mismatched.Count > 0)
        throw new InvalidOperationException("Not every value held what was asked for: "
            + string.Join("; ", mismatched) + ". NOTHING from this call was kept.");

    var broken = labelledNow()
        .Where(d => agreedIds.Contains(d.UniqueId))
        .Where(d => !agrees(d))
        .Select(d => "the dimension labelled " + d.FamilyLabel.Definition.Name + " reads "
            + (d.Value.HasValue ? mm(d.Value.Value) + " mm" : "nothing") + " where the parameter holds "
            + (current.AsDouble(d.FamilyLabel).HasValue ? mm(current.AsDouble(d.FamilyLabel).Value) + " mm" : "nothing"))
        .ToList();
    if (broken.Count > 0)
        throw new InvalidOperationException("Revit could not move the family to these values: "
            + string.Join("; ", broken) + ". Its constraints cannot take them, so NOTHING from this call "
            + "was kept.");

    if (planned.Count > 0)
        findings.Add(drives == 0
        ? "Nothing in the family is labelled with or associated to these parameters yet, so no "
          + "geometry moved."
        : "These parameters drive " + drives + " labelled dimension(s) or associated element "
          + "parameter(s), and every labelled dimension reads its parameter after the write.");
}

// THE FAMILY'S TYPES AS THEY STAND AFTER THE CALL, read back whatever it did.
if (doc.IsFamilyDocument) typesNow = typeList();
if (refused != null) findings.Add(refused);
