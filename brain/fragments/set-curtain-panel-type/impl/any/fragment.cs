// NOT STANDALONE. Assumes `doc`, `elements`, `panelTypeName` and `whichPanels`
// are in scope; leaves `changed`, `differs`, `alreadyThatType`, `chosen`,
// `skipped`, `layout`, `measured`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and opens none. Lengths are
// read off Revit in feet and printed in millimetres, / 304.8 the other way
// (D-20); none is written.
//
// ===========================================================================
// PANELS OF ONE WALL, PICKED BY WHERE THEY SIT, CHANGED BY THE GRID ITSELF.
// ===========================================================================
//
// CurtainGrid.ChangePanelType is the grid's own way to change what fills a
// cell, and its reference says what it takes on a curtain wall: a PanelType or
// a WallType - a curtain panel, a curtain wall door, or a basic wall as a
// solid infill. CHANGE_ELEMENT_TYPE stays the way for the panel the modeller
// has selected; this is for panels picked by their row, their column or a
// floor line, and for a wall as infill.
//
// ROWS AND COLUMNS ARE MEASURED, NEVER READ OFF U AND V. Revit's reference
// does not tie a curtain grid's U or V lines to vertical or horizontal on a
// wall - SET_CURTAIN_WALL_GRID measures both for that reason - so every grid
// line is classed by the way it actually runs, against Revit's own angle
// tolerance. The horizontal lines' heights, with the lowest and the highest
// panel edge, are the row edges: row 1 is the BOTTOM. The vertical lines'
// places along the wall's location line, with the first and the last panel
// edge, are the column edges: column 1 is at the line's START, the end the
// wall was drawn from - which the answer names as left or right seen from the
// wall's exterior side. A panel belongs to every row and column whose MIDDLE
// its box covers, so a panel merged across a grid line is in both.
//
// A WALL WHOSE GRID HAS A LINE AT AN ANGLE has no rows or columns to count;
// only "all" reaches its panels, and anything else is refused.
//
// FLOOR LINES are the rows each level crosses: a level whose height is inside
// the wall and inside one row. A level within 5 mm of a grid line has no row
// straddling it and is named, never guessed into the row above or below.
//
// A CELL MAY SHOW A WALL INSTEAD OF ITS PANEL. Panel.FindHostPanel names the
// wall Revit draws in a panel's place, so what a cell IS - for the count, and
// for "already that type" - is what it shows.
//
// A CHANGED PANEL MAY COME BACK AS A NEW ELEMENT - a panel that becomes a wall
// cannot keep its id. Nothing is read off the old element after its change:
// the element ChangePanelType returns is read, and the wall's grid is read
// again afterwards for the count by type.
//
// SKIPPED, NOT FAILED: a panel another user holds in a shared model, checked
// first and never taken; and a panel Revit refuses with the two exceptions its
// reference names, with Revit's words and whether the panel is pinned. Nothing
// is unpinned. Any other exception stops the call, and nothing it did is kept.
//
// THE WALL IS SELECTED, NEVER ITS PANELS. A selection of panels is refused,
// naming CHANGE_ELEMENT_TYPE, which changes exactly what was picked: here a
// request meant for a few selected panels could otherwise reach every panel
// of their wall.
//
// NOTHING IS LOADED, and nothing is written until the whole request is
// settled: every refusal of the request is made before the first change.

var changed = 0;
var differs = new List<string>();
var alreadyThatType = 0;
var chosen = 0;
var skipped = new List<string>();
var layout = "";
var measured = "";
var refused = "";
var findings = new List<string>();

var MillimetresPerFoot = 304.8;
// Two edges closer than this are one edge.
var SameEdgeMm = 1.0;
// A level this close to a grid line sits ON it, and no row straddles it.
var OnGridLineMm = 5.0;
// A refusal names at most this many types of one kind, and counts the rest.
var ListedAtMost = 40;
var Forms = "bottom row, top row, row 2, first column, last column, column 3, row 1 column 3, "
    + "floor lines, all";

var sameEdge = SameEdgeMm / MillimetresPerFoot;
var onGridLine = OnGridLineMm / MillimetresPerFoot;

// Lower case, one space between words, commas gone. For THIS file's words -
// the positions - only; a type's name is the model's and is compared exactly.
Func<string, string> plain = text =>
    string.Join(" ", (text ?? "").Trim().ToLowerInvariant().Replace(",", " ")
        .Split((char[])null, StringSplitOptions.RemoveEmptyEntries));

// A type the way the Properties palette writes it - "System Panel: Solid".
Func<ElementType, string> fullName = type =>
{
    if (type == null) return "(no type)";
    string family = "";
    try { family = type.FamilyName; } catch { family = ""; }
    return string.IsNullOrEmpty(family) ? type.Name : family + ": " + type.Name;
};

Func<Element, ElementId> typeIdOf = element =>
{
    if (element == null) return null;
    try { return element.GetTypeId(); } catch { return null; }
};

Func<Element, string> typeNameOf = element =>
{
    var typeId = typeIdOf(element);
    var type = typeId == null ? null : doc.GetElement(typeId) as ElementType;
    return type == null ? "(no type)" : fullName(type);
};

// WHAT A CELL SHOWS: its curtain panel, or the wall Revit draws in its place.
Func<Element, Element> shownBy = element =>
{
    var asPanel = element as Panel;
    if (asPanel == null) return element;
    ElementId hostId = null;
    try { hostId = asPanel.FindHostPanel(); } catch { hostId = null; }
    if (hostId == null || hostId == ElementId.InvalidElementId) return element;
    var hostPanel = doc.GetElement(hostId);
    return hostPanel ?? element;
};

Func<Element, string> describe = element =>
{
    if (element == null) return "(nothing)";
    string category = "", name = "";
    try { category = element.Category == null ? "" : element.Category.Name; } catch { category = ""; }
    try { name = element.Name; } catch { name = ""; }
    return string.Format("'{0}'{1} (id {2})", name, category.Length == 0 ? "" : ", " + category,
        element.Id);
};

Func<double, string> mm = feet => string.Format("{0:F0}", feet * MillimetresPerFoot);

// Who holds an element in a shared model, or "" when nobody else does.
Func<ElementId, string> heldBy = heldId =>
{
    if (heldId == null || !doc.IsWorkshared) return "";
    try
    {
        string owner;
        if (WorksharingUtils.GetCheckoutStatus(doc, heldId, out owner) == CheckoutStatus.OwnedByOtherUser)
            return string.IsNullOrEmpty(owner) ? "another user" : owner;
    }
    catch (Exception) { }
    return "";
};

Func<List<string>, string> listed = names =>
    names.Count == 0 ? "none"
    : names.Count <= ListedAtMost ? string.Join(", ", names)
    : string.Join(", ", names.Take(ListedAtMost)) + string.Format(", and {0} more", names.Count - ListedAtMost);

// THE WALL'S PANELS BY TYPE, off Revit's own list of its cells each time it is
// asked: (count by type's name, count by type's id, empty cells).
Func<Wall, Tuple<SortedDictionary<string, int>, Dictionary<ElementId, int>, int>> census = counted =>
{
    var byName = new SortedDictionary<string, int>(StringComparer.Ordinal);
    var byId = new Dictionary<ElementId, int>();
    var holes = 0;
    CurtainGrid countedGrid = null;
    try { countedGrid = counted == null ? null : counted.CurtainGrid; } catch { countedGrid = null; }
    ICollection<ElementId> countedIds = null;
    try { countedIds = countedGrid == null ? null : countedGrid.GetPanelIds(); } catch { countedIds = null; }
    if (countedIds == null) return null;
    var countedShown = new HashSet<ElementId>();
    foreach (var countedId in countedIds)
    {
        var countedCell = doc.GetElement(countedId);
        if (countedCell == null) { holes++; continue; }
        var countedFace = shownBy(countedCell);
        if (!countedShown.Add(countedFace.Id)) continue;
        var key = "'" + typeNameOf(countedFace) + "'";
        int had;
        byName[key] = byName.TryGetValue(key, out had) ? had + 1 : 1;
        var countedType = typeIdOf(countedFace);
        if (countedType != null) byId[countedType] = byId.TryGetValue(countedType, out had) ? had + 1 : 1;
    }
    return Tuple.Create(byName, byId, holes);
};

Func<Tuple<SortedDictionary<string, int>, Dictionary<ElementId, int>, int>, string> spoken = counts =>
{
    if (counts == null) return "not readable";
    var total = counts.Item1.Values.Sum();
    return string.Format("{0} panel(s){1}{2}", total,
        counts.Item1.Count == 0 ? "" : " - " + string.Join(", ", counts.Item1.Select(p => p.Value + " x " + p.Key)),
        counts.Item3 == 0 ? "" : string.Format(", and {0} empty cell(s)", counts.Item3));
};

// ---------------------------------------------------------------------------
// 1. THE POSITIONS ASKED FOR, read whole before the model is: {row, column},
//    0 for any, -1 for the top row or the last column.
// ---------------------------------------------------------------------------
var asked = new List<int[]>();
var askedWords = new List<string>();
var wantAll = false;
var wantFloorLines = false;
var form = new System.Text.RegularExpressions.Regex(
    @"^(?:row (\d+)|(bottom row)|(top row))?\s*(?:column (\d+)|(first column)|(last column))?$");

if (string.IsNullOrWhiteSpace(panelTypeName))
{
    refused = "No panel type was named. Name it the way Revit writes it - 'System Panel: Solid', a "
        + "curtain wall door type, or a basic wall type for a solid infill - and ask again. Nothing was "
        + "changed";
}
else if (string.IsNullOrWhiteSpace(whichPanels))
{
    refused = "No panels were named. Say which, semicolons between: " + Forms + ". Nothing was changed";
}
else
{
    foreach (var piece in whichPanels.Split(';'))
    {
        var said = plain(piece);
        if (said.Length == 0) continue;
        if (said == "all" || said == "all panels" || said == "every panel")
        {
            wantAll = true;
            askedWords.Add("all");
            continue;
        }
        if (said == "floor lines" || said == "floor line" || said == "every floor line")
        {
            wantFloorLines = true;
            askedWords.Add("floor lines");
            continue;
        }

        var match = form.Match(said);
        if (!match.Success)
        {
            refused = string.Format("'{0}' is not a position this counts. Say which panels, semicolons "
                + "between: {1}. Rows count from the bottom, columns from the wall's start end. Nothing "
                + "was changed", piece.Trim(), Forms);
            break;
        }

        int row = 0, column = 0, number;
        if (match.Groups[1].Success)
        {
            if (!int.TryParse(match.Groups[1].Value, out number) || number < 1)
            {
                refused = string.Format("'{0}': rows count from 1, at the bottom. Nothing was changed", said);
                break;
            }
            row = number;
        }
        else if (match.Groups[2].Success) row = 1;
        else if (match.Groups[3].Success) row = -1;

        if (match.Groups[4].Success)
        {
            if (!int.TryParse(match.Groups[4].Value, out number) || number < 1)
            {
                refused = string.Format("'{0}': columns count from 1, at the wall's start end. Nothing "
                    + "was changed", said);
                break;
            }
            column = number;
        }
        else if (match.Groups[5].Success) column = 1;
        else if (match.Groups[6].Success) column = -1;

        asked.Add(new[] { row, column });
        askedWords.Add(said);
    }

    if (refused.Length == 0 && !wantAll && !wantFloorLines && asked.Count == 0)
        refused = "No panels were named. Say which, semicolons between: " + Forms + ". Nothing was changed";
}

var askedText = string.Join("; ", askedWords);

// ---------------------------------------------------------------------------
// 2. THE ONE CURTAIN WALL, SELECTED ITSELF. A selection of its PANELS is
//    refused, naming CHANGE_ELEMENT_TYPE: panels the modeller picked are that
//    card's job, and a request meant for them must never reach the whole wall
//    here - "make these panels glazed" answered with "all" would.
// ---------------------------------------------------------------------------
Wall curtainWall = null;
var leftAlone = new List<string>();
if (refused.Length == 0)
{
    var reached = new List<Wall>();
    var panelsPicked = new List<string>();
    Dictionary<ElementId, Wall> wallOfCell = null;
    var onASystem = 0;
    foreach (var element in elements ?? new List<Element>())
    {
        if (element == null) continue;
        Wall found = null;
        var asWall = element as Wall;
        CurtainGrid ownGrid = null;
        if (asWall != null) { try { ownGrid = asWall.CurtainGrid; } catch { ownGrid = null; } }
        if (ownGrid != null) found = asWall;
        else
        {
            // A PANEL IS KNOWN BY BEING IN ITS WALL'S OWN LIST OF CELLS - the
            // wall's word for it, read rather than assumed from Host.
            if (wallOfCell == null)
            {
                wallOfCell = new Dictionary<ElementId, Wall>();
                foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(Wall)))
                {
                    var curtain = candidate as Wall;
                    CurtainGrid cells = null;
                    try { cells = curtain == null ? null : curtain.CurtainGrid; } catch { cells = null; }
                    ICollection<ElementId> cellList = null;
                    try { cellList = cells == null ? null : cells.GetPanelIds(); } catch { cellList = null; }
                    if (cellList == null) continue;
                    foreach (var listedId in cellList)
                    {
                        wallOfCell[listedId] = curtain;
                        var listedCell = doc.GetElement(listedId);
                        var listedFace = listedCell == null ? null : shownBy(listedCell);
                        if (listedFace != null && !listedFace.Id.Equals(listedId)) wallOfCell[listedFace.Id] = curtain;
                    }
                }
            }
            Wall owner;
            if (wallOfCell.TryGetValue(element.Id, out owner))
            {
                panelsPicked.Add(describe(element) + " on curtain wall " + owner.Id);
                continue;
            }
        }

        if (found == null)
        {
            Element host = null;
            var hosted = element as FamilyInstance;
            try { host = hosted == null ? null : hosted.Host; } catch { host = null; }
            if (element is Panel && host is CurtainSystem) onASystem++;
            leftAlone.Add(describe(element));
            continue;
        }
        if (!reached.Any(w => w.Id.Equals(found.Id))) reached.Add(found);
    }

    if (panelsPicked.Count > 0)
    {
        refused = string.Format("{0} curtain panel(s) are selected, not a curtain wall: {1}. To change "
            + "just the panels picked, CHANGE_ELEMENT_TYPE does it with them selected. To pick panels "
            + "here by row, column or floor line, select the curtain wall itself - Tab to it - and ask "
            + "again. Nothing was changed", panelsPicked.Count, listed(panelsPicked));
    }
    else if (reached.Count == 0)
    {
        refused = leftAlone.Count == 0
            ? "Nothing is selected. Select the curtain wall itself - Tab to it - and ask again. Nothing "
                + "was changed"
            : string.Format("None of what is selected is a curtain wall: {0}.{1} Select the curtain wall "
                + "itself - Tab to it - and ask again. Nothing was changed",
                listed(leftAlone), onASystem == 0 ? ""
                    : string.Format(" {0} of them is a curtain SYSTEM's panel - rows and columns are "
                        + "counted on a curtain wall, and a curtain system is not done here yet.", onASystem));
    }
    else if (reached.Count > 1)
    {
        refused = string.Format("The selection reaches {0} curtain walls ({1}). Rows and columns are "
            + "counted on one wall, so select one and ask again. Nothing was changed", reached.Count,
            string.Join(", ", reached.Select(w => "id " + w.Id)));
    }
    else
    {
        curtainWall = reached[0];
        var wallHolder = heldBy(curtainWall.Id);
        if (wallHolder.Length > 0)
            refused = string.Format("Curtain wall {0} is held by {1} in this shared model - they "
                + "relinquish it, then ask again. Nothing was changed", curtainWall.Id, wallHolder);
    }
}

// ---------------------------------------------------------------------------
// 3. THE NEW TYPE, BY NAME - exactly one curtain panel, door, window or wall
//    type, or the whole request is refused (D-54 s3).
// ---------------------------------------------------------------------------
ElementType newType = null;
var newTypeKind = "";
if (refused.Length == 0)
{
    var panelCategory = new ElementId(BuiltInCategory.OST_CurtainWallPanels);
    var doorCategory = new ElementId(BuiltInCategory.OST_Doors);
    var windowCategory = new ElementId(BuiltInCategory.OST_Windows);
    var candidates = new List<ElementType>();
    var kinds = new List<string>();
    var curtainWallTypes = new List<ElementType>();
    foreach (var element in new FilteredElementCollector(doc).WhereElementIsElementType())
    {
        var asWallType = element as WallType;
        if (asWallType != null)
        {
            WallKind kind;
            try { kind = asWallType.Kind; } catch { continue; }
            if (kind == WallKind.Basic) { candidates.Add(asWallType); kinds.Add("a basic wall type"); }
            else if (kind == WallKind.Stacked) { candidates.Add(asWallType); kinds.Add("a stacked wall type"); }
            else if (kind == WallKind.Curtain) curtainWallTypes.Add(asWallType);
            continue;
        }
        var symbol = element as FamilySymbol;
        if (symbol == null) continue;
        ElementId categoryId = null;
        try { categoryId = symbol.Category == null ? null : symbol.Category.Id; } catch { categoryId = null; }
        var isPanelType = symbol is PanelType;
        if (categoryId != null && categoryId.Equals(doorCategory))
        {
            candidates.Add(symbol);
            kinds.Add(isPanelType ? "a curtain wall door type" : "a door type");
        }
        else if (categoryId != null && categoryId.Equals(windowCategory))
        {
            candidates.Add(symbol);
            kinds.Add(isPanelType ? "a curtain wall window type" : "a window type");
        }
        else if (isPanelType || (categoryId != null && categoryId.Equals(panelCategory)))
        {
            candidates.Add(symbol);
            kinds.Add("a curtain panel type");
        }
    }

    var wanted = panelTypeName.Trim();
    Func<ElementType, bool> isNamed = type =>
    {
        string name = "", family = "";
        try { name = type.Name; family = type.FamilyName; } catch { return false; }
        return string.Equals(name, wanted, StringComparison.Ordinal)
            || (!string.IsNullOrEmpty(family)
                && (string.Equals(family + ": " + name, wanted, StringComparison.Ordinal)
                    || string.Equals(family + " : " + name, wanted, StringComparison.Ordinal)));
    };

    var hits = new List<int>();
    for (var c = 0; c < candidates.Count; c++)
        if (isNamed(candidates[c])) hits.Add(c);

    if (hits.Count == 0)
    {
        var curtainHit = curtainWallTypes.FirstOrDefault(isNamed);
        if (curtainHit != null)
        {
            refused = string.Format("'{0}' is a curtain wall type. A curtain panel takes a curtain panel, "
                + "door or window type, or a basic or stacked wall type. Nothing was changed", fullName(curtainHit));
        }
        else
        {
            Func<Func<string, bool>, List<string>> namesOf = keep =>
                Enumerable.Range(0, candidates.Count).Where(c => keep(kinds[c]))
                    .Select(c => fullName(candidates[c])).Distinct()
                    .OrderBy(n => n, StringComparer.Ordinal).ToList();
            refused = string.Format(
                "No panel type called '{0}' in this model - capitals count. Its curtain panel types: {1}. "
                + "Its door and window types (Revit puts only a curtain wall door or window in a panel): "
                + "{2}. Its basic and stacked wall types, any of which can fill a panel as a solid infill: "
                + "{3}. A family that is not in the model has to be loaded first - Insert > Load Family; "
                + "Heron does not load families. Nothing was changed", wanted,
                listed(namesOf(k => k == "a curtain panel type")),
                listed(namesOf(k => k.Contains("door") || k.Contains("window"))),
                listed(namesOf(k => k.Contains("wall type"))));
        }
    }
    else if (hits.Count > 1)
    {
        refused = string.Format("{0} types are called '{1}', so the name does not say which is meant. Say "
            + "which: {2}. Nothing was changed", hits.Count, wanted,
            string.Join(", or ", hits.Select(c => "'" + fullName(candidates[c]) + "' (" + kinds[c] + ")").Distinct()));
    }
    else
    {
        newType = candidates[hits[0]];
        newTypeKind = kinds[hits[0]];
    }
}

// ---------------------------------------------------------------------------
// 4. THE WALL, MEASURED: every cell, where its box sits, and the grid's lines
//    by the way each one runs.
// ---------------------------------------------------------------------------
CurtainGrid wallGrid = null;
Curve path = null;
var cellIds = new List<ElementId>();
var cellFaces = new List<Element>();
var lowest = new List<double>();
var highest = new List<double>();
var startsAt = new List<double>();
var endsAt = new List<double>();
var rowsOf = new List<List<int>>();
var columnsOf = new List<List<int>>();
var rowEdges = new List<double>();
var columnEdges = new List<double>();
var unmeasured = 0;
var angled = 0;
var startSide = "";
var floorLineNotes = new List<string>();
var picked = new HashSet<int>();
Tuple<SortedDictionary<string, int>, Dictionary<ElementId, int>, int> before = null;

// Where along the location line a point sits, in feet from its start.
Func<XYZ, double> alongOf = point =>
{
    if (path == null || point == null) return double.NaN;
    try
    {
        var hit = path.Project(point);
        return hit == null ? double.NaN : path.ComputeNormalizedParameter(hit.Parameter) * path.Length;
    }
    catch (Exception) { return double.NaN; }
};

// Which way a curve runs: 0 vertical, 1 horizontal, 2 at an angle, -1 not
// readable - against Revit's own angle tolerance, the way SET_CURTAIN_WALL_GRID
// measures it.
var steep = Math.Cos(doc.Application.AngleTolerance);
var flat = Math.Sin(doc.Application.AngleTolerance);
Func<Curve, int> runs = curve =>
{
    if (curve == null) return -1;
    try
    {
        var along = curve.GetEndPoint(1) - curve.GetEndPoint(0);
        var length = along.GetLength();
        if (length <= 0) return -1;
        var rise = Math.Abs(along.Z) / length;
        if (rise >= steep) return 0;
        if (rise <= flat) return 1;
        return 2;
    }
    catch { return -1; }
};

// Edges: the first, every cut strictly inside and more than SameEdgeMm past
// the last kept, and the last.
Func<List<double>, double, double, List<double>> edgesOf = (cuts, first, last) =>
{
    var kept = new List<double> { first };
    foreach (var cut in cuts.OrderBy(v => v))
        if (cut > kept[kept.Count - 1] + sameEdge && cut < last - sameEdge) kept.Add(cut);
    kept.Add(last);
    return kept;
};

Func<List<double>, double, string> bandsOf = (edges, origin) =>
{
    var bands = new List<string>();
    for (var b = 1; b < edges.Count; b++) bands.Add(mm(edges[b - 1] - origin) + "-" + mm(edges[b] - origin));
    return listed(bands);
};

Func<List<int>, string, string> span = (numbers, word) =>
    numbers.Count == 0 ? word + " -"
    : numbers.Count == 1 ? word + " " + numbers[0]
    : word + "s " + numbers.First() + "-" + numbers.Last();

if (refused.Length == 0)
{
    try { wallGrid = curtainWall.CurtainGrid; } catch { wallGrid = null; }
    var located = curtainWall.Location as LocationCurve;
    path = located == null ? null : located.Curve;
    before = census(curtainWall);

    ICollection<ElementId> gridCells = null;
    try { gridCells = wallGrid == null ? null : wallGrid.GetPanelIds(); } catch { gridCells = null; }
    if (gridCells == null)
    {
        refused = string.Format("Revit gives no list of panels for curtain wall {0}, so none can be "
            + "picked. Nothing was changed", curtainWall.Id);
    }
    else
    {
        var faceSeen = new HashSet<ElementId>();
        foreach (var gridCellId in gridCells)
        {
            var gridCell = doc.GetElement(gridCellId);
            if (gridCell == null) continue;          // an empty cell: nothing there to change
            var face = shownBy(gridCell);
            if (!faceSeen.Add(face.Id)) continue;
            cellIds.Add(gridCellId);
            cellFaces.Add(face);

            BoundingBoxXYZ box = null;
            try { box = face.get_BoundingBox(null); } catch { box = null; }
            double down = double.NaN, up = double.NaN, near = double.NaN, far = double.NaN;
            if (box != null)
            {
                down = box.Min.Z;
                up = box.Max.Z;
                var corners = new[]
                {
                    new XYZ(box.Min.X, box.Min.Y, box.Min.Z), new XYZ(box.Min.X, box.Max.Y, box.Min.Z),
                    new XYZ(box.Max.X, box.Min.Y, box.Min.Z), new XYZ(box.Max.X, box.Max.Y, box.Min.Z),
                };
                foreach (var corner in corners)
                {
                    var at = alongOf(corner);
                    if (double.IsNaN(at)) continue;
                    near = double.IsNaN(near) ? at : Math.Min(near, at);
                    far = double.IsNaN(far) ? at : Math.Max(far, at);
                }
            }
            else unmeasured++;
            lowest.Add(down);
            highest.Add(up);
            startsAt.Add(near);
            endsAt.Add(far);
        }

        // THE GRID'S LINES, by the way each runs - never by U and V.
        var rowCuts = new List<double>();
        var columnCuts = new List<double>();
        var lineIds = new List<ElementId>();
        try { lineIds.AddRange(wallGrid.GetUGridLineIds()); lineIds.AddRange(wallGrid.GetVGridLineIds()); }
        catch (Exception) { }
        foreach (var lineId in lineIds)
        {
            var gridLine = doc.GetElement(lineId) as CurtainGridLine;
            Curve full = null;
            try { full = gridLine == null ? null : gridLine.FullCurve; } catch { full = null; }
            var way = runs(full);
            if (way == 0)
            {
                var at = alongOf(full.GetEndPoint(0));
                if (!double.IsNaN(at)) columnCuts.Add(at);
            }
            else if (way == 1) rowCuts.Add(full.GetEndPoint(0).Z);
            else if (way == 2) angled++;
        }

        var heights = Enumerable.Range(0, cellIds.Count).Where(i => !double.IsNaN(lowest[i])).ToList();
        var lengths = Enumerable.Range(0, cellIds.Count).Where(i => !double.IsNaN(startsAt[i])).ToList();
        var bottom = heights.Count == 0 ? double.NaN : heights.Min(i => lowest[i]);
        var top = heights.Count == 0 ? double.NaN : heights.Max(i => highest[i]);
        if (heights.Count > 0) rowEdges = edgesOf(rowCuts, bottom, top);
        if (lengths.Count > 0) columnEdges = edgesOf(columnCuts, lengths.Min(i => startsAt[i]), lengths.Max(i => endsAt[i]));
        var rows = Math.Max(0, rowEdges.Count - 1);
        var columns = Math.Max(0, columnEdges.Count - 1);

        // A panel is in every row and column whose MIDDLE its box covers.
        for (var i = 0; i < cellIds.Count; i++)
        {
            var inRows = new List<int>();
            var inColumns = new List<int>();
            for (var r = 1; r <= rows; r++)
            {
                var middle = (rowEdges[r - 1] + rowEdges[r]) / 2;
                if (!double.IsNaN(lowest[i]) && lowest[i] < middle && highest[i] > middle) inRows.Add(r);
            }
            for (var k = 1; k <= columns; k++)
            {
                var middle = (columnEdges[k - 1] + columnEdges[k]) / 2;
                if (!double.IsNaN(startsAt[i]) && startsAt[i] < middle && endsAt[i] > middle) inColumns.Add(k);
            }
            rowsOf.Add(inRows);
            columnsOf.Add(inColumns);
        }

        // WHICH END COLUMN 1 IS, seen from the wall's exterior side.
        try
        {
            if (path != null)
            {
                var chord = path.GetEndPoint(1) - path.GetEndPoint(0);
                var right = curtainWall.Orientation.Negate().CrossProduct(XYZ.BasisZ);
                var lean = chord.DotProduct(right);
                startSide = lean > sameEdge ? ", its LEFT end seen from outside"
                    : lean < -sameEdge ? ", its RIGHT end seen from outside" : "";
            }
        }
        catch (Exception) { startSide = ""; }

        var byPosition = asked.Count > 0 || wantFloorLines;
        if (byPosition && angled > 0)
        {
            refused = string.Format("Curtain wall {0} has {1} grid line(s) at an angle, so its panels do "
                + "not sit in rows and columns this can count. 'all' still reaches every panel on it. "
                + "Nothing was changed", curtainWall.Id, angled);
        }
        else if (byPosition && rows == 0)
        {
            refused = string.Format("No panel of curtain wall {0} has a box Revit can measure, so no row "
                + "or column can be counted. 'all' still reaches them. Nothing was changed", curtainWall.Id);
        }
        else if (asked.Any(p => p[1] != 0) && columns == 0)
        {
            refused = string.Format("Revit gives curtain wall {0} no location line to count columns "
                + "along. Rows and 'all' still work. Nothing was changed", curtainWall.Id);
        }

        // THE PANELS AT EACH POSITION ASKED FOR.
        foreach (var position in asked)
        {
            if (refused.Length > 0) break;
            var row = position[0] == -1 ? rows : position[0];
            var column = position[1] == -1 ? columns : position[1];
            if (position[0] != 0 && row > rows)
            {
                refused = string.Format("Row {0} is not on curtain wall {1}: it has {2} row(s), counted from "
                    + "the bottom. Nothing was changed", row, curtainWall.Id, rows);
                break;
            }
            if (position[1] != 0 && column > columns)
            {
                refused = string.Format("Column {0} is not on curtain wall {1}: it has {2} column(s), "
                    + "counted from its start end{3}. Nothing was changed", column, curtainWall.Id, columns,
                    startSide);
                break;
            }
            for (var i = 0; i < cellIds.Count; i++)
            {
                if (position[0] != 0 && !rowsOf[i].Contains(row)) continue;
                if (position[1] != 0 && !columnsOf[i].Contains(column)) continue;
                picked.Add(i);
            }
        }

        // FLOOR LINES: the row each level crosses.
        if (refused.Length == 0 && wantFloorLines)
        {
            var levels = new List<Level>();
            foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Level)))
            {
                var asLevel = element as Level;
                if (asLevel != null) levels.Add(asLevel);
            }
            foreach (var level in levels.OrderBy(l => l.ProjectElevation))
            {
                var height = level.ProjectElevation;
                if (height <= bottom + onGridLine || height >= top - onGridLine) continue;
                var crossed = 0;
                for (var r = 1; r <= rows; r++)
                    if (height > rowEdges[r - 1] + onGridLine && height < rowEdges[r] - onGridLine) crossed = r;
                if (crossed == 0)
                {
                    floorLineNotes.Add(string.Format("'{0}', {1} mm above the wall's base, is ON a grid line, "
                        + "so no row straddles it - name the row above or below by its number", level.Name,
                        mm(height - bottom)));
                    continue;
                }
                floorLineNotes.Add(string.Format("'{0}', {1} mm above the wall's base: row {2}", level.Name,
                    mm(height - bottom), crossed));
                for (var i = 0; i < cellIds.Count; i++)
                    if (rowsOf[i].Contains(crossed)) picked.Add(i);
            }
            if (floorLineNotes.Count == 0)
                floorLineNotes.Add(string.Format("no level crosses curtain wall {0} between its base and its "
                    + "top", curtainWall.Id));
        }

        if (refused.Length == 0 && wantAll)
            for (var i = 0; i < cellIds.Count; i++) picked.Add(i);

        layout = string.Format(
            "wall {0}: {1} row(s), counted from the bottom - mm above its lowest panel edge: {2}. {3} "
            + "column(s), counted from its start end{4} - mm along its location line: {5}{6}{7}{8}",
            curtainWall.Id, rows, rows == 0 ? "none" : bandsOf(rowEdges, bottom), columns, startSide,
            columns == 0 ? "none" : bandsOf(columnEdges, 0),
            angled == 0 ? "" : string.Format(". {0} grid line(s) at an angle", angled),
            unmeasured == 0 ? "" : string.Format(". {0} panel(s) with no box to measure, reached only by "
                + "'all'", unmeasured),
            floorLineNotes.Count == 0 ? "" : ". Floor lines: " + string.Join("; ", floorLineNotes));

        if (refused.Length == 0 && picked.Count == 0)
        {
            refused = string.Format("Nothing on curtain wall {0} sits at '{1}'{2}. See layout for its rows "
                + "and columns. Nothing was changed", curtainWall.Id, askedText,
                floorLineNotes.Count == 0 ? "" : " - " + string.Join("; ", floorLineNotes));
        }
    }
}

// ---------------------------------------------------------------------------
// 5. THE CHANGE, panel by panel, bottom row first.
// ---------------------------------------------------------------------------
if (refused.Length == 0)
{
    chosen = picked.Count;
    var order = picked
        .OrderBy(i => rowsOf[i].Count == 0 ? int.MaxValue : rowsOf[i].Min())
        .ThenBy(i => columnsOf[i].Count == 0 ? int.MaxValue : columnsOf[i].Min())
        .ToList();

    Func<int, string> placeOf = i => string.Format("{0}, {1} (id {2})", span(rowsOf[i], "row"),
        span(columnsOf[i], "column"), cellIds[i]);

    var unlocked = new HashSet<ElementId>();
    var lockRead = true;
    try { foreach (var openId in wallGrid.GetUnlockedPanelIds()) unlocked.Add(openId); }
    catch (Exception) { lockRead = false; }
    var lockedChosen = lockRead ? order.Count(i => !unlocked.Contains(cellIds[i])) : -1;
    var pinnedChosen = order.Count(i =>
    {
        try { return cellFaces[i].Pinned || (doc.GetElement(cellIds[i]) ?? cellFaces[i]).Pinned; }
        catch { return false; }
    });

    var attempts = 0;
    var notSuitable = 0;
    var firstWords = "";
    var newIds = 0;
    foreach (var i in order)
    {
        var cellNow = doc.GetElement(cellIds[i]);
        if (cellNow == null)
        {
            skipped.Add(placeOf(i) + ": no longer in the model when it was reached");
            continue;
        }
        var faceNow = shownBy(cellNow);
        var typeNow = typeIdOf(faceNow);
        if (typeNow != null && typeNow.Equals(newType.Id)) { alreadyThatType++; continue; }

        var holder = heldBy(cellNow.Id);
        if (holder.Length == 0 && !faceNow.Id.Equals(cellNow.Id)) holder = heldBy(faceNow.Id);
        if (holder.Length > 0)
        {
            skipped.Add(string.Format("{0}: held by {1} in this shared model - they relinquish it, then "
                + "ask again", placeOf(i), holder));
            continue;
        }

        var pinned = false;
        try { pinned = cellNow.Pinned || faceNow.Pinned; } catch { pinned = false; }
        var pinNote = pinned ? " It is pinned; Heron does not unpin it" : "";

        CurtainGrid changing = null;
        try { changing = curtainWall.CurtainGrid; } catch { changing = null; }
        if (changing == null)
            throw new InvalidOperationException(string.Format("Curtain wall {0} lost its curtain grid part-way "
                + "through. Nothing this call did is kept", curtainWall.Id));

        attempts++;
        Element returned;
        try
        {
            returned = changing.ChangePanelType(cellNow, newType);
        }
        // THE TWO EXCEPTIONS ChangePanelType's REFERENCE NAMES, matched by their
        // type's name: the add-in's layer is the one place that spells Revit's
        // namespaces out (tools/check-structure.py). Anything else is not
        // caught here - it stops the call, and nothing it did is kept.
        catch (Exception unsuitable) when (unsuitable.GetType().Name == "ArgumentException")
        {
            notSuitable++;
            if (firstWords.Length == 0) firstWords = unsuitable.Message;
            skipped.Add(string.Format("{0}: Revit says '{1}' cannot be used for this panel - {2}.{3}",
                placeOf(i), fullName(newType), unsuitable.Message, pinNote));
            continue;
        }
        catch (Exception notTaken) when (notTaken.GetType().Name == "InvalidOperationException")
        {
            skipped.Add(string.Format("{0}: Revit could not change it - {1}.{2}", placeOf(i),
                notTaken.Message, pinNote));
            continue;
        }

        if (returned == null)
        {
            differs.Add(placeOf(i) + ": Revit took the change and handed back no panel");
            continue;
        }
        var resultFace = shownBy(returned);
        var resultType = typeIdOf(resultFace);
        if (resultType != null && resultType.Equals(newType.Id))
        {
            changed++;
            if (!returned.Id.Equals(cellIds[i])) newIds++;
        }
        else
        {
            differs.Add(string.Format("{0}: Revit took the change, and the panel shows '{1}'", placeOf(i),
                typeNameOf(resultFace)));
        }
    }

    // THE REBUILD IS NOT SWALLOWED - a failure leaves a model that must not be
    // read again, and the executor rolls the whole call back.
    if (changed > 0 || differs.Count > 0)
    {
        try
        {
            doc.Regenerate();
        }
        catch (Exception notRebuilt)
        {
            throw new InvalidOperationException(string.Format("Revit could not rebuild curtain wall {0} with "
                + "these panels - {1}. Nothing this call did is kept", curtainWall.Id, notRebuilt.Message));
        }
    }

    if (attempts > 0 && notSuitable == attempts)
    {
        refused = string.Format("Revit would not put '{0}' ({1}) in any of the {2} panel(s) tried - "
            + "'{3}'. A door or window has to be a curtain wall door or window family to fill a panel. "
            + "Nothing was changed", fullName(newType), newTypeKind, attempts, firstWords);
    }

    // READ AGAIN OFF THE WALL - what Revit holds, never what was asked.
    var after = census(doc.GetElement(curtainWall.Id) as Wall);
    int beforeOfNew = 0, afterOfNew = 0;
    if (before != null) before.Item2.TryGetValue(newType.Id, out beforeOfNew);
    if (after != null) after.Item2.TryGetValue(newType.Id, out afterOfNew);

    measured = string.Format(
        "wall {0}. Before: {1}. After, read off its grid again: {2}. '{3}' is {4} - a {5} to Revit's "
        + "API. Of the {6} chosen, Revit counted {7} locked and {8} pinned before the change. {9} of the "
        + "{10} changed came back as a new element, with a new id",
        curtainWall.Id, spoken(before), spoken(after), fullName(newType), newTypeKind,
        newType.GetType().Name, chosen, lockedChosen < 0 ? "an unknown number" : lockedChosen.ToString(),
        pinnedChosen, newIds, changed);

    if (refused.Length > 0)
    {
        findings.Add(refused);
    }
    else
    {
        findings.Add(changed > 0
            ? string.Format("Curtain wall {0}: {1} panel(s) at '{2}' now show '{3}' - read back off Revit, "
                + "not counted off the call. {4} already did; {5} skipped", curtainWall.Id, changed,
                askedText, fullName(newType), alreadyThatType, skipped.Count)
            : string.Format("Curtain wall {0}: nothing changed. {1} of the {2} panel(s) at '{3}' already "
                + "showed '{4}'; {5} skipped", curtainWall.Id, alreadyThatType, chosen, askedText,
                fullName(newType), skipped.Count));
        if (changed > 0)
            findings.Add("Only these panels changed. The wall's type, and the Curtain Panel it gives a "
                + "new cell, are as they were");
    }
    if (after != null && before != null && afterOfNew != beforeOfNew + changed)
    {
        findings.Add(string.Format("THE COUNTS DO NOT ADD UP: '{0}' was on {1} panel(s) of the wall before "
            + "and is on {2} now, and {3} were changed here - see measured", fullName(newType), beforeOfNew,
            afterOfNew, changed));
    }
    if (differs.Count > 0)
        findings.Add(string.Format("{0} panel(s) took the call and do NOT show '{1}' - see differs",
            differs.Count, fullName(newType)));
    if (skipped.Count > 0)
        findings.Add(string.Format("{0} panel(s) skipped - see skipped", skipped.Count));
    if (leftAlone.Count > 0)
        findings.Add(string.Format("{0} selected element(s) are not on this wall and were left alone: {1}",
            leftAlone.Count, listed(leftAlone)));
    findings.Add("Rows count from the bottom, columns from the wall's start end" + startSide
        + " - see layout");
}
else
{
    findings.Add(refused);
}
