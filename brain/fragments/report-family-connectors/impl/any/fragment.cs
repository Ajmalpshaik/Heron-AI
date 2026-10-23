// NOT STANDALONE. Assumes `doc` is in scope; leaves `connectorCount`,
// `connectorList`, `notAFamily` and `findings` behind.
//
// READS ONLY; it opens no transaction and needs none.
//
// EVERY CONNECTOR OF THE FAMILY OPEN IN THE FAMILY EDITOR, with no selection:
// its domain, System Classification, size and the family parameters driving
// it, Flow Direction, Flow Configuration, Loss Method, Description, which way
// it points, where it sits, primary or secondary, what it is linked to - and
// the planes its face lies on, which is the name ADD_FAMILY_CONNECTOR and
// SET_FAMILY_CONNECTOR_ROLES take for it. REPORT_CONNECTORS reads placed
// elements in a project; a family's own connectors are ConnectorElements and
// are read here. The row is SET_FAMILY_CONNECTOR_ROLES' read-back, word for
// word, so the two never describe one connector two ways.
//
// VERSION 2 (2026-10-22): THE TICK BOXES AND THE OTHER TYPED VALUES that
// SET_FAMILY_CONNECTOR_ROLES version 4 sets are read here too, so a modeller
// reads them back without a write: a pipe connector's Allow Slope Adjustments
// either way, Utility where ticked, a fitting connector's Angle, a preset
// Flow, a System flow's Flow Factor - and an electrical connector's Voltage,
// Number of Poles, Apparent Load, Power Factor, Power Factor State and Load
// Classification, which version 3 of the write read back and version 1 here
// did not. A connector without such a field says nothing of it.
//
// ONE LINE, as REPORT_FAMILY_FORMS answers: a list is cut to three entries
// before anyone reads it.

var findings = new List<string>();
var connectorCount = 0;
var connectorList = "";
var notAFamily = false;

var halfMillimetre = 0.5 / 304.8;
// Revit keeps a voltage and a power in feet-based units, an angle in radians
// and a flow in cubic feet per second - the factors SET_FAMILY_CONNECTOR_ROLES
// writes with, read back here the same way.
var perVolt = 1.0 / (0.3048 * 0.3048);
var perDegree = Math.PI / 180.0;
var perLitrePerSecond = 0.001 / (0.3048 * 0.3048 * 0.3048);
var invariant = System.Globalization.CultureInfo.InvariantCulture;
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

// A fitting's connectors carry an Angle - duct, pipe, cable tray, conduit; the
// preset Flow is one parameter on a duct connector and another on a pipe one.
Func<ConnectorElement, BuiltInParameter?> angleOf = c =>
    c.Domain == Domain.DomainHvac || c.Domain == Domain.DomainPiping || c.Domain == Domain.DomainCableTrayConduit
    ? BuiltInParameter.CONNECTOR_ANGLE : (BuiltInParameter?)null;
Func<ConnectorElement, BuiltInParameter?> flowOf = c =>
    c.Domain == Domain.DomainHvac ? BuiltInParameter.RBS_DUCT_FLOW_PARAM
    : c.Domain == Domain.DomainPiping ? BuiltInParameter.RBS_PIPE_FLOW_PARAM : (BuiltInParameter?)null;

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

FamilyManager fm = doc.IsFamilyDocument ? doc.FamilyManager : null;

var all = new List<ConnectorElement>();
var onPlanes = new Dictionary<ElementId, List<string>>();

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
// the connector has no such field. Word for word SET_FAMILY_CONNECTOR_ROLES'.
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

// VERSION 2: THE TICK BOXES AND THE OTHER TYPED VALUES as read - Allow Slope
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
    findings.Add("The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "REPORT_CONNECTORS reads the connectors of elements selected in a project.");
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

    connectorCount = all.Count;
    connectorList = string.Join("; ", all.Select(c => row(c)));
    findings.Add(all.Count == 0
        ? "\"" + doc.Title + "\" has no connectors - ADD_FAMILY_CONNECTOR puts one on a face."
        : all.Count + " connector(s): " + connectorList + ".");
    var shared = all.Where(c => onPlanes[c.Id].Count > 0)
        .SelectMany(c => onPlanes[c.Id].Select(n => Tuple.Create(n, c.Id)))
        .GroupBy(t => t.Item1).Where(g => g.Count() > 1).ToList();
    foreach (var g in shared)
        findings.Add(g.Count() + " connectors lie on \"" + g.Key + "\" - " + string.Join(", ", g.Select(t => t.Item2.ToString()))
            + "; name them by id, not by that plane.");
}
