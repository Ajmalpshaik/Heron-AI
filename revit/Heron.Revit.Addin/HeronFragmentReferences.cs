// Heron-Agent:  HERON-REVIT-CMP-021
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System.Linq;
using System.Reflection;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// The assemblies every fragment script is compiled against - ONE list,
    /// read by RevitFragment.BuildOptions and by
    /// tests/Heron.StackGuard.TestHost, which links this file by source.
    ///
    /// WHY IT IS A FILE OF ITS OWN - FRAGMENT-ISSUES row 5b-1000. The list
    /// lived inside BuildOptions and the guard's test host kept a list of its
    /// own. The stack guard writes
    /// `global::Heron.Revit.Addin.HeronRunClock.Check();` into every script;
    /// the host's list carried the run clock's assembly and the add-in's did
    /// not, so every guarded script compiled in the test and none compiled in
    /// Revit (CS0400, Revit 2024, 2026-10-17). One list cannot disagree with
    /// itself.
    ///
    /// THE RUN CLOCK'S ASSEMBLY IS NOT THERE BY WAY OF THE GLOBALS TYPE. The
    /// scripting API references the globals type's assembly under an alias of
    /// its own, never the global one, so `global::Heron` names nothing unless
    /// this list names it.
    ///
    /// THE REVIT API IS THE CALLER'S. RevitAPI and RevitAPIUI are handed in,
    /// never named here: naming their types would put the Revit API in a file
    /// the test host compiles, and that host runs where there is no Revit.
    /// </summary>
    internal static class HeronFragmentReferences
    {
        internal static Assembly[] With(params Assembly[] revitApi)
        {
            return new[]
            {
                typeof(object).Assembly,                    // mscorlib
                typeof(Enumerable).Assembly,                // System.Core
                typeof(HeronRunClock).Assembly,             // this add-in - the run clock's check
            }
            .Concat(revitApi)
            .ToArray();
        }
    }
}
