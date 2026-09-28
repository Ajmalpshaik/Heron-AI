// NOT STANDALONE. Assumes `doc`, `target`, `categories` and `includeLinks` are
// in scope; leaves `elements`, `findings`, `linksSearched` and `linkedMatches`
// behind.
//
// OVERLAPPING IS NOT MEETING, AND FOR MEP THAT DECIDES THE ANSWER. Two ducts
// joined by an elbow at a shared connector are touching face to face and do NOT
// occupy the same space, so this returns nothing for them - run against exactly
// that pair, across every category, and it found nothing. The test is
// VOLUMETRIC OVERLAP and a clean connection has none.
//
// SO AN EMPTY ANSWER MEANS NO CLASH, NOT NO RELATIONSHIP, and the report says
// so - a bare zero here reads as a broken filter or, worse, as a clear model.
//
// THE TARGET IS NEVER RETURNED. It overlaps its own geometry perfectly, and
// including it puts a false pair in every clash list built on this.
//
// LINKS ARE READ ONLY WHEN ASKED FOR, AND WHAT OVERLAPS THERE IS NAMED, NEVER
// SELECTED - D-59. "What does this duct run through" is usually answered by
// the architect's walls and the structural slabs, which are links. With
// `includeLinks` set, the target's solids are carried into each placement's
// own coordinates and tested against that link's elements, and every overlap
// is reported as TEXT in `linkedMatches` - its type, its id IN THE LINK and its
// Fire Rating, which is what a penetration through a linked wall has to
// answer. They never enter `elements`: that list feeds the next fragment, and
// the chain revives ids against the HOST document, so a linked id carried there
// binds an unrelated element silently (FRAGMENT-ISSUES row 75).
//
// IN A LINK THE CHEAP TEST GOES FIRST. The target's box, carried into the link,
// narrows the set before any solid is opened - the slow-filter trap noted
// below the routing is not repeated for the links. NESTED LINKS ARE NOT READ,
// AND THE ANSWER COUNTS THEM. Only model elements are read, never a link's
// views or sheets.

var elements = new List<Element>();
var findings = new List<string>();

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
}


// Fire Rating by the name Revit's own walls, floors and doors carry it under,
// on the element first and then its type. Read as the palette shows it. A
// family that calls it something else reads "not set" - said, never guessed.
Func<Element, string> fireRatingOf = element =>
{
    Element type = null;
    try { type = element.Document.GetElement(element.GetTypeId()); } catch (Exception) { }
    foreach (var source in new[] { element, type })
    {
        if (source == null) continue;
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

if (target == null)
{
    findings.Add("No element was given - name the one everything else is tested against");
}
else
{
    var targetId = target.Id;
    var targetName = string.IsNullOrEmpty(target.Name) ? "that element" : target.Name;

    try
    {
        var collector = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .WherePasses(new ElementIntersectsElementFilter(target));

        if (categories != null && categories.Count > 0)
            collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

        foreach (var element in collector)
        {
            if (element.Id == targetId) continue;   // it always overlaps itself
            elements.Add(element);
        }

        findings.Add(string.Format("{0} element(s) physically overlap '{1}' (id {2}){3}",
            elements.Count, targetName, targetId,
            categories == null || categories.Count == 0
                ? ", across every category"
                : string.Format(", within {0} category/categories", categories.Count)));

        if (elements.Count == 0)
            findings.Add("Nothing OVERLAPS it. That is not the same as nothing being related to it: "
                + "two runs joined at a connector meet face to face and share no volume, so a "
                + "correctly connected duct returns zero here. TRACE_CONNECTIVITY answers "
                + "'what is connected to this'");
    }
    catch (Exception ex)
    {
        findings.Add(string.Format("'{0}' could not be tested for overlap: {1}. An element with no "
            + "solid geometry has nothing to intersect", targetName, ex.Message));
    }
}

// ---- D-59: the same overlap, in each link ----------------------------------

if (target != null && linkDocs.Count > 0)
{
    // The target's solids, one GeometryInstance deep - a family's solids sit
    // inside its instance. Unrolled rather than recursed on purpose.
    var targetSolids = new List<Solid>();
    GeometryElement geometry = null;
    try { geometry = target.get_Geometry(new Options()); } catch (Exception) { }
    if (geometry != null)
    {
        foreach (var item in geometry)
        {
            var solid = item as Solid;
            if (solid != null && solid.Volume > 0) { targetSolids.Add(solid); continue; }
            var nested = item as GeometryInstance;
            if (nested == null) continue;
            GeometryElement inner = null;
            try { inner = nested.GetInstanceGeometry(); } catch (Exception) { }
            if (inner == null) continue;
            foreach (var innerItem in inner)
            {
                var innerSolid = innerItem as Solid;
                if (innerSolid != null && innerSolid.Volume > 0) targetSolids.Add(innerSolid);
            }
        }
    }

    BoundingBoxXYZ targetBox = null;
    try { targetBox = target.get_BoundingBox(null); } catch (Exception) { }

    if (targetSolids.Count == 0 || targetBox == null)
    {
        linkBlocked = "Links NOT read: the element given has no solid to test against them";
    }
    else
    {
        for (var i = 0; i < linkDocs.Count; i++)
        {
            var linked = linkDocs[i];
            var hits = new List<Element>();
            var seen = new HashSet<ElementId>();

            foreach (var placement in linkPlacements[i])
            {
                try
                {
                    var inverse = placement.GetTotalTransform().Inverse;

                    // The target's box in the link's space, from all eight corners.
                    var low = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
                    var high = new XYZ(double.MinValue, double.MinValue, double.MinValue);
                    foreach (var x in new[] { targetBox.Min.X, targetBox.Max.X })
                        foreach (var y in new[] { targetBox.Min.Y, targetBox.Max.Y })
                            foreach (var z in new[] { targetBox.Min.Z, targetBox.Max.Z })
                            {
                                var corner = inverse.OfPoint(new XYZ(x, y, z));
                                low = new XYZ(Math.Min(low.X, corner.X), Math.Min(low.Y, corner.Y),
                                    Math.Min(low.Z, corner.Z));
                                high = new XYZ(Math.Max(high.X, corner.X), Math.Max(high.Y, corner.Y),
                                    Math.Max(high.Z, corner.Z));
                            }

                    foreach (var solid in targetSolids)
                    {
                        var moved = SolidUtils.CreateTransformed(solid, inverse);
                        var collector = new FilteredElementCollector(linked)
                            .WhereElementIsNotElementType()
                            .WherePasses(new BoundingBoxIntersectsFilter(new Outline(low, high)));
                        if (categories != null && categories.Count > 0)
                            collector = collector.WherePasses(new ElementMulticategoryFilter(categories));
                        foreach (var element in collector.WherePasses(new ElementIntersectsSolidFilter(moved)))
                            if (element != null && seen.Add(element.Id)) hits.Add(element);
                    }
                }
                catch (Exception) { }
            }

            var byCategory = new Dictionary<string, int>();
            foreach (var hit in hits)
            {
                var name = hit.Category == null ? "(no category)" : hit.Category.Name;
                byCategory[name] = byCategory.ContainsKey(name) ? byCategory[name] + 1 : 1;
            }
            var parts = new List<string>();
            foreach (var entry in byCategory) parts.Add(entry.Key + " " + entry.Value);

            linksSearched++;
            linkedTotal += hits.Count;
            linkedMatches.Add(string.Format("{0}: {1} overlap{2}", linked.Title, hits.Count,
                parts.Count > 0 ? " (" + string.Join(", ", parts.ToArray()) + ")" : ""));

            // Each one named, up to twenty a link - the wall a duct runs through
            // is the answer, not the count of it.
            for (var h = 0; h < hits.Count && h < 20; h++)
                linkedMatches.Add("  " + linked.Title + " - " + describeLinked(linked, hits[h]));
            if (hits.Count > 20)
                linkedMatches.Add(string.Format("  {0} - and {1} more not named", linked.Title,
                    hits.Count - 20));
        }
    }
}
else if (target == null && linkDocs.Count > 0)
{
    linkBlocked = "Links NOT read: no element was given to test against them";
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
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} match(es), NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("Linked elements are named here, never selected - the next step would "
        + "look them up in this model and could bind the wrong element");
