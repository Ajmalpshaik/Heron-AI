// NOT STANDALONE. Assumes `doc`, `railingTypeName`, `duplicateAs`,
// `topRailTypeName`, `topRailHeightMm`, `handrailTypeName`,
// `handrailHeightMm` and `handrailSide` are in scope; leaves `railingType`,
// `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// WHAT A RAILING TYPE IS MADE OF - the top rail and the main handrail, as
// Type Properties sets them:
//
//   railingTypeName   the type to change
//   duplicateAs       a new name: the type is copied first and the copy is
//                     changed; EMPTY changes the type itself
//   topRailTypeName   a top rail type by name; EMPTY keeps it
//   topRailHeightMm   the top rail's height; 0 keeps it
//   handrailTypeName  a handrail type by name; EMPTY keeps it
//   handrailHeightMm  the handrail's height; 0 keeps it. REVIT KEEPS THIS
//                     ON THE HANDRAIL TYPE, not the railing type, so it
//                     changes every railing type that shares that handrail
//                     type - the report counts them
//   handrailSide      "left", "right", "both" or "none"; EMPTY keeps it
//
// Changing a type changes every railing of it - which is why a copy can be
// made first. Balusters and posts stay as they are; the pattern editor has
// no settable equivalent worth guessing at.
//
// READ BACK: every value asked is read again from the type after a
// regeneration.

const double MmPerFoot = 304.8;
RailingType railingType = null;
var lines = new List<string>();
var findings = new List<string>();
string refusal = null;

Func<Type, string, Element> byName = (kind, name) =>
{
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(kind))
        if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)) return candidate;
    return null;
};
Func<Type, string> namesOf = kind =>
    string.Join(", ", new FilteredElementCollector(doc).OfClass(kind).Select(e => e.Name).ToArray());

var start = byName(typeof(RailingType), (railingTypeName ?? "").Trim()) as RailingType;
var topWanted = (topRailTypeName ?? "").Trim();
var handWanted = (handrailTypeName ?? "").Trim();
var side = (handrailSide ?? "").Trim().ToLowerInvariant();
var topRail = topWanted == "" ? null : byName(typeof(TopRailType), topWanted);
var handrail = handWanted == "" ? null : byName(typeof(HandRailType), handWanted);
var newName = (duplicateAs ?? "").Trim();

if (start == null) refusal = "No railing type is named \"" + railingTypeName + "\" - there are: " + namesOf(typeof(RailingType)) + ". NOTHING WAS CHANGED.";
else if (topWanted != "" && topRail == null) refusal = "No top rail type is named \"" + topWanted + "\" - there are: " + namesOf(typeof(TopRailType)) + ". NOTHING WAS CHANGED.";
else if (handWanted != "" && handrail == null) refusal = "No handrail type is named \"" + handWanted + "\" - there are: " + namesOf(typeof(HandRailType)) + ". NOTHING WAS CHANGED.";
else if (side != "" && side != "left" && side != "right" && side != "both" && side != "none") refusal = "\"" + handrailSide + "\" is not left, right, both or none. NOTHING WAS CHANGED.";
else if (topRailHeightMm < 0 || handrailHeightMm < 0) refusal = "A height cannot be below zero. NOTHING WAS CHANGED.";
else if (newName != "" && byName(typeof(RailingType), newName) != null) refusal = "A railing type is already named \"" + newName + "\". NOTHING WAS CHANGED.";
else if (newName == "" && topRail == null && handrail == null && side == "" && topRailHeightMm == 0 && handrailHeightMm == 0)
    refusal = "Nothing was asked - give a rail type, a height or a handrail side. NOTHING WAS CHANGED.";

if (refusal == null)
{
    try
    {
        railingType = newName == "" ? start : start.Duplicate(newName) as RailingType;
        var position = side == "left" ? HandRailPosition.Left : side == "right" ? HandRailPosition.Right
            : side == "both" ? HandRailPosition.LeftAndRight : HandRailPosition.None;
        if (topRail != null) railingType.TopRailType = topRail.Id;
        if (topRailHeightMm > 0) railingType.TopRailHeight = topRailHeightMm / MmPerFoot;
        if (handrail != null) railingType.PrimaryHandrailType = handrail.Id;
        if (side != "") railingType.PrimaryHandRailPosition = position;
        var handType = doc.GetElement(railingType.PrimaryHandrailType) as HandRailType;
        var sharing = 0;
        if (handrailHeightMm > 0)
        {
            if (handType == null) throw new InvalidOperationException("the railing type has no handrail to set a height on");
            handType.Height = handrailHeightMm / MmPerFoot;
            sharing = new FilteredElementCollector(doc).OfClass(typeof(RailingType)).Cast<RailingType>()
                .Count(t => t.Id != railingType.Id && t.PrimaryHandrailType == handType.Id);
        }
        doc.Regenerate();

        // Read back.
        var wrong = new List<string>();
        if (topRail != null && railingType.TopRailType != topRail.Id) wrong.Add("the top rail reads another type");
        if (topRailHeightMm > 0 && Math.Abs(railingType.TopRailHeight * MmPerFoot - topRailHeightMm) > 0.5) wrong.Add("the top rail height reads " + Math.Round(railingType.TopRailHeight * MmPerFoot) + " mm");
        if (handrail != null && railingType.PrimaryHandrailType != handrail.Id) wrong.Add("the handrail reads another type");
        if (side != "" && railingType.PrimaryHandRailPosition != position) wrong.Add("the handrail side reads " + railingType.PrimaryHandRailPosition);
        if (handrailHeightMm > 0 && Math.Abs(railingType.PrimaryHandrailHeight * MmPerFoot - handrailHeightMm) > 0.5) wrong.Add("the handrail height reads " + Math.Round(railingType.PrimaryHandrailHeight * MmPerFoot) + " mm");

        var topName = doc.GetElement(railingType.TopRailType) != null ? doc.GetElement(railingType.TopRailType).Name : "none";
        var handName = doc.GetElement(railingType.PrimaryHandrailType) != null ? doc.GetElement(railingType.PrimaryHandrailType).Name : "none";
        lines.Add((newName != "" ? "Copied \"" + start.Name + "\" as \"" + railingType.Name + "\"; " : "\"" + railingType.Name + "\": ")
            + "top rail " + topName + " at " + Math.Round(railingType.TopRailHeight * MmPerFoot) + " mm, handrail " + handName
            + " " + railingType.PrimaryHandRailPosition + " at " + Math.Round(railingType.PrimaryHandrailHeight * MmPerFoot) + " mm"
            + (wrong.Count > 0 ? " - NOT AS ASKED: " + string.Join("; ", wrong) : ", read back as asked") + ".");
        if (sharing > 0) lines.Add("The handrail height is the handrail type \"" + handType.Name + "\"'s, and " + sharing + " other railing type(s) share it and changed with it.");
        var users = new FilteredElementCollector(doc).OfClass(typeof(Railing)).Count(e => e.GetTypeId() == railingType.Id);
        lines.Add(users + " railing(s) are of this type and changed with it.");
    }
    catch (Exception ex)
    {
        refusal = "Revit refused - " + ex.Message + ". Anything set before the refusal is in this call's one undo entry.";
        railingType = null;
    }
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
