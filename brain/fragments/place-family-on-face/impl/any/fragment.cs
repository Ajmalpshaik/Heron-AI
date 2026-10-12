// NOT STANDALONE. Assumes `doc`, `symbol`, `pointsMm`, `ray`, `view3D`,
// `facing` and `searchLinks` are in scope; leaves `created`, `placed`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// PLACE_FAMILY_INSTANCES CANNOT DO THIS. That one places at a point on a level,
// so a diffuser sits at a height that looks right and knows nothing about the
// ceiling. Placed here it is HOSTED: it moves with its host and dies with it.
//
// VERSION 2 (2026-10-13, the gap fill) FINDS THE FACE ITSELF. Version 1 took
// Revit face references, which no request can carry, so nothing could run it.
// Here each point looks along `ray` - up to the ceiling above, down to the
// floor, sideways to a wall - and the first face it meets in `view3D` hosts the
// family there:
//
//   pointsMm     "x,y,z; ..." in millimetres - where to look from
//   ray          "up", "down", "north", "south", "east", "west", or "x,y,z";
//                EMPTY means up
//   view3D       a 3D view, not a template - Revit finds faces only through
//                one, and an element hidden in it is not found
//   facing       "x,y,z" the way the family's own X should point along the
//                face; EMPTY takes the project's X on a ceiling or floor and
//                straight up on a wall
//   searchLinks  true looks in loaded links as well - the ceiling is usually
//                in the architect's model
//
// A FACE IN A LINK HOSTS TOO. Version 1 refused it on a belief that was never
// measured; Revit's own Place on Face takes a linked face, and the reference
// Revit hands back for a hit in a link is one this model can use. If the link
// is unloaded later the instance stays, unhosted - Revit's behaviour, said in
// the findings. Whether this Revit takes it is what row DS of the register
// measures.
//
// THE PLACEMENT TYPE IS CHECKED FIRST: the face overload needs a work-plane
// based family and throws an unreadable error for any other.
//
// THE HOST IS VERIFIED AFTER PLACING, NOT ASSUMED. An instance that came back
// unhosted is the exact failure this exists to prevent, and it looks completely
// correct on screen.

const double MmPerFoot = 304.8;
var created = new List<FamilyInstance>();
var refused = new List<string>();
var findings = new List<string>();
var placed = 0;
string stop = null;

var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;
Func<string, XYZ> readXyz = text =>
{
    var parts = (text ?? "").Split(',');
    double x, y, z;
    if (parts.Length != 3 || !double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
        || !double.TryParse(parts[1].Trim(), numberStyle, culture, out y)
        || !double.TryParse(parts[2].Trim(), numberStyle, culture, out z)) return null;
    return new XYZ(x, y, z);
};

var points = new List<XYZ>();
foreach (var chunk in (pointsMm ?? "").Split(';'))
{
    if (chunk.Trim() == "") continue;
    var p = readXyz(chunk);
    if (p == null) { points = null; break; }
    points.Add(p / MmPerFoot);
}

var rayText = (ray ?? "").Trim().ToLowerInvariant();
XYZ look = rayText == "" || rayText == "up" ? XYZ.BasisZ : rayText == "down" ? -XYZ.BasisZ
    : rayText == "north" ? XYZ.BasisY : rayText == "south" ? -XYZ.BasisY
    : rayText == "east" ? XYZ.BasisX : rayText == "west" ? -XYZ.BasisX : readXyz(rayText);
XYZ along = (facing ?? "").Trim() == "" ? null : readXyz(facing);

if (symbol == null || !symbol.IsValidObject) stop = "No family type was given.";
else if (symbol.Family == null || symbol.Family.FamilyPlacementType != FamilyPlacementType.WorkPlaneBased)
    stop = "\"" + symbol.Name + "\" is not a face-based family - its placement type is "
        + (symbol.Family == null ? "unknown" : symbol.Family.FamilyPlacementType.ToString())
        + ", and placing on a face needs a work-plane-based one. PLACE_FAMILY_INSTANCES places a level-based family; "
        + "PLACE_HOSTED_FAMILY puts a wall-, floor- or ceiling-hosted one into its host.";
else if (points == null || points.Count == 0) stop = "No points were given - x,y,z in millimetres, separated by semicolons.";
else if (look == null || look.GetLength() < 1e-9) stop = "\"" + ray + "\" is not up, down, north, south, east, west or x,y,z.";
else if ((facing ?? "").Trim() != "" && (along == null || along.GetLength() < 1e-9)) stop = "\"" + facing + "\" is not x,y,z.";
else if (view3D == null || view3D.IsTemplate) stop = "A 3D view that is not a template is needed - Revit finds faces through one.";

if (stop != null) findings.Add(stop + " NOTHING WAS PLACED.");
else
{
    look = look.Normalize();
    if (!symbol.IsActive)
    {
        // An unactivated type places nothing and reports no error.
        symbol.Activate();
        doc.Regenerate();
        findings.Add("\"" + symbol.Name + "\" was not activated in this project and has been.");
    }

    var finder = new ReferenceIntersector(view3D);
    finder.TargetType = FindReferenceTarget.Face;
    finder.FindReferencesInRevitLinks = searchLinks;
    var refDirection = along != null ? along.Normalize() : Math.Abs(look.Z) > 0.7 ? XYZ.BasisX : XYZ.BasisZ;
    var linked = 0;

    for (var i = 0; i < points.Count; i++)
    {
        var from = points[i];
        var label = "point " + (i + 1) + " (" + Math.Round(from.X * MmPerFoot) + ", " + Math.Round(from.Y * MmPerFoot)
            + ", " + Math.Round(from.Z * MmPerFoot) + ")";
        ReferenceWithContext hit = null;
        try { hit = finder.FindNearest(from, look); }
        catch (Exception ex) { refused.Add(label + " - Revit could not look: " + ex.Message); continue; }
        if (hit == null) { refused.Add(label + " - no face that way in \"" + view3D.Name + "\""); continue; }

        var face = hit.GetReference();
        var at = face.GlobalPoint ?? from + look * hit.Proximity;
        var inLink = doc.GetElement(face.ElementId) as RevitLinkInstance;
        try
        {
            var instance = doc.Create.NewFamilyInstance(face, at, refDirection, symbol);
            if (instance == null) { refused.Add(label + " - Revit returned nothing and raised nothing"); continue; }
            doc.Regenerate();
            if (instance.Host == null)
            {
                refused.Add(label + " - placed but came back with NO HOST (id " + instance.Id + ")");
                continue;
            }
            created.Add(instance);
            placed++;
            if (inLink != null) linked++;
        }
        catch (Exception ex)
        {
            refused.Add(label + " - Revit refused the face" + (inLink != null ? " in link \"" + inLink.Name + "\"" : "") + ": " + ex.Message);
        }
    }

    findings.Add("Placed " + placed + " of " + points.Count + " instance(s) of \"" + symbol.Name + "\", each read back as hosted"
        + (linked > 0 ? "; " + linked + " on a face in a linked model - unloading that link leaves them unhosted" : "") + ".");
}

if (refused.Count > 0) findings.Add(refused.Count + " point(s) placed nothing: " + string.Join("; ", refused.ToArray()));
