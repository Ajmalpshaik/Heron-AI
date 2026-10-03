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
            else
                problems.Add("\"" + entry + "\" is not a connector setting. The settings are system, flowDirection, "
                    + "flowConfiguration, lossMethod, lossCoefficient, pressureDrop (Pa), description, flip and "
                    + "sizeParameters, written \"name=value\" with `;` between.");
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
            Action<string, Tuple<BuiltInParameter, Type>, string> canTake = (what, which, word) =>
            {
                if (word == null) return;
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
            canTake("Flow Direction", flowDirectionOf(c), wantFlowDirection);
            canTake("Flow Configuration", flowConfigurationOf(c), wantFlowConfiguration);
            canTake("Loss Method", lossMethodOf(c), wantLossMethod);
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
