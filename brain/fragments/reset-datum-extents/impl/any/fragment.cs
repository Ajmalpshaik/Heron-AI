// NOT STANDALONE. Assumes `doc`, `elements` and `views` are in scope, and
// leaves `endsReset`, `datumsChanged`, `alreadyShared`, `notADatum` and
// `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). A set of views, one undo.
//
// A VIEW-SPECIFIC END OVERRIDES THE SHARED ONE.
//
// That is why maximizing the extents can change nothing in the one view
// somebody is looking at: the end there is on its own extent and ignores the
// shared length entirely. Clearing the override is what lets the shared extent
// through, which is why these two fragments are a pair.
//
// AND IT IS NOT A CHANGE CONFINED TO THIS VIEW.
//
// Handing an end back to the shared extent moves the line here AND ends its
// independence here for good. Somebody dragged it short for a reason, once -
// so this is a bulk change worth counting out loud, not a tidy-up.
//
// AN END ALREADY SHARED IS NOT A CHANGE.
//
// Setting it again would succeed and do nothing, and reporting it as reset
// would put a number in front of somebody that does not match the drawings.

int endsReset = 0;
var datumsChanged = new List<ElementId>();
var alreadyShared = new List<ElementId>();
var notADatum = new List<ElementId>();
var refused = new List<ElementId>();

var ends = new DatumEnds[] { DatumEnds.End0, DatumEnds.End1 };

foreach (var element in elements)
{
    var datum = element as DatumPlane;
    if (datum == null || !(datum is Grid || datum is Level))
    {
        if (element != null) notADatum.Add(element.Id);
        continue;
    }

    bool changedHere = false;
    bool anyOverride = false;
    bool anyFailure = false;

    foreach (var view in views)
    {
        if (view == null) continue;

        foreach (var end in ends)
        {
            DatumExtentType current;
            try { current = datum.GetDatumExtentTypeInView(end, view); }
            catch { continue; }   // this datum is not shown in this view at all

            if (current != DatumExtentType.ViewSpecific) continue;

            anyOverride = true;

            try { datum.SetDatumExtentType(end, view, DatumExtentType.Model); }
            catch { anyFailure = true; continue; }

            // What the model says now. The set call returning is not evidence.
            DatumExtentType after;
            try { after = datum.GetDatumExtentTypeInView(end, view); }
            catch { anyFailure = true; continue; }

            if (after == DatumExtentType.Model)
            {
                endsReset++;
                changedHere = true;
            }
            else anyFailure = true;
        }
    }

    if (changedHere) datumsChanged.Add(datum.Id);
    else if (!anyOverride) alreadyShared.Add(datum.Id);

    if (anyFailure && !refused.Contains(datum.Id)) refused.Add(datum.Id);
}
