// NOT STANDALONE. Assumes `doc`, `elements`, `distance`, `mode` and `towards`
// are in scope; leaves `results`, `offsetCount`, `notOffset`, `refused`,
// `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// REVIT'S MODIFY > OFFSET, FOR LINES AND WALLS. The model lines, detail lines
// and walls handed in are sorted into CHAINS - pieces whose ends meet, as
// Revit's Tab selects them - and each chain is offset as ONE CurveLoop with
// CurveLoop.CreateViaOffset, so the corners stay joined: every piece is
// lengthened or shortened to meet its neighbour, never left overlapping or
// apart. A piece on its own is a chain of one. A COPY is the default, as
// Revit's own Copy box is ticked by default; `mode` move moves the originals.
//
// WHICH SIDE. RevitAPI.xml: a positive distance pushes every point "to the
// right" of the chain, walking along it with the plane's normal as up. Which
// way a chain walks depends on how it was drawn, so `towards` - a point in mm
// on the side wanted, as Revit's own cursor is - turns the sign round when the
// first piece's offset would land away from it. Left out, the sign decides,
// and the answer says which way each chain went.
//
// THE PLANE. A model or detail line is offset in its own sketch plane (its
// normal); a wall along its location line in plan (the world vertical). Lines
// and walls never share a chain, nor do detail lines of two views or pieces on
// planes that are not parallel.
//
// A COPY IS MADE WITH ElementTransformUtils.CopyElement, so it keeps the
// line style, the wall type and everything else the original had - then its
// curve is set to the offset one. A wall's doors and windows come with a
// copied wall, as Revit's own copy brings them, and are named.
//
// READ BACK. After one regeneration every curve is read again and compared
// with the one set; a piece Revit moved to keep a join is named, never
// counted as exact.

var invariant = System.Globalization.CultureInfo.InvariantCulture;
const double MillimetresPerFoot = 304.8;
Func<double, string> mm = feet => Math.Round(feet * MillimetresPerFoot, 1).ToString("0.#", invariant);
Func<XYZ, string> direction = v => "(" + v.X.ToString("0.###", invariant) + ", " + v.Y.ToString("0.###", invariant)
    + ", " + v.Z.ToString("0.###", invariant) + ")";

var results = new List<ElementId>();
var offsetCount = 0;
var notOffset = new List<ElementId>();
var refused = new List<string>();
var report = "";
var findings = new List<string>();

// An optional need left out arrives as the text "none" (row 5b-1432).
var modeWord = (mode ?? "").Trim().ToLowerInvariant();
if (string.Equals(modeWord, "none", StringComparison.OrdinalIgnoreCase)) modeWord = "";
if (modeWord.Length == 0) modeWord = "copy";
var towardsText = (towards ?? "").Trim();
if (string.Equals(towardsText, "none", StringComparison.OrdinalIgnoreCase)) towardsText = "";

XYZ towardsPoint = null;
var stop = "";
if (modeWord != "copy" && modeWord != "move")
    stop = "mode \"" + (mode ?? "") + "\" is not copy or move. Nothing was offset.";
else if (double.IsNaN(distance) || double.IsInfinity(distance) || Math.Abs(distance) < 0.05)
    stop = "The distance must be a length in millimetres other than 0 - " + distance.ToString(invariant)
        + " was given. Nothing was offset.";
else if (towardsText.Length > 0)
{
    var parts = towardsText.Split(',');
    var values = new List<double>();
    foreach (var part in parts)
    {
        double v;
        if (double.TryParse(part.Trim(), System.Globalization.NumberStyles.Float, invariant, out v)) values.Add(v);
    }
    if ((parts.Length == 2 || parts.Length == 3) && values.Count == parts.Length)
        towardsPoint = new XYZ(values[0] / MillimetresPerFoot, values[1] / MillimetresPerFoot,
            values.Count == 3 ? values[2] / MillimetresPerFoot : 0.0);
    else
        stop = "towards \"" + towardsText + "\" is not a point - write it as x,y or x,y,z in millimetres. "
            + "Nothing was offset.";
}
if (stop.Length == 0 && (elements == null || elements.Count == 0))
    stop = "Nothing was handed in - select the model lines, detail lines or walls to offset. Nothing was offset.";

// ---- what can be offset: lines and walls, each with its curve and plane ----
var ids = new List<ElementId>();
var curves = new List<Curve>();
var normals = new List<XYZ>();
var isWall = new List<bool>();
var ownerViews = new List<ElementId>();

if (stop.Length == 0)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        Curve curve = null;
        XYZ normal = null;
        var wall = element as Wall;
        var line = element as CurveElement;
        if (wall != null)
        {
            var location = wall.Location as LocationCurve;
            if (location != null) curve = location.Curve;
            normal = XYZ.BasisZ;
        }
        else if (line != null && (line is ModelCurve || line is DetailCurve))
        {
            try
            {
                curve = line.GeometryCurve;
                if (line.SketchPlane != null) normal = line.SketchPlane.GetPlane().Normal;
            }
            catch (Exception) { curve = null; }
        }
        if (curve == null || normal == null || !curve.IsBound)
        {
            notOffset.Add(element.Id);
            continue;
        }
        ids.Add(element.Id);
        curves.Add(curve);
        normals.Add(normal.Normalize());
        isWall.Add(wall != null);
        ownerViews.Add(element.OwnerViewId);
    }
    if (ids.Count == 0)
        stop = "None of the " + elements.Count + " element(s) handed in is a model line, a detail line or a wall "
            + "with a location line. Nothing was offset.";
}

// ---- chains: pieces whose ends meet, of one kind, one plane and one view ----
const double Touch = 0.001; // feet, about 0.3 mm
Func<int, bool, XYZ> startOf = (i, reversed) => curves[i].GetEndPoint(reversed ? 1 : 0);
Func<int, bool, XYZ> endOf = (i, reversed) => curves[i].GetEndPoint(reversed ? 0 : 1);
Func<int, int, bool> together = (a, b) =>
    isWall[a] == isWall[b]
    && ownerViews[a] == ownerViews[b]
    && Math.Abs(Math.Abs(normals[a].DotProduct(normals[b])) - 1.0) < 1e-6;

var chains = new List<List<int>>();
var flips = new List<List<bool>>();
var used = new bool[ids.Count];
for (var s = 0; s < ids.Count && stop.Length == 0; s++)
{
    if (used[s]) continue;
    used[s] = true;
    var order = new List<int> { s };
    var flipped = new List<bool> { false };
    var grew = true;
    while (grew)
    {
        grew = false;
        var tail = endOf(order[order.Count - 1], flipped[flipped.Count - 1]);
        for (var j = 0; j < ids.Count; j++)
        {
            if (used[j] || !together(s, j)) continue;
            if (curves[j].GetEndPoint(0).DistanceTo(tail) < Touch) { order.Add(j); flipped.Add(false); }
            else if (curves[j].GetEndPoint(1).DistanceTo(tail) < Touch) { order.Add(j); flipped.Add(true); }
            else continue;
            used[j] = true;
            grew = true;
            break;
        }
    }
    grew = true;
    while (grew)
    {
        grew = false;
        var head = startOf(order[0], flipped[0]);
        for (var j = 0; j < ids.Count; j++)
        {
            if (used[j] || !together(s, j)) continue;
            if (curves[j].GetEndPoint(1).DistanceTo(head) < Touch) { order.Insert(0, j); flipped.Insert(0, false); }
            else if (curves[j].GetEndPoint(0).DistanceTo(head) < Touch) { order.Insert(0, j); flipped.Insert(0, true); }
            else continue;
            used[j] = true;
            grew = true;
            break;
        }
    }
    chains.Add(order);
    flips.Add(flipped);
}

// ---- offset each chain as one loop, then copy or move its pieces ----
var feet = distance / MillimetresPerFoot;
var settled = new List<KeyValuePair<ElementId, Curve>>();
var chainNumber = 0;
foreach (var order in chains)
{
    var flipped = flips[chainNumber];
    chainNumber++;
    var label = "Chain " + chainNumber + " (" + order.Count + " " + (isWall[order[0]] ? "wall" : "line")
        + (order.Count == 1 ? "" : "s") + ", first id " + ids[order[0]] + ")";
    var normal = normals[order[0]];

    var loop = new CurveLoop();
    var oriented = new List<Curve>();
    try
    {
        for (var k = 0; k < order.Count; k++)
        {
            var piece = flipped[k] ? curves[order[k]].CreateReversed() : curves[order[k]];
            oriented.Add(piece);
            loop.Append(piece);
        }
    }
    catch (Exception ex)
    {
        refused.Add(label + ": Revit would not join its pieces into one chain - " + ex.Message + " Nothing in it was offset.");
        continue;
    }

    var shifted = new List<Curve>();
    var usedFeet = feet;
    try
    {
        var offsetLoop = CurveLoop.CreateViaOffset(loop, usedFeet, normal);
        foreach (Curve c in offsetLoop) shifted.Add(c);
        if (towardsPoint != null && shifted.Count > 0)
        {
            var from = oriented[0].Evaluate(0.5, true);
            var went = shifted[0].Evaluate(0.5, true) - from;
            if (went.DotProduct(towardsPoint - from) < 0)
            {
                usedFeet = -feet;
                shifted.Clear();
                foreach (Curve c in CurveLoop.CreateViaOffset(loop, usedFeet, normal)) shifted.Add(c);
            }
        }
    }
    catch (Exception ex)
    {
        refused.Add(label + ": Revit could not offset it by " + mm(Math.Abs(feet)) + " mm - " + ex.Message
            + " (an arc offset inward past its own centre, or an ellipse or spline that cannot be trimmed, is the usual "
            + "reason). Nothing in it was offset.");
        continue;
    }

    if (shifted.Count != order.Count)
    {
        refused.Add(label + ": the offset came back with " + shifted.Count + " piece(s) for " + order.Count
            + " - a piece too short to survive the offset is the usual reason. Nothing in it was offset.");
        continue;
    }

    var side = shifted[0].Evaluate(0.5, true) - oriented[0].Evaluate(0.5, true);
    findings.Add(label + ": offset " + mm(Math.Abs(feet)) + " mm towards " + direction(side.Normalize())
        + " (a direction, not a point)" + (usedFeet != feet ? ", the sign turned round to land on the towards side" : "")
        + (loop.IsOpen() ? "" : ", a closed loop"));

    for (var k = 0; k < order.Count; k++)
    {
        var i = order[k];
        var target = flipped[k] ? shifted[k].CreateReversed() : shifted[k];
        var targetId = ids[i];
        try
        {
            if (modeWord == "copy")
            {
                var copies = ElementTransformUtils.CopyElement(doc, ids[i], target.Evaluate(0.5, true) - curves[i].Evaluate(0.5, true));
                targetId = ElementId.InvalidElementId;
                var others = new List<string>();
                foreach (var copyId in copies)
                {
                    var copy = doc.GetElement(copyId);
                    if (copy == null) continue;
                    if (targetId == ElementId.InvalidElementId && (isWall[i] ? copy is Wall : copy is CurveElement)) targetId = copyId;
                    else others.Add((copy.Category != null ? copy.Category.Name : "element") + " " + copyId);
                }
                if (targetId == ElementId.InvalidElementId)
                {
                    refused.Add("Id " + ids[i] + ": Revit's copy came back without a " + (isWall[i] ? "wall" : "line") + ". Skipped.");
                    continue;
                }
                if (others.Count > 0)
                    findings.Add("Copying wall " + ids[i] + " also copied what it hosts: " + string.Join(", ", others) + ".");
            }
            var targetElement = doc.GetElement(targetId);
            if (isWall[i])
                ((LocationCurve)targetElement.Location).Curve = target;
            else
                ((CurveElement)targetElement).SetGeometryCurve(target, true);
            settled.Add(new KeyValuePair<ElementId, Curve>(targetId, target));
            results.Add(targetId);
            offsetCount++;
        }
        catch (Exception ex)
        {
            // Pinned, in a group, a curve of another type for the line (an
            // ellipse comes back a spline), or a wall Revit will not reshape.
            refused.Add("Id " + ids[i] + ": Revit refused to " + (modeWord == "copy" ? "set the copy's" : "move its")
                + " curve - " + ex.Message);
        }
    }
}

// ---- read back, after Revit has resolved the joins ----
if (settled.Count > 0)
{
    doc.Regenerate();
    var moved = new List<string>();
    foreach (var pair in settled)
    {
        var element = doc.GetElement(pair.Key);
        Curve now = null;
        if (element is Wall) { var lc = element.Location as LocationCurve; if (lc != null) now = lc.Curve; }
        else if (element is CurveElement) now = ((CurveElement)element).GeometryCurve;
        if (now == null) { moved.Add(pair.Key + " (not readable)"); continue; }
        var gap = Math.Max(now.GetEndPoint(0).DistanceTo(pair.Value.GetEndPoint(0)),
                           now.GetEndPoint(1).DistanceTo(pair.Value.GetEndPoint(1)));
        if (gap > Touch) moved.Add(pair.Key + " (an end " + mm(gap) + " mm from where it was set)");
    }
    if (moved.Count > 0)
        findings.Add("READ BACK: Revit moved " + moved.Count + " piece(s) after they were set, to keep a join - "
            + string.Join("; ", moved) + ". Look at them before keeping.");
    else
        findings.Add("READ BACK: every piece is where it was set, ends within 0.3 mm.");
}

if (stop.Length > 0) refused.Insert(0, stop);
report = stop.Length > 0 ? stop
    : (modeWord == "copy" ? "Copied " : "Moved ") + offsetCount + " of " + ids.Count + " line(s) and wall(s) "
      + mm(Math.Abs(feet)) + " mm, in " + chains.Count + " chain(s), corners kept joined"
      + (notOffset.Count > 0 ? "; " + notOffset.Count + " element(s) not a line or wall, left alone" : "")
      + (refused.Count > 0 ? "; " + refused.Count + " refusal(s), each named" : "") + ".";
if (notOffset.Count > 0)
    findings.Add(notOffset.Count + " element(s) are not model lines, detail lines or walls and were left alone - "
        + "a duct, pipe or other run is OFFSET_ELEMENTS'.");
foreach (var reason in refused) findings.Add(reason);