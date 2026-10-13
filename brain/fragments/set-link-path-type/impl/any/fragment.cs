// NOT STANDALONE. Assumes `doc`, `linkName` and `pathType` are in scope;
// leaves `changed`, `alreadyThat`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// HOW EACH REVIT LINK STORES ITS PATH - Absolute (the whole drive path) or
// Relative (from this model's folder, so the set can move as one) - switched
// for one link or every link. Manage Links' Path Type column, for many at once.
//
// Revit forbids changing a path to or from Server, so a link on Revit Server
// or a cloud link is named and left alone.
//
// READ BACK: each link type's path type is read again after the change.

var changed = 0;
var alreadyThat = 0;
var lines = new List<string>();
var findings = new List<string>();

var wanted = (pathType ?? "").Trim().ToLowerInvariant();
PathType target = PathType.Relative;
string refusal = null;
if (wanted == "absolute") target = PathType.Absolute;
else if (wanted != "relative") refusal = "\"" + pathType + "\" is not absolute or relative. NOTHING WAS CHANGED.";

var name = (linkName ?? "").Trim();
var types = new List<RevitLinkType>();
if (refusal == null)
{
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkType)))
    {
        var type = element as RevitLinkType;
        if (type == null) continue;
        if (name != "" && !type.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase)) continue;
        types.Add(type);
    }
    if (types.Count == 0)
        refusal = name == "" ? "The model has no Revit links. NOTHING WAS CHANGED."
                             : "No Revit link's name starts \"" + name + "\". NOTHING WAS CHANGED.";
}

var asked = new List<RevitLinkType>();
if (refusal == null)
{
    foreach (var type in types)
    {
        PathType now;
        try { now = type.PathType; } catch { lines.Add(type.Name + ": its path type could not be read."); continue; }
        if (now == target) { alreadyThat++; continue; }
        if (now == PathType.Server)
        {
            lines.Add(type.Name + ": a Revit Server or cloud link, which Revit will not switch.");
            continue;
        }
        try
        {
            type.PathType = target;
            asked.Add(type);
        }
        catch (Exception ex)
        {
            lines.Add(type.Name + ": Revit refused - " + ex.Message);
        }
    }

    doc.Regenerate();
    foreach (var type in asked)
    {
        if (type.PathType == target) changed++;
        else lines.Add(type.Name + ": asked for " + target + " and reads back " + type.PathType + ".");
    }
    findings.Add(changed + " link(s) switched to " + target + " and read back; " + alreadyThat + " already " + target + ".");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
