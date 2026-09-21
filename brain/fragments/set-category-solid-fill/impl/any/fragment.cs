// NOT STANDALONE. Assumes `doc`, `view`, `categories` and `overrides` are in
// scope; leaves `overridden`, `solidPattern`, `notControllable` and `refused`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// WHAT THIS ADDS TO SET_CATEGORY_GRAPHICS, AND WHY IT IS HERE AND NOT THERE.
//
// The override string parser in the add-in sets nine things and refuses the
// fill patterns on purpose: a pattern is an ELEMENT, and looking one up is not
// something a string parse should do. So a caller can say
// surface-colour=230,230,230 and get a colour stored against a pattern that
// was never set - which reads back perfectly and prints nothing.
//
// This fragment is the other half. The colours still arrive already parsed;
// all it does is find the solid fill and stamp it on before applying. The
// lookup lives in fragment source, which is sent on every call, so closing
// this gap cost no add-in rebuild and no Revit restart.
//
// IT REPLACES RATHER THAN MERGES, inherited from SetCategoryOverrides itself.

var overridden = 0;
var notControllable = new List<string>();
var refused = new List<string>();
var solidPattern = "";

// THE ELEMENT LOOKUP THE PARSER WILL NOT DO. Same collector COLOR_BY_PARAMETER
// uses, for the same reason: a solid fill is what makes a surface read as a
// colour rather than as a tinted hatch.
FillPatternElement solid = null;
try
{
    solid = new FilteredElementCollector(doc)
        .OfClass(typeof(FillPatternElement))
        .Cast<FillPatternElement>()
        .FirstOrDefault(f => f.GetFillPattern() != null && f.GetFillPattern().IsSolidFill);
}
catch { solid = null; }

if (solid == null)
{
    // NAMED, NOT SWALLOWED, AND NOTHING IS WRITTEN. Every Revit template ships
    // a solid fill, so a model without one has been purged - and applying the
    // colours anyway would store them to no visible effect, which is the exact
    // failure this fragment exists to prevent.
    refused.Add("this model has no solid fill pattern, so a colour would be "
              + "stored and paint nothing. Nothing was overridden.");
    solidPattern = "none found";
}
else
{
    solidPattern = solid.Name;

    // MUTATES THE SETTINGS OBJECT IT WAS HANDED. It is built fresh per call by
    // the caller's parser and is shared with nobody, so stamping the patterns
    // onto it keeps every colour and weight the caller asked for instead of
    // rebuilding them here and losing one.
    overrides.SetSurfaceForegroundPatternId(solid.Id);
    overrides.SetSurfaceForegroundPatternVisible(true);
    overrides.SetCutForegroundPatternId(solid.Id);
    overrides.SetCutForegroundPatternVisible(true);

    foreach (var category in categories)
    {
        if (category == null) continue;

        if (!category.get_AllowsVisibilityControl(view))
        {
            notControllable.Add(category.Name);
            continue;
        }

        try
        {
            view.SetCategoryOverrides(category.Id, overrides);
            overridden++;
        }
        catch
        {
            // A view template holding the graphics, almost always. Named, so
            // the answer is "that view has a template" rather than a count.
            refused.Add(category.Name);
        }
    }
}
