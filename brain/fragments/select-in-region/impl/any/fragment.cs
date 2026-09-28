// NOT STANDALONE. Assumes `doc`, `minMm`, `maxMm`, `exact`, `categories` and
// `includeLinks` are in scope; leaves `elements`, `withoutGeometry`,
// `findings`, `linksSearched` and `linkedMatches` behind.
//
// TWO TESTS, AND THE FAST ONE OVER-REPORTS SILENTLY. A bounding box is
// axis-aligned, so a sloped drainage pipe or a rotated duct has a box far
// larger than the pipe and is reported inside a volume it never enters. On
// level work the two agree; on anything sloped they do not, and nothing warns.
// So the report always names which test ran.
//
// THE EXACT TEST HAS ITS OWN TRAP: an element with no solid geometry cannot
// intersect anything and drops out - much annotation, and the analytical
// elements. Those are COUNTED, so a small exact answer beside a large fast one
// can be read rather than guessed at.
//
// THE CORNERS ARRIVE IN FEET, NOT MILLIMETRES, AND THAT COST THIS FRAGMENT A
// PROOF. A caller types millimetres - the names say so - but the add-in's own
// point parser converts every XYZ with HeronUnits.MillimetresToFeet BEFORE a
// fragment sees it, because Revit's internal unit is the foot and every other
// XYZ fragment here relies on that. This one converted a SECOND time.
//
// The effect was invisible and total: a region was divided by 304.8, so a
// 200 m box became a 656 mm one and the answer was a perfectly calm "0
// element(s)". The report even printed "656 mm", which was the truth about the
// box it searched and read as the truth about the box that was asked for.
// Found on 2026-09-13 by proving it against 1,053 ducts and getting nothing.
//
// So: `low`/`high` are used AS GIVEN, and 304.8 appears exactly once - turning
// feet back into the millimetres the report speaks.
//
// LINKS ARE READ ONLY WHEN ASKED FOR, AND THEY ARE COUNTED, NEVER SELECTED -
// D-59. Absent `includeLinks` means host only, which is what this did before
// and what its proof measured. When it is set, each loaded link is read with
// the same test and its matches are reported as TEXT in `linkedMatches`, one
// line per link. They never enter `elements`: that list feeds the next
// fragment in a chain, and the chain revives ids against the HOST document, so
// a linked id that happens to be in use in the host binds an unrelated element
// silently (FRAGMENT-ISSUES row 75, 2 of 1128 measured).
//
// NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM - see
// SELECT_BY_CATEGORY_NAME, which carries the same rule.
//
// THE VOLUME IS MOVED INTO EACH LINK, PLACEMENT BY PLACEMENT. The corners are
// in THIS model's coordinates and a link's elements are in its own, so each
// placement's transform is inverted and applied to the volume - a file placed
// twice is asked twice, and an element is counted once. The exact test moves
// the solid itself. The box test cannot turn a box, so for a ROTATED link it
// tests the box around the turned volume: looser than the host's own box test,
// and the answer says so when it happens.

const double MillimetresPerFoot = 304.8;

var elements = new List<Element>();
var withoutGeometry = 0;
var findings = new List<string>();

// ---- D-59: which links, only when asked for --------------------------------

var linksSearched = 0;
var linkedMatches = new List<string>();
var linkedTotal = 0;
var nestedLinks = 0;
var linkBlocked = "";

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

if (minMm == null || maxMm == null)
{
    findings.Add("Both corners of the volume are needed - give the minimum and maximum in millimetres");
}
else
{
    // Either order works. A box given max-first is not a mistake anybody should
    // have to think about.
    var lowMm = new XYZ(Math.Min(minMm.X, maxMm.X), Math.Min(minMm.Y, maxMm.Y), Math.Min(minMm.Z, maxMm.Z));
    var highMm = new XYZ(Math.Max(minMm.X, maxMm.X), Math.Max(minMm.Y, maxMm.Y), Math.Max(minMm.Z, maxMm.Z));

    // IN MILLIMETRES, FOR THE REPORT ONLY. The corners themselves stay in the
    // feet Revit works in; these three exist so the sentence can say "mm" and
    // mean it. A caller who hands in a value already converted still sees an
    // absurd volume rather than a quiet zero, which is what this was for.
    var width = (highMm.X - lowMm.X) * MillimetresPerFoot;
    var depth = (highMm.Y - lowMm.Y) * MillimetresPerFoot;
    var height = (highMm.Z - lowMm.Z) * MillimetresPerFoot;

    if (width <= 0 || depth <= 0 || height <= 0)
    {
        findings.Add(string.Format("That is not a volume: {0:0} by {1:0} by {2:0} mm. Two corners "
            + "have to differ in all three directions", width, depth, height));
    }
    else
    {
        // AS GIVEN. Already feet - see the note at the top of this file.
        var low = lowMm;
        var high = highMm;

        var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
        if (categories != null && categories.Count > 0)
            collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

        var failed = "";
        Solid regionSolid = null;   // kept for the links - see the header

        if (exact)
        {
            try
            {
                var corner = low;
                var dx = high.X - low.X;
                var dy = high.Y - low.Y;
                var dz = high.Z - low.Z;

                var profile = new List<Curve>
                {
                    Line.CreateBound(corner, corner + new XYZ(dx, 0, 0)),
                    Line.CreateBound(corner + new XYZ(dx, 0, 0), corner + new XYZ(dx, dy, 0)),
                    Line.CreateBound(corner + new XYZ(dx, dy, 0), corner + new XYZ(0, dy, 0)),
                    Line.CreateBound(corner + new XYZ(0, dy, 0), corner),
                };

                var loops = new List<CurveLoop> { CurveLoop.Create(profile) };
                var box = GeometryCreationUtilities.CreateExtrusionGeometry(loops, XYZ.BasisZ, dz);

                collector = collector.WherePasses(new ElementIntersectsSolidFilter(box));
                regionSolid = box;
            }
            catch (Exception ex)
            {
                failed = ex.Message;
            }
        }
        else
        {
            collector = collector.WherePasses(new BoundingBoxIntersectsFilter(new Outline(low, high)));
        }

        if (failed.Length > 0)
        {
            findings.Add(string.Format("The exact test could not build its volume: {0}. Nothing was "
                + "returned - a silent fall back to the bounding-box test would answer a different "
                + "question under the same name", failed));
        }
        else
        {
            foreach (var element in collector) elements.Add(element);

            if (exact)
            {
                // Only meaningful for the exact test: the fast one never asks
                // for geometry, so nothing can drop out of it this way.
                var wider = new FilteredElementCollector(doc).WhereElementIsNotElementType();
                if (categories != null && categories.Count > 0)
                    wider = wider.WherePasses(new ElementMulticategoryFilter(categories));
                wider = wider.WherePasses(new BoundingBoxIntersectsFilter(new Outline(low, high)));

                var loose = 0;
                foreach (var element in wider) loose++;
                withoutGeometry = loose - elements.Count;
                if (withoutGeometry < 0) withoutGeometry = 0;
            }

            findings.Add(string.Format("{0} element(s) in a {1:0} x {2:0} x {3:0} mm volume, by the "
                + "{4} test{5}",
                elements.Count, width, depth, height,
                exact ? "EXACT geometry" : "fast BOUNDING BOX",
                categories == null || categories.Count == 0
                    ? ", across every category"
                    : string.Format(", within {0} category/categories", categories.Count)));

            if (!exact)
                findings.Add("A bounding box is axis-aligned, so a sloped or rotated element is "
                    + "reported inside a volume its geometry never enters. Ask for the exact test "
                    + "where that matters");
            else if (withoutGeometry > 0)
                findings.Add(string.Format("{0} element(s) whose bounding box overlaps the volume are "
                    + "NOT in this answer - either their real geometry misses it, or they have no "
                    + "solid geometry to test at all", withoutGeometry));

            // THE SAME VOLUME IN EACH LINK. Counted, never selected.
            var turned = false;
            for (var i = 0; i < linkDocs.Count; i++)
            {
                var seen = new HashSet<ElementId>();
                var unreadPlacements = 0;

                foreach (var placement in linkPlacements[i])
                {
                    try
                    {
                        var toLink = placement.GetTotalTransform().Inverse;

                        var linkedCollector = new FilteredElementCollector(linkDocs[i])
                            .WhereElementIsNotElementType();
                        if (categories != null && categories.Count > 0)
                            linkedCollector = linkedCollector.WherePasses(
                                new ElementMulticategoryFilter(categories));

                        if (exact)
                        {
                            linkedCollector = linkedCollector.WherePasses(new ElementIntersectsSolidFilter(
                                SolidUtils.CreateTransformed(regionSolid, toLink)));
                        }
                        else
                        {
                            // The eight corners, moved, and the box around them.
                            var lowIn = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
                            var highIn = new XYZ(double.MinValue, double.MinValue, double.MinValue);
                            foreach (var x in new[] { low.X, high.X })
                            foreach (var y in new[] { low.Y, high.Y })
                            foreach (var z in new[] { low.Z, high.Z })
                            {
                                var moved = toLink.OfPoint(new XYZ(x, y, z));
                                lowIn = new XYZ(Math.Min(lowIn.X, moved.X), Math.Min(lowIn.Y, moved.Y),
                                    Math.Min(lowIn.Z, moved.Z));
                                highIn = new XYZ(Math.Max(highIn.X, moved.X), Math.Max(highIn.Y, moved.Y),
                                    Math.Max(highIn.Z, moved.Z));
                            }

                            if (Math.Abs(toLink.BasisX.X - 1.0) > 1e-9) turned = true;

                            linkedCollector = linkedCollector.WherePasses(
                                new BoundingBoxIntersectsFilter(new Outline(lowIn, highIn)));
                        }

                        foreach (var id in linkedCollector.ToElementIds()) seen.Add(id);
                    }
                    catch (Exception) { unreadPlacements++; }
                }

                linksSearched++;
                linkedTotal += seen.Count;
                linkedMatches.Add(string.Format("{0}: {1}{2}", linkDocs[i].Title, seen.Count,
                    unreadPlacements > 0
                        ? string.Format(" ({0} placement(s) could not be tested)", unreadPlacements)
                        : ""));
            }

            if (turned)
                linkedMatches.Add("A ROTATED link was tested with the box around the turned volume - "
                    + "looser than the host's box test. Ask for the exact test where that matters");
        }
    }
}

if (includeLinks && linkDocs.Count > 0 && linksSearched == 0)
    linkBlocked = "Links NOT read: no volume was tested - see findings";

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
    linkedMatches.Add("Linked elements are counted here, never selected - the next step would "
        + "look them up in this model and could bind the wrong element");
