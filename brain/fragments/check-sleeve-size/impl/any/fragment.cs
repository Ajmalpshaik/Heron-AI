// NOT STANDALONE. Assumes `doc`, `sleeves`, `annularClearance`, `maxOversize`
// and `includeLinks` are in scope; leaves `findings`, `undersized`, `orphaned`,
// `unreadable`, `linksSearched` and `linkedMatches` behind.
//
// READ ONLY. Opens no transaction and needs none. Sizes are internal FEET.
//
// THE SERVICE IS FOUND BY GEOMETRY, NOT BY A PARAMETER. A sleeve family almost
// never records what goes through it - that link is stored nowhere - so the
// pairing is made by asking which service occupies the same space.
//
// A SLEEVE WITH NOTHING THROUGH IT IS A FINDING. It is an orphan left by a run
// that moved, or a run moved and the hole did not. Omitting it makes both
// invisible.
//
// REQUIRED = SERVICE SIZE + 2 x INSULATION + 2 x ANNULAR CLEARANCE. Insulation
// is read from the real insulation element rather than assumed: a 50 mm jacket
// turns a comfortable sleeve into a tight one.
//
// SLEEVE SIZE IS READ BY PARAMETER NAME AND FAMILIES DO NOT AGREE ON NAMES. The
// usual ones are tried and the one that answered is NAMED per row, so a family
// calling it something else reads as unreadable rather than as passing.
//
// A NAME TWO PARAMETERS SHARE IS NOT READ. A shared or project parameter can be
// bound beside a family's own one of the same name, and asked by name Revit
// then returns "the first one encountered", which its own reference says "is
// determined at random". So every name - a sleeve size, a service size, a
// linked wall's Fire Rating - is counted before it is read, and where the
// lookup would land on two the sleeve is unreadable, or the rating NOT READ,
// with the name said. Never the next name in the list instead: that would be
// choosing (D-54 s3, FRAGMENT-ISSUES 5b-203).
//
// WHAT THE SLEEVE GOES THROUGH IS READ FROM THE LINKS ONLY WHEN ASKED FOR -
// D-59. On a coordination job the wall or slab a sleeve crosses is usually the
// architect's or the structural engineer's, in a link, and its Fire Rating
// decides the fire-stopping - which is why an oversized sleeve matters. With
// `includeLinks` set, each sleeve's centre is looked for inside the linked
// walls, floors, roofs, ceilings and structural members, in each placement's
// own coordinates, and what it sits in is reported as TEXT in `linkedMatches`.
// `undersized` and `orphaned` stay this model's own: the chain revives ids
// against the HOST document, so a linked id carried there binds an unrelated
// element silently (FRAGMENT-ISSUES row 75). "Sits inside" is a box test, so a
// sleeve near a corner can name both walls - every candidate is listed.
// NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM. Only model elements
// are read from a link, never its views or sheets.

// MILLIMETRES IN, FEET INSIDE (D-71). Every length a caller types is
// millimetres. The add-in converts an XYZ at the boundary and cannot convert a
// bare double - nothing in a contract says which doubles are lengths - so the
// conversion belongs here, once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
annularClearance = annularClearance / MillimetresPerFoot;
maxOversize = maxOversize / MillimetresPerFoot;

var findings = new List<string>();
var undersized = new List<ElementId>();
var orphaned = new List<ElementId>();
var unreadable = 0;

var SIZE_NAMES = new[] { "Diameter", "Sleeve Diameter", "Nominal Diameter", "Opening Diameter",
                         "Width", "Sleeve Width", "Opening Width", "Height", "Sleeve Height" };
var SERVICE_CATEGORIES = new[] { BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_DuctCurves,
                                 BuiltInCategory.OST_CableTray, BuiltInCategory.OST_Conduit };

Func<Element, string, double> readNamed = (element, name) =>
{
    var parameter = element.LookupParameter(name);
    if (parameter == null || !parameter.HasValue || parameter.StorageType != StorageType.Double) return -1;
    return parameter.AsDouble();
};

// Two parameters by this name on this element - asked BEFORE readNamed, so a
// shared name is never read at all. See the header.
Func<Element, string, bool> nameIsShared = (element, name) =>
    element.GetParameters(name).Count > 1;

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

// What an opening can be cut through. Model elements only - never a view.
var LINKED_HOST_CATEGORIES = new List<BuiltInCategory> { BuiltInCategory.OST_Walls,
    BuiltInCategory.OST_Floors, BuiltInCategory.OST_Roofs, BuiltInCategory.OST_Ceilings,
    BuiltInCategory.OST_StructuralFraming, BuiltInCategory.OST_StructuralColumns };

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

// Every linked host element whose box holds this point, as text: the link, the
// category, 'Family: Type', its id IN THE LINK, and its Fire Rating.
Func<XYZ, List<string>> linkedHostsAt = point =>
{
    var hits = new List<string>();
    for (var i = 0; i < linkDocs.Count; i++)
    {
        var seen = new HashSet<ElementId>();
        foreach (var placement in linkPlacements[i])
        {
            try
            {
                var local = placement.GetTotalTransform().Inverse.OfPoint(point);
                foreach (var element in new FilteredElementCollector(linkDocs[i])
                             .WhereElementIsNotElementType()
                             .WherePasses(new ElementMulticategoryFilter(LINKED_HOST_CATEGORIES))
                             .WherePasses(new BoundingBoxContainsPointFilter(local)))
                {
                    if (element == null || !seen.Add(element.Id)) continue;
                    var typeName = "";
                    try
                    {
                        var type = linkDocs[i].GetElement(element.GetTypeId()) as ElementType;
                        if (type != null)
                            typeName = string.IsNullOrEmpty(type.FamilyName)
                                ? type.Name : type.FamilyName + ": " + type.Name;
                    }
                    catch (Exception) { }
                    hits.Add(string.Format("{0} - {1} '{2}' (id {3} in the link), Fire Rating {4}",
                        linkDocs[i].Title,
                        element.Category == null ? "element" : element.Category.Name,
                        typeName, element.Id, fireRatingOf(element)));
                }
            }
            catch (Exception) { }
        }
    }
    return hits;
};

// The services, collected once. Doing it per sleeve is the same query run
// hundreds of times.
var services = new List<Element>();
foreach (var category in SERVICE_CATEGORIES)
{
    foreach (var element in new FilteredElementCollector(doc)
        .OfCategory(category).WhereElementIsNotElementType())
    {
        services.Add(element);
    }
}

foreach (var sleeve in sleeves)
{
    if (sleeve == null || !sleeve.IsValidObject) continue;

    var sleeveBox = sleeve.get_BoundingBox(null);
    if (sleeveBox == null)
    {
        unreadable++;
        findings.Add(string.Format("sleeve {0}: no readable geometry - not checked", sleeve.Id));
        continue;
    }

    // What it passes through, in the links. Text only - see the header.
    if (linkDocs.Count > 0)
    {
        var inLinks = linkedHostsAt((sleeveBox.Min + sleeveBox.Max) / 2.0);
        if (inLinks.Count > 0) linkedTotal++;
        linkedMatches.Add(inLinks.Count > 0
            ? string.Format("sleeve {0}: through {1}", sleeve.Id, string.Join("; ", inLinks))
            : string.Format("sleeve {0}: inside nothing in any link", sleeve.Id));
    }

    // Which service passes through. The box test is the whole test here and it
    // is stated rather than dressed up: a sleeve is a short element and a
    // service crossing its box is, in practice, through it. A service running
    // past in the next bay shares no box with a sleeve this small.
    Element through = null;
    foreach (var service in services)
    {
        var serviceBox = service.get_BoundingBox(null);
        if (serviceBox == null) continue;
        if (serviceBox.Max.X < sleeveBox.Min.X || serviceBox.Min.X > sleeveBox.Max.X) continue;
        if (serviceBox.Max.Y < sleeveBox.Min.Y || serviceBox.Min.Y > sleeveBox.Max.Y) continue;
        if (serviceBox.Max.Z < sleeveBox.Min.Z || serviceBox.Min.Z > sleeveBox.Max.Z) continue;
        through = service;
        break;
    }

    if (through == null)
    {
        orphaned.Add(sleeve.Id);
        findings.Add(string.Format("sleeve {0}: NOTHING passes through it. Either an orphan left when a "
            + "run moved, or the run moved and the hole did not", sleeve.Id));
        continue;
    }

    // The sleeve's own size, by whichever name its family used.
    var sleeveSize = -1.0;
    var sizeNameUsed = "";
    var sizeNameShared = "";
    foreach (var name in SIZE_NAMES)
    {
        if (nameIsShared(sleeve, name)) { sizeNameShared = name; break; }
        var value = readNamed(sleeve, name);
        if (value > 0) { sleeveSize = value; sizeNameUsed = name; break; }
    }

    if (sizeNameShared.Length > 0)
    {
        unreadable++;
        findings.Add(string.Format("sleeve {0}: TWO OR MORE parameters are named '{1}' - asked by name, "
            + "Revit picks one of them at random, so its size was NOT read and it is not checked. Select "
            + "it and look in Properties to see both", sleeve.Id, sizeNameShared));
        continue;
    }

    if (sleeveSize <= 0)
    {
        unreadable++;
        findings.Add(string.Format("sleeve {0}: no size parameter this recognises. Tried {1}. Add the "
            + "family's own name to the list rather than reading this as a pass",
            sleeve.Id, string.Join(", ", SIZE_NAMES)));
        continue;
    }

    // The service's size, and its real insulation. Each name is counted before
    // it is read, in the order it is tried - see the header.
    var serviceSize = -1.0;
    var serviceNameShared = "";
    foreach (var name in new[] { "Outside Diameter", "Diameter", "Width" })
    {
        if (nameIsShared(through, name)) { serviceNameShared = name; break; }
        serviceSize = readNamed(through, name);
        if (serviceSize > 0) break;
    }

    if (serviceNameShared.Length > 0)
    {
        unreadable++;
        findings.Add(string.Format("sleeve {0}: the {1} through it ({2}) carries TWO OR MORE parameters "
            + "named '{3}' - asked by name, Revit picks one of them at random, so its size was NOT read "
            + "and the sleeve is not checked", sleeve.Id,
            through.Category == null ? "service" : through.Category.Name, through.Id, serviceNameShared));
        continue;
    }

    if (serviceSize <= 0)
    {
        var serviceBox = through.get_BoundingBox(null);
        if (serviceBox != null)
        {
            var dx = serviceBox.Max.X - serviceBox.Min.X;
            var dy = serviceBox.Max.Y - serviceBox.Min.Y;
            var dz = serviceBox.Max.Z - serviceBox.Min.Z;
            // The two SMALLEST extents are the cross-section; the largest is the
            // run's length and is not a size.
            var sorted = new List<double> { dx, dy, dz };
            sorted.Sort();
            serviceSize = sorted[1];
        }
    }

    var insulation = 0.0;
    try
    {
        var insulationIds = InsulationLiningBase.GetInsulationIds(doc, through.Id);
        foreach (var id in insulationIds)
        {
            var wrap = doc.GetElement(id) as InsulationLiningBase;
            if (wrap != null && wrap.Thickness > insulation) insulation = wrap.Thickness;
        }
    }
    catch { }   // an element that cannot carry a wrap throws, and that IS the filter

    var required = serviceSize + (2 * insulation) + (2 * annularClearance);
    var slack = sleeveSize - required;

    var describe = string.Format("sleeve {0} ({1} = {2:0.#} mm) round {3} {4:0.#} mm{5}: needs {6:0.#} mm",
        sleeve.Id, sizeNameUsed, sleeveSize * 304.8,
        through.Category == null ? "service" : through.Category.Name,
        serviceSize * 304.8,
        insulation > 0 ? string.Format(" + {0:0.#} mm insulation", insulation * 304.8) : " (no insulation)",
        required * 304.8);

    if (slack < 0)
    {
        undersized.Add(sleeve.Id);
        findings.Add(describe + string.Format("  - UNDERSIZED by {0:0.#} mm", -slack * 304.8));
    }
    else if (slack > maxOversize)
    {
        findings.Add(describe + string.Format("  - OVERSIZED by {0:0.#} mm. Costs fire-stopping and "
            + "gets queried on site", slack * 304.8));
    }
    else
    {
        findings.Add(describe + string.Format("  - ok, {0:0.#} mm spare", slack * 304.8));
    }
}

findings.Insert(0, string.Format("{0} sleeve(s) checked: {1} undersized, {2} with nothing through them, "
    + "{3} unreadable", sleeves.Count, undersized.Count, orphaned.Count, unreadable));

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} of {2} sleeve(s) pass through a "
        + "linked wall, floor, roof, ceiling or structural member, NOT selected",
        linksSearched, linkedTotal, sleeves.Count));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("Linked elements are named here, never selected - the next step would "
        + "look them up in this model and could bind the wrong element");
