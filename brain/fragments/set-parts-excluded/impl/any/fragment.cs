// NOT STANDALONE. Assumes `doc`, `elements` and `include` are in scope; leaves
// `changed`, `alreadyThat`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// PARTS EXCLUDED FROM THE MODEL - or brought back with include true. An
// excluded part stays in the file and drops out of views and schedules, which
// is how a finish layer is taken out of one area.
//
// READ BACK: each part's Excluded state is read again.

var changed = 0;
var alreadyThat = 0;
var lines = new List<string>();
var findings = new List<string>();
var wantExcluded = !include;
var asked = new List<Part>();

foreach (var element in elements)
{
    var part = element as Part;
    if (part == null) { if (element != null) lines.Add(element.Id + ": not a part."); continue; }
    if (part.Excluded == wantExcluded) { alreadyThat++; continue; }
    try { part.Excluded = wantExcluded; asked.Add(part); }
    catch (Exception ex) { lines.Add("part " + part.Id + ": Revit refused - " + ex.Message); }
}
doc.Regenerate();
foreach (var part in asked)
{
    if (part.Excluded == wantExcluded) changed++;
    else lines.Add("part " + part.Id + ": asked, and it does not read back so.");
}

var report = string.Join(" | ", lines);
findings.Add(changed + " part(s) " + (wantExcluded ? "excluded" : "included") + " and read back; " + alreadyThat + " already so.");
findings.AddRange(lines);
