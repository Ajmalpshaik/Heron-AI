// NOT STANDALONE. Assumes `doc` and `includeLinks` are in scope, and leaves
// `takeoffJson`, `spaces`, `linksSearched` and `findings` behind.
//
// READS ONLY. No transaction is opened and nothing is set.
//
// THE TAKE-OFF FOR A BUILDING'S LOADS (docs/44 section 4). Every MEP Space in
// the model, each with every face that bounds it - outside wall, partition,
// roof, floor - its area, its outline, which way it faces, what is on the other
// side, the windows and doors in it with where they sit, and the thermal values
// each type carries. The brain (heron_takeoff.py) reads it, and the Companion's
// 3D view draws the same faces; nothing here calculates a load.
//
// EVERY SPACE, NOT THE SELECTION. "Calculate the loads for this building" is a
// question about the whole model, and a selection-bound reader refuses to run
// when nothing is selected - which is the usual state when that is asked.
//
// NOTHING IS DEFAULTED. A type with no U-value, SHGC or absorptance is written
// as null, never 0 - a window read as SHGC 0 loses its sun without a word. The
// brain refuses that Space and names the type.
//
// LINKED WALLS ONLY WHEN ASKED FOR - D-59. In a federated MEP model the walls
// a Space is bounded by are usually in the architect's link. Absent
// `includeLinks` means host only, and such a face reads beyond "unknown" with a
// finding that says links were not read. With it set, the bounding element is
// read FROM its link - its type, its function, its windows and doors - and its
// types are keyed by the link they come from, so a link's type 12345 is never
// mistaken for the host's. `linksSearched` counts the loaded links there were
// to read. Nested links are not read.
//
// EVERY NUMBER CARRIES ITS UNIT IN ITS NAME: _m2, _m3, _m, _deg, _h, _w_m2k.
// Revit's internal feet become metres here, at the edge (D-20): 1 ft = 0.3048 m
// exactly. Outlines and positions are metres in the model's own coordinates,
// rounded to the millimetre.
//
// ONE STRING, BECAUSE A LIST CROSSES AS THREE NAMES (FRAGMENT-ISSUES 5b-195) -
// the workaround READ_ELEMENT_TABLE uses. Format 1 is defined in ONE place:
// docs/work-notes/plans/building-loads-2026-10-04.md, Task 2.
//
// THE BACKSLASH AND THE QUOTE ARE BUILT FROM THEIR CHARACTER CODES, as in
// read-element-table: the tools that write these files have eaten typed
// backslashes before.

var findings = new List<string>();
var spaces = 0;
var linksSearched = 0;
var takeoffJson = "";

var FeetToMetres = 0.3048;
var SquareFeetToSquareMetres = 0.09290304;
var CubicFeetToCubicMetres = 0.028316846592;
var backslash = ((char)92).ToString();
var quote = ((char)34).ToString();
var invariant = System.Globalization.CultureInfo.InvariantCulture;

Func<string, string> esc = text =>
{
    if (text == null) return "null";
    var sb = new System.Text.StringBuilder(quote);
    foreach (var c in text)
    {
        if (c == (char)92) sb.Append(backslash + backslash);
        else if (c == (char)34) sb.Append(backslash + quote);
        else if (c < (char)32) sb.Append(backslash + "u" + ((int)c).ToString("x4"));
        else sb.Append(c);
    }
    return sb.Append(quote).ToString();
};
Func<double, string> num = d =>
    (double.IsNaN(d) || double.IsInfinity(d)) ? "null" : Math.Round(d, 6).ToString("R", invariant);
Func<double?, string> numOrNull = d => d.HasValue ? num(d.Value) : "null";
// A point in the model, in metres, to the millimetre.
Func<double, string> metres = d =>
    (double.IsNaN(d) || double.IsInfinity(d)) ? "null" : Math.Round(d * FeetToMetres, 3).ToString("R", invariant);
Func<XYZ, string> point = p => "[" + metres(p.X) + "," + metres(p.Y) + "," + metres(p.Z) + "]";

// ---- the phase the take-off is read in -------------------------------------

Phase phase = null;
try
{
    var vp = doc.ActiveView == null ? null : doc.ActiveView.get_Parameter(BuiltInParameter.VIEW_PHASE);
    if (vp != null && vp.HasValue) phase = doc.GetElement(vp.AsElementId()) as Phase;
}
catch { }
if (phase == null && doc.Phases.Size > 0) phase = doc.Phases.get_Item(doc.Phases.Size - 1) as Phase;

// ---- the links there are to read (D-59) -------------------------------------

if (includeLinks)
{
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)))
    {
        var inst = e as RevitLinkInstance;
        Document linked = null;
        try { linked = inst == null ? null : inst.GetLinkDocument(); } catch { }
        if (linked != null) linksSearched++;
    }
    if (linksSearched == 0)
        findings.Add("Links were asked for, and no linked model is loaded - every boundary was read from this model only.");
}
var linkedNotRead = new Dictionary<string, int>();

// ---- the types met, with their thermal values ------------------------------

var types = new Dictionary<string, string>();
Func<Element, BuiltInParameter, double?> thermal = (element, which) =>
{
    try
    {
        var p = element.get_Parameter(which);
        if (p != null && p.HasValue && p.StorageType == StorageType.Double) return p.AsDouble();
    }
    catch { }
    return null;
};
// U is read AS IS: Revit's internal unit for a heat transfer coefficient is
// kg/(s3.K), which IS W/(m2.K). THE FIRST THING CHECKED ON A REAL MODEL
// (cases.yaml): a window's U against its type's Properties on screen.
//
// THE U PARAMETER WAS RENAMED IN REVIT 2027. 2020 to 2026 call it
// ANALYTICAL_HEAT_TRANSFER_COEFFICIENT ("Heat Transfer Coefficient (U)"); 2027
// removed that name and has ANALYTICAL_THERMAL_TRANSMITTANCE ("Thermal
// Transmittance (U)"), measured in each release's RevitAPI.xml on 2026-10-04.
// Neither name compiles on every release, so it is found BY NAME at run time,
// the older first - one implementation, no version branch. A release with
// neither reads every U as null, and the brain refuses those Spaces in words.
BuiltInParameter? uParameter = null;
foreach (var candidate in new[] { "ANALYTICAL_HEAT_TRANSFER_COEFFICIENT", "ANALYTICAL_THERMAL_TRANSMITTANCE" })
{
    BuiltInParameter parsed;
    if (Enum.TryParse(candidate, out parsed)) { uParameter = parsed; break; }
}
if (uParameter == null) findings.Add("This Revit has no U-value parameter Heron knows by name - every U reads as missing.");
Func<Element, double?> uValue = element => uParameter.HasValue ? thermal(element, uParameter.Value) : null;

// A type is keyed by the document it lives in: "" for this model, "L<link
// instance id>:" for a link, so two documents' ids never collide.
Func<Document, string, string, ElementId, string> typeKey = (source, prefix, linkName, typeId) =>
{
    if (typeId == null || typeId == ElementId.InvalidElementId) return null;
    var key = prefix + typeId.ToString();
    if (types.ContainsKey(key)) return key;
    var type = source.GetElement(typeId) as ElementType;
    if (type == null) return null;
    var name = type.Name;
    try { if (!string.IsNullOrEmpty(type.FamilyName)) name = type.FamilyName + ": " + type.Name; } catch { }
    types[key] = "{" + esc("name") + ":" + esc(name)
        + "," + esc("category") + ":" + esc(type.Category == null ? null : type.Category.Name)
        + "," + esc("u_w_m2k") + ":" + numOrNull(uValue(type))
        + "," + esc("shgc") + ":" + numOrNull(thermal(type, BuiltInParameter.ANALYTICAL_SOLAR_HEAT_GAIN_COEFFICIENT))
        + "," + esc("absorptance") + ":" + numOrNull(thermal(type, BuiltInParameter.ANALYTICAL_ABSORPTANCE))
        + "," + esc("link") + ":" + esc(linkName)
        + "}";
    return key;
};

// ---- an opening's size: the instance's, else its type's - never guessed ----

Func<Element, Document, BuiltInParameter[], double?> length = (element, source, which) =>
{
    foreach (var bip in which)
    {
        foreach (var holder in new Element[] { element, source.GetElement(element.GetTypeId()) })
        {
            if (holder == null) continue;
            var v = thermal(holder, bip);
            if (v.HasValue && v.Value > 0) return v.Value;
        }
    }
    return null;
};
var widths = new[] { BuiltInParameter.WINDOW_WIDTH, BuiltInParameter.DOOR_WIDTH, BuiltInParameter.FAMILY_WIDTH_PARAM };
var heights = new[] { BuiltInParameter.WINDOW_HEIGHT, BuiltInParameter.DOOR_HEIGHT, BuiltInParameter.FAMILY_HEIGHT_PARAM };
var panelWidths = new[] { BuiltInParameter.CURTAIN_WALL_PANELS_WIDTH };
var panelHeights = new[] { BuiltInParameter.CURTAIN_WALL_PANELS_HEIGHT };

// The middle of an element's box, in this model's coordinates - how an opening
// is matched to the face it is in, and where the 3D view draws it. A door's own
// point sits on its sill, on the very edge of the face, where a projection is
// least reliable; the middle of its box is inside the opening.
Func<Element, Transform, XYZ> middleOf = (element, toHost) =>
{
    try
    {
        var box = element.get_BoundingBox(null);
        if (box == null) return null;
        return (toHost.OfPoint(box.Min) + toHost.OfPoint(box.Max)) * 0.5;
    }
    catch { return null; }
};
// Is it in THIS face: its middle projects onto the face, no further away than
// the wall is thick and half a foot more. A curtain wall or a long wall bounds
// several Spaces; each panel and opening belongs only to the face it is in.
Func<XYZ, Face, double, IntersectionResult> onThisFace = (middle, face, thickness) =>
{
    if (middle == null) return null;
    IntersectionResult hit = null;
    try { hit = face.Project(middle); } catch { }
    return hit != null && hit.Distance <= thickness + 0.5 ? hit : null;
};
// Where an opening sits, for the 3D view only: its middle on this face, at the
// height of the middle of its box. Its AREA is the size above, never the box.
Func<IntersectionResult, XYZ, string> placeAt = (hit, middle) =>
    hit == null || middle == null ? null : point(new XYZ(hit.XYZPoint.X, hit.XYZPoint.Y, middle.Z));

// How thick the element a face sits on is - so the look past it starts on its
// far side. A wall's width; a floor's, roof's or ceiling's own thickness, else
// the height of its box. A slab between two storeys was read as "nothing
// beyond" when the look started half a foot past a thicker slab's underside
// (the review of 2026-10-04, C1).
Func<Element, Document, double> thicknessOf = (host, source) =>
{
    var wall = host as Wall;
    if (wall != null) { try { return wall.Width; } catch { } }
    foreach (var bip in new[] { BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM,
                                BuiltInParameter.ROOF_ATTR_THICKNESS_PARAM,
                                BuiltInParameter.CEILING_THICKNESS })
    {
        foreach (var holder in new Element[] { host, source.GetElement(host.GetTypeId()) })
        {
            if (holder == null) continue;
            var v = thermal(holder, bip);
            if (v.HasValue && v.Value > 0) return v.Value;
        }
    }
    try
    {
        var box = host.get_BoundingBox(null);
        if (box != null) return Math.Min(box.Max.Z - box.Min.Z, 3.0);
    }
    catch { }
    return 0.0;
};
var seenOpenings = new Dictionary<string, string>();   // in an outside wall or a roof
var placedOpenings = new HashSet<string>();

// ---- the site ----------------------------------------------------------------

string siteJson;
{
    double? lat = null, lon = null, tz = null, elev = null, north = null;
    string place = null;
    try
    {
        var site = doc.SiteLocation;
        if (site != null)
        {
            lat = site.Latitude * 180.0 / Math.PI;
            lon = site.Longitude * 180.0 / Math.PI;
            tz = site.TimeZone;
            elev = site.Elevation * FeetToMetres;
            place = site.PlaceName;
        }
    }
    catch { findings.Add("The model's site location could not be read."); }
    // THE SIGN IS CHECKED ON A ROTATED PROJECT (cases.yaml). The brain applies
    // this angle in ONE place, heron_takeoff.azimuth_deg, so a wrong sign is
    // fixed there, once.
    try
    {
        var position = doc.ActiveProjectLocation.GetProjectPosition(XYZ.Zero);
        if (position != null) north = position.Angle * 180.0 / Math.PI;
    }
    catch { findings.Add("The model's True North could not be read."); }
    siteJson = "{" + esc("latitude_deg") + ":" + numOrNull(lat)
        + "," + esc("longitude_deg") + ":" + numOrNull(lon)
        + "," + esc("utc_offset_h") + ":" + numOrNull(tz)
        + "," + esc("elevation_m") + ":" + numOrNull(elev)
        + "," + esc("project_to_true_north_deg") + ":" + numOrNull(north)
        + "," + esc("named") + ":" + esc(place) + "}";
}

// ---- the three fields Finalize writes, as Revit shows them -----------------

var fieldNames = new[] { "Design Cooling Load", "Design Heating Load", "Specified Supply Airflow" };
string powerSymbol = null, airflowSymbol = null;
Func<Element, string, string> shown = (element, name) =>
{
    var found = element.GetParameters(name);
    if (found.Count != 1) return null;
    string text = null;
    try { text = found[0].AsValueString(); } catch { }
    if (string.IsNullOrWhiteSpace(text)) { try { text = found[0].AsString(); } catch { } }
    return text;
};
Func<string, string> symbolOf = text =>
{
    if (string.IsNullOrWhiteSpace(text)) return null;
    var at = text.Trim().IndexOf(' ');
    return at < 0 ? null : text.Trim().Substring(at + 1).Trim();
};

// ---- the air terminals, by the Space they sit in ---------------------------

var terminalsBySpace = new Dictionary<string, List<string>>();
foreach (var t in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_DuctTerminal)
                                                   .WhereElementIsNotElementType())
{
    var fi = t as FamilyInstance;
    if (fi == null || phase == null) continue;
    Space inside = null;
    try { inside = fi.get_Space(phase); } catch { }
    if (inside == null) continue;
    var key = inside.Id.ToString();
    if (!terminalsBySpace.ContainsKey(key)) terminalsBySpace[key] = new List<string>();
    terminalsBySpace[key].Add(fi.Id.ToString());
}

// ---- every Space -------------------------------------------------------------

var windowsCategory = new ElementId(BuiltInCategory.OST_Windows);
var doorsCategory = new ElementId(BuiltInCategory.OST_Doors);
var options = new SpatialElementBoundaryOptions();
options.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish;
var calculator = new SpatialElementGeometryCalculator(doc, options);
var spaceRows = new List<string>();

foreach (var element in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_MEPSpaces)
                                                         .WhereElementIsNotElementType())
{
    var space = element as Space;
    if (space == null) continue;
    spaces++;
    var label = string.Format("{0} {1}", space.Number ?? "", space.Name ?? "").Trim();

    double area = 0.0, volume = 0.0, height = 0.0;
    try { area = space.Area; } catch { }
    try { volume = space.Volume; } catch { }
    try { height = space.UnboundedHeight; } catch { }
    var placed = area > 0.0 && space.Location != null;

    string zone = null, spaceType = null;
    try { if (space.Zone != null) zone = space.Zone.Name; } catch { }
    try
    {
        var st = space.LookupParameter("Space Type");
        if (st != null && st.HasValue) spaceType = st.AsValueString();
    }
    catch { }
    if (spaceType == "<Building>" || spaceType == "") spaceType = null;

    var current = new List<string>();
    foreach (var name in fieldNames)
    {
        var text = shown(space, name);
        current.Add(esc(name) + ":" + esc(text));
        if (name == "Design Cooling Load" && powerSymbol == null) powerSymbol = symbolOf(text);
        if (name == "Specified Supply Airflow" && airflowSymbol == null) airflowSymbol = symbolOf(text);
    }
    List<string> terminals;
    terminalsBySpace.TryGetValue(space.Id.ToString(), out terminals);

    var faces = new List<string>();
    if (placed)
    {
        SpatialElementGeometryResults results = null;
        try { results = calculator.CalculateSpatialElementGeometry(space); }
        catch { findings.Add("Space " + label + ": its faces could not be worked out - it is read with none."); }
        Solid solid = results == null ? null : results.GetGeometry();
        if (solid != null)
        {
            foreach (Face face in solid.Faces)
            {
                IList<SpatialElementBoundarySubface> subs = null;
                try { subs = results.GetBoundaryFaceInfo(face); } catch { }
                if (subs == null || subs.Count == 0)
                {
                    // NOTHING BOUNDS THIS FACE - a Space's top at its own upper
                    // limit, with no room-bounding ceiling or roof. It is written
                    // as such, never dropped: dropped, the Space loses its roof
                    // without a word (the second review, N4). The brain refuses
                    // the Space and says what to make room-bounding.
                    try
                    {
                        var fbox = face.GetBoundingBox();
                        var fmid = new UV((fbox.Min.U + fbox.Max.U) / 2.0, (fbox.Min.V + fbox.Max.V) / 2.0);
                        var fn = face.ComputeNormal(fmid);
                        var fside = fn.Z > 0.7 ? "top" : fn.Z < -0.7 ? "bottom" : "wall";
                        var floops = new List<string>();
                        foreach (CurveLoop loop in face.GetEdgesAsCurveLoops())
                        {
                            var pts = new List<string>();
                            foreach (Curve curve in loop)
                            {
                                var along = curve.Tessellate();
                                for (int i = 0; i < along.Count - 1; i++) pts.Add(point(along[i]));
                            }
                            if (pts.Count >= 3) floops.Add("[" + string.Join(",", pts.ToArray()) + "]");
                        }
                        faces.Add("{" + esc("element") + ":null," + esc("type") + ":null," + esc("link") + ":null"
                            + "," + esc("bounded_by") + ":" + esc("nothing")
                            + "," + esc("side") + ":" + esc(fside)
                            + "," + esc("normal") + ":[" + num(fn.X) + "," + num(fn.Y) + "," + num(fn.Z) + "]"
                            + "," + esc("area_m2") + ":" + num(face.Area * SquareFeetToSquareMetres)
                            + "," + esc("beyond") + ":" + esc("unknown")
                            + "," + esc("beyond_space") + ":null"
                            + "," + esc("loops") + ":[" + string.Join(",", floops.ToArray()) + "]"
                            + "," + esc("openings") + ":[]}");
                        findings.Add("Space " + label + ": no element bounds its " + fside + " face - make the ceiling, roof, floor or wall there room-bounding, or set the Space's limits.");
                    }
                    catch { findings.Add("Space " + label + ": a face with nothing bounding it could not be read."); }
                    continue;
                }
                foreach (var sub in subs)
                {
                    var side = sub.SubfaceType == SubfaceType.Top ? "top"
                             : sub.SubfaceType == SubfaceType.Bottom ? "bottom" : "wall";
                    var subface = sub.GetSubface();
                    if (subface == null) continue;
                    // The subface IS part of the Space's own solid, so its
                    // normal points out of the Space - read both the normal and
                    // the centre in the subface's own UV.
                    var box = subface.GetBoundingBox();
                    var mid = new UV((box.Min.U + box.Max.U) / 2.0, (box.Min.V + box.Max.V) / 2.0);
                    var normal = subface.ComputeNormal(mid);
                    var centre = subface.Evaluate(mid);
                    // OUT OF THE SPACE, MADE SURE: the Space's own solid face
                    // points out of the solid. If the subface's normal points
                    // the other way it is turned round, not trusted (the second
                    // review, m13; Group CC row CC17 checks it on a model).
                    try
                    {
                        var onSolid = face.Project(centre);
                        if (onSolid != null && normal.DotProduct(face.ComputeNormal(onSolid.UVPoint)) < 0)
                            normal = normal.Negate();
                    }
                    catch { }

                    // ---- what bounds it: this model's element, or a link's (D-59)
                    var link = sub.SpatialBoundaryElement;
                    Element host = null;
                    Document source = doc;
                    var prefix = "";
                    string linkName = null;
                    string unreadLink = null;
                    var toHost = Transform.Identity;
                    if (link != null && link.LinkInstanceId != ElementId.InvalidElementId)
                    {
                        var inst = doc.GetElement(link.LinkInstanceId) as RevitLinkInstance;
                        Document linked = null;
                        try { linked = inst == null ? null : inst.GetLinkDocument(); } catch { }
                        if (!includeLinks || linked == null)
                        {
                            var why = inst == null ? "a linked model" : inst.Name;
                            linkedNotRead[why] = (linkedNotRead.ContainsKey(why) ? linkedNotRead[why] : 0) + 1;
                            // Marked as a link NOT READ - never confused with a
                            // face nothing bounds (the second review, N4).
                            unreadLink = why;
                        }
                        else
                        {
                            source = linked;
                            host = linked.GetElement(link.LinkedElementId);
                            prefix = "L" + inst.Id.ToString() + ":";
                            linkName = inst.Name;
                            try { toHost = inst.GetTotalTransform(); } catch { }
                        }
                    }
                    else if (link != null)
                    {
                        host = doc.GetElement(link.HostElementId);
                    }

                    // ---- what is on the other side: looked for just past the
                    // element's far side, then two feet further on (a finish,
                    // a ceiling void) before anything is called unknown.
                    string beyond = "unknown", beyondSpace = "null";
                    var wall = host as Wall;
                    double thickness = host == null ? 0.0 : thicknessOf(host, source);
                    if (host != null)
                    {
                        Space other = null;
                        // The second look is for ceilings and floors only - a
                        // ceiling void, a raised floor. Past a wall it could see
                        // across a narrow shaft to the next Space (the second
                        // review, m7).
                        foreach (var past in side == "wall" ? new[] { 0.5 } : new[] { 0.5, 2.0 })
                        {
                            try { other = doc.GetSpaceAtPoint(centre + normal * (thickness + past), phase); } catch { other = null; }
                            if (other != null && other.Id != space.Id) break;
                            other = null;
                        }
                        if (other != null && other.Id != space.Id)
                        {
                            beyond = "space";
                            beyondSpace = esc(other.Id.ToString());
                        }
                        else if (wall != null)
                        {
                            var function = WallFunction.Interior;
                            try { function = wall.WallType.Function; } catch { }
                            beyond = function == WallFunction.Exterior ? "outside" : "unconditioned";
                        }
                        else if (host is RoofBase)
                        {
                            beyond = "outside";
                        }
                    }

                    // ---- the windows, doors and curtain panels in it
                    var openings = new List<string>();
                    var hostObject = host as HostObject;
                    // Openings counted in this face: windows and doors in a wall,
                    // windows in a roof or a floor above - skylights.
                    var takesOpenings = hostObject != null && (side == "wall" || side == "top");
                    var outsideFace = beyond == "outside";
                    if (takesOpenings)
                    {
                        CurtainGrid grid = null;
                        if (wall != null) { try { grid = wall.CurtainGrid; } catch { } }
                        if (grid != null)
                        {
                            foreach (var panelId in grid.GetPanelIds())
                            {
                                var panel = source.GetElement(panelId);
                                if (panel == null) continue;
                                // Only the panels in THIS face: a curtain wall
                                // across two Spaces gives each its own.
                                var panelMiddle = middleOf(panel, toHost);
                                var panelHit = onThisFace(panelMiddle, subface, thickness);
                                if (panelHit == null) continue;
                                var panelArea = thermal(panel, BuiltInParameter.HOST_AREA_COMPUTED);
                                if (!panelArea.HasValue)
                                {
                                    findings.Add("Space " + label + ": curtain panel " + panel.Id + " has no area Revit reports - left out, never guessed.");
                                    continue;
                                }
                                var kind = panel is Wall || (panel.Category != null && panel.Category.Id == doorsCategory)
                                    ? "door" : "curtain_panel";
                                var pw = length(panel, source, panelWidths);
                                var ph = length(panel, source, panelHeights);
                                var where = placeAt(panelHit, panelMiddle);
                                openings.Add("{" + esc("element") + ":" + esc(prefix + panel.Id.ToString())
                                    + "," + esc("kind") + ":" + esc(kind)
                                    + "," + esc("type") + ":" + esc(typeKey(source, prefix, linkName, panel.GetTypeId()))
                                    + "," + esc("area_m2") + ":" + num(panelArea.Value * SquareFeetToSquareMetres)
                                    + "," + esc("centre") + ":" + (where ?? "null")
                                    + "," + esc("width_m") + ":" + (pw.HasValue ? metres(pw.Value) : "null")
                                    + "," + esc("height_m") + ":" + (ph.HasValue ? metres(ph.Value) : "null") + "}");
                            }
                        }
                        else
                        {
                            foreach (var insertId in hostObject.FindInserts(true, false, false, true))
                            {
                                var insert = source.GetElement(insertId) as FamilyInstance;
                                if (insert == null || insert.Category == null) continue;
                                var isWindow = insert.Category.Id == windowsCategory;
                                var isDoor = insert.Category.Id == doorsCategory && side == "wall";
                                if (!isWindow && !isDoor) continue;
                                var key = prefix + insert.Id.ToString();
                                if (outsideFace && !seenOpenings.ContainsKey(key))
                                    seenOpenings[key] = insert.Category.Name.ToLower() + " " + insert.Id;
                                // In THIS face: the middle of its box projects
                                // onto this Space's side of the host, within it.
                                var insertMiddle = middleOf(insert, toHost);
                                if (insertMiddle == null)
                                {
                                    var at = insert.Location as LocationPoint;
                                    if (at != null) insertMiddle = toHost.OfPoint(at.Point);
                                }
                                var hit = onThisFace(insertMiddle, subface, thickness);
                                if (hit == null) continue;
                                placedOpenings.Add(key);
                                var w = length(insert, source, widths);
                                var h = length(insert, source, heights);
                                if (!w.HasValue || !h.HasValue)
                                {
                                    findings.Add("Space " + label + ": the size of " + insert.Category.Name.ToLower() + " " + insert.Id + " could not be read - it is left out, never guessed.");
                                    continue;
                                }
                                var where = placeAt(hit, insertMiddle);
                                openings.Add("{" + esc("element") + ":" + esc(key)
                                    + "," + esc("kind") + ":" + esc(!isWindow ? "door" : side == "top" ? "skylight" : "window")
                                    + "," + esc("type") + ":" + esc(typeKey(source, prefix, linkName, insert.GetTypeId()))
                                    + "," + esc("area_m2") + ":" + num(w.Value * h.Value * SquareFeetToSquareMetres)
                                    + "," + esc("centre") + ":" + (where ?? "null")
                                    + "," + esc("width_m") + ":" + metres(w.Value)
                                    + "," + esc("height_m") + ":" + metres(h.Value) + "}");
                            }
                        }
                    }

                    // ---- its outline, for the 3D view: every loop, in metres
                    var loops = new List<string>();
                    try
                    {
                        foreach (CurveLoop loop in subface.GetEdgesAsCurveLoops())
                        {
                            var pts = new List<string>();
                            foreach (Curve curve in loop)
                            {
                                var along = curve.Tessellate();
                                for (int i = 0; i < along.Count - 1; i++) pts.Add(point(along[i]));
                            }
                            if (pts.Count >= 3) loops.Add("[" + string.Join(",", pts.ToArray()) + "]");
                        }
                    }
                    catch { }

                    faces.Add("{" + esc("element") + ":" + (host == null ? "null" : esc(prefix + host.Id.ToString()))
                        + "," + esc("type") + ":" + esc(host == null ? null : typeKey(source, prefix, linkName, host.GetTypeId()))
                        + "," + esc("link") + ":" + esc(linkName ?? unreadLink)
                        + "," + esc("bounded_by") + ":" + esc(unreadLink != null ? "unread link" : host == null ? "nothing" : "element")
                        + "," + esc("side") + ":" + esc(side)
                        + "," + esc("normal") + ":[" + num(normal.X) + "," + num(normal.Y) + "," + num(normal.Z) + "]"
                        + "," + esc("area_m2") + ":" + num(subface.Area * SquareFeetToSquareMetres)
                        + "," + esc("beyond") + ":" + esc(beyond)
                        + "," + esc("beyond_space") + ":" + beyondSpace
                        + "," + esc("loops") + ":[" + string.Join(",", loops.ToArray()) + "]"
                        + "," + esc("openings") + ":[" + string.Join(",", openings.ToArray()) + "]}");
                }
            }
        }
    }

    spaceRows.Add("{" + esc("id") + ":" + esc(space.Id.ToString())
        + "," + esc("unique_id") + ":" + esc(space.UniqueId)
        + "," + esc("number") + ":" + esc(space.Number)
        + "," + esc("name") + ":" + esc(space.Name)
        + "," + esc("level") + ":" + esc(space.Level == null ? null : space.Level.Name)
        + "," + esc("area_m2") + ":" + num(area * SquareFeetToSquareMetres)
        + "," + esc("volume_m3") + ":" + num(volume * CubicFeetToCubicMetres)
        + "," + esc("height_m") + ":" + (height > 0 ? num(height * FeetToMetres) : "null")
        + "," + esc("zone") + ":" + esc(zone)
        + "," + esc("space_type") + ":" + esc(spaceType)
        + "," + esc("placed") + ":" + (placed ? "true" : "false")
        + "," + esc("current") + ":{" + string.Join(",", current.ToArray()) + "}"
        + "," + esc("terminals") + ":[" + string.Join(",", (terminals ?? new List<string>()).Select(x => esc(x)).ToArray()) + "]"
        + "," + esc("faces") + ":[" + string.Join(",", faces.ToArray()) + "]}");
}

if (spaces == 0) findings.Add("The model has no MEP Spaces. Rooms carry no load - place Spaces first.");
// A window or door in an outside wall or a roof that sits in no Space's face
// carries no load anywhere - said, never dropped (docs/44 s6, gate 1).
foreach (var pair in seenOpenings)
{
    if (!placedOpenings.Contains(pair.Key))
        findings.Add("The " + pair.Value + " in an outside wall or roof sits in no Space's face, so "
            + "no load is counted for it - check the Space behind it is placed and bounded.");
}
foreach (var pair in linkedNotRead)
{
    findings.Add(pair.Value + " face(s) are bounded by elements in " + pair.Key + ", which was "
        + (includeLinks ? "not loaded" : "not read - ask again with links included")
        + "; what is beyond them is not known.");
}

var typeRows = new List<string>();
foreach (var pair in types) typeRows.Add(esc(pair.Key) + ":" + pair.Value);
var findingRows = new List<string>();
foreach (var f in findings) findingRows.Add(esc(f));

takeoffJson = "{" + esc("format") + ":1"
    + "," + esc("document") + ":" + esc(doc.Title)
    + "," + esc("units") + ":{" + esc("power") + ":" + esc(powerSymbol) + "," + esc("airflow") + ":" + esc(airflowSymbol) + "}"
    + "," + esc("site") + ":" + siteJson
    + "," + esc("links_read") + ":" + linksSearched
    + "," + esc("types") + ":{" + string.Join(",", typeRows.ToArray()) + "}"
    + "," + esc("spaces") + ":[" + string.Join(",", spaceRows.ToArray()) + "]"
    + "," + esc("findings") + ":[" + string.Join(",", findingRows.ToArray()) + "]}";
