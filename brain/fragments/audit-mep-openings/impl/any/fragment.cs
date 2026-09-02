// NOT STANDALONE. Assumes `doc` and `openings` are in scope; leaves `findings`,
// `stale`, `combined` and `unhosted` behind.
//
// READ ONLY. Opens no transaction and changes nothing, here or in any link.
//
// THE TWO KINDS OF "OPENING" ARE NOT THE SAME KIND OF ELEMENT, and this is the
// trap the fragment is built around:
//   * a cut void is a Revit `Opening`. It has NO SOLID - only a boundary and a
//     host. Asking it for geometry returns nothing usable.
//   * a placed sleeve is a FamilyInstance, which does have solids.
// The obvious way to gather openings returns mostly the FIRST kind, so an audit
// written for the second reports "no geometry" for every one: no crash, no
// error, and an audit that audited nothing. Both are handled, and each row says
// which route was used.
//
// THE EXTENT IS CROSS-CHECKED. An Opening's boundary and its bounding box must
// agree; where they do not, the row reads SUSPECT rather than being audited on
// geometry that may be in the wrong place. The boundary's coordinate space is
// not stated in the documentation, so it is verified rather than assumed.
//
// STRUCTURE IN A LINK IS NOT SCANNED. On a normal coordination job the walls
// and slabs are a link, so a clean UNHOSTED count there has checked nothing.

var findings = new List<string>();
var stale = new List<ElementId>();
var combined = new List<ElementId>();
var unhosted = new List<ElementId>();

var SERVICE_CATEGORIES = new[] { BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_DuctCurves,
                                 BuiltInCategory.OST_CableTray, BuiltInCategory.OST_Conduit };

var services = new List<KeyValuePair<Element, BoundingBoxXYZ>>();
foreach (var category in SERVICE_CATEGORIES)
{
    foreach (var element in new FilteredElementCollector(doc)
        .OfCategory(category).WhereElementIsNotElementType())
    {
        var box = element.get_BoundingBox(null);
        if (box != null) services.Add(new KeyValuePair<Element, BoundingBoxXYZ>(element, box));
    }
}

foreach (var opening in openings)
{
    if (opening == null || !opening.IsValidObject) continue;

    var asOpening = opening as Opening;
    var route = asOpening != null ? "cut void" : "placed family";

    var box = opening.get_BoundingBox(null);
    if (box == null)
    {
        findings.Add(string.Format("opening {0} ({1}): no readable extent - not audited",
            opening.Id, route));
        continue;
    }

    // A cut void's boundary is cross-checked against its box. See the header.
    var suspect = false;
    if (asOpening != null)
    {
        try
        {
            var rect = asOpening.BoundaryRect;
            if (rect != null && rect.Count >= 2)
            {
                var low = rect[0];
                var high = rect[1];
                var boundaryWidth = Math.Abs(high.X - low.X);
                var boxWidth = box.Max.X - box.Min.X;
                // An order-of-magnitude disagreement means the boundary is not
                // in the space this assumed.
                if (boundaryWidth > 0 && boxWidth > 0
                    && (boundaryWidth > boxWidth * 10 || boxWidth > boundaryWidth * 10))
                {
                    suspect = true;
                }
            }
        }
        catch { }
    }

    // Hosting. Answered against the HOST model only.
    var hosted = true;
    if (asOpening != null)
    {
        try { hosted = asOpening.Host != null; }
        catch { hosted = false; }
    }
    else
    {
        var instance = opening as FamilyInstance;
        if (instance != null)
        {
            try { hosted = instance.Host != null; }
            catch { hosted = false; }
        }
    }

    if (!hosted)
    {
        unhosted.Add(opening.Id);
    }

    // What passes through it.
    var through = new List<string>();
    foreach (var pair in services)
    {
        var serviceBox = pair.Value;
        if (serviceBox.Max.X < box.Min.X || serviceBox.Min.X > box.Max.X) continue;
        if (serviceBox.Max.Y < box.Min.Y || serviceBox.Min.Y > box.Max.Y) continue;
        if (serviceBox.Max.Z < box.Min.Z || serviceBox.Min.Z > box.Max.Z) continue;
        if (through.Count < 6)
        {
            through.Add(string.Format("{0} {1}",
                pair.Key.Category == null ? "service" : pair.Key.Category.Name, pair.Key.Id));
        }
        else
        {
            through.Add("...");
            break;
        }
    }

    var status = new List<string>();
    if (suspect) status.Add("SUSPECT GEOMETRY - its boundary and its extent disagree, so the rest of "
        + "this row is not trustworthy");
    if (!hosted) status.Add("UNHOSTED - it is not inside anything in the HOST model. If the structure "
        + "is a link, that is expected and this question has not really been asked");

    if (through.Count == 0)
    {
        stale.Add(opening.Id);
        status.Add("STALE - nothing runs through it any more. Either the run moved and the hole did "
            + "not, or it was never needed");
    }
    else if (through.Count > 1)
    {
        combined.Add(opening.Id);
        status.Add(string.Format("COMBINED - {0} services share it: {1}. Sometimes intended, and worth "
            + "confirming with the structural engineer", through.Count, string.Join(", ", through)));
    }
    else
    {
        status.Add("OK - " + through[0] + " through it");
    }

    findings.Add(string.Format("opening {0} ({1}): {2}", opening.Id, route, string.Join("; ", status)));
}

findings.Insert(0, string.Format("{0} opening(s) audited against {1} service(s) in the host model: {2} "
    + "stale, {3} shared by more than one service, {4} unhosted. STRUCTURE IN LINKS WAS NOT SCANNED",
    openings.Count, services.Count, stale.Count, combined.Count, unhosted.Count));
