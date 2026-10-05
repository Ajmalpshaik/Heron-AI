// NOT STANDALONE. Assumes `doc`, `uidoc` and `systemName` are in scope, and
// leaves `networkJson`, `elementCount` and `findings` behind.
//
// READS ONLY. No transaction is opened and nothing is set.
//
// THE NETWORK A SPRINKLER SYSTEM'S HYDRAULIC CALCULATION IS SOLVED ON (docs/46
// section 3). One fire protection piping system - every pipe with its length
// and inside diameter, every fitting with its part type, every valve, every
// sprinkler with the K-factor Revit holds, and for every element each piping
// connector's point and which connectors it is joined to. The brain
// (heron_sprinkler_takeoff.py) joins them into nodes; nothing here calculates
// or joins anything.
//
// WHICH SYSTEM. A name in `systemName` wins. `*` means: the system of what is
// selected - only when the model read is the one in front - else the only fire
// protection system in the model. With none chosen it reads no elements and
// lists the fire protection systems there are, so the chat can ask which.
//
// THE ELEMENTS ARE THREE SETS, JOINED. PipingNetwork holds the pipes and
// fittings; Elements holds the terminals - the sprinklers; BaseEquipment is
// neither. Each is read and de-duplicated by id.
//
// THE K-FACTOR IS READ, NEVER TRUSTED. AssignedKCoefficient is what Revit's
// own calculation runs on - the member the PROVEN REPORT_CONNECTOR_LOADS reads -
// and the unit it is held in is not one Heron relies on. So it crosses raw,
// beside any parameter whose name says K-Factor as Revit displays it, and the
// modeller confirms each type's K on the Companion (docs/46 s3.1).
//
// A TAP OR A SPUD JOINS A PIPE PART-WAY ALONG IT, through a Curve connector -
// so every connector says whether it is an End or a Curve, and the brain splits
// the pipe there.
//
// NOTHING IS DEFAULTED. A value Revit does not give is null, never 0.
//
// EVERY NUMBER CARRIES ITS UNIT IN ITS NAME: _m, _deg. Revit's internal feet
// become metres here, at the edge (D-20): 1 ft = 0.3048 m exactly. Points are
// metres in the model's own coordinates, to the millimetre.
//
// ONE STRING, BECAUSE A LIST CROSSES AS THREE NAMES (FRAGMENT-ISSUES 5b-195).
// Format 1 is defined in ONE place:
// docs/work-notes/plans/sprinkler-panel-2026-10-04.md.
//
// THE BACKSLASH AND THE QUOTE ARE BUILT FROM THEIR CHARACTER CODES, as in
// read-element-table: the tools that write these files have eaten typed
// backslashes before.

var findings = new List<string>();
var elementCount = 0;
var networkJson = "";

var FeetToMetres = 0.3048;
var backslash = ((char)92).ToString();
var quote = ((char)34).ToString();
var invariant = System.Globalization.CultureInfo.InvariantCulture;

Func<string, string> esc = text =>
{
    if (text == null) return "null";
    var sb = new System.Text.StringBuilder(quote);
    foreach (var c in text)
    {
        if (c == (char)92) sb.Append(backslash + backslash);
        else if (c == (char)34) sb.Append(backslash + quote);
        else if (c < (char)32) sb.Append(backslash + "u" + ((int)c).ToString("x4"));
        else sb.Append(c);
    }
    return sb.Append(quote).ToString();
};
Func<double, string> num = d =>
    (double.IsNaN(d) || double.IsInfinity(d)) ? "null" : Math.Round(d, 6).ToString("R", invariant);
Func<double?, string> numOrNull = d => d.HasValue ? num(d.Value) : "null";
Func<double, string> metres = d =>
    (double.IsNaN(d) || double.IsInfinity(d)) ? "null" : Math.Round(d * FeetToMetres, 3).ToString("R", invariant);
Func<XYZ, string> point = p => p == null ? "null" : "[" + metres(p.X) + "," + metres(p.Y) + "," + metres(p.Z) + "]";

// ---- the fire protection systems -------------------------------------------

// The classification is read by NAME, so no member a release lacks is named.
Func<PipingSystem, string> classification = ps =>
{
    try { return ps.SystemType.ToString(); } catch { return null; }
};
var fireSystems = new List<PipingSystem>();
foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(PipingSystem)))
{
    var ps = e as PipingSystem;
    var cls = ps == null ? null : classification(ps);
    if (cls != null && cls.StartsWith("FireProtect", StringComparison.Ordinal)) fireSystems.Add(ps);
}

Func<Element, PipingSystem> systemOf = element =>
{
    var direct = element as PipingSystem;
    if (direct != null) return direct;
    try
    {
        var curve = element as MEPCurve;
        if (curve != null) return curve.MEPSystem as PipingSystem;
        var instance = element as FamilyInstance;
        if (instance != null && instance.MEPModel != null && instance.MEPModel.ConnectorManager != null)
            foreach (Connector c in instance.MEPModel.ConnectorManager.Connectors)
            {
                var s = c.MEPSystem as PipingSystem;
                if (s != null) return s;
            }
    }
    catch { }
    return null;
};

PipingSystem chosen = null;
var wanted = (systemName ?? "").Trim();
if (wanted.Length > 0 && wanted != "*")
{
    foreach (var ps in fireSystems)
        if (string.Equals(ps.Name, wanted, StringComparison.OrdinalIgnoreCase)) { chosen = ps; break; }
    if (chosen != null) findings.Add("The system was chosen by its name, " + chosen.Name + ".");
    else findings.Add("No fire protection system is named " + quote + wanted + quote + " in this model.");
}
else
{
    // THE SELECTION ONLY FOR THE MODEL IN FRONT. A model read from behind
    // has no selection of its own; another window's would be the wrong answer.
    if (uidoc != null && uidoc.Document != null && uidoc.Document.Equals(doc))
    {
        foreach (var id in uidoc.Selection.GetElementIds())
        {
            var s = systemOf(doc.GetElement(id));
            if (s == null) continue;
            if (fireSystems.Exists(f => f.Id.Equals(s.Id)))
            {
                chosen = s;
                findings.Add("The system was chosen from the selection, " + s.Name + ".");
                break;
            }
            findings.Add("The selection is on " + s.Name + ", which is not a fire protection system.");
        }
    }
    if (chosen == null && fireSystems.Count == 1)
    {
        chosen = fireSystems[0];
        findings.Add("The model has one fire protection system, " + chosen.Name + ", so it was read.");
    }
    if (chosen == null && fireSystems.Count > 1)
        findings.Add("The model has " + fireSystems.Count + " fire protection systems and none was selected or named - say which.");
}
if (fireSystems.Count == 0) findings.Add("The model has no fire protection piping system.");

// ---- the elements: three sets, joined --------------------------------------

var elements = new List<Element>();
var seen = new HashSet<string>();
Action<Element> take = element =>
{
    if (element == null) return;
    var key = element.Id.ToString();
    if (seen.Add(key)) elements.Add(element);
};
string baseEquipment = null;
if (chosen != null)
{
    try { foreach (Element e in chosen.PipingNetwork) take(e); } catch { findings.Add("The system's pipes and fittings could not be read."); }
    try { foreach (Element e in chosen.Elements) take(e); } catch { findings.Add("The system's sprinklers could not be read."); }
    try
    {
        var b = chosen.BaseEquipment;
        if (b != null) { take(b); baseEquipment = b.Id.ToString(); }
    }
    catch { }
}

Func<Element, BuiltInCategory, bool> isCategory = (element, bic) =>
{
    try { return element.Category != null && element.Category.Id.Equals(new ElementId(bic)); }
    catch { return false; }
};
Func<Element, BuiltInParameter, double?> doubleOf = (element, which) =>
{
    try
    {
        var p = element.get_Parameter(which);
        if (p != null && p.HasValue && p.StorageType == StorageType.Double) return p.AsDouble();
    }
    catch { }
    return null;
};
Func<Element, string> levelOf = element =>
{
    try
    {
        var id = element.LevelId;
        if (id == null || id == ElementId.InvalidElementId)
        {
            var p = element.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);
            if (p == null || !p.HasValue) p = element.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
            if (p != null && p.HasValue && p.StorageType == StorageType.ElementId) id = p.AsElementId();
        }
        var level = id == null ? null : doc.GetElement(id);
        return level == null ? null : level.Name;
    }
    catch { return null; }
};
Func<Element, ConnectorManager> managerOf = element =>
{
    try
    {
        var curve = element as MEPCurve;
        if (curve != null) return curve.ConnectorManager;
        var instance = element as FamilyInstance;
        if (instance != null && instance.MEPModel != null) return instance.MEPModel.ConnectorManager;
    }
    catch { }
    return null;
};
// A parameter whose name says K-Factor, as Revit displays it - on the
// instance, then the type. Text, never a number Heron converts.
Func<Element, string> kParameter = element =>
{
    var holders = new List<Element> { element };
    try { var t = doc.GetElement(element.GetTypeId()); if (t != null) holders.Add(t); } catch { }
    foreach (var holder in holders)
        foreach (Parameter p in holder.Parameters)
        {
            try
            {
                var name = p.Definition == null ? "" : (p.Definition.Name ?? "");
                var low = name.ToLowerInvariant();
                if (!low.Contains("factor") || !(low.StartsWith("k") || low.Contains(" k"))) continue;
                if (!p.HasValue) continue;
                var shown = p.AsValueString();
                if (string.IsNullOrEmpty(shown) && p.StorageType == StorageType.String) shown = p.AsString();
                if (!string.IsNullOrEmpty(shown)) return name + " = " + shown;
            }
            catch { }
        }
    return null;
};

// The Spaces the heads sit in, for the spacing check (docs/46 s13.2): each
// read once, after the elements, with its outline.
var headSpaces = new Dictionary<string, Space>();

var body = new System.Text.StringBuilder();
var first = true;
foreach (var element in elements)
{
    string kind;
    if (element is Pipe || element is FlexPipe) kind = "pipe";
    else if (isCategory(element, BuiltInCategory.OST_PipeFitting)) kind = "fitting";
    else if (isCategory(element, BuiltInCategory.OST_PipeAccessory)) kind = "accessory";
    else if (isCategory(element, BuiltInCategory.OST_Sprinklers)) kind = "sprinkler";
    else if (isCategory(element, BuiltInCategory.OST_MechanicalEquipment)
             || isCategory(element, BuiltInCategory.OST_PlumbingFixtures)) kind = "equipment";
    else kind = "other";

    string family = null, typeName = null, typeId = null, partType = null, kRaw = null;
    double? inner = null, nominal = null, length = null, angle = null, kConnector = null;
    XYZ at = null;
    try { var t = element.GetTypeId(); if (t != null && t != ElementId.InvalidElementId) typeId = t.ToString(); } catch { }
    var instance = element as FamilyInstance;
    if (instance != null)
    {
        try { family = instance.Symbol.FamilyName; typeName = instance.Symbol.Name; } catch { }
        try { var lp = instance.Location as LocationPoint; if (lp != null) at = lp.Point; } catch { }
        try
        {
            var fitting = instance.MEPModel as MechanicalFitting;
            if (fitting != null) partType = fitting.PartType.ToString();
        }
        catch { }
        try
        {
            var a = element.LookupParameter("Angle");
            if (a != null && a.HasValue && a.StorageType == StorageType.Double) angle = a.AsDouble() * 180.0 / Math.PI;
        }
        catch { }
    }
    else
    {
        try { var t = doc.GetElement(element.GetTypeId()); if (t != null) typeName = t.Name; } catch { }
        family = kind == "pipe" ? "Pipe Types" : null;
    }
    if (kind == "pipe")
    {
        inner = doubleOf(element, BuiltInParameter.RBS_PIPE_INNER_DIAM_PARAM);
        nominal = doubleOf(element, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
        length = doubleOf(element, BuiltInParameter.CURVE_ELEM_LENGTH);
    }
    if (kind == "sprinkler") kRaw = kParameter(element);

    var connectors = new System.Text.StringBuilder();
    var firstConnector = true;
    var manager = managerOf(element);
    if (manager != null)
    {
        foreach (Connector c in manager.Connectors)
        {
            string type;
            try
            {
                if (c.Domain != Domain.DomainPiping) continue;
                if (c.ConnectorType == ConnectorType.End) type = "end";
                else if (c.ConnectorType == ConnectorType.Curve) type = "curve";
                else continue;
            }
            catch { continue; }
            XYZ origin = null;
            double? radius = null;
            try { origin = c.Origin; } catch { }
            try { if (c.Shape == ConnectorProfileType.Round) radius = c.Radius; } catch { }
            if (kind == "sprinkler" && !kConnector.HasValue)
            {
                // Throws for a connector that is not piping - guarded on its own.
                try { kConnector = c.AssignedKCoefficient; } catch { }
            }
            var to = new System.Text.StringBuilder();
            try
            {
                foreach (Connector r in c.AllRefs)
                {
                    if (r == null || r.Owner == null || r.Owner.Id.Equals(element.Id)) continue;
                    if (r.ConnectorType != ConnectorType.End && r.ConnectorType != ConnectorType.Curve) continue;
                    if (to.Length > 0) to.Append(",");
                    to.Append("[").Append(esc(r.Owner.Id.ToString())).Append(",").Append(r.Id.ToString(invariant)).Append("]");
                }
            }
            catch { }
            if (!firstConnector) connectors.Append(",");
            firstConnector = false;
            connectors.Append("{" + esc("id") + ":").Append(c.Id.ToString(invariant))
                .Append("," + esc("type") + ":").Append(esc(type))
                .Append("," + esc("point") + ":").Append(point(origin))
                .Append("," + esc("radius_m") + ":").Append(radius.HasValue ? metres(radius.Value) : "null")
                .Append("," + esc("to") + ":[").Append(to.ToString()).Append("]}");
        }
    }

    string space = null, spaceId = null;
    if (kind == "sprinkler" && at != null)
    {
        try
        {
            var s = doc.GetSpaceAtPoint(at);
            if (s != null)
            {
                space = (s.Number + " " + s.Name).Trim();
                spaceId = s.Id.ToString();
                headSpaces[spaceId] = s;
            }
        }
        catch { }
    }

    if (!first) body.Append(",");
    first = false;
    elementCount++;
    body.Append("{" + esc("id") + ":").Append(esc(element.Id.ToString()))
        .Append("," + esc("kind") + ":").Append(esc(kind))
        .Append("," + esc("category") + ":").Append(esc(element.Category == null ? null : element.Category.Name))
        .Append("," + esc("family") + ":").Append(esc(family))
        .Append("," + esc("type") + ":").Append(esc(typeName))
        .Append("," + esc("type_id") + ":").Append(esc(typeId))
        .Append("," + esc("inner_diameter_m") + ":").Append(inner.HasValue ? num(inner.Value * FeetToMetres) : "null")
        .Append("," + esc("nominal_diameter_m") + ":").Append(nominal.HasValue ? num(nominal.Value * FeetToMetres) : "null")
        .Append("," + esc("length_m") + ":").Append(length.HasValue ? num(length.Value * FeetToMetres) : "null")
        .Append("," + esc("part_type") + ":").Append(esc(partType))
        .Append("," + esc("angle_deg") + ":").Append(numOrNull(angle))
        .Append("," + esc("k") + ":").Append(kind == "sprinkler"
            ? "{" + esc("connector") + ":" + numOrNull(kConnector) + "," + esc("parameter") + ":" + esc(kRaw) + "}"
            : "null")
        .Append("," + esc("level") + ":").Append(esc(levelOf(element)))
        .Append("," + esc("space") + ":").Append(esc(space))
        .Append("," + esc("space_id") + ":").Append(esc(spaceId))
        .Append("," + esc("at") + ":").Append(point(at))
        .Append("," + esc("connectors") + ":[").Append(connectors.ToString()).Append("]}");
}

// ---- the whole network, format 1 -------------------------------------------

var json = new System.Text.StringBuilder("{" + esc("format") + ":1");
json.Append("," + esc("document") + ":").Append(esc(doc.Title));
json.Append("," + esc("systems") + ":[");
for (var i = 0; i < fireSystems.Count; i++)
{
    var ps = fireSystems[i];
    if (i > 0) json.Append(",");
    json.Append("{" + esc("id") + ":").Append(esc(ps.Id.ToString()))
        .Append("," + esc("name") + ":").Append(esc(ps.Name))
        .Append("," + esc("classification") + ":").Append(esc(classification(ps)))
        .Append("," + esc("chosen") + ":").Append(chosen != null && chosen.Id.Equals(ps.Id) ? "true" : "false")
        .Append("}");
}
json.Append("]");
if (chosen == null) json.Append("," + esc("system") + ":null");
else
    json.Append("," + esc("system") + ":{" + esc("id") + ":").Append(esc(chosen.Id.ToString()))
        .Append("," + esc("name") + ":").Append(esc(chosen.Name))
        .Append("," + esc("classification") + ":").Append(esc(classification(chosen)))
        .Append("," + esc("base_equipment") + ":").Append(esc(baseEquipment))
        .Append("}");
json.Append("," + esc("elements") + ":[").Append(body.ToString()).Append("]");

// ---- the Spaces the heads sit in, with their outlines -----------------------
//
// The first boundary loop, as Revit bounds the Space with its default options,
// each curve tessellated into points, metres in plan, a point repeating the one
// before it left out. A Space whose boundary cannot be read is listed with a
// null outline and a finding - its heads are then not checked, never guessed.
json.Append("," + esc("spaces") + ":[");
var firstSpace = true;
foreach (var pair in headSpaces)
{
    var s = pair.Value;
    var outline = new System.Text.StringBuilder();
    var points = 0;
    try
    {
        var loops = s.GetBoundarySegments(new SpatialElementBoundaryOptions());
        if (loops != null && loops.Count > 0)
        {
            XYZ last = null;
            foreach (BoundarySegment segment in loops[0])
            {
                var curve = segment.GetCurve();
                if (curve == null) continue;
                foreach (XYZ p in curve.Tessellate())
                {
                    if (last != null && p.DistanceTo(last) < 1e-6) continue;
                    if (points > 0) outline.Append(",");
                    outline.Append("[").Append(metres(p.X)).Append(",").Append(metres(p.Y)).Append("]");
                    last = p;
                    points++;
                }
            }
        }
    }
    catch { points = 0; }
    if (points < 3) findings.Add("The outline of Space " + (s.Number + " " + s.Name).Trim() + " could not be read - its heads are not checked for spacing.");
    double? area = null;
    try
    {
        var a = s.get_Parameter(BuiltInParameter.ROOM_AREA);
        if (a != null && a.HasValue && a.StorageType == StorageType.Double) area = a.AsDouble() * 0.09290304;
    }
    catch { }
    string levelName = null;
    try { if (s.Level != null) levelName = s.Level.Name; } catch { }
    if (!firstSpace) json.Append(",");
    firstSpace = false;
    json.Append("{" + esc("id") + ":").Append(esc(pair.Key))
        .Append("," + esc("number") + ":").Append(esc(s.Number))
        .Append("," + esc("name") + ":").Append(esc(s.Name))
        .Append("," + esc("level") + ":").Append(esc(levelName))
        .Append("," + esc("area_m2") + ":").Append(numOrNull(area))
        .Append("," + esc("outline") + ":").Append(points >= 3 ? "[" + outline.ToString() + "]" : "null")
        .Append("}");
}
json.Append("]");
json.Append("," + esc("findings") + ":[");
for (var i = 0; i < findings.Count; i++)
{
    if (i > 0) json.Append(",");
    json.Append(esc(findings[i]));
}
json.Append("]}");
networkJson = json.ToString();
