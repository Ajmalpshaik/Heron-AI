// Heron-Agent:  none
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Heron.Tools
{
    /// <summary>
    /// STAGE 2 SHAPE PROOF - a SECOND PIECE OF THE SAME TAB.
    ///
    /// docs/work-notes/plans/plugin-extension/02-implementation.md, Stage 2,
    /// second half. THIS IS THE HALF THAT BREAKS FIRST.
    ///
    /// THE QUESTION
    /// ------------
    /// S3 / D-89 lets the "Heron" tab be built by the AI Bridge piece, the
    /// tools piece, or BOTH. Three installs have to work:
    ///
    ///     AI Bridge only   the Heron tab, AI Bridge panel only - today
    ///     Tools only       the Heron tab EXISTS, with NO AI Bridge panel
    ///     Both             ONE Heron tab with both panels - never two tabs
    ///                      carrying the same name
    ///
    /// TOOLS-ONLY IS THE CASE THAT WILL BREAK FIRST, and it is the one R-34
    /// promises: a site modeller may want Heron's tools and refuse the AI.
    /// That is a supported install, not a degraded one.
    ///
    /// WHY IT BREAKS
    /// -------------
    /// Revit decides which add-in loads first and never says. So whichever
    /// piece gets there first has to CREATE the tab, and the other has to
    /// JOIN it. Revit's CreateRibbonTab THROWS when the tab already exists.
    ///
    /// A piece that assumes it is first crashes when it is second.
    /// A piece that assumes it is second draws no tab when it is first.
    /// NEITHER assumption is allowed here (R-35).
    ///
    /// The shape that satisfies both is four lines, and it is the same four
    /// HeronApplication.BuildRibbon() has had since Step 1: try to create,
    /// swallow the "already exists" exception, then create the panel.
    ///
    /// DELETE THIS WHOLE PROJECT when Stage 2 closes and the real Heron tools
    /// are built. It is PROVING in platform/heron-products.json - the state
    /// that means "the files exist, and no user may be offered them".
    /// </summary>
    public sealed class HeronToolsApplication : IExternalApplication
    {
        /// <summary>
        /// THE SAME STRING HeronApplication USES, character for character.
        ///
        /// It is repeated rather than shared, and that is deliberate: a
        /// throwaway proof must not make the real add-in depend on it, and a
        /// project reference would survive this project's deletion as a
        /// dangling one.
        ///
        /// The cost of repeating it is that a one-character difference gives
        /// TWO tabs both called Heron, which is the exact failure this stage
        /// exists to catch and is easy to miss on screen. So it is not left
        /// to care: tests/test_ribbon_tab_sharing.py reads both files and
        /// fails if the two strings ever stop matching.
        /// </summary>
        private const string TabName = "Heron";

        /// <summary>
        /// A DIFFERENT panel from the bridge's "AI Bridge", because both
        /// panels have to be able to sit on one tab at the same time.
        /// </summary>
        private const string PanelName = "Tools";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                BuildRibbon(application);
                return Result.Succeeded;
            }
            catch
            {
                // Never an unhandled exception out of OnStartup. A proof that
                // fails should fail visibly as a missing tab, which IS the
                // answer, not as a Revit error box naming a class.
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private static void BuildRibbon(UIControlledApplication application)
        {
            // THE FOUR LINES THE WHOLE STAGE TURNS ON.
            //
            // Loaded FIRST:  CreateRibbonTab succeeds and this piece makes the
            //                Heron tab. The bridge then joins it, because the
            //                bridge catches the same exception.
            // Loaded SECOND: CreateRibbonTab throws, this catches it, and the
            //                panel below is added to the tab the bridge made.
            // Alone:         CreateRibbonTab succeeds and the Heron tab exists
            //                with tools and no AI Bridge panel - R-34.
            //
            // Autodesk.Revit.Exceptions.ArgumentException is the one Revit
            // raises for a duplicate tab. It is caught by its full name
            // because System.ArgumentException is a different type and
            // catching that one would catch nothing.
            //
            // The catch is narrow ON PURPOSE. A bare catch here would swallow
            // a genuine failure to build the tab and leave a silent, missing
            // ribbon - the failure mode with no message, which is the worst
            // kind to hand a modeller.
            try { application.CreateRibbonTab(TabName); }
            catch (Autodesk.Revit.Exceptions.ArgumentException) { /* tab exists */ }

            var panel = application.CreateRibbonPanel(TabName, PanelName);
            var assemblyPath = typeof(HeronToolsApplication).Assembly.Location;

            var button = new PushButtonData(
                "HeronToolsProof",
                "Proof",
                assemblyPath,
                typeof(HeronToolsProofCommand).FullName);

            button.ToolTip = "Does nothing. It is here to prove this panel joined the Heron tab.";
            button.LongDescription =
                "This button belongs to a shape proof, not to a tool. It exists " +
                "to show that the Heron tab can be built by the tools on their " +
                "own, by the AI Bridge on its own, or by both at once as ONE " +
                "tab.\n\n" +
                "It is removed when the real Heron tools are built.";

            panel.AddItem(button);
        }
    }

    /// <summary>
    /// The dummy button. It says it was pressed, and that is all.
    /// </summary>
    /// <remarks>
    /// Touches no document and opens no transaction, so it declares Manual
    /// rather than leaving Revit to allow a write nothing here wants.
    ///
    /// The dialog is the EVIDENCE for this stage rather than a courtesy - a
    /// button that answers silently answers nothing - and it goes when the
    /// proof does.
    /// </remarks>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class HeronToolsProofCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData,
                              ref string message,
                              ElementSet elements)
        {
            TaskDialog.Show(
                "Heron",
                "This is the Heron tools proof button.\n\n" +
                "It does nothing on purpose. Seeing it on the Heron tab means " +
                "the tools panel and the AI Bridge panel can share one tab - " +
                "or that the tools built the tab on their own, if the AI " +
                "Bridge is not installed.");

            return Result.Succeeded;
        }
    }
}
