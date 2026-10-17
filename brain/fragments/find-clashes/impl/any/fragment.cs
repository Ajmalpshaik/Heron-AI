// NOT STANDALONE. Assumes `doc`, `elements`, `against`, `includeLinks`,
// `categories`, `includeHost` and `compareParameter` are in scope; leaves
// `clashes`, `clashing`, `clear`, `notChecked`, `findings`, `linksSearched`,
// `linkedMatches`, `passedThrough`, `mismatched` and `comparison` behind.
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
// binds an unrelated element silently (FRAGMENT-ISSUES row 75). NESTED LINKS
// ARE NOT READ, AND THE ANSWER COUNTS THEM. Only model elements are read from a
// link, never its views or sheets.
//
// ===========================================================================
// VERSION 3: WHAT EACH ELEMENT PASSES THROUGH, AND A PARAMETER AGAINST IT.
// ===========================================================================
//
// Asked on a real project (FRAGMENT-ISSUES 5b-760): "check all the cut-out wall
// width". Eighteen opening families carried Wall Width 255 or 260, and what they
// actually passed through was a 30 mm wall in the structural link, a 305 mm deep
// louvre (an Air Terminal) in another, and damper actuators (Electrical
// Fixtures) - none of it a wall of 255. Version 2 could find the walls and
// nothing else, gave no thickness, and could not say which side of the opening
// the wall sat on. Three things are new, and with all three left out the
// answer is version 2's:
//
//   `categories` - "building" when left out: version 2's list. "all" is every
//   MODEL category except rooms, spaces, areas and link instances, which have a
//   volume and are not a thing anything passes through. Or names, as
//   Visibility/Graphics shows them - "Walls, Air Terminals" - or API names
//   (OST_Walls). A name this model does not know reads NOTHING, in the links or
//   here, and says so - a half-read answer to a misspelt list is the one that
//   gets believed.
//
//   `includeHost` - the same search in THIS model, the elements handed in left
//   out. `against` still decides `clashes` exactly as before; this only adds
//   lines to `passedThrough`. It exists because `against` can be filled only
//   with the selection or with nothing, so the open model was otherwise out of
//   reach for this question.
//
//   `compareParameter` - "none" when left out. A LENGTH on each element (its
//   instance first, then its type) - a Length, or anything Revit shows in a
//   length unit, as a duct's Width (a Duct Size) - compared with the thickness
//   of everything it passes through. Two parameters by that name read NOT READ
//   (D-54 s3, FRAGMENT-ISSUES 5b-203), and a parameter that is not a length is
//   named and not compared - a Number holding 255 is not 255 mm.
//
// THICKNESS IS THE THING'S OWN, NOT THE PART INSIDE THE ELEMENT. A wall is its
// Width, a floor, roof or ceiling its type's layers added up, and anything else
// its own depth along the element's facing (a family) or run (a duct, a beam).
// An element with neither has its depth left unmeasured and said so. When the
// element holds less of it than that - an opening shallower than the wall - the
// line says how much.
//
// THE SIDE IS WHERE THE OTHER ELEMENT'S OWN CENTRE SITS - a wall's
// centreline - measured from the element's centre along the wall's face
// normal, up for a floor, or along the element's own axis. In front of or
// behind is the element's own facing; a wall also says which of its faces that
// is toward. Where its own faces do not give its thickness - a curved wall, a
// sweep on it, a sloped floor - the centre of the part inside the element
// (cut out by Revit's own Boolean) is used, and the line says so.
//
// EQUAL IS REVIT'S OWN WORD FOR THE SAME POINT - `Application.VertexTolerance`
// - not a number chosen here. A hit with no thickness is not compared, an
// element that passes through nothing is not a match, and neither is counted as
// one.
//
// AN ELEMENT WITH NO SOLID IS TESTED BY ITS BOUNDING BOX, AND EVERY HIT IT GETS
// SAYS SO. Version 2 left those untested against the links. An opening family
// may be all void, and the probe that answered the real job tested each
// opening's box. A box is larger than a turned element, so a "(box)" hit is a
// candidate, not a clash. Only an element with neither is not tested.
//
// A TAG ON A LINKED ELEMENT IS NOT READ HERE. Reading a tag's TagText on a
// linked element threw "An internal error has occurred" on Revit 2020 while
// the real job was probed; anything added here that reads one wants a guard.

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

// 'Family: Type' as the palette shows it, read in the element's own document.
Func<Element, string> typeNameOf = element =>
{
    try
    {
        var type = element.Document.GetElement(element.GetTypeId()) as ElementType;
        if (type != null)
            return string.IsNullOrEmpty(type.FamilyName) ? type.Name : type.FamilyName + ": " + type.Name;
    }
    catch (Exception) { }
    try { return element.Name; } catch (Exception) { }
    return "";
};

// A linked element as text: the category, 'Family: Type', its id IN THE LINK
// (never this model's), and its Fire Rating. Version 2's words exactly - an
// element with no type reads '' here, where typeNameOf falls back to its name.
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

// ---- version 3: which categories are looked for -----------------------------

Func<string, string, bool> sameWord = (said, word) =>
    string.Equals((said ?? "").Trim(), word, StringComparison.OrdinalIgnoreCase);

var categoryWord = (categories ?? "").Trim();
var anyCategory = sameWord(categoryWord, "all") || sameWord(categoryWord, "any");
var buildingOnly = categoryWord.Length == 0 || sameWord(categoryWord, "building");
var lookFor = new List<ElementId>();
var lookForNames = new List<string>();
var unknownCategories = new List<string>();

// Volumes nothing passes through, left out of "all" - asked for by name they
// are read, because the modeller said so.
var notPassedThrough = new List<ElementId> { new ElementId(BuiltInCategory.OST_Rooms),
    new ElementId(BuiltInCategory.OST_MEPSpaces), new ElementId(BuiltInCategory.OST_Areas),
    new ElementId(BuiltInCategory.OST_RvtLinks) };

if (buildingOnly)
{
    foreach (var category in buildingCategories) lookFor.Add(new ElementId(category));
}
else if (!anyCategory)
{
    foreach (var part in categoryWord.Split(','))
    {
        var said = part.Trim();
        if (said.Length == 0) continue;

        // The name Visibility/Graphics shows, then an API name. A NUMBER IS
        // NEVER PARSED: Enum.Parse takes "-2000011" as a category, which is an
        // id built from an integer by another road.
        Category match = null;
        foreach (Category category in doc.Settings.Categories)
        {
            if (category == null) continue;
            string shown = null;
            try { shown = category.Name; } catch (Exception) { }
            if (string.Equals(shown, said, StringComparison.OrdinalIgnoreCase)) { match = category; break; }
        }
        if (match == null && said.StartsWith("OST_", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var builtIn = (BuiltInCategory)Enum.Parse(typeof(BuiltInCategory), said, true);
                match = Category.GetCategory(doc, builtIn);
            }
            catch (Exception) { match = null; }
        }

        if (match == null) { unknownCategories.Add(said); continue; }
        if (lookFor.Contains(match.Id)) continue;
        lookFor.Add(match.Id);
        lookForNames.Add(match.Name);
    }

    if (unknownCategories.Count > 0)
        linkBlocked = string.Format("NOTHING WAS LOOKED FOR, in the links or here - {0} not a category "
            + "this model knows. Name them as Visibility/Graphics shows them - Walls, Air Terminals - "
            + "separated by commas, or say all", string.Join(", ", unknownCategories.Select(u => "'" + u + "'")
            .ToArray()) + (unknownCategories.Count == 1 ? " is" : " are"));
    else if (lookFor.Count == 0)
        linkBlocked = "NOTHING WAS LOOKED FOR, in the links or here - no category was named. Name them "
            + "separated by commas, or say all";
}

var lookingFor = buildingOnly ? "walls, floors, roofs, ceilings, columns, beams, foundations or stairs"
    : anyCategory ? "elements of any model category" : string.Join(", ", lookForNames.ToArray());

// What a collector of one document keeps, by category. "all" is filtered here
// rather than by a category filter, because it is everything but a few.
Func<Element, bool> isLookedFor = found =>
{
    if (found == null || found.Category == null) return false;
    if (!anyCategory) return true;
    if (found.Category.CategoryType != CategoryType.Model) return false;
    return !notPassedThrough.Contains(found.Category.Id);
};

// ---- version 3: the geometry every search and measurement uses --------------

var mm = 304.8;
var vertexTolerance = 0.0;
try { vertexTolerance = doc.Application.VertexTolerance; } catch (Exception) { }

// An element's solids in its OWN document's coordinates, one GeometryInstance
// deep - the family instance case.
Func<Element, List<Solid>> solidsIn = element =>
{
    var solids = new List<Solid>();
    GeometryElement geometry = null;
    try { geometry = element.get_Geometry(new Options()); } catch (Exception) { }
    if (geometry == null) return solids;
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
    return solids;
};

// An element's bounding box as a solid, for an element with none of its own.
Func<Element, Solid> boxSolidOf = element =>
{
    BoundingBoxXYZ box = null;
    try { box = element.get_BoundingBox(null); } catch (Exception) { }
    if (box == null) return null;
    var low = box.Min;
    var high = box.Max;
    if (high.X - low.X <= vertexTolerance || high.Y - low.Y <= vertexTolerance
        || high.Z - low.Z <= vertexTolerance) return null;
    try
    {
        var loop = CurveLoop.Create(new List<Curve>
        {
            Line.CreateBound(new XYZ(low.X, low.Y, low.Z), new XYZ(high.X, low.Y, low.Z)),
            Line.CreateBound(new XYZ(high.X, low.Y, low.Z), new XYZ(high.X, high.Y, low.Z)),
            Line.CreateBound(new XYZ(high.X, high.Y, low.Z), new XYZ(low.X, high.Y, low.Z)),
            Line.CreateBound(new XYZ(low.X, high.Y, low.Z), new XYZ(low.X, low.Y, low.Z))
        });
        var made = GeometryCreationUtilities.CreateExtrusionGeometry(
            new List<CurveLoop> { loop }, XYZ.BasisZ, high.Z - low.Z);
        return box.Transform == null || box.Transform.IsIdentity
            ? made : SolidUtils.CreateTransformed(made, box.Transform);
    }
    catch (Exception) { return null; }
};

// The box around a solid as an Outline, for the quick filter in front of the
// slow one.
Func<Solid, Outline> outlineOf = solid =>
{
    var box = solid.GetBoundingBox();
    var min = box.Transform.OfPoint(box.Min);
    var max = box.Transform.OfPoint(box.Max);
    return new Outline(
        new XYZ(Math.Min(min.X, max.X), Math.Min(min.Y, max.Y), Math.Min(min.Z, max.Z)),
        new XYZ(Math.Max(min.X, max.X), Math.Max(min.Y, max.Y), Math.Max(min.Z, max.Z)));
};

// The centre of some solids, by volume. Null when Revit gives none.
Func<List<Solid>, XYZ> centreOf = solids =>
{
    var sum = XYZ.Zero;
    var volume = 0.0;
    foreach (var solid in solids)
    {
        try
        {
            var centroid = solid.ComputeCentroid();
            sum = sum + centroid * solid.Volume;
            volume += solid.Volume;
        }
        catch (Exception) { }
    }
    return volume > 0 ? sum / volume : null;
};

// How far some solids reach along a direction: their least and greatest
// position on it, read off every edge. Null when there are no edges.
Func<List<Solid>, XYZ, double[]> extentAlong = (solids, direction) =>
{
    var low = double.MaxValue;
    var high = double.MinValue;
    foreach (var solid in solids)
    {
        try
        {
            foreach (Edge edge in solid.Edges)
            {
                foreach (var point in edge.Tessellate())
                {
                    var along = point.DotProduct(direction);
                    if (along < low) low = along;
                    if (along > high) high = along;
                }
            }
        }
        catch (Exception) { }
    }
    return low <= high ? new[] { low, high } : null;
};

// The part of `other` inside `own`: Revit's own Boolean, piece by piece.
Func<List<Solid>, List<Solid>, List<Solid>> overlapOf = (own, other) =>
{
    var parts = new List<Solid>();
    foreach (var mine in own)
    {
        foreach (var theirs in other)
        {
            try
            {
                var part = BooleanOperationsUtils.ExecuteBooleanOperation(mine, theirs,
                    BooleanOperationsType.Intersect);
                if (part != null && part.Volume > 0) parts.Add(part);
            }
            catch (Exception) { }
        }
    }
    return parts;
};

// The element's own axis: a family's facing, a curve's run, or none.
Func<Element, XYZ> facingOf = element =>
{
    var instance = element as FamilyInstance;
    if (instance == null) return null;
    try
    {
        var facing = instance.FacingOrientation;
        return facing != null && facing.GetLength() > 0 ? facing.Normalize() : null;
    }
    catch (Exception) { return null; }
};
Func<Element, XYZ> runOf = element =>
{
    try
    {
        var curve = element.Location as LocationCurve;
        var line = curve == null ? null : curve.Curve as Line;
        return line == null ? null : line.Direction.Normalize();
    }
    catch (Exception) { return null; }
};

// What one element passes through, measured in the OTHER element's document:
// `own` is the element's solids already carried there, `centre` its centre,
// `facing` and `run` its axes. Returns the text, and the thickness in feet
// that the comparison reads - below zero when there is none.
Func<Element, List<Solid>, XYZ, XYZ, XYZ, Tuple<string, double>> measure =
    (other, own, centre, facing, run) =>
{
    var otherSolids = solidsIn(other);
    XYZ axis = null;
    var kind = "other";
    var thickness = -1.0;
    var thicknessText = "";

    var wall = other as Wall;
    if (wall != null)
    {
        kind = "wall";
        try { axis = wall.Orientation.Normalize(); } catch (Exception) { axis = null; }
        try { thickness = wall.Width; } catch (Exception) { thickness = -1.0; }
        thicknessText = thickness > 0 ? string.Format("wall {0:0} mm thick", thickness * mm)
            : "wall thickness NOT READ";
    }
    else if (other is CeilingAndFloor || other is RoofBase)
    {
        // A floor, roof or ceiling: its type's layers, measured up. NOT every
        // HostObject - a duct, a pipe, a tray, a face wall and a slab edge are
        // HostObjects too, and they are measured along the element below.
        kind = "slab";
        axis = XYZ.BasisZ;
        try
        {
            var type = other.Document.GetElement(other.GetTypeId()) as HostObjAttributes;
            var structure = type == null ? null : type.GetCompoundStructure();
            if (structure != null) thickness = structure.GetWidth();
        }
        catch (Exception) { thickness = -1.0; }
        thicknessText = thickness > 0 ? string.Format("{0:0} mm thick", thickness * mm)
            : "thickness NOT READ - its type has no layers";
    }
    else
    {
        axis = facing ?? run;
        var extent = axis == null ? null : extentAlong(otherSolids, axis);
        if (extent != null)
        {
            thickness = extent[1] - extent[0];
            thicknessText = string.Format("{0:0} mm deep along the element's {1}", thickness * mm,
                facing != null ? "facing" : "run");
        }
        else
        {
            thicknessText = axis == null ? "depth NOT MEASURED - the element has no facing and no run"
                : "depth NOT MEASURED - it has no solid";
        }
    }

    if (axis == null || centre == null) return Tuple.Create(thicknessText, thickness);

    // HOW MUCH OF IT IS INSIDE: the part inside the element, cut out by
    // Revit's own Boolean - an opening shallower than the wall says so.
    var inside = overlapOf(own, otherSolids);
    var held = "";
    var depth = inside.Count == 0 ? null : extentAlong(inside, axis);
    if (depth != null && thickness > 0 && thickness - (depth[1] - depth[0]) > vertexTolerance)
        held = string.Format(", only {0:0} mm of it inside the element", (depth[1] - depth[0]) * mm);

    // WHERE IT SITS: the other element's OWN centre along the axis - a wall's
    // centreline, wherever its faces fall against the element. Its own reach
    // along the axis is its two faces for a straight wall or a flat slab;
    // where that reach is not its thickness - a curved wall, a sweep on it, a
    // sloped floor - the centre of the part inside the element is taken
    // instead, and said.
    var reach = extentAlong(otherSolids, axis);
    var fromWhat = "";
    double? along = null;
    if (reach != null && (kind == "other"
        || (thickness > 0 && Math.Abs((reach[1] - reach[0]) - thickness) <= vertexTolerance)))
        along = (reach[0] + reach[1]) / 2.0;
    else
    {
        var middle = centreOf(inside);
        if (middle != null)
        {
            along = middle.DotProduct(axis);
            fromWhat = " (the centre of the part inside the element - its own faces do not give its thickness here)";
        }
    }
    if (along == null) return Tuple.Create(thicknessText + held + ", side NOT MEASURED", thickness);

    var offset = along.Value - centre.DotProduct(axis);
    var off = Math.Abs(offset) * mm;
    var side = "";
    var facingDot = facing == null ? 0.0 : axis.DotProduct(facing);
    if (Math.Round(off) == 0)
        side = "centre 0 mm off the element's centre";
    else if (Math.Abs(facingDot) > 1e-9)
        side = string.Format("centre {0:0} mm {1} the element's centre", off,
            offset * Math.Sign(facingDot) > 0 ? "in front of" : "behind");
    else if (kind == "slab")
        side = string.Format("centre {0:0} mm {1} the element's centre", off, offset > 0 ? "above" : "below");
    else if (kind != "wall")
        side = string.Format("centre {0:0} mm off the element's centre along its {1}", off,
            run != null ? "run" : "axis");
    if (kind == "wall" && Math.Round(off) != 0)
        side = (side.Length > 0 ? side + ", " : string.Format("centre {0:0} mm ", off))
            + string.Format("to the wall's {0} side of the element's centre", offset > 0 ? "exterior" : "interior");

    return Tuple.Create(thicknessText + held + ", " + side + fromWhat, thickness);
};

// ---- version 3: the parameter compared, read once per element ---------------

var parameterWord = (compareParameter ?? "").Trim();
var comparing = parameterWord.Length > 0 && !sameWord(parameterWord, "none");

// A LENGTH, asked of the running Revit: Definition.GetDataType against
// SpecTypeId.Length from 2022, Definition.ParameterType before it - OR SHOWN
// IN A LENGTH UNIT, Parameter.GetUnitTypeId from 2021 and DisplayUnitType
// before it. The second half is not optional: a duct's Width is a Duct Size,
// not a Length, and a pipe's diameter a Pipe Size, and both are millimetres.
// The add-in compiles fragments with no release symbols (FRAGMENT-ISSUES
// 5b-181), so an `#if` would take the same branch everywhere - every member
// here is reached by name instead.
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;
Func<object, object, bool> sameKind = (first, second) =>
{
    if (first == null || second == null) return false;
    var nameEquals = first.GetType().GetMethod("NameEquals", new[] { second.GetType() });
    if (nameEquals != null)
    {
        try { return (bool)nameEquals.Invoke(first, new[] { second }); }
        catch (Exception) { }
    }
    return first.Equals(second);
};
Func<Parameter, bool> isLengthSpec = p =>
{
    try
    {
        var definition = p.Definition;
        if (definition == null) return false;
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null)
        {
            var spec = getDataType.Invoke(definition, null);
            var owner = revitAssembly.GetType(dbNamespace + ".SpecTypeId");
            var property = owner == null ? null : owner.GetProperty("Length", flags);
            var length = property == null ? null : property.GetValue(null);
            return sameKind(spec, length);
        }
        var old = definition.GetType().GetProperty("ParameterType");
        var value = old == null ? null : old.GetValue(definition);
        return value != null && value.ToString() == "Length";
    }
    catch (Exception)
    {
        return false;
    }
};

// The length units, as the running Revit names them - each one absent on a
// release that has no such member simply is not in the list.
var lengthUnits = new List<object>();
var unitOwner = revitAssembly.GetType(dbNamespace + ".UnitTypeId");
foreach (var name in new[] { "Millimeters", "Centimeters", "Decimeters", "Meters", "MetersCentimeters",
    "Feet", "FeetFractionalInches", "Inches", "FractionalInches", "UsSurveyFeet" })
{
    var property = unitOwner == null ? null : unitOwner.GetProperty(name, flags);
    var unit = property == null ? null : property.GetValue(null);
    if (unit != null) lengthUnits.Add(unit);
}
var oldLengthUnits = new[] { "DUT_MILLIMETERS", "DUT_CENTIMETERS", "DUT_DECIMETERS", "DUT_METERS",
    "DUT_METERS_CENTIMETERS", "DUT_DECIMAL_FEET", "DUT_FEET_FRACTIONAL_INCHES", "DUT_DECIMAL_INCHES",
    "DUT_FRACTIONAL_INCHES" };
Func<Parameter, bool> shownAsLength = p =>
{
    try
    {
        var getUnit = p.GetType().GetMethod("GetUnitTypeId", System.Type.EmptyTypes);
        if (getUnit != null)
        {
            // Throws for a value with no unit - a Number - which is a no.
            var unit = getUnit.Invoke(p, null);
            return lengthUnits.Any(u => sameKind(unit, u));
        }
        var old = p.GetType().GetProperty("DisplayUnitType");
        var value = old == null ? null : old.GetValue(p);
        return value != null && oldLengthUnits.Contains(value.ToString());
    }
    catch (Exception)
    {
        return false;
    }
};
Func<Parameter, bool> isLength = p => isLengthSpec(p) || shownAsLength(p);

// The value in feet, or the reason there is none: on the instance, then its
// type, and never one of two parameters sharing the name (5b-203).
Func<Element, Tuple<double, string>> parameterOf = element =>
{
    Element type = null;
    try { type = doc.GetElement(element.GetTypeId()); } catch (Exception) { }
    foreach (var source in new[] { element, type })
    {
        if (source == null) continue;
        var sharing = 0;
        try { sharing = source.GetParameters(parameterWord).Count; } catch (Exception) { }
        if (sharing > 1) return Tuple.Create(-1.0, "NOT READ - " + sharing + " parameters share that name");
        Parameter parameter = null;
        try { parameter = source.LookupParameter(parameterWord); } catch (Exception) { }
        if (parameter == null) continue;
        if (parameter.StorageType != StorageType.Double || !isLength(parameter))
            return Tuple.Create(-1.0, "not a length");
        if (!parameter.HasValue) return Tuple.Create(-1.0, "no value");
        try { return Tuple.Create(parameter.AsDouble(), (string)null); }
        catch (Exception) { return Tuple.Create(-1.0, "no value"); }
    }
    return Tuple.Create(-1.0, "missing");
};

// ---- version 3: every element, through every place asked for ----------------

var passedThrough = new List<string>();
var mismatched = new List<ElementId>();
var comparison = new List<string>();

var handedIds = new HashSet<ElementId>();
foreach (var element in elements) if (element != null) handedIds.Add(element.Id);

var searchHost = includeHost && linkBlocked.Length == 0;
var searchLinks = linkDocs.Count > 0 && linkBlocked.Length == 0;
var lookFilter = anyCategory || lookFor.Count == 0 ? null : new ElementMulticategoryFilter(lookFor);

// Per link: how many elements hit it, how many hits, and version 2's lines.
var linkHitElements = new int[linkDocs.Count];
var linkHits = new int[linkDocs.Count];
var linkLines = new List<List<string>>();
for (var i = 0; i < linkDocs.Count; i++) linkLines.Add(new List<string>());

var linkUntested = 0;
var byBox = 0;
var through = 0;
var throughNothing = new List<ElementId>();
var notTested = new List<ElementId>();
var refusedAgainst = new List<ElementId>();
var elementLines = new List<string>();

// The comparison's tally.
var agree = 0;
var compareFailures = new Dictionary<string, List<ElementId>>();
var nothingToCompare = 0;
var hitsUnmeasured = 0;
var mismatchLines = new List<string>();

// Where this answer looked, said on its first line - `against` counts when it
// was handed in, whether or not anything clashed.
var againstGiven = against != null && against.Count > 0;
var places = new List<string>();
if (searchLinks) places.Add(linkDocs.Count + " link(s)");
if (againstGiven) places.Add("`against`");
if (searchHost) places.Add("this model");

var done = new HashSet<ElementId>();
foreach (var element in places.Count > 0 ? elements : new List<Element>())
{
    if (element == null || !done.Add(element.Id)) continue;

    var own = solidsIn(element);
    // A box only stands in where a solid is carried - `against`'s own test
    // reads the element's geometry itself.
    var boxed = false;
    if (own.Count == 0 && (searchLinks || searchHost))
    {
        var box = boxSolidOf(element);
        if (box != null) { own.Add(box); boxed = true; byBox++; }
    }

    // NOTHING TO CARRY, NOTHING LOOKED FOR WITH IT. `against`'s own test needs
    // no solid of ours, so its clashes are still listed; the links and this
    // model's search are not, and the element is NOT tested there.
    var carried = own.Count > 0;
    if (!carried && searchLinks) linkUntested++;
    var partly = !carried && (searchLinks || searchHost);
    // REVIT REFUSED ITS TEST AGAINST `against` - it is in `notChecked`, with
    // Revit's reason in `findings`. Whatever else it was tested against, it was
    // not tested there, so it never reads "through nothing".
    var refusedThere = againstGiven && notChecked.Contains(element.Id);
    var tested = !partly && !refusedThere
                 && (againstGiven || (carried && (searchLinks || searchHost)));

    var centre = carried ? centreOf(own) : null;
    var facing = facingOf(element);
    var run = runOf(element);
    var mark = boxed ? " (box)" : "";

    // Each hit: where, its words, and its thickness for the comparison.
    var hitTexts = new List<string>();
    var hitThickness = new List<Tuple<string, double>>();

    // THIS MODEL: `against`'s clashes first, then the search includeHost asks for.
    var hostSeen = new HashSet<ElementId>();
    var hostHits = new List<Element>();
    ICollection<ElementId> clashed;
    if (clashes.TryGetValue(element.Id, out clashed))
        foreach (var id in clashed) { var e = doc.GetElement(id); if (e != null && hostSeen.Add(id)) hostHits.Add(e); }
    if (searchHost && carried)
    {
        foreach (var solid in own)
        {
            try
            {
                var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
                if (lookFilter != null) collector = collector.WherePasses(lookFilter);
                foreach (var found in collector
                             .WherePasses(new BoundingBoxIntersectsFilter(outlineOf(solid)))
                             .WherePasses(new ElementIntersectsSolidFilter(solid)))
                {
                    if (!isLookedFor(found) || handedIds.Contains(found.Id)) continue;
                    if (hostSeen.Add(found.Id)) hostHits.Add(found);
                }
            }
            catch (Exception) { }
        }
    }
    foreach (var found in hostHits)
    {
        var measured = measure(found, own, centre, facing, run);
        var text = string.Format("this model: {0} '{1}' (id {2}), {3}{4}",
            found.Category == null ? "element" : found.Category.Name, typeNameOf(found), found.Id,
            measured.Item1, mark);
        hitTexts.Add(text);
        hitThickness.Add(Tuple.Create(text, measured.Item2));
    }

    // THE LINKS, each placement in its own coordinates.
    for (var i = 0; i < (searchLinks && carried ? linkDocs.Count : 0); i++)
    {
        var linked = linkDocs[i];
        var seen = new HashSet<ElementId>();
        var named = new List<string>();
        foreach (var placement in linkPlacements[i])
        {
            try
            {
                var inverse = placement.GetTotalTransform().Inverse;
                var moved = own.Select(s => SolidUtils.CreateTransformed(s, inverse)).ToList();
                var movedCentre = centre == null ? null : inverse.OfPoint(centre);
                var movedFacing = facing == null ? null : inverse.OfVector(facing);
                var movedRun = run == null ? null : inverse.OfVector(run);
                foreach (var solid in moved)
                {
                    var collector = new FilteredElementCollector(linked).WhereElementIsNotElementType();
                    if (lookFilter != null) collector = collector.WherePasses(lookFilter);
                    foreach (var found in collector
                                 .WherePasses(new BoundingBoxIntersectsFilter(outlineOf(solid)))
                                 .WherePasses(new ElementIntersectsSolidFilter(solid)))
                    {
                        if (!isLookedFor(found) || !seen.Add(found.Id)) continue;
                        named.Add(describeLinked(linked, found));
                        var measured = measure(found, moved, movedCentre, movedFacing, movedRun);
                        var text = string.Format("{0}: {1} '{2}' (id {3} in the link), {4}{5}", linked.Title,
                            found.Category == null ? "element" : found.Category.Name, typeNameOf(found),
                            found.Id, measured.Item1, mark);
                        hitTexts.Add(text);
                        hitThickness.Add(Tuple.Create(text, measured.Item2));
                    }
                }
            }
            catch (Exception) { }
        }
        if (named.Count == 0) continue;
        linkHitElements[i]++;
        linkHits[i] += named.Count;
        if (linkLines[i].Count < 50)
            linkLines[i].Add(string.Format("  {0} - element {1}{2} hits {3}", linked.Title, element.Id,
                boxed ? " (by its box - it has no solid)" : "", string.Join("; ", named.ToArray())));
    }

    // THE ELEMENT'S OWN LINE, with the parameter beside it when one is asked.
    Tuple<double, string> value = comparing ? parameterOf(element) : null;
    var valueText = value == null ? ""
        : value.Item2 == null ? string.Format(", {0} {1:0} mm", parameterWord, value.Item1 * mm)
        : string.Format(", {0} {1}", parameterWord, value.Item2);

    if (hitTexts.Count == 0)
    {
        if (tested) throughNothing.Add(element.Id);
        else if (refusedThere) refusedAgainst.Add(element.Id);
        else notTested.Add(element.Id);
    }
    else
    {
        through++;
        if (elementLines.Count < 100)
            elementLines.Add(string.Format("  element {0} '{1}'{2}: {3} - {4}{5}", element.Id,
                typeNameOf(element), valueText, hitTexts.Count, string.Join("; ", hitTexts.ToArray()),
                (partly ? " - it has no solid and no box, so the links and this model were NOT searched with it"
                : "") + (refusedThere ? " - and Revit refused its test against `against`" : "")));
    }

    // THE COMPARISON. A failure to read is counted by its reason, a hit with
    // no thickness is not compared, and nothing to compare is not a match. An
    // element NOT tested is counted as that and nothing else.
    if (!comparing || (hitTexts.Count == 0 && !tested)) continue;
    if (value.Item2 != null)
    {
        List<ElementId> failed;
        if (!compareFailures.TryGetValue(value.Item2, out failed))
            compareFailures[value.Item2] = failed = new List<ElementId>();
        failed.Add(element.Id);
        continue;
    }
    var measuredHits = hitThickness.Where(h => h.Item2 > 0).ToList();
    hitsUnmeasured += hitThickness.Count - measuredHits.Count;
    if (measuredHits.Count == 0) { nothingToCompare++; continue; }

    var differs = false;
    foreach (var hit in measuredHits)
    {
        var gap = value.Item1 - hit.Item2;
        if (Math.Abs(gap) <= vertexTolerance) continue;
        differs = true;
        if (mismatchLines.Count < 100)
            mismatchLines.Add(string.Format("  element {0} '{1}' {2} {3:0.#} mm - {4:0.#} mm {5} than {6}",
                element.Id, typeNameOf(element), parameterWord, value.Item1 * mm,
                Math.Abs(gap) * mm, gap > 0 ? "MORE" : "LESS", hit.Item1));
    }
    if (differs) mismatched.Add(element.Id);
    else agree++;
}

// ---- D-59: each link's account, in version 2's words -------------------------

for (var i = 0; i < (searchLinks ? linkDocs.Count : 0); i++)
{
    linksSearched++;
    linkedTotal += linkHitElements[i];
    linkedMatches.Add(string.Format("{0}: {1} of {2} element(s) clash with its {3} - {4} hit(s){5}",
        linkDocs[i].Title, linkHitElements[i], done.Count, lookingFor, linkHits[i],
        linkHitElements[i] > 50 ? "; the first 50 are named" : ""));
    linkedMatches.AddRange(linkLines[i]);
}

if (linksSearched > 0 && byBox > 0)
    linkedMatches.Add(string.Format("{0} element(s) have no solid and were tested against the links by "
        + "their bounding box - larger than the element where it is turned, so a hit on one is a "
        + "candidate, not a clash", byBox));
if (linksSearched > 0 && linkUntested > 0)
    linkedMatches.Add(string.Format("{0} element(s) have no solid and no box to carry into a link and "
        + "were NOT tested against the links - they are not clear of them", linkUntested));

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} element(s) clash with linked {2}, NOT selected",
        linksSearched, linkedTotal, buildingOnly ? "building elements" : lookingFor));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("Linked elements are counted here, never selected - the next step would "
        + "look them up in this model and could bind the wrong element");

// ---- version 3: what each element passes through -----------------------------

if (linkBlocked.Length > 0)
    passedThrough.Add(linkBlocked);
else if (places.Count == 0)
    passedThrough.Add("NOTHING WAS LOOKED IN - set includeLinks for the links or includeHost for this "
        + "model, or hand in `against`. Not one element passes through nothing; none was asked about");
else
{
    passedThrough.Add(string.Format("{0} element(s) looked at in {1}, for {2}: {3} pass through something, "
        + "{4} through nothing, {5} NOT tested{6}. Linked elements are text, never selected",
        done.Count, string.Join(" and ", places.ToArray()), lookingFor, through, throughNothing.Count,
        notTested.Count + refusedAgainst.Count, byBox > 0 ? string.Format("; {0} tested by their bounding box, their hits marked "
        + "(box)", byBox) : ""));
    passedThrough.AddRange(elementLines);
    if (through > elementLines.Count)
        passedThrough.Add(string.Format("  the first {0} are named; {1} more pass through something",
            elementLines.Count, through - elementLines.Count));
    if (throughNothing.Count > 0)
        passedThrough.Add("  through nothing: element " + string.Join(", ", throughNothing.Take(50)
            .Select(id => id.ToString()).ToArray()) + (throughNothing.Count > 50 ? " and more" : ""));
    if (notTested.Count > 0)
        passedThrough.Add("  NOT tested - no solid and no box: element " + string.Join(", ", notTested.Take(50)
            .Select(id => id.ToString()).ToArray()) + (notTested.Count > 50 ? " and more" : ""));
    if (refusedAgainst.Count > 0)
        passedThrough.Add("  NOT tested - Revit refused its test against `against` (findings says why) and "
            + "nothing else was hit: element " + string.Join(", ", refusedAgainst.Take(50)
            .Select(id => id.ToString()).ToArray()) + (refusedAgainst.Count > 50 ? " and more" : ""));
}

// ---- version 3: the comparison ------------------------------------------------

if (!comparing)
    comparison.Add("No parameter named - nothing compared. Name one in compareParameter - Wall Width - "
        + "to compare it with the thickness of what each element passes through");
else if (linkBlocked.Length > 0 || places.Count == 0)
    comparison.Add(string.Format("'{0}' NOT compared - nothing was looked in. See passedThrough",
        parameterWord));
else
{
    var notRead = compareFailures.Values.Sum(list => list.Count);
    comparison.Add(string.Format("'{0}' compared with the thickness of what each element passes through: "
        + "{1} element(s) differ, {2} agree, {3} pass through nothing it could be compared with, {4} could "
        + "not be read, {5} NOT tested. Equal is within Revit's own vertex tolerance ({6:0.##} mm)",
        parameterWord, mismatched.Count, agree, nothingToCompare, notRead,
        notTested.Count + refusedAgainst.Count, vertexTolerance * mm));
    comparison.AddRange(mismatchLines);
    if (mismatched.Count > 0 && mismatchLines.Count >= 100)
        comparison.Add("  the first 100 differences are named");
    foreach (var failure in compareFailures)
        comparison.Add(string.Format("  '{0}' {1} on {2} element(s): {3}", parameterWord, failure.Key,
            failure.Value.Count, string.Join(", ", failure.Value.Take(20).Select(id => id.ToString())
            .ToArray()) + (failure.Value.Count > 20 ? " and more" : "")));
    if (hitsUnmeasured > 0)
        comparison.Add(string.Format("  {0} hit(s) have no thickness to compare and were left out - "
            + "passedThrough says why", hitsUnmeasured));
}
