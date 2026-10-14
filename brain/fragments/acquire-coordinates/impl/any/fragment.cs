// NOT STANDALONE. Assumes `doc` and `linkInstance` are in scope; leaves
// `acquired`, `before`, `after`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// NOTHING MOVES; WHAT EVERYTHING IS SAID TO BE DOES. The shared position of the
// internal origin is read before and after - the evidence of what changed -
// and the after is read back from Revit, never worked out here.

var acquired = false;
var before = "";
var after = "";
var refused = new List<string>();
var findings = new List<string>();

Func<string> origin = () =>
{
    var location = doc.ActiveProjectLocation;
    if (location == null) return "";
    var p = location.GetProjectPosition(XYZ.Zero);
    if (p == null) return "";
    return string.Format("E/W {0:0} mm, N/S {1:0} mm, elevation {2:0} mm, angle to True North {3:0.###} degrees",
        p.EastWest * 304.8, p.NorthSouth * 304.8, p.Elevation * 304.8, p.Angle * 180.0 / Math.PI);
};

if (linkInstance == null)
{
    refused.Add("No linked model was named, so NOTHING WAS ACQUIRED.");
}
else
{
    var linked = linkInstance.GetLinkDocument();
    if (linked == null)
    {
        refused.Add("'" + linkInstance.Name + "' is not loaded, so its coordinates cannot be read. NOTHING WAS "
            + "ACQUIRED - RELOAD_LINKS first.");
    }
    else
    {
        try { before = origin(); } catch (Exception ex) { before = ""; findings.Add("The position before could not be read - " + ex.Message); }
        try
        {
            doc.AcquireCoordinates(linkInstance.Id);
            acquired = true;
        }
        catch (Exception ex)
        {
            refused.Add("Revit refused to acquire coordinates from '" + linkInstance.Name + "' - " + ex.Message
                + " Nothing was changed.");
        }

        if (acquired)
        {
            try { after = origin(); } catch (Exception ex) { findings.Add("The position after could not be read - " + ex.Message); }
            findings.Add("Acquired shared coordinates from '" + linkInstance.Name + "' (" + linked.Title + ").");
            findings.Add("This model's internal origin, in shared coordinates - before: "
                + (before.Length == 0 ? "not read" : before) + "; after: " + (after.Length == 0 ? "not read" : after) + ".");
            if (before.Length > 0 && before == after)
                findings.Add("THE POSITION DID NOT CHANGE - the two models already shared these coordinates.");
            findings.Add("NO ELEMENT MOVED. Every shared coordinate, spot coordinate and export now reads from the "
                + "link's system - SAVE to keep it, and CHECK_LINK_COORDINATES on the other links.");
        }
    }
}

foreach (var reason in refused) findings.Add(reason);
