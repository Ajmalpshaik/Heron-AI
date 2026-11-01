// NOT STANDALONE. Assumes `view`, `categories`, `overrides` and `solidFill`
// are in scope; leaves `overridden`, `notKept`, `notControllable`, `refused`,
// `solidPattern` and `notCuttable` behind.
//
// VERSION 3 (2026-11-01) - SET_CATEGORY_SOLID_FILL MERGED IN, on the owner's
// instruction, as ONE SWITCH, `solidFill`, OFF unless asked.
//
// WHY A SWITCH AND NOT ALWAYS. A surface colour tints the pattern the
// material already carries, and where it carries none the colour is stored,
// reads back, and paints nothing - which is why SET_CATEGORY_SOLID_FILL
// existed. But a surface colour ALONE never stamps a solid fill here: a
// grayout's background, and BUILDERS_WORK_LAYOUT's first step, tint what is
// drawn and must not paint every wall a solid block. So the fill is stamped
// only when the caller turns the switch on.
//
// WITH THE SWITCH ON, what SET_CATEGORY_SOLID_FILL did, unchanged:
//
//   the model's solid fill is found by what it IS (FillPattern.IsSolidFill),
//   never by a name. A model with none has been purged, and then NOTHING IS
//   WRITTEN on any category - a colour stored against no pattern is the exact
//   failure the switch exists to prevent - and `refused` says so.
//
//   the surface foreground pattern is stamped solid and shown; the CUT half
//   only where `Category.IsCuttable` says the category is ever cut. Revit
//   accepts a cut pattern on a category it never cuts, stores it, reads it
//   back, and draws nothing with it - so those categories are NAMED in
//   `notCuttable`. They are still overridden and still counted when the
//   surface half reads back.
//
//   `solidPattern` names the pattern found, and the read-back below compares
//   the stamped pattern ids as well as every other setting, so a fill Revit
//   did not keep is in `notKept` or `refused`, never counted.
//
//   AN EMPTY SETTINGS OBJECT WITH THE SWITCH ON STAMPS THE FILL - the switch
//   is an explicit ask, so it is honoured with nothing else beside it. With
//   the switch off an empty object is still the clear, as before.
//
// The caller's settings object is COPIED per category before stamping, so
// every colour and weight asked for survives, and the cut decision belongs to
// each category rather than to one shared object. The document is the view's
// own, so the card needs no `doc`.
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
var solidPattern = "";
var notCuttable = new List<string>();

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

    // THE SOLID FILL, only when the switch is on (version 3, from
    // SET_CATEGORY_SOLID_FILL). Found by what it is, in the view's own model.
    FillPatternElement solid = null;
    var stopped = false;
    if (solidFill)
    {
        try
        {
            solid = new FilteredElementCollector(view.Document)
                .OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .FirstOrDefault(f => f.GetFillPattern() != null && f.GetFillPattern().IsSolidFill);
        }
        catch { solid = null; }

        if (solid == null)
        {
            // NAMED, NOT SWALLOWED, AND NOTHING IS WRITTEN on any category.
            refused.Add("this model has no solid fill pattern, so a colour would be "
                      + "stored and paint nothing. Nothing was overridden.");
            solidPattern = "none found";
            stopped = true;
        }
        else
        {
            solidPattern = solid.Name;
        }
    }

    foreach (var category in (stopped ? new List<Category>() : categories))
    {
        if (category == null) continue;

        if (!category.get_AllowsVisibilityControl(view))
        {
            notControllable.Add(category.Name);
            continue;
        }

        // ONE SETTINGS OBJECT PER CATEGORY when the fill is stamped: the cut
        // half belongs to the category. Without the switch, the caller's own.
        var forThis = overrides;
        if (solid != null)
        {
            forThis = new OverrideGraphicSettings(overrides);
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
                // A NOTE ON WHAT LANDED, not a refusal: the surface half still
                // applies and is counted when it reads back.
                notCuttable.Add(category.Name);
            }
        }

        try
        {
            view.SetCategoryOverrides(category.Id, forThis);
        }
        catch
        {
            // A view template holding the graphics, almost always. Named, so the
            // answer is "that view has a template" rather than a failure count.
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
