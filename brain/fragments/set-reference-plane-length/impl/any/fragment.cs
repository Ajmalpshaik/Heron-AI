// NOT STANDALONE. Assumes `doc`, `elements` and `lengthMm` are in scope;
// leaves `changed`, `alreadyThat`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// REFERENCE PLANES MADE ONE LENGTH, each about its own middle - the plane
// stays where it is and faces the same way, only its two ends move in or out
// along it. A drawing full of planes dragged to every length reads as clutter;
// one length reads as a set.
//
// The plane's direction never changes: Revit refuses an end that would turn
// it, and the ends are moved only along the line they already lie on.
//
// READ BACK: each plane's length is read again afterwards, and only one that
// reads as asked counts.

const double MmPerFoot = 304.8;
var changed = 0;
var alreadyThat = 0;
var lines = new List<string>();
var findings = new List<string>();
var target = lengthMm / MmPerFoot;

if (lengthMm <= 0) lines.Add("A length must be more than 0 mm. NOTHING WAS CHANGED.");
else
{
    foreach (var element in elements)
    {
        var plane = element as ReferencePlane;
        if (plane == null) { if (element != null) lines.Add(element.Id + ": not a reference plane."); continue; }
        var a = plane.BubbleEnd;
        var b = plane.FreeEnd;
        var now = a.DistanceTo(b);
        if (Math.Abs(now - target) < 0.5 / MmPerFoot) { alreadyThat++; continue; }
        var middle = (a + b) / 2.0;
        var along = (b - a).Normalize();
        try
        {
            // Free end first, then bubble end: setting either one to the other's
            // position is refused, and moving outward first keeps them apart.
            var newFree = middle + along * (target / 2.0);
            var newBubble = middle - along * (target / 2.0);
            if (target > now) { plane.FreeEnd = newFree; plane.BubbleEnd = newBubble; }
            else { plane.BubbleEnd = newBubble; plane.FreeEnd = newFree; }
        }
        catch (Exception ex) { lines.Add("reference plane " + plane.Id + ": Revit refused - " + ex.Message); continue; }
        doc.Regenerate();
        var after = plane.BubbleEnd.DistanceTo(plane.FreeEnd);
        if (Math.Abs(after - target) < 0.5 / MmPerFoot) changed++;
        else lines.Add("reference plane " + plane.Id + ": asked for " + lengthMm + " mm and reads back "
            + Math.Round(after * MmPerFoot) + " mm.");
    }
    findings.Add(changed + " reference plane(s) set to " + lengthMm + " mm about their middles and read back; "
        + alreadyThat + " already that length.");
}

var report = string.Join(" | ", lines);
findings.AddRange(lines);
