// NOT STANDALONE. Assumes `doc`, `elements`, `newTypeName` and `typeName` are
// in scope; leaves `findings`, `createdTypeId`, `sourceTypeName` and `noType`
// behind.
//
// VERSION 2 (2026-11-16, FRAGMENT-ISSUES row 5b-1705) - A TYPE IS DUPLICATED
// FROM THE TYPE ITSELF. Version 1 asked each element handed in for its type,
// and a TYPE answers that with no type - so a door type picked by SELECT_TYPES
// landed in `noType`, and each new door size cost a seed instance placed only
// to be duplicated from (Ajmalpshaik/Heron-AI#853, Revit 2024, item B7). Now:
//   - an element handed in that IS a type is that type;
//   - `typeName` names one with nothing selected - "Family: Type" as the
//     Project Browser shows it ("M_Single-Flush: 0915 x 2134mm"), or a type
//     name alone when exactly one type in the project has it. Typed wins over
//     the selection, and the answer says the selection was not used. A name
//     that matches no type, or a bare name more than one family has, is
//     refused with what it found - never the first match (D-54).
// Everything after the source type is found is version 1's, unchanged.
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

// VERSION 2: the type typed by name, when one is. Everything from here to the
// last finding sits in one block, so its working values - the name typed, the
// selection echoed back - are not handed back beside the answer (the lesson of
// ROTATE_ELEMENTS' row 5b-1708).
{
var typedName = (typeName ?? "").Trim();
var byName = typedName.Length > 0 && !string.Equals(typedName, "none", StringComparison.OrdinalIgnoreCase);
var handedIn = elements ?? new List<Element>();
string refusal = null;

if (byName)
{
    Func<ElementType, string> familyOf = t =>
    {
        try { return t.FamilyName ?? ""; } catch (Exception) { return ""; }
    };
    var allTypes = new FilteredElementCollector(doc).WhereElementIsElementType()
        .OfType<ElementType>().ToList();

    var colon = typedName.IndexOf(':');
    List<ElementType> hits;
    if (colon > 0)
    {
        var family = typedName.Substring(0, colon).Trim();
        var type = typedName.Substring(colon + 1).Trim();
        hits = allTypes.Where(t => string.Equals(familyOf(t), family, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.Name ?? "", type, StringComparison.OrdinalIgnoreCase)).ToList();
        // A type whose own name holds a colon is still found by that whole name.
        if (hits.Count == 0)
            hits = allTypes.Where(t => string.Equals(t.Name ?? "", typedName, StringComparison.OrdinalIgnoreCase))
                .ToList();
    }
    else
    {
        hits = allTypes.Where(t => string.Equals(t.Name ?? "", typedName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    if (hits.Count == 1)
    {
        sources.Add(hits[0]);
        if (handedIn.Count > 0)
            findings.Add(string.Format("The type was named, so the {0} element(s) selected were not used",
                handedIn.Count));
    }
    else if (hits.Count == 0)
    {
        refusal = string.Format("No type in this project is called '{0}'. Name it as the Project Browser "
            + "shows it, \"Family: Type\" - \"M_Single-Flush: 0915 x 2134mm\". NOTHING was created", typedName);
    }
    else
    {
        refusal = string.Format("'{0}' is the name of {1} types, so it does not say which one. Name it as "
            + "\"Family: Type\" - one of: {2}. NOTHING was created", typedName, hits.Count,
            string.Join(", ", hits.Take(10).Select(t => "'" + familyOf(t) + ": " + t.Name + "'")));
    }
}

foreach (var element in (byName ? new List<Element>() : handedIn))
{
    if (element == null) continue;

    // VERSION 2: a type handed in is the type to duplicate - SELECT_TYPES
    // hands types, and a type has no type of its own to ask for.
    var itself = element as ElementType;
    if (itself != null)
    {
        if (!sources.Any(t => t.Id == itself.Id)) sources.Add(itself);
        continue;
    }

    var typeId = element.GetTypeId();
    if (typeId == ElementId.InvalidElementId) { noType.Add(element.Id); continue; }

    var type = doc.GetElement(typeId) as ElementType;
    if (type == null) { noType.Add(element.Id); continue; }

    // Compared as ElementId to ElementId. Never as a number - that is what
    // broke at Revit 2024.
    if (!sources.Any(t => t.Id == type.Id)) sources.Add(type);
}

if (refusal != null)
{
    findings.Add(refusal);
}
else if (sources.Count == 0)
{
    findings.Add(handedIn.Count == 0
        ? "Nothing was selected and no type was named, so there is nothing to duplicate. Select an element "
          + "or a type, or name the type as \"Family: Type\""
        : "Nothing in the selection resolves to a type, so there is nothing to duplicate");
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

}

if (noType.Count > 0)
    findings.Add(string.Format("{0} element(s) in the selection have no type that could be resolved",
        noType.Count));
