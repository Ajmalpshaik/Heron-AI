// NOT STANDALONE. Assumes `doc`, `categories`, `maxTriangles` and `wholeModel`
// are in scope, and leaves `geometryJson`, `triangles` and `findings` behind.
//
// READS ONLY. No transaction is opened and nothing is set.
//
// THE MODEL'S 3D GEOMETRY FOR THE COMPANION'S MODEL VIEW (docs/40 s21.10).
// Every placed model element in scope, triangulated, in metres, one entry per
// element with its category - as one JSON string, format 1, that the brain
// (heron_model_view.py) hands to Heron's own WebGL renderer.
//
// THE SCOPE. Left out, wholeModel is false: with a 3D view active, only what
// that view can show - FilteredElementCollector(doc, view) and Options.View,
// so its hidden categories, hidden elements, filters, phase and design options
// are the view's - and anything wholly outside its section box is left out
// here too, because Revit's view collector may still pass it. With Options.View
// set the VIEW'S detail level is used: Revit refuses an Options with both View
// and DetailLevel (RevitAPI.xml). No 3D view active, or wholeModel true: the
// whole model at Coarse, and the first case says so in notRead.
//
// THE ELEMENTS ARE REPORT_MODEL_DASHBOARD'S - not a type, a MODEL category, not
// view-specific, with an extent, not a Level - less what this leaves out on
// purpose: a Revit link placement (another model), a CAD import, rooms, spaces
// and areas (volumes Revit does not draw in 3D), each counted in notRead; and a
// model group or assembly, whose members are elements of their own and are
// drawn as themselves, never twice.
//
// THE GEOMETRY WALK IS REPORT_ELEMENT_GEOMETRY'S: a GeometryInstance through
// GetInstanceGeometry(), so a family instance lands in model coordinates as
// placed and turned; a solid with no faces is skipped. Each face through
// Face.Triangulate() - REPORT_GEOMETRY_COMPLEXITY's route, so a triangle here
// is a triangle there. A Mesh is taken as it is. Curves and points have
// nothing to draw.
//
// A PIPE OR DUCT AT COARSE IS A LINE. An element whose geometry at the first
// detail level is only lines - nothing to triangulate - is read once more at
// Fine and drawn as its solid. One with no lines and no surfaces is not read
// again: at that level it is simply not shown.
//
// LARGEST FIRST, AND THE BUDGET STOPS THE READING. Elements are read in order
// of the diagonal of their extent, longest first, so a capped answer still
// shows the building's big pieces. The first element that would carry the
// total past maxTriangles is left out, and every one after it is left out
// without being read - which is what bounds the time on a large model.
//
// METRES BY * 0.3048, EXACT BY DEFINITION, and no unit API (D-20) - the same
// on every release. Each vertex is rounded to the millimetre and kept as whole
// millimetres until it is written, so points shared at the millimetre are one
// point per element, a triangle collapsed to a line is dropped, and the offset
// - the centre of the box round everything drawn - subtracts exactly.
//
// ONE ELEMENT NEVER STOPS THE READ. Each is read in its own try: one that
// throws is counted in noGeometry and the first such is named in a
// "geometry:" line of notRead. A face that will not triangulate is skipped
// and counted in a "faces:" line. The run clock's stop is caught with the
// rest and is sticky - the add-in still reports the run stopped (HeronRunClock).
//
// REFUSED BEFORE ANYTHING IS READ: a category that is not a BuiltInCategory
// name this release has, and maxTriangles above 500000 or below 1.
//
// ONE STRING, BECAUSE A LIST CROSSES AS ITS FIRST THREE ITEMS (FRAGMENT-ISSUES
// 5b-195). Element ids are digit strings: ToString(), never read as a number.
//
// THE WORK IS INSIDE ONE BLOCK ON PURPOSE. The add-in hands back every name
// left at the top of a fragment, so only the three outputs are left there.
//
// THE BACKSLASH AND THE QUOTE ARE BUILT FROM THEIR CHARACTER CODES, as in
// report-model-dashboard: the tools that write these files have eaten typed
// backslashes before.

var geometryJson = "";
var triangles = 0;
var findings = "";

{
    const double MetresPerFoot = 0.3048;
    const int MostTriangles = 500000;
    var invariant = System.Globalization.CultureInfo.InvariantCulture;
    var backslash = ((char)92).ToString();
    var quote = ((char)34).ToString();

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
    Func<long, string> whole = n => n.ToString(invariant);
    // Whole millimetres written as metres, to the millimetre.
    Func<long, string> metres = mm => (mm / 1000.0).ToString("0.###", invariant);
    Func<double, long> mmOf = feet => (long)Math.Round(feet * MetresPerFoot * 1000.0, MidpointRounding.AwayFromZero);
    Func<Exception, string> why = ex => ex.GetType().Name + ": " + (ex.Message ?? "").Trim();
    Func<IEnumerable<string>, string> textList = items => "[" + string.Join(",", items.Select(esc).ToArray()) + "]";

    // ---- what was asked, refused before anything is read --------------------

    if (maxTriangles > MostTriangles)
        throw new InvalidOperationException("maxTriangles " + whole(maxTriangles) + " is above the limit of "
            + whole(MostTriangles) + " triangles, so NOTHING was read. Ask for " + whole(MostTriangles)
            + " or fewer - and to see more of a large model, narrow it with categories, or open a 3D view "
            + "with a section box round the part wanted");
    if (maxTriangles < 1)
        throw new InvalidOperationException("maxTriangles " + whole(maxTriangles) + " leaves nothing to draw, "
            + "so NOTHING was read. Ask for 1 to " + whole(MostTriangles) + "; left out it is 150000");

    // "all", or nothing typed, is every model category - the card's `absent:`.
    var askedText = (categories ?? "").Trim();
    var narrowed = askedText.Replace(",", "").Trim().Length > 0
        && !string.Equals(askedText, "all", StringComparison.OrdinalIgnoreCase);
    var wanted = new List<BuiltInCategory>();
    var wantedNames = new List<string>();
    if (narrowed)
    {
        var known = Enum.GetNames(typeof(BuiltInCategory));
        var unknown = new List<string>();
        foreach (var piece in askedText.Split(','))
        {
            var typed = piece.Trim();
            if (typed.Length == 0) continue;
            string spelt = null;
            foreach (var name in known)
            {
                if (string.Equals(name, typed, StringComparison.OrdinalIgnoreCase)) { spelt = name; break; }
            }
            if (spelt == null || !spelt.StartsWith("OST_", StringComparison.Ordinal)) { unknown.Add(typed); continue; }
            var builtIn = (BuiltInCategory)Enum.Parse(typeof(BuiltInCategory), spelt);
            if (wanted.Contains(builtIn)) continue;
            wanted.Add(builtIn);
            wantedNames.Add(spelt);
        }
        if (unknown.Count > 0)
            throw new InvalidOperationException("'" + string.Join("', '", unknown.ToArray()) + "' "
                + (unknown.Count == 1 ? "is not a Revit category name" : "are not Revit category names")
                + " this release knows, so NOTHING was read. A category is named as Revit names it inside - "
                + "OST_Walls, OST_DuctCurves, OST_MechanicalEquipment - commas between; leave categories out "
                + "for every model category");
    }

    var notRead = new List<string>();
    var capped = new List<string>();
    var lines = new List<string>();

    // ---- the scope -----------------------------------------------------------

    View active = null;
    try { active = doc.ActiveView; } catch (Exception) { active = null; }
    var view3d = active as View3D;
    var useView = !wholeModel && view3d != null && !view3d.IsTemplate;
    FilteredElementCollector collector = null;
    FilteredElementCollector linkCollector = null;
    if (useView)
    {
        try
        {
            collector = new FilteredElementCollector(doc, view3d.Id);
            linkCollector = new FilteredElementCollector(doc, view3d.Id);
        }
        catch (Exception ex)
        {
            useView = false;
            collector = null;
            linkCollector = null;
            notRead.Add("active view: '" + view3d.Name + "' could not be read element by element (" + why(ex)
                + "), so the whole model was read");
        }
    }
    else if (!wholeModel)
    {
        notRead.Add(active == null
            ? "active view: this model has no active view - it is not the one in front - so the whole model was read"
            : "active view: '" + active.Name + "' is not a 3D view, so the whole model was read - open a 3D view "
              + "first to draw only what it shows");
    }
    if (collector == null)
    {
        collector = new FilteredElementCollector(doc);
        linkCollector = new FilteredElementCollector(doc);
    }
    var scope = useView ? "active 3D view '" + view3d.Name + "'" : "whole model";

    // The box round the section box, along the model's axes, in millimetres:
    // min x, y, z then max x, y, z. Its eight corners through its transform, so
    // a turned section box gives the box that holds it - never one smaller.
    long[] box = null;
    if (useView)
    {
        try
        {
            if (view3d.IsSectionBoxActive)
            {
                var section = view3d.GetSectionBox();
                if (section != null && section.Min != null && section.Max != null)
                {
                    var turn = section.Transform;
                    box = new long[] { long.MaxValue, long.MaxValue, long.MaxValue, long.MinValue, long.MinValue, long.MinValue };
                    for (var corner = 0; corner < 8; corner++)
                    {
                        var local = new XYZ((corner & 1) == 0 ? section.Min.X : section.Max.X,
                                            (corner & 2) == 0 ? section.Min.Y : section.Max.Y,
                                            (corner & 4) == 0 ? section.Min.Z : section.Max.Z);
                        var p = turn == null ? local : turn.OfPoint(local);
                        var at = new long[] { mmOf(p.X), mmOf(p.Y), mmOf(p.Z) };
                        for (var axis = 0; axis < 3; axis++)
                        {
                            if (at[axis] < box[axis]) box[axis] = at[axis];
                            if (at[axis] > box[axis + 3]) box[axis + 3] = at[axis];
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            box = null;
            notRead.Add("section box: it could not be read (" + why(ex) + "), so nothing was left out by it - "
                + "only Revit's own view filter applied");
        }
    }

    // ---- the elements in scope -----------------------------------------------

    var links = 0;
    try
    {
        foreach (var link in linkCollector.OfClass(typeof(RevitLinkInstance))) if (link != null) links++;
    }
    catch (Exception ex) { notRead.Add("links: they could not be counted (" + why(ex) + "), and none is drawn"); links = 0; }
    if (links > 0)
        notRead.Add("links: " + whole(links) + " Revit link placement(s) in scope are not drawn - a link is another "
            + "model, and its walls, floors and rooms are its own");

    collector.WhereElementIsNotElementType();
    if (wanted.Count > 0) collector.WherePasses(new ElementMulticategoryFilter(wanted));

    var candidates = new List<Element>();
    var sizes = new List<double>();
    var inScope = new Dictionary<string, int>();      // category id -> elements in scope
    var cad = 0;
    var spatial = 0;
    var outsideBox = 0;
    var unsorted = 0;
    string firstUnsorted = null;
    foreach (var element in collector)
    {
        if (element == null) continue;
        try
        {
            if (element is RevitLinkInstance || element is Level || element.ViewSpecific) continue;
            if (element is ImportInstance) { cad++; continue; }
            var category = element.Category;
            if (category == null || category.CategoryType != CategoryType.Model) continue;
            if (element is SpatialElement) { spatial++; continue; }
            if (element is Group || element is AssemblyInstance) continue;
            var extent = element.get_BoundingBox(null);
            if (extent == null || extent.Min == null || extent.Max == null) continue;
            if (box != null
                && (mmOf(extent.Max.X) < box[0] || mmOf(extent.Min.X) > box[3]
                 || mmOf(extent.Max.Y) < box[1] || mmOf(extent.Min.Y) > box[4]
                 || mmOf(extent.Max.Z) < box[2] || mmOf(extent.Min.Z) > box[5]))
            {
                outsideBox++;
                continue;
            }
            var dx = extent.Max.X - extent.Min.X;
            var dy = extent.Max.Y - extent.Min.Y;
            var dz = extent.Max.Z - extent.Min.Z;
            var key = category.Id.ToString();
            candidates.Add(element);
            sizes.Add(Math.Sqrt(dx * dx + dy * dy + dz * dz));
            inScope[key] = (inScope.ContainsKey(key) ? inScope[key] : 0) + 1;
        }
        catch (Exception ex)
        {
            unsorted++;
            if (firstUnsorted == null) firstUnsorted = element.Id.ToString() + " - " + why(ex);
        }
    }
    if (unsorted > 0)
        notRead.Add("elements: " + whole(unsorted) + " could not be looked at and are not drawn - the first, "
            + firstUnsorted);
    if (cad > 0)
        notRead.Add("CAD: " + whole(cad) + " CAD import(s) or link(s) in scope are not drawn");
    if (spatial > 0)
        notRead.Add("rooms and spaces: " + whole(spatial) + " room(s), space(s) or area(s) in scope are not drawn - "
            + "they are volumes, and a 3D view does not show them");

    // Each category asked for: one this model has not got, or one that is not
    // a model category, is said - never an empty drawing nobody can explain.
    for (var i = 0; i < wanted.Count; i++)
    {
        Category asked = null;
        try { asked = Category.GetCategory(doc, wanted[i]); } catch (Exception) { asked = null; }
        if (asked == null)
            notRead.Add("categories: " + wantedNames[i] + " - this model has no such category");
        else if (asked.CategoryType != CategoryType.Model)
            notRead.Add("categories: " + asked.Name + " (" + wantedNames[i] + ") is not a model category, so "
                + "nothing of it is drawn");
        else if (!inScope.ContainsKey(asked.Id.ToString()))
            lines.Add(asked.Name + " (" + wantedNames[i] + "): nothing of it in the " + scope + ".");
    }

    // Longest extent first; the order they were found breaks a tie.
    var order = new List<int>();
    for (var i = 0; i < candidates.Count; i++) order.Add(i);
    order.Sort((a, b) =>
    {
        var bySize = sizes[b].CompareTo(sizes[a]);
        return bySize != 0 ? bySize : a.CompareTo(b);
    });

    // ---- the geometry --------------------------------------------------------

    var firstOptions = new Options();
    if (useView) firstOptions.View = view3d;
    else firstOptions.DetailLevel = ViewDetailLevel.Coarse;
    firstOptions.ComputeReferences = false;
    var fineOptions = new Options();
    fineOptions.DetailLevel = ViewDetailLevel.Fine;
    fineOptions.ComputeReferences = false;

    // One element's points (whole millimetres, three a point), its triangles
    // (three point indexes each), and the point each millimetre position is.
    List<long> points = null;
    List<int> corners = null;
    Dictionary<string, int> pointAt = null;
    var sawLine = false;
    var facesSkipped = 0;
    string firstFace = null;

    Action<Mesh> addMesh = mesh =>
    {
        if (mesh == null || mesh.NumTriangles == 0) return;
        var vertices = mesh.Vertices;
        var map = new int[vertices.Count];
        for (var i = 0; i < vertices.Count; i++)
        {
            var p = vertices[i];
            long x = mmOf(p.X), y = mmOf(p.Y), z = mmOf(p.Z);
            var key = whole(x) + "," + whole(y) + "," + whole(z);
            int index;
            if (!pointAt.TryGetValue(key, out index))
            {
                index = points.Count / 3;
                points.Add(x);
                points.Add(y);
                points.Add(z);
                pointAt[key] = index;
            }
            map[i] = index;
        }
        for (var i = 0; i < mesh.NumTriangles; i++)
        {
            var triangle = mesh.get_Triangle(i);
            var a = map[(int)triangle.get_Index(0)];
            var b = map[(int)triangle.get_Index(1)];
            var c = map[(int)triangle.get_Index(2)];
            if (a == b || b == c || a == c) continue;   // a line at the millimetre
            corners.Add(a);
            corners.Add(b);
            corners.Add(c);
        }
    };

    Action<GeometryElement> walk = null;
    walk = geometry =>
    {
        if (geometry == null) return;
        foreach (GeometryObject item in geometry)
        {
            if (item == null) continue;
            var solid = item as Solid;
            if (solid != null)
            {
                if (solid.Faces.Size == 0) continue;
                foreach (Face face in solid.Faces)
                {
                    if (face == null) continue;
                    Mesh mesh = null;
                    try { mesh = face.Triangulate(); }
                    catch (Exception ex)
                    {
                        facesSkipped++;
                        if (firstFace == null) firstFace = why(ex);
                        continue;
                    }
                    addMesh(mesh);
                }
                continue;
            }
            var asMesh = item as Mesh;
            if (asMesh != null) { addMesh(asMesh); continue; }
            var instance = item as GeometryInstance;
            if (instance != null) { walk(instance.GetInstanceGeometry()); continue; }
            var nested = item as GeometryElement;
            if (nested != null) { walk(nested); continue; }
            if (item is Curve || item is PolyLine) sawLine = true;
        }
    };

    var drawnIds = new List<string>();
    var drawnCategory = new List<string>();
    var drawnPoints = new List<long[]>();
    var drawnCorners = new List<int[]>();
    var categoryName = new Dictionary<string, string>();
    var noGeometry = 0;
    var leftOut = 0;
    var readAtFine = 0;
    var failed = 0;
    string firstFailed = null;
    var stopped = false;

    foreach (var at in order)
    {
        if (stopped) { leftOut++; continue; }
        var element = candidates[at];
        points = new List<long>();
        corners = new List<int>();
        pointAt = new Dictionary<string, int>();
        sawLine = false;
        try
        {
            walk(element.get_Geometry(firstOptions));
            if (corners.Count == 0 && sawLine)
            {
                points = new List<long>();
                corners = new List<int>();
                pointAt = new Dictionary<string, int>();
                walk(element.get_Geometry(fineOptions));
                if (corners.Count > 0) readAtFine++;
            }
        }
        catch (Exception ex)
        {
            failed++;
            if (firstFailed == null) firstFailed = element.Id.ToString() + " - " + why(ex);
            noGeometry++;
            continue;
        }
        if (corners.Count == 0) { noGeometry++; continue; }

        var count = corners.Count / 3;
        if ((long)triangles + count > maxTriangles)
        {
            stopped = true;
            leftOut++;
            continue;
        }
        triangles += count;

        var category = element.Category;
        var key = category.Id.ToString();
        if (!categoryName.ContainsKey(key)) categoryName[key] = category.Name;
        drawnIds.Add(element.Id.ToString());
        drawnCategory.Add(key);
        drawnPoints.Add(points.ToArray());
        drawnCorners.Add(corners.ToArray());
    }
    if (failed > 0)
        notRead.Add("geometry: " + whole(failed) + " element(s) would not give their geometry and are counted in "
            + "noGeometry - the first, " + firstFailed);
    if (facesSkipped > 0)
        notRead.Add("faces: " + whole(facesSkipped) + " face(s) would not triangulate and are missing from their "
            + "element - the first, " + firstFace);
    if (leftOut > 0)
        capped.Add("triangles: the budget of " + whole(maxTriangles) + " was reached; " + whole(leftOut)
            + " element(s) left out - the largest were drawn first");

    // ---- the offset: the centre of the box round everything drawn ------------

    var low = new long[] { long.MaxValue, long.MaxValue, long.MaxValue };
    var high = new long[] { long.MinValue, long.MinValue, long.MinValue };
    foreach (var pts in drawnPoints)
    {
        for (var i = 0; i < pts.Length; i++)
        {
            var axis = i % 3;
            if (pts[i] < low[axis]) low[axis] = pts[i];
            if (pts[i] > high[axis]) high[axis] = pts[i];
        }
    }
    var offset = new long[3];
    if (drawnPoints.Count > 0)
        for (var axis = 0; axis < 3; axis++) offset[axis] = (low[axis] + high[axis]) / 2;

    // Categories by name, each once; an element points at its own.
    var names = new List<string>();
    foreach (var name in categoryName.Values) if (!names.Contains(name)) names.Add(name);
    names.Sort(StringComparer.OrdinalIgnoreCase);
    var indexOf = new Dictionary<string, int>();
    foreach (var pair in categoryName) indexOf[pair.Key] = names.IndexOf(pair.Value);

    // ---- the JSON ------------------------------------------------------------

    var json = new System.Text.StringBuilder();
    json.Append("{").Append(esc("format")).Append(":1");
    json.Append(",").Append(esc("scope")).Append(":").Append(esc(scope));
    json.Append(",").Append(esc("units")).Append(":").Append(esc("m"));
    json.Append(",").Append(esc("offset")).Append(":[")
        .Append(metres(offset[0])).Append(",").Append(metres(offset[1])).Append(",").Append(metres(offset[2])).Append("]");
    json.Append(",").Append(esc("categories")).Append(":").Append(textList(names));
    json.Append(",").Append(esc("elements")).Append(":[");
    for (var e = 0; e < drawnIds.Count; e++)
    {
        if (e > 0) json.Append(",");
        json.Append("{").Append(esc("id")).Append(":").Append(esc(drawnIds[e]));
        json.Append(",").Append(esc("c")).Append(":").Append(whole(indexOf[drawnCategory[e]]));
        json.Append(",").Append(esc("v")).Append(":[");
        var pts = drawnPoints[e];
        for (var i = 0; i < pts.Length; i++)
        {
            if (i > 0) json.Append(",");
            json.Append(metres(pts[i] - offset[i % 3]));
        }
        json.Append("],").Append(esc("t")).Append(":[");
        var tri = drawnCorners[e];
        for (var i = 0; i < tri.Length; i++)
        {
            if (i > 0) json.Append(",");
            json.Append(whole(tri[i]));
        }
        json.Append("]}");
    }
    json.Append("]");
    json.Append(",").Append(esc("triangles")).Append(":").Append(whole(triangles));
    json.Append(",").Append(esc("elementsRead")).Append(":").Append(whole(candidates.Count));
    json.Append(",").Append(esc("noGeometry")).Append(":").Append(whole(noGeometry));
    json.Append(",").Append(esc("leftOut")).Append(":").Append(whole(leftOut));
    json.Append(",").Append(esc("notRead")).Append(":").Append(textList(notRead));
    json.Append(",").Append(esc("capped")).Append(":").Append(textList(capped));
    json.Append("}");
    geometryJson = json.ToString();

    // ---- the findings --------------------------------------------------------

    lines.Insert(0, "Read the " + scope + ": " + whole(drawnIds.Count) + " element(s) drawn in " + whole(triangles)
        + " triangle(s), in metres, centred on " + metres(offset[0]) + ", " + metres(offset[1]) + ", "
        + metres(offset[2]) + " m.");
    lines.Insert(1, whole(candidates.Count) + " model element(s) in scope: " + whole(drawnIds.Count) + " drawn, "
        + whole(noGeometry) + " with nothing to draw, " + whole(leftOut) + " left out by the budget of "
        + whole(maxTriangles) + ".");
    if (readAtFine > 0)
        lines.Add(whole(readAtFine) + " element(s) were only lines at " + (useView ? "the view's detail level" : "Coarse")
            + " - pipes, ducts and their fittings draw that way - and were read again at Fine and drawn as solids.");
    if (outsideBox > 0)
        lines.Add(whole(outsideBox) + " element(s) Revit passed for the view lie wholly outside its section box and "
            + "were not drawn.");
    foreach (var line in notRead) lines.Add("Not read - " + line);
    foreach (var line in capped) lines.Add("Cut - " + line);
    findings = string.Join("\n", lines.ToArray());
}
