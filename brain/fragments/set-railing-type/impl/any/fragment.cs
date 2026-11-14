// NOT STANDALONE. Assumes `doc`, `railingTypeName`, `duplicateAs`,
// `topRailTypeName`, `topRailHeightMm`, `handrailTypeName`,
// `handrailHeightMm` and `handrailSide` are in scope; leaves `railingType`,
// `changedTypeId`, `report` and `findings` behind.
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
//
// VERSION 2 (2026-11-11, row 5b-1415) - THE HANDRAIL IS READ ONLY WHEN THE
// TYPE HAS ONE. Revit throws "The rail has no primary hand rail" on the
// primary handrail's height for a type whose Handrail 1 is None - how most
// railing types ship - and version 1 read its side and height into the report
// unasked, so every such type was refused whatever was asked. Now the type's
// handrail is looked up first: with none the report says "handrail none" and
// neither is read. With one, the side and the height are read each on its
// own: a read Revit still refuses is said in the report for that one alone,
// and counts as NOT AS ASKED only when that one was asked for.
//
// VERSION 3 (2026-11-13, row 5b-1633) - A RESULT A RUNNER CAN READ. The only
// result was `railingType`, a RailingType object, which no runner can read as
// found or not found, so a run that did exactly what it was asked was
// POSITIVE UNREADABLE. `changedTypeId` is the id of the type changed - the
// copy, or the type named - and ONLY when every value asked READ BACK AS
// ASKED; InvalidElementId when refused, or when anything read back otherwise
// (the report then says NOT AS ASKED). The same shape as SET_TEXT_TYPE's and
// SET_DIMENSION_TYPE's `changedTypeId`. Nothing it does to the model changed.

const double MmPerFoot = 304.8;
RailingType railingType = null;
var changedTypeId = ElementId.InvalidElementId;
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

        // The handrail as the type now holds it (version 2) - see the header.
        var hasHandrail = doc.GetElement(railingType.PrimaryHandrailType) is HandRailType;
        var sideNow = HandRailPosition.None;
        var handHeightNowMm = 0.0;
        string sideUnread = null, heightUnread = null;
        if (hasHandrail)
        {
            try { sideNow = railingType.PrimaryHandRailPosition; } catch (Exception ex) { sideUnread = ex.Message; }
            try { handHeightNowMm = railingType.PrimaryHandrailHeight * MmPerFoot; } catch (Exception ex) { heightUnread = ex.Message; }
        }

        // Read back.
        var wrong = new List<string>();
        if (topRail != null && railingType.TopRailType != topRail.Id) wrong.Add("the top rail reads another type");
        if (topRailHeightMm > 0 && Math.Abs(railingType.TopRailHeight * MmPerFoot - topRailHeightMm) > 0.5) wrong.Add("the top rail height reads " + Math.Round(railingType.TopRailHeight * MmPerFoot) + " mm");
        if (handrail != null && railingType.PrimaryHandrailType != handrail.Id) wrong.Add("the handrail reads another type");
        if (side != "" && sideUnread != null) wrong.Add("the handrail side could not be read back");
        else if (side != "" && sideNow != position) wrong.Add(hasHandrail ? "the handrail side reads " + sideNow : "the type has no handrail to put on a side");
        if (handrailHeightMm > 0 && heightUnread != null) wrong.Add("the handrail height could not be read back");
        else if (handrailHeightMm > 0 && Math.Abs(handHeightNowMm - handrailHeightMm) > 0.5) wrong.Add("the handrail height reads " + Math.Round(handHeightNowMm) + " mm");

        var topName = doc.GetElement(railingType.TopRailType) != null ? doc.GetElement(railingType.TopRailType).Name : "none";
        var handName = doc.GetElement(railingType.PrimaryHandrailType) != null ? doc.GetElement(railingType.PrimaryHandrailType).Name : "none";
        lines.Add((newName != "" ? "Copied \"" + start.Name + "\" as \"" + railingType.Name + "\"; " : "\"" + railingType.Name + "\": ")
            + "top rail " + topName + " at " + Math.Round(railingType.TopRailHeight * MmPerFoot) + " mm, handrail " + handName
            + (!hasHandrail ? "" : " " + (sideUnread != null ? "(Revit would not give its side - " + sideUnread + ")" : sideNow.ToString())
                + " at " + (heightUnread != null ? "(Revit would not give its height - " + heightUnread + ")" : Math.Round(handHeightNowMm) + " mm"))
            + (wrong.Count > 0 ? " - NOT AS ASKED: " + string.Join("; ", wrong) : ", read back as asked") + ".");
        if (sharing > 0) lines.Add("The handrail height is the handrail type \"" + handType.Name + "\"'s, and " + sharing + " other railing type(s) share it and changed with it.");
        var users = new FilteredElementCollector(doc).OfClass(typeof(Railing)).Count(e => e.GetTypeId() == railingType.Id);
        lines.Add(users + " railing(s) are of this type and changed with it.");
        if (wrong.Count == 0) changedTypeId = railingType.Id;
    }
    catch (Exception ex)
    {
        refusal = "Revit refused - " + ex.Message + ". Anything set before the refusal is in this call's one undo entry.";
        railingType = null;
        changedTypeId = ElementId.InvalidElementId;
    }
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
