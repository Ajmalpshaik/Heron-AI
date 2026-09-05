// NOT STANDALONE. Assumes `doc`, `categories` and `wantOpenEnds` are in scope;
// leaves `elements`, `withoutConnectors` and `findings` behind.
//
// ASKING A CONNECTOR WHETHER IT IS CONNECTED CAN THROW. A panelboard's six
// connectors include surface ones, and reading connection status on those
// raises rather than returning false - measured. So only END connectors are
// asked, which is also the only kind the question means: a surface connector
// has no end to leave open. Every read is guarded even so.
//
// A LOCAL CHECK, NOT A TRACE. It asks each element about its own connectors and
// walks nothing. A run joined at every joint can still be a closed loop that
// reaches no plant - FIND_SYSTEM_ISLANDS is what sees that.
//
// AN ELEMENT WITH NO END CONNECTORS IS NEITHER OPEN NOR JOINED and is counted
// separately. Putting it in either list would be an invention, and a large
// count there usually means the category is wrong rather than the model.

var elements = new List<Element>();
var withoutConnectors = 0;
var findings = new List<string>();

Func<Element, ConnectorManager> managerOf = element =>
{
    var curve = element as MEPCurve;
    if (curve != null) return curve.ConnectorManager;

    var instance = element as FamilyInstance;
    if (instance != null && instance.MEPModel != null) return instance.MEPModel.ConnectorManager;

    return null;
};

var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
if (categories != null && categories.Count > 0)
    collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

var scanned = 0;
var unreadable = 0;

foreach (var element in collector)
{
    scanned++;

    ConnectorManager manager = null;
    try { manager = managerOf(element); }
    catch { manager = null; }

    if (manager == null) { withoutConnectors++; continue; }

    var ends = 0;
    var open = 0;
    var refused = false;

    try
    {
        foreach (Connector connector in manager.Connectors)
        {
            // END connectors only. A surface connector has no end to leave
            // open, and asking it about connection status is what throws.
            if (connector.ConnectorType != ConnectorType.End) continue;
            ends++;

            try
            {
                if (!connector.IsConnected) open++;
            }
            catch
            {
                refused = true;
            }
        }
    }
    catch
    {
        refused = true;
    }

    if (refused) unreadable++;

    if (ends == 0) { withoutConnectors++; continue; }

    var hasOpenEnd = open > 0;
    if (hasOpenEnd == wantOpenEnds) elements.Add(element);
}

findings.Add(string.Format("{0} of {1} scanned element(s) {2}. {3} have no end connectors at all and "
    + "are in neither list",
    elements.Count,
    scanned,
    wantOpenEnds ? "have at least one OPEN end" : "are joined at every end",
    withoutConnectors));

if (unreadable > 0)
    findings.Add(string.Format("{0} element(s) refused to report connection status on at least one "
        + "end and may be answered wrongly. Reading that status on the wrong kind of connector raises "
        + "rather than returning false, which is why only ends are asked", unreadable));

if (withoutConnectors > 0 && withoutConnectors >= scanned / 2)
    findings.Add("More than half of what was scanned has no end connectors. That usually means the "
        + "category list is wrong rather than the model being unmodelled");

findings.Add("This asked each element about itself and walked nothing. A run joined at every joint "
    + "can still reach no plant at all - FIND_SYSTEM_ISLANDS is what sees that, and this cannot");
