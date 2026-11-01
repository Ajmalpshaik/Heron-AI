// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `spotType`, `at` and
// `leaderOffsetMm` are in scope, and leaves `placed`, `newSpots`, `notPlaced`,
// `typesLoaded`, `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One view of spots is one undo
// entry, and the caller owns it; nothing here opens or commits one.
//
// REVIT'S ANNOTATE > SPOT COORDINATE. doc.Create.NewSpotCoordinate(View,
// Reference, XYZ origin, XYZ bend, XYZ end, XYZ refPt, bool hasLeader), the
// same signature 2020 to 2027 (tools/api-surface --members, 2026-11-01).
//
// THE REFERENCE IS TAKEN THE WAY ADD_SPOT_ELEVATIONS TAKES IT - Options with
// ComputeReferences and Fine detail, every planar face with a reference
// collected first, never first-one-wins - and the face is chosen by a POSITIVE
// test: its normal points AT THE VIEWER (dot with the view direction above
// 0.9). In a plan that is the top, never the soffit; Math.Abs would let the
// soffit through. An element with no such face is named and skipped.
//
// THE SPOT TYPE IS THE MODELLER'S AND IS NEVER GUESSED (D-33). It decides which
// origin the north and east are measured from. Left out, or a name no spot
// coordinate type has: NOTHING IS PLACED and the loaded types are listed.
//
// VIEWS REVIT CANNOT ANNOTATE ARE REFUSED BY NAME before anything is placed:
// only plans, ceiling, structural and area plans, sections, elevations, detail
// views, and a 3D view that is locked and not a perspective.
//
// GRID INTERSECTIONS. Two or more straight grids in the selection: each place
// two cross is a point, and the spot goes on the highest upward face of the
// other selected elements under it. Plans only.
//
// EVERY SPOT IS READ BACK - its type, its view, and the text Revit shows - and
// one that reads back wrong is removed again inside the same undo.

const double MillimetresPerFoot = 304.8;
const double OneMillimetre = 1.0 / MillimetresPerFoot;

var placed = 0;
var newSpots = new List<ElementId>();
var notPlaced = new List<ElementId>();
var typesLoaded = new List<string>();
var lines = new List<string>();
var findings = new List<string>();
string refused = "";

var culture = System.Globalization.CultureInfo.InvariantCulture;
var spotCategory = new ElementId(BuiltInCategory.OST_SpotCoordinates);

Func<double, string> mm = v => Math.Round(v * MillimetresPerFoot, 1).ToString("0.#", culture);
Func<XYZ, string> atPoint = p => "(" + mm(p.X) + ", " + mm(p.Y) + ", " + mm(p.Z) + ") mm from the internal origin";

// How a type reads in the Properties palette - "Spot Coordinates: Survey Point".
Func<ElementType, string> typeLabel = t =>
{
    if (t == null) return "(no type)";
    string family = null;
    try { family = t.FamilyName; } catch { family = null; }
    return string.IsNullOrEmpty(family) ? t.Name : family + ": " + t.Name;
};

// A type is named as Revit shows it: its own name, or "Family: Name".
Func<ElementType, string, bool> isNamed = (t, text) =>
{
    string own;
    try { own = t.Name; } catch { return false; }
    if (string.Equals(own, text, StringComparison.Ordinal)) return true;
    string family;
    try { family = t.FamilyName; } catch { return false; }
    if (string.IsNullOrEmpty(family)) return false;
    return string.Equals(family + ": " + own, text.Replace(" : ", ": "), StringComparison.Ordinal);
};

var coordinateTypes = new FilteredElementCollector(doc)
    .OfClass(typeof(SpotDimensionType))
    .Cast<SpotDimensionType>()
    .Where(t =>
    {
        try { return t.StyleType == DimensionStyleType.SpotCoordinate; }
        catch { return false; }
    })
    .OrderBy(t => typeLabel(t), StringComparer.Ordinal)
    .ToList();
typesLoaded.Add("Spot coordinate types: " + (coordinateTypes.Count == 0
    ? "none in this model - make one under Annotate > Dimension > Spot Coordinate Types first"
    : string.Join(", ", coordinateTypes.Select(t => "'" + typeLabel(t) + "'").ToArray())));

var typeText = (spotType ?? "").Trim();
var atText = (at ?? "").Trim().ToLowerInvariant();
var wantCorners = atText == "corners" || atText == "corner";
SpotDimensionType chosenType = null;

// ---- refused before anything is placed -------------------------------------

string viewProblem = null;
if (view != null && !view.IsTemplate)
{
    var kind = view.ViewType;
    if (kind == ViewType.ThreeD)
    {
        var view3d = view as View3D;
        if (view3d == null || view3d.IsPerspective)
            viewProblem = "\"" + view.Name + "\" is a perspective 3D view, and Revit puts no spot coordinate in a "
                + "perspective. Name a plan, a section, an elevation or a locked orthographic 3D view.";
        else if (!view3d.IsLocked)
            viewProblem = "\"" + view.Name + "\" is a 3D view that is not locked. Revit annotates a 3D view only "
                + "once its orientation is saved and locked (the view cube's Save Orientation and Lock View). "
                + "Lock it, or name a plan, a section or an elevation.";
    }
    else if (kind != ViewType.FloorPlan && kind != ViewType.CeilingPlan && kind != ViewType.EngineeringPlan
        && kind != ViewType.AreaPlan && kind != ViewType.Section && kind != ViewType.Elevation
        && kind != ViewType.Detail)
    {
        viewProblem = "\"" + view.Name + "\" is a " + kind + " view, and Revit places a spot coordinate only in "
            + "a plan, a section, an elevation, a detail view or a locked 3D view - not in a schedule, a sheet, "
            + "a legend or a drafting view. Name one of those.";
    }
}

if (view == null)
{
    refused = "No view was named. Spot coordinates go in one view; name it as the Project Browser shows it. "
            + "Nothing was placed.";
}
else if (view.IsTemplate)
{
    refused = "\"" + view.Name + "\" is a view template, and a template holds no annotation. Name the view "
            + "itself. Nothing was placed.";
}
else if (viewProblem != null)
{
    refused = viewProblem + " Nothing was placed.";
}
else if (atText.Length > 0 && !wantCorners && atText != "centre" && atText != "center" && atText != "middle")
{
    refused = "'" + at + "' is not a place on the element this knows - say \"centre\" for one spot in the "
            + "middle of each, or \"corners\" for one at each corner. Nothing was placed.";
}
else if (typeText.Length == 0 || typeText.Equals("none", StringComparison.OrdinalIgnoreCase))
{
    refused = "No spot coordinate type was named, and one is never chosen for you - the type decides which "
            + "origin the north and east are measured from. The loaded ones are in typesLoaded. Nothing was placed.";
}
else
{
    var hits = coordinateTypes.Where(t => isNamed(t, typeText)).ToList();
    if (hits.Count == 1)
    {
        chosenType = hits[0];
    }
    else if (hits.Count > 1)
    {
        refused = hits.Count + " spot coordinate types are called '" + typeText + "' - "
                + string.Join(", ", hits.Select(t => "'" + typeLabel(t) + "'").ToArray())
                + " - so the name does not say which. Name it with its family. Nothing was placed.";
    }
    else
    {
        // A spot ELEVATION or SLOPE type by that name is named as such, so the
        // modeller sees why it cannot do.
        var elsewhere = new FilteredElementCollector(doc)
            .OfClass(typeof(SpotDimensionType))
            .Cast<SpotDimensionType>()
            .Where(t => isNamed(t, typeText))
            .Select(t => "'" + typeLabel(t) + "' (" + t.StyleType + ")")
            .Distinct()
            .Take(3)
            .ToList();
        refused = elsewhere.Count > 0
            ? "'" + typeText + "' is " + string.Join(", ", elsewhere.ToArray()) + ", not a spot coordinate "
              + "type. The loaded ones are in typesLoaded. Nothing was placed."
            : "No spot coordinate type is called '" + typeText + "' - the name has to match the Project "
              + "Browser, capitals included. The loaded ones are in typesLoaded. Nothing was placed.";
    }
}

// Grids switch to intersections - plans only. Checked before anything is placed.
var grids = new List<Grid>();
var others = new List<Element>();
var seen = new HashSet<ElementId>();
foreach (var element in elements ?? new List<Element>())
{
    if (element == null || !seen.Add(element.Id)) continue;
    var grid = element as Grid;
    if (grid != null) grids.Add(grid); else others.Add(element);
}
var gridMode = grids.Count >= 2;

if (refused.Length == 0 && seen.Count == 0)
{
    refused = "Nothing was selected to put a spot coordinate on. Select the columns, floors or other elements "
            + "- or two or more grids and the floor under them. Nothing was placed.";
}
if (refused.Length == 0 && gridMode && view.ViewDirection.Z < 0.9)
{
    refused = "Grids were selected, and grid intersections are found only in a plan, looking down - \""
            + view.Name + "\" is a " + view.ViewType + " view. Name a plan. Nothing was placed.";
}
if (refused.Length == 0 && gridMode && others.Count == 0)
{
    refused = "Only grids were selected. A grid has no face to put a spot coordinate on, so the spot goes on "
            + "the floor or element under each intersection - select that too. Nothing was placed.";
}

if (refused.Length == 0)
{
    var offset = (leaderOffsetMm <= 0 ? 300.0 : leaderOffsetMm) / MillimetresPerFoot;
    var toViewer = view.ViewDirection;
    var right = view.RightDirection;
    var up = view.UpDirection;

    bool hiddenHere = false;
    try { hiddenHere = view.GetCategoryHidden(spotCategory); } catch { }

    // Spot coordinates already in this view, so a re-run stacks nothing.
    var already = new List<XYZ>();
    foreach (var element in new FilteredElementCollector(doc, view.Id).OfClass(typeof(SpotDimension)))
    {
        var spot = element as SpotDimension;
        if (spot == null || spot.Category == null || spot.Category.Id != spotCategory) continue;
        try { if (spot.Origin != null) already.Add(spot.Origin); } catch { }
    }

    var options = new Options();
    options.ComputeReferences = true;
    options.DetailLevel = ViewDetailLevel.Fine;

    // EVERY planar face with a reference, from the element and from inside its
    // family instance - collected before one is chosen.
    Func<Element, List<PlanarFace>> facesOf = element =>
    {
        var found = new List<PlanarFace>();
        GeometryElement geometry = null;
        try { geometry = element.get_Geometry(options); } catch { geometry = null; }
        if (geometry == null) return found;
        var solids = new List<Solid>();
        foreach (GeometryObject top in geometry)
        {
            var solid = top as Solid;
            if (solid != null) { solids.Add(solid); continue; }
            var nested = top as GeometryInstance;
            if (nested == null) continue;
            foreach (GeometryObject inner in nested.GetInstanceGeometry())
            {
                var innerSolid = inner as Solid;
                if (innerSolid != null) solids.Add(innerSolid);
            }
        }
        foreach (var solid in solids)
        {
            if (solid.Faces.Size == 0) continue;
            foreach (Face face in solid.Faces)
            {
                var planar = face as PlanarFace;
                if (planar == null || face.Reference == null) continue;
                found.Add(planar);
            }
        }
        return found;
    };

    Func<PlanarFace, UV> middleOf = face =>
    {
        var box = face.GetBoundingBox();
        return box.Min.Add(box.Max).Divide(2.0);
    };

    // POSITIVE test - the normal points at the viewer. Nearest such face wins.
    Func<List<PlanarFace>, PlanarFace> facingViewer = faces =>
    {
        PlanarFace best = null;
        double bestDepth = double.NegativeInfinity;
        foreach (var face in faces)
        {
            if (face.FaceNormal.DotProduct(toViewer) <= 0.9) continue;
            var depth = face.Evaluate(middleOf(face)).DotProduct(toViewer);
            if (best == null || depth > bestDepth) { best = face; bestDepth = depth; }
        }
        return best;
    };

    var alreadyCount = 0;
    var refusedByRevit = 0;
    var readWrong = 0;
    var noText = 0;
    string firstRefusal = null;

    // One spot, placed, typed, read back. Returns what to say about it.
    Func<Reference, XYZ, string, string> placeOne = (reference, point, label) =>
    {
        foreach (var there in already)
            if (there.DistanceTo(point) < OneMillimetre)
            {
                alreadyCount++;
                return label + ": a spot coordinate is already there in this view - left alone.";
            }

        SpotDimension made = null;
        try
        {
            var bend = point.Add(right.Add(up).Multiply(offset));
            var end = bend.Add(right.Multiply(offset));
            made = doc.Create.NewSpotCoordinate(view, reference, point, bend, end, point, true);
        }
        catch (Exception failure)
        {
            if (firstRefusal == null) firstRefusal = failure.Message;
            refusedByRevit++;
            return label + ": Revit placed no spot coordinate - " + failure.Message;
        }
        if (made == null)
        {
            refusedByRevit++;
            return label + ": Revit placed no spot coordinate.";
        }

        string wrong = null;
        string shows = null;
        try
        {
            if (made.GetTypeId() != chosenType.Id) made.SpotDimensionType = chosenType;
            doc.Regenerate();
            if (made.GetTypeId() != chosenType.Id) wrong = "it carries another type";
            else if (made.OwnerViewId != view.Id) wrong = "it belongs to another view";
            else shows = made.ValueString;
        }
        catch (Exception failure) { wrong = "it could not be read back - " + failure.Message; }

        if (wrong != null)
        {
            // Ours, made a moment ago, and wrong - removed.
            try { doc.Delete(made.Id); } catch { }
            readWrong++;
            return label + ": the spot read back wrong - " + wrong + " - and was removed.";
        }

        placed++;
        newSpots.Add(made.Id);
        already.Add(point);
        if (string.IsNullOrWhiteSpace(shows))
        {
            noText++;
            return label + ": spot " + made.Id + " placed at " + atPoint(point) + "; Revit handed back no text for it "
                + "- read the north and east on the drawing.";
        }
        return label + ": spot " + made.Id + " placed, showing \""
            + shows.Replace("\r\n", " / ").Replace("\n", " / ").Replace("\r", " / ").Trim()
            + "\", at " + atPoint(point) + ".";
    };

    Func<Element, string> describe = element =>
    {
        var name = element.Category != null ? element.Category.Name : "element";
        return name + " " + element.Id;
    };

    if (!gridMode)
    {
        foreach (var element in others.Concat(grids.Cast<Element>()))
        {
            if (element is Grid)
            {
                notPlaced.Add(element.Id);
                lines.Add("grid " + element.Name + ": a grid on its own has no face and crosses nothing - select "
                    + "two or more grids and the floor under them. Left alone.");
                continue;
            }

            bool inThisModel = false;
            try { inThisModel = element.Document != null && element.Document.Equals(doc); } catch { }
            if (!inThisModel)
            {
                notPlaced.Add(element.Id);
                lines.Add(describe(element) + ": of another model - left alone.");
                continue;
            }

            var face = facingViewer(facesOf(element));
            if (face == null)
            {
                notPlaced.Add(element.Id);
                lines.Add(describe(element) + ": no face of it looks at the viewer in \"" + view.Name + "\", so "
                    + "there is nothing here to put a spot coordinate on. Left alone.");
                continue;
            }

            var points = new List<XYZ>();
            if (wantCorners)
            {
                foreach (EdgeArray loop in face.EdgeLoops)
                    foreach (Edge edge in loop)
                    {
                        var line = edge.AsCurve() as Line;
                        if (line == null) continue;
                        for (var end = 0; end < 2; end++)
                        {
                            var corner = line.GetEndPoint(end);
                            if (!points.Any(p => p.DistanceTo(corner) < OneMillimetre)) points.Add(corner);
                        }
                    }
                if (points.Count == 0)
                {
                    notPlaced.Add(element.Id);
                    lines.Add(describe(element) + ": its face has no straight edge, so it has no corners - ask for "
                        + "its centre instead. Left alone.");
                    continue;
                }
            }
            else
            {
                var middle = middleOf(face);
                if (!face.IsInside(middle))
                {
                    notPlaced.Add(element.Id);
                    lines.Add(describe(element) + ": the middle of its face is not on the face (an L or a ring "
                        + "shape) - ask for its corners instead. Left alone.");
                    continue;
                }
                points.Add(face.Evaluate(middle));
            }

            var before = placed;
            for (var i = 0; i < points.Count; i++)
            {
                var label = describe(element) + (wantCorners ? " corner " + (i + 1) : "");
                lines.Add(placeOne(face.Reference, points[i], label));
            }
            if (placed == before) notPlaced.Add(element.Id);
        }
    }
    else
    {
        // Each host's upward faces, once.
        var hostFaces = new List<PlanarFace>();
        foreach (var host in others)
        {
            bool inThisModel = false;
            try { inThisModel = host.Document != null && host.Document.Equals(doc); } catch { }
            if (!inThisModel)
            {
                notPlaced.Add(host.Id);
                lines.Add(describe(host) + ": of another model - not used.");
                continue;
            }
            foreach (var face in facesOf(host))
                if (face.FaceNormal.DotProduct(toViewer) > 0.9) hostFaces.Add(face);
        }

        var straight = new List<Grid>();
        foreach (var grid in grids)
        {
            if (grid.Curve is Line) straight.Add(grid);
            else lines.Add("grid " + grid.Name + ": curved - where it crosses the others is not worked out here.");
        }

        var crossings = 0;
        for (var i = 0; i < straight.Count; i++)
            for (var j = i + 1; j < straight.Count; j++)
            {
                var a = (Line)straight[i].Curve;
                var b = (Line)straight[j].Curve;
                var p = a.GetEndPoint(0);
                var r = a.GetEndPoint(1).Subtract(p);
                var q = b.GetEndPoint(0);
                var s = b.GetEndPoint(1).Subtract(q);
                var across = r.X * s.Y - r.Y * s.X;
                if (Math.Abs(across) < 1e-9) continue;   // parallel - they never cross
                var qp = q.Subtract(p);
                var t = (qp.X * s.Y - qp.Y * s.X) / across;
                var u = (qp.X * r.Y - qp.Y * r.X) / across;
                var slackA = OneMillimetre / Math.Max(Math.Sqrt(r.X * r.X + r.Y * r.Y), OneMillimetre);
                var slackB = OneMillimetre / Math.Max(Math.Sqrt(s.X * s.X + s.Y * s.Y), OneMillimetre);
                if (t < -slackA || t > 1 + slackA || u < -slackB || u > 1 + slackB) continue;   // they stop short
                crossings++;

                var x = p.X + t * r.X;
                var y = p.Y + t * r.Y;
                var label = "grid " + straight[i].Name + "/" + straight[j].Name;

                PlanarFace under = null;
                XYZ onFace = null;
                foreach (var face in hostFaces)
                {
                    IntersectionResult hit = null;
                    try { hit = face.Project(new XYZ(x, y, face.Evaluate(middleOf(face)).Z)); } catch { hit = null; }
                    if (hit == null || !face.IsInside(hit.UVPoint)) continue;
                    var spot = hit.XYZPoint;
                    if (Math.Abs(spot.X - x) > OneMillimetre || Math.Abs(spot.Y - y) > OneMillimetre) continue;
                    if (onFace == null || spot.Z > onFace.Z) { under = face; onFace = spot; }
                }
                if (under == null)
                {
                    lines.Add(label + ": nothing selected has an upward face under it - left alone.");
                    continue;
                }
                lines.Add(placeOne(under.Reference, onFace, label));
            }
        if (crossings == 0)
            lines.Add("The " + straight.Count + " straight grid(s) selected do not cross one another, so there was "
                + "no intersection to mark.");
    }

    // THE WHOLE ANSWER AS ONE LINE - every count in words.
    findings.Add(placed + " spot coordinate(s) placed in \"" + view.Name + "\" as \"" + typeLabel(chosenType)
        + "\", " + (gridMode ? "at grid intersections" : wantCorners ? "at the corners" : "at the centre")
        + ". Not placed: " + alreadyCount + " already shown there, " + refusedByRevit + " refused by Revit, "
        + readWrong + " read back wrong and removed, " + notPlaced.Count + " element(s) given none.");

    if (noText > 0)
        findings.Add(noText + " spot(s) placed gave back no text through the API, so their north and east are "
            + "not printed here - read them on the drawing. They depend on the type's origin setting, which "
            + "only Revit applies.");
    if (hiddenHere && placed > 0)
        findings.Add("Spot Coordinates are turned off in \"" + view.Name + "\" - in its Visibility/Graphics or its "
            + "view template - so the " + placed + " spot(s) placed are in the model and not shown in that view.");
    if (firstRefusal != null)
        findings.Add("Revit's first refusal said: " + firstRefusal);
}
else
{
    lines.Add(refused);
}

var report = string.Join(" | ", lines);
findings.AddRange(lines);
