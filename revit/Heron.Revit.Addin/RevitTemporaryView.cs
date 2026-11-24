// Heron-Agent:  HERON-REVIT-VIE-013, HERON-REVIT-SEL-008
// Heron-Step:   17
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
    /// Revit's own Temporary Hide/Isolate in the active view - "isolate
    /// these", "hide these for now" - and the reset that ends it.
    /// `isolate_temporary`, `hide_temporary` and `reset_temporary`, all
    /// EXECUTE in the tool registry.
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit. It COMPILES on 2020 through 2027
    /// and that is the whole of what is known about it. NEEDS-CHECKING group
    /// ES is where it stops being a claim.
    /// ===================================================================
    ///
    /// WHY THIS IS FIXED CODE AND NOT A FRAGMENT
    /// ------------------------------------------
    /// Revit takes a temporary isolate only inside a transaction. The show
    /// door, `run_fragment_show`, opens none - that is its whole guarantee,
    /// and it is what lets it run with Changes OFF. Opening a transaction for
    /// a FRAGMENT with Changes off would be a write path with no switch in
    /// front of it: a fragment is arbitrary C#, and inside a transaction it
    /// could change anything. So the transaction is here, around exactly one
    /// of IsolateElementsTemporary, IsolateCategoriesTemporary,
    /// HideElementsTemporary, HideCategoriesTemporary or
    /// DisableTemporaryViewMode, and around nothing a caller can send.
    ///
    /// WHY IT IS EXECUTE ALTHOUGH IT OPENS A TRANSACTION
    /// ---------------------------------------------------
    /// Temporary Hide/Isolate is a display state, not a model change: the
    /// view's saved visibility, its template and every element are untouched,
    /// Revit draws the coloured border round the view to say so, and closing
    /// the model ends it. It is the sunglasses button, done for the modeller.
    /// Execute is docs/12's "changes what is shown, never what exists".
    ///
    /// WHAT IS NOT KNOWN, said rather than assumed: whether the committed
    /// transaction lands in Revit's undo list as an entry of its own
    /// (NEEDS-CHECKING ES3), whether Revit then marks the model changed - a
    /// save question on close for an isolate (ES14) - and whether a
    /// workshared view owned by somebody else takes it (ES15). All three are
    /// answered by running it, not by reading this.
    ///
    /// THE ACTIVE VIEW, AND ONLY THAT. A temporary isolate belongs to one
    /// view, and the one the modeller is looking at is the only one where
    /// "isolate these" means anything. Every answer names it.
    /// </summary>
    internal static class RevitTemporaryView
    {
        /// <summary>
        /// Isolate, in the active view, either the elements whose ids were
        /// typed or every element of the categories named - one or the other,
        /// never both.
        ///
        /// `ids` is element ids as Heron's answers print them, comma
        /// separated: Revit's number, or the long UniqueId the family form
        /// tools give back (row 5b-2024), the two mixed as the chat has them -
        /// read by RevitCapture.SplitIds, so they read here exactly as they do
        /// for a picture. A number is matched in the model by the typed-id
        /// rule (RevitFragment.ElementsWithTypedIds), a UniqueId by Revit's own
        /// Document.GetElement(string), so no ElementId is ever built from a
        /// number. `category` is the BIM words `select_by_category` takes, from
        /// the one table both use.
        ///
        /// A TEMPORARY ISOLATE ALREADY ON IS REPLACED, AND THE ANSWER SAYS SO.
        /// "Isolate these" means show these and nothing else; adding them to
        /// what an earlier isolate left would show something nobody asked for.
        /// </summary>
        public static string Isolate(UIApplication app, string ids, string category,
                                     string expectProject)
        {
            return Apply(app, ids, category, expectProject, false);
        }

        /// <summary>
        /// Hide, in the active view, the elements whose ids were typed or every
        /// element of the categories named - Revit's own Hide Element and Hide
        /// Category under Temporary Hide/Isolate, the HH and HC of the view
        /// control bar. `hide_temporary`, EXECUTE in the tool registry (row
        /// 5b-2024, 2026-11-24: "hide for temporary all the outside panels so I
        /// can see inside" could only be answered by selecting the 46 forms and
        /// asking the modeller to press HH).
        ///
        /// A TEMPORARY HIDE/ISOLATE ALREADY ON IS KEPT, AND THESE ARE ADDED TO
        /// IT - what Revit's own HH does. "Hide these too" after an isolate or
        /// an earlier hide means hide more; ending what is already hidden would
        /// show things the modeller put away. The answer says it was added to.
        ///
        /// ITS OWN OPERATION NAME, NOT A MODE OF `isolate_temporary`: an add-in
        /// built before this would read a mode it did not know as nothing and
        /// ISOLATE the very elements the modeller asked to hide. Under its own
        /// name an older add-in answers unknown_op and nothing is done.
        /// </summary>
        public static string Hide(UIApplication app, string ids, string category,
                                  string expectProject)
        {
            return Apply(app, ids, category, expectProject, true);
        }

        /// <summary>
        /// The one body behind Isolate and Hide. `hide` chooses Revit's call
        /// and the words; every check before it is the same for both.
        /// </summary>
        private static string Apply(UIApplication app, string ids, string category,
                                    string expectProject, bool hide)
        {
            var typedIds = (ids ?? "").Trim();
            var words = (category ?? "").Trim();
            var verb = hide ? "hide" : "isolate";
            var done = hide ? "hidden" : "isolated";


            // ONE OR THE OTHER. Both given is two different requests - these
            // elements, or everything of that kind - and picking one is a
            // guess that looks exactly like an answer.
            if (typedIds.Length > 0 && words.Length > 0)
            {
                return Json.Error("ambiguous_request",
                    "Both element ids and a category were given, and those are two different "
                    + "requests - these elements, or everything of that kind. NOTHING was "
                    + done + ". Send one or the other.");
            }

            if (typedIds.Length == 0 && words.Length == 0)
            {
                return Nothing(hide,
                    "Nothing was named to " + verb + ". Give element ids, comma separated as "
                    + "Heron's answers print them, or a category such as 'ducts' or "
                    + "'pipes, pipe fittings'.");
            }

            Document doc;
            View view;
            var refusal = ActiveView(app, expectProject, done, out doc, out view);
            if (refusal != null) return refusal;

            var wanted = new List<ElementId>();
            var names = new List<string>();
            var inView = -1;

            if (typedIds.Length > 0)
            {
                // TWO SHAPES OF ID, EACH READ ITS OWN WAY - the picture's rule
                // (5b-1314), from the same method, so a form's long id out of
                // REPORT_FAMILY_FORMS or CREATE_FAMILY_FORMS is taken here too.
                var typed = new List<string>();
                var unique = new List<string>();
                var problem = RevitCapture.SplitIds(typedIds, typed, unique, done);
                if (problem != null) return Json.Error("bad_request_value", problem);

                if (typed.Count > 0)
                {
                    var found = RevitFragment.ElementsWithTypedIds(doc, typed, out problem)
                                as List<ElementId>;
                    if (found == null)
                    {
                        return Json.Error("bad_request_value",
                            (problem ?? "The ids typed could not be found in this model.")
                            + " NOTHING was " + done + ".");
                    }
                    wanted.AddRange(found);
                }

                // A UNIQUEID IS REVIT'S OWN LOOKUP. One that names nothing here
                // - another family's form, the project in front instead of the
                // family, a typing slip - refuses the whole request, named, as
                // one number that names nothing does. An element named both
                // ways is counted once.
                var missing = new List<string>();
                foreach (var id in unique)
                {
                    var element = doc.GetElement(id);
                    if (element == null) { missing.Add(id); continue; }
                    if (!wanted.Contains(element.Id)) wanted.Add(element.Id);
                }
                if (missing.Count > 0)
                {
                    return Json.Error("bad_request_value",
                        "These long ids name nothing in \"" + doc.Title + "\": "
                        + string.Join(", ", missing.ToArray()) + ". NOTHING was " + done
                        + ". Give ids from an answer about the model in front.");
                }

                // A TYPE IS NOT A PLACED ELEMENT, and the typed-id rule finds
                // types too - an id an earlier answer printed may be one. A type
                // has nothing to draw, so isolating or hiding it would either
                // throw or count an element nobody can see. One refuses the
                // request, as one unknown category word does.
                var types = new List<string>();
                foreach (var id in wanted)
                    if (doc.GetElement(id) is ElementType) types.Add(id.ToString());
                if (types.Count > 0)
                {
                    return Json.Error("bad_request_value",
                        "These ids name element TYPES, not placed elements, and a type has "
                        + "nothing to " + verb + ": " + string.Join(", ", types.ToArray())
                        + ". NOTHING was " + done + ". Give the ids of placed elements.");
                }

                // HOW MANY OF THEM THIS VIEW CAN SHOW AT ALL. An element on
                // another level, or one the view's own settings hide, is
                // isolated and still not seen - and "isolated 5" over a screen
                // showing 3 reads as Heron losing two. D-52: what was asked
                // for and what can be seen are reported side by side.
                //
                // NOT COUNTED UNDER AN EARLIER ISOLATE. Whether this collector
                // honours a temporary hide is not known, and if it does, every
                // element the earlier isolate hid would count as "not in this
                // view" - an answer that blames the view's level for Revit's
                // own temporary state. Not counted is said; miscounted is not.
                if (!view.IsTemporaryHideIsolateActive())
                {
                    var shown = new HashSet<ElementId>(
                        new FilteredElementCollector(doc, view.Id).ToElementIds());
                    inView = 0;
                    foreach (var id in wanted)
                        if (shown.Contains(id)) inView++;
                }
            }
            else
            {
                // ONE UNKNOWN WORD REFUSES THE WHOLE REQUEST, for
                // select_by_category's reason: isolating three of four
                // categories looks right and is short, with nothing saying so.
                foreach (var part in words.Split(','))
                {
                    var name = part.Trim();
                    if (name.Length == 0) continue;

                    BuiltInCategory builtIn;
                    var unknown = RevitOperations.ResolveCategory(name, out builtIn);
                    if (unknown != null) return unknown;

                    var found = Category.GetCategory(doc, builtIn);
                    if (found == null)
                    {
                        return Json.Error("unknown_category",
                            "This model has no '" + name + "' category, so it cannot be "
                            + done + ". NOTHING was " + done + ".");
                    }

                    if (wanted.Contains(found.Id)) continue;
                    wanted.Add(found.Id);
                    names.Add(found.Name);
                }

                if (wanted.Count == 0)
                {
                    return Nothing(hide,
                        "No category was given. Try 'ducts', or several at once like "
                        + "'pipes, pipe fittings'.");
                }
            }

            var byCategory = names.Count > 0;
            var wasOn = view.IsTemporaryHideIsolateActive();

            // AN ISOLATE REPLACES, A HIDE ADDS - see Isolate and Hide above.
            var replaced = wasOn && !hide;

            // WHAT REVIT'S UNDO LIST WILL SAY if it lists this at all - which
            // is NEEDS-CHECKING ES's question, not something assumed here.
            var label = "Heron: " + verb + " "
                        + (byCategory ? string.Join(", ", names.ToArray()) : wanted.Count + " element(s)")
                        + " temporarily";

            string failed;
            var kept = Within(doc, label, () =>
            {
                if (replaced) view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                if (hide)
                {
                    if (byCategory) view.HideCategoriesTemporary(wanted);
                    else view.HideElementsTemporary(wanted);
                }
                else
                {
                    if (byCategory) view.IsolateCategoriesTemporary(wanted);
                    else view.IsolateElementsTemporary(wanted);
                }
            }, out failed);

            if (!kept)
            {
                return Failed(hide,
                    failed + " The view's own visibility settings were not touched.");
            }

            // WHAT REVIT SAYS NOW, not what this method meant. A call that
            // returned cleanly and left no temporary hide/isolate on is a
            // failure, and it would otherwise be reported as one that worked.
            var active = view.IsTemporaryHideIsolateActive();
            if (!active)
            {
                return Failed(hide,
                    "Revit accepted the " + verb + " in \"" + view.Name + "\" and reports no "
                    + "Temporary Hide/Isolate on afterwards, so nothing can be said to be "
                    + done + ". The view's own visibility settings were not touched.");
            }

            // documentPath BESIDE THE TITLE AND THE KEYS, for the reason the
            // reset gives below: the server's pin compares it. `isolated`
            // keeps its name for a hide too - it is the count of what the
            // request named, and the server reads it under that name - and
            // `mode` says which it was, so the answer can never call a hide an
            // isolate.
            var fields = new List<string>
            {
                Json.Str("mode", verb),
                Json.Num("isolated", wanted.Count),
                Json.Str("isolatedBy", byCategory ? "category" : "element id"),
                Json.Str("view", view.Name),
                Json.Bool("replacedEarlier", replaced),
                Json.Bool("addedToEarlier", hide && wasOn),
                Json.Str("document", doc.Title),
                Json.Str("documentPath", string.IsNullOrEmpty(doc.PathName) ? null : doc.PathName),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Str("creationGuid", RevitOperations.CreationGuid(doc)),
                Json.Str("temporary",
                    "Revit's Temporary Hide/Isolate - the view's saved visibility is untouched, "
                    + "and reset_temporary or Revit's own Reset Temporary Hide/Isolate ends it"),
            };
            if (byCategory) fields.Add(Json.Str("categories", string.Join(", ", names.ToArray())));
            if (inView >= 0) fields.Add(Json.Num("inView", inView));

            return Json.Ok(fields.ToArray());
        }

        /// <summary>
        /// The refusal when nothing was named, under the request's own code.
        /// EACH CODE TYPED WHOLE, inside its own Json.Error, never built from
        /// a verb: tests/test_failure_analysis.py checks the failure table
        /// (mcp/server/heron_failure.py) against the codes written in Json.Error
        /// calls, read as text - a code made of pieces is one it cannot see.
        /// </summary>
        private static string Nothing(bool hide, string message)
        {
            return hide ? Json.Error("nothing_to_hide", message)
                        : Json.Error("nothing_to_isolate", message);
        }

        /// <summary>Revit refused the hide or the isolate - Nothing's rule for the code.</summary>
        private static string Failed(bool hide, string message)
        {
            return hide ? Json.Error("hide_failed", message)
                        : Json.Error("isolate_failed", message);
        }

        /// <summary>
        /// End the Temporary Hide/Isolate in the active view - Revit's own
        /// Reset Temporary Hide/Isolate, by DisableTemporaryViewMode named
        /// explicitly. Not by isolating everything, which would look the same
        /// and leave the view in a temporary mode the modeller did not choose.
        ///
        /// NONE ON IS AN ANSWER, NOT A FAILURE, and no transaction is opened
        /// for it: there is nothing to end, and an undo entry for nothing is
        /// noise in somebody's undo list.
        /// </summary>
        public static string Reset(UIApplication app, string expectProject)
        {
            Document doc;
            View view;
            var refusal = ActiveView(app, expectProject, "reset", out doc, out view);
            if (refusal != null) return refusal;

            var wasOn = view.IsTemporaryHideIsolateActive();
            if (wasOn)
            {
                string failed;
                var done = Within(doc, "Heron: reset temporary hide/isolate", () =>
                    view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate),
                    out failed);
                if (!done) return Json.Error("reset_failed", failed);

                if (view.IsTemporaryHideIsolateActive())
                {
                    return Json.Error("reset_failed",
                        "Revit accepted the reset in \"" + view.Name + "\" and still reports a "
                        + "Temporary Hide/Isolate on. Use Revit's own Reset Temporary "
                        + "Hide/Isolate on the view control bar.");
                }
            }

            return Json.Ok(
                Json.Bool("reset", wasOn),
                Json.Str("view", view.Name),
                Json.Str("document", doc.Title),
                // THE PATH TOO, as every other reply the pin reads carries it:
                // on Revit 2020 to 2023, with no CreationGUID, two models made
                // from one template are told apart by nothing else.
                Json.Str("documentPath", string.IsNullOrEmpty(doc.PathName) ? null : doc.PathName),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Str("creationGuid", RevitOperations.CreationGuid(doc)),
                Json.Str("said", wasOn
                    ? "The Temporary Hide/Isolate in \"" + view.Name + "\" is off, and the view "
                      + "shows what its own settings show."
                    : "Nothing was hidden or isolated temporarily in \"" + view.Name + "\", so "
                      + "there was nothing to reset and nothing was done."));
        }

        /// <summary>
        /// The document in front and its active view, checked - or the
        /// refusal to send back, naming what was NOT done in `notDone`'s words.
        ///
        /// GOLDEN RULE 20 BEFORE THE TRANSACTION, the guard SelectByCategory
        /// carries and for its reason: a refusal that arrives after the act is
        /// a description, not a guard. `expectProject` empty means "do not
        /// check" - a first request has nothing to compare against.
        /// </summary>
        private static string ActiveView(UIApplication app, string expectProject, string notDone,
                                         out Document doc, out View view)
        {
            doc = null;
            view = null;

            var uiDoc = app == null ? null : app.ActiveUIDocument;
            doc = uiDoc == null ? null : uiDoc.Document;
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            if (!string.IsNullOrEmpty(expectProject) && RevitOperations.ProjectKey(doc) != expectProject)
            {
                return Json.Error("wrong_document",
                    "This chat has been working on another model, and the one in front of Revit "
                    + "now is \"" + doc.Title + "\". NOTHING was " + notDone + ". Say so "
                    + "explicitly if you meant to change model.");
            }

            view = doc.ActiveView;
            if (view == null)
            {
                return Json.Error("no_view",
                    "\"" + doc.Title + "\" has no active view to work in. NOTHING was "
                    + notDone + ".");
            }

            // A SHEET, A SCHEDULE OR A LEGEND HAS NO TEMPORARY HIDE/ISOLATE,
            // and Revit's own button is greyed out there. Asked first, so the
            // answer is Revit's rule in plain words rather than an exception.
            if (!view.CanUseTemporaryVisibilityModes())
            {
                return Json.Error("not_this_view",
                    "\"" + view.Name + "\" cannot hide or isolate temporarily - a sheet, a "
                    + "schedule or a legend has no Temporary Hide/Isolate. NOTHING was "
                    + notDone + ". Open a plan, section or 3D view and ask again.");
            }

            return null;
        }

        /// <summary>
        /// One transaction round one call, kept only when Revit commits it.
        /// False with Revit's own words in `failed` otherwise, and the
        /// transaction rolled back - a half-applied isolate is not a state the
        /// modeller can read off the screen.
        ///
        /// REVIT'S FAILURES ARE ANSWERED HERE, NEVER LEFT TO REVIT - the rule
        /// every transaction in revit/ keeps (tests/test_failure_note.py). An
        /// error rolls the whole thing back and is quoted; a warning is
        /// dismissed. Revit's own resolution of an error is never applied.
        /// </summary>
        private static bool Within(Document doc, string label, Action act, out string failed)
        {
            failed = null;
            var answer = new ViewOnly();
            using (var transaction = new Transaction(doc, label))
            {
                try
                {
                    transaction.Start();

                    var options = transaction.GetFailureHandlingOptions();
                    options.SetFailuresPreprocessor(answer);
                    options.SetClearAfterRollback(true);
                    transaction.SetFailureHandlingOptions(options);

                    act();

                    // A REVIT DIALOG HERON DECLINED STOPS THIS before it is
                    // kept, as it stops a move or a fragment - see
                    // HeronDialogWatch and the revit-addin-conventions skill.
                    if (HeronDialogWatch.Refused)
                    {
                        transaction.RollBack();
                        failed = HeronDialogWatch.Armed.RefusedSentence()
                                 + " The view was left as it was.";
                        return false;
                    }

                    if (transaction.Commit() == TransactionStatus.Committed) return true;
                    failed = "Revit did not accept the change to the view, so nothing was done."
                             + (answer.Error == null ? "" : " Revit said: " + answer.Error);
                    return false;
                }
                catch (Exception ex)
                {
                    if (transaction.HasStarted() && !transaction.HasEnded())
                    {
                        try { transaction.RollBack(); }
                        catch (Exception) { }
                    }
                    failed = "Revit refused it, and nothing was done. Revit said: " + ex.Message;
                    return false;
                }
            }
        }

        /// <summary>
        /// The failures preprocessor for a change to what a view shows. An
        /// error rolls back, and its words are kept for the answer; a warning
        /// is dismissed - there is nothing in a temporary isolate for one to
        /// be about that the modeller could act on.
        /// </summary>
        private sealed class ViewOnly : IFailuresPreprocessor
        {
            public string Error;

            public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
            {
                var messages = accessor.GetFailureMessages();

                // EVERY MESSAGE JUDGED BEFORE ANY WARNING IS TOUCHED, the move
                // path's order: a batch holding one error rolls back whole.
                foreach (var message in messages)
                {
                    if (message.GetSeverity() >= FailureSeverity.Error)
                    {
                        Error = message.GetDescriptionText();
                        return FailureProcessingResult.ProceedWithRollBack;
                    }
                }

                foreach (var message in messages)
                {
                    if (message.GetSeverity() == FailureSeverity.Warning)
                        accessor.DeleteWarning(message);
                }

                return FailureProcessingResult.Continue;
            }
        }
    }
}
