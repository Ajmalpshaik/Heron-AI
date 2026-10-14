// NOT STANDALONE. Assumes `doc`, `linkName` and `roomBounding` are in scope;
// leaves `changed`, `alreadyThat`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A TYPE SETTING ON EACH REVIT LINK TYPE. The built-in parameter is asked
// first and the type parameter named "Room Bounding" second; a type carrying
// neither is named and left alone. Every change is READ BACK.

var changed = 0;
var alreadyThat = 0;
var refused = new List<string>();
var findings = new List<string>();

var name = (linkName ?? "").Trim();
var types = new List<RevitLinkType>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkType)))
{
    var type = element as RevitLinkType;
    if (type == null) continue;
    if (name.Length > 0 && !type.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase)) continue;
    types.Add(type);
}

if (types.Count == 0)
{
    refused.Add(name.Length == 0 ? "This model has no Revit links. NOTHING WAS CHANGED."
                                 : "No Revit link's name starts \"" + name + "\". NOTHING WAS CHANGED.");
}
else
{
    var wanted = roomBounding ? 1 : 0;
    foreach (var type in types)
    {
        var p = type.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING);
        if (p == null) p = type.LookupParameter("Room Bounding");
        if (p == null || p.StorageType != StorageType.Integer)
        {
            refused.Add(type.Name + ": no Room Bounding setting could be found on this link type - left alone.");
            continue;
        }
        if (p.AsInteger() == wanted) { alreadyThat++; continue; }
        if (p.IsReadOnly)
        {
            refused.Add(type.Name + ": Room Bounding is read-only here - owned by somebody else on a workshared "
                + "model, or the link is not loaded. Left alone.");
            continue;
        }
        try
        {
            p.Set(wanted);
            var again = type.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING);
            if (again == null) again = type.LookupParameter("Room Bounding");
            if (again != null && again.AsInteger() == wanted)
            {
                changed++;
                findings.Add(type.Name + ": Room Bounding is now " + (roomBounding ? "ON" : "OFF") + ", read back.");
            }
            else
            {
                refused.Add(type.Name + ": Revit accepted the change and it does not read back - left as it was found.");
            }
        }
        catch (Exception ex)
        {
            refused.Add(type.Name + ": Revit refused - " + ex.Message);
        }
    }

    findings.Insert(0, string.Format("{0} link type(s) set Room Bounding {1}, {2} already were, {3} left alone. "
        + "Every placed copy of a link follows its type.", changed, roomBounding ? "ON" : "OFF", alreadyThat,
        refused.Count));
    if (roomBounding && changed > 0)
        findings.Add("Rooms and spaces that were unbounded may now bound: PLACE_ROOMS places them, "
            + "SYNC_SPACES_WITH_LINKED_ROOMS names them from the link.");
}

foreach (var reason in refused) findings.Add(reason);
