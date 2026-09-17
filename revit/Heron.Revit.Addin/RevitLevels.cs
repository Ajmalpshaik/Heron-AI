// Heron-Agent:  HERON-REVIT-LVL-027
// Heron-Step:   4
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Heron.Bridge;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Levels and grids. HERON-REVIT-LVL-027, whose row calls them "the hosts
    /// almost everything depends on".
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit, and on a day another session had
    /// the only Revit open. It COMPILES on 2020 through 2027 and that is the
    /// whole of what is known about it.
    /// ===================================================================
    ///
    /// WHY A MODELLER CARES, IN ONE SENTENCE
    /// --------------------------------------
    /// Everything else in the model hangs off these. A duct is at a level, a
    /// room is bounded between two, a view is cut at one, a dimension is
    /// measured to a grid. Get a level wrong and nothing downstream is
    /// merely wrong - it is wrong in a way that looks fine on the sheet.
    ///
    /// THE THREE THINGS IT LOOKS FOR, AND WHY THEY ARE THOSE THREE
    /// ------------------------------------------------------------
    /// A list of level names tells a modeller nothing they did not know. So
    /// this reports the three states that are genuinely hard to see in the
    /// Revit user interface and genuinely cost a day when missed:
    ///
    ///   TWO LEVELS AT THE SAME ELEVATION. Perfectly legal, occasionally
    ///   deliberate, and the usual cause of "I put it on Level 3 and it
    ///   isn't there". The Project Browser sorts by name, so two levels a
    ///   millimetre apart sit nowhere near each other in it.
    ///
    ///   A LEVEL NOTHING IS ON. Either a leftover from setting-out, or the
    ///   sign that somebody has been modelling onto the wrong one.
    ///
    ///   A NAME THAT DOES NOT SORT. "Level 10" sorts before "Level 2" in
    ///   every browser and every schedule, because both sort as text. It is
    ///   reported as an OBSERVATION and never as an error - plenty of jobs
    ///   name levels for what they are rather than for a number, and this
    ///   file has no business having an opinion about a naming standard it
    ///   was not shown.
    ///
    /// ELEVATIONS ARE ASKED FOR, NEVER CALCULATED
    /// --------------------------------------------
    /// Revit holds a length in decimal feet whatever the project is set to,
    /// so `level.Elevation` returns 9.84 for a level at 3000 mm. Handing
    /// that back is the exact defect RevitParameters was written to avoid -
    /// a number with no unit near it is read as millimetres by the next
    /// person who sees it.
    ///
    /// So every elevation here comes from `AsValueString()` on the level's
    /// own parameter, which is the only thing that applies the project's
    /// unit settings, and it arrives as a STRING a modeller would recognise
    /// from a schedule. The raw double is used for ORDERING and for
    /// comparing two levels to each other, and never printed.
    ///
    /// NOTHING IS SET
    /// ----------------
    /// The register gives this row MODIFY, which its own column header
    /// defines as "the HIGHEST permission level it can require". Reading
    /// requires none of it. Renaming a level or moving its elevation drags
    /// every hosted element with it, on everybody's job, and doing that from
    /// a machine that has never opened Revit is not something this file will
    /// be the first to try. A write will be a SEPARATE operation at its own
    /// risk, because the risk is looked up by name and one name must mean
    /// one thing.
    /// </summary>
    internal static class RevitLevels
    {
        /// <summary>
        /// Two elevations this close are the same elevation for a modeller's
        /// purposes. In decimal feet, because that is what Revit stores and
        /// converting to compare would add a rounding step for nothing.
        ///
        /// 1/32 inch. Chosen because it is below anything anyone sets out
        /// deliberately and above the floating-point noise of a level that
        /// was typed in millimetres and stored in feet - 3000 mm is not
        /// exactly representable, so two levels both "at 3000" can differ in
        /// the twelfth decimal place and must still read as one elevation.
        /// </summary>
        private const double SameElevation = 1.0 / 384.0;

        /// <summary>
        /// Every level and grid in the active document, with what is on each
        /// level and what looks wrong.
        /// </summary>
        public static string List(UIApplication app)
        {
            var doc = RevitOperations.ActiveDocument(app);
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            var levels = new List<Level>();
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(Level))
                                        .WhereElementIsNotElementType())
            {
                var level = element as Level;
                if (level != null) levels.Add(level);
            }

            var grids = new List<Grid>();
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(Grid))
                                        .WhereElementIsNotElementType())
            {
                var grid = element as Grid;
                if (grid != null) grids.Add(grid);
            }

            // COUNTED, NOT REASONED ABOUT. One pass over the placed elements,
            // asking each which level it names. An element that names none is
            // not a fault - a grid names no level, and neither does a view.
            var hosted = new Dictionary<string, int>(StringComparer.Ordinal);
            var onNoLevel = 0;
            var placed = 0;
            foreach (var element in new FilteredElementCollector(doc)
                                        .WhereElementIsNotElementType())
            {
                if (element == null) continue;
                placed++;
                var key = LevelKeyOf(doc, element);
                if (key == null) { onNoLevel++; continue; }
                int already;
                hosted[key] = hosted.TryGetValue(key, out already) ? already + 1 : 1;
            }

            levels.Sort(CompareByElevation);

            var levelRows = new List<string>();
            var sharing = 0;
            var empty = 0;
            for (var i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                var key = level.UniqueId;

                int on;
                if (!hosted.TryGetValue(key, out on)) on = 0;
                if (on == 0) empty++;

                // Sorted by elevation, so a level sharing one is adjacent to
                // the level it shares it with. Both ends of a pair are
                // flagged, because "one of these two" is not an answer.
                var shared =
                    (i > 0 && Together(levels[i - 1], level)) ||
                    (i + 1 < levels.Count && Together(level, levels[i + 1]));
                if (shared) sharing++;

                levelRows.Add(Json.Obj(
                    Json.Str("name", SafeName(level)),
                    Json.Str("elevation", ElevationText(level)),
                    Json.Num("elements", on),
                    Json.Bool("sharesElevation", shared)));
            }

            var gridRows = new List<string>();
            var gridNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var grid in grids)
            {
                var name = SafeName(grid);
                int seen;
                gridNames[name] = gridNames.TryGetValue(name, out seen) ? seen + 1 : 1;
                gridRows.Add(Json.Obj(
                    Json.Str("name", name),
                    Json.Str("extent", ExtentText(grid))));
            }

            var repeatedGrids = 0;
            foreach (var pair in gridNames) if (pair.Value > 1) repeatedGrids++;

            var view = SafeView(doc);
            return Json.Ok(
                Json.Arr("levels", levelRows),
                Json.Arr("grids", gridRows),
                Json.Num("levelCount", levelRows.Count),
                Json.Num("gridCount", gridRows.Count),
                Json.Num("levelsSharingAnElevation", sharing),
                Json.Num("levelsWithNothingOnThem", empty),
                Json.Num("repeatedGridNames", repeatedGrids),
                Json.Num("placedElements", placed),
                Json.Num("onNoLevel", onNoLevel),
                Json.Bool("namesSortOutOfOrder", OutOfOrder(levels)),
                Json.Str("activeView", view == null ? null : SafeName(view)),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Str("reads", Reads(levelRows.Count, sharing, empty,
                                        repeatedGrids, OutOfOrder(levels))));
        }

        /// <summary>
        /// What the numbers above mean, in the words a modeller would use.
        ///
        /// Written per shape rather than as one paragraph, because a sentence
        /// listing every state a model might be in is a sentence nobody
        /// finishes reading. A clean model gets a short answer.
        /// </summary>
        private static string Reads(int levels, int sharing, int empty,
                                    int repeatedGrids, bool outOfOrder)
        {
            if (levels == 0)
            {
                return "This model has no levels at all. That is normal for a "
                     + "family or a detail file and unusual for a project.";
            }

            var said = new List<string>();
            if (sharing > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} level(s) sit at the same elevation as another. That is "
                  + "legal and sometimes deliberate, and it is also the usual "
                  + "reason something modelled on one level cannot be found on "
                  + "the other. The Project Browser sorts by NAME, so the two "
                  + "are nowhere near each other in it", sharing));
            }
            if (empty > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} level(s) have nothing on them - either set-out that "
                  + "was never cleared up, or work that went onto a different "
                  + "level than intended", empty));
            }
            if (repeatedGrids > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} grid name(s) are used more than once. A dimension to "
                  + "\"grid B\" stops being an instruction anybody can follow",
                    repeatedGrids));
            }
            if (outOfOrder)
            {
                said.Add("level names do not sort in elevation order - \"Level "
                       + "10\" sorts before \"Level 2\" in every browser and "
                       + "every schedule, because both sort as text. This is an "
                       + "observation, not a fault: plenty of jobs name levels "
                       + "for what they are rather than for a number");
            }

            if (said.Count == 0)
            {
                return "Every level has a distinct elevation, something on it, "
                     + "and a name that sorts in the same order as the "
                     + "building. Elevations are as Revit prints them, in the "
                     + "project's own units.";
            }

            return "Elevations are as Revit prints them, in the project's own "
                 + "units - never a raw number. " + string.Join(". ", said.ToArray())
                 + ". Nothing was changed.";
        }

        /// <summary>Do these two levels sit at one elevation, for a modeller?</summary>
        private static bool Together(Level below, Level above)
        {
            try
            {
                return Math.Abs(above.Elevation - below.Elevation) < SameElevation;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
        }

        /// <summary>Lowest first. A level that will not say where it is sorts last.</summary>
        private static int CompareByElevation(Level left, Level right)
        {
            double a, b;
            try { a = left.Elevation; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { a = double.MaxValue; }
            try { b = right.Elevation; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { b = double.MaxValue; }
            return a.CompareTo(b);
        }

        /// <summary>
        /// Do the names sort into the same order as the building?
        ///
        /// Compared with the ORDINAL comparison a browser and a schedule
        /// actually use, rather than a natural-sort that understands "10"
        /// follows "2". Answering with the clever comparison would say the
        /// names are fine while Revit continues to display them jumbled,
        /// which helps nobody.
        /// </summary>
        private static bool OutOfOrder(List<Level> byElevation)
        {
            for (var i = 1; i < byElevation.Count; i++)
            {
                var previous = SafeName(byElevation[i - 1]);
                var current = SafeName(byElevation[i]);
                if (string.CompareOrdinal(previous, current) > 0) return true;
            }
            return false;
        }

        /// <summary>
        /// The elevation as Revit itself would print it, in the project's
        /// units, or null.
        ///
        /// `AsValueString` and never `Elevation`. The double is decimal feet
        /// whatever the project is set to, and a level reported as 9.84 with
        /// no unit anywhere near it is read as millimetres by the next person
        /// to see it. When Revit will not format it, nothing is substituted.
        /// </summary>
        private static string ElevationText(Level level)
        {
            try
            {
                var p = level.get_Parameter(BuiltInParameter.LEVEL_ELEV);
                if (p == null) return null;
                var shown = p.AsValueString();
                return string.IsNullOrEmpty(shown) ? null : shown;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }

        /// <summary>
        /// How far a grid runs, as Revit would print it, or null.
        ///
        /// The row asks for "extents". A grid's curve is the honest source -
        /// its length is what a modeller sees as the line on the plan - and
        /// it is formatted through the same parameter route as everything
        /// else here rather than converted by hand.
        /// </summary>
        private static string ExtentText(Grid grid)
        {
            try
            {
                var p = grid.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                if (p == null) return null;
                var shown = p.AsValueString();
                return string.IsNullOrEmpty(shown) ? null : shown;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }

        /// <summary>
        /// The UniqueId of the level an element names, or null.
        ///
        /// Asked through the PARAMETERS rather than a property, because the
        /// parameter that carries it differs by what the element is: a wall
        /// answers on one, a family instance on another, an element scheduled
        /// by level on a third. Asking all three in turn is how an answer
        /// covers ducts and walls and equipment rather than one of them.
        ///
        /// UniqueId and never the integer: ElementId.IntegerValue is
        /// deprecated at 2024 and throws above 32 bits.
        /// </summary>
        private static string LevelKeyOf(Document doc, Element element)
        {
            var wanted = new[]
            {
                BuiltInParameter.FAMILY_LEVEL_PARAM,
                BuiltInParameter.SCHEDULE_LEVEL_PARAM,
                BuiltInParameter.LEVEL_PARAM,
                BuiltInParameter.WALL_BASE_CONSTRAINT,
            };

            foreach (var which in wanted)
            {
                try
                {
                    var p = element.get_Parameter(which);
                    if (p == null) continue;
                    var id = p.AsElementId();
                    if (id == null || id == ElementId.InvalidElementId) continue;
                    var level = doc.GetElement(id) as Level;
                    if (level != null) return level.UniqueId;
                }
                catch (Autodesk.Revit.Exceptions.ApplicationException)
                {
                    // This element does not answer that question. Try the next.
                }
            }
            return null;
        }

        /// <summary>The active view, or null when there is no UI document.</summary>
        private static View SafeView(Document doc)
        {
            try { return doc.ActiveView; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }

        /// <summary>A name, never an exception mid-answer.</summary>
        private static string SafeName(Element element)
        {
            try
            {
                return string.IsNullOrEmpty(element.Name) ? "(unnamed)" : element.Name;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return "(unnamed)";
            }
        }
    }
}
