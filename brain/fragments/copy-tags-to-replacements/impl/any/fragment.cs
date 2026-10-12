// NOT STANDALONE. Assumes `doc`, `elements` and `csvPath` are in scope, and
// leaves `made`, `madeTags`, `report`, `notReproduced`, `alreadyThere`,
// `oldWithNoTags`, `refused`, `rowsUnmatched` and `badRows` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Every view, one undo.
//
// WHY THIS EXISTS. When an element is swapped for another - an in-place unit
// for a loadable family, one family for another - its tags do not follow. No
// release from 2020 to 2027 lets a tag be pointed at a different element, so
// the only way to keep the drawings is to make a NEW tag on the new element
// for every tag the old one carries: same view, same tag type, same head,
// same leader. On 2026-10-12 that was 16 tags across 9 floor plans and
// sections, all with leaders, re-done by hand.
//
// IT NEVER DELETES THE OLD TAGS. Deleting the old element takes its tags with
// it, and that delete is the modeller's own decision, on their own yes. Until
// then the old and the new tags sit side by side, which is also what lets a
// person compare them on the drawing before anything is lost.
//
// THE PAIRS COME FROM A FILE, AND NO ID IS EVER BUILT FROM A NUMBER. The file
// says "old id, new id"; each id is matched as text against what Revit itself
// hands over - the new element among `elements`, the old one as the element a
// tag points at. Nothing here constructs an ElementId from the file, so
// 2024's move to 64-bit ids does not reach this code.
//
// THE NEW TAG IS MADE WITH THE OLD TAG'S TYPE, THEN READ BACK, AND CORRECTED.
// IndependentTag.Create has been measured making a tag of the document's
// default type instead of the one passed (FRG-VIEW-030). So the type is read
// back and ChangeTypeId'd when it differs, and if the typed overload throws,
// the tag is made by category - by multi-category for a multi-category tag -
// and changed to the old type after. A tag that will not take the old type
// is removed again here, in the same undo: a tag of the wrong type is a
// drawing that prints the wrong text.
//
// EVERY PROPERTY IS READ BACK, NOT ASSUMED. Head, leader on or off, end
// condition, a free leader end, an elbow, the orientation and - from 2022 -
// the rotation angle are each compared with the old tag after a regenerate.
// Whatever differs is named per view in `notReproduced` and the tag is KEPT:
// a tag with its elbow straightened is still the right tag on the right
// element, and the modeller can drag an elbow in a second.
//
// AN ATTACHED END KEEPS ITS SHAPE IN REVIT'S HANDS. With the end attached,
// Revit owns where the leader meets the element, and an elbow written through
// the API can return normally and not hold (FRG-VIEW-088's measurements). So
// the elbow is written, read back, and named if it did not hold - the end is
// NEVER freed to make it hold, because a free end stops following the
// element, which is a worse drawing than a straight leader.
//
// ALREADY THERE IS NOT MADE TWICE. A tag of the same type in the same view,
// on the new element, with its head where the old one's is, means this pair
// was transferred by an earlier run. It is counted in `alreadyThere` and
// nothing is made - so running it again is safe.

const double MmToFeet = 1.0 / 304.8;

var made = 0;
var madeTags = new List<ElementId>();
var notReproduced = new List<string>();
var alreadyThere = new List<ElementId>();
var oldWithNoTags = new List<string>();
var refused = new List<ElementId>();
var rowsUnmatched = 0;
var badRows = 0;
var report = "";

var lines = new List<string>();

// ---- the file: old id first, new id second -------------------------------
//
// THE FIRST LINE IS A HEADER and is never read as a row, whatever it says.
// A row that is not two ids, names the same id twice, or repeats an old id
// already read is a bad row - one old element has one replacement.

var newFor = new Dictionary<string, string>();   // old id -> new id
var fileRead = false;

if (string.IsNullOrEmpty(csvPath) || !System.IO.File.Exists(csvPath))
{
    lines.Add("There is no file at '" + (csvPath ?? "") + "', so NOTHING was made");
}
else
{
    string[] rows = null;
    try { rows = System.IO.File.ReadAllLines(csvPath); }
    catch (Exception ex)
    {
        lines.Add("That file could not be read: " + ex.Message + ". Nothing was made");
    }

    if (rows != null)
    {
        fileRead = true;
        var header = true;
        for (var n = 0; n < rows.Length; n++)
        {
            var row = (rows[n] ?? "").Trim();
            if (row.Length == 0) continue;
            if (header) { header = false; continue; }

            var cells = row.Split(',');
            var oldText = cells[0].Trim();
            var newText = cells.Length > 1 ? cells[1].Trim() : "";
            long probe;
            if (cells.Length < 2 || oldText.Length == 0 || newText.Length == 0
                || !long.TryParse(oldText, out probe) || !long.TryParse(newText, out probe)
                || oldText == newText)
            {
                badRows++;
                if (badRows <= 10)
                    lines.Add("  line " + (n + 1) + ": '" + row + "' is not an old id and a different "
                        + "new id - nothing was made for it");
                continue;
            }
            if (newFor.ContainsKey(oldText))
            {
                badRows++;
                if (badRows <= 10)
                    lines.Add("  line " + (n + 1) + ": old id " + oldText + " is in the file twice - the "
                        + "first row stands, and this one was not used");
                continue;
            }
            newFor[oldText] = newText;
        }
    }
}

// ---- the new elements, from what was handed in ---------------------------

var newById = new Dictionary<string, Element>();
var wantedNew = new HashSet<string>(newFor.Values);
foreach (var element in elements ?? new List<Element>())
{
    if (element == null || !element.IsValidObject) continue;
    var key = element.Id.ToString();
    if (wantedNew.Contains(key)) newById[key] = element;
}

var usable = new Dictionary<string, Element>();   // old id -> new element
foreach (var pair in newFor)
{
    Element target;
    if (newById.TryGetValue(pair.Value, out target)) usable[pair.Key] = target;
    else
    {
        rowsUnmatched++;
        if (rowsUnmatched <= 10)
            lines.Add("  new id " + pair.Value + " (for old " + pair.Key + ") is not among the elements "
                + "handed in - usually a filter that did not cover it, or the wrong model. Nothing was "
                + "made for that pair");
    }
}

// ---- the 2022 split, looked up at run time -------------------------------
//
// NO VERSION #if. An add-in built before row 5b-181's fix compiles a fragment
// with no release symbols, so an #if always takes its #else - which on 2020
// and 2021 calls members those releases do not have. Each member is found by
// name on the tag in front of it instead: the 2022-and-later method where
// Revit has it, the 2020/2021 property where it does not. Read off the
// reference assemblies for every release on 2026-10-12:
//
//   TaggedLocalElementId, GetTaggedReference,     2020 and 2021 (and 2022);
//   LeaderEnd, LeaderElbow, HasElbow              gone from 2023
//   GetTaggedLocalElementIds, GetTaggedReferences, 2022 on
//   GetLeaderEnd/SetLeaderEnd, HasLeaderElbow,
//   GetLeaderElbow/SetLeaderElbow, RotationAngle

Func<IndependentTag, List<ElementId>> localIdsOf = tag =>
{
    var found = new List<ElementId>();
    var many = tag.GetType().GetMethod("GetTaggedLocalElementIds", Type.EmptyTypes);
    if (many != null)
    {
        var all = many.Invoke(tag, null) as IEnumerable<ElementId>;
        if (all != null)
            foreach (var id in all)
                if (id != null && id != ElementId.InvalidElementId) found.Add(id);
        return found;
    }
    var single = tag.GetType().GetProperty("TaggedLocalElementId");
    var one = single == null ? null : single.GetValue(tag, null) as ElementId;
    if (one != null && one != ElementId.InvalidElementId) found.Add(one);
    return found;
};

// The tag's one reference, which the 2022-and-later leader methods take.
// Null on 2020 and 2021, where the properties need none.
Func<IndependentTag, Reference> referenceOf = tag =>
{
    var method = tag.GetType().GetMethod("GetTaggedReferences", Type.EmptyTypes);
    if (method == null) return null;
    var references = method.Invoke(tag, null) as IList<Reference>;
    return references != null && references.Count > 0 ? references[0] : null;
};

// How many things the tag points at. One on 2020 and 2021, always.
Func<IndependentTag, int> referenceCountOf = tag =>
{
    var method = tag.GetType().GetMethod("GetTaggedReferences", Type.EmptyTypes);
    if (method == null) return 1;
    var references = method.Invoke(tag, null) as IList<Reference>;
    return references == null ? 0 : references.Count;
};

Func<IndependentTag, XYZ> leaderEndOf = tag =>
{
    var method = tag.GetType().GetMethod("GetLeaderEnd", new[] { typeof(Reference) });
    if (method != null)
    {
        var reference = referenceOf(tag);
        return reference == null ? null : method.Invoke(tag, new object[] { reference }) as XYZ;
    }
    var property = tag.GetType().GetProperty("LeaderEnd");
    return property == null ? null : property.GetValue(tag, null) as XYZ;
};

Func<IndependentTag, XYZ, bool> setLeaderEnd = (tag, point) =>
{
    var method = tag.GetType().GetMethod("SetLeaderEnd", new[] { typeof(Reference), typeof(XYZ) });
    if (method != null)
    {
        var reference = referenceOf(tag);
        if (reference == null) return false;
        method.Invoke(tag, new object[] { reference, point });
        return true;
    }
    var property = tag.GetType().GetProperty("LeaderEnd");
    if (property == null || !property.CanWrite) return false;
    property.SetValue(tag, point, null);
    return true;
};

// The elbow, or null when the leader has none.
Func<IndependentTag, XYZ> elbowOf = tag =>
{
    var has = tag.GetType().GetMethod("HasLeaderElbow", new[] { typeof(Reference) });
    if (has != null)
    {
        var reference = referenceOf(tag);
        if (reference == null) return null;
        if (!(bool)has.Invoke(tag, new object[] { reference })) return null;
        var get = tag.GetType().GetMethod("GetLeaderElbow", new[] { typeof(Reference) });
        return get == null ? null : get.Invoke(tag, new object[] { reference }) as XYZ;
    }
    var hasProperty = tag.GetType().GetProperty("HasElbow");
    if (hasProperty == null || !(bool)hasProperty.GetValue(tag, null)) return null;
    var property = tag.GetType().GetProperty("LeaderElbow");
    return property == null ? null : property.GetValue(tag, null) as XYZ;
};

Func<IndependentTag, XYZ, bool> setElbow = (tag, point) =>
{
    var method = tag.GetType().GetMethod("SetLeaderElbow", new[] { typeof(Reference), typeof(XYZ) });
    if (method != null)
    {
        var reference = referenceOf(tag);
        if (reference == null) return false;
        method.Invoke(tag, new object[] { reference, point });
        return true;
    }
    var property = tag.GetType().GetProperty("LeaderElbow");
    if (property == null || !property.CanWrite) return false;
    property.SetValue(tag, point, null);
    return true;
};

// Rotation, 2022 on. Null where the release has no such property.
var rotationProperty = typeof(IndependentTag).GetProperty("RotationAngle");

// ---- every tag in the document, once -------------------------------------
//
// ONE PASS, NOT ONE PER VIEW. The old tags are found by what they point at,
// and the same pass indexes the tags already on each new element, which is
// what "already there" is judged against.

var oldTagsFor = new Dictionary<string, List<IndependentTag>>();
var onNew = new Dictionary<string, List<IndependentTag>>();
var newIds = new HashSet<string>();
foreach (var target in usable.Values) newIds.Add(target.Id.ToString());

if (usable.Count > 0)
{
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(IndependentTag)))
    {
        var tag = candidate as IndependentTag;
        if (tag == null) continue;
        List<ElementId> ids;
        try { ids = localIdsOf(tag); } catch { continue; }
        foreach (var id in ids)
        {
            var key = id.ToString();
            if (usable.ContainsKey(key))
            {
                if (!oldTagsFor.ContainsKey(key)) oldTagsFor[key] = new List<IndependentTag>();
                oldTagsFor[key].Add(tag);
            }
            if (newIds.Contains(key))
            {
                if (!onNew.ContainsKey(key)) onNew[key] = new List<IndependentTag>();
                onNew[key].Add(tag);
            }
        }
    }
}

foreach (var oldKey in usable.Keys)
{
    if (!oldTagsFor.ContainsKey(oldKey))
    {
        oldWithNoTags.Add(oldKey);
    }
}

// ---- per view: what was made, and what could not be reproduced ------------

var viewOrder = new List<ElementId>();
var viewMade = new Dictionary<ElementId, int>();
var viewOf = new Dictionary<ElementId, int>();
var viewIssues = new Dictionary<ElementId, List<string>>();
var visibleIn = new Dictionary<string, bool>();

Action<ElementId, string> issue = (viewId, text) =>
{
    if (!viewIssues.ContainsKey(viewId)) viewIssues[viewId] = new List<string>();
    viewIssues[viewId].Add(text);
};

foreach (var pair in usable)
{
    List<IndependentTag> olds;
    if (!oldTagsFor.TryGetValue(pair.Key, out olds)) continue;
    var target = pair.Value;
    var targetKey = target.Id.ToString();

    foreach (var old in olds)
    {
        var viewId = ElementId.InvalidElementId;
        try { viewId = old.OwnerViewId; } catch { }
        if (!viewOrder.Contains(viewId)) { viewOrder.Add(viewId); viewMade[viewId] = 0; viewOf[viewId] = 0; }
        viewOf[viewId]++;

        var view = doc.GetElement(viewId) as View;
        var tagName = "tag " + old.Id + " (old " + pair.Key + " -> new " + targetKey + ")";

        // ---- what the old tag is ----
        int pointsAt = 1;
        try { pointsAt = referenceCountOf(old); } catch { }
        if (pointsAt > 1)
        {
            refused.Add(old.Id);
            issue(viewId, tagName + ": points at " + pointsAt + " elements - a tag on the new one alone "
                + "would drop the others, so none was made");
            continue;
        }
        bool isMaterial = false;
        try { isMaterial = old.IsMaterialTag; } catch { }
        if (isMaterial)
        {
            refused.Add(old.Id);
            issue(viewId, tagName + ": a material tag points at a material inside the element, which a "
                + "tag on the whole new element cannot reproduce - none was made");
            continue;
        }

        var typeId = old.GetTypeId();
        XYZ head = null;
        bool hasLeader = false;
        var condition = LeaderEndCondition.Attached;
        XYZ freeEnd = null;
        XYZ elbow = null;
        var orientation = TagOrientation.Horizontal;
        object rotation = null;
        bool multiCategory = false;
        try { head = old.TagHeadPosition; } catch { }
        try { hasLeader = old.HasLeader; } catch { }
        try { orientation = old.TagOrientation; } catch { }
        try { multiCategory = old.IsMulticategoryTag; } catch { }
        try { if (rotationProperty != null) rotation = rotationProperty.GetValue(old, null); } catch { }
        if (hasLeader)
        {
            try { condition = old.LeaderEndCondition; } catch { }
            // A free end is a point the modeller placed. An attached one is
            // Revit's to choose and reading it is refused on some releases.
            if (condition == LeaderEndCondition.Free) { try { freeEnd = leaderEndOf(old); } catch { } }
            try { elbow = elbowOf(old); } catch { }
        }
        if (head == null || view == null)
        {
            refused.Add(old.Id);
            issue(viewId, tagName + ": Revit gave no head position or view for it - none was made");
            continue;
        }

        double scale = 1;
        try { scale = Math.Max(1, view.Scale); } catch { }
        // Half a paper millimetre, in the model, at this view's scale.
        double tolerance = 0.5 * MmToFeet * scale;

        // ---- already transferred by an earlier run? ----
        bool there = false;
        List<IndependentTag> existing;
        if (onNew.TryGetValue(targetKey, out existing))
        {
            foreach (var other in existing)
            {
                try
                {
                    if (other.OwnerViewId == viewId && other.GetTypeId() == typeId
                        && other.TagHeadPosition.DistanceTo(head) <= tolerance)
                    { there = true; break; }
                }
                catch { }
            }
        }
        if (there)
        {
            alreadyThere.Add(old.Id);
            issue(viewId, tagName + ": the new element already has this tag here - nothing made");
            continue;
        }

        // ---- is the new element seen in this view at all? ----
        //
        // A tag on an element the view does not show is made, or refused, by
        // Revit in ways that differ by release - so it is asked first. Asked
        // per view and category, once, never per tag.
        if (target.Category != null)
        {
            var seenKey = viewId + "|" + targetKey;
            bool seen;
            if (!visibleIn.TryGetValue(seenKey, out seen))
            {
                seen = true;   // when the view cannot be asked, Revit decides below
                try
                {
                    seen = false;
                    foreach (var id in new FilteredElementCollector(doc, viewId)
                                 .OfCategoryId(target.Category.Id).ToElementIds())
                        if (id == target.Id) { seen = true; break; }
                }
                catch { seen = true; }
                visibleIn[seenKey] = seen;
            }
            if (!seen)
            {
                refused.Add(old.Id);
                issue(viewId, tagName + ": the new element is not seen in this view - hidden, outside the "
                    + "crop or outside the view range - so no tag was made");
                continue;
            }
        }

        // ---- make it ----
        IndependentTag tag = null;
        var route = "by type";
        try
        {
            tag = IndependentTag.Create(doc, typeId, viewId, new Reference(target), hasLeader,
                                        orientation, head);
        }
        catch (Exception)
        {
            tag = null;
        }
        if (tag == null)
        {
            // The route this job was first worked out on: by category, then
            // the type changed after.
            route = multiCategory ? "by multi-category" : "by category";
            try
            {
                tag = IndependentTag.Create(doc, viewId, new Reference(target), hasLeader,
                                            multiCategory ? TagMode.TM_ADDBY_MULTICATEGORY
                                                          : TagMode.TM_ADDBY_CATEGORY,
                                            orientation, head);
            }
            catch (Exception ex)
            {
                refused.Add(old.Id);
                issue(viewId, tagName + ": Revit would not make the tag either way (" + ex.Message + ")");
                continue;
            }
        }
        if (tag == null)
        {
            refused.Add(old.Id);
            issue(viewId, tagName + ": Revit returned no tag");
            continue;
        }

        // ---- the type: read back, and corrected ----
        if (tag.GetTypeId() != typeId)
        {
            try { tag.ChangeTypeId(typeId); } catch { }
            if (tag.GetTypeId() != typeId)
            {
                // A tag of another type prints other text. It is taken out
                // again here, in the same undo - it is this run's own tag,
                // never one the modeller had.
                try { doc.Delete(tag.Id); } catch { }
                refused.Add(old.Id);
                issue(viewId, tagName + ": the new tag would not take the old tag's type, so it was not kept");
                continue;
            }
            issue(viewId, tagName + ": made " + route + " as the default type and changed to the old type");
        }

        // ---- the leader: end condition, a free end, an elbow ----
        var asked = new List<string>();
        try
        {
            if (tag.HasLeader != hasLeader) tag.HasLeader = hasLeader;
            if (hasLeader)
            {
                if (tag.LeaderEndCondition != condition && tag.CanLeaderEndConditionBeAssigned(condition))
                    tag.LeaderEndCondition = condition;
                if (freeEnd != null && !setLeaderEnd(tag, freeEnd)) asked.Add("leader end (no member takes it here)");
                if (elbow != null && !setElbow(tag, elbow)) asked.Add("elbow (no member takes it here)");
            }
            if (tag.TagOrientation != orientation) tag.TagOrientation = orientation;
            if (rotation != null && rotationProperty.CanWrite) rotationProperty.SetValue(tag, rotation, null);
            // Last, so nothing above can have moved it.
            tag.TagHeadPosition = head;
        }
        catch (Exception ex)
        {
            asked.Add("a leader or head setting Revit refused (" + ex.Message + ")");
        }

        // ---- read back, after Revit has worked the tag out ----
        try { doc.Regenerate(); } catch { }

        var differs = new List<string>(asked);
        try { if (tag.TagHeadPosition.DistanceTo(head) > tolerance) differs.Add("head position"); }
        catch { differs.Add("head position (unreadable)"); }
        try { if (tag.HasLeader != hasLeader) differs.Add("leader " + (hasLeader ? "on" : "off")); }
        catch { }
        try { if (tag.TagOrientation != orientation) differs.Add("orientation " + orientation); }
        catch { }
        try
        {
            if (rotation != null && !Equals(rotationProperty.GetValue(tag, null), rotation))
            {
                var was = (double)rotation;
                var now = (double)rotationProperty.GetValue(tag, null);
                if (Math.Abs(was - now) > 1e-6) differs.Add("rotation");
            }
        }
        catch { }
        if (hasLeader)
        {
            try { if (tag.LeaderEndCondition != condition) differs.Add("leader end " + condition); }
            catch { }
            if (freeEnd != null)
            {
                try
                {
                    var end = leaderEndOf(tag);
                    if (end == null || end.DistanceTo(freeEnd) > tolerance) differs.Add("leader end point");
                }
                catch { differs.Add("leader end point (unreadable)"); }
            }
            if (elbow != null)
            {
                try
                {
                    var now = elbowOf(tag);
                    if (now == null || now.DistanceTo(elbow) > tolerance)
                        differs.Add(condition == LeaderEndCondition.Attached
                            ? "elbow (Revit keeps an attached leader's shape; the end was left attached)"
                            : "elbow");
                }
                catch { differs.Add("elbow (unreadable)"); }
            }
        }

        made++;
        madeTags.Add(tag.Id);
        viewMade[viewId]++;
        if (differs.Count > 0)
        {
            var text = tagName + ", new tag " + tag.Id + ": not reproduced - " + string.Join(", ", differs);
            notReproduced.Add((view.Name ?? "") + ": " + text);
            issue(viewId, text);
        }
    }
}

// ---- the report ----------------------------------------------------------
//
// ONE STRING, because a list reaches the chat as three entries cut short and
// the cut part is where the views and the reasons are.

var summary = new List<string>();
summary.Add("Made " + made + " tag(s) on the new elements, across " + viewOrder.Count + " view(s). "
    + "No old tag was deleted - deleting the old element removes them.");
if (!fileRead) summary.Add("The pairs file was not read.");
summary.Add("Pairs read " + newFor.Count + ", usable " + usable.Count + ", unmatched " + rowsUnmatched
    + ", bad rows " + badRows + ".");
foreach (var oldKey in oldWithNoTags)
    summary.Add("Old " + oldKey + " has no tag on it anywhere in the model, so nothing was made for it "
        + "(if it was already deleted, its tags went with it).");

foreach (var viewId in viewOrder)
{
    var view = doc.GetElement(viewId) as View;
    var name = view == null ? "view " + viewId : "'" + view.Name + "' (" + view.ViewType + ")";
    summary.Add(name + ": made " + viewMade[viewId] + " of " + viewOf[viewId] + ".");
    List<string> found;
    if (viewIssues.TryGetValue(viewId, out found))
        foreach (var text in found) summary.Add("    " + text);
}
summary.AddRange(lines);
report = string.Join("\n", summary);
