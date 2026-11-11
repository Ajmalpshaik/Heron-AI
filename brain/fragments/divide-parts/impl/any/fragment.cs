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
// refer to a valid SketchPlane" - for a level and a grid alike. A HORIZONTAL
// sketch plane through the origin is now made for the call. Revit's API
// reference calls that argument the sketch plane "for the curves that divide
// the elements", and no curve is handed in, so the plane carries nothing: the
// named datums do the cutting, each where it stands. ONE plane for every part,
// whatever way a part faces - never a plane taken from one part, which would
// govern the others if Revit did read it.
//
// It is made in a step of its own, taken back whole when Revit refuses or
// hands back no division, and then looked for again: a refusal says so if the
// plane is still there. An answer with no division in it is refused rather
// than counted. A division that is made KEEPS the plane - Revit holds one
// with every division - and the answer says a work plane was added.
// COMPILED ONLY, NOT RUN IN REVIT: that Revit takes this plane is not measured.

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
    // The division's sketch plane (version 2) - see the header.
    var sketchId = ElementId.InvalidElementId;
    var step = new SubTransaction(doc);
    try
    {
        step.Start();
        sketchId = SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero)).Id;
        doc.Regenerate();
        var maker = PartUtils.DivideParts(doc, parts, cutters, new List<Curve>(), sketchId);
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
    // READ BACK (version 2): a refusal leaves no work plane behind - or says so.
    if (refusal != null && sketchId != ElementId.InvalidElementId && doc.GetElement(sketchId) != null)
        refusal += " The work plane made for the division could not be taken back and is still in the model - Undo this call to remove it.";
    foreach (var s in sources)
    {
        try { partCountAfter += PartUtils.GetAssociatedParts(doc, s, false, true).Count; } catch { }
    }
    if (refusal == null)
        findings.Add(divided + " part(s) divided at " + cutters.Count + " datum(s); their elements now carry " + partCountAfter + " part(s), read back."
            + " Revit keeps a work plane with every division, so one unnamed horizontal work plane was added with it - nothing is drawn on it, and one Undo takes both back.");
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.AddRange(lines);
