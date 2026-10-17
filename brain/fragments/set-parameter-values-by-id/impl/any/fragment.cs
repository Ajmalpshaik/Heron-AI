// NOT STANDALONE. Assumes `doc`, `rows` and `keepFace` are in scope, and
// leaves `written`, `resultJson`, `stale`, `refused` and `findings` behind.
// `keepFace` is version 3's and the card reads it as "none" when it is left
// out - see "keep a face" below.
//
// ASSUMES AN OPEN TRANSACTION. It does not start one: the operation's
// TransactionGroup makes the whole Apply one entry in Revit's undo list.
//
// TWO WAYS TO NAME THE ELEMENTS, AND THE FIRST ROW SAYS WHICH (version 2).
//
//   BY ID - version 1, for the Companion's table and the loads Finalize:
//   rows `id|uniqueId|parameter|was|new`. Its code below is unchanged but
//   for two `keyed` guards that make it step aside.
//
//   BY ANY PARAMETER VALUE - a room's Number, a sheet's Sheet Number, a
//   Mark: a first row `key=Number|in=Rooms`, then `5|Name=Lift 02` for each
//   element. A caller cannot get element ids out of a reply - a list stops
//   at three - so giving 13 rooms 13 names took 26 calls, a find and a write
//   per room (FRAGMENT-ISSUES 5b-459). A key that names no element, or more
//   than one, refuses THAT ROW by name and never guesses.
//
// BY ID: TWO PASSES, AND THE FIRST WRITES NOTHING. Every row is checked
// against the model before any row is written - still there, the same
// element, the parameter there once, writable, and still holding the value
// the table SHOWED. One failure and nothing is written at all (Article 12c;
// the owner's answer to docs/40 section 18 question 2). A half-applied table
// is the "preview that expired" the Constitution forbids.
//
// THE BACKSLASH AND THE QUOTE ARE BUILT FROM THEIR CHARACTER CODES; see
// read-element-table for why.

var written = 0;
var stale = new List<string>();
var refused = new List<string>();
var findings = new List<string>();
var resultJson = "";

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

Func<Parameter, string> readValue = p =>
{
    string text = null;
    try { text = p.AsValueString(); } catch { }
    if (string.IsNullOrWhiteSpace(text))
    {
        try { text = p.AsString(); } catch { }
    }
    return text;
};

Func<string, string> same = text => (text ?? "").Trim();

// WHAT COUNTS AS "STILL THE SAME". Text is compared EXACTLY - a Comment
// changed in Revit from "A" to " A " is a change, and trimming would have
// written over it (Codex review of #362). A number, a length or a tick box
// is compared by its display string trimmed, because that is the form both
// sides read it in.
Func<Parameter, string, bool> unchanged = (p, was) =>
    p.StorageType == StorageType.String
        ? (readValue(p) ?? "") == (was ?? "")
        : same(readValue(p)) == same(was);

// A TICK BOX IS FOUND BY WHAT IT IS, NOT BY WHAT IT SAYS. Version 1 looked for
// the display words "Yes" or "No", which a Revit in another language does not
// show. This is write-element-parameters' own version-safe check, the same
// text, so the two cannot disagree about which parameter is a Yes/No.
// SpecTypeId.Boolean.YesNo on 2022 and later; null on 2020 and 2021, which
// answer through ParameterType instead.
var yesNoSpecs = typeof(Document).Assembly.GetType(typeof(Document).Namespace + ".SpecTypeId+Boolean");
var yesNoProperty = yesNoSpecs == null ? null : yesNoSpecs.GetProperty("YesNo",
    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
var yesNoSpec = yesNoProperty == null ? null : yesNoProperty.GetValue(null, null);

Func<Parameter, bool> isYesNo = p =>
{
    if (p.StorageType != StorageType.Integer) return false;
    try
    {
        var definition = p.Definition;
        if (definition == null) return false;

        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null && yesNoSpec != null)
        {
            var spec = getDataType.Invoke(definition, null);
            if (spec == null) return false;
            // NameEquals, because a spec id carries its version -
            // "...bool-1.0.0" - and a parameter made under another version
            // must still read as a tick box.
            var nameEquals = spec.GetType().GetMethod("NameEquals", new[] { yesNoSpec.GetType() });
            return nameEquals != null
                ? (bool)nameEquals.Invoke(spec, new[] { yesNoSpec })
                : spec.Equals(yesNoSpec);
        }

        var parameterType = definition.GetType().GetProperty("ParameterType");
        var kind = parameterType == null ? null : parameterType.GetValue(definition, null);
        return kind != null && kind.ToString() == "YesNo";
    }
    catch (Exception)
    {
        // Not known to be a tick box, so it takes the version 3 road, where
        // SetValueString refused the two Yes/No values measured rather than
        // writing them wrongly.
        return false;
    }
};

var ticked = new[] { "yes", "true", "on", "1", "ticked" };
var unticked = new[] { "no", "false", "off", "0", "unticked" };

// ONE NAME, SEVERAL PARAMETERS - AND WHEN ONLY ONE OF THEM CAN BE WRITTEN, THAT
// ONE IS MEANT. A sheet carries its own Sheet Number and, being a view, a second
// read-only "Sheet Number" saying which sheet it sits on; version 1 refused
// both, so no sheet number could be edited (the owner, 2026-09-29, on the
// first table he opened). D-54 s3 forbids GUESSING between parameters that
// could each be written; when every one but one is read-only there is nothing
// to guess, because a write can only land on that one. Two or more writable
// still refuses, exactly as before (FRAGMENT-ISSUES 5b-203).
Func<Element, string, Parameter> theOne = (element, parameterName) =>
{
    var found = element.GetParameters(parameterName);
    if (found.Count == 1) return found[0];
    Parameter writable = null;
    foreach (var candidate in found)
    {
        if (candidate.IsReadOnly) continue;
        if (writable != null) return null;       // two could be written: refuse
        writable = candidate;
    }
    return writable;
};


// THE STORED VALUE, beside the display text. A length shown as "1000 mm" can
// hold two different raw values that round to the same text, and comparing
// only the text would miss a change made in Revit between the two (Codex
// review of #362). Numbers are compared by what Revit stores.
Func<Parameter, string> storedValue = p =>
{
    try
    {
        if (p.StorageType == StorageType.Double)
            return p.AsDouble().ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        if (p.StorageType == StorageType.Integer)
            return p.AsInteger().ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (p.StorageType == StorageType.String) return p.AsString() ?? "";
    }
    catch { }
    return null;
};

// A MEMBER OF A MODEL GROUP whose parameter does not vary across groups: a
// change to it is copied into the same element in every placement of the
// group, so one row would silently edit elements the table never showed
// (Codex review of #362). Such a cell is not editable.
Func<Element, Parameter, bool> groupLocked = (element, p) =>
{
    if (element.GroupId == null || element.GroupId == ElementId.InvalidElementId) return false;
    try
    {
        var definition = p.Definition as InternalDefinition;
        return definition == null || !definition.VariesAcrossGroups;
    }
    catch { return true; }
};

// ------------------------------------------------------------ keep a face
// VERSION 3 (FRAGMENT-ISSUES 5b-610). `keepFace` names ONE face of every
// element written that must stay where it is while the values change: left,
// right, top, bottom, near or far of a named view ("left of Section 3"), or
// "outside", the face looking out of the building. Left out it reads "none"
// (the card's `absent:`), faceKeeper is null, and every line below that asks
// it steps aside: the run is version 2's, statement for statement.
//
// MEASURED, NEVER WORKED OUT FROM A VALUE. Before anything is written the
// face is measured off the element's geometry - its outermost point in that
// direction. After the write and one regeneration it is measured again, the
// element is moved back by exactly the difference, and after a second
// regeneration it is measured a third time. On the first job 13 of 18
// openings held a Wall Width of 255.5 while Revit SHOWED 260 - the project
// rounds to 10 mm - so a move worked out from the shown value was 2.25 mm
// out. Nothing here reads the value to decide a move.
//
// THE SAME TRANSACTION as the write, so Ctrl+Z takes back both at once. A
// face that is not back within 0.1 mm throws, and the operation's
// TransactionGroup takes back every row - all or nothing, as for a value
// Revit turns down after the check.
//
// ONE TOP-LEVEL NAME, as `keyed` was for version 2: every top-level variable
// is printed in the reply, so the whole mode lives inside one object.
var faceKeeper = new[] { keepFace }.Select(asked =>
{
    var said = (asked ?? "").Trim();
    if (said.Length == 0 || said.Equals("none", StringComparison.OrdinalIgnoreCase)) return null;

    var invariant = System.Globalization.CultureInfo.InvariantCulture;
    var metre = 1.0 / 0.3048;
    var tolerance = 0.1 / 304.8;
    Func<double, int, string> plain = (value, places) =>
    {
        var rounded = Math.Round(value, places);
        if (rounded == 0) rounded = 0;   // never "-0"
        return rounded.ToString("0.####", invariant);
    };
    Func<double, string> mmNumber = feet => double.IsNaN(feet) ? "null" : plain(feet * 304.8, 3);
    Func<XYZ, string> facing = d => "x " + plain(d.X, 2) + ", y " + plain(d.Y, 2);

    // ------------------------------------------------- what was asked
    // "<side> of <view>", "<side>: <view>" or "<side> face of <view>".
    var cut = said.IndexOfAny(new[] { ' ', ':', '|' });
    var side = (cut < 0 ? said : said.Substring(0, cut)).Trim().ToLowerInvariant();
    var viewName = cut < 0 ? "" : said.Substring(cut + 1).Trim().TrimStart(':', '|').Trim();
    if (viewName.Equals("face", StringComparison.OrdinalIgnoreCase)) viewName = "";
    if (viewName.StartsWith("face ", StringComparison.OrdinalIgnoreCase)) viewName = viewName.Substring(5).Trim();
    if (viewName.Equals("of", StringComparison.OrdinalIgnoreCase)) viewName = "";
    if (viewName.StartsWith("of ", StringComparison.OrdinalIgnoreCase)) viewName = viewName.Substring(3).Trim();

    string ruleProblem = null;
    XYZ viewToward = null;
    var sides = new[] { "left", "right", "top", "bottom", "near", "far", "outside" };
    if (Array.IndexOf(sides, side) < 0)
    {
        ruleProblem = "keepFace '" + said + "' names no face Heron knows. Say outside, or left, right, top, " +
                      "bottom, near or far of a named view - left of Section 3 - or none.";
    }
    else if (side == "outside" && viewName.Length > 0)
    {
        ruleProblem = "keepFace outside needs no view - the outside is found from the building, not from a view. " +
                      "Say outside on its own, or a side of a view (left of " + viewName + ").";
    }
    else if (side != "outside" && viewName.Length == 0)
    {
        ruleProblem = "keepFace " + side + " needs a view - which face is " + side + " depends on how the model " +
                      "is looked at. Say '" + side + " of <view name>'.";
    }
    else if (side != "outside")
    {
        // A VIEW IS FOUND BY ITS NAME, NEVER A TEMPLATE. A floor plan and a
        // ceiling plan may share a name; that is no guess when both make
        // the asked side point the same way, and refused when they do not.
        Func<View, XYZ> sideOf = v =>
            side == "left" ? v.RightDirection.Negate()
            : side == "right" ? v.RightDirection
            : side == "top" ? v.UpDirection
            : side == "bottom" ? v.UpDirection.Negate()
            : side == "near" ? v.ViewDirection
            : v.ViewDirection.Negate();
        // A view's Name THROWS on a handful of view kinds rather than reading
        // empty (the add-in's own ViewsNamed guards it the same way), and one
        // such view must not stop the run.
        Func<View, string> nameOf = v =>
        {
            try { return v.Name; } catch (Exception) { return null; }
        };
        var named = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
            .Where(v => !v.IsTemplate && string.Equals(nameOf(v), viewName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var inModel = named.Where(v => v.ViewType == ViewType.FloorPlan || v.ViewType == ViewType.CeilingPlan
                                       || v.ViewType == ViewType.EngineeringPlan || v.ViewType == ViewType.AreaPlan
                                       || v.ViewType == ViewType.Elevation || v.ViewType == ViewType.Section
                                       || v.ViewType == ViewType.Detail || v.ViewType == ViewType.ThreeD)
                           .ToList();
        if (named.Count == 0)
            ruleProblem = "No view in this model is called '" + viewName + "', so its " + side + " is not known.";
        else if (inModel.Count == 0)
            ruleProblem = "'" + viewName + "' is a " + named[0].ViewType + " view - only a plan, section, " +
                          "elevation, detail or 3D view has a " + side + " in the model.";
        else
        {
            viewToward = sideOf(inModel[0]).Normalize();
            if (inModel.Skip(1).Any(v => !sideOf(v).Normalize().IsAlmostEqualTo(viewToward)))
            {
                ruleProblem = inModel.Count + " views are called '" + viewName + "' and their " + side +
                              " faces point different ways, so which is meant would be a guess.";
                viewToward = null;
            }
        }
    }

    // ------------------------------------------------- measuring a face
    // Every point of an element's geometry, in this model's coordinates: the
    // ends of every edge of every solid, every curve and mesh. Visible
    // geometry first; an element with none (a void-only cut-out) is measured
    // with its hidden geometry, and every later measure of it the same way.
    Func<Element, bool, List<XYZ>> pointsOf = (element, hidden) =>
    {
        var points = new List<XYZ>();
        Action<GeometryElement, Transform> walk = null;
        walk = (geometry, place) =>
        {
            if (geometry == null) return;
            foreach (GeometryObject piece in geometry)
            {
                var instance = piece as GeometryInstance;
                if (instance != null)
                {
                    walk(instance.GetSymbolGeometry(), place.Multiply(instance.Transform));
                    continue;
                }
                var solid = piece as Solid;
                if (solid != null)
                {
                    if (solid.Faces.Size == 0) continue;
                    foreach (Edge edge in solid.Edges)
                        foreach (var p in edge.Tessellate()) points.Add(place.OfPoint(p));
                    continue;
                }
                var curve = piece as Curve;
                if (curve != null)
                {
                    foreach (var p in curve.Tessellate()) points.Add(place.OfPoint(p));
                    continue;
                }
                var mesh = piece as Mesh;
                if (mesh != null)
                {
                    foreach (var p in mesh.Vertices) points.Add(place.OfPoint(p));
                    continue;
                }
                var spot = piece as Point;
                if (spot != null) points.Add(place.OfPoint(spot.Coord));
            }
        };
        GeometryElement whole = null;
        try
        {
            whole = element.get_Geometry(new Options
            {
                DetailLevel = ViewDetailLevel.Fine,
                IncludeNonVisibleObjects = hidden
            });
        }
        catch (Exception) { }
        walk(whole, Transform.Identity);
        return points;
    };
    Func<List<XYZ>, XYZ, double> outermost = (points, toward) => points.Max(p => p.DotProduct(toward));

    // ------------------------------------------- the outside: three signs
    // THE MODEL AND ITS LOADED LINKS, each with the transform that takes its
    // coordinates into this model's. A link not loaded is counted and said.
    var models = new List<Tuple<Document, Transform, string>>();
    var linksNotLoaded = 0;
    if (side == "outside" && ruleProblem == null)
    {
        models.Add(Tuple.Create(doc, Transform.Identity, "this model"));
        foreach (RevitLinkInstance link in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)))
        {
            Document linkDoc = null;
            try { linkDoc = link.GetLinkDocument(); } catch (Exception) { }
            if (linkDoc == null) { linksNotLoaded++; continue; }
            models.Add(Tuple.Create(linkDoc, link.GetTotalTransform(), link.Name));
        }
    }

    // Elements of one category whose box meets a box given in THIS model's
    // coordinates, looked for in the model or link's own.
    Func<Tuple<Document, Transform, string>, XYZ, XYZ, BuiltInCategory, List<Element>> nearBy =
        (model, low, high, category) =>
        {
            var back = model.Item2.Inverse;
            var corners = new List<XYZ>();
            foreach (var x in new[] { low.X, high.X })
                foreach (var y in new[] { low.Y, high.Y })
                    foreach (var z in new[] { low.Z, high.Z })
                        corners.Add(back.OfPoint(new XYZ(x, y, z)));
            var box = new Outline(new XYZ(corners.Min(p => p.X), corners.Min(p => p.Y), corners.Min(p => p.Z)),
                                  new XYZ(corners.Max(p => p.X), corners.Max(p => p.Y), corners.Max(p => p.Z)));
            return new FilteredElementCollector(model.Item1).OfCategory(category).WhereElementIsNotElementType()
                .WherePasses(new BoundingBoxIntersectsFilter(box)).ToList();
        };

    // A wall's middle line where it passes nearest a point given in this
    // model's coordinates: the foot of the line, its horizontal normal and
    // its along direction - all in this model's coordinates - or null.
    Func<Tuple<Document, Transform, string>, Wall, XYZ, Tuple<XYZ, XYZ, XYZ>> wallLineNear = (model, wall, at) =>
    {
        var located = wall.Location as LocationCurve;
        if (located == null || located.Curve == null) return null;
        var curve = located.Curve;
        var mine = model.Item2.Inverse.OfPoint(at);
        IntersectionResult hit = null;
        try { hit = curve.Project(new XYZ(mine.X, mine.Y, curve.GetEndPoint(0).Z)); } catch (Exception) { }
        if (hit == null) return null;
        XYZ along;
        try { along = curve.ComputeDerivatives(hit.Parameter, false).BasisX; } catch (Exception) { return null; }
        along = new XYZ(along.X, along.Y, 0);
        if (along.GetLength() < 1e-9) return null;
        along = along.Normalize();
        var foot = model.Item2.OfPoint(new XYZ(hit.XYZPoint.X, hit.XYZPoint.Y, mine.Z));
        var alongHere = model.Item2.OfVector(along).Normalize();
        return Tuple.Create(foot, alongHere.CrossProduct(XYZ.BasisZ).Normalize(), alongHere);
    };

    Func<Element, double?> levelHeight = element =>
    {
        var ids = new List<ElementId> { element.LevelId };
        foreach (var which in new[] { BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
                                      BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM,
                                      BuiltInParameter.FAMILY_LEVEL_PARAM })
        {
            try
            {
                var p = element.get_Parameter(which);
                if (p != null && p.StorageType == StorageType.ElementId) ids.Add(p.AsElementId());
            }
            catch (Exception) { }
        }
        var placed = element as FamilyInstance;
        if (placed != null && placed.Host is Level) ids.Add(placed.Host.Id);
        foreach (var id in ids)
        {
            var level = id == null ? null : doc.GetElement(id) as Level;
            if (level != null) return level.ProjectElevation;
        }
        return null;
    };

    // A STRUCTURAL floor at a spot, in a band from 1 m below the element's
    // level to 0.5 m above it - in this model or any loaded link. Answers
    // where it found one, or null.
    Func<XYZ, double, string> slabAt = (spot, levelZ) =>
    {
        var top = new XYZ(spot.X, spot.Y, levelZ + 0.5 * metre);
        var bottom = new XYZ(spot.X, spot.Y, levelZ - 1.0 * metre);
        var reach = new XYZ(0.01, 0.01, 0);
        foreach (var model in models)
        {
            var back = model.Item2.Inverse;
            Line probe;
            try { probe = Line.CreateBound(back.OfPoint(top), back.OfPoint(bottom)); }
            catch (Exception) { continue; }
            foreach (var candidate in nearBy(model, bottom - reach, top + reach, BuiltInCategory.OST_Floors))
            {
                if (!(candidate is Floor)) continue;
                var structural = candidate.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
                if (structural == null || structural.AsInteger() != 1) continue;
                GeometryElement shape = null;
                try { shape = candidate.get_Geometry(new Options()); } catch (Exception) { }
                if (shape == null) continue;
                foreach (GeometryObject piece in shape)
                {
                    var solid = piece as Solid;
                    if (solid == null || solid.Faces.Size == 0) continue;
                    try
                    {
                        if (solid.IntersectWithCurve(probe, new SolidCurveIntersectionOptions()).SegmentCount > 0)
                            return "floor " + candidate.Id + " in " + model.Item3;
                    }
                    catch (Exception) { }
                }
            }
        }
        return null;
    };

    // OUTSIDE. Measured on 18 openings in a real project, three signs agreed
    // every time, and each is asked here on its own:
    //   slab  - no structural floor 1 m and 3 m out on that side, at the
    //           element's level, in this model or a loaded link, and one on
    //           the other side;
    //   skin  - a thin linked wall (at most half as thick as the wall the
    //           element sits in) runs beside it on that side only;
    //   ducts - this model's ducts and duct accessories nearby sit on the
    //           OTHER side, the inside.
    // A sign that finds nothing to go on says so and does not vote. None
    // voting, or two voting different ways, refuses the element and names
    // what each sign found - never a guess.
    // Answers the direction, or null with why, and the signs as JSON.
    Func<Element, List<XYZ>, Tuple<XYZ, string, string>> outsideOf = (element, points) =>
    {
        var low = new XYZ(points.Min(p => p.X), points.Min(p => p.Y), points.Min(p => p.Z));
        var high = new XYZ(points.Max(p => p.X), points.Max(p => p.Y), points.Max(p => p.Z));
        var middle = (low + high) * 0.5;

        // THE WALL IT SITS IN: one whose middle line passes within half its
        // thickness of the element's middle, in this model or a loaded link.
        var pad = new XYZ(0.5 * metre, 0.5 * metre, 0);
        var holders = new List<Tuple<XYZ, XYZ, double, string>>();   // foot, normal, width, which
        foreach (var model in models)
        {
            foreach (var candidate in nearBy(model, low - pad, high + pad, BuiltInCategory.OST_Walls))
            {
                var wall = candidate as Wall;
                // A WALL WRITTEN ITSELF is the wall it sits in: its own middle
                // line passes through its own middle, so it is not skipped.
                if (wall == null) continue;
                double width;
                try { width = wall.Width; } catch (Exception) { continue; }
                var line = wallLineNear(model, wall, middle);
                if (line == null) continue;
                var apart = middle - line.Item1;
                if (Math.Abs(apart.DotProduct(line.Item2)) > width / 2 + 0.001 * metre) continue;
                if (Math.Abs(apart.DotProduct(line.Item3)) > 0.001 * metre) continue;   // past the wall's end
                var box = candidate.get_BoundingBox(null);
                if (box != null)
                {
                    var zs = new[] { model.Item2.OfPoint(box.Min).Z, model.Item2.OfPoint(box.Max).Z };
                    if (middle.Z < zs.Min() - 0.001 * metre || middle.Z > zs.Max() + 0.001 * metre) continue;
                }
                holders.Add(Tuple.Create(line.Item1, line.Item2, width, "wall " + wall.Id + " in " + model.Item3));
            }
        }
        var unloaded = linksNotLoaded > 0 ? " (" + linksNotLoaded + " link(s) not loaded were not looked in)" : "";
        if (holders.Count == 0)
            return Tuple.Create((XYZ)null, "it sits in no wall of this model or its loaded links" + unloaded +
                                           ", so which side is outside cannot be told", (string)null);
        var normal = holders[0].Item2;
        var across = holders.FirstOrDefault(h => Math.Abs(h.Item2.DotProduct(normal)) < 0.9998);
        if (across != null)
            return Tuple.Create((XYZ)null, "it sits where two walls meet (" + holders[0].Item4 + " and " +
                                           across.Item4 + "), so which face is outside would be a guess", (string)null);
        var foot0 = holders[0].Item1;
        var halfWall = holders.Max(h => h.Item3) / 2;

        // Each sign answers +1 (outside faces `normal`), -1 (outside faces
        // away from it) or 0 (nothing to go on), and what it found, in words.
        var sideA = "the side facing " + facing(normal);
        var sideB = "the side facing " + facing(normal.Negate());
        Func<int, string> named = vote => vote > 0 ? sideA : sideB;

        // slab
        int slabVote = 0;
        string slabSaid;
        var levelZ = levelHeight(element);
        if (levelZ == null)
        {
            slabSaid = "not used - the element has no level, so no slab at its level can be looked for";
        }
        else
        {
            var found = new Dictionary<int, List<string>>();
            foreach (var way in new[] { 1, -1 })
            {
                var toward = normal.Multiply(way);
                // OUT FROM THE WALL'S FACE, or the element's where it stands
                // proud of it: an element thinner than its wall would otherwise
                // probe 1 m out from a point inside the wall.
                var wallFace = foot0.DotProduct(toward) + halfWall;
                var reachOut = Math.Max(outermost(points, toward), wallFace) - middle.DotProduct(toward);
                found[way] = new List<string>();
                foreach (var metres in new[] { 1.0, 3.0 })
                {
                    var where = slabAt(middle + toward.Multiply(reachOut + metres * metre), levelZ.Value);
                    if (where != null) found[way].Add(metres + " m out: " + where);
                }
            }
            if (found[1].Count == 0 && found[-1].Count > 0) slabVote = 1;
            else if (found[-1].Count == 0 && found[1].Count > 0) slabVote = -1;
            slabSaid = slabVote != 0
                ? "outside is " + named(slabVote) + " - no structural floor 1 m or 3 m out there, and "
                  + string.Join("; ", found[-slabVote].ToArray()) + " on the other side"
                : found[1].Count > 0
                    ? "not used - a structural floor on both sides (" + found[1][0] + "; " + found[-1][0] + ")"
                    : "not used - no structural floor 1 m or 3 m out on either side at its level" + unloaded;
        }

        // skin
        var skins = new Dictionary<int, List<string>> { { 1, new List<string>() }, { -1, new List<string>() } };
        var skinPad = new XYZ(1.5 * metre, 1.5 * metre, 0);
        foreach (var model in models.Skip(1))
        {
            foreach (var candidate in nearBy(model, low - skinPad, high + skinPad, BuiltInCategory.OST_Walls))
            {
                var wall = candidate as Wall;
                if (wall == null) continue;
                double width;
                try { width = wall.Width; } catch (Exception) { continue; }
                if (width <= 0 || width > halfWall) continue;
                var line = wallLineNear(model, wall, middle);
                if (line == null || Math.Abs(line.Item2.DotProduct(normal)) < 0.9998) continue;
                if (Math.Abs((middle - line.Item1).DotProduct(line.Item3)) > 0.001 * metre) continue;
                var offset = (line.Item1 - foot0).DotProduct(normal);
                if (Math.Abs(offset) <= halfWall || Math.Abs(offset) > halfWall + 1.0 * metre) continue;
                skins[offset > 0 ? 1 : -1].Add("wall " + wall.Id + " (" + mmNumber(width) + " mm) in " + model.Item3);
            }
        }
        var skinVote = skins[1].Count > 0 && skins[-1].Count == 0 ? 1
                     : skins[-1].Count > 0 && skins[1].Count == 0 ? -1 : 0;
        var skinSaid = skinVote != 0
            ? "outside is " + named(skinVote) + " - thin linked wall " + string.Join(", ", skins[skinVote].Take(3).ToArray())
            : skins[1].Count > 0
                ? "not used - thin linked walls on both sides (" + skins[1][0] + "; " + skins[-1][0] + ")"
                : "not used - no thin linked wall within 1 m of either face" + unloaded;

        // ducts
        var ductPad = new XYZ(3.0 * metre, 3.0 * metre, 1.0 * metre);
        var count = new Dictionary<int, int> { { 1, 0 }, { -1, 0 } };
        foreach (var category in new[] { BuiltInCategory.OST_DuctCurves, BuiltInCategory.OST_DuctAccessory })
        {
            foreach (var near in nearBy(models[0], low - ductPad, high + ductPad, category))
            {
                // A DUCT BY ITS POINT NEAREST THE ELEMENT, never its ends: a long
                // duct passing outside the wall has both ends far away, and one
                // through the opening has its nearest point in the wall itself.
                var spots = new List<XYZ>();
                var onCurve = near.Location as LocationCurve;
                var onPoint = near.Location as LocationPoint;
                if (onCurve != null && onCurve.Curve != null)
                {
                    IntersectionResult closest = null;
                    try { closest = onCurve.Curve.Project(middle); } catch (Exception) { }
                    if (closest != null && closest.XYZPoint.DistanceTo(middle) <= 3.0 * metre)
                        spots.Add(closest.XYZPoint);
                }
                else if (onPoint != null && onPoint.Point.DistanceTo(middle) <= 3.0 * metre) spots.Add(onPoint.Point);
                foreach (var spot in spots)
                {
                    var offset = (spot - foot0).DotProduct(normal);
                    if (Math.Abs(offset) <= halfWall) continue;   // in the wall itself
                    count[offset > 0 ? 1 : -1]++;
                }
            }
        }
        var ductVote = count[1] > count[-1] ? -1 : count[-1] > count[1] ? 1 : 0;
        var ductSaid = ductVote != 0
            ? "outside is " + named(ductVote) + " - " + count[-ductVote] + " duct(s) or duct accessories "
              + "within 3 m on the other side, the inside, and " + count[ductVote] + " on this one"
            : count[1] > 0
                ? "not used - as many ducts or duct accessories on each side (" + count[1] + ")"
                : "not used - no duct or duct accessory of this model within 3 m";

        var signsJson = "{" + esc("slab") + ": " + esc(slabSaid) + ", " + esc("skin") + ": " + esc(skinSaid)
                      + ", " + esc("ducts") + ": " + esc(ductSaid) + "}";
        var votes = new[] { slabVote, skinVote, ductVote };
        var all = "slab: " + slabSaid + "; skin: " + skinSaid + "; ducts: " + ductSaid;
        if (votes.All(v => v == 0))
            return Tuple.Create((XYZ)null, "none of the three signs of the outside applies here - " + all, signsJson);
        if (votes.Contains(1) && votes.Contains(-1))
            return Tuple.Create((XYZ)null, "the signs of the outside disagree - " + all, signsJson);
        return Tuple.Create(normal.Multiply(votes.Contains(1) ? 1 : -1), (string)null, signsJson);
    };

    // ------------------------------------------- each element, three times
    var faceToward = new Dictionary<string, XYZ>();
    var hiddenToo = new Dictionary<string, bool>();
    var before = new Dictionary<string, double>();
    var afterWrite = new Dictionary<string, double>();
    var settled = new Dictionary<string, double>();
    var signs = new Dictionary<string, string>();
    var turnedDown = new List<string>();
    var refusedWhy = new Dictionary<string, string>();
    var refusedRows = 0;
    var keptCount = 0;

    // BEFORE ANYTHING IS WRITTEN: the face can be found and the element can
    // be moved. Measures the face and keeps it. Null when it can be kept.
    Func<Element, string> checkFace = element =>
    {
        if (ruleProblem != null) return null;   // said once, for the whole run
        var key = element.UniqueId;
        if (faceToward.ContainsKey(key)) return null;   // another row on the same element
        if (refusedWhy.ContainsKey(key))
        {
            refusedRows++;
            return refusedWhy[key];
        }
        string why = null;
        if (element.Pinned)
            why = "it is pinned, and keeping its " + side + " face means moving it - unpin it, or leave keepFace out";
        else if (element.GroupId != null && element.GroupId != ElementId.InvalidElementId)
            why = "it is in a model group, which cannot move one member at a time";
        else
        {
            var hidden = false;
            var points = pointsOf(element, false);
            if (points.Count == 0)
            {
                hidden = true;
                points = pointsOf(element, true);
            }
            if (points.Count == 0) why = "it has no geometry to measure a face on";
            else
            {
                var found = side == "outside"
                    ? outsideOf(element, points)
                    : Tuple.Create(viewToward, (string)null, (string)null);
                if (found.Item3 != null) signs[key] = found.Item3;
                if (found.Item1 == null) why = found.Item2;
                else
                {
                    faceToward[key] = found.Item1;
                    hiddenToo[key] = hidden;
                    before[key] = outermost(points, found.Item1);
                }
            }
        }
        if (why == null) return null;
        turnedDown.Add(element.Id.ToString());
        refusedRows++;
        refusedWhy[key] = "its " + side + " face cannot be kept - " + why;
        return refusedWhy[key];
    };

    // AFTER THE WRITE AND ONE REGENERATION: where each face went, the move
    // that puts it back, a second regeneration and a third measure. Null when
    // every face is back within 0.1 mm; otherwise why, and the caller throws.
    Func<IEnumerable<Element>, string> keepFaces = elements =>
    {
        var moving = new List<Element>();
        var once = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in elements)
        {
            if (faceToward.ContainsKey(element.UniqueId) && once.Add(element.UniqueId))
                moving.Add(element);
        }
        foreach (var element in moving)
        {
            var key = element.UniqueId;
            var points = pointsOf(element, hiddenToo[key]);
            if (points.Count == 0)
                return element.Id + " has no geometry after the write, so its " + side + " face cannot be put back.";
            afterWrite[key] = outermost(points, faceToward[key]);
        }
        foreach (var element in moving)
        {
            var key = element.UniqueId;
            var back = before[key] - afterWrite[key];
            if (Math.Abs(back) < 1e-9) continue;
            try { ElementTransformUtils.MoveElement(doc, element.Id, faceToward[key].Multiply(back)); }
            catch (Exception failure)
            {
                return failure.Message + " (Revit, moving " + element.Id + " back by " + mmNumber(back) + " mm).";
            }
        }
        doc.Regenerate();
        var off = new List<string>();
        foreach (var element in moving)
        {
            var key = element.UniqueId;
            var points = pointsOf(element, hiddenToo[key]);
            settled[key] = points.Count == 0 ? double.NaN : outermost(points, faceToward[key]);
            if (double.IsNaN(settled[key]) || Math.Abs(settled[key] - before[key]) > tolerance)
                off.Add(element.Id + " (" + mmNumber(settled[key] - before[key]) + " mm from where it was)");
        }
        if (off.Count > 0)
        {
            return "The " + side + " face of " + off.Count + " element(s) did not go back to where it was after the " +
                   "move - " + string.Join(", ", off.Take(5).ToArray()) + (off.Count > 5 ? " and more" : "") +
                   ". Revit may hold them to a host or a constraint.";
        }
        keptCount = moving.Count;
        return null;
    };

    // What one element's face did, for its readBack entry; empty for an
    // element whose face was not kept.
    Func<Element, string> faceJson = element =>
    {
        var key = element.UniqueId;
        if (!faceToward.ContainsKey(key) || !settled.ContainsKey(key)) return "";
        var d = faceToward[key];
        return ", " + esc("face") + ": {" + esc("side") + ": " + esc(side) + ", "
             + esc("toward") + ": [" + plain(d.X, 4) + ", " + plain(d.Y, 4) + ", " + plain(d.Z, 4) + "], "
             + esc("measured") + ": " + esc(hiddenToo[key] ? "hidden geometry" : "geometry") + ", "
             + esc("beforeMm") + ": " + mmNumber(before[key]) + ", "
             + esc("writeMovedItMm") + ": " + mmNumber(afterWrite[key] - before[key]) + ", "
             + esc("movedBackMm") + ": " + mmNumber(before[key] - afterWrite[key]) + ", "
             + esc("nowMm") + ": " + mmNumber(settled[key]) + ", "
             + esc("offByMm") + ": " + mmNumber(settled[key] - before[key])
             + (signs.ContainsKey(key) ? ", " + esc("signs") + ": " + signs[key] : "") + "}";
    };

    Func<string> summaryJson = () =>
        "{" + esc("asked") + ": " + esc(said) + ", " + esc("side") + ": " + esc(side) + ", "
        + esc("view") + ": " + esc(viewName.Length > 0 ? viewName : null) + ", "
        + esc("problem") + ": " + esc(ruleProblem) + ", "
        + esc("kept") + ": " + keptCount + ", "
        + esc("turnedDown") + ": " + turnedDown.Count + ", "
        + esc("toleranceMm") + ": 0.1}";

    Func<string> keptSentence = () =>
    {
        var moved = afterWrite.Keys.Where(k => settled.ContainsKey(k))
                              .Select(k => Math.Abs(afterWrite[k] - before[k])).ToList();
        return "The " + side + " face of " + keptCount + " element(s) was kept where it was: the write moved it "
             + mmNumber(moved.Min()) + (moved.Max() - moved.Min() > tolerance ? " to " + mmNumber(moved.Max()) : "")
             + " mm and each element was moved back by exactly what was measured, in the same undo entry.";
    };

    // When the face stopped the run: the rule itself, or some elements. Said
    // beside version 2's sentence for the rows that changed in Revit, never
    // instead of it - the two have different fixes.
    Func<string> stoppedSentence = () => ruleProblem != null
        ? "Nothing was written: " + ruleProblem
        : "Nothing was written: the " + side + " face of " + turnedDown.Count + " element(s) cannot be kept - "
          + "stale says why, row by row.";

    return new
    {
        Problem = ruleProblem,
        Check = checkFace,
        Keep = keepFaces,
        Json = faceJson,
        Summary = summaryJson,
        Kept = (Func<int>)(() => keptCount),
        Said = keptSentence,
        Stopped = stoppedSentence,
        // The id rows the face turned down - the rule's own stale row counts one.
        Rows = (Func<int>)(() => (ruleProblem != null ? 1 : 0) + refusedRows)
    };
}).First();

// ------------------------------------------------------------- which way
// BY KEY when the first row is key=... - an id row starts with an element id,
// so nothing a version 1 caller sends can land there by accident. One
// top-level name only: every top-level variable is printed in the reply.
var keyed = (rows ?? "").Split(';').Where(r => r.Trim().Length > 0).Take(1)
    .Any(r => r.IndexOf('=') > 0 && Uri.UnescapeDataString(r.Substring(0, r.IndexOf('=')))
                                        .Trim().Equals("key", StringComparison.OrdinalIgnoreCase));

// ------------------------------------------------------------------ parse
var plan = new List<Tuple<Element, string, string>>();   // element, parameter, new value
var staleJson = new List<string>();

// A keepFace that cannot be used stops the run once, as its own stale row.
if (!keyed && faceKeeper != null && faceKeeper.Problem != null)
{
    stale.Add("keepFace: " + faceKeeper.Problem);
    staleJson.Add("{" + esc("id") + ": null, " + esc("name") + ": " + esc("keepFace")
                  + ", " + esc("why") + ": " + esc(faceKeeper.Problem) + "}");
}

foreach (var raw in (keyed ? "" : (rows ?? "")).Split(';'))
{
    if (raw.Trim().Length == 0) continue;
    var f = raw.Split('|');
    if (f.Length != 5 && f.Length != 6)
    {
        findings.Add("A row did not have its five parts - id, UniqueId, parameter, old value, " +
                     "new value, and optionally the stored old value - so nothing was written.");
        stale.Add("(unreadable row)");
        continue;
    }
    var id = Uri.UnescapeDataString(f[0]);
    var uniqueId = Uri.UnescapeDataString(f[1]);
    var name = Uri.UnescapeDataString(f[2]);
    var was = Uri.UnescapeDataString(f[3]);
    var want = Uri.UnescapeDataString(f[4]);
    var wasStored = f.Length == 6 ? Uri.UnescapeDataString(f[5]) : "";

    string why = null;
    string now = null;
    string nowStored = null;
    Element element = null;
    try { element = doc.GetElement(uniqueId); } catch { }

    if (element == null) why = "it is no longer in the model";
    else if (element.Id.ToString() != id) why = "the id no longer names the same element";
    else
    {
        var matches = element.GetParameters(name);
        var target = matches.Count > 0 ? theOne(element, name) : null;
        if (matches.Count == 0) why = "it no longer has a parameter called " + name;
        else if (target == null)
            why = "two parameters on it are called " + name + " and more than one can be written";
        else if (target.IsReadOnly) why = name + " is read-only";
        else if (target.StorageType == StorageType.ElementId
                 || target.StorageType == StorageType.None)
            why = name + " holds a reference to another element, which cannot be typed as text";
        else if (groupLocked(element, target))
            why = "it is in a model group, and " + name + " would change in every copy of the group";
        else if (!unchanged(target, was)
                 || (wasStored.Length > 0 && target.StorageType != StorageType.String
                     && storedValue(target) != wasStored))
        {
            now = readValue(target) ?? "";
            nowStored = storedValue(target);
            why = "it was changed in Revit since the table was read - it now reads '" + now + "'";
        }
    }

    // Version 3: the face to keep is found and measured now, before any row
    // is written - one that cannot be kept stops the run like a stale row.
    if (why == null && faceKeeper != null) why = faceKeeper.Check(element);

    if (why != null)
    {
        stale.Add(id + ": " + why);
        // WHAT REVIT HOLDS NOW, for a cell that was changed there: the table
        // takes it as its new "was", so the next Apply is checked against
        // the model as it is, not refused for ever (Codex review of #362).
        staleJson.Add("{" + esc("id") + ": " + esc(id) + ", " + esc("name") + ": " + esc(name)
                      + ", " + esc("why") + ": " + esc(why)
                      + (now == null ? "" : ", " + esc("now") + ": " + esc(now)
                         + ", " + esc("nowRaw") + ": " + esc(nowStored)) + "}");
        continue;
    }
    plan.Add(Tuple.Create(element, name, want));
}

// ------------------------------------------------------------------ write
var readBack = new List<string>();
var applied = stale.Count == 0 && plan.Count > 0;

if (stale.Count > 0)
{
    // Version 3: rows the face turned down are said apart from rows changed
    // in Revit. Left out, faceRows is 0 and this is version 2's sentence.
    var faceRows = faceKeeper == null ? 0 : faceKeeper.Rows();
    if (faceRows > 0) findings.Add(faceKeeper.Stopped());
    if (stale.Count > faceRows)
    findings.Add(string.Format(
        "Nothing was written: {0} row(s) no longer match what the table showed. " +
        "Refresh the table, look at them, and apply again.", stale.Count - faceRows));
}
else if (plan.Count == 0)
{
    if (!keyed) findings.Add("No rows were given, so nothing was written.");
}
else
{
    var refusedHere = new List<string>();
    foreach (var step in plan)
    {
        var p = theOne(step.Item1, step.Item2);
        var want = step.Item3;
        var ok = false;

        if (isYesNo(p))
        {
            var said = same(want).ToLowerInvariant();
            if (Array.IndexOf(ticked, said) >= 0) ok = p.Set(1);
            else if (Array.IndexOf(unticked, said) >= 0) ok = p.Set(0);
        }
        else if (p.StorageType == StorageType.String)
        {
            ok = p.Set(want ?? "");
        }
        else
        {
            ok = p.SetValueString(want ?? "");
        }

        if (!ok) refusedHere.Add(step.Item1.Id + ": Revit would not take '" + want + "' for " + step.Item2);
    }

    if (refusedHere.Count > 0)
    {
        // A value Revit turned down after the check passed. Thrown, so the
        // operation's TransactionGroup rolls EVERY row back: all or nothing
        // holds on this side of the check as well.
        throw new InvalidOperationException("Nothing was kept. " + string.Join("; ", refusedHere.ToArray()));
    }

    doc.Regenerate();

    // Version 3: each face put back where it was, in this same transaction.
    // One that will not go back takes every row back with it.
    if (faceKeeper != null)
    {
        var notKept = faceKeeper.Keep(plan.Select(step => step.Item1));
        if (notKept != null) throw new InvalidOperationException("Nothing was kept. " + notKept);
    }

    foreach (var step in plan)
    {
        var again = theOne(step.Item1, step.Item2);
        var now = again == null ? null : readValue(again);
        var nowRaw = again == null ? null : storedValue(again);
        written++;
        readBack.Add("{" + esc("id") + ": " + esc(step.Item1.Id.ToString()) + ", "
                     + esc("name") + ": " + esc(step.Item2) + ", "
                     + esc("value") + ": " + esc(now) + ", "
                     + esc("raw") + ": " + esc(nowRaw)
                     + (faceKeeper == null ? "" : faceKeeper.Json(step.Item1)) + "}");
    }
}

resultJson = "{" + esc("format") + ": 1, "
           + esc("applied") + ": " + (applied ? "true" : "false") + ", "
           + esc("written") + ": " + written + ", "
           + esc("stale") + ": [" + string.Join(", ", staleJson.ToArray()) + "], "
           + esc("readBack") + ": [" + string.Join(", ", readBack.ToArray()) + "]}";

if (keyed)
{
    // ============================================================== by key
    // Version 2. The first row names the key; every row after it names ONE
    // element by the value it holds and gives what to write on it. Nothing
    // here runs for the id rows above, and nothing above runs for these.

    var keyedRows = new List<string>();
    foreach (var raw in (rows ?? "").Split(';'))
    {
        if (raw.Trim().Length > 0) keyedRows.Add(raw);
    }
    var header = keyedRows[0];
    keyedRows.RemoveAt(0);

    // ------------------------------------------------------------ the header
    // key=   the parameter whose value names each element - Number, Mark.
    // in=    the categories to look in, comma-separated, by the name
    //        Visibility/Graphics shows. REQUIRED: a door's Mark and a
    //        window's Mark can both read W1, and a key must never land on a
    //        kind of element nobody meant.
    // Any other name=value NARROWS the search to elements that hold that
    // value too - Level=Level 14 when every floor numbers its rooms from 1.
    string keyName = null;
    var categoryNames = new List<string>();
    var narrowing = new List<Tuple<string, string>>();
    string problem = null;

    foreach (var part in header.Split('|'))
    {
        if (part.Trim().Length == 0) continue;
        var at = part.IndexOf('=');
        var word = at < 0 ? "" : Uri.UnescapeDataString(part.Substring(0, at)).Trim();
        if (word.Length == 0)
        {
            problem = "The first row's '" + Uri.UnescapeDataString(part) + "' is not name=value - " +
                      "the first row is key=<parameter>|in=<categories>.";
            break;
        }
        var said = part.Substring(at + 1);
        if (word.Equals("key", StringComparison.OrdinalIgnoreCase))
        {
            keyName = Uri.UnescapeDataString(said).Trim();
        }
        else if (word.Equals("in", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var one in said.Split(','))
            {
                var name = Uri.UnescapeDataString(one).Trim();
                if (name.Length > 0) categoryNames.Add(name);
            }
        }
        else
        {
            narrowing.Add(Tuple.Create(word, Uri.UnescapeDataString(said)));
        }
    }

    if (problem == null && string.IsNullOrEmpty(keyName))
        problem = "The first row names no key - write key=<parameter>, for example key=Number.";
    if (problem == null && categoryNames.Count == 0)
        problem = "The first row says no category to look in - add in=<categories>, for example " +
                  "in=Rooms, so a key cannot land on a kind of element you did not mean.";
    if (problem == null && faceKeeper != null && faceKeeper.Problem != null)
        problem = faceKeeper.Problem;

    var categories = new List<Category>();
    if (problem == null)
    {
        foreach (var wanted in categoryNames)
        {
            Category found = null;
            foreach (Category category in doc.Settings.Categories)
            {
                if (string.Equals(category.Name, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    found = category;
                    break;
                }
            }
            if (found == null)
            {
                problem = "This model has no category called '" + wanted + "'. Give it as " +
                          "Visibility/Graphics shows it - Rooms, Sheets, Doors.";
                break;
            }
            if (!categories.Any(c => c.Id == found.Id)) categories.Add(found);
        }
    }

    var where = string.Join(", ", categoryNames.ToArray());
    if (narrowing.Count > 0)
    {
        where += " with " + string.Join(" and ",
            narrowing.Select(n => n.Item1 + " '" + n.Item2 + "'").ToArray());
    }

    // ------------------------------------------------------ the candidates
    // Every element in those categories - never a type - in THIS model; a
    // linked model's elements are not written here. A value is compared as
    // the Properties palette shows it, exactly, ignoring spaces at the ends:
    // "01" is not "1", and "Lift 1" is not "lift 1".
    Func<Element, string, string, bool> holds = (element, name, value) =>
    {
        foreach (Parameter p in element.GetParameters(name))
        {
            if (same(readValue(p)) == same(value)) return true;
        }
        return false;
    };

    var byKey = new Dictionary<string, List<Element>>(StringComparer.Ordinal);
    var candidates = 0;
    var carryingKey = 0;
    if (problem == null)
    {
        foreach (var category in categories)
        {
            var collector = new FilteredElementCollector(doc)
                .OfCategoryId(category.Id)
                .WhereElementIsNotElementType();
            foreach (var element in collector)
            {
                if (narrowing.Any(n => !holds(element, n.Item1, n.Item2))) continue;
                candidates++;

                // AN ELEMENT IS LISTED ONCE PER VALUE IT SHOWS under the key's
                // name - a sheet carries two parameters called Sheet Number.
                var keyParameters = element.GetParameters(keyName);
                if (keyParameters.Count > 0) carryingKey++;
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var p in keyParameters)
                {
                    var value = same(readValue(p));
                    if (!seen.Add(value)) continue;
                    List<Element> list;
                    if (!byKey.TryGetValue(value, out list))
                    {
                        list = new List<Element>();
                        byKey[value] = list;
                    }
                    list.Add(element);
                }
            }
        }

        if (candidates == 0)
            problem = "There is no element in " + where + " in this model, so no key can name one.";
        else if (carryingKey == 0)
            problem = "No element in " + where + " has a parameter called " + keyName + ".";
    }

    // ----------------------------------------------------------- the rows
    // EVERY KEY IS MATCHED AGAINST THE MODEL AS IT WAS BEFORE ANY ROW IS
    // WRITTEN, so a row that renumbers an element never moves another row
    // onto it. A ROW IS REFUSED ON ITS OWN, never guessed: a key that names
    // no element, or several, or is given twice, writes nothing for that row
    // and says why by name; the other rows are written.
    var refusedJson = new List<string>();
    Action<string, string> refuse = (key, why) =>
    {
        refused.Add(key + ": " + why);
        refusedJson.Add("{" + esc("key") + ": " + esc(key) + ", " + esc("why") + ": " + esc(why) + "}");
    };

    Func<List<Element>, string> idsOf = list =>
        string.Join(", ", list.Take(5).Select(e => e.Id.ToString()).ToArray())
        + (list.Count > 5 ? " and " + (list.Count - 5) + " more" : "");

    // The same five reasons a cell cannot be written as the id rows above,
    // in the same words.
    Func<Element, string, string> whyNot = (element, name) =>
    {
        var matches = element.GetParameters(name);
        var target = matches.Count > 0 ? theOne(element, name) : null;
        if (matches.Count == 0) return "it has no parameter called " + name;
        if (target == null) return "two parameters on it are called " + name + " and more than one can be written";
        if (target.IsReadOnly) return name + " is read-only";
        if (target.StorageType == StorageType.ElementId || target.StorageType == StorageType.None)
            return name + " holds a reference to another element, which cannot be typed as text";
        if (groupLocked(element, target))
            return "it is in a model group, and " + name + " would change in every copy of the group";
        return null;
    };

    var keyedPlan = new List<Tuple<string, Element, List<Tuple<string, string>>>>();
    if (problem == null)
    {
        var parsed = new List<Tuple<string, List<Tuple<string, string>>>>();
        var timesGiven = new Dictionary<string, int>(StringComparer.Ordinal);
        var rowNumber = 1;
        foreach (var raw in keyedRows)
        {
            rowNumber++;
            var f = raw.Split('|');
            var key = same(Uri.UnescapeDataString(f[0]));
            var pairs = new List<Tuple<string, string>>();
            string bad = key.Length == 0 ? "it has no key before the first |" : null;
            for (var i = 1; i < f.Length && bad == null; i++)
            {
                if (f[i].Trim().Length == 0) continue;
                var at = f[i].IndexOf('=');
                var name = at < 0 ? "" : Uri.UnescapeDataString(f[i].Substring(0, at)).Trim();
                if (name.Length == 0) bad = "'" + Uri.UnescapeDataString(f[i]) + "' is not parameter=value";
                else if (pairs.Any(x => x.Item1 == name)) bad = name + " is given twice";
                else pairs.Add(Tuple.Create(name, Uri.UnescapeDataString(f[i].Substring(at + 1))));
            }
            if (bad == null && pairs.Count == 0) bad = "it gives no parameter=value to write";
            if (bad != null)
            {
                refuse(key.Length > 0 ? key : "(row " + rowNumber + ")", "not written - " + bad);
                continue;
            }
            parsed.Add(Tuple.Create(key, pairs));
            timesGiven[key] = (timesGiven.ContainsKey(key) ? timesGiven[key] : 0) + 1;
        }

        var matched = new List<Tuple<string, Element, List<Tuple<string, string>>>>();
        foreach (var row in parsed)
        {
            var key = row.Item1;
            List<Element> hits;
            byKey.TryGetValue(key, out hits);
            if (timesGiven[key] > 1)
                refuse(key, "given on " + timesGiven[key] + " rows, so which value is meant would be a guess - not written");
            else if (hits == null || hits.Count == 0)
                refuse(key, "no element in " + where + " has " + keyName + " '" + key + "' - not written");
            else if (hits.Count > 1)
                refuse(key, hits.Count + " elements in " + where + " have " + keyName + " '" + key + "' (ids "
                       + idsOf(hits) + ") - which one is meant would be a guess, so none is written");
            else
                matched.Add(Tuple.Create(key, hits[0], row.Item2));
        }

        // TWO ROWS ON ONE ELEMENT - two keys that are both values it shows -
        // would write over each other, so neither is written.
        var rowsOnElement = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in matched)
        {
            var id = row.Item2.Id.ToString();
            rowsOnElement[id] = (rowsOnElement.ContainsKey(id) ? rowsOnElement[id] : 0) + 1;
        }

        foreach (var row in matched)
        {
            var element = row.Item2;
            if (rowsOnElement[element.Id.ToString()] > 1)
            {
                refuse(row.Item1, "another row's key names the same element (id " + element.Id + ") - not written");
                continue;
            }
            string why = null;
            foreach (var pair in row.Item3)
            {
                why = whyNot(element, pair.Item1);
                if (why != null) break;
            }
            // Version 3: the face to keep, found and measured before any row
            // is written; one that cannot be kept refuses this row alone.
            if (why == null && faceKeeper != null) why = faceKeeper.Check(element);
            if (why != null)
            {
                refuse(row.Item1, why + " - not written (id " + element.Id + ")");
                continue;
            }
            keyedPlan.Add(row);
        }
    }

    // ------------------------------------------------------------ write
    // EACH ROW IN ITS OWN SUB-TRANSACTION: a value Revit turns down takes
    // back that row's other values and no other row's. The whole call is
    // still ONE entry in Revit's undo list - the operation's TransactionGroup.
    var done = new List<Tuple<string, Element, List<Tuple<string, string>>>>();
    foreach (var row in keyedPlan)
    {
        var element = row.Item2;
        string turnedDown = null;
        using (var attempt = new SubTransaction(doc))
        {
            attempt.Start();
            try
            {
                foreach (var pair in row.Item3)
                {
                    var target = theOne(element, pair.Item1);
                    var asked = pair.Item2 ?? "";
                    var ok = false;
                    if (isYesNo(target))
                    {
                        var said = same(asked).ToLowerInvariant();
                        if (Array.IndexOf(ticked, said) >= 0) ok = target.Set(1);
                        else if (Array.IndexOf(unticked, said) >= 0) ok = target.Set(0);
                    }
                    else if (target.StorageType == StorageType.String)
                    {
                        ok = target.Set(asked);
                    }
                    else
                    {
                        ok = target.SetValueString(asked);
                    }
                    if (!ok)
                    {
                        turnedDown = "Revit would not take '" + asked + "' for " + pair.Item1;
                        break;
                    }
                }
            }
            catch (Exception failure)
            {
                // REVIT'S OWN WORDS FIRST - a reply cuts a line at 60 characters.
                turnedDown = failure.Message + " (Revit, writing this row)";
            }
            if (turnedDown == null) attempt.Commit();
            else attempt.RollBack();
        }

        if (turnedDown != null)
        {
            refuse(row.Item1, turnedDown + " - nothing on this row was kept (id " + element.Id + ")");
            continue;
        }
        done.Add(row);
    }

    // ------------------------------------------------------- read back
    // After ONE regeneration, every value is read from the model again: what
    // Revit holds now, not what was asked.
    var keyedReadBack = new List<string>();
    if (done.Count > 0) doc.Regenerate();
    // Version 3: each face put back where it was, in this same transaction.
    // One that will not go back takes every row back with it - a row already
    // written cannot be refused on its own once its element has moved.
    if (done.Count > 0 && faceKeeper != null)
    {
        var notKept = faceKeeper.Keep(done.Select(row => row.Item2));
        if (notKept != null) throw new InvalidOperationException("Nothing was kept. " + notKept);
    }
    foreach (var row in done)
    {
        written++;
        foreach (var pair in row.Item3)
        {
            var again = theOne(row.Item2, pair.Item1);
            var now = again == null ? null : readValue(again);
            var nowRaw = again == null ? null : storedValue(again);
            keyedReadBack.Add("{" + esc("key") + ": " + esc(row.Item1) + ", "
                         + esc("id") + ": " + esc(row.Item2.Id.ToString()) + ", "
                         + esc("name") + ": " + esc(pair.Item1) + ", "
                         + esc("asked") + ": " + esc(pair.Item2) + ", "
                         + esc("value") + ": " + esc(now) + ", "
                         + esc("raw") + ": " + esc(nowRaw)
                         + (faceKeeper == null ? "" : faceKeeper.Json(row.Item2)) + "}");
            if (again != null && again.StorageType == StorageType.String && (now ?? "") != (pair.Item2 ?? ""))
                findings.Add(row.Item1 + ": " + pair.Item1 + " reads back '" + now + "', not '" + pair.Item2 + "'");
        }
    }

    if (problem != null)
        findings.Insert(0, "Nothing was written. " + problem);
    else if (refused.Count > 0)
        findings.Insert(0, string.Format("{0} row(s) not written, {1} written - refused says why, by key.",
                                         refused.Count, written));
    else if (keyedRows.Count == 0)
        findings.Add("No rows were given after the key row, so nothing was written.");

    resultJson = "{" + esc("format") + ": 2, "
               + esc("applied") + ": " + (written > 0 ? "true" : "false") + ", "
               + esc("written") + ": " + written + ", "
               + esc("key") + ": " + esc(keyName) + ", "
               + esc("in") + ": [" + string.Join(", ", categoryNames.Select(n => esc(n)).ToArray()) + "], "
               + esc("where") + ": [" + string.Join(", ", narrowing.Select(n => "{" + esc("name") + ": "
                   + esc(n.Item1) + ", " + esc("value") + ": " + esc(n.Item2) + "}").ToArray()) + "], "
               + esc("problem") + ": " + esc(problem) + ", "
               + esc("stale") + ": [], "
               + esc("refused") + ": [" + string.Join(", ", refusedJson.ToArray()) + "], "
               + esc("readBack") + ": [" + string.Join(", ", keyedReadBack.ToArray()) + "]}";
}

// ------------------------------------------------------------ keep a face
// Version 3: what keepFace asked and how it went, on the answer itself.
// Nothing here runs when keepFace is left out.
if (faceKeeper != null)
{
    if (faceKeeper.Kept() > 0) findings.Add(faceKeeper.Said());
    resultJson = resultJson.Substring(0, resultJson.Length - 1)
               + ", " + esc("keepFace") + ": " + faceKeeper.Summary() + "}";

    // LET IT GO. Every top-level name is kept in the chain after the run, and
    // this one holds the document, every loaded link and each element's
    // measures; resultJson already carries all of it. Null, the reply prints
    // `faceKeeper (null)` - the same line as when keepFace is left out.
    faceKeeper = null;
}
