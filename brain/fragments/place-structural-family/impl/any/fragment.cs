// NOT STANDALONE. Assumes `doc`, `symbol`, `points`, `level` and
// `structuralType` are in scope; leaves `placed`, `failed` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Points are internal FEET.
//
// WHY THIS IS NOT PLACE_FAMILY_INSTANCES. That one passes
// StructuralType.NonStructural as a literal, which is correct for the air
// terminals and sprinkler heads its purpose names and wrong for every column
// and footing. The structural type is an INPUT here, and that is the only
// material difference between the two files.
//
// A NON-STRUCTURAL COLUMN LOOKS IDENTICAL ON EVERY DRAWING and is counted
// differently by every schedule, so the wrong value is invisible until somebody
// prices it.

var placed = new List<ElementId>();
var failed = 0;
var findings = new List<string>();

var asked = (structuralType ?? "").Trim().ToLowerInvariant();
var wanted = StructuralType.NonStructural;
var understood = true;

if (asked == "column") wanted = StructuralType.Column;
else if (asked == "footing") wanted = StructuralType.Footing;
else if (asked == "beam") wanted = StructuralType.Beam;
else if (asked == "brace") wanted = StructuralType.Brace;
else if (asked == "nonstructural" || asked == "non-structural") wanted = StructuralType.NonStructural;
else understood = false;

if (!understood)
{
    findings.Add(string.Format(
        "'{0}' is not a structural type Revit knows. Say column, footing, beam, "
        + "brace or non-structural", structuralType ?? "(nothing)"));
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
            var made = doc.Create.NewFamilyInstance(point, symbol, level, wanted);
            if (made == null) failed++;
            else placed.Add(made.Id);
        }
        catch
        {
            failed++;
        }
    }

    findings.Add(string.Format(
        "{0} placed as {1} on '{2}', {3} point(s) could not take one",
        placed.Count, wanted, level == null ? "(no level)" : level.Name, failed));
}
