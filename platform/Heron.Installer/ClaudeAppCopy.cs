// Heron-Agent:  HERON-INS-ENV-002
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.IO;

namespace Heron.Installer
{
    /// <summary>
    /// What the installer needs to look at to tell whether it is running
    /// inside a Microsoft Store Claude app's private copy of AppData.
    ///
    /// AN INTERFACE so the deciding below runs against a fake on a machine
    /// with no Windows and no Claude. The real one is WindowsClaudeAppLook in
    /// WindowsAdapters.cs.
    /// </summary>
    public interface IClaudeAppLook
    {
        /// <summary>%APPDATA% as this process sees it, or null.</summary>
        string Roaming { get; }

        /// <summary>%LOCALAPPDATA% as this process sees it, or null.</summary>
        string Local { get; }

        /// <summary>
        /// The LocalCache folder of every Store-built Claude app for this
        /// user - where Windows keeps what Claude's children wrote under
        /// AppData. Empty on a PC with no such app.
        /// </summary>
        IReadOnlyList<string> Copies();

        /// <summary>The names directly inside a folder, or none when it cannot be read.</summary>
        IReadOnlyList<string> Names(string folder);

        /// <summary>True when the two paths open ONE thing on disk, false when two, null when it cannot tell.</summary>
        bool? SameEntry(string first, string second);
    }

    /// <summary>
    /// Whether this run is inside a Microsoft Store Claude app's private copy
    /// of AppData - Q-63, rows 5b-416 to 5b-419.
    ///
    /// WHAT HAPPENS THERE. When the Claude desktop app is the Microsoft Store
    /// (MSIX) build, Windows sends every new file its child processes write
    /// under %APPDATA% and %LOCALAPPDATA% to the app's own
    /// %LOCALAPPDATA%\Packages\AnthropicPBC.Claude_...\LocalCache, and shows
    /// only those processes the two laid over each other. Claude's own shell
    /// is such a child - which is where an AI runs heron-install from.
    ///
    /// SO AN INSTALL RUN FROM IN THERE LANDS WHERE NOTHING ELSE LOOKS.
    /// Reasoned from what row 5b-416 measured on the owner's laptop on
    /// 2026-10-10, NOT tried: the add-in's files go under
    /// %APPDATA%\Autodesk\Revit\Addins, Revit is not a child of the app, and
    /// would never see them; `pip install --user` puts the packages in the
    /// copy only, which is exactly row 5b-417. Both doors therefore refuse to
    /// run from inside and say where to run from instead. Run from Explorer
    /// or from the person's own terminal, the same install lands in the real
    /// folders - and Claude's children read the real folders through the
    /// overlay, which is why Revit tools work there today.
    ///
    /// THE TEST IS tools/heron-ready.py's inside_claude_app(), in C#: inside
    /// the app, a name in the copy and the same name under %APPDATA% open as
    /// ONE thing on disk; outside, they are two, or the second is not there.
    /// A packaged app's child has no package identity to ask for -
    /// GetCurrentPackageFullName answered 15700, no package, from inside
    /// Claude on the owner's laptop that day - so this is what there is.
    ///
    /// IT READS AND NEVER WRITES. And when it cannot tell, it says outside:
    /// a refusal on a PC where the install would have worked is the worse of
    /// the two mistakes, and the install from outside is the supported route
    /// anyway.
    /// </summary>
    public static class ClaudeAppCopy
    {
        /// <summary>
        /// The Claude app's folder under %LOCALAPPDATA%\Packages when it is
        /// the Store build; the letters after the underscore are Windows'.
        /// The same pattern as tools/heron-ready.py's CLAUDE_PACKAGES.
        /// </summary>
        public const string PackagePattern = "AnthropicPBC.Claude_*";

        /// <summary>
        /// What either door says instead of installing. What happened and what
        /// to do next - docs/14 - and the one supported route, in plain words.
        /// </summary>
        public const string Refusal =
            "Heron was not installed. This is running inside the Claude app (the Microsoft Store " +
            "build), which keeps its own private copy of AppData: an add-in installed from here " +
            "would land where Revit never looks, and Heron's Python packages where your own " +
            "terminal cannot see them. Nothing was changed. Close this, then double-click " +
            "HeronInstaller.exe in the Heron folder from File Explorer - or run heron-install " +
            "from a terminal you opened yourself, outside Claude. Claude sees what is installed " +
            "that way.";

        public static bool Inside(IClaudeAppLook look)
        {
            if (look == null) return false;

            foreach (var copy in look.Copies())
            {
                if (Same(look, copy, "Roaming", look.Roaming)) return true;
                if (Same(look, copy, "Local", look.Local)) return true;
            }
            return false;
        }

        private static bool Same(IClaudeAppLook look, string copy, string side, string real)
        {
            if (string.IsNullOrEmpty(real)) return false;

            var mine = Path.Combine(copy, side);
            foreach (var name in look.Names(mine))
            {
                // ONE IS ENOUGH. Inside, every name in the copy is the same
                // thing under AppData; outside, none is.
                if (look.SameEntry(Path.Combine(real, name), Path.Combine(mine, name)) == true)
                    return true;
            }
            return false;
        }
    }
}
