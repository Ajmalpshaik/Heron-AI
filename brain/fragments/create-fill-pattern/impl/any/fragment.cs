// NOT STANDALONE. Assumes `doc`, `patternName`, `target`, `angleDegrees`,
// `spacingMm`, `crossSpacingMm` and `replaceExisting` are in scope; leaves
// `pattern`, `madeNew`, `readBack`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A FILL PATTERN OF PARALLEL LINES, OR A CROSSHATCH - the two Revit's own Fill
// Patterns dialog makes without a .pat file:
//
//   crossSpacingMm 0     lines at angleDegrees, spacingMm apart
//   crossSpacingMm > 0   a second set at right angles, crossSpacingMm apart
//
// DRAFTING OR MODEL. A drafting pattern's spacing is on paper and scales with
// the view; a model pattern's is real size on the element - a 600 mm tile
// grid. `target` says which, and a name is unique within its target only.
//
// AN EXISTING NAME IS REFUSED unless replaceExisting is true, and then the
// pattern is replaced in place, so every region and material using it
// follows.
//
// READ BACK: the stored grids - angle and spacing of each - are read off the
// pattern and printed.

const double MmPerFoot = 304.8;
var findings = new List<string>();
string refused = null;
FillPatternElement pattern = null;
var madeNew = false;
var readBack = "";

var name = (patternName ?? "").Trim();
var kind = (target ?? "").Trim().ToLowerInvariant();
var fillTarget = FillPatternTarget.Drafting;

if (name == "") refused = "No pattern name was given, so NOTHING WAS MADE.";
else if (kind == "model") fillTarget = FillPatternTarget.Model;
else if (kind != "drafting" && kind != "")
    refused = "\"" + target + "\" is not drafting or model, so NOTHING WAS MADE.";

if (refused == null && spacingMm <= 0)
    refused = "The spacing must be more than 0 mm, so NOTHING WAS MADE.";
if (refused == null && crossSpacingMm < 0)
    refused = "The cross spacing is 0 for parallel lines or more than 0 for a crosshatch. NOTHING WAS MADE.";

if (refused == null)
{
    var angle = angleDegrees * Math.PI / 180.0;
    var fill = crossSpacingMm > 0
        ? new FillPattern(name, fillTarget, FillPatternHostOrientation.ToView, angle,
                          spacingMm / MmPerFoot, crossSpacingMm / MmPerFoot)
        : new FillPattern(name, fillTarget, FillPatternHostOrientation.ToView, angle,
                          spacingMm / MmPerFoot);

    var existing = FillPatternElement.GetFillPatternElementByName(doc, fillTarget, name);
    try
    {
        if (existing != null)
        {
            if (!replaceExisting)
                refused = "A " + (fillTarget == FillPatternTarget.Model ? "model" : "drafting")
                    + " fill pattern named \"" + name + "\" already exists, so NOTHING WAS MADE. Say "
                    + "replaceExisting to replace it in place - every region and material using it follows.";
            else
            {
                existing.SetFillPattern(fill);
                pattern = existing;
            }
        }
        else
        {
            pattern = FillPatternElement.Create(doc, fill);
            madeNew = true;
        }
    }
    catch (Exception ex)
    {
        refused = "Revit refused the pattern: " + ex.Message + ". NOTHING WAS MADE.";
        pattern = null;
    }
}

if (pattern != null)
{
    doc.Regenerate();
    var grids = new List<string>();
    foreach (var grid in pattern.GetFillPattern().GetFillGrids())
    {
        grids.Add(Math.Round(grid.Angle * 180.0 / Math.PI, 3).ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " degrees, " + Math.Round(grid.Offset * MmPerFoot, 3).ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " mm apart");
    }
    readBack = string.Join("; ", grids);
    findings.Add((madeNew ? "Made" : "Replaced") + " " + (fillTarget == FillPatternTarget.Model ? "model" : "drafting")
        + " fill pattern \"" + name + "\": " + readBack + " (read back).");
}
if (refused != null) findings.Add(refused);
