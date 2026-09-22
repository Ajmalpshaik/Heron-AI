// Heron-Agent:  none
// Heron-Step:   14
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// What the caller supplied that nothing read, said in the words of the
    /// person who typed it.
    ///
    /// THE DEFECT IT EXISTS FOR. RevitFragment.BindNeeds walks the needs it
    /// EXPECTS and looks each one up in what the caller supplied. It never
    /// walked the other way, so a supplied name matching no need was read by
    /// nothing, reported by nothing, and the caller was told the whole thing
    /// had been applied. `categoryName` for `categories` is the common shape:
    /// one character wrong, a clean success, and a model that did not change
    /// the way somebody believed it had.
    ///
    /// It is the same silence that let a value split on a semicolon go out as
    /// a success on 2026-09-22 - walls asked to go red stayed white through
    /// four writes, and every reply said it had worked.
    ///
    /// IT REPORTS AND DOES NOT REFUSE. A stray name cannot be refused, because
    /// the setup steps and the fragment share ONE supplied dictionary: a value
    /// this fragment ignores may be exactly what its arrangement step consumes.
    /// So the judgement is made once, after every step has bound, and the run
    /// is reported as it actually went.
    ///
    /// SPLIT OUT OF RevitFragment.cs SO IT CAN BE PROVED WITHOUT REVIT, the
    /// same move HeronBindingNote.cs makes and for the same reason: nothing
    /// here touches an Autodesk type, so it is linked BY SOURCE into a test
    /// host and there is exactly ONE copy of the rule.
    /// </summary>
    internal static class HeronIgnoredValues
    {
        /// <summary>
        /// The supplied names that nothing read, with what this run does take.
        /// Null when every value landed, so a caller with nothing to hear is
        /// told nothing.
        /// </summary>
        /// <param name="supplied">Every name the caller sent, in this request.</param>
        /// <param name="consumed">Names some step actually read.</param>
        /// <param name="askable">
        /// Request-sourced names that were on offer across every step. Named in
        /// the message because by the time this runs the contract is out of
        /// scope, and "it was not read" without "here is what it takes" leaves
        /// the reader exactly where they started.
        /// </param>
        internal static string Describe(IEnumerable<string> supplied,
                                        ICollection<string> consumed,
                                        IEnumerable<string> askable)
        {
            if (supplied == null || consumed == null) return null;

            var spare = new List<string>();
            foreach (var given in supplied)
                if (!string.IsNullOrEmpty(given) && !consumed.Contains(given))
                    spare.Add(given);

            if (spare.Count == 0) return null;
            spare.Sort(StringComparer.Ordinal);

            var takes = new List<string>();
            if (askable != null)
            {
                foreach (var name in askable)
                    if (!string.IsNullOrEmpty(name)) takes.Add(name);
                takes.Sort(StringComparer.Ordinal);
            }

            // WHAT HAPPENED, THEN WHAT TO DO NEXT. The run is reported as it
            // went - this does not undo anything and must not read as a
            // failure - but the value that did nothing is named, because the
            // caller believed it would do something.
            var said = "'" + string.Join("', '", spare.ToArray()) + "' "
                     + (spare.Count == 1 ? "was" : "were")
                     + " supplied and nothing read "
                     + (spare.Count == 1 ? "it" : "them") + ". ";

            said += takes.Count == 0
                ? "This run takes no values from the caller, so whatever was "
                  + "meant by that has not happened."
                : "This run takes '" + string.Join("', '", takes.ToArray())
                  + "'. Check the spelling - a value nothing reads is a value "
                  + "that did not happen, and the rest of the run still did.";
            return said;
        }
    }
}
