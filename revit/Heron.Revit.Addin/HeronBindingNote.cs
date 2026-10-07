// Heron-Agent:  none
// Heron-Step:   14
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System.Collections;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// How the binding note counts what a fragment was actually given.
    ///
    /// SPLIT OUT OF RevitFragment.cs SO IT CAN BE PROVED WITHOUT REVIT, the
    /// same move HeronStackGuard.cs and HeronActivityBanner.cs already make
    /// and for the reason Heron.StackGuard.TestHost's own project file gives:
    /// referencing the add-in would drag in RevitAPI.dll and make the one
    /// testable piece untestable on any machine without Revit. Nothing here
    /// touches an Autodesk type - it counts two collections - so it is linked
    /// BY SOURCE into a test host and there is exactly ONE copy of the rule.
    ///
    /// THE DEFECT IT EXISTS FOR IS FRAGMENT-ISSUES ROW 75. RevitFragment
    /// revives a carried value by walking each ElementId through
    /// doc.GetElement(id) on the HOST document, swallowing the miss and
    /// keeping what resolved. Zero survivors is handled honestly - the caller
    /// says "nothing usable survived". A PARTIAL revival was not: 1128 linked
    /// walls carried over, two ids happened to name real elements in the host
    /// document, and the note read "elements from select-from-link (2)",
    /// which is indistinguishable from a deliberate narrowing to two.
    /// </summary>
    internal static class HeronBindingNote
    {
        /// <summary>
        /// " (N)", or " (N of M)" when M were offered and N survived.
        ///
        /// IT REPORTS AND DOES NOT REFUSE. The wrong elements are still bound;
        /// stopping that needs the carried value to say which DOCUMENT it came
        /// from, which is row 75's real repair. Naming the loss is the half
        /// that cannot break a caller relying on today's behaviour.
        ///
        /// NO THRESHOLD, DELIBERATELY. "Refuse below some percentage" needs a
        /// floor, and R-60 says a floor is derived from a measurement or it is
        /// not set at all. Reporting both numbers needs neither.
        ///
        /// A non-collection answers "" exactly as before, so a scalar need
        /// gains no parenthesis it did not have.
        /// </summary>
        internal static string Size(object shaped, object before)
        {
            var list = shaped as ICollection;
            if (list == null) return "";

            var was = before as ICollection;
            if (was != null && was.Count != list.Count)
                return " (" + list.Count + " of " + was.Count + ")";

            return " (" + list.Count + ")";
        }

        /// <summary>
        /// What an ABSENT `source: request` need binds to, or null to refuse
        /// it as before.
        ///
        /// ONE CASE, AND IT IS D-59's. A switch a modeller did not mention is
        /// a switch left OFF - "include the links" not said means host only,
        /// which is what D-59 decided and what every proof of the fragments
        /// that take it measured. Until this existed the binder refused every
        /// request need that arrived without a value (FRAGMENT-ISSUES row
        /// 5b-227), so adding `includeLinks` to a fragment would have stopped
        /// it running for every caller who did not know the switch was there.
        ///
        /// ONLY A `bool` DECLARED `optional: true`. A view, a name or a
        /// number has no value that means "not asked", and inventing one is
        /// how a job runs against the wrong thing and reports success - so
        /// those still refuse, and so does a bool nobody marked optional. The
        /// three cards row 5b-227 found carrying the key on a View or a string
        /// are refused exactly as they were.
        ///
        /// Returns the C# literal to bind, which the caller writes into the
        /// generated prologue. Nothing here touches an Autodesk type, so it is
        /// proved without Revit in Heron.BindingNote.TestHost.
        /// </summary>
        internal static string AbsentValue(string type, string optional)
        {
            if (!string.Equals((optional ?? "").Trim(), "true", System.StringComparison.OrdinalIgnoreCase))
                return null;

            return (type ?? "").Trim() == "bool" ? "false" : null;
        }

        /// <summary>
        /// What an element list the MODEL would fill binds to when nothing
        /// fills it - no chain, no selection, no creator's output - or null
        /// to refuse it as needs_unbound, as before.
        ///
        /// ONE CASE: A READ OF THE WHOLE PROJECT THAT CAN ALSO BE POINTED AT A
        /// SELECTION. REPORT_LOCATION version 4 answers "where is this
        /// project" - the site, True North - and, for whatever is selected,
        /// where each element is. Asked with nothing selected it was refused
        /// before it ran, "elements was never supplied", which is right for a
        /// tool whose whole answer is about the elements and wrong for one
        /// whose answer is the project. The owner chose, on 2026-10-06, to
        /// widen that tool rather than add a second reader of the site.
        ///
        /// ONLY AN ELEMENT LIST THE CARD MARKS `optional: true`, and only
        /// once the chain, the selection and a creator's output have all been
        /// asked - so it never wins over a real value. Every other unfilled
        /// need still refuses: a fragment that had not said "nothing is a
        /// fine answer" would report 0 results, which reads as "there was
        /// nothing to find" rather than "nobody was asked". A list of
        /// ElementId is not an element list here, as in IsElementList.
        ///
        /// Returns the C# expression to bind, which the caller writes into the
        /// generated prologue. Nothing here touches an Autodesk type, so it is
        /// proved without Revit in Heron.BindingNote.TestHost.
        /// </summary>
        internal static string EmptyWhenAbsent(string type, string optional)
        {
            if (!string.Equals((optional ?? "").Trim(), "true", System.StringComparison.OrdinalIgnoreCase))
                return null;

            var shape = (type ?? "").Replace(" ", "");
            if (shape.IndexOf("ElementId", System.StringComparison.Ordinal) >= 0) return null;
            switch (shape)
            {
                case "IList<Element>":
                case "List<Element>":
                case "ICollection<Element>":
                case "IEnumerable<Element>":
                    return "new List<Element>()";
                default:
                    return null;
            }
        }
    }
}
