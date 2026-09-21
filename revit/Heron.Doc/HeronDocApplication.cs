// Heron-Agent:  none
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Heron.Doc
{
    /// <summary>
    /// STAGE 2 SHAPE PROOF - a SECOND Heron tab, and nothing else.
    ///
    /// docs/work-notes/plans/plugin-extension/02-implementation.md, Stage 2.
    ///
    /// THE ONE QUESTION THIS ANSWERS
    /// -----------------------------
    /// Can two Heron tabs live in one Revit? Every stage after this one
    /// assumes yes. Finding out costs one dummy button here; finding out
    /// after the installer window is built costs the installer window.
    ///
    /// So this deliberately does NOTHING ELSE. No bridge, no fragments, no
    /// document, no transaction, no real tool. A shape proof with a real tool
    /// mixed into it leaves BOTH unproven the moment it fails, because
    /// nothing says which half broke.
    ///
    /// DELETE THIS WHOLE PROJECT when Stage 2 closes and the real Heron Doc
    /// is built. It is listed as PROVING in platform/heron-products.json,
    /// which is the state that means "the files exist, and no user may be
    /// offered them".
    /// </summary>
    public sealed class HeronDocApplication : IExternalApplication
    {
        /// <summary>
        /// Its OWN tab, which is the whole point - S1 / D-87 says one product
        /// is one tab. This string must NOT match Heron's tab: if it did,
        /// this project would be proving the Heron.Tools case instead.
        /// </summary>
        private const string TabName = "Heron Doc";

        private const string PanelName = "Proof";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                BuildRibbon(application);
                return Result.Succeeded;
            }
            catch
            {
                // A shape proof that throws out of OnStartup would take the
                // whole add-in down and tell Revit's user nothing useful.
                // Failing to draw a proof tab is worth a Failed result -
                // that IS the answer Stage 2 is asking for - but it is never
                // worth an unhandled exception.
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private static void BuildRibbon(UIControlledApplication application)
        {
            // NEITHER PIECE MAY ASSUME IT IS FIRST (R-35).
            //
            // Revit chooses the load order of add-ins and does not say what it
            // chose. CreateRibbonTab THROWS when the tab already exists, so
            // the only correct shape is: try to create it, and carry on if
            // somebody else already did. HeronApplication.BuildRibbon() has
            // caught exactly this since Step 1.
            //
            // Nothing else here needs it - "Heron Doc" is this product's own
            // tab and no other add-in builds it. It is written this way
            // anyway, because the day a second Heron Doc piece is added is
            // the day an assumption made here becomes a bug there.
            try { application.CreateRibbonTab(TabName); }
            catch (Autodesk.Revit.Exceptions.ArgumentException) { /* tab exists */ }

            var panel = application.CreateRibbonPanel(TabName, PanelName);
            var assemblyPath = typeof(HeronDocApplication).Assembly.Location;

            var button = new PushButtonData(
                "HeronDocProof",
                "Proof",
                assemblyPath,
                typeof(HeronDocProofCommand).FullName);

            button.ToolTip = "Does nothing. It is here to prove this tab loaded.";
            button.LongDescription =
                "This button belongs to a shape proof, not to a tool. It exists " +
                "to show that a second Heron tab can appear in Revit beside the " +
                "first one.\n\n" +
                "It is removed when the real Heron Doc is built.";

            panel.AddItem(button);
        }
    }

    /// <summary>
    /// The dummy button. It says it was pressed, and that is all.
    /// </summary>
    /// <remarks>
    /// Transaction.Manual and ReadOnly on purpose: this touches no document
    /// and opens no transaction, so Revit is told so rather than being left
    /// to allow a write nothing here wants.
    ///
    /// A dialog that only says "it worked" is normally refused by the add-in
    /// conventions, and rightly. THIS ONE IS THE EVIDENCE, not a courtesy:
    /// Stage 2 asks whether the tab loaded and the button is wired, and a
    /// button that answers silently answers nothing. It goes when the proof
    /// does.
    /// </remarks>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class HeronDocProofCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData,
                              ref string message,
                              ElementSet elements)
        {
            TaskDialog.Show(
                "Heron Doc",
                "This is the Heron Doc proof button.\n\n" +
                "It does nothing on purpose. Seeing it means the Heron Doc " +
                "tab loaded beside the Heron tab, which is what this stage " +
                "was built to find out.");

            return Result.Succeeded;
        }
    }
}
