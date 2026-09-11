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
using Heron.Core;

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

            // ================= THE GATE =================
            // Every operation, one place, BEFORE any routing. This is what
            // makes docs/12 section 71 true - "risk level is declared in the
            // tool registry, not decided per call".
            //
            // It sits here rather than inside each operation for the sake of
            // the operation nobody has written yet. An operation that forgot
            // to check its own permission used to write unchecked and nothing
            // noticed; now it is refused before it is reached, because being
            // absent from the registry is a refusal rather than a default of
            // zero risk.
            //
            // GOLDEN RULE 19: the risk is looked up BY NAME. Nothing in the
            // request can influence it - a caller may ask for an operation,
            // but not say how dangerous that operation is.
            var gate = Gate(op);
            if (gate != null) return gate;

            switch (op)
            {
                case "count_elements":
                    return CountElements(app);

                case "select_by_category":
                    return SelectByCategory(app, Json.ReadString(request, "category"),
                                            Json.ReadString(request, "expectProject"));

                // D-28's executor. Reads only: it opens no transaction, so
                // Revit itself refuses anything that would change the model.
                // Its own file, for the same reason RevitWrite has one - the
                // code that runs OTHER PEOPLE'S code is worth reading in one
                // piece.
                case "run_fragment_read":
                    return RevitFragment.Run(app, request);

                // THE WRITE PATH FOR FRAGMENTS. It stays in RevitFragment
                // rather than moving to RevitWrite.cs, and that is the one
                // place this repository's "all writes in one file" rule is
                // deliberately not followed. Splitting it would mean a second
                // copy of the document choice, the contract binding and the
                // compile - identical work, exercised half as often, drifting
                // from the half that is proven. The transaction is what makes
                // it a write, and it is nine lines; the executor is four
                // hundred. The gate is unchanged and sits above this switch:
                // the risk comes from the tool registry, by name.
                case "run_fragment_write":
                    return RevitFragment.Run(app, request, true);

                default:
                    // Step 6 added the write path. It lives in its own file so
                    // that everything able to change a model is in one place a
                    // reviewer can read end to end, rather than interleaved
                    // with the reads that are already proven.
                    var written = RevitWrite.Run(app, request, op);
                    if (written != null) return written;

                    // Declared in the registry but with no handler here. That
                    // is a bug in Heron rather than a bad request, and saying
                    // so plainly is more use than repeating the op list.
                    return Json.Error("not_implemented",
                        "'" + op + "' is declared but Heron has no handler for it. " +
                        "That is a fault in Heron, not something you did.");
            }
        }

        /// <summary>
        /// Declared? Permitted? Not stopped? In that order, and all three
        /// before anything reaches a model.
        ///
        /// Returns null to let the operation proceed, or the refusal to send
        /// back instead of running it.
        /// </summary>
        private static string Gate(string op)
        {
            // 1. UNDECLARED IS REFUSED, never assumed harmless. The list of
            //    what IS available comes from the registry rather than being
            //    typed here - a hand-written copy of a table eventually
            //    disagrees with it.
            if (!HeronOperationRegistry.IsDeclared(op))
            {
                return Json.Error("unknown_op",
                    "Heron has no operation called '" + (op ?? "(none)") + "'. " +
                    "It has: " + HeronOperationRegistry.Describe() + ".");
            }

            var risk = HeronOperationRegistry.RiskOf(op);

            // 2. THE EMERGENCY STOP, and it deliberately blocks only what can
            //    CHANGE the model. Counting and selecting still work while
            //    stopped, which matters: diagnosing what went wrong is exactly
            //    what somebody does after pressing that button, and taking
            //    away the read tools at that moment would be the wrong help.
            if (HeronStop.IsStopped && risk >= HeronRisk.Modify)
            {
                return Json.Error("stopped", HeronStop.Message);
            }

            // 3. THE PERMISSION LEVEL.
            var denied = HeronPermissions.Explain(risk);
            if (denied != null) return Json.Error("write_disabled", denied);

            return null;
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
                Json.Str("projectKey", ProjectKey(doc)),
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
        ///
        /// GOLDEN RULE 20 IS CHECKED HERE, BEFORE THE SELECTION MOVES, and
        /// checking it on the other side of the wire was not enough. The MCP
        /// server compared the pin against the REPLY - which arrives after
        /// SetElementIds has already run - so switching the active document
        /// mid-conversation highlighted every duct in the wrong model and then
        /// returned a refusal saying nothing had been sent to Revit. A refusal
        /// that arrives after the act is a description, not a guard. Found by
        /// a review 2026-09-11.
        ///
        /// `expectProject` is the caller's pinned project key, or empty when
        /// the chat has not pinned one yet. Empty means "do not check": a
        /// first request has nothing to compare against, and refusing it would
        /// make the pin unobtainable.
        /// </summary>
        private static string SelectByCategory(UIApplication app, string category,
                                               string expectProject)
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

            if (!string.IsNullOrEmpty(expectProject))
            {
                var here = ProjectKey(doc);
                if (here != expectProject)
                {
                    return Json.Error("wrong_document",
                        "This chat has been working on another model, and the "
                        + "one in front of Revit now is \"" + doc.Title
                        + "\". NOTHING was selected. Say so explicitly if you "
                        + "meant to change model.");
                }
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
                Json.Str("projectKey", ProjectKey(doc)),
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
        /// <summary>
        /// The document's PROJECT KEY - what a knowledge scope is named after.
        ///
        /// heron_scope._safe_key states the contract: the key is the UniqueId
        /// of the document's own Project Information element, chosen because
        /// it is created with the document and survives save, rename and move.
        /// The add-in already computed exactly this inside RevitWrite for the
        /// preview/commit pairing and never SENT it, so the MCP server had no
        /// way to obtain one - it built a path-based key instead and the
        /// project store was named after a file name, or, in a read-only
        /// conversation where no write tool had run, was not opened at all.
        /// Found by a review 2026-09-11.
        ///
        /// NOT Document.CreationGUID, for the reason RevitWrite.DocumentKey
        /// gives at length: it does not exist in Revit 2020, and D-05 does not
        /// extrapolate a runtime table.
        ///
        /// A family document has no Project Information. Heron keeps no
        /// project knowledge for those, and null says so rather than
        /// inventing an identity they do not have.
        /// </summary>
        internal static string ProjectKey(Document doc)
        {
            if (doc == null) return null;
            var info = doc.ProjectInformation;
            return info == null ? null : info.UniqueId;
        }

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
