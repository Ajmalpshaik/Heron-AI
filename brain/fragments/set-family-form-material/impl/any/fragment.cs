// NOT STANDALONE. Assumes `doc`, `uidoc`, `forms` and `material` are in scope;
// leaves `changed`, `linked`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A FORM'S MATERIAL - the Material field in Properties for the forms named, the
// two ways that field takes one: LINKED to a family material parameter, so a
// project can set it per type ("Body Material"), or SET to one material in the
// family ("Steel"). "by category" puts it back to <By Category>.
//
// ONE FORM AT A TIME IS THE POINT. LINK_FAMILY_PARAMETER links a parameter on
// every element of a category at once - every extrusion - and a family whose
// body and neck take different materials needs them apart.
//
// WHICH ONE IS MEANT IS READ FROM THE FAMILY, NEVER GUESSED. The name is looked
// for as a family parameter and as a material. A parameter that is not a
// MATERIAL parameter is refused naming its kind - Revit's own remark: the two
// parameters must be the same type. A name that is both is refused as
// ambiguous; a name that is neither is refused listing the family's material
// parameters and materials.
//
// A FORM ALREADY LINKED TO A PARAMETER IS UNLINKED BEFORE A MATERIAL IS SET ON
// IT, and the answer says so - a linked field cannot hold a value of its own,
// and leaving the link would leave the value ignored.
//
// A VOID HAS NO MATERIAL to show, and is refused by name.
//
// READ BACK, ALL OR NOTHING. Each form's material, or its link, is read again
// after the write and reported before and after; one that does not read as
// asked fails the call and nothing is kept.

var findings = new List<string>();
var changed = new List<string>();
var linked = false;
var notAFamily = false;
string refused = null;

var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;

Func<GenericForm, string> kindOf = f => f is Extrusion ? "extrusion" : f is Revolution ? "revolve"
    : f is Blend ? "blend" : f is SweptBlend ? "swept blend" : f is Sweep ? "sweep" : "form";
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

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

// Is this family parameter a MATERIAL parameter? From 2022 its data type is
// compared with the material spec; before that its parameter type is read -
// that enum's Material member is gone from 2023.
Func<FamilyParameter, bool> isMaterial = p =>
{
    try
    {
        var definition = p.Definition;
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null)
        {
            var spec = getDataType.Invoke(definition, null);
            var owner = revitAssembly.GetType(dbNamespace + ".SpecTypeId+Reference");
            var property = owner == null ? null : owner.GetProperty("Material", flags);
            var wanted = property == null ? null : property.GetValue(null);
            return sameKind(spec, wanted);
        }
        var old = definition.GetType().GetProperty("ParameterType");
        var value = old == null ? null : old.GetValue(definition);
        return value != null && value.ToString() == "Material";
    }
    catch (Exception)
    {
        return false;
    }
};

var targets = new List<GenericForm>();
var problems = new List<string>();
FamilyParameter parameter = null;
Material chosenMaterial = null;
var byCategory = false;
var said = (material ?? "").Trim();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A form's material is set "
        + "inside the family - open it first.";
}
else
{
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
        else if (!f.IsSolid)
            problems.Add("The " + kindOf(f) + " " + f.UniqueId + " is a void, and a void has no material to show.");
        else if (f.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM) == null)
            problems.Add("The " + kindOf(f) + " " + f.UniqueId + " has no Material field.");
        else if (!targets.Any(t => t.Id == f.Id)) targets.Add(f);
    }
    if (problems.Count == 0 && targets.Count == 0)
        problems.Add("No form was named. Name them by the ids the form tools gave back, commas between, or select "
            + "them and say \"selected\".");

    var fm = doc.FamilyManager;
    var materials = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>().ToList();
    var squashed = squash(said);
    if (said.Length == 0)
        problems.Add("No material was named - a family material parameter, a material in the family, or "
            + "\"by category\".");
    else if (squashed == "bycategory" || squashed == "none")
        byCategory = true;
    else
    {
        FamilyParameter asParameter = null;
        foreach (FamilyParameter p in fm.Parameters)
            if (string.Equals(p.Definition.Name, said, StringComparison.OrdinalIgnoreCase)) { asParameter = p; break; }
        var asMaterial = materials.FirstOrDefault(m => string.Equals(m.Name, said, StringComparison.OrdinalIgnoreCase));

        if (asParameter != null && asMaterial != null)
            problems.Add("\"" + said + "\" is both a family parameter and a material in this family, so it does not "
                + "say which is meant. Rename one of them.");
        else if (asParameter != null && !isMaterial(asParameter))
            problems.Add("\"" + asParameter.Definition.Name + "\" is a family parameter, and not a MATERIAL one - "
                + "Revit links a form's Material only to a material parameter.");
        else if (asParameter != null && asParameter.IsReporting)
            problems.Add("\"" + asParameter.Definition.Name + "\" is a reporting parameter, and cannot drive a "
                + "material.");
        else if (asParameter != null) parameter = asParameter;
        else if (asMaterial != null) chosenMaterial = asMaterial;
        else
        {
            var materialParameters = new List<string>();
            foreach (FamilyParameter p in fm.Parameters)
                if (isMaterial(p)) materialParameters.Add(p.Definition.Name);
            problems.Add("\"" + said + "\" is neither a family parameter nor a material in this family. Its material "
                + "parameters are " + (materialParameters.Count == 0 ? "none - ADD_FAMILY_PARAMETERS makes one"
                    : string.Join(", ", materialParameters.OrderBy(n => n)))
                + "; its materials include " + string.Join(", ", materials.Select(m => m.Name).OrderBy(n => n).Take(15))
                + ".");
        }
    }

    if (problems.Count > 0) refused = "Nothing was changed. " + string.Join(" ", problems);
}

if (refused == null)
{
    var fm = doc.FamilyManager;
    Func<GenericForm, string> readOf = f =>
    {
        var field = f.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
        var link = fm.GetAssociatedFamilyParameter(field);
        if (link != null) return "linked to \"" + link.Definition.Name + "\"";
        var id = field.AsElementId();
        var m = id == null || id == ElementId.InvalidElementId ? null : doc.GetElement(id) as Material;
        return m == null ? "<By Category>" : m.Name;
    };

    foreach (var f in targets)
    {
        var field = f.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
        var before = readOf(f);
        var wasLinked = fm.GetAssociatedFamilyParameter(field) != null;
        try
        {
            if (parameter != null)
            {
                if (!fm.CanElementParameterBeAssociated(field))
                    throw new InvalidOperationException("Revit says this form's Material cannot be linked");
                fm.AssociateElementParameterToFamilyParameter(field, parameter);
            }
            else
            {
                if (wasLinked) fm.AssociateElementParameterToFamilyParameter(field, null);
                field.Set(byCategory ? ElementId.InvalidElementId : chosenMaterial.Id);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not give the " + kindOf(f) + " " + f.UniqueId + " the "
                + "material \"" + said + "\": " + ex.Message + " NOTHING from this call was kept.");
        }

        var after = readOf(f);
        var expected = parameter != null ? "linked to \"" + parameter.Definition.Name + "\""
            : byCategory ? "<By Category>" : chosenMaterial.Name;
        if (after != expected)
            throw new InvalidOperationException("The " + kindOf(f) + " " + f.UniqueId + " reads " + after + " after "
                + "being given " + expected + ". NOTHING from this call was kept.");

        changed.Add(kindOf(f) + " " + f.UniqueId + ": " + before + " -> " + after
            + (wasLinked && parameter == null ? " (its link was removed first)" : ""));
    }

    linked = parameter != null;
    findings.Add((linked ? "Linked " : "Set ") + changed.Count + " form(s): " + string.Join("; ", changed) + ".");
    if (linked)
        findings.Add("The material now comes from \"" + parameter.Definition.Name + "\" in each type. Its value is "
            + "picked in Family Types - SET_FAMILY_TYPE_VALUES refuses a material by name, as its card says.");
}

if (refused != null) findings.Add(refused);
