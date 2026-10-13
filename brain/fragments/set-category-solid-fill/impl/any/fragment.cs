// NOT STANDALONE. Assumes `doc`, `view`, `categories` and `overrides` are in
// scope; leaves `overridden`, `solidPattern`, `notCuttable`, `notKept`,
// `notControllable` and `refused` behind.
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
//
// SET, THEN READ BACK - and count a category only when what was asked for, the
// stamped patterns included, is what Revit holds. Version 1 did `overridden++`
// straight after the Set, which counted the CALL. Revit takes an override it
// will not keep without a word: on Revit 2020.2 (Project1, a floor plan,
// 2026-10-13) a projection line colour and weight read back on Doors and Walls
// and read back as nothing on Rooms, with no exception and
// `IsCategoryOverridable` true (FRAGMENT-ISSUES 5b-453). On Revit 2024 Ducts
// kept one setting of the four asked for (row 175), and the trap note on this
// card says the surface fill may not land on six MEP categories. Same shape as
// SET_CATEGORY_VISIBILITY's read-back of 2026-09-13, and the same code as
// SET_CATEGORY_GRAPHICS's.
//
// THREE ANSWERS, NOT TWO. All of it there: `overridden`. None of it there - the
// category reads back as if nothing were set: `refused`, in a sentence. Some
// of it there: `notKept`, naming each setting asked for and what came back.
// `notCuttable` is unchanged: the cut half was never asked for on those, so it
// is not compared as missing.
//
// EVERY SETTING THE ADD-IN'S PARSER CAN SET IS COMPARED, by its text. The
// pattern "shown" ticks read TRUE on a category with nothing set, and a fresh
// settings object reads the same, so they compare equal and never mislead.

var overridden = 0;
var notCuttable = new List<string>();
var notKept = new List<string>();
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

    // THE WORKING VALUES ARE INSIDE THIS BLOCK. The add-in reports every
    // top-level variable a fragment leaves (RevitFragment.Report), so the
    // helpers and the settings tables declared at the top would arrive on the
    // answer beside the names above - a table as "21 entry(ies)".
    Func<Color, string> colourText = c =>
        c == null || !c.IsValid ? "none" : string.Format("{0},{1},{2}", c.Red, c.Green, c.Blue);
    Func<ElementId, string> idText = id =>
        id == null || id == ElementId.InvalidElementId ? "none" : id.ToString();
    Func<OverrideGraphicSettings, Dictionary<string, string>> settingsText = s =>
        new Dictionary<string, string>
        {
            { "halftone", s.Halftone ? "on" : "off" },
            { "transparency", s.Transparency.ToString() },
            { "detail level", s.DetailLevel.ToString() },
            { "line colour", colourText(s.ProjectionLineColor) },
            { "line weight", s.ProjectionLineWeight.ToString() },
            { "line pattern", idText(s.ProjectionLinePatternId) },
            { "cut line colour", colourText(s.CutLineColor) },
            { "cut line weight", s.CutLineWeight.ToString() },
            { "cut line pattern", idText(s.CutLinePatternId) },
            { "surface pattern", idText(s.SurfaceForegroundPatternId) },
            { "surface colour", colourText(s.SurfaceForegroundPatternColor) },
            { "surface pattern shown", s.IsSurfaceForegroundPatternVisible ? "yes" : "no" },
            { "surface background pattern", idText(s.SurfaceBackgroundPatternId) },
            { "surface background colour", colourText(s.SurfaceBackgroundPatternColor) },
            { "surface background shown", s.IsSurfaceBackgroundPatternVisible ? "yes" : "no" },
            { "cut pattern", idText(s.CutForegroundPatternId) },
            { "cut colour", colourText(s.CutForegroundPatternColor) },
            { "cut pattern shown", s.IsCutForegroundPatternVisible ? "yes" : "no" },
            { "cut background pattern", idText(s.CutBackgroundPatternId) },
            { "cut background colour", colourText(s.CutBackgroundPatternColor) },
            { "cut background shown", s.IsCutBackgroundPatternVisible ? "yes" : "no" }
        };

    var nothingSet = settingsText(new OverrideGraphicSettings());

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
            // overridden and is counted as such - when the read-back below
            // finds the surface half there.
            notCuttable.Add(category.Name);
        }

        try
        {
            view.SetCategoryOverrides(category.Id, forThis);
        }
        catch
        {
            // A view template holding the graphics, almost always. Named, so
            // the answer is "that view has a template" rather than a count.
            refused.Add(category.Name);
            continue;
        }

        var asked = settingsText(forThis);
        Dictionary<string, string> got;
        try
        {
            got = settingsText(view.GetCategoryOverrides(category.Id));
        }
        catch (Exception e)
        {
            notKept.Add(string.Format("{0}: set, and Revit would not read it back - {1}",
                category.Name, e.Message));
            continue;
        }

        var missed = new List<string>();
        var anythingStored = false;
        foreach (var setting in asked)
        {
            if (got[setting.Key] != nothingSet[setting.Key]) anythingStored = true;
            if (got[setting.Key] != setting.Value)
            {
                missed.Add(string.Format("{0} asked {1}, read back {2}",
                    setting.Key, setting.Value, got[setting.Key]));
            }
        }

        if (missed.Count == 0) overridden++;
        else if (!anythingStored)
        {
            refused.Add(string.Format("Revit took the override and stored nothing for {0}",
                category.Name));
        }
        else
        {
            notKept.Add(string.Format("{0}: not kept - {1}", category.Name, string.Join("; ", missed)));
        }
    }
}
