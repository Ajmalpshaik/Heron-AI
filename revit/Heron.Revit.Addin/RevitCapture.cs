// Heron-Agent:  HERON-REVIT-VIE-013
// Heron-Step:   6
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Heron.Bridge;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// A picture of one view, handed back to the chat - capture_view, D-123.
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit. It COMPILES on 2020 through 2027
    /// and that is the whole of what is known about it. NEEDS-CHECKING group
    /// ER is the run that would say more.
    /// ===================================================================
    ///
    /// WHAT IT IS FOR. A chat that has just placed a duct, built a family or
    /// filled a sheet can count what it made, and cannot SEE it. A count says
    /// 12 sprinklers; a picture says they are all in one corner. So this
    /// takes the picture and the MCP tool revit_view_image hands it to the
    /// chat as an image, for the chat to look at its own work.
    ///
    /// TWO WAYS TO SAY WHAT TO PICTURE
    /// --------------------------------
    ///   A VIEW, BY NAME. Exported as it stands: its template, its crop, its
    ///   filters, exactly what the modeller sees. No name means the view in
    ///   front. Nothing about the view is changed.
    ///
    ///   ELEMENTS, BY ID - Revit's number, or the long UniqueId the family
    ///   form tools answer with (5b-1314), the two mixed as the chat has
    ///   them. A temporary isometric 3D view, a section box round
    ///   the elements and a margin, and nothing in it but the model - every
    ///   annotation category and every datum hidden, because a level line
    ///   across a picture of three ducts is noise. Datums and link instances
    ///   given as ids are LEFT OUT OF THE BOX rather than refused: a level's
    ///   box runs the whole width of the model, and framing it would picture
    ///   the building instead of the elements.
    ///
    /// WHY IT IS ANALYZE AND OPENS A TRANSACTION
    /// -------------------------------------------
    /// The second way needs a new view, and Revit makes nothing outside a
    /// transaction. Every other Analyze operation opens none, and the read
    /// executor's guarantee is exactly that. This one's guarantee is a
    /// different one, and it is said here rather than implied: the
    /// TransactionGroup around the work is ALWAYS ROLLED BACK. There is no
    /// apply flag - nothing a request carries can keep the view - and the
    /// rollback sits in a finally a reviewer can read in one screen. That is
    /// why it is fixed C# here and not a fragment: a fragment's rollback is
    /// the executor's default, and the executor also knows how to keep.
    ///
    /// The first way opens a group too, though it makes nothing: if an export
    /// ever changed something in the document, the group would put it back.
    /// A read-only document cannot start one, so there it exports without.
    ///
    /// WHERE THE PICTURE GOES
    /// -----------------------
    /// Revit can only export to a file. The file goes in its own folder under
    /// HeronPaths.Captures - Heron's own Derived folder, never a place anybody
    /// else reads - is read back, and the folder is deleted before this
    /// answers. The picture travels as base64 IN THE REPLY, never as a path:
    /// under the Microsoft Store Claude app, Python and Revit see different
    /// AppData folders (FRAGMENT-ISSUES 5b-416 to 5b-419), so a path written
    /// by one is not found by the other.
    ///
    /// AND THE EMERGENCY STOP STILL STOPS IT. Stopped means Heron opens no
    /// transaction, so the elements way is refused while stopped; a named
    /// view opens nothing that writes and is still answered.
    /// </summary>
    internal static class RevitCapture
    {
        /// <summary>The width asked for when none is, in pixels.</summary>
        private const int DefaultWidth = 1280;
        private const int NarrowestWidth = 320;
        private const int WidestWidth = 2400;

        /// <summary>
        /// The largest picture sent back, in bytes before base64. Base64 is
        /// four characters for every three bytes, so this keeps the encoded
        /// image under 5 MB - the per-image limit the Claude API documents. A
        /// picture over it is refused with the width to ask for instead,
        /// rather than sent and dropped by the host with no reason given.
        /// </summary>
        private const int LargestPicture = 3750000;

        /// <summary>
        /// The space round the elements, as a share of the box's longest side,
        /// and never less than one foot (internal units). Without a margin the
        /// section box cuts through the very faces the picture is meant to show.
        /// </summary>
        private const double MarginShare = 0.10;
        private const double SmallestMargin = 1.0;

        private const string Label = "Heron: a picture for the chat (always undone)";

        public static string Capture(UIApplication app, string request)
        {
            var uiDoc = app == null ? null : app.ActiveUIDocument;
            var doc = uiDoc == null ? null : uiDoc.Document;
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again. Nothing was pictured.");
            }

            // THE PIN, BEFORE ANYTHING IS MADE - the same guard and the same
            // reason as select_by_category: a refusal that arrives after the
            // act is a description. Empty means this chat has not pinned a
            // model yet, and there is nothing to compare.
            var expectProject = Json.ReadString(request, "expectProject");
            if (!string.IsNullOrEmpty(expectProject)
                && RevitOperations.ProjectKey(doc) != expectProject)
            {
                return Json.Error("wrong_document",
                    "This chat has been working on another model, and the one in front of "
                    + "Revit now is \"" + doc.Title + "\". NOTHING was pictured. Say so "
                    + "explicitly if you meant to change model.");
            }

            var viewName = (Json.ReadString(request, "view") ?? "").Trim();
            var idText = (Json.ReadString(request, "elementIds") ?? "").Trim();
            if (viewName.Length > 0 && idText.Length > 0)
            {
                return Json.Error("both_given",
                    "Give a view name OR element ids, not both - one picture is one of the "
                    + "two. Nothing was pictured.");
            }

            var asked = Json.ReadLong(request, "pixelWidth");
            var width = asked.HasValue ? (int)Math.Max(NarrowestWidth,
                                         Math.Min(WidestWidth, asked.Value)) : DefaultWidth;
            var clamped = asked.HasValue && asked.Value != width;

            return idText.Length > 0
                ? OfElements(doc, idText, width, clamped)
                : OfView(uiDoc, doc, viewName, width, clamped);
        }

        // ------------------------------------------------------- a named view

        private static string OfView(UIDocument uiDoc, Document doc, string name,
                                     int width, bool clamped)
        {
            string problem = null;
            var view = name.Length == 0 ? uiDoc.ActiveView : ViewNamed(doc, name, out problem);
            if (name.Length > 0 && view == null) return problem;
            if (view == null)
            {
                return Json.Error("no_view",
                    "Revit has no view in front to picture. Name a view, and try again.");
            }

            if (view.IsTemplate || !view.CanBePrinted)
            {
                return Json.Error("not_printable",
                    "'" + view.Name + "' cannot be exported as a picture - "
                    + (view.IsTemplate ? "it is a view template" : "Revit cannot print a view of that kind")
                    + ". Name a plan, section, elevation, 3D view or sheet. Nothing was pictured.");
            }

            // NO GROUP ON A READ-ONLY DOCUMENT, because Revit will not start
            // one there - and nothing here makes anything a group would need
            // to undo. See the class summary.
            //
            // AND WATCHED WHERE THERE IS ONE, like every write job's group: a
            // capture is never kept, but its rollback must not take the last
            // kept job's elements with it any more than a preview's may - see
            // RevitFragment.RollBackAndCheck, which every rollback here goes
            // through.
            //
            // THE NAME IS READ BEFORE THE GROUP, because the catch below runs
            // after Exported has rolled it back, and nothing here reads an
            // element after a rollback (5b-1090).
            var viewName = view.Name;
            using (var watch = doc.IsReadOnly ? null : RevitFragment.Watch(doc, Label, Label))
            {
                var group = doc.IsReadOnly ? null : new TransactionGroup(doc, Label);
                HeronRollbackWitness.Outcome after = null;
                try
                {
                    if (group != null) group.Start();
                    return Exported(doc, view, width, clamped, "view", null, 0, ref after, group, watch);
                }
                catch (Exception ex)
                {
                    // Exported rolls the group back in its own finally before an
                    // exception leaves it; asked again, the helper gives the same
                    // answer, and a group Start never began counts as undone.
                    if (group != null) after = RevitFragment.RollBackAndCheck(group, doc, watch);
                    return Json.Error("capture_failed",
                        "Revit could not picture '" + viewName + "': " + ex.Message + ". "
                        + Undone(after));
                }
                finally
                {
                    if (group != null) group.Dispose();
                }
            }
        }

        /// <summary>
        /// The view with this name, or null and the refusal to give back.
        ///
        /// Revit lets a floor plan and a ceiling plan share a name, so a name
        /// that matches two is refused with each one written the second way
        /// this accepts - "CeilingPlan: Level 1", the view type as Revit spells
        /// it - so the chat can copy one back exactly. Templates are never a
        /// match: a template is a set of settings, not something to look at.
        /// </summary>
        private static View ViewNamed(Document doc, string name, out string problem)
        {
            problem = null;
            var exact = new List<View>();
            var anyCase = new List<View>();
            foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
            {
                var view = element as View;
                if (view == null || view.IsTemplate) continue;

                var typed = view.ViewType.ToString() + ": " + view.Name;
                if (string.Equals(view.Name, name, StringComparison.Ordinal)
                    || string.Equals(typed, name, StringComparison.Ordinal))
                    exact.Add(view);
                else if (string.Equals(view.Name, name, StringComparison.OrdinalIgnoreCase)
                         || string.Equals(typed, name, StringComparison.OrdinalIgnoreCase))
                    anyCase.Add(view);
            }

            var found = exact.Count > 0 ? exact : anyCase;
            if (found.Count == 1) return found[0];

            if (found.Count == 0)
            {
                problem = Json.Error("no_such_view",
                    "There is no view called '" + name + "' in \"" + doc.Title + "\". "
                    + "revit_views names the view in front; the Project Browser has the rest. "
                    + "Nothing was pictured.");
                return null;
            }

            var names = new List<string>();
            foreach (var view in found) names.Add(view.ViewType.ToString() + ": " + view.Name);
            names.Sort(StringComparer.Ordinal);
            problem = Json.Error("ambiguous_view",
                found.Count + " views are called '" + name + "'. Name one exactly as written "
                + "here: " + string.Join(", ", names.ToArray()) + ". Nothing was pictured.");
            return null;
        }

        // ---------------------------------------------------- elements, by id

        private static string OfElements(Document doc, string idText, int width, bool clamped)
        {
            if (HeronStop.IsStopped)
            {
                return Json.Error("stopped", HeronStop.Message);
            }

            if (doc.IsReadOnly)
            {
                return Json.Error("read_only",
                    "\"" + doc.Title + "\" is open read-only, and picturing elements needs a "
                    + "temporary 3D view Revit will not make there. Name a view instead. "
                    + "Nothing was pictured.");
            }

            // TWO SHAPES OF ID, EACH READ ITS OWN WAY (5b-1314). Revit's
            // number goes through the binder's own reading of typed ids -
            // digits, each once - so an id copied out of any Heron answer reads
            // the same here as it does there. The UniqueId the family form
            // tools answer with is looked up by Revit itself, below. Neither
            // is ever built into an ElementId: that constructor changed from
            // int to long at Revit 2024 (D-05), and a UniqueId needs none.
            var typed = new List<string>();
            var unique = new List<string>();
            var problem = SplitIds(idText, typed, unique);
            if (problem != null) return Json.Error("bad_ids", problem);

            var wanted = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var id in typed) wanted[id] = false;

            var framed = new List<Element>();
            if (typed.Count > 0)
            {
                foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
                {
                    var key = element.Id.ToString();
                    if (!wanted.ContainsKey(key) || wanted[key]) continue;
                    wanted[key] = true;
                    framed.Add(element);
                    if (framed.Count == typed.Count) break;
                }
            }

            var missing = new List<string>();
            foreach (var id in typed) if (!wanted[id]) missing.Add(id);

            // A UNIQUEID IS REVIT'S OWN LOOKUP, Document.GetElement(string), as
            // SELECT_FAMILY_FORMS reads the same ids. One that names nothing
            // here - another family's form, a project in front, a typing slip -
            // or names an element type, which the collector above never
            // matches either, is named with the numbers that name nothing. An
            // element named both ways is framed once.
            foreach (var id in unique)
            {
                Element element = null;
                try { element = doc.GetElement(id); }
                catch (Exception) { element = null; }

                if (element == null || element is ElementType)
                {
                    missing.Add(id);
                    continue;
                }
                if (!framed.Exists(e => e.Id.Equals(element.Id))) framed.Add(element);
            }

            if (missing.Count > 0)
            {
                return Json.Error("no_such_elements",
                    missing.Count + " of the " + (typed.Count + unique.Count) + " id(s) name no "
                    + "placed element in \"" + doc.Title + "\": " + string.Join(", ", missing.ToArray())
                    + ". An id belongs to one model, or one family, and is gone once its element "
                    + "is deleted. Nothing was pictured.");
            }

            // THE BOX ROUND WHAT IS LEFT ONCE DATUMS, LINKS AND ANNOTATION ARE
            // TAKEN OUT - each one taken out is named, so a picture that shows
            // fewer things than were asked for says why.
            var left = new List<string>();
            XYZ low = null, high = null;
            var shown = 0;
            foreach (var element in framed)
            {
                var why = LeftOut(element);
                var box = why == null ? element.get_BoundingBox(null) : null;
                if (why == null && box == null) why = "has no extent in the model";
                if (why != null)
                {
                    left.Add(element.Id + " " + why);
                    continue;
                }

                Grow(box, ref low, ref high);
                shown++;
            }

            if (shown == 0)
            {
                return Json.Error("nothing_to_frame",
                    "None of the " + framed.Count + " element(s) can be framed in 3D: "
                    + string.Join("; ", left.ToArray()) + ". Give the ids of model elements, "
                    + "or name a view. Nothing was pictured.");
            }

            var threeD = IsometricType(doc);
            if (threeD == null)
            {
                return Json.Error("no_isometric_type",
                    "\"" + doc.Title + "\" has no 3D view type to make a temporary view from. "
                    + "Name an existing view instead. Nothing was pictured.");
            }

            var margin = Math.Max(SmallestMargin, MarginShare * Math.Max(high.X - low.X,
                                  Math.Max(high.Y - low.Y, high.Z - low.Z)));
            var section = new BoundingBoxXYZ();
            section.Min = new XYZ(low.X - margin, low.Y - margin, low.Z - margin);
            section.Max = new XYZ(high.X + margin, high.Y + margin, high.Z + margin);

            HeronRollbackWitness.Outcome after = null;
            using (var watch = RevitFragment.Watch(doc, Label, Label))
            using (var group = new TransactionGroup(doc, Label))
            {
                try
                {
                    group.Start();

                    View3D view;
                    using (var transaction = new Transaction(doc, Label))
                    {
                        transaction.Start();
                        var options = transaction.GetFailureHandlingOptions();
                        options.SetFailuresPreprocessor(new DismissWarnings());
                        options.SetClearAfterRollback(true);
                        transaction.SetFailureHandlingOptions(options);

                        view = View3D.CreateIsometric(doc, threeD.Id);
                        Prepare(doc, view, section);
                        doc.Regenerate();

                        // COMMITTED INSIDE THE GROUP, so Revit has drawn the
                        // view before it is asked to export it - and the
                        // group, rolled back below, still takes it away.
                        if (transaction.Commit() != TransactionStatus.Committed)
                        {
                            after = RevitFragment.RollBackAndCheck(group, doc, watch);
                            return Json.Error("capture_failed",
                                "Revit would not make the temporary 3D view. "
                                + Undone(after));
                        }
                    }

                    return Exported(doc, view, width, clamped, "elements",
                                    string.Join("; ", left.ToArray()), shown,
                                    ref after, group, watch);
                }
                catch (Exception ex)
                {
                    after = RevitFragment.RollBackAndCheck(group, doc, watch);
                    return Json.Error("capture_failed",
                        "Revit could not picture those elements: " + ex.Message + ". "
                        + Undone(after));
                }
            }
        }

        /// <summary>
        /// The typed ids sorted by shape into Revit's numbers and UniqueIds,
        /// or the refusal to give back (5b-1314).
        ///
        /// A FORM HAS NO NUMBER IN ANY HERON ANSWER. CREATE_FAMILY_FORMS,
        /// REPORT_FAMILY_FORMS and SELECT_FAMILY_FORMS name a form by its
        /// UniqueId and nothing else, so a list of digits only could never
        /// picture one form out of a family the chat had just built.
        ///
        /// THE NUMBERS STILL GO THROUGH HeronBindingNote.TypedIds, so they
        /// read exactly as they did - each once, digits only. A part that is
        /// neither shape refuses the whole list, named, rather than being
        /// dropped: a picture of fewer things than were asked for with no
        /// reason given is the failure the refusals here exist to prevent.
        /// </summary>
        private static string SplitIds(string text, List<string> typed, List<string> unique)
        {
            var numbers = new List<string>();
            foreach (var piece in text.Split(','))
            {
                var part = piece.Trim();
                if (part.Length == 0) continue;

                if (UniqueIdShaped(part))
                {
                    if (unique.Contains(part))
                        return part + " is typed more than once - type each id once. "
                             + "Nothing was pictured.";
                    unique.Add(part);
                }
                else if (Digits(part)) numbers.Add(part);
                else
                    return "\"" + part + "\" is not an element id. Give the ids as Heron's "
                         + "answers print them - Revit's number, digits only, or the long id "
                         + "the family form tools give back - comma separated, with nothing "
                         + "else in the list. Nothing was pictured.";
            }

            if (numbers.Count == 0 && unique.Count == 0)
                return "'" + text + "' is not a list of element ids. Type them as Heron's "
                     + "answers print them, comma separated. Nothing was pictured.";
            if (numbers.Count == 0) return null;

            string problem;
            var read = HeronBindingNote.TypedIds(string.Join(",", numbers.ToArray()), out problem);
            if (read == null)
                return problem ?? "'" + text + "' is not a list of element ids. Type them as "
                     + "Revit shows them - digits only, comma separated. Nothing was pictured.";
            typed.AddRange(read);
            return null;
        }

        /// <summary>
        /// Whether TEXT has the shape of a Revit UniqueId: a GUID - eight,
        /// four, four, four and twelve hex digits, hyphens between - then a
        /// hyphen and hex digits, as in bfa50ce5-...-00000b71. The shape
        /// only; whether the document has it is Document.GetElement's answer.
        /// </summary>
        private static bool UniqueIdShaped(string text)
        {
            if (text.Length < 38) return false;
            for (var i = 0; i < text.Length; i++)
            {
                var hyphen = i == 8 || i == 13 || i == 18 || i == 23 || i == 36;
                if (hyphen ? text[i] != '-' : !Uri.IsHexDigit(text[i])) return false;
            }
            return true;
        }

        /// <summary>Whether TEXT is one or more of the ten ASCII digits and nothing else.</summary>
        private static bool Digits(string text)
        {
            foreach (var c in text)
                if (c < '0' || c > '9') return false;
            return text.Length > 0;
        }

        /// <summary>
        /// Why an element given by id is left out of the box, or null to frame
        /// it. A datum or a link frames the whole building, and annotation
        /// has no place in a 3D picture.
        /// </summary>
        private static string LeftOut(Element element)
        {
            if (element is RevitLinkInstance) return "is a linked model";

            var category = element.Category;
            if (category == null) return "has no category";
            foreach (var datum in Datums)
                if (category.Id.Equals(new ElementId(datum))) return "is a datum (" + category.Name + ")";
            if (category.CategoryType != CategoryType.Model)
                return "is not a model element (" + category.Name + ")";
            return null;
        }

        /// <summary>The categories that mark a place rather than make a thing.</summary>
        private static readonly BuiltInCategory[] Datums =
        {
            BuiltInCategory.OST_Levels,
            BuiltInCategory.OST_Grids,
            BuiltInCategory.OST_ProjectBasePoint,
            BuiltInCategory.OST_SharedBasePoint,
            BuiltInCategory.OST_CLines,
            BuiltInCategory.OST_VolumeOfInterest,
        };

        /// <summary>
        /// Widen LOW and HIGH to take in all eight corners of BOX, in model
        /// coordinates - a box's Min and Max are in its own Transform, which is
        /// not promised to be the identity.
        /// </summary>
        private static void Grow(BoundingBoxXYZ box, ref XYZ low, ref XYZ high)
        {
            var transform = box.Transform ?? Transform.Identity;
            foreach (var x in new[] { box.Min.X, box.Max.X })
            foreach (var y in new[] { box.Min.Y, box.Max.Y })
            foreach (var z in new[] { box.Min.Z, box.Max.Z })
            {
                var p = transform.OfPoint(new XYZ(x, y, z));
                low = low == null ? p : new XYZ(Math.Min(low.X, p.X), Math.Min(low.Y, p.Y), Math.Min(low.Z, p.Z));
                high = high == null ? p : new XYZ(Math.Max(high.X, p.X), Math.Max(high.Y, p.Y), Math.Max(high.Z, p.Z));
            }
        }

        private static ViewFamilyType IsometricType(Document doc)
        {
            foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)))
            {
                var type = element as ViewFamilyType;
                if (type != null && type.ViewFamily == ViewFamily.ThreeDimensional) return type;
            }
            return null;
        }

        /// <summary>
        /// The temporary view, made to show the elements and nothing else.
        ///
        /// ITS TEMPLATE IS TAKEN OFF FIRST. A 3D view type can carry a default
        /// template, and a view under one refuses every setting below. Each
        /// setting after that is on its own: a category Revit will not hide,
        /// or a style it will not set, costs that one setting and never the
        /// picture.
        /// </summary>
        private static void Prepare(Document doc, View3D view, BoundingBoxXYZ section)
        {
            view.ViewTemplateId = ElementId.InvalidElementId;
            view.SetSectionBox(section);
            view.IsSectionBoxActive = true;

            try { view.DetailLevel = ViewDetailLevel.Fine; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { }
            catch (InvalidOperationException) { }
            try { view.DisplayStyle = DisplayStyle.ShadingWithEdges; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { }
            catch (InvalidOperationException) { }

            var datums = new List<ElementId>();
            foreach (var datum in Datums) datums.Add(new ElementId(datum));

            foreach (Category category in doc.Settings.Categories)
            {
                if (category == null) continue;
                if (category.CategoryType == CategoryType.Model && !datums.Contains(category.Id))
                    continue;
                try
                {
                    if (view.CanCategoryBeHidden(category.Id))
                        view.SetCategoryHidden(category.Id, true);
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException) { }
                catch (Autodesk.Revit.Exceptions.InvalidOperationException) { }
            }
        }

        // -------------------------------------------------- the export itself

        /// <summary>
        /// Export VIEW into a folder of its own, read the picture back, delete
        /// the folder, roll the group back, and answer.
        ///
        /// THE FILE NAME IS NOT PREDICTED. Revit adds the view type and name
        /// to the stem it is given, so the folder is this capture's alone and
        /// whatever picture lands in it is the one.
        /// </summary>
        private static string Exported(Document doc, View view, int width, bool clamped,
                                       string mode, string leftOut, int shown,
                                       ref HeronRollbackWitness.Outcome after,
                                       TransactionGroup group,
                                       HeronRollbackWitness.Watch watch)
        {
            // READ OFF THE VIEW NOW, while it is still in the model. Given
            // element ids, VIEW is the temporary 3D view, and the rollback in
            // the finally below takes it away: read after that, Revit throws
            // "The referenced object is not valid" and the finished picture is
            // thrown away with it (FRAGMENT-ISSUES 5b-1090). Nothing below the
            // finally touches `view`.
            var viewName = view.Name;
            var viewType = view.ViewType.ToString();

            var folder = Path.Combine(HeronPaths.Captures, Guid.NewGuid().ToString("N"));
            byte[] picture = null;
            var deleted = false;
            string failure = null;
            try
            {
                Directory.CreateDirectory(folder);

                var options = new ImageExportOptions();
                options.FilePath = Path.Combine(folder, "capture");
                options.ExportRange = ExportRange.SetOfViews;
                options.HLRandWFViewsFileType = ImageFileType.PNG;
                options.ShadowViewsFileType = ImageFileType.PNG;
                options.ZoomType = ZoomFitType.FitToPage;
                options.FitDirection = FitDirectionType.Horizontal;
                options.PixelSize = width;
                options.SetViewsAndSheets(new List<ElementId> { view.Id });

                // HERON'S OWN EXPORT, into its own folder, so the run guard
                // armed round this job lets it through - a fragment that
                // exports is still cancelled. See HeronRunGuard.HeronExports.
                using (HeronRunGuard.HeronExports())
                {
                    doc.ExportImage(options);
                }

                var written = Directory.GetFiles(folder, "*.png");
                if (written.Length == 0) failure = "Revit exported the view and wrote no picture.";
                else picture = File.ReadAllBytes(written[0]);
            }
            finally
            {
                deleted = DeleteFolder(folder);

                // THE PROMISE OF THIS FILE: whatever happened above, the group
                // is undone before anything is said about it.
                if (group != null) after = RevitFragment.RollBackAndCheck(group, doc, watch);
            }

            if (failure != null)
            {
                return Json.Error("capture_failed", failure + " "
                    + Undone(after));
            }

            if (picture.Length > LargestPicture)
            {
                return Json.Error("picture_too_large",
                    "The picture of '" + viewName + "' came to "
                    + (picture.Length / 1000).ToString(CultureInfo.InvariantCulture)
                    + " KB at " + width.ToString(CultureInfo.InvariantCulture)
                    + " px wide, too large to hand to the chat. Ask again with a smaller "
                    + "width. " + Undone(after));
            }

            return Json.Ok(
                Json.Str("mode", mode),
                Json.Str("view", viewName),
                Json.Str("viewType", viewType),
                Json.Num("pixelWidth", width),
                Json.Bool("widthClamped", clamped),
                Json.Num("bytes", picture.Length),
                Json.Num("shown", shown),
                Json.Str("leftOut", string.IsNullOrEmpty(leftOut) ? null : leftOut),
                // HELD, AS FAR AS IT WAS CHECKED: Revit's status, and the
                // temporary view counted back (RevitFragment.RollBackAndCheck).
                // `keptLost` and `rollbackNote` say what the rollback took from
                // the job kept before this one, so the chat never says "nothing
                // was changed" over a loss.
                Json.Bool("rolledBack", after == null || after.Held),
                Json.Num("keptLost", after == null ? 0 : after.Lost),
                Json.Str("rollbackNote", after == null || after.Also().Length == 0
                                             ? null : after.Also()),
                Json.Bool("fileDeleted", deleted),
                Json.Str("document", doc.Title),
                Json.Str("documentPath", string.IsNullOrEmpty(doc.PathName) ? null : doc.PathName),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Str("creationGuid", RevitOperations.CreationGuid(doc)),
                Json.Str("png", Convert.ToBase64String(picture)));
        }

        /// <summary>
        /// Delete this capture's folder, never throwing, and say whether it
        /// went. A folder left behind is in Derived and safe to delete; the
        /// answer still says so rather than leaving a picture of somebody's
        /// model on disk unannounced.
        /// </summary>
        private static bool DeleteFolder(string folder)
        {
            try
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
                return !Directory.Exists(folder);
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        // THIS FILE'S OWN SafeRollBack LIVED HERE until 2026-10-17. Every
        // rollback here now goes through RevitFragment.RollBackAndCheck, and
        // the one SafeRollBack it calls carries this one's rule: a group that
        // never started counts as undone, because Start is the line that
        // failed and nothing was made inside it.

        /// <summary>
        /// What the rollback left, in the words a capture's refusal ends on:
        /// nothing kept - said only when Revit confirmed it AND the temporary
        /// view was counted back gone - then what the rollback took from the
        /// job kept before this one, if anything. No group, nothing to say.
        /// </summary>
        private static string Undone(HeronRollbackWitness.Outcome after)
        {
            var said = after == null || after.Held ? "Nothing was kept in the model." : NotConfirmed;
            var also = after == null ? "" : after.Also();
            return also.Length == 0 ? said : said + " " + also;
        }

        private const string NotConfirmed =
            "Revit did not confirm the temporary view was taken away. Look under 3D Views "
            + "in the Project Browser, and at Undo, before going on.";

        /// <summary>
        /// Warnings dismissed, so a temporary view can never put a dialog in
        /// front of the modeller; an error rolls the transaction back, and the
        /// capture says it failed.
        /// </summary>
        private sealed class DismissWarnings : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
            {
                foreach (var message in accessor.GetFailureMessages())
                {
                    if (message.GetSeverity() >= FailureSeverity.Error)
                        return FailureProcessingResult.ProceedWithRollBack;
                }
                foreach (var message in accessor.GetFailureMessages())
                {
                    if (message.GetSeverity() == FailureSeverity.Warning)
                        accessor.DeleteWarning(message);
                }
                return FailureProcessingResult.Continue;
            }
        }
    }
}
