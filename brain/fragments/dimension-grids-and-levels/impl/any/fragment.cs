// NOT STANDALONE. Assumes `doc`, `view`, `elements` and `offsetMm` are in
// scope; leaves `created`, `refused`, `notVisibleInView`, `notADatum` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// MILLIMETRES TO FEET BY ARITHMETIC (D-20).
//
// A GRID AND A LEVEL ARE REFERENCED AS THE WHOLE ELEMENT, and that single fact
// is why this is not part of CREATE_DIMENSION. Everywhere else a Reference has
// to be dug out of geometry read with ComputeReferences on; a datum needs none
// of it. Reading a grid's geometry the other way returns nothing at all.
//
// GRIDS AND LEVELS ARE NEVER MIXED INTO ONE STRING. They are measured along
// axes at right angles to each other. A string holding both measures every
// reference along the ONE axis it was given, so the levels inside a grid string
// come back as zeros - a dimension drawn wrongly, which is read as correct.
//
// A DATUM NOT VISIBLE IN THE VIEW TAKES THE WHOLE STRING DOWN. Revit throws
// when asked to dimension to something the view is not showing, and the throw
// loses the datums that were fine too. They are checked against the view first.
//
// THE LINE IS WHERE THE STRING SITS, NOT WHAT IT MEASURES. Revit works the
// measurement out from the references. The line decides where the text and
// witness lines land, so it is built ACROSS the datums and in the view plane,
// with offsetMm sliding it clear along the other axis.

var offset = offsetMm / 304.8;

ElementId created = null;
var refused = new List<ElementId>();
var notVisibleInView = new List<ElementId>();
var notADatum = new List<ElementId>();
var findings = new List<string>();

// What this view is actually showing. Asked once, per class, rather than per
// element - a view-scoped collector is the only honest answer to "will Revit
// let me dimension to this here".
var visibleDatumIds = new HashSet<ElementId>();
foreach (var shown in new FilteredElementCollector(doc, view.Id).OfClass(typeof(Grid)))
{
    visibleDatumIds.Add(shown.Id);
}
foreach (var shown in new FilteredElementCollector(doc, view.Id).OfClass(typeof(Level)))
{
    visibleDatumIds.Add(shown.Id);
}

var grids = new List<Grid>();
var levels = new List<Level>();

foreach (var element in elements)
{
    if (element == null) continue;

    var grid = element as Grid;
    var level = element as Level;

    if (grid == null && level == null)
    {
        // A wall, a duct, a piece of equipment. Not a failure of this fragment
        // - a different mechanism entirely, and CREATE_DIMENSION or
        // DIMENSION_MEP_RUNS is where it goes.
        notADatum.Add(element.Id);
        continue;
    }

    if (!visibleDatumIds.Contains(element.Id))
    {
        notVisibleInView.Add(element.Id);
        continue;
    }

    if (grid != null) grids.Add(grid);
    else levels.Add(level);
}

if (grids.Count > 0 && levels.Count > 0)
{
    // Refused rather than drawn along whichever axis happened to win. The two
    // are measured at right angles to each other and a mixed string is wrong
    // in a way that looks right on the sheet.
    foreach (var grid in grids) refused.Add(grid.Id);
    foreach (var level in levels) refused.Add(level.Id);

    findings.Add("Grids and levels were given together. They are measured along axes at right "
        + "angles to each other, so one string across both measures the wrong thing for "
        + "whichever kind loses. Dimension the grids and the levels as two strings.");
}
else
{
    var references = new List<Reference>();
    var positions = new List<double>();
    var accepted = new List<ElementId>();

    // The measuring axis, and the axis the string is offset along. Built once,
    // from the kind of datum in hand.
    XYZ across = null;
    XYZ placementSeed = null;

    if (grids.Count > 0)
    {
        XYZ gridDirection = null;

        foreach (var grid in grids)
        {
            var line = grid.Curve as Line;
            if (line == null)
            {
                // An arc grid has no one direction, so no single axis a string
                // across it measures along.
                refused.Add(grid.Id);
                continue;
            }

            var along = line.GetEndPoint(1) - line.GetEndPoint(0);
            if (along.GetLength() < 1e-9) { refused.Add(grid.Id); continue; }
            along = along.Normalize();

            if (gridDirection == null)
            {
                gridDirection = along;
                placementSeed = line.GetEndPoint(0);
            }
            else if (Math.Abs(along.DotProduct(gridDirection)) < 0.999)
            {
                // Out of parallel by more than about two and a half degrees.
                // The gap between two grids that are not parallel is not one
                // number, so there is nothing honest to put in the string.
                refused.Add(grid.Id);
                continue;
            }

            references.Add(new Reference(grid));
            positions.Add(0.0);
            accepted.Add(grid.Id);
        }

        if (gridDirection != null)
        {
            // Across the grids and drawable on this view: the view normal
            // crossed with the grid direction is the only direction that is
            // both.
            across = view.ViewDirection.CrossProduct(gridDirection);
        }

        // Positions along the measuring axis, now that the axis exists.
        if (across != null && across.GetLength() >= 1e-9)
        {
            across = across.Normalize();

            positions.Clear();
            foreach (var id in accepted)
            {
                var line = (doc.GetElement(id) as Grid).Curve as Line;
                positions.Add(line.GetEndPoint(0).DotProduct(across));
            }
        }
    }
    else if (levels.Count > 0)
    {
        // Levels stack vertically, so the measuring axis is simply up.
        across = XYZ.BasisZ;

        // A plan view looks straight down the measuring axis: there is no way
        // to draw a level-to-level dimension on it, and Revit would take the
        // whole string down. Refused with the reason rather than attempted.
        if (Math.Abs(XYZ.BasisZ.DotProduct(view.ViewDirection)) > 0.001)
        {
            foreach (var level in levels) refused.Add(level.Id);
            across = null;

            findings.Add("Levels cannot be dimensioned in this view - it looks along the "
                + "vertical, which is the axis the dimension would have to measure. A section "
                + "or an elevation is where floor to floor heights go.");
        }
        else
        {
            placementSeed = view.Origin;

            foreach (var level in levels)
            {
                references.Add(new Reference(level));

                // ProjectElevation, NOT Elevation. These positions build the
                // dimension LINE, in model coordinates. Elevation is measured
                // from whatever the level type's Elevation Base says - a
                // survey point, a moved base point - and is off by that
                // offset in model space; two level types with different bases
                // can even put two different heights at one number, which the
                // zero-length check below would then refuse. Until 2026-09-23
                // this read Elevation.
                positions.Add(level.ProjectElevation);
                accepted.Add(level.Id);
            }
        }
    }

    // Two references is the minimum a dimension can hold. One is not a short
    // dimension, it is not a dimension.
    if (references.Count >= 2 && across != null && placementSeed != null)
    {
        var offsetDirection = across.CrossProduct(view.ViewDirection);

        if (offsetDirection.GetLength() < 1e-9)
        {
            refused.AddRange(accepted);
            findings.Add("The measuring axis and the view direction are the same line, so the "
                + "string has nowhere to sit on this view.");
        }
        else
        {
            offsetDirection = offsetDirection.Normalize();

            double lowest = positions[0], highest = positions[0];
            for (int i = 1; i < positions.Count; i++)
            {
                lowest = Math.Min(lowest, positions[i]);
                highest = Math.Max(highest, positions[i]);
            }

            var basePoint = placementSeed
                - across * placementSeed.DotProduct(across)
                + offsetDirection * offset;

            var from = basePoint + across * lowest;
            var to = basePoint + across * highest;

            if ((to - from).GetLength() > 1e-6)
            {
                var array = new ReferenceArray();
                foreach (var reference in references) array.Append(reference);

                var dimension = doc.Create.NewDimension(view, Line.CreateBound(from, to), array);
                if (dimension != null) created = dimension.Id;
            }
            else
            {
                // Every datum sits at the same position along the measuring
                // axis - two grids drawn on top of each other, or two levels at
                // one elevation. The gap is zero and there is nothing to draw.
                refused.AddRange(accepted);
                findings.Add("Every datum given sits at the same position along the measuring "
                    + "axis, so the string would measure zero.");
            }
        }
    }
    else if (references.Count == 1)
    {
        refused.AddRange(accepted);
        findings.Add("Only one datum was usable. One reference is not a short dimension, it is "
            + "not a dimension.");
    }
}

if (notVisibleInView.Count > 0)
{
    findings.Add(notVisibleInView.Count + " datum(s) are not shown in this view and were left "
        + "out. Revit throws when asked to dimension to something the view is not showing, and "
        + "that throw would have lost the whole string.");
}
