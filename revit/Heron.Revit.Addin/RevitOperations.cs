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
                // Linked models. HERON-REVIT-LNK-015, and its own file for
                // the same reason RevitFragment has one: everything about
                // links is in one place a reviewer can read end to end.
                // Read-only - it opens no transaction and loads no link.
                case "list_links":
                    return RevitLinks.List(app);

                // Phases and design options. HERON-REVIT-PHS-032, and its
                // own file for the same reason: both of them change what
                // "all ducts" means, and every number it gives is COUNTED
                // rather than reasoned about. Read-only.
                case "list_phases":
                    return RevitPhases.List(app);

                // Duct and pipe systems, and the elements on none of them.
                // HERON-REVIT-SYS-030, its own file for the same reason
                // again: an element connected to nothing is missing from
                // every total downstream and nothing else says so. It
                // COUNTS rather than judging - the end of a run is an open
                // connector and is supposed to be. Read-only.
                case "list_systems":
                    return RevitSystems.List(app);

                // Instance, type and shared parameters. HERON-REVIT-PAR-011,
                // its own file for the same reason as the three above: the
                // two ways a parameter answer goes silently wrong - reading
                // only the instance when the data sits on the type, and
                // handing back a bare decimal-feet number - are both decided
                // in one place a reviewer can read end to end. Read-only.
                case "read_parameters":
                    return RevitParameters.Read(app,
                                                Json.ReadString(request, "category"),
                                                Json.ReadString(request, "parameter"),
                                                Json.ReadString(request, "expectProject"));

                // Groups and assemblies. HERON-REVIT-GRP-033, and it is the
                // pre-flight for the write path already in this file: a move
                // of a group member returns CLEANLY and shifts nothing, so
                // RevitWrite compares positions either side and then says
                // "almost certainly inside a group". This is how that stops
                // being a guess, and stops being afterwards. Read-only.
                case "list_groups":
                    return RevitGroups.List(app,
                                            Json.ReadString(request, "category"),
                                            Json.ReadString(request, "expectProject"));

                // Levels and grids. HERON-REVIT-LVL-027, its own file for the
                // same reason as the four above: everything else in the model
                // hangs off these, and the three states that actually cost a
                // day - two levels at one elevation, a level with nothing on
                // it, names that sort out of order - are decided in one place
                // a reviewer can read end to end. Read-only.
                case "list_levels":
                    return RevitLevels.List(app);

                // Worksets, ownership and checkout. HERON-REVIT-WRK-014, and
                // its own file because the row's last sentence - "ownership
                // failure is a normal outcome" - decides the whole voice of
                // the answer. Somebody else owning most of the model is the
                // system working. Ownership is SAMPLED and the sample is
                // stated, because asking the central model is one round trip
                // per element on Revit's own thread. Read-only.
                case "list_worksets":
                    return RevitWorksets.List(app);

                // Views, templates and what a view is showing THROUGH.
                // HERON-REVIT-VIE-013. A count taken in a view has been
                // through its template, discipline, detail level, filters and
                // crop before anybody sees it - the other half of the
                // argument list_phases makes. Read-only.
                case "list_views":
                    return RevitViews.List(app);

                // Sheets, numbering, titleblocks and revisions.
                // HERON-REVIT-SHT-029, which OWNS sheets - VIE-013's row
                // claimed them too and the owner settled it on 2026-09-17.
                // The sheets are the deliverable, and a numbered blank one is
                // invisible in the Project Browser until it prints. Read-only.
                case "list_sheets":
                    return RevitSheets.List(app);

                // Rooms, MEP spaces and areas. HERON-REVIT-RM-028. Rooms are
                // the architect's and spaces are the engineer's, and they go
                // out of step whenever a partition moves - so both are
                // reported side by side. UNPLACED and NOT ENCLOSED are counted
                // separately because they look identical in a schedule and are
                // different jobs to fix. Read-only.
                case "list_rooms":
                    return RevitRooms.List(app);

                // Schedules. HERON-REVIT-SCH-026 - "the way BIM people
                // actually extract data", and the answer that leaves the
                // office. A schedule is a FILTERED VIEW: its category, phase
                // and filters each remove rows silently, so every row carries
                // what the model holds in that category beside it. The two
                // numbers together are the answer. Read-only; the ROWS are
                // the export agent's job, at PUBLISH risk.
                case "list_schedules":
                    return RevitSchedules.List(app);

                // Families, types and placement. HERON-REVIT-FAM-012, and the
                // row's word "loading" is exactly what it will NOT do: a
                // family carries its own materials and loading one overwrites
                // the project's - six families once reset the pipe colour on a
                // whole job silently, with no count moving. Types and
                // instances are reported separately, because "how many of
                // these are there" has two answers in Revit. Read-only.
                case "list_families":
                    return RevitFamilies.List(app);

                // Export READINESS. HERON-REVIT-EXP-018, the one row here at
                // PUBLISH risk - and nothing leaves the model. Once a file has
                // left it has left: no undo, and somebody may already be
                // building from it. So this answers "would an export of this be
                // worth sending", which is the question nothing in Revit
                // answers until the file is already written. The doing half
                // needs a destination, an overwrite decision and a person who
                // meant it, and those rails do not exist yet.
                case "check_export":
                    return RevitExport.Check(app);

                // What has been brought in from outside. HERON-REVIT-IMP-019.
                // LINKED and IMPORTED are counted separately because confusing
                // them is the defect: an import is copied INTO the model and
                // never leaves, bringing its layers, line patterns and text
                // styles permanently - and deleting it does not remove them.
                // The two look identical in the drawing area. Read-only.
                case "list_imports":
                    return RevitImports.List(app);

                // Dimensions, tags, text and keynotes. HERON-REVIT-DIM-031.
                // The thing it exists to find is an OVERRIDDEN dimension - one
                // typed over with text, that says 2400 on a wall that is 2100,
                // survives every model change, is checked by nobody because it
                // looks correct, and gets built. Nothing in Revit lists them.
                // Read-only.
                case "list_annotation":
                    return RevitAnnotation.List(app);

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

                // PIPES, ADDED 2026-09-16 UNDER THE RULE ABOVE - tried against
                // a real model first, not added because it looked obvious.
                // `select-by-category-name` was run against
                // 4355-BHVD-3D-50C10-BL001A (26,589 elements, Revit 2020) in
                // view {3D - ajmal.al} and returned 120 elements with
                // `resolvedTo: Pipes` and 0 near misses.
                //
                // IT WAS THE INCONSISTENCY THAT MADE THIS URGENT rather than
                // the missing feature: the FRAGMENTS resolve categories a
                // different way and always could, so a modeller was told
                // "Heron does not know the category 'pipes'" by a system that
                // had just counted 120 of them in the model in front of them.
                // FRAGMENT-ISSUES row 110.
                //
                // OST_PipeCurves is the PIPE, not its fittings or accessories
                // - the same distinction OST_DuctCurves draws above, and the
                // reason a count here will not match a Pipe Fittings schedule.
                { "pipe", BuiltInCategory.OST_PipeCurves },
                { "pipes", BuiltInCategory.OST_PipeCurves },
                { "pipework", BuiltInCategory.OST_PipeCurves },
                { "pipe curves", BuiltInCategory.OST_PipeCurves },

                // THE REST OF A RUN. A modeller asking for "the pipes" and
                // getting only OST_PipeCurves gets the straights and none of
                // the bends, valves or flexes - which is why selecting several
                // at once exists at all. Added 2026-09-16 with the comma
                // support above, for "pipes, pipe fittings".
                //
                // EACH IS VERIFIED BY RESOLUTION RATHER THAN BY COUNT. A model
                // with no pipe accessories is not evidence the row is wrong,
                // so what is checked is that the word resolves to the category
                // asked for - the same thing `resolvedTo: Pipes` established
                // for the rows above.
                { "pipe fitting", BuiltInCategory.OST_PipeFitting },
                { "pipe fittings", BuiltInCategory.OST_PipeFitting },
                { "fittings", BuiltInCategory.OST_PipeFitting },
                { "pipe accessory", BuiltInCategory.OST_PipeAccessory },
                { "pipe accessories", BuiltInCategory.OST_PipeAccessory },
                { "valves", BuiltInCategory.OST_PipeAccessory },
                { "flex pipe", BuiltInCategory.OST_FlexPipeCurves },
                { "flex pipes", BuiltInCategory.OST_FlexPipeCurves },
                { "pipe insulation", BuiltInCategory.OST_PipeInsulations },

                { "duct fitting", BuiltInCategory.OST_DuctFitting },
                { "duct fittings", BuiltInCategory.OST_DuctFitting },
                { "duct accessory", BuiltInCategory.OST_DuctAccessory },
                { "duct accessories", BuiltInCategory.OST_DuctAccessory },
                { "dampers", BuiltInCategory.OST_DuctAccessory },
                { "flex duct", BuiltInCategory.OST_FlexDuctCurves },
                { "flex ducts", BuiltInCategory.OST_FlexDuctCurves },
                { "duct insulation", BuiltInCategory.OST_DuctInsulations },
                { "air terminals", BuiltInCategory.OST_DuctTerminal },
                { "air terminal", BuiltInCategory.OST_DuctTerminal },
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
            // SEVERAL CATEGORIES, COMMA SEPARATED - "pipes, pipe fittings".
            //
            // WHY THIS IS HERE AND NOT IN A FRAGMENT. `select-by-categories`
            // already takes a list and works, but it is a FRAGMENT: it leaves
            // its elements in the chain, and `set-selection` - the only thing
            // that can put them where Revit's own selection can be seen -
            // needs them FROM that chain, which is reset between calls. So the
            // one job a modeller actually asks for, "show me the pipes and the
            // fittings", could not be done at all. Measured 2026-09-16.
            // docs/36 is the general fix; this is the narrow one that does not
            // wait for it.
            //
            // ONE UNKNOWN NAME REFUSES THE WHOLE REQUEST. Selecting three of
            // four categories and reporting a total is the failure this repo
            // exists to avoid: the number looks right, the selection is short,
            // and nothing says so. The refusal names the word that failed.
            var wanted = new List<BuiltInCategory>();
            var names = new List<string>();

            // NULL IS NOT A CATEGORY, AND IT USED TO BE A CRASH. Json.ReadString
            // returns null for an absent key, so a request with no `category`
            // at all reached Split and threw - caught by the dispatcher, which
            // is right, and shown to the modeller as "Object reference not set
            // to an instance of an object." The empty string was handled
            // beautifully twenty lines below, by the refusal written for
            // exactly this case. Treating null as empty is what lets the
            // request reach it. FRAGMENT-ISSUES section 5b, row 15.
            foreach (var part in (category ?? "").Split(','))
            {
                var name = part.Trim();
                if (name.Length == 0) continue;

                BuiltInCategory one;
                var refusal = ResolveCategory(name, out one);
                if (refusal != null) return refusal;

                // A NAME REPEATED, OR TWO WORDS FOR ONE CATEGORY ("pipe" and
                // "pipes"), MUST NOT SELECT IT TWICE. The ids would be
                // duplicated in the count and Revit would be handed the same
                // element more than once.
                if (!wanted.Contains(one))
                {
                    wanted.Add(one);
                    names.Add(name);
                }
            }

            if (wanted.Count == 0)
            {
                return Json.Error("no_category",
                    "No category was given. Try 'ducts', or several at once "
                    + "like 'pipes, pipe fittings'.");
            }

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

            // ONE COLLECTOR PER CATEGORY, UNIONED. An element cannot be in two
            // Revit categories, so the union needs no de-duplication - but the
            // PER-CATEGORY counts are kept and reported, because "selected 231"
            // across three categories hides the one that returned nothing, and
            // that is usually the interesting one.
            var found = new List<ElementId>();
            var breakdown = new List<string>();

            for (int i = 0; i < wanted.Count; i++)
            {
                var these = new FilteredElementCollector(doc)
                    .OfCategory(wanted[i])
                    .WhereElementIsNotElementType()
                    .ToElementIds();

                found.AddRange(these);
                breakdown.Add(names[i] + " " + these.Count);
            }

            uiDoc.Selection.SetElementIds(found);

            return Json.Ok(
                Json.Num("selected", found.Count),
                Json.Str("category", category.Trim()),
                Json.Str("breakdown", string.Join(", ", breakdown.ToArray())),
                Json.Num("categories", wanted.Count),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", ProjectKey(doc)),
                Json.Str("scope", "the whole model, not just the active view"));
        }

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

        /// <summary>
        /// A BIM word to a Revit category, or the refusal to give back.
        ///
        /// Shared with the write path on purpose. One table means "ducts"
        /// cannot mean OST_DuctCurves when Heron SELECTS and something else
        /// when it MOVES - which is the kind of divergence that is invisible
        /// in review and obvious only in the model afterwards.
        ///
        /// This paragraph sat above ProjectKey until 2026-09-21, stacked as a
        /// second summary on an unrelated method while the one it describes
        /// carried none - in a repository where the comment is the design
        /// record. FRAGMENT-ISSUES section 5b, row 16.
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
        internal static Document ActiveDocument(UIApplication app)
        {
            if (app == null) return null;
            var uiDoc = app.ActiveUIDocument;
            return uiDoc == null ? null : uiDoc.Document;
        }
    }
}
