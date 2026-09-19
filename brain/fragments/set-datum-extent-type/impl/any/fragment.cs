// NOT STANDALONE. Assumes `doc`, `view`, `elements` and `viewSpecific` are in
// scope; leaves `changed`, `wasAlready`, `notADatum` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE MISSING DIRECTION. SetDatumExtentType appears exactly once elsewhere in
// this library - reset-datum-extents line 60 - and always writes Model. Nothing
// could make an end view-specific, and nothing could ASK which it was without
// modifying the model to find out.
//
// EXTENTS ARE SHARED, BUBBLES ARE NOT. Setting an end back to Model reaches
// every view that shows the datum, not only this one. SET_DATUM_BUBBLES states
// the same distinction from the other side.
//
// BOTH ENDS SEPARATELY, because only one is usually dragged.

var changed = new List<ElementId>();
var wasAlready = 0;
var notADatum = new List<ElementId>();
var findings = new List<string>();

var wanted = viewSpecific ? DatumExtentType.ViewSpecific : DatumExtentType.Model;
var ends = new[] { DatumEnds.End0, DatumEnds.End1 };

if (view == null)
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
        if (datum == null)
        {
            notADatum.Add(element.Id);
            continue;
        }

        var touched = false;

        foreach (var end in ends)
        {
            DatumExtentType current;
            try
            {
                current = datum.GetDatumExtentTypeInView(end, view);
            }
            catch
            {
                // This view cannot answer for this end - not a change, and not
                // a datum problem either.
                continue;
            }

            if (current == wanted)
            {
                wasAlready++;
                continue;
            }

            try
            {
                datum.SetDatumExtentType(end, view, wanted);
            }
            catch
            {
                continue;
            }

            // READ IT BACK. The set can be accepted and not take.
            try
            {
                if (datum.GetDatumExtentTypeInView(end, view) == wanted) touched = true;
            }
            catch
            {
            }
        }

        if (touched) changed.Add(element.Id);
    }

    findings.Add(string.Format(
        "{0} datum(s) changed to {1}, {2} end(s) were already on it, {3} were "
        + "not grids or levels{4}",
        changed.Count,
        viewSpecific ? "2D (this view only)" : "3D (shared)",
        wasAlready, notADatum.Count,
        viewSpecific ? "" : ". A 3D extent is SHARED - this reaches every view "
            + "that shows these, not only this one"));
}
