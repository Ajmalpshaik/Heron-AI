// NOT STANDALONE. Assumes `doc`, `elements`, `pathMm`, `host`,
// `railingTypeName`, `flip` and `reset` are in scope; leaves `edited`,
// `editedIds`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// EDITS RAILINGS ALREADY IN THE MODEL - Edit Path, Pick New Host, Reset
// Railing, Flip, and the type in Properties. Each part is done only when it is
// asked, in this order:
//
//   reset            true puts a hosted railing back on its host's own path
//   pathMm           "x,y; x,y; ..." a new path, an open run of straight
//                    lines at the railing's own height; EMPTY keeps it
//   host             an element id - a stair, a floor, a roof - to host it;
//                    "none" frees it from its host; EMPTY keeps it
//   railingTypeName  another railing type by name; EMPTY keeps it
//   flip             true turns it to the other side of its path
//
// READ BACK: the path's length, the host, the type and the side are read
// again after a regeneration and each asked change is checked.

const double MmPerFoot = 304.8;
var edited = 0;
var editedIds = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();
string refusal = null;

var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;
var path = new List<XYZ>();
foreach (var chunk in (pathMm ?? "").Split(';'))
{
    if (chunk.Trim() == "") continue;
    var parts = chunk.Split(',');
    double x, y;
    if (parts.Length != 2 || !double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
        || !double.TryParse(parts[1].Trim(), numberStyle, culture, out y)) { path = null; break; }
    path.Add(new XYZ(x / MmPerFoot, y / MmPerFoot, 0));
}
if (path == null) refusal = "The path is x,y points in millimetres, separated by semicolons. NOTHING WAS CHANGED.";
else if (path.Count == 1) refusal = "A path needs two points or more. NOTHING WAS CHANGED.";

Element newType = null;
var wantedType = (railingTypeName ?? "").Trim();
if (refusal == null && wantedType != "")
{
    var names = new List<string>();
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(RailingType)))
    {
        names.Add(candidate.Name);
        if (string.Equals(candidate.Name, wantedType, StringComparison.OrdinalIgnoreCase)) newType = candidate;
    }
    if (newType == null) refusal = "No railing type is named \"" + wantedType + "\" - there are: " + string.Join(", ", names) + ". NOTHING WAS CHANGED.";
}

Element newHost = null;
var hostText = (host ?? "").Trim();
var freeIt = string.Equals(hostText, "none", StringComparison.OrdinalIgnoreCase);
if (refusal == null && hostText != "" && !freeIt)
{
    foreach (var candidate in new FilteredElementCollector(doc).WhereElementIsNotElementType())
        if (candidate.Id.ToString() == hostText || candidate.UniqueId == hostText) { newHost = candidate; break; }
    if (newHost == null) refusal = "No element " + hostText + " to host the railings. NOTHING WAS CHANGED.";
}

var nothingAsked = !reset && path != null && path.Count == 0 && hostText == "" && newType == null && !flip;
if (refusal == null && nothingAsked) refusal = "Nothing was asked - give a path, a host, a type, flip or reset. NOTHING WAS CHANGED.";

if (refusal == null)
{
    foreach (var element in elements)
    {
        var railing = element as Railing;
        if (railing == null) { if (element != null) lines.Add(element.Id + ": not a railing."); continue; }
        var done = new List<string>();
        var wrong = new List<string>();
        try
        {
            var wasFlipped = railing.Flipped;
            if (reset)
            {
                if (railing.CanReset) { railing.Reset(); done.Add("reset to its host"); }
                else wrong.Add("it cannot be reset - it has no host path");
            }
            if (path.Count >= 2)
            {
                var old = railing.GetPath();
                var z = old != null && old.Count > 0 ? old[0].GetEndPoint(0).Z : 0;
                var loop = new CurveLoop();
                for (var i = 0; i + 1 < path.Count; i++)
                    loop.Append(Line.CreateBound(new XYZ(path[i].X, path[i].Y, z), new XYZ(path[i + 1].X, path[i + 1].Y, z)));
                if (!Railing.IsValidPathForRailing(loop)) wrong.Add("Revit will not take that path - one connected run of straight lines");
                else { railing.SetPath(loop); done.Add("new path"); }
            }
            if (freeIt)
            {
                if (railing.HasHost) { railing.RemoveHost(); done.Add("freed from its host"); }
            }
            else if (newHost != null)
            {
                if (!railing.RailingCanBeHostedByElement(newHost.Id)) wrong.Add(newHost.Id + " cannot host it");
                else { railing.HostId = newHost.Id; done.Add("hosted by " + newHost.Id); }
            }
            if (newType != null && railing.GetTypeId() != newType.Id) { railing.ChangeTypeId(newType.Id); done.Add("type " + newType.Name); }
            if (flip) railing.Flip();
            doc.Regenerate();

            // Read back.
            if (flip)
            {
                if (railing.Flipped != wasFlipped) done.Add("flipped");
                else wrong.Add("asked to flip and the side reads the same");
            }
            if (newType != null && railing.GetTypeId() != newType.Id) wrong.Add("the type reads " + doc.GetElement(railing.GetTypeId()).Name);
            if (newHost != null && railing.HostId != newHost.Id) wrong.Add("the host reads " + railing.HostId);
            if (freeIt && railing.HasHost) wrong.Add("it still reads a host");
            double length = 0;
            foreach (var curve in railing.GetPath()) length += curve.Length;
            edited += done.Count > 0 ? 1 : 0;
            if (done.Count > 0) editedIds.Add(railing.Id);
            lines.Add("railing " + railing.Id + ": " + (done.Count > 0 ? string.Join(", ", done) : "nothing changed")
                + (wrong.Count > 0 ? "; " + string.Join("; ", wrong) : "")
                + "; path " + Math.Round(length * MmPerFoot) + " mm now.");
        }
        catch (Exception ex) { lines.Add("railing " + railing.Id + ": Revit refused - " + ex.Message); }
    }
    findings.Add(edited + " railing(s) edited and read back.");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
