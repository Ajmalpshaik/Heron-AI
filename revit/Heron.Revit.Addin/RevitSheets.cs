// Heron-Agent:  HERON-REVIT-SHT-029
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
    /// Sheets, numbering, titleblocks and revisions. HERON-REVIT-SHT-029.
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit, and on a day another session had
    /// the only Revit open. It COMPILES on 2020 through 2027 and that is the
    /// whole of what is known about it.
    /// ===================================================================
    ///
    /// THIS AGENT OWNS SHEETS, AND THAT WAS DECIDED RATHER THAN ASSUMED
    /// -----------------------------------------------------------------
    /// Two rows in docs/28 claimed sheets: this one, and HERON-REVIT-VIE-013
    /// ("Views, SHEETS, view templates, visibility"). The owner settled it on
    /// 2026-09-17 - **the Sheet Agent owns sheets** - so numbering,
    /// titleblocks and revisions are here, and RevitViews.cs says only
    /// whether a view is placed, which is a fact about the view.
    ///
    /// WHY A MODELLER CARES, IN ONE SENTENCE
    /// --------------------------------------
    /// The sheets ARE the deliverable. A model is an argument about a
    /// building; a drawing set is what gets issued, checked, built from and
    /// paid against. The states below are the ones that turn up at issue,
    /// which is the worst possible moment to find them.
    ///
    /// THE FOUR THINGS IT LOOKS FOR
    /// -----------------------------
    ///   A SHEET WITH NOTHING ON IT. Numbered, in the register, in the
    ///   issue sheet, and blank. It is the single most embarrassing thing to
    ///   issue and it is invisible in the Project Browser, which shows a
    ///   sheet with one view and a sheet with none identically.
    ///
    ///   A SHEET WITH NO TITLEBLOCK. It will print without a border, a
    ///   number, a revision or a signature block.
    ///
    ///   MORE THAN ONE TITLEBLOCK FAMILY IN USE. Usually a sheet created
    ///   from the wrong template. The set prints with two different borders
    ///   and nobody notices until it is on paper.
    ///
    ///   A PLACEHOLDER SHEET. A number reserved for a drawing that does not
    ///   exist yet. Legitimate, and it must never be counted as a sheet that
    ///   is ready - so it is counted separately rather than filtered out.
    ///
    /// REVISIONS ARE REPORTED, NEVER INTERPRETED
    /// -------------------------------------------
    /// Which revision a sheet carries and whether a revision is ISSUED are
    /// read straight off Revit. Nothing here decides whether a sheet SHOULD
    /// carry one - that is a question about somebody's issue process, and a
    /// file that has never run against a real project has no business having
    /// an opinion about it.
    ///
    /// SHEET NUMBERS ARE NOT CHECKED FOR UNIQUENESS
    /// ----------------------------------------------
    /// Revit enforces that itself - it refuses a duplicate sheet number at
    /// the point of entry. Re-checking it here would be this repository's own
    /// recurring mistake in miniature: a gate guarding something that cannot
    /// happen, reporting zero for ever, and read as evidence that the check
    /// works. What IS reported is the numbering as it stands, so a person can
    /// see whether it follows their own standard, which is not a thing this
    /// file could know.
    ///
    /// NOTHING IS SET
    /// ----------------
    /// The register gives this row MODIFY - the highest level it could ever
    /// require. Reading requires none of it. Renumbering a sheet breaks every
    /// drawing reference pointing at it, across the whole set and every
    /// consultant's copy, and a machine that has never opened Revit will not
    /// be the first to try it.
    /// </summary>
    internal static class RevitSheets
    {
        /// <summary>
        /// Every sheet in the active document, what is on it, what it is
        /// drawn on, and what revisions it carries.
        /// </summary>
        public static string List(UIApplication app)
        {
            var doc = RevitOperations.ActiveDocument(app);
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            var sheets = new List<ViewSheet>();
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(ViewSheet))
                                        .WhereElementIsNotElementType())
            {
                var sheet = element as ViewSheet;
                if (sheet != null) sheets.Add(sheet);
            }

            if (sheets.Count == 0)
            {
                return Json.Ok(
                    Json.Arr("sheets", new List<string>()),
                    Json.Num("sheetCount", 0),
                    Json.Str("document", doc.Title),
                    Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                    Json.Bool("modifiedAnything", false),
                    Json.Str("reads",
                        "This model has no sheets. Normal for a working model "
                      + "or a family, and worth knowing before anybody asks "
                      + "for a drawing set from it."));
            }

            // Which titleblock family sits on each sheet. Collected per sheet
            // rather than in one sweep, because a titleblock is owned by its
            // sheet and OwnedByView is the question that says so.
            var titleblockUse = new Dictionary<string, int>(StringComparer.Ordinal);

            var rows = new List<string>();
            var emptySheets = 0;
            var noTitleblock = 0;
            var placeholders = 0;
            var withRevisions = 0;

            foreach (var sheet in sheets)
            {
                var placeholder = IsPlaceholder(sheet);
                if (placeholder) placeholders++;

                var views = PlacedViewCount(sheet);
                if (!placeholder && views == 0) emptySheets++;

                var titleblock = TitleblockOf(doc, sheet);
                if (titleblock == null)
                {
                    if (!placeholder) noTitleblock++;
                }
                else
                {
                    int seen;
                    titleblockUse[titleblock] =
                        titleblockUse.TryGetValue(titleblock, out seen) ? seen + 1 : 1;
                }

                var revisions = RevisionCount(sheet);
                if (revisions > 0) withRevisions++;

                rows.Add(Json.Obj(
                    Json.Str("number", NumberOf(sheet)),
                    Json.Str("name", SafeName(sheet)),
                    Json.Num("views", views),
                    Json.Str("titleblock", titleblock),
                    Json.Num("revisions", revisions),
                    Json.Bool("placeholder", placeholder)));
            }

            var titleblockRows = new List<string>();
            foreach (var pair in titleblockUse)
            {
                titleblockRows.Add(Json.Obj(
                    Json.Str("name", pair.Key),
                    Json.Num("sheets", pair.Value)));
            }

            // Every revision in the model, issued or not.
            var revisionRows = new List<string>();
            var issued = 0;
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(Revision))
                                        .WhereElementIsNotElementType())
            {
                var revision = element as Revision;
                if (revision == null) continue;
                var isIssued = IsIssued(revision);
                if (isIssued) issued++;
                revisionRows.Add(Json.Obj(
                    Json.Str("number", RevisionNumberOf(revision)),
                    Json.Str("description", DescriptionOf(revision)),
                    Json.Bool("issued", isIssued)));
            }

            return Json.Ok(
                Json.Arr("sheets", rows),
                Json.Arr("titleblocks", titleblockRows),
                Json.Arr("revisions", revisionRows),
                Json.Num("sheetCount", rows.Count),
                Json.Num("placeholderSheets", placeholders),
                Json.Num("sheetsWithNothingOnThem", emptySheets),
                Json.Num("sheetsWithNoTitleblock", noTitleblock),
                Json.Num("titleblockFamiliesInUse", titleblockRows.Count),
                Json.Num("sheetsCarryingARevision", withRevisions),
                Json.Num("revisionCount", revisionRows.Count),
                Json.Num("revisionsIssued", issued),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Str("reads", Reads(rows.Count, placeholders, emptySheets,
                                        noTitleblock, titleblockRows.Count)));
        }

        /// <summary>What the numbers mean, in the words a modeller would use.</summary>
        private static string Reads(int sheets, int placeholders, int empty,
                                    int noTitleblock, int titleblockFamilies)
        {
            var said = new List<string>();

            if (empty > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} sheet(s) have nothing placed on them. The Project "
                  + "Browser shows a sheet with one view and a sheet with none "
                  + "identically, so this does not show up until it is printed",
                    empty));
            }

            if (noTitleblock > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} sheet(s) have no titleblock. They will print with no "
                  + "border, no number, no revision and no signature block",
                    noTitleblock));
            }

            if (titleblockFamilies > 1)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} different titleblock families are in use across the "
                  + "set. Usually a sheet started from the wrong template, and "
                  + "it is not noticed until two borders appear on paper",
                    titleblockFamilies));
            }

            if (placeholders > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} of these are PLACEHOLDER sheets - a number reserved "
                  + "for a drawing that does not exist yet. Counted separately "
                  + "rather than left out, because a placeholder must never be "
                  + "read as a sheet that is ready", placeholders));
            }

            if (said.Count == 0)
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "All {0} sheet(s) carry a titleblock, have something on "
                  + "them, and share one titleblock family. Sheet numbers are "
                  + "listed as they stand rather than checked against a "
                  + "standard - Revit already refuses a duplicate number, and "
                  + "what your numbering ought to look like is not something "
                  + "this can know.", sheets);
            }

            return string.Join(". ", said.ToArray())
                 + ". Sheet numbers are listed as they stand rather than checked "
                 + "against a standard: Revit already refuses a duplicate, and "
                 + "what your numbering ought to look like is not something this "
                 + "can know. Nothing was changed.";
        }

        /// <summary>
        /// How many views are placed on this sheet.
        ///
        /// `GetAllPlacedViews` rather than counting Viewports, because it is
        /// the sheet's own answer and it includes what a Viewport sweep would
        /// miss.
        /// </summary>
        private static int PlacedViewCount(ViewSheet sheet)
        {
            try
            {
                var placed = sheet.GetAllPlacedViews();
                return placed == null ? 0 : placed.Count;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return 0; }
            catch (InvalidOperationException) { return 0; }
        }

        /// <summary>
        /// The titleblock family and type on this sheet, or null when there
        /// is none.
        ///
        /// A sheet owns its titleblock, so `OwnedByView` is the question that
        /// finds it. More than one is possible and the first is reported -
        /// which is honest for the thing being asked, "what is this drawn
        /// on", and the count of DISTINCT families across the set is what
        /// catches the real problem anyway.
        /// </summary>
        private static string TitleblockOf(Document doc, ViewSheet sheet)
        {
            try
            {
                foreach (var element in new FilteredElementCollector(doc, sheet.Id)
                                            .OfCategory(BuiltInCategory.OST_TitleBlocks)
                                            .WhereElementIsNotElementType())
                {
                    if (element == null) continue;
                    var type = doc.GetElement(element.GetTypeId());
                    return type == null ? SafeName(element) : SafeName(type);
                }
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
            return null;
        }

        /// <summary>How many revisions this sheet carries.</summary>
        private static int RevisionCount(ViewSheet sheet)
        {
            try
            {
                var revisions = sheet.GetAllRevisionIds();
                return revisions == null ? 0 : revisions.Count;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return 0; }
            catch (InvalidOperationException) { return 0; }
        }

        /// <summary>Is this a placeholder rather than a real sheet?</summary>
        private static bool IsPlaceholder(ViewSheet sheet)
        {
            try { return sheet.IsPlaceholder; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
        }

        /// <summary>The sheet number, never an exception mid-answer.</summary>
        private static string NumberOf(ViewSheet sheet)
        {
            try
            {
                return string.IsNullOrEmpty(sheet.SheetNumber)
                     ? "(no number)" : sheet.SheetNumber;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return "(no number)";
            }
        }

        /// <summary>
        /// The revision's number as Revit shows it.
        ///
        /// `RevisionNumber` is the SEQUENCE Revit prints on the sheet, which
        /// is what a modeller reads on the drawing, rather than the internal
        /// ordering. When it will not answer, nothing is substituted.
        /// </summary>
        private static string RevisionNumberOf(Revision revision)
        {
            try
            {
                return string.IsNullOrEmpty(revision.RevisionNumber)
                     ? null : revision.RevisionNumber;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }

        /// <summary>The revision's description, or null.</summary>
        private static string DescriptionOf(Revision revision)
        {
            try
            {
                return string.IsNullOrEmpty(revision.Description)
                     ? null : revision.Description;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }

        /// <summary>Has this revision been issued?</summary>
        private static bool IsIssued(Revision revision)
        {
            try { return revision.Issued; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
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
