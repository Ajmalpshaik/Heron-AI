// Heron-Agent:  HERON-REVIT-VIE-013
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
    /// Views, view templates and what a view is actually showing.
    /// HERON-REVIT-VIE-013.
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit, and on a day another session had
    /// the only Revit open. It COMPILES on 2020 through 2027 and that is the
    /// whole of what is known about it.
    /// ===================================================================
    ///
    /// SHEETS ARE NOT HERE, AND THAT WAS A DECISION
    /// ----------------------------------------------
    /// This agent's row in docs/28 reads "Views, SHEETS, view templates,
    /// visibility", and HERON-REVIT-SHT-029's row reads "Sheets, numbering,
    /// titleblocks, revisions. Distinct from views". Both claimed sheets.
    ///
    /// The owner settled it on 2026-09-17: **the Sheet Agent owns sheets.**
    /// So this file reads views and says, for each one, whether it is placed
    /// on a sheet - because an unplaced view is a fact about the VIEW - and
    /// it says nothing about numbering, titleblocks or revisions, which are
    /// facts about the sheet and live in RevitSheets.cs.
    ///
    /// The overlapping words were left in both rows rather than edited out of
    /// one: VIE-013's row is quoted from the specification and SHT-029's is
    /// marked as a proposal, and deleting quoted text to record a decision
    /// loses the fact that the decision was ever needed.
    ///
    /// WHY A MODELLER CARES, IN ONE SENTENCE
    /// --------------------------------------
    /// A count taken in a view is a count THROUGH that view. Its template,
    /// its discipline, its detail level, its filters, its crop box and its
    /// phase each remove things before you ever see them - so "412 ducts" is
    /// a fact about a view at least as much as about a model. RevitPhases
    /// makes the same argument one layer down and this is its other half.
    ///
    /// THE THREE THINGS IT LOOKS FOR
    /// ------------------------------
    ///   A VIEW WITH NO TEMPLATE. Nobody controls what it shows. It will
    ///   drift from every other view of the same thing, and it is the usual
    ///   reason two plans of one floor disagree.
    ///
    ///   A VIEW ON NO SHEET. Not a fault - working views are supposed to
    ///   exist - but a browser with three hundred of them is a browser
    ///   nobody can find anything in, and each one still costs file size.
    ///
    ///   A TEMPLATE NOTHING USES. Set up once, never applied, and quietly
    ///   believed to be in force.
    ///
    /// None of the three is reported as an error, because none of them is
    /// one. They are reported as counts, with the reason a modeller would
    /// care attached.
    ///
    /// NOTHING IS SET
    /// ----------------
    /// The register gives this row MODIFY - the highest level it could ever
    /// require. Reading requires none of it. Applying a template, changing a
    /// discipline or turning a category off changes what every other person
    /// on the job sees in that view, and a machine that has never opened
    /// Revit will not be the first to try it.
    /// </summary>
    internal static class RevitViews
    {
        /// <summary>
        /// Every view in the active document, what governs it, and what the
        /// active view is showing through.
        /// </summary>
        public static string List(UIApplication app)
        {
            var doc = RevitOperations.ActiveDocument(app);
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            var views = new List<View>();
            var templates = new List<View>();
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(View))
                                        .WhereElementIsNotElementType())
            {
                var view = element as View;
                if (view == null) continue;
                if (IsTemplate(view)) templates.Add(view);
                else views.Add(view);
            }

            // WHICH VIEWS ARE PLACED, FROM BOTH THINGS THAT PLACE ONE.
            // A drawing view arrives on a sheet as a Viewport; a schedule
            // arrives as a ScheduleSheetInstance and carries no Viewport at
            // all. Counting only the first reports every schedule in the job
            // as unplaced, which is the kind of confident wrong answer this
            // repository keeps writing about.
            var placed = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(Viewport)))
            {
                var viewport = element as Viewport;
                if (viewport == null) continue;
                var key = KeyOf(doc, viewport.ViewId);
                if (key != null) placed[key] = true;
            }
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(ScheduleSheetInstance)))
            {
                var instance = element as ScheduleSheetInstance;
                if (instance == null) continue;
                var key = KeyOf(doc, instance.ScheduleId);
                if (key != null) placed[key] = true;
            }

            // Which templates are actually applied to something.
            var templateUsed = new Dictionary<string, int>(StringComparer.Ordinal);
            var noTemplate = 0;
            var notPlaced = 0;
            var byType = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var view in views)
            {
                var kind = TypeNameOf(view);
                int seen;
                byType[kind] = byType.TryGetValue(kind, out seen) ? seen + 1 : 1;

                var template = KeyOf(doc, TemplateIdOf(view));
                if (template == null) noTemplate++;
                else
                {
                    int used;
                    templateUsed[template] = templateUsed.TryGetValue(template, out used)
                                             ? used + 1 : 1;
                }

                if (!placed.ContainsKey(view.UniqueId)) notPlaced++;
            }

            var typeRows = new List<string>();
            foreach (var pair in byType)
            {
                typeRows.Add(Json.Obj(
                    Json.Str("type", pair.Key),
                    Json.Num("views", pair.Value)));
            }

            var templateRows = new List<string>();
            var unusedTemplates = 0;
            foreach (var template in templates)
            {
                int used;
                if (!templateUsed.TryGetValue(template.UniqueId, out used)) used = 0;
                if (used == 0) unusedTemplates++;
                templateRows.Add(Json.Obj(
                    Json.Str("name", SafeName(template)),
                    Json.Num("appliedTo", used)));
            }

            var active = SafeView(doc);
            return Json.Ok(
                Json.Arr("viewsByType", typeRows),
                Json.Arr("templates", templateRows),
                Json.Num("viewCount", views.Count),
                Json.Num("templateCount", templates.Count),
                Json.Num("viewsWithNoTemplate", noTemplate),
                Json.Num("viewsOnNoSheet", notPlaced),
                Json.Num("templatesNothingUses", unusedTemplates),
                Json.Str("activeView", active == null ? null : SafeName(active)),
                Json.Str("activeViewType", active == null ? null : TypeNameOf(active)),
                Json.Str("activeViewTemplate", active == null ? null
                         : NameOfId(doc, TemplateIdOf(active))),
                Json.Str("activeViewDiscipline", active == null ? null
                         : TextOfParameter(active, BuiltInParameter.VIEW_DISCIPLINE)),
                Json.Str("activeViewDetailLevel", active == null ? null
                         : TextOfParameter(active, BuiltInParameter.VIEW_DETAIL_LEVEL)),
                Json.Num("activeViewFilters", active == null ? 0 : FilterCount(active)),
                Json.Bool("activeViewCropped", active != null && IsCropped(active)),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Str("reads", Reads(views.Count, noTemplate, notPlaced,
                                        unusedTemplates, active)));
        }

        /// <summary>What the numbers mean, in the words a modeller would use.</summary>
        private static string Reads(int views, int noTemplate, int notPlaced,
                                    int unusedTemplates, View active)
        {
            if (views == 0)
            {
                return "This model has no views at all, which is unusual for "
                     + "anything but a container file.";
            }

            var said = new List<string>();

            if (active != null)
            {
                said.Add("Any count you take in the active view has been through "
                       + "that view's template, discipline, detail level, filters "
                       + "and crop before you see it - so a number from a view is "
                       + "a fact about the view as much as about the model");
            }

            if (noTemplate > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} view(s) have NO view template, so nothing controls what "
                  + "they show. They drift from every other view of the same "
                  + "thing, which is the usual reason two plans of one floor "
                  + "disagree", noTemplate));
            }

            if (notPlaced > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} view(s) are on no sheet. Not a fault - working views are "
                  + "supposed to exist - but they still cost file size and a "
                  + "browser nobody can find anything in", notPlaced));
            }

            if (unusedTemplates > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} view template(s) are applied to nothing. Set up once, "
                  + "never used, and quietly believed to be in force",
                    unusedTemplates));
            }

            if (said.Count == 0)
            {
                return "Every view carries a template and is placed on a sheet, "
                     + "and every template is in use.";
            }

            return string.Join(". ", said.ToArray())
                 + ". A view placed on a sheet was found from BOTH things that "
                 + "place one - a Viewport for a drawing and a "
                 + "ScheduleSheetInstance for a schedule - so schedules are not "
                 + "reported as unplaced. Nothing was changed.";
        }

        /// <summary>Is this a view template rather than a view?</summary>
        private static bool IsTemplate(View view)
        {
            try { return view.IsTemplate; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
        }

        /// <summary>The template this view uses, or null.</summary>
        private static ElementId TemplateIdOf(View view)
        {
            try { return view.ViewTemplateId; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }

        /// <summary>How many view filters are on this view.</summary>
        private static int FilterCount(View view)
        {
            try
            {
                var filters = view.GetFilters();
                return filters == null ? 0 : filters.Count;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return 0; }
            catch (InvalidOperationException) { return 0; }
        }

        /// <summary>Is the crop box on? A crop removes things before you count them.</summary>
        private static bool IsCropped(View view)
        {
            try { return view.CropBoxActive; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
        }

        /// <summary>The view's kind, as a word rather than an enum value.</summary>
        private static string TypeNameOf(View view)
        {
            try { return view.ViewType.ToString(); }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return "Unknown"; }
        }

        /// <summary>
        /// A parameter as Revit itself would print it.
        ///
        /// `AsValueString` first, for the reason RevitParameters gives at
        /// length: it is the only reading that applies the project's own
        /// settings, and the alternatives hand back a bare number.
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

        /// <summary>The name of whatever an id points at, or null.</summary>
        private static string NameOfId(Document doc, ElementId id)
        {
            if (doc == null || id == null || id == ElementId.InvalidElementId) return null;
            try
            {
                var element = doc.GetElement(id);
                return element == null ? null : SafeName(element);
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }

        /// <summary>
        /// A stable string key for an element id.
        ///
        /// UniqueId and never the integer: ElementId.IntegerValue is
        /// deprecated at 2024 and throws above 32 bits.
        /// </summary>
        private static string KeyOf(Document doc, ElementId id)
        {
            if (doc == null || id == null || id == ElementId.InvalidElementId) return null;
            try
            {
                var element = doc.GetElement(id);
                return element == null ? null : element.UniqueId;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
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
