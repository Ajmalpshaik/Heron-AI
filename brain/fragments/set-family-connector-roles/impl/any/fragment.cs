// NOT STANDALONE. Assumes `doc`, `connectors`, `settings`, `primary` and `links`
// are in scope; leaves `connectorReport`, `changed`, `notAFamily`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// EVERY SETTING OF A CONNECTOR ALREADY IN THE FAMILY. ADD_FAMILY_CONNECTOR makes
// one and leaves the rest at Revit's defaults; this changes them afterwards:
// System Classification (Global and Fitting included, wherever Revit allows them
// for that connector), Flow Direction, Flow Configuration, Loss Method and its
// coefficient or pressure drop, Description, which family parameters drive its
// size, which way it points (FlipDirection) - and, as before, which connector is
// PRIMARY and which pairs are LINKED.
//
// VERSION 3 (2026-10-12): AN ELECTRICAL CONNECTOR'S VALUES - Voltage, Number of
// Poles, Apparent Load, Power Factor and Load Classification - which
// ADD_FAMILY_CONNECTOR leaves at Revit's defaults and nothing set until now.
// Widened here rather than given a tool of its own: it is the same job, a
// setting of a connector already in the family, named the same way. Each value
// is a number with its unit - "voltage=400 V", "poles=3", "apparentLoad=1.5
// kVA", "powerFactor=0.85" - or a FAMILY PARAMETER in square brackets that
// drives it, "voltage=[Voltage]", made as a type parameter of the connector
// field's own kind when the family has none of that name (what Revit's own
// Associate Family Parameter dialog does with New Parameter), or both,
// "voltage=400 V [Voltage]", which writes 400 V into the current type as well.
// Revit keeps a voltage and a load in its own internal units, feet-based:
// one volt and one volt-ampere are 1 / 0.3048² of them. That factor is checked
// against Revit's own UnitUtils on the release that runs - DisplayUnitType to
// 2021, UnitTypeId from 2021, both reached by reflection - before anything
// changes, and every value is read back as Revit shows it.
//
// VERSION 4 (2026-10-22): THE TICK BOXES AND THE OTHER TYPED VALUES a connector
// shows in the Family Editor's Properties, which nothing set until now - above
// all ALLOW SLOPE ADJUSTMENTS on a pipe connector, the box a drain's connectors
// need ticked for the pipes to be laid to a fall. Widened here, as version 3
// was, because it is the same job: a setting of a connector already in the
// family, named the same way. "allowSlopeAdjustments=yes" and "utility=yes"
// are the ticks (yes or no); "angle=90" is a fitting connector's Angle in
// degrees, "flowFactor=0.5" a duct or pipe connector's Flow Factor (or 50%),
// "presetFlow=12 L/s" its preset Flow (L/s, m3/h or cfm), each a value, a
// [family parameter] or both as the electrical values are; and
// "powerFactorState=Lagging" an electrical connector's Power Factor State.
// A KEY ANOTHER DOMAIN'S CONNECTOR HAS NOT GOT IS REFUSED naming the connector
// and its domain - Allow Slope Adjustments on a duct connector - never passed
// over in silence. Revit keeps an angle in radians and a flow in cubic feet
// per second; both factors are checked against Revit's own UnitUtils before
// anything changes, as the voltage's is.
//
// `settings` IS ONE VALUE, "system=Global; flowDirection=In; flip=true". The
// binder refuses an absent string need (HeronBindingNote.AbsentValue), so ten
// separate optional settings would have to be typed empty on every call; one
// value with the settings that are wanted, and nothing for the rest, is how
// SET_CATEGORY_OVERRIDES' `overrides` already arrives.
//
// REVIT DECIDES WHICH SYSTEM A CONNECTOR MAY TAKE - IsSystemClassificationValid
// is asked for every connector named, so a duct system on a pipe connector, or
// Global on a connector Revit keeps from it, is refused before anything changes.
//
// A LINKED PAIR PASSES A SYSTEM THROUGH THE FAMILY - a coil, a pump, a valve
// body. The sources docs/43 §10 records say it does so only when both
// connectors' system is Global; a pair that is not, and was not set to Global in
// the same call, is reported with the words that set it.
//
// A CONNECTOR IS NAMED BY THE PLANE ITS FACE LIES ON - "Port Left", the way
// ADD_FAMILY_CONNECTOR places one - by its id, or "all". A plane holding more
// than one connector is refused with their ids, so the wrong one is never
// changed.
//
// NOTHING ASKED IS A READ: every connector is listed with its domain, system,
// size and the parameters driving it, flow, the way it points, where it sits
// and the plane its face lies on, and nothing is changed. REPORT_FAMILY_CONNECTORS
// gives the same list without a write being asked for.
//
// EVERYTHING IS CHECKED BEFORE ANYTHING CHANGES, then read back. A read-back
// that differs THROWS, and the host rolls the whole call back.

var findings = new List<string>();
var connectorReport = "";
var changed = new List<string>();
var notAFamily = false;
string refused = null;

var halfMillimetre = 0.5 / 304.8;
// Revit keeps a pressure in kg/(ft*s2): one pascal, kg/(m*s2), is 0.3048 of it.
var pascal = 0.3048;
// Revit keeps a voltage in kg*ft²/(s³*A) and a power in kg*ft²/s³: one volt, or
// one volt-ampere, in square metres, is 1 / 0.3048² of the feet-based unit.
var perVolt = 1.0 / (0.3048 * 0.3048);
// Revit keeps an angle in radians and a flow in cubic feet per second: one
// degree is pi / 180 of a radian, one litre per second 0.001 / 0.3048³ of a
// cubic foot per second.
var perDegree = Math.PI / 180.0;
var perLitrePerSecond = 0.001 / (0.3048 * 0.3048 * 0.3048);
var invariant = System.Globalization.CultureInfo.InvariantCulture;
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(invariant);
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};

Func<XYZ, string> pointing = d =>
{
    if (d == null) return "no direction";
    if (Math.Abs(d.Z) > 0.9999) return d.Z > 0 ? "up" : "down";
    if (Math.Abs(d.X) > 0.9999) return d.X > 0 ? "right (+X)" : "left (-X)";
    if (Math.Abs(d.Y) > 0.9999) return d.Y > 0 ? "back (+Y)" : "front (-Y)";
    return "(" + Math.Round(d.X, 3).ToString(invariant) + ", " + Math.Round(d.Y, 3).ToString(invariant) + ", "
        + Math.Round(d.Z, 3).ToString(invariant) + ")";
};

Func<ConnectorElement, string> domainWords = c =>
    c.Domain == Domain.DomainHvac ? "duct" : c.Domain == Domain.DomainPiping ? "pipe"
    : c.Domain == Domain.DomainElectrical ? "electrical"
    : c.Domain == Domain.DomainCableTrayConduit ? "cable tray or conduit" : c.Domain.ToString();

// WHERE REVIT'S LIST SAYS SOMETHING THE ENUM DOES NOT - the names
// ADD_FAMILY_CONNECTOR also answers to (row 5b-297).
var shownAs = new Dictionary<string, string>
{
    { "FireProtectWet", "Fire Protection Wet" },
    { "FireProtectDry", "Fire Protection Dry" },
    { "FireProtectPreaction", "Fire Protection Pre-Action" },
    { "FireProtectOther", "Fire Protection Other" },
    { "SupplyHydronic", "Hydronic Supply" },
    { "ReturnHydronic", "Hydronic Return" },
};

// "SupplyAir" read as "Supply Air", the way a modeller writes it.
Func<string, string> spaced = name =>
{
    if (shownAs.ContainsKey(name)) return shownAs[name];
    var text = new System.Text.StringBuilder();
    for (var i = 0; i < name.Length; i++)
    {
        if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) text.Append(' ');
        text.Append(name[i]);
    }
    return text.ToString();
};

// THE ENUM-VALUED CONNECTOR PARAMETERS, by domain: (parameter, its enum). A
// domain or a release that has not got one leaves it null, and asking for it
// there is refused by name.
Func<ConnectorElement, Tuple<BuiltInParameter, Type>> flowDirectionOf = c =>
    c.Domain == Domain.DomainHvac ? Tuple.Create(BuiltInParameter.RBS_DUCT_FLOW_DIRECTION_PARAM, typeof(FlowDirectionType))
    : c.Domain == Domain.DomainPiping ? Tuple.Create(BuiltInParameter.RBS_PIPE_FLOW_DIRECTION_PARAM, typeof(FlowDirectionType))
    : null;
Func<ConnectorElement, Tuple<BuiltInParameter, Type>> flowConfigurationOf = c =>
    c.Domain == Domain.DomainHvac ? Tuple.Create(BuiltInParameter.RBS_DUCT_FLOW_CONFIGURATION_PARAM, typeof(DuctFlowConfigurationType))
    : c.Domain == Domain.DomainPiping ? Tuple.Create(BuiltInParameter.RBS_PIPE_FLOW_CONFIGURATION_PARAM, typeof(PipeFlowConfigurationType))
    : null;
Func<ConnectorElement, Tuple<BuiltInParameter, Type>> lossMethodOf = c =>
    c.Domain == Domain.DomainHvac ? Tuple.Create(BuiltInParameter.RBS_DUCT_FITTING_LOSS_METHOD_PARAM, typeof(DuctLossMethodType))
    : c.Domain == Domain.DomainPiping ? Tuple.Create(BuiltInParameter.RBS_PIPE_FITTING_LOSS_METHOD_PARAM, typeof(PipeLossMethodType))
    : null;

// VERSION 4: an electrical connector's Power Factor State - Lagging or Leading.
Func<ConnectorElement, Tuple<BuiltInParameter, Type>> powerFactorStateOf = c =>
    c.Domain == Domain.DomainElectrical ? Tuple.Create(BuiltInParameter.RBS_ELEC_POWER_FACTOR_STATE, typeof(PowerFactorStateType))
    : null;

Func<ConnectorElement, Tuple<BuiltInParameter, Type>, Parameter> parameterOf = (c, which) =>
    which == null ? null : c.get_Parameter(which.Item1);

// An enum parameter's value as its name - "In", "Preset" - or null.
Func<ConnectorElement, Tuple<BuiltInParameter, Type>, string> enumRead = (c, which) =>
{
    var p = parameterOf(c, which);
    if (p == null || p.StorageType != StorageType.Integer) return null;
    var value = p.AsInteger();
    return Enum.IsDefined(which.Item2, value) ? Enum.ToObject(which.Item2, value).ToString() : value.ToString(invariant);
};

// A word matched to an enum's names, spaces and case ignored: "bidirectional",
// "Specific Loss". Returns the matching name, or null.
Func<Type, string, string> enumWord = (type, word) =>
{
    foreach (var name in Enum.GetNames(type))
        if (squash(name) == squash(word) || squash(spaced(name)) == squash(word)) return name;
    return null;
};

Func<Type, string> enumNames = type => string.Join(", ", Enum.GetNames(type).Select(n => spaced(n)));

var problems = new List<string>();
var all = new List<ConnectorElement>();
// The planes each connector's face lies on, by connector.
var onPlanes = new Dictionary<ElementId, List<string>>();
var targets = new List<ConnectorElement>();
ConnectorElement wantPrimary = null;
// Each link asked for: (from, to - null to remove the link, as written).
var asked = new List<Tuple<ConnectorElement, ConnectorElement, string>>();
var reading = false;

// THE SETTINGS ASKED FOR, each null when not asked.
MEPSystemClassification? wantSystem = null;
string wantFlowDirection = null;
string wantFlowConfiguration = null;
string wantLossMethod = null;
double? wantCoefficient = null;
double? wantPressure = null;
string wantDescription = null;
var flip = false;
var wantSizes = new List<FamilyParameter>();
var sizesAsked = false;

// THE TYPED VALUES ASKED FOR - an electrical connector's from version 3; a
// fitting connector's Angle, a duct or pipe connector's Flow Factor and
// preset Flow from version 4 - each (label, the key as typed, the value in
// Revit's internal units or null, the family parameter that drives it or
// null, the value as written).
var typed = new List<Tuple<string, string, double?, string, string>>();
ElectricalLoadClassification wantLoadClass = null;
var loadClassAsked = false;
string wantPowerFactorState = null;

// WHICH DOMAIN'S CONNECTOR CARRIES A FIELD - null for the others, and a key
// asked of one of those is refused naming the connector and its domain.
Func<BuiltInParameter, Func<ConnectorElement, BuiltInParameter?>> electricalOnly = bip =>
    c => c.Domain == Domain.DomainElectrical ? bip : (BuiltInParameter?)null;
Func<BuiltInParameter, Func<ConnectorElement, BuiltInParameter?>> pipeOnly = bip =>
    c => c.Domain == Domain.DomainPiping ? bip : (BuiltInParameter?)null;
Func<BuiltInParameter, Func<ConnectorElement, BuiltInParameter?>> ductOrPipe = bip =>
    c => c.Domain == Domain.DomainHvac || c.Domain == Domain.DomainPiping ? bip : (BuiltInParameter?)null;
Func<BuiltInParameter, Func<ConnectorElement, BuiltInParameter?>> anyDomain = bip =>
    c => (BuiltInParameter?)bip;
// Angle is shown on a FITTING's connectors - duct, pipe, cable tray, conduit.
Func<ConnectorElement, BuiltInParameter?> angleOf = c =>
    c.Domain == Domain.DomainHvac || c.Domain == Domain.DomainPiping || c.Domain == Domain.DomainCableTrayConduit
    ? BuiltInParameter.CONNECTOR_ANGLE : (BuiltInParameter?)null;
// The preset Flow is one parameter on a duct connector and another on a pipe one.
Func<ConnectorElement, BuiltInParameter?> flowOf = c =>
    c.Domain == Domain.DomainHvac ? BuiltInParameter.RBS_DUCT_FLOW_PARAM
    : c.Domain == Domain.DomainPiping ? BuiltInParameter.RBS_PIPE_FLOW_PARAM : (BuiltInParameter?)null;

// WHERE A NEW FAMILY PARAMETER IS FILED: (the group's enum name to 2022, its
// GroupTypeId property from 2022, the heading Revit shows).
var underLoads = Tuple.Create("PG_ELECTRICAL_LOADS", "ElectricalLoads", "Electrical - Loads");
var underDimensions = Tuple.Create("PG_GEOMETRY", "Geometry", "Dimensions");
var underMechanical = Tuple.Create("PG_MECHANICAL", "Mechanical", "Mechanical");

// Each key: (label, whose field it is in words, the connector's built-in
// parameter by domain, where a new family parameter driving it is filed).
var typedKeys = new Dictionary<string, Tuple<string, string, Func<ConnectorElement, BuiltInParameter?>, Tuple<string, string, string>>>
{
    { "voltage",        Tuple.Create("Voltage", "an electrical connector's", electricalOnly(BuiltInParameter.RBS_ELEC_VOLTAGE), underLoads) },
    { "volts",          Tuple.Create("Voltage", "an electrical connector's", electricalOnly(BuiltInParameter.RBS_ELEC_VOLTAGE), underLoads) },
    { "poles",          Tuple.Create("Number of Poles", "an electrical connector's", electricalOnly(BuiltInParameter.RBS_ELEC_NUMBER_OF_POLES), underLoads) },
    { "numberofpoles",  Tuple.Create("Number of Poles", "an electrical connector's", electricalOnly(BuiltInParameter.RBS_ELEC_NUMBER_OF_POLES), underLoads) },
    { "phases",         Tuple.Create("Number of Poles", "an electrical connector's", electricalOnly(BuiltInParameter.RBS_ELEC_NUMBER_OF_POLES), underLoads) },
    { "apparentload",   Tuple.Create("Apparent Load", "an electrical connector's", electricalOnly(BuiltInParameter.RBS_ELEC_APPARENT_LOAD), underLoads) },
    { "apparentpower",  Tuple.Create("Apparent Load", "an electrical connector's", electricalOnly(BuiltInParameter.RBS_ELEC_APPARENT_LOAD), underLoads) },
    { "va",             Tuple.Create("Apparent Load", "an electrical connector's", electricalOnly(BuiltInParameter.RBS_ELEC_APPARENT_LOAD), underLoads) },
    { "powerfactor",    Tuple.Create("Power Factor", "an electrical connector's", electricalOnly(BuiltInParameter.RBS_ELEC_POWER_FACTOR), underLoads) },
    { "pf",             Tuple.Create("Power Factor", "an electrical connector's", electricalOnly(BuiltInParameter.RBS_ELEC_POWER_FACTOR), underLoads) },
    { "angle",          Tuple.Create("Angle", "a duct, pipe, cable tray or conduit connector's - a fitting's", angleOf, underDimensions) },
    { "connectorangle", Tuple.Create("Angle", "a duct, pipe, cable tray or conduit connector's - a fitting's", angleOf, underDimensions) },
    { "flowfactor",     Tuple.Create("Flow Factor", "a duct or pipe connector's", ductOrPipe(BuiltInParameter.RBS_FLOW_FACTOR_PARAM), underMechanical) },
    { "presetflow",     Tuple.Create("Flow", "a duct or pipe connector's", flowOf, underMechanical) },
    { "flowrate",       Tuple.Create("Flow", "a duct or pipe connector's", flowOf, underMechanical) },
};

// THE TICK BOXES ASKED FOR, each (label, whose box it is, the connector's
// parameter by domain, ticked or not). Allow Slope Adjustments is the box
// Revit shows on a PIPE connector in the Family Editor - a project reads it
// back as the placed connector's AllowsSlopeAdjustments; Utility is asked of
// any connector, and a connector that has no such box says so by name.
var ticks = new List<Tuple<string, string, Func<ConnectorElement, BuiltInParameter?>, bool>>();
var tickKeys = new Dictionary<string, Tuple<string, string, Func<ConnectorElement, BuiltInParameter?>>>
{
    { "allowslopeadjustments", Tuple.Create("Allow Slope Adjustments", "a pipe connector's", pipeOnly(BuiltInParameter.RBS_ADJUSTABLE_CONNECTOR)) },
    { "allowslopeadjustment",  Tuple.Create("Allow Slope Adjustments", "a pipe connector's", pipeOnly(BuiltInParameter.RBS_ADJUSTABLE_CONNECTOR)) },
    { "slopeadjustments",      Tuple.Create("Allow Slope Adjustments", "a pipe connector's", pipeOnly(BuiltInParameter.RBS_ADJUSTABLE_CONNECTOR)) },
    { "slopeadjustment",       Tuple.Create("Allow Slope Adjustments", "a pipe connector's", pipeOnly(BuiltInParameter.RBS_ADJUSTABLE_CONNECTOR)) },
    { "allowslope",            Tuple.Create("Allow Slope Adjustments", "a pipe connector's", pipeOnly(BuiltInParameter.RBS_ADJUSTABLE_CONNECTOR)) },
    { "utility",               Tuple.Create("Utility", "a connector's", anyDomain(BuiltInParameter.CONNECTOR_UTILITY_PARAM)) },
    { "utilityconnector",      Tuple.Create("Utility", "a connector's", anyDomain(BuiltInParameter.CONNECTOR_UTILITY_PARAM)) },
};

// What Revit's own UnitUtils makes of one unit on THIS release - UnitTypeId
// from 2021, DisplayUnitType before - or null where neither is reached.
Func<string, string, double?> revitPerUnit = (unitTypeName, displayUnitName) =>
{
    try
    {
        var unitTypeId = revitAssembly.GetType(dbNamespace + ".UnitTypeId");
        var property = unitTypeId == null ? null : unitTypeId.GetProperty(unitTypeName, flags);
        if (property != null)
        {
            var id = property.GetValue(null);
            var convert = typeof(UnitUtils).GetMethod("ConvertToInternalUnits", new[] { typeof(double), id.GetType() });
            if (convert != null) return (double)convert.Invoke(null, new object[] { 1.0, id });
        }
        var displayUnit = revitAssembly.GetType(dbNamespace + ".DisplayUnitType");
        if (displayUnit != null)
        {
            var value = Enum.Parse(displayUnit, displayUnitName);
            var convert = typeof(UnitUtils).GetMethod("ConvertToInternalUnits", new[] { typeof(double), displayUnit });
            if (convert != null) return (double)convert.Invoke(null, new object[] { 1.0, value });
        }
    }
    catch (Exception) { }
    return null;
};

// A parameter's KIND - a ForgeTypeId from 2022 (GetDataType), the
// ParameterType enum before - reached by reflection, as LINK_FAMILY_PARAMETER
// reads one, so one build serves every release.
Func<Definition, object> kindOf = definition =>
{
    try
    {
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null) return getDataType.Invoke(definition, null);
        var old = definition.GetType().GetProperty("ParameterType");
        return old == null ? null : old.GetValue(definition);
    }
    catch (Exception) { return null; }
};
Func<object, object, bool> sameKind = (first, second) =>
{
    if (first == null || second == null) return false;
    var nameEquals = first.GetType().GetMethod("NameEquals", new[] { second.GetType() });
    if (nameEquals != null)
    {
        try { return (bool)nameEquals.Invoke(first, new[] { second }); }
        catch (Exception) { }
    }
    return first.Equals(second);
};
Func<object, string> kindWords = kind =>
{
    if (kind == null) return "an unknown kind";
    var typeId = kind.GetType().GetProperty("TypeId");
    var text = typeId == null ? kind.ToString() : (typeId.GetValue(kind) as string ?? "");
    var colon = text.LastIndexOf(':');
    if (colon >= 0) text = text.Substring(colon + 1);
    var dash = text.IndexOf('-');
    return dash > 0 ? text.Substring(0, dash) : text;
};

// FamilyManager.AddParameter(name, GROUP, KIND, isInstance) whose third type is
// the connector field's kind's own type - ForgeTypeId from 2022, ParameterType
// before - under the group the key names: Electrical - Loads for a load,
// Dimensions for an angle, Mechanical for a flow.
Func<object, Tuple<string, string, string>, Tuple<System.Reflection.MethodInfo, object>> addParameterFor = (kind, under) =>
{
    if (kind == null || under == null) return null;
    var method = typeof(FamilyManager).GetMethods()
        .Where(m => m.Name == "AddParameter")
        .FirstOrDefault(m =>
        {
            var p = m.GetParameters();
            return p.Length == 4 && p[0].ParameterType == typeof(string) && p[2].ParameterType == kind.GetType()
                && p[3].ParameterType == typeof(bool);
        });
    if (method == null) return null;
    var groupType = method.GetParameters()[1].ParameterType;
    object group = null;
    try
    {
        if (groupType.IsEnum) group = Enum.Parse(groupType, under.Item1);
        else
        {
            var owner = revitAssembly.GetType(dbNamespace + ".GroupTypeId");
            var property = owner == null ? null : owner.GetProperty(under.Item2, flags);
            group = property == null ? null : property.GetValue(null);
        }
    }
    catch (Exception) { group = null; }
    return group == null ? null : Tuple.Create(method, group);
};

FamilyManager fm = doc.IsFamilyDocument ? doc.FamilyManager : null;

Func<ConnectorElement, string> label = c =>
{
    var planes = onPlanes.ContainsKey(c.Id) ? onPlanes[c.Id] : new List<string>();
    return domainWords(c) + " connector " + c.Id.ToString()
        + (planes.Count > 0 ? " on \"" + string.Join("\" / \"", planes) + "\"" : "");
};

// The family parameter driving one of the connector's own parameters, or null.
Func<ConnectorElement, BuiltInParameter, string> drivenBy = (c, bip) =>
{
    var own = c.get_Parameter(bip);
    if (own == null || fm == null) return null;
    try
    {
        var driver = fm.GetAssociatedFamilyParameter(own);
        return driver == null ? null : driver.Definition.Name;
    }
    catch (Exception) { return null; }
};

Func<ConnectorElement, string> sizeWords = c =>
{
    if (c.Domain != Domain.DomainHvac && c.Domain != Domain.DomainPiping) return "no size";
    try
    {
        if (c.Shape == ConnectorProfileType.Round)
        {
            var by = drivenBy(c, BuiltInParameter.CONNECTOR_DIAMETER);
            return "round " + mm(c.Radius * 2) + " mm" + (by == null ? " (size driven by no parameter)" : " (diameter <- " + by + ")");
        }
        var w = drivenBy(c, BuiltInParameter.CONNECTOR_WIDTH);
        var h = drivenBy(c, BuiltInParameter.CONNECTOR_HEIGHT);
        return c.Shape.ToString().ToLowerInvariant() + " " + mm(c.Width) + " x " + mm(c.Height) + " mm (width <- "
            + (w ?? "no parameter") + ", height <- " + (h ?? "no parameter") + ")";
    }
    catch (Exception) { return "size unreadable"; }
};

// One typed value as Heron reads it - "400 V", "3 poles", "90 degrees",
// "12 L/s", "Lagging" - with the family parameter driving it, or null where
// the connector has no such field.
Func<ConnectorElement, BuiltInParameter, string> typedValue = (c, bip) =>
{
    var p = c.get_Parameter(bip);
    if (p == null) return null;
    string value;
    if (p.StorageType == StorageType.Integer)
    {
        var n = p.AsInteger();
        value = bip == BuiltInParameter.RBS_ELEC_POWER_FACTOR_STATE && Enum.IsDefined(typeof(PowerFactorStateType), n)
            ? Enum.ToObject(typeof(PowerFactorStateType), n).ToString() : n.ToString(invariant);
    }
    else if (p.StorageType == StorageType.ElementId)
    {
        var named = p.AsElementId() == null ? null : doc.GetElement(p.AsElementId());
        value = named == null ? "none" : named.Name;
    }
    else if (p.StorageType == StorageType.Double)
    {
        var d = p.AsDouble();
        value = bip == BuiltInParameter.RBS_ELEC_VOLTAGE ? Math.Round(d / perVolt, 2).ToString(invariant) + " V"
            : bip == BuiltInParameter.RBS_ELEC_APPARENT_LOAD ? Math.Round(d / perVolt, 2).ToString(invariant) + " VA"
            : bip == BuiltInParameter.CONNECTOR_ANGLE ? Math.Round(d / perDegree, 2).ToString(invariant) + " degrees"
            : bip == BuiltInParameter.RBS_DUCT_FLOW_PARAM || bip == BuiltInParameter.RBS_PIPE_FLOW_PARAM
                ? Math.Round(d / perLitrePerSecond, 3).ToString(invariant) + " L/s"
            : Math.Round(d, 4).ToString(invariant);
    }
    else value = p.AsString() ?? "";
    var by = drivenBy(c, bip);
    return value + (by == null ? "" : " (<- " + by + ")");
};

Func<ConnectorElement, string> electricalWords = c =>
{
    if (c.Domain != Domain.DomainElectrical) return "";
    var parts = new List<string>();
    var voltage = typedValue(c, BuiltInParameter.RBS_ELEC_VOLTAGE);
    if (voltage != null) parts.Add(voltage);
    var poles = typedValue(c, BuiltInParameter.RBS_ELEC_NUMBER_OF_POLES);
    if (poles != null) parts.Add(poles + " pole(s)");
    var load = typedValue(c, BuiltInParameter.RBS_ELEC_APPARENT_LOAD);
    if (load != null) parts.Add(load);
    var factor = typedValue(c, BuiltInParameter.RBS_ELEC_POWER_FACTOR);
    if (factor != null) parts.Add("power factor " + factor);
    var state = typedValue(c, BuiltInParameter.RBS_ELEC_POWER_FACTOR_STATE);
    if (state != null) parts.Add("power factor state " + state);
    var loadClass = typedValue(c, BuiltInParameter.RBS_ELEC_LOAD_CLASSIFICATION);
    if (loadClass != null) parts.Add("load classification " + loadClass);
    return parts.Count == 0 ? "" : ", " + string.Join(", ", parts);
};

// VERSION 4: THE TICK BOXES AND THE OTHER TYPED VALUES as read - Allow Slope
// Adjustments on a pipe connector either way, Utility where it is ticked, a
// fitting connector's Angle, the preset Flow of a connector whose Flow
// Configuration is Preset and the Flow Factor of one whose configuration is
// System. A connector that has no such field says nothing of it.
Func<ConnectorElement, string> moreWords = c =>
{
    var parts = new List<string>();
    var slope = c.Domain == Domain.DomainPiping ? c.get_Parameter(BuiltInParameter.RBS_ADJUSTABLE_CONNECTOR) : null;
    if (slope != null && slope.StorageType == StorageType.Integer)
        parts.Add(slope.AsInteger() == 1 ? "slope adjustments allowed" : "no slope adjustments");
    var utility = c.get_Parameter(BuiltInParameter.CONNECTOR_UTILITY_PARAM);
    if (utility != null && utility.StorageType == StorageType.Integer && utility.AsInteger() == 1) parts.Add("utility");
    var fitting = c.SystemClassification == MEPSystemClassification.Fitting || c.Domain == Domain.DomainCableTrayConduit;
    var angle = fitting && angleOf(c).HasValue ? typedValue(c, angleOf(c).Value) : null;
    if (angle != null) parts.Add("angle " + angle);
    var configuration = enumRead(c, flowConfigurationOf(c));
    var flow = configuration == "Preset" && flowOf(c).HasValue ? typedValue(c, flowOf(c).Value) : null;
    if (flow != null) parts.Add("preset flow " + flow);
    var factor = configuration == "System" && (c.Domain == Domain.DomainHvac || c.Domain == Domain.DomainPiping)
        ? typedValue(c, BuiltInParameter.RBS_FLOW_FACTOR_PARAM) : null;
    if (factor != null) parts.Add("flow factor " + factor);
    return parts.Count == 0 ? "" : ", " + string.Join(", ", parts);
};

Func<ConnectorElement, string> row = c =>
{
    var linked = c.GetLinkedConnectorElement();
    var text = label(c) + ", " + spaced(c.SystemClassification.ToString()) + ", " + sizeWords(c) + electricalWords(c);
    var direction = enumRead(c, flowDirectionOf(c));
    if (direction != null) text += ", flow " + spaced(direction);
    var configuration = enumRead(c, flowConfigurationOf(c));
    if (configuration != null) text += ", flow configuration " + spaced(configuration);
    var loss = enumRead(c, lossMethodOf(c));
    if (loss != null && loss != "NotDefined") text += ", loss method " + spaced(loss);
    var description = c.get_Parameter(BuiltInParameter.RBS_CONNECTOR_DESCRIPTION);
    if (description != null && !string.IsNullOrEmpty(description.AsString())) text += ", described \"" + description.AsString() + "\"";
    text += moreWords(c);
    var at = c.Origin;
    text += ", pointing " + pointing(c.Direction)
        + (at == null ? "" : ", at X " + mm(at.X) + ", Y " + mm(at.Y) + ", Z " + mm(at.Z) + " mm")
        + " - " + (c.IsPrimary ? "PRIMARY" : "secondary") + ", "
        + (linked == null ? "not linked" : "linked to " + linked.Id.ToString());
    return text;
};

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "A connector's settings are changed inside the family - open it first.";
}
else
{
    all = new FilteredElementCollector(doc).OfClass(typeof(ConnectorElement)).Cast<ConnectorElement>()
        .OrderBy(c => c.Id.ToString(), StringComparer.Ordinal).ToList();

    // EVERY NAMED PLANE A FACE CAN LIE ON: reference planes by their own name,
    // and levels.
    var planes = new List<Tuple<string, Plane>>();
    foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
        if (ownName(rp).Length > 0) planes.Add(Tuple.Create(ownName(rp), rp.GetPlane()));
    foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
        planes.Add(Tuple.Create(level.Name, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, level.Elevation))));

    foreach (var c in all)
    {
        var names = new List<string>();
        var origin = c.Origin;
        var direction = c.Direction;
        foreach (var p in planes)
        {
            var normal = p.Item2.Normal;
            if (origin == null || direction == null) continue;
            if (Math.Abs(normal.DotProduct(origin - p.Item2.Origin)) < halfMillimetre
                && Math.Abs(normal.DotProduct(direction)) > 0.9999)
                names.Add(p.Item1);
        }
        onPlanes[c.Id] = names;
    }

    // A connector by its id, or by the plane its face lies on.
    Func<string, List<ConnectorElement>> named = token =>
    {
        var t = (token ?? "").Trim();
        var direct = all.Where(c => c.Id.ToString() == t || c.UniqueId == t).ToList();
        if (direct.Count > 0) return direct;
        return all.Where(c => onPlanes[c.Id].Any(n => squash(n) == squash(t))).ToList();
    };
    var listed = all.Count == 0 ? "none" : string.Join("; ", all.Select(c => label(c)).Take(12));
    Func<string, ConnectorElement> one = token =>
    {
        var found = named(token);
        if (found.Count == 1) return found[0];
        if (found.Count == 0)
            problems.Add("No connector lies on a plane called \"" + token.Trim() + "\" or has that id. This family's "
                + "connectors: " + listed + ".");
        else
            problems.Add(found.Count + " connectors lie on \"" + token.Trim() + "\" - "
                + string.Join(", ", found.Select(c => c.Id.ToString())) + ". Name one by its id.");
        return null;
    };

    var primaryWord = (primary ?? "").Trim();
    var linkEntries = (links ?? "").Split('|').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
    var connectorWords = (connectors ?? "").Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
    var settingEntries = (settings ?? "").Split(';').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();

    if (all.Count == 0)
        problems.Add("This family has no connectors - ADD_FAMILY_CONNECTOR puts one on a face first.");
    else if (primaryWord.Length == 0 && linkEntries.Count == 0 && settingEntries.Count == 0)
        reading = true;
    else
    {
        // THE CONNECTORS THE SETTINGS ARE FOR.
        if (settingEntries.Count > 0)
        {
            if (connectorWords.Count == 0)
                problems.Add("Settings were given but no connectors - name them by the plane their face lies on, "
                    + "\"Port Left, Port Right\", by id, or \"all\".");
            else if (connectorWords.Count == 1 && squash(connectorWords[0]) == "all")
                targets = all.ToList();
            else
                foreach (var word in connectorWords)
                {
                    var found = one(word);
                    if (found != null && !targets.Any(t => t.Id == found.Id)) targets.Add(found);
                }
        }

        // THE SETTINGS, each "name=value".
        foreach (var entry in settingEntries)
        {
            var at = entry.IndexOf('=');
            var key = squash(at < 0 ? entry : entry.Substring(0, at));
            var value = at < 0 ? "" : entry.Substring(at + 1).Trim();
            if (key == "system" || key == "systemclassification" || key == "systemtype")
            {
                var name = enumWord(typeof(MEPSystemClassification), value);
                if (name == null || name == "UndefinedSystemClassification")
                    problems.Add("No System Classification is called \"" + value + "\" - Revit's names are "
                        + enumNames(typeof(MEPSystemClassification)).Replace("Undefined System Classification, ", "") + ".");
                else wantSystem = (MEPSystemClassification)Enum.Parse(typeof(MEPSystemClassification), name);
            }
            else if (key == "flowdirection" || key == "flow")
            {
                wantFlowDirection = enumWord(typeof(FlowDirectionType), value);
                if (wantFlowDirection == null)
                    problems.Add("Flow Direction \"" + value + "\" is not one of " + enumNames(typeof(FlowDirectionType)) + ".");
            }
            else if (key == "flowconfiguration")
            {
                wantFlowConfiguration = value;
                if (value.Length == 0) problems.Add("flowConfiguration was given no value.");
            }
            else if (key == "lossmethod")
            {
                wantLossMethod = value;
                if (value.Length == 0) problems.Add("lossMethod was given no value.");
            }
            else if (key == "losscoefficient" || key == "coefficient")
            {
                double number;
                if (double.TryParse(value, System.Globalization.NumberStyles.Float, invariant, out number) && number >= 0)
                    wantCoefficient = number;
                else problems.Add("lossCoefficient \"" + value + "\" is not a number of zero or more.");
            }
            else if (key == "pressuredrop" || key == "specificloss")
            {
                double number;
                if (double.TryParse(value.Replace("Pa", "").Replace("pa", "").Trim(), System.Globalization.NumberStyles.Float,
                        invariant, out number) && number >= 0)
                    wantPressure = number * pascal;
                else problems.Add("pressureDrop \"" + value + "\" is not a number of pascals, zero or more.");
            }
            else if (key == "description")
                wantDescription = value;
            else if (key == "flip" || key == "flipdirection")
            {
                var word = squash(value);
                if (word == "true" || word == "yes" || word.Length == 0) flip = true;
                else if (word != "false" && word != "no")
                    problems.Add("flip takes true or false, not \"" + value + "\".");
            }
            else if (key == "sizeparameters" || key == "size" || key == "sizes")
            {
                sizesAsked = true;
                foreach (var name in value.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0))
                {
                    var p = fm.get_Parameter(name);
                    if (p == null) problems.Add("\"" + name + "\" is not a parameter of this family - ADD_FAMILY_PARAMETERS makes one.");
                    else wantSizes.Add(p);
                }
                if (wantSizes.Count == 0 && problems.Count == 0) problems.Add("sizeParameters named no parameter.");
            }
            else if (typedKeys.ContainsKey(key))
            {
                // A NUMBER WITH ITS UNIT, A [FAMILY PARAMETER], OR BOTH.
                var what = typedKeys[key];
                var split = System.Text.RegularExpressions.Regex.Match(value, @"^(?<n>[^\[\]]*?)\s*(\[(?<p>[^\[\]]*)\])?\s*$");
                var numberText = split.Success ? split.Groups["n"].Value.Trim() : value;
                string driver = split.Success && split.Groups["p"].Success ? split.Groups["p"].Value.Trim() : null;
                double? inside = null;
                var ok = split.Success;
                if (driver != null && driver.Length == 0)
                {
                    problems.Add(what.Item1 + " \"" + value + "\" has empty brackets - name the family parameter inside them.");
                    ok = false;
                }
                if (ok && numberText.Length == 0 && driver == null)
                {
                    problems.Add(what.Item1 + " was given no value.");
                    ok = false;
                }
                if (ok && numberText.Length > 0)
                {
                    System.Text.RegularExpressions.Match read;
                    double n;
                    var options = System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                    if (what.Item1 == "Voltage")
                    {
                        read = System.Text.RegularExpressions.Regex.Match(numberText, @"^([0-9.eE+-]+)\s*(kv|v)?$", options);
                        if (read.Success && double.TryParse(read.Groups[1].Value, System.Globalization.NumberStyles.Float, invariant, out n)
                            && n > 0)
                            inside = n * (read.Groups[2].Value.ToLowerInvariant() == "kv" ? 1000 : 1) * perVolt;
                        else problems.Add("Voltage \"" + value + "\" is not a voltage above 0 - \"400 V\", \"230\", \"11 kV\".");
                    }
                    else if (what.Item1 == "Apparent Load")
                    {
                        read = System.Text.RegularExpressions.Regex.Match(numberText, @"^([0-9.eE+-]+)\s*(kva|va)?$", options);
                        if (read.Success && double.TryParse(read.Groups[1].Value, System.Globalization.NumberStyles.Float, invariant, out n)
                            && n >= 0)
                            inside = n * (read.Groups[2].Value.ToLowerInvariant() == "kva" ? 1000 : 1) * perVolt;
                        else problems.Add("Apparent Load \"" + value + "\" is not volt-amperes, 0 or more - \"1500 VA\", "
                            + "\"1.5 kVA\". A load in watts is not an apparent load.");
                    }
                    else if (what.Item1 == "Number of Poles")
                    {
                        read = System.Text.RegularExpressions.Regex.Match(numberText, @"^([0-9]+)\s*(-?\s*(phases?|poles?|p))?$", options);
                        int poles;
                        if (read.Success && int.TryParse(read.Groups[1].Value, System.Globalization.NumberStyles.Integer, invariant, out poles)
                            && poles >= 1 && poles <= 3)
                            inside = poles;
                        else problems.Add("Number of Poles \"" + value + "\" is not 1, 2 or 3 - a three-phase connector is 3.");
                    }
                    else if (what.Item1 == "Angle")
                    {
                        // DEGREES, as the Properties panel shows an angle; Revit keeps radians.
                        read = System.Text.RegularExpressions.Regex.Match(numberText, @"^([0-9.eE+-]+)\s*(deg|degs|degree|degrees)?$", options);
                        if (read.Success && double.TryParse(read.Groups[1].Value, System.Globalization.NumberStyles.Float, invariant, out n)
                            && n >= 0 && n <= 360)
                            inside = n * perDegree;
                        else problems.Add("Angle \"" + value + "\" is not an angle in degrees, 0 to 360 - \"90\", \"45 degrees\".");
                    }
                    else if (what.Item1 == "Flow Factor")
                    {
                        read = System.Text.RegularExpressions.Regex.Match(numberText, @"^([0-9.eE+-]+)\s*(%)?$", options);
                        if (read.Success && double.TryParse(read.Groups[1].Value, System.Globalization.NumberStyles.Float, invariant, out n))
                        {
                            if (read.Groups[2].Value == "%") n = n / 100.0;
                            if (n >= 0 && n <= 1) inside = n;
                            else problems.Add("Flow Factor \"" + value + "\" is not 0 to 1 - \"0.5\" or \"50%\", the share of the "
                                + "system's flow this connector takes.");
                        }
                        else problems.Add("Flow Factor \"" + value + "\" is not a number - \"0.5\" or \"50%\".");
                    }
                    else if (what.Item1 == "Flow")
                    {
                        // LITRES PER SECOND unless m3/h or cfm is written; Revit keeps cubic feet per second.
                        read = System.Text.RegularExpressions.Regex.Match(numberText,
                            @"^([0-9.eE+-]+)\s*(l/s|lps|l/sec|m3/h|m3/hr|cmh|cfm)?$", options);
                        if (read.Success && double.TryParse(read.Groups[1].Value, System.Globalization.NumberStyles.Float, invariant, out n)
                            && n >= 0)
                        {
                            var unit = read.Groups[2].Value.ToLowerInvariant();
                            var litres = unit == "m3/h" || unit == "m3/hr" || unit == "cmh" ? n / 3.6
                                : unit == "cfm" ? n * 0.3048 * 0.3048 * 0.3048 * 1000.0 / 60.0 : n;
                            inside = litres * perLitrePerSecond;
                        }
                        else problems.Add("Flow \"" + value + "\" is not a flow, 0 or more - \"12 L/s\", \"100 m3/h\", \"250 cfm\".");
                    }
                    else
                    {
                        read = System.Text.RegularExpressions.Regex.Match(numberText, @"^([0-9.eE+-]+)\s*(%)?$", options);
                        if (read.Success && double.TryParse(read.Groups[1].Value, System.Globalization.NumberStyles.Float, invariant, out n))
                        {
                            if (read.Groups[2].Value == "%") n = n / 100.0;
                            if (n > 0 && n <= 1) inside = n;
                            else problems.Add("Power Factor \"" + value + "\" is not above 0 and at most 1 - \"0.85\" or \"85%\".");
                        }
                        else problems.Add("Power Factor \"" + value + "\" is not a number - \"0.85\" or \"85%\".");
                    }
                    if (!inside.HasValue) ok = false;
                }
                if (ok)
                {
                    if (typed.Any(e => e.Item1 == what.Item1))
                        problems.Add(what.Item1 + " is given twice.");
                    else typed.Add(Tuple.Create(what.Item1, key, inside, driver, value));
                }
            }
            else if (tickKeys.ContainsKey(key))
            {
                // A TICK BOX: yes or no, as SET_FAMILY_SETTINGS reads one.
                var what = tickKeys[key];
                var word = squash(value);
                if (ticks.Any(t => t.Item1 == what.Item1))
                    problems.Add(what.Item1 + " is given twice.");
                else if (word == "yes" || word == "true" || word == "on" || word == "1" || word == "ticked" || word == "tick")
                    ticks.Add(Tuple.Create(what.Item1, what.Item2, what.Item3, true));
                else if (word == "no" || word == "false" || word == "off" || word == "0" || word == "unticked" || word == "untick")
                    ticks.Add(Tuple.Create(what.Item1, what.Item2, what.Item3, false));
                else
                    problems.Add(what.Item1 + " takes yes or no, not \"" + value + "\".");
            }
            else if (key == "powerfactorstate" || key == "pfstate")
            {
                wantPowerFactorState = value;
                if (value.Length == 0) problems.Add("powerFactorState was given no value - Lagging or Leading.");
            }
            else if (key == "loadclassification" || key == "loadclass")
            {
                loadClassAsked = true;
                var classes = new FilteredElementCollector(doc).OfClass(typeof(ElectricalLoadClassification))
                    .Cast<ElectricalLoadClassification>().ToList();
                wantLoadClass = classes.FirstOrDefault(l => squash(l.Name) == squash(value));
                if (value.IndexOf('[') >= 0)
                    problems.Add("Load Classification takes a classification's name, not a family parameter.");
                else if (wantLoadClass == null)
                    problems.Add("No Load Classification is called \"" + value + "\" in this family - it has "
                        + (classes.Count == 0 ? "none." : string.Join(", ", classes.Select(l => l.Name).OrderBy(n => n)) + "."));
            }
            else
                problems.Add("\"" + entry + "\" is not a connector setting. The settings are system, flowDirection, "
                    + "flowConfiguration, lossMethod, lossCoefficient, pressureDrop (Pa), description, flip, "
                    + "sizeParameters and utility (yes/no); on a pipe connector allowSlopeAdjustments (yes/no); on a duct "
                    + "or pipe connector flowFactor and presetFlow; on a fitting's connector angle; and on an electrical "
                    + "connector voltage, poles, apparentLoad, powerFactor, powerFactorState and loadClassification - "
                    + "written \"name=value\" with `;` between.");
        }

        // EACH SETTING CHECKED AGAINST EACH CONNECTOR, before anything changes.
        foreach (var c in targets)
        {
            if (wantSystem.HasValue && !c.IsSystemClassificationValid(wantSystem.Value))
            {
                var valid = Enum.GetValues(typeof(MEPSystemClassification)).Cast<MEPSystemClassification>()
                    .Where(v => v != MEPSystemClassification.UndefinedSystemClassification && c.IsSystemClassificationValid(v))
                    .Select(v => spaced(v.ToString())).ToList();
                problems.Add("Revit does not allow " + spaced(wantSystem.Value.ToString()) + " on " + label(c)
                    + " - a " + domainWords(c) + " connector takes " + (valid.Count == 0 ? "no other" : string.Join(", ", valid)) + ".");
            }
            Action<string, string, Tuple<BuiltInParameter, Type>, string> canTake = (what, whose, which, word) =>
            {
                if (word == null) return;
                if (which == null)
                {
                    // ANOTHER DOMAIN'S FIELD - refused naming the connector and its domain.
                    problems.Add(label(c) + " is a " + domainWords(c) + " connector - " + what + " is " + whose + ".");
                    return;
                }
                var p = parameterOf(c, which);
                if (p == null || p.StorageType != StorageType.Integer)
                {
                    problems.Add(label(c) + " has no " + what + " in this Revit.");
                    return;
                }
                if (enumWord(which.Item2, word) == null)
                    problems.Add(what + " \"" + word + "\" is not one of " + enumNames(which.Item2) + " on a "
                        + domainWords(c) + " connector.");
            };
            canTake("Flow Direction", "a duct or pipe connector's", flowDirectionOf(c), wantFlowDirection);
            canTake("Flow Configuration", "a duct or pipe connector's", flowConfigurationOf(c), wantFlowConfiguration);
            canTake("Loss Method", "a duct or pipe connector's", lossMethodOf(c), wantLossMethod);
            canTake("Power Factor State", "an electrical connector's", powerFactorStateOf(c), wantPowerFactorState);
            if (wantCoefficient.HasValue && c.get_Parameter(BuiltInParameter.RBS_LOSS_COEFFICIENT) == null)
                problems.Add(label(c) + " has no Loss Coefficient in this Revit.");
            if (wantPressure.HasValue && c.get_Parameter(BuiltInParameter.RBS_PRESSURE_DROP) == null)
                problems.Add(label(c) + " has no Pressure Drop in this Revit.");
            if (wantDescription != null && c.get_Parameter(BuiltInParameter.RBS_CONNECTOR_DESCRIPTION) == null)
                problems.Add(label(c) + " has no Description in this Revit.");
            if (sizesAsked)
            {
                if (c.Domain != Domain.DomainHvac && c.Domain != Domain.DomainPiping)
                    problems.Add(label(c) + " has no size, so no size parameters are taken.");
                else if (c.Shape == ConnectorProfileType.Round && wantSizes.Count != 1)
                    problems.Add(label(c) + " is round: ONE length parameter, its diameter.");
                else if (c.Shape != ConnectorProfileType.Round && wantSizes.Count != 2)
                    problems.Add(label(c) + " is " + c.Shape.ToString().ToLowerInvariant() + ": TWO length parameters, "
                        + "width then height.");
            }

            // THE TICK BOXES AND THE TYPED VALUES - a field another domain's
            // connector has not got is refused naming the connector and its
            // domain; then each field is checked on the connector, and each
            // family parameter against its kind.
            if (loadClassAsked && c.Domain != Domain.DomainElectrical)
                problems.Add(label(c) + " is a " + domainWords(c) + " connector - Load Classification is an electrical "
                    + "connector's.");
            else if (loadClassAsked && wantLoadClass != null
                && c.get_Parameter(BuiltInParameter.RBS_ELEC_LOAD_CLASSIFICATION) == null)
                problems.Add(label(c) + " has no Load Classification in this Revit.");
            foreach (var t in ticks)
            {
                var which = t.Item3(c);
                if (!which.HasValue)
                {
                    problems.Add(label(c) + " is a " + domainWords(c) + " connector - " + t.Item1 + " is " + t.Item2 + ".");
                    continue;
                }
                var box = c.get_Parameter(which.Value);
                if (box == null)
                    problems.Add(label(c) + " (" + spaced(c.SystemClassification.ToString()) + ") has no " + t.Item1
                        + " in this Revit.");
                else if (box.StorageType != StorageType.Integer)
                    problems.Add(label(c) + "'s " + t.Item1 + " is not a yes/no box in this Revit.");
                else if (drivenBy(c, which.Value) != null)
                    problems.Add(label(c) + "'s " + t.Item1 + " is driven by the family parameter \""
                        + drivenBy(c, which.Value) + "\" - LINK_FAMILY_PARAMETER with \"none\" unlinks it first.");
                else if (box.IsReadOnly)
                    problems.Add(label(c) + "'s " + t.Item1 + " is read-only on a " + spaced(c.SystemClassification.ToString())
                        + " connector - Revit decides it there.");
            }
            foreach (var e in typed)
            {
                var what = typedKeys[e.Item2];
                var which = what.Item3(c);
                if (!which.HasValue)
                {
                    problems.Add(label(c) + " is a " + domainWords(c) + " connector - " + e.Item1 + " is " + what.Item2 + ".");
                    continue;
                }
                var own = c.get_Parameter(which.Value);
                if (own == null)
                {
                    problems.Add(label(c) + " (" + spaced(c.SystemClassification.ToString()) + ") has no " + e.Item1
                        + " in this Revit.");
                    continue;
                }
                var by = drivenBy(c, which.Value);
                if (e.Item4 == null)
                {
                    if (by != null)
                        problems.Add(label(c) + "'s " + e.Item1 + " is driven by the family parameter \"" + by + "\". "
                            + "Give \"" + e.Item5 + " [" + by + "]\" to write the value into the current type, or "
                            + "LINK_FAMILY_PARAMETER with \"none\" to unlink it first.");
                    else if (own.IsReadOnly && !wantSystem.HasValue)
                        problems.Add(label(c) + "'s " + e.Item1 + " is read-only on a "
                            + spaced(c.SystemClassification.ToString()) + " connector - Revit works it out from the "
                            + "connector's other values.");
                    continue;
                }
                if (!fm.CanElementParameterBeAssociated(own))
                {
                    problems.Add(label(c) + "'s " + e.Item1 + " cannot be driven by a family parameter in this Revit.");
                    continue;
                }
                var driver = fm.get_Parameter(e.Item4);
                if (driver == null)
                {
                    if (addParameterFor(kindOf(own.Definition), what.Item4) == null)
                        problems.Add("\"" + e.Item4 + "\" is not a parameter of this family, and this Revit gives no "
                            + "way to make one of " + e.Item1 + "'s kind here - ADD_FAMILY_SHARED_PARAMETERS adds one "
                            + "from the shared parameter file.");
                }
                else if (!sameKind(kindOf(driver.Definition), kindOf(own.Definition)))
                    problems.Add("\"" + e.Item4 + "\" holds " + kindWords(kindOf(driver.Definition)) + ", and "
                        + label(c) + "'s " + e.Item1 + " is " + kindWords(kindOf(own.Definition)) + " - Revit links "
                        + "a field only to a parameter of its own kind.");
                else if (driver.IsReporting)
                    problems.Add("\"" + e.Item4 + "\" is a reporting parameter, and cannot drive " + e.Item1 + ".");
                else if (e.Item3.HasValue && !string.IsNullOrEmpty(driver.Formula))
                    problems.Add("\"" + e.Item4 + "\" is worked out by the formula \"" + driver.Formula + "\", so a "
                        + "value cannot be written into it - give [" + e.Item4 + "] alone.");
            }
        }
        if (typed.Any(e => e.Item4 != null && e.Item3.HasValue) && fm.CurrentType == null)
            problems.Add("This family has no type yet, so a value cannot be written into a family parameter. "
                + "SET_FAMILY_TYPE_VALUES makes the first type.");

        // THE UNIT, CHECKED AGAINST REVIT'S OWN on the release that runs: a
        // factor that disagrees would write every voltage, load, angle and
        // flow wrong. (label, UnitTypeId property, DisplayUnitType name,
        // Heron's factor, the unit in words.)
        foreach (var unit in new[] { Tuple.Create("Voltage", "Volts", "DUT_VOLTS", perVolt, "volt"),
            Tuple.Create("Apparent Load", "VoltAmperes", "DUT_VOLT_AMPERES", perVolt, "volt-ampere"),
            Tuple.Create("Angle", "Degrees", "DUT_DECIMAL_DEGREES", perDegree, "degree"),
            Tuple.Create("Flow", "LitersPerSecond", "DUT_LITERS_PER_SECOND", perLitrePerSecond, "litre per second") })
        {
            if (!typed.Any(e => e.Item1 == unit.Item1 && e.Item3.HasValue)) continue;
            var revits = revitPerUnit(unit.Item2, unit.Item3);
            if (revits.HasValue && Math.Abs(revits.Value - unit.Item4) > 1e-9 * unit.Item4)
                problems.Add("Revit makes one " + unit.Item5 + " " + revits.Value.ToString(invariant)
                    + " internal units, and Heron would have written " + unit.Item4.ToString(invariant)
                    + " - so no " + unit.Item1 + " is written until that is looked at.");
        }

        if (sizesAsked && wantSizes.Count > 0 && fm.CurrentType == null)
            problems.Add("This family has no type yet, so its size parameters hold nothing to check the connector "
                + "against. SET_FAMILY_TYPE_VALUES makes the first type.");

        if (primaryWord.Length > 0) wantPrimary = one(primaryWord);

        foreach (var entry in linkEntries)
        {
            var halves = entry.Split('>');
            if (halves.Length != 2 || halves[0].Trim().Length == 0 || halves[1].Trim().Length == 0)
            {
                problems.Add("\"" + entry + "\" is not a pair - \"Inlet > Outlet\", or \"Inlet > none\" to unlink.");
                continue;
            }
            var from = one(halves[0]);
            var removing = squash(halves[1]) == "none";
            var to = removing ? null : one(halves[1]);
            if (from == null || (!removing && to == null)) continue;
            if (to != null && to.Id == from.Id)
            {
                problems.Add("\"" + entry + "\" links a connector to itself - Revit refuses that.");
                continue;
            }
            if (to != null && to.Domain != from.Domain)
            {
                problems.Add("\"" + entry + "\" pairs a " + domainWords(from) + " connector with a " + domainWords(to)
                    + " one - Revit links connectors of one domain only.");
                continue;
            }
            asked.Add(Tuple.Create(from, to, entry));
        }

        // A connector in two pairs would be linked twice; the second would
        // silently undo the first. ONE unlink beside ONE link is allowed - it is
        // how a connector moves to a new partner, "A > none | A > C" - because
        // the unlinks run first.
        var unlinking = new HashSet<ElementId>(asked.Where(a => a.Item2 == null).Select(a => a.Item1.Id));
        var used = asked.Where(a => a.Item2 != null).SelectMany(a => new[] { a.Item1, a.Item2 })
            .GroupBy(c => c.Id).Where(g => g.Count() > 1).Select(g => g.Key)
            .Concat(asked.Where(a => a.Item2 == null).GroupBy(a => a.Item1.Id).Where(g => g.Count() > 1).Select(g => g.Key))
            .Distinct().Select(id => id.ToString()).ToList();
        if (used.Count > 0)
            problems.Add("Connector " + string.Join(", ", used) + " appears in more than one pair - each connector "
                + "links to one other; to move one to a new partner, unlink and link it in the same call, \"A > none | "
                + "A > C\".");

        // A connector already linked to a THIRD one, not changed by this call,
        // would be left pointing at a connector that points elsewhere - unless
        // that end is unlinked in this call, which frees both.
        var touched = new HashSet<ElementId>(asked.SelectMany(a => a.Item2 == null
            ? new[] { a.Item1.Id } : new[] { a.Item1.Id, a.Item2.Id }));
        foreach (var a in asked.Where(a => a.Item2 != null))
            foreach (var end in new[] { a.Item1, a.Item2 })
            {
                var other = end == a.Item1 ? a.Item2 : a.Item1;
                var now = end.GetLinkedConnectorElement();
                if (now != null && now.Id != other.Id && !touched.Contains(now.Id) && !unlinking.Contains(end.Id))
                    problems.Add(label(end) + " is already linked to connector " + now.Id.ToString() + " - unlink "
                        + "it first, \"" + end.Id.ToString() + " > none\", in the same call or before.");
            }
    }

    if (problems.Count > 0) refused = "Nothing was changed. " + string.Join(" ", problems.Distinct());
}

// ---------------------------------------------------------------------------
// NOTHING ASKED: READ
// ---------------------------------------------------------------------------

if (refused == null && reading)
{
    connectorReport = all.Count + " connector(s): " + string.Join("; ", all.Select(c => row(c))) + ".";
    findings.Add(connectorReport);
    findings.Add("Nothing was asked, so nothing was changed. Give `connectors` and `settings` - \"system=Global; "
        + "flowDirection=In; flip=true\" - or `primary` / `links` to change them.");
}

// ---------------------------------------------------------------------------
// CHANGE, THEN READ EVERYTHING BACK
// ---------------------------------------------------------------------------

else if (refused == null)
{
    var before = all.Select(c => row(c)).ToList();
    var directionsBefore = targets.ToDictionary(c => c.Id, c => c.Direction);
    // Family parameters whose value this call has written, so a value meant
    // for several connectors is written into the type once.
    var valuesWritten = new HashSet<string>();

    Action<string, Action> attempt = (what, act) =>
    {
        try { act(); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused " + what + ": " + ex.Message
                + " The call failed, and Heron rolls the whole call back.");
        }
    };

    Action<ConnectorElement, string, Tuple<BuiltInParameter, Type>, string> setEnum = (c, what, which, word) =>
    {
        var name = enumWord(which.Item2, word);
        var now = enumRead(c, which);
        if (now == name) { findings.Add(label(c) + "'s " + what + " was already " + spaced(name) + "."); return; }
        var p = parameterOf(c, which);
        if (p.IsReadOnly)
            throw new InvalidOperationException(label(c) + "'s " + what + " is read-only here. The call failed, and Heron "
                + "rolls the whole call back.");
        attempt(what + " " + spaced(name) + " on " + label(c), () => p.Set(Convert.ToInt32(Enum.Parse(which.Item2, name), invariant)));
        changed.Add(label(c) + ": " + what + " " + (now == null ? "" : spaced(now) + " -> ") + spaced(name) + ".");
    };

    Action<ConnectorElement, BuiltInParameter, double, string, string> setNumber = (c, bip, value, what, unit) =>
    {
        var p = c.get_Parameter(bip);
        if (p.IsReadOnly)
            throw new InvalidOperationException(label(c) + "'s " + what + " is read-only - set lossMethod in the same "
                + "call first. The call failed, and Heron rolls the whole call back.");
        attempt(what + " on " + label(c), () => p.Set(value));
        changed.Add(label(c) + ": " + what + " " + unit + ".");
    };

    foreach (var c in targets)
    {
        if (wantSystem.HasValue)
        {
            if (c.SystemClassification == wantSystem.Value)
                findings.Add(label(c) + " was already " + spaced(wantSystem.Value.ToString()) + ".");
            else
            {
                var was = c.SystemClassification;
                attempt(spaced(wantSystem.Value.ToString()) + " on " + label(c), () => c.SystemClassification = wantSystem.Value);
                changed.Add(label(c) + ": system " + spaced(was.ToString()) + " -> " + spaced(wantSystem.Value.ToString()) + ".");
            }
        }
        if (wantFlowDirection != null) setEnum(c, "Flow Direction", flowDirectionOf(c), wantFlowDirection);
        if (wantFlowConfiguration != null) setEnum(c, "Flow Configuration", flowConfigurationOf(c), wantFlowConfiguration);
        if (wantLossMethod != null) setEnum(c, "Loss Method", lossMethodOf(c), wantLossMethod);
        if (wantCoefficient.HasValue)
            setNumber(c, BuiltInParameter.RBS_LOSS_COEFFICIENT, wantCoefficient.Value, "Loss Coefficient",
                wantCoefficient.Value.ToString(invariant));
        if (wantPressure.HasValue)
            setNumber(c, BuiltInParameter.RBS_PRESSURE_DROP, wantPressure.Value, "Pressure Drop",
                (wantPressure.Value / pascal).ToString(invariant) + " Pa");
        if (wantDescription != null)
        {
            var p = c.get_Parameter(BuiltInParameter.RBS_CONNECTOR_DESCRIPTION);
            attempt("the description on " + label(c), () => p.Set(wantDescription));
            changed.Add(label(c) + ": described \"" + wantDescription + "\".");
        }
        if (wantSizes.Count > 0)
        {
            var own = c.Shape == ConnectorProfileType.Round
                ? new[] { BuiltInParameter.CONNECTOR_DIAMETER }
                : new[] { BuiltInParameter.CONNECTOR_WIDTH, BuiltInParameter.CONNECTOR_HEIGHT };
            for (var i = 0; i < own.Length; i++)
            {
                var ownParameter = c.get_Parameter(own[i]);
                var driver = wantSizes[i];
                if (ownParameter == null || !fm.CanElementParameterBeAssociated(ownParameter))
                    throw new InvalidOperationException(label(c) + "'s " + own[i] + " cannot be tied to a family parameter in "
                        + "this Revit. The call failed, and Heron rolls the whole call back.");
                attempt("tying " + label(c) + " to \"" + driver.Definition.Name + "\"",
                    () => fm.AssociateElementParameterToFamilyParameter(ownParameter, driver));
                changed.Add(label(c) + ": " + (own[i] == BuiltInParameter.CONNECTOR_DIAMETER ? "diameter"
                    : own[i] == BuiltInParameter.CONNECTOR_WIDTH ? "width" : "height") + " <- " + driver.Definition.Name + ".");
            }
        }
        foreach (var t in ticks)
        {
            var box = c.get_Parameter(t.Item3(c).Value);
            var now = box.AsInteger() == 1;
            if (now == t.Item4)
            {
                findings.Add(label(c) + "'s " + t.Item1 + " was already " + (t.Item4 ? "ticked" : "unticked") + ".");
                continue;
            }
            attempt(t.Item1 + (t.Item4 ? " ticked" : " unticked") + " on " + label(c), () => box.Set(t.Item4 ? 1 : 0));
            changed.Add(label(c) + ": " + t.Item1 + " " + (now ? "yes" : "no") + " -> " + (t.Item4 ? "yes" : "no") + ".");
        }
        if (wantPowerFactorState != null) setEnum(c, "Power Factor State", powerFactorStateOf(c), wantPowerFactorState);
        foreach (var e in typed)
        {
            var what = typedKeys[e.Item2];
            var field = what.Item3(c).Value;
            var own = c.get_Parameter(field);
            if (e.Item4 == null)
            {
                var was = typedValue(c, field);
                if (own.IsReadOnly)
                    throw new InvalidOperationException(label(c) + "'s " + e.Item1 + " is read-only on a "
                        + spaced(c.SystemClassification.ToString()) + " connector. The call failed, and Heron rolls the "
                        + "whole call back.");
                attempt(e.Item1 + " " + e.Item5 + " on " + label(c), () =>
                {
                    if (own.StorageType == StorageType.Integer) own.Set((int)Math.Round(e.Item3.Value));
                    else own.Set(e.Item3.Value);
                });
                changed.Add(label(c) + ": " + e.Item1 + " " + was + " -> " + e.Item5 + ".");
                continue;
            }
            // THE FAMILY PARAMETER - made once when new, its value written once.
            var driver = fm.get_Parameter(e.Item4);
            if (driver == null)
            {
                var how = addParameterFor(kindOf(own.Definition), what.Item4);
                attempt("a family parameter \"" + e.Item4 + "\" for " + e.Item1, () =>
                    how.Item1.Invoke(fm, new object[] { e.Item4, how.Item2, kindOf(own.Definition), false }));
                driver = fm.get_Parameter(e.Item4);
                if (driver == null)
                    throw new InvalidOperationException("Revit made no parameter \"" + e.Item4 + "\". The call failed, and "
                        + "Heron rolls the whole call back.");
                changed.Add("Family parameter \"" + e.Item4 + "\" made - a type parameter of " + e.Item1 + "'s kind, "
                    + kindWords(kindOf(driver.Definition)) + ", under " + what.Item4.Item3 + ".");
            }
            if (e.Item3.HasValue && !valuesWritten.Contains(e.Item4))
            {
                attempt("writing " + e.Item5 + " into \"" + e.Item4 + "\"", () =>
                {
                    if (driver.StorageType == StorageType.Integer) fm.Set(driver, (int)Math.Round(e.Item3.Value));
                    else fm.Set(driver, e.Item3.Value);
                });
                valuesWritten.Add(e.Item4);
                changed.Add("Family parameter \"" + e.Item4 + "\" in type \"" + fm.CurrentType.Name + "\": "
                    + e.Item5.Substring(0, e.Item5.IndexOf('[')).Trim() + ".");
            }
            if (drivenBy(c, field) == e.Item4)
                findings.Add(label(c) + "'s " + e.Item1 + " was already driven by \"" + e.Item4 + "\".");
            else
            {
                var linkedTo = driver;
                attempt("tying " + label(c) + "'s " + e.Item1 + " to \"" + e.Item4 + "\"",
                    () => fm.AssociateElementParameterToFamilyParameter(own, linkedTo));
                changed.Add(label(c) + ": " + e.Item1 + " <- " + e.Item4 + ".");
            }
        }
        if (wantLoadClass != null)
        {
            var own = c.get_Parameter(BuiltInParameter.RBS_ELEC_LOAD_CLASSIFICATION);
            var was = typedValue(c, BuiltInParameter.RBS_ELEC_LOAD_CLASSIFICATION);
            attempt("Load Classification " + wantLoadClass.Name + " on " + label(c), () => own.Set(wantLoadClass.Id));
            changed.Add(label(c) + ": Load Classification " + was + " -> " + wantLoadClass.Name + ".");
        }
        if (flip)
        {
            attempt("flipping " + label(c), () => c.FlipDirection());
            changed.Add(label(c) + ": flipped, was pointing " + pointing(directionsBefore[c.Id]) + ".");
        }
    }

    if (wantPrimary != null)
    {
        if (wantPrimary.IsPrimary) findings.Add(label(wantPrimary) + " was already primary - left as it is.");
        else
        {
            attempt("making " + label(wantPrimary) + " primary", () => wantPrimary.AssignAsPrimary());
            changed.Add(label(wantPrimary) + " made primary.");
        }
    }

    // Unlinks first, so a pair asked for in the same call finds both ends free.
    foreach (var a in asked.OrderBy(x => x.Item2 == null ? 0 : 1))
    {
        var formerly = a.Item1.GetLinkedConnectorElement();
        attempt("\"" + a.Item3 + "\"", () => a.Item1.SetLinkedConnectorElement(a.Item2));
        doc.Regenerate();
        // THE LINK IS A PAIR. Where Revit set only the end it was asked about,
        // the other end is set to match, so neither points at a connector that
        // does not point back; which of the two Revit does is BV17's question.
        if (a.Item2 != null)
        {
            var back = a.Item2.GetLinkedConnectorElement();
            if (back == null || back.Id != a.Item1.Id)
            {
                a.Item2.SetLinkedConnectorElement(a.Item1);
                findings.Add("Linking " + a.Item1.Id.ToString() + " to " + a.Item2.Id.ToString() + " did not link "
                    + a.Item2.Id.ToString() + " back, so Heron linked it too.");
            }
        }
        else if (formerly != null)
        {
            var back = formerly.GetLinkedConnectorElement();
            if (back != null && back.Id == a.Item1.Id)
            {
                formerly.SetLinkedConnectorElement(null);
                findings.Add("Unlinking " + a.Item1.Id.ToString() + " left " + formerly.Id.ToString() + " pointing at "
                    + "it, so Heron unlinked that end too.");
            }
        }
        changed.Add(a.Item2 == null
            ? label(a.Item1) + " unlinked" + (formerly == null ? " (it was not linked)." : " from " + formerly.Id.ToString() + ".")
            : label(a.Item1) + " and " + label(a.Item2) + " linked.");
    }

    doc.Regenerate();

    // READ BACK - every setting asked for, on every connector named.
    Action<string> wrong = what =>
    {
        throw new InvalidOperationException(what + " The call failed, and Heron rolls the whole call back.");
    };
    foreach (var c in targets)
    {
        if (wantSystem.HasValue && c.SystemClassification != wantSystem.Value)
            wrong(label(c) + " reads " + spaced(c.SystemClassification.ToString()) + ", not " + spaced(wantSystem.Value.ToString()) + ".");
        if (wantFlowDirection != null && enumRead(c, flowDirectionOf(c)) != enumWord(typeof(FlowDirectionType), wantFlowDirection))
            wrong(label(c) + "'s Flow Direction reads " + enumRead(c, flowDirectionOf(c)) + ", not " + wantFlowDirection + ".");
        if (wantFlowConfiguration != null
            && enumRead(c, flowConfigurationOf(c)) != enumWord(flowConfigurationOf(c).Item2, wantFlowConfiguration))
            wrong(label(c) + "'s Flow Configuration reads " + enumRead(c, flowConfigurationOf(c)) + ", not " + wantFlowConfiguration + ".");
        if (wantLossMethod != null && enumRead(c, lossMethodOf(c)) != enumWord(lossMethodOf(c).Item2, wantLossMethod))
            wrong(label(c) + "'s Loss Method reads " + enumRead(c, lossMethodOf(c)) + ", not " + wantLossMethod + ".");
        if (wantCoefficient.HasValue
            && Math.Abs(c.get_Parameter(BuiltInParameter.RBS_LOSS_COEFFICIENT).AsDouble() - wantCoefficient.Value) > 1e-6)
            wrong(label(c) + "'s Loss Coefficient does not read " + wantCoefficient.Value.ToString(invariant) + ".");
        if (wantPressure.HasValue
            && Math.Abs(c.get_Parameter(BuiltInParameter.RBS_PRESSURE_DROP).AsDouble() - wantPressure.Value) > 1e-6)
            wrong(label(c) + "'s Pressure Drop does not read what was set.");
        if (wantDescription != null && (c.get_Parameter(BuiltInParameter.RBS_CONNECTOR_DESCRIPTION).AsString() ?? "") != wantDescription)
            wrong(label(c) + "'s Description does not read \"" + wantDescription + "\".");
        if (wantSizes.Count > 0)
        {
            var round = c.Shape == ConnectorProfileType.Round;
            var own = round ? new[] { BuiltInParameter.CONNECTOR_DIAMETER }
                : new[] { BuiltInParameter.CONNECTOR_WIDTH, BuiltInParameter.CONNECTOR_HEIGHT };
            for (var i = 0; i < own.Length; i++)
            {
                if (drivenBy(c, own[i]) != wantSizes[i].Definition.Name)
                    wrong(label(c) + "'s " + own[i] + " is not driven by \"" + wantSizes[i].Definition.Name + "\".");
                var holds = fm.CurrentType.AsDouble(wantSizes[i]);
                var reads = own[i] == BuiltInParameter.CONNECTOR_DIAMETER ? c.Radius * 2
                    : own[i] == BuiltInParameter.CONNECTOR_WIDTH ? c.Width : c.Height;
                if (holds.HasValue && Math.Abs(holds.Value - reads) > halfMillimetre)
                    wrong(label(c) + " reads " + mm(reads) + " mm where \"" + wantSizes[i].Definition.Name + "\" holds "
                        + mm(holds.Value) + " mm.");
            }
        }
        foreach (var t in ticks)
        {
            var box = c.get_Parameter(t.Item3(c).Value);
            if ((box.AsInteger() == 1) != t.Item4)
                wrong(label(c) + "'s " + t.Item1 + " reads " + (box.AsInteger() == 1 ? "yes" : "no") + ", not "
                    + (t.Item4 ? "yes" : "no") + ".");
            // AS REVIT ITSELF SHOWS IT - the box as the Properties panel reads it.
            findings.Add(label(c) + "'s " + t.Item1 + " reads " + (t.Item4 ? "yes" : "no") + "; Revit shows \""
                + (box.AsValueString() ?? "") + "\".");
        }
        if (wantPowerFactorState != null
            && enumRead(c, powerFactorStateOf(c)) != enumWord(powerFactorStateOf(c).Item2, wantPowerFactorState))
            wrong(label(c) + "'s Power Factor State reads " + enumRead(c, powerFactorStateOf(c)) + ", not " + wantPowerFactorState + ".");
        foreach (var e in typed)
        {
            var field = typedKeys[e.Item2].Item3(c).Value;
            var own = c.get_Parameter(field);
            if (e.Item4 != null && drivenBy(c, field) != e.Item4)
                wrong(label(c) + "'s " + e.Item1 + " is not driven by \"" + e.Item4 + "\".");
            if (e.Item3.HasValue)
            {
                var reads = own.StorageType == StorageType.Integer ? own.AsInteger() : own.AsDouble();
                if (Math.Abs(reads - e.Item3.Value) > 1e-6 * Math.Max(1.0, Math.Abs(e.Item3.Value)))
                    wrong(label(c) + "'s " + e.Item1 + " reads " + typedValue(c, field) + ", not " + e.Item5 + ".");
            }
            // AS REVIT ITSELF SHOWS IT - the check on the unit a person can read.
            findings.Add(label(c) + "'s " + e.Item1 + " reads " + typedValue(c, field) + "; Revit shows \""
                + (own.AsValueString() ?? "") + "\".");
        }
        if (wantLoadClass != null)
        {
            var own = c.get_Parameter(BuiltInParameter.RBS_ELEC_LOAD_CLASSIFICATION);
            if (own.AsElementId() != wantLoadClass.Id)
                wrong(label(c) + "'s Load Classification reads " + typedValue(c, BuiltInParameter.RBS_ELEC_LOAD_CLASSIFICATION)
                    + ", not " + wantLoadClass.Name + ".");
        }
        if (flip)
        {
            var was = directionsBefore[c.Id];
            if (c.Direction == null || was == null || c.Direction.DotProduct(was) > -0.9999)
                wrong(label(c) + " points " + pointing(c.Direction) + " after the flip; it pointed " + pointing(was) + ".");
        }
    }
    if (wantPrimary != null && !wantPrimary.IsPrimary)
        wrong(label(wantPrimary) + " does not read primary after AssignAsPrimary.");
    var relinked = new HashSet<ElementId>(asked.Where(x => x.Item2 != null).SelectMany(x => new[] { x.Item1.Id, x.Item2.Id }));
    foreach (var a in asked)
    {
        // An unlink followed by a link of the same connector is read back by the link.
        if (a.Item2 == null && relinked.Contains(a.Item1.Id)) continue;
        var now = a.Item1.GetLinkedConnectorElement();
        var back = a.Item2 == null ? null : a.Item2.GetLinkedConnectorElement();
        var right = a.Item2 == null ? now == null
            : now != null && now.Id == a.Item2.Id && back != null && back.Id == a.Item1.Id;
        if (!right)
            wrong("\"" + a.Item3 + "\" does not read back as asked - " + label(a.Item1) + " reads "
                + (now == null ? "not linked" : "linked to " + now.Id.ToString()) + ".");
    }

    // ONE PRIMARY PER DOMAIN, as read now.
    foreach (var g in all.GroupBy(c => c.Domain))
    {
        var primaries = g.Where(c => c.IsPrimary).ToList();
        if (primaries.Count != 1)
            findings.Add("The " + domainWords(g.First()) + " connectors read " + primaries.Count + " primary - one "
                + "per domain is what Revit keeps; look at them in the Family Editor.");
    }

    // A LINKED PAIR PASSES A SYSTEM THROUGH ONLY AS GLOBAL (docs/43 §10).
    foreach (var a in asked.Where(x => x.Item2 != null))
        if (a.Item1.SystemClassification != MEPSystemClassification.Global
            || a.Item2.SystemClassification != MEPSystemClassification.Global)
            findings.Add("\"" + a.Item3 + "\" is linked, but its connectors are " + spaced(a.Item1.SystemClassification.ToString())
                + " and " + spaced(a.Item2.SystemClassification.ToString()) + ". The sources docs/43 §10 records say "
                + "linked connectors pass a system through only when both are Global - give both connectors "
                + "\"system=Global\" if the system must pass through.");

    // A FITTING'S PRIMARY CONNECTOR, by Autodesk's help, is the one on the X
    // axis (mep-fitting-family-creation).
    var partType = doc.OwnerFamily == null ? null : doc.OwnerFamily.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE);
    var part = partType == null || partType.StorageType != StorageType.Integer ? (int)PartType.Undefined : partType.AsInteger();
    if (wantPrimary != null && part != (int)PartType.Undefined && part != (int)PartType.Normal
        && (wantPrimary.Direction == null || Math.Abs(wantPrimary.Direction.X) < 0.9999))
        findings.Add("This family's Part Type is " + spaced(((PartType)part).ToString()) + ", and Autodesk's help puts a "
            + "fitting's primary connector on the face lying on the X axis - " + label(wantPrimary) + " points "
            + pointing(wantPrimary.Direction) + ".");

    var after = all.Select(c => row(c)).ToList();
    connectorReport = all.Count + " connector(s), read back: " + string.Join("; ", after) + ".";
    findings.Add("Before: " + string.Join("; ", before) + ".");
    findings.Add(connectorReport);
}

if (refused != null) findings.Add(refused);
