// Heron-Agent:  none
// Heron-Step:   7
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
    /// `show_elements` (D-163): select elements by id in the model on screen,
    /// and zoom to them when asked - the Activity pane's Select and Zoom.
    ///
    /// EXECUTE, AND FIXED CODE. It changes what is selected and what the view
    /// shows, never what exists: no transaction, so it runs with Changes OFF,
    /// like select_by_category. Selection and ShowElements need none.
    ///
    /// TOLERANT OF WHAT IS GONE. A change listed in the pane may be minutes
    /// old; elements deleted or undone since are skipped and counted, never a
    /// reason to select nothing. Types are skipped too - there is nothing to
    /// draw. Only when NONE remain is it refused.
    ///
    /// ONLY THE MODEL THE CHANGE WAS MADE IN. The record names it by title; a
    /// different model in front is refused rather than selecting elements that
    /// happen to share an id there.
    /// </summary>
    internal static class RevitShowElements
    {
        public static string Show(UIApplication app, string ids, bool zoom, string document)
        {
            var front = app == null ? null : app.ActiveUIDocument;
            var doc = front == null ? null : front.Document;
            if (doc == null)
                return Json.Error("no_document", "No model is open in Revit. Nothing was selected.");

            if (!string.IsNullOrEmpty(document) && !string.Equals(doc.Title, document, StringComparison.Ordinal))
            {
                return Json.Error("wrong_document",
                    "This change was made in \"" + document + "\", and the model in front is \""
                    + doc.Title + "\". Nothing was selected. Bring \"" + document + "\" to the front.");
            }

            var wanted = new HashSet<string>(StringComparer.Ordinal);
            foreach (var part in (ids ?? "").Split(','))
            {
                var trimmed = part.Trim();
                if (trimmed.Length > 0) wanted.Add(trimmed);
            }
            if (wanted.Count == 0)
                return Json.Error("bad_request_value", "No element ids were given. Nothing was selected.");

            // Found by the model's own ids - no ElementId is built from a number
            // here, so nothing depends on 2024's change from int to long.
            var found = new List<ElementId>();
            var types = 0;
            foreach (var id in new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElementIds())
            {
                if (!wanted.Contains(id.ToString())) continue;
                found.Add(id);
                if (found.Count == wanted.Count) break;
            }
            foreach (var id in new FilteredElementCollector(doc).WhereElementIsElementType().ToElementIds())
            {
                if (wanted.Contains(id.ToString())) types++;
            }
            var gone = wanted.Count - found.Count - types;

            if (found.Count == 0)
            {
                return Json.Error("nothing_to_show",
                    "None of the " + wanted.Count + " element(s) of this change is in \"" + doc.Title
                    + "\" now - deleted or undone since. Nothing was selected.");
            }

            front.Selection.SetElementIds(found);
            if (zoom) front.ShowElements(found);

            var said = (zoom ? "Selected and zoomed to " : "Selected ") + found.Count + " element(s) in \""
                     + doc.Title + "\"."
                     + (gone > 0 ? " " + gone + " are no longer in the model." : "")
                     + (types > 0 ? " " + types + " were types, with nothing to draw." : "")
                     + " Nothing in the model changed.";
            return Json.Ok(
                Json.Num("selected", found.Count),
                Json.Num("gone", Math.Max(0, gone)),
                Json.Str("document", doc.Title),
                Json.Str("said", said));
        }
    }
}
