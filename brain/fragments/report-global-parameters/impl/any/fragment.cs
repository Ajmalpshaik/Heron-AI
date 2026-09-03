// NOT STANDALONE. Assumes `doc` is in scope; leaves `findings`, `globalCount`
// and `allowed` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// A GLOBAL PARAMETER IS NOT A PROJECT PARAMETER. A project parameter is a
// COLUMN - every door has its own value. A global is ONE value for the whole
// model that can be attached to dimensions, so changing it MOVES GEOMETRY.
//
// THE INTERESTING PART IS GetLabeledDimensions, NOT THE VALUE. The value is one
// number; the dimensions labelled by it are the list of things that move when
// somebody edits it, and that is the answer to "why did that wall shift".
//
// NOT EVERY DOCUMENT ALLOWS THEM - a family document does not. Asked first,
// because an empty list otherwise reads as "this model has none" when the truth
// is "this kind of document cannot have any".

var findings = new List<string>();
var globalCount = 0;

var allowed = GlobalParametersManager.AreGlobalParametersAllowed(doc);

if (!allowed)
{
    findings.Add("this document cannot hold global parameters at all - a family document and some "
        + "template states do not allow them. That is NOT the same as having none");
}
else
{
    var ids = GlobalParametersManager.GetAllGlobalParameters(doc);
    globalCount = ids.Count;

    if (globalCount == 0)
    {
        findings.Add("this project has no global parameters. Nothing here is driven by one");
    }

    foreach (var id in ids)
    {
        var parameter = doc.GetElement(id) as GlobalParameter;
        if (parameter == null) continue;

        var value = "(unreadable)";
        try
        {
            var held = parameter.GetValue();
            var asDouble = held as DoubleParameterValue;
            var asString = held as StringParameterValue;
            var asInt = held as IntegerParameterValue;
            var asId = held as ElementIdParameterValue;

            if (asDouble != null)
            {
                // Internal feet. Reported in mm as well, because that is the
                // number the person typed into the dialog.
                value = string.Format("{0:0.####} ft  ({1:0.#} mm)", asDouble.Value, asDouble.Value * 304.8);
            }
            else if (asString != null) value = "\"" + asString.Value + "\"";
            else if (asInt != null) value = asInt.Value.ToString();
            else if (asId != null)
            {
                var pointed = doc.GetElement(asId.Value);
                value = pointed == null ? "(nothing)" : pointed.Name;
            }
        }
        catch { }

        var how = "typed in";
        try
        {
            if (parameter.IsReporting) how = "REPORTING - it reads a dimension from the model and "
                + "cannot be set at all";
            else if (parameter.IsDrivenByFormula)
            {
                var formula = "";
                try { formula = parameter.GetFormula(); }
                catch { }
                how = string.IsNullOrEmpty(formula) ? "driven by a formula" : "= " + formula;
            }
        }
        catch { }

        var drives = 0;
        try
        {
            var labelled = parameter.GetLabeledDimensions();
            if (labelled != null) drives = labelled.Count;
        }
        catch { }

        findings.Add(string.Format("'{0}' = {1}  - {2}  - drives {3} dimension(s){4}",
            parameter.Name, value, how, drives,
            drives == 0
                ? ". Attached to no dimension, so editing it moves nothing"
                : ". Those move when this number changes"));
    }

    findings.Insert(0, string.Format("{0} global parameter(s) in this project. A global is ONE value "
        + "for the whole model, not a column on each element", globalCount));
}
