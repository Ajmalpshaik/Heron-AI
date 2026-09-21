// Heron-Agent:  HERON-REVIT-CMP-021
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// What a fragment finds already in scope - the host's half of the
    /// contract every fragment is written against.
    ///
    /// PUBLIC, AND TOP LEVEL, AND BOTH WERE LEARNED BY RUNNING IT. This began
    /// as a public nested class inside the internal RevitFragment, which
    /// compiles perfectly and then fails at the machine with:
    ///
    ///     error CS0122: 'RevitFragment.FragmentGlobals.doc' is inaccessible
    ///                   due to its protection level
    ///
    /// A script is compiled into ITS OWN ASSEMBLY. Public members of a public
    /// type nested in an internal one are not reachable from outside the
    /// declaring assembly, so every fragment failed on its first line - the
    /// one naming `doc`. Nothing on the build side could see it: the add-in
    /// compiled clean, and so did EVERY fragment against the gate's own
    /// wrapper, because that wrapper puts them in the SAME assembly.
    ///
    /// That is the exact failure D-28 warned the compile gate could not cover,
    /// arriving in the first minute of the first real run.
    ///
    /// FIELDS, NOT PROPERTIES. Roslyn exposes the globals object's members as
    /// bare identifiers, and a fragment is written to assume `doc` exists with
    /// no wrapper of its own. Fields keep that reading exact.
    /// </summary>
    public sealed class HeronFragmentGlobals
    {
        /// <summary>The open model. Every fragment needs this one.</summary>
        public Document doc;

        /// <summary>
        /// The UI document, for the few fragments that legitimately reach the
        /// interface - a selection, a view the user is looking at.
        /// </summary>
        public UIDocument uidoc;

        /// <summary>
        /// The application, for fragments that read across documents rather
        /// than inside one.
        /// </summary>
        public Autodesk.Revit.ApplicationServices.Application app;

        /// <summary>
        /// EVERY OTHER `needs` THE HOST HAS TO BIND, by the name the contract
        /// gives it. A fragment never touches this - RevitFragment generates a
        /// typed local for each entry and puts it in front of the source, so
        /// the snippet still reads `elements` and not a dictionary lookup.
        ///
        /// WHY A BAG AND NOT MORE FIELDS. The obvious move is a field per
        /// name, and it does not survive contact with the library: the
        /// host-sourced needs are `elements`, `view`, and then `equipment`,
        /// `targets`, `first`, `second`, `openings`, `values`, `blank` - role
        /// names a fragment gives to whatever was handed to it. A field per
        /// role means editing this file, rebuilding the add-in and restarting
        /// Revit every time somebody writes a fragment that names its input
        /// something new, which is not a contract, it is a queue.
        ///
        /// THE GENERATED PROLOGUE IS THE SAME IDEA AS THE COMPILE GATE'S
        /// METHOD PARAMETERS, and that is the point rather than a
        /// coincidence. tools/check-fragments-compile.py wraps each snippet in
        /// a method whose parameters ARE its declared needs. Until this bag
        /// existed the executor could supply three names, so 196 fragments
        /// compiled green against a scope the machine could not reproduce -
        /// the `needs` version of the drift tests/test_fragment_imports.py
        /// guards for namespaces. Generating the scope from the same contract
        /// on both sides is what makes them agree by construction instead of
        /// by two lists somebody has to keep level.
        ///
        /// The double underscore is deliberate: it must never collide with a
        /// name a fragment declares, and no contract in the library uses one.
        /// </summary>
        public System.Collections.Generic.IDictionary<string, object> __heron;
    }
}
