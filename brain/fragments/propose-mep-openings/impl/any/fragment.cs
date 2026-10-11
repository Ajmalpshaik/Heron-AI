// NOT STANDALONE. Assumes `doc`, `elements`, `hosts`, `allowanceMm` and
// `includeLinks` are in scope; leaves `proposals`, `hostIds`, `runIds`,
// `unmeasured`, `findings`, `linksSearched` and `linkedMatches` behind.
//
// NO TRANSACTION, and it needs none. IT CREATES NOTHING - cutting holes in
// somebody's structure is not a side effect of looking for them.
//
// IT MEASURES THE REAL INTERSECTION, NOT A BOUNDING BOX. A duct crossing a wall
// at an angle needs a bigger hole than its own width, and a bounding-box answer
// is right only for a perpendicular crossing.
//
// THE WALLS AND SLABS ARE USUALLY IN A LINK, AND THEY ARE READ ONLY WHEN ASKED
// FOR - D-59. With `includeLinks` set, each service's centre line is carried
// into each placement of every loaded link and intersected with that link's
// walls, floors, roofs, ceilings and structural members - the same real
// intersection, never a box - and every crossing is proposed as TEXT in
// `linkedMatches`: the linked host's type, its id IN THE LINK and its Fire
// Rating, the hole size, the depth through it and its centre in THIS model's
// coordinates. `proposals`, `hostIds` and `runIds` stay this model's own:
// `hostIds` feeds the step that cuts, and the chain revives ids against the
// HOST document, so a linked id carried there binds an unrelated element
// silently (FRAGMENT-ISSUES row 75). A hole in a linked wall is cut in THAT
// model, by whoever owns it. With links read, an empty `hosts` is not a
// refusal - the hosts are expected to be in the links. Only a linked element
// near the service's own box is opened, so a large link is not read solid by
// solid. NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM. Only model
// elements are read from a link, never its views or sheets.

var proposals = new List<string>();
var hostIds = new List<ElementId>();
var runIds = new List<ElementId>();
var unmeasured = new List<string>();
var findings = new List<string>();

var mmPerFoot = 304.8;

// ---- D-59: which links, only when asked for --------------------------------

var linksSearched = 0;
var linkedMatches = new List<string>();
var linkedTotal = 0;
var nestedLinks = 0;

// One entry per link FILE, keyed by link type - a file placed twice is one
// model placed twice, and counting placements would report a job with four
// links as having nine. LIST_LINKED_MODELS' rule, as REPORT_AREAS applies it.
var linkTypes = new List<ElementId>();
var linkDocs = new List<Document>();
var linkPlacements = new List<List<RevitLinkInstance>>();

if (includeLinks)
{
    foreach (var instance in new FilteredElementCollector(doc)
        .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
    {
        if (instance == null) continue;

        var typeId = instance.GetTypeId();
        if (typeId == null || typeId == ElementId.InvalidElementId) continue;

        var known = linkTypes.IndexOf(typeId);
        if (known >= 0) { linkPlacements[known].Add(instance); continue; }

        // LOADED IS ESTABLISHED BY ASKING FOR THE DOCUMENT, never by a status.
        Document linked = null;
        try { linked = instance.GetLinkDocument(); }
        catch (Exception) { linked = null; }
        if (linked == null) continue;

        linkTypes.Add(typeId);
        linkDocs.Add(linked);
        linkPlacements.Add(new List<RevitLinkInstance> { instance });

        try
        {
            nestedLinks += new FilteredElementCollector(linked)
                .OfClass(typeof(RevitLinkInstance)).GetElementCount();
        }
        catch (Exception) { }
    }
    linksSearched = linkDocs.Count;
}


// Fire Rating by the name Revit's own walls, floors and doors carry it under,
// on the element first and then its type. Read as the palette shows it. A
// family that calls it something else reads "not set" - said, never guessed.
// TWO PARAMETERS BY THAT NAME where the lookup lands - a shared or project
// "Fire Rating" bound beside the built-in one - read "NOT READ" with the
// count, never either value: asked by name, Revit returns "the first one
// encountered", which its own reference says "is determined at random"
// (D-54 s3, FRAGMENT-ISSUES 5b-203). The guard check-sleeve-size carries.
Func<Element, string> fireRatingOf = element =>
{
    Element type = null;
    try { type = element.Document.GetElement(element.GetTypeId()); } catch (Exception) { }
    foreach (var source in new[] { element, type })
    {
        if (source == null) continue;
        // Two by this name where the lookup lands: said, never read (5b-203).
        var sharing = 0;
        try { sharing = source.GetParameters("Fire Rating").Count; } catch (Exception) { }
        if (sharing > 1) return "NOT READ - " + sharing + " parameters share that name";
        Parameter parameter = null;
        try { parameter = source.LookupParameter("Fire Rating"); } catch (Exception) { }
        if (parameter == null || !parameter.HasValue) continue;
        string text = null;
        try
        {
            text = parameter.StorageType == StorageType.String
                ? parameter.AsString() : parameter.AsValueString();
        }
        catch (Exception) { }
        if (!string.IsNullOrEmpty(text)) return text;
    }
    return "not set";
};

// A linked element as text: the category, 'Family: Type', its id IN THE LINK
// (never this model's), and its Fire Rating.
Func<Document, Element, string> describeLinked = (linked, element) =>
{
    var typeName = "";
    try
    {
        var type = linked.GetElement(element.GetTypeId()) as ElementType;
        if (type != null)
            typeName = string.IsNullOrEmpty(type.FamilyName)
                ? type.Name : type.FamilyName + ": " + type.Name;
    }
    catch (Exception) { }
    return string.Format("{0} '{1}' (id {2} in the link), Fire Rating {3}",
        element.Category == null ? "element" : element.Category.Name,
        typeName, element.Id, fireRatingOf(element));
};

var linkBlocked = "";
var hostCount = hosts == null ? 0 : hosts.Count;

if (allowanceMm < 0)
{
    findings.Add("A negative allowance makes no sense, so NOTHING WAS PROPOSED.");
    if (linkDocs.Count > 0) linkBlocked = "Links NOT read: nothing was proposed - see findings";
}
else if (elements == null || elements.Count == 0 || (hostCount == 0 && !includeLinks))
{
    findings.Add("Both services and hosts are needed, and at least one list was empty. "
        + "NOTHING WAS PROPOSED.");
    if (linkDocs.Count > 0) linkBlocked = "Links NOT read: nothing was proposed - see findings";
}
else
{
    var options = new Options();
    options.ComputeReferences = false;
    options.DetailLevel = ViewDetailLevel.Fine;

    var intersectOptions = new SolidCurveIntersectionOptions();

    // HOST GEOMETRY IS EXTRACTED ONCE, NOT ONCE PER RUN. It used to sit inside
    // the loop below, which reads correctly and is unusable: 200 services
    // against 50 walls asked Revit for the same 50 solids 200 times over.
    // Geometry extraction is among the most expensive calls in the API, so that
    // is not a slow fragment, it is one that looks like a hang on a real model.
    var hostSolids = new List<KeyValuePair<Element, Solid>>();

    foreach (var host in hosts ?? new List<Element>())
    {
        if (host == null || !host.IsValidObject) continue;

        GeometryElement hostGeometry = null;
        try { hostGeometry = host.get_Geometry(options); } catch { }

        if (hostGeometry == null)
        {
            unmeasured.Add(host.Id + " has no geometry this could read");
            continue;
        }

        foreach (GeometryObject shape in hostGeometry)
        {
            var hostSolid = shape as Solid;
            if (hostSolid == null || hostSolid.Volume <= 0) continue;
            hostSolids.Add(new KeyValuePair<Element, Solid>(host, hostSolid));
        }
    }

    foreach (var run in elements)
    {
        if (run == null || !run.IsValidObject) continue;

        var curve = run as MEPCurve;
        if (curve == null)
        {
            unmeasured.Add(run.Id + " is not a duct, pipe, tray or conduit - it has no centre "
                + "line to intersect");
            continue;
        }

        var location = curve.Location as LocationCurve;
        if (location == null || location.Curve == null)
        {
            unmeasured.Add(curve.Id + " has no centre line");
            continue;
        }

        // Width, Height and Diameter THROW for the wrong profile rather than
        // returning zero, so each is asked for inside its own guard.
        var widthFt = 0.0;
        var heightFt = 0.0;
        var round = false;

        try { var d = curve.Diameter; if (d > 0) { widthFt = d; heightFt = d; round = true; } }
        catch { }

        if (!round)
        {
            try { widthFt = curve.Width; } catch { }
            try { heightFt = curve.Height; } catch { }
        }

        if (widthFt <= 0 && heightFt <= 0)
        {
            unmeasured.Add(curve.Id + " has no size this could read, so no hole size can be "
                + "proposed for it");
            continue;
        }

        foreach (var hostPair in hostSolids)
        {
            var host = hostPair.Key;
            var solid = hostPair.Value;

            {
                SolidCurveIntersection crossing = null;
                try { crossing = solid.IntersectWithCurve(location.Curve, intersectOptions); }
                catch { continue; }

                if (crossing == null || crossing.SegmentCount == 0) continue;

                // A RUN THROUGH TWO WALLS IS TWO OPENINGS, and a run that
                // re-enters the same solid is two as well. Each segment is its
                // own hole because that is how many have to be cut.
                for (var i = 0; i < crossing.SegmentCount; i++)
                {
                    Curve segment = null;
                    try { segment = crossing.GetCurveSegment(i); } catch { continue; }
                    if (segment == null) continue;

                    var throughMm = segment.Length * mmPerFoot;
                    var midpoint = segment.Evaluate(0.5, true);

                    var holeWidthMm = widthFt * mmPerFoot + allowanceMm * 2.0;
                    var holeHeightMm = heightFt * mmPerFoot + allowanceMm * 2.0;

                    proposals.Add("host " + host.Id + " - service " + curve.Id
                        + " - opening " + Math.Round(holeWidthMm) + " x " + Math.Round(holeHeightMm)
                        + " mm" + (round ? " (round service, squared off)" : "")
                        + ", passing through " + Math.Round(throughMm) + " mm of host"
                        + ", centred at " + Math.Round(midpoint.X * mmPerFoot) + ", "
                        + Math.Round(midpoint.Y * mmPerFoot) + ", "
                        + Math.Round(midpoint.Z * mmPerFoot) + " mm");

                    hostIds.Add(host.Id);
                    runIds.Add(curve.Id);
                }
            }
        }
    }

    // ---- D-59: the same crossings, in each link ------------------------------

    var linkCategories = new List<BuiltInCategory> { BuiltInCategory.OST_Walls,
        BuiltInCategory.OST_Floors, BuiltInCategory.OST_Roofs, BuiltInCategory.OST_Ceilings,
        BuiltInCategory.OST_StructuralFraming, BuiltInCategory.OST_StructuralColumns };

    // One link element's solids, read once however many services cross it.
    var linkSolids = new Dictionary<string, List<Solid>>();

    for (var i = 0; i < linkDocs.Count; i++)
    {
        var linked = linkDocs[i];
        var linkCount = 0;
        var summaryAt = linkedMatches.Count;
        foreach (var placement in linkPlacements[i])
        {
            Transform placed = null;
            try { placed = placement.GetTotalTransform(); } catch (Exception) { }
            if (placed == null) continue;
            var inverse = placed.Inverse;

            foreach (var run in elements)
            {
                var curve = run as MEPCurve;
                if (curve == null || !curve.IsValidObject) continue;
                var location = curve.Location as LocationCurve;
                if (location == null || location.Curve == null) continue;

                // The same size rules as above.
                var widthFt = 0.0;
                var heightFt = 0.0;
                var round = false;
                try { var d = curve.Diameter; if (d > 0) { widthFt = d; heightFt = d; round = true; } }
                catch { }
                if (!round)
                {
                    try { widthFt = curve.Width; } catch { }
                    try { heightFt = curve.Height; } catch { }
                }
                if (widthFt <= 0 && heightFt <= 0) continue;

                Curve local = null;
                try { local = location.Curve.CreateTransformed(inverse); } catch (Exception) { }
                if (local == null) continue;

                var low = local.GetEndPoint(0);
                var high = local.GetEndPoint(1);
                var outline = new Outline(
                    new XYZ(Math.Min(low.X, high.X), Math.Min(low.Y, high.Y), Math.Min(low.Z, high.Z)),
                    new XYZ(Math.Max(low.X, high.X), Math.Max(low.Y, high.Y), Math.Max(low.Z, high.Z)));

                IList<Element> near = null;
                try
                {
                    near = new FilteredElementCollector(linked)
                        .WhereElementIsNotElementType()
                        .WherePasses(new ElementMulticategoryFilter(linkCategories))
                        .WherePasses(new BoundingBoxIntersectsFilter(outline, 0.01))
                        .ToElements();
                }
                catch (Exception) { near = new List<Element>(); }

                foreach (var host in near)
                {
                    var key = i + ":" + host.Id;
                    List<Solid> solids;
                    if (!linkSolids.TryGetValue(key, out solids))
                    {
                        solids = new List<Solid>();
                        GeometryElement geometry = null;
                        try { geometry = host.get_Geometry(options); } catch { }
                        if (geometry != null)
                            foreach (GeometryObject shape in geometry)
                            {
                                var solid = shape as Solid;
                                if (solid != null && solid.Volume > 0) solids.Add(solid);
                            }
                        linkSolids[key] = solids;
                    }

                    foreach (var solid in solids)
                    {
                        SolidCurveIntersection crossing = null;
                        try { crossing = solid.IntersectWithCurve(local, intersectOptions); }
                        catch { continue; }
                        if (crossing == null || crossing.SegmentCount == 0) continue;

                        for (var k = 0; k < crossing.SegmentCount; k++)
                        {
                            Curve segment = null;
                            try { segment = crossing.GetCurveSegment(k); } catch { continue; }
                            if (segment == null) continue;

                            // Back into THIS model's coordinates for the centre.
                            var midpoint = placed.OfPoint(segment.Evaluate(0.5, true));
                            linkCount++;
                            linkedMatches.Add(string.Format("  {0} - {1} - service {2} - opening {3} x {4} "
                                + "mm{5}, through {6} mm, centred at {7}, {8}, {9} mm in this model",
                                linked.Title, describeLinked(linked, host), curve.Id,
                                Math.Round(widthFt * mmPerFoot + allowanceMm * 2.0),
                                Math.Round(heightFt * mmPerFoot + allowanceMm * 2.0),
                                round ? " (round service, squared off)" : "",
                                Math.Round(segment.Length * mmPerFoot),
                                Math.Round(midpoint.X * mmPerFoot), Math.Round(midpoint.Y * mmPerFoot),
                                Math.Round(midpoint.Z * mmPerFoot)));
                        }
                    }
                }
            }
        }
        linkedTotal += linkCount;
        linkedMatches.Insert(summaryAt, string.Format("{0}: {1} opening(s) proposed through its walls, "
            + "floors, roofs, ceilings and structure", linked.Title, linkCount));
    }

    findings.Add(proposals.Count + " opening(s) proposed across " + elements.Count + " service(s) "
        + "and " + hostCount + " host(s), each sized from the service plus " + allowanceMm
        + " mm all round.");

    findings.Add("NOTHING WAS CREATED. This is a proposal to read and argue with. Cutting the "
        + "holes is a separate operation somebody has to approve.");
}

if (unmeasured.Count > 0)
{
    findings.Add(unmeasured.Count + " item(s) could not be measured and are NOT in the proposals: "
        + string.Join("; ", unmeasured.ToArray())
        + ". They are reported apart, because a hole schedule that silently omits what it could not "
        + "read is one that gets built from.");
}

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} opening(s) proposed through linked hosts, NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("What a link holds is reported here as text only - nothing from a link "
        + "is carried to the next step, which would look it up in this model");
