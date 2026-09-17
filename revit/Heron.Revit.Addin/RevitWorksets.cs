// Heron-Agent:  HERON-REVIT-WRK-014
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
    /// Worksets, ownership and checkout state. HERON-REVIT-WRK-014, whose row
    /// ends with the sentence that decides how this file is written:
    /// <em>"Ownership failure is a normal outcome."</em>
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit, and on a day another session had
    /// the only Revit open. It COMPILES on 2020 through 2027 and that is the
    /// whole of what is known about it.
    /// ===================================================================
    ///
    /// SOMEBODY ELSE OWNING SOMETHING IS NOT AN ERROR
    /// ------------------------------------------------
    /// On a workshared job, most of the model belongs to somebody else most
    /// of the time. That is the system working. A tool that reports it as a
    /// failure teaches its reader to ignore failures, which is the one thing
    /// a tool must never teach - A18's whole lesson.
    ///
    /// So ownership here is DESCRIBED and never judged. "Owned by someone
    /// else" is a state, reported in the same voice as "free". The only
    /// thing that would be a finding is Heron claiming it can edit something
    /// it cannot, and this file claims nothing at all, because it writes
    /// nothing at all.
    ///
    /// THE FIRST QUESTION IS WHETHER THE MODEL IS WORKSHARED AT ALL
    /// -------------------------------------------------------------
    /// Most Heron models will not be. A family is not, a detail file is not,
    /// and a single-person job often is not. Answering a workset question
    /// about a non-workshared model with an empty list reads as "there are no
    /// worksets", which is true and useless - the useful answer is "this
    /// model is not workshared, so the question does not apply".
    ///
    /// WHY OWNERSHIP IS SAMPLED AND THE SAMPLE IS DECLARED
    /// ----------------------------------------------------
    /// `WorksharingUtils.GetWorksharingTooltipInfo` asks the central model
    /// about ONE element. Calling it for every element of a real job is
    /// thousands of round trips on Revit's own thread, and the conventions
    /// are explicit that nothing may block it: a slow handler freezes the
    /// user interface for whoever is modelling.
    ///
    /// So ownership is read for a BOUNDED sample and the bound is reported
    /// in the answer, every time, as `ownershipSampled` and `ownershipOf`.
    /// A number whose sample is not stated is a number that will be quoted
    /// as if it covered everything. The per-workset ELEMENT counts are not
    /// sampled - they come from a parameter every element already carries,
    /// which costs one pass and no round trips.
    ///
    /// NOTHING IS SET
    /// ----------------
    /// The register gives this row MODIFY - the highest level it could ever
    /// require. Reading requires none of it. Nothing here creates a workset,
    /// opens or closes one, borrows an element, or relinquishes anything.
    /// Relinquishing somebody's borrowed element from under them loses their
    /// unsynchronised work, and a machine that has never opened Revit will
    /// not be the first thing to try it.
    /// </summary>
    internal static class RevitWorksets
    {
        /// <summary>
        /// How many elements to ask the central model about.
        ///
        /// Ownership is one round trip per element. 250 is enough to say
        /// whether a model is mostly free or mostly borrowed, and small
        /// enough that the worst case is a pause rather than a freeze. The
        /// answer always states the sample rather than implying a survey.
        /// </summary>
        private const int OwnershipSample = 250;

        /// <summary>
        /// Every workset in the active document, what is on it, and a sampled
        /// reading of who owns what.
        /// </summary>
        public static string List(UIApplication app)
        {
            var doc = RevitOperations.ActiveDocument(app);
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            if (!IsWorkshared(doc))
            {
                // NOT A FAILURE, AND NOT AN EMPTY LIST. The two read very
                // differently to somebody who just asked.
                return Json.Ok(
                    Json.Arr("worksets", new List<string>()),
                    Json.Num("worksetCount", 0),
                    Json.Bool("workshared", false),
                    Json.Str("document", doc.Title),
                    Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                    Json.Bool("modifiedAnything", false),
                    Json.Str("reads",
                        "This model is not workshared, so worksets, ownership "
                      + "and checkout do not apply to it. That is not an empty "
                      + "list - it is a different question. Families, detail "
                      + "files and single-person jobs are normally like this."));
            }

            var worksets = new List<Workset>();
            try
            {
                var found = new FilteredWorksetCollector(doc)
                                .OfKind(WorksetKind.UserWorkset);
                foreach (var workset in found)
                {
                    if (workset != null) worksets.Add(workset);
                }
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return Json.Error("worksets_unreadable",
                    "Revit would not list the worksets in " + doc.Title
                  + ". Nothing has been sent to Revit and nothing was changed.");
            }

            // ONE PASS, NO ROUND TRIPS. Every element already carries the
            // workset it is on as a parameter; asking that is local.
            var onWorkset = new Dictionary<string, int>(StringComparer.Ordinal);
            var placed = 0;
            var onNoWorkset = 0;
            var sampleIds = new List<ElementId>();
            foreach (var element in new FilteredElementCollector(doc)
                                        .WhereElementIsNotElementType())
            {
                if (element == null) continue;
                placed++;

                var key = WorksetKeyOf(element);
                if (key == null) onNoWorkset++;
                else
                {
                    int already;
                    onWorkset[key] = onWorkset.TryGetValue(key, out already)
                                     ? already + 1 : 1;
                }

                if (sampleIds.Count < OwnershipSample) sampleIds.Add(element.Id);
            }

            // THE SAMPLE. Bounded above, and the bound travels in the answer.
            var free = 0;
            var mine = 0;
            var theirs = 0;
            var unknown = 0;
            var owners = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var me = CurrentUser(doc);
            foreach (var id in sampleIds)
            {
                var owner = OwnerOf(doc, id);
                if (owner == null) { unknown++; continue; }
                if (owner.Length == 0) { free++; continue; }

                int seen;
                owners[owner] = owners.TryGetValue(owner, out seen) ? seen + 1 : 1;
                if (me != null && string.Equals(owner, me, StringComparison.OrdinalIgnoreCase))
                    mine++;
                else
                    theirs++;
            }

            var rows = new List<string>();
            var closed = 0;
            foreach (var workset in worksets)
            {
                int held;
                var key = KeyOf(workset);
                if (!onWorkset.TryGetValue(key, out held)) held = 0;
                var open = IsOpen(workset);
                if (!open) closed++;

                rows.Add(Json.Obj(
                    Json.Str("name", NameOf(workset)),
                    Json.Num("elements", held),
                    Json.Bool("open", open)));
            }

            var ownerRows = new List<string>();
            foreach (var pair in owners)
            {
                ownerRows.Add(Json.Obj(
                    Json.Str("name", pair.Key),
                    Json.Num("inSample", pair.Value)));
            }

            return Json.Ok(
                Json.Arr("worksets", rows),
                Json.Arr("ownersInSample", ownerRows),
                Json.Num("worksetCount", rows.Count),
                Json.Num("closedWorksets", closed),
                Json.Num("placedElements", placed),
                Json.Num("onNoWorkset", onNoWorkset),
                Json.Bool("workshared", true),
                Json.Num("ownershipSampled", sampleIds.Count),
                Json.Num("ownershipOf", placed),
                Json.Num("freeInSample", free),
                Json.Num("ownedByMeInSample", mine),
                Json.Num("ownedByOthersInSample", theirs),
                Json.Num("ownershipNotKnownInSample", unknown),
                Json.Str("user", me),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Str("reads", Reads(rows.Count, closed, sampleIds.Count,
                                        placed, theirs, unknown)));
        }

        /// <summary>
        /// What the numbers mean, without calling anybody's ownership a fault.
        /// </summary>
        private static string Reads(int worksets, int closed, int sampled,
                                    int placed, int theirs, int unknown)
        {
            var said = new List<string>();

            said.Add(string.Format(CultureInfo.InvariantCulture,
                "Ownership was read for {0} of {1} placed element(s) - a sample, "
              + "not a survey. Asking the central model costs one round trip per "
              + "element and it runs on Revit's own thread, so a full sweep would "
              + "freeze the model for whoever is working in it",
                sampled.ToString("N0", CultureInfo.InvariantCulture),
                placed.ToString("N0", CultureInfo.InvariantCulture)));

            if (theirs > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} of the sample are owned by somebody else. That is the "
                  + "system working, not a problem - on a workshared job most of "
                  + "the model belongs to somebody else most of the time",
                    theirs.ToString("N0", CultureInfo.InvariantCulture)));
            }

            if (closed > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} workset(s) are CLOSED in this session. Elements on a "
                  + "closed workset are not in the model you are looking at, so "
                  + "any count taken here has left them out", closed));
            }

            if (unknown > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} of the sample would not say who owns them - reported as "
                  + "not known rather than as free, because the two are different "
                  + "and only one of them is safe to act on",
                    unknown.ToString("N0", CultureInfo.InvariantCulture)));
            }

            return string.Join(". ", said.ToArray()) + ". Nothing was changed.";
        }

        /// <summary>Is this document workshared? A refusal to say means no.</summary>
        private static bool IsWorkshared(Document doc)
        {
            try { return doc.IsWorkshared; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
        }

        /// <summary>
        /// Who owns this element: their name, an empty string for nobody, or
        /// null when the question could not be answered.
        ///
        /// THREE OUTCOMES AND NOT TWO. "Free" and "could not tell" look the
        /// same in a boolean and are completely different to act on, so the
        /// null is preserved all the way into the answer rather than being
        /// flattened into "free" on the way.
        /// </summary>
        private static string OwnerOf(Document doc, ElementId id)
        {
            try
            {
                var info = WorksharingUtils.GetWorksharingTooltipInfo(doc, id);
                if (info == null) return null;
                return info.Owner ?? string.Empty;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>The username this Revit session is signed in as, or null.</summary>
        private static string CurrentUser(Document doc)
        {
            try
            {
                var who = doc.Application.Username;
                return string.IsNullOrEmpty(who) ? null : who;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }

        /// <summary>
        /// The workset an element is on, as a string key, or null.
        ///
        /// Read from the parameter every element carries rather than by
        /// asking the central model, because this runs once per element and
        /// the answer is already local.
        /// </summary>
        private static string WorksetKeyOf(Element element)
        {
            try
            {
                var p = element.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
                if (p == null) return null;
                var on = p.AsInteger();
                return on.ToString(CultureInfo.InvariantCulture);
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
        }

        /// <summary>
        /// The same key, from the workset's own side.
        ///
        /// `WorksetId.IntegerValue` and NOT `ElementId.IntegerValue` - they
        /// are different types and only the second one was deprecated at
        /// 2024. That is worth a sentence rather than a silent assumption:
        /// the version-support skill's rule is about element ids overflowing
        /// 32 bits, and a workset id is a small counter that cannot. The
        /// pairing is checked by `check-api-surface.py` across all eight
        /// releases rather than believed.
        /// </summary>
        private static string KeyOf(Workset workset)
        {
            try
            {
                return workset.Id.IntegerValue.ToString(CultureInfo.InvariantCulture);
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return "?"; }
        }

        /// <summary>Is this workset open in this session?</summary>
        private static bool IsOpen(Workset workset)
        {
            try { return workset.IsOpen; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return true; }
        }

        /// <summary>A name, never an exception mid-answer.</summary>
        private static string NameOf(Workset workset)
        {
            try
            {
                return string.IsNullOrEmpty(workset.Name) ? "(unnamed)" : workset.Name;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return "(unnamed)";
            }
        }
    }
}
