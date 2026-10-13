// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `coped`,
// `copedIds`, `alreadyCoped`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// THE COPING CUT WHERE A BEAM MEETS A COLUMN OR ANOTHER BEAM - Revit's own
// Cope tool, for many beams at once. Each beam handed in is coped by every
// structural column and every other structural framing member whose box it
// runs into; the beam is the one cut, never the column.
//
// A cut already there is counted, not made twice. READ BACK: each beam's coping
// list is read again, and only a cutter now on it counts.

var coped = 0;
var copedIds = new List<ElementId>();
var alreadyCoped = 0;
var lines = new List<string>();
var findings = new List<string>();

var framingCategory = new ElementId(BuiltInCategory.OST_StructuralFraming);
var columnCategory = new ElementId(BuiltInCategory.OST_StructuralColumns);

foreach (var element in elements)
{
    var beam = element as FamilyInstance;
    if (beam == null || beam.Category == null || beam.Category.Id != framingCategory)
    {
        if (element != null) lines.Add(element.Id + ": not structural framing.");
        continue;
    }
    var box = beam.get_BoundingBox(null);
    if (box == null) continue;
    var filter = new BoundingBoxIntersectsFilter(new Outline(box.Min, box.Max));
    var before = new HashSet<ElementId>(beam.GetCopingIds());
    var asked = new List<ElementId>();
    foreach (var candidate in new FilteredElementCollector(doc).WherePasses(filter).OfClass(typeof(FamilyInstance)))
    {
        if (candidate.Id == beam.Id || candidate.Category == null) continue;
        if (candidate.Category.Id != columnCategory && candidate.Category.Id != framingCategory) continue;
        if (before.Contains(candidate.Id)) { alreadyCoped++; continue; }
        try
        {
            if (beam.AddCoping((FamilyInstance)candidate)) asked.Add(candidate.Id);
        }
        catch (Exception ex) { lines.Add("beam " + beam.Id + " by " + candidate.Id + ": Revit refused - " + ex.Message); }
    }
    doc.Regenerate();
    var after = new HashSet<ElementId>(beam.GetCopingIds());
    var took = asked.Count(id => after.Contains(id));
    coped += took;
    if (took > 0)
    {
        copedIds.Add(beam.Id);
        lines.Add("beam " + beam.Id + ": coped by " + took + " member(s).");
    }
}

var report = string.Join(" | ", lines);
findings.Add(coped + " coping cut(s) made and read back on " + copedIds.Count + " beam(s); " + alreadyCoped + " already there.");
findings.AddRange(lines);
