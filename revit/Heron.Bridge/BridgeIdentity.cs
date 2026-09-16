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
using System.Security.Cryptography;
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
        // 2 since Step 6: a request now carries a "client" id, and the lease
        // (HeronLease) decides who may send anything at all. An older client
        // sends no id and would be refused as anonymous - which is correct but
        // reads as a fault, so the version is bumped instead and the mismatch
        // is reported as what it is: restart that Revit to finish updating.
        public const int ProtocolVersion = 2;

        public int ProcessId { get; private set; }
        public string RevitVersion { get; private set; }
        public string AddinVersion { get; private set; }
        public string PipeName { get; private set; }
        public DateTime StartedAtUtc { get; private set; }

        /// <summary>
        /// The shared secret for this connected session. Minted on every
        /// connect and cleared on disconnect, so a client still holding one
        /// from an earlier session is refused rather than quietly served.
        ///
        /// What it is for, honestly: the pipe's ACL already limits it to this
        /// user, so this is not what keeps other people out. It stops another
        /// process running as the SAME user from reaching Revit by guessing a
        /// pipe name - it would have to read the discovery file first, which
        /// makes reaching Revit a deliberate act rather than an accident.
        /// </summary>
        public string Token { get; private set; }

        /// <summary>
        /// Starts a session: a new token, invalidating every earlier one.
        /// Called on connect, before the bridge is announced.
        /// </summary>
        public void BeginSession()
        {
            var bytes = new byte[24];
            // Create() rather than new RNGCryptoServiceProvider(): .NET 8 reports
            // that obsolete (SYSLIB0023) and Revit 2025+ builds on it, while
            // Create() exists on .NET Framework 4.7.2 too. Same platform CSPRNG.
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            Token = Convert.ToBase64String(bytes);
        }

        /// <summary>Ends the session. A token that no longer exists cannot be replayed.</summary>
        public void EndSession()
        {
            Token = null;
        }

        /// <summary>
        /// Compares in constant time. A plain comparison returns on the first
        /// differing character, which leaks the token one character at a time
        /// to anything able to measure how long the reply took.
        /// </summary>
        public bool TokenMatches(string candidate)
        {
            var expected = Token;
            if (expected == null || candidate == null) return false;
            if (expected.Length != candidate.Length) return false;

            var difference = 0;
            for (var i = 0; i < expected.Length; i++)
                difference |= expected[i] ^ candidate[i];
            return difference == 0;
        }

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
            json.AppendFormat("  \"token\": \"{0}\",\n", Escape(Token));
            json.AppendFormat("  \"startedAt\": \"{0}\"\n",
                StartedAtUtc.ToString("o", CultureInfo.InvariantCulture));
            json.Append("}\n");

            // Atomic, so a client never reads a half-written file - and never
            // reads NO file either. Until 2026-09-16 this deleted before it
            // moved, and that second case was not covered by the sentence this
            // comment used to carry: a client arriving in the gap finds no
            // discovery file and concludes no bridge is running, which is a
            // wrong answer rather than a missing one. See HeronAtomicWrite.
            HeronAtomicWrite.WriteAllText(DiscoveryFilePath, json.ToString());
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
