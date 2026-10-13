// NOT STANDALONE. Assumes `view`, `categories` and `overrides` are in scope;
// leaves `overridden`, `notKept`, `notControllable` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// WHY A GRAYOUT SHOULD DO ITS BACKGROUND THIS WAY.
//
// Overriding every wall, floor and ceiling one at a time gives the right
// picture TODAY and a wrong one tomorrow: a wall drawn after the grayout ran
// keeps its normal graphics, because nothing ever touched it. The drawing
// quietly stops matching itself, and the person who notices is whoever prints
// it.
//
// A category override covers everything in the category, including what does
// not exist yet. It is also one setting to clear instead of four hundred.
//
// AN EMPTY SETTINGS OBJECT CLEARS IT. Same as the per-element reset - there is
// no separate clear call, and passing a fresh OverrideGraphicSettings removes
// whatever the category had. So this fragment applies AND undoes, depending
// entirely on what it is handed.
//
// IT REPLACES RATHER THAN MERGES, exactly like the per-element version. To
// change one aspect and keep the rest, read the category's current overrides
// with `view.GetCategoryOverrides(id)`, modify that, and pass it back.
//
// CATEGORIES THAT CANNOT BE CONTROLLED ARE NAMED, not counted. "Two categories
// were skipped" is not something a person can act on.
//
// SET, THEN READ BACK - and count a category only when what was asked for is
// what Revit holds. Version 1 did `overridden++` straight after the Set, which
// counted the CALL. Revit takes an override it will not keep without a word:
// on Revit 2020.2 (Project1, a floor plan, 2026-10-13) a projection line
// colour 200,30,30 and weight 5 read back on Doors and Walls and read back as
// nothing on Rooms, with no exception and `IsCategoryOverridable` true, so
// Rooms came back "overridden 1" with nothing stored (FRAGMENT-ISSUES 5b-453).
// On Revit 2024 Ducts kept one setting of the four asked for (row 175). Same
// shape as SET_CATEGORY_VISIBILITY's read-back of 2026-09-13.
//
// THREE ANSWERS, NOT TWO. All of it there: `overridden`. None of it there - the
// category reads back as if nothing were set: `refused`, in a sentence. Some
// of it there: `notKept`, naming each setting asked for and what came back,
// because "Ducts kept the line colour and dropped the surface colour" is
// something a person can act on and a count is not. An EMPTY settings object
// (the clear) is judged the same way: it is all there when nothing reads back.
//
// EVERY SETTING THE ADD-IN'S PARSER CAN SET IS COMPARED, by its text. The
// pattern "shown" ticks read TRUE on a category with nothing set, and a fresh
// settings object reads the same, so they compare equal and never mislead.

var overridden = 0;
var notKept = new List<string>();
var notControllable = new List<string>();
var refused = new List<string>();

// THE WORKING VALUES ARE INSIDE A BLOCK. The add-in reports every top-level
// variable a fragment leaves (RevitFragment.Report), so the helpers and the
// two settings tables declared out here would arrive on the answer beside the
// four names above - the tables as "21 entry(ies)". Same as LIST_MATERIALS.
{
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
    var asked = settingsText(overrides);

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
        }
        catch
        {
            // A view template holding the graphics, almost always. Named, so the
            // answer is "that view has a template" rather than a failure count.
            refused.Add(category.Name);
            continue;
        }

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
