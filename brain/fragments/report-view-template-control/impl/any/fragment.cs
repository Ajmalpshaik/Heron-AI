// NOT STANDALONE. Assumes `doc` and `views` are in scope; leaves `findings`,
// `controlledParameters`, `withTemplate` and `withoutTemplate` behind.
//
// READS ONLY. No transaction, nothing changed.
//
// THE INCLUDE LIST BELONGS TO THE TEMPLATE, NOT TO THE VIEW FOLLOWING IT.
// Revit's own documentation for GetNonControlledTemplateParameterIds says the
// parameters are those "not marked as included when THIS VIEW is used as a
// template" - identical wording on 2020 and on 2027. So both lists are read off
// the TEMPLATE, and two views sharing one template get the same answer, because
// that is the truth. The earlier library read them off the view instead and
// described per-view exclusions that Revit does not have.
//
// PARAMETERS ARE NAMED WITHOUT EVER READING AN ElementId AS A NUMBER. The usual
// route - negative id, cast to BuiltInParameter, LabelUtils - needs the id as
// an integer, and that member went 64-bit at 2024 and lost its old name by
// 2026. Walking the template's own parameter set and comparing ElementId to
// ElementId gives the same display name Revit shows in Properties, on every
// release, with no reflection.
//
// A TEMPLATE PASSED IN DIRECTLY IS ANSWERED, NOT REFUSED. "What does this
// template control" is the same question one step earlier.

var findings = new List<string>();
var controlledParameters = new List<string>();
var withTemplate = 0;
var withoutTemplate = 0;

// The template's parameter ids named off its own parameter set. Nothing here
// converts an id to a number.
Func<View, ICollection<ElementId>, List<string>> nameThem = (owner, ids) =>
{
    var names = new List<string>();
    if (ids == null) return names;

    foreach (var id in ids)
    {
        var name = "";
        try
        {
            foreach (Parameter parameter in owner.Parameters)
            {
                if (parameter == null || parameter.Definition == null) continue;
                if (parameter.Id == id) { name = parameter.Definition.Name; break; }
            }
        }
        catch (Exception) { }

        if (string.IsNullOrEmpty(name))
        {
            // Not on the template's own parameter set. A project parameter
            // element can still name itself.
            try
            {
                var asElement = doc.GetElement(id) as ParameterElement;
                if (asElement != null) name = asElement.Name;
            }
            catch (Exception) { }
        }

        if (!string.IsNullOrEmpty(name)) names.Add(name);
    }
    names.Sort();
    return names;
};

// Both halves of one template, read once. Called for a template passed in
// directly and for the template a view is following.
Action<View, string> describeTemplate = (template, prefix) =>
{
    ICollection<ElementId> mayControl = null;
    ICollection<ElementId> notIncluded = null;
    try { mayControl = template.GetTemplateParameterIds(); }
    catch (Exception ex)
    {
        findings.Add(string.Format("{0}the template '{1}' will not say what it can control: {2}",
            prefix, template.Name, ex.Message));
        return;
    }
    try { notIncluded = template.GetNonControlledTemplateParameterIds(); }
    catch (Exception) { notIncluded = new List<ElementId>(); }

    var excluded = new HashSet<ElementId>(notIncluded ?? new List<ElementId>());
    var locked = new List<ElementId>();
    foreach (var id in mayControl)
    {
        if (!excluded.Contains(id)) locked.Add(id);
    }

    var lockedNames = nameThem(template, locked);
    var freeNames = nameThem(template, notIncluded);

    foreach (var name in lockedNames)
    {
        if (!controlledParameters.Contains(name)) controlledParameters.Add(name);
    }

    findings.Add(string.Format("{0}'{1}' HOLDS {2} setting(s): {3}", prefix, template.Name,
        lockedNames.Count,
        lockedNames.Count == 0 ? "none - it is applied and controls nothing"
                               : string.Join(", ", lockedNames.ToArray())));
    findings.Add(string.Format("{0}'{1}' LEAVES FREE {2} setting(s): {3}", prefix, template.Name,
        freeNames.Count,
        freeNames.Count == 0 ? "none - everything it can control, it controls"
                             : string.Join(", ", freeNames.ToArray())));
};

foreach (var view in views)
{
    if (view == null || !view.IsValidObject) continue;

    if (view.IsTemplate)
    {
        findings.Add(string.Format("'{0}' IS a view template, not a view following one.", view.Name));
        describeTemplate(view, "  ");
        continue;
    }

    if (view.ViewTemplateId == ElementId.InvalidElementId)
    {
        findings.Add(string.Format("'{0}': no view template. Every setting on it is its own, and "
            + "nothing is holding it to a standard", view.Name));
        withoutTemplate++;
        continue;
    }

    var applied = doc.GetElement(view.ViewTemplateId) as View;
    if (applied == null)
    {
        findings.Add(string.Format("'{0}': follows a template that cannot be read back from the "
            + "document. Its settings are still locked and nothing here can say by what",
            view.Name));
        withTemplate++;
        continue;
    }

    findings.Add(string.Format("'{0}': follows the template '{1}'. Anything in the HOLDS list below "
        + "is read-only on this view", view.Name, applied.Name));
    describeTemplate(applied, "  ");
    withTemplate++;
}

controlledParameters.Sort();
