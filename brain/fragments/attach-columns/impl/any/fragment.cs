// NOT STANDALONE. Assumes `doc`, `elements`, `where`, `detach`, `offset`,
// `cutStyle` and `justification` are in scope; leaves `changed`, `changedIds`,
// `alreadyDone`, `left`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry and
// the caller owns it. Each column is changed inside a SubTransaction of its
// own within it, so a column Revit refuses is put back alone - its earlier
// attachment included - and the rest of the batch stands.
//
// REVIT'S MODIFY COLUMN > ATTACH TOP/BASE AND DETACH TOP/BASE, for structural
// and architectural columns alike. ColumnAttachment is the whole API, the same
// on every release 2020 to 2027 (read with tools/api-surface --members on
// 2026-11-01): AddColumnAttachment(doc, column, target, baseOrTop, cutStyle,
// justification, offset), RemoveColumnAttachment(column, baseOrTop),
// GetColumnAttachment(column, baseOrTop), IsValidColumn, IsValidTarget.
// baseOrTop is 0 for the base and 1 for the top (NEEDS-CHECKING KD2 confirms
// it on a real column - an end the wrong way round shows at once in section).
//
// THE TARGET IS NEVER GUESSED. It arrives in `elements` with the columns:
// what Revit accepts as a column is a column, and everything else selected is
// a target. Attaching with no target selected changes nothing and says so.
// A selected target Revit accepts for NONE of the columns - a door, a grid -
// is refused BY NAME and NOTHING IS CHANGED: the selection is wrong, and
// attaching the rest would hide that.
//
// EACH COLUMN TAKES THE ONE SELECTED TARGET OVER OR UNDER IT - the target
// whose plan extent meets the column's - so "the column tops to the beams
// above" pairs each column with its own beam. A column no selected target
// meets, or that two or more meet, is named and left; choosing one of two
// would be a guess.
//
// ALREADY ATTACHED TO THAT TARGET AT THAT END: left alone and counted in
// alreadyDone, its cut style and offset as they were. Attached at that end to
// SOMETHING ELSE: moved to the target asked, and the report names both.
//
// DETACH removes the attachment at that end - "both" takes top and base. With
// targets selected, only an attachment to one of them is removed; one to
// something not selected is named and left.
//
// READ BACK: after a regenerate each column's attachment at that end is read
// again, and only a column that now reads as asked counts in `changed`.
// Ids are carried and compared as ElementId, never read as numbers. The offset
// arrives in MILLIMETRES and becomes feet here (D-20).

var changed = 0;
var changedIds = new List<ElementId>();
var alreadyDone = new List<ElementId>();
var left = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();
string refusal = null;

const double MillimetresPerFoot = 304.8;
// A beam framing into a column is often cut back to the column's face, so its
// plan extent stops exactly where the column's starts. 10 mm lets touching
// count as meeting.
const double TouchFeet = 10.0 / MillimetresPerFoot;

Func<Element, string> name = one =>
{
    if (one == null) return "(nothing)";
    var category = one.Category != null ? one.Category.Name : "Element";
    string label = null;
    try { label = one.Name; } catch { label = null; }
    return category + " " + one.Id + (string.IsNullOrEmpty(label) ? "" : " \"" + label + "\"");
};

// ---- the values asked for ----
var end = (where ?? "").Trim().ToLowerInvariant();
if (end == "") end = "top";
if (end == "bottom") end = "base";
if (end != "top" && end != "base" && end != "both")
    refusal = "\"" + where + "\" is not top, base or both. NOTHING WAS CHANGED.";
else if (end == "both" && !detach)
    refusal = "Attach takes one end at a time - say top or base. NOTHING WAS CHANGED.";
var ends = end == "both" ? new[] { 1, 0 } : new[] { end == "base" ? 0 : 1 };
Func<int, string> endName = e => e == 0 ? "base" : "top";

var style = ColumnAttachmentCutStyle.CutColumn;
var styleText = (cutStyle ?? "").Trim().ToLowerInvariant();
if (refusal == null && !detach)
{
    if (styleText == "" || styleText == "cut column" || styleText == "column") style = ColumnAttachmentCutStyle.CutColumn;
    else if (styleText == "cut target" || styleText == "target") style = ColumnAttachmentCutStyle.CutTarget;
    else if (styleText == "none" || styleText == "do not cut" || styleText == "don't cut") style = ColumnAttachmentCutStyle.None;
    else refusal = "\"" + cutStyle + "\" is not an attachment style - say cut column, cut target or do not cut. NOTHING WAS CHANGED.";
}

var fit = ColumnAttachmentJustification.Minimum;
var fitText = (justification ?? "").Trim().ToLowerInvariant();
if (refusal == null && !detach)
{
    if (fitText == "" || fitText.StartsWith("minimum")) fit = ColumnAttachmentJustification.Minimum;
    else if (fitText.StartsWith("mid")) fit = ColumnAttachmentJustification.Midpoint;
    else if (fitText.StartsWith("maximum")) fit = ColumnAttachmentJustification.Maximum;
    else if (fitText == "tangent") fit = ColumnAttachmentJustification.Tangent;
    else refusal = "\"" + justification + "\" is not an attachment justification - say minimum, midpoint, maximum or tangent. NOTHING WAS CHANGED.";
}

if (refusal == null && !detach && (double.IsNaN(offset) || double.IsInfinity(offset) || Math.Abs(offset) > 30000.0))
    refusal = "An offset of " + offset + " mm is not one Heron applies to an attachment. NOTHING WAS CHANGED.";
var offsetFeet = offset / MillimetresPerFoot;

// ---- columns and targets, split by what Revit accepts as a column ----
var columns = new List<FamilyInstance>();
var targets = new List<Element>();
if (refusal == null)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        var instance = element as FamilyInstance;
        var isColumn = false;
        if (instance != null)
        {
            try { isColumn = ColumnAttachment.IsValidColumn(instance); } catch { isColumn = false; }
        }
        if (isColumn) columns.Add(instance);
        else targets.Add(element);
    }

    if (columns.Count == 0)
        refusal = "None of the " + targets.Count + " element(s) given is a column Revit can attach. NOTHING WAS CHANGED.";
    else if (!detach && targets.Count == 0)
        refusal = "No target was selected with the " + columns.Count + " column(s) - select the roof, floor, ceiling, beam "
                + "or footing they attach to together with them. Nothing is guessed. NOTHING WAS CHANGED.";
}

// A target Revit accepts for none of the columns is the selection's mistake.
if (refusal == null && !detach)
{
    var notAccepted = new List<string>();
    foreach (var target in targets)
    {
        var acceptedByOne = false;
        foreach (var column in columns)
        {
            try { if (ColumnAttachment.IsValidTarget(column, target)) { acceptedByOne = true; break; } }
            catch { }
        }
        if (!acceptedByOne) notAccepted.Add(name(target));
    }
    if (notAccepted.Count > 0)
        refusal = "Revit does not accept " + string.Join(", ", notAccepted) + " as something a column attaches to. "
                + "Take " + (notAccepted.Count == 1 ? "it" : "them") + " out of the selection. NOTHING WAS CHANGED.";
}

// ---- plan extents ----
Func<BoundingBoxXYZ, BoundingBoxXYZ, bool> meetInPlan = (a, b) =>
    a != null && b != null
    && a.Min.X <= b.Max.X + TouchFeet && a.Max.X >= b.Min.X - TouchFeet
    && a.Min.Y <= b.Max.Y + TouchFeet && a.Max.Y >= b.Min.Y - TouchFeet;
var targetBoxes = new Dictionary<ElementId, BoundingBoxXYZ>();
foreach (var target in targets) targetBoxes[target.Id] = target.get_BoundingBox(null);

// What each column was asked: Item2 the end, Item3 the target (InvalidElementId
// on a detach), Item4 what it was attached to before (or InvalidElementId).
var tried = new List<Tuple<FamilyInstance, int, ElementId, ElementId>>();

if (refusal == null)
{
    foreach (var column in columns)
    {
        if (detach)
        {
            var anyRemoved = false;
            var anyHeldElsewhere = false;
            var notes = new List<string>();
            foreach (var e in ends)
            {
                ColumnAttachment existing = null;
                try { existing = ColumnAttachment.GetColumnAttachment(column, e); } catch { existing = null; }
                if (existing == null) { notes.Add(endName(e) + " not attached"); continue; }
                var heldBy = existing.TargetId;
                if (targets.Count > 0 && !targets.Any(t => t.Id == heldBy))
                {
                    anyHeldElsewhere = true;
                    notes.Add(endName(e) + " attached to " + name(doc.GetElement(heldBy)) + ", which was not selected - left");
                    continue;
                }
                var sub = new SubTransaction(doc);
                try
                {
                    sub.Start();
                    ColumnAttachment.RemoveColumnAttachment(column, e);
                    sub.Commit();
                    tried.Add(Tuple.Create(column, e, ElementId.InvalidElementId, heldBy));
                    anyRemoved = true;
                }
                catch (Exception ex)
                {
                    if (sub.HasStarted() && !sub.HasEnded()) sub.RollBack();
                    if (!left.Contains(column.Id)) left.Add(column.Id);
                    notes.Add(endName(e) + ": Revit refused - " + ex.Message);
                }
            }
            if (!anyRemoved)
            {
                if (anyHeldElsewhere) { if (!left.Contains(column.Id)) left.Add(column.Id); }
                else if (!left.Contains(column.Id)) alreadyDone.Add(column.Id);
                lines.Add("column " + name(column) + ": " + string.Join("; ", notes) + ".");
            }
            else if (notes.Count > 0) lines.Add("column " + name(column) + ": " + string.Join("; ", notes) + ".");
            continue;
        }

        var at = ends[0];
        var box = column.get_BoundingBox(null);
        var meeting = targets.Where(t => meetInPlan(box, targetBoxes[t.Id])).ToList();
        if (meeting.Count == 0)
        {
            left.Add(column.Id);
            lines.Add("column " + name(column) + ": none of the selected targets is over or under it - left.");
            continue;
        }
        if (meeting.Count > 1)
        {
            left.Add(column.Id);
            lines.Add("column " + name(column) + ": " + meeting.Count + " selected targets meet it ("
                + string.Join(", ", meeting.Select(t => name(t))) + ") - select only the one it attaches to. Left.");
            continue;
        }
        var target = meeting[0];
        var accepted = false;
        try { accepted = ColumnAttachment.IsValidTarget(column, target); } catch { accepted = false; }
        if (!accepted)
        {
            left.Add(column.Id);
            lines.Add("column " + name(column) + ": Revit does not accept " + name(target) + " as this column's target - left.");
            continue;
        }

        ColumnAttachment before = null;
        try { before = ColumnAttachment.GetColumnAttachment(column, at); } catch { before = null; }
        var beforeId = before != null ? before.TargetId : ElementId.InvalidElementId;
        if (before != null && beforeId == target.Id)
        {
            alreadyDone.Add(column.Id);
            lines.Add("column " + name(column) + ": " + endName(at) + " already attached to " + name(target)
                + " - left as it was, its style and offset untouched.");
            continue;
        }

        var step = new SubTransaction(doc);
        try
        {
            step.Start();
            if (before != null) ColumnAttachment.RemoveColumnAttachment(column, at);
            ColumnAttachment.AddColumnAttachment(doc, column, target, at, style, fit, offsetFeet);
            step.Commit();
            tried.Add(Tuple.Create(column, at, target.Id, beforeId));
        }
        catch (Exception ex)
        {
            if (step.HasStarted() && !step.HasEnded()) step.RollBack();
            left.Add(column.Id);
            lines.Add("column " + name(column) + " to " + name(target) + ": Revit refused - " + ex.Message
                + (before != null ? " Its attachment to " + name(doc.GetElement(beforeId)) + " is as it was." : ""));
        }
    }

    doc.Regenerate();

    // ---- read back ----
    var counted = new HashSet<ElementId>();
    foreach (var ask in tried)
    {
        ColumnAttachment now = null;
        try { now = ColumnAttachment.GetColumnAttachment(ask.Item1, ask.Item2); } catch { now = null; }
        var wasDetach = ask.Item3 == ElementId.InvalidElementId;
        var ok = wasDetach ? now == null : now != null && now.TargetId == ask.Item3;
        if (!ok)
        {
            if (!left.Contains(ask.Item1.Id)) left.Add(ask.Item1.Id);
            lines.Add("column " + name(ask.Item1) + ": " + endName(ask.Item2) + " asked, and it does not read back as "
                + (wasDetach ? "detached." : "attached to " + name(doc.GetElement(ask.Item3)) + "."));
            continue;
        }
        if (wasDetach)
            lines.Add("column " + name(ask.Item1) + ": " + endName(ask.Item2) + " detached from " + name(doc.GetElement(ask.Item4)) + ".");
        else
            lines.Add("column " + name(ask.Item1) + ": " + endName(ask.Item2) + " attached to " + name(doc.GetElement(ask.Item3))
                + (ask.Item4 != ElementId.InvalidElementId ? " (was attached to " + name(doc.GetElement(ask.Item4)) + ")" : "") + ".");
        if (counted.Add(ask.Item1.Id))
        {
            changed++;
            changedIds.Add(ask.Item1.Id);
        }
    }
    // "both" on a detach can change one end and not the other: that column is
    // in changedIds AND in left, and the report says which end is which.

    findings.Add(changed + " column(s) " + (detach ? "detached" : "attached") + " at the " + end + " and read back; "
        + alreadyDone.Count + " already " + (detach ? "had nothing to detach" : "attached to that target") + "; "
        + left.Count + " left, each named with why.");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
