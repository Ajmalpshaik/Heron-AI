// NOT STANDALONE. Assumes `doc`, `elements`, `whichEnd` and `allowJoin` are in
// scope, and leaves `changed`, `wasPinned`, `unsupported` and `refused`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A PINNED ELEMENT REFUSES WITHOUT SAYING SO.
//
// The join calls do not throw on a pinned wall. They return normally and
// change nothing - so the script reports success and the model is untouched,
// which reads as a Revit bug rather than as a pin. Pinned elements are
// unpinned, changed, and pinned back, and the count is reported because
// otherwise nothing about this is visible.
//
// TWO FAMILIES OF CALL, NO SHARED ONE.
//
// Walls have their own join utilities and structural framing has different
// ones. There is no common method, so anything that is neither is named as
// unsupported. A mixed selection is the normal input, and "nothing happened"
// is not an answer to it.
//
// THE END NUMBERS ARE THE LOCATION CURVE'S, NOT THE SCREEN'S.
//
// End 0 is where the element was started and end 1 where it was finished.
// Which of those is the corner somebody is pointing at depends on how it was
// drawn, and nothing shows it - so both is the answer that matches what people
// mean.
//
// READ THE STATE BACK.
//
// Each end is asked afterwards whether the join is allowed, and only a state
// that actually matches what was asked for counts as changed.

int changed = 0;
var wasPinned = new List<ElementId>();
var unsupported = new List<ElementId>();
var refused = new List<ElementId>();

string ends = (whichEnd ?? "").Trim().ToLowerInvariant();
bool doStart = ends != "end";
bool doFinish = ends != "start";

foreach (var element in elements)
{
    if (element == null) continue;

    var wall = element as Wall;
    var framing = element as FamilyInstance;
    bool isFraming = false;
    if (wall == null && framing != null)
    {
        try
        {
            isFraming = framing.Category != null &&
                        framing.Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming);
        }
        catch { }
    }

    if (wall == null && !isFraming) { unsupported.Add(element.Id); continue; }

    bool pinned = false;
    try { pinned = element.Pinned; } catch { }
    if (pinned)
    {
        wasPinned.Add(element.Id);
        try { element.Pinned = false; }
        catch { refused.Add(element.Id); continue; }
    }

    bool everyEndTook = true;

    for (int end = 0; end <= 1; end++)
    {
        if (end == 0 && !doStart) continue;
        if (end == 1 && !doFinish) continue;

        try
        {
            if (wall != null)
            {
                if (allowJoin) WallUtils.AllowWallJoinAtEnd(wall, end);
                else WallUtils.DisallowWallJoinAtEnd(wall, end);
            }
            else
            {
                if (allowJoin) StructuralFramingUtils.AllowJoinAtEnd(framing, end);
                else StructuralFramingUtils.DisallowJoinAtEnd(framing, end);
            }
        }
        catch { everyEndTook = false; continue; }

        // What the model says now, not what was asked for.
        bool allowedNow = true;
        try
        {
            allowedNow = wall != null
                ? WallUtils.IsWallJoinAllowedAtEnd(wall, end)
                : StructuralFramingUtils.IsJoinAllowedAtEnd(framing, end);
        }
        catch { everyEndTook = false; continue; }

        if (allowedNow != allowJoin) everyEndTook = false;
    }

    // Back as it was found. A pin is somebody's decision and this is not the
    // fragment that revisits it.
    if (pinned)
    {
        try { element.Pinned = true; } catch { }
    }

    if (everyEndTook) changed++;
    else refused.Add(element.Id);
}
