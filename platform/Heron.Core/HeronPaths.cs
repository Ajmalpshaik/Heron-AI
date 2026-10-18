// Heron-Agent:  HERON-WSP-PTH-007
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Heron.Core
{
    /// <summary>
    /// Path Manager - the single place that knows where anything lives.
    ///
    /// Enforces the product / data / derived separation from docs/06 section 2
    /// in code rather than in prose. Nothing else may build a Heron path.
    ///
    ///   PRODUCT   Replaced wholesale on update. The user never edits it.
    ///   DATA      Belongs to the user. Must survive every update, uninstall
    ///             and reinstall. The updater must be unable to write here.
    ///   DERIVED   Rebuildable cache and runtime state. Safe to delete at any
    ///             moment; deleting it must always be a valid recovery action.
    ///
    /// This is one of the few things docs/27 calls ruinous to retrofit: once
    /// paths are scattered across twenty files, separating them means finding
    /// every one of them.
    /// </summary>
    public static class HeronPaths
    {
        private const string AppFolder = "Heron";

        /// <summary>PRODUCT - where the running assemblies live.</summary>
        public static string Product
        {
            get
            {
                var asm = Assembly.GetExecutingAssembly().Location;
                return string.IsNullOrEmpty(asm)
                    ? AppDomain.CurrentDomain.BaseDirectory
                    : Path.GetDirectoryName(asm);
            }
        }

        /// <summary>
        /// DATA - %APPDATA%\Heron. The user's own. Roaming, because a
        /// preference or a learned skill should follow them between machines.
        /// </summary>
        public static string Data
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    AppFolder);
            }
        }

        /// <summary>
        /// DERIVED - %LOCALAPPDATA%\Heron. Machine-local and rebuildable.
        ///
        /// Deliberately NOT roaming. In a domain environment with roaming
        /// profiles, %APPDATA% synchronises between machines - and a bridge
        /// file announcing process 24156 on someone else's PC is meaningless
        /// here. Runtime state belongs to the machine, not the person.
        /// </summary>
        public static string Derived
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    AppFolder);
            }
        }

        /// <summary>DERIVED - live bridges announce themselves here.</summary>
        public static string Bridges { get { return Ensure(Path.Combine(Derived, "bridges")); } }

        /// <summary>
        /// DERIVED - one small file per connected Revit saying which model,
        /// which view and what is selected, for the Companion page to read
        /// (D-108). Throwaway state: rewritten on every change, deleted when
        /// the bridge disconnects.
        /// </summary>
        public static string Live { get { return Ensure(Path.Combine(Derived, "live")); } }

        /// <summary>
        /// DERIVED, in the temporary folder - %TEMP%\Heron. Machine-local,
        /// rebuildable and safe to delete, like Derived; somewhere else only
        /// because of what a Microsoft Store Claude does (D-135). Windows keeps a
        /// private copy of %LOCALAPPDATA% for that app and every process it
        /// starts, the chat's MCP process included, and Revit - not such a
        /// process - reads only the real folder; so a file a chat writes under
        /// Derived is one this add-in never sees (FRAGMENT-ISSUES 5b-416). The
        /// temporary folder is not copied: measured on the owner's laptop on
        /// 2026-10-17 from both sides. Only what a chat writes FOR the add-in to
        /// read belongs here.
        ///
        /// IN THE USER'S OWN TEMPORARY FOLDER, NOT THIS PROCESS'S (D-137,
        /// FRAGMENT-ISSUES 5b-1100). Revit runs with a temporary folder of its
        /// own - a GUID-named sub-folder of the user's, new at each start, seen on
        /// the owner's laptop as ...\Temp\63835622-bdd8-4d57-a4a9-19a6afb83cd6 -
        /// while a chat names the user's own; so Path.GetTempPath() alone named a
        /// folder no chat writes, and the Companion button said no chat was
        /// running while four were. UserTemp takes that one GUID part off.
        /// </summary>
        public static string SharedDerived
        {
            get { return Path.Combine(UserTemp(Path.GetTempPath()), AppFolder); }
        }

        /// <summary>
        /// A Revit-style GUID, the way Revit spells its folder: 8-4-4-4-12 hex
        /// digits with hyphens. mcp/client/heron_bridge_client.py GUID_NAME is
        /// the same pattern, and tests/test_data_home.py reads both to hold them
        /// together - so the chat and the add-in take off exactly the same names.
        /// </summary>
        private static readonly Regex GuidName = new Regex(
            @"\A(?:[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12})\z",
            RegexOptions.CultureInvariant);

        /// <summary>
        /// The user's own temporary folder, from the one a process was given:
        /// that folder less its last part when that part is a GUID, once;
        /// otherwise the folder as it is.
        ///
        /// WHY THIS RULE AND NOT THE USER'S TEMP FROM THE REGISTRY
        /// (Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.User)).
        /// This one has already run in a real Revit: create-family-forms finds
        /// %TEMP%\heron-work beside Revit's own folder the same way (row 5b-430;
        /// it parses the name with Guid.TryParse, which takes other spellings
        /// too), and NEEDS-CHECKING DA8 passed with it on the laptop in Revit
        /// 2024.3 on 2026-10-17. It reads nothing but the folder the process was
        /// given, so the chat applies the very same rule to its own and the two
        /// agree whatever that folder is - a terminal server's per-session
        /// Temp\2 included, where the registry would still name Temp (reasoned,
        /// not measured). The registry value is a REG_EXPAND_SZ, absent on some
        /// PCs, that nobody has read from inside Revit or a Store Claude. A
        /// folder not named by a GUID is left alone, so a Revit that runs with
        /// the user's own folder, and every other process, is unchanged.
        /// </summary>
        private static string UserTemp(string temporary)
        {
            if (string.IsNullOrEmpty(temporary)) return temporary;
            var trimmed = temporary.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!GuidName.IsMatch(Path.GetFileName(trimmed) ?? "")) return temporary;
            var parent = Path.GetDirectoryName(trimmed);
            return string.IsNullOrEmpty(parent) ? temporary : parent;
        }

        /// <summary>
        /// DERIVED - one small file per running chat whose Companion page is
        /// up, saying which port it is on and holding a one-time code, so the
        /// Companion button in Revit can open the page (D-109). Written and
        /// deleted by the chat's MCP process; read, never written, by the add-in.
        /// Under SharedDerived since D-135, so the chat and Revit name one folder
        /// on every PC - mcp/companion/heron_companion.py companion_dir() - and
        /// in the user's own temporary folder since D-137, not Revit's own.
        /// </summary>
        public static string Companion { get { return Ensure(Path.Combine(SharedDerived, "companion")); } }

        /// <summary>
        /// DERIVED - where capture_view puts a view's picture for the moment
        /// it takes to read it back (D-123). Each capture writes into its own
        /// folder here and deletes it before it answers; the picture travels
        /// to the chat inside the reply, never as a path. Anything left here
        /// is a capture Revit did not finish, and is safe to delete.
        /// </summary>
        public static string Captures { get { return Ensure(Path.Combine(Derived, "captures")); } }

        /// <summary>DERIVED - logs. Verbose, rotated, disposable.</summary>
        public static string Logs { get { return Ensure(Path.Combine(Derived, "logs")); } }

        /// <summary>
        /// DERIVED - one record per fragment write, named by its workflow id:
        /// every element it changed and its whole reply, which the chat is
        /// given only in short (FRAGMENT-ISSUES 5b-407). Paged back from disk
        /// by the MCP server's heron_run_record; pruned by runs.retainDays.
        ///
        /// DERIVED, NOT DATA, and the difference is the point. The audit
        /// trail under DATA is the evidence and is never pruned; this is the
        /// detail behind one of its lines, kept for a while so a chat can
        /// read what it was not sent - and IsSafeToDelete must be able to say
        /// yes to it, which it never does for anything under DATA.
        /// mcp/client/heron_bridge_client.py names the same folder as
        /// RUNS_DIR, and tests/test_bridge_paths.py holds the two together.
        /// </summary>
        public static string Runs { get { return Ensure(Path.Combine(Derived, "runs")); } }

        /// <summary>DATA - configuration the user or an admin owns.</summary>
        public static string Config { get { return Ensure(Path.Combine(Data, "config")); } }

        /// <summary>
        /// DATA - the audit trail. Deliberately NOT derived: it is evidence
        /// (Golden Rule 14) and must survive a cache wipe.
        /// </summary>
        public static string Audit { get { return Ensure(Path.Combine(Data, "audit")); } }

        /// <summary>
        /// True when the path is safe for an update or a cleanup to delete.
        /// Anything under DATA is the user's and is never touched.
        ///
        /// A FOLDER MERELY NAMED LIKE HERON'S IS NOT INSIDE IT. This used to
        /// ask StartsWith on the bare text, and Path.GetFullPath returns no
        /// trailing separator - so with Derived at %LOCALAPPDATA%\Heron, the
        /// path %LOCALAPPDATA%\HeronBackup\anything started with it and this
        /// answered TRUE. Heron-old, Heron.bak and "Heron backup" did the
        /// same. Nothing in the repository passed such a path, so nothing was
        /// deleted; the defect was that the next caller would not know. This
        /// is a public guard whose whole job is to be the protection, and
        /// platform/README.md presents it as exactly that.
        /// FRAGMENT-ISSUES section 5b, row 1.
        /// </summary>
        public static bool IsSafeToDelete(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string full;
            // A path the operating system cannot even parse is not a path this
            // may call safe. GetFullPath throws on an empty volume, an illegal
            // character or a name too long, and the honest answer to all three
            // is the same one: no.
            try { full = Path.GetFullPath(path); }
            catch (ArgumentException) { return false; }
            catch (NotSupportedException) { return false; }
            catch (PathTooLongException) { return false; }

            if (IsWithin(full, Path.GetFullPath(Data))) return false;
            return IsWithin(full, Path.GetFullPath(Derived))
                || IsWithin(full, Path.GetFullPath(SharedDerived));
        }

        /// <summary>
        /// Whether one path is the folder itself or something inside it -
        /// compared segment by segment rather than character by character.
        ///
        /// The root counts as inside itself: docs/06 lists DERIVED as "safe
        /// to delete at any time, rebuildable", which is a statement about
        /// the whole folder and not only its contents.
        /// </summary>
        private static bool IsWithin(string full, string root)
        {
            if (string.Equals(full, root, StringComparison.OrdinalIgnoreCase)) return true;
            var separator = Path.DirectorySeparatorChar.ToString();
            var prefix = root.EndsWith(separator, StringComparison.Ordinal)
                ? root : root + separator;
            return full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static string Ensure(string path)
        {
            try { Directory.CreateDirectory(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return path;
        }
    }
}
