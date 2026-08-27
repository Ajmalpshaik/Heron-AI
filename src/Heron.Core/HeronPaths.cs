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
        /// </summary>
        public static bool IsSafeToDelete(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var full = Path.GetFullPath(path);
            var data = Path.GetFullPath(Data);
            var derived = Path.GetFullPath(Derived);

            if (full.StartsWith(data, StringComparison.OrdinalIgnoreCase)) return false;
            return full.StartsWith(derived, StringComparison.OrdinalIgnoreCase);
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
