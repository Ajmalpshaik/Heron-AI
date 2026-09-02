// NOT STANDALONE. Assumes `doc`, `elements` and `newTypeName` are in scope;
// leaves `findings`, `createdTypeId`, `sourceTypeName` and `noType` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// MORE THAN ONE SOURCE TYPE IS A REFUSAL, NOT A LOOP. Revit requires type names
// to be unique, so one literal name applied to several distinct types succeeds
// on the first and fails on the rest - a batch that reports success and half
// happened. The types are counted first and named in the refusal, which is the
// information the user needed in order to narrow the selection.
//
// FORTY INSTANCES OF ONE TYPE GIVE ONE NEW TYPE. The types are de-duplicated by
// id before anything is created.
//
// AN ELEMENT WITH NO RESOLVABLE TYPE IS REPORTED, NOT SKIPPED. It is usually a
// sign the selection contains something that is not what the user thinks it is.
//
// NOTHING IS SET ON THE NEW TYPE AND NO INSTANCE IS MOVED ONTO IT. A duplicate
// is an exact copy; making it different is WRITE_ELEMENT_PARAMETERS, and moving
// instances is CHANGE_ELEMENT_TYPE. Doing either here would be deciding on
// somebody's behalf that their existing elements should change.

var findings = new List<string>();
var createdTypeId = ElementId.InvalidElementId;
var sourceTypeName = "";
var noType = new List<ElementId>();

var sources = new List<ElementType>();

foreach (var element in elements)
{
    if (element == null) continue;

    var typeId = element.GetTypeId();
    if (typeId == ElementId.InvalidElementId) { noType.Add(element.Id); continue; }

    var type = doc.GetElement(typeId) as ElementType;
    if (type == null) { noType.Add(element.Id); continue; }

    // Compared as ElementId to ElementId. Never as a number - that is what
    // broke at Revit 2024.
    if (!sources.Any(t => t.Id == type.Id)) sources.Add(type);
}

if (sources.Count == 0)
{
    findings.Add("Nothing in the selection resolves to a type, so there is nothing to duplicate");
}
else if (sources.Count > 1)
{
    findings.Add(string.Format(
        "The selection sits on {0} different types, and one name cannot serve them all - Revit needs type "
        + "names to be unique. NOTHING was created. The types are: {1}",
        sources.Count, string.Join(", ", sources.Select(t => "'" + t.Name + "'"))));
}
else
{
    var source = sources[0];
    sourceTypeName = source.Name ?? "";

    try
    {
        var made = source.Duplicate(newTypeName);
        if (made == null)
        {
            findings.Add(string.Format("Duplicating '{0}' returned nothing and no type was made", sourceTypeName));
        }
        else
        {
            createdTypeId = made.Id;

            // READ IT BACK. The name Revit ended up with is the answer, not the
            // name that was asked for.
            var back = doc.GetElement(createdTypeId) as ElementType;
            var actual = back == null ? null : back.Name;

            findings.Add(string.Format("'{0}' duplicated as '{1}'{2}. It is an EXACT copy - set what makes "
                + "it different before using it, and nothing has been moved onto it",
                sourceTypeName, actual ?? newTypeName,
                (actual != null && actual != newTypeName)
                    ? string.Format(" (asked for '{0}', Revit named it '{1}')", newTypeName, actual)
                    : ""));
        }
    }
    catch (Exception ex)
    {
        // A name already in use is the usual cause, and it is a question for
        // the user rather than something to work around by inventing a name.
        findings.Add(string.Format("Could not duplicate '{0}' as '{1}': {2}",
            sourceTypeName, newTypeName, ex.Message));
    }
}

if (noType.Count > 0)
    findings.Add(string.Format("{0} element(s) in the selection have no type that could be resolved",
        noType.Count));
