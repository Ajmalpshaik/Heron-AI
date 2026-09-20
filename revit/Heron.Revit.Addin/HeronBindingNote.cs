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
    }
}
