// NOT STANDALONE. Assumes `doc`, `view`, `categories` and `overrides` are in
// scope; leaves `overridden`, `solidPattern`, `notCuttable`, `notControllable`
// and `refused` behind.
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
// NOT EVERY CATEGORY CAN BE CUT, AND REVIT WILL NOT TELL YOU.
//
// Furniture, most equipment in plan and every annotation category are not
// cuttable - their Cut columns are greyed out in Visibility/Graphics. Set a
// cut pattern on one anyway and Revit ACCEPTS IT, stores it, hands it back on
// a read, and draws nothing with it. No exception, no warning, and no count
// can see it.
//
// That is the same failure this fragment exists to close, one level down. So
// the cut half is applied ONLY where `IsCuttable` says it means something, and
// the categories that could not take it are NAMED. Reported by name and never
// as a count, because "one category was skipped" is not something a person can
// act on.
//
// IT REPLACES RATHER THAN MERGES, inherited from SetCategoryOverrides itself.

var overridden = 0;
var notCuttable = new List<string>();
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

    foreach (var category in categories)
    {
        if (category == null) continue;

        if (!category.get_AllowsVisibilityControl(view))
        {
            notControllable.Add(category.Name);
            continue;
        }

        // ONE SETTINGS OBJECT PER CATEGORY. Whether the CUT half means
        // anything is a property of the CATEGORY, not of the request, so the
        // caller's object is copied rather than stamped and un-stamped round
        // the loop. Every colour and weight the caller asked for is carried
        // into the copy by the copy constructor.
        var forThis = new OverrideGraphicSettings(overrides);

        forThis.SetSurfaceForegroundPatternId(solid.Id);
        forThis.SetSurfaceForegroundPatternVisible(true);

        var cuttable = false;
        try { cuttable = category.IsCuttable; }
        catch { cuttable = false; }

        if (cuttable)
        {
            forThis.SetCutForegroundPatternId(solid.Id);
            forThis.SetCutForegroundPatternVisible(true);
        }
        else
        {
            // The surface half still applies and is still worth having - a
            // plan view of furniture shows its surface, never its cut. So
            // this is a NOTE ON WHAT LANDED, not a refusal: the category IS
            // overridden and is counted as such.
            notCuttable.Add(category.Name);
        }

        try
        {
            view.SetCategoryOverrides(category.Id, forThis);
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
