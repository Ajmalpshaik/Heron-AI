// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Heron.Core
{
    /// <summary>
    /// The run records: one file per fragment write, named by its workflow id,
    /// holding what the chat was told only in short.
    ///
    /// WHY THEY EXIST. A write's reply went back into the chat whole, every
    /// call, and two family builds carried 322,725 characters that way, about
    /// 70% of it saying nothing new (FRAGMENT-ISSUES 5b-407). The chat now
    /// gets a short change report, and the full detail - every element the
    /// job added, modified or deleted, and the reply as Revit's side wrote it
    /// - is kept here, where heron_run_record reads it back a page at a time.
    ///
    /// LINKED TO THE AUDIT BY THE WORKFLOW ID, and by nothing else. The audit
    /// line for a job and its record carry the same id, so either finds the
    /// other without a second index to keep in step.
    ///
    /// DERIVED, AND PRUNED. HeronPaths.Runs is under DERIVED: a record is the
    /// detail behind an audit line, not the evidence itself, so it ages out
    /// after runs.retainDays like the verbose log - and no prune here can
    /// reach DATA, because every delete asks HeronPaths.IsSafeToDelete first.
    ///
    /// NEVER THROWS. A record that could not be written costs the chat the
    /// detail, and the reply says so by carrying no `run`; it must never cost
    /// the job, which has already been done to the model by the time this
    /// runs.
    /// </summary>
    public static class HeronRuns
    {
        /// <summary>A record's file extension.</summary>
        public const string Extension = ".json";

        // HeronIdentity.NewWorkflowId's shape - wf-20260827-192001-0003-a1b2 -
        // and nothing wider. The id becomes a FILE NAME, so a separator, a dot
        // or a drive letter in it would be a path; this refuses them all
        // rather than trusting whoever minted it.
        private static readonly Regex RunId = new Regex("^wf-[0-9A-Za-z-]{1,64}$",
                                                        RegexOptions.CultureInvariant);

        /// <summary>Whether an id is a workflow id this may name a file after.</summary>
        public static bool IsRunId(string id)
        {
            return !string.IsNullOrEmpty(id) && RunId.IsMatch(id);
        }

        /// <summary>The record's file in <paramref name="folder"/>, or null for an id that is not one.</summary>
        public static string PathFor(string folder, string id)
        {
            if (string.IsNullOrEmpty(folder) || !IsRunId(id)) return null;
            return Path.Combine(folder, id + Extension);
        }

        /// <summary>Writes one record into HeronPaths.Runs. False when it could not.</summary>
        public static bool Write(string id, string body)
        {
            return Write(HeronPaths.Runs, id, body);
        }

        /// <summary>
        /// Writes one record into <paramref name="folder"/> - whole, through
        /// HeronAtomicWrite, so a reader never meets half a file. False for an
        /// id that is not a workflow id, an empty body, or a disk that would
        /// not take it.
        /// </summary>
        public static bool Write(string folder, string id, string body)
        {
            var path = PathFor(folder, id);
            if (path == null || string.IsNullOrEmpty(body)) return false;
            try
            {
                HeronAtomicWrite.WriteAllText(path, body);
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (ArgumentException) { return false; }
            catch (NotSupportedException) { return false; }
        }

        /// <summary>Prunes HeronPaths.Runs. The number of records deleted.</summary>
        public static int Prune(int retainDays)
        {
            return Prune(HeronPaths.Runs, retainDays, DateTime.UtcNow);
        }

        /// <summary>
        /// Deletes the records in <paramref name="folder"/> last written more
        /// than <paramref name="retainDays"/> days before
        /// <paramref name="nowUtc"/>. The add-in's PruneLogs, for records.
        ///
        /// ONLY A RECORD, AND ONLY WHERE A DELETE IS SAFE. A file is touched
        /// only when its name is a workflow id with the record extension - a
        /// file somebody put beside them is not this method's - and only when
        /// HeronPaths.IsSafeToDelete says yes, so a folder outside DERIVED, or
        /// anything under DATA, loses nothing whatever it holds. Below one day
        /// nothing is pruned: zero is "keep nothing", which no setting should
        /// be able to mean by accident.
        /// </summary>
        public static int Prune(string folder, int retainDays, DateTime nowUtc)
        {
            if (retainDays < 1 || string.IsNullOrEmpty(folder)) return 0;
            var deleted = 0;
            try
            {
                if (!Directory.Exists(folder)) return 0;
                var cutoff = nowUtc.AddDays(-retainDays);
                foreach (var file in Directory.GetFiles(folder, "wf-*" + Extension))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    if (!IsRunId(name)) continue;
                    if (File.GetLastWriteTimeUtc(file) >= cutoff) continue;
                    if (!HeronPaths.IsSafeToDelete(file)) continue;
                    try
                    {
                        File.Delete(file);
                        deleted++;
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return deleted;
        }
    }
}
