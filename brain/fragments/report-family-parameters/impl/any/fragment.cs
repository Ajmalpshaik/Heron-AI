// NOT STANDALONE. Assumes `doc` and `nameContains` are in scope; leaves
// `parameterCount`, `parameterList`, `materialParameters`, `currentTypeName`,
// `scanned`, `notAFamily` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// IT ANSWERS "WHERE DOES THIS FAMILY GET ITS MATERIAL". A fitting family
// usually carries a MATERIAL PARAMETER and ties its geometry to that, so the
// project can drive it per type. Painting a material onto the geometry
// instead hard-codes it forever. Which of the two a family does is invisible
// from outside, so it is read before anything is changed.
//
// A FORMULA MEANS THE VALUE IS NOT YOURS TO SET. A parameter driven by a
// formula refuses a written value, and that refusal reads like a broken write
// rather than a fact about the parameter - so the formula is reported beside
// the value.
//
// DisplayUnitType IS DELIBERATELY NOT READ. It is the property that became
// ForgeTypeId at 2021, and touching it is what makes one implementation for
// every release impossible. Values cross as Revit's own strings instead.
//
// EVERY PARAMETER COMES BACK AS ONE STRING. A list is abbreviated to three
// entries before a reader sees it, and forty parameters then read as three.

var findings = new List<string>();
var parameterCount = 0;
var scanned = 0;
var parameterList = "";
var materialParameters = "";
var currentTypeName = "";
var notAFamily = false;

var needle = (nameContains ?? "").Trim();
var rows = new List<string>();
var materialRows = new List<string>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    findings.Add("The document in front is a project, not a family being edited. Parameters "
        + "belong to the family - open the family for editing and run this again.");
}
else
{
    var manager = doc.FamilyManager;
    if (manager == null)
    {
        findings.Add("This family document has no family manager, which should not happen - "
            + "nothing could be read.");
    }
    else
    {
        FamilyType current = null;
        try { current = manager.CurrentType; }
        catch (Exception) { current = null; }

        // VALUES WITHOUT A TYPE BESIDE THEM ARE VALUES FROM NOWHERE. A family
        // holds one set of values per type, so which type is being read is
        // part of the answer.
        try { currentTypeName = current == null ? "" : current.Name; }
        catch (Exception) { currentTypeName = ""; }

        if (currentTypeName.Length == 0) currentTypeName = "<no current type>";

        var parameters = new List<FamilyParameter>();
        try
        {
            foreach (FamilyParameter parameter in manager.GetParameters())
                if (parameter != null) parameters.Add(parameter);
        }
        catch (Exception) { }

        scanned = parameters.Count;

        foreach (var parameter in parameters)
        {
            var name = "";
            try
            {
                if (parameter.Definition != null) name = parameter.Definition.Name;
            }
            catch (Exception) { }
            if (name == null || name.Length == 0) name = "<unnamed>";

            if (needle.Length > 0
                && name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            parameterCount++;

            var storage = "";
            try { storage = parameter.StorageType.ToString(); }
            catch (Exception) { storage = "?"; }

            var scope = "type";
            try { if (parameter.IsInstance) scope = "instance"; }
            catch (Exception) { }

            var formula = "";
            try { formula = parameter.Formula ?? ""; }
            catch (Exception) { }

            // REVIT'S OWN STRING, never a number re-derived here. See the
            // header - this is also what keeps DisplayUnitType out of it.
            var value = "";
            if (current != null)
            {
                try
                {
                    if (parameter.StorageType == StorageType.ElementId)
                    {
                        var id = current.AsElementId(parameter);
                        var referenced = id == null ? null : doc.GetElement(id);
                        value = referenced == null ? "<none>" : referenced.Name;
                    }
                    else if (parameter.StorageType == StorageType.String)
                    {
                        value = current.AsString(parameter) ?? "";
                    }
                    else
                    {
                        value = current.AsValueString(parameter) ?? "";
                    }
                }
                catch (Exception) { value = "?"; }
            }

            rows.Add(string.Format("{0} [{1}/{2}] = {3}{4}", name, storage, scope,
                value.Length == 0 ? "<blank>" : value,
                formula.Length == 0 ? "" : string.Format(" (formula: {0})", formula)));

            // THE MATERIAL PARAMETERS, CALLED OUT. They are the usual reason
            // for asking, and finding them in a list of forty is the work.
            var isMaterial = false;
            try
            {
                isMaterial = parameter.StorageType == StorageType.ElementId
                    && parameter.Definition != null
                    && name.IndexOf("material", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch (Exception) { }

            if (isMaterial)
            {
                materialRows.Add(string.Format("{0} [{1}] = {2}", name, scope,
                    value.Length == 0 ? "<blank>" : value));
            }
        }

        parameterList = string.Join("  ||  ", rows.ToArray());
        materialParameters = string.Join("  ||  ", materialRows.ToArray());

        if (materialRows.Count == 0)
        {
            findings.Add("NO material parameter was found. This family does not take its "
                + "material from a parameter - so either its geometry carries a material "
                + "directly, or it carries none at all and draws in the category's own.");
        }
        else
        {
            findings.Add(string.Format("{0} material parameter(s): {1}", materialRows.Count,
                materialParameters));
        }
    }
}

findings.Insert(0, string.Format("{0}; {1} parameter(s) reported of {2}, values from type '{3}'",
    notAFamily ? "NOT a family document" : string.Format("family '{0}'", doc.Title),
    parameterCount, scanned, currentTypeName));
