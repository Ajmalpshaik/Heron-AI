// NOT STANDALONE. Assumes `doc`, `parameterName` and `formula` are in scope;
// leaves `formulaNow`, `valueNow`, `cleared`, `notAFamily`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A FORMULA NEEDS A CURRENT TYPE. Revit refuses one in a family with no type
// ("There is no valid family type") - reported from an earlier family build -
// so that is checked first and named, rather than asked of Revit.
//
// A PARAMETER NAME IN QUOTES IS REFUSED HERE, with the reason. Revit's own
// refusal of `Height + "Neck Depth"` says only that the formula is invalid, and
// the fix - names are bare, spaces and all - was found by trying each name
// alone. Only the family's OWN parameter names are checked, so a text formula
// with quoted strings in it is left alone.
//
// THE ONE WRITE IS THE FORMULA, and a refusal from Revit THROWS so the host
// rolls the call back and quotes Revit. What the family holds afterwards - the
// formula and the value it produced - is read back, never assumed.

var findings = new List<string>();
var formulaNow = "";
var valueNow = "";
var cleared = false;
var notAFamily = false;
string refused = null;

var wantedName = parameterName == null ? "" : parameterName.Trim();
var wantedFormula = formula == null ? "" : formula.Trim();

FamilyParameter target = null;
var nothingToDo = false;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor, so it has "
        + "no family formulas. Open the family first.";
}
else if (wantedName.Length == 0)
{
    refused = "No parameter was named, so no formula was set.";
}
else
{
    var fm = doc.FamilyManager;
    target = fm.get_Parameter(wantedName);

    if (target == null)
    {
        refused = "\"" + wantedName + "\" is not a parameter of this family, so no formula was set. "
            + "ADD_FAMILY_PARAMETERS makes one.";
    }
    else if (fm.CurrentType == null)
    {
        refused = "This family has no type yet, and a formula needs a current type to hold its "
            + "result - Revit refuses it with \"There is no valid family type\". SET_FAMILY_TYPE_VALUES "
            + "makes the first type; then set the formula.";
    }
    else if (!target.CanAssignFormula)
    {
        refused = "Revit does not allow a formula on \"" + wantedName + "\" - a reporting parameter, "
            + "or one of the category's own built-in fields that Revit keeps to itself.";
    }
    else if (wantedFormula.Length == 0 && string.IsNullOrEmpty(target.Formula))
    {
        nothingToDo = true;
        findings.Add("\"" + wantedName + "\" has no formula, so there was nothing to clear and nothing "
            + "changed.");
    }
    else
    {
        // THE FAMILY'S OWN NAMES, IN QUOTES. The one mistake Revit's refusal
        // does not explain.
        var quoted = new List<string>();
        foreach (FamilyParameter other in fm.Parameters)
        {
            var name = other.Definition.Name;
            if (wantedFormula.Contains("\"" + name + "\"") || wantedFormula.Contains("'" + name + "'"))
                quoted.Add(name);
        }

        if (quoted.Count > 0)
        {
            refused = "The formula puts " + string.Join(", ", quoted.Select(n => "\"" + n + "\""))
                + " in quotes. Parameter names are written BARE in a Revit formula, spaces and all - "
                + "Height + Neck Depth, not Height + \"Neck Depth\" - and quoted, Revit rejects the "
                + "whole formula as invalid without saying why. Nothing changed.";
        }
    }
}

if (refused == null && !nothingToDo && target != null)
{
    var fm = doc.FamilyManager;
    var had = target.Formula ?? "";

    try
    {
        fm.SetFormula(target, wantedFormula.Length == 0 ? null : wantedFormula);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the formula \"" + wantedFormula + "\" on \""
            + wantedName + "\": " + ex.Message + " Names are written bare, spaces and all; a unit may "
            + "follow a number (60 mm); and a formula cannot use the parameter it sets, directly or "
            + "through another. Nothing was kept.");
    }

    doc.Regenerate();

    // READ BACK - the formula the family holds and what it produced.
    var readBack = fm.get_Parameter(wantedName);
    formulaNow = readBack == null || readBack.Formula == null ? "" : readBack.Formula;
    valueNow = readBack == null || fm.CurrentType == null ? "" : (fm.CurrentType.AsValueString(readBack) ?? "");
    cleared = wantedFormula.Length == 0 && formulaNow.Length == 0;

    if (wantedFormula.Length == 0)
    {
        findings.Add(cleared
            ? "Cleared the formula \"" + had + "\" on \"" + wantedName + "\". It keeps the value the "
              + "formula last gave it - " + valueNow + " in type \"" + fm.CurrentType.Name + "\" - and "
              + "can be typed into again."
            : "The formula on \"" + wantedName + "\" was cleared and reads \"" + formulaNow
              + "\" afterwards. Revit did not keep the change.");
    }
    else
    {
        findings.Add("\"" + wantedName + "\" = " + formulaNow + ", read back from the family. In the "
            + "current type \"" + fm.CurrentType.Name + "\" it gives " + valueNow + "."
            + (had.Length > 0 ? " It replaced the formula \"" + had + "\"." : ""));

        if (!string.Equals(formulaNow.Replace(" ", ""), wantedFormula.Replace(" ", ""),
                           StringComparison.OrdinalIgnoreCase))
            findings.Add("Revit holds the formula as \"" + formulaNow + "\", not exactly as it was "
                + "typed. That is Revit tidying it; the value above is what it gives.");
    }
}

if (refused != null) findings.Add(refused);
