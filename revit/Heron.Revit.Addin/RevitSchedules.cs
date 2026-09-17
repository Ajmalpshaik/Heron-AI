// Heron-Agent:  HERON-REVIT-SCH-026
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
    /// Schedules. HERON-REVIT-SCH-026, whose row calls them "the way BIM
    /// people actually extract data".
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit, and on a day another session had
    /// the only Revit open. It COMPILES on 2020 through 2027 and that is the
    /// whole of what is known about it.
    /// ===================================================================
    ///
    /// WHY A MODELLER CARES, IN ONE SENTENCE
    /// --------------------------------------
    /// A schedule is the model's answer to a question, and it is the answer
    /// that leaves the office. It gets exported, priced, ordered from and
    /// argued over - so what a schedule LEAVES OUT matters more than what it
    /// contains, and what it leaves out is invisible on the schedule itself.
    ///
    /// THE THING THIS EXISTS TO SAY: A SCHEDULE IS A FILTERED VIEW
    /// ------------------------------------------------------------
    /// Every schedule has a category, a phase, and usually a filter. Any of
    /// the three silently removes rows. "48 doors" off a door schedule is
    /// not the number of doors in the model; it is the number of doors of
    /// that category, on that phase, that pass that filter.
    ///
    /// That is the same argument list_phases and list_views make, one layer
    /// further out, and it is the third place it has had to be made - which
    /// is itself the finding. So this reports, for every schedule, HOW MANY
    /// FILTERS it carries and which phase it is on, next to the count of
    /// what the model holds in that category. **The two numbers side by side
    /// are the answer**; neither on its own is.
    ///
    /// WHAT IT DOES NOT DO: IT DOES NOT READ THE ROWS
    /// -----------------------------------------------
    /// Exporting a schedule's contents is HERON-REVIT-EXP-018's job, and it
    /// is genuinely a different job: the rows are potentially enormous, the
    /// formatting is the point, and it belongs on the export path with a
    /// PUBLISH risk rather than on a read. This says what schedules EXIST,
    /// what governs them, and where they are - not what is in them.
    ///
    /// KEY SCHEDULES AND TAKEOFFS ARE SCHEDULES TOO
    /// ----------------------------------------------
    /// `ViewSchedule` covers the ordinary ones, material takeoffs, key
    /// schedules and the sheet list alike. They are reported together with a
    /// flag rather than filtered out, because a job whose "schedule count"
    /// silently excluded its material takeoffs would be wrong in the
    /// direction nobody checks.
    ///
    /// NOTHING IS SET
    /// ----------------
    /// The register gives this row MODIFY - the highest level it could ever
    /// require. Reading requires none of it. Adding a field or changing a
    /// filter changes what everybody downstream is pricing from.
    /// </summary>
    internal static class RevitSchedules
    {
        /// <summary>
        /// Every schedule in the active document, what governs it, and
        /// whether it is on a sheet.
        /// </summary>
        public static string List(UIApplication app)
        {
            var doc = RevitOperations.ActiveDocument(app);
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            var schedules = new List<ViewSchedule>();
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(ViewSchedule))
                                        .WhereElementIsNotElementType())
            {
                var schedule = element as ViewSchedule;
                if (schedule == null) continue;
                if (IsTemplate(schedule)) continue;
                schedules.Add(schedule);
            }

            if (schedules.Count == 0)
            {
                return Json.Ok(
                    Json.Arr("schedules", new List<string>()),
                    Json.Num("scheduleCount", 0),
                    Json.Str("document", doc.Title),
                    Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                    Json.Bool("modifiedAnything", false),
                    Json.Str("reads",
                        "This model has no schedules. Nothing is extracting "
                      + "data out of it yet, which is worth knowing before "
                      + "anybody asks for a quantity from it."));
            }

            // Which schedules are placed on a sheet. A schedule arrives on a
            // sheet as a ScheduleSheetInstance and never as a Viewport - the
            // same fact RevitViews.cs has to know from the other direction.
            var placed = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(ScheduleSheetInstance)))
            {
                var instance = element as ScheduleSheetInstance;
                if (instance == null) continue;
                var target = doc.GetElement(instance.ScheduleId);
                if (target != null) placed[target.UniqueId] = true;
            }

            // ONE PASS OVER THE MODEL, NOT ONE PER SCHEDULE.
            //
            // The first draft asked "how many elements are in this category"
            // inside the per-schedule loop, which is a full sweep of the
            // document for every schedule - fifty schedules on a real job is
            // millions of iterations, on REVIT'S OWN THREAD, which the
            // conventions forbid outright because a slow handler freezes the
            // model for whoever is working in it. One sweep, then a lookup.
            var byCategory = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var element in new FilteredElementCollector(doc)
                                        .WhereElementIsNotElementType())
            {
                if (element == null) continue;
                var key = CategoryKeyOf(element);
                if (key == null) continue;
                long already;
                byCategory[key] = byCategory.TryGetValue(key, out already)
                                  ? already + 1 : 1;
            }

            var rows = new List<string>();
            var filtered = 0;
            var notOnASheet = 0;
            var takeoffs = 0;

            foreach (var schedule in schedules)
            {
                var filters = FilterCount(schedule);
                if (filters > 0) filtered++;

                var onSheet = placed.ContainsKey(schedule.UniqueId);
                if (!onSheet) notOnASheet++;

                var takeoff = IsMaterialTakeoff(schedule);
                if (takeoff) takeoffs++;

                var category = CategoryNameOf(doc, schedule);

                rows.Add(Json.Obj(
                    Json.Str("name", SafeName(schedule)),
                    Json.Str("category", category),
                    Json.Num("filters", filters),
                    Json.Num("fields", FieldCount(schedule)),
                    Json.Str("phase", TextOfParameter(schedule,
                                          BuiltInParameter.VIEW_PHASE)),
                    Json.Bool("materialTakeoff", takeoff),
                    Json.Bool("onASheet", onSheet),
                    Json.Num("elementsInCategory",
                             Held(byCategory, ScheduleCategoryKey(schedule)))));
            }

            return Json.Ok(
                Json.Arr("schedules", rows),
                Json.Num("scheduleCount", rows.Count),
                Json.Num("schedulesWithFilters", filtered),
                Json.Num("schedulesOnNoSheet", notOnASheet),
                Json.Num("materialTakeoffs", takeoffs),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Str("reads", Reads(rows.Count, filtered, notOnASheet)));
        }

        /// <summary>What the numbers mean, in the words a modeller would use.</summary>
        private static string Reads(int schedules, int filtered, int notOnASheet)
        {
            var said = new List<string>();

            said.Add(string.Format(CultureInfo.InvariantCulture,
                "{0} schedule(s). A schedule is a FILTERED VIEW, so a number "
              + "read off one is the count of that category, on that phase, "
              + "that passes that filter - not the count in the model. "
              + "`elementsInCategory` beside each one is what the model holds "
              + "in that category, so the two numbers together say what is "
              + "being left out", schedules));

            if (filtered > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} of them carry at least one filter, which is where a "
                  + "row disappears without anybody seeing it go", filtered));
            }

            if (notOnASheet > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} are on no sheet. Not a fault - a working schedule is "
                  + "a normal thing - but it will not be in the issued set",
                    notOnASheet));
            }

            return string.Join(". ", said.ToArray())
                 + ". The ROWS are not read here: exporting a schedule's "
                 + "contents belongs to the export agent, at PUBLISH risk, and "
                 + "is a different job from saying which schedules exist. "
                 + "Nothing was changed.";
        }

        /// <summary>
        /// How many elements the model holds in this schedule's category,
        /// looked up from the single sweep taken above.
        ///
        /// THE NUMBER TO PUT BESIDE THE SCHEDULE, and the reason this agent
        /// is worth having. It is deliberately NOT the schedule's row count -
        /// it is what the category holds BEFORE the schedule's phase and
        /// filters have had their say, so the difference between the two is
        /// visible instead of invisible.
        ///
        /// Zero where the category cannot be resolved - a key schedule or a
        /// sheet list has no model category in the ordinary sense - and the
        /// answer carries the category NAME beside it, so a reader can tell
        /// that case from a genuinely empty one.
        /// </summary>
        private static long Held(Dictionary<string, long> byCategory, string key)
        {
            if (key == null) return 0;
            long held;
            return byCategory.TryGetValue(key, out held) ? held : 0;
        }

        /// <summary>
        /// A category as a string key.
        ///
        /// The id spelled as a string rather than kept as an ElementId,
        /// because ElementId.IntegerValue is deprecated at 2024 and the
        /// built-in category enums went 64-bit in the same release. A string
        /// key has nothing in it for Autodesk to move.
        /// </summary>
        private static string CategoryKeyOf(Element element)
        {
            try
            {
                var category = element.Category;
                return category == null ? null : category.Name;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }

        /// <summary>The same key, from the schedule's side.</summary>
        private static string ScheduleCategoryKey(ViewSchedule schedule)
        {
            try
            {
                var definition = schedule.Definition;
                if (definition == null) return null;
                var id = definition.CategoryId;
                if (id == null || id == ElementId.InvalidElementId) return null;
                var category = Category.GetCategory(schedule.Document, id);
                return category == null ? null : category.Name;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
            catch (InvalidOperationException) { return null; }
        }

        /// <summary>The schedule's category, as a word, or null.</summary>
        private static string CategoryNameOf(Document doc, ViewSchedule schedule)
        {
            try
            {
                var definition = schedule.Definition;
                if (definition == null) return null;
                var id = definition.CategoryId;
                if (id == null || id == ElementId.InvalidElementId) return null;
                var category = Category.GetCategory(doc, id);
                return category == null ? null : category.Name;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
            catch (InvalidOperationException) { return null; }
        }

        /// <summary>How many filters this schedule carries.</summary>
        private static int FilterCount(ViewSchedule schedule)
        {
            try
            {
                var definition = schedule.Definition;
                if (definition == null) return 0;
                var filters = definition.GetFilters();
                return filters == null ? 0 : filters.Count;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return 0; }
            catch (InvalidOperationException) { return 0; }
        }

        /// <summary>How many fields (columns) it shows.</summary>
        private static int FieldCount(ViewSchedule schedule)
        {
            try
            {
                var definition = schedule.Definition;
                if (definition == null) return 0;
                return definition.GetFieldCount();
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return 0; }
            catch (InvalidOperationException) { return 0; }
        }

        /// <summary>Is this a material takeoff rather than an ordinary schedule?</summary>
        private static bool IsMaterialTakeoff(ViewSchedule schedule)
        {
            try
            {
                var definition = schedule.Definition;
                return definition != null && definition.IsMaterialTakeoff;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
            catch (InvalidOperationException) { return false; }
        }

        /// <summary>Is this a schedule template rather than a schedule?</summary>
        private static bool IsTemplate(ViewSchedule schedule)
        {
            try { return schedule.IsTemplate; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
        }

        /// <summary>
        /// A parameter as Revit itself would print it, in the project's own
        /// settings. `AsValueString` first, for the reason RevitParameters
        /// gives at length.
        /// </summary>
        private static string TextOfParameter(View view, BuiltInParameter which)
        {
            try
            {
                var p = view.get_Parameter(which);
                if (p == null) return null;
                var shown = p.AsValueString();
                return string.IsNullOrEmpty(shown) ? null : shown;
            }
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
