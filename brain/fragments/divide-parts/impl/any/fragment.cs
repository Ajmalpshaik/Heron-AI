// NOT STANDALONE. Assumes `doc`, `elements` and `cutterNames` are in scope;
// leaves `divided`, `partCountAfter`, `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// PARTS DIVIDED WHERE GRIDS, LEVELS OR REFERENCE PLANES CROSS THEM - Divide
// Parts with Intersecting References. `cutterNames` names them, "A, B, 3"
// or "Level 2"; each name must find exactly one grid, level or reference
// plane. Parts Revit will not divide are named.
//
// READ BACK: the parts made from the original elements are counted again
// afterwards.
//
// VERSION 2 (2026-11-11, row 5b-1418) - THE DIVISION IS GIVEN A SKETCH PLANE.
// Revit keeps a sketch with every division, also one cut by datums alone, and
// Revit 2024 refused the empty id version 1 passed - "The element id should
// refer to a valid SketchPlane" - for a level and a grid alike. A sketch plane
// is now made for the call, on the largest flat face of the first part: where
// a division is sketched by hand, and square to any datum that crosses the
// part. A part showing no flat face gets a horizontal one. Nothing is drawn
// on it - the named datums do the cutting. It is made in a step of its own,
// taken back whole when Revit refuses or hands back no division, so a refusal
// leaves no sketch plane behind, and an answer with no division in it is
// refused rather than counted.
// WHICH PLANE REVIT ACCEPTS IS NOT MEASURED: compiled only, not run in Revit.

var divided = 0;
var partCountAfter = 0;
var lines = new List<string>();
var findings = new List<string>();

var cutters = new List<ElementId>();
string refusal = null;
foreach (var raw in (cutterNames ?? "").Split(','))
{
    var name = raw.Trim();
    if (name == "") continue;
    var found = new List<Element>();
    foreach (var type in new[] { typeof(Grid), typeof(Level), typeof(ReferencePlane) })
        foreach (var e in new FilteredElementCollector(doc).OfClass(type))
            if (string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase)) found.Add(e);
    if (found.Count == 1) cutters.Add(found[0].Id);
    else { refusal = (found.Count == 0 ? "No grid, level or reference plane is named \"" : "More than one datum is named \"") + name + "\". NOTHING WAS DIVIDED."; break; }
}
var parts = new List<ElementId>();
foreach (var element in elements) if (element is Part) parts.Add(element.Id); else if (element != null) lines.Add(element.Id + ": not a part.");

if (refusal == null && cutters.Count == 0) refusal = "No grid, level or reference plane was named. NOTHING WAS DIVIDED.";
else if (refusal == null && parts.Count == 0) refusal = "No parts were handed in. NOTHING WAS DIVIDED.";

if (refusal == null)
{
    bool ok = false;
    try { ok = PartUtils.ArePartsValidForDivide(doc, parts); } catch { }
    if (!ok) refusal = "Revit will not divide these parts - a part already divided by a sketch, or one in another assembly. NOTHING WAS DIVIDED.";
}
if (refusal == null)
{
    var sources = new HashSet<ElementId>();
    foreach (var id in parts)
    {
        var part = doc.GetElement(id) as Part;
        try { foreach (var s in part.GetSourceElementIds()) sources.Add(s.HostElementId); } catch { }
    }
    // Where the division's sketch lies (version 2) - see the header.
    var sketchOn = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
    try
    {
        var largest = 0.0;
        var shape = doc.GetElement(parts[0]).get_Geometry(new Options());
        if (shape != null)
            foreach (var piece in shape)
            {
                var solid = piece as Solid;
                if (solid == null) continue;
                foreach (Face face in solid.Faces)
                {
                    var flat = face as PlanarFace;
                    if (flat == null || flat.Area <= largest) continue;
                    largest = flat.Area;
                    sketchOn = Plane.CreateByNormalAndOrigin(flat.FaceNormal, flat.Origin);
                }
            }
    }
    catch { }
    var step = new SubTransaction(doc);
    try
    {
        step.Start();
        var sketch = SketchPlane.Create(doc, sketchOn);
        doc.Regenerate();
        var maker = PartUtils.DivideParts(doc, parts, cutters, new List<Curve>(), sketch.Id);
        if (maker == null)
        {
            step.RollBack();
            refusal = "Revit divided none of these parts at the datum(s) named - check that one of them crosses the parts. NOTHING WAS DIVIDED.";
        }
        else
        {
            step.Commit();
            divided = parts.Count;
        }
    }
    catch (Exception ex)
    {
        try { if (step.HasStarted() && !step.HasEnded()) step.RollBack(); } catch { }
        refusal = "Revit refused the division: " + ex.Message + ". NOTHING WAS DIVIDED.";
    }
    doc.Regenerate();
    foreach (var s in sources)
    {
        try { partCountAfter += PartUtils.GetAssociatedParts(doc, s, false, true).Count; } catch { }
    }
    if (refusal == null)
        findings.Add(divided + " part(s) divided at " + cutters.Count + " datum(s); their elements now carry " + partCountAfter + " part(s), read back.");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
