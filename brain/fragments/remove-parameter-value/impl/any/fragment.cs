// NOT STANDALONE. Assumes `doc`, `elements` and `parameterName` are in scope;
// leaves `cleared`, `alreadyEmpty` and `refused` behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// WRITE_ELEMENT_PARAMETERS CANNOT DO THIS. It takes the value as TEXT on
// purpose, and an empty string clears a text parameter but NOT a length, an
// airflow or one pointing at another element. Clearing is its own call and works
// on all four.
//
// AN EMPTY PARAMETER AND A ZERO ARE NOT THE SAME THING. A blank in a schedule
// means nobody has said; a 0 means somebody said none. Writing "0" to clear a
// field is the mistake this exists to make unnecessary.
//
// A READ-ONLY PARAMETER IS REPORTED, NEVER SKIPPED. Area, Volume and Length are
// computed by Revit and cannot be cleared - passing over them quietly reports
// forty elements cleared when none was.
//
// A TYPE PARAMETER IS REFUSED FROM AN INSTANCE LIST. A type is shared, so
// clearing one from forty instances changes every other instance in the project.

var cleared = 0;
var alreadyEmpty = 0;
var refused = new List<string>();

var missing = 0;
var readOnly = 0;
var typeLevel = 0;

foreach (var element in elements)
{
    if (element == null || !element.IsValidObject) continue;

    var parameter = element.LookupParameter(parameterName);

    if (parameter == null)
    {
        // It may be on the TYPE rather than the instance - a different thing
        // entirely, and not this fragment's to clear.
        var typeId = element.GetTypeId();
        if (typeId != ElementId.InvalidElementId)
        {
            var elementType = doc.GetElement(typeId);
            if (elementType != null && elementType.LookupParameter(parameterName) != null)
            {
                typeLevel++;
                continue;
            }
        }
        missing++;
        continue;
    }

    if (parameter.IsReadOnly)
    {
        readOnly++;
        continue;
    }

    if (!parameter.HasValue)
    {
        alreadyEmpty++;
        continue;
    }

    try
    {
        parameter.ClearValue();
        cleared++;
    }
    catch (Exception ex)
    {
        // REVIT REFUSES ClearValue ON A PLAIN TEXT BUILT-IN, and the header at
        // the top of this file assumed it "works on all four". It does not.
        // Measured 2026-09-13 on `test projject`: Comments written to four
        // walls and then cleared, every one refused with "Cannot call
        // Clear...". That is the commonest field anybody wants emptied, so the
        // fragment's main path failed on its main case.
        //
        // FOR A STRING PARAMETER THE EMPTY STRING IS THE EMPTY STATE - this
        // file says exactly that five lines above - so that is the fallback,
        // and it is read back rather than assumed.
        //
        // ONLY FOR String. A length, an airflow or one pointing at another
        // element still refuses rather than being set to zero, because "an
        // empty parameter and a zero are not the same thing" is the whole
        // reason this fragment exists and a fallback must not quietly break it.
        var handled = false;

        if (parameter.StorageType == StorageType.String)
        {
            try
            {
                parameter.Set(string.Empty);
                if (!parameter.HasValue || string.IsNullOrEmpty(parameter.AsString()))
                {
                    cleared++;
                    handled = true;
                }
            }
            catch { }
        }

        if (!handled)
            refused.Add(string.Format("{0} (id {1}): {2}", element.Name, element.Id, ex.Message));
    }
}

if (missing > 0)
{
    refused.Add(string.Format("{0} element(s) have no parameter called '{1}' at all", missing, parameterName));
}
if (readOnly > 0)
{
    refused.Add(string.Format("{0} element(s) carry '{1}' as READ-ONLY and it cannot be cleared. Area, "
        + "Volume and Length are computed by Revit - reporting them as cleared would be a lie the "
        + "schedule contradicts", readOnly, parameterName));
}
if (typeLevel > 0)
{
    refused.Add(string.Format("{0} element(s) carry '{1}' on their TYPE, not on the instance. Clearing "
        + "it there would change every other instance in the project using that type, so it was NOT "
        + "done - hand in the types if that is what you want", typeLevel, parameterName));
}

refused.Add(string.Format("{0} cleared, {1} already empty. An empty field and a zero are different "
    + "things in a schedule - blank means nobody has said, 0 means somebody said none",
    cleared, alreadyEmpty));
