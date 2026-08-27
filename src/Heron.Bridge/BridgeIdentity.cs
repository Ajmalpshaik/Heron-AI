// Heron-Agent:  HERON-SES-DIS-001
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  bridge
// See docs/29-metadata-standard.md

using System;
using System.Diagnostics;
using Heron.Core;
using System.Globalization;
using System.IO;
using System.Text;

namespace Heron.Bridge
{
    /// <summary>
    /// Who this bridge is, and how a client finds it.
    ///
    /// Two rules from the field notes (docs/00e, docs/25) are enforced here:
    ///
    ///   1. The pipe name carries the process id. A single shared name is
    ///      single-instance forever - before 2026-08-20 the second Revit
    ///      simply refused to start.
    ///
    ///   2. The discovery file is an ADDRESS BOOK, not a status report. It
    ///      carries only facts that are fixed for the life of the process.
    ///      The open document is deliberately absent: storing it is exactly
    ///      what produced the stale-name trap.
    /// </summary>
    public sealed class BridgeIdentity
    {
        /// <summary>Bumped when the wire format changes. A client that does
        /// not recognise this refuses cleanly rather than half-working.</summary>
        public const int ProtocolVersion = 1;

        public int ProcessId { get; private set; }
        public string RevitVersion { get; private set; }
        public string AddinVersion { get; private set; }
        public string PipeName { get; private set; }
        public DateTime StartedAtUtc { get; private set; }

        public BridgeIdentity(string revitVersion, string addinVersion)
        {
            if (string.IsNullOrWhiteSpace(revitVersion))
                throw new ArgumentException("revitVersion is required", "revitVersion");

            ProcessId = Process.GetCurrentProcess().Id;
            RevitVersion = revitVersion;
            AddinVersion = string.IsNullOrWhiteSpace(addinVersion) ? "0.0.0" : addinVersion;
            StartedAtUtc = DateTime.UtcNow;
            PipeName = string.Format(
                CultureInfo.InvariantCulture, "heron.{0}.{1}", revitVersion, ProcessId);
        }

        /// <summary>
        /// The discovery directory. Owned by the Path Manager, not by this
        /// class - so DERIVED runtime state lives in one place, and an
        /// update can never confuse it with the user's own data
        /// (docs/06 section 2).
        /// </summary>
        public static string DiscoveryDirectory
        {
            get { return HeronPaths.Bridges; }
        }

        public string DiscoveryFilePath
        {
            get
            {
                return Path.Combine(
                    DiscoveryDirectory,
                    ProcessId.ToString(CultureInfo.InvariantCulture) + ".json");
            }
        }

        /// <summary>
        /// Announce this bridge. Static facts only - never the document name.
        /// </summary>
        public void Publish()
        {

            var json = new StringBuilder();
            json.Append("{\n");
            json.AppendFormat(CultureInfo.InvariantCulture, "  \"pid\": {0},\n", ProcessId);
            json.AppendFormat("  \"pipeName\": \"{0}\",\n", Escape(PipeName));
            json.AppendFormat("  \"revitVersion\": \"{0}\",\n", Escape(RevitVersion));
            json.AppendFormat("  \"addinVersion\": \"{0}\",\n", Escape(AddinVersion));
            json.AppendFormat(CultureInfo.InvariantCulture, "  \"protocolVersion\": {0},\n", ProtocolVersion);
            json.AppendFormat("  \"startedAt\": \"{0}\"\n",
                StartedAtUtc.ToString("o", CultureInfo.InvariantCulture));
            json.Append("}\n");

            // Write-then-move so a client never reads a half-written file.
            var tmp = DiscoveryFilePath + ".tmp";
            File.WriteAllText(tmp, json.ToString(), new UTF8Encoding(false));
            if (File.Exists(DiscoveryFilePath)) File.Delete(DiscoveryFilePath);
            File.Move(tmp, DiscoveryFilePath);
        }

        /// <summary>
        /// Remove this bridge's entry. Best effort - Revit can be killed
        /// without ever reaching here, which is exactly why a client must
        /// verify a bridge answers before trusting its file.
        /// </summary>
        public void Unpublish()
        {
            try
            {
                if (File.Exists(DiscoveryFilePath)) File.Delete(DiscoveryFilePath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
