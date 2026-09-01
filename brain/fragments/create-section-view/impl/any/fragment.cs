// NOT STANDALONE. Assumes `doc`, `elements` and `marginMm` are in scope; leaves
// `created` and `noGeometry` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// MILLIMETRES TO FEET BY ARITHMETIC (D-20). 1 ft = 304.8 mm exactly, so the
// division is exact and UnitUtils - rewritten in 2021, so the 2020 call does
// not exist on 2022 - never has to be named.
//
// THE SECTION IS DEFINED BY A TRANSFORM, NOT BY THE BOX. This is the part that
// is easy to get subtly wrong and hard to see afterwards: CreateSection reads
// the bounding box's Transform for WHERE the cut is and WHICH WAY it looks,
// and reads Min/Max as distances IN THAT TRANSFORM'S OWN AXES. Feeding it a
// model-space box gives a section that is somewhere else, pointing somewhere
// else, and still looks like a section.
//
//   BasisX  right across the drawing
//   BasisY  up the drawing
//   BasisZ  TOWARD THE VIEWER - the view looks down -BasisZ
//   Min.Z   the far clip, so it is NEGATIVE
//   Max.Z   the near side, at the cut plane
//
// WHICH WAY TO LOOK, when the elements have a direction of their own. A duct,
// a pipe or a wall carries a LocationCurve, and the section somebody wants
// through one of those is the CROSS-section: the view looks ALONG the run.
// So the run's own direction becomes BasisZ. With no curve to go on - a set of
// fittings, some equipment - it falls back to looking along the SHORTER
// horizontal side of the bounding box, which puts the long face on the paper
// rather than edge-on.
//
// UP IS UP, EXCEPT WHEN THE RUN IS VERTICAL. BasisY is world up for any normal
// horizontal run. A riser looks along world up, and using world up as BasisY
// too would give three axes that do not form a frame and a section Revit
// refuses. The vertical case takes world X as up instead.

var margin = marginMm / 304.8;

var noGeometry = new List<ElementId>();
View created = null;

// The extent to cover, in model space. Built from the elements' own bounding
// boxes rather than from their location points: a duct's location is a line
// through its middle and a section sized to that would clip the duct in half.
double minX = 0, minY = 0, minZ = 0, maxX = 0, maxY = 0, maxZ = 0;
bool anyBox = false;

XYZ runDirection = null;

foreach (var element in elements)
{
    if (element == null) continue;

    // null View means model extents rather than what one view happens to show,
    // which is what a section has to be cut around.
    var box = element.get_BoundingBox(null);
    if (box == null) { noGeometry.Add(element.Id); continue; }

    if (!anyBox)
    {
        minX = box.Min.X; minY = box.Min.Y; minZ = box.Min.Z;
        maxX = box.Max.X; maxY = box.Max.Y; maxZ = box.Max.Z;
        anyBox = true;
    }
    else
    {
        minX = Math.Min(minX, box.Min.X); maxX = Math.Max(maxX, box.Max.X);
        minY = Math.Min(minY, box.Min.Y); maxY = Math.Max(maxY, box.Max.Y);
        minZ = Math.Min(minZ, box.Min.Z); maxZ = Math.Max(maxZ, box.Max.Z);
    }

    // The FIRST location curve found sets the direction. Not an average of all
    // of them: two ducts crossing at right angles average to a diagonal, and a
    // section on a diagonal is square to nothing in the model.
    if (runDirection == null)
    {
        var location = element.Location as LocationCurve;
        if (location != null && location.Curve != null)
        {
            var curve = location.Curve;
            var along = curve.GetEndPoint(1) - curve.GetEndPoint(0);
            if (along.GetLength() > 1e-9) runDirection = along.Normalize();
        }
    }
}

if (anyBox)
{
    var centre = new XYZ((minX + maxX) / 2.0, (minY + maxY) / 2.0, (minZ + maxZ) / 2.0);

    XYZ toViewer = runDirection;
    if (toViewer == null)
    {
        // No run to follow. Look along whichever horizontal side is SHORTER,
        // so the wider face of the set ends up across the paper instead of
        // being seen end-on.
        toViewer = (maxX - minX) <= (maxY - minY) ? XYZ.BasisX : XYZ.BasisY;
    }

    // A riser looks straight up, and world up cannot then also be the drawing's
    // up. Detected by how much of the direction is vertical rather than by an
    // equality test, because a run at 89 degrees has the same problem.
    var up = Math.Abs(toViewer.Z) > 0.99 ? XYZ.BasisX : XYZ.BasisZ;

    var right = up.CrossProduct(toViewer);
    if (right.GetLength() < 1e-9) right = XYZ.BasisY;
    right = right.Normalize();

    // Rebuilt from the other two so the three axes are exactly orthogonal.
    // Revit rejects a transform whose axes are merely nearly square, and the
    // input direction came from a curve whose ends were measured, not assumed.
    up = toViewer.CrossProduct(right).Normalize();

    var frame = Transform.Identity;
    frame.Origin = centre;
    frame.BasisX = right;
    frame.BasisY = up;
    frame.BasisZ = toViewer;

    // The extent, measured in the section's own axes. Every corner of the model
    // box is projected onto each axis: taking the box's own X and Y would be
    // right only when the section happens to be square to the world, and a duct
    // run at 30 degrees is neither.
    double halfRight = 0, halfUp = 0, halfDepth = 0;

    for (int i = 0; i < 8; i++)
    {
        var corner = new XYZ((i & 1) == 0 ? minX : maxX,
                             (i & 2) == 0 ? minY : maxY,
                             (i & 4) == 0 ? minZ : maxZ);
        var offset = corner - centre;

        halfRight = Math.Max(halfRight, Math.Abs(offset.DotProduct(right)));
        halfUp    = Math.Max(halfUp,    Math.Abs(offset.DotProduct(up)));
        halfDepth = Math.Max(halfDepth, Math.Abs(offset.DotProduct(toViewer)));
    }

    var extent = new BoundingBoxXYZ();
    extent.Transform = frame;
    extent.Min = new XYZ(-(halfRight + margin), -(halfUp + margin), -(halfDepth + margin));
    extent.Max = new XYZ(halfRight + margin, halfUp + margin, halfDepth + margin);

    // Any section type in the project. Revit ships one and projects rename it,
    // so the type is found by its ViewFamily - what it IS - rather than by a
    // name that a template is free to change.
    ViewFamilyType sectionType = null;
    foreach (var candidate in new FilteredElementCollector(doc)
                                 .OfClass(typeof(ViewFamilyType))
                                 .Cast<ViewFamilyType>())
    {
        if (candidate.ViewFamily == ViewFamily.Section) { sectionType = candidate; break; }
    }

    // No section type means no section, and saying so beats throwing. A project
    // started from a bare template genuinely has none.
    if (sectionType != null) created = ViewSection.CreateSection(doc, sectionType.Id, extent);
}
