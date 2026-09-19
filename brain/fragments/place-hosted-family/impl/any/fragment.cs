// NOT STANDALONE. Assumes `doc`, `symbol`, `points`, `host` and `level`;
// leaves `placed`, `failed` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Points are internal FEET.
//
// THE HOST OVERLOAD IS THE POINT. Measured across the library on 2026-09-18,
// every NewFamilyInstance call passed a point, a face or a view - never a host
// element - so a door could not be put in a wall at all.
//
// A DOOR PLACED WITHOUT ITS HOST CUTS NOTHING, schedules wrong and does not
// follow the wall. It looks correct in plan, which is why the host is required
// here rather than optional.
//
// NonStructural is passed deliberately - a door is not a structural member, and
// PLACE_STRUCTURAL_FAMILY is where that decision belongs.

var placed = new List<ElementId>();
var failed = 0;
var findings = new List<string>();

if (host == null)
{
    findings.Add("No host was given. A hosted family needs the element it lives "
        + "in - placed free-standing it cuts nothing and does not follow the "
        + "wall. For a free-standing family use PLACE_FAMILY_INSTANCES");
}
else if (points == null || points.Count == 0)
{
    findings.Add("No points were given, so there is nothing to place");
}
else if (symbol == null)
{
    findings.Add("No family type was given");
}
else
{
    if (!symbol.IsActive)
    {
        symbol.Activate();
        doc.Regenerate();
    }

    for (var i = 0; i < points.Count; i++)
    {
        var point = points[i];
        if (point == null) { failed++; continue; }
        try
        {
            var made = doc.Create.NewFamilyInstance(
                point, symbol, host, level, StructuralType.NonStructural);
            if (made == null) failed++;
            else placed.Add(made.Id);
        }
        catch
        {
            failed++;
        }
    }

    var hostName = host.Name;
    findings.Add(string.Format(
        "{0} placed into '{1}', {2} point(s) it would not take one at",
        placed.Count, string.IsNullOrEmpty(hostName) ? host.Category == null
            ? "the host" : host.Category.Name : hostName, failed));
}
