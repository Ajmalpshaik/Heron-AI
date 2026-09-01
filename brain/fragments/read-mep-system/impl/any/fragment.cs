// NOT STANDALONE. Assumes `elements` is in scope; leaves `systemName`,
// `systemType` and `noSystem` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THIS IS WHAT THE MODEL SAYS, NOT WHAT IS PHYSICALLY JOINED. The standing
// lesson here is that Revit's own data describes intent: the CRAC pairing was
// wrong in the tag names AND in the connector flags, and only tracing the
// geometry gave the real answer. A system name is data somebody entered or
// Revit assigned, and TRACE_CONNECTIVITY is the fragment that goes and looks.
//
// NAME AND TYPE ARE READ SEPARATELY because they answer different questions.
// Two ducts can share a type - Supply Air - and be on entirely separate
// systems, which is precisely what somebody is checking when they ask whether
// these are "all on the same system".
//
// READ AS STRINGS. `AsValueString` gives the displayed text for a parameter
// stored as an ElementId, so nothing here handles an id at all - which keeps
// the fragment clear of 2024's move to 64-bit ElementId and of the units API.

var systemName = new Dictionary<ElementId, string>();
var systemType = new Dictionary<ElementId, string>();
var noSystem = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;
    if (systemName.ContainsKey(element.Id) || noSystem.Contains(element.Id)) continue;

    var nameParameter = element.get_Parameter(BuiltInParameter.RBS_SYSTEM_NAME_PARAM);
    var name = nameParameter != null && nameParameter.HasValue
        ? nameParameter.AsString()
        : null;

    if (string.IsNullOrEmpty(name))
    {
        // No system name is usually no system - and that usually means not
        // connected. Named rather than recorded as an empty string, which
        // would sort alongside real names in any report built on this.
        noSystem.Add(element.Id);
        continue;
    }

    systemName[element.Id] = name;

    // The classification first - it is present on more element kinds - then
    // the duct system type as the more specific answer where it exists.
    string type = null;

    var classification = element.get_Parameter(
        BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM);
    if (classification != null && classification.HasValue)
    {
        type = classification.AsValueString();
    }

    var ductType = element.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM);
    if (ductType != null && ductType.HasValue)
    {
        var specific = ductType.AsValueString();
        if (!string.IsNullOrEmpty(specific)) type = specific;
    }

    if (!string.IsNullOrEmpty(type)) systemType[element.Id] = type;
}
