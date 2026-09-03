// NOT STANDALONE. Assumes `doc`, `sourceLegend`, `newName` and `withContents`
// are in scope; leaves `created` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// DUPLICATING IS THE ONLY ROUTE. Revit exposes no legend creation method on any
// supported release - checked against the shipped assemblies at both ends. So a
// project with no legend cannot get its first one from a script, and that is
// said plainly rather than discovered through an obscure failure.
//
// THE SOURCE IS HANDED IN, not searched for. A project with several legends
// should not have one picked for it by whichever the collector returned first.
//
// A LEGEND CAN SIT ON MANY SHEETS AT ONCE - the only view type that can. So
// copying one per sheet is usually the wrong instinct, and this is for when the
// CONTENT genuinely differs.

var created = ElementId.InvalidElementId;
var findings = new List<string>();

if (sourceLegend == null)
{
    findings.Add("No legend was given to copy. Revit's API cannot create the FIRST legend on any "
        + "release - if this project has none, one has to be drawn by hand once (View, then Legends), "
        + "and every copy after that can come from here");
}
else if (sourceLegend.ViewType != ViewType.Legend)
{
    findings.Add(string.Format("'{0}' is a {1}, not a legend. DUPLICATE_VIEW copies any view; this one "
        + "is only about legends", sourceLegend.Name, sourceLegend.ViewType));
}
else
{
    var option = withContents ? ViewDuplicateOption.WithDetailing : ViewDuplicateOption.Duplicate;

    var canDo = false;
    try { canDo = sourceLegend.CanViewBeDuplicated(option); }
    catch { canDo = false; }

    if (!canDo)
    {
        findings.Add(string.Format("Revit will not duplicate '{0}'{1}", sourceLegend.Name,
            withContents ? " with its contents" : ""));
    }
    else
    {
        try
        {
            created = sourceLegend.Duplicate(option);

            var made = doc.GetElement(created) as View;
            if (made == null)
            {
                findings.Add("The duplicate returned an id that is not a view");
                created = ElementId.InvalidElementId;
            }
            else
            {
                var asked = (newName ?? "").Trim();
                var namedOk = true;

                if (asked.Length > 0)
                {
                    try { made.Name = asked; }
                    catch { namedOk = false; }
                }

                // READ IT BACK. Revit decides what a view ends up called.
                var nowCalled = (doc.GetElement(created) as View).Name;

                findings.Add(string.Format("Legend '{0}' copied as '{1}'{2}. It carries {3}, and it is "
                    + "on no sheet yet - a legend can sit on many at once, so place it rather than "
                    + "copying it again",
                    sourceLegend.Name, nowCalled,
                    namedOk ? "" : string.Format(" (the name '{0}' was refused, usually because it is "
                        + "already in use)", asked),
                    withContents ? "the source's contents" : "no contents"));
            }
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("'{0}' could not be duplicated: {1}", sourceLegend.Name, ex.Message));
        }
    }
}
