// NOT STANDALONE. Assumes `doc`, `elements`, `findMaterial` and
// `replaceMaterial` are in scope; leaves `layersChanged`, `parametersChanged`,
// `typesTouched`, `skippedInGroup` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ===========================================================================
// A MATERIAL HIDES IN MORE THAN ONE PLACE, AND MISSING ONE IS THE DEFECT.
// ===========================================================================
//
//   LAYERS      walls, floors, roofs. The material is on each LAYER of the
//               TYPE's compound structure. The layers that come back are
//               COPIES - editing them in place changes nothing at all, so the
//               structure has to be set back onto the type.
//
//   PARAMETERS  any parameter holding a material, on the instance or its type.
//
// A swap that covers one and not the other leaves the old material half in use,
// where it then refuses to purge and nobody can see why.
//
// "<BY CATEGORY>" IS NOT "NO MATERIAL". It is an invalid element id. Reading it
// as empty is how a swap quietly misses everything set by category, so a
// parameter is recognised by its DEFINITION where that can be read, and only
// falls back to what it currently holds when it cannot.
//
// EDITING A TYPE CHANGES EVERY ELEMENT OF THAT TYPE. Two walls of one type are
// one type. That is Revit working correctly and it still surprises people, so
// types touched are counted separately and each is done once.
//
// A GROUPED ELEMENT IS SKIPPED FOR INSTANCE PARAMETERS unless the parameter
// really does vary across groups. Otherwise the write is either refused or
// applied to EVERY instance of that group in the model, and neither outcome
// announces itself.
//
// THREE FAMILIES ARE SKIPPED BY NAME - curtain wall, sloped glazing, basic
// ceiling. They report a compound structure that is not a real layer stack.

var layersChanged = 0;
var parametersChanged = 0;
var typesTouched = new List<ElementId>();
var skippedInGroup = new List<ElementId>();
var findings = new List<string>();

// Whichever of the two exists on this release - see the compatibility note.
// Neither name may appear in source that compiles on 2020 AND on 2027.
var definitionType = typeof(Definition);
var dataTypeMethod = definitionType.GetMethod("GetDataType", Type.EmptyTypes);
var parameterTypeProperty = definitionType.GetProperty("ParameterType");

Func<Parameter, bool> holdsAMaterial = parameter =>
{
    try
    {
        if (dataTypeMethod != null)
        {
            var kind = dataTypeMethod.Invoke(parameter.Definition, null);
            if (kind != null && kind.ToString().IndexOf("material", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        else if (parameterTypeProperty != null)
        {
            var kind = parameterTypeProperty.GetValue(parameter.Definition);
            if (kind != null && kind.ToString() == "Material") return true;
        }
    }
    catch { }

    // Last resort: it is holding one right now. This cannot recognise a
    // material parameter currently set to "<By Category>", which is exactly why
    // the definition is asked first.
    try
    {
        var held = parameter.AsElementId();
        return held != ElementId.InvalidElementId && doc.GetElement(held) is Material;
    }
    catch { return false; }
};

Func<Element, bool> alreadyDone = element =>
{
    foreach (var id in typesTouched) if (id == element.Id) return true;
    return false;
};

var skipFamilies = new[] { "Curtain Wall", "Sloped Glazing", "Basic Ceiling" };
var refusals = 0;

foreach (var element in elements)
{
    if (element == null) continue;

    var typeElement = doc.GetElement(element.GetTypeId());

    // ---- the layers, which live on the TYPE ----
    var host = typeElement as HostObjAttributes;
    if (host != null && !alreadyDone(typeElement))
    {
        string family = null;
        try { family = host.FamilyName; } catch { }

        if (family != null && skipFamilies.Contains(family))
        {
            typesTouched.Add(typeElement.Id);
            findings.Add(string.Format("'{0}' is a {1} - its compound structure is not a real layer "
                + "stack and was left alone", typeElement.Name, family));
        }
        else
        {
            try
            {
                var structure = host.GetCompoundStructure();
                if (structure != null)
                {
                    // The layers are COPIES. Editing them in place does nothing
                    // - the structure has to go back onto the type.
                    var layers = structure.GetLayers();
                    var touched = 0;
                    for (var i = 0; i < layers.Count; i++)
                    {
                        if (layers[i].MaterialId != findMaterial.Id) continue;
                        layers[i].MaterialId = replaceMaterial.Id;
                        touched++;
                    }

                    if (touched > 0)
                    {
                        structure.SetLayers(layers);
                        host.SetCompoundStructure(structure);

                        // READ IT BACK off the type, not off the copy.
                        var after = host.GetCompoundStructure().GetLayers();
                        var stuck = 0;
                        foreach (var layer in after)
                            if (layer.MaterialId == replaceMaterial.Id) stuck++;

                        layersChanged += touched;
                        findings.Add(string.Format("'{0}': {1} layer(s) changed, {2} now carry '{3}'",
                            typeElement.Name, touched, stuck, replaceMaterial.Name));
                    }
                    typesTouched.Add(typeElement.Id);
                }
            }
            catch (Exception ex)
            {
                findings.Add(string.Format("'{0}': its layers could not be changed - {1}",
                    typeElement.Name, ex.Message));
            }
        }
    }

    // ---- material parameters, on the instance and on its type ----
    var inGroup = element.GroupId != null && element.GroupId != ElementId.InvalidElementId;

    foreach (Parameter parameter in element.Parameters)
    {
        if (parameter.StorageType != StorageType.ElementId || parameter.IsReadOnly) continue;
        if (!holdsAMaterial(parameter)) continue;
        if (parameter.AsElementId() != findMaterial.Id) continue;

        if (inGroup)
        {
            // VariesAcrossGroups lives on InternalDefinition, NOT on Definition -
            // checked against the reference assemblies at both ends. Reading it
            // off `parameter.Definition` does not compile on any release.
            var varies = false;
            var internalDefinition = parameter.Definition as InternalDefinition;
            if (internalDefinition != null)
            {
                try { varies = internalDefinition.VariesAcrossGroups; } catch { }
            }
            if (!varies)
            {
                // Refused, or applied to every instance of the group. Neither
                // announces itself, so it is not attempted.
                if (!skippedInGroup.Contains(element.Id)) skippedInGroup.Add(element.Id);
                continue;
            }
        }

        try
        {
            parameter.Set(replaceMaterial.Id);
            // READ IT BACK. A write that returns is not a write that took.
            if (parameter.AsElementId() == replaceMaterial.Id) parametersChanged++;
            else refusals++;
        }
        catch { refusals++; }
    }

    if (typeElement != null && !alreadyDone(typeElement) && !(typeElement is HostObjAttributes))
    {
        foreach (Parameter parameter in typeElement.Parameters)
        {
            if (parameter.StorageType != StorageType.ElementId || parameter.IsReadOnly) continue;
            if (!holdsAMaterial(parameter)) continue;
            if (parameter.AsElementId() != findMaterial.Id) continue;

            try
            {
                parameter.Set(replaceMaterial.Id);
                if (parameter.AsElementId() == replaceMaterial.Id) parametersChanged++;
                else refusals++;
            }
            catch { refusals++; }
        }
        typesTouched.Add(typeElement.Id);
    }
}

findings.Add(string.Format(
    "'{0}' replaced by '{1}': {2} layer(s) and {3} parameter(s) changed across {4} type(s)",
    findMaterial.Name, replaceMaterial.Name, layersChanged, parametersChanged, typesTouched.Count));

if (typesTouched.Count > 0)
    findings.Add(string.Format("{0} TYPE(s) were edited, which changes EVERY element of those types in "
        + "the model and not only the ones handed in", typesTouched.Count));

if (skippedInGroup.Count > 0)
    findings.Add(string.Format("{0} element(s) inside groups were left alone - the parameter does not "
        + "vary across groups, so the write would either be refused or applied to every instance of "
        + "that group", skippedInGroup.Count));

if (refusals > 0)
    findings.Add(string.Format("{0} write(s) were accepted and did nothing - usually a material locked "
        + "by a family formula", refusals));
