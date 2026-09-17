// Heron-Agent:  HERON-REVIT-DIM-031
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
    /// Dimensions, tags, text and keynotes. HERON-REVIT-DIM-031, whose row
    /// notes that "create dimensions" is one of Heron's founding examples.
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit, and on a day another session had
    /// the only Revit open. It COMPILES on 2020 through 2027 and that is the
    /// whole of what is known about it.
    /// ===================================================================
    ///
    /// THE ONE THING WORTH FINDING: AN OVERRIDDEN DIMENSION
    /// -----------------------------------------------------
    /// A dimension in Revit normally reports the distance it measures. It can
    /// also be OVERRIDDEN - typed over with any text at all - and it then
    /// looks exactly like a real one on the drawing.
    ///
    /// That is a dimension that says 2400 on a wall that is 2100. It survives
    /// every model change, because nothing recomputes it. It is checked by
    /// nobody, because it looks correct. And it gets built.
    ///
    /// Nothing in Revit lists them. This does, and it is the reason the agent
    /// earns its place ahead of anything it could say about tag counts.
    ///
    /// THE SECOND THING: A TAG THAT LOST WHAT IT WAS TAGGING
    /// ------------------------------------------------------
    /// A tag whose host has been deleted becomes an orphan. Revit warns once,
    /// at deletion, and then never again - so the warning is dismissed on a
    /// busy day and the empty tag stays on the sheet for ever.
    ///
    /// UNITS ARE NEVER RE-DERIVED FROM A DIMENSION
    /// ---------------------------------------------
    /// The value a dimension shows is Revit's own formatted string, taken as
    /// it is. This file does not convert it, does not parse it back into a
    /// number and does not compare it to a measurement - it only asks whether
    /// Revit considers the value overridden. Re-deriving a length would be
    /// the decimal-feet defect RevitParameters exists to prevent, arriving
    /// through the one agent whose whole subject is numbers on a drawing.
    ///
    /// WHY 2025 IS MENTIONED IN A FILE THAT DOES NOT BRANCH ON IT
    /// -----------------------------------------------------------
    /// Revit 2025 split `Dimension` into `LinearDimension`, `RadialDimension`
    /// and `ArcLengthDimension`. Any code doing an EXACT type check on
    /// `Dimension` breaks there. This collects by CLASS - `typeof(Dimension)`
    /// - which matches a subclass as well, so it needs no version branch and
    /// none is written. The version-support skill's rule applies: prefer the
    /// spelling that is simply correct everywhere over a `#if` that hides one
    /// that is not.
    ///
    /// NOTHING IS SET
    /// ----------------
    /// The register gives this row MODIFY - the highest level it could ever
    /// require. Reading requires none of it. Creating a dimension, clearing
    /// an override or deleting a tag all change a drawing somebody may have
    /// already checked.
    /// </summary>
    internal static class RevitAnnotation
    {
        /// <summary>
        /// Every dimension, tag, text note and keynote in the active
        /// document, with the two states worth acting on.
        /// </summary>
        public static string List(UIApplication app)
        {
            var doc = RevitOperations.ActiveDocument(app);
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            // --- dimensions, and the overridden ones ----------------------
            var dimensions = 0;
            var overridden = 0;
            var overriddenExamples = new List<string>();

            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(Dimension))
                                        .WhereElementIsNotElementType())
            {
                var dimension = element as Dimension;
                if (dimension == null) continue;
                dimensions++;

                var typed = OverrideOf(dimension);
                if (typed == null) continue;

                overridden++;
                if (overriddenExamples.Count < 8)
                {
                    overriddenExamples.Add(Json.Obj(
                        Json.Str("shows", typed),
                        Json.Str("view", OwnerViewName(doc, dimension))));
                }
            }

            // --- tags, and the ones that lost their host -------------------
            var tags = 0;
            var orphanTags = 0;
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(IndependentTag))
                                        .WhereElementIsNotElementType())
            {
                var tag = element as IndependentTag;
                if (tag == null) continue;
                tags++;
                if (IsOrphan(tag)) orphanTags++;
            }

            // --- text and keynotes ----------------------------------------
            var textNotes = Count(doc, typeof(TextNote));
            var keynotes = CountCategory(doc, BuiltInCategory.OST_KeynoteTags);

            return Json.Ok(
                Json.Arr("overriddenExamples", overriddenExamples),
                Json.Num("dimensions", dimensions),
                Json.Num("dimensionsOverridden", overridden),
                Json.Num("tags", tags),
                Json.Num("tagsWithNoHost", orphanTags),
                Json.Num("textNotes", textNotes),
                Json.Num("keynotes", keynotes),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Str("reads", Reads(dimensions, overridden, tags,
                                        orphanTags, textNotes)));
        }

        /// <summary>What was found, with the overridden dimensions leading.</summary>
        private static string Reads(int dimensions, int overridden, int tags,
                                    int orphanTags, int textNotes)
        {
            var said = new List<string>();

            if (overridden > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} of {1} dimension(s) are OVERRIDDEN - typed over with "
                  + "text rather than reporting what they measure. On the "
                  + "drawing they look exactly like a real dimension. One of "
                  + "these says 2400 on a wall that is 2100, survives every "
                  + "model change because nothing recomputes it, is checked by "
                  + "nobody because it looks correct, and gets built. **Nothing "
                  + "in Revit lists them.** The text each one shows is given "
                  + "above so you can go and look", overridden, dimensions));
            }

            if (orphanTags > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} of {1} tag(s) have lost what they were tagging. Revit "
                  + "warns once, at the moment of deletion, and never again - so "
                  + "the warning gets dismissed on a busy day and the empty tag "
                  + "stays on the sheet", orphanTags, tags));
            }

            if (said.Count == 0)
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "{0} dimension(s), none overridden; {1} tag(s), all still "
                  + "attached; {2} text note(s). The values shown are Revit's "
                  + "own formatted strings, taken as they are - nothing here "
                  + "re-derives a length, which is how a decimal-feet number "
                  + "would get loose.", dimensions, tags, textNotes);
            }

            return string.Join(". ", said.ToArray())
                 + ". Values are Revit's own formatted strings, taken as they "
                 + "are: nothing here parses a dimension back into a number or "
                 + "compares it against a measurement, only whether Revit "
                 + "considers it overridden. Nothing was changed.";
        }

        /// <summary>
        /// The text a dimension has been typed over with, or null when it is
        /// reporting what it measures.
        ///
        /// `ValueOverride` is Revit's own answer to exactly this question, so
        /// it is asked rather than inferred by comparing a shown value against
        /// a computed one - which would mean re-deriving a length, in decimal
        /// feet, which is the defect this file most needs to avoid.
        ///
        /// A dimension with several segments carries its overrides on the
        /// segments instead, and those are checked too: a multi-segment
        /// dimension with one typed-over segment is the same lie in a form
        /// that is harder to see.
        /// </summary>
        private static string OverrideOf(Dimension dimension)
        {
            try
            {
                var typed = dimension.ValueOverride;
                if (!string.IsNullOrEmpty(typed)) return typed;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                // Ask the segments instead.
            }

            try
            {
                var segments = dimension.Segments;
                if (segments == null) return null;
                foreach (DimensionSegment segment in segments)
                {
                    if (segment == null) continue;
                    var typed = segment.ValueOverride;
                    if (!string.IsNullOrEmpty(typed)) return typed;
                }
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
            catch (InvalidOperationException) { return null; }

            return null;
        }

        /// <summary>
        /// Has this tag lost what it was tagging?
        ///
        /// `GetTaggedLocalElementIds` is the 2022-and-later spelling and is
        /// what the version-support skill records as the replacement for
        /// `TaggedElementId`. It is used unconditionally because it exists
        /// across the supported span - confirmed by `check-api-surface.py`
        /// rather than assumed.
        /// </summary>
        private static bool IsOrphan(IndependentTag tag)
        {
            try
            {
                var hosts = tag.GetTaggedLocalElementIds();
                if (hosts == null || hosts.Count == 0) return true;
                foreach (var id in hosts)
                {
                    if (id != null && id != ElementId.InvalidElementId) return false;
                }
                return true;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
            catch (InvalidOperationException) { return false; }
        }

        /// <summary>How many elements of one class are placed.</summary>
        private static int Count(Document doc, Type ofClass)
        {
            try
            {
                var found = new FilteredElementCollector(doc)
                                .OfClass(ofClass)
                                .WhereElementIsNotElementType()
                                .ToElementIds();
                return found == null ? 0 : found.Count;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return 0; }
        }

        /// <summary>How many elements of one category are placed.</summary>
        private static int CountCategory(Document doc, BuiltInCategory category)
        {
            try
            {
                var found = new FilteredElementCollector(doc)
                                .OfCategory(category)
                                .WhereElementIsNotElementType()
                                .ToElementIds();
                return found == null ? 0 : found.Count;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return 0; }
        }

        /// <summary>The view an annotation lives on, or null.</summary>
        private static string OwnerViewName(Document doc, Element element)
        {
            try
            {
                var id = element.OwnerViewId;
                if (id == null || id == ElementId.InvalidElementId) return null;
                var view = doc.GetElement(id) as View;
                if (view == null) return null;
                return string.IsNullOrEmpty(view.Name) ? "(unnamed)" : view.Name;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }
    }
}
