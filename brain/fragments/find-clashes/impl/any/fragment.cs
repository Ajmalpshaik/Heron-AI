// NOT STANDALONE. Assumes `doc`, `elements`, `against` and `includeLinks` are in
// scope; leaves `clashes`, `clashing`, `clear`, `notChecked`, `findings`,
// `linksSearched` and `linkedMatches` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// REVIT'S OWN SOLID INTERSECTION, NOT BOUNDING BOXES. A bounding box around a
// diagonal duct overlaps everything in the rectangle it spans, and a clash
// report built that way runs to hundreds of rows that are not clashes - which
// is how a coordination report stops being read. `ElementIntersectsElementFilter`
// tests the real geometry.
//
// ONE COLLECTOR PASS PER ELEMENT, over the OTHER SET ONLY. The collector is
// constructed from `against` rather than from the whole document, so the work
// is |elements| x |against| and not |elements| x |model|. On a real model that
// is the difference between a check somebody runs and one they do not.
//
// ===========================================================================
// A TEST THAT COULD NOT BE RUN IS NOT A CLEAR RESULT.
// ===========================================================================
//
// Revit's filter THROWS for some elements rather than returning nothing - a
// view, a group, an element whose geometry is not generated in the active
// context. Until 2026-09-23 those were counted CLEAR, and this header said that
// was true because "nothing was found to intersect them". Nothing was LOOKED
// FOR. A clash report that calls an element clear when it never tested it is
// the one line a coordination meeting acts on without checking - and the same
// trap was observed elsewhere, in another tool's clash check.
//
// So they are still stepped over - a batch of two hundred must not end at the
// eleventh - but they are named in `notChecked`, with Revit's reason, and
// clashing + clear + notChecked is the number handed in.
//
// NOTHING TO CHECK AGAINST IS NOT CLEAR EITHER. An empty `against` used to count
// every element clear. It now counts none, and says why.
//
// TOUCHING COUNTS AS INTERSECTING and that is left alone deliberately. A duct
// resting exactly on a beam soffit is a hit geometrically and is usually fine
// on site. No tolerance is invented here, because a number invented in a
// fragment is a number quietly relied on everywhere afterwards.
//
// THE STRUCTURE IS USUALLY A LINK, AND IT IS READ ONLY WHEN ASKED FOR - D-59.
// With `includeLinks` set, each element's solids are carried into every
// placement of every loaded link and tested against that link's walls, floors,
// roofs, ceilings, columns, beams, foundations and stairs - the building, not
// another trade's MEP - with the same solid test behind a box filter. Every hit
// is TEXT in `linkedMatches`: the service, and the linked element's type, id
// IN THE LINK and Fire Rating. `clashes`, `clashing`, `clear` and `notChecked`
// stay this model's own, so the host verdict reads exactly as its proof: the
// chain revives ids against the HOST document, and a linked id carried there
// binds an unrelated element silently (FRAGMENT-ISSUES row 75). An element
// with no solid to carry is counted as not tested against the links, never as
// clear of them. NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM. Only
// model elements are read from a link, never its views or sheets.

var clashes = new Dictionary<ElementId, ICollection<ElementId>>();
var clashing = 0;
var clear = 0;
var notChecked = new List<ElementId>();
var findings = new List<string>();

// Why an element could not be tested, counted by reason, so two hundred
// elements of one kind Revit refuses are one sentence and not two hundred.
var whyNot = new Dictionary<string, int>();
var handed = 0;

if (against == null || against.Count == 0)
{
    foreach (var element in elements) if (element != null) handed++;
    findings.Add(string.Format("Nothing was given to check against, so NOTHING WAS CHECKED - none of "
        + "the {0} element(s) is clear and none clashes. Hand in the second set.", handed));
}
else
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        if (clashes.ContainsKey(element.Id)) continue;
        handed++;

        List<ElementId> hit;
        try
        {
            var filter = new ElementIntersectsElementFilter(element);
            hit = new FilteredElementCollector(doc, against)
                .WherePasses(filter)
                .ToElementIds()
                .ToList();
        }
        catch (Exception ex)
        {
            // Not tested, so neither clear nor clashing. See the header.
            notChecked.Add(element.Id);
            var reason = string.IsNullOrEmpty(ex.Message) ? ex.GetType().Name : ex.Message;
            var times = 0;
            whyNot.TryGetValue(reason, out times);
            whyNot[reason] = times + 1;
            continue;
        }

        // An element is in both sets on an overlapping request. Intersecting
        // itself is not a clash, and reporting it would put a row in every
        // coordination report that nobody can act on.
        hit.Remove(element.Id);

        if (hit.Count == 0)
        {
            clear++;
            continue;
        }

        clashes[element.Id] = hit;
        clashing++;
    }

    findings.Add(string.Format("{0} element(s) checked against {1}: {2} clash, {3} clear, {4} could not "
        + "be tested", handed, against.Count, clashing, clear, notChecked.Count));

    foreach (var why in whyNot)
    {
        findings.Add(string.Format("{0} element(s) NOT CHECKED - Revit's intersection test refused them: "
            + "{1}. They are in `notChecked` and are NOT counted clear", why.Value, why.Key));
    }
}

// ---- D-59: the same elements against each link's building ------------------

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

var buildingCategories = new List<BuiltInCategory> { BuiltInCategory.OST_Walls,
    BuiltInCategory.OST_Floors, BuiltInCategory.OST_Roofs, BuiltInCategory.OST_Ceilings,
    BuiltInCategory.OST_Columns, BuiltInCategory.OST_StructuralColumns,
    BuiltInCategory.OST_StructuralFraming, BuiltInCategory.OST_StructuralFoundation,
    BuiltInCategory.OST_Stairs };

// Each element's solids, one GeometryInstance deep, read once for every link.
var solidsOf = new Dictionary<ElementId, List<Solid>>();
var linkUntested = 0;
foreach (var element in linkDocs.Count > 0 ? elements : new List<Element>())
{
    if (element == null || solidsOf.ContainsKey(element.Id)) continue;
    var solids = new List<Solid>();
    GeometryElement geometry = null;
    try { geometry = element.get_Geometry(new Options()); } catch (Exception) { }
    if (geometry != null)
    {
        foreach (var item in geometry)
        {
            var solid = item as Solid;
            if (solid != null && solid.Volume > 0) { solids.Add(solid); continue; }
            var nested = item as GeometryInstance;
            if (nested == null) continue;
            GeometryElement inner = null;
            try { inner = nested.GetInstanceGeometry(); } catch (Exception) { }
            if (inner == null) continue;
            foreach (var innerItem in inner)
            {
                var innerSolid = innerItem as Solid;
                if (innerSolid != null && innerSolid.Volume > 0) solids.Add(innerSolid);
            }
        }
    }
    if (solids.Count == 0) linkUntested++;
    solidsOf[element.Id] = solids;
}

for (var i = 0; i < linkDocs.Count; i++)
{
    var linked = linkDocs[i];
    var summaryAt = linkedMatches.Count;
    var hitElements = 0;
    var hits = 0;

    foreach (var pair in solidsOf)
    {
        var seen = new HashSet<ElementId>();
        var named = new List<string>();
        foreach (var placement in linkPlacements[i])
        {
            try
            {
                var inverse = placement.GetTotalTransform().Inverse;
                foreach (var solid in pair.Value)
                {
                    var moved = SolidUtils.CreateTransformed(solid, inverse);
                    var box = moved.GetBoundingBox();
                    var min = box.Transform.OfPoint(box.Min);
                    var max = box.Transform.OfPoint(box.Max);
                    var outline = new Outline(
                        new XYZ(Math.Min(min.X, max.X), Math.Min(min.Y, max.Y), Math.Min(min.Z, max.Z)),
                        new XYZ(Math.Max(min.X, max.X), Math.Max(min.Y, max.Y), Math.Max(min.Z, max.Z)));
                    foreach (var found in new FilteredElementCollector(linked)
                                 .WhereElementIsNotElementType()
                                 .WherePasses(new ElementMulticategoryFilter(buildingCategories))
                                 .WherePasses(new BoundingBoxIntersectsFilter(outline))
                                 .WherePasses(new ElementIntersectsSolidFilter(moved)))
                    {
                        if (found != null && seen.Add(found.Id)) named.Add(describeLinked(linked, found));
                    }
                }
            }
            catch (Exception) { }
        }
        if (named.Count == 0) continue;
        hitElements++;
        hits += named.Count;
        if (linkedMatches.Count - summaryAt < 50)
            linkedMatches.Add(string.Format("  {0} - element {1} hits {2}", linked.Title, pair.Key,
                string.Join("; ", named.ToArray())));
    }

    linksSearched++;
    linkedTotal += hitElements;
    linkedMatches.Insert(summaryAt, string.Format("{0}: {1} of {2} element(s) clash with its walls, "
        + "floors, roofs, ceilings, columns, beams, foundations or stairs - {3} hit(s){4}",
        linked.Title, hitElements, solidsOf.Count, hits,
        hitElements > 50 ? "; the first 50 are named" : ""));
}

if (linksSearched > 0 && linkUntested > 0)
    linkedMatches.Add(string.Format("{0} element(s) have no solid to carry into a link and were NOT "
        + "tested against the links - they are not clear of them", linkUntested));

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} element(s) clash with linked building elements, NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("Linked elements are counted here, never selected - the next step would "
        + "look them up in this model and could bind the wrong element");
