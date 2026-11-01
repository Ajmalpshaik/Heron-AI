// NOT STANDALONE. Assumes `doc`, `view`, `views`, `elements` and
// `viewSpecific` are in scope; leaves `changed`, `wasAlready`, `notADatum`,
// `findings`, `endsChanged`, `alreadyAsAsked` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). A set of views, one undo.
//
// VERSION 2 (2026-11-01) - RESET_DATUM_EXTENTS MERGED IN. That card was this
// one's write in one direction - every view-specific end back to the shared
// 3D extent - over MANY views. What it had and version 1 lacked:
//
//   MANY VIEWS. `views` as well as `view`: every end of every datum in every
//   view named. A datum not shown in one of them is skipped there without a
//   refusal, and the other views still done.
//
//   REFUSALS PER DATUM, AND A FAILED WRITE NEVER DROPPED. An end whose set
//   throws, or reads back on the other extent type, puts its datum in
//   `refused`. Version 1 stepped over both without a word - the write that
//   did not take vanished, and the datum simply was not in `changed`.
//
//   COUNTED IN ENDS AS WELL AS DATUMS - `endsChanged` - and the datums every
//   shown end of which was already as asked in `alreadyAsAsked`.
//
//   ONLY GRIDS AND LEVELS, as RESET_DATUM_EXTENTS took. Anything else handed
//   in is `notADatum`.
//
// THE MISSING DIRECTION, version 1's reason. SetDatumExtentType appeared once
// elsewhere in this library - RESET_DATUM_EXTENTS - and always wrote Model.
// Nothing could make an end view-specific, and nothing could ASK which it was
// without modifying the model to find out.
//
// A VIEW-SPECIFIC END OVERRIDES THE SHARED ONE. That is why maximizing the
// extents can change nothing in the one view somebody is looking at: the end
// there is on its own extent. Clearing the override is what lets the shared
// extent through - and it ends that end's independence in that view for good.
//
// EXTENTS ARE SHARED, BUBBLES ARE NOT. Setting an end back to Model reaches
// every view that shows the datum, not only this one. SET_DATUM_BUBBLES states
// the same distinction from the other side.
//
// BOTH ENDS SEPARATELY, because only one is usually dragged.
//
// AN END ALREADY AS ASKED IS NOT A CHANGE. Setting it again would succeed and
// do nothing, and a count of it would not match the drawings.

var changed = new List<ElementId>();
var wasAlready = 0;
var notADatum = new List<ElementId>();
var findings = new List<string>();
// VERSION 2, from RESET_DATUM_EXTENTS.
var endsChanged = 0;
var alreadyAsAsked = new List<ElementId>();
var refused = new List<ElementId>();

var wanted = viewSpecific ? DatumExtentType.ViewSpecific : DatumExtentType.Model;
var ends = new[] { DatumEnds.End0, DatumEnds.End1 };

// THE VIEWS: `view`, and every one in `views`, each once.
var inViews = new List<View>();
if (view != null) inViews.Add(view);
if (views != null)
{
    foreach (var each in views)
    {
        if (each != null && !inViews.Any(had => had.Id == each.Id)) inViews.Add(each);
    }
}

if (inViews.Count == 0)
{
    findings.Add("No view was given. A datum's extent type is asked and set PER "
        + "VIEW, even though the model extent it points at is shared");
}
else if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were given, so there is nothing to set");
}
else
{
    foreach (var element in elements)
    {
        if (element == null) continue;

        var datum = element as DatumPlane;
        if (datum == null || !(datum is Grid || datum is Level))
        {
            notADatum.Add(element.Id);
            continue;
        }

        var touched = false;
        var needed = false;
        var failedHere = false;

        foreach (var inView in inViews)
        {
            foreach (var end in ends)
            {
                DatumExtentType current;
                try
                {
                    current = datum.GetDatumExtentTypeInView(end, inView);
                }
                catch
                {
                    // This datum is not shown in this view, or this view cannot
                    // answer for this end - not a change, and not a refusal.
                    continue;
                }

                if (current == wanted)
                {
                    wasAlready++;
                    continue;
                }

                needed = true;
                try
                {
                    datum.SetDatumExtentType(end, inView, wanted);
                }
                catch
                {
                    // VERSION 2: A FAILED WRITE IS RECORDED, NEVER DROPPED.
                    failedHere = true;
                    continue;
                }

                // READ IT BACK. The set can be accepted and not take.
                var took = false;
                try { took = datum.GetDatumExtentTypeInView(end, inView) == wanted; }
                catch { took = false; }

                if (took)
                {
                    endsChanged++;
                    touched = true;
                }
                else failedHere = true;
            }
        }

        if (touched) changed.Add(element.Id);
        else if (!needed) alreadyAsAsked.Add(element.Id);
        if (failedHere && !refused.Contains(element.Id)) refused.Add(element.Id);
    }

    findings.Add(string.Format(
        "{0} datum(s) changed to {1} in {2} view(s) - {3} end(s) - {4} end(s) were already on it, {5} "
        + "datum(s) had an end Revit would not change{6}, {7} were not grids or levels{8}",
        changed.Count,
        viewSpecific ? "2D (that view only)" : "3D (shared)",
        inViews.Count, endsChanged, wasAlready, refused.Count,
        refused.Count == 0 ? "" : " (" + string.Join(", ", refused.Select(id => id.ToString())) + ")",
        notADatum.Count,
        viewSpecific ? "" : ". A 3D extent is SHARED - this reaches every view "
            + "that shows these, not only the ones named"));
}
