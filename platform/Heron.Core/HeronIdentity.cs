// Heron-Agent:  HERON-KRN-IDN-002
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Heron.Core
{
    /// <summary>
    /// Identity Manager - stable identity for the things Heron must be able
    /// to name later.
    ///
    /// Golden Rule 10: every important object has identity, version and
    /// lifecycle. This owns the first of the three.
    ///
    /// Two kinds of identity, for two very different lifetimes:
    ///
    ///   INSTALLATION  One id per Heron installation, persisted. Survives
    ///                 restarts and updates. Lets a log written last month be
    ///                 attributed to this install.
    ///
    ///   WORKFLOW      One id per user request, generated. The correlation key
    ///                 that ties one sentence to every agent, retrieval,
    ///                 model call and element touched (docs/21 section 13).
    ///                 This is what makes "what did Heron change?" a query.
    /// </summary>
    public static class HeronIdentity
    {
        private static readonly object Gate = new object();
        private static string _installationId;
        private static long _sequence;

        /// <summary>
        /// This installation's id. Created once, then read for ever.
        ///
        /// Deliberately random and carrying nothing about the machine or the
        /// person - it exists to correlate logs, not to identify anybody.
        /// </summary>
        public static string InstallationId
        {
            get
            {
                if (_installationId != null) return _installationId;

                lock (Gate)
                {
                    if (_installationId != null) return _installationId;

                    var path = Path.Combine(HeronPaths.Config, "installation-id");
                    try
                    {
                        if (File.Exists(path))
                        {
                            var existing = File.ReadAllText(path).Trim();
                            if (existing.Length > 0)
                            {
                                _installationId = existing;
                                return _installationId;
                            }
                        }

                        _installationId = Guid.NewGuid().ToString("N").Substring(0, 16);
                        File.WriteAllText(path, _installationId, new UTF8Encoding(false));
                    }
                    catch (IOException)
                    {
                        // Unwritable config is not fatal. A volatile id still
                        // correlates everything within this session.
                        _installationId = Guid.NewGuid().ToString("N").Substring(0, 16);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        _installationId = Guid.NewGuid().ToString("N").Substring(0, 16);
                    }

                    return _installationId;
                }
            }
        }

        /// <summary>
        /// A new workflow id: one per user request.
        ///
        /// Sortable, readable, and unique without coordination - the time
        /// prefix makes a log skimmable by eye, and the counter keeps two
        /// requests in the same second apart.
        ///
        ///     wf-20260827-192001-0003-a1b2
        /// </summary>
        public static string NewWorkflowId()
        {
            var n = Interlocked.Increment(ref _sequence);
            return string.Format(
                CultureInfo.InvariantCulture,
                "wf-{0:yyyyMMdd-HHmmss}-{1:D4}-{2}",
                DateTime.UtcNow,
                n % 10000,
                Guid.NewGuid().ToString("N").Substring(0, 4));
        }

        /// <summary>
        /// A stable id for a knowledge object - fragment, skill, capability.
        ///
        /// Derived from the semantic identity, never from a filename, so
        /// renaming a file does not create a second object (docs/09 section 4).
        /// </summary>
        public static string ForKnowledge(string kind, string semanticIdentity)
        {
            if (string.IsNullOrWhiteSpace(kind))
                throw new ArgumentException("kind is required", "kind");
            if (string.IsNullOrWhiteSpace(semanticIdentity))
                throw new ArgumentException("semanticIdentity is required", "semanticIdentity");

            var slug = Slug(semanticIdentity);
            return kind.Trim().ToUpperInvariant() + "-" + slug;
        }

        private static string Slug(string value)
        {
            var sb = new StringBuilder(value.Length);
            var lastDash = false;
            foreach (var c in value.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    lastDash = false;
                }
                else if (!lastDash && sb.Length > 0)
                {
                    sb.Append('-');
                    lastDash = true;
                }
            }
            var slug = sb.ToString().TrimEnd('-');
            return slug.Length == 0 ? "unnamed" : slug;
        }
    }
}
