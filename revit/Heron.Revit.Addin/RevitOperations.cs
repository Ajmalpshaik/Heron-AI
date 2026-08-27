// Heron-Agent:  HERON-REVIT-DOC-004, HERON-REVIT-SEL-008, HERON-REVIT-CAT-009
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
    /// What Heron can actually ask a model, and the only code allowed to call
    /// the Revit API. It is reached solely from
    /// <see cref="RevitDispatcher.Execute"/>, which means every method here is
    /// already on Revit's thread and inside an API context.
    ///
    /// EVERYTHING IN THIS FILE IS READ-ONLY, and nothing here opens a
    /// transaction. Counting and selecting change what is shown, never what
    /// exists, so Ctrl+Z has nothing to undo after either of them.
    ///
    /// Writing arrived at Step 6 and lives in RevitWrite, which this file
    /// routes to for any operation it does not recognise. Keeping it separate
    /// is not tidiness: it means the whole set of things that can change a
    /// model is one file a reviewer can read end to end, and it keeps the
    /// proven read path from being edited every time the write path moves.
    /// </summary>
    internal static class RevitOperations
    {
        public static string Run(UIApplication app, string request)
        {
            var op = Json.ReadString(request, "op");

            switch (op)
            {
                case "count_elements":
                    return CountElements(app);

                case "select_by_category":
                    return SelectByCategory(app, Json.ReadString(request, "category"));

                default:
                    // Step 6 added the write path. It lives in its own file so
                    // that everything able to change a model is in one place a
                    // reviewer can read end to end, rather than interleaved
                    // with the reads that are already proven.
                    var written = RevitWrite.Run(app, request, op);
                    if (written != null) return written;

                    return Json.Error("unknown_op",
                        "No handler for '" + (op ?? "(none)") + "'. " +
                        "Heron supports ping, info, count_elements, select_by_category, " +
                        "preview_move and move_elements.");
            }
        }

        /// <summary>
        /// How many elements are in the open model.
        ///
        /// The answer ALWAYS names the document it came from. A bare number is
        /// how somebody acts on a count that came from a model they were not
        /// looking at - the failure the field notes are about (docs/25).
        /// </summary>
        private static string CountElements(UIApplication app)
        {
            var doc = ActiveDocument(app);
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            // WhereElementIsNotElementType: element TYPES are definitions, not
            // things placed in the model. Counting both would give a number
            // that matches nothing a modeller can see or select.
            var count = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .GetElementCount();

            return Json.Ok(
                Json.Num("count", count),
                Json.Str("document", doc.Title),
                Json.Str("documentPath", string.IsNullOrEmpty(doc.PathName) ? null : doc.PathName),
                Json.Bool("unsaved", doc.IsModified),
                Json.Str("counts", "placed elements, excluding types"));
        }

        /// <summary>
        /// A BIM word to a Revit category. HERON-REVIT-CAT-009.
        ///
        /// A modeller says "ducts". Revit means OST_DuctCurves, and means
        /// something different by duct fittings, flex duct and duct accessories
        /// - so the mapping is explicit and its plural, singular and spacing
        /// variants are listed rather than guessed at with string trimming.
        ///
        /// Step 4 resolves ducts. The table is the mechanism; adding a row is
        /// how the rest arrive, once each has been tried against a real model.
        /// </summary>
        private static readonly Dictionary<string, BuiltInCategory> Categories =
            new Dictionary<string, BuiltInCategory>(StringComparer.OrdinalIgnoreCase)
            {
                { "duct", BuiltInCategory.OST_DuctCurves },
                { "ducts", BuiltInCategory.OST_DuctCurves },
                { "ductwork", BuiltInCategory.OST_DuctCurves },
                { "duct curves", BuiltInCategory.OST_DuctCurves },
            };

        /// <summary>
        /// Selects everything of one category in the open model.
        ///
        /// Selecting is not a model change: it alters what is highlighted, not
        /// what exists, so it needs no transaction and Ctrl+Z has nothing to
        /// undo. It is still the first time Heron does something the user can
        /// SEE, which is why it is the Phase 0 goal.
        ///
        /// Scoped to the whole document, not the active view, and the answer
        /// says so. "Selected 126 ducts" means something different in a view
        /// than in a model, and a modeller reading a number must not have to
        /// guess which was meant.
        /// </summary>
        private static string SelectByCategory(UIApplication app, string category)
        {
            BuiltInCategory builtIn;
            var unknown = ResolveCategory(category, out builtIn);
            if (unknown != null) return unknown;

            var uiDoc = app == null ? null : app.ActiveUIDocument;
            var doc = uiDoc == null ? null : uiDoc.Document;
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            var found = new FilteredElementCollector(doc)
                .OfCategory(builtIn)
                .WhereElementIsNotElementType()
                .ToElementIds();

            uiDoc.Selection.SetElementIds(found);

            return Json.Ok(
                Json.Num("selected", found.Count),
                Json.Str("category", category.Trim()),
                Json.Str("document", doc.Title),
                Json.Str("scope", "the whole model, not just the active view"));
        }

        /// <summary>
        /// A BIM word to a Revit category, or the refusal to give back.
        ///
        /// Shared with the write path on purpose. One table means "ducts"
        /// cannot mean OST_DuctCurves when Heron SELECTS and something else
        /// when it MOVES - which is the kind of divergence that is invisible
        /// in review and obvious only in the model afterwards.
        /// </summary>
        internal static string ResolveCategory(string category, out BuiltInCategory builtIn)
        {
            builtIn = BuiltInCategory.INVALID;

            if (string.IsNullOrEmpty(category))
            {
                return Json.Error("no_category",
                    "No category was given. Try 'ducts'.");
            }

            if (!Categories.TryGetValue(category.Trim(), out builtIn))
            {
                return Json.Error("unknown_category",
                    "Heron does not know the category '" + category + "' yet. " +
                    "It understands: " + string.Join(", ", Known()) + ".");
            }

            return null;
        }

        private static string[] Known()
        {
            var names = new List<string>();
            foreach (var key in Categories.Keys) names.Add(key);
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names.ToArray();
        }

        /// <summary>
        /// The document in front, resolved fresh every single time.
        ///
        /// Never cache a Document (docs/03 section 4). Revit invalidates it
        /// when the user closes or switches model, and a held reference is how
        /// a tool ends up answering about a model that was closed twenty
        /// minutes ago - or writing to one.
        ///
        /// ActiveUIDocument is null on the start screen, which is an ordinary
        /// state and not an error.
        /// </summary>
        private static Document ActiveDocument(UIApplication app)
        {
            if (app == null) return null;
            var uiDoc = app.ActiveUIDocument;
            return uiDoc == null ? null : uiDoc.Document;
        }
    }
}
