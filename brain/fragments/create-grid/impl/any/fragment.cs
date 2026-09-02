// NOT STANDALONE. Assumes `doc`, `from`, `to` and `gridName` are in scope;
// leaves `created` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// NO UNIT CONVERSION. The points arrive as XYZ, already in Revit's feet. A
// millimetre input would need D-20 arithmetic; this has none, and adding one
// "to match the other fragments" would place every grid 304.8 times too far
// out.
//
// A GRID IS A VERTICAL PLANE, SO ITS DEFINING LINE IS HORIZONTAL. Two points at
// different heights describe a sloping plane, which is not a thing Revit has,
// and Grid.Create refuses the call. Caught here so the message can say why -
// the exception underneath names a curve, not a height.
//
// THE TOLERANCE IS A MILLIMETRE'S WORTH OF FEET, not zero. Points arriving from
// a snap or a calculation are rarely exactly level, and refusing a grid because
// its two ends differ by a millionth of a foot would be correct and useless.
//
// THE NAME IS CHECKED BEFORE CREATION, same as CREATE_SHEET and CREATE_LEVEL,
// and for the same reason: Revit throws on a duplicate, and a throw AFTER
// Grid.Create leaves a grid in the model under whatever default name Revit
// chose - in the middle of a setting-out other people are working to.

ElementId created = null;
string refused = null;

var name = (gridName ?? "").Trim();

if (from == null || to == null)
{
    refused = "a grid needs two points to run between";
}
else if (Math.Abs(from.Z - to.Z) > 1.0 / 304.8)
{
    refused = string.Format(
        "the two ends are {0:0.#} mm apart in height. A grid is a VERTICAL PLANE, so "
        + "the line defining it has to be level - there is no sloping grid in Revit",
        Math.Abs(from.Z - to.Z) * 304.8);
}
else if (from.DistanceTo(to) < 1.0 / 304.8)
{
    refused = "the two points are the same - there is no direction for the grid to run in";
}
else
{
    string clash = null;

    if (name.Length > 0)
    {
        foreach (var existing in new FilteredElementCollector(doc)
                                     .OfClass(typeof(Grid))
                                     .Cast<Grid>())
        {
            if (existing == null) continue;
            if (string.Equals(existing.Name ?? "", name, StringComparison.OrdinalIgnoreCase))
            {
                clash = existing.Name;
                break;
            }
        }
    }

    if (clash != null)
    {
        refused = string.Format(
            "a grid called \"{0}\" already exists - Revit refuses a duplicate name, and "
            + "creating it first and renaming after leaves a wrongly-named grid in the "
            + "setting-out", clash);
    }
    else
    {
        // Both ends forced onto one height. They are already within a
        // millimetre, checked above; this removes the remainder so Revit is
        // handed an exactly level line rather than a nearly level one.
        var level = new XYZ(to.X, to.Y, from.Z);

        var grid = Grid.Create(doc, Line.CreateBound(from, level));

        if (grid == null)
        {
            refused = "Revit declined to create the grid";
        }
        else
        {
            if (name.Length > 0) grid.Name = name;
            created = grid.Id;
        }
    }
}
