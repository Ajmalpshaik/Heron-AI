// NOT STANDALONE. Assumes `doc`, `parameterName` and `newValue` are in scope;
// leaves `changed`, `before`, `after` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THIS MOVES GEOMETRY. A global drives every dimension labelled by it, so one
// write can move walls on twelve levels. REPORT_GLOBAL_PARAMETERS lists what
// each one drives, and that list is what will move.
//
// A GLOBAL'S VALUE IS NOT A PARAMETER ON AN ELEMENT. It is read and written
// through its own pair of calls, which is why WRITE_ELEMENT_PARAMETERS cannot
// reach it - pointed at a global, the ordinary parameter writer finds nothing
// to write.
//
// TWO KINDS CANNOT BE SET AND BOTH LOOK ORDINARY:
//
//   driven by a formula   the value comes from the formula; a write is refused
//   reporting             it READS a dimension out of the model - an output,
//                         never an input
//
// Both are checked before anything is attempted.
//
// A FAMILY DOCUMENT CANNOT HAVE GLOBALS AT ALL, and "none found" there means
// this kind of document cannot, not there are none.

var findings = new List<string>();
var changed = false;
var before = "";
var after = "";

// How a value reads, whatever kind it is. Used for the before and after, which
// are the only evidence that anything happened.
Func<ParameterValue, string> describe = value =>
{
    if (value == null) return "(none)";

    var asDouble = value as DoubleParameterValue;
    if (asDouble != null) return asDouble.Value.ToString("R") + " (internal units)";

    var asString = value as StringParameterValue;
    if (asString != null) return "\"" + (asString.Value ?? "") + "\"";

    var asInteger = value as IntegerParameterValue;
    if (asInteger != null) return asInteger.Value.ToString();

    var asElementId = value as ElementIdParameterValue;
    if (asElementId != null) return "element id";

    return "(unreadable kind)";
};

if (!GlobalParametersManager.AreGlobalParametersAllowed(doc))
{
    findings.Add("This document cannot hold global parameters at all - they exist in projects, not "
        + "in families. That is different from having none, and reporting it as \"not found\" "
        + "would send somebody looking for a parameter that could never be there.");
}
else if (string.IsNullOrEmpty(parameterName))
{
    findings.Add("No global parameter name was given, so nothing was written.");
}
else if (newValue == null)
{
    findings.Add("No value was given, so nothing was written. Clearing a global is not the same "
        + "as writing an empty one and is not what this does.");
}
else
{
    var id = GlobalParametersManager.FindByName(doc, parameterName);

    if (id == null || id == ElementId.InvalidElementId)
    {
        findings.Add("No global parameter called \"" + parameterName + "\" in this project. "
            + "REPORT_GLOBAL_PARAMETERS lists the exact names.");
    }
    else
    {
        var global = doc.GetElement(id) as GlobalParameter;

        if (global == null)
        {
            findings.Add("\"" + parameterName + "\" resolved to something that is not a global "
                + "parameter. Nothing was written.");
        }
        else
        {
            before = describe(global.GetValue());

            var reporting = false;
            try { reporting = global.IsReporting; } catch { }

            var formula = "";
            try { formula = global.GetFormula(); } catch { }

            if (reporting)
            {
                after = before;
                findings.Add("\"" + parameterName + "\" is a REPORTING global - it reads a "
                    + "dimension out of the model and can never be written. It is an output. "
                    + "Nothing was changed.");
            }
            else if (!string.IsNullOrEmpty(formula))
            {
                after = before;
                findings.Add("\"" + parameterName + "\" is driven by the formula " + formula
                    + ", so its value comes from that and a write is refused. Change the formula, "
                    + "or the parameters the formula reads. Nothing was changed.");
            }
            else if (global.GetValue() != null
                     && global.GetValue().GetType() != newValue.GetType())
            {
                after = before;
                findings.Add("\"" + parameterName + "\" holds " + before + " and the value offered "
                    + "is a different kind. Refused rather than coerced - writing text into a "
                    + "length is not a conversion anybody meant.");
            }
            else
            {
                global.SetValue(newValue);

                // Read back. A global that refuses can return without raising,
                // and what moved is the whole point of this fragment.
                after = describe(global.GetValue());
                changed = after != before;

                findings.Add(changed
                    ? "\"" + parameterName + "\" changed from " + before + " to " + after
                      + ". EVERYTHING LABELLED BY IT HAS MOVED - REPORT_GLOBAL_PARAMETERS lists "
                      + "what that is."
                    : "\"" + parameterName + "\" reads " + after + " after the write, which is "
                      + "what it read before. Nothing moved.");
            }
        }
    }
}
