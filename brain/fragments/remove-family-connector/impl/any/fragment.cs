// NOT STANDALONE. Assumes `doc` and `connectors` are in scope; leaves `removed`,
// `connectorReport`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// DELETES CONNECTORS FROM THE FAMILY OPEN IN THE FAMILY EDITOR - the one
// ADD_FAMILY_CONNECTOR put on the wrong face, or one the family no longer
// needs. DELETE_ELEMENTS cannot reach them: a family connector is not in a
// selection the modeller can hand over by category, and it has no level for
// FILTER_ELEMENTS_BY_CATEGORY to look under (asked for on 2026-10-07, after a
// pipe connector made on "Air Outlet" in Family1 could not be taken off).
//
// A CONNECTOR IS NAMED THE WAY SET_FAMILY_CONNECTOR_ROLES NAMES ONE - by the
// plane its face lies on, "Air Outlet", by the id REPORT_FAMILY_CONNECTORS
// gives it, or "all". A plane holding more than one connector is refused with
// their ids, so the wrong one is never deleted. A name that finds nothing is
// refused with the family's connectors listed - never guessed.
//
// ALL OR NOTHING. Every name is checked before the first connector goes; one
// refusal deletes nothing. Each connector is then deleted, and read back: it
// is no longer in the document. What Revit took WITH it - Document.Delete's
// returned set, which can be longer than what was asked for - is listed.
//
// WHAT IS LEFT BEHIND IS SAID: the family parameters that drove its size stay
// in the family (DELETE_FAMILY_PARAMETERS deletes them); a partner it was
// linked to is read back unlinked; a domain left with no PRIMARY connector is
// named with the words that set one.

var findings = new List<string>();
var removed = new List<string>();
var connectorReport = "";
var notAFamily = false;
string refused = null;

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

var all = new List<ConnectorElement>();
// The planes each connector's face lies on, by connector.
var onPlanes = new Dictionary<ElementId, List<string>>();
var targets = new List<ConnectorElement>();
var problems = new List<string>();
FamilyManager fm = doc.IsFamilyDocument ? doc.FamilyManager : null;

Func<ConnectorElement, string> label = c =>
{
    var planes = onPlanes.ContainsKey(c.Id) ? onPlanes[c.Id] : new List<string>();
    return domainWords(c) + " connector " + c.Id.ToString()
        + (planes.Count > 0 ? " on \"" + string.Join("\" / \"", planes) + "\"" : "");
};

// The family parameters driving the connector's size - they stay when it goes.
Func<ConnectorElement, List<string>> drivers = c =>
{
    var names = new List<string>();
    if (fm == null) return names;
    foreach (var bip in new[] { BuiltInParameter.CONNECTOR_DIAMETER, BuiltInParameter.CONNECTOR_RADIUS,
        BuiltInParameter.CONNECTOR_WIDTH, BuiltInParameter.CONNECTOR_HEIGHT })
    {
        var own = c.get_Parameter(bip);
        if (own == null) continue;
        try
        {
            var driver = fm.GetAssociatedFamilyParameter(own);
            if (driver != null && !names.Contains(driver.Definition.Name)) names.Add(driver.Definition.Name);
        }
        catch (Exception) { }
    }
    return names;
};

Func<ConnectorElement, string> row = c =>
{
    var linked = c.GetLinkedConnectorElement();
    var at = c.Origin;
    return label(c) + ", " + spaced(c.SystemClassification.ToString()) + ", pointing " + pointing(c.Direction)
        + (at == null ? "" : ", at X " + mm(at.X) + ", Y " + mm(at.Y) + ", Z " + mm(at.Z) + " mm")
        + " - " + (c.IsPrimary ? "PRIMARY" : "secondary") + ", "
        + (linked == null ? "not linked" : "linked to " + linked.Id.ToString());
};

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "Nothing was deleted. The document in front, \"" + doc.Title + "\", is a project, not a family open in "
        + "the Family Editor. A family's own connectors are deleted inside the family - open it first.";
}
else
{
    all = new FilteredElementCollector(doc).OfClass(typeof(ConnectorElement)).Cast<ConnectorElement>()
        .OrderBy(c => c.Id.ToString(), StringComparer.Ordinal).ToList();

    // EVERY NAMED PLANE A FACE CAN LIE ON: reference planes by their own name,
    // and levels - the same test SET_FAMILY_CONNECTOR_ROLES makes.
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

    var listed = all.Count == 0 ? "none" : string.Join("; ", all.Select(c => label(c)).Take(12));
    var words = (connectors ?? "").Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();

    if (all.Count == 0)
        problems.Add("This family has no connectors, so there is nothing to delete.");
    else if (words.Count == 0)
        problems.Add("No connector was named. Name the ones to delete by the plane their face lies on - \"Air "
            + "Outlet\" - by id, or \"all\". This family's connectors: " + listed + ".");
    else if (words.Count == 1 && squash(words[0]) == "all")
        targets = all.ToList();
    else
        foreach (var word in words)
        {
            // By id first, then by the plane its face lies on.
            var found = all.Where(c => c.Id.ToString() == word || c.UniqueId == word).ToList();
            if (found.Count == 0) found = all.Where(c => onPlanes[c.Id].Any(n => squash(n) == squash(word))).ToList();
            if (found.Count == 0)
                problems.Add("No connector lies on a plane called \"" + word + "\" or has that id. This family's "
                    + "connectors: " + listed + ".");
            else if (found.Count > 1)
                problems.Add(found.Count + " connectors lie on \"" + word + "\" - "
                    + string.Join(", ", found.Select(c => c.Id.ToString())) + ". Name the one to delete by its id.");
            else if (!targets.Any(t => t.Id == found[0].Id))
                targets.Add(found[0]);
        }

    if (problems.Count > 0) refused = "Nothing was deleted. " + string.Join(" ", problems.Distinct());
}

// ---------------------------------------------------------------------------
// DELETE, THEN READ BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var before = all.Select(c => row(c)).ToList();
    var asked = new HashSet<ElementId>(targets.Select(c => c.Id));
    // What each target leaves behind, read before it goes.
    var partners = new List<Tuple<string, ElementId>>();
    var keptDrivers = new List<string>();
    var primaryDomains = new List<Domain>();
    foreach (var c in targets)
    {
        var linked = c.GetLinkedConnectorElement();
        if (linked != null && !asked.Contains(linked.Id)) partners.Add(Tuple.Create(label(c), linked.Id));
        foreach (var name in drivers(c)) if (!keptDrivers.Contains(name)) keptDrivers.Add(name);
        if (c.IsPrimary && !primaryDomains.Contains(c.Domain)) primaryDomains.Add(c.Domain);
    }

    var alsoWent = new List<string>();
    foreach (var c in targets)
    {
        var words = row(c);
        var id = c.Id;
        if (doc.GetElement(id) == null) { removed.Add(words + " - went with an earlier one."); continue; }
        ICollection<ElementId> went;
        try { went = doc.Delete(id); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused to delete " + label(c) + ": " + ex.Message
                + " The call failed, and Heron rolls the whole call back.");
        }
        removed.Add(words + ".");
        foreach (var other in went)
        {
            if (other == id || asked.Contains(other)) continue;
            alsoWent.Add(other.ToString());
        }
    }

    doc.Regenerate();

    // READ BACK - every connector asked for is gone, and only those.
    var left = new FilteredElementCollector(doc).OfClass(typeof(ConnectorElement)).Cast<ConnectorElement>()
        .OrderBy(c => c.Id.ToString(), StringComparer.Ordinal).ToList();
    var stillThere = targets.Select(c => c.Id).Where(id => doc.GetElement(id) != null).ToList();
    if (stillThere.Count > 0)
        throw new InvalidOperationException("Connector " + string.Join(", ", stillThere.Select(i => i.ToString()))
            + " is still in the family after Revit deleted it. The call failed, and Heron rolls the whole call back.");
    var lostOthers = all.Where(c => !asked.Contains(c.Id) && !left.Any(l => l.Id == c.Id)).Select(c => c.Id.ToString()).ToList();
    if (lostOthers.Count > 0)
        throw new InvalidOperationException("Deleting the connectors asked for also took connector "
            + string.Join(", ", lostOthers) + ", which was not named. The call failed, and Heron rolls the whole call back.");

    if (alsoWent.Count > 0)
        findings.Add("Revit deleted " + alsoWent.Count + " other element(s) with them - ids "
            + string.Join(", ", alsoWent.Take(12)) + (alsoWent.Count > 12 ? ", ..." : "") + ". One Undo brings them back too.");

    foreach (var p in partners)
    {
        var partner = doc.GetElement(p.Item2) as ConnectorElement;
        if (partner == null) continue;
        var still = partner.GetLinkedConnectorElement();
        findings.Add(p.Item1 + " was linked to connector " + p.Item2.ToString() + ", which "
            + (still == null ? "now reads not linked." : "still reads linked to " + still.Id.ToString() + " - look at it in the Family Editor."));
    }

    if (keptDrivers.Count > 0)
        findings.Add("The family parameters that drove their size stay in the family: " + string.Join(", ", keptDrivers)
            + ". DELETE_FAMILY_PARAMETERS deletes them if nothing else uses them.");

    foreach (var domain in primaryDomains)
    {
        var same = left.Where(c => c.Domain == domain).ToList();
        if (same.Count > 0 && !same.Any(c => c.IsPrimary))
            findings.Add("A PRIMARY connector was deleted, and none of the " + same.Count + " " + domainWords(same[0])
                + " connector(s) left reads primary. SET_FAMILY_CONNECTOR_ROLES sets one - primary \""
                + same[0].Id.ToString() + "\".");
    }

    connectorReport = left.Count == 0 ? "No connectors are left in the family."
        : left.Count + " connector(s) left: " + string.Join("; ", left.Select(c => row(c))) + ".";
    findings.Add("Deleted " + removed.Count + " connector(s) and read back that the family no longer has them. "
        + "One Undo puts them back.");
    findings.Add("Before: " + string.Join("; ", before) + ".");
    findings.Add(connectorReport);
}

if (refused != null) findings.Add(refused);
