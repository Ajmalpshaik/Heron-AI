// NOT STANDALONE. Assumes `doc` is in scope, and leaves `takeoffJson`, `spaces`
// and `findings` behind.
//
// READS ONLY. No transaction is opened and nothing is set.
//
// THE TAKE-OFF FOR A BUILDING'S LOADS (docs/44 section 4). Every MEP Space in
// the model, each with every face that bounds it - outside wall, partition,
// roof, floor - its area, which way it faces, what is on the other side, the
// windows and doors in it, and the thermal values each type carries. The brain
// (heron_takeoff.py) reads it; nothing here calculates a load.
//
// EVERY SPACE, NOT THE SELECTION. "Calculate the loads for this building" is a
// question about the whole model, and a selection-bound reader refuses to run
// when nothing is selected - which is the usual state when that is asked.
//
// NOTHING IS DEFAULTED. A type with no U-value, SHGC or absorptance is written
// as null, never 0 - a window read as SHGC 0 loses its sun without a word. The
// brain refuses that Space and names the type.
//
// EVERY NUMBER CARRIES ITS UNIT IN ITS NAME: _m2, _m3, _m, _deg, _h, _w_m2k.
// Revit's internal feet become metres here, at the edge (D-20): 1 ft = 0.3048 m
// exactly.
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

// ---- the phase the take-off is read in -------------------------------------

Phase phase = null;
try
{
    var vp = doc.ActiveView == null ? null : doc.ActiveView.get_Parameter(BuiltInParameter.VIEW_PHASE);
    if (vp != null && vp.HasValue) phase = doc.GetElement(vp.AsElementId()) as Phase;
}
catch { }
if (phase == null && doc.Phases.Size > 0) phase = doc.Phases.get_Item(doc.Phases.Size - 1) as Phase;

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
Func<ElementId, string> typeKey = typeId =>
{
    if (typeId == null || typeId == ElementId.InvalidElementId) return null;
    var key = typeId.ToString();
    if (types.ContainsKey(key)) return key;
    var type = doc.GetElement(typeId) as ElementType;
    if (type == null) return null;
    var family = type as FamilySymbol;
    var name = family != null ? family.FamilyName + ": " + type.Name : type.Name;
    if (family == null)
    {
        try { name = type.FamilyName + ": " + type.Name; } catch { }
    }
    types[key] = "{" + esc("name") + ":" + esc(name)
        + "," + esc("category") + ":" + esc(type.Category == null ? null : type.Category.Name)
        + "," + esc("u_w_m2k") + ":" + numOrNull(uValue(type))
        + "," + esc("shgc") + ":" + numOrNull(thermal(type, BuiltInParameter.ANALYTICAL_SOLAR_HEAT_GAIN_COEFFICIENT))
        + "," + esc("absorptance") + ":" + numOrNull(thermal(type, BuiltInParameter.ANALYTICAL_ABSORPTANCE))
        + "}";
    return key;
};

// ---- an opening's size: the instance's, else its type's - never guessed ----

Func<Element, BuiltInParameter[], double?> length = (element, which) =>
{
    foreach (var bip in which)
    {
        foreach (var holder in new Element[] { element, doc.GetElement(element.GetTypeId()) })
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
var windowsCategory = new ElementId(BuiltInCategory.OST_Windows);
var doorsCategory = new ElementId(BuiltInCategory.OST_Doors);

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
                if (subs == null) continue;
                foreach (var sub in subs)
                {
                    var side = sub.SubfaceType == SubfaceType.Top ? "top"
                             : sub.SubfaceType == SubfaceType.Bottom ? "bottom" : "wall";
                    var subface = sub.GetSubface();
                    if (subface == null) continue;
                    var box = subface.GetBoundingBox();
                    var mid = new UV((box.Min.U + box.Max.U) / 2.0, (box.Min.V + box.Max.V) / 2.0);
                    // Outward from the Space's own solid: a solid's face normal
                    // points out of the solid.
                    var normal = face.ComputeNormal(mid);
                    var centre = subface.Evaluate(mid);

                    var link = sub.SpatialBoundaryElement;
                    Element host = null;
                    string beyond = "unknown", beyondSpace = "null";
                    if (link != null && link.LinkedElementId != ElementId.InvalidElementId)
                    {
                        findings.Add("Space " + label + ": a face is bounded by an element in a linked model - read from the link; what is beyond it is not known.");
                    }
                    else if (link != null)
                    {
                        host = doc.GetElement(link.HostElementId);
                    }
                    double thickness = 0.0;
                    var wall = host as Wall;
                    if (wall != null) { try { thickness = wall.Width; } catch { } }
                    if (host != null)
                    {
                        Space other = null;
                        try { other = doc.GetSpaceAtPoint(centre + normal * (thickness + 0.5), phase); } catch { }
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

                    var openings = new List<string>();
                    if (wall != null && side == "wall")
                    {
                        CurtainGrid grid = null;
                        try { grid = wall.CurtainGrid; } catch { }
                        if (grid != null)
                        {
                            foreach (var panelId in grid.GetPanelIds())
                            {
                                var panel = doc.GetElement(panelId);
                                if (panel == null) continue;
                                var panelArea = thermal(panel, BuiltInParameter.HOST_AREA_COMPUTED);
                                if (!panelArea.HasValue)
                                {
                                    findings.Add("Space " + label + ": curtain panel " + panel.Id + " has no area Revit reports - left out, never guessed.");
                                    continue;
                                }
                                var kind = panel is Wall || (panel.Category != null && panel.Category.Id == doorsCategory)
                                    ? "door" : "curtain_panel";
                                openings.Add("{" + esc("element") + ":" + esc(panel.Id.ToString())
                                    + "," + esc("kind") + ":" + esc(kind)
                                    + "," + esc("type") + ":" + esc(typeKey(panel.GetTypeId()))
                                    + "," + esc("area_m2") + ":" + num(panelArea.Value * SquareFeetToSquareMetres) + "}");
                            }
                        }
                        else
                        {
                            foreach (var insertId in wall.FindInserts(true, false, false, true))
                            {
                                var insert = doc.GetElement(insertId) as FamilyInstance;
                                if (insert == null || insert.Category == null) continue;
                                var isWindow = insert.Category.Id == windowsCategory;
                                var isDoor = insert.Category.Id == doorsCategory;
                                if (!isWindow && !isDoor) continue;
                                // In THIS face: the insert's point projects onto
                                // this Space's side of the wall, within it.
                                var at = insert.Location as LocationPoint;
                                if (at == null) continue;
                                IntersectionResult hit = null;
                                try { hit = subface.Project(at.Point); } catch { }
                                if (hit == null || hit.Distance > thickness + 0.5) continue;
                                var w = length(insert, widths);
                                var h = length(insert, heights);
                                if (!w.HasValue || !h.HasValue)
                                {
                                    findings.Add("Space " + label + ": the size of " + insert.Category.Name.ToLower() + " " + insert.Id + " could not be read - it is left out, never guessed.");
                                    continue;
                                }
                                openings.Add("{" + esc("element") + ":" + esc(insert.Id.ToString())
                                    + "," + esc("kind") + ":" + esc(isWindow ? "window" : "door")
                                    + "," + esc("type") + ":" + esc(typeKey(insert.GetTypeId()))
                                    + "," + esc("area_m2") + ":" + num(w.Value * h.Value * SquareFeetToSquareMetres) + "}");
                            }
                        }
                    }

                    faces.Add("{" + esc("element") + ":" + (host == null ? "null" : esc(host.Id.ToString()))
                        + "," + esc("type") + ":" + esc(host == null ? null : typeKey(host.GetTypeId()))
                        + "," + esc("side") + ":" + esc(side)
                        + "," + esc("normal") + ":[" + num(normal.X) + "," + num(normal.Y) + "," + num(normal.Z) + "]"
                        + "," + esc("area_m2") + ":" + num(subface.Area * SquareFeetToSquareMetres)
                        + "," + esc("beyond") + ":" + esc(beyond)
                        + "," + esc("beyond_space") + ":" + beyondSpace
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

var typeRows = new List<string>();
foreach (var pair in types) typeRows.Add(esc(pair.Key) + ":" + pair.Value);
var findingRows = new List<string>();
foreach (var f in findings) findingRows.Add(esc(f));

takeoffJson = "{" + esc("format") + ":1"
    + "," + esc("document") + ":" + esc(doc.Title)
    + "," + esc("units") + ":{" + esc("power") + ":" + esc(powerSymbol) + "," + esc("airflow") + ":" + esc(airflowSymbol) + "}"
    + "," + esc("site") + ":" + siteJson
    + "," + esc("types") + ":{" + string.Join(",", typeRows.ToArray()) + "}"
    + "," + esc("spaces") + ":[" + string.Join(",", spaceRows.ToArray()) + "]"
    + "," + esc("findings") + ":[" + string.Join(",", findingRows.ToArray()) + "]}";
