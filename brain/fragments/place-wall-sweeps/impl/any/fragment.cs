// NOT STANDALONE. Assumes `doc`, `elements`, `sweepTypeName`, `side`,
// `heightMm` and `reveal` are in scope; leaves `made`, `sweepIds`, `report`
// and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A WALL SWEEP - or a reveal - along each wall handed in: skirting along the
// base, a cornice at the top, a dado rail at 900 mm. The sweep runs the wall's
// whole length and Revit cuts it at doors on its own, as the ribbon's Sweep
// does.
//
//   side       "interior" (the default) or "exterior" - the wall's own sides
//   heightMm   from the wall's base to the sweep's insertion
//   reveal     true makes a reveal (a cut) instead of a sweep (an added profile)
//
// The type is found by name among the project's wall sweep (or reveal) types;
// a name that is not there refuses the call, naming the ones that are.
//
// For skirting in rooms, hand in the room-bounding walls of those rooms; which
// side faces the room is the wall's interior or exterior as it was drawn, and
// is said in the answer rather than guessed.
//
// READ BACK: each new sweep is looked up and its host read; only one hosted by
// its wall counts.

const double MmPerFoot = 304.8;
var made = 0;
var sweepIds = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();

var category = reveal ? BuiltInCategory.OST_Reveals : BuiltInCategory.OST_Cornices;
var types = new FilteredElementCollector(doc).OfCategory(category).WhereElementIsElementType().ToElements();
Element type = null;
var wantedType = (sweepTypeName ?? "").Trim();
foreach (var candidate in types)
    if (string.Equals(candidate.Name, wantedType, StringComparison.OrdinalIgnoreCase)) { type = candidate; break; }

var sideText = (side ?? "").Trim().ToLowerInvariant();
string refusal = null;
if (type == null)
    refusal = "No wall " + (reveal ? "reveal" : "sweep") + " type is named \"" + wantedType + "\". The project has: "
        + (types.Count == 0 ? "none" : string.Join(", ", types.Select(t => t.Name))) + ". NOTHING WAS MADE.";
else if (sideText != "" && sideText != "interior" && sideText != "exterior")
    refusal = "\"" + side + "\" is not interior or exterior. NOTHING WAS MADE.";
else if (heightMm < 0)
    refusal = "A height below the wall's base means nothing here. NOTHING WAS MADE.";

if (refusal == null)
{
    foreach (var element in elements)
    {
        var wall = element as Wall;
        if (wall == null) { if (element != null) lines.Add(element.Id + ": not a wall."); continue; }
        try
        {
            var info = new WallSweepInfo(reveal ? WallSweepType.Reveal : WallSweepType.Sweep, false);
            info.WallSide = sideText == "exterior" ? WallSide.Exterior : WallSide.Interior;
            info.Distance = heightMm / MmPerFoot;
            var sweep = WallSweep.Create(wall, type.Id, info);
            doc.Regenerate();
            if (sweep != null && sweep.IsValidObject && sweep.GetHostIds().Contains(wall.Id))
            {
                made++;
                sweepIds.Add(sweep.Id);
            }
            else lines.Add("wall " + wall.Id + ": a sweep was asked for and does not read back on it.");
        }
        catch (Exception ex) { lines.Add("wall " + wall.Id + ": Revit refused - " + ex.Message); }
    }
    findings.Add(made + " " + (reveal ? "reveal(s)" : "sweep(s)") + " of \"" + type.Name + "\" made on the "
        + (sideText == "exterior" ? "exterior" : "interior") + " side at " + heightMm + " mm and read back on their walls.");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
