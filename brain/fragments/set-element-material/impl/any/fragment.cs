// NOT STANDALONE. Assumes `doc`, `elements`, `material` and
// `materialParameter` are in scope; leaves `writtenIds`,
// `materialAssignReport`, `alreadyAsAsked`, `skippedElements`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// "MAKE THESE COLUMNS CONCRETE" - THE MATERIAL IS WRITTEN WHERE IT LIVES, found
// from the element and never asked of the modeller unless there is a choice:
//   1. a material parameter on the element itself,
//   2. else one on its type - which changes EVERY element of that type, said
//      in the answer with the count where Revit gives one,
//   3. else, for a duct, pipe, fitting or accessory in a system, the system
//      type's - which changes how every element of that system type draws.
// A parameter is a material parameter when it holds a material now, or its
// definition says it holds one: `ParameterType.Material` on 2020 to 2022,
// `SpecTypeId.Reference.Material` from 2022 - read by reflection, as
// WRITE_ELEMENT_PARAMETERS reads it, because the first is gone at 2023 and the
// second begins at 2022. Read-only ones are passed over.
//
// TWO OR MORE TO CHOOSE FROM IS ASKED, NEVER GUESSED: a door's frame and panel,
// a column's own Material beside its Structural Material. The element is
// skipped naming them, and `materialParameter` names the one meant.
//
// A WALL, FLOOR, ROOF OR CEILING TYPE BUILT OF LAYERS is skipped and named: its
// material lives in a layer, which SET_COMPOUND_LAYER_WIDTH sets - unless
// `materialParameter` names a parameter it also carries. A pipe whose material
// comes from its pipe segment, and nothing else holds one, is skipped saying so.
//
// AN ELEMENT IN A GROUP is skipped for a parameter of its own - written, it
// either fails or lands on every copy of the group (REPLACE_MATERIAL's rule).
// A type's parameter is not the group's, and is written.
//
// A TYPE IS WRITTEN ONCE, however many of its elements are handed in. Types
// handed in themselves - SELECT_TYPES - are written directly.
//
// IN A PROJECT. In the Family Editor a form's material is
// SET_FAMILY_FORM_MATERIAL's and a family type's Material parameter
// SET_FAMILY_TYPE_VALUES', and a family document is refused naming them.
//
// THE MATERIAL IS BY ITS NAME and must already be there: the exact name, or the
// same name in another case when only one has it. "<By Category>" or "none"
// clears it. Nothing is ever made here.
//
// READ BACK, ALL OR NOTHING: after one regeneration every parameter written
// must hold the material asked; one that does not fails the call.

var writtenIds = new List<ElementId>();
var materialAssignReport = "";
var alreadyAsAsked = 0;
var skippedElements = new List<string>();
var refused = "";
var findings = new List<string>();

{
    Func<Element, string> label = e =>
    {
        if (e == null) return "?";
        var category = e.Category == null ? "" : e.Category.Name + " ";
        var type = e as ElementType;
        if (type != null) return category + "type '" + (string.IsNullOrEmpty(type.FamilyName) ? "" : type.FamilyName + ": ") + type.Name + "'";
        return category + e.Id;
    };
    var asked = (material ?? "").Trim();
    var plain = asked.ToLowerInvariant();
    ElementId wantedId = null;
    var all = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>().ToList();

    if (doc.IsFamilyDocument)
        refused = "This is a family. In the Family Editor a form's material is SET_FAMILY_FORM_MATERIAL and a family type's Material "
            + "parameter is SET_FAMILY_TYPE_VALUES - nothing was changed";
    else if (elements == null || elements.Count == 0)
        refused = "No elements were handed in - select them, or pick them with SELECT_BY_CATEGORY_NAME or SELECT_TYPES first. Nothing was changed";
    else if (asked.Length == 0)
        refused = "No material was named. Nothing was changed";
    else if (plain == "<by category>" || plain == "by category" || plain == "none")
        wantedId = ElementId.InvalidElementId;
    else
    {
        var exact = all.Where(each => each.Name == asked).ToList();
        var anyCase = all.Where(each => string.Equals(each.Name, asked, StringComparison.OrdinalIgnoreCase)).ToList();
        if (exact.Count == 1) wantedId = exact[0].Id;
        else if (exact.Count == 0 && anyCase.Count == 1) wantedId = anyCase[0].Id;
        else if (anyCase.Count > 1)
            refused = string.Format("'{0}' matches {1} materials when case is ignored and none exactly - type it as Revit spells it. Nothing was changed",
                asked, anyCase.Count);
        else
        {
            var words = new HashSet<string>(System.Text.RegularExpressions.Regex.Split(asked.ToLowerInvariant(), "[^a-z0-9]+").Where(w => w.Length > 1));
            var near = all.Select(each => new
                {
                    each.Name,
                    score = System.Text.RegularExpressions.Regex.Split(each.Name.ToLowerInvariant(), "[^a-z0-9]+").Count(w => words.Contains(w))
                })
                .Where(each => each.score > 0).OrderByDescending(each => each.score).ThenBy(each => each.Name)
                .Take(8).Select(each => "'" + each.Name + "'").ToList();
            refused = string.Format("There is no material '{0}' in this project, and none is made here - closest: {1}. LIST_MATERIALS names "
                + "every one; CREATE_MATERIAL or CREATE_STARTER_MATERIALS makes one. Nothing was changed",
                asked, near.Count == 0 ? "nothing close" : string.Join(", ", near));
        }
    }

    if (refused.Length > 0) findings.Add(refused);
    else
    {
        var materialSpecProperty = (typeof(Document).Assembly.GetType(typeof(Document).Namespace + ".SpecTypeId+Reference")
            ?? typeof(object)).GetProperty("Material", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var materialSpec = materialSpecProperty == null ? null : materialSpecProperty.GetValue(null, null);
        Func<Parameter, bool> holdsMaterial = p =>
        {
            if (p == null || p.StorageType != StorageType.ElementId || p.Definition == null) return false;
            try
            {
                var heldId = p.AsElementId();
                if (heldId != null && heldId != ElementId.InvalidElementId) return doc.GetElement(heldId) is Material;
                var definition = p.Definition;
                var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
                if (getDataType != null && materialSpec != null)
                {
                    var spec = getDataType.Invoke(definition, null);
                    if (spec == null) return false;
                    var nameEquals = spec.GetType().GetMethod("NameEquals", new[] { materialSpec.GetType() });
                    return nameEquals != null ? (bool)nameEquals.Invoke(spec, new[] { materialSpec }) : spec.Equals(materialSpec);
                }
                var parameterType = definition.GetType().GetProperty("ParameterType");
                var kind = parameterType == null ? null : parameterType.GetValue(definition, null);
                return kind != null && kind.ToString() == "Material";
            }
            catch (Exception) { return false; }
        };
        Func<Element, List<Parameter>> materialParameters = owner =>
        {
            var found = new List<Parameter>();
            if (owner == null) return found;
            foreach (Parameter p in owner.Parameters)
                if (!p.IsReadOnly && holdsMaterial(p)) found.Add(p);
            return found;
        };
        Func<Element, Element> systemTypeOf = e =>
        {
            try
            {
                MEPSystem system = null;
                var curve = e as MEPCurve;
                if (curve != null) system = curve.MEPSystem;
                var instance = e as FamilyInstance;
                if (system == null && instance != null && instance.MEPModel != null && instance.MEPModel.ConnectorManager != null)
                    foreach (Connector connector in instance.MEPModel.ConnectorManager.Connectors)
                        if (connector.MEPSystem != null) { system = connector.MEPSystem; break; }
                if (system == null) return null;
                var typeId = system.GetTypeId();
                return typeId == null || typeId == ElementId.InvalidElementId ? null : doc.GetElement(typeId);
            }
            catch (Exception) { return null; }
        };
        Func<ElementId, string> materialName = id =>
        {
            if (id == null || id == ElementId.InvalidElementId) return "<By Category>";
            var held = doc.GetElement(id);
            return held == null ? "<By Category>" : held.Name;
        };
        var namedParameter = (materialParameter ?? "").Trim();
        if (namedParameter.ToLowerInvariant() == "any") namedParameter = "";

        // owner key -> the owner, its parameter, where it was found, and the elements handed in that reach it.
        var plans = new Dictionary<string, Tuple<Element, Parameter, string, List<ElementId>>>(StringComparer.Ordinal);
        var order = new List<string>();

        foreach (var element in elements)
        {
            if (element == null) continue;
            var asType = element as ElementType;
            var typeElement = asType != null ? null : doc.GetElement(element.GetTypeId());
            var layered = (asType as HostObjAttributes) ?? (typeElement as HostObjAttributes);
            var isLayered = false;
            if (layered != null)
            {
                var structure = layered.GetCompoundStructure();
                isLayered = structure != null && structure.LayerCount > 0;
            }

            var places = new List<Tuple<Element, string>>();
            if (asType != null) places.Add(Tuple.Create((Element)asType, "type"));
            else
            {
                places.Add(Tuple.Create(element, "element"));
                if (typeElement != null) places.Add(Tuple.Create(typeElement, "type"));
            }

            var candidates = new List<Tuple<Element, Parameter, string>>();
            foreach (var place in places)
                foreach (var p in materialParameters(place.Item1))
                    candidates.Add(Tuple.Create(place.Item1, p, place.Item2));
            if (asType == null && (candidates.Count == 0 || namedParameter.Length > 0))
            {
                var systemType = systemTypeOf(element);
                if (systemType != null && (candidates.Count == 0
                        || !candidates.Any(c => string.Equals(c.Item2.Definition.Name, namedParameter, StringComparison.OrdinalIgnoreCase))))
                    foreach (var p in materialParameters(systemType))
                        candidates.Add(Tuple.Create(systemType, p, "system type"));
            }
            if (namedParameter.Length > 0)
            {
                var named = candidates.Where(c => string.Equals(c.Item2.Definition.Name, namedParameter, StringComparison.OrdinalIgnoreCase)).ToList();
                if (named.Count == 0)
                {
                    skippedElements.Add(string.Format("{0}: no material parameter called '{1}' - it has {2}", label(element), namedParameter,
                        candidates.Count == 0 ? "none" : string.Join(", ", candidates.Select(c => "'" + c.Item2.Definition.Name + "' (" + c.Item3 + ")").Distinct())));
                    continue;
                }
                candidates = named;
            }
            else if (isLayered)
            {
                skippedElements.Add(string.Format("{0}: a {1} built of layers - its material is a layer's, set with SET_COMPOUND_LAYER_WIDTH",
                    label(element), label(layered)));
                continue;
            }
            if (candidates.Count == 0)
            {
                skippedElements.Add(string.Format("{0}: holds no material parameter it can be given{1}", label(element),
                    element is MEPCurve ? " - a pipe's material is its pipe segment's, and a duct's comes from its system type when it has one" : ""));
                continue;
            }
            if (candidates.Count > 1)
            {
                skippedElements.Add(string.Format("{0}: holds {1} material parameters - {2}; name the one meant in materialParameter",
                    label(element), candidates.Count, string.Join(", ", candidates.Select(c => "'" + c.Item2.Definition.Name + "' (" + c.Item3 + ")"))));
                continue;
            }
            var chosen = candidates[0];
            if (chosen.Item3 == "element" && element.GroupId != null && element.GroupId != ElementId.InvalidElementId)
            {
                skippedElements.Add(string.Format("{0}: inside a group - its own '{1}' is not written", label(element), chosen.Item2.Definition.Name));
                continue;
            }
            var key = chosen.Item1.Id + "|" + chosen.Item2.Id;
            Tuple<Element, Parameter, string, List<ElementId>> plan;
            if (!plans.TryGetValue(key, out plan))
            {
                plan = Tuple.Create(chosen.Item1, chosen.Item2, chosen.Item3, new List<ElementId>());
                plans[key] = plan;
                order.Add(key);
            }
            plan.Item4.Add(element.Id);
        }

        var lines = new List<string>();
        var before = new Dictionary<string, string>(StringComparer.Ordinal);
        var toCheck = new List<string>();
        foreach (var key in order)
        {
            var plan = plans[key];
            var held = plan.Item2.AsElementId();
            before[key] = materialName(held);
            if (held == wantedId || (held == null && wantedId == ElementId.InvalidElementId))
            {
                alreadyAsAsked++;
                lines.Add(string.Format("{0} '{1}' already {2}", label(plan.Item1), plan.Item2.Definition.Name, before[key]));
                continue;
            }
            if (!plan.Item2.Set(wantedId))
                throw new InvalidOperationException(string.Format("Revit would not give {0} '{1}' the material {2}. Nothing from this call was kept",
                    label(plan.Item1), plan.Item2.Definition.Name, materialName(wantedId)));
            toCheck.Add(key);
        }

        doc.Regenerate();
        var wrong = new List<string>();
        foreach (var key in toCheck)
        {
            var plan = plans[key];
            var now = plan.Item2.AsElementId();
            if (now != wantedId && !(now == null && wantedId == ElementId.InvalidElementId))
            {
                wrong.Add(string.Format("{0} '{1}' reads {2}", label(plan.Item1), plan.Item2.Definition.Name, materialName(now)));
                continue;
            }
            writtenIds.Add(plan.Item1.Id);
            string reach;
            if (plan.Item3 == "element") reach = "";
            else if (plan.Item3 == "system type") reach = " - every element of this system type draws with it now";
            else if (plan.Item1 is FamilySymbol)
            {
                var placed = new FilteredElementCollector(doc).WherePasses(new FamilyInstanceFilter(doc, plan.Item1.Id)).GetElementCount();
                reach = string.Format(" - a type change: all {0} placed element(s) of this type changed, {1} of them handed in", placed, plan.Item4.Count);
            }
            else reach = string.Format(" - a type change: every element of this type changed, {0} of them handed in", plan.Item4.Count);
            lines.Add(string.Format("{0} '{1}' {2} -> {3}{4}", label(plan.Item1), plan.Item2.Definition.Name, before[key],
                materialName(now), reach));
        }
        if (wrong.Count > 0)
            throw new InvalidOperationException("Not every material read back as asked: " + string.Join("; ", wrong) + ". Nothing from this call was kept");

        materialAssignReport = string.Join("  ||  ", lines);
        findings.Add(string.Format("{0} parameter(s) given {1}; {2} already held it; {3} element(s) skipped",
            writtenIds.Count, materialName(wantedId), alreadyAsAsked, skippedElements.Count));
        if (skippedElements.Count > 0) findings.Add("Skipped: " + string.Join("; ", skippedElements.Take(6))
            + (skippedElements.Count > 6 ? " and " + (skippedElements.Count - 6) + " more" : ""));
        if (plans.Values.Any(each => each.Item3 != "element"))
            findings.Add("A type or system type was written, so elements that were not selected changed too - one Undo takes it all back");
    }
}
