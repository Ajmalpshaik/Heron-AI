// NOT STANDALONE. Assumes `doc`, `elements`, `mode` and `value` are in
// scope; leaves `edited`, `resultIds`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// THE TERRAIN'S OWN TOOLS, on the toposolids handed in:
//
//   mode "excavate"    value: element ids, comma separated - floors, building
//                      pads, foundations - each cutting its shape out of the
//                      terrain (Revit 2025 on)
//   mode "unexcavate"  value: the same ids - the cut taken away again
//                      (Revit 2025 on)
//   mode "split"       value: "x,y; x,y; ... | ..." in millimetres, closed
//                      loops - each loop's area becomes a toposolid of its own
//   mode "simplify"    value: the share of inner points to keep, 1 to 99 (%)
//   mode "smooth"      value: "on" or "off" - Smooth Surface, which Revit
//                      takes with the document (Revit 2025 on)
//
// Excavation and smoothing arrived in 2025 and are found by name, so the card
// runs on 2024 and says so when a 2025 call is asked of it there. Point
// heights and laying a floor to the terrain are EDIT_SLAB_SHAPE's.
//
// READ BACK: excavation by reading Revit's own check again, a split by the
// new toposolids looked up, a simplify by the shape points counted before
// and after - a toposolid whose count did not change is named as left as it
// was, never counted as edited - smoothing by its setting read again.

const double MmPerFoot = 304.8;
var edited = 0;
var unchanged = 0;
var resultIds = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();
string refusal = null;

var how = (mode ?? "").Trim().ToLowerInvariant();
var text = (value ?? "").Trim();
var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;
if (how != "excavate" && how != "unexcavate" && how != "split" && how != "simplify" && how != "smooth")
    refusal = "\"" + mode + "\" is not excavate, unexcavate, split, simplify or smooth. NOTHING WAS CHANGED.";

// Cutters by id.
var cutters = new List<Element>();
if (refusal == null && (how == "excavate" || how == "unexcavate"))
{
    var wanted = new HashSet<string>(text.Split(',').Select(t => t.Trim()).Where(t => t != ""));
    foreach (var candidate in new FilteredElementCollector(doc).WhereElementIsNotElementType())
        if (wanted.Contains(candidate.Id.ToString()) || wanted.Contains(candidate.UniqueId)) cutters.Add(candidate);
    if (cutters.Count == 0) refusal = "None of the ids \"" + text + "\" is in this model. NOTHING WAS CHANGED.";
}

// Split loops.
var loops = new List<List<XYZ>>();
if (refusal == null && how == "split")
{
    foreach (var loopText in text.Split('|'))
    {
        var loop = new List<XYZ>();
        foreach (var chunk in loopText.Split(';'))
        {
            if (chunk.Trim() == "") continue;
            var parts = chunk.Split(',');
            double x, y;
            if (parts.Length != 2 || !double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
                || !double.TryParse(parts[1].Trim(), numberStyle, culture, out y)) { loops = null; break; }
            loop.Add(new XYZ(x / MmPerFoot, y / MmPerFoot, 0));
        }
        if (loops == null) break;
        if (loop.Count > 0) loops.Add(loop);
    }
    if (loops == null || loops.Count == 0 || loops.Any(l => l.Count < 3))
        refusal = "Each split loop is three x,y points or more in millimetres, loops separated by |. NOTHING WAS CHANGED.";
}

double share = 0;
if (refusal == null && how == "simplify"
    && (!double.TryParse(text.TrimEnd('%'), numberStyle, culture, out share) || share < 1 || share > 99))
    refusal = "For simplify, give the share of inner points to keep, 1 to 99. NOTHING WAS CHANGED.";
if (refusal == null && how == "smooth" && text.ToLowerInvariant() != "on" && text.ToLowerInvariant() != "off")
    refusal = "For smooth, give on or off. NOTHING WAS CHANGED.";

if (refusal == null)
{
    foreach (var element in elements)
    {
        var topo = element as Toposolid;
        if (topo == null) { if (element != null) lines.Add(element.Id + ": not a toposolid."); continue; }
        try
        {
            if (how == "excavate" || how == "unexcavate")
            {
                var cut = topo.GetType().GetMethod(how == "excavate" ? "ExcavateBy" : "RemoveExcavationBy", new[] { typeof(ElementId) });
                var check = topo.GetType().GetMethod("CanBeExcavatedBy", new[] { typeof(ElementId) });
                if (cut == null) { lines.Add(topo.Id + ": this Revit has no toposolid excavation - Revit 2025 brought it."); continue; }
                var done = new List<string>();
                foreach (var cutter in cutters)
                {
                    if (how == "excavate" && check != null && !(bool)check.Invoke(topo, new object[] { cutter.Id }))
                    { done.Add(cutter.Id + " cannot cut it"); continue; }
                    cut.Invoke(topo, new object[] { cutter.Id });
                    done.Add((how == "excavate" ? "cut by " : "uncut by ") + cutter.Id);
                }
                doc.Regenerate();
                edited++;
                resultIds.Add(topo.Id);
                lines.Add("toposolid " + topo.Id + ": " + string.Join(", ", done) + ".");
            }
            else if (how == "split")
            {
                var z = 0.0;
                var sketch = doc.GetElement(topo.SketchId) as Sketch;
                if (sketch != null && sketch.SketchPlane != null) z = sketch.SketchPlane.GetPlane().Origin.Z;
                var curveLoops = new List<CurveLoop>();
                foreach (var loop in loops)
                {
                    var curveLoop = new CurveLoop();
                    for (var i = 0; i < loop.Count; i++)
                    {
                        var a = loop[i];
                        var b = loop[(i + 1) % loop.Count];
                        curveLoop.Append(Line.CreateBound(new XYZ(a.X, a.Y, z), new XYZ(b.X, b.Y, z)));
                    }
                    curveLoops.Add(curveLoop);
                }
                var made = topo.Split(curveLoops);
                doc.Regenerate();
                var found = made.Where(id => doc.GetElement(id) != null).ToList();
                resultIds.AddRange(found);
                edited++;
                lines.Add("toposolid " + topo.Id + ": split into " + found.Count + " new toposolid(s), read back: " + string.Join(", ", found.Select(i => i.ToString())) + ".");
            }
            else if (how == "simplify")
            {
                var before = topo.GetSlabShapeEditor().SlabShapeVertices.Size;
                topo.Simplify(share / 100.0);
                doc.Regenerate();
                var after = topo.GetSlabShapeEditor().SlabShapeVertices.Size;
                // Only a toposolid whose shape points changed is edited (row
                // 5b-1331): on Revit 2024.3 one of ten read 2399 -> 1293 and
                // nine read 0 -> 0, and all ten were counted.
                if (after == before)
                {
                    unchanged++;
                    lines.Add("toposolid " + topo.Id + ": " + (before == 0
                        ? "nothing to simplify - 0 shape points, left as it was."
                        : "unchanged - " + before + " shape point(s) before and after."));
                    continue;
                }
                edited++;
                resultIds.Add(topo.Id);
                lines.Add("toposolid " + topo.Id + ": " + before + " shape point(s) before, " + after + " after.");
            }
            else
            {
                // Smooth Surface is taken by Revit with the document, so it
                // is set once, on the first toposolid's call, and read back.
                var set = typeof(Toposolid).GetMethod("SetSmoothedSurface", new[] { typeof(Document), typeof(bool) });
                var read = typeof(Toposolid).GetMethod("IsSmoothedSurfaceEnabled", new[] { typeof(Document) });
                if (set == null) { lines.Add(topo.Id + ": this Revit has no Smooth Surface setting in code - Revit 2025 brought it."); continue; }
                var on = text.ToLowerInvariant() == "on";
                if (edited == 0)
                {
                    set.Invoke(set.IsStatic ? null : topo, new object[] { doc, on });
                    doc.Regenerate();
                }
                var now = read != null ? read.Invoke(read.IsStatic ? null : topo, new object[] { doc }) : null;
                edited++;
                resultIds.Add(topo.Id);
                lines.Add("toposolid " + topo.Id + ": Smooth Surface asked " + (on ? "on" : "off") + (now != null ? ", reads " + now : "") + ".");
            }
        }
        catch (System.Reflection.TargetInvocationException wrapped)
        {
            lines.Add("toposolid " + topo.Id + ": Revit refused - " + (wrapped.InnerException ?? wrapped).Message);
        }
        catch (Exception ex) { lines.Add("toposolid " + topo.Id + ": Revit refused - " + ex.Message); }
    }
    findings.Add(edited + " toposolid(s) edited (" + how + ") and read back."
        + (unchanged > 0 ? " " + unchanged + " toposolid(s) unchanged - the shape point count did not change." : ""));
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
