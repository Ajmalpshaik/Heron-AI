// NOT STANDALONE. Assumes `doc`, `elements`, `target` and `arrange` are in
// scope; leaves `moved`, `notMovable` and `findings`.
//
// LINED UP OR SPREAD OUT, TOO (version 2, 2026-10-13, the gap fill). `arrange`
// EMPTY or "point" moves each one to `target`, as version 1 did. Otherwise:
//   "align left", "align right", "align top", "align bottom" - each moved
//   across or up its view until it lines up with the outermost one;
//   "align centre" or "align middle" - lined up on the average, across or up
//   (centre across, middle up);
//   "spread across", "spread down" - spaced evenly between the outermost two.
// Across and up are the annotation's own view's directions, not the model's.
// A tag is placed by its head, a dimension by the middle of its line, a text
// note or anything else by its location point; `target` is not used.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). The target is internal FEET.
//
// EVERY OTHER TAG FRAGMENT HERE DECIDES THE POSITION ITSELF - arrange, stack,
// centre, park at the edges. Not one takes a position, which is why "move this
// tag" had no route at all.
//
// A TAG MOVES BY ITS HEAD. IndependentTag.TagHeadPosition is the head; moving
// the ELEMENT drags the whole tag including its leader anchor, which is a
// different result from the one somebody asking means.
//
// THE LEADER IS LEFT ALONE. What the tag points at does not change - only where
// its head sits. Re-anchoring while moving would quietly re-tag things.

var moved = new List<ElementId>();
var notMovable = new List<ElementId>();
var findings = new List<string>();

var how = (arrange ?? "").Trim().ToLowerInvariant();
var arranging = how != "" && how != "point";
var known = new[] { "align left", "align right", "align top", "align bottom", "align centre", "align center", "align middle", "spread across", "spread down" };

// Where an annotation sits, and how to put it somewhere else.
Func<Element, XYZ> placeOf = element =>
{
    var tag = element as IndependentTag;
    if (tag != null) return tag.TagHeadPosition;
    var dimension = element as Dimension;
    if (dimension != null) { try { return dimension.Origin; } catch { return null; } }
    var spot = element.Location as LocationPoint;
    return spot != null ? spot.Point : null;
};
var byHead = 0;
var byLocation = 0;
Action<Element, XYZ> putAt = (element, to) =>
{
    var tag = element as IndependentTag;
    if (tag != null)
    {
        try { tag.TagHeadPosition = to; moved.Add(element.Id); byHead++; }
        catch { notMovable.Add(element.Id); }
        return;
    }
    var from = placeOf(element);
    if (from == null) { notMovable.Add(element.Id); return; }
    var shift = to - from;
    if (shift.IsZeroLength()) { moved.Add(element.Id); byLocation++; return; }
    try { ElementTransformUtils.MoveElement(doc, element.Id, shift); moved.Add(element.Id); byLocation++; }
    catch { notMovable.Add(element.Id); }
};

if (elements == null || elements.Count == 0)
{
    findings.Add("No annotation was given, so there is nothing to move");
}
else if (arranging && !known.Contains(how))
{
    findings.Add("\"" + arrange + "\" is not point, align left, align right, align top, align bottom, align centre, "
        + "align middle, spread across or spread down. NOTHING WAS MOVED");
}
else if (!arranging && target == null)
{
    findings.Add("No target point was given");
}
else if (!arranging)
{
    foreach (var element in elements)
        if (element != null) putAt(element, target);
    findings.Add(string.Format(
        "{0} moved - {1} by its tag head, {2} by its location point - and {3} "
        + "had neither. What each tag POINTS AT is unchanged",
        moved.Count, byHead, byLocation, notMovable.Count));
}
else
{
    // Across and up are the first annotation's own view's.
    var placed = new List<Tuple<Element, XYZ>>();
    foreach (var element in elements)
    {
        if (element == null) continue;
        var at = placeOf(element);
        if (at == null) notMovable.Add(element.Id); else placed.Add(Tuple.Create(element, at));
    }
    View owner = placed.Count > 0 ? doc.GetElement(placed[0].Item1.OwnerViewId) as View : null;
    var across = owner != null ? owner.RightDirection : XYZ.BasisX;
    var up = owner != null ? owner.UpDirection : XYZ.BasisY;
    var vertical = how == "align top" || how == "align bottom" || how == "align middle" || how == "spread down";
    var axis = vertical ? up : across;
    if (placed.Count < 2) findings.Add("Lining up or spreading out needs two annotations or more with a position. NOTHING WAS MOVED");
    else
    {
        var along = placed.Select(p => p.Item2.DotProduct(axis)).ToList();
        var meant = new Dictionary<ElementId, double>();
        if (how.StartsWith("spread"))
        {
            var order = placed.OrderBy(p => p.Item2.DotProduct(axis)).ToList();
            var first = along.Min();
            var step = (along.Max() - first) / (order.Count - 1);
            for (var i = 0; i < order.Count; i++)
            {
                meant[order[i].Item1.Id] = first + step * i;
                putAt(order[i].Item1, order[i].Item2 + axis * (first + step * i - order[i].Item2.DotProduct(axis)));
            }
        }
        else
        {
            var line = how == "align left" || how == "align bottom" ? along.Min()
                : how == "align right" || how == "align top" ? along.Max() : along.Average();
            foreach (var p in placed)
            {
                meant[p.Item1.Id] = line;
                putAt(p.Item1, p.Item2 + axis * (line - p.Item2.DotProduct(axis)));
            }
        }
        doc.Regenerate();
        // READ BACK: each one's place along the axis, against where it was sent.
        var off = placed.Where(p => moved.Contains(p.Item1.Id)).Count(p =>
        {
            var now = placeOf(p.Item1);
            return now == null || Math.Abs(now.DotProduct(axis) - meant[p.Item1.Id]) > 1.0 / 304.8;
        });
        if (off > 0) findings.Add(off + " annotation(s) read back more than 1 mm from where they were sent - a dimension's text can sit away from its line");
        findings.Add(string.Format("{0} {1} - {2} by its tag head, {3} by its location or dimension line - and {4} "
            + "had no position to move. What each tag POINTS AT is unchanged", moved.Count,
            how.StartsWith("spread") ? "spread evenly (" + how + ")" : "lined up (" + how + ")", byHead, byLocation, notMovable.Count));
    }
}
