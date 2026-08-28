// Heron-Agent:  HERON-KRN-CFG-001
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Heron.Core
{
    /// <summary>
    /// Configuration Manager.
    ///
    /// Configuration is a SECURITY BOUNDARY, not a settings bag
    /// (docs/21 section 9). Two rules follow, and both are enforced here:
    ///
    ///   1. It lives under DATA, so an update can never overwrite it.
    ///
    ///   2. Nothing Heron READS may change it - not a document, not a family
    ///      name, not a community package. Only a person, deliberately.
    ///      That is Golden Rule 19, and it is why there is a Load and a Save
    ///      but no ApplyFromRequest.
    ///
    /// Every key here is read by something. A setting that is declared,
    /// documented and then ignored is worse than no setting: it is a promise
    /// the code does not keep. log.verbose went for exactly that reason, and
    /// bridge.listeners followed once the newest connection began winning
    /// outright - a pool size stopped being a number worth tuning. Either can
    /// come back when there is something for it to do.
    ///
    /// Deliberately flat key/value. Step 1 has three settings; a schema would
    /// be ceremony. When it outgrows this - Phase 1 or 2 - replace it with a
    /// typed, versioned, migratable config and delete this without regret.
    /// </summary>
    public sealed class HeronConfig
    {
        private const string FileName = "heron.config";

        private readonly Dictionary<string, string> _values =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Defaults. Every key Heron understands is declared here.</summary>
        private static readonly Dictionary<string, string> Defaults =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "bridge.autoConnect", "false" },      // connecting stays explicit, by design
                { "bridge.idleReleaseMinutes", "3" },    // safety net; preemption is the real mechanism

                // Revit runs a queued request only when it is idle. These two
                // must stay BELOW the client's own deadline, so the bridge is
                // what answers "Revit is busy" - a client that gives up first
                // can only say "no answer", which tells the user nothing.
                { "revit.busyTimeoutSeconds", "10" },    // Revit never picked the request up
                { "revit.operationTimeoutSeconds", "60" },  // it started, but has not finished

                // MUST be declared here, not only read by HeronPermissions.
                // Load() adopts a file value ONLY for a key in this table, so
                // an undeclared key is silently ignored - and a setting that
                // is silently ignored while a refusal tells the user to set it
                // is the most opaque failure this file can produce. Off by
                // default (D-19); it is turned on deliberately or not at all.
                { "write.enabled", "false" },

                { "log.retainDays", "14" },
            };

        public static string FilePath { get { return Path.Combine(HeronPaths.Config, FileName); } }

        public static HeronConfig Load()
        {
            var config = new HeronConfig();
            foreach (var kv in Defaults) config._values[kv.Key] = kv.Value;

            try
            {
                if (!File.Exists(FilePath)) return config;

                foreach (var raw in File.ReadAllLines(FilePath))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line[0] == '#') continue;

                    var i = line.IndexOf('=');
                    if (i <= 0) continue;

                    var key = line.Substring(0, i).Trim();
                    var value = line.Substring(i + 1).Trim();

                    // An unknown key is ignored, not adopted. Configuration is
                    // a closed set; anything else is noise or an attempt.
                    if (Defaults.ContainsKey(key)) config._values[key] = value;
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return config;
        }

        /// <summary>
        /// Writes the current values. ADMIN-level in the permission model -
        /// this is only ever reached from a deliberate human action.
        /// </summary>
        public void Save()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Heron configuration");
            sb.AppendLine("# Owned by you. Never overwritten by an update.");
            sb.AppendLine("# Unknown keys are ignored - see docs/21 section 9.");
            sb.AppendLine();
            foreach (var kv in _values)
                sb.AppendLine(kv.Key + " = " + kv.Value);

            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(false));
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(tmp, FilePath);
        }

        public string Get(string key)
        {
            string value;
            if (_values.TryGetValue(key, out value)) return value;
            return Defaults.ContainsKey(key) ? Defaults[key] : null;
        }

        public int GetInt(string key, int fallback)
        {
            int parsed;
            var raw = Get(key);
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                ? parsed : fallback;
        }

        public bool GetBool(string key, bool fallback)
        {
            var raw = Get(key);
            if (string.IsNullOrEmpty(raw)) return fallback;
            return raw.Equals("true", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("1", StringComparison.Ordinal)
                || raw.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Sets a known key. Rejects anything not declared in the defaults -
        /// configuration is a closed set, deliberately.
        /// </summary>
        public void Set(string key, string value)
        {
            if (!Defaults.ContainsKey(key))
                throw new ArgumentException("'" + key + "' is not a Heron setting.", "key");
            _values[key] = value;
        }

        public IEnumerable<KeyValuePair<string, string>> All() { return _values; }
    }
}
