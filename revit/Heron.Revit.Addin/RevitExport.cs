// Heron-Agent:  HERON-REVIT-EXP-018
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
    /// Export readiness. HERON-REVIT-EXP-018, the one row in this batch whose
    /// risk is PUBLISH rather than MODIFY.
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit, and on a day another session had
    /// the only Revit open. It COMPILES on 2020 through 2027 and that is the
    /// whole of what is known about it.
    /// ===================================================================
    ///
    /// WHY THIS ONE DOES NOT EXPORT, AND THE REASON IS NOT TIMIDITY
    /// -------------------------------------------------------------
    /// PUBLISH is the risk level for *sending something out of the model* -
    /// an IFC to a consultant, a DWG to a contractor, a PDF into an issue.
    /// Once a file has left, it has left: there is no undo, the recipient has
    /// it, and on a real job somebody may already be building from it.
    ///
    /// That is categorically unlike every other agent in this batch. A bad
    /// read wastes a minute. A bad export is a drawing on a site hut wall.
    ///
    /// So the operation that exists is `check_export`, and what it answers is
    /// **"would an export of this be worth sending?"** - which is the
    /// question somebody actually has, and the one nothing in Revit answers
    /// before the file is already written.
    ///
    /// The doing half needs a destination path, an overwrite decision, a
    /// format, a setup, and a human who meant it. Those are the write-path
    /// rails D-22 and Golden Rule 20 describe, and none of them exists for
    /// export yet. A write path built before its safety path is a write path
    /// that ships without one - so this is the safety path, first,
    /// deliberately, and the export itself is a separate operation at PUBLISH
    /// risk for whoever builds those rails.
    ///
    /// WHAT MAKES AN EXPORT NOT WORTH SENDING
    /// ---------------------------------------
    /// Every one of these has been found in an issued set at least once on a
    /// real job, and none of them is visible in Revit before you export:
    ///
    ///   NOTHING TO EXPORT. No sheets, or no views on them. An export runs
    ///   happily and produces an empty or near-empty set.
    ///
    ///   SHEETS THAT WOULD PRINT BLANK. Counted by RevitSheets and repeated
    ///   here because at export time it is the thing that matters, and the
    ///   person exporting is usually not the person who modelled.
    ///
    ///   UNRESOLVED LINKS. A link that is not loaded exports as nothing at
    ///   all. The drawing goes out with the structure missing and the
    ///   geometry it was coordinated against absent.
    ///
    ///   UNPLACED ROOMS FEEDING AREA SCHEDULES. An area total that is wrong
    ///   in a schedule is wrong in every export of that schedule.
    ///
    /// It says what it found. It does not decide whether that is acceptable -
    /// a coordination model with links deliberately unloaded is a legitimate
    /// thing to export, and this file has never seen anybody's issue process.
    ///
    /// NOTHING LEAVES THE MODEL
    /// --------------------------
    /// No file is written, no path is touched, nothing is printed and nothing
    /// is sent. The operation is registered at Read for exactly that reason -
    /// the row's PUBLISH is the highest level it could require, and asking
    /// whether an export would be sound requires none of it.
    /// </summary>
    internal static class RevitExport
    {
        /// <summary>
        /// Whether an export of the active document would be worth sending,
        /// and what is wrong with it if not.
        /// </summary>
        public static string Check(UIApplication app)
        {
            var doc = RevitOperations.ActiveDocument(app);
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            // --- what there is to export at all ---------------------------
            var sheets = 0;
            var blankSheets = 0;
            var placeholders = 0;
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(ViewSheet))
                                        .WhereElementIsNotElementType())
            {
                var sheet = element as ViewSheet;
                if (sheet == null) continue;
                sheets++;
                if (IsPlaceholder(sheet)) { placeholders++; continue; }
                if (PlacedViewCount(sheet) == 0) blankSheets++;
            }

            // --- links, which export as nothing when unloaded --------------
            var links = 0;
            var linksNotLoaded = 0;
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(RevitLinkType))
                                        .WhereElementIsElementType())
            {
                var link = element as RevitLinkType;
                if (link == null) continue;
                links++;
                if (!IsLoaded(link)) linksNotLoaded++;
            }

            // --- rooms whose area is wrong, and so wrong in every export ---
            var rooms = 0;
            var roomsWithNoArea = 0;
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfCategory(BuiltInCategory.OST_Rooms)
                                        .WhereElementIsNotElementType())
            {
                if (element == null) continue;
                rooms++;
                if (AreaOf(element) <= 0.0) roomsWithNoArea++;
            }

            var ready = sheets > 0
                     && blankSheets == 0
                     && linksNotLoaded == 0
                     && roomsWithNoArea == 0;

            return Json.Ok(
                Json.Bool("worthSending", ready),
                Json.Num("sheets", sheets),
                Json.Num("placeholderSheets", placeholders),
                Json.Num("sheetsThatWouldPrintBlank", blankSheets),
                Json.Num("links", links),
                Json.Num("linksNotLoaded", linksNotLoaded),
                Json.Num("rooms", rooms),
                Json.Num("roomsWithNoArea", roomsWithNoArea),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Bool("anythingLeftTheModel", false),
                Json.Str("reads", Reads(sheets, placeholders, blankSheets,
                                        links, linksNotLoaded, roomsWithNoArea)));
        }

        /// <summary>What was found, without deciding whether it is acceptable.</summary>
        private static string Reads(int sheets, int placeholders, int blank,
                                    int links, int notLoaded, int roomsNoArea)
        {
            var said = new List<string>();

            if (sheets == 0)
            {
                said.Add("There are NO SHEETS in this model, so an export would "
                       + "produce an empty or near-empty set. An export runs "
                       + "happily either way and says nothing");
            }

            if (blank > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} sheet(s) would print BLANK - numbered, in the register, "
                  + "and with nothing on them. The Project Browser shows an empty "
                  + "sheet and a full one identically, and the person exporting is "
                  + "usually not the person who modelled", blank));
            }

            if (notLoaded > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} of {1} link(s) are NOT LOADED. An unloaded link exports "
                  + "as nothing at all, so the drawing goes out with that model "
                  + "missing - and the geometry it was coordinated against absent",
                    notLoaded, links));
            }

            if (roomsNoArea > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} room(s) have no area - unplaced, or not enclosed. An "
                  + "area total that is wrong in a schedule is wrong in every "
                  + "export of that schedule", roomsNoArea));
            }

            if (placeholders > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} placeholder sheet(s) - numbers reserved for drawings "
                  + "that do not exist yet. Normal, and worth knowing before the "
                  + "set is counted", placeholders));
            }

            if (said.Count == 0)
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "{0} sheet(s), all with something on them; every link loaded; "
                  + "every room with an area. Nothing here says an export would be "
                  + "WRONG. NOTHING WAS EXPORTED - this answers whether an export "
                  + "would be worth sending, which is the question nothing in "
                  + "Revit answers until the file is already written.", sheets);
            }

            return string.Join(". ", said.ToArray())
                 + ". None of this is a verdict on your issue process - a "
                 + "coordination model with links deliberately unloaded is a "
                 + "legitimate thing to export, and this has never seen how you "
                 + "issue. NOTHING WAS EXPORTED and nothing left the model: no "
                 + "file was written, no path touched, nothing printed and nothing "
                 + "sent. Doing the export is a separate operation at PUBLISH "
                 + "risk, and it needs a destination, an overwrite decision and a "
                 + "person who meant it - rails that do not exist yet.";
        }

        /// <summary>Is this link loaded into the host right now?</summary>
        private static bool IsLoaded(RevitLinkType link)
        {
            try
            {
                var status = link.GetLinkedFileStatus();
                return status == LinkedFileStatus.Loaded;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
            catch (InvalidOperationException) { return false; }
        }

        /// <summary>How many views are placed on this sheet.</summary>
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

        /// <summary>Is this a placeholder rather than a real sheet?</summary>
        private static bool IsPlaceholder(ViewSheet sheet)
        {
            try { return sheet.IsPlaceholder; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
        }

        /// <summary>
        /// The raw area, used ONLY to decide whether it is zero. Never
        /// printed - Revit holds an area in square feet whatever the project
        /// is set to.
        /// </summary>
        private static double AreaOf(Element element)
        {
            try
            {
                var p = element.get_Parameter(BuiltInParameter.ROOM_AREA);
                return p == null ? 0.0 : p.AsDouble();
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return 0.0; }
        }
    }
}
