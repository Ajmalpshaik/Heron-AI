// NOT STANDALONE. Assumes `doc`, `namePattern`, `requiredParameters` and
// `mustHaveConnectors` are in scope; leaves `findings`, `offStandard` and
// `notChecked` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// IT AUDITS TYPES, NOT INSTANCES. One badly built type placed two hundred times
// is ONE thing to fix; an instance-based report shows it as two hundred
// findings and buries everything else. The instance count beside each row is
// what makes it urgent.
//
// CONNECTORS ARE READ THROUGH A PLACED INSTANCE. A FamilySymbol does not expose
// them - there is no symbol.Connectors - so one instance per type is sampled. A
// type with NO instance placed is reported NOT CHECKED rather than passed, and
// that distinction is the difference between an audit and a guess.
//
// SYSTEM FAMILIES HAVE NO FAMILY FILE - duct types, wall types, pipe types.
// Counted and excluded, never silently skipped.

var findings = new List<string>();
var offStandard = 0;
var notChecked = 0;
var systemFamilies = 0;

// One placed instance per type, gathered once. Doing it per type is the same
// query run hundreds of times.
var sampleByType = new Dictionary<ElementId, FamilyInstance>();
var countByType = new Dictionary<ElementId, int>();

foreach (var element in new FilteredElementCollector(doc)
    .OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType())
{
    var instance = element as FamilyInstance;
    if (instance == null) continue;
    var typeId = instance.GetTypeId();
    if (typeId == ElementId.InvalidElementId) continue;

    if (!sampleByType.ContainsKey(typeId)) sampleByType[typeId] = instance;
    countByType[typeId] = (countByType.ContainsKey(typeId) ? countByType[typeId] : 0) + 1;
}

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)))
{
    var symbol = element as FamilySymbol;
    if (symbol == null) continue;

    var family = symbol.Family;
    if (family == null) { systemFamilies++; continue; }

    // An in-place family belongs to one model and cannot be a library standard.
    var inPlace = false;
    try { inPlace = family.IsInPlace; }
    catch { }

    var placed = countByType.ContainsKey(symbol.Id) ? countByType[symbol.Id] : 0;
    var faults = new List<string>();

    // ---- name
    if (!string.IsNullOrEmpty(namePattern)
        && family.Name.IndexOf(namePattern, StringComparison.OrdinalIgnoreCase) < 0)
    {
        faults.Add(string.Format("name does not contain '{0}'", namePattern));
    }

    // ---- required parameters, on the TYPE
    foreach (var wanted in requiredParameters)
    {
        if (string.IsNullOrEmpty(wanted)) continue;
        var parameter = symbol.LookupParameter(wanted);
        if (parameter == null) faults.Add(string.Format("no '{0}' parameter", wanted));
    }

    // ---- connectors, through a placed instance only
    if (mustHaveConnectors)
    {
        if (placed == 0)
        {
            notChecked++;
            findings.Add(string.Format("{0} : {1}  - NOT CHECKED for connectors: no instance is placed, "
                + "and a type does not expose its connectors. Place one and re-run",
                family.Name, symbol.Name));
        }
        else
        {
            var sample = sampleByType[symbol.Id];
            var connectorCount = 0;
            try
            {
                var manager = sample.MEPModel == null ? null : sample.MEPModel.ConnectorManager;
                if (manager != null) connectorCount = manager.Connectors.Size;
            }
            catch { }

            if (connectorCount == 0)
            {
                faults.Add("NO CONNECTORS - it cannot join a system, cannot carry flow and will never "
                    + "appear in a system browser, while looking normal in every plan");
            }
        }
    }

    if (faults.Count > 0)
    {
        offStandard++;
        findings.Add(string.Format("{0} : {1}  ({2} placed{3})  - {4}",
            family.Name, symbol.Name, placed, inPlace ? ", IN-PLACE" : "",
            string.Join("; ", faults)));
    }
}

findings.Insert(0, string.Format("{0} family type(s) off standard, {1} not checked for connectors. "
    + "{2} system family type(s) excluded - a duct type or a wall type has no family file and cannot "
    + "be audited this way", offStandard, notChecked, systemFamilies));
