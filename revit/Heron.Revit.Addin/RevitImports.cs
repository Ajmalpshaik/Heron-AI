// Heron-Agent:  HERON-REVIT-IMP-019
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
    /// What has been brought into this model from outside.
    /// HERON-REVIT-IMP-019.
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit, and on a day another session had
    /// the only Revit open. It COMPILES on 2020 through 2027 and that is the
    /// whole of what is known about it.
    /// ===================================================================
    ///
    /// LINKED AND IMPORTED ARE DIFFERENT, AND CONFUSING THEM IS THE DEFECT
    /// --------------------------------------------------------------------
    /// A LINKED CAD file stays outside the model and is re-read from disk. It
    /// can be reloaded, unloaded, and removed cleanly.
    ///
    /// An IMPORTED one is copied INTO the model and never leaves. It brings
    /// its layers, its line patterns, its text styles and its fonts with it,
    /// permanently, and they appear in every dialog in the project from then
    /// on. Deleting the import does not remove them.
    ///
    /// **That is the single best-known way to ruin a Revit file**, and it is
    /// invisible: an imported DWG and a linked one look identical in the
    /// drawing area. Only the Manage Links dialog and the Import category
    /// tell them apart, and nobody opens those until the file is already
    /// slow.
    ///
    /// So this counts them SEPARATELY and says which is which, every time.
    /// Revit link models are counted too, but named as their own thing:
    /// RevitLinks (HERON-REVIT-LNK-015) is the agent that reads those
    /// properly, and repeating its work here would be a second place for one
    /// fact.
    ///
    /// EXPLODED IMPORTS CANNOT BE COUNTED, AND THAT IS SAID OUT LOUD
    /// --------------------------------------------------------------
    /// An import that has been exploded is no longer an import: it is loose
    /// model lines and text, indistinguishable from work somebody drew. The
    /// damage - the line patterns, the text styles - remains. Nothing can
    /// count those after the fact, so the answer says the count is of
    /// UNEXPLODED imports and does not pretend to a total.
    ///
    /// This is the same discipline as the redundant rooms in RevitRooms.cs:
    /// naming what a tool cannot see is worth more than a number that reads
    /// like a survey and is not one.
    ///
    /// NOTHING IS IMPORTED
    /// ---------------------
    /// The register gives this row MODIFY - the highest level it could ever
    /// require. Reading requires none of it. Nothing here imports, links,
    /// reloads or removes anything. An import is irreversible in the way
    /// described above, and it will not be attempted first by a machine that
    /// has never opened Revit.
    /// </summary>
    internal static class RevitImports
    {
        /// <summary>
        /// Everything that has come into this model from outside, with linked
        /// and imported told apart.
        /// </summary>
        public static string List(UIApplication app)
        {
            var doc = RevitOperations.ActiveDocument(app);
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            var imported = new List<string>();
            var linkedCad = new List<string>();
            var importedCount = 0;
            var linkedCadCount = 0;

            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(ImportInstance))
                                        .WhereElementIsNotElementType())
            {
                var instance = element as ImportInstance;
                if (instance == null) continue;

                var isLink = IsLinked(instance);
                var name = FileNameOf(doc, instance);
                var row = Json.Obj(
                    Json.Str("name", name),
                    Json.Bool("linked", isLink),
                    Json.Str("view", OwnerViewName(doc, instance)));

                if (isLink) { linkedCadCount++; linkedCad.Add(row); }
                else { importedCount++; imported.Add(row); }
            }

            // Revit links, named rather than described - LNK-015 owns them.
            var revitLinks = 0;
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(RevitLinkType))
                                        .WhereElementIsElementType())
            {
                if (element != null) revitLinks++;
            }

            return Json.Ok(
                Json.Arr("imported", imported),
                Json.Arr("linkedCad", linkedCad),
                Json.Num("importedCount", importedCount),
                Json.Num("linkedCadCount", linkedCadCount),
                Json.Num("revitLinks", revitLinks),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Str("reads", Reads(importedCount, linkedCadCount, revitLinks)));
        }

        /// <summary>What was found, and what could not be looked for.</summary>
        private static string Reads(int imported, int linkedCad, int revitLinks)
        {
            var said = new List<string>();

            if (imported > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} CAD file(s) are IMPORTED rather than linked. An import "
                  + "is copied INTO the model and never leaves: its layers, line "
                  + "patterns, text styles and fonts are in this project "
                  + "permanently and appear in every dialog from now on, and "
                  + "DELETING THE IMPORT DOES NOT REMOVE THEM. An imported DWG "
                  + "and a linked one look identical in the drawing area, which "
                  + "is why this is worth saying", imported));
            }

            if (linkedCad > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} CAD file(s) are LINKED, which is the safe form - they "
                  + "stay outside the model, can be reloaded or unloaded, and "
                  + "come out cleanly", linkedCad));
            }

            if (revitLinks > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} Revit link(s) are present; `revit_links` reads those "
                  + "properly, including their load state", revitLinks));
            }

            if (said.Count == 0)
            {
                said.Add("Nothing has been imported or linked into this model");
            }

            return string.Join(". ", said.ToArray())
                 + ". THE COUNT IS OF UNEXPLODED IMPORTS ONLY. An import that "
                 + "has been exploded is no longer an import - it is loose model "
                 + "lines and text, indistinguishable from work somebody drew - "
                 + "while its line patterns and text styles remain. Nothing can "
                 + "count those after the fact, so this is not a total and does "
                 + "not pretend to be one. Nothing was imported and nothing was "
                 + "changed.";
        }

        /// <summary>
        /// Is this instance a LINK rather than an import?
        ///
        /// The single most important question in the file, so it is asked of
        /// Revit directly rather than inferred from a name or a category.
        /// </summary>
        private static bool IsLinked(ImportInstance instance)
        {
            try { return instance.IsLinked; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
        }

        /// <summary>
        /// The CAD file this instance came from, as a modeller would name it.
        ///
        /// ASKED OF THE TYPE, NOT THE INSTANCE, and that is the whole fix.
        /// `ImportInstance.Name` describes where the thing SITS - against
        /// Project1 work_ajmal.al on 2026-09-18 it read `location <Not
        /// Shared>`, which is a correct answer to a question nobody asked. The
        /// filename lives on the CADLinkType, which is also what Revit's own
        /// Manage Links dialog shows.
        ///
        /// Found by running the agent rather than by reading it: the
        /// linked-versus-imported verdict was right, and the label beside it
        /// was useless. A right answer nobody can act on is half an answer.
        ///
        /// Falls back to the instance's own name rather than to nothing, so a
        /// type that will not answer still leaves something to go on.
        /// </summary>
        private static string FileNameOf(Document doc, ImportInstance instance)
        {
            try
            {
                var id = instance.GetTypeId();
                if (id != null && id != ElementId.InvalidElementId)
                {
                    var type = doc.GetElement(id);
                    if (type != null)
                    {
                        var named = SafeName(type);
                        if (named != "(unnamed)") return named;
                    }
                }
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                // fall through to the instance
            }
            return SafeName(instance);
        }

        /// <summary>
        /// The view this import is stuck to, or null when it is model-wide.
        ///
        /// A view-specific import is the more surprising of the two: it shows
        /// on one drawing and nowhere else, so somebody checking another view
        /// sees nothing wrong.
        /// </summary>
        private static string OwnerViewName(Document doc, ImportInstance instance)
        {
            try
            {
                var id = instance.OwnerViewId;
                if (id == null || id == ElementId.InvalidElementId) return null;
                var view = doc.GetElement(id) as View;
                return view == null ? null : SafeName(view);
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
