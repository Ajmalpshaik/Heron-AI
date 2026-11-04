// NOT STANDALONE. Assumes `doc`, `linkName`, `roomBounding` and `pathType` are
// in scope; leaves `changed`, `alreadyThat`, `refused`, `report` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A REVIT LINK TYPE'S OWN SETTINGS, for one link or every link - version 2
// (2026-11-01), when SET_LINK_PATH_TYPE was merged in:
//
//   roomBounding   "true"/"on" or "false"/"off" - Room Bounding, so this
//                  model's rooms and spaces stop at the link's walls. The
//                  built-in parameter is asked first and the type parameter
//                  named "Room Bounding" second; a type carrying neither is
//                  named and left alone
//   pathType       "relative" or "absolute" - how the link stores its path,
//                  Manage Links' Path Type column. Revit forbids switching to
//                  or from Server, so a Revit Server or cloud link is named
//                  and left alone
//
// Either may be left out ("unchanged"), never both. Every change is READ
// BACK, after the model is regenerated (SET_LINK_PATH_TYPE's rule), and a
// setting already as asked is never written again.

var changed = 0;
var alreadyThat = 0;
var refused = new List<string>();
var lines = new List<string>();
var findings = new List<string>();

// WHAT WAS ASKED. "unchanged" is the card's word for left out.
var boundingText = (roomBounding ?? "").Trim().ToLowerInvariant();
var pathText = (pathType ?? "").Trim().ToLowerInvariant();
var askBounding = boundingText.Length > 0 && boundingText != "unchanged";
var askPath = pathText.Length > 0 && pathText != "unchanged";
var wantedBounding = 0;
PathType targetPath = PathType.Relative;
string refusal = null;

if (!askBounding && !askPath)
    refusal = "Nothing was asked - say roomBounding (true or false), pathType (relative or absolute), or both. "
        + "NOTHING WAS CHANGED.";
else if (askBounding && boundingText != "true" && boundingText != "on" && boundingText != "yes"
         && boundingText != "false" && boundingText != "off" && boundingText != "no")
    refusal = "\"" + roomBounding + "\" is not true or false for Room Bounding. NOTHING WAS CHANGED.";
else if (askPath && pathText != "relative" && pathText != "absolute")
    refusal = "\"" + pathType + "\" is not absolute or relative. NOTHING WAS CHANGED.";
if (askBounding) wantedBounding = boundingText == "true" || boundingText == "on" || boundingText == "yes" ? 1 : 0;
if (askPath && pathText == "absolute") targetPath = PathType.Absolute;

var name = (linkName ?? "").Trim();
var types = new List<RevitLinkType>();
if (refusal == null)
{
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkType)))
    {
        var type = element as RevitLinkType;
        if (type == null) continue;
        if (name.Length > 0 && !type.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase)) continue;
        types.Add(type);
    }
    if (types.Count == 0)
        refusal = name.Length == 0 ? "This model has no Revit links. NOTHING WAS CHANGED."
                                   : "No Revit link's name starts \"" + name + "\". NOTHING WAS CHANGED.";
}

if (refusal != null)
{
    refused.Add(refusal);
}
else
{
    Func<RevitLinkType, Parameter> boundingOf = type =>
    {
        var p = type.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING);
        if (p == null) p = type.LookupParameter("Room Bounding");
        return p;
    };

    // Per link type: what it was asked, what it was written, what is already so.
    var pathAsked = new List<RevitLinkType>();
    var boundingAsked = new List<RevitLinkType>();
    var touched = new HashSet<ElementId>();
    var unchangedTypes = 0;

    foreach (var type in types)
    {
        var allAlready = true;

        // PATH TYPE (from SET_LINK_PATH_TYPE).
        if (askPath)
        {
            PathType now;
            var read = true;
            try { now = type.PathType; } catch { now = PathType.Absolute; read = false; }
            if (!read)
            {
                allAlready = false;
                lines.Add(type.Name + ": its path type could not be read.");
            }
            else if (now != targetPath)
            {
                allAlready = false;
                if (now == PathType.Server)
                    lines.Add(type.Name + ": a Revit Server or cloud link, which Revit will not switch.");
                else
                {
                    try
                    {
                        type.PathType = targetPath;
                        pathAsked.Add(type);
                    }
                    catch (Exception ex)
                    {
                        lines.Add(type.Name + ": Revit refused the path type - " + ex.Message);
                    }
                }
            }
        }

        // ROOM BOUNDING.
        if (askBounding)
        {
            var p = boundingOf(type);
            if (p == null || p.StorageType != StorageType.Integer)
            {
                allAlready = false;
                refused.Add(type.Name + ": no Room Bounding setting could be found on this link type - left alone.");
            }
            else if (p.AsInteger() != wantedBounding)
            {
                allAlready = false;
                if (p.IsReadOnly)
                    refused.Add(type.Name + ": Room Bounding is read-only here - owned by somebody else on a workshared "
                        + "model, or the link is not loaded. Left alone.");
                else
                {
                    try
                    {
                        p.Set(wantedBounding);
                        boundingAsked.Add(type);
                    }
                    catch (Exception ex)
                    {
                        refused.Add(type.Name + ": Revit refused - " + ex.Message);
                    }
                }
            }
        }

        if (allAlready) alreadyThat++;
    }

    // REGENERATED BEFORE ANYTHING IS READ BACK (SET_LINK_PATH_TYPE's rule).
    if (pathAsked.Count > 0 || boundingAsked.Count > 0) doc.Regenerate();

    foreach (var type in pathAsked)
    {
        if (type.PathType == targetPath)
        {
            touched.Add(type.Id);
            findings.Add(type.Name + ": path type is now " + targetPath + ", read back.");
        }
        else lines.Add(type.Name + ": asked for " + targetPath + " and reads back " + type.PathType + ".");
    }
    foreach (var type in boundingAsked)
    {
        var again = boundingOf(type);
        if (again != null && again.AsInteger() == wantedBounding)
        {
            touched.Add(type.Id);
            findings.Add(type.Name + ": Room Bounding is now " + (wantedBounding == 1 ? "ON" : "OFF") + ", read back.");
        }
        else
        {
            refused.Add(type.Name + ": Revit accepted the change and it does not read back - left as it was found.");
        }
    }
    changed = touched.Count;
    unchangedTypes = types.Count - changed - alreadyThat;

    var asked = new List<string>();
    if (askPath) asked.Add("path type " + targetPath);
    if (askBounding) asked.Add("Room Bounding " + (wantedBounding == 1 ? "ON" : "OFF"));
    findings.Insert(0, string.Format("{0} link type(s) set to {1} and read back, {2} already were, {3} left alone. "
        + "Every placed copy of a link follows its type.", changed, string.Join(" and ", asked), alreadyThat,
        unchangedTypes));
    if (askBounding && wantedBounding == 1 && boundingAsked.Count > 0)
        findings.Add("Rooms and spaces that were unbounded may now bound: PLACE_ROOMS places them, "
            + "SYNC_SPACES_WITH_LINKED_ROOMS names them from the link.");
}

// Every link left alone, and why, in one string (SET_LINK_PATH_TYPE's `report`).
foreach (var reason in refused) lines.Add(reason);
var report = string.Join(" | ", lines);
foreach (var line in lines) findings.Add(line);
