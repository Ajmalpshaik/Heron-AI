// NOT STANDALONE. Assumes `doc`, `primary` and `links` are in scope; leaves
// `connectorReport`, `changed`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE TWO CONNECTOR SETTINGS ADD_FAMILY_CONNECTOR LEAVES AT REVIT'S DEFAULTS:
// which connector is PRIMARY, and which pairs are LINKED. A family has one
// primary connector per domain; `ConnectorElement.AssignAsPrimary` promotes one
// and, in Revit's own remark, makes the rest of its system secondary. A LINKED
// pair passes a system through the family - a coil, a pump, a valve body - and
// the sources docs/41 §10 records say it does so only when the pair's system is
// Global, so a pair that is not is reported, never quietly changed.
//
// A CONNECTOR IS NAMED BY THE PLANE ITS FACE LIES ON - "Neck Top", the way
// ADD_FAMILY_CONNECTOR places one - or by its id. A plane holding more than one
// connector is refused with their ids, so the wrong one is never changed.
//
// EVERYTHING IS CHECKED BEFORE ANYTHING CHANGES, then read back: the primary
// must read primary and each link must read linked, both ways. A read-back that
// differs THROWS, and the host rolls the whole call back.

var findings = new List<string>();
var connectorReport = "";
var changed = new List<string>();
var notAFamily = false;
string refused = null;

var halfMillimetre = 0.5 / 304.8;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
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

// "SupplyAir" read as "Supply Air", the way a modeller writes it.
Func<string, string> spaced = name =>
{
    var text = new System.Text.StringBuilder();
    for (var i = 0; i < name.Length; i++)
    {
        if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) text.Append(' ');
        text.Append(name[i]);
    }
    return text.ToString();
};

var problems = new List<string>();
var connectors = new List<ConnectorElement>();
// The planes each connector's face lies on, by connector.
var onPlanes = new Dictionary<ElementId, List<string>>();
ConnectorElement wantPrimary = null;
// Each link asked for: (from, to - null to remove the link, as written).
var asked = new List<Tuple<ConnectorElement, ConnectorElement, string>>();

Func<ConnectorElement, string> label = c =>
{
    var planes = onPlanes.ContainsKey(c.Id) ? onPlanes[c.Id] : new List<string>();
    return domainWords(c) + " connector " + c.Id.ToString()
        + (planes.Count > 0 ? " on \"" + string.Join("\" / \"", planes) + "\"" : "");
};

Func<ConnectorElement, string> row = c =>
{
    var linked = c.GetLinkedConnectorElement();
    return label(c) + ", " + spaced(c.SystemClassification.ToString()) + ", pointing " + pointing(c.Direction)
        + " - " + (c.IsPrimary ? "PRIMARY" : "secondary") + ", "
        + (linked == null ? "not linked" : "linked to " + linked.Id.ToString());
};

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "A connector's primary flag and links are set inside the family - open it first.";
}
else
{
    connectors = new FilteredElementCollector(doc).OfClass(typeof(ConnectorElement)).Cast<ConnectorElement>()
        .OrderBy(c => c.Id.ToString(), StringComparer.Ordinal).ToList();

    // EVERY NAMED PLANE A FACE CAN LIE ON: reference planes by their own name,
    // and levels.
    var planes = new List<Tuple<string, Plane>>();
    foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
        if (ownName(rp).Length > 0) planes.Add(Tuple.Create(ownName(rp), rp.GetPlane()));
    foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
        planes.Add(Tuple.Create(level.Name, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, level.Elevation))));

    foreach (var c in connectors)
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
        var direct = connectors.Where(c => c.Id.ToString() == t || c.UniqueId == t).ToList();
        if (direct.Count > 0) return direct;
        return connectors.Where(c => onPlanes[c.Id].Any(n => squash(n) == squash(t))).ToList();
    };
    var listed = connectors.Count == 0 ? "none"
        : string.Join("; ", connectors.Select(c => label(c)).Take(12));
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

    if (connectors.Count == 0)
        problems.Add("This family has no connectors - ADD_FAMILY_CONNECTOR puts one on a face first.");
    else if (primaryWord.Length == 0 && linkEntries.Count == 0)
        problems.Add("Nothing was asked: name the connector to make primary - the plane its face lies on, \"Neck "
            + "Top\" - and/or the pairs to link, \"Inlet > Outlet\", `|` between pairs, \"Inlet > none\" to unlink.");
    else
    {
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
        // silently undo the first.
        var used = asked.SelectMany(a => a.Item2 == null ? new[] { a.Item1 } : new[] { a.Item1, a.Item2 })
            .GroupBy(c => c.Id).Where(g => g.Count() > 1).Select(g => g.Key.ToString()).ToList();
        if (used.Count > 0)
            problems.Add("Connector " + string.Join(", ", used) + " appears in more than one pair - each connector "
                + "links to one other.");

        // A connector already linked to a THIRD one, not changed by this call,
        // would be left pointing at a connector that points elsewhere.
        var touched = new HashSet<ElementId>(asked.SelectMany(a => a.Item2 == null
            ? new[] { a.Item1.Id } : new[] { a.Item1.Id, a.Item2.Id }));
        foreach (var a in asked.Where(a => a.Item2 != null))
            foreach (var end in new[] { a.Item1, a.Item2 })
            {
                var other = end == a.Item1 ? a.Item2 : a.Item1;
                var now = end.GetLinkedConnectorElement();
                if (now != null && now.Id != other.Id && !touched.Contains(now.Id))
                    problems.Add(label(end) + " is already linked to connector " + now.Id.ToString() + " - unlink "
                        + "it first, \"" + end.Id.ToString() + " > none\", in the same call or before.");
            }
    }

    if (problems.Count > 0) refused = "Nothing was changed. " + string.Join(" ", problems.Distinct());
}

// ---------------------------------------------------------------------------
// CHANGE, THEN READ EVERYTHING BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var before = connectors.Select(c => row(c)).ToList();

    if (wantPrimary != null)
    {
        if (wantPrimary.IsPrimary) findings.Add(label(wantPrimary) + " was already primary - left as it is.");
        else
        {
            try { wantPrimary.AssignAsPrimary(); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit refused to make " + label(wantPrimary) + " primary: "
                    + ex.Message + " The call failed, and Heron rolls the whole call back.");
            }
            changed.Add(label(wantPrimary) + " made primary.");
        }
    }

    // Unlinks first, so a pair asked for in the same call finds both ends free.
    foreach (var a in asked.OrderBy(x => x.Item2 == null ? 0 : 1))
    {
        var formerly = a.Item1.GetLinkedConnectorElement();
        try { a.Item1.SetLinkedConnectorElement(a.Item2); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused \"" + a.Item3 + "\": " + ex.Message + " The call failed, "
                + "and Heron rolls the whole call back.");
        }
        doc.Regenerate();
        // THE LINK IS A PAIR. Where Revit set only the end it was asked about,
        // the other end is set to match, so neither points at a connector that
        // does not point back; which of the two Revit does is BT17's question.
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

    if (wantPrimary != null && !wantPrimary.IsPrimary)
        throw new InvalidOperationException(label(wantPrimary) + " does not read primary after AssignAsPrimary. The "
            + "call failed, and Heron rolls the whole call back.");
    foreach (var a in asked)
    {
        var now = a.Item1.GetLinkedConnectorElement();
        var back = a.Item2 == null ? null : a.Item2.GetLinkedConnectorElement();
        var right = a.Item2 == null ? now == null
            : now != null && now.Id == a.Item2.Id && back != null && back.Id == a.Item1.Id;
        if (!right)
            throw new InvalidOperationException("\"" + a.Item3 + "\" does not read back as asked - "
                + label(a.Item1) + " reads " + (now == null ? "not linked" : "linked to " + now.Id.ToString())
                + ". The call failed, and Heron rolls the whole call back.");
    }

    // ONE PRIMARY PER DOMAIN, as read now.
    foreach (var g in connectors.GroupBy(c => c.Domain))
    {
        var primaries = g.Where(c => c.IsPrimary).ToList();
        if (primaries.Count != 1)
            findings.Add("The " + domainWords(g.First()) + " connectors read " + primaries.Count + " primary - one "
                + "per domain is what Revit keeps; look at them in the Family Editor.");
    }

    // A LINKED PAIR PASSES A SYSTEM THROUGH ONLY AS GLOBAL (docs/41 §10).
    foreach (var a in asked.Where(x => x.Item2 != null))
        if (a.Item1.SystemClassification != MEPSystemClassification.Global
            || a.Item2.SystemClassification != MEPSystemClassification.Global)
            findings.Add("\"" + a.Item3 + "\" is linked, but its connectors are " + spaced(a.Item1.SystemClassification.ToString())
                + " and " + spaced(a.Item2.SystemClassification.ToString()) + ". The sources docs/41 §10 records say "
                + "linked connectors pass a system through only when both are Global - set System Classification "
                + "to Global in each connector's Properties if the system must pass through. Heron did not change it.");

    // A FITTING'S PRIMARY CONNECTOR, by Autodesk's help, is the one on the X
    // axis (mep-fitting-family-creation).
    var partType = doc.OwnerFamily == null ? null : doc.OwnerFamily.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE);
    var part = partType == null || partType.StorageType != StorageType.Integer ? (int)PartType.Undefined : partType.AsInteger();
    if (wantPrimary != null && part != (int)PartType.Undefined && part != (int)PartType.Normal
        && (wantPrimary.Direction == null || Math.Abs(wantPrimary.Direction.X) < 0.9999))
        findings.Add("This family's Part Type is " + spaced(((PartType)part).ToString()) + ", and Autodesk's help puts a "
            + "fitting's primary connector on the face lying on the X axis - " + label(wantPrimary) + " points "
            + pointing(wantPrimary.Direction) + ".");

    var after = connectors.Select(c => row(c)).ToList();
    connectorReport = connectors.Count + " connector(s), read back: " + string.Join("; ", after) + ".";
    findings.Add("Before: " + string.Join("; ", before) + ".");
    findings.Add(connectorReport);
}

if (refused != null) findings.Add(refused);
