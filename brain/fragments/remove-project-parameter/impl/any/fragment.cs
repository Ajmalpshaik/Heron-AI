// NOT STANDALONE. Assumes `doc`, `parameterName` and `confirm` are in scope;
// leaves `removed`, `valuesLost`, `alsoAffected`, `refused`, `report` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16): one
// request is one entry in Revit's undo list, and Undo puts the parameter and
// every value back.
//
// MANAGE > PROJECT PARAMETERS > REMOVE. The parameter is found by its exact
// name in the project's parameter bindings - the list that dialog shows. No
// match, a match only in another case, and two parameters of one name are each
// refused by name with what there is: which one is never guessed.
//
// ARTICLE 7. Removing it discards the value every element holds in it, so it
// runs only with `confirm` repeating the name exactly - passed after the owner
// has said yes for THIS parameter. Without it nothing is removed, and the
// answer says what would go: how many elements hold a value, the categories it
// is bound to, the schedules that show it and the view filters that use it.
//
// HOW IT GOES. A parameter that is not shared is deleted from the project with
// its binding, as the dialog's Remove does, and what else Revit deleted with it
// is counted. A SHARED parameter's binding is removed and its definition left
// in the project: a loaded family may carry the same shared parameter, and
// deleting the definition would reach into it. The answer says which.
//
// READ BACK. After one regeneration the parameter must be gone from the
// bindings; if it is not, it is reported as not removed.

var removed = false;
var valuesLost = 0;
var alsoAffected = new List<string>();
var refused = "";
var findings = new List<string>();

var name = (parameterName ?? "").Trim();
var yes = (confirm ?? "").Trim();
if (string.Equals(yes, "none", StringComparison.OrdinalIgnoreCase)) yes = "";

// Every project parameter: its definition and its binding.
var bound = new List<Tuple<Definition, ElementBinding>>();
if (!doc.IsFamilyDocument)
{
    var it = doc.ParameterBindings.ForwardIterator();
    it.Reset();
    while (it.MoveNext())
    {
        var binding = it.Current as ElementBinding;
        if (it.Key != null && binding != null) bound.Add(Tuple.Create(it.Key, binding));
    }
}

Func<int, string> some = n =>
{
    var names = bound.Select(b => b.Item1.Name).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
    if (names.Count == 0) return "This project has no project parameters.";
    return "The project parameters here are: " + string.Join(", ", names.Take(n).Select(s => "\"" + s + "\""))
        + (names.Count > n ? " and " + (names.Count - n) + " more" : "") + ".";
};
Func<Tuple<Definition, ElementBinding>, string> describe = b =>
{
    var internalDef = b.Item1 as InternalDefinition;
    var element = internalDef != null ? doc.GetElement(internalDef.Id) : null;
    var shared = element as SharedParameterElement;
    var cats = new List<string>();
    foreach (Category c in b.Item2.Categories) cats.Add(c.Name);
    cats.Sort(StringComparer.OrdinalIgnoreCase);
    return string.Format("\"{0}\" ({1}, {2} parameter, on {3})", b.Item1.Name,
        shared != null ? "shared, GUID " + shared.GuidValue : "not shared",
        b.Item2 is InstanceBinding ? "an instance" : "a type",
        cats.Count == 0 ? "no category" : string.Join(", ", cats));
};

var matches = bound.Where(b => string.Equals(b.Item1.Name, name, StringComparison.Ordinal)).ToList();

if (doc.IsFamilyDocument)
    refused = "This is a family, which has family parameters and no project parameters - removing one there is "
        + "DELETE_FAMILY_PARAMETERS'. NOTHING WAS REMOVED.";
else if (name == "" || string.Equals(name, "none", StringComparison.OrdinalIgnoreCase))
    refused = "No parameter was named. " + some(40) + " NOTHING WAS REMOVED.";
else if (matches.Count == 0)
{
    var near = bound.Where(b => string.Equals(b.Item1.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
    refused = near.Count > 0
        ? "No project parameter is named exactly \"" + name + "\" - " + string.Join("; ", near.Select(describe))
            + " differs only in capitals, and a name is never guessed. NOTHING WAS REMOVED."
        : "No project parameter is named \"" + name + "\". " + some(40) + " NOTHING WAS REMOVED.";
}
else if (matches.Count > 1)
    refused = matches.Count + " project parameters are named \"" + name + "\": " + string.Join("; ", matches.Select(describe))
        + ". Which one is never guessed - remove it in Manage > Project Parameters, where they can be told apart. "
        + "NOTHING WAS REMOVED.";
else
{
    var target = matches[0];
    var definition = target.Item1;
    var internalDef = definition as InternalDefinition;
    var parameterElement = internalDef != null ? doc.GetElement(internalDef.Id) as ParameterElement : null;
    var isShared = parameterElement is SharedParameterElement;
    var parameterId = internalDef != null ? internalDef.Id : ElementId.InvalidElementId;

    // WHAT WOULD GO - counted before anything changes, and said either way.
    var categoryIds = new List<ElementId>();
    foreach (Category c in target.Item2.Categories) categoryIds.Add(c.Id);
    if (categoryIds.Count > 0)
    {
        var collector = new FilteredElementCollector(doc).WherePasses(new ElementMulticategoryFilter(categoryIds));
        collector = target.Item2 is InstanceBinding ? collector.WhereElementIsNotElementType() : collector.WhereElementIsElementType();
        foreach (var e in collector)
        {
            var p = e.get_Parameter(definition);
            if (p == null || !p.HasValue) continue;
            if (p.StorageType == StorageType.String && string.IsNullOrEmpty(p.AsString())) continue;
            valuesLost++;
        }
    }
    if (parameterId != ElementId.InvalidElementId)
    {
        foreach (var schedule in new FilteredElementCollector(doc).OfClass(typeof(ViewSchedule)).Cast<ViewSchedule>())
        {
            if (schedule.IsTemplate) continue;
            var definitionOfSchedule = schedule.Definition;
            for (int i = 0; i < definitionOfSchedule.GetFieldCount(); i++)
                if (definitionOfSchedule.GetField(i).ParameterId == parameterId)
                {
                    alsoAffected.Add("schedule \"" + schedule.Name + "\" shows it in a column");
                    break;
                }
        }
        foreach (var filter in new FilteredElementCollector(doc).OfClass(typeof(ParameterFilterElement)).Cast<ParameterFilterElement>())
            if (filter.GetElementFilterParameters().Contains(parameterId))
                alsoAffected.Add("view filter \"" + filter.Name + "\" uses it in a rule");
    }
    var consequence = describe(target) + ": " + valuesLost + " element(s) hold a value in it"
        + (alsoAffected.Count > 0 ? "; " + string.Join("; ", alsoAffected) : "; no schedule or view filter uses it");

    if (!string.Equals(yes, name, StringComparison.Ordinal))
        refused = "Removing " + consequence + " - and every one of those values would be lost. It is removed only "
            + "with confirm repeating its name exactly, \"" + name + "\", after the owner has said yes for it"
            + (yes == "" ? "" : " (confirm was \"" + yes + "\")") + ". NOTHING WAS REMOVED.";
    else
    {
        var how = "";
        try
        {
            if (!isShared && parameterElement != null)
            {
                var gone = doc.Delete(parameterElement.Id);
                var others = gone != null ? gone.Count(id => id != parameterElement.Id) : 0;
                how = "deleted from the project with its binding" + (others > 0 ? ", and Revit deleted " + others
                    + " other element(s) with it" : "");
            }
            else
            {
                if (!doc.ParameterBindings.Remove(definition))
                    refused = "Revit refused to remove the binding of " + describe(target) + ". NOTHING WAS REMOVED.";
                how = isShared
                    ? "its binding removed; the shared parameter's definition stays in the project, because a loaded "
                        + "family may carry it"
                    : "its binding removed";
            }
        }
        catch (Exception ex)
        {
            refused = "Revit would not remove " + describe(target) + " - Revit said: " + ex.Message
                + " NOTHING WAS REMOVED.";
        }

        if (refused == "")
        {
            doc.Regenerate();
            var still = false;
            var check = doc.ParameterBindings.ForwardIterator();
            check.Reset();
            while (check.MoveNext())
            {
                var key = check.Key as InternalDefinition;
                if (check.Key != null && (string.Equals(check.Key.Name, name, StringComparison.Ordinal)
                    && (key == null || parameterId == ElementId.InvalidElementId || key.Id == parameterId)))
                    still = true;
            }
            if (still)
                refused = "Revit took the removal but \"" + name + "\" is still in the project's parameters - "
                    + "it is NOT counted as removed.";
            else
            {
                removed = true;
                findings.Add("Removed " + consequence + " - " + how + ". Read back: no longer a project parameter. "
                    + valuesLost + " value(s) went with it; Undo puts them back.");
            }
        }
    }
}

if (refused != "") findings.Add(refused);
var report = findings.Count > 0 ? string.Join(" ", findings) : "";
