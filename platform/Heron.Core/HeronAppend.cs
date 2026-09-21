// Heron-Agent:  none
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Heron.Core
{
    /// <summary>
    /// One line onto the end of a file that ANOTHER PROCESS may be writing to
    /// at the same moment - or a plain answer that it could not be done.
    ///
    /// WHY THIS EXISTS. Two files in Heron are appended to from more than one
    /// Revit at a time: the audit trail (HeronAudit, one audit-YYYYMM.jsonl
    /// for the user) and the verbose add-in log (one addin-YYYYMMDD.log for
    /// the machine, per day). Both used `lock` around File.AppendAllText, and
    /// both caught IOException so that a full disk could not kill a Revit
    /// session. That is right. What was wrong is the SCOPE of the lock: a
    /// `lock` serialises the threads of ONE process, and every Revit runs its
    /// own add-in in its own process, so the second one's append simply threw
    /// and the line vanished with nobody told.
    ///
    /// MEASURED, NOT REASONED (FRAGMENT-ISSUES section 5b, rows 5 and 19). A
    /// program held the file exactly as File.AppendAllText does - FileMode
    /// Append, FileAccess Write, and crucially FileShare.READ - and then
    /// appended again the way a second process would. IOException: "the
    /// process cannot access the file because it is being used by another
    /// process", and the file ended with one line where two were written.
    ///
    /// WHAT THIS DOES DIFFERENTLY, in three parts, because each alone leaves
    /// a hole:
    ///
    ///   1. FileShare.ReadWrite, so a second writer is not refused at the
    ///      door. This is the part that makes two Revits possible at all -
    ///      and it only works because BOTH of them come through here.
    ///
    ///   2. A named system-wide mutex, so the two do not interleave inside
    ///      one line. Sharing the file is not the same as taking turns.
    ///
    ///   3. A short retry, and then AN HONEST FALSE. It returns whether the
    ///      line was written, so a caller can leave a mark. Golden Rule 14
    ///      makes the audit evidence; a hole in evidence that nothing records
    ///      is the one failure evidence cannot have.
    ///
    /// It never throws. A caller in the middle of a Revit transaction must
    /// not lose its work because a log file was busy.
    /// </summary>
    public static class HeronAppend
    {
        // The collision window is a single append, so the wait is short and
        // the retries are few. A writer that cannot get in after this is not
        // contending with a sibling - it is blocked by something else, and
        // saying so quickly beats holding up Revit.
        private const int Attempts = 5;
        private const int PauseMs = 20;
        private const int MutexWaitMs = 2000;

        /// <summary>
        /// Append one line. True when it is on disk, false when it is not.
        /// </summary>
        public static bool Line(string path, string text)
        {
            if (string.IsNullOrEmpty(path)) return false;

            var bytes = new UTF8Encoding(false).GetBytes((text ?? string.Empty) + Environment.NewLine);

            Mutex gate = null;
            var held = false;
            try
            {
                gate = Gate(path);
                held = Take(gate);

                // ONLY BUSY IS WORTH ANOTHER GO. `Write` told the two apart
                // in its own comments from the first day - "busy: worth
                // another attempt" against "permissions: retrying changes
                // nothing" - and then returned the same `false` for both, so
                // this loop slept 20, 40, 60 and 80 ms on a path that was
                // never going to succeed, per line, on every line. Not a
                // correctness fault; it is a comment that described a
                // distinction the code did not make, in a file whose whole
                // subject is telling two failures apart.
                for (var attempt = 0; attempt < Attempts; attempt++)
                {
                    bool worthRetrying;
                    if (Write(path, bytes, out worthRetrying)) return true;
                    if (!worthRetrying) return false;
                    Thread.Sleep(PauseMs * (attempt + 1));
                }
                return false;
            }
            catch (Exception)
            {
                // Including the case where a named mutex cannot be created at
                // all. There is no configuration in which losing a log line
                // may take down the operation being logged.
                return false;
            }
            finally
            {
                if (gate != null)
                {
                    if (held)
                    {
                        try { gate.ReleaseMutex(); } catch (ApplicationException) { }
                    }
                    gate.Close();
                }
            }
        }

        /// <summary>
        /// One append. True when it landed; `worthRetrying` says whether a
        /// second go could answer differently.
        ///
        /// THE DISTINCTION WAS IN THE COMMENTS AND NOT IN THE RETURN. A busy
        /// file is the whole reason this class exists and clears in
        /// milliseconds; a permission or a path nobody can write to will
        /// answer the same way five times over. Both came back as a bare
        /// `false`, so the caller slept through 200 ms of guaranteed failure
        /// for every line it tried to write. FRAGMENT-ISSUES section 5b.
        /// </summary>
        private static bool Write(string path, byte[] bytes, out bool worthRetrying)
        {
            worthRetrying = false;
            try
            {
                var folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                // FileShare.ReadWrite is the whole point. A reader tailing the
                // file is welcome too - this is a log, and somebody watching
                // it must never be the reason a line is lost.
                using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write,
                                                   FileShare.ReadWrite, 4096))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }
                return true;
            }
            catch (IOException)
            {
                worthRetrying = true;          // busy: worth another attempt
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;                  // permissions: retrying changes nothing
            }
            catch (ArgumentException)
            {
                return false;                  // a path nobody can write to
            }
        }

        /// <summary>
        /// The mutex that serialises writers to ONE path, across processes.
        ///
        /// Named after the file rather than after Heron, so the audit trail
        /// and the verbose log never wait for each other. A mutex name may
        /// not contain a path separator, so the path is hashed - and the hash
        /// only has to be stable and collision-unlikely, never secret.
        /// </summary>
        private static Mutex Gate(string path)
        {
            var name = "Heron.Append." + Fingerprint(path.ToUpperInvariant());
            try
            {
                // Global\ reaches across terminal-server sessions, which is
                // where a second Revit can hide. Some accounts may not create
                // one, so a refusal falls back to the session-local name
                // rather than giving up the serialising altogether.
                return new Mutex(false, "Global\\" + name);
            }
            catch (Exception)
            {
                return new Mutex(false, name);
            }
        }

        private static bool Take(Mutex gate)
        {
            try
            {
                return gate.WaitOne(MutexWaitMs, false);
            }
            catch (AbandonedMutexException)
            {
                // A process died holding it. This call OWNS the mutex now,
                // and the file is in whatever state that process left it -
                // which for an append is either a whole line or nothing.
                return true;
            }
        }

        /// <summary>
        /// A short, stable, filesystem-free name for a path.
        ///
        /// Deliberately not a cryptographic hash: nothing here is a secret,
        /// and MD5 would need a using that invites somebody to reach for it
        /// where it does matter.
        /// </summary>
        private static string Fingerprint(string text)
        {
            unchecked
            {
                // FNV-1a, 64-bit. Small, well known, and no dependency.
                const ulong Offset = 14695981039346656037UL;
                const ulong Prime = 1099511628211UL;

                var hash = Offset;
                foreach (var c in text)
                {
                    hash ^= (byte)(c & 0xFF);
                    hash *= Prime;
                    hash ^= (byte)(c >> 8);
                    hash *= Prime;
                }
                return hash.ToString("x16");
            }
        }
    }
}
