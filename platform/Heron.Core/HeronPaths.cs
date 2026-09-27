// Heron-Agent:  HERON-WSP-PTH-007
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.IO;
using System.Reflection;

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

        /// <summary>DERIVED - logs. Verbose, rotated, disposable.</summary>
        public static string Logs { get { return Ensure(Path.Combine(Derived, "logs")); } }

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
            return IsWithin(full, Path.GetFullPath(Derived));
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
