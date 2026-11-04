// NOT STANDALONE. Assumes `doc`, `linkInstance`, `position` and `confirm` are
// in scope; leaves `published`, `positionName`, `before`, `after`, `expected`,
// `waitingToSave`, `refused`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// REVIT'S MANAGE > COORDINATES > PUBLISH COORDINATES, FOR ONE REVIT LINK.
// This model's shared origin and True North are recorded in the linked model
// as one of its named positions, based on where the link sits now -
// Document.PublishCoordinates(LinkElementId(link, position in the link)).
//
// ONLY ON THE OWNER'S YES FOR THIS LINK (Article 7). `confirm` must repeat the
// link's name exactly - its file name as Manage Links lists it, or its
// instance name. Anything else publishes nothing and says what would change.
//
// THE LINKED FILE IS NOT SAVED HERE (Article 10). Revit holds the published
// position in the session and asks at the next save of this model whether to
// save it into the link. RevitLinkType.SavePositions would write it now, but
// it takes a callback that has to be a class - a fragment cannot declare one -
// and it is a save of another file, which is the modeller's. Whether a
// position is waiting is read back with HasSaveablePositions.
//
// READ BACK. The linked model's internal origin, in the chosen position's
// shared coordinates, is read before and after, and set beside this model's
// shared coordinates at the link's origin - what it should now read. A
// difference is named, never hidden.

var published = false;
var positionName = "";
var before = "";
var after = "";
var expected = "";
var waitingToSave = false;
var refused = "";
var report = "";
var findings = new List<string>();

const double MillimetresPerFoot = 304.8;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<ProjectPosition, string> say = p => p == null ? "" : string.Format(invariant,
    "E/W {0:0} mm, N/S {1:0} mm, elevation {2:0} mm, angle to True North {3:0.###} degrees",
    p.EastWest * MillimetresPerFoot, p.NorthSouth * MillimetresPerFoot, p.Elevation * MillimetresPerFoot,
    p.Angle * 180.0 / Math.PI);

// An optional need left out arrives as the text "none" (row 5b-1432).
var wantedPosition = (position ?? "").Trim();
if (string.Equals(wantedPosition, "none", StringComparison.OrdinalIgnoreCase)) wantedPosition = "";
var yes = (confirm ?? "").Trim();

Document linked = null;
RevitLinkType linkType = null;
ProjectLocation target = null;
var linkName = "";
var instanceName = "";

if (doc.IsFamilyDocument)
    refused = "The document in front is a family. Coordinates are published from a project into a linked "
        + "project. NOTHING WAS PUBLISHED.";
else if (linkInstance == null)
    refused = "No linked model was named. Name the Revit link to publish into, as Manage Links lists it. "
        + "NOTHING WAS PUBLISHED.";

if (refused.Length == 0)
{
    try { instanceName = linkInstance.Name ?? ""; } catch (Exception) { instanceName = ""; }
    try { linkType = doc.GetElement(linkInstance.GetTypeId()) as RevitLinkType; } catch (Exception) { linkType = null; }
    if (linkType != null) { try { linkName = linkType.Name ?? ""; } catch (Exception) { linkName = ""; } }
    try { linked = linkInstance.GetLinkDocument(); } catch (Exception) { linked = null; }
    if (linked == null)
        refused = "'" + instanceName + "' is not loaded, so there is no linked model to record the position in. "
            + "NOTHING WAS PUBLISHED - reload the link first.";
}

var names = new List<string>();
if (refused.Length == 0)
{
    foreach (ProjectLocation candidate in linked.ProjectLocations)
    {
        if (candidate == null) continue;
        var name = "";
        try { name = candidate.Name ?? ""; } catch (Exception) { name = ""; }
        names.Add("\"" + name + "\"");
        if (wantedPosition.Length > 0 && string.Equals(name, wantedPosition, StringComparison.Ordinal)) target = candidate;
    }
    if (wantedPosition.Length == 0)
    {
        try { target = linked.ActiveProjectLocation; } catch (Exception) { target = null; }
    }
    if (target == null)
        refused = (wantedPosition.Length > 0
                ? "The linked model '" + linked.Title + "' has no position named exactly \"" + wantedPosition + "\""
                : "The linked model '" + linked.Title + "' has no current position Revit would name")
            + ". Its positions are " + (names.Count == 0 ? "none" : string.Join(", ", names)) + ". NOTHING WAS PUBLISHED.";
    else
    {
        try { positionName = target.Name ?? ""; } catch (Exception) { positionName = ""; }
    }
}

// What the position should read afterwards: this model's shared coordinates
// at the link's origin, and what it reads now.
ProjectPosition expectedPosition = null;
ProjectPosition beforePosition = null;
if (refused.Length == 0)
{
    try
    {
        var origin = linkInstance.GetTotalTransform().Origin;
        var here = doc.ActiveProjectLocation;
        if (here != null) expectedPosition = here.GetProjectPosition(origin);
    }
    catch (Exception ex) { findings.Add("This model's shared coordinates at the link's origin could not be read - " + ex.Message); }
    try { beforePosition = target.GetProjectPosition(XYZ.Zero); }
    catch (Exception ex) { findings.Add("The linked position before could not be read - " + ex.Message); }
    expected = say(expectedPosition);
    before = say(beforePosition);
}

if (refused.Length == 0 && !(yes.Length > 0
    && (string.Equals(yes, linkName, StringComparison.Ordinal) || string.Equals(yes, instanceName, StringComparison.Ordinal))))
{
    refused = "Publishing coordinates into '" + (linkName.Length > 0 ? linkName : instanceName) + "' records this model's "
        + "shared origin and True North in that linked model as its position \"" + positionName + "\" - now "
        + (before.Length > 0 ? before : "not read") + ", afterwards " + (expected.Length > 0 ? expected : "not worked out")
        + " - and the next save of this model asks whether to write it into the linked file. It is done only with "
        + "confirm repeating the link's name exactly, \"" + (linkName.Length > 0 ? linkName : instanceName) + "\""
        + (instanceName.Length > 0 && instanceName != linkName ? " or \"" + instanceName + "\"" : "")
        + ", after the owner has said yes for it" + (yes.Length == 0 ? "" : " (confirm was \"" + yes + "\")")
        + ". NOTHING WAS PUBLISHED.";
}

if (refused.Length == 0)
{
    try
    {
        doc.PublishCoordinates(new LinkElementId(linkInstance.Id, target.Id));
        published = true;
    }
    catch (Exception ex)
    {
        refused = "Revit refused to publish coordinates into '" + linkName + "' - " + ex.Message + " NOTHING WAS PUBLISHED.";
    }
}

if (published)
{
    try { doc.Regenerate(); } catch (Exception) { }
    try { after = say(target.GetProjectPosition(XYZ.Zero)); }
    catch (Exception ex) { findings.Add("The linked position after could not be read - " + ex.Message); }
    try { waitingToSave = linkType != null && linkType.HasSaveablePositions(); }
    catch (Exception ex) { findings.Add("Whether a position waits to be saved could not be read - " + ex.Message); }

    findings.Add("Published this model's shared coordinates into '" + linkName + "' (" + linked.Title + ") as its position \""
        + positionName + "\".");
    findings.Add("The linked model's internal origin in that position - before: " + (before.Length > 0 ? before : "not read")
        + "; after: " + (after.Length > 0 ? after : "not read") + "; this model's shared coordinates at the link's origin: "
        + (expected.Length > 0 ? expected : "not read") + ".");
    if (after.Length > 0 && after == before)
        findings.Add("THE LINKED POSITION READS THE SAME AS BEFORE - either the two models already agreed, or Revit "
            + "shows the new position only after the link is saved and reloaded. Look before keeping.");
    findings.Add(waitingToSave
        ? "THE LINKED FILE IS NOT SAVED YET. At the next save of this model Revit asks whether to save the new position "
          + "into '" + linkName + "' - that answer is yours. Nothing here saves it."
        : "Revit reports no position waiting to be saved into '" + linkName + "'. Look at Manage Links before keeping.");
}

report = published
    ? "Published coordinates into '" + linkName + "', position \"" + positionName + "\"; after " + (after.Length > 0 ? after : "not read")
      + ", expected " + (expected.Length > 0 ? expected : "not read") + (waitingToSave ? "; the linked file is saved only at your next save, if you choose it" : "") + "."
    : refused;
if (refused.Length > 0) findings.Add(refused);