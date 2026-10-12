// NOT STANDALONE. Assumes `doc`, `elements`, `view` and `ends` are in scope;
// leaves `added`, `addedOn`, `alreadyHad`, `noBubble`, `report` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// THE ELBOW ON A GRID OR LEVEL HEAD - "Add Elbow" in the ribbon - for many
// datums in one view at once. Revit's own rule decides where one can go: only
// at an end whose bubble is SHOWN in that view, and only where there is no
// leader already. Both are checked first, per end, and every end left alone is
// counted with its reason.
//
// WHICH ENDS: "bubble" (the default) is every end showing a bubble in the
// view; "start" and "end" are one end only.
//
// READ BACK: an end counts only if Revit hands a leader back for it after a
// regeneration.
//
// Reference planes carry no leaders in any release and are named, not tried.
// Revit offers no call to remove a leader; that is done by hand.

var added = 0;
var addedOn = new List<ElementId>();
var alreadyHad = 0;
var noBubble = 0;
var lines = new List<string>();
var findings = new List<string>();

var which = (ends ?? "").Trim().ToLowerInvariant();
var wantedEnds = new List<DatumEnds>();
if (which == "start") wantedEnds.Add(DatumEnds.End0);
else if (which == "end") wantedEnds.Add(DatumEnds.End1);
else { wantedEnds.Add(DatumEnds.End0); wantedEnds.Add(DatumEnds.End1); }

string refusal = null;
if (view == null) refusal = "No view was given, and a datum's elbow belongs to one view. NOTHING WAS ADDED.";
else if (which != "" && which != "bubble" && which != "start" && which != "end")
    refusal = "\"" + ends + "\" is not bubble, start or end. NOTHING WAS ADDED.";

var tried = new List<Tuple<DatumPlane, DatumEnds>>();
if (refusal == null)
{
    foreach (var element in elements)
    {
        if (element == null || !element.IsValidObject) continue;
        var label = (element.Category == null ? "element" : element.Category.Name) + " " + element.Id;
        if (element is ReferencePlane)
        {
            lines.Add(label + ": a reference plane carries no leader in any release.");
            continue;
        }
        var datum = element as DatumPlane;
        if (datum == null)
        {
            lines.Add(label + ": not a grid or level.");
            continue;
        }
        bool visible = false;
        try { visible = datum.CanBeVisibleInView(view); } catch { }
        if (!visible)
        {
            lines.Add(label + ": not seen in \"" + view.Name + "\".");
            continue;
        }

        foreach (var end in wantedEnds)
        {
            bool bubble = false;
            try { bubble = datum.HasBubbleInView(end, view); } catch { }
            if (!bubble)
            {
                // With "bubble" an end with no bubble is simply not one of the
                // ends asked for; named only when the caller chose that end.
                if (which == "start" || which == "end")
                {
                    noBubble++;
                    lines.Add(label + ": no bubble shown at its " + which + " in this view, and "
                        + "Revit puts an elbow only where the bubble shows.");
                }
                continue;
            }
            Leader existing = null;
            try { existing = datum.GetLeader(end, view); } catch { }
            if (existing != null) { alreadyHad++; continue; }
            try
            {
                datum.AddLeader(end, view);
                tried.Add(Tuple.Create(datum, end));
            }
            catch (Exception ex)
            {
                lines.Add(label + ": Revit refused the elbow - " + ex.Message);
            }
        }
    }

    doc.Regenerate();

    foreach (var pair in tried)
    {
        Leader now = null;
        try { now = pair.Item1.GetLeader(pair.Item2, view); } catch { }
        if (now != null)
        {
            added++;
            if (!addedOn.Contains(pair.Item1.Id)) addedOn.Add(pair.Item1.Id);
        }
        else lines.Add(pair.Item1.Id + ": asked for, and no leader reads back at that end.");
    }

    findings.Add(added + " elbow(s) added and read back in \"" + view.Name + "\", " + alreadyHad
        + " end(s) already had one, " + noBubble + " end(s) showed no bubble.");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
