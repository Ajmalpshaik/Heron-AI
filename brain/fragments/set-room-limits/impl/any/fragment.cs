// NOT STANDALONE. Assumes `doc`, `elements`, `upperLevel` and `limitOffset` are
// in scope; leaves `changed`, `alreadyRight`, `notSpatial`, `topBelowFloor`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One batch, one undo.
//
// THE LEVEL AND THE OFFSET ARE SET TOGETHER, BECAUSE EITHER ALONE MOVES THE TOP.
// A room's top is its Upper Limit (a level) plus its Limit Offset. Change the
// level and keep the offset, and the top jumps by the distance between the two
// levels. On 2026-09-24, in Project1, one room's Upper Limit was moved to Level
// 2 by hand with its offset left at +3700 - its top went 3700 mm ABOVE Level 2,
// through the slab. So the caller names both, and the top they make is worked
// out before anything is written.
//
// NOTHING ELSE COULD SET THE LEVEL. WRITE_ELEMENT_PARAMETERS refuses a
// parameter that stores an element id, deliberately: "Level 2" as text is a
// name. Here the level arrives already resolved, by name and refused if two
// match, so the id written is the level the caller meant.
//
// ROOMS AND SPACES ALIKE, TOLD APART BY CATEGORY. Both carry the same Upper
// Limit and Limit Offset parameters. An Area is a spatial element too and has
// no such top, so it is named in `notSpatial` rather than written to.
//
// A TOP AT OR BELOW THE ROOM'S OWN FLOOR IS REFUSED, per room, before anything
// is written to it - the floor being its own level plus its base offset.
//
// EVERY ONE IS READ BACK after one regeneration. A room whose level or offset
// did not stick is named in `refused`, never counted.

// MILLIMETRES IN, FEET INSIDE (D-71). The add-in converts an XYZ at the
// boundary and cannot convert a bare double, so the conversion happens here,
// once, before the value is used for anything.
const double MillimetresPerFoot = 304.8;
limitOffset = limitOffset / MillimetresPerFoot;

var changed = 0;
var alreadyRight = new List<ElementId>();
var notSpatial = new List<ElementId>();
var topBelowFloor = new List<ElementId>();
var refused = new List<ElementId>();
var findings = new List<string>();
var pending = new List<Element>();

var roomsCategory = new ElementId(BuiltInCategory.OST_Rooms);
var spacesCategory = new ElementId(BuiltInCategory.OST_MEPSpaces);

if (upperLevel == null)
{
    findings.Add("No level was given for the top - name the level these should stop at");
}
else
{
    var top = upperLevel.ProjectElevation + limitOffset;

    foreach (var element in elements)
    {
        if (element == null) continue;

        var category = element.Category;
        if (category == null || (category.Id != roomsCategory && category.Id != spacesCategory))
        {
            notSpatial.Add(element.Id);
            continue;
        }

        var levelParameter = element.get_Parameter(BuiltInParameter.ROOM_UPPER_LEVEL);
        var offsetParameter = element.get_Parameter(BuiltInParameter.ROOM_UPPER_OFFSET);
        if (levelParameter == null || offsetParameter == null
            || levelParameter.IsReadOnly || offsetParameter.IsReadOnly)
        {
            refused.Add(element.Id);
            continue;
        }

        // The floor it stands on: its own level plus its base offset.
        Level baseLevel = null;
        try
        {
            baseLevel = doc.GetElement(element.get_Parameter(BuiltInParameter.ROOM_LEVEL_ID)
                                              .AsElementId()) as Level;
        }
        catch { }
        if (baseLevel == null)
        {
            refused.Add(element.Id);
            continue;
        }

        double baseOffset = 0;
        try { baseOffset = element.get_Parameter(BuiltInParameter.ROOM_LOWER_OFFSET).AsDouble(); }
        catch { }

        if (top <= baseLevel.ProjectElevation + baseOffset + 1.0 / MillimetresPerFoot)
        {
            topBelowFloor.Add(element.Id);
            continue;
        }

        if (levelParameter.AsElementId() == upperLevel.Id
            && Math.Abs(offsetParameter.AsDouble() - limitOffset) < 1e-9)
        {
            alreadyRight.Add(element.Id);
            continue;
        }

        try
        {
            levelParameter.Set(upperLevel.Id);
            offsetParameter.Set(limitOffset);
            pending.Add(element);
        }
        catch
        {
            refused.Add(element.Id);
        }
    }

    if (pending.Count > 0)
    {
        doc.Regenerate();

        foreach (var element in pending)
        {
            var levelNow = element.get_Parameter(BuiltInParameter.ROOM_UPPER_LEVEL);
            var offsetNow = element.get_Parameter(BuiltInParameter.ROOM_UPPER_OFFSET);
            if (levelNow != null && offsetNow != null
                && levelNow.AsElementId() == upperLevel.Id
                && Math.Abs(offsetNow.AsDouble() - limitOffset) < 1e-9)
                changed++;
            else
                refused.Add(element.Id);
        }
    }

    findings.Add(string.Format(
        "{0} set to stop at '{1}' {2:+0;-0;0} mm, a top at {3:0} mm. {4} were already there, {5} are "
        + "not rooms or spaces, {6} would have had a top at or below their own floor and were left "
        + "alone, {7} refused",
        changed, upperLevel.Name, limitOffset * MillimetresPerFoot, top * MillimetresPerFoot,
        alreadyRight.Count, notSpatial.Count, topBelowFloor.Count, refused.Count));
}
