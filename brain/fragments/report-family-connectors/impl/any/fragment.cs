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
// ONE LINE, as REPORT_FAMILY_FORMS answers: a list is cut to three entries
// before anyone reads it.

var findings = new List<string>();
var connectorCount = 0;
var connectorList = "";
var notAFamily = false;

var halfMillimetre = 0.5 / 304.8;
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

Func<ConnectorElement, string> row = c =>
{
    var linked = c.GetLinkedConnectorElement();
    var text = label(c) + ", " + spaced(c.SystemClassification.ToString()) + ", " + sizeWords(c);
    var direction = enumRead(c, flowDirectionOf(c));
    if (direction != null) text += ", flow " + spaced(direction);
    var configuration = enumRead(c, flowConfigurationOf(c));
    if (configuration != null) text += ", flow configuration " + spaced(configuration);
    var loss = enumRead(c, lossMethodOf(c));
    if (loss != null && loss != "NotDefined") text += ", loss method " + spaced(loss);
    var description = c.get_Parameter(BuiltInParameter.RBS_CONNECTOR_DESCRIPTION);
    if (description != null && !string.IsNullOrEmpty(description.AsString())) text += ", described \"" + description.AsString() + "\"";
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
