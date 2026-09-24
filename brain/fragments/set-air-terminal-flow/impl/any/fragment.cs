// NOT STANDALONE. Assumes `doc`, `elements` and `csvPath` are in scope; leaves
// `changed`, `alreadyThatFlow`, `notATerminal`, `noFlowParameter`, `refused`,
// `builtInDisagrees`, `rowsUnmatched`, `badRows` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One batch, one undo.
//
// THE PARAMETER IS FOUND THROUGH THE CONNECTOR, NEVER BY ITS NAME. An air
// terminal carries TWO parameters called "Flow": the family's own, which the
// duct connector takes its flow from, and a built-in one Revit works out from
// the connector. On 2026-09-24, in Project1, 110 L/s was written by that name
// to one terminal, and its read-back, by the same name, said 235.0 L/s - the
// family's default. On 2026-09-25 that terminal's connector parameter already
// held 110. So the value HAD landed and the read-back did not show it. A write
// by name cannot say what it did. D-54 s3 says a name that matches twice is
// refused, never chosen from, and since row 5b-203 the ten writers that look a
// name up refuse "Flow" here. So this asks the connector instead: its connector
// info names the ONE family parameter its flow is tied to, and that parameter,
// matched by id, is the one written. A terminal whose connector flow is tied to
// nothing - its Flow Configuration is Calculated or System - is named in
// `noFlowParameter` and left alone.
//
// LITRES PER SECOND IN, CUBIC FEET PER SECOND INSIDE (D-71). One constant, so
// there is no UnitTypeId (2021 on) or DisplayUnitType (to 2021) branch.
//
// EVERY ONE IS READ BACK after one regeneration. A terminal whose parameter
// does not hold the asked-for value is put back to what it held and named in
// `refused`, never counted. The built-in "Flow" is read too, and a terminal
// where it still disagrees is named in `builtInDisagrees` - reported, not put
// back, because the connector's own parameter did take the value.

const double LitresPerCubicFoot = 28.316846592;
const double Tolerance = 0.000001;

var changed = 0;
var alreadyThatFlow = new List<ElementId>();
var notATerminal = new List<ElementId>();
var noFlowParameter = new List<ElementId>();
var refused = new List<ElementId>();
var builtInDisagrees = new List<ElementId>();
var rowsUnmatched = 0;
var badRows = 0;
var findings = new List<string>();

// ---- the file: the element id first, the flow in L/s second -----------------
//
// THE FIRST LINE IS A HEADER and is never read as a row, whatever it says. Each
// later line is an id and a flow; anything after the second comma is ignored.
// A flow that is not a number, or is below zero, is a bad row - counted and
// named, and nothing is written for it.

var wanted = new Dictionary<string, double>();
var fileRead = false;

if (string.IsNullOrEmpty(csvPath) || !System.IO.File.Exists(csvPath))
{
    findings.Add("There is no file at '" + (csvPath ?? "") + "', so NOTHING was written");
}
else
{
    string[] lines = null;
    try { lines = System.IO.File.ReadAllLines(csvPath); }
    catch (Exception ex)
    {
        findings.Add("That file could not be read: " + ex.Message + ". Nothing was written");
    }

    if (lines != null)
    {
        fileRead = true;
        var header = true;
        for (var n = 0; n < lines.Length; n++)
        {
            var line = (lines[n] ?? "").Trim();
            if (line.Length == 0) continue;
            if (header) { header = false; continue; }

            var cells = line.Split(',');
            var id = cells[0].Trim();
            double flow;
            if (cells.Length < 2 || id.Length == 0
                || !double.TryParse(cells[1].Trim(), System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, out flow)
                || flow < 0 || double.IsNaN(flow) || double.IsInfinity(flow))
            {
                badRows++;
                if (badRows <= 10)
                    findings.Add("  line " + (n + 1) + ": '" + line + "' is not an id and a flow of "
                        + "zero or more L/s - nothing was written for it");
                continue;
            }
            if (wanted.ContainsKey(id))
            {
                badRows++;
                if (badRows <= 10)
                    findings.Add("  line " + (n + 1) + ": id " + id + " is in the file twice - the "
                        + "first row stands, and this one was not used");
                continue;
            }
            wanted[id] = flow;
        }
    }
}

// ---- each terminal: find the parameter its connector's flow is tied to ------

var terminalsCategory = new ElementId(BuiltInCategory.OST_DuctTerminal);
var connectorFlow = new ElementId(BuiltInParameter.RBS_DUCT_FLOW_PARAM);

var matched = new HashSet<string>();
var pendingElements = new List<Element>();
var pendingParameters = new List<Parameter>();
var pendingBefore = new List<double>();
var pendingWanted = new List<double>();

foreach (var element in elements)
{
    if (element == null || !element.IsValidObject) continue;

    var key = element.Id.ToString();
    if (!wanted.ContainsKey(key)) continue;
    matched.Add(key);

    var category = element.Category;
    if (category == null || category.Id != terminalsCategory)
    {
        notATerminal.Add(element.Id);
        continue;
    }

    var instance = element as FamilyInstance;
    if (instance == null || instance.MEPModel == null || instance.MEPModel.ConnectorManager == null)
    {
        noFlowParameter.Add(element.Id);
        continue;
    }

    // ONE DUCT CONNECTOR, OR NONE OF THEM IS WRITTEN. With two, which one the
    // flow is meant for is a guess, and a guess here is a wrong design flow
    // nobody will re-check.
    Connector duct = null;
    var ductConnectors = 0;
    foreach (Connector connector in instance.MEPModel.ConnectorManager.Connectors)
    {
        if (connector.ConnectorType != ConnectorType.End) continue;
        if (connector.Domain != Domain.DomainHvac) continue;
        ductConnectors++;
        duct = connector;
    }
    if (ductConnectors != 1)
    {
        noFlowParameter.Add(element.Id);
        if (noFlowParameter.Count <= 5)
            findings.Add("  id " + key + " has " + ductConnectors + " duct connectors, so which one "
                + "the flow is for cannot be told - nothing was written");
        continue;
    }

    var parameterId = ElementId.InvalidElementId;
    try
    {
        var info = duct.GetMEPConnectorInfo() as MEPFamilyConnectorInfo;
        if (info != null) parameterId = info.GetAssociateFamilyParameterId(connectorFlow);
    }
    catch (Exception)
    {
        parameterId = ElementId.InvalidElementId;
    }
    if (parameterId == null || parameterId == ElementId.InvalidElementId)
    {
        noFlowParameter.Add(element.Id);
        continue;
    }

    Parameter target = null;
    var sameId = 0;
    foreach (Parameter parameter in instance.Parameters)
    {
        if (parameter.Id == parameterId)
        {
            target = parameter;
            sameId++;
        }
    }
    if (sameId != 1 || target.StorageType != StorageType.Double)
    {
        noFlowParameter.Add(element.Id);
        continue;
    }
    if (target.IsReadOnly)
    {
        refused.Add(element.Id);
        continue;
    }

    var value = wanted[key] / LitresPerCubicFoot;
    var before = target.AsDouble();
    if (Math.Abs(before - value) <= Tolerance)
    {
        alreadyThatFlow.Add(element.Id);
        continue;
    }

    bool accepted;
    try { accepted = target.Set(value); }
    catch (Exception) { accepted = false; }
    if (!accepted)
    {
        refused.Add(element.Id);
        continue;
    }

    pendingElements.Add(element);
    pendingParameters.Add(target);
    pendingBefore.Add(before);
    pendingWanted.Add(value);
}

foreach (var key in wanted.Keys)
{
    if (!matched.Contains(key)) rowsUnmatched++;
}

// ---- read every one back, and put back any that did not take ----------------

if (pendingElements.Count > 0) doc.Regenerate();

var putBack = 0;
for (var i = 0; i < pendingElements.Count; i++)
{
    var landed = pendingParameters[i].AsDouble();
    if (Math.Abs(landed - pendingWanted[i]) > Tolerance)
    {
        pendingParameters[i].Set(pendingBefore[i]);
        putBack++;
        refused.Add(pendingElements[i].Id);
        continue;
    }
    changed++;

    var shown = pendingElements[i].get_Parameter(BuiltInParameter.RBS_DUCT_FLOW_PARAM);
    if (shown != null && shown.StorageType == StorageType.Double
        && Math.Abs(shown.AsDouble() - pendingWanted[i]) > Tolerance)
    {
        builtInDisagrees.Add(pendingElements[i].Id);
    }
}
if (putBack > 0) doc.Regenerate();

findings.Insert(0, changed + " air terminal flow(s) set and read back through the connector's own "
    + "parameter"
    + (alreadyThatFlow.Count > 0 ? ", " + alreadyThatFlow.Count + " already at that flow" : "")
    + (putBack > 0 ? ". " + putBack + " did not hold the value and were PUT BACK to what they held"
                   : "")
    + (builtInDisagrees.Count > 0
        ? ". On " + builtInDisagrees.Count + " the built-in Flow Revit shows still reads something "
            + "else - look at one in Properties"
        : "")
    + (rowsUnmatched > 0
        ? ". " + rowsUnmatched + " row(s) matched no element handed in - check the file is for this "
            + "model and that the filter above covered it"
        : "")
    + (fileRead && wanted.Count == 0 ? ". The file had no usable rows" : "")
    + ". Flows are litres per second");
