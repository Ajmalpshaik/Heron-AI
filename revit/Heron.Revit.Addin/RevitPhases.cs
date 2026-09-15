// Heron-Agent:  HERON-REVIT-PHS-032
// Heron-Step:   4
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Heron.Bridge;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Phases and design options. HERON-REVIT-PHS-032, whose row says the
    /// whole thing: they "change what <em>all ducts</em> even means".
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit. It COMPILES on 2020 through 2027
    /// and that is the whole of what is known about it. NEEDS-CHECKING group
    /// L is where it stops being a claim.
    /// ===================================================================
    ///
    /// WHY A MODELLER CARES, IN ONE SENTENCE
    /// --------------------------------------
    /// "412 ducts" is not a fact about a model. It is a fact about a model
    /// AND a phase AND a design option, and on a real job those three
    /// numbers differ by a lot: the Existing phase holds the survey, New
    /// Construction holds the work, and an option set called "Riser Layout"
    /// holds two complete alternative arrangements of the same shafts.
    ///
    /// Counting all of them together answers a question nobody asked.
    /// Counting one of them and calling it the model is worse.
    ///
    /// IT REPORTS THE BREAKDOWN AND ASSERTS NOTHING ABOUT COLLECTORS
    /// --------------------------------------------------------------
    /// The obvious sentence to write here is how a FilteredElementCollector
    /// treats elements in a non-primary option. That sentence is not
    /// written, because this code has never run against Revit and the
    /// compiler cannot check a claim about behaviour - only about names.
    /// D-05 is the same lesson one level down: `ElementId.IntegerValue`
    /// compiled on five releases and was written down as true of eight.
    ///
    /// So every number here is COUNTED rather than reasoned about. The
    /// answer says how many elements carry each phase and each option, and
    /// a caller comparing that with what `count_elements` returned can see
    /// for themselves what was included. Measured beats argued.
    ///
    /// NOTHING IS SET
    /// ----------------
    /// The register gives this row MODIFY, which the register's own column
    /// header defines as "the HIGHEST permission level it can require".
    /// Reading requires none of it. Setting a view's phase or activating a
    /// design option changes what every other person on the job sees in
    /// that view, and doing it from a machine that has never opened Revit
    /// is not something this file will be the first to try.
    /// </summary>
    internal static class RevitPhases
    {
        /// <summary>
        /// Every phase and design option in the active document, with a
        /// count of what is in each.
        ///
        /// The counts are the point. A list of phase names tells a modeller
        /// nothing they did not know; "Existing holds 1,204 elements and
        /// New Construction holds 3,881" tells them which number they were
        /// just given.
        /// </summary>
        public static string List(UIApplication app)
        {
            var doc = RevitOperations.ActiveDocument(app);
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            var phaseNames = new Dictionary<string, string>(StringComparer.Ordinal);
            var phaseOrder = new List<string>();
            foreach (Phase phase in doc.Phases)
            {
                if (phase == null) continue;
                var key = phase.UniqueId;
                phaseNames[key] = SafeName(phase);
                phaseOrder.Add(key);
            }

            var options = new Dictionary<string, DesignOption>(StringComparer.Ordinal);
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(DesignOption)))
            {
                var option = element as DesignOption;
                if (option == null) continue;
                options[option.UniqueId] = option;
            }

            // COUNTED, NOT REASONED ABOUT. One pass over the placed
            // elements, asking each what it actually carries.
            var created = new Dictionary<string, int>(StringComparer.Ordinal);
            var demolished = new Dictionary<string, int>(StringComparer.Ordinal);
            var inOption = new Dictionary<string, int>(StringComparer.Ordinal);
            var mainModel = 0;
            var noPhase = 0;
            var placed = 0;

            foreach (var element in new FilteredElementCollector(doc)
                                        .WhereElementIsNotElementType())
            {
                if (element == null) continue;
                placed++;

                var madeIn = KeyOf(doc, SafeCreated(element));
                if (madeIn == null) noPhase++;
                else Add(created, madeIn);

                var goneIn = KeyOf(doc, SafeDemolished(element));
                if (goneIn != null) Add(demolished, goneIn);

                var option = SafeOption(element);
                if (option == null) mainModel++;
                else Add(inOption, option.UniqueId);
            }

            var phaseRows = new List<string>();
            foreach (var key in phaseOrder)
            {
                int made, gone;
                if (!created.TryGetValue(key, out made)) made = 0;
                if (!demolished.TryGetValue(key, out gone)) gone = 0;
                phaseRows.Add(Json.Obj(
                    Json.Str("name", phaseNames[key]),
                    Json.Num("created", made),
                    Json.Num("demolished", gone)));
            }

            var optionRows = new List<string>();
            foreach (var pair in options)
            {
                int held;
                if (!inOption.TryGetValue(pair.Key, out held)) held = 0;
                optionRows.Add(Json.Obj(
                    Json.Str("name", SafeName(pair.Value)),
                    Json.Bool("primary", SafePrimary(pair.Value)),
                    Json.Str("set", SetOf(doc, pair.Value)),
                    Json.Num("elements", held)));
            }

            var view = SafeView(doc);
            return Json.Ok(
                Json.Arr("phases", phaseRows),
                Json.Arr("designOptions", optionRows),
                Json.Num("phaseCount", phaseRows.Count),
                Json.Num("designOptionCount", optionRows.Count),
                Json.Num("placedElements", placed),
                Json.Num("inMainModel", mainModel),
                Json.Num("withNoPhase", noPhase),
                Json.Str("activeView", view == null ? null : SafeName(view)),
                Json.Str("viewPhase", NameOfParameter(doc, view,
                                                      BuiltInParameter.VIEW_PHASE)),
                Json.Str("viewPhaseFilter", NameOfParameter(doc, view,
                                                            BuiltInParameter.VIEW_PHASE_FILTER)),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Str("reads",
                    phaseRows.Count <= 1 && optionRows.Count == 0
                    ? "one phase and no design options, so a count of this "
                      + "model is a count of the whole model."
                    : "EVERY NUMBER HERE WAS COUNTED, not reasoned about. "
                      + "`created` is how many elements name that phase as "
                      + "the one they were built in; `elements` under a "
                      + "design option is how many carry that option. "
                      + "inMainModel is everything carrying no option at "
                      + "all. Whether a count you were given elsewhere "
                      + "included any particular group is a question about "
                      + "THAT count - compare it with these and you can "
                      + "see. Nothing here asserts how a collector behaves, "
                      + "because this code has never run against Revit."));
        }

        private static void Add(Dictionary<string, int> into, string key)
        {
            int already;
            into[key] = into.TryGetValue(key, out already) ? already + 1 : 1;
        }

        /// <summary>
        /// The phase an element was built in, or null.
        ///
        /// Not every element has one - a level, a grid, a view are not built
        /// in a phase - and asking can throw rather than returning nothing.
        /// A null here means "this element does not answer that question",
        /// which is different from "phase zero".
        /// </summary>
        private static ElementId SafeCreated(Element element)
        {
            try { return element.CreatedPhaseId; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }

        private static ElementId SafeDemolished(Element element)
        {
            try { return element.DemolishedPhaseId; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }

        /// <summary>The design option an element sits in, or null for the main model.</summary>
        private static DesignOption SafeOption(Element element)
        {
            try { return element.DesignOption; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }

        /// <summary>
        /// Whether this is the option Revit treats as the primary one.
        ///
        /// It matters more than it sounds: the primary option is what a view
        /// shows unless somebody has said otherwise, so "the model" usually
        /// means the main model plus one option out of each set.
        /// </summary>
        private static bool SafePrimary(DesignOption option)
        {
            try { return option.IsPrimary; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
        }

        /// <summary>
        /// The option SET a design option belongs to, by name.
        ///
        /// There is no DesignOptionSet class - this was checked against the
        /// shipped assemblies for 2020 and 2027 before the line was written,
        /// and asking HERON-REVIT-ACI-034 took less time than a compile
        /// would have. A set is an element of category OST_DesignOptionSets,
        /// and an option names its set through a parameter.
        /// </summary>
        private static string SetOf(Document doc, DesignOption option)
        {
            try
            {
                var parameter = option.get_Parameter(
                    BuiltInParameter.OPTION_SET_ID);
                if (parameter == null) return null;
                var owner = doc.GetElement(parameter.AsElementId());
                return owner == null ? null : SafeName(owner);
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return null;
            }
        }

        /// <summary>The active view, or null when there is no UI document.</summary>
        private static View SafeView(Document doc)
        {
            try { return doc.ActiveView; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }

        /// <summary>
        /// The name of whatever element a view parameter points at.
        ///
        /// VIEW_PHASE and VIEW_PHASE_FILTER are what decide which elements a
        /// view draws and how it draws them, and a modeller reading "412
        /// ducts" off a plan is reading it through both.
        /// </summary>
        private static string NameOfParameter(Document doc, View view,
                                              BuiltInParameter which)
        {
            if (view == null) return null;
            try
            {
                var parameter = view.get_Parameter(which);
                if (parameter == null) return null;
                var target = doc.GetElement(parameter.AsElementId());
                return target == null ? null : SafeName(target);
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return null;
            }
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

        /// <summary>
        /// A stable key for an element id, as a string.
        ///
        /// UniqueId and not the integer, for the reason RevitLinks.KeyOf
        /// records at length: ElementId.IntegerValue is GONE on 2026 and
        /// 2027, and compiled without complaint on the five releases before
        /// them.
        /// </summary>
        private static string KeyOf(Document doc, ElementId id)
        {
            if (doc == null || id == null || id == ElementId.InvalidElementId) return null;
            var element = doc.GetElement(id);
            return element == null ? null : element.UniqueId;
        }
    }
}
