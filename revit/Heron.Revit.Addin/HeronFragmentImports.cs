// Heron-Agent:  HERON-REVIT-CMP-021
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

namespace Heron.Revit.Addin
{
    /// <summary>
    /// What a fragment may assume is already imported. ONE list, in one place.
    ///
    /// THIS IS A CONTRACT WITH tools/check-fragments-compile.py, and the gate
    /// says so itself:
    ///
    ///     "THIS LIST IS A CONTRACT WITH UNBUILT WORK. It declares what a
    ///      fragment may assume is in scope, so D-28's in-process Roslyn
    ///      executor must supply the same set. A namespace added here and not
    ///      there compiles green and fails at the PC."
    ///
    /// The executor is now built, so the two halves of that sentence exist and
    /// can disagree. tests/test_fragment_imports.py compares them and fails if
    /// they ever do - because the failure it prevents is the worst kind: 343
    /// fragments passing a green gate and one of them throwing at the machine,
    /// for a reason no test on this side could see.
    ///
    /// ADDING A NAMESPACE IS A PROMISE, NOT A CONVENIENCE. Whatever is added
    /// here, the executor must supply and the gate must compile against. A
    /// namespace whose types a fragment could reach ANOTHER way does not
    /// belong - the Revit exceptions namespace was deliberately left out for
    /// exactly that reason.
    ///
    /// IT LIVES IN revit/ BECAUSE IT NAMES THE REVIT API, and the structure
    /// gate refused it anywhere else the moment it was written into platform/.
    /// That refusal is correct even though these are only strings: docs/16
    /// section 4 keeps the adapter boundary at the assembly, and a list of
    /// Revit namespaces is Revit knowledge wherever it is stored.
    /// </summary>
    public static class HeronFragmentImports
    {
        /// <summary>
        /// In the same order as the gate's own list, so a diff between the two
        /// reads as a diff rather than as a reordering.
        /// </summary>
        public static readonly string[] Namespaces =
        {
            "System",
            "System.Collections.Generic",
            "System.Linq",
            "Autodesk.Revit.ApplicationServices",
            "Autodesk.Revit.DB",

            // Rooms live here - Room.IsPointInRoom is the API designed for the
            // question FILTER_ELEMENTS_IN_ROOM asks, and hand-rolling
            // point-in-polygon is how an L-shaped room gets answered wrongly.
            "Autodesk.Revit.DB.Architecture",
            "Autodesk.Revit.DB.Mechanical",
            "Autodesk.Revit.DB.Plumbing",
            "Autodesk.Revit.DB.Electrical",
            "Autodesk.Revit.DB.Structure",
            "Autodesk.Revit.UI",

            // Extensible storage - the data an add-in writes INTO the file,
            // invisible everywhere in Revit's own interface.
            "Autodesk.Revit.DB.ExtensibleStorage",

            // Revit's analysis visualisation - the gradient overlay with a
            // legend that a value per element is painted with.
            "Autodesk.Revit.DB.Analysis",
        };
    }
}
