// NOT STANDALONE. Assumes `doc`, `elements`, `sampleOnly` and
// `includeTypeParameters` are in scope; leaves `findings`, `parameterNames`,
// `instanceCount`, `typeCount` and `elementsRead` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE PARAMETER GROUP IS DELIBERATELY NOT REPORTED. `Definition.ParameterGroup`
// is gone by Revit 2027 and its replacement `GetGroupTypeId` has never existed
// on 2020 - neither works at both ends, and this is one implementation for all
// eight releases. The group is a sorting convenience in a dialog; nothing about
// reading or writing a value needs it.
//
// THREE KINDS, AND ONLY THREE CAN HONESTLY BE TOLD APART. Revit carries no flag
// separating a family parameter from a project parameter once both are on an
// element, so the third bucket is named as the bucket it is rather than guessed
// into one of two.
//
// VALUES COME THROUGH AsValueString FIRST - Revit's own formatting in the
// project's units, so a duct size reads "450x250" and no units API is named.
// AsString is the fallback for text; the raw number is never invented.
//
// SAMPLING ONE ELEMENT IS THE DEFAULT. One element is representative of its
// type and answers fast; `sampleOnly` off unions across a mixed set, which is
// how variation between different families is found.

var findings = new List<string>();
var parameterNames = new List<string>();
var instanceCount = 0;
var typeCount = 0;
var elementsRead = 0;

var seen = new HashSet<string>();

Func<Parameter, string> kindOf = p =>
{
    var internalDef = p.Definition as InternalDefinition;
    if (internalDef != null && internalDef.BuiltInParameter != BuiltInParameter.INVALID)
        return "built-in";
    if (p.IsShared) return "shared";
    return "project/family (not shared)";
};

Func<Parameter, string> valueOf = p =>
{
    try
    {
        if (!p.HasValue) return "<blank>";
        var shown = p.AsValueString();
        if (!string.IsNullOrEmpty(shown)) return shown;
        if (p.StorageType == StorageType.String) return p.AsString() ?? "<blank>";
        if (p.StorageType == StorageType.Integer) return p.AsInteger().ToString();
        if (p.StorageType == StorageType.ElementId)
        {
            var target = doc.GetElement(p.AsElementId());
            return target == null ? "<none>" : target.Name;
        }
        return "<no readable value>";
    }
    catch (Exception) { return "<unreadable>"; }
};

Action<Element, string> readOne = null;
readOne = (element, whose) =>
{
    if (element == null) return;
    foreach (Parameter parameter in element.Parameters)
    {
        if (parameter == null || parameter.Definition == null) continue;

        var name = parameter.Definition.Name;
        var key = whose + "|" + name;
        if (!seen.Add(key)) continue;

        parameterNames.Add(name);
        if (whose == "instance") instanceCount++; else typeCount++;

        findings.Add(string.Format("  {0,-40} {1,-9} {2,-28} {3}{4}",
            name, parameter.StorageType, kindOf(parameter), valueOf(parameter),
            parameter.IsReadOnly ? "   [read-only]" : ""));
    }
};

foreach (var element in elements)
{
    if (element == null) continue;

    elementsRead++;
    if (elementsRead == 1 || !sampleOnly)
    {
        if (elementsRead == 1)
        {
            findings.Add(string.Format("INSTANCE parameters, from '{0}' (id {1}):",
                element.Name, element.Id));
        }
        readOne(element, "instance");
    }

    if (sampleOnly && elementsRead >= 1) break;
}

if (includeTypeParameters && elements.Count > 0)
{
    var first = elements[0];
    var type = first == null ? null : doc.GetElement(first.GetTypeId()) as ElementType;
    if (type == null)
    {
        findings.Add("TYPE parameters: this element has no type - a line, a group or a view does "
            + "not. That is not a fault");
    }
    else
    {
        findings.Add(string.Format("TYPE parameters, from '{0}: {1}':", type.FamilyName, type.Name));
        readOne(type, "type");

        if (!sampleOnly)
        {
            var seenTypes = new HashSet<ElementId>();
            seenTypes.Add(type.Id);
            foreach (var element in elements)
            {
                if (element == null) continue;
                var other = doc.GetElement(element.GetTypeId()) as ElementType;
                if (other == null || !seenTypes.Add(other.Id)) continue;
                readOne(other, "type");
            }
        }
    }
}

if (parameterNames.Count == 0)
{
    findings.Add("No parameters could be read at all. That is not a normal state for a model "
        + "element - check what was handed in");
}

findings.Insert(0, string.Format("{0} parameter(s): {1} on the instance, {2} on the type. Read from "
    + "{3} of {4} element(s){5}. The parameter GROUP is not reported - that member is gone by Revit "
    + "2027 and its replacement never existed on 2020",
    parameterNames.Count, instanceCount, typeCount,
    sampleOnly ? 1 : elementsRead, elements.Count,
    sampleOnly ? " (sampled - pass sampleOnly off to union across a mixed set)" : ""));
