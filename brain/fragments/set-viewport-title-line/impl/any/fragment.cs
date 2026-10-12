// NOT STANDALONE. Assumes `doc`, `sheetNumbers` and `lengthMm` are in scope;
// leaves `changed`, `changedIds`, `alreadyThat`, `report` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// THE LENGTH OF EACH VIEWPORT'S TITLE LINE, on the sheets named or on every
// sheet. Revit 2022 opened the title line's length to code; before that it is
// dragged by hand, and this card declares 2022 on only.
//
//   lengthMm > 0   every title line that long, in millimetres on the sheet
//   lengthMm 0     each line fitted to its own title: the width of the title's
//                  outline as Revit reports it. Whether that outline already
//                  includes the line is what the proof records.
//
// A viewport whose type shows no title or no extension line still has a
// length stored; it is set, and the answer says the line does not show.
//
// READ BACK: each length is read off the viewport after a regeneration, and
// only one that reads as asked counts.

const double MmPerFoot = 304.8;
var changed = 0;
var changedIds = new List<ElementId>();
var alreadyThat = 0;
var lines = new List<string>();
var findings = new List<string>();

var wantedSheets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var part in (sheetNumbers ?? "").Split(','))
    if (part.Trim() != "") wantedSheets.Add(part.Trim());

var sheetsFound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var asked = new List<Tuple<Viewport, double>>();

if (lengthMm < 0) lines.Add("A length below 0 mm means nothing. NOTHING WAS CHANGED.");
else
{
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)))
    {
        var sheet = element as ViewSheet;
        if (sheet == null || sheet.IsPlaceholder) continue;
        if (wantedSheets.Count > 0 && !wantedSheets.Contains(sheet.SheetNumber)) continue;
        sheetsFound.Add(sheet.SheetNumber);

        foreach (var id in sheet.GetAllViewports())
        {
            var viewport = doc.GetElement(id) as Viewport;
            if (viewport == null) continue;
            double target;
            if (lengthMm > 0) target = lengthMm / MmPerFoot;
            else
            {
                var outline = viewport.GetLabelOutline();
                target = outline == null ? 0 : outline.MaximumPoint.X - outline.MinimumPoint.X;
                if (target <= 0)
                {
                    lines.Add(sheet.SheetNumber + ": viewport " + viewport.Id + " shows no title to fit to - left as it is.");
                    continue;
                }
            }
            if (Math.Abs(viewport.LabelLineLength - target) < 0.1 / MmPerFoot) { alreadyThat++; continue; }
            try
            {
                viewport.LabelLineLength = target;
                asked.Add(Tuple.Create(viewport, target));
            }
            catch (Exception ex)
            {
                lines.Add(sheet.SheetNumber + ": viewport " + viewport.Id + " - Revit refused: " + ex.Message);
            }
        }
    }

    doc.Regenerate();
    foreach (var pair in asked)
    {
        var now = pair.Item1.LabelLineLength;
        if (Math.Abs(now - pair.Item2) < 0.1 / MmPerFoot)
        {
            changed++;
            changedIds.Add(pair.Item1.Id);
        }
        else lines.Add("viewport " + pair.Item1.Id + ": asked for " + Math.Round(pair.Item2 * MmPerFoot, 1)
            + " mm and reads back " + Math.Round(now * MmPerFoot, 1) + " mm.");
    }

    foreach (var wanted in wantedSheets)
        if (!sheetsFound.Contains(wanted)) lines.Add("No sheet is numbered " + wanted + ".");

    findings.Add(changed + " title line(s) set and read back" + (lengthMm > 0 ? " at " + lengthMm + " mm" : ", each fitted to its title")
        + " on " + sheetsFound.Count + " sheet(s); " + alreadyThat + " already that length.");
}

var report = string.Join(" | ", lines);
findings.AddRange(lines);
