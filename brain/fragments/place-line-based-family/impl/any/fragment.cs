// NOT STANDALONE. Assumes `doc`, `symbol`, `curves`, `level` and
// `structuralType` are in scope; leaves `placed`, `failed` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Curves are internal FEET.
//
// THE CURVE OVERLOAD IS THE POINT OF THIS FILE. Every other placement fragment
// here calls NewFamilyInstance(XYZ, ...), which cannot express a beam: a beam
// is driven by the LINE between its two ends, not by a point.
//
// THE STRUCTURAL TYPE IS THE CALLER'S, NOT A DEFAULT. Beam and Brace are the
// same call with a different enum, and Revit's analytical model, its schedules
// and the load take-off all follow it.

var placed = new List<ElementId>();
var failed = 0;
var findings = new List<string>();

var asked = (structuralType ?? "").Trim().ToLowerInvariant();
var wanted = StructuralType.UnknownFraming;
var understood = true;

if (asked == "beam") wanted = StructuralType.Beam;
else if (asked == "brace") wanted = StructuralType.Brace;
else if (asked == "column") wanted = StructuralType.Column;
else understood = false;

if (!understood)
{
    findings.Add(string.Format(
        "'{0}' is not a line-based structural type. Say beam, brace or column - "
        + "a footing and anything non-structural are placed at a POINT, which is "
        + "PLACE_STRUCTURAL_FAMILY", structuralType ?? "(nothing)"));
}
else if (curves == null || curves.Count == 0)
{
    findings.Add("No lines were given, so there is nothing to place along");
}
else if (symbol == null)
{
    findings.Add("No family type was given");
}
else
{
    // AN UNACTIVATED SYMBOL PLACES NOTHING AND SAYS NOTHING. Same trap
    // PLACE_FAMILY_INSTANCES exists to close.
    if (!symbol.IsActive)
    {
        symbol.Activate();
        doc.Regenerate();
    }

    for (var i = 0; i < curves.Count; i++)
    {
        var curve = curves[i];
        if (curve == null) { failed++; continue; }
        try
        {
            var made = doc.Create.NewFamilyInstance(curve, symbol, level, wanted);
            if (made == null) failed++;
            else placed.Add(made.Id);
        }
        catch
        {
            failed++;
        }
    }

    findings.Add(string.Format(
        "{0} placed as {1} on '{2}', {3} line(s) could not take one",
        placed.Count, wanted, level == null ? "(no level)" : level.Name, failed));
}
