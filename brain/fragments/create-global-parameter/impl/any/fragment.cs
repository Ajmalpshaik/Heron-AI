// NOT STANDALONE. Assumes `doc`, `parameterName` and `specTypeId` are in scope;
// leaves `created`, `parameterId` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// IT MAKES THE PARAMETER AND DOES NOT SET ITS VALUE. Writing a value is
// SET_GLOBAL_PARAMETER, which re-drives every dimension labelled by the global.
// Making the thing and filling it in are different operations with different
// risks, and folding them together means a typo in a value can also create a
// parameter.
//
// RE-RUNNING DOES NOT MAKE A SECOND ONE. Revit refuses duplicate global names
// outright, so the alternative to checking first is a throw in the middle of
// somebody else's transaction. An existing one is handed back and named.
//
// A FAMILY DOCUMENT CANNOT HOLD GLOBALS AT ALL, and that is a different answer
// from "none found" - one means this kind of document cannot, the other means
// there are none yet.
//
// THE ANSWER IS READ BACK BY NAME AFTER THE WRITE, never taken from the call
// having returned. That is the rule this project exists around.
//
// 2022 AND LATER. The third argument changed type at 2022 - `ParameterType`
// before it, `ForgeTypeId` after - and `ParameterType` is not present at all in
// 2024. The spec arrives already built, so no units API is touched here (D-20).

var findings = new List<string>();
var created = false;
var parameterId = ElementId.InvalidElementId;

var wantedName = parameterName == null ? "" : parameterName.Trim();

if (!GlobalParametersManager.AreGlobalParametersAllowed(doc))
{
    findings.Add("This document cannot hold global parameters at all - they exist in projects, not "
        + "in families. That is different from having none, and reporting it as a failure to "
        + "create would send somebody looking for a setting that could never be there.");
}
else if (wantedName.Length == 0)
{
    findings.Add("No name was given, so nothing was created. A global parameter is found by name "
        + "everywhere else in this library, so an unnamed one could not be written to afterwards.");
}
else if (specTypeId == null)
{
    findings.Add("No spec was given, so nothing was created. A global holds a KIND of value - a "
        + "length, a number, a yes/no, text - and Revit needs that decided at the moment it is "
        + "made. It cannot be changed afterwards.");
}
else
{
    // ALREADY THERE IS NOT AN ERROR. It is the ordinary result of running the
    // same request twice, and the useful answer is the one that exists.
    var existingId = GlobalParametersManager.FindByName(doc, wantedName);

    if (existingId != null && existingId != ElementId.InvalidElementId)
    {
        parameterId = existingId;
        findings.Add("\"" + wantedName + "\" already exists in this project, so NOTHING WAS "
            + "CREATED and the one that is already there is what came back. Its spec was fixed "
            + "when it was made and this does not change it. To write a value into it, that is "
            + "SET_GLOBAL_PARAMETER.");
    }
    else if (!GlobalParametersManager.IsUniqueName(doc, wantedName))
    {
        // TWO READS DISAGREEING IS ITSELF THE FINDING. FindByName said there is
        // no global of that name and IsUniqueName says the name is taken, so
        // something else in the project holds it. Creating anyway is a throw.
        findings.Add("Nothing was created. No global parameter called \"" + wantedName + "\" was "
            + "found, and yet Revit says that name is not available - so the name is held by "
            + "something this lookup cannot see. Choose another name rather than forcing this one.");
    }
    else
    {
        var made = GlobalParameter.Create(doc, wantedName, specTypeId);

        // READ BACK BY NAME. `Create` returning is not evidence the project
        // holds it - that is what is being checked, not assumed.
        var foundId = GlobalParametersManager.FindByName(doc, wantedName);

        if (foundId == null || foundId == ElementId.InvalidElementId)
        {
            findings.Add("The create call returned, and looking the name up again found NOTHING. "
                + "Nothing was reported as created, because what the project holds afterwards is "
                + "the only thing worth reporting.");
        }
        else
        {
            created = true;
            parameterId = foundId;

            var readBack = doc.GetElement(foundId) as GlobalParameter;
            var reportedName = readBack == null ? wantedName : readBack.Name;

            findings.Add("Created the global parameter \"" + reportedName + "\", read back by name "
                + "after the write.");

            if (readBack != null && !string.Equals(reportedName, wantedName, StringComparison.Ordinal))
            {
                // REVIT RENAMED IT. Worth saying out loud - every other fragment
                // finds this parameter BY NAME, so a silent rename is a global
                // nobody can reach again.
                findings.Add("REVIT DID NOT KEEP THE NAME ASKED FOR. It was requested as \""
                    + wantedName + "\" and the project holds \"" + reportedName + "\". Everything "
                    + "else here finds a global by name, so use the name it actually has.");
            }

            findings.Add("It has no value yet beyond whatever its spec starts at, and nothing is "
                + "labelled by it, so nothing in the model has moved. Writing a value is "
                + "SET_GLOBAL_PARAMETER, and labelling a dimension with it is done in Revit.");
        }
    }
}
